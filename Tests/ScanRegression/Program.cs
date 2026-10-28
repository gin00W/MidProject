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
