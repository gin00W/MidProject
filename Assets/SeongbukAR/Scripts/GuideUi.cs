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
        internal static readonly Color Ink = Hex("183E35");
        internal static readonly Color Muted = Hex("5A6B62");
        internal static readonly Color Paper = Hex("F6F7F3");
        internal static readonly Color Soft = Hex("E9F0E8");
        internal static readonly Color Line = Hex("DDE5DC");
        internal static readonly Color Orange = Hex("A85A30");

        internal readonly Font Regular;
        internal readonly Font Semibold;
        readonly RectTransform _safe;
        readonly Image _background;
        GameObject _page;
        Rect _lastSafe;
        static Sprite _rounded;

        internal GuideUi(Transform parent)
        {
            Regular = Resources.Load<Font>("Fonts/Pretendard-Regular");
            Semibold = Resources.Load<Font>("Fonts/Pretendard-SemiBold");
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
            var layout = Column(page, 14, 22);
            layout.padding.top = 16;
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
            text.lineSpacing = 1.12f;
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
            image.color = fill ?? (primary ? Ink : Color.white);
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.94f, 0.97f, 0.95f);
            colors.pressedColor = new Color(0.80f, 0.87f, 0.82f);
            colors.disabledColor = new Color(0.72f, 0.76f, 0.73f);
            button.colors = colors;
            button.onClick.AddListener(() => action?.Invoke());
            Size(rect, height: height, flexibleWidth: 1);
            var text = Label(rect, label, textSize, true,
                primary ? Color.white : Ink, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 14, 8);
            return button;
        }

        internal Button Choice(Transform parent, string label, Action action)
        {
            float lines = Mathf.Clamp(Mathf.Ceil(label.Length / 19f), 1, 4);
            return Button(parent, label, action, false, Mathf.Max(56, lines * 21 + 24));
        }

        internal InputField SearchField(Transform parent, string value, Action<string> onChange)
        {
            var rect = NewRect("Search Field", parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            Size(rect, height: 54);
            var input = rect.gameObject.AddComponent<InputField>();
            input.targetGraphic = image;
            input.characterLimit = 60;
            input.lineType = InputField.LineType.SingleLine;
            var placeholder = Label(rect, "엽떡 용기, 캔, 단추…", 16, false, Muted, TextAnchor.MiddleLeft);
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
            Column(content, 12, 0).padding.bottom = 12;
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
            Size(row, height: 42);
            if (back != null)
            {
                var button = Button(row, "‹", back, false, 42, 27, Soft);
                Size(button.transform, width: 42, height: 42, flexibleWidth: 0);
            }
            var label = Label(row, title, back == null ? 23 : 19, true, Ink, TextAnchor.MiddleLeft);
            Size(label.transform, flexibleWidth: 1);
            var region = Label(row, "성북구", 13, true, Muted, TextAnchor.MiddleRight);
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
                const int size = 96;
                const float radius = 23;
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
                    new Vector4(24, 24, 24, 24));
                return _rounded;
            }
        }
    }
}
