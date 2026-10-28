using System;

namespace SeongbukAR.Scanning
{
    [Serializable]
    public sealed class ScanLabel
    {
        public string identifier;
        public float confidence;
    }

    [Serializable]
    public sealed class ScanResponse
    {
        public bool success;
        public string requestId;
        public string engine;
        public string error;
        public string detail;
        public ScanLabel[] labels;
    }

    // Pure C# so malformed native payloads can be checked outside Unity too.
    public static class ScanPayloadRules
    {
        public static bool IsValid(ScanResponse response)
        {
            if (response == null || string.IsNullOrWhiteSpace(response.requestId)) return false;
            if (!response.success) return !string.IsNullOrWhiteSpace(response.error);
            if (response.labels == null || response.labels.Length == 0 || response.labels.Length > 32)
                return false;
            foreach (var label in response.labels)
            {
                if (label == null || string.IsNullOrWhiteSpace(label.identifier) ||
                    label.identifier.Length > 256 || float.IsNaN(label.confidence) ||
                    float.IsInfinity(label.confidence) || label.confidence < 0 || label.confidence > 1)
                    return false;
            }
            return true;
        }
    }
}
