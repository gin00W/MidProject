using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace ChessAR
{
    // Chess has its own interface and scenes; the recycling app is not loaded.
    internal sealed class ChessUi
    {
        internal static readonly Color Paper = Color.white;
        internal static readonly Color Ink = Hex("242424");
        internal static readonly Color Muted = Hex("6A6A6A");
        internal static readonly Color Accent = Hex("416B83");
        internal static readonly Color Line = Hex("E1E1E1");
        internal readonly Font Regular;
        internal readonly Font Bold;
        readonly GameObject _root;
        readonly RectTransform _safe;
        readonly Image _background;
        GameObject _page;

        internal ChessUi(Transform parent)
        {
            Regular = Resources.Load<Font>("Fonts/SUIT-Regular");
            Bold = Resources.Load<Font>("Fonts/SUIT-SemiBold");
            if (Regular == null) Regular = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (Bold == null) Bold = Regular;
            _root = new GameObject("Chess Interface", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(parent, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390, 844);
            scaler.matchWidthOrHeight = .5f;
            var background = Rect("Background", _root.transform);
            Stretch(background);
            _background = background.gameObject.AddComponent<Image>();
            _background.raycastTarget = false;
            _safe = Rect("Safe Area", _root.transform);
            Stretch(_safe);
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Chess Events", typeof(EventSystem));
                events.transform.SetParent(parent, false);
#if ENABLE_INPUT_SYSTEM
                events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
                events.AddComponent<StandaloneInputModule>();
#endif
            }
            UpdateSafeArea();
        }

        internal void SetVisible(bool value) => _root.SetActive(value);

        internal void UpdateSafeArea()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var area = Screen.safeArea;
            _safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            _safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            _safe.offsetMin = _safe.offsetMax = Vector2.zero;
        }

        internal RectTransform Begin(bool showCamera)
        {
            if (_page != null)
            {
                _page.SetActive(false);
                UnityEngine.Object.Destroy(_page);
            }
            _background.color = showCamera ? Color.clear : Paper;
            var rect = Rect("Page", _safe);
            Stretch(rect);
            _page = rect.gameObject;
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 14, 16);
            layout.spacing = 12;
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            return rect;
        }

        internal RectTransform Card(Transform parent, int padding = 12, float gap = 6)
        {
            var rect = Rect("Card", parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Paper;
            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = gap;
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return rect;
        }

        internal RectTransform Row(Transform parent, float gap = 8)
        {
            var rect = Rect("Row", parent);
            var layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = gap;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = layout.childControlWidth = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            return rect;
        }

        internal Text Text(Transform parent, string value, int size = 15, bool bold = false,
            Color? color = null, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var rect = Rect("Text", parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = bold ? Bold : Regular;
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

        internal Button Button(Transform parent, string title, Action action,
            bool selected = false, float height = 44, int size = 14)
        {
            var rect = Rect("Button " + title, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = selected ? Ink : Color.white;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (action != null) button.onClick.AddListener(() => action());
            var colors = button.colors;
            colors.highlightedColor = new Color(.94f, .94f, .94f);
            colors.pressedColor = new Color(.8f, .8f, .8f);
            colors.disabledColor = new Color(.65f, .65f, .65f, .45f);
            button.colors = colors;
            Size(rect, height: height, flexibleWidth: 1);
            var label = Text(rect, title, size, selected, selected ? Color.white : Ink, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 8, 4);
            return button;
        }

        internal Button TextButton(Transform parent, string title, Action action,
            float height = 44, int size = 14, bool selected = false)
        {
            var button = Button(parent, title, action, false, height, size);
            button.GetComponent<Image>().color = Color.clear;
            var label = button.GetComponentInChildren<Text>();
            label.font = selected ? Bold : Regular;
            label.color = selected ? Ink : Muted;
            button.targetGraphic = label;
            return button;
        }

        internal Button Tab(Transform parent, string title, Action action, bool selected, int size = 14,
            bool compact = false)
        {
            var button = TextButton(parent, title, action, 44, size, selected);
            Stretch(button.GetComponentInChildren<Text>().rectTransform, compact ? 3 : 8, 4);
            if (selected)
            {
                var line = Rect("Selected tab", button.transform);
                line.anchorMin = Vector2.zero;
                line.anchorMax = Vector2.right;
                line.pivot = new Vector2(.5f, 0);
                line.sizeDelta = new Vector2(-16, 2);
                var image = line.gameObject.AddComponent<Image>();
                image.color = Ink;
                image.raycastTarget = false;
            }
            return button;
        }

        internal void Divider(Transform parent)
        {
            var rect = Rect("Divider", parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Line;
            image.raycastTarget = false;
            Size(rect, height: 1);
        }

        internal void Preview(Transform parent, Texture texture, Action rotate)
        {
            var imageRect = Rect("3D Preview", parent);
            var image = imageRect.gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            var fit = imageRect.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = 1;
            var control = TextButton(parent, "돌려보기", rotate, 36, 12).GetComponent<RectTransform>();
            control.GetComponent<LayoutElement>().ignoreLayout = true;
            control.anchorMin = control.anchorMax = Vector2.one;
            control.pivot = Vector2.one;
            control.sizeDelta = new Vector2(85, 36);
            control.anchoredPosition = new Vector2(-4, -4);
        }

        internal static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        internal static void Size(Transform target, float height = -1, float flexibleWidth = -1, float flexibleHeight = -1)
        {
            var element = target.GetComponent<LayoutElement>();
            if (element == null) element = target.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
        }

        internal static void Stretch(RectTransform rect, float x = 0, float y = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(x, y);
            rect.offsetMax = new Vector2(-x, -y);
        }

        static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out var color);
            return color;
        }
    }
}
