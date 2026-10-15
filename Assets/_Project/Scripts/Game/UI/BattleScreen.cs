using System;
using System.Collections;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// Full-screen, player-driven battle. A stage is a story intro, a chain of fights and an outro.
    /// In each fight every living party member gets a turn in which the player picks a command (attack, one of that member's
    /// martial arts, guard, meditate) and taps an enemy to choose the target. Enemies telegraph their heavy moves and who
    /// they will hit, so guarding, evading, healing or interrupting at the right time matters.
    /// The rules live in <see cref="InteractiveBattle"/>; this class only draws and animates what it reports.
    /// </summary>
    public class BattleScreen : MonoBehaviour
    {
        private const float StepDelay = 0.42f, HitDelay = 0.16f, SmallDelay = 0.30f;
        private static readonly Color Backdrop = new Color(0.13f, 0.12f, 0.11f, 1f);
        private static readonly Color QiBlue = new Color(0.30f, 0.50f, 0.80f, 1f);
        private static readonly Color HpGreen = new Color(0.30f, 0.62f, 0.34f, 1f);
        private static readonly Color GuardBlue = new Color(0.5f, 0.7f, 0.95f, 1f);
        private static readonly Color Disabled = new Color(0.25f, 0.25f, 0.24f, 1f);

        private sealed class EnemyCard
        {
            public Combatant Unit;
            public RectTransform Root;
            public Image Frame, Body, Art;
            public Text Name, Glyph, Intent, HpText;
            public UiFactory.Bar Hp;
            public Vector2 Center;
        }

        private sealed class PartyPanel
        {
            public Combatant Unit;
            public RectTransform Root;
            public Image Frame, Art;
            public Text Name, Status, HpText, QiText, SpiritText;
            public UiFactory.Bar Hp, Qi, Spirit;
            public Vector2 Center;
        }

        private sealed class CommandButton
        {
            public Button Button;
            public Image Image;
            public Text Label;
            public GameObject Root;
        }

        private RectTransform _cardsRoot, _partyRoot, _fx;
        private Image _backdropImage;
        private Text _stageLabel, _speedLabel, _log, _banner;
        private Image _flash, _bannerBg;
        private readonly List<Image> _turnChips = new List<Image>();
        private readonly List<Text> _turnLabels = new List<Text>();
        private readonly List<EnemyCard> _cards = new List<EnemyCard>();
        private readonly List<PartyPanel> _panels = new List<PartyPanel>();
        private readonly CommandButton[] _commands = new CommandButton[9];

        // story / result panels
        private GameObject _panel;
        private Text _panelTitle, _panelText;
        private GameObject _panelNext, _panelRetry, _panelBack;
        private readonly Image[] _portrait = new Image[2];
        private readonly Text[] _portraitGlyph = new Text[2];
        private Text _nameTag;
        private RectTransform _panelTextRect;
        // Visual-novel style story layer (full screen): tap anywhere to advance.
        private GameObject _storyRoot;
        private Text _storyTitle, _storyText;
        private RectTransform _namePlate;
        private bool _storySkip;

        private InteractiveBattle _battle;
        private List<Combatant> _party = new List<Combatant>();
        private EnemyCard _target;

        private bool _hasCommand, _panelDone, _retry, _leave, _fled, _itemMode, _jointMode, _drawn;
        private readonly List<Combatant> _jointPartners = new List<Combatant>();
        private string _jointName = "합공";
        private PlayerCommand _command;
        private int _speed = 1;
        private Action _onClose;
        private bool _built;

        public void Build(RectTransform root)
        {
            if (_built) return;
            _built = true;

            UiFactory.Panel("Backdrop", root, Vector2.zero, Vector2.one, Backdrop);
            _backdropImage = UiFactory.Panel("StageArt", root, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0f));
            _backdropImage.preserveAspect = false;
            UiFactory.Panel("Dim", root, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.35f)); // keeps text readable over artwork

            // Top bar
            UiFactory.Panel("TopBar", root, new Vector2(0f, 0.945f), new Vector2(1f, 1f), UiFactory.Ink);
            UiFactory.Panel("TopBarLine", root, new Vector2(0f, 0.944f), new Vector2(1f, 0.9465f), UiFactory.GoldDim);
            _stageLabel = UiFactory.Label("Stage", root, new Vector2(0.04f, 0.947f), new Vector2(0.60f, 0.998f), "", 34, UiFactory.PaperText, TextAnchor.MiddleLeft);
            var speed = UiFactory.Button("Speed", root, new Vector2(0.62f, 0.953f), new Vector2(0.80f, 0.992f), "속도 x1", UiFactory.InkSoft, OnSpeedPressed, 28);
            _speedLabel = speed.GetComponentInChildren<Text>();
            UiFactory.Button("Flee", root, new Vector2(0.82f, 0.953f), new Vector2(0.97f, 0.992f), "포기", UiFactory.InkSoft, OnFleePressed, 28);

            // Turn order
            UiFactory.Label("TurnLabel", root, new Vector2(0.02f, 0.905f), new Vector2(0.14f, 0.940f), "순서", 24, UiFactory.Gold);
            for (int i = 0; i < 7; i++)
            {
                float x0 = 0.15f + i * 0.123f;
                var chip = UiFactory.Panel("Chip" + i, root, new Vector2(x0, 0.905f), new Vector2(x0 + 0.115f, 0.940f), UiFactory.InkSoft);
                var label = UiFactory.Label("ChipText", root, new Vector2(x0, 0.905f), new Vector2(x0 + 0.115f, 0.940f), "", 22, UiFactory.PaperText);
                _turnChips.Add(chip);
                _turnLabels.Add(label);
            }

            _cardsRoot = UiFactory.Rect("Cards", root, Vector2.zero, Vector2.one);

            // Ground strip under the two formations (side-view field), then the log line below it
            UiFactory.Panel("Ground", root, new Vector2(0f, 0.335f), new Vector2(1f, 0.46f), new Color(0f, 0f, 0f, 0.28f));
            UiFactory.Panel("GroundLine", root, new Vector2(0f, 0.458f), new Vector2(1f, 0.462f), UiFactory.GoldDim);
            UiFactory.Frame(UiFactory.Panel("LogBand", root, new Vector2(0.04f, 0.292f), new Vector2(0.96f, 0.334f), UiFactory.Ink).transform, UiFactory.GoldDim, 2f);
            _log = UiFactory.Label("Log", root, new Vector2(0.06f, 0.292f), new Vector2(0.94f, 0.334f), "", 28, UiFactory.PaperText);

            // Party panels are created per stage (their count depends on who has joined)
            _partyRoot = UiFactory.Rect("Party", root, Vector2.zero, Vector2.one);

            // Commands: 2 rows x 4 columns
            for (int i = 0; i < _commands.Length; i++)
            {
                int index = i;
                int row = i / 4, col = i % 4;
                float x0 = 0.03f + col * 0.2425f;
                float y1 = 0.285f - row * 0.087f;
                float y0 = y1 - 0.083f;
                var btn = UiFactory.Button("Cmd" + i, root, new Vector2(x0, y0), new Vector2(x0 + 0.2275f, y1), "", UiFactory.Red, () => OnCommandPressed(index), 28);
                _commands[i] = new CommandButton { Button = btn, Image = btn.GetComponent<Image>(), Label = btn.GetComponentInChildren<Text>(), Root = btn.gameObject };
                btn.gameObject.AddComponent<LongPress>().OnLong = () => ShowCommandTip(index);
            }

            // Effects layers (above everything except panels)
            _fx = UiFactory.Rect("Fx", root, Vector2.zero, Vector2.one);
            _flash = UiFactory.Panel("Flash", root, Vector2.zero, Vector2.one, new Color(0.8f, 0.1f, 0.1f, 0f));
            _bannerBg = UiFactory.Panel("BannerBg", root, new Vector2(0f, 0.60f), new Vector2(1f, 0.70f), new Color(0f, 0f, 0f, 0f));
            _banner = UiFactory.Label("Banner", root, new Vector2(0f, 0.60f), new Vector2(1f, 0.70f), "", 64, UiFactory.Gold);

            BuildPanel(root);
            BuildStory(root);
            BuildTip(root);
        }

        private void BuildPanel(RectTransform root)
        {
            var rect = UiFactory.Rect("Panel", root, new Vector2(0.05f, 0.27f), new Vector2(0.95f, 0.75f));
            _panel = rect.gameObject;
            var bg = rect.gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.07f, 0.11f, 0.97f);
            UiFactory.Frame(rect, UiFactory.Gold, 3f);
            UiFactory.Panel("TitleLine", rect, new Vector2(0.06f, 0.895f), new Vector2(0.94f, 0.899f), UiFactory.GoldDim);
            _panelTitle = UiFactory.Label("Title", rect, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.99f), "", 38, UiFactory.Gold);

            var text = UiFactory.Label("Text", rect, new Vector2(0.06f, 0.26f), new Vector2(0.94f, 0.80f), "", 36, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);
            _panelText = text;
            _panelTextRect = text.rectTransform;
            _panelNext = UiFactory.Button("Next", rect, new Vector2(0.30f, 0.05f), new Vector2(0.70f, 0.20f), "다음", UiFactory.Red, () => _panelDone = true, 40).gameObject;
            _panelRetry = UiFactory.Button("Retry", rect, new Vector2(0.06f, 0.05f), new Vector2(0.48f, 0.20f), "다시 도전", UiFactory.Red, () => { _retry = true; _panelDone = true; }, 36).gameObject;
            _panelBack = UiFactory.Button("Back", rect, new Vector2(0.52f, 0.05f), new Vector2(0.94f, 0.20f), "강호로", UiFactory.InkSoft, () => { _retry = false; _leave = true; _panelDone = true; }, 36).gameObject;
            _panel.SetActive(false);
        }

        /// <summary>Result / hub panels: plain centred message in the middle panel.</summary>
        private void SetPanelMode(bool story)
        {
            _panelTextRect.anchorMin = new Vector2(0.06f, 0.26f);
            _panelTextRect.anchorMax = new Vector2(0.94f, 0.88f);
            _panelText.alignment = TextAnchor.MiddleCenter;
            _panelText.fontSize = 36;
            _panelText.resizeTextMaxSize = 36;
        }

        /// <summary>Builds the full-screen story layer: large portraits on both sides, a dialogue box with a name plate at the bottom.</summary>
        private void BuildStory(RectTransform root)
        {
            var rect = UiFactory.Rect("Story", root, Vector2.zero, Vector2.one);
            _storyRoot = rect.gameObject;

            // Whole screen is the "next" button (added first so everything else sits on top of it).
            var tapImg = rect.gameObject.AddComponent<Image>();
            tapImg.color = new Color(0f, 0f, 0f, 0.30f);
            var tap = rect.gameObject.AddComponent<Button>();
            tap.targetGraphic = tapImg;
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => _panelDone = true);

            _storyTitle = UiFactory.Label("Chapter", rect, new Vector2(0.04f, 0.925f), new Vector2(0.72f, 0.985f), "", 34, UiFactory.Gold, TextAnchor.MiddleLeft);
            UiFactory.Panel("ChapterLine", rect, new Vector2(0.04f, 0.922f), new Vector2(0.72f, 0.925f), UiFactory.GoldDim);
            UiFactory.Button("Skip", rect, new Vector2(0.78f, 0.932f), new Vector2(0.97f, 0.985f), "건너뛰기", UiFactory.InkSoft, () => { _storySkip = true; _panelDone = true; }, 28);

            // Portraits (left = protagonist, right = whoever else is talking)
            for (int side = 0; side < 2; side++)
            {
                var holder = UiFactory.Panel("Portrait" + side, rect, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0f));
                holder.preserveAspect = true;
                _portrait[side] = holder;
                _portraitGlyph[side] = UiFactory.Label("Glyph", holder.transform, Vector2.zero, Vector2.one, "", 220, UiFactory.PaperText);
                PlacePortrait(side, true);
            }

            // Dialogue box
            var box = UiFactory.Panel("Box", rect, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.285f), new Color(0.04f, 0.06f, 0.10f, 0.92f));
            UiFactory.Frame(box.transform, UiFactory.Gold, 3f);
            _storyText = UiFactory.Label("Text", rect, new Vector2(0.06f, 0.055f), new Vector2(0.94f, 0.255f), "", 36, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal);
            UiFactory.Label("Arrow", rect, new Vector2(0.88f, 0.045f), new Vector2(0.95f, 0.09f), "▼", 30, UiFactory.Gold);

            // Name plate sits on the top edge of the box; moves to the right for the other speaker.
            var plate = UiFactory.Panel("NamePlate", rect, new Vector2(0.05f, 0.268f), new Vector2(0.40f, 0.330f), new Color(0.20f, 0.16f, 0.07f, 0.97f));
            UiFactory.Frame(plate.transform, UiFactory.Gold, 3f);
            _namePlate = plate.rectTransform;
            _nameTag = UiFactory.Label("Name", plate.transform, new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), "", 34, UiFactory.Gold);

            _storyRoot.SetActive(false);
        }

        /// <summary>Full-height artwork area when a portrait image exists; a smaller placeholder block otherwise.</summary>
        private void PlacePortrait(int side, bool hasSprite)
        {
            var rt = _portrait[side].rectTransform;
            if (hasSprite)
            {
                rt.anchorMin = side == 0 ? new Vector2(0.00f, 0.27f) : new Vector2(0.42f, 0.27f);
                rt.anchorMax = side == 0 ? new Vector2(0.58f, 0.93f) : new Vector2(1.00f, 0.93f);
            }
            else
            {
                rt.anchorMin = side == 0 ? new Vector2(0.06f, 0.34f) : new Vector2(0.58f, 0.34f);
                rt.anchorMax = side == 0 ? new Vector2(0.42f, 0.84f) : new Vector2(0.94f, 0.84f);
            }
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Sprite PortraitFor(string name) { return ArtLibrary.Portrait(name); }

        private static Color PortraitTint(string name) { return ArtLibrary.TintFor(name); }

        private void ShowPortrait(int side, string name, bool speaking)
        {
            var img = _portrait[side];
            var glyph = _portraitGlyph[side];
            if (string.IsNullOrEmpty(name))
            {
                img.color = new Color(1f, 1f, 1f, 0f);
                glyph.text = "";
                img.sprite = null;
                return;
            }
            var sprite = PortraitFor(name);
            img.sprite = sprite;
            PlacePortrait(side, sprite != null);
            float a = speaking ? 1f : 0.45f;
            img.rectTransform.localScale = Vector3.one * (speaking ? 1.03f : 0.96f);
            if (sprite != null) { img.color = new Color(speaking ? 1f : 0.6f, speaking ? 1f : 0.6f, speaking ? 1f : 0.7f, 1f); glyph.text = ""; }
            else { var c = PortraitTint(name); c.a = a; img.color = c; glyph.text = name.Substring(0, 1); glyph.color = new Color(1f, 1f, 1f, a); }
        }

        // ------------------------------------------------------------------ flow

        private void ApplySavedSpeed()
        {
            var settings = GameManager.Instance.Save.Settings;
            _speed = settings != null ? Mathf.Clamp(settings.BattleSpeed, 1, 3) : 1;
            _speedLabel.text = "속도 x" + _speed;
        }

        public void Open(StageData stage, Action onClose)
        {
            _onClose = onClose;
            StopAllCoroutines();
            ApplySavedSpeed();
            StartCoroutine(RunStage(stage));
        }

        private IEnumerator RunStage(StageData stage)
        {
            var save = GameManager.Instance.Save;
            _fled = false;
            _stageLabel.text = stage.IsTower ? stage.Name : "스테이지 " + stage.Number + " · " + stage.Name;
            var bg = ArtLibrary.Background(stage.Number);
            _backdropImage.sprite = bg;
            _backdropImage.color = bg != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            ClearCards();
            ClearPanels();
            SetCommandsVisible(false);
            _log.text = "";

            // Continue a stage the player stepped back from; anything else starts fresh (and drops an old resume point).
            bool resume = !stage.IsQuest && !stage.IsTower && StageProgress.Has(save, stage.Number);
            int startIndex = resume ? Math.Min(save.ProgressNext, stage.Encounters.Length - 1) : 0;
            if (!resume && !stage.IsQuest && !stage.IsTower && save.ProgressStage != 0) { StageProgress.Clear(save); GameManager.Instance.SaveNow(); }

            if (!resume)
            {
                if (stage.Prologue != null && save.ClearedStage < stage.Number)
                    foreach (var scene in stage.Prologue)
                        yield return ShowStory(scene.Title, scene.Lines);
                if (!stage.IsTower) yield return ShowStory(stage.Name, stage.Intro);
            }

            bool again = !_fled;
            while (again)
            {
                again = false;
                BuildParty(stage);
                if (resume) { StageProgress.Restore(save, _party); RefreshParty(); }
                long gainedExp = 0, gainedDoo = 0;
                bool cleared = true;
                bool replay = !stage.IsQuest && !stage.IsTower && save.ClearedStage >= stage.Number;
                bool towerFirst = stage.IsTower && stage.TowerFloor > save.TowerBest;

                for (int i = startIndex; i < stage.Encounters.Length; i++)
                {
                    _stageLabel.text = stage.IsTower ? stage.Name : "스테이지 " + stage.Number + " · " + stage.Name + "  (" + (i + 1) + "/" + stage.Encounters.Length + ")";
                    yield return Banner(i == stage.Encounters.Length - 1 ? "결전!" : "전투 " + (i + 1), 0.7f);
                    yield return Fight(stage.Encounters[i], stage.TrialFor(i));

                    if (_fled) { cleared = false; break; }
                    if (!_battle.PlayerWon) { cleared = false; break; }

                    long exp = 0, doo = 0;
                    save.BattleWins++;
                    foreach (var id in stage.Encounters[i])
                    {
                        Codex.SeeEnemy(save, id);
                        exp += EarlyGameContent.ExpReward(stage, id);
                        doo += EarlyGameContent.DooReward(stage, id);
                    }
                    if (stage.IsQuest) exp = Quests.ExpReward(save);
                    if (stage.IsTower) { exp = towerFirst ? Tower.ExpReward(save, stage.TowerFloor) : 0; if (!towerFirst) doo = 0; }
                    if (replay) exp = (long)Math.Round(exp * EarlyGameContent.ReplayExpFactor);
                    var drops = stage.IsQuest || stage.IsTower ? new List<ItemStack>() : RollDrops(stage.Encounters[i]);
                    int levels = Progression.AddExp(save, exp);
                    save.Doo += doo;
                    gainedExp += exp; gainedDoo += doo;
                    GameManager.Instance.SaveNow();
                    if (levels > 0) RebuildPartyForNewLevel(stage);
                    SoundManager.Sfx(levels > 0 ? "levelup" : "win");
                    yield return Banner("승리!  경험 +" + exp + "  두 +" + doo + (drops.Count > 0 ? "\n획득: " + DescribeStacks(drops) : "") + (levels > 0 ? "\nLv." + save.Level + " 상승!" : ""), levels > 0 || drops.Count > 0 ? 1.5f : 0.9f);
                    foreach (var m in _party) m.RestBetweenFights(0.5f);
                    RefreshParty();

                    // Between fights the player decides when to go on (the next fight may be the boss).
                    if (i < stage.Encounters.Length - 1)
                    {
                        yield return ShowHub(stage, i);
                        if (_leave)
                        {
                            StageProgress.Store(save, stage.Number, i + 1, _party);
                            GameManager.Instance.SaveNow();
                            _fled = true; cleared = false; break;
                        }
                    }
                }

                if (_fled)
                {
                    SetCommandsVisible(false);
                    break;
                }
                if (!_fled && !stage.IsQuest && !stage.IsTower) { StageProgress.Clear(save); GameManager.Instance.SaveNow(); }
                if (cleared && stage.IsQuest)
                {
                    var quest = Quests.Find(stage.QuestId);
                    string questNote = quest != null ? Quests.Complete(save, quest) : "";
                    string bonds = AwardBonds(save, quest != null ? quest.BondPoints : 1);
                    GameManager.Instance.SaveNow();
                    yield return ShowResult("의뢰 완수!", "획득  경험 " + gainedExp + " · 두 " + gainedDoo + (questNote.Length > 0 ? "\n" + questNote : "") + (bonds.Length > 0 ? "\n" + bonds : ""), false);
                }
                else if (cleared && stage.IsTower)
                {
                    string towerNote = Tower.Complete(save, stage.TowerFloor);
                    GameManager.Instance.SaveNow();
                    yield return ShowResult(stage.TowerFloor + "층 돌파!", towerFirst
                        ? "획득  경험 " + gainedExp + " · 두 " + gainedDoo + (towerNote.Length > 0 ? "\n" + towerNote : "")
                        : "이미 돌파한 층이다. (보상 없음)", false);
                }
                else if (cleared)
                {
                    save.ClearedStage = Math.Max(save.ClearedStage, stage.Number);
                    GameManager.Instance.SaveNow();
                    yield return ShowStory(stage.Name + " 돌파", stage.Outro);
                    string bondNote = AwardBonds(save);
                    var reward = Inventory.GrantStageReward(save, stage.Number);
                    GameManager.Instance.SaveNow();
                    yield return ShowResult("스테이지 돌파!", "획득  경험 " + gainedExp + " · 두 " + gainedDoo + (bondNote.Length > 0 ? "\n" + bondNote : "") + (reward.Count > 0 ? "\n\n첫 돌파 보상\n" + DescribeStacks(reward) : ""), false);
                }
                else
                {
                    yield return ShowResult("패배...", "다시 도전하면 이 스테이지의 첫 전투부터 시작한다. (이미 얻은 경험과 두는 그대로 남는다.)", true);
                    again = _retry;
                    resume = false;
                    startIndex = 0;
                }
            }

            _panel.SetActive(false);
            _banner.text = "";
            _bannerBg.color = new Color(0f, 0f, 0f, 0f);
            ClearCards();
            ClearPanels();
            SoundManager.Bgm("lobby");
            _onClose?.Invoke();
        }

        /// <summary>+1 인연 for every companion who fought in this stage; returns a note when a bond level rose.</summary>
        private string AwardBonds(SaveData save, int points = 1)
        {
            var notes = new List<string>();
            foreach (var m in _party)
            {
                if (m.Id == "player") continue;
                int rose = Bonds.Add(save, m.Id, points);
                if (rose >= 0) notes.Add(m.Name + "와(과)의 인연: " + Bonds.LevelNames[rose] + (rose == 1 ? " — 합공 가능!" : ""));
            }
            GameManager.Instance.SaveNow();
            return string.Join("\n", notes);
        }

        // ------------------------------------------------------------------ 승급 비무

        /// <summary>Opens a 승급 비무: the fee was already paid by the caller. Two solo duels against the offer's opponents.</summary>
        public void OpenPromotion(PromotionOffer offer, Action onClose)
        {
            _onClose = onClose;
            StopAllCoroutines();
            StartCoroutine(RunPromotion(offer));
        }

        private IEnumerator RunPromotion(PromotionOffer offer)
        {
            var save = GameManager.Instance.Save;
            var arena = new StageData { Number = 0, Name = "승급 비무" };   // Number 0 = no companions
            _fled = false;
            _backdropImage.sprite = null;
            _backdropImage.color = new Color(1f, 1f, 1f, 0f);
            ClearCards();
            ClearPanels();
            SetCommandsVisible(false);
            _log.text = "";
            _stageLabel.text = offer.Title;

            yield return ShowStory(offer.Title, new[]
            {
                "비무대 위에 선 것은 나 혼자다. 동료의 도움은 없다.",
                Promotion.RankName(offer.TargetRank) + "에 오르려면 상대 둘을 연달아 꺾어야 한다.",
                "한 판은 " + Promotion.TurnLimitFor(offer.TargetRank) + "턴 안에 끝내야 한다. 시간이 다 되면 무승부로 치되, 상금은 절반이다."
            });

            bool again = !_fled;
            while (again)
            {
                again = false;
                BuildParty(arena);
                bool anyDraw = false, lost = false;
                for (int i = 0; i < offer.Opponents.Length; i++)
                {
                    _stageLabel.text = offer.Title + "  (" + (i + 1) + "/" + offer.Opponents.Length + ")";
                    yield return Banner(i == offer.Opponents.Length - 1 ? "마지막 비무!" : "비무 " + (i + 1), 0.7f);
                    yield return Fight(new[] { offer.Opponents[i] }, Promotion.TurnLimitFor(offer.TargetRank));
                    if (_fled) break;
                    if (_drawn)
                    {
                        anyDraw = true;
                        yield return Banner("시간 초과! 무승부", 1.0f);
                    }
                    else if (!_battle.PlayerWon) { lost = true; break; }
                    else yield return Banner("승리!", 0.7f);
                    foreach (var m in _party) m.RestBetweenFights(0.5f);
                    RefreshParty();
                }

                if (_fled) { SetCommandsVisible(false); break; }
                if (lost)
                {
                    bool canRetry = Promotion.CanAfford(save, offer);
                    yield return ShowResult("패배...", "입장료 " + offer.Fee.ToString("N0") + "두는 돌려받지 못한다." + (canRetry ? "\n다시 도전하려면 입장료를 한 번 더 내야 한다." : "\n두가 모자라 다시 도전할 수 없다."), true, canRetry);
                    if (_retry && Promotion.Pay(save, offer)) { GameManager.Instance.SaveNow(); again = true; }
                }
                else
                {
                    long doo = Promotion.Complete(save, offer, anyDraw);
                    int levels = Progression.AddExp(save, offer.ExpReward);
                    GameManager.Instance.SaveNow();
                    yield return ShowResult(Promotion.RankName(offer.TargetRank) + " 승급!",
                        (anyDraw ? "무승부가 섞여 상금이 절반이다.\n" : "") + "상금 " + doo.ToString("N0") + "두 · 경험 " + offer.ExpReward + (levels > 0 ? "\nLv." + save.Level + " 상승!" : ""), false);
                }
            }

            _panel.SetActive(false);
            _banner.text = "";
            _bannerBg.color = new Color(0f, 0f, 0f, 0f);
            ClearCards();
            ClearPanels();
            _onClose?.Invoke();
        }

        private List<ItemStack> RollDrops(string[] enemyIds)
        {
            var save = GameManager.Instance.Save;
            var rng = new SeededRandom(Environment.TickCount);
            var all = new List<ItemStack>();
            foreach (var id in enemyIds)
                foreach (var stack in Inventory.RollDrops(id, rng))
                {
                    Inventory.Add(save, stack.Id, stack.Count);
                    all.Add(stack);
                }
            return all;
        }

        private static string DescribeStacks(List<ItemStack> stacks)
        {
            var names = new List<string>();
            foreach (var s in stacks) names.Add(Inventory.Describe(s));
            return string.Join(", ", names);
        }

        private IEnumerator Fight(string[] enemyIds, TrialRule trial)
        {
            var data = new EnemyData[enemyIds.Length];
            for (int i = 0; i < data.Length; i++) data[i] = EarlyGameContent.FindEnemy(enemyIds[i]);
            if (trial != null) yield return Banner("[" + trial.Title + "]\n" + trial.Brief, 2.4f);
            yield return Fight(data, 0, trial);
        }

        /// <summary>Plays one fight. With <paramref name="turnLimit"/> > 0 the fight is a draw once the player has acted that many times.</summary>
        private IEnumerator Fight(EnemyData[] enemies, int turnLimit, TrialRule trial = null)
        {
            bool bossFight = false;
            foreach (var en in enemies) if (en.IsBoss) bossFight = true;
            SoundManager.Bgm(bossFight ? "boss" : "battle");
            _drawn = false;
            int actions = 0;
            string label = _stageLabel.text;
            SpawnCards(enemies);
            _battle = new InteractiveBattle(_party, _cards.ConvertAll(c => c.Unit), new SeededRandom(Environment.TickCount)) { Trial = trial };
            _target = _cards[0];
            RefreshParty();
            RefreshCards();

            yield return PlayEvents(_battle.Begin());

            while (!_battle.IsOver && !_fled)
            {
                if (_target == null || !_target.Unit.IsAlive) _target = _cards.Find(c => c.Unit.IsAlive);
                _itemMode = false;
                _jointMode = false;
                if (turnLimit > 0) _stageLabel.text = label + "   " + actions + "/" + turnLimit + "턴";
                else if (trial != null) _stageLabel.text = label + "   " + _battle.TrialStatus;
                RefreshCards();
                RefreshParty();
                RefreshCommands();
                RefreshTurnOrder();

                // First-time hints (battle controls, 합공) appear right when they become relevant.
                if (!_fled && HelpPanel.Instance != null)
                {
                    var save0 = GameManager.Instance.Save;
                    string hint = !Tutorial.IsDone(save0, "battle") ? "battle" : _jointPartners.Count > 0 && !Tutorial.IsDone(save0, "joint") ? "joint" : null;
                    if (hint != null) yield return HelpPanel.Instance.ShowOnce(hint);
                }

                _hasCommand = false;
                while (!_hasCommand && !_fled) yield return null;
                if (_fled) yield break;

                SetCommandsInteractable(false);
                var actor = _battle.Current;
                var events = _battle.PlayerAct(_command);
                if (_command.Kind == CommandKind.Skill && actor != null && actor.Id == "player" && _command.Skill != null && _command.Skill.Id != "basic")
                {
                    int rose = Mastery.RecordUse(GameManager.Instance.Save, _command.Skill.Id);
                    GameManager.Instance.SaveNow();
                    if (rose >= 0) SoundManager.Sfx("levelup");
                    if (rose >= 0) _masteryNotice = _command.Skill.Name + " 숙련 " + Mastery.LevelNames[rose] + "!" + (rose == Mastery.VariantLevel ? "  변초 해금 (무공 탭)" : "");
                }
                if (_command.Kind == CommandKind.Item)
                {
                    Inventory.Remove(GameManager.Instance.Save, _command.Item.Id, 1);
                    GameManager.Instance.SaveNow();
                }
                yield return PlayEvents(events);
                if (_masteryNotice != null)
                {
                    string note = _masteryNotice; _masteryNotice = null;
                    yield return Banner(note, 1.2f);
                    if (HelpPanel.Instance != null) yield return HelpPanel.Instance.ShowOnce("mastery");
                }
                actions++;
                if (turnLimit > 0 && actions >= turnLimit && !_battle.IsOver) { _drawn = true; break; }
            }
            if (trial != null && _battle.TrialFailed) _log.text = _battle.TrialFailReason;
            if (turnLimit > 0 || trial != null) _stageLabel.text = label;

            RefreshParty();
            RefreshCards();
            SetCommandsVisible(false);
            yield return new WaitForSeconds(0.25f);
        }

        // ------------------------------------------------------------------ setup

        private static List<SkillSpec> ArtsOf(Combatant member)
        {
            var list = new List<SkillSpec>();
            foreach (var s in member.Skills) if (s.Id != "basic") list.Add(s);
            return list;
        }

        private List<Combatant> CreateParty(StageData stage)
        {
            var save = GameManager.Instance.Save;
            var skills = new List<SkillSpec>();
            foreach (var id in save.EquippedArts)
            {
                var art = EarlyGameContent.FindArt(id);
                if (art != null && art.OwnerId == null && skills.Count < Progression.MaxEquippedArts) skills.Add(Mastery.Apply(save, art));
            }
            var stats = InnerArts.HeroStats(save);
            var hero = new Combatant("player", "비류연", Team.Player, stats, skills);
            InnerArts.ApplyPerks(hero, save.EquippedInner);
            var party = new List<Combatant> { hero };
            foreach (var c in EarlyGameContent.SelectCompanionsFor(stage.Number, save))
            {
                int bondLevel = Bonds.Level(save, c.Id);
                party.Add(c.CreateCombatant(save.Level, bondLevel));
                if (bondLevel >= Bonds.RescueLevel && string.IsNullOrEmpty(hero.RescuerId)) hero.RescuerId = c.Id;
            }
            return party;
        }

        // Side-view formations: our party on the left half, the enemies on the right half (each a 2-column grid).
        private const float CellW = 0.22f, CellH = 0.265f, RowTopY0 = 0.625f, RowBotY0 = 0.350f;

        /// <summary>Cell rectangle (x0, y0, x1, y1) of the i-th unit of n in a formation; partySide = left half.</summary>
        private static Rect Cell(int i, int n, bool partySide, bool bigSingle)
        {
            if (n == 1)
            {
                float w = bigSingle ? 0.44f : 0.30f, h = bigSingle ? 0.46f : 0.34f;
                float x = partySide ? 0.14f : (bigSingle ? 0.52f : 0.60f);
                float y = 0.62f - h / 2f + 0.07f;
                return new Rect(x, y, w, h);
            }
            float front = partySide ? 0.24f : 0.54f, back = partySide ? 0.01f : 0.77f;
            float x0, y0;
            switch (i)
            {
                case 0: x0 = front; y0 = RowTopY0; break;
                case 1: x0 = front; y0 = RowBotY0; break;
                case 2: x0 = back; y0 = n == 3 ? (RowTopY0 + RowBotY0) / 2f : RowTopY0; break;
                default: x0 = back; y0 = RowBotY0; break;
            }
            return new Rect(x0, y0, CellW, CellH);
        }

        private void BuildParty(StageData stage)
        {
            _party = CreateParty(stage);
            ClearPanels();
            int n = _party.Count;
            for (int i = 0; i < n; i++)
            {
                var unit = _party[i];
                var cell = Cell(i, n, true, false);
                var p = new PartyPanel { Unit = unit, Center = new Vector2(cell.x + cell.width / 2f, cell.y + cell.height / 2f) };
                p.Root = UiFactory.Rect("Member" + i, _partyRoot, new Vector2(cell.x, cell.y), new Vector2(cell.x + cell.width, cell.y + cell.height));
                p.Frame = UiFactory.Panel("Frame", p.Root, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));
                UiFactory.Panel("Bg", p.Root, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f), new Color(0.06f, 0.08f, 0.12f, 0.62f));
                var sprite = i == 0 ? ArtLibrary.Player() : ArtLibrary.Companion(unit.Id);
                if (sprite != null)
                {
                    p.Art = UiFactory.Panel("Art", p.Root, new Vector2(0.04f, 0.30f), new Vector2(0.96f, 0.92f), Color.white);
                    p.Art.sprite = sprite;
                    p.Art.preserveAspect = true;
                }
                else
                {
                    UiFactory.Label("Glyph", p.Root, new Vector2(0.04f, 0.32f), new Vector2(0.96f, 0.90f), FirstGlyph(unit.Name), 110, i == 0 ? UiFactory.Gold : UiFactory.PaperText);
                }
                p.Name = UiFactory.Label("Name", p.Root, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.99f), unit.Name, 24, UiFactory.Gold);
                p.Status = UiFactory.Label("Status", p.Root, new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.76f), "", 26, UiFactory.PaperText);
                // Thin gauges under the figure: 체력 / 내공 / 기세
                p.Hp = UiFactory.MakeBar("Hp", p.Root, new Vector2(0.06f, 0.215f), new Vector2(0.94f, 0.275f), new Color(0f, 0f, 0f, 0.7f), HpGreen);
                p.HpText = UiFactory.Label("HpText", p.Root, new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.215f), "", 18, UiFactory.PaperText);
                p.Qi = UiFactory.MakeBar("Qi", p.Root, new Vector2(0.06f, 0.095f), new Vector2(0.94f, 0.140f), new Color(0f, 0f, 0f, 0.7f), QiBlue);
                p.Spirit = UiFactory.MakeBar("Spirit", p.Root, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.085f), new Color(0f, 0f, 0f, 0.7f), UiFactory.Gold);
                p.QiText = UiFactory.Label("QiText", p.Root, new Vector2(0.06f, 0.095f), new Vector2(0.94f, 0.140f), "", 14, UiFactory.PaperText);
                p.SpiritText = UiFactory.Label("SpiritText", p.Root, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.085f), "", 14, UiFactory.PaperText);
                p.QiText.gameObject.SetActive(false);
                p.SpiritText.gameObject.SetActive(false);
                var panelUnit = unit;
                p.Root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // catches the press
                p.Root.gameObject.AddComponent<LongPress>().OnLong = () => ShowMemberTip(panelUnit);
                _panels.Add(p);
            }
        }

        private static void MakeStatRow(RectTransform parent, string label, float y0, Color fill, out UiFactory.Bar bar, out Text value)
        {
            UiFactory.Label(label + "Label", parent, new Vector2(0.05f, y0), new Vector2(0.24f, y0 + 0.19f), label, 22, UiFactory.PaperText, TextAnchor.MiddleLeft);
            bar = UiFactory.MakeBar(label + "Bar", parent, new Vector2(0.25f, y0 + 0.02f), new Vector2(0.96f, y0 + 0.17f), new Color(0.2f, 0.2f, 0.2f, 0.9f), fill);
            value = UiFactory.Label(label + "Value", parent, new Vector2(0.25f, y0), new Vector2(0.96f, y0 + 0.19f), "", 20, UiFactory.PaperText);
        }

        private void RebuildPartyForNewLevel(StageData stage)
        {
            var fresh = CreateParty(stage);
            for (int i = 0; i < fresh.Count && i < _party.Count; i++)
            {
                float ratio = (float)_party[i].Hp / Math.Max(1, _party[i].Stats.MaxHp);
                fresh[i].Hp = _party[i].IsAlive ? Math.Max(1, (int)Math.Round(fresh[i].Stats.MaxHp * ratio)) : 0;
                if (i < _panels.Count) _panels[i].Unit = fresh[i];
            }
            _party = fresh;
        }

        private void ClearCards()
        {
            foreach (var c in _cards) if (c.Root != null) Destroy(c.Root.gameObject);
            _cards.Clear();
        }

        private void ClearPanels()
        {
            foreach (var p in _panels) if (p.Root != null) Destroy(p.Root.gameObject);
            _panels.Clear();
        }

        private void SpawnCards(EnemyData[] enemies)
        {
            ClearCards();
            int n = enemies.Length;
            for (int i = 0; i < n; i++)
            {
                var data = enemies[i];
                var cell = Cell(i, n, false, data.IsBoss);
                var card = new EnemyCard { Unit = data.CreateCombatant(data.Id + "#" + i), Center = new Vector2(cell.x + cell.width / 2f, cell.y + cell.height / 2f) };
                card.Root = UiFactory.Rect("Card" + i, _cardsRoot, new Vector2(cell.x, cell.y), new Vector2(cell.x + cell.width, cell.y + cell.height));
                card.Frame = UiFactory.Panel("Frame", card.Root, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));
                card.Body = UiFactory.Panel("Body", card.Root, new Vector2(0.025f, 0.02f), new Vector2(0.975f, 0.98f), data.IsBoss ? new Color(0.30f, 0.08f, 0.10f, 0.70f) : new Color(0.06f, 0.08f, 0.12f, 0.62f));
                card.Name = UiFactory.Label("Name", card.Root, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.99f), data.Name, data.IsBoss ? 30 : 22, data.IsBoss ? UiFactory.Gold : UiFactory.PaperText);
                var art = ArtLibrary.Enemy(data.ArtKey);
                if (art != null)
                {
                    var pic = UiFactory.Panel("Art", card.Root, new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.88f), Color.white);
                    pic.sprite = art;
                    pic.preserveAspect = true;
                    card.Art = pic;
                }
                card.Glyph = UiFactory.Label("Glyph", card.Root, new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.88f), art != null ? "" : FirstGlyph(data.Name), data.IsBoss ? 170 : 110, data.IsBoss ? UiFactory.Gold : UiFactory.PaperText);
                card.Hp = UiFactory.MakeBar("Hp", card.Root, new Vector2(0.06f, 0.215f), new Vector2(0.94f, 0.275f), new Color(0f, 0f, 0f, 0.7f), UiFactory.Red);
                card.HpText = UiFactory.Label("HpText", card.Root, new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.215f), "", 18, UiFactory.PaperText);
                card.Intent = UiFactory.Label("Intent", card.Root, new Vector2(0.04f, 0.01f), new Vector2(0.96f, 0.15f), "", 18, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);

                // Tap to target
                var tap = card.Root.gameObject.AddComponent<Button>();
                var tapImg = card.Root.gameObject.AddComponent<Image>();
                tapImg.color = new Color(0f, 0f, 0f, 0f);
                tap.targetGraphic = tapImg;
                var captured = card;
                tap.onClick.AddListener(() => { if (captured.Unit.IsAlive) { _target = captured; RefreshCards(); } });
                _cards.Add(card);
            }
        }

        private static string FirstGlyph(string name)
        {
            // Skip honorific prefixes so the big letter is a meaningful syllable.
            string[] skip = { "비풍도 ", "일도파쇄 ", "선풍검룡 ", "호아장주 ", "호아장 대사형 ", "청풍검 ", "기관 ", "염도 ", "패호 ", "패룡 " };
            foreach (var s in skip) if (name.StartsWith(s)) name = name.Substring(s.Length);
            return name.Length > 0 ? name.Substring(0, 1) : "?";
        }

        // ------------------------------------------------------------------ refresh

        private void RefreshCards()
        {
            foreach (var c in _cards)
            {
                var u = c.Unit;
                bool dead = !u.IsAlive;
                c.Hp.SetPct(u.Stats.MaxHp > 0 ? (float)u.Hp / u.Stats.MaxHp : 0f);
                c.HpText.text = u.Hp + " / " + u.Stats.MaxHp;
                c.Frame.color = c == _target && !dead ? UiFactory.Gold : new Color(0f, 0f, 0f, 0f);
                SetAlpha(c.Body, dead ? 0.15f : 0.68f);
                if (c.Art != null) SetAlpha(c.Art, dead ? 0.25f : 1f);
                c.Glyph.color = dead ? new Color(1f, 1f, 1f, 0.2f) : c.Glyph.color;
                if (dead) { c.Intent.text = "쓰러졌다"; continue; }
                if (u.Stunned) { c.Intent.text = "행동 불능"; c.Intent.color = GuardBlue; continue; }
                var move = u.PeekMove();
                if (u.Charging)
                {
                    c.Intent.text = "위험! " + move.Name + (u.ChargeTarget != null ? "\n→ " + u.ChargeTarget.Name : "");
                    c.Intent.color = UiFactory.Red;
                }
                else if (move.Charge)
                {
                    c.Intent.text = "다음: " + move.Name;
                    c.Intent.color = UiFactory.Gold;
                }
                else
                {
                    c.Intent.text = "다음: " + move.Name + (move.Hits > 1 ? " x" + move.Hits : "");
                    c.Intent.color = UiFactory.PaperText;
                }
            }
        }

        private void RefreshParty()
        {
            foreach (var p in _panels)
            {
                var u = p.Unit;
                p.Hp.SetPct((float)u.Hp / Math.Max(1, u.Stats.MaxHp));
                p.Qi.SetPct((float)u.Qi / Math.Max(1, u.Stats.MaxQi));
                p.Spirit.SetPct(u.Spirit / (float)InteractiveBattle.MaxSpirit);
                p.HpText.text = u.Hp + "/" + u.Stats.MaxHp;
                p.QiText.text = u.Qi + "/" + u.Stats.MaxQi;
                p.SpiritText.text = u.Spirit + "/" + InteractiveBattle.MaxSpirit;
                p.Status.text = !u.IsAlive ? "쓰러짐" : u.Guarding ? "방어" : u.Evading ? "회피" : "";
                p.Name.color = u.IsAlive ? UiFactory.Gold : new Color(0.5f, 0.5f, 0.5f, 1f);
                bool current = _battle != null && _battle.Current == u && _battle.IsPlayerTurn;
                p.Frame.color = current ? UiFactory.Gold : new Color(0f, 0f, 0f, 0f);
            }
        }

        private void RefreshTurnOrder()
        {
            var order = _battle.PreviewOrder(_turnChips.Count);
            for (int i = 0; i < _turnChips.Count; i++)
            {
                bool has = i < order.Count;
                _turnChips[i].gameObject.SetActive(has);
                _turnLabels[i].gameObject.SetActive(has);
                if (!has) continue;
                bool ours = order[i].Team == Team.Player;
                _turnChips[i].color = ours ? UiFactory.Gold : new Color(0.55f, 0.16f, 0.14f, 1f);
                _turnLabels[i].text = ShortName(order[i].Name);
                _turnLabels[i].color = ours ? UiFactory.Ink : UiFactory.PaperText;
            }
        }

        private static string ShortName(string name)
        {
            int space = name.LastIndexOf(' ');
            if (space >= 0) name = name.Substring(space + 1);
            return name.Length > 3 ? name.Substring(0, 3) : name;
        }

        private static List<ItemData> UsableItems()
        {
            var list = new List<ItemData>();
            foreach (var s in GameManager.Instance.Save.Items)
            {
                var item = EarlyGameContent.FindItem(s.Id);
                if (item != null && item.Kind == ItemKind.Consumable && s.Count > 0) list.Add(item);
            }
            return list;
        }

        private void RefreshCommands()
        {
            var me = _battle.Current;
            if (me == null) return;
            SetCommandsVisible(true);

            if (_jointMode)
            {
                for (int i = 0; i < 7; i++)
                {
                    if (i >= _jointPartners.Count) { _commands[i].Root.SetActive(false); continue; }
                    var c = EarlyGameContent.FindCompanion(_jointPartners[i].Id);
                    SetCommand(i, c != null ? c.JointName : "합공", _jointPartners[i].Name, true);
                }
                SetCommand(7, "취소", "", true);
                _commands[8].Root.SetActive(false);
                return;
            }

            if (_itemMode)
            {
                _commands[8].Root.SetActive(false);
                var items = UsableItems();
                for (int i = 0; i < 7; i++)
                {
                    if (i >= items.Count) { _commands[i].Root.SetActive(false); continue; }
                    var item = items[i];
                    SetCommand(i, item.Name + " x" + Inventory.Count(GameManager.Instance.Save, item.Id), ItemHint(item), true);
                }
                SetCommand(7, "취소", "", true);
                return;
            }

            SetCommand(0, "공격", "내공 0", true);
            var arts = ArtsOf(me);
            for (int i = 0; i < Progression.MaxEquippedArts; i++)
            {
                int slot = 1 + i;
                if (i >= arts.Count) { _commands[slot].Root.SetActive(false); continue; }
                var s = arts[i];
                int cd = me.CooldownLeft(s);
                bool ok = _battle.CanUse(s);
                string sub = cd > 0 ? "재사용 " + cd + "턴" : s.SpiritCost > 0 ? "기세 " + s.SpiritCost : s.QiCost > 0 ? "내공 " + s.QiCost : "";
                if (cd == 0 && me.Qi < s.QiCost) sub = "내공 부족";
                SetCommand(slot, s.Name, sub, ok);
            }
            SetCommand(5, "방어", "피해 60% 감소", true);
            SetCommand(6, "운기", "내공 +" + InteractiveBattle.MeditateQi + " 체력 소량", true);
            SetCommand(7, "아이템", UsableItems().Count > 0 ? "" : "없음", UsableItems().Count > 0);

            RefreshJointPartners();
            if (_jointPartners.Count == 0) _commands[8].Root.SetActive(false);
            else SetCommand(8, "합공", me.Spirit >= InteractiveBattle.JointSpiritCost ? "기세 " + InteractiveBattle.JointSpiritCost : "기세 부족", me.Spirit >= InteractiveBattle.JointSpiritCost);
        }

        /// <summary>Living companions whose bond with the protagonist allows a 합공 (only on the protagonist's turn).</summary>
        private void RefreshJointPartners()
        {
            _jointPartners.Clear();
            var me = _battle.Current;
            if (me == null || me != _battle.Player) return;
            var save = GameManager.Instance.Save;
            foreach (var m in _battle.Party)
            {
                if (m == me || !m.IsAlive) continue;
                if (Bonds.Level(save, m.Id) >= 1) _jointPartners.Add(m);
            }
        }

        private void IssueJoint(Combatant partner, Combatant target)
        {
            var companion = EarlyGameContent.FindCompanion(partner.Id);
            var spec = Bonds.JointSpec(companion, Bonds.Level(GameManager.Instance.Save, partner.Id), Bonds.ReferencePower(_battle.Player.Skills));
            if (spec == null || !_battle.CanJoint(partner)) return;
            _jointName = spec.Name;
            _command = PlayerCommand.Joint(spec, partner, target);
            _hasCommand = true;
        }

        private static string ItemHint(ItemData item)
        {
            if (item.HealPct >= 1f) return "완전 회복";
            if (item.HealPct > 0f) return "체력 " + Mathf.RoundToInt(item.HealPct * 100f) + "%";
            if (item.QiRestore > 0) return "내공 +" + item.QiRestore;
            return "";
        }

        private void SetCommand(int slot, string name, string sub, bool enabled)
        {
            var c = _commands[slot];
            c.Root.SetActive(true);
            c.Label.text = name + (string.IsNullOrEmpty(sub) ? "" : "\n" + sub);
            c.Button.interactable = enabled;
            c.Image.color = enabled ? (slot == 5 ? new Color(0.25f, 0.40f, 0.55f, 1f) : slot == 6 ? new Color(0.25f, 0.45f, 0.35f, 1f) : UiFactory.Red) : Disabled;
        }

        // ------------------------------------------------------------------ long-press tips

        private GameObject _tipRoot;
        private Text _tipTitle, _tipBody;

        private void BuildTip(RectTransform root)
        {
            var rt = UiFactory.Rect("Tip", root, Vector2.zero, Vector2.one);
            _tipRoot = rt.gameObject;
            var dim = rt.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            var close = rt.gameObject.AddComponent<Button>();
            close.targetGraphic = dim;
            close.transition = Selectable.Transition.None;
            close.onClick.AddListener(() => _tipRoot.SetActive(false));
            var card = UiFactory.Panel("Card", rt, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.72f), new Color(0.05f, 0.07f, 0.11f, 0.98f));
            UiFactory.Frame(card.transform, UiFactory.Gold, 3f);
            _tipTitle = UiFactory.Label("Title", card.transform, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.98f), "", 40, UiFactory.Gold, TextAnchor.MiddleLeft);
            _tipBody = UiFactory.Label("Body", card.transform, new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.85f), "", 28, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal);
            UiFactory.Label("Hint", card.transform, new Vector2(0.05f, 0.01f), new Vector2(0.95f, 0.09f), "아무 곳이나 누르면 닫힌다", 20, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);
            _tipRoot.SetActive(false);
        }

        private void ShowTip(string title, string body)
        {
            if (_tipRoot == null) return;
            _tipTitle.text = title;
            _tipBody.text = body;
            _tipRoot.transform.SetAsLastSibling();
            _tipRoot.SetActive(true);
            SoundManager.Sfx("ui_click");
        }

        /// <summary>"위력 x2.4 · 3타 · 내공 30 · 재사용 3턴" plus what the effect does.</summary>
        private static string SkillDetails(SkillSpec s)
        {
            var parts = new List<string>();
            if (s.Effect == SkillEffect.Evade) parts.Add("다음 차례까지 모든 공격 회피");
            else if (s.Effect == SkillEffect.GuardParty) parts.Add("파티 전체 방어 태세");
            else if (s.Effect == SkillEffect.HealAlly) parts.Add("가장 다친 동료 체력 " + Mathf.RoundToInt(s.HealPct * 100f) + "% 회복");
            else
            {
                parts.Add("위력 ×" + s.Power.ToString("0.0") + (s.Hits > 1 ? " · " + s.Hits + "타" : ""));
                if (s.AllEnemies) parts.Add("적 전체");
                if (s.Effect == SkillEffect.Stun) parts.Add("굳힘 (기를 모으는 적의 강타를 끊는다)");
            }
            parts.Add(s.SpiritCost > 0 ? "기세 " + s.SpiritCost : s.QiCost > 0 ? "내공 " + s.QiCost : "내공 0");
            if (s.Cooldown > 0) parts.Add("재사용 " + s.Cooldown + "턴");
            return string.Join(" · ", parts);
        }

        private static string SkillBody(SkillSpec s)
        {
            string desc = s.Id == "basic" ? "기본 공격." : s.Description;
            return SkillDetails(s) + (string.IsNullOrEmpty(desc) ? "" : "\n" + desc);
        }

        private static string SkillBlock(SkillSpec s) { return s.Name + "\n" + SkillBody(s); }

        /// <summary>Long press on a command button: what that command does.</summary>
        private void ShowCommandTip(int slot)
        {
            if (_battle == null || _battle.Current == null || !_commands[slot].Root.activeSelf) return;
            var me = _battle.Current;
            if (_jointMode)
            {
                if (slot < _jointPartners.Count) ShowJointTip(_jointPartners[slot]);
                return;
            }
            if (_itemMode)
            {
                var items = UsableItems();
                if (slot < items.Count) ShowTip(items[slot].Name, ItemTipText(items[slot]));
                return;
            }
            if (slot == 0) ShowTip("공격", "기본 공격. 내공을 쓰지 않고 기세가 오른다.");
            else if (slot >= 1 && slot <= Progression.MaxEquippedArts)
            {
                var arts = ArtsOf(me);
                if (slot - 1 < arts.Count) ShowTip(arts[slot - 1].Name, SkillBody(arts[slot - 1]));
            }
            else if (slot == 5) ShowTip("방어", "다음 차례까지 받는 피해가 크게 줄어든다(피해 60% 감소). 기세가 조금 오른다.");
            else if (slot == 6) ShowTip("운기", "내공 +" + InteractiveBattle.MeditateQi + "과 체력을 조금 회복한다.");
            else if (slot == 7) ShowTip("아이템", "행낭의 소모품을 쓴다. 가장 다친 동료의 체력이 회복된다.");
            else if (slot == 8 && _jointPartners.Count > 0)
            {
                if (_jointPartners.Count == 1) ShowJointTip(_jointPartners[0]);
                else ShowTip("합공", "기세 " + InteractiveBattle.JointSpiritCost + "를 쓰고 인연 있는 동료와 함께 연달아 친다. 동료는 다음 행동을 쓴다.");
            }
        }

        private static string ItemTipText(ItemData item)
        {
            var fx = new List<string>();
            if (item.HealPct > 0f) fx.Add("체력 " + Mathf.RoundToInt(item.HealPct * 100f) + "% 회복");
            if (item.QiRestore > 0) fx.Add("내공 " + (item.QiRestore >= 999 ? "가득" : "+" + item.QiRestore));
            if (item.SpiritGain > 0) fx.Add("기세 +" + item.SpiritGain);
            return (fx.Count > 0 ? string.Join(" · ", fx) + "\n" : "") + item.Description;
        }

        private void ShowJointTip(Combatant partner)
        {
            var companion = EarlyGameContent.FindCompanion(partner.Id);
            var spec = companion != null ? Bonds.JointSpec(companion, Bonds.Level(GameManager.Instance.Save, partner.Id), Bonds.ReferencePower(_battle.Player.Skills)) : null;
            string name = companion != null && !string.IsNullOrEmpty(companion.JointName) ? companion.JointName : "합공";
            string extra = spec == null ? "" : spec.Effect == SkillEffect.Stun ? "\n적을 굳혀 강타를 끊는다." : spec.Effect == SkillEffect.GuardParty ? "\n파티 전체가 방어 태세를 취한다." : spec.Effect == SkillEffect.HealAlly ? "\n가장 다친 동료를 치유한다." : "";
            ShowTip(name + " (" + partner.Name + ")", "기세 " + InteractiveBattle.JointSpiritCost + " · 위력 ×" + (spec != null ? spec.Power.ToString("0.0") : "?") + "\n비류연과 " + partner.Name + "이(가) 연달아 친다. " + partner.Name + "은(는) 다음 행동을 쓴다." + extra);
        }

        /// <summary>Long press on a party member's panel: every skill they have.</summary>
        private void ShowMemberTip(Combatant unit)
        {
            if (unit == null) return;
            var lines = new List<string>();
            foreach (var s in unit.Skills) lines.Add(SkillBlock(s));
            var companion = EarlyGameContent.FindCompanion(unit.Id);
            if (companion != null && !string.IsNullOrEmpty(companion.JointName))
                lines.Add("합공: " + companion.JointName + " (인연 " + Bonds.LevelNames[Bonds.Level(GameManager.Instance.Save, companion.Id)] + ", " + Bonds.BonusText(Bonds.Level(GameManager.Instance.Save, companion.Id)) + ")");
            ShowTip(unit.Name + " — 스킬", string.Join("\n\n", lines));
        }

        private void SetCommandsVisible(bool visible)
        {
            foreach (var c in _commands) if (c != null) c.Root.SetActive(visible);
        }

        private void SetCommandsInteractable(bool on)
        {
            foreach (var c in _commands) if (c != null) c.Button.interactable = on;
        }

        // ------------------------------------------------------------------ input

        private void OnCommandPressed(int slot)
        {
            if (LongPress.JustFired) return;
            if (_battle == null || !_battle.IsPlayerTurn || _hasCommand) return;
            var me = _battle.Current;

            if (_jointMode)
            {
                if (slot == 7) { _jointMode = false; RefreshCommands(); return; }
                if (slot >= _jointPartners.Count) return;
                if (_target == null || !_target.Unit.IsAlive) _target = _cards.Find(c => c.Unit.IsAlive);
                IssueJoint(_jointPartners[slot], _target != null ? _target.Unit : null);
                return;
            }

            if (_itemMode)
            {
                if (slot == 7) { _itemMode = false; RefreshCommands(); return; }
                var items = UsableItems();
                if (slot >= items.Count) return;
                _command = PlayerCommand.UseItem(items[slot]);
                _hasCommand = true;
                return;
            }

            if (_target == null || !_target.Unit.IsAlive) _target = _cards.Find(c => c.Unit.IsAlive);
            var targetUnit = _target != null ? _target.Unit : null;
            if (slot == 0) _command = PlayerCommand.UseSkill(SkillSpec.BasicAttack(), targetUnit);
            else if (slot >= 1 && slot <= Progression.MaxEquippedArts)
            {
                var arts = ArtsOf(me);
                int i = slot - 1;
                if (i >= arts.Count || !_battle.CanUse(arts[i])) return;
                _command = PlayerCommand.UseSkill(arts[i], targetUnit);
            }
            else if (slot == 8)
            {
                if (me.Spirit < InteractiveBattle.JointSpiritCost || _jointPartners.Count == 0) return;
                if (_jointPartners.Count == 1) { IssueJoint(_jointPartners[0], targetUnit); return; }
                _jointMode = true;
                RefreshCommands();
                return;
            }
            else if (slot == 5) _command = PlayerCommand.Guard();
            else if (slot == 6) _command = PlayerCommand.Meditate();
            else if (slot == 7)
            {
                if (UsableItems().Count == 0) return;
                _itemMode = true;
                RefreshCommands();
                return;
            }
            else return;
            _hasCommand = true;
        }

        private void OnSpeedPressed()
        {
            _speed = _speed == 1 ? 2 : _speed == 2 ? 3 : 1;
            _speedLabel.text = "속도 x" + _speed;
            GameManager.Instance.Save.Settings.BattleSpeed = _speed;
        }

        private void OnFleePressed()
        {
            _fled = true;
            _panelDone = true;
            _retry = false;
        }

        // ------------------------------------------------------------------ playback

        private float Wait(float seconds) { return seconds / _speed; }

        private PartyPanel PanelOf(string unitId) { return _panels.Find(p => p.Unit.Id == unitId); }

        private EnemyCard CardOf(string unitId) { return _cards.Find(c => c.Unit.Id == unitId); }

        private string NameOf(string unitId)
        {
            var p = PanelOf(unitId);
            if (p != null) return p.Unit.Name;
            var c = CardOf(unitId);
            return c != null ? c.Unit.Name : "";
        }

        private string _cutInKey = "", _masteryNotice;

        private IEnumerator PlayEvents(List<BattleEvent> events)
        {
            _cutInKey = "";
            foreach (var ev in events)
            {
                if (_fled) yield break;
                if (ev.Kind != EventKind.Attack) _cutInKey = "";
                var actorPanel = PanelOf(ev.ActorId);
                bool byParty = actorPanel != null;
                string actorName = NameOf(ev.ActorId);

                switch (ev.Kind)
                {
                    case EventKind.Attack:
                        if (ev.HitIndex == 0) _log.text = actorName + "의 " + SkillLabel(ev, actorPanel) + (ev.HitCount > 1 ? " (" + ev.HitCount + "연타)" : "");
                        if (ev.Joint && _cutInKey != "joint")
                        {
                            _cutInKey = "joint";
                            yield return SkillCutIn(_jointName, true);
                        }
                        else if (byParty && ev.HitIndex == 0 && !ev.Joint)
                        {
                            string key = ev.ActorId + "|" + ev.SkillId;
                            if (key != _cutInKey)
                            {
                                _cutInKey = key;
                                var spec = actorPanel.Unit.Skills.Find(x => x.Id == ev.SkillId);
                                if (spec != null && spec.Id != "basic") yield return SkillCutIn(spec.Name, spec.SpiritCost >= 100);
                            }
                        }
                        if (byParty) yield return PartyHit(ev);
                        else yield return EnemyHit(ev);
                        break;
                    case EventKind.Guard:
                        SoundManager.Sfx("guard");
                        _log.text = ev.Message;
                        foreach (var p in _panels) if (p.Unit.Guarding) Float(p.Center, "방어!", GuardBlue, 52);
                        RefreshParty();
                        yield return new WaitForSeconds(Wait(SmallDelay));
                        break;
                    case EventKind.Meditate:
                        SoundManager.Sfx("heal");
                        _log.text = ev.Message;
                        if (actorPanel != null) Float(actorPanel.Center, "+" + ev.Heal, HpGreen, 60);
                        RefreshParty();
                        yield return new WaitForSeconds(Wait(SmallDelay));
                        break;
                    case EventKind.Heal:
                        SoundManager.Sfx("heal");
                        _log.text = ev.Message;
                        var healed = PanelOf(ev.TargetId);
                        if (healed != null) Float(healed.Center, "+" + ev.Heal, HpGreen, 70);
                        RefreshParty();
                        yield return new WaitForSeconds(Wait(StepDelay));
                        break;
                    case EventKind.Item:
                        SoundManager.Sfx("heal");
                        _log.text = ev.Message;
                        var itemTarget = PanelOf(ev.TargetId);
                        if (ev.Heal > 0 && itemTarget != null) Float(itemTarget.Center, "+" + ev.Heal, HpGreen, 70);
                        else if (actorPanel != null) Float(actorPanel.Center, "회복", QiBlue, 56);
                        RefreshParty();
                        yield return new WaitForSeconds(Wait(StepDelay));
                        break;
                    case EventKind.Evade:
                        SoundManager.Sfx("dodge");
                        _log.text = ev.Message;
                        if (actorPanel != null) Float(actorPanel.Center, "회피 태세", new Color(0.7f, 0.9f, 1f), 52);
                        RefreshParty();
                        yield return new WaitForSeconds(Wait(SmallDelay));
                        break;
                    case EventKind.Charge:
                        SoundManager.Sfx("charge");
                        _log.text = ev.Message;
                        var card = CardOf(ev.ActorId);
                        if (card != null) { Float(card.Center, "!!", UiFactory.Red, 90); yield return Shake(card.Root, 10f); }
                        RefreshCards();
                        yield return new WaitForSeconds(Wait(SmallDelay));
                        break;
                    case EventKind.StunSkip:
                        _log.text = ev.Message;
                        var frozen = CardOf(ev.ActorId);
                        if (frozen != null) Float(frozen.Center, "굳음", GuardBlue, 64);
                        RefreshCards();
                        yield return new WaitForSeconds(Wait(SmallDelay));
                        break;
                }
            }
            RefreshParty();
            RefreshCards();
        }

        private string SkillLabel(BattleEvent ev, PartyPanel actor)
        {
            if (ev.Joint) return _jointName;
            if (actor != null)
            {
                var s = actor.Unit.Skills.Find(x => x.Id == ev.SkillId);
                return s != null ? s.Name : "공격";
            }
            return ev.SkillId;
        }

        private IEnumerator PartyHit(BattleEvent ev)
        {
            var card = CardOf(ev.TargetId);
            if (card == null) yield break;
            var striker = PanelOf(ev.ActorId);
            if (striker != null && ev.HitIndex == 0) StartCoroutine(Lunge(striker.Root, new Vector2(130f, 0f)));
            SlashFx(card.Center, ev.HitIndex, ev.IsCrit, striker != null && striker.Unit.Skills.Find(x => x.Id == ev.SkillId) is SkillSpec sp && sp.SpiritCost >= 100);
            if (ev.IsCrit) { StartCoroutine(Punch(1.06f)); yield return new WaitForSeconds(Wait(0.07f)); }
            SoundManager.Sfx(ev.IsCrit ? "hit_crit" : "hit");
            if (ev.IsCrit) SoundManager.Haptic(40);
            Float(card.Center + new Vector2(UnityEngine.Random.Range(-0.05f, 0.05f), 0.02f * ev.HitIndex), ev.Damage + (ev.IsCrit ? "!" : ""), ev.IsCrit ? UiFactory.Gold : UiFactory.PaperText, ev.IsCrit ? 84 : 64);
            if (ev.Harmony) Float(card.Center + new Vector2(0f, 0.08f), "합격!", UiFactory.Gold, 80);
            if (ev.Interrupted) Float(card.Center + new Vector2(0f, -0.07f), "차단!", UiFactory.Gold, 72);
            else if (ev.Stunned) Float(card.Center + new Vector2(0f, -0.07f), "굳음", GuardBlue, 60);
            card.Hp.SetPct(card.Unit.Stats.MaxHp > 0 ? (float)ev.TargetHpAfter / card.Unit.Stats.MaxHp : 0f);
            card.HpText.text = ev.TargetHpAfter + " / " + card.Unit.Stats.MaxHp;
            StartCoroutine(Shake(card.Root, ev.IsCrit ? 22f : 14f));
            if (ev.TargetDefeated) { StartCoroutine(Collapse(card)); card.Intent.text = ev.Retreated ? "물러났다" : "쓰러졌다"; if (ev.Retreated) _log.text = ev.Message; }
            RefreshParty();
            yield return new WaitForSeconds(Wait(ev.HitCount > 1 ? HitDelay : StepDelay));
        }

        private IEnumerator EnemyHit(BattleEvent ev)
        {
            var attacker = CardOf(ev.ActorId);
            if (attacker != null && ev.HitIndex == 0) StartCoroutine(Lunge(attacker.Root, new Vector2(-130f, 0f)));
            var victim = PanelOf(ev.TargetId);
            if (victim != null && !ev.Dodged) SlashFx(victim.Center, ev.HitIndex, ev.IsCrit, false, true);
            if (victim != null)
            {
                if (ev.Dodged)
                {
                    SoundManager.Sfx("dodge");
                    Float(victim.Center, "회피!", new Color(0.7f, 0.9f, 1f), 70);
                }
                else
                {
                    SoundManager.Sfx(ev.Guarded ? "block" : "hit_taken");
                    if (!ev.Guarded) SoundManager.Haptic(ev.IsCrit ? 60 : 25);
                    Float(victim.Center, "-" + ev.Damage + (ev.Guarded ? " 방어" : ev.IsCrit ? "!" : ""), ev.Guarded ? GuardBlue : UiFactory.Red, ev.IsCrit ? 84 : 66);
                    victim.Hp.SetPct((float)ev.TargetHpAfter / Math.Max(1, victim.Unit.Stats.MaxHp));
                    victim.HpText.text = ev.TargetHpAfter + "/" + victim.Unit.Stats.MaxHp;
                    StartCoroutine(Shake(victim.Root, ev.Guarded ? 6f : 16f));
                    StartCoroutine(Flash(ev.Guarded ? 0.12f : 0.30f));
                    if (ev.TargetDefeated) victim.Status.text = "쓰러짐";
                    if (ev.Rescued)
                    {
                        Float(victim.Center + new Vector2(0f, 0.07f), "구원!", UiFactory.Gold, 84);
                        _log.text = ev.Message;
                        SoundManager.Sfx("levelup");
                    }
                }
            }
            yield return new WaitForSeconds(Wait(ev.HitCount > 1 ? HitDelay : StepDelay));
        }

        // ------------------------------------------------------------------ effects

        private void Float(Vector2 at, string text, Color color, int size)
        {
            var rt = UiFactory.Rect("Float", _fx, at, at);
            rt.sizeDelta = new Vector2(520f, 140f);
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
            StartCoroutine(FloatUp(rt, t));
        }

        private IEnumerator FloatUp(RectTransform rt, Text t)
        {
            float duration = 0.7f / _speed;
            Vector2 start = rt.anchoredPosition;
            for (float e = 0f; e < duration; e += Time.deltaTime)
            {
                float k = e / duration;
                rt.anchoredPosition = start + new Vector2(0f, 150f * k);
                var c = t.color; c.a = 1f - k * k; t.color = c;
                yield return null;
            }
            Destroy(rt.gameObject);
        }

        private IEnumerator Shake(RectTransform rt, float amount)
        {
            if (rt == null) yield break;
            Vector2 origin = rt.anchoredPosition;
            for (int i = 0; i < 6; i++)
            {
                if (rt == null) yield break;
                rt.anchoredPosition = origin + new Vector2(UnityEngine.Random.Range(-amount, amount), UnityEngine.Random.Range(-amount, amount) * 0.5f);
                yield return new WaitForSeconds(0.02f / _speed);
            }
            if (rt != null) rt.anchoredPosition = origin;
        }

        private IEnumerator Flash(float peak)
        {
            for (float e = 0f; e < 0.25f; e += Time.deltaTime)
            {
                var c = _flash.color; c.a = peak * (1f - e / 0.25f); _flash.color = c;
                yield return null;
            }
            var end = _flash.color; end.a = 0f; _flash.color = end;
        }

        // ------------------------------------------------------------------ strike effects

        private IEnumerator Lunge(RectTransform rt, Vector2 delta)
        {
            if (rt == null) yield break;
            Vector2 origin = rt.anchoredPosition;
            float out_ = 0.09f / _speed, back = 0.14f / _speed;
            for (float e = 0f; e < out_; e += Time.deltaTime)
            {
                if (rt == null) yield break;
                rt.anchoredPosition = origin + delta * Mathf.SmoothStep(0f, 1f, e / out_);
                yield return null;
            }
            for (float e = 0f; e < back; e += Time.deltaTime)
            {
                if (rt == null) yield break;
                rt.anchoredPosition = origin + delta * (1f - Mathf.SmoothStep(0f, 1f, e / back));
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = origin;
        }

        /// <summary>Slashing streaks plus a spark burst at the hit point; drawn from plain rectangles so no art is needed.</summary>
        private void SlashFx(Vector2 at, int hitIndex, bool crit, bool ultimate, bool enemySide = false)
        {
            Color main = enemySide ? new Color(1f, 0.45f, 0.35f) : ultimate ? UiFactory.Gold : crit ? new Color(1f, 0.92f, 0.6f) : new Color(0.95f, 0.97f, 1f);
            float baseAngle = (hitIndex % 2 == 0 ? -35f : 35f) + UnityEngine.Random.Range(-12f, 12f);
            int streaks = ultimate ? 3 : crit ? 2 : 1;
            for (int i = 0; i < streaks; i++)
            {
                var rt = UiFactory.Rect("Slash", _fx, at, at);
                rt.sizeDelta = new Vector2(ultimate ? 560f : 400f, crit ? 16f : 10f);
                rt.localRotation = Quaternion.Euler(0f, 0f, baseAngle + i * 22f);
                var img = rt.gameObject.AddComponent<Image>();
                img.color = main; img.raycastTarget = false;
                StartCoroutine(StreakAnim(rt, img));
            }
            for (int i = 0; i < (crit ? 8 : 4); i++)
            {
                var rt = UiFactory.Rect("Spark", _fx, at, at);
                rt.sizeDelta = new Vector2(UnityEngine.Random.Range(14f, 30f), 6f);
                float a = UnityEngine.Random.Range(0f, 360f);
                rt.localRotation = Quaternion.Euler(0f, 0f, a);
                var img = rt.gameObject.AddComponent<Image>();
                img.color = main; img.raycastTarget = false;
                StartCoroutine(SparkAnim(rt, img, a * Mathf.Deg2Rad));
            }
        }

        private IEnumerator StreakAnim(RectTransform rt, Image img)
        {
            float d = 0.22f / _speed;
            for (float e = 0f; e < d; e += Time.deltaTime)
            {
                if (rt == null) yield break;
                float k = e / d;
                rt.localScale = new Vector3(Mathf.Min(1f, k * 4f), 1f - k * 0.6f, 1f);
                var c = img.color; c.a = 1f - k * k; img.color = c;
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        private IEnumerator SparkAnim(RectTransform rt, Image img, float angle)
        {
            float d = 0.30f / _speed;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float dist = UnityEngine.Random.Range(90f, 190f);
            for (float e = 0f; e < d; e += Time.deltaTime)
            {
                if (rt == null) yield break;
                float k = e / d;
                rt.anchoredPosition = dir * dist * (1f - (1f - k) * (1f - k));
                var c = img.color; c.a = 1f - k; img.color = c;
                yield return null;
            }
            if (rt != null) Destroy(rt.gameObject);
        }

        /// <summary>Brief zoom of the enemy field on a critical hit.</summary>
        private IEnumerator Punch(float scale)
        {
            float d = 0.18f / _speed;
            for (float e = 0f; e < d; e += Time.deltaTime)
            {
                float k = e / d;
                float s = 1f + (scale - 1f) * (1f - k);
                _cardsRoot.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            _cardsRoot.localScale = Vector3.one;
        }

        /// <summary>Defeated enemy staggers, sinks and fades instead of just dimming.</summary>
        private IEnumerator Collapse(EnemyCard card)
        {
            SoundManager.Sfx("enemy_down");
            float d = 0.45f / _speed;
            Vector2 origin = card.Root.anchoredPosition;
            for (float e = 0f; e < d; e += Time.deltaTime)
            {
                if (card.Root == null) yield break;
                float k = e / d;
                card.Root.anchoredPosition = origin + new Vector2(Mathf.Sin(k * 40f) * 10f * (1f - k), -40f * k);
                SetAlpha(card.Body, Mathf.Lerp(0.68f, 0.15f, k));
                if (card.Art != null) SetAlpha(card.Art, Mathf.Lerp(1f, 0.25f, k));
                yield return null;
            }
            if (card.Root != null) card.Root.anchoredPosition = origin + new Vector2(0f, -40f);
            SetAlpha(card.Body, 0.15f);
            if (card.Art != null) SetAlpha(card.Art, 0.25f);
        }

        /// <summary>Skill-name flash before the strike; 비기 get a full-width gold cut-in with a dark flash.</summary>
        private IEnumerator SkillCutIn(string skillName, bool ultimate)
        {
            SoundManager.Sfx(ultimate ? "ultimate" : "skill");
            if (ultimate) SoundManager.Haptic(120);
            _banner.fontSize = ultimate ? 96 : 72;
            _banner.color = ultimate ? UiFactory.Gold : UiFactory.PaperText;
            _banner.text = ultimate ? "\u300e " + skillName + " \u300f" : skillName;
            _bannerBg.color = new Color(0f, 0f, 0f, ultimate ? 0.85f : 0.55f);
            if (ultimate) StartCoroutine(Punch(1.12f));
            yield return new WaitForSeconds(Wait(ultimate ? 0.75f : 0.35f));
            _banner.text = "";
            _banner.fontSize = 64;
            _banner.color = UiFactory.Gold;
            _bannerBg.color = new Color(0f, 0f, 0f, 0f);
        }

        private IEnumerator Banner(string text, float seconds)
        {
            _banner.text = text;
            _bannerBg.color = new Color(0f, 0f, 0f, 0.65f);
            yield return new WaitForSeconds(seconds / _speed);
            _banner.text = "";
            _bannerBg.color = new Color(0f, 0f, 0f, 0f);
        }

        private static void SetAlpha(Image img, float a)
        {
            var c = img.color; c.a = a; img.color = c;
        }

        // ------------------------------------------------------------------ panels

        private IEnumerator ShowStory(string title, string[] lines)
        {
            SetCommandsVisible(false);
            _panel.SetActive(false);
            _storySkip = false;
            string left = StoryLine.Protagonist, right = null;
            _storyRoot.SetActive(true);
            for (int i = 0; i < lines.Length && !_fled && !_storySkip; i++)
            {
                var line = StoryLine.Parse(lines[i]);
                if (line.IsDialogue && !line.IsProtagonist) right = line.Speaker;
                _storyTitle.text = title;
                ShowPortrait(0, line.IsDialogue || right != null ? left : null, line.IsProtagonist);
                ShowPortrait(1, right, line.IsDialogue && !line.IsProtagonist);

                bool rightSide = line.IsDialogue && !line.IsProtagonist;
                _namePlate.gameObject.SetActive(line.IsDialogue);
                _namePlate.anchorMin = new Vector2(rightSide ? 0.60f : 0.05f, 0.268f);
                _namePlate.anchorMax = new Vector2(rightSide ? 0.95f : 0.40f, 0.330f);
                _nameTag.text = line.IsDialogue ? line.Speaker : "";
                _nameTag.color = line.IsProtagonist ? UiFactory.Gold : UiFactory.PaperText;
                _storyText.fontStyle = line.IsDialogue ? FontStyle.Bold : FontStyle.Italic;
                _storyText.color = line.IsDialogue ? UiFactory.PaperText : new Color(0.80f, 0.78f, 0.70f, 1f);
                _storyText.text = "";
                _panelDone = false;

                // Typewriter: a tap while it types shows the full line; the next tap moves on.
                for (int c = 1; c <= line.Text.Length && !_panelDone; c++)
                {
                    _storyText.text = line.Text.Substring(0, c);
                    yield return new WaitForSeconds(0.022f);
                }
                if (_storySkip) break;
                _storyText.text = line.Text;
                _panelDone = false;
                while (!_panelDone) yield return null;
            }
            _storyRoot.SetActive(false);
            _storySkip = false;
        }

        /// <summary>Pause between the fights of a stage: shows the party's state and waits for the player to challenge the next fight or step back.</summary>
        private IEnumerator ShowHub(StageData stage, int doneIndex)
        {
            SetCommandsVisible(false);
            SetPanelMode(false);
            int next = doneIndex + 1;
            bool boss = next == stage.Encounters.Length - 1;
            var sb = new System.Text.StringBuilder();
            sb.Append("전투 ").Append(doneIndex + 1).Append("/").Append(stage.Encounters.Length).Append(" 승리!\n");
            foreach (var m in _party) sb.Append(m.Name).Append("  체력 ").Append(m.Hp).Append("/").Append(m.Stats.MaxHp).Append("  내공 ").Append(m.Qi).Append("\n");
            sb.Append("\n다음은 ").Append(boss ? "결전이다." : "전투 " + (next + 1) + "이다.");
            var trial = stage.TrialFor(next);
            if (trial != null) sb.Append("\n[").Append(trial.Title).Append("] ").Append(trial.Brief);
            sb.Append("\n준비가 되면 도전하자. 물러나도 여기서부터 이어서 할 수 있다.");

            _panelTitle.text = stage.Name;
            _panelText.text = sb.ToString();
            var nextLabel = _panelNext.GetComponentInChildren<Text>();
            var backLabel = _panelBack.GetComponentInChildren<Text>();
            string nextOld = nextLabel.text, backOld = backLabel.text;
            nextLabel.text = boss ? "결전에 도전" : "다음 전투 도전";
            backLabel.text = "물러나기 (저장)";
            _panelNext.SetActive(true);
            _panelBack.SetActive(true);
            _panelRetry.SetActive(false);
            // Next and Back share the row: shrink Next to the left half while the hub is up.
            var nextRt = (RectTransform)_panelNext.transform;
            var oldMin = nextRt.anchorMin; var oldMax = nextRt.anchorMax;
            nextRt.anchorMin = new Vector2(0.06f, 0.05f); nextRt.anchorMax = new Vector2(0.62f, 0.20f);
            var backRt = (RectTransform)_panelBack.transform;
            var backMin = backRt.anchorMin; var backMax = backRt.anchorMax;
            backRt.anchorMin = new Vector2(0.66f, 0.05f); backRt.anchorMax = new Vector2(0.94f, 0.20f);
            _panel.SetActive(true);
            _panelDone = false;
            _leave = false;
            while (!_panelDone) yield return null;
            _panel.SetActive(false);
            nextRt.anchorMin = oldMin; nextRt.anchorMax = oldMax;
            backRt.anchorMin = backMin; backRt.anchorMax = backMax;
            nextLabel.text = nextOld; backLabel.text = backOld;
            _panelBack.SetActive(false);
        }

        private IEnumerator ShowResult(string title, string text, bool defeat, bool allowRetry = true)
        {
            SetCommandsVisible(false);
            SetPanelMode(false);
            SoundManager.Sfx(defeat ? "lose" : "clear");
            if (defeat) SoundManager.Haptic(200);
            _panelTitle.text = title;
            _panelText.text = text;
            _panelNext.SetActive(!defeat);
            _panelRetry.SetActive(defeat && allowRetry);
            _panelBack.SetActive(defeat);
            _panel.SetActive(true);
            _panelDone = false;
            _retry = false;
            _leave = false;
            while (!_panelDone) yield return null;
            _panel.SetActive(false);
        }
    }
}
