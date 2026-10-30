using System.Collections;
using System.Collections.Generic;
using BiRyeoDo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    public partial class BattleScreen
    {
        private enum StrikeStyle { Impact, Blade, Projectile, Footwork }

        private sealed class ImpactShake
        {
            public float Amount, Remaining;
        }

        private readonly Dictionary<RectTransform, ImpactShake> _impactShakes = new Dictionary<RectTransform, ImpactShake>();
        private float _punchPeak = 1f, _punchRemaining, _flashPeak, _flashRemaining;
        private bool _sequenceImpactPunched;
        private Vector2 _punchFocus;
        private Color _flashTint;
        private float _flashDuration;
        private int _impactGeneration;

        // Use the equipped skill's actual name/effect, including mastery variants and story commands.
        // Unknown arts retain an impact rather than acquiring invented weapon lore.
        private static StrikeStyle StyleOf(SkillSpec skill, BattleEvent ev)
        {
            if (skill != null && skill.Effect == SkillEffect.Evade) return StrikeStyle.Footwork;
            string name = skill != null ? skill.Name ?? "" : ev.SkillId ?? "";
            if (ev.Ranged || name.Contains("비도") || name.Contains("비뢰도") || name.Contains("뢰광류하곡") || (skill != null && skill.Id == "pungsin")) return StrikeStyle.Projectile;
            string description = skill != null ? skill.Description ?? "" : "";
            if (name.Contains("검") || name.Contains("도법") || name.Contains("절영") || name.Contains("은섬풍")
                || name.Contains("곤") || name.Contains("창") || description.Contains("검기")) return StrikeStyle.Blade;
            return StrikeStyle.Impact;
        }

        private void ImpactRing(Vector2 at, Color tint, bool strong)
        {
            var rt = UiFactory.Rect("ImpactWave", _fx, at, at);
            rt.sizeDelta = new Vector2(strong ? 230f : 180f, strong ? 150f : 120f);
            var ring = rt.gameObject.AddComponent<WuxiaGlyph>();
            ring.Key = "line_icon_rim"; // vector ring, never a painted inventory icon
            ring.Stroke = strong ? 4f : 3f;
            ring.color = tint;
            ring.raycastTarget = false;
            StartCoroutine(WaveAnim(rt, ring, strong ? .28f : .20f));
        }

        private IEnumerator WaveAnim(RectTransform rt, Graphic graphic, float seconds)
        {
            float duration = seconds / Pace;
            Color tint = graphic.color;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                if (rt == null) yield break;
                float k = elapsed / duration;
                float size = Mathf.Lerp(.25f, 1.35f, 1f - (1f - k) * (1f - k));
                rt.localScale = new Vector3(size, size, 1f);
                graphic.color = new Color(tint.r, tint.g, tint.b, tint.a * (1f - k));
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        private IEnumerator ProjectileFx(Vector2 from, Vector2 to, bool strong, bool enemySide = false)
        {
            // Both centers use the same normalized battlefield coordinates as the existing slash.
            var rt = UiFactory.Rect("FlyingBlade", _fx, to, to);
            Vector2 start = Vector2.Scale(from - to, _fx.rect.size);
            float angle = Mathf.Atan2(-start.y, -start.x) * Mathf.Rad2Deg;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            rt.sizeDelta = new Vector2(strong ? 130f : 100f, strong ? 7f : 4f);
            var trail = rt.gameObject.AddComponent<Image>();
            trail.color = enemySide ? new Color(1f, .45f, .35f) : strong ? UiFactory.Gold : new Color(.72f, .88f, 1f);
            trail.raycastTarget = false;
            float duration = .10f / Pace;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                if (rt == null) yield break;
                float k = elapsed / duration;
                rt.anchoredPosition = Vector2.Lerp(start, Vector2.zero, k);
                rt.localScale = new Vector3(Mathf.Lerp(.45f, 1f, k), 1f, 1f);
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        private void FootworkFx(PartyPanel panel)
        {
            var figure = PartyFigure(panel);
            if (figure == null || _fx == null) return;
            for (int i = 0; i < 3; i++)
            {
                var rt = UiFactory.Rect("FootworkEcho", _fx, panel.Center, panel.Center);
                rt.sizeDelta = new Vector2(180f, 210f);
                Graphic echo;
                if (figure is Image source && source.sprite != null)
                {
                    var img = rt.gameObject.AddComponent<Image>();
                    img.sprite = source.sprite;
                    img.preserveAspect = true;
                    echo = img;
                }
                else if (figure is Text text)
                {
                    var label = rt.gameObject.AddComponent<Text>();
                    label.font = text.font; label.text = text.text; label.fontSize = text.fontSize;
                    label.alignment = TextAnchor.MiddleCenter;
                    echo = label;
                }
                else { Destroy(rt.gameObject); continue; }
                echo.color = new Color(.56f, .83f, 1f, .25f - .05f * i);
                echo.raycastTarget = false;
                StartCoroutine(EchoAnim(rt, echo, i));
            }
        }

        private IEnumerator EchoAnim(RectTransform rt, Graphic echo, int index)
        {
            float duration = .30f / Pace;
            Color tint = echo.color;
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                if (rt == null) yield break;
                float k = elapsed / duration;
                rt.anchoredPosition = new Vector2(-(index + 1) * (22f + 24f * k), 14f + 10f * k);
                echo.color = new Color(tint.r, tint.g, tint.b, tint.a * (1f - k));
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        private void ResetImpactEffects()
        {
            _impactGeneration++;
            foreach (var entry in _impactShakes)
                if (entry.Key != null) entry.Key.anchoredPosition = HomeOf(entry.Key);
            _impactShakes.Clear();
            _punchPeak = 1f; _punchRemaining = 0f;
            _flashPeak = _flashRemaining = _flashDuration = 0f;
            _edgePeak = _edgeRemaining = _edgeDuration = 0f;
            if (_edgeFlash != null) SetAlpha(_edgeFlash, 0f);
            _sequenceImpactPunched = false;
            if (_cardsRoot != null)
            {
                _cardsRoot.localScale = Vector3.one;
                Vector2 home;
                if (_home.TryGetValue(_cardsRoot, out home)) _cardsRoot.anchoredPosition = home;
            }
            if (_flash != null) _flash.color = new Color(_flash.color.r, _flash.color.g, _flash.color.b, 0f);
            if (_fx != null)
                foreach (Transform child in _fx) Destroy(child.gameObject);
        }

        private void ResponseNotice(string enemyId, string message)
        {
            RefreshFocusHud(CardOf(enemyId));
            if (_focusDetail == null) return;
            _focusDetail.text = message;
            _focusDetail.color = UiFactory.Gold;
            _focusTag.text = "공방";
        }

        private void OnDisable()
        {
            CancelBasicMotion();
            StopAllCoroutines();
            ClearUltimateCutIn();
            ResetFeel();
            ResetImpactEffects();
        }
    }
}
