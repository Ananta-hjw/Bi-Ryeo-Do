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
        public static readonly Color Dim = new Color(.73f, .78f, .73f, 1f);
        public static readonly Color Jade = new Color(.39f, .62f, .55f, 1f);

        public const string BreakthroughHex = "#8ECFFF";
        private const string LockedTraitHex = "#9CA6A0";

        /// <summary>Highlights only unlocked, fixed breakthrough effects; base effects keep the label colour.</summary>
        public static string GearEffectText(BiRyeoDo.Core.ItemData item, int enhance = 0, int stars = 0)
        {
            return BiRyeoDo.Core.StatusSheet.EffectText(item, enhance, stars, BreakthroughHex);
        }

        public static string GearTraitsText(BiRyeoDo.Core.ItemData item, int stars)
        {
            return BiRyeoDo.Core.Gear.TraitsText(item, stars, BreakthroughHex, LockedTraitHex);
        }

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

        private static Sprite _bottomShade;
        /// <summary>A neutral alpha fade, with no hard top edge or background-specific tint.</summary>
        public static Sprite BottomShade
        {
            get
            {
                if (_bottomShade == null)
                {
                    var texture = new Texture2D(1, 64, TextureFormat.RGBA32, false)
                    { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                    for (int y = 0; y < 64; y++)
                        texture.SetPixel(0, y, new Color(0f, 0f, 0f, .55f * Mathf.Pow(1f - y / 63f, 1.5f)));
                    texture.Apply(false, true);
                    _bottomShade = Sprite.Create(texture, new Rect(0f, 0f, 1f, 64f), new Vector2(.5f, .5f), 100f);
                }
                return _bottomShade;
            }
        }

        /// <summary>Shared circular alpha silhouette for UI stencil masks.</summary>
        public static Sprite CircleMask { get { return HelpCircle; } }

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

        /// <summary>A large square picture whose proportions stay consistent as list rows grow.</summary>
        public static Image TopIcon(Transform parent, float size, float top = 16f)
        {
            var image = Panel("Icon", parent, new Vector2(.5f, 1f), new Vector2(.5f, 1f), Color.white);
            image.rectTransform.pivot = new Vector2(.5f, 1f);
            image.rectTransform.sizeDelta = new Vector2(size, size);
            image.rectTransform.anchoredPosition = new Vector2(0f, -top);
            image.preserveAspect = true;
            return image;
        }

        public static Text Label(string name, Transform parent, Vector2 min, Vector2 max, string text, int size,
            Color color, TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Bold)
        {
            var t = Rect(name, parent, min, max).gameObject.AddComponent<Text>();
            t.font = KoreanFont;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color == InkSoft ? Dim : color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Min(size, 28);
            t.resizeTextMaxSize = size;
            t.raycastTarget = false;
            if (name == "Title" || name == "Header") Heading(t);
            var shadow = t.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            shadow.effectDistance = new Vector2(1f, -1f);
            return t;
        }

        /// <summary>A small fixed-size badge placed in pixels from its parent's top-left; returns its width so callers can line badges up.</summary>
        public static float Chip(Transform parent, float x, float y, string text, Color back, float height = 42f, int size = 24)
        {
            var chip = Panel("Chip", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), back);
            chip.raycastTarget = false;
            var label = Body(Label("Text", chip.transform, Vector2.zero, Vector2.one, text, size, PaperText, TextAnchor.MiddleCenter, FontStyle.Normal));
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            float width = label.preferredWidth;
            if (width < 1f) width = text.Length * size * .8f;   // no layout info yet: a generous estimate
            width = Mathf.Ceil(width) + 22f;
            var rt = chip.rectTransform;
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);
            return width;
        }

        /// <summary>Reading-text style: a fixed font size (no shrink-to-fit, so sizes match across rows) with roomier lines.</summary>
        public static Text Body(Text t, float lineSpacing = 1.25f)
        {
            t.resizeTextForBestFit = false;
            t.lineSpacing = lineSpacing;
            return t;
        }

        /// <summary>Measured wrapping height, in canvas units, including rich text and the current font.</summary>
        public static float MeasureHeight(Text text, string value, float width)
        {
            var settings = text.GetGenerationSettings(new Vector2(Mathf.Max(1f, width), 0f));
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            settings.resizeTextForBestFit = false;
            return Mathf.Ceil(text.cachedTextGeneratorForLayout.GetPreferredHeight(value ?? "", settings) / text.pixelsPerUnit);
        }

        /// <summary>Wraps an existing paragraph in a masked, draggable reading area without shrinking its letters.</summary>
        public static Text ScrollBody(Text text, int fontSize = 36)
        {
            if (text.GetComponent<ReadingText>() != null) return text;
            var rect = text.rectTransform;
            var viewport = Rect(text.name + "Viewport", rect.parent, rect.anchorMin, rect.anchorMax);
            viewport.offsetMin = rect.offsetMin; viewport.offsetMax = rect.offsetMax;
            viewport.SetSiblingIndex(rect.GetSiblingIndex());
            var hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            rect.SetParent(viewport, false);
            rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            // Keep a left gutter and reserve the right gutter for the scrollbar.
            rect.sizeDelta = new Vector2(-58f, 0f);
            rect.anchoredPosition = new Vector2(16f, 0f);
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Normal;
            text.alignment = TextAnchor.UpperLeft;
            Body(text, 1.3f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = rect;
            scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 50f;
            ScrollIndicator(scroll, viewport);
            var reading = text.gameObject.AddComponent<ReadingText>();
            reading.Viewport = viewport; reading.Scroll = scroll;
            return text;
        }

        public static void ScrollIndicator(ScrollRect scroll, RectTransform viewport)
        {
            var track = Panel("ScrollTrack", viewport, new Vector2(1f, 0f), Vector2.one, InkSoft);
            track.rectTransform.offsetMin = new Vector2(-8f, 0f);
            var thumb = Panel("Thumb", track.transform, Vector2.zero, Vector2.one, GoldDim);
            track.raycastTarget = true; thumb.raycastTarget = true;
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.targetGraphic = thumb; bar.handleRect = thumb.rectTransform;
            bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        public static RectTransform ReadingViewport(Text text)
        {
            var reading = text.GetComponent<ReadingText>();
            return reading != null ? reading.Viewport : text.rectTransform;
        }

        /// <summary>Story-only pagination. Keeps the original characters and line breaks intact.</summary>
        public static System.Collections.Generic.List<string> Pages(Text layout, string value)
        {
            float width = Mathf.Max(1f, layout.rectTransform.rect.width);
            float height = Mathf.Max(layout.fontSize * 2f, layout.rectTransform.rect.height - layout.fontSize * .5f);
            return ReadingPages.Split(value, text => MeasureHeight(layout, text, width), height);
        }

        public const float TabHeaderHeight = 115f;
        private const float TabHeaderMargin = 10f;

        /// <summary>A fixed-height top lane, including inside the shorter martial-arts page.</summary>
        public static RectTransform TopLane(string name, Transform parent, float left = .04f, float right = .96f)
        {
            var rect = Rect(name, parent, new Vector2(left, 1f), new Vector2(right, 1f));
            rect.offsetMin = new Vector2(0f, -TabHeaderMargin - TabHeaderHeight);
            rect.offsetMax = new Vector2(0f, -TabHeaderMargin);
            return rect;
        }

        public static Text TabHeader(Transform parent, string icon, string title = "", float titleRight = .84f)
        {
            var band = TopLane("HeaderBand", parent);
            var background = band.gameObject.AddComponent<Image>();
            background.color = Ink; background.raycastTarget = false;
            Frame(band, GoldDim, 2f);
            var glyph = Glyph(icon, band, new Vector2(0f, .5f), new Vector2(0f, .5f), Gold);
            glyph.rectTransform.pivot = new Vector2(0f, .5f);
            glyph.rectTransform.sizeDelta = new Vector2(92f, 92f);
            glyph.rectTransform.anchoredPosition = new Vector2(24f, 0f);
            glyph.Stroke = 3f;
            var text = Label("Header", band, new Vector2(0f, .10f), new Vector2(titleRight, .90f), title, 40, Gold, TextAnchor.MiddleLeft);
            text.rectTransform.offsetMin = new Vector2(128f, 0f);
            return text;
        }

        public static void CenterInHeader(RectTransform rect, float height)
        {
            var min = rect.offsetMin; var max = rect.offsetMax;
            rect.anchorMin = new Vector2(rect.anchorMin.x, 1f);
            rect.anchorMax = new Vector2(rect.anchorMax.x, 1f);
            float center = -TabHeaderMargin - TabHeaderHeight / 2f;
            rect.offsetMin = new Vector2(min.x, center - height / 2f);
            rect.offsetMax = new Vector2(max.x, center + height / 2f);
        }

        private static Sprite _helpCircle;

        private static Sprite HelpCircle
        {
            get
            {
                if (_helpCircle == null)
                {
                    const int size = 128;
                    var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                    {
                        wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
                    };
                    for (int y = 0; y < size; y++)
                        for (int x = 0; x < size; x++)
                        {
                            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f));
                            var color = distance > 60f ? GoldDim : InkSoft;
                            color.a *= Mathf.Clamp01(63.5f - distance);
                            texture.SetPixel(x, y, color);
                        }
                    // Keep pixels readable for the circular alpha hit test.
                    texture.Apply();
                    _helpCircle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100f);
                }
                return _helpCircle;
            }
        }

        /// <summary>Compact circular help control with the same diameter on every screen.</summary>
        public static Button HelpButton(Transform parent, UnityAction onClick)
        {
            var rect = Rect("Help", parent, new Vector2(.905f, .935f), new Vector2(.905f, .935f));
            rect.sizeDelta = new Vector2(76f, 76f);
            CenterInHeader(rect, 76f);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = HelpCircle;
            image.color = Color.white;
            image.preserveAspect = true;
            image.alphaHitTestMinimumThreshold = .1f;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null) button.onClick.AddListener(onClick);
            button.onClick.AddListener(() => Systems.SoundManager.Sfx("ui_click"));
            Label("Label", rect, new Vector2(.05f, .05f), new Vector2(.95f, .95f), "?", 32, Gold);
            return button;
        }

        /// <summary>A small explanation window over a tab (hidden at first): tap the backdrop or 「닫기」 to close; the body scrolls. Show it with <see cref="ShowInfo"/>.</summary>
        public static RectTransform InfoWindow(Transform parent, string title, string body)
        {
            var window = Rect("InfoWindow", parent, Vector2.zero, Vector2.one);
            var dim = window.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.78f);   // dims and blocks what is underneath
            var tap = window.gameObject.AddComponent<Button>();
            tap.targetGraphic = dim;
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => window.gameObject.SetActive(false));
            var card = Panel("Card", window, new Vector2(.15f, .10f), new Vector2(.85f, .90f), new Color(0.05f, 0.07f, 0.11f, 0.99f));
            Frame(card.transform, GoldDim, 3f);
            Label("Title", card.transform, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f), title, 40, Gold);
            card.raycastTarget = true;
            var cardTap = card.gameObject.AddComponent<Button>();
            cardTap.targetGraphic = card;
            cardTap.transition = Selectable.Transition.None;
            Body(ScrollBody(Label("Body", card.transform, new Vector2(.06f, .20f), new Vector2(.94f, .83f), body, 32, PaperText, TextAnchor.UpperLeft, FontStyle.Normal), 32), 1.35f);
            BackButton(card.transform, new Vector2(0.30f, 0.04f), new Vector2(0.70f, 0.17f), () => window.gameObject.SetActive(false));
            window.gameObject.SetActive(false);
            return window;
        }

        /// <summary>A yes/no warning on top of <paramref name="parent"/>: 「취소」 just closes it, the confirm button closes it and runs <paramref name="onConfirm"/>. Used before anything that cannot be undone (분해, 전투 포기).</summary>
        public static RectTransform Confirm(Transform parent, string title, string body, string confirmLabel, System.Action onConfirm)
        {
            var window = Rect("ConfirmWindow", parent, Vector2.zero, Vector2.one);
            var dim = window.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.78f);   // blocks taps on what is underneath
            var card = Panel("Card", window, new Vector2(.20f, .22f), new Vector2(.80f, .78f), new Color(0.05f, 0.07f, 0.11f, 0.99f));
            Frame(card.transform, Red, 3f);
            card.raycastTarget = true;
            Label("Title", card.transform, new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.96f), title, 40, Red);
            Body(ScrollBody(Label("Body", card.transform, new Vector2(.07f, .30f), new Vector2(.93f, .78f), body, 30, PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 32), 1.3f);
            Button("Cancel", card.transform, new Vector2(0.06f, .04f), new Vector2(0.47f, .26f), "취소", InkSoft, () => Object.Destroy(window.gameObject), 32);
            Button("Confirm", card.transform, new Vector2(0.53f, .04f), new Vector2(0.94f, .26f), confirmLabel, Red, () =>
            {
                Object.Destroy(window.gameObject);
                if (onConfirm != null) onConfirm();
            }, 32);
            window.SetAsLastSibling();
            return window;
        }

        public static void ShowInfo(RectTransform window)
        {
            if (window == null) return;
            window.SetAsLastSibling();
            window.gameObject.SetActive(true);
        }

        /// <summary>돌아가기 in a card or pop-up: a button in the given place showing only the thin gold U-turn arrow (it replaced every 닫기).</summary>
        public static Button BackButton(Transform parent, Vector2 min, Vector2 max, UnityAction onClick)
        {
            var button = Button("Close", parent, min, max, "", InkSoft, onClick, 28);
            var arrow = Glyph("back", button.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Gold);
            arrow.rectTransform.sizeDelta = new Vector2(84f, 72f);
            arrow.raycastTarget = false;
            return button;
        }

        /// <summary>돌아가기 on a full-screen overlay: the same arrow in the same top-left spot as the screens' back bar.</summary>
        public static Button BackCorner(Transform parent, UnityAction onClick)
        {
            var button = Button("Back", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), "", Color.clear, onClick, 28);
            var rect = (RectTransform)button.transform;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(16f, -6f);
            rect.sizeDelta = new Vector2(135f, 96f);
            var arrow = Glyph("back", button.transform, new Vector2(.12f, .1f), new Vector2(.88f, .9f), Gold);
            arrow.raycastTarget = false;
            return button;
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
            Label("Label", img.transform, new Vector2(.05f, .08f), new Vector2(.95f, .92f), label, Mathf.Max(30, fontSize), PaperText);
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
