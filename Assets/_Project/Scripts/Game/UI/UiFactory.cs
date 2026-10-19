using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Tiny helpers to build uGUI from code (anchor-based, no prefabs needed). Ink-wash palette.</summary>
    public static class UiFactory
    {
        // Ink, celadon, aged paper and cinnabar: shared across every menu.
        public static readonly Color Parchment = new Color(.035f, .065f, .067f, 1f);
        public static readonly Color Ink = new Color(.065f, .105f, .108f, .97f);
        public static readonly Color InkSoft = new Color(.13f, .19f, .19f, .96f);
        public static readonly Color Red = new Color(.66f, .22f, .17f, 1f);
        public static readonly Color Gold = new Color(.83f, .72f, .48f, 1f);
        public static readonly Color GoldDim = new Color(.46f, .41f, .29f, 1f);
        public static readonly Color PaperText = new Color(.94f, .91f, .82f, 1f);
        public static readonly Color Dim = new Color(.65f, .71f, .67f, 1f);
        public static readonly Color Jade = new Color(.39f, .62f, .55f, 1f);

        /// <summary>Colour of an item grade: 일반 grey, 고급 green, 희귀 blue, 영웅 purple, 전설 orange, 신화 red.</summary>
        public static Color GradeColor(BiRyeoDo.Core.ItemGrade grade)
        {
            switch (grade)
            {
                case BiRyeoDo.Core.ItemGrade.Fine: return new Color(0.45f, 0.85f, 0.45f, 1f);
                case BiRyeoDo.Core.ItemGrade.Rare: return new Color(0.40f, 0.65f, 1.00f, 1f);
                case BiRyeoDo.Core.ItemGrade.Epic: return new Color(0.75f, 0.50f, 1.00f, 1f);
                case BiRyeoDo.Core.ItemGrade.Legendary: return new Color(1.00f, 0.65f, 0.20f, 1f);
                case BiRyeoDo.Core.ItemGrade.Mythic: return new Color(1.00f, 0.30f, 0.35f, 1f);
                case BiRyeoDo.Core.ItemGrade.Transcendent: return new Color(1.00f, 0.90f, 0.45f, 1f);
                default: return new Color(0.75f, 0.76f, 0.80f, 1f);
            }
        }

        /// <summary>Rich-text hex such as "#66A6FF" for a grade colour.</summary>
        public static string GradeHex(BiRyeoDo.Core.ItemGrade grade) { return "#" + ColorUtility.ToHtmlStringRGB(GradeColor(grade)); }

        /// <summary>"[희귀] 이름" with the grade tag coloured.</summary>
        public static string GradedName(BiRyeoDo.Core.ItemData item)
        {
            return "<color=" + GradeHex(item.Grade) + ">[" + BiRyeoDo.Core.Grades.NameOf(item.Grade) + "]</color> " + item.Name;
        }

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

        /// <summary>Quiet ruled edges with stronger scroll-like corner brackets.</summary>
        public static void Frame(Transform parent, Color color, float thickness = 3f)
        {
            var graphic = Rect("Frame", parent, Vector2.zero, Vector2.one).gameObject.AddComponent<WuxiaGlyph>();
            graphic.Key = "frame";
            graphic.Stroke = thickness;
            graphic.color = color;
            graphic.raycastTarget = false;
        }

        public static WuxiaGlyph Glyph(string key, Transform parent, Vector2 min, Vector2 max, Color tint)
        {
            var glyph = Rect("Glyph_" + key, parent, min, max).gameObject.AddComponent<WuxiaGlyph>();
            glyph.Key = key;
            glyph.color = tint;
            glyph.raycastTarget = false;
            return glyph;
        }

        private static Font _displayFont;
        public static Text Heading(Text text)
        {
            if (_displayFont == null)
            {
                _displayFont = KoreanFont;
                var installed = Font.GetOSInstalledFontNames();
                foreach (var candidate in new[] { "Noto Serif CJK KR", "Noto Serif KR", "NanumMyeongjo", "Batang" })
                    if (System.Array.IndexOf(installed, candidate) >= 0)
                    {
                        _displayFont = Font.CreateDynamicFontFromOSFont(candidate, 40);
                        break;
                    }
            }
            text.font = _displayFont;
            return text;
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
            if (name == "Title" || name == "Header") Heading(t);
            var shadow = t.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(1f, -1f);
            return t;
        }

        /// <summary>Reading-text style: a fixed font size (no shrink-to-fit, so sizes match across rows) with roomier lines.</summary>
        public static Text Body(Text t, float lineSpacing = 1.25f)
        {
            t.resizeTextForBestFit = false;
            t.lineSpacing = lineSpacing;
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
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, .96f, .86f, 1f);
            colors.pressedColor = new Color(.68f, .71f, .65f, 1f);
            colors.disabledColor = new Color(.55f, .58f, .55f, .65f);
            colors.fadeDuration = .12f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(onClick);
            btn.onClick.AddListener(() => Systems.SoundManager.Sfx("ui_click"));
            if (bg.a > 0.05f) Frame(img.transform, GoldDim, 2f);
            Label("Label", img.transform, new Vector2(.04f, 0f), new Vector2(.96f, 1f), label, fontSize, PaperText);
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
            fillImg.sprite = Gradient;
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
