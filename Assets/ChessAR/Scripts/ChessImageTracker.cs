using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ChessAR
{
    [RequireComponent(typeof(ARTrackedImageManager))]
    public sealed class ChessImageTracker : MonoBehaviour
    {
        public bool IsTracking { get; private set; }
        public string Status { get; private set; } = "출력한 체스판 전체를 비춰 주세요.";
        ARTrackedImageManager _manager;
        ChessApp _app;
        ChessBoardVisual _visual;
        bool _configured;

        public void Configure(ChessApp app, Font font)
        {
            _app = app;
            _manager = GetComponent<ARTrackedImageManager>();
            var library = Resources.Load<XRReferenceImageLibrary>("Chess/BoardImages");
            if (library == null || library.count == 0)
            {
                _manager.enabled = false;
                Status = "체스판 자료를 불러오지 못했어요. 앱을 업데이트해 주세요.";
                return;
            }
            try
            {
                _manager.enabled = false;
                _manager.referenceLibrary = library;
                _manager.requestedMaxNumberOfMovingImages = 1;
                _manager.enabled = true;
                var root = new GameObject("Board Anchored Chess Guide");
                root.transform.SetParent(transform, false);
                _visual = root.AddComponent<ChessBoardVisual>();
                _visual.Configure(font);
                root.SetActive(false);
                _configured = true;
            }
            catch (Exception exception)
            {
                Status = "그림 인식을 준비하지 못했어요. 기기 설정을 확인해 주세요.";
                Debug.LogWarning("Chess image tracking setup: " + exception.Message);
            }
        }

        void Update()
        {
            if (!_configured || _app == null || !_app.IsBoardViewVisible)
            {
                Hide();
                return;
            }
            if (!_manager.isActiveAndEnabled || ARSession.state != ARSessionState.SessionTracking)
            {
                Hide();
                Status = "카메라를 준비하고 있어요. 출력판 전체를 천천히 비춰 주세요.";
                return;
            }
            ARTrackedImage found = null;
            foreach (var image in _manager.trackables)
            {
                if (image.trackingState == TrackingState.Tracking && image.referenceImage.name == "chess_board")
                {
                    found = image;
                    break;
                }
            }
            if (found == null)
            {
                Hide();
                Status = "출력판을 다시 비추면 보던 안내가 이어집니다.";
                return;
            }
            IsTracking = true;
            Status = "출력판 인식 중 · 판 위의 위치를 확인하세요.";
            _visual.transform.SetPositionAndRotation(found.transform.position, found.transform.rotation);
            _visual.gameObject.SetActive(true);
            if (_app.Lesson.Mode == LessonMode.Setup) _visual.ShowSetup(_app.Lesson.SetupStep);
            else _visual.ShowMoves(_app.Lesson.SelectedKind);
        }

        void Hide()
        {
            IsTracking = false;
            if (_visual != null) _visual.gameObject.SetActive(false);
        }

        void OnDisable() => Hide();
        void OnDestroy()
        {
            if (_visual != null) Destroy(_visual.gameObject);
        }
    }
}
