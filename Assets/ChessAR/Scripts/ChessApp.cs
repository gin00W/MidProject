using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace ChessAR
{
    public sealed class ChessApp : MonoBehaviour
    {
        public bool editorPreview;
        public ChessLessonState Lesson { get; } = new ChessLessonState();
        public bool IsBoardViewVisible => !_help && isActiveAndEnabled;
        ChessUi _ui;
        ChessImageTracker _tracker;
        ChessPreviewRenderer _preview;
        Text _status;
        bool _help;
        bool _started;

        bool IsPreview => editorPreview || Application.isEditor ||
            Application.platform != RuntimePlatform.IPhonePlayer;

        void Awake()
        {
            Application.targetFrameRate = 60;
            _ui = new ChessUi(transform);
        }

        void Start()
        {
            var cameraManager = FindAnyObjectByType<ARCameraManager>();
            var camera = cameraManager != null ? cameraManager.GetComponent<Camera>() : Camera.main;
            if (IsPreview)
            {
                if (cameraManager != null)
                {
                    cameraManager.enabled = false;
                    var background = cameraManager.GetComponent<ARCameraBackground>();
                    if (background != null) background.enabled = false;
                    var pose = cameraManager.GetComponent<TrackedPoseDriver>();
                    if (pose != null) pose.enabled = false;
                }
                _preview = gameObject.AddComponent<ChessPreviewRenderer>();
                _preview.Configure(_ui.Bold, camera);
            }
            else if (cameraManager != null)
            {
                cameraManager.autoFocusRequested = true;
                var origin = cameraManager.GetComponentInParent<XROrigin>();
                if (origin != null)
                {
                    _tracker = origin.GetComponent<ChessImageTracker>();
                    if (_tracker == null) _tracker = origin.gameObject.AddComponent<ChessImageTracker>();
                    _tracker.Configure(this, _ui.Bold);
                }
            }
            _started = true;
            Render();
        }

        void Update()
        {
            _ui?.UpdateSafeArea();
            if (_status == null) return;
            string value = _help ? "사용 방법" : IsPreview ? "미리보기" :
                _tracker != null ? _tracker.Status : "AR 카메라를 준비하지 못했어요. 앱 설정을 확인해 주세요.";
            if (_status.text != value) _status.text = value;
        }

        void OnEnable()
        {
            _ui?.SetVisible(true);
            if (_started) Render();
        }

        void OnDisable() => _ui?.SetVisible(false);

        void Render()
        {
            var page = _ui.Begin(!IsPreview && !_help);
            var header = _ui.Card(page, 0, 3);
            var title = _ui.Row(header);
            var name = _ui.Text(title, "체스 연습", 18, true);
            ChessUi.Size(name.transform, flexibleWidth: 1);
            var help = _ui.TextButton(title, _help ? "돌아가기" : "도움말", () => { _help = !_help; Render(); }, 38, 13);
            ChessUi.Size(help.transform, flexibleWidth: 0);
            help.GetComponent<LayoutElement>().preferredWidth = 82;
            _status = _ui.Text(header, IsPreview ? "미리보기" : "체스판 전체를 비춰 주세요.", 12, false, ChessUi.Muted);
            _ui.Divider(header);
            if (_help) { RenderHelp(page); return; }

            var view = ChessUi.Rect("Board View", page);
            ChessUi.Size(view, flexibleHeight: 1);
            if (IsPreview && _preview != null) _ui.Preview(view, _preview.Texture, _preview.Rotate);
            var controls = _ui.Card(page, 0, 8);
            var tabs = _ui.Row(controls);
            _ui.Tab(tabs, "말 놓기", () => { Lesson.ShowSetup(); Render(); }, Lesson.Mode == LessonMode.Setup);
            _ui.Tab(tabs, "이동 연습", () => { Lesson.SelectKind(Lesson.SelectedKind); Render(); }, Lesson.Mode == LessonMode.Moves);
            var heading = _ui.Row(controls);
            var lessonTitle = _ui.Text(heading, Lesson.Title, 17, true);
            ChessUi.Size(lessonTitle.transform, flexibleWidth: 1);
            var progress = _ui.Text(heading, Lesson.Mode == LessonMode.Setup
                ? (Lesson.SetupStep + 1) + " / " + Lesson.SetupStepCount
                : Lesson.CurrentExample.SelectedPiece.Square.Name, 12, false, ChessUi.Muted, TextAnchor.MiddleRight);
            ChessUi.Size(progress.transform, flexibleWidth: 0);
            progress.GetComponent<LayoutElement>().preferredWidth = 40;
            _ui.Text(controls, Lesson.Instruction, 14, false, ChessUi.Muted);

            if (Lesson.Mode == LessonMode.Setup)
            {
                var row = _ui.Row(controls);
                var previous = _ui.TextButton(row, "이전", () => { Lesson.Previous(); Render(); });
                previous.interactable = Lesson.CanPrevious;
                var next = _ui.Button(row, Lesson.CanNext ? "다음" : "다시 놓기", () =>
                {
                    if (Lesson.CanNext) Lesson.Next(); else Lesson.Restart();
                    Render();
                }, true);
                ChessUi.Size(next.transform, flexibleWidth: 0);
                next.GetComponent<LayoutElement>().preferredWidth = 104;
            }
            else
            {
                PieceButtons(controls, new[] { PieceKind.Pawn, PieceKind.Rook, PieceKind.Knight,
                    PieceKind.Bishop, PieceKind.Queen, PieceKind.King });
                _ui.Text(controls, "점: 이동할 칸    붉은 테두리: 잡을 말", 12, false, ChessUi.Muted);
                _ui.Text(controls, ChessRules.LearningScopeNotice, 11, false, ChessUi.Muted);
            }
            _preview?.Show(Lesson);
        }

        void PieceButtons(Transform parent, PieceKind[] kinds)
        {
            var row = _ui.Row(parent, 4);
            foreach (var kind in kinds)
            {
                var selected = kind;
                _ui.Tab(row, PieceName(kind), () => { Lesson.SelectKind(selected); Render(); },
                    Lesson.SelectedKind == kind, 13, true);
            }
        }

        static string PieceName(PieceKind kind)
        {
            switch (kind)
            {
                case PieceKind.Pawn: return "폰";
                case PieceKind.Rook: return "룩";
                case PieceKind.Knight: return "나이트";
                case PieceKind.Bishop: return "비숍";
                case PieceKind.Queen: return "퀸";
                default: return "킹";
            }
        }

        void RenderHelp(Transform page)
        {
            var help = _ui.Card(page, 0, 16);
            ChessUi.Size(help, flexibleHeight: 1);
            _ui.Text(help, "사용 방법", 18, true);
            _ui.Text(help, "준비\n체스판과 종이 말을 A4, 실제 크기 100%로 출력합니다.", 15);
            _ui.Text(help, "판 인식\n평평하게 놓은 출력판을 테두리까지 비춥니다.", 15);
            _ui.Text(help, "말 놓기\n표시된 칸에 종이 말을 놓은 뒤 ‘다음’을 누릅니다.", 15);
            _ui.Text(help, "이동 연습\n종이 말을 치우고, 배우고 싶은 말을 선택합니다.", 15);
            _ui.Text(help, "직접 놓은 말은 화면의 예시와 비교해 확인하세요. 판을 다시 비추면 보던 안내가 이어집니다.", 13, false, ChessUi.Muted);
            if (IsPreview) _ui.Text(help, "미리보기에서는 3D 모형을 살펴볼 수 있습니다. 실제 판 인식은 아이폰·아이패드에서 확인합니다.", 13, false, ChessUi.Muted);
            _ui.TextButton(help, "돌아가기", () => { _help = false; Render(); });
        }
    }
}
