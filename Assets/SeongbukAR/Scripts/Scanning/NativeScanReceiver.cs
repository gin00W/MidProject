using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace SeongbukAR.Scanning
{
    // UnitySendMessage finds this method by name in an IL2CPP build.
    [Preserve]
    public sealed class NativeScanReceiver : MonoBehaviour
    {
        public Action<string> Handler;

        [Preserve]
        public void OnNativeScanResult(string json) => Handler?.Invoke(json);
    }
}
