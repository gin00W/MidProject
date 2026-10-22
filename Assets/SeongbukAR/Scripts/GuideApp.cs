using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeongbukAR
{
    // Immediate-mode UI keeps the starter independent of scene UI wiring.
    // Replace this view with Canvas/TMP later without changing WasteCatalog.
    public sealed class GuideApp : MonoBehaviour
    {
        public bool editorPreview;
        public WasteItem CurrentItem { get; private set; }
        public GuideNode CurrentNode { get; private set; }
        public string TrackingMessage { get; set; } = "그림카드를 비추거나 품목을 검색하세요.";

        WasteCatalog _catalog;
        readonly Stack<GuideNode> _history = new Stack<GuideNode>();
        string _query = "";
        string _error;
        Vector2 _scroll;
        Font _font;
        GUIStyle _title, _label, _small, _button, _field;

        void Awake()
        {
            Application.targetFrameRate = 60;
            _font = Resources.Load<Font>("Fonts/NanumGothic-Regular");
            try
            {
                var json = Resources.Load<TextAsset>("Data/seongbuk-demo");
                if (json == null) throw new InvalidOperationException("품목 JSON 파일을 찾을 수 없습니다.");
                _catalog = new WasteCatalog(JsonUtility.FromJson<WasteCatalogData>(json.text));
            }
            catch (Exception exception)
            {
                _error = exception.Message;
                Debug.LogException(exception);
            }
        }

        public bool OpenItem(string id)
        {
            var item = _catalog?.Find(id);
            if (item == null)
            {
                TrackingMessage = "연결된 품목을 찾을 수 없습니다: " + id;
                return false;
            }
            if (CurrentItem == item) return true;
            CurrentItem = item;
            CurrentNode = item.FindNode(item.startNodeId);
            _history.Clear();
            _scroll = Vector2.zero;
            return true;
        }

        void Choose(GuideChoice choice)
        {
            var next = CurrentItem.FindNode(choice.nextNodeId);
            if (next == null) return;
            _history.Push(CurrentNode);
            CurrentNode = next;
            _scroll = Vector2.zero;
        }

        void OnGUI()
        {
            InitStyles();
            float scale = Screen.width / 720f;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            var safe = Screen.safeArea;
            var area = new Rect(20, (Screen.height - safe.yMax) / scale + 16,
                680, safe.height / scale - 32);
            float headerHeight = Mathf.Min(220, area.height * 0.3f);
            var headerArea = new Rect(area.x, area.y, area.width, headerHeight);
            DrawBackground(headerArea);
            GUILayout.BeginArea(headerArea);
            GUILayout.Label("어디다 버려?", _title);
            GUILayout.Label("성북구 AR 분리배출 도우미 · 시작 프로젝트", _small);
            GUILayout.Space(12);
            GUILayout.Label(editorPreview ? "맥북 미리보기 · 질문 흐름 확인" : TrackingMessage, _label);
            GUILayout.Label("기능 확인용 예시 데이터입니다. 성북구 전체 품목은 아직 미수록입니다.", _small);
            GUILayout.EndArea();
            // Leave the middle of the camera feed visible so the AR character
            // is not hidden underneath a full-screen interface.
            float bodyTop = editorPreview ? area.y + headerHeight + 12 :
                area.y + Mathf.Max(headerHeight + 12, area.height * 0.48f);
            var bodyArea = new Rect(area.x, bodyTop, area.width, area.yMax - bodyTop);
            DrawBackground(bodyArea);
            GUILayout.BeginArea(bodyArea);
            if (_error != null) GUILayout.Label(_error, _label);
            else if (_catalog != null)
            {
                if (CurrentItem == null) DrawSearch();
                else DrawItem();
            }
            GUILayout.EndArea();
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        static void DrawBackground(Rect area)
        {
            GUI.color = new Color(0.08f, 0.16f, 0.14f, 0.94f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        void DrawSearch()
        {
            GUILayout.Label("버릴 물건을 검색해 보세요", _label);
            _query = GUILayout.TextField(_query, _field, GUILayout.Height(58));
            GUILayout.Space(10);
            _scroll = GUILayout.BeginScrollView(_scroll);
            int count = 0;
            foreach (var item in _catalog.Search(_query))
            {
                if (GUILayout.Button(item.name, _button, GUILayout.MinHeight(66)))
                    OpenItem(item.id);
                GUILayout.Space(8);
                count++;
            }
            if (count == 0)
            {
                GUILayout.Label("아직 등록되지 않은 품목이에요. 공식 안내에서 확인해 주세요.", _label);
                if (GUILayout.Button("공식 품목사전 열기", _button))
                    Application.OpenURL("https://www.wasteguide.or.kr/front/dischargeMethod/dictionary.do");
            }
            GUILayout.EndScrollView();
        }

        void DrawItem()
        {
            if (GUILayout.Button("품목 목록으로", _button, GUILayout.Height(54)))
            {
                CurrentItem = null;
                CurrentNode = null;
                _history.Clear();
                _scroll = Vector2.zero;
                return;
            }
            GUILayout.Space(12);
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Label(CurrentItem.name, _title);
            if (CurrentItem.reviewStatus != "verified_seongbuk")
                GUILayout.Label("성북구 기준 검토 전 · 아래 내용은 개발용 예시입니다.", _small);
            GUILayout.Space(16);
            if (CurrentNode.result != null)
            {
                var result = CurrentNode.result;
                GUILayout.Label(result.destination, _title);
                for (int i = 0; i < result.steps.Length; i++)
                {
                    GUILayout.Label((i + 1) + ". " + result.steps[i], _label);
                    GUILayout.Space(8);
                }
                GUILayout.Space(10);
                GUILayout.Label(result.explanation, _label);
                foreach (string url in result.sourceUrls)
                    if (GUILayout.Button("안내 출처 확인", _button, GUILayout.MinHeight(56)))
                        Application.OpenURL(url);
            }
            else
            {
                GUILayout.Label(CurrentNode.prompt, _label);
                GUILayout.Space(12);
                foreach (var choice in CurrentNode.choices)
                {
                    if (GUILayout.Button(choice.label, _button, GUILayout.MinHeight(70))) Choose(choice);
                    GUILayout.Space(10);
                }
            }
            GUILayout.Space(16);
            if (_history.Count > 0 && GUILayout.Button("이전 질문으로", _button, GUILayout.Height(54)))
                CurrentNode = _history.Pop();
            GUILayout.EndScrollView();
        }

        void InitStyles()
        {
            if (_title != null) return;
            _label = new GUIStyle(GUI.skin.label) { font = _font, fontSize = 27, wordWrap = true };
            _label.normal.textColor = Color.white;
            _title = new GUIStyle(_label) { fontSize = 37, fontStyle = FontStyle.Bold };
            _small = new GUIStyle(_label) { fontSize = 21 };
            _small.normal.textColor = new Color(0.78f, 0.9f, 0.84f);
            _button = new GUIStyle(GUI.skin.button)
            {
                font = _font, fontSize = 26, wordWrap = true,
                padding = new RectOffset(18, 18, 12, 12)
            };
            _field = new GUIStyle(GUI.skin.textField)
            {
                font = _font, fontSize = 27, padding = new RectOffset(12, 12, 10, 10)
            };
        }
    }
}
