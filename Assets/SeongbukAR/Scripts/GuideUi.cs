using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace SeongbukAR
{
    // Runtime uGUI keeps existing GuidePreview/GuideAR scenes usable after Pull.
    // Layout sizes are logical pixels; the CanvasScaler handles device density.
    internal sealed class GuideUi
    {
        internal static readonly Color Ink = Hex("252A30");
        internal static readonly Color Muted = Hex("626A73");
        internal static readonly Color Paper = Hex("F8F9FA");
        internal static readonly Color Soft = Hex("EDF1F5");
        internal static readonly Color Line = Hex("DEE3E9");
        internal static readonly Color Accent = Hex("3E58B5");
        internal static readonly Color Orange = Hex("925629");

        internal readonly Font Regular;
        internal readonly Font Semibold;
        readonly RectTransform _safe;
        readonly Image _background;
        GameObject _page;
        Rect _lastSafe;
        static Sprite _rounded;

        internal GuideUi(Transform parent)
        {
            Regular = Resources.Load<Font>("Fonts/SUIT-Regular");
            Semibold = Resources.Load<Font>("Fonts/SUIT-SemiBold");
            if (Regular == null) Regular = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (Semibold == null) Semibold = Regular;
            var root = new GameObject("Guide Interface", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            canvas.pixelPerfect = true;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 844);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var background = NewRect("Background", root.transform);
            Stretch(background);
            _background = background.gameObject.AddComponent<Image>();
            _background.raycastTarget = false;
            _safe = NewRect("Safe Area", root.transform);
            Stretch(_safe);
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Guide Event System", typeof(EventSystem));
                events.transform.SetParent(parent, false);
#if ENABLE_INPUT_SYSTEM
                events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
                events.AddComponent<StandaloneInputModule>();
#endif
            }
            UpdateSafeArea();
        }

        internal void UpdateSafeArea()
        {
            Rect safe = Screen.safeArea;
            if (TouchScreenKeyboard.visible && TouchScreenKeyboard.area.height > 0)
            {
                float top = safe.yMax;
                safe.yMin = Mathf.Min(top - 180, Mathf.Max(safe.yMin, TouchScreenKeyboard.area.yMax));
            }
            if (safe == _lastSafe || Screen.width <= 0 || Screen.height <= 0) return;
            _lastSafe = safe;
            _safe.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            _safe.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            _safe.offsetMin = _safe.offsetMax = Vector2.zero;
        }

        internal void SetVisible(bool visible) => _safe.parent.gameObject.SetActive(visible);

        internal RectTransform Begin(bool cameraVisible = false)
        {
            if (_page != null)
            {
                _page.SetActive(false);
                UnityEngine.Object.Destroy(_page);
            }
            _background.color = cameraVisible ? Color.clear : Paper;
            var page = NewRect("Page", _safe);
            Stretch(page);
            _page = page.gameObject;
            var layout = Column(page, 14, 24);
            layout.padding.top = 12;
            layout.padding.bottom = 18;
            return page;
        }

        internal Text Label(Transform parent, string value, int size = 16, bool bold = false,
            Color? color = null, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var rect = NewRect("Text", parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = bold ? Semibold : Regular;
            text.text = value ?? "";
            text.fontSize = size;
            text.color = color ?? Ink;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.lineSpacing = 1.18f;
            text.fontStyle = FontStyle.Normal;
            text.supportRichText = false;
            text.raycastTarget = false;
            return text;
        }

        internal RectTransform Card(Transform parent, Color? color = null, int padding = 18, float gap = 10)
        {
            var card = NewRect("Card", parent);
            var image = card.gameObject.AddComponent<Image>();
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
            image.color = color ?? Color.white;
            Column(card, gap, padding);
            return card;
        }

        internal RectTransform Row(Transform parent, float gap = 10)
        {
            var rect = NewRect("Row", parent);
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = gap;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = layout.childForceExpandWidth = false;
            return rect;
        }

        internal Button Button(Transform parent, string label, Action action, bool primary = false,
            float height = 52, int textSize = 16, Color? fill = null)
        {
            var rect = NewRect("Button " + label, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
            image.color = fill ?? (primary ? Accent : Soft);
            var button = MakeButton(rect, image, action);
            Size(rect, height: height, flexibleWidth: 1);
            var text = Label(rect, label, textSize, true,
                primary ? Color.white : Ink, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 14, 8);
            return button;
        }

        internal Button Choice(Transform parent, string label, Action action)
        {
            return ListButton(parent, label, null, action);
        }

        // A row grows with its actual wrapped text. This keeps long material
        // choices readable in narrow windows without guessing character counts.
        internal Button ListButton(Transform parent, string label, string detail, Action action)
        {
            var rect = NewRect("List Row " + label, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;
            var button = MakeButton(rect, image, action);
            var layout = Column(rect, 5, 0);
            layout.padding = new RectOffset(16, 38, 16, 16);
            Size(rect, flexibleWidth: 1, minHeight: 56);
            Label(rect, label, 16, false, Ink);
            if (!string.IsNullOrWhiteSpace(detail))
                Label(rect, detail, 13, false, Muted);

            var arrow = NewRect("Chevron", rect);
            arrow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            arrow.anchorMin = new Vector2(1, 0);
            arrow.anchorMax = Vector2.one;
            arrow.pivot = new Vector2(1, 0.5f);
            arrow.offsetMin = new Vector2(-32, 0);
            arrow.offsetMax = new Vector2(-12, 0);
            Chevron(arrow);
            var rule = NewRect("Row Divider", rect);
            rule.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            rule.anchorMin = rule.anchorMax = Vector2.zero;
            rule.anchorMax = Vector2.right;
            rule.pivot = new Vector2(0.5f, 0);
            rule.sizeDelta = new Vector2(0, 1);
            rule.anchoredPosition = Vector2.zero;
            var line = rule.gameObject.AddComponent<Image>();
            line.color = Line;
            line.raycastTarget = false;
            return button;
        }

        internal Button TextButton(Transform parent, string label, Action action,
            float height = 44, int textSize = 14)
        {
            var button = Button(parent, label, action, false, height, textSize, Color.clear);
            var text = button.GetComponentInChildren<Text>();
            text.font = Regular;
            text.color = Accent;
            // Tint the lettering so a transparent text control still responds
            // visibly to touch, keyboard focus, and disabled state.
            button.targetGraphic = text;
            var colors = button.colors;
            colors.highlightedColor = new Color(0.86f, 0.86f, 0.86f);
            colors.pressedColor = new Color(0.65f, 0.65f, 0.65f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            button.colors = colors;
            return button;
        }

        internal void Divider(Transform parent)
        {
            var rect = NewRect("Divider", parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Line;
            image.raycastTarget = false;
            Size(rect, height: 1);
        }

        // Simple line geometry serves the same role as an icon in a class
        // project. There is no generated image or decorative character here.
        internal RectTransform ScanMark(Transform parent, float size = 116)
        {
            var holder = NewRect("Scan Mark", parent);
            Size(holder, height: size);
            var mark = NewRect("Outline", holder);
            mark.anchorMin = mark.anchorMax = new Vector2(0.5f, 0.5f);
            mark.sizeDelta = new Vector2(116, 116);
            mark.localScale = Vector3.one * (size / 116);
            Stroke(mark, 10, 10, 18, 2, Accent);
            Stroke(mark, 10, 10, 2, 18, Accent);
            Stroke(mark, 88, 10, 18, 2, Accent);
            Stroke(mark, 104, 10, 2, 18, Accent);
            Stroke(mark, 10, 104, 18, 2, Accent);
            Stroke(mark, 10, 88, 2, 18, Accent);
            Stroke(mark, 88, 104, 18, 2, Accent);
            Stroke(mark, 104, 88, 2, 18, Accent);
            Stroke(mark, 39, 36, 38, 2, Ink);
            Stroke(mark, 39, 40, 2, 39, Ink);
            Stroke(mark, 75, 40, 2, 39, Ink);
            Stroke(mark, 39, 79, 38, 2, Ink);
            Stroke(mark, 51, 32, 14, 2, Muted);
            Stroke(mark, 45, 47, 2, 25, Line);
            return holder;
        }

        static void Stroke(RectTransform parent, float left, float top, float width, float height, Color color)
        {
            var rect = NewRect("Line", parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(left, -top);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        // Navigation uses geometry because SUIT deliberately has a smaller
        // symbol repertoire than its Korean/Latin text coverage.
        internal void Chevron(Transform parent, bool back = false)
        {
            for (int i = 0; i < 2; i++)
            {
                var rect = NewRect("Chevron Line", parent);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(8, 1.6f);
                rect.anchoredPosition = new Vector2(back ? 0.7f : -0.7f, i == 0 ? 2.5f : -2.5f);
                rect.localRotation = Quaternion.Euler(0, 0, (i == 0 ? -45 : 45) * (back ? -1 : 1));
                var image = rect.gameObject.AddComponent<Image>();
                image.color = Muted;
                image.raycastTarget = false;
            }
        }

        static Button MakeButton(RectTransform rect, Image image, Action action)
        {
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Hex("F0F2F8");
            colors.pressedColor = Hex("DCE1EF");
            colors.disabledColor = Hex("BDC3CD");
            button.colors = colors;
            button.onClick.AddListener(() => action?.Invoke());
            return button;
        }

        internal InputField SearchField(Transform parent, string value, Action<string> onChange)
        {
            var rect = NewRect("Search Field", parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
            image.color = Soft;
            Size(rect, height: 52);
            var input = rect.gameObject.AddComponent<InputField>();
            input.targetGraphic = image;
            input.characterLimit = 60;
            input.lineType = InputField.LineType.SingleLine;
            var placeholder = Label(rect, "물건 이름 검색", 16, false, Muted, TextAnchor.MiddleLeft);
            Stretch(placeholder.rectTransform, 16, 8);
            var text = Label(rect, "", 16, false, Ink, TextAnchor.MiddleLeft);
            Stretch(text.rectTransform, 16, 8);
            text.supportRichText = false;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.SetTextWithoutNotify(value ?? "");
            input.onValueChanged.AddListener(newValue => onChange?.Invoke(newValue));
            return input;
        }

        internal RectTransform Scroll(Transform parent)
        {
            var rect = NewRect("Scroll", parent);
            Size(rect, flexibleHeight: 1, minHeight: 60);
            var viewport = NewRect("Viewport", rect);
            Stretch(viewport);
            var image = viewport.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            Column(content, 10, 0).padding.bottom = 12;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = rect.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32;
            return content;
        }

        internal void Photo(Transform parent, Texture texture, float height = 170)
        {
            if (texture == null) return;
            var holder = NewRect("Captured Photo", parent);
            Size(holder, height: height);
            var rect = NewRect("Photo", holder);
            Stretch(rect);
            var raw = rect.gameObject.AddComponent<RawImage>();
            raw.texture = texture;
            raw.raycastTarget = false;
            var aspect = rect.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = (float)texture.width / texture.height;
        }

        internal void Header(Transform parent, string title, Action back = null)
        {
            var row = Row(parent);
            Size(row, height: 44);
            if (back != null)
            {
                var button = Button(row, "", back, false, 44, 18, Color.clear);
                Size(button.transform, width: 44, height: 44, flexibleWidth: 0);
                Chevron(button.transform, true);
            }
            var label = Label(row, title, back == null ? 22 : 18, true, Ink, TextAnchor.MiddleLeft);
            Size(label.transform, flexibleWidth: 1);
            var region = Label(row, "성북구", 12, false, Muted, TextAnchor.MiddleRight);
            Size(region.transform, width: 48);
        }

        internal static void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                parent.GetChild(i).gameObject.SetActive(false);
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        internal static RectTransform NewRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        internal static VerticalLayoutGroup Column(RectTransform rect, float gap, int padding)
        {
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = gap;
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return layout;
        }

        internal static void Size(Transform target, float width = -1, float height = -1,
            float flexibleWidth = -1, float flexibleHeight = -1, float minHeight = -1)
        {
            var layout = target.GetComponent<LayoutElement>();
            if (layout == null) layout = target.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            layout.flexibleWidth = flexibleWidth;
            layout.flexibleHeight = flexibleHeight;
            layout.minHeight = minHeight;
        }

        internal static void Stretch(RectTransform rect, float horizontalInset = 0, float verticalInset = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(horizontalInset, verticalInset);
            rect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
        }

        internal static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            return color;
        }

        static Sprite Rounded
        {
            get
            {
                if (_rounded != null) return _rounded;
                const int size = 32;
                const float radius = 6;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color[size * size];
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(radius - x, x - (size - 1 - radius), 0);
                        float dy = Mathf.Max(radius - y, y - (size - 1 - radius), 0);
                        float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                        pixels[y * size + x] = new Color(1, 1, 1, alpha);
                    }
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                texture.wrapMode = TextureWrapMode.Clamp;
                _rounded = Sprite.Create(texture, new Rect(0, 0, size, size),
                    new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect,
                    new Vector4(7, 7, 7, 7));
                return _rounded;
            }
        }
    }
}
