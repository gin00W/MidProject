# On-device iPhone image classification

`WasteScanController` captures one center-cropped rear AR camera frame when
`RequestScan()` is called. The iOS native plug-in copies the RGBA bytes and runs
Apple Vision's `VNClassifyImageRequest` on a serial background queue. Results are
JSON-encoded and delivered to Unity's main thread through a unique callback
GameObject. Images are not sent to a server and no API credential is required.

## Integration

```csharp
scanner.cameraManager = arCameraManager;
scanner.Completed += response =>
{
    // These are classifier candidates, not a verified waste category.
    // Map conservatively, ask the user to confirm the item, then ask about
    // material and contamination before giving disposal guidance.
    if (response.success) ShowCandidates(response.labels);
};
bool started = scanner.RequestScan();
// If false, display scanner.Status. No fake Editor recognition is emitted.
```

- `IsSupported`: true only in an iOS player with the native Vision capability.
- `IsBusy`: true until completion, cancellation or a 25-second timeout.
- `Status`: Korean status message suitable for the UI.
- `CancelScan()`: cancels a pending UI request and suppresses stale callbacks.
- `LatestCapturedFrame`: upright snapshot texture for UI; it is owned, replaced
  and destroyed by the controller. Consumers must not destroy it.
- `ScanResponse.labels`: up to 12 actual Vision identifiers and confidence
  values. `ScanPayloadRules` validates that envelope before the UI receives it.

The iOS build postprocessor links Vision, ImageIO and CoreGraphics frameworks to
UnityFramework and enables ARC and Objective-C exceptions for this native plug-in file.
The latter is required by its `@try`/`@catch`/`@finally` error-handling code, even if
Unity disables Objective-C exceptions for the rest of UnityFramework.
For an already exported project that reports `Cannot use '@try' with Objective-C exceptions disabled`,
set **UnityFramework > Build Settings > Enable Objective-C Exceptions** to **Yes** and rebuild.
ARKit, camera permission text, rear camera configuration and an
active AR session still need to be configured in the scene/project.

## Accuracy and verification boundaries

Apple's built-in taxonomy describes general image subjects and depends on the
OS/model revision. It is not a trained Seoul waste classifier. A confidence
value does not verify the material, food residue, oil contamination, remaining
contents or local disposal rules. Never force an answer when no label maps to a
supported item; show a manual choice/search option instead.

The app is currently portrait-oriented; rear sensor orientation is supplied as
EXIF Right in portrait, with rear landscape and upside-down mappings included.
Actual camera orientation, recognition performance, iOS compilation and device
behavior require validation in Xcode/on an iPhone. They have not been verified
in this Linux environment. `ScanModels.cs` and `ScanFrameOrientation.cs` have no
Unity dependency and can be compiled into a .NET regression harness.

Official API references:

- https://developer.apple.com/documentation/vision/vnclassifyimagerequest
- https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.3/manual/features/camera/image-capture.html
- https://docs.unity3d.com/6000.3/Documentation/Manual/PluginsForIOS.html
