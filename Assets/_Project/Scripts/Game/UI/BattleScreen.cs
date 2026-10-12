using System.Collections;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Battle;
using BiRyeoDo.Game.Data;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// 전투 탭. Builds its own uGUI under a parent rect and runs the endless auto-battle loop:
    /// normal enemy → fight → rewards → repeat; after enough kills the 보스 도전 button appears.
    /// Layout mirrors the reference: header (제N생, 두), stage title + story, enemy HP, enemy art, player block.
    /// </summary>
    [RequireComponent(typeof(BattleController))]
    public class BattleScreen : MonoBehaviour
    {
        private BattleController _controller;
        private StageDef _stage;
        private StageProgress _progress;
        private bool _challengeBoss;
        private bool _built;

        private Image _background;
        private Text _lifeLabel, _dooLabel, _stageTitle, _story, _enemyName, _enemyPct, _log, _playerInfo, _playerHpText, _killsLabel;
        private UiFactory.Bar _enemyBar, _playerBar;
        private Image _enemyImage;
        private RectTransform _enemyRect, _floatLayer;
        private GameObject _bossButton;
        private Text _bossButtonLabel;

        private void Awake()
        {
            _controller = GetComponent<BattleController>();
        }

        /// <summary>Creates the widgets under <paramref name="root"/> (a full-stretch panel) and starts the loop.</summary>
        public void Build(RectTransform root)
        {
            if (_built) return;
            _built = true;

            _background = UiFactory.Panel("Background", root, Vector2.zero, Vector2.one, UiFactory.Parchment);

            // Header: portrait + 제N생 (left), 두 (right)
            UiFactory.Panel("PortraitFrame", root, new Vector2(0.03f, 0.905f), new Vector2(0.19f, 0.985f), UiFactory.Ink);
            UiFactory.Label("PortraitText", root, new Vector2(0.03f, 0.905f), new Vector2(0.19f, 0.985f), "협객", 36, UiFactory.PaperText);
            UiFactory.Panel("LifeBand", root, new Vector2(0.20f, 0.915f), new Vector2(0.50f, 0.975f), UiFactory.Ink);
            _lifeLabel = UiFactory.Label("Life", root, new Vector2(0.21f, 0.915f), new Vector2(0.49f, 0.975f), "", 40, UiFactory.Gold);
            UiFactory.Panel("DooBand", root, new Vector2(0.58f, 0.925f), new Vector2(0.97f, 0.975f), UiFactory.Ink);
            _dooLabel = UiFactory.Label("Doo", root, new Vector2(0.60f, 0.925f), new Vector2(0.96f, 0.975f), "", 40, UiFactory.Gold, TextAnchor.MiddleRight);

            // Stage title + story
            UiFactory.Panel("TitleBand", root, new Vector2(0.08f, 0.835f), new Vector2(0.92f, 0.900f), UiFactory.Ink);
            _stageTitle = UiFactory.Label("StageTitle", root, new Vector2(0.10f, 0.835f), new Vector2(0.90f, 0.900f), "", 50, UiFactory.PaperText);
            UiFactory.Panel("StoryBand", root, new Vector2(0.08f, 0.745f), new Vector2(0.92f, 0.830f), UiFactory.InkSoft);
            _story = UiFactory.Label("Story", root, new Vector2(0.10f, 0.748f), new Vector2(0.90f, 0.828f), "", 32, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);

            // Enemy HP row (+ boss challenge on the right)
            UiFactory.Panel("EnemyNameBand", root, new Vector2(0.06f, 0.675f), new Vector2(0.66f, 0.735f), UiFactory.Ink);
            _enemyName = UiFactory.Label("EnemyName", root, new Vector2(0.08f, 0.675f), new Vector2(0.50f, 0.735f), "", 40, UiFactory.PaperText, TextAnchor.MiddleLeft);
            _enemyPct = UiFactory.Label("EnemyPct", root, new Vector2(0.50f, 0.675f), new Vector2(0.64f, 0.735f), "", 36, UiFactory.Red, TextAnchor.MiddleRight);
            _enemyBar = UiFactory.MakeBar("EnemyHp", root, new Vector2(0.06f, 0.660f), new Vector2(0.66f, 0.675f), new Color(0.2f, 0.2f, 0.2f, 0.6f), UiFactory.Red);
            _killsLabel = UiFactory.Label("Kills", root, new Vector2(0.06f, 0.628f), new Vector2(0.66f, 0.658f), "", 28, UiFactory.Ink, TextAnchor.MiddleLeft);

            var boss = UiFactory.Button("BossButton", root, new Vector2(0.70f, 0.640f), new Vector2(0.96f, 0.735f), "보스 도전", UiFactory.Red, OnBossPressed, 38);
            _bossButton = boss.gameObject;
            _bossButtonLabel = boss.GetComponentInChildren<Text>();

            // Enemy art + floating damage layer
            var enemyArea = UiFactory.Rect("EnemyArea", root, new Vector2(0.18f, 0.32f), new Vector2(0.82f, 0.625f));
            _enemyRect = enemyArea;
            _enemyImage = enemyArea.gameObject.AddComponent<Image>();
            _enemyImage.color = new Color(0.25f, 0.25f, 0.23f, 0.55f);
            _enemyImage.raycastTarget = false;
            _enemyImage.preserveAspect = true;
            _floatLayer = UiFactory.Rect("FloatLayer", root, new Vector2(0f, 0.25f), new Vector2(1f, 0.70f));

            // Battle log
            _log = UiFactory.Label("Log", root, new Vector2(0.06f, 0.285f), new Vector2(0.94f, 0.318f), "", 30, UiFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);

            // Player block
            UiFactory.Panel("PlayerFrame", root, new Vector2(0.05f, 0.03f), new Vector2(0.45f, 0.27f), UiFactory.Ink);
            UiFactory.Label("PlayerPortrait", root, new Vector2(0.05f, 0.03f), new Vector2(0.45f, 0.27f), "협객", 60, UiFactory.PaperText);
            _playerInfo = UiFactory.Label("PlayerInfo", root, new Vector2(0.48f, 0.15f), new Vector2(0.96f, 0.27f), "", 38, UiFactory.Ink, TextAnchor.MiddleLeft);
            _playerBar = UiFactory.MakeBar("PlayerHp", root, new Vector2(0.48f, 0.115f), new Vector2(0.96f, 0.135f), new Color(0.2f, 0.2f, 0.2f, 0.6f), new Color(0.25f, 0.55f, 0.30f, 1f));
            _playerHpText = UiFactory.Label("PlayerHpText", root, new Vector2(0.48f, 0.06f), new Vector2(0.96f, 0.11f), "", 32, UiFactory.Ink, TextAnchor.MiddleLeft);

            LoadStage(GameManager.Instance.Save.Stage);
            Refresh();
            StartCoroutine(Loop());
        }

        private void LoadStage(int number)
        {
            _stage = ContentDatabase.GetStageAtOrAfter(number);
            if (_stage == null)
            {
                _stageTitle.text = "스테이지 데이터 없음";
                _story.text = "메뉴 BiRyeoDo ▸ Setup Project 를 실행하세요.";
                return;
            }
            GameManager.Instance.Save.Stage = _stage.number;
            _progress = new StageProgress(_stage.killsToBoss);
            _challengeBoss = false;
            _stageTitle.text = "스테이지 " + _stage.number + " · " + _stage.displayName;
            _story.text = _stage.storyText;
            if (_stage.background != null)
            {
                _background.sprite = _stage.background;
                _background.color = Color.white;
            }
        }

        private void Refresh()
        {
            var save = GameManager.Instance.Save;
            _lifeLabel.text = "제" + save.Life + "생  Lv." + save.Level;
            _dooLabel.text = save.Doo.ToString("N0") + " 두";

            var st = Progression.StatsFor(save.Level, save.Life);
            string art = ArtName();
            _playerInfo.text = art + "\n속도 " + st.Speed + " · 치명타 " + (st.CritRate * 100f).ToString("0.0") + "%";

            if (_progress != null)
            {
                _killsLabel.text = "처치 " + _progress.Kills + " / " + _progress.KillsToBoss;
                _bossButton.SetActive(_progress.BossReady && !_challengeBoss);
            }
        }

        private string ArtName()
        {
            var ids = GameManager.Instance.Save.EquippedArts;
            MartialArtDef best = null;
            foreach (var id in ids)
            {
                var a = ContentDatabase.GetArt(id);
                if (a != null && (best == null || a.power > best.power)) best = a;
            }
            return best != null ? best.displayName : "맨손";
        }

        private void OnBossPressed()
        {
            if (_progress == null || !_progress.BossReady || _stage == null || _stage.boss == null) return;
            _challengeBoss = true;
            _bossButton.SetActive(false);
            _log.text = "보스 도전 준비! 다음 전투에서 " + _stage.boss.displayName + "와(과) 맞붙는다.";
        }

        private EnemyDef PickEnemy()
        {
            if (_challengeBoss && _stage.boss != null) return _stage.boss;
            if (_stage.enemies == null || _stage.enemies.Length == 0) return _stage.boss;
            return _stage.enemies[Random.Range(0, _stage.enemies.Length)];
        }

        private IEnumerator Loop()
        {
            while (true)
            {
                if (_stage == null) { yield return new WaitForSeconds(1f); continue; }

                var enemy = PickEnemy();
                if (enemy == null) { _log.text = "적 데이터가 없습니다."; yield return new WaitForSeconds(1f); continue; }

                var arts = new List<MartialArtDef>();
                foreach (var id in GameManager.Instance.Save.EquippedArts)
                {
                    var a = ContentDatabase.GetArt(id);
                    if (a != null) arts.Add(a);
                }

                ShowEnemy(enemy);
                BattleResult result = null;
                _controller.EventPlayed = OnEvent;
                _controller.BattleFinished = r => result = r;
                _controller.StartBattle(enemy, arts);
                _playerBar.SetPct(1f);
                _playerHpText.text = _controller.Player.Stats.MaxHp + " / " + _controller.Player.Stats.MaxHp;

                while (result == null) yield return null;

                bool wasBoss = enemy.isBoss && _challengeBoss;
                if (result.PlayerWon)
                {
                    _log.text = enemy.displayName + " 처치!  +" + enemy.dooReward + "두  +" + enemy.expReward + " 경험";
                    if (wasBoss) OnBossCleared(); else _progress.OnNormalWin();
                }
                else
                {
                    _log.text = "패배... 다시 수련하고 도전한다.";
                    _challengeBoss = false;
                }
                Refresh();
                yield return new WaitForSeconds(wasBoss && result.PlayerWon ? 1.5f : 0.9f);
            }
        }

        private void OnBossCleared()
        {
            var save = GameManager.Instance.Save;
            var next = ContentDatabase.GetStageAtOrAfter(_stage.number + 1);
            if (next == null || next.number <= _stage.number)
            {
                _log.text = "마지막 스테이지를 돌파했다! (다음 스테이지는 추후 추가)";
                _challengeBoss = false;
                _progress.Reset(_stage.killsToBoss);
                return;
            }
            save.Stage = next.number;
            GameManager.Instance.SaveNow();
            LoadStage(next.number);
        }

        private void ShowEnemy(EnemyDef enemy)
        {
            _enemyName.text = (enemy.isBoss ? "[보스] " : "") + enemy.displayName;
            _enemyBar.SetPct(1f);
            _enemyPct.text = "100%";
            if (enemy.portrait != null)
            {
                _enemyImage.sprite = enemy.portrait;
                _enemyImage.color = Color.white;
            }
            else
            {
                _enemyImage.sprite = null;
                _enemyImage.color = enemy.isBoss ? new Color(0.45f, 0.12f, 0.10f, 0.6f) : new Color(0.25f, 0.25f, 0.23f, 0.55f);
            }
        }

        private void OnEvent(BattleEvent ev)
        {
            bool onPlayer = ev.TargetId == _controller.Player.Id;
            var targetMax = onPlayer ? _controller.Player.Stats.MaxHp : _controller.Enemy.Stats.MaxHp;
            float pct = targetMax > 0 ? (float)ev.TargetHpAfter / targetMax : 0f;

            var actor = ev.ActorId == _controller.Player.Id ? _controller.Player : _controller.Enemy;
            _log.text = actor.Name + "의 " + _controller.SkillName(actor, ev.SkillId) + (ev.IsCrit ? " (치명타!)" : "");

            if (onPlayer)
            {
                _playerBar.SetPct(pct);
                _playerHpText.text = ev.TargetHpAfter + " / " + targetMax;
                StartCoroutine(Shake(_floatLayer, 12f));
                SpawnFloat("-" + ev.Damage, new Vector2(-250f, -330f), false);
            }
            else
            {
                _enemyBar.SetPct(pct);
                _enemyPct.text = Mathf.CeilToInt(pct * 100f) + "%";
                StartCoroutine(Shake(_enemyRect, 16f));
                SpawnFloat("-" + ev.Damage + (ev.IsCrit ? "!" : ""), Vector2.zero, ev.IsCrit);
            }
        }

        private void SpawnFloat(string text, Vector2 offset, bool crit)
        {
            var rt = UiFactory.Rect("Dmg", _floatLayer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            rt.sizeDelta = new Vector2(420f, 120f);
            rt.anchoredPosition = offset + new Vector2(Random.Range(-60f, 60f), 40f);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UiFactory.KoreanFont;
            t.text = text;
            t.fontSize = crit ? 96 : 72;
            t.fontStyle = FontStyle.Bold;
            t.color = crit ? UiFactory.Gold : UiFactory.Red;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            StartCoroutine(FloatUp(rt, t));
        }

        private IEnumerator FloatUp(RectTransform rt, Text t)
        {
            const float duration = 0.8f;
            Vector2 start = rt.anchoredPosition;
            for (float e = 0f; e < duration; e += Time.deltaTime)
            {
                float k = e / duration;
                rt.anchoredPosition = start + new Vector2(0f, 140f * k);
                var c = t.color; c.a = 1f - k * k; t.color = c;
                yield return null;
            }
            Destroy(rt.gameObject);
        }

        private IEnumerator Shake(RectTransform rt, float amount)
        {
            Vector2 origin = rt.anchoredPosition;
            for (int i = 0; i < 6; i++)
            {
                rt.anchoredPosition = origin + new Vector2(Random.Range(-amount, amount), Random.Range(-amount, amount) * 0.5f);
                yield return new WaitForSeconds(0.025f);
            }
            rt.anchoredPosition = origin;
        }
    }
}
