using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SeongbukAR
{
    /// <summary>
    /// Places a guide at a detected surface point in the current AR session.
    /// This is a surface-placed world-space panel, not tracking of the waste
    /// object and not a persistent/relocalizable ARAnchor.
    /// </summary>
    public sealed class SpatialGuide : MonoBehaviour
    {
        const float PanelWidth = 420f;
        const float MetresPerCanvasUnit = 0.00065f;
        const float Padding = 24f;

        static readonly Color Teal = new Color32(25, 70, 60, 255);
        static readonly Color Ink = new Color32(32, 49, 44, 255);
        static readonly Color Muted = new Color32(99, 117, 109, 255);
        static readonly Color Rule = new Color32(222, 233, 225, 255);

        readonly List<ARRaycastHit> _hits = new List<ARRaycastHit>();
        Camera _camera;
        Font _regular;
        Font _semibold;
        XROrigin _origin;
        ARPlaneManager _planes;
        ARRaycastManager _raycasts;
        GameObject _placedRoot;
        RectTransform _panel;

        public bool IsAvailable
        {
            get
            {
                return IsDeviceRuntime && _camera != null && _camera.isActiveAndEnabled && _regular != null &&
                    _planes != null && _planes.isActiveAndEnabled &&
                    _raycasts != null && _raycasts.isActiveAndEnabled &&
                    ARSession.state == ARSessionState.SessionTracking;
            }
        }

        public void Configure(Camera camera, Font regular, Font semibold)
        {
            if (_camera != camera) Clear();
            _camera = camera;
            _regular = regular;
            _semibold = semibold != null ? semibold : regular;

            if (!IsDeviceRuntime) return;
            _origin = _camera != null ? _camera.GetComponentInParent<XROrigin>() : null;
            if (_origin == null) _origin = FindFirstObjectByType<XROrigin>();
            if (_origin == null) return;
            if (_camera == null) _camera = _origin.Camera;

            _planes = _origin.GetComponent<ARPlaneManager>();
            if (_planes == null)
            {
                _planes = _origin.gameObject.AddComponent<ARPlaneManager>();
                _planes.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            }
            _raycasts = _origin.GetComponent<ARRaycastManager>();
            if (_raycasts == null)
                _raycasts = _origin.gameObject.AddComponent<ARRaycastManager>();
        }

        public bool TryPlace(GuideResult result, string itemName, out string message)
        {
            Clear();
            if (result == null || string.IsNullOrWhiteSpace(result.destination))
            {
                message = "먼저 물건의 배출 방법을 확인해 주세요.";
                return false;
            }
            if (!IsDeviceRuntime)
            {
                message = "공간 안내는 AR을 지원하는 아이폰에서 사용할 수 있어요.";
                return false;
            }
            if (!IsAvailable)
            {
                message = "카메라로 주변을 천천히 비춰 AR 공간 인식을 준비해 주세요.";
                return false;
            }

            _hits.Clear();
            Vector2 screenCentre = _camera.pixelRect.center;
            if (!_raycasts.Raycast(screenCentre, _hits, TrackableType.PlaneWithinPolygon) || _hits.Count == 0)
            {
                message = "카메라를 책상이나 바닥으로 천천히 움직인 뒤 다시 눌러 주세요.";
                return false;
            }

            CreatePanel(result, itemName, _hits[0].pose.position);
            FaceCamera();
            message = "선택한 표면 위에 배출 안내를 놓았어요. 주변으로 움직이며 확인해 보세요.";
            return true;
        }

        static bool IsDeviceRuntime =>
            !Application.isEditor && Application.platform == RuntimePlatform.IPhonePlayer;

        public void Clear()
        {
            if (_placedRoot != null) Destroy(_placedRoot);
            _placedRoot = null;
            _panel = null;
            _hits.Clear();
        }

        void LateUpdate()
        {
            if (_panel == null) return;
            bool tracked = _camera != null && _camera.isActiveAndEnabled &&
                ARSession.state == ARSessionState.SessionTracking;
            if (_panel.gameObject.activeSelf != tracked) _panel.gameObject.SetActive(tracked);
            if (tracked) FaceCamera();
        }

        void FaceCamera()
        {
            if (_panel == null || _camera == null) return;
            // A Canvas is read from its local -Z side. Pointing +Z away from
            // the eye keeps the lettering facing the camera without mirroring.
            Vector3 awayFromEye = _panel.position - _camera.transform.position;
            if (awayFromEye.sqrMagnitude < 0.0001f) return;
            Vector3 up = Mathf.Abs(Vector3.Dot(awayFromEye.normalized, Vector3.up)) > 0.98f
                ? _camera.transform.up : Vector3.up;
            _panel.rotation = Quaternion.LookRotation(awayFromEye, up);
        }

        void CreatePanel(GuideResult result, string itemName, Vector3 surfacePosition)
        {
            _placedRoot = new GameObject("Surface Placed Waste Guide");
            if (_origin != null) _placedRoot.transform.SetParent(_origin.transform, false);
            _placedRoot.transform.SetPositionAndRotation(surfacePosition, Quaternion.identity);

            var panelObject = new GameObject("Guide Panel", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(Image));
            panelObject.transform.SetParent(_placedRoot.transform, false);
            _panel = panelObject.GetComponent<RectTransform>();
            _panel.pivot = new Vector2(0.5f, 0.5f);
            var canvas = panelObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = _camera;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 20;
            panelObject.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 3;
            _panel.sizeDelta = new Vector2(PanelWidth, 1);
            _panel.localScale = Vector3.one * MetresPerCanvasUnit;
            var background = panelObject.GetComponent<Image>();
            background.color = Color.white;
            background.raycastTarget = false;
            var shadow = panelObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.04f, 0.13f, 0.10f, 0.16f);
            shadow.effectDistance = new Vector2(0, -6);

            AddRule("Accent", 0, 5, Teal, 0);
            float top = Padding;
            top += AddText("Item", string.IsNullOrWhiteSpace(itemName) ? "배출 방법" : itemName,
                top, 17, _regular, Muted, 22);
            top += 8;
            top += AddText("Destination", result.destination, top, 25, _semibold, Teal, 34);
            top += 16;
            AddRule("Divider", top, 1, Rule, Padding);
            top += 17;

            int shown = 0;
            foreach (string step in result.steps ?? System.Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(step)) continue;
                shown++;
                top += AddText("Step " + shown, shown + ".  " + step, top, 19, _regular, Ink, 25);
                top += 12;
                if (shown == 3) break;
            }
            if (shown == 0)
                top += AddText("Full Guide", "자세한 준비 순서는 화면의 안내를 확인해 주세요.",
                    top, 19, _regular, Ink, 25) + 12;

            string footer = result.steps != null && result.steps.Length > 3
                ? "전체 단계는 화면의 안내에서 확인해 주세요."
                : "선택한 책상·바닥 위에 놓은 안내";
            top += AddText("Context", footer, top + 3, 13, _regular, Muted, 18) + 3;
            float panelHeight = Mathf.Max(190, top + Padding);
            _panel.sizeDelta = new Vector2(PanelWidth, panelHeight);
            _panel.localPosition = Vector3.up * (panelHeight * MetresPerCanvasUnit * 0.5f + 0.025f);
        }

        float AddText(string name, string value, float top, int size, Font font, Color color, float minimumHeight)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Text));
            child.transform.SetParent(_panel, false);
            var rect = child.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(-Padding * 2, minimumHeight);
            rect.anchoredPosition = new Vector2(0, -top);
            var text = child.GetComponent<Text>();
            text.font = font;
            text.fontStyle = FontStyle.Normal;
            text.fontSize = size;
            text.color = color;
            text.text = value;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = false;
            text.supportRichText = false;
            text.lineSpacing = 1.12f;
            text.raycastTarget = false;
            float height = Mathf.Max(minimumHeight, Mathf.Ceil(text.preferredHeight));
            rect.sizeDelta = new Vector2(-Padding * 2, height);
            return height;
        }

        void AddRule(string name, float top, float height, Color color, float inset)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(Image));
            child.transform.SetParent(_panel, false);
            var rect = child.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(-inset * 2, height);
            rect.anchoredPosition = new Vector2(0, -top);
            var image = child.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        void OnDisable() => Clear();
        void OnDestroy() => Clear();
    }
}
