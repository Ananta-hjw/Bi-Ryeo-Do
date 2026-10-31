using System.Collections;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// Scripted showdowns (TrialKind.Finale, ShowdownScript): after the opening the scene plays, a large icon waits in the middle of the screen,
    /// and pressing it plays the 필살기 cut-in of the last art (일반 30 풍신, 46 묵뢰살). The rules live in InteractiveBattle.
    /// </summary>
    public partial class BattleScreen
    {
        private RectTransform _finaleLayer;
        private bool _finalePressed;

        /// <summary>The scene after the opening, then the art's button; leaves the command the battle turns into that art.</summary>
        private IEnumerator FinaleFlow(ShowdownScript script)
        {
            SetCommandsVisible(false);
            yield return ShowStory(script.Title, script.AfterBlow);
            if (_fled) yield break;
            yield return FinaleButton(script);
            _command = PlayerCommand.Guard();   // whatever is sent, the battle resolves 비류연's last action as the showdown's art
        }

        private IEnumerator FinaleButton(ShowdownScript script)
        {
            ClearFinaleLayer();
            _finalePressed = false;
            var layer = UiFactory.Rect("FinaleButton", _root, Vector2.zero, Vector2.one);
            layer.SetAsLastSibling();
            _finaleLayer = layer;
            Fill(layer, "Dim", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.55f));

            var halo = Fill(layer, "Halo", new Vector2(.38f, .30f), new Vector2(.62f, .74f), new Color(1f, .78f, .3f, .22f));
            var iconRect = UiFactory.Rect("Icon", layer, new Vector2(.40f, .34f), new Vector2(.60f, .70f));
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = ArtLibrary.Art(script.IconArtId) ?? ArtLibrary.Command(script.IconArtId) ?? ArtLibrary.Icon("ultimate_slot");
            icon.preserveAspect = true;
            icon.color = icon.sprite != null ? Color.white : UiFactory.Gold;
            var button = iconRect.gameObject.AddComponent<Button>();
            button.targetGraphic = icon;
            button.onClick.AddListener(() => { _finalePressed = true; SoundManager.Sfx("ui_click"); });

            UiFactory.Label("Name", layer, new Vector2(.2f, .74f), new Vector2(.8f, .82f), script.IconName, 72, UiFactory.Gold);
            UiFactory.Label("Hint", layer, new Vector2(.2f, .25f), new Vector2(.8f, .32f), script.Hint, 40, UiFactory.PaperText);

            // the icon breathes until it is pressed (real time, so the speed setting and hit-stop never freeze it)
            float t = 0f;
            while (!_finalePressed && !_fled)
            {
                t += Time.unscaledDeltaTime;
                float pulse = 1f + 0.06f * Mathf.Sin(t * 4f);
                if (iconRect != null) iconRect.localScale = new Vector3(pulse, pulse, 1f);
                if (halo != null) SetAlpha(halo, 0.16f + 0.12f * (0.5f + 0.5f * Mathf.Sin(t * 4f)));
                yield return null;
            }
            ClearFinaleLayer();
        }

        private void ClearFinaleLayer()
        {
            if (_finaleLayer != null) Destroy(_finaleLayer.gameObject);
            _finaleLayer = null;
        }

        /// <summary>After the strike: 비류연 sinks down if the script says so, then the closing lines.</summary>
        private IEnumerator FinaleCollapse(ShowdownScript script)
        {
            var hero = script.HeroCollapses ? PanelOf("player") : null;
            if (hero != null && hero.Art != null)
            {
                var rect = hero.Art.rectTransform;
                Vector2 origin = rect.anchoredPosition;
                float d = 0.6f / Pace;
                for (float e = 0f; e < d; e += Time.deltaTime)
                {
                    if (rect == null) break;
                    float k = e / d;
                    rect.anchoredPosition = origin + new Vector2(0f, -36f * k);
                    SetAlpha(hero.Art, Mathf.Lerp(1f, 0.4f, k));
                    yield return null;
                }
                if (rect != null) rect.anchoredPosition = origin + new Vector2(0f, -36f);
                SetAlpha(hero.Art, 0.4f);
                hero.Name.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }
            if (script.AfterStrike != null && script.AfterStrike.Length > 0) yield return ShowStory(script.Title, script.AfterStrike);
        }
    }
}
