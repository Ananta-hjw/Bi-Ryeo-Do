using System.Collections;
using System.Collections.Generic;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>The 필살기 cut-in and its heavier hits, drawn from plain UI shapes (no extra art): the field goes dark, an ink band slashes across with speed lines, the user's standing art slides in, the art's name stamps down with gold lightning, then the band is cut away in a white flash and the field shakes.</summary>
    public partial class BattleScreen
    {
        private static readonly Color UltimateInk = new Color(0.05f, 0.04f, 0.035f, 0.97f);
        private static readonly Color UltimateGlow = new Color(1f, 0.86f, 0.42f, 1f);
        private const float UltimateBandTilt = -7f;
        /// <summary>Standing art height as a share of the screen: about the upper two thirds (head to thigh) stays on screen.</summary>
        private const float UltimateStandingHeight = 1.45f;
        /// <summary>Real seconds the whole cut-in lasts, whatever the battle speed (the held frame takes up the rest).</summary>
        private const float UltimateLength = 3.5f;
        private RectTransform _ultimateLayer;

        /// <summary>Removes a cut-in left half-drawn when the overlay is reopened (its coroutine was stopped).</summary>
        private void ClearUltimateCutIn()
        {
            if (_ultimateLayer != null) { _home.Remove(_ultimateLayer); Destroy(_ultimateLayer.gameObject); }
            _ultimateLayer = null;
        }

        private IEnumerator UltimateCutIn(string skillName, string artKey)
        {
            ClearUltimateCutIn();
            SoundManager.Sfx("ultimate");
            SoundManager.Haptic(120);

            var layer = UiFactory.Rect("UltimateCutIn", _root, Vector2.zero, Vector2.one);
            layer.SetSiblingIndex(_banner.transform.GetSiblingIndex() + 1);
            _ultimateLayer = layer;
            float w = Mathf.Max(1f, _root.rect.width), h = Mathf.Max(1f, _root.rect.height);

            var dark = Fill(layer, "Dark", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));

            var band = UiFactory.Rect("Band", layer, new Vector2(-0.3f, 0.33f), new Vector2(1.3f, 0.67f));
            band.localRotation = Quaternion.Euler(0f, 0f, UltimateBandTilt);
            Fill(band, "Ink", Vector2.zero, Vector2.one, UltimateInk);
            Fill(band, "Red", new Vector2(0f, 0.86f), new Vector2(1f, 0.9f), new Color(0.62f, 0.08f, 0.05f, 0.9f));
            Fill(band, "Red", new Vector2(0f, 0.1f), new Vector2(1f, 0.14f), new Color(0.62f, 0.08f, 0.05f, 0.9f));
            Fill(band, "EdgeTop", new Vector2(0f, 0.965f), new Vector2(1f, 1f), UltimateGlow);
            Fill(band, "EdgeBottom", new Vector2(0f, 0f), new Vector2(1f, 0.035f), UltimateGlow);

            // Speed lines race right to left across the band.
            float bandW = w * 1.6f, bandH = h * 0.34f;
            var lines = new List<RectTransform>();
            var lineSpeed = new List<float>();
            for (int i = 0; i < 22; i++)
            {
                var rt = UiFactory.Rect("Speed", band, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                rt.sizeDelta = new Vector2(Random.Range(160f, 620f), Random.Range(3f, 9f));
                rt.anchoredPosition = new Vector2(Random.Range(-bandW / 2f, bandW / 2f), Random.Range(-bandH * 0.42f, bandH * 0.42f));
                var img = rt.gameObject.AddComponent<Image>();
                img.color = new Color(1f, 0.95f, 0.8f, Random.Range(0.12f, 0.4f));
                img.raycastTarget = false;
                lines.Add(rt);
                lineSpeed.Add(Random.Range(2.4f, 4.2f) * w);
            }

            // The user's standing art (SD figure only when it is missing), taller than the band so the upper body bursts out of it; the legs run off the bottom of the screen.
            Image figure = null;
            RectTransform figureRt = null;
            var sprite = string.IsNullOrEmpty(artKey) ? null : ArtLibrary.StandingPortrait(artKey);
            bool standing = sprite != null;
            if (!standing && !string.IsNullOrEmpty(artKey)) sprite = BattleFigure(artKey);
            if (sprite != null)
            {
                float aspect = sprite.rect.height > 0f ? sprite.rect.width / sprite.rect.height : 0.66f;
                float figureH = h * (standing ? UltimateStandingHeight : 0.74f);
                figureRt = UiFactory.Rect("Figure", layer, new Vector2(0.22f, 0.98f), new Vector2(0.22f, 0.98f));
                figureRt.pivot = new Vector2(0.5f, 1f);
                figureRt.sizeDelta = new Vector2(figureH * aspect, figureH);
                figure = figureRt.gameObject.AddComponent<Image>();
                figure.sprite = sprite;
                figure.preserveAspect = true;
                figure.raycastTarget = false;
                figure.color = new Color(1f, 1f, 1f, 0f);
            }

            var caption = UiFactory.Label("Caption", layer, new Vector2(0.36f, 0.6f), new Vector2(0.98f, 0.68f), "必 殺 · 필살기", 44, UltimateGlow);
            caption.resizeTextForBestFit = false;
            caption.horizontalOverflow = HorizontalWrapMode.Overflow;
            caption.color = new Color(1f, 0.86f, 0.42f, 0f);
            var title = UiFactory.Label("Name", layer, new Vector2(0.36f, 0.38f), new Vector2(0.98f, 0.6f), skillName, 136, UltimateGlow);
            title.resizeTextForBestFit = false;
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
            var outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.06f, 0.02f, 1f);
            outline.effectDistance = new Vector2(5f, -5f);
            title.color = new Color(1f, 0.86f, 0.42f, 0f);

            var flash = Fill(layer, "Flash", Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0.9f));

            // The name and figure hold on screen before the closing slash so the whole cut-in lasts UltimateLength real seconds.
            float hold = Mathf.Max(0f, UltimateLength - 1.25f / Pace);
            float total = 1.25f / Pace + hold, enter = 0.12f / Pace, stamp = 0.34f / Pace, second = 0.62f / Pace, exit = 0.98f / Pace + hold;
            bool stamped = false, struck = false, cut = false;
            float nextBolt = second + 0.5f / Pace;
            for (float e = 0f; e < total; e += Time.deltaTime)
            {
                if (layer == null) yield break;
                float k;

                // Dark field and the opening white pop.
                SetColorAlpha(dark, Mathf.Min(0.78f, e / enter * 0.78f) * (e > exit ? 1f - (e - exit) / (total - exit) : 1f));
                if (!cut) SetColorAlpha(flash, Mathf.Max(0f, 0.9f * (1f - e / enter)));

                // Band: slides in and opens, then is sliced shut.
                k = Mathf.Clamp01(e / (enter * 1.4f));
                float open = e < exit ? EaseOut(k) : 1f - EaseOut(Mathf.Clamp01((e - exit) / (0.12f / Pace)));
                band.localScale = new Vector3(1f, Mathf.Max(0.001f, open), 1f);
                band.anchoredPosition = new Vector2((1f - EaseOut(k)) * w, 0f);

                for (int i = 0; i < lines.Count; i++)
                {
                    var p = lines[i].anchoredPosition;
                    p.x -= lineSpeed[i] * Time.deltaTime;
                    if (p.x < -bandW / 2f) p.x += bandW;
                    lines[i].anchoredPosition = p;
                }

                // Figure slides in from the left and keeps drifting.
                if (figureRt != null)
                {
                    float fk = EaseOut(Mathf.Clamp01((e - enter * 0.6f) / (0.22f / Pace)));
                    figureRt.anchoredPosition = new Vector2(-w * 0.45f * (1f - fk) + e * 30f * Pace, 0f);
                    SetColorAlpha(figure, e < exit ? fk : Mathf.Clamp01(1f - (e - exit) / (0.14f / Pace)));
                }

                // Name stamps down from twice its size with a little overshoot.
                float nk = Mathf.Clamp01((e - enter * 1.5f) / (stamp - enter * 1.5f));
                float scale = nk < 1f ? Mathf.Lerp(2.6f, 0.92f, EaseIn(nk)) : Mathf.Lerp(0.92f, 1f, Mathf.Clamp01((e - stamp) / (0.08f / Pace)));
                title.rectTransform.localScale = new Vector3(scale, scale, 1f);
                float textAlpha = e < exit ? nk : Mathf.Clamp01(1f - (e - exit) / (0.12f / Pace));
                SetColorAlpha(title, textAlpha);
                SetColorAlpha(caption, e < stamp ? 0f : textAlpha);

                if (!stamped && e >= stamp)
                {
                    stamped = true;
                    SoundManager.Haptic(60);
                    StartCoroutine(Shake(layer, 16f));
                    for (int i = 0; i < 3; i++) Bolt(layer, w, h, Random.Range(0.4f, 0.95f));
                }
                if (!struck && e >= second)
                {
                    struck = true;
                    for (int i = 0; i < 2; i++) Bolt(layer, w, h, Random.Range(0.1f, 0.95f));
                }
                // Stray bolts keep the held frame alive.
                if (struck && e >= nextBolt && e < exit - 0.2f / Pace)
                {
                    nextBolt += 0.5f / Pace;
                    Bolt(layer, w, h, Random.Range(0.1f, 0.95f));
                }
                if (!cut && e >= exit)
                {
                    cut = true;
                    SlashLine(layer, w);
                    SetColorAlpha(flash, 0.85f);
                }
                if (cut) SetColorAlpha(flash, Mathf.Max(0f, flash.color.a - Time.deltaTime * 5f * Pace));
                yield return null;
            }

            ClearUltimateCutIn();
            StartCoroutine(Punch(1.12f));
            StartCoroutine(Shake(_cardsRoot, 26f));
            StartCoroutine(Shake(_partyRoot, 12f));
        }

        /// <summary>Extra weight on each hit of a 필살기: gold rays burst from the target, a warm flash and a field shake.</summary>
        private void UltimateImpact(Vector2 at)
        {
            for (int i = 0; i < 12; i++)
            {
                var rt = UiFactory.Rect("Ray", _fx, at, at);
                float a = i * 30f + Random.Range(-8f, 8f);
                rt.sizeDelta = new Vector2(Random.Range(120f, 220f), Random.Range(6f, 12f));
                rt.localRotation = Quaternion.Euler(0f, 0f, a);
                var img = rt.gameObject.AddComponent<Image>();
                img.color = UltimateGlow; img.raycastTarget = false;
                StartCoroutine(SparkAnim(rt, img, a * Mathf.Deg2Rad));
            }
            StartCoroutine(FlashColor(new Color(1f, 0.93f, 0.75f), 0.35f, 0.16f));
            StartCoroutine(Shake(_cardsRoot, 14f));
        }

        private IEnumerator FlashColor(Color color, float peak, float seconds)
        {
            float d = seconds / Pace;
            for (float e = 0f; e < d; e += Time.deltaTime)
            {
                color.a = peak * (1f - e / d);
                _flash.color = color;
                yield return null;
            }
            color.a = 0f;
            _flash.color = color;
        }

        /// <summary>A jagged gold lightning bolt from the top of the screen down to the band, built from short rotated bars plus a soft glow.</summary>
        private void Bolt(RectTransform parent, float w, float h, float x01)
        {
            var bolt = UiFactory.Rect("Bolt", parent, Vector2.zero, Vector2.one);
            var points = new List<Vector2>();
            float x = (x01 - 0.5f) * w, y = h * 0.55f, endY = Random.Range(-0.05f, 0.12f) * h;
            points.Add(new Vector2(x, y));
            int steps = 7;
            for (int i = 1; i <= steps; i++)
            {
                x += Random.Range(-0.05f, 0.05f) * w;
                points.Add(new Vector2(x, Mathf.Lerp(h * 0.55f, endY, i / (float)steps)));
            }
            var images = new List<Image>();
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i + 1 < points.Count; i++)
                {
                    Vector2 a = points[i], b = points[i + 1], d = b - a;
                    var rt = UiFactory.Rect("Seg", bolt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                    rt.anchoredPosition = (a + b) / 2f;
                    rt.sizeDelta = new Vector2(d.magnitude + 4f, pass == 0 ? 26f : 7f);
                    rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                    var img = rt.gameObject.AddComponent<Image>();
                    img.color = pass == 0 ? new Color(1f, 0.8f, 0.3f, 0.35f) : new Color(1f, 0.98f, 0.85f, 1f);
                    img.raycastTarget = false;
                    images.Add(img);
                }
            StartCoroutine(BoltAnim(bolt, images));
        }

        private IEnumerator BoltAnim(RectTransform bolt, List<Image> images)
        {
            float d = 0.26f / Pace;
            for (float e = 0f; e < d; e += Time.deltaTime)
            {
                if (bolt == null) yield break;
                float k = e / d;
                float alpha = (1f - k) * (Random.value < 0.25f ? 0.35f : 1f);   // flicker
                foreach (var img in images)
                {
                    var c = img.color; c.a = alpha * (img.rectTransform.sizeDelta.y > 20f ? 0.35f : 1f); img.color = c;
                }
                yield return null;
            }
            if (bolt != null) Destroy(bolt.gameObject);
        }

        /// <summary>The closing cut: a bright line sweeps along the band's tilt and fades.</summary>
        private void SlashLine(RectTransform parent, float w)
        {
            var rt = UiFactory.Rect("Cut", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rt.sizeDelta = new Vector2(w * 1.6f, 18f);
            rt.localRotation = Quaternion.Euler(0f, 0f, UltimateBandTilt);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = Color.white; img.raycastTarget = false;
            StartCoroutine(StreakAnim(rt, img));
        }

        private static Image Fill(Transform parent, string name, Vector2 min, Vector2 max, Color color)
        {
            var img = UiFactory.Rect(name, parent, min, max).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static void SetColorAlpha(Graphic g, float a)
        {
            if (g == null) return;
            var c = g.color; c.a = Mathf.Clamp01(a); g.color = c;
        }

        private static float EaseOut(float k) { return 1f - (1f - k) * (1f - k) * (1f - k); }
        private static float EaseIn(float k) { return k * k; }
    }
}
