using System.Collections;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// 타격감: hit-stop and a zoom toward the target on critical hits, damage numbers sized by how much of the target's HP they took,
    /// a 연격 counter for multi-hit arts, a short slow-motion when a boss falls, a red pulse while 비류연 is below 25% HP,
    /// and 「간파!」 when a charged strike is guarded or dodged. Pure presentation: no battle rule reads any of it.
    /// </summary>
    public partial class BattleScreen
    {
        private const float HitStopSeconds = 0.2f, CritZoom = 1.12f;
        private const float BossSlowScale = 0.25f, BossSlowSeconds = 0.7f;
        private const float CrisisHpPct = 0.25f;
        private static readonly int[] TierSizes = { 58, 68, 88, 112 };
        private static readonly Color[] TierColors =
        {
            new Color(.93f, .90f, .82f, 1f), new Color(1f, .98f, .92f, 1f), new Color(1f, .68f, .35f, 1f), new Color(1f, .42f, .23f, 1f)
        };
        private static readonly Color CritColor = new Color(1f, .83f, .30f, 1f);
        private static readonly Color EdgeGold = new Color(1f, .78f, .31f, 1f);
        private static readonly Color CrisisRed = new Color(.85f, .05f, .05f, 1f);

        private Image _edgeFlash, _crisis;
        private RectTransform _comboRoot;
        private CanvasGroup _comboGroup;
        private Text _comboCount, _comboSum;
        private string _comboKey;
        private int _comboHits, _comboDamage;
        private Coroutine _comboFade;
        private bool _hitStop, _timeTouched;
        private float _slowUntil;
        private static Sprite _edgeSprite, _glowSprite;

        /// <summary>Edge layers (gold flash, red crisis pulse) and the 연격 counter; called from Build right after the hit flash.</summary>
        private void BuildFeel(RectTransform root)
        {
            _crisis = UiFactory.Panel("CrisisEdge", root, Vector2.zero, Vector2.one, new Color(CrisisRed.r, CrisisRed.g, CrisisRed.b, 0f));
            _crisis.sprite = EdgeSprite();
            _crisis.raycastTarget = false;
            _crisis.gameObject.AddComponent<FullBleed>();
            _edgeFlash = UiFactory.Panel("EdgeFlash", root, Vector2.zero, Vector2.one, new Color(EdgeGold.r, EdgeGold.g, EdgeGold.b, 0f));
            _edgeFlash.sprite = EdgeSprite();
            _edgeFlash.raycastTarget = false;
            _edgeFlash.gameObject.AddComponent<FullBleed>();

            _comboRoot = UiFactory.Rect("Combo", root, new Vector2(.70f, .68f), new Vector2(FieldRight - .01f, FieldTop - .005f));
            _comboGroup = _comboRoot.gameObject.AddComponent<CanvasGroup>();
            _comboGroup.alpha = 0f;
            _comboGroup.blocksRaycasts = false;
            _comboCount = UiFactory.Label("Count", _comboRoot, new Vector2(0f, .35f), Vector2.one, "", 72, CritColor, TextAnchor.MiddleRight);
            _comboSum = UiFactory.Label("Sum", _comboRoot, Vector2.zero, new Vector2(1f, .35f), "", 30, UiFactory.PaperText, TextAnchor.MiddleRight);
            foreach (var t in new[] { _comboCount, _comboSum })
            {
                t.raycastTarget = false;
                var outline = t.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(.23f, .08f, .02f, 1f);
                outline.effectDistance = new Vector2(3f, -3f);
            }
        }

        /// <summary>Back to normal time with every feel layer hidden (a new fight, a closed screen, or coroutines that were stopped midway).</summary>
        private void ResetFeel()
        {
            _hitStop = false;
            _slowUntil = 0f;
            if (_timeTouched) { Time.timeScale = 1f; _timeTouched = false; }
            if (_edgeFlash != null) SetAlpha(_edgeFlash, 0f);
            if (_crisis != null) SetAlpha(_crisis, 0f);
            _comboKey = null;
            _comboHits = _comboDamage = 0;
            if (_comboGroup != null) _comboGroup.alpha = 0f;
            _comboFade = null;
        }

        private void OnDisable()
        {
            if (_timeTouched) { Time.timeScale = 1f; _timeTouched = false; }
        }

        private void LateUpdate()
        {
            if (_timeTouched) ApplyTimeScale();
            if (_crisis == null) return;
            float alpha = 0f;
            var hero = _panels.Find(p => p.Unit != null && p.Unit.Id == "player");
            if (hero != null && hero.Root != null && hero.Root.gameObject.activeInHierarchy
                && hero.ShownHp > 0 && hero.ShownHp < hero.Unit.Stats.MaxHp * CrisisHpPct)
                alpha = 0.38f + 0.22f * Mathf.Sin(Time.unscaledTime * 4.5f);
            SetAlpha(_crisis, alpha);
        }

        // ------------------------------------------------------------------ time

        private void ApplyTimeScale()
        {
            float scale = _hitStop ? 0f : Time.unscaledTime < _slowUntil ? BossSlowScale : 1f;
            Time.timeScale = scale;
            _timeTouched = scale != 1f;
        }

        /// <summary>Everything stops for <see cref="HitStopSeconds"/> real seconds, whatever the battle speed.</summary>
        private IEnumerator HitStop()
        {
            _hitStop = true;
            ApplyTimeScale();
            yield return new WaitForSecondsRealtime(HitStopSeconds);
            _hitStop = false;
            ApplyTimeScale();
        }

        /// <summary>The blow that fells a boss plays at quarter speed for a moment.</summary>
        private IEnumerator BossFinish()
        {
            _slowUntil = Time.unscaledTime + BossSlowSeconds;
            ApplyTimeScale();
            StartCoroutine(EdgePulse(_edgeFlash, .6f, .9f));
            SoundManager.Haptic(90);
            yield return new WaitForSecondsRealtime(BossSlowSeconds);
            ApplyTimeScale();
        }

        // ------------------------------------------------------------------ critical hit

        /// <summary>Critical hit on an enemy: zoom toward it, a white burst and gold edges, then the hit-stop; the zoom eases back afterwards.</summary>
        private IEnumerator CritImpact(EnemyCard card)
        {
            Vector2 home = HomeOf(_cardsRoot);
            Vector2 size = _cardsRoot.rect.size;
            Vector2 focus = new Vector2((card.Center.x - .5f) * size.x, (card.Center.y - .5f) * size.y);
            SetZoom(home, focus, CritZoom);
            Glow(card.Center, 520f);
            StartCoroutine(EdgePulse(_edgeFlash, .55f, .45f));
            SoundManager.Haptic(70);
            yield return HitStop();
            StartCoroutine(ZoomBack(home, focus));
        }

        private void SetZoom(Vector2 home, Vector2 focus, float scale)
        {
            // Scaling about the root's centre moves the focus point to focus x scale; shift it back so the target stays put.
            _cardsRoot.localScale = new Vector3(scale, scale, 1f);
            _cardsRoot.anchoredPosition = home + focus * (1f - scale);
        }

        private IEnumerator ZoomBack(Vector2 home, Vector2 focus)
        {
            float d = 0.26f / Pace;
            for (float e = 0f; e < d; e += Time.deltaTime)
            {
                SetZoom(home, focus, Mathf.Lerp(CritZoom, 1f, 1f - (1f - e / d) * (1f - e / d)));
                yield return null;
            }
            _cardsRoot.localScale = Vector3.one;
            _cardsRoot.anchoredPosition = home;
        }

        /// <summary>A soft white burst at the hit point (a generated radial sprite, no art needed).</summary>
        private void Glow(Vector2 at, float size)
        {
            var rt = UiFactory.Rect("Glow", _fx, at, at);
            rt.sizeDelta = new Vector2(size, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = GlowSprite();
            img.color = Color.white;
            img.raycastTarget = false;
            StartCoroutine(GlowAnim(rt, img));
        }

        private IEnumerator GlowAnim(RectTransform rt, Image img)
        {
            float d = 0.35f / Pace;
            for (float e = 0f; e < d; e += Time.deltaTime)
            {
                if (rt == null) yield break;
                float k = e / d;
                rt.localScale = Vector3.one * (0.6f + 0.8f * k);
                SetAlpha(img, 1f - k);
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        /// <summary>Screen edges light up to <paramref name="peak"/> and fade over <paramref name="seconds"/> (held while time is stopped).</summary>
        private IEnumerator EdgePulse(Image edge, float peak, float seconds)
        {
            if (edge == null) yield break;
            float d = seconds / Pace;
            for (float e = 0f; e < d; e += Time.deltaTime)
            {
                SetAlpha(edge, peak * (1f - e / d));
                yield return null;
            }
            SetAlpha(edge, 0f);
        }

        // ------------------------------------------------------------------ damage numbers

        /// <summary>0..3: under 8%, 8-20%, 20-40% and 40%+ of the target's max HP.</summary>
        private static int DamageTier(int damage, int maxHp)
        {
            float f = maxHp > 0 ? damage / (float)maxHp : 0f;
            return f < .08f ? 0 : f < .2f ? 1 : f < .4f ? 2 : 3;
        }

        /// <summary>A damage number stamped down large, then tossed up and aside; critical hits add 「치명!」 above it.</summary>
        private void DamageNumber(Vector2 at, string text, Color color, int size, bool crit)
        {
            var label = NumberLabel(at, text, color, size);
            StartCoroutine(PopUp(label.rectTransform, label, crit || size >= TierSizes[2] ? 2.3f : 1.7f, 120f, UnityEngine.Random.Range(-45f, 45f)));
            if (crit)
            {
                var tag = NumberLabel(at + new Vector2(0f, .075f), "치명!", Color.white, 46);
                tag.GetComponent<Outline>().effectColor = new Color(.85f, .38f, .05f, 1f);
                StartCoroutine(PopUp(tag.rectTransform, tag, 0.4f, 40f, 0f));
            }
        }

        private Text NumberLabel(Vector2 at, string text, Color color, int size)
        {
            var rt = UiFactory.Rect("Float", _fx, at, at);
            rt.sizeDelta = new Vector2(620f, 160f);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UiFactory.KoreanFont;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.10f, .05f, .02f, .95f);
            outline.effectDistance = new Vector2(3f, -3f);
            return t;
        }

        private IEnumerator PopUp(RectTransform rt, Text t, float startScale, float rise, float drift)
        {
            float d = 0.9f / Pace;
            Vector2 start = rt.anchoredPosition;
            Color c = t.color;
            for (float e = 0f; e < d; e += Time.deltaTime)
            {
                if (rt == null) yield break;
                float k = e / d;
                float s = k < .14f ? Mathf.Lerp(startScale, .9f, k / .14f) : k < .24f ? Mathf.Lerp(.9f, 1.05f, (k - .14f) / .1f) : Mathf.Lerp(1.05f, 1f, (k - .24f) / .76f);
                rt.localScale = new Vector3(s, s, 1f);
                float m = k < .24f ? 0f : (k - .24f) / .76f;
                rt.anchoredPosition = start + new Vector2(drift * m, rise * (1f - (1f - m) * (1f - m)));
                c.a = k < .1f ? k / .1f : k < .7f ? 1f : 1f - (k - .7f) / .3f;
                t.color = c;
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        // ------------------------------------------------------------------ 연격

        /// <summary>Counts the hits of one multi-hit art on one target; shown from the second hit.</summary>
        private void ComboHit(BattleEvent ev)
        {
            if (ev.HitCount <= 1 || _comboRoot == null) return;
            string key = ev.ActorId + "|" + ev.SkillId + "|" + ev.TargetId;
            if (key != _comboKey || ev.HitIndex == 0) { _comboKey = key; _comboHits = 0; _comboDamage = 0; }
            _comboHits++;
            _comboDamage += ev.Damage;
            if (_comboHits < 2) return;
            if (_comboFade != null) { StopCoroutine(_comboFade); _comboFade = null; }
            _comboGroup.alpha = 1f;
            _comboCount.text = _comboHits + "<size=40>연격" + (_comboHits >= 5 ? "!!" : _comboHits >= 3 ? "!" : "") + "</size>";
            _comboSum.text = "";
            StartCoroutine(ComboBump());
        }

        /// <summary>The sequence ended: show the total for a moment, then fade the counter.</summary>
        private void ComboEnd()
        {
            if (_comboKey == null) return;
            _comboKey = null;
            if (_comboHits >= 2)
            {
                _comboSum.text = "총 피해 " + _comboDamage.ToString("N0");
                _comboFade = StartCoroutine(ComboFade());
            }
            _comboHits = _comboDamage = 0;
        }

        private IEnumerator ComboBump()
        {
            var rt = _comboCount.rectTransform;
            float d = 0.22f / Pace;
            for (float e = 0f; e < d; e += Time.deltaTime)
            {
                float k = e / d;
                float s = k < .4f ? Mathf.Lerp(1.6f, .92f, k / .4f) : Mathf.Lerp(.92f, 1f, (k - .4f) / .6f);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        private IEnumerator ComboFade()
        {
            for (float e = 0f; e < 0.9f / Pace; e += Time.deltaTime) yield return null;
            float d = 0.3f / Pace;
            for (float e = 0f; e < d; e += Time.deltaTime)
            {
                _comboGroup.alpha = 1f - e / d;
                yield return null;
            }
            _comboGroup.alpha = 0f;
            _comboFade = null;
        }

        // ------------------------------------------------------------------ generated sprites

        /// <summary>Clear centre fading to opaque edges and corners, tinted by the Image colour.</summary>
        private static Sprite EdgeSprite()
        {
            if (_edgeSprite != null) return _edgeSprite;
            _edgeSprite = Radial(128, r => Mathf.Pow(Mathf.Clamp01((r - .55f) / .65f), 1.4f));
            return _edgeSprite;
        }

        /// <summary>Opaque centre fading out to a clear rim.</summary>
        private static Sprite GlowSprite()
        {
            if (_glowSprite != null) return _glowSprite;
            _glowSprite = Radial(64, r => { float k = Mathf.Clamp01(1f - r); return k * k; });
            return _glowSprite;
        }

        private static Sprite Radial(int n, System.Func<float, float> alphaAt)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + .5f) / n * 2f - 1f, v = (y + .5f) / n * 2f - 1f;
                    pixels[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alphaAt(Mathf.Sqrt(u * u + v * v)) * 255f));
                }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f));
        }
    }
}
