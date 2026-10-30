#import <Foundation/Foundation.h>
#import <Vision/Vision.h>
#import <ImageIO/ImageIO.h>
#import <CoreGraphics/CoreGraphics.h>
#import <dispatch/dispatch.h>
#include <cmath>

extern "C" void UnitySendMessage(const char *objectName, const char *methodName, const char *message);

static NSMutableSet<NSString *> *SBWCancelledRequests()
{
    static NSMutableSet<NSString *> *requests;
    static dispatch_once_t once;
    dispatch_once(&once, ^{ requests = [NSMutableSet new]; });
    return requests;
}

static BOOL SBWConsumeCancellation(NSString *requestId)
{
    NSMutableSet<NSString *> *requests = SBWCancelledRequests();
    @synchronized (requests)
    {
        if (![requests containsObject:requestId]) return NO;
        [requests removeObject:requestId];
        return YES;
    }
}

static void SBWSend(NSString *objectName, NSString *requestId, BOOL success,
                    NSString *errorCode, NSString *detail, NSArray *labels)
{
    NSDictionary *payload = @{
        @"success": @(success), @"requestId": requestId ?: @"",
        @"engine": @"Apple Vision", @"error": errorCode ?: @"", @"detail": detail ?: @"",
        @"labels": labels ?: @[]
    };
    NSData *json = [NSJSONSerialization dataWithJSONObject:payload options:0 error:NULL];
    NSString *text = [[NSString alloc] initWithData:json encoding:NSUTF8StringEncoding];
    if (text == nil) return;
    dispatch_async(dispatch_get_main_queue(), ^{
        if (SBWConsumeCancellation(requestId)) return;
        UnitySendMessage(objectName.UTF8String, "OnNativeScanResult", text.UTF8String);
    });
}

extern "C" int SBW_IsVisionAvailable()
{
    if (@available(iOS 13.0, *)) return 1;
    return 0;
}

extern "C" void SBW_CancelScan(const char *requestIdUtf8)
{
    if (requestIdUtf8 == nullptr) return;
    NSString *requestId = [NSString stringWithUTF8String:requestIdUtf8];
    if (requestId.length == 0) return;
    NSMutableSet<NSString *> *requests = SBWCancelledRequests();
    @synchronized (requests) { [requests addObject:requestId]; }
}

extern "C" void SBW_ClassifyRgba(const unsigned char *rgba, int byteCount, int width, int height,
                                  int orientation, const char *callbackObjectUtf8,
                                  const char *requestIdUtf8)
{
    if (callbackObjectUtf8 == nullptr || requestIdUtf8 == nullptr) return;
    NSString *objectName = [NSString stringWithUTF8String:callbackObjectUtf8];
    NSString *requestId = [NSString stringWithUTF8String:requestIdUtf8];
    if (objectName.length == 0 || requestId.length == 0) return;
    if (rgba == nullptr || width <= 0 || height <= 0 || width > 2048 || height > 2048 ||
        byteCount != width * height * 4 ||
        (orientation != 1 && orientation != 3 && orientation != 6 && orientation != 8))
    {
        SBWSend(objectName, requestId, NO, @"invalid_image", @"Invalid RGBA camera frame.", @[]);
        return;
    }
    if (@available(iOS 13.0, *))
    {
        // Copy synchronously: managed byte[] pinning ends when this call returns.
        NSData *pixels = [NSData dataWithBytes:rgba length:(NSUInteger)byteCount];
        static dispatch_queue_t queue;
        static dispatch_once_t once;
        dispatch_once(&once, ^{ queue = dispatch_queue_create("kr.seongbuk.waste.vision", DISPATCH_QUEUE_SERIAL); });
        dispatch_async(queue, ^{
            @autoreleasepool
            {
                if (SBWConsumeCancellation(requestId)) return;
                CGDataProviderRef provider = CGDataProviderCreateWithCFData((__bridge CFDataRef)pixels);
                CGColorSpaceRef colorSpace = CGColorSpaceCreateDeviceRGB();
                if (provider == NULL || colorSpace == NULL)
                {
                    if (provider != NULL) CGDataProviderRelease(provider);
                    if (colorSpace != NULL) CGColorSpaceRelease(colorSpace);
                    SBWSend(objectName, requestId, NO, @"invalid_image", @"Unable to allocate image provider.", @[]);
                    return;
                }
                CGImageRef image = CGImageCreate((size_t)width, (size_t)height, 8, 32, (size_t)width * 4,
                    colorSpace, (CGBitmapInfo)(kCGImageAlphaNoneSkipLast | kCGBitmapByteOrder32Big),
                    provider, NULL, false, kCGRenderingIntentDefault);
                CGColorSpaceRelease(colorSpace);
                CGDataProviderRelease(provider);
                if (image == NULL)
                {
                    SBWSend(objectName, requestId, NO, @"invalid_image", @"Unable to create camera image.", @[]);
                    return;
                }
                CGImageRef focusedImage = NULL;
                @try
                {
                    // A second, tighter view helps when the photographed item
                    // occupies only part of the frame. Keep the wider view too:
                    // zooming alone can remove a can's rim or a box's edges.
                    const size_t focusWidth = MAX((size_t)1, (size_t)std::floor(width * 0.70));
                    const size_t focusHeight = MAX((size_t)1, (size_t)std::floor(height * 0.70));
                    focusedImage = CGImageCreateWithImageInRect(image, CGRectMake(
                        ((size_t)width - focusWidth) / 2, ((size_t)height - focusHeight) / 2,
                        focusWidth, focusHeight));
                    CGImageRef views[2] = { image, focusedImage };
                    const NSUInteger viewCount = focusedImage != NULL ? 2 : 1;
                    NSMutableDictionary<NSString *, NSNumber *> *scores = [NSMutableDictionary new];
                    NSError *lastError = nil;
                    NSUInteger completedViews = 0;
                    for (NSUInteger view = 0; view < viewCount; view++)
                    {
                        // Navigation or a timeout must also suppress a queued
                        // zoom pass, rather than classifying a cancelled scan.
                        if (SBWConsumeCancellation(requestId)) return;
                        VNClassifyImageRequest *request = [[VNClassifyImageRequest alloc] init];
                        VNImageRequestHandler *handler = [[VNImageRequestHandler alloc]
                            initWithCGImage:views[view]
                            orientation:(CGImagePropertyOrientation)orientation options:@{}];
                        NSError *error = nil;
                        BOOL performed = [handler performRequests:@[request] error:&error];
                        if (!performed || error != nil)
                        {
                            lastError = error;
                            continue;
                        }
                        completedViews++;
                        NSArray<VNClassificationObservation *> *observations =
                            [request.results sortedArrayUsingComparator:^NSComparisonResult(
                                id first, id second) {
                                VNClassificationObservation *left = first;
                                VNClassificationObservation *right = second;
                                if (left.confidence > right.confidence) return NSOrderedAscending;
                                if (left.confidence < right.confidence) return NSOrderedDescending;
                                return [left.identifier compare:right.identifier];
                            }];
                        NSUInteger retained = 0;
                        for (VNClassificationObservation *observation in observations)
                        {
                            if (retained == 16) break;
                            if (observation.identifier.length == 0 || !std::isfinite(observation.confidence) ||
                                observation.confidence <= 0 || observation.confidence > 1) continue;
                            retained++;
                            NSNumber *previous = scores[observation.identifier];
                            // Scores are neither summed nor treated as an
                            // accuracy percentage. Preserve an actual model
                            // observation when combining the two views.
                            if (previous == nil || observation.confidence > previous.floatValue)
                                scores[observation.identifier] = @(observation.confidence);
                        }
                    }
                    NSArray<NSString *> *identifiers =
                        [scores.allKeys sortedArrayUsingComparator:^NSComparisonResult(id first, id second) {
                            float left = [scores[first] floatValue];
                            float right = [scores[second] floatValue];
                            if (left > right) return NSOrderedAscending;
                            if (left < right) return NSOrderedDescending;
                            return [first compare:second];
                        }];
                    NSMutableArray *labels = [NSMutableArray arrayWithCapacity:32];
                    for (NSString *identifier in identifiers)
                    {
                        if (labels.count == 32) break;
                        [labels addObject:@{@"identifier": identifier, @"confidence": scores[identifier]}];
                    }
                    NSString *detail = [NSString stringWithFormat:@"Classified %lu of %lu camera views.",
                        (unsigned long)completedViews, (unsigned long)viewCount];
                    if (completedViews == 0)
                        SBWSend(objectName, requestId, NO, @"vision_failed", lastError.localizedDescription, @[]);
                    else
                        SBWSend(objectName, requestId, labels.count > 0,
                            labels.count > 0 ? @"" : @"no_classifications", detail, labels);
                }
                @catch (NSException *exception)
                {
                    SBWSend(objectName, requestId, NO, @"vision_failed", exception.reason, @[]);
                }
                @finally
                {
                    if (focusedImage != NULL) CGImageRelease(focusedImage);
                    CGImageRelease(image);
                }
            }
        });
    }
    else
    {
        SBWSend(objectName, requestId, NO, @"unsupported_ios", @"Image classification requires iOS 13 or newer.", @[]);
    }
}
