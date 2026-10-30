using System;
using SeongbukAR.Learning;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SeongbukAR
{
    // A registered picture supplies the model's position and rotation every
    // frame. Lost pictures hide the model; reacquisition preserves lesson state.
    [RequireComponent(typeof(ARTrackedImageManager))]
    public sealed class CardGuideTracker : MonoBehaviour
    {
        public GuideApp app;
        // Retained for old serialized scenes; lesson parts own their materials.
        public Material guideMaterial;
        public bool IsConfigured { get; private set; }
        public bool IsTracking { get; private set; }
        public string ActiveImageId { get; private set; }
        public string Status { get; private set; } = "등록된 학습 그림을 비춰 주세요.";

        ARTrackedImageManager _manager;
        WasteLessonVisual _visual;

        public void Configure(GuideApp owner, Camera camera)
        {
            app = owner;
            _manager = GetComponent<ARTrackedImageManager>();
            var library = Resources.Load<XRReferenceImageLibrary>("Tracking/WasteLessonImages");
            if (library == null || library.count == 0)
            {
                Status = "학습 그림을 불러오지 못했어요. 앱을 업데이트해 주세요.";
                IsConfigured = false;
                _manager.enabled = false;
                return;
            }
            try
            {
                _manager.enabled = false;
                _manager.referenceLibrary = library;
                _manager.requestedMaxNumberOfMovingImages = 1;
                _manager.enabled = true;
                var model = new GameObject("Image Anchored Recycling Lesson");
                model.transform.SetParent(transform, false);
                _visual = model.AddComponent<WasteLessonVisual>();
                _visual.Configure(camera);
                _visual.gameObject.SetActive(false);
                IsConfigured = true;
            }
            catch (Exception exception)
            {
                IsConfigured = false;
                Status = "이 기기에서 그림 인식을 준비하지 못했어요.";
                Debug.LogWarning("Image AR lesson setup failed: " + exception.Message);
            }
        }

        void Update()
        {
            if (!IsConfigured || app == null || !app.IsLessonViewActive ||
                !_manager.isActiveAndEnabled || ARSession.state != ARSessionState.SessionTracking)
            {
                Hide();
                if (IsConfigured)
                    Status = "카메라를 준비하고 있어요. 학습 그림 전체를 비춰 주세요.";
                return;
            }
            ARTrackedImage found = null;
            foreach (var image in _manager.trackables)
            {
                if (image.trackingState != TrackingState.Tracking ||
                    !WasteLessonState.Supports(image.referenceImage.name)) continue;
                // Keep the current visible image when two targets overlap.
                if (found == null) found = image;
                if (image.referenceImage.name == app.CurrentLessonItemId)
                {
                    found = image;
                    break;
                }
            }
            if (found == null)
            {
                Hide();
                Status = app.CurrentLessonItemId == null
                    ? "페트병 또는 배달 용기 학습 그림 전체를 비춰 주세요."
                    : "그림을 다시 비추면 보던 단계부터 이어집니다.";
                return;
            }
            string id = found.referenceImage.name;
            if (!app.ObserveLessonImage(id)) { Hide(); return; }
            ActiveImageId = id;
            IsTracking = true;
            Status = "그림 인식 중 · 그림을 움직여 모형을 살펴보세요.";
            _visual.transform.SetPositionAndRotation(found.transform.position, found.transform.rotation);
            _visual.gameObject.SetActive(true);
            _visual.Show(id, app.LessonStepIndex);
        }

        void Hide()
        {
            IsTracking = false;
            ActiveImageId = null;
            if (_visual != null) _visual.gameObject.SetActive(false);
        }

        void OnDisable() => Hide();

        void OnDestroy()
        {
            if (_visual != null) Destroy(_visual.gameObject);
        }
    }
}
