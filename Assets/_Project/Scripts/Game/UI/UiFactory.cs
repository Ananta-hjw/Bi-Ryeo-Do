using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Tiny helpers to build uGUI from code (anchor-based, no prefabs needed). Ink-wash palette.</summary>
    public static class UiFactory
    {
        public static readonly Color Parchment = new Color(0.93f, 0.90f, 0.82f, 1f);
        public static readonly Color Ink = new Color(0.08f, 0.08f, 0.08f, 0.88f);
        public static readonly Color InkSoft = new Color(0.20f, 0.20f, 0.18f, 0.75f);
        public static readonly Color Red = new Color(0.78f, 0.16f, 0.12f, 1f);
        public static readonly Color Gold = new Color(0.90f, 0.72f, 0.22f, 1f);
        public static readonly Color PaperText = new Color(0.95f, 0.93f, 0.86f, 1f);

        private static Font _font;

        /// <summary>Dynamic OS font so Hangul renders without importing a font asset (Noto CJK on Android, Malgun on Windows).</summary>
        public static Font KoreanFont
        {
            get
            {
                if (_font == null)
                {
                    _font = Font.CreateDynamicFontFromOSFont(new[]
                    {
                        "Noto Sans CJK KR", "NotoSansCJK-Regular", "Noto Sans KR", "NanumGothic",
                        "Malgun Gothic", "Apple SD Gothic Neo", "AppleGothic", "Droid Sans Fallback"
                    }, 32);
                }
                return _font;
            }
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var img = Rect(name, parent, min, max).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(string name, Transform parent, Vector2 min, Vector2 max, string text, int size,
            Color color, TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var t = Rect(name, parent, min, max).gameObject.AddComponent<Text>();
            t.font = KoreanFont;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 12;
            t.resizeTextMaxSize = size;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(string name, Transform parent, Vector2 min, Vector2 max, string label, Color bg,
            UnityAction onClick, int fontSize = 40)
        {
            var img = Rect(name, parent, min, max).gameObject.AddComponent<Image>();
            img.color = bg;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);
            Label("Label", img.transform, Vector2.zero, Vector2.one, label, fontSize, PaperText);
            return btn;
        }

        /// <summary>Horizontal bar whose fill is driven by anchors (works without a sprite).</summary>
        public sealed class Bar
        {
            public RectTransform Root;
            public RectTransform Fill;

            public void SetPct(float pct)
            {
                Fill.anchorMax = new Vector2(Mathf.Clamp01(pct), 1f);
            }
        }

        public static Bar MakeBar(string name, Transform parent, Vector2 min, Vector2 max, Color back, Color fill)
        {
            var bg = Panel(name, parent, min, max, back);
            var fillImg = Panel("Fill", bg.transform, Vector2.zero, Vector2.one, fill);
            return new Bar { Root = bg.rectTransform, Fill = fillImg.rectTransform };
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
