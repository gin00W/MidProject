using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SeongbukAR.Scanning
{
    // Captures one rear-camera frame per button press. Apple Vision performs
    // real on-device classification; the caller must confirm a candidate and
    // ask about material/contamination rather than treating labels as truth.
    public sealed class WasteScanController : MonoBehaviour
    {
        public ARCameraManager cameraManager;
        [Range(256, 1024)] public int captureResolution = 512;
        public event Action<ScanResponse> Completed;

        public bool IsBusy => _requests.IsBusy;
        public bool IsSupported { get; private set; }
        public string Status { get; private set; } = "물건을 화면 중앙에 놓고 스캔해 주세요.";
        // Owned by this component: replaced by the next successful capture and
        // destroyed with the controller. Consumers must not Destroy it.
        public Texture2D LatestCapturedFrame { get; private set; }

        NativeScanReceiver _receiver;
        readonly ScanRequestState _requests = new ScanRequestState();
        string _activeRequestId => _requests.ActiveRequestId;
        Coroutine _timeout;

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern int SBW_IsVisionAvailable();
        [DllImport("__Internal")]
        static extern void SBW_ClassifyRgba([In] byte[] rgba, int byteCount, int width, int height,
            int exifOrientation, string callbackObject, string requestId);
        [DllImport("__Internal")]
        static extern void SBW_CancelScan(string requestId);
#endif

        void Awake()
        {
#if UNITY_IOS && !UNITY_EDITOR
            try { IsSupported = SBW_IsVisionAvailable() != 0; }
            catch (Exception) { IsSupported = false; }
            if (!IsSupported) Status = "이 기기에서는 사진 인식 기능을 사용할 수 없어요.";
#else
            IsSupported = false;
            Status = "실물 스캔은 아이폰에 설치한 앱에서 사용할 수 있어요.";
#endif
            var callback = new GameObject("SeongbukVisionCallback_" + Guid.NewGuid().ToString("N"));
            callback.transform.SetParent(transform, false);
            _receiver = callback.AddComponent<NativeScanReceiver>();
            _receiver.Handler = ReceiveNativeResult;
        }

        public bool RequestScan()
        {
            if (IsBusy) return false;
            if (!IsSupported)
            {
                Status = "실물 스캔은 아이폰에 설치한 앱에서 사용할 수 있어요.";
                return false;
            }
            if (!isActiveAndEnabled || cameraManager == null || !cameraManager.isActiveAndEnabled)
            {
                Status = "카메라를 준비하고 있어요. 잠시 후 다시 스캔해 주세요.";
                return false;
            }
            if (cameraManager.currentFacingDirection == CameraFacingDirection.User)
            {
                Status = "후면 카메라로 물건을 비춰 주세요.";
                return false;
            }
            if (!cameraManager.TryAcquireLatestCpuImage(out var image))
            {
                Status = "카메라 화면을 기다리고 있어요. 카메라 사용 허용도 확인해 주세요.";
                return false;
            }

            byte[] pixels;
            int size = Mathf.Clamp(captureResolution, 256, 1024);
            int orientation = ExifOrientationForScreen();
            IntPtr buffer = IntPtr.Zero;
            try
            {
                // Central crop: users should keep one item inside the scan frame.
                int side = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(image.width, image.height) * 0.85f));
                var conversion = new XRCpuImage.ConversionParams
                {
                    inputRect = new RectInt((image.width - side) / 2, (image.height - side) / 2, side, side),
                    outputDimensions = new Vector2Int(size, size),
                    outputFormat = TextureFormat.RGBA32,
                    transformation = XRCpuImage.Transformation.None
                };
                int byteCount = image.GetConvertedDataSize(conversion);
                buffer = Marshal.AllocHGlobal(byteCount);
                image.Convert(conversion, buffer, byteCount);
                pixels = new byte[byteCount];
                Marshal.Copy(buffer, pixels, 0, byteCount);
            }
            catch (Exception exception)
            {
                Status = "사진을 준비하지 못했어요. 다시 스캔해 주세요.";
                Debug.LogWarning("Camera scan conversion failed: " + exception.Message);
                return false;
            }
            finally
            {
                image.Dispose();
                if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer);
            }

            try
            {
                var thumbnail = ScanFrameOrientation.ToUnityThumbnail(pixels, size, size, orientation);
                if (LatestCapturedFrame != null) Destroy(LatestCapturedFrame);
                LatestCapturedFrame = new Texture2D(size, size, TextureFormat.RGBA32, false);
                LatestCapturedFrame.LoadRawTextureData(thumbnail);
                LatestCapturedFrame.Apply(false, false);
                if (!_requests.TryBegin(Guid.NewGuid().ToString("N"))) return false;
                Status = "사진에서 물건을 찾고 있어요…";
#if UNITY_IOS && !UNITY_EDITOR
                // The native plug-in copies these bytes before this call returns.
                SBW_ClassifyRgba(pixels, pixels.Length, size, size, orientation,
                    _receiver.gameObject.name, _activeRequestId);
#endif
                _timeout = StartCoroutine(TimeoutAfter(_activeRequestId));
                return true;
            }
            catch (Exception exception)
            {
                CancelScan();
                Status = "스캔을 시작하지 못했어요. 다시 시도해 주세요.";
                Debug.LogWarning("Native scan start failed: " + exception.Message);
                return false;
            }
        }

        public void CancelScan()
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (!string.IsNullOrEmpty(_activeRequestId))
            {
                try { SBW_CancelScan(_activeRequestId); }
                catch (Exception) { /* A missing native plug-in must not crash navigation. */ }
            }
#endif
            ClearPendingRequest();
        }

        void ClearPendingRequest()
        {
            _requests.Cancel();
            if (_timeout != null) StopCoroutine(_timeout);
            _timeout = null;
        }

        IEnumerator TimeoutAfter(string requestId)
        {
            yield return new WaitForSecondsRealtime(25);
            if (_activeRequestId != requestId) yield break;
            _timeout = null;
            CancelScan();
            Status = "인식 시간이 오래 걸리고 있어요. 물건을 가까이 두고 다시 스캔해 주세요.";
            Completed?.Invoke(new ScanResponse
            {
                success = false, requestId = requestId, engine = "Apple Vision",
                error = "timeout", labels = Array.Empty<ScanLabel>()
            });
        }

        void ReceiveNativeResult(string json)
        {
            if (!IsBusy || string.IsNullOrEmpty(_activeRequestId)) return;
            ScanResponse response;
            try { response = JsonUtility.FromJson<ScanResponse>(json); }
            catch (Exception) { response = null; }
            // A result from a cancelled/older request cannot replace the current one.
            if (response != null && !string.IsNullOrEmpty(response.requestId) &&
                response.requestId != _activeRequestId) return;
            if (!ScanPayloadRules.IsValid(response))
                response = new ScanResponse
                {
                    success = false, requestId = _activeRequestId, engine = "Apple Vision",
                    error = "invalid_response", labels = Array.Empty<ScanLabel>()
                };
            ClearPendingRequest();
            Status = response.success ? "찾은 물건이 맞는지 확인해 주세요." :
                "물건을 인식하지 못했어요. 다시 스캔하거나 이름으로 찾아 주세요.";
            Completed?.Invoke(response);
        }

        static int ExifOrientationForScreen()
        {
            switch (Screen.orientation)
            {
                case ScreenOrientation.PortraitUpsideDown: return 8;
                case ScreenOrientation.LandscapeLeft: return 1;
                case ScreenOrientation.LandscapeRight: return 3;
                default: return 6; // Rear iPhone sensor held in portrait.
            }
        }

        void OnDisable() => CancelScan();

        void OnDestroy()
        {
            CancelScan();
            if (_receiver != null) _receiver.Handler = null;
            if (LatestCapturedFrame != null) Destroy(LatestCapturedFrame);
        }
    }
}
