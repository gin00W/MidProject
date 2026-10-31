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
            string value = IsPreview ? "3D 미리보기 · 실제 판 인식은 아이폰·아이패드에서" :
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
            var header = _ui.Card(page, 10, 4);
            var title = _ui.Row(header);
            var name = _ui.Text(title, "체스, 첫 수", 22, true);
            ChessUi.Size(name.transform, flexibleWidth: 1);
            var help = _ui.Button(title, _help ? "돌아가기" : "사용법", () => { _help = !_help; Render(); }, false, 38, 13);
            ChessUi.Size(help.transform, flexibleWidth: 0);
            help.GetComponent<LayoutElement>().preferredWidth = 82;
            _status = _ui.Text(header, IsPreview ? "3D 미리보기" : "출력한 체스판 전체를 비춰 주세요.", 12, false, ChessUi.Muted);
            if (_help) { RenderHelp(page); return; }

            var view = ChessUi.Rect("Board View", page);
            ChessUi.Size(view, flexibleHeight: 1);
            if (IsPreview && _preview != null) _ui.Preview(view, _preview.Texture, _preview.Rotate);
            var controls = _ui.Card(page, 12, 6);
            var tabs = _ui.Row(controls);
            _ui.Button(tabs, "처음 배치", () => { Lesson.ShowSetup(); Render(); }, Lesson.Mode == LessonMode.Setup);
            _ui.Button(tabs, "말의 이동법", () => { Lesson.SelectKind(Lesson.SelectedKind); Render(); }, Lesson.Mode == LessonMode.Moves);
            _ui.Text(controls, Lesson.Title, 18, true);
            _ui.Text(controls, Lesson.Instruction, 14, false, ChessUi.Muted);

            if (Lesson.Mode == LessonMode.Setup)
            {
                _ui.Text(controls, (Lesson.SetupStep + 1) + " / " + Lesson.SetupStepCount + " 단계 · 직접 놓은 뒤 다음을 누르세요", 12, false, ChessUi.Accent);
                var row = _ui.Row(controls);
                var previous = _ui.Button(row, "이전 단계", () => { Lesson.Previous(); Render(); });
                previous.interactable = Lesson.CanPrevious;
                _ui.Button(row, Lesson.CanNext ? "다음 단계" : "처음부터", () =>
                {
                    if (Lesson.CanNext) Lesson.Next(); else Lesson.Restart();
                    Render();
                }, true);
            }
            else
            {
                PieceButtons(controls, new[] { PieceKind.Pawn, PieceKind.Rook, PieceKind.Knight });
                PieceButtons(controls, new[] { PieceKind.Bishop, PieceKind.Queen, PieceKind.King });
                _ui.Text(controls, "초록: 선택한 말  ·  파랑: 이동 칸  ·  주황: 잡을 말", 12, false, ChessUi.Muted);
                _ui.Text(controls, ChessRules.LearningScopeNotice, 11, false, ChessUi.Muted);
            }
            _preview?.Show(Lesson);
        }

        void PieceButtons(Transform parent, PieceKind[] kinds)
        {
            var row = _ui.Row(parent, 6);
            foreach (var kind in kinds)
            {
                var selected = kind;
                _ui.Button(row, PieceName(kind), () => { Lesson.SelectKind(selected); Render(); },
                    Lesson.SelectedKind == kind, 38, 13);
            }
        }

        static string PieceName(PieceKind kind)
        {
            switch (kind)
            {
                case PieceKind.Pawn: return "폰 P";
                case PieceKind.Rook: return "룩 R";
                case PieceKind.Knight: return "나이트 N";
                case PieceKind.Bishop: return "비숍 B";
                case PieceKind.Queen: return "퀸 Q";
                default: return "킹 K";
            }
        }

        void RenderHelp(Transform page)
        {
            var help = _ui.Card(page, 16, 12);
            ChessUi.Size(help, flexibleHeight: 1);
            _ui.Text(help, "종이 체스판으로 시작하세요", 22, true);
            _ui.Text(help, "1. 제공된 체스판을 A4, 실제 크기 100%로 출력하세요. 종이 말도 잘라서 준비할 수 있어요.", 16);
            _ui.Text(help, "2. 출력판을 평평하게 놓고 테두리까지 카메라에 비춰 주세요. 인식한 판 위에 가상 말이 나타납니다.", 16);
            _ui.Text(help, "3. 처음 배치에서는 가상 말의 칸을 보고 종이 말을 놓은 다음, 직접 다음 단계를 눌러 주세요.", 16);
            _ui.Text(help, "4. 이동법을 볼 때는 종이 말을 치우고 예시 위치에서 각 말의 방향과 잡을 수 있는 칸을 살펴보세요.", 16);
            _ui.Text(help, "실제 말의 위치는 직접 비교하며 확인합니다. 판을 잠시 놓쳐도 선택한 안내는 유지됩니다.", 14, false, ChessUi.Muted);
            _ui.Button(help, "판 위에서 시작하기", () => { _help = false; Render(); }, true);
        }
    }
}
