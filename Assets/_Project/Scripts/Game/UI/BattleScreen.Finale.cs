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
            yield return FinaleButton(script.IconArtId, script.IconName, script.Hint);
            if (_fled) yield break;
            if (script.TwoStep)
            {
                // 43: the 필살기 breaks out, something cuts into it, and a second button decides where it goes
                var hero = PanelOf("player");
                yield return UltimateCutIn(script.CutInName, hero != null ? Roster.StoryArtKey(hero.Unit) : "player");
                if (_fled) yield break;
                yield return FinaleInterrupt();
                yield return ShowStory(script.Title, script.InterruptLines);
                if (_fled) yield break;
                yield return FinaleButton(script.SecondIconArtId, script.SecondIconName, script.SecondHint);
            }
            _command = PlayerCommand.Guard();   // whatever is sent, the battle resolves 비류연's last action as the showdown's art
        }

        /// <summary>
        /// 20: the opened mouth. The art's icon waits <see cref="ShowdownScript.WindowSeconds"/> real seconds over the battle; pressed in time it sets
        /// <see cref="_finalePressed"/>, missed and the turn goes on with the usual commands until the next wind-up.
        /// </summary>
        private IEnumerator FinaleWindow(ShowdownScript script)
        {
            SetCommandsVisible(false);
            ClearFinaleLayer();
            _finalePressed = false;
            var layer = UiFactory.Rect("FinaleWindow", _root, Vector2.zero, Vector2.one);
            layer.SetAsLastSibling();
            _finaleLayer = layer;
            Fill(layer, "Dim", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.35f));
            var iconRect = UiFactory.Rect("Icon", layer, new Vector2(.42f, .36f), new Vector2(.58f, .66f));
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = ArtLibrary.Art(script.IconArtId) ?? ArtLibrary.Command(script.IconArtId) ?? ArtLibrary.Icon("ultimate_slot");
            icon.preserveAspect = true;
            icon.color = icon.sprite != null ? Color.white : UiFactory.Gold;
            var button = iconRect.gameObject.AddComponent<Button>();
            button.targetGraphic = icon;
            button.onClick.AddListener(() => { _finalePressed = true; SoundManager.Sfx("ui_click"); });
            UiFactory.Label("Hint", layer, new Vector2(.2f, .68f), new Vector2(.8f, .78f), script.Hint, 64, UiFactory.Red);
            var count = UiFactory.Label("Count", layer, new Vector2(.3f, .26f), new Vector2(.7f, .34f), "", 48, UiFactory.PaperText);
            SoundManager.Sfx("charge");
            for (float t = 0f; t < script.WindowSeconds && !_finalePressed && !_fled; t += Time.unscaledDeltaTime)
            {
                count.text = script.IconName + "  " + Mathf.CeilToInt(script.WindowSeconds - t) + "초";
                float pulse = 1f + 0.08f * Mathf.Sin(t * 10f);
                if (iconRect != null) iconRect.localScale = new Vector3(pulse, pulse, 1f);
                yield return null;
            }
            ClearFinaleLayer();
            if (!_finalePressed && !_fled)
            {
                _log.text = "입이 닫혔다…….";
                SetCommandsVisible(true);
                RefreshCommands();
            }
        }

        private int _duelResult;   // -1 early, 0 late, 1 in time

        /// <summary>
        /// 24 동전 대결: the scene, then the coin is thrown until 비류연 presses in time. An early press throws it again, a late one lets
        /// <see cref="InteractiveBattle.DuelMiss"/> land the opponent's blade (the last chance lost ends the fight); each miss widens the window.
        /// Leaves the command that the battle resolves as the duel's art (a single white flash instead of the 필살기 cut-in).
        /// </summary>
        private IEnumerator FinaleDuel(ShowdownScript script)
        {
            SetCommandsVisible(false);
            yield return ShowStory(script.Title, script.AfterBlow);
            while (!_fled && !_battle.IsOver)
            {
                yield return DuelCoin(script, script.DuelWindowSeconds + script.DuelWindowStep * _battle.DuelMisses);
                if (_fled) yield break;
                if (_duelResult > 0) { yield return DuelFlash(); _command = PlayerCommand.Guard(); yield break; }
                if (_duelResult < 0) { yield return DuelNotice(script.EarlyText, UiFactory.PaperText); continue; }
                yield return DuelNotice(script.LateText, UiFactory.Red);
                yield return PlayEvents(_battle.DuelMiss());
                RefreshParty();
            }
        }

        /// <summary>One throw: the coin rises and falls for a random time; pressing before it lands is early, within the window after it is in time.</summary>
        private IEnumerator DuelCoin(ShowdownScript script, float window)
        {
            ClearFinaleLayer();
            _finalePressed = false;
            _duelResult = 0;
            var layer = UiFactory.Rect("FinaleDuel", _root, Vector2.zero, Vector2.one);
            layer.SetAsLastSibling();
            _finaleLayer = layer;
            Fill(layer, "Dim", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.6f));
            Fill(layer, "Ground", new Vector2(.30f, .395f), new Vector2(.70f, .40f), new Color(1f, 1f, 1f, .35f));
            var coin = Fill(layer, "Coin", new Vector2(.49f, .40f), new Vector2(.51f, .435f), UiFactory.Gold).rectTransform;
            var iconRect = UiFactory.Rect("Icon", layer, new Vector2(.44f, .10f), new Vector2(.56f, .32f));
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = ArtLibrary.Art(script.IconArtId) ?? ArtLibrary.Icon("ultimate_slot");
            icon.preserveAspect = true;
            icon.color = icon.sprite != null ? Color.white : UiFactory.Gold;
            var button = iconRect.gameObject.AddComponent<Button>();
            button.targetGraphic = icon;
            button.onClick.AddListener(() => { _finalePressed = true; SoundManager.Sfx("ui_click"); });
            UiFactory.Label("Name", layer, new Vector2(.3f, .32f), new Vector2(.7f, .38f), script.IconName, 44, UiFactory.Gold);
            UiFactory.Label("Hint", layer, new Vector2(.2f, .80f), new Vector2(.8f, .88f), script.Hint, 40, UiFactory.PaperText);
            UiFactory.Label("Chances", layer, new Vector2(.2f, .74f), new Vector2(.8f, .80f), _battle.TrialStatus, 34, UiFactory.PaperText);

            // the coin goes up for a fixed short while, then falls for the rest of the random time
            float total = UnityEngine.Random.Range(script.CoinMinSeconds, script.CoinMaxSeconds), rise = 0.35f;
            SoundManager.Sfx("ui_click");   // 팅!
            for (float t = 0f; t < total && !_fled; t += Time.unscaledDeltaTime)
            {
                if (_finalePressed) { _duelResult = -1; ClearFinaleLayer(); yield break; }
                float h = t < rise ? Mathf.Sin(t / rise * Mathf.PI * 0.5f) : 1f - Mathf.Pow((t - rise) / (total - rise), 2f);
                if (coin != null)
                {
                    float y = Mathf.Lerp(.40f, .70f, h);
                    coin.anchorMin = new Vector2(.49f, y);
                    coin.anchorMax = new Vector2(.51f, y + .035f);
                    coin.localScale = new Vector3(Mathf.Abs(Mathf.Cos(t * 18f)) * 0.8f + 0.2f, 1f, 1f);   // spinning
                }
                yield return null;
            }
            if (_fled) yield break;
            if (coin != null) { coin.anchorMin = new Vector2(.49f, .40f); coin.anchorMax = new Vector2(.51f, .435f); coin.localScale = Vector3.one; }
            SoundManager.Sfx("hit");   // 탕!
            SoundManager.Haptic(30);
            var flash = Fill(layer, "Land", new Vector2(.40f, .37f), new Vector2(.60f, .47f), new Color(1f, 1f, 1f, .5f));
            for (float t = 0f; t < window && !_fled; t += Time.unscaledDeltaTime)
            {
                if (_finalePressed) { _duelResult = 1; break; }
                if (flash != null) SetAlpha(flash, Mathf.Lerp(.5f, 0f, t / window));
                yield return null;
            }
            ClearFinaleLayer();
        }

        /// <summary>A short line in the middle of the screen (「아직!」, 「늦었다!」).</summary>
        private IEnumerator DuelNotice(string text, Color color)
        {
            if (string.IsNullOrEmpty(text)) yield break;
            _log.text = text;
            var label = UiFactory.Label("DuelNotice", _root, new Vector2(.2f, .45f), new Vector2(.8f, .6f), text, 96, color);
            label.transform.SetAsLastSibling();
            for (float t = 0f; t < 0.7f && !_fled; t += Time.unscaledDeltaTime) yield return null;
            if (label != null) Destroy(label.gameObject);
        }

        /// <summary>섬뢰: one white line across the screen and a beat of silence, faster than any cut-in.</summary>
        private IEnumerator DuelFlash()
        {
            SoundManager.Sfx("ultimate");
            SoundManager.Haptic(80);
            var line = Fill(_root, "DuelFlash", new Vector2(0f, .49f), new Vector2(1f, .51f), Color.white);
            line.transform.SetAsLastSibling();
            var white = Fill(_root, "DuelWhite", Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, .7f));
            white.transform.SetAsLastSibling();
            for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
            {
                if (white != null) SetAlpha(white, Mathf.Lerp(.7f, 0f, t / 0.25f));
                yield return null;
            }
            if (white != null) Destroy(white.gameObject);
            for (float t = 0f; t < 0.6f; t += Time.unscaledDeltaTime) yield return null;   // the silence before anyone understands
            if (line != null) Destroy(line.gameObject);
        }

        /// <summary>A red flash and a shake: the moment something cuts into the 필살기.</summary>
        private IEnumerator FinaleInterrupt()
        {
            SoundManager.Sfx("charge");
            SoundManager.Haptic(120);
            var flash = Fill(_root, "FinaleInterrupt", Vector2.zero, Vector2.one, new Color(0.75f, 0.06f, 0.04f, 0.55f));
            flash.transform.SetAsLastSibling();
            StartCoroutine(Shake(_root, 22f));
            for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
            {
                if (flash == null) yield break;
                SetAlpha(flash, Mathf.Lerp(0.55f, 0f, t / 0.5f));
                yield return null;
            }
            if (flash != null) Destroy(flash.gameObject);
        }

        private IEnumerator FinaleButton(string iconArtId, string iconName, string hint)
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
            icon.sprite = ArtLibrary.Art(iconArtId) ?? ArtLibrary.Command(iconArtId) ?? ArtLibrary.Icon("ultimate_slot");
            icon.preserveAspect = true;
            icon.color = icon.sprite != null ? Color.white : UiFactory.Gold;
            var button = iconRect.gameObject.AddComponent<Button>();
            button.targetGraphic = icon;
            button.onClick.AddListener(() => { _finalePressed = true; SoundManager.Sfx("ui_click"); });

            UiFactory.Label("Name", layer, new Vector2(.2f, .74f), new Vector2(.8f, .82f), iconName, 72, UiFactory.Gold);
            UiFactory.Label("Hint", layer, new Vector2(.2f, .25f), new Vector2(.8f, .32f), hint, 40, UiFactory.PaperText);

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
