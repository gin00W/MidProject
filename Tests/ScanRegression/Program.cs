using System;
using System.Linq;
using System.Text.Json;
using SeongbukAR.Scanning;

static class Program
{
    static int _passed;
    static int _failed;

    static int Main()
    {
        Run("native success JSON with ranked labels is accepted", () =>
        {
            const string json = "{\"success\":true,\"requestId\":\"scan-1\",\"engine\":\"Apple Vision\",\"error\":\"\",\"detail\":\"\",\"labels\":[{\"identifier\":\"bottle\",\"confidence\":0.73},{\"identifier\":\"container\",\"confidence\":0.14}]}";
            var response = JsonSerializer.Deserialize<ScanResponse>(json,
                new JsonSerializerOptions { IncludeFields = true });
            Check(ScanPayloadRules.IsValid(response), "Valid native envelope rejected.");
            Check(response.labels[0].identifier == "bottle" && response.labels[0].confidence > .7f,
                "Labels/confidence did not deserialize.");
        });
        Run("native failure envelope is accepted without guessed labels", () =>
        {
            var response = new ScanResponse { success = false, requestId = "scan-1", error = "vision_failed" };
            Check(ScanPayloadRules.IsValid(response), "Valid failure rejected.");
        });
        Run("successful payload requires labels and request identity", () =>
        {
            var response = ValidResponse();
            response.labels = Array.Empty<ScanLabel>();
            Check(!ScanPayloadRules.IsValid(response), "Empty labels accepted.");
            response = ValidResponse();
            response.requestId = "";
            Check(!ScanPayloadRules.IsValid(response), "Missing request ID accepted.");
        });
        Run("two-view result accepts 32 labels but rejects an oversized payload", () =>
        {
            var response = ValidResponse();
            response.labels = Enumerable.Range(0, 32).Select(i =>
                new ScanLabel { identifier = "label-" + i, confidence = .3f }).ToArray();
            Check(ScanPayloadRules.IsValid(response), "Two-view label envelope rejected.");
            response.labels = response.labels.Concat(new[] { response.labels[0] }).ToArray();
            Check(!ScanPayloadRules.IsValid(response), "Oversized envelope accepted.");
        });
        Run("invalid confidence and empty label values are rejected", () =>
        {
            foreach (float value in new[] { float.NaN, float.PositiveInfinity, -.1f, 1.1f })
            {
                var response = ValidResponse();
                response.labels[0].confidence = value;
                Check(!ScanPayloadRules.IsValid(response), "Invalid confidence accepted: " + value);
            }
            var empty = ValidResponse();
            empty.labels[0].identifier = " ";
            Check(!ScanPayloadRules.IsValid(empty), "Empty identifier accepted.");
        });
        Run("duplicate scan begins cannot overwrite a running request", () =>
        {
            var state = new ScanRequestState();
            Check(state.TryBegin("first"), "First request did not begin.");
            Check(!state.TryBegin("second"), "Busy request was overwritten.");
            Check(state.IsCurrent("first"), "Current request changed.");
        });
        Run("cancellation permits another scan and suppresses stale completion", () =>
        {
            var state = new ScanRequestState();
            state.TryBegin("first");
            Check(state.Cancel() == "first" && !state.IsBusy, "Cancel did not clear request.");
            Check(state.TryBegin("second"), "Re-request blocked after cancellation.");
            Check(!state.TryComplete("first") && state.IsCurrent("second"), "Stale result replaced new scan.");
            Check(state.TryComplete("second") && !state.IsBusy, "Current result did not complete.");
        });
        Run("completion and repeated cancellation are idempotent", () =>
        {
            var state = new ScanRequestState();
            Check(!state.TryBegin(" "), "Blank request ID accepted.");
            state.TryBegin("first");
            Check(state.TryComplete("first"), "Expected completion failed.");
            Check(!state.TryComplete("first"), "Completed request completed twice.");
            Check(state.Cancel() == null && state.Cancel() == null, "Idle cancel changed state.");
            Check(state.TryBegin("next"), "A completed scan prevented another request.");
        });
        Run("upright camera rows become Unity bottom-left rows", () =>
            CheckOrientation(1, new byte[] { 5, 6, 3, 4, 1, 2 }));
        Run("upside-down camera orientation rotates every pixel", () =>
            CheckOrientation(3, new byte[] { 2, 1, 4, 3, 6, 5 }));
        Run("portrait right orientation rotates every pixel", () =>
            CheckOrientation(6, new byte[] { 6, 4, 2, 5, 3, 1 }));
        Run("portrait left orientation rotates every pixel", () =>
            CheckOrientation(8, new byte[] { 1, 3, 5, 2, 4, 6 }));
        Run("invalid pixel buffers and mirrored orientations are rejected", () =>
        {
            RejectPixels(new byte[4], 2, 3, 6);
            RejectPixels(new byte[24], 2, 3, 2);
            RejectPixels(new byte[0], 0, 3, 6);
        });
        Run("tall portrait display crops only the visible aiming square", () =>
        {
            var crop = ScanCaptureGeometry.CenteredVisibleSquare(1920, 1440, 1170, 2532, 6);
            Check(crop.Side == 550 && crop.X == 685 && crop.Y == 445,
                "Tall display crop includes off-screen sensor background.");
        });
        Run("iPad portrait aiming square stays at the camera center", () =>
        {
            var crop = ScanCaptureGeometry.CenteredVisibleSquare(1920, 1440, 1640, 2360, 6);
            Check(crop.Side == 827 && crop.X == 546 && crop.Y == 306,
                "iPad crop no longer matches the centered aiming frame.");
        });
        Run("rotating a device preserves the visible square size", () =>
        {
            var portrait = ScanCaptureGeometry.CenteredVisibleSquare(1920, 1440, 1170, 2532, 6);
            var landscape = ScanCaptureGeometry.CenteredVisibleSquare(1920, 1440, 2532, 1170, 1);
            var upsideDown = ScanCaptureGeometry.CenteredVisibleSquare(1920, 1440, 1170, 2532, 8);
            var landscapeRight = ScanCaptureGeometry.CenteredVisibleSquare(1920, 1440, 2532, 1170, 3);
            Check(portrait.Side == landscape.Side && portrait.Side == upsideDown.Side &&
                portrait.Side == landscapeRight.Side, "Screen rotation changed the framing scale.");
        });
        Run("camera crops stay in bounds and project inside the visible screen", () =>
        {
            foreach (var image in new[] { (1920, 1440), (4032, 3024), (1280, 720), (641, 479) })
            foreach (var screen in new[] { (1170, 2532), (1640, 2360), (2360, 1640), (1024, 1024) })
            foreach (int orientation in new[] { 1, 3, 6, 8 })
            {
                var crop = ScanCaptureGeometry.CenteredVisibleSquare(image.Item1, image.Item2,
                    screen.Item1, screen.Item2, orientation);
                Check(crop.X >= 0 && crop.Y >= 0 && crop.Side > 0 &&
                    crop.X + crop.Side <= image.Item1 && crop.Y + crop.Side <= image.Item2,
                    "Crop exceeds sensor bounds.");
                Check(Math.Abs(crop.X + crop.Side / 2.0 - image.Item1 / 2.0) <= .5 &&
                    Math.Abs(crop.Y + crop.Side / 2.0 - image.Item2 / 2.0) <= .5,
                    "Crop drifted from the camera center.");
                int width = orientation == 6 || orientation == 8 ? image.Item2 : image.Item1;
                int height = orientation == 6 || orientation == 8 ? image.Item1 : image.Item2;
                double scale = Math.Max((double)screen.Item1 / width, (double)screen.Item2 / height);
                double projectedSide = crop.Side * scale;
                double aimingSide = Math.Min(screen.Item1, screen.Item2) *
                    (double)ScanCaptureGeometry.FrameCoverage;
                Check(projectedSide <= Math.Min(screen.Item1, screen.Item2) &&
                    projectedSide <= aimingSide + .001 && aimingSide - projectedSide < scale + .001,
                    "Crop does not project to the visible aiming square.");
            }
        });
        Run("invalid camera geometry is rejected", () =>
        {
            foreach (var dimensions in new[] { (0, 1440, 1170, 2532), (1920, -1, 1170, 2532),
                (1920, 1440, 0, 2532), (1920, 1440, 1170, -1) })
            {
                bool rejected = false;
                try { ScanCaptureGeometry.CenteredVisibleSquare(dimensions.Item1, dimensions.Item2,
                    dimensions.Item3, dimensions.Item4, 6); }
                catch (ArgumentException) { rejected = true; }
                Check(rejected, "Invalid dimensions accepted.");
            }
            bool rejectedOrientation = false;
            try { ScanCaptureGeometry.CenteredVisibleSquare(1920, 1440, 1170, 2532, 2); }
            catch (ArgumentException) { rejectedOrientation = true; }
            Check(rejectedOrientation, "Mirrored camera orientation accepted.");
        });
        Console.WriteLine("Passed " + _passed + " scan checks; failed " + _failed + ".");
        return _failed == 0 ? 0 : 1;
    }

    static ScanResponse ValidResponse() => new ScanResponse
    {
        success = true, requestId = "scan-1",
        labels = new[] { new ScanLabel { identifier = "bottle", confidence = .7f } }
    };

    static void CheckOrientation(int orientation, byte[] expected)
    {
        var rgba = new byte[24];
        for (int i = 0; i < 6; i++)
        {
            rgba[i * 4] = (byte)(i + 1);
            rgba[i * 4 + 1] = 42;
            rgba[i * 4 + 2] = 77;
            rgba[i * 4 + 3] = 255;
        }
        var result = ScanFrameOrientation.ToUnityThumbnail(rgba, 2, 3, orientation);
        Check(Enumerable.Range(0, 6).Select(i => result[i * 4]).SequenceEqual(expected),
            "Pixel orientation mismatch for EXIF " + orientation);
        Check(Enumerable.Range(0, 6).All(i => result[i * 4 + 1] == 42 &&
            result[i * 4 + 2] == 77 && result[i * 4 + 3] == 255), "RGBA channels corrupted.");
    }

    static void RejectPixels(byte[] pixels, int width, int height, int orientation)
    {
        try { ScanFrameOrientation.ToUnityThumbnail(pixels, width, height, orientation); }
        catch (ArgumentException) { return; }
        throw new Exception("Invalid frame was accepted.");
    }

    static void Run(string label, Action test)
    {
        try { test(); _passed++; Console.WriteLine("PASS " + label); }
        catch (Exception error) { _failed++; Console.WriteLine("FAIL " + label + ": " + error.Message); }
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
