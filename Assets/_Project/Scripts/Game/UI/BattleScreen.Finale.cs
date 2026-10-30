using System.Collections;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// 일반 30 「화룡멸겁대진」 (FireDragonFinale): after the 진's blow the four masters lend 비류연 their 내력, a large 풍신 icon waits in the
    /// middle of the screen, and pressing it plays the 필살기 cut-in of 「풍뢰의 장 · 나선의 인 · 용권승룡」. The rules live in InteractiveBattle (TrialKind.Finale).
    /// </summary>
    public partial class BattleScreen
    {
        private RectTransform _finaleLayer;
        private bool _finalePressed;

        /// <summary>The scene after the blow, then the 풍신 button; leaves the command the battle turns into the 풍신.</summary>
        private IEnumerator FinaleFlow()
        {
            SetCommandsVisible(false);
            yield return ShowStory("화룡멸겁대진", FireDragonFinale.AfterBlow);
            if (_fled) yield break;
            yield return FinaleButton();
            _command = PlayerCommand.Guard();   // whatever is sent, the battle resolves 비류연's last action as the 풍신
        }

        private IEnumerator FinaleButton()
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
            icon.sprite = ArtLibrary.Art("pungsin") ?? ArtLibrary.Command("pungsin") ?? ArtLibrary.Icon("ultimate_slot");
            icon.preserveAspect = true;
            icon.color = icon.sprite != null ? Color.white : UiFactory.Gold;
            var button = iconRect.gameObject.AddComponent<Button>();
            button.targetGraphic = icon;
            button.onClick.AddListener(() => { _finalePressed = true; SoundManager.Sfx("ui_click"); });

            UiFactory.Label("Name", layer, new Vector2(.2f, .74f), new Vector2(.8f, .82f), "풍신(風神)", 72, UiFactory.Gold);
            UiFactory.Label("Hint", layer, new Vector2(.2f, .25f), new Vector2(.8f, .32f), "풍신 아이콘을 눌러 펼치세요", 40, UiFactory.PaperText);

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

        /// <summary>After the 진 is gone: 비류연 sinks down, then his last line.</summary>
        private IEnumerator FinaleCollapse()
        {
            var hero = PanelOf("player");
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
            yield return ShowStory("화룡멸겁대진", FireDragonFinale.AfterStrike);
        }
    }
}
