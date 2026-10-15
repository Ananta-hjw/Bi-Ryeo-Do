using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Tiny helpers to build uGUI from code (anchor-based, no prefabs needed). Ink-wash palette.</summary>
    public static class UiFactory
    {
        // Dark navy + antique gold, in the spirit of a modern mobile RPG menu (step 1 of the Brown Dust-style UI pass).
        public static readonly Color Parchment = new Color(0.05f, 0.06f, 0.09f, 1f);   // screen backdrop
        public static readonly Color Ink = new Color(0.09f, 0.11f, 0.16f, 0.95f);       // panel / card fill
        public static readonly Color InkSoft = new Color(0.17f, 0.20f, 0.27f, 0.92f);   // secondary button / chip fill
        public static readonly Color Red = new Color(0.66f, 0.17f, 0.20f, 1f);          // primary action / danger
        public static readonly Color Gold = new Color(0.86f, 0.72f, 0.42f, 1f);         // accents, titles
        public static readonly Color GoldDim = new Color(0.52f, 0.43f, 0.26f, 1f);      // frames
        public static readonly Color PaperText = new Color(0.93f, 0.94f, 0.96f, 1f);    // body text
        public static readonly Color Dim = new Color(0.64f, 0.68f, 0.76f, 1f);          // hints on the backdrop

        private static Sprite _gradient;

        /// <summary>1x64 white-to-grey vertical gradient; tinted by Image.color it gives every button a subtle lit top edge.</summary>
        public static Sprite Gradient
        {
            get
            {
                if (_gradient == null)
                {
                    var tex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                    for (int y = 0; y < 64; y++)
                    {
                        float v = Mathf.Lerp(0.62f, 1.0f, y / 63f);
                        tex.SetPixel(0, y, new Color(v, v, v, 1f));
                    }
                    tex.Apply();
                    _gradient = Sprite.Create(tex, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f), 100f);
                }
                return _gradient;
            }
        }

        /// <summary>Thin gold border drawn with four edge images (no sprite needed).</summary>
        public static void Frame(Transform parent, Color color, float thickness = 3f)
        {
            var root = Rect("Frame", parent, Vector2.zero, Vector2.one);
            var edges = new[]
            {
                new[] { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -thickness), new Vector2(0f, 0f) }, // top
                new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, thickness) },   // bottom
                new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(thickness, 0f) },  // left
                new[] { new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-thickness, 0f), new Vector2(0f, 0f) }, // right
            };
            foreach (var e in edges)
            {
                var rt = Rect("Edge", root, e[0], e[1]);
                rt.offsetMin = e[2];
                rt.offsetMax = e[3];
                var img = rt.gameObject.AddComponent<Image>();
                img.color = color;
                img.raycastTarget = false;
            }
        }

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
            var shadow = t.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        public static Button Button(string name, Transform parent, Vector2 min, Vector2 max, string label, Color bg,
            UnityAction onClick, int fontSize = 40)
        {
            var img = Rect(name, parent, min, max).gameObject.AddComponent<Image>();
            img.sprite = Gradient;
            img.type = Image.Type.Simple;
            img.color = bg;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);
            btn.onClick.AddListener(() => Systems.SoundManager.Sfx("ui_click"));
            if (bg.a > 0.05f) Frame(img.transform, GoldDim, 2f);
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
