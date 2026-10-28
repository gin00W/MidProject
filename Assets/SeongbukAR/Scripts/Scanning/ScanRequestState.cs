using System;

namespace SeongbukAR.Scanning
{
    // UI/main-thread request state, separated from the camera/native boundary.
    public sealed class ScanRequestState
    {
        public string ActiveRequestId { get; private set; }
        public bool IsBusy => ActiveRequestId != null;

        public bool TryBegin(string requestId)
        {
            if (IsBusy || string.IsNullOrWhiteSpace(requestId)) return false;
            ActiveRequestId = requestId;
            return true;
        }

        public bool IsCurrent(string requestId) => IsBusy &&
            string.Equals(ActiveRequestId, requestId, StringComparison.Ordinal);

        public bool TryComplete(string requestId)
        {
            if (!IsCurrent(requestId)) return false;
            ActiveRequestId = null;
            return true;
        }

        public string Cancel()
        {
            string cancelled = ActiveRequestId;
            ActiveRequestId = null;
            return cancelled;
        }
    }
}
