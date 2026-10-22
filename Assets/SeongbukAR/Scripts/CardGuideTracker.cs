using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SeongbukAR
{
    [RequireComponent(typeof(ARTrackedImageManager))]
    public sealed class CardGuideTracker : MonoBehaviour
    {
        public GuideApp app;
        public Material guideMaterial;
        ARTrackedImageManager _manager;
        Transform _visual;
        string _lastCardId;

        void Awake() => _manager = GetComponent<ARTrackedImageManager>();

        void Update()
        {
            if (app == null) return;
            ARTrackedImage found = null;
            foreach (var image in _manager.trackables)
            {
                if (image.trackingState != TrackingState.Tracking) continue;
                found = image;
                break;
            }
            if (found == null)
            {
                if (_visual != null) _visual.gameObject.SetActive(false);
                if (_lastCardId != null)
                    app.TrackingMessage = "카드를 다시 비춰 주세요. 선택한 안내는 유지됩니다.";
                _lastCardId = null;
                return;
            }
            string id = found.referenceImage.name;
            if (_lastCardId != id)
            {
                _lastCardId = id;
                if (!app.OpenItem(id))
                {
                    if (_visual != null) _visual.gameObject.SetActive(false);
                    return;
                }
            }
            if (app.CurrentItem == null || app.CurrentItem.id != id)
            {
                if (_visual != null) _visual.gameObject.SetActive(false);
                return;
            }
            app.TrackingMessage = "카드 인식: " + app.CurrentItem.name;
            if (_visual == null) CreateVisual();
            _visual.gameObject.SetActive(true);
            _visual.SetPositionAndRotation(found.transform.position, found.transform.rotation);
            _visual.position += found.transform.up * (0.04f + Mathf.Sin(Time.time * 2f) * 0.004f);
        }

        void CreateVisual()
        {
            _visual = new GameObject("AR Guide Mascot").transform;
            AddPart(PrimitiveType.Sphere, Vector3.zero, new Vector3(0.045f, 0.045f, 0.045f));
            AddPart(PrimitiveType.Cube, new Vector3(0, -0.027f, 0), new Vector3(0.038f, 0.025f, 0.032f));
            AddPart(PrimitiveType.Cube, new Vector3(-0.01f, 0.004f, -0.022f), Vector3.one * 0.006f);
            AddPart(PrimitiveType.Cube, new Vector3(0.01f, 0.004f, -0.022f), Vector3.one * 0.006f);
        }

        void AddPart(PrimitiveType type, Vector3 position, Vector3 scale)
        {
            var part = GameObject.CreatePrimitive(type);
            part.transform.SetParent(_visual, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = guideMaterial;
            Destroy(part.GetComponent<Collider>());
        }

        void OnDisable()
        {
            if (_visual != null) Destroy(_visual.gameObject);
        }
    }
}
