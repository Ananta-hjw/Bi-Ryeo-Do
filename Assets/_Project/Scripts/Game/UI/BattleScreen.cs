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
    public partial class BattleScreen : MonoBehaviour
    {
        private const float StepDelay = 0.42f, HitDelay = 0.16f, SmallDelay = 0.30f;
        private static readonly Color Backdrop = new Color(0.025f, 0.055f, 0.06f, 1f);
        private static readonly Color QiBlue = new Color(0.30f, 0.50f, 0.80f, 1f);
        private static readonly Color HpGreen = new Color(0.30f, 0.62f, 0.34f, 1f);
        private static readonly Color GuardBlue = new Color(0.5f, 0.7f, 0.95f, 1f);
        private static readonly Color Disabled = new Color(0.25f, 0.25f, 0.24f, 1f);

        private sealed class EnemyCard
        {
            public Combatant Unit;
            public RectTransform Root;
            public Image Frame, Art, IntentBack;
            public WuxiaGlyph TargetFrame;
            public int ShownHp, ShownPhase;
            public Text Name, Glyph, Intent, HpText, QiText, SpiritText, Badge, Realm, ActionBadge;
            public UiFactory.Bar Hp, Qi, Spirit;
            public Vector2 Center;
            public EnemyData Data;
        }

        private sealed class PartyPanel
        {
            public Combatant Unit;
            public RectTransform Root;
            public Image Frame, Art;
            public WuxiaGlyph ActiveFrame;
            public Text Name, Status, HpText, QiText, SpiritText, ActionBadge;
            public Text FigureGlyph;
            public UiFactory.Bar Hp, Qi, Spirit;
            public Vector2 Center;
            public int ShownHp, ShownQi, ShownSpirit;   // as drawn; lag behind the unit while events are animated
        }

        private sealed class CommandButton
        {
            public Button Button;
            public Image Image;
            public Text Label, Detail;
            public WuxiaGlyph Fallback;
            public GameObject Root;
            public Image Icon;   // martial art picture on the left of the button (hidden when there is none)
        }

        private RectTransform _cardsRoot, _partyRoot, _fx;
        private BasicAttackMotion _basicMotion;
        private Image _backdropImage;
        private Text _stageLabel, _speedLabel, _log, _banner;
        private Image _flash, _bannerBg;
        private readonly List<Image> _turnChips = new List<Image>();
        private readonly List<Text> _turnLabels = new List<Text>();
        private readonly List<EnemyCard> _cards = new List<EnemyCard>();
        private readonly List<PartyPanel> _panels = new List<PartyPanel>();
        private readonly CommandButton[] _commands = new CommandButton[10];   // 0 attack, 1-4 arts, 5 guard, 6 meditate, 7 item, 8 joint, 9 필살기

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
        private Text _storyTitle, _storyText, _storyProgress;
        private Image _storyBackdrop;
        private RectTransform _namePlate;
        private bool _storySkip;

        private InteractiveBattle _battle;
        private List<Combatant> _party = new List<Combatant>();
        // Aiming: pressing an attack first lights up who can and cannot be hit; the attack is only used once a hittable enemy is tapped.
        private SkillSpec _aimSkill;
        private Combatant _aimPartner;   // set while aiming a 합공 (the ally who strikes with the protagonist)
        private bool Aiming { get { return _aimSkill != null; } }
        // Ally aiming: healing arts and healing items ask which ally to heal before they are used.
        private Func<Combatant, PlayerCommand> _allyAim;
        private bool AimingAlly { get { return _allyAim != null; } }

        private bool _hasCommand, _panelDone, _retry, _leave, _fled, _itemMode, _jointMode, _drawn;
        private readonly List<Combatant> _jointPartners = new List<Combatant>();
        private string _jointName = "합공";
        private PlayerCommand _command;
        private int _speed = 1;
        /// <summary>Every battle animation runs at this share of the chosen x1/x2/x3 speed (0.8 = 80%, a little slower than the raw setting).</summary>
        private const float PaceScale = 0.8f;
        private float Pace { get { return _speed * PaceScale; } }
        private Action _onClose;
        private bool _built;
        private RectTransform _root;
        private bool _fleeAsking;   // the 포기 warning is open
        private bool _promotionRun;  // a 승급 비무 is running (its 포기 warning mentions the fee)

        public void Build(RectTransform root)
        {
            if (_built) return;
            _built = true;
            _root = root;
            _basicMotion = root.gameObject.AddComponent<BasicAttackMotion>();

            UiFactory.Panel("Backdrop", root, Vector2.zero, Vector2.one, Backdrop).raycastTarget = true;   // swallows taps so nothing underneath (the 강호 tab) is pressed during the battle
            _backdropImage = UiFactory.Panel("StageArt", root, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0f));
            _backdropImage.preserveAspect = false;
            UiFactory.Panel("Dim", root, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.35f)); // keeps text readable over artwork
            BuildBoard(root);

            _cardsRoot = UiFactory.Rect("Cards", root, Vector2.zero, Vector2.one);
            _partyRoot = UiFactory.Rect("Party", root, Vector2.zero, Vector2.one);
            BuildBattleHud(root);

            // Effects layers (above everything except panels)
            _fx = UiFactory.Rect("Fx", root, Vector2.zero, Vector2.one);
            _flash = UiFactory.Panel("Flash", root, Vector2.zero, Vector2.one, new Color(0.8f, 0.1f, 0.1f, 0f));
            _bannerBg = UiFactory.Panel("BannerBg", root, new Vector2(FieldLeft, .45f), new Vector2(FieldRight, .57f), new Color(0f, 0f, 0f, 0f));
            _banner = UiFactory.Label("Banner", root, new Vector2(FieldLeft, .45f), new Vector2(FieldRight, .57f), "", 64, UiFactory.Gold);

            BuildPanel(root);
            BuildStory(root);
            BuildTip(root);
            BuildPick(root);
            BuildInputGuard(root);
        }

        private GameObject _inputGuard;

        /// <summary>An invisible full-screen blocker above everything. It is switched on for a moment after the story or a result window closes, so a player who keeps tapping to read quickly does not hit the buttons that appear underneath.</summary>
        private void BuildInputGuard(RectTransform root)
        {
            var rect = UiFactory.Rect("InputGuard", root, Vector2.zero, Vector2.one);
            var img = rect.gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);
            img.raycastTarget = true;
            _inputGuard = rect.gameObject;
            _inputGuard.SetActive(false);
        }

        private IEnumerator GuardTaps(float seconds = 0.5f)
        {
            if (_inputGuard == null) yield break;
            _inputGuard.transform.SetAsLastSibling();
            _inputGuard.SetActive(true);
            yield return new WaitForSecondsRealtime(seconds);
            _inputGuard.SetActive(false);
        }

        private void BuildPanel(RectTransform root)
        {
            var host = UiFactory.Panel("Panel", root, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, .75f));
            host.raycastTarget = true;
            _panel = host.gameObject;
            var rect = UiFactory.Rect("Card", host.transform, new Vector2(.12f, .08f), new Vector2(.88f, .92f));
            var bg = rect.gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.07f, 0.11f, 0.97f);
            UiFactory.Frame(rect, UiFactory.Gold, 3f);
            UiFactory.Panel("TitleLine", rect, new Vector2(0.06f, 0.895f), new Vector2(0.94f, 0.899f), UiFactory.GoldDim);
            _panelTitle = UiFactory.Label("Title", rect, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.99f), "", 38, UiFactory.Gold);

            var text = UiFactory.ScrollBody(UiFactory.Label("Text", rect, new Vector2(0.06f, 0.26f), new Vector2(0.94f, 0.80f), "", 36, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 36);
            _panelText = text;
            _panelTextRect = UiFactory.ReadingViewport(text);
            _panelNext = UiFactory.Button("Next", rect, new Vector2(0.30f, 0.05f), new Vector2(0.70f, 0.20f), "다음", UiFactory.Red, () => _panelDone = true, 40).gameObject;
            _panelRetry = UiFactory.Button("Retry", rect, new Vector2(0.06f, 0.05f), new Vector2(0.48f, 0.20f), "다시 도전", UiFactory.Red, () => { _retry = true; _panelDone = true; }, 36).gameObject;
            _panelBack = UiFactory.Button("Back", rect, new Vector2(0.52f, 0.05f), new Vector2(0.94f, 0.20f), "강호로", UiFactory.InkSoft, () => { _retry = false; _leave = true; _panelDone = true; }, 36).gameObject;
            _panel.SetActive(false);
        }

        /// <summary>Result / hub panels: plain centred message in the middle panel.</summary>
        private void SetPanelMode(bool story)
        {
            _panelTextRect.anchorMin = new Vector2(0.06f, 0.26f);
            _panelTextRect.anchorMax = new Vector2(0.94f, 0.87f);
            _panelText.alignment = TextAnchor.UpperLeft;
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

            // An opaque layer showing the stage art, so the battle HUD underneath (stage label, speed / flee buttons, turn order, cards) cannot show through the story.
            _storyBackdrop = UiFactory.Panel("StoryBackdrop", rect, Vector2.zero, Vector2.one, Backdrop);
            _storyBackdrop.preserveAspect = false;
            _storyBackdrop.raycastTarget = false;
            _storyBackdrop.transform.SetAsFirstSibling();
            UiFactory.Panel("StoryDim", _storyBackdrop.transform, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.35f)).raycastTarget = false;
            UiFactory.Panel("ChapterBand", rect, new Vector2(0f, 0.915f), Vector2.one, new Color(0.02f, 0.03f, 0.05f, 0.85f)).raycastTarget = false;

            _storyTitle = UiFactory.Label("Chapter", rect, new Vector2(0.04f, 0.925f), new Vector2(0.72f, 0.985f), "", 34, UiFactory.Gold, TextAnchor.MiddleLeft);
            UiFactory.Panel("ChapterLine", rect, new Vector2(0.04f, 0.922f), new Vector2(0.72f, 0.925f), UiFactory.GoldDim);
            UiFactory.Button("Skip", rect, new Vector2(0.76f, 0.915f), new Vector2(0.97f, 0.99f), "건너뛰기", UiFactory.InkSoft, () => { _storySkip = true; _panelDone = true; }, 28);

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
            var box = UiFactory.Panel("Box", rect, new Vector2(.055f, .035f), new Vector2(.945f, .37f), new Color(0.04f, 0.06f, 0.10f, 0.92f));
            UiFactory.Frame(box.transform, UiFactory.Gold, 3f);
            _storyText = UiFactory.Label("Text", rect, new Vector2(.085f, .11f), new Vector2(.915f, .31f), "", 36, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal);
            UiFactory.Body(_storyText, 1.3f);
            _storyText.supportRichText = false;
            _storyProgress = UiFactory.Label("ReadingProgress", rect, new Vector2(.085f, .045f), new Vector2(.915f, .09f), "", 28, UiFactory.Dim, TextAnchor.MiddleRight, FontStyle.Normal);

            // Name plate sits on the top edge of the box; moves to the right for the other speaker.
            var plate = UiFactory.Panel("NamePlate", rect, new Vector2(.07f, .34f), new Vector2(.32f, .405f), new Color(0.20f, 0.16f, 0.07f, 0.97f));
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
                // Two halves with a small gap so the speakers never overlap (the speaking one is drawn 3% larger)
                rt.anchorMin = side == 0 ? new Vector2(0.01f, 0.27f) : new Vector2(0.51f, 0.27f);
                rt.anchorMax = side == 0 ? new Vector2(0.49f, .895f) : new Vector2(0.99f, .895f);
            }
            else
            {
                rt.anchorMin = side == 0 ? new Vector2(0.06f, 0.34f) : new Vector2(0.58f, 0.34f);
                rt.anchorMax = side == 0 ? new Vector2(0.42f, 0.84f) : new Vector2(0.94f, 0.84f);
            }
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Sprite PortraitFor(string name) { return ArtLibrary.Feathered(ArtLibrary.Portrait(name)); }   // soft edges so full-bleed pictures blend into the backdrop

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
            if (_inputGuard != null) _inputGuard.SetActive(false);   // a guard left on by a window that closed the overlay must not block the next fight
            _onClose = onClose;
            CancelBasicMotion();
            StopAllCoroutines();
            HideFightRewards();
            ApplySavedSpeed();
            StartCoroutine(RunStage(stage));
        }

        // ------------------------------------------------------------------ 전투 선택 (replays of cleared stages)

        private int _lastFightNumber;
        private GameObject _pickRoot;
        private Text _pickTitle, _pickToggleLabel;
        private readonly List<Button> _pickButtons = new List<Button>();
        private readonly List<Text> _pickLabels = new List<Text>();
        private bool _pickDone, _pickSingle;
        private int _pickIndex;

        private void BuildPick(RectTransform root)
        {
            var rect = UiFactory.Rect("Pick", root, Vector2.zero, Vector2.one);
            _pickRoot = rect.gameObject;
            rect.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);   // blocks taps underneath
            var card = UiFactory.Panel("Card", rect, new Vector2(.12f, .08f), new Vector2(.88f, .92f), new Color(0.05f, 0.07f, 0.11f, 0.98f));
            UiFactory.Frame(card.transform, UiFactory.Gold, 3f);
            _pickTitle = UiFactory.Label("Title", card.transform, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.99f), "", 38, UiFactory.Gold);
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                var btn = UiFactory.Button("Fight" + i, card.transform, new Vector2(.05f + (i % 2) * .46f, .70f - (i / 2) * .20f), new Vector2(.49f + (i % 2) * .46f, .88f - (i / 2) * .20f), "", UiFactory.InkSoft, () => { _pickIndex = index; _pickDone = true; }, 28);
                _pickButtons.Add(btn);
                _pickLabels.Add(btn.GetComponentInChildren<Text>());
            }
            var toggle = UiFactory.Button("Single", card.transform, new Vector2(0.05f, .15f), new Vector2(0.95f, .28f), "", UiFactory.InkSoft, () => { _pickSingle = !_pickSingle; UpdatePickToggle(); }, 28);
            _pickToggleLabel = toggle.GetComponentInChildren<Text>();
            UiFactory.Button("Cancel", card.transform, new Vector2(0.30f, .015f), new Vector2(0.70f, .135f), "강호로", UiFactory.InkSoft, () => { _pickIndex = -1; _pickDone = true; }, 30);
            _pickRoot.SetActive(false);
        }

        private void UpdatePickToggle()
        {
            _pickToggleLabel.text = _pickSingle ? "이 전투 하나만 하고 끝내기: 켜짐" : "이 전투 하나만 하고 끝내기: 꺼짐";
        }

        /// <summary>Lets the player choose which fight of a cleared stage to start from; returns the index or -1 for 강호로.</summary>
        private IEnumerator PickFight(StageData stage, int diff)
        {
            var save = GameManager.Instance.Save;
            SetCommandsVisible(false);
            _panel.SetActive(false);
            _pickTitle.text = "전투 선택 · " + stage.Name;
            for (int i = 0; i < _pickButtons.Count; i++)
            {
                bool has = i < stage.Encounters.Length;
                _pickButtons[i].gameObject.SetActive(has);
                if (!has) continue;
                var names = new List<string>();
                foreach (var id in stage.Encounters[i])
                {
                    var e = EarlyGameContent.FindEnemy(id);
                    string n = e != null ? e.Name : id;
                    if (!names.Contains(n)) names.Add(n);
                }
                int mask = StageStars.MaskOf(save, stage.Number, i, diff);
                _pickLabels[i].text = "전투 " + (i + 1) + (i == stage.Encounters.Length - 1 ? " (결전)" : "") + " · " + string.Join(", ", names) + "   " + StarGlyphs(mask);
            }
            UpdatePickToggle();
            _pickRoot.SetActive(true);
            _pickRoot.transform.SetAsLastSibling();
            _pickDone = false;
            _pickIndex = -1;
            while (!_pickDone) yield return null;
            _pickRoot.SetActive(false);
            StartCoroutine(GuardTaps());
        }

        private IEnumerator RunStage(StageData stage)
        {
            _promotionRun = false;
            var save = GameManager.Instance.Save;
            _fled = false;
            int diff = stage.IsTower || stage.IsQuest ? 0 : stage.Difficulty;
            string tag = StageStars.TagFor(diff);
            _stageLabel.text = stage.IsTower ? stage.Name : "스테이지 " + stage.Number + " · " + tag + stage.Name;
            var bg = ArtLibrary.Background(stage.Number) ?? ArtLibrary.Background(1);
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

            // A cleared stage can be replayed from any fight (optionally just that one); the first clear always runs from the start.
            int pickStart = 0;
            bool singleFight = false;
            bool clearedHere = !stage.IsQuest && !stage.IsTower && (diff == 0 ? save.ClearedStage >= stage.Number : StageStars.StageCleared(save, stage.Number, diff));
            if (!resume && clearedHere && stage.Encounters.Length > 1)
            {
                yield return PickFight(stage, diff);
                if (_pickIndex < 0) _fled = true;
                else { pickStart = _pickIndex; singleFight = _pickSingle; startIndex = pickStart; }
            }

            if (!resume && !_fled && pickStart == 0 && !singleFight)
            {
                var recap = !stage.IsTower && !stage.IsQuest && save.ClearedStage < stage.Number ? StoryBriefings.Briefing(stage.Number) : null;
                if (recap != null) yield return ShowStory(stage.Name + " — 이야기 배경", recap);
                var between = recap != null ? StoryBriefings.Interlude(stage.Number) : null;
                if (between != null) yield return ShowStory("막간 — " + between.Title, between.Lines);
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
                var materialLines = new List<string>();   // 강화 재료 found in each fight of this run, for the result window
                bool cleared = true;
                int newStars = 0;   // stars earned in this run (each fight has its own three)
                bool replay = !stage.IsQuest && !stage.IsTower && (diff == 0 ? save.ClearedStage >= stage.Number : StageStars.StageCleared(save, stage.Number, diff));
                bool towerFirst = stage.IsTower && stage.TowerFloor > save.TowerBest;
                bool singleDone = false;

                for (int i = startIndex; i < stage.Encounters.Length; i++)
                {
                    _stageLabel.text = stage.IsTower ? stage.Name : "스테이지 " + stage.Number + " · " + tag + stage.Name + "  (" + (i + 1) + "/" + stage.Encounters.Length + ")";
                    _lastFightNumber = i + 1;
                    yield return Banner(i == stage.Encounters.Length - 1 ? "결전!" : "전투 " + (i + 1), 0.7f);
                    yield return Fight(stage.Encounters[i], stage.TrialFor(i), stage);

                    if (_fled) { cleared = false; break; }
                    if (!_battle.PlayerWon) { cleared = false; break; }

                    // Every fight has its own stars: won, nobody fell, and fast enough.
                    _lastStarLine = "";
                    string repLine = "";
                    var found = new List<string>();   // 심득석 / 수련서 discovered by this win
                    bool firstWin = true;
                    int rewardMask = -1, rewardNewStars = 0;
                    var materialDrops = GearDrops.RollFight(save, stage, i, new SeededRandom(Environment.TickCount));
                    if (!stage.IsQuest && !stage.IsTower)
                    {
                        // Reputation, 수련서 and full 경험/두 come from the first win of each fight on each difficulty, so replaying one fight cannot farm them
                        // (losing a later fight and retrying the stage replays the fights already won).
                        firstWin = (StageStars.MaskOf(save, stage.Number, i, diff) & 1) == 0;
                        if (firstWin)
                        {
                            var rises = Factions.OnFightWon(save, stage.Number, diff);
                            string gains = Factions.GainText(stage.Number, diff);
                            if (gains.Length > 0) repLine = "평판 " + gains + (rises.Count > 0 ? "  (" + string.Join(", ", rises) + ")" : "");
                        }
                        int fightMask = StageStars.Evaluate(stage.Number, i, _fightActions, _fightFell, diff);
                        bool stageWasCleared = StageStars.StageCleared(save, stage.Number, diff);
                        int fightNew = StageStars.Record(save, stage.Number, i, diff, fightMask);
                        rewardMask = fightMask; rewardNewStars = fightNew;
                        int stones = StageStars.GrantClearInsight(save, stage.Number, diff, stageWasCleared);
                        if (stones > 0) { repLine += (repLine.Length > 0 ? "\n" : "") + "심득석 +" + stones; found.Add("심득석 x" + stones); }
                        if (!replay && firstWin) foreach (var scroll in Mastery.RollScrolls(save, new SeededRandom(Environment.TickCount), stage.Number, diff))
                        {
                            var scrollData = EarlyGameContent.FindItem(scroll.Id);
                            if (scrollData != null) found.Add(scrollData.Name);
                        }
                        newStars += fightNew;
                        _lastStarLine = FightStarLine(stage.Number, i, diff, fightMask, _fightActions, fightNew);
                    }

                    long exp = 0, doo = 0;
                    save.BattleWins++;
                    Daily.Record(save, DailyEvent.BattleWin);
                    if (!_fightFell) Daily.Record(save, DailyEvent.Flawless);
                    foreach (var id in stage.Encounters[i]) Codex.SeeEnemy(save, id);
                    Recommend.FightReward(save, stage, i, replay || !firstWin, out exp, out doo);   // a fight already won pays the replay rate
                    var drops = new List<ItemStack>();
                    var materialNames = new List<string>();
                    foreach (var materialDrop in materialDrops)
                    {
                        Inventory.Add(save, materialDrop.Id, materialDrop.Count);
                        drops.Add(materialDrop);
                        materialNames.Add(MaterialName(materialDrop.Id) + " " + materialDrop.Count + "개");
                    }
                    if (!stage.IsQuest && !stage.IsTower) materialLines.Add("전투 " + (i + 1) + ": " + (materialNames.Count > 0 ? string.Join(" · ", materialNames) : "없음"));
                    if (!stage.IsQuest && !stage.IsTower) for (int roll = 0; roll < StageStars.DropRolls(diff); roll++) drops.AddRange(RollDrops(stage.Encounters[i]));
                    CompanionGrowth.EnsureRecords(save);   // before the protagonist levels, so old saves' companions start at the old level
                    // Capture presentation snapshots before experience changes anyone's level.
                    var levelsBefore = new Dictionary<string, int> { { Inventory.Hero, save.Level } };
                    var statsBefore = new Dictionary<string, CombatStats>();
                    foreach (var member in _party) statsBefore[member.Id] = member.Stats;
                    foreach (var companion in EarlyGameContent.Companions)
                        if (companion.JoinStage <= save.HighestUnlockedStage)
                            levelsBefore[companion.Id] = CompanionGrowth.LevelOf(save, companion.Id);
                    int levels = Progression.AddExp(save, exp);
                    // Everyone who fought gains the same exp and levels on their own; whoever levels up is fully restored
                    var leveled = new List<string>();
                    var levelLines = new List<string>();
                    if (levels > 0) { leveled.Add("player"); levelLines.Add("비류연  Lv." + (save.Level - levels) + " → Lv." + save.Level + "  (체력·내공 가득)"); }
                    for (int k = 1; k < _party.Count; k++)
                    {
                        int before = CompanionGrowth.LevelOf(save, _party[k].Id);
                        int gained = CompanionGrowth.AddExp(save, _party[k].Id, exp);
                        if (gained > 0)
                        {
                            leveled.Add(_party[k].Id);
                            levelLines.Add(_party[k].Name + "  Lv." + before + " → Lv." + (before + gained) + "  (체력·내공 가득)");
                            var learner = EarlyGameContent.FindCompanion(_party[k].Id);
                            if (learner != null) levelLines.AddRange(CompanionGrowth.NewArtLines(learner, before, before + gained));
                        }
                    }
                    if (stage.Number > 0)   // companions who sat this fight out still gain half (not in the solo promotion duels)
                    {
                        var fielded = new List<string>();
                        foreach (var m in _party) fielded.Add(m.Id);
                        levelLines.AddRange(CompanionGrowth.ShareWithBench(save, fielded, exp));
                    }
                    save.Doo += doo;
                    gainedExp += exp; gainedDoo += doo;
                    GameManager.Instance.SaveNow();
                    if (leveled.Count > 0) RebuildPartyForNewLevel(stage, leveled);
                    SoundManager.Sfx(leveled.Count > 0 ? "levelup" : "win");
                    yield return ShowFightRewards(stage, i, rewardMask, rewardNewStars, exp, doo, drops, repLine, levels);
                    if (levelLines.Count > 0)
                        yield return ShowLevelUp(stage, levelsBefore, statsBefore);
                    if (found.Count > 0)
                    {
                        GameManager.Instance.SaveNow();
                        yield return ShowResult("발견!", "전투 도중 귀한 물건을 발견했습니다.\n\n" + string.Join("\n", found) + "\n\n행낭에서 확인할 수 있습니다.", false);
                    }
                    foreach (var m in _party) m.RestBetweenFights(Combatant.RestHealPct + Factions.Bonus(save, PerkKind.RestHeal));
                    RefreshParty();

                    if (singleFight && i < stage.Encounters.Length - 1) { singleDone = true; break; }

                    // Between fights the player decides when to go on (the next fight may be the boss).
                    if (i < stage.Encounters.Length - 1)
                    {
                        yield return ShowHub(stage, i, rewardMask, rewardNewStars);
                        if (_leave)
                        {
                            StageProgress.Store(save, stage.Number, i + 1, _party, diff);
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
                if (singleDone)
                {
                    yield return ShowResult("전투 " + (_lastFightNumber) + " 승리!", "획득  경험 " + gainedExp + " · 두 " + gainedDoo + MaterialSummary(materialLines) + (_lastStarLine.Length > 0 ? "\n" + _lastStarLine : "") + "\n\n다른 전투는 스테이지를 다시 눌러 고를 수 있습니다.", false);
                    break;
                }
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
                    if (towerFirst) Daily.Record(save, DailyEvent.TowerClear);   // replays of cleared floors do not count
                    GameManager.Instance.SaveNow();
                    yield return ShowResult(stage.TowerFloor + "층 돌파!", towerFirst
                        ? "획득  경험 " + gainedExp + " · 두 " + gainedDoo + (towerNote.Length > 0 ? "\n" + towerNote : "")
                        : "이미 돌파한 층입니다. (보상 없음)", false);
                }
                else if (cleared)
                {
                    bool firstClear = save.ClearedStage < stage.Number;
                    int highestBefore = save.HighestUnlockedStage;
                    save.ClearedStage = Math.Max(save.ClearedStage, stage.Number);
                    Daily.Record(save, DailyEvent.StageClear);
                    var joiners = Roster.NewJoiners(highestBefore, save.HighestUnlockedStage);
                    Roster.BenchNewcomers(save, joiners);
                    GameManager.Instance.SaveNow();
                    yield return ShowStory(stage.Name + " 돌파", stage.Outro);
                    var after = firstClear ? StoryBriefings.Aftermath(stage.Number) : null;
                    if (after != null) yield return ShowStory(stage.Name + " — 그 뒤", after);
                    string bondNote = AwardBonds(save);
                    var reward = Inventory.GrantStageReward(save, stage.Number);
                    int chests = StageStars.ClaimableCount(save);
                    GameManager.Instance.SaveNow();
                    yield return ShowStageRewards(stage, gainedExp, gainedDoo, newStars, reward, materialLines, bondNote, chests);
                    if (joiners.Count > 0)
                    {
                        var joinNames = new List<string>();
                        foreach (var j in joiners) joinNames.Add(j.Name);
                        SoundManager.Sfx("levelup");
                        yield return ShowResult("새 동료 합류!", string.Join(", ", joinNames) + (joiners.Count > 1 ? " 이(가) " : "이(가) ") + "파티에 합류했습니다.\n\n지금은 대기 상태입니다. 강호 탭의 「파티 편성」에서 출전 여부와 자리를 정해 주세요.", false);
                    }
                }
                else
                {
                    yield return ShowResult("패배...", "다시 도전하면 " + (pickStart > 0 ? "고른 전투(전투 " + (pickStart + 1) + ")" : "이 스테이지의 첫 전투") + "부터 시작합니다. (이미 얻은 경험과 두는 그대로 남습니다.)", true);
                    again = _retry;
                    resume = false;
                    startIndex = pickStart;
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

        // ------------------------------------------------------------------ story-only scenes (나예린 추억)

        /// <summary>Plays a scene in the visual-novel layer without any fight, then calls <paramref name="onClose"/>.</summary>
        public void OpenStory(string title, string[] lines, Action onClose)
        {
            if (_inputGuard != null) _inputGuard.SetActive(false);
            _onClose = onClose;
            CancelBasicMotion();
            StopAllCoroutines();
            HideFightRewards();
            StartCoroutine(RunStoryOnly(title, lines));
        }

        private IEnumerator RunStoryOnly(string title, string[] lines)
        {
            _fled = false;
            _backdropImage.sprite = null;
            _backdropImage.color = new Color(1f, 1f, 1f, 0f);
            ClearCards();
            ClearPanels();
            SetCommandsVisible(false);
            _log.text = "";
            _stageLabel.text = title;
            SoundManager.Bgm("lobby");
            yield return ShowStory(title, lines);
            _panel.SetActive(false);
            _banner.text = "";
            _bannerBg.color = new Color(0f, 0f, 0f, 0f);
            _onClose?.Invoke();
        }

        /// <summary>Opens a 승급 비무: the fee was already paid by the caller. Two solo duels against the offer's opponents.</summary>
        public void OpenPromotion(PromotionOffer offer, Action onClose)
        {
            if (_inputGuard != null) _inputGuard.SetActive(false);
            _onClose = onClose;
            CancelBasicMotion();
            StopAllCoroutines();
            HideFightRewards();
            StartCoroutine(RunPromotion(offer));
        }

        private IEnumerator RunPromotion(PromotionOffer offer)
        {
            _promotionRun = true;
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
                    else
                    {
                        Daily.Record(save, DailyEvent.BattleWin);
                        yield return Banner("승리!", 0.7f);
                    }
                    foreach (var m in _party) m.RestBetweenFights();
                    RefreshParty();
                }

                if (_fled) { SetCommandsVisible(false); break; }
                if (lost)
                {
                    bool canRetry = Promotion.CanAfford(save, offer);
                    yield return ShowResult("패배...", "입장료 " + offer.Fee.ToString("N0") + "두는 돌려받지 못합니다." + (canRetry ? "\n다시 도전하려면 입장료를 한 번 더 내야 합니다." : "\n두가 모자라 다시 도전할 수 없습니다."), true, canRetry);
                    if (_retry && Promotion.Pay(save, offer)) { GameManager.Instance.SaveNow(); again = true; }
                }
                else
                {
                    long doo = Promotion.Complete(save, offer, anyDraw);
                    int levels = Progression.AddExp(save, offer.ExpReward);
                    GameManager.Instance.SaveNow();
                    yield return ShowResult(Promotion.RankName(offer.TargetRank) + " 승급!",
                        (anyDraw ? "무승부가 섞여 상금이 절반입니다.\n" : "") + "상금 " + doo.ToString("N0") + "두 · 경험 " + offer.ExpReward + (levels > 0 ? "\nLv." + save.Level + " 상승!" : ""), false);
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
                foreach (var stack in Inventory.RollDrops(id, rng, 1f + Factions.Bonus(save, PerkKind.DropPct)))
                {
                    Inventory.Add(save, stack.Id, stack.Count);
                    all.Add(stack);
                }
            return all;
        }

        /// <summary>"강화 재료" block of the result window: one line per fight played in this run (fights skipped by a resume or a picked start are not listed).</summary>
        private static string MaterialName(string id)
        {
            var item = EarlyGameContent.FindItem(id);
            return item != null ? item.Name : id;
        }

        private static string MaterialSummary(List<string> lines)
        {
            return lines.Count > 0 ? "\n\n강화 재료\n" + string.Join("\n", lines) : "";
        }

                private static string DescribeStacks(List<ItemStack> stacks)
        {
            var names = new List<string>();
            foreach (var s in stacks) names.Add(Inventory.Describe(s));
            return string.Join(", ", names);
        }

        private int _fightActions;
        private bool _fightFell;

        private string _lastStarLine = "";

        private static string StarGlyphs(int mask)
        {
            return ((mask & 1) != 0 ? "★" : "☆") + ((mask & 2) != 0 ? "★" : "☆") + ((mask & 4) != 0 ? "★" : "☆");
        }

        /// <summary>"★★☆  (신속 14/9행동)  +1" for the fight that was just won: the stars of this run plus the reason for a missing one.</summary>
        private static string FightStarLine(int stageNumber, int fight, int difficulty, int mask, int actions, int gained)
        {
            string line = StarGlyphs(mask);
            var missed = new List<string>();
            if ((mask & 2) == 0) missed.Add("전원 생존 실패");
            if ((mask & 4) == 0) missed.Add("신속 " + actions + "/" + StageStars.FastLimit(stageNumber, fight, difficulty) + "행동");
            if (missed.Count > 0) line += "  (" + string.Join(", ", missed) + ")";
            if (gained > 0) line += "  새 별 +" + gained;
            return line;
        }

        private IEnumerator Fight(string[] enemyIds, TrialRule trial, StageData stage)
        {
            var data = new EnemyData[enemyIds.Length];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = stage.EnemyFor(enemyIds[i]);
            }
            if (trial != null)
            {
                yield return Banner("[" + trial.Title + "]\n" + trial.Brief, 2.4f);
                // the full rule once, before the first fight of each trial kind (always in 도움말 afterwards)
                string topic = Tutorial.TrialTopic(trial.Kind);
                if (topic != null && HelpPanel.Instance != null) yield return HelpPanel.Instance.ShowOnce(topic);
            }
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
            RefreshParty();
            RefreshCards();

            yield return PlayEvents(_battle.Begin());

            while (!_battle.IsOver && !_fled)
            {
                EndAim();
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
                    bool anyStatus = false;
                    foreach (var u in _party) if (u.Statuses.Count > 0) anyStatus = true;
                    foreach (var c in _cards) if (c.Unit.Statuses.Count > 0) anyStatus = true;
                    string hint = !Tutorial.IsDone(save0, "battle") ? "battle" : !Tutorial.IsDone(save0, "reach") ? "reach" : _jointPartners.Count > 0 && !Tutorial.IsDone(save0, "joint") ? "joint"
                        : anyStatus && !Tutorial.IsDone(save0, "status") ? "status" : _phaseSeen && !Tutorial.IsDone(save0, "phase") ? "phase" : null;
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
                    Daily.Record(GameManager.Instance.Save, DailyEvent.SkillUse);
                    if (Progression.IsUltimate(_command.Skill)) Daily.Record(GameManager.Instance.Save, DailyEvent.UltimateUse);
                    int rose = Mastery.RecordUse(GameManager.Instance.Save, _command.Skill.Id);
                    GameManager.Instance.SaveNow();
                    if (rose >= 0) SoundManager.Sfx("levelup");
                    if (rose >= 0) _masteryNotice = _command.Skill.Name + " 숙련 " + Mastery.LevelNames[rose] + "!" + (rose == Mastery.VariantLevel ? "  변초 해금 (무공 탭)" : "");
                }
                if (_command.Kind == CommandKind.Joint) Daily.Record(GameManager.Instance.Save, DailyEvent.JointUse);
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
            _fightActions = actions;
            _fightFell = _party.Exists(m => !m.IsAlive);
            if (trial != null && _battle.TrialFailed) _log.text = _battle.TrialFailReason;
            if (turnLimit > 0 || trial != null) _stageLabel.text = label;

            RefreshParty();
            RefreshCards();
            SetCommandsVisible(false);
            yield return new WaitForSeconds(Wait(0.25f));
        }

        // ------------------------------------------------------------------ setup

        private static List<SkillSpec> ArtsOf(Combatant member)
        {
            var list = new List<SkillSpec>();
            foreach (var s in member.Skills) if (s.Id != "basic" && s.SpiritCost <= 0) list.Add(s);
            return list;
        }

        /// <summary>The unit's 기세 100 art (shown on the dedicated 필살기 button), or null.</summary>
        private static SkillSpec UltimateOf(Combatant member)
        {
            foreach (var s in member.Skills) if (s.Id != "basic" && s.SpiritCost > 0) return s;
            return null;
        }

        private List<Combatant> CreateParty(StageData stage)
        {
            var save = GameManager.Instance.Save;
            var hero = Formation.BuildHero(save);   // equipped arts (four + one 비기), stats and 심법 perks
            var party = new List<Combatant> { hero };
            foreach (var c in EarlyGameContent.SelectCompanionsFor(Roster.PartyStage(stage, save), save))
            {
                int bondLevel = Bonds.Level(save, c.Id);
                party.Add(CompanionGrowth.Create(save, c));
                if (bondLevel >= Bonds.RescueLevel && string.IsNullOrEmpty(hero.RescuerId)) hero.RescuerId = c.Id;
            }
            Formation.Apply(party, save, stage);   // 앞줄/뒷줄 from the saved formation, or planned against this stage's enemies
            return party;
        }

        // ------------------------------------------------------------------ the board (6 x 3 tiles, our side left, the enemies right)

        private const float FieldLeft = .015f, FieldRight = .705f;
        private const float FieldTop = .706f, FieldBottom = .142f, HeaderBottom = .710f, HeaderTop = .734f;
        private static readonly Color PartyTile = new Color(0.18f, 0.42f, 0.37f, 0.07f), EnemyTile = new Color(0.52f, 0.20f, 0.15f, 0.07f);

        /// <summary>The screen rectangle (fractions of the battle root) of a board tile.</summary>
        private static Rect TileCell(int col, int row)
        {
            float rowH = (FieldTop - FieldBottom) / Board.Rows;
            float width = (FieldRight - FieldLeft) / Board.Cols;
            return new Rect(FieldLeft + col * width, FieldTop - (row + 1) * rowH, width, rowH);
        }

        private readonly Image[,] _tileHi = new Image[Board.Cols, Board.Rows];

        /// <summary>The floor: our half tinted blue, the enemy half red; tiles light up green where the acting member may walk.</summary>
        private void BuildBoard(RectTransform root)
        {
            UiFactory.Panel("FieldWash", root, new Vector2(FieldLeft, FieldBottom), new Vector2(FieldRight, HeaderTop), new Color(.02f, .045f, .05f, .32f));
            UiFactory.Glyph("halo", root, new Vector2(.12f, .28f), new Vector2(.60f, .70f), new Color(.83f, .72f, .48f, .065f));
            // Column header: 앞 (facing the other side) / 중 / 뒤, so it is clear where the front and back lines are.
            string[] names = { "뒤", "중", "앞", "앞", "중", "뒤" };
            for (int c = 0; c < Board.Cols; c++)
            {
                bool front = c == Board.PartyCols - 1 || c == Board.PartyCols;
                var tint = c < Board.PartyCols ? new Color(.12f, .29f, .26f, front ? .75f : .35f) : new Color(.35f, .15f, .12f, front ? .75f : .35f);
                float x0 = FieldLeft + c * (FieldRight - FieldLeft) / Board.Cols, x1 = FieldLeft + (c + 1) * (FieldRight - FieldLeft) / Board.Cols;
                UiFactory.Panel("Head" + c, root, new Vector2(x0 + 0.004f, HeaderBottom + 0.004f), new Vector2(x1 - 0.004f, HeaderTop), tint);
                UiFactory.Label("HeadText" + c, root, new Vector2(x0, HeaderBottom), new Vector2(x1, HeaderTop), names[c] + (c == 0 ? " 줄" : c == Board.Cols - 1 ? " 줄" : front ? " 줄" : ""), 22, front ? UiFactory.Gold : UiFactory.PaperText);
            }
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    var cell = TileCell(c, r);
                    bool frontCol = c == Board.PartyCols - 1 || c == Board.PartyCols;
                    var baseColor = c < Board.PartyCols ? PartyTile : EnemyTile;
                    if (frontCol) baseColor.a = 0.13f;
                    var tile = UiFactory.Panel("Tile", root, new Vector2(cell.x, cell.y), new Vector2(cell.x + cell.width, cell.y + cell.height), baseColor);
                    UiFactory.Frame(tile.transform, new Color(.69f, .73f, .62f, .10f), 1f);
                    _tileHi[c, r] = UiFactory.Panel("Hi", tile.transform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f), new Color(0f, 0f, 0f, 0f));
                }
            UiFactory.Panel("CampLine", root, new Vector2((FieldLeft + FieldRight) * .5f - .001f, FieldBottom), new Vector2((FieldLeft + FieldRight) * .5f + .001f, HeaderTop), new Color(.83f, .72f, .48f, .22f));
        }

        private void BuildParty(StageData stage)
        {
            _party = CreateParty(stage);
            _hudActor = null;
            ClearPanels();
            int n = _party.Count;
            for (int i = 0; i < n; i++)
            {
                var unit = _party[i];
                var cell = TileCell(unit.Col, unit.Row);
                var p = new PartyPanel { Unit = unit, Center = new Vector2(cell.x + cell.width / 2f, cell.y + cell.height / 2f) };
                p.Root = UiFactory.Rect("Member" + i, _partyRoot, new Vector2(cell.x, cell.y), new Vector2(cell.x + cell.width, cell.y + cell.height));
                p.ActiveFrame = UiFactory.Glyph("frame", p.Root, new Vector2(.015f, .005f), new Vector2(.985f, .995f), Color.clear);
                UiFactory.Glyph("stance", p.Root, new Vector2(.12f, .355f), new Vector2(.88f, .40f), new Color(.39f, .62f, .55f, .55f));
                p.Frame = UiFactory.Panel("Marker", p.Root, new Vector2(0.10f, 0.298f), new Vector2(0.90f, 0.314f), new Color(0f, 0f, 0f, 0f));   // gold bar under the feet marks whose turn it is
                var sprite = BattleFigure(i == 0 ? "player" : unit.Id);
                if (sprite != null)
                {
                    p.Art = FitArt(p.Root, sprite, cell, true);
                }
                else
                {
                    p.FigureGlyph = UiFactory.Label("Glyph", p.Root, new Vector2(0.04f, 0.32f), new Vector2(0.96f, 0.90f), FirstGlyph(unit.Name), 110, i == 0 ? UiFactory.Gold : UiFactory.PaperText);
                }
                p.Name = UiFactory.Label("Name", p.Root, new Vector2(0.03f, 0.845f), new Vector2(0.97f, 0.995f), ShortName(unit.Name), 24, UiFactory.Gold);
                p.Status = UiFactory.Label("Status", p.Root, new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.80f), "", 22, UiFactory.PaperText);
                // HP is the primary number; Qi and Spirit stay as meters (exact values are in the acting-member HUD and details).
                var gaugeBack = new Color(0f, 0f, 0f, 0.75f);
                p.Hp = UiFactory.MakeBar("Hp", p.Root, new Vector2(0.04f, 0.20f), new Vector2(0.96f, 0.35f), gaugeBack, HpGreen);
                p.Qi = UiFactory.MakeBar("Qi", p.Root, new Vector2(0.04f, 0.13f), new Vector2(0.96f, 0.16f), gaugeBack, QiBlue);
                p.Spirit = UiFactory.MakeBar("Spirit", p.Root, new Vector2(0.04f, 0.065f), new Vector2(0.96f, 0.095f), gaugeBack, UiFactory.Gold);
                p.HpText = GaugeText("HpText", p.Root, .20f, .35f);
                p.QiText = GaugeText("QiText", p.Root, 0.110f, 0.200f);
                p.SpiritText = GaugeText("SpiritText", p.Root, 0.015f, 0.105f);
                p.QiText.gameObject.SetActive(false); p.SpiritText.gameObject.SetActive(false);
                p.ActionBadge = BuildActionBadge(p.Root);
                var panelUnit = unit;
                var catchImg = p.Root.gameObject.AddComponent<Image>();
                catchImg.color = new Color(0f, 0f, 0f, 0f);   // catches the press
                var allyBtn = p.Root.gameObject.AddComponent<Button>();
                allyBtn.targetGraphic = catchImg;
                allyBtn.transition = Selectable.Transition.None;
                var allyPanel = p;
                allyBtn.onClick.AddListener(() => OnAllyPressed(allyPanel));
                p.Root.gameObject.AddComponent<LongPress>().OnLong = () => ShowMemberTip(panelUnit);
                _panels.Add(p);
            }
        }

        /// <summary>Numbers written over a gauge: light text with a dark outline so it reads on both the fill and the empty part.</summary>
        private static Text GaugeText(string name, RectTransform parent, float y0, float y1)
        {
            var t = UiFactory.Label(name, parent, new Vector2(0.05f, y0), new Vector2(0.95f, y1), "", 23, Color.white);
            var outline = t.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        private static void MakeStatRow(RectTransform parent, string label, float y0, Color fill, out UiFactory.Bar bar, out Text value)
        {
            UiFactory.Label(label + "Label", parent, new Vector2(0.05f, y0), new Vector2(0.24f, y0 + 0.19f), label, 22, UiFactory.PaperText, TextAnchor.MiddleLeft);
            bar = UiFactory.MakeBar(label + "Bar", parent, new Vector2(0.25f, y0 + 0.02f), new Vector2(0.96f, y0 + 0.17f), new Color(0.2f, 0.2f, 0.2f, 0.9f), fill);
            value = UiFactory.Label(label + "Value", parent, new Vector2(0.25f, y0), new Vector2(0.96f, y0 + 0.19f), "", 20, UiFactory.PaperText);
        }

        private void RebuildPartyForNewLevel(StageData stage, List<string> leveledIds)
        {
            var fresh = CreateParty(stage);
            for (int i = 0; i < fresh.Count && i < _party.Count; i++)
            {
                // A new Combatant starts with full 체력 and 내공, which is exactly what a character who levelled up gets.
                // Everyone else keeps their wounds and 내공 by ratio (the stats may still have changed through the protagonist's rank-ups etc.); 기세 is carried over for everyone.
                if (!leveledIds.Contains(fresh[i].Id))
                {
                    float ratio = (float)_party[i].Hp / Math.Max(1, _party[i].Stats.MaxHp);
                    fresh[i].Hp = _party[i].IsAlive ? Math.Max(1, (int)Math.Round(fresh[i].Stats.MaxHp * ratio)) : 0;
                    fresh[i].Qi = Math.Min(fresh[i].Stats.MaxQi, (int)Math.Round(fresh[i].Stats.MaxQi * (float)_party[i].Qi / Math.Max(1, _party[i].Stats.MaxQi)));
                }
                fresh[i].Spirit = _party[i].Spirit;
                if (i < _panels.Count) _panels[i].Unit = fresh[i];
            }
            _party = fresh;
        }

        private void ClearCards()
        {
            CancelBasicMotion();
            foreach (var c in _cards) if (c.Root != null) Destroy(c.Root.gameObject);
            _cards.Clear();
            _home.Clear();
        }

        private void ClearPanels()
        {
            CancelBasicMotion();
            foreach (var p in _panels) if (p.Root != null) Destroy(p.Root.gameObject);
            _panels.Clear();
            _home.Clear();
        }

        private void SpawnCards(EnemyData[] enemies)
        {
            ClearCards();
            int n = enemies.Length;
            var units = new List<Combatant>();
            for (int i = 0; i < n; i++) units.Add(enemies[i].CreateCombatant(enemies[i].Id + "#" + i));
            Formation.PlaceEnemies(units, _party);   // the best answer to our lines (expected damage), or the stat rule for big groups
            for (int i = 0; i < n; i++)
            {
                var data = enemies[i];
                var cell = TileCell(units[i].Col, units[i].Row);
                var card = new EnemyCard { Unit = units[i], Data = data, ShownHp = units[i].Hp, Center = new Vector2(cell.x + cell.width / 2f, cell.y + cell.height / 2f) };
                card.Root = UiFactory.Rect("Card" + i, _cardsRoot, new Vector2(cell.x, cell.y), new Vector2(cell.x + cell.width, cell.y + cell.height));
                card.TargetFrame = UiFactory.Glyph("frame", card.Root, new Vector2(.015f, .005f), new Vector2(.985f, .995f), Color.clear);
                UiFactory.Glyph("stance", card.Root, new Vector2(.12f, .355f), new Vector2(.88f, .40f), new Color(.66f, .30f, .21f, .55f));
                card.Frame = UiFactory.Panel("Marker", card.Root, new Vector2(0.10f, 0.303f), new Vector2(0.90f, 0.319f), new Color(0f, 0f, 0f, 0f));   // gold bar under the feet marks the target
                var rank = data.Rank;
                card.Name = UiFactory.Label("Name", card.Root, new Vector2(0.03f, 0.845f), new Vector2(0.97f, 0.995f), (rank == EnemyRank.Elite ? "◆ " : "") + ShortName(data.Name), 24,
                    rank == EnemyRank.Boss ? UiFactory.Gold : rank == EnemyRank.Elite ? new Color(0.62f, 0.82f, 1f, 1f) : UiFactory.PaperText);
                var art = BattleFigure(data.ArtKey);
                if (art != null)
                {
                    card.Art = FitArt(card.Root, art, cell, true, true);
                }
                card.Glyph = UiFactory.Label("Glyph", card.Root, new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.88f), art != null ? "" : FirstGlyph(data.Name), data.IsBoss ? 170 : 110, data.IsBoss ? UiFactory.Gold : UiFactory.PaperText);
                // HP remains readable; the resource meters leave room for a concise intent line.
                var gaugeBack = new Color(0f, 0f, 0f, 0.75f);
                card.Hp = UiFactory.MakeBar("Hp", card.Root, new Vector2(0.04f, 0.23f), new Vector2(0.96f, 0.37f), gaugeBack, UiFactory.Red);
                card.Qi = UiFactory.MakeBar("Qi", card.Root, new Vector2(0.04f, 0.195f), new Vector2(0.96f, 0.215f), gaugeBack, QiBlue);
                card.Spirit = UiFactory.MakeBar("Spirit", card.Root, new Vector2(0.04f, 0.16f), new Vector2(0.96f, 0.18f), gaugeBack, UiFactory.Gold);
                card.HpText = GaugeText("HpText", card.Root, .23f, .37f);
                card.QiText = GaugeText("QiText", card.Root, 0.165f, 0.230f);
                card.SpiritText = GaugeText("SpiritText", card.Root, 0.095f, 0.160f);
                card.QiText.gameObject.SetActive(false); card.SpiritText.gameObject.SetActive(false);
                if (rank == EnemyRank.Normal)   // 일반 enemies have neither 내공 nor 기세: leave their rows out
                {
                    card.Qi.Root.gameObject.SetActive(false); card.Spirit.Root.gameObject.SetActive(false);
                    card.QiText.gameObject.SetActive(false); card.SpiritText.gameObject.SetActive(false);
                }
                // 무공 경지 under the name, from the stats this fight actually uses (harder difficulties raise it)
                string realm = Realm.HasRealm(data) ? Realm.Label(units[i].RealmStep) : "";
                card.Realm = UiFactory.Label("Realm", card.Root, new Vector2(0.03f, 0.73f), new Vector2(0.97f, 0.845f), realm, 18, new Color(0.85f, 0.75f, 0.45f, 1f));
                card.Badge = UiFactory.Label("Badge", card.Root, new Vector2(0.04f, 0.60f), new Vector2(0.96f, 0.73f), "", 20, UiFactory.PaperText);
                card.IntentBack = UiFactory.Panel("IntentBack", card.Root, new Vector2(.01f, 0f), new Vector2(.99f, .145f), HudInk);
                card.Intent = UiFactory.Label("Intent", card.Root, new Vector2(0.02f, 0.0f), new Vector2(0.98f, 0.145f), "", 21, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);

                card.ActionBadge = BuildActionBadge(card.Root);

                // Tap to attack (only while an attack is being aimed)
                var tap = card.Root.gameObject.AddComponent<Button>();
                var tapImg = card.Root.gameObject.AddComponent<Image>();
                tapImg.color = new Color(0f, 0f, 0f, 0f);
                tap.targetGraphic = tapImg;
                var captured = card;
                tap.onClick.AddListener(() => OnEnemyPressed(captured));
                card.Root.gameObject.AddComponent<LongPress>().OnLong = () => ShowEnemyTip(captured);
                _cards.Add(card);
            }
        }

        /// <summary>
        /// Draws a cut-out figure inside a cell: the sprite keeps its aspect ratio, is scaled to fill the cell's art area
        /// (never wider or taller), and stands on the cell's feet line so every unit shares one ground level.
        /// </summary>
        private Image FitArt(RectTransform cellRoot, Sprite sprite, Rect cell, bool chibi = false, bool enemy = false)
        {
            var pic = UiFactory.Panel("Art", cellRoot, new Vector2(.5f, .38f), new Vector2(.5f, .38f), Color.white);
            pic.sprite = sprite;
            pic.preserveAspect = true;
            var rt = pic.rectTransform;
            rt.pivot = new Vector2(.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            if (chibi && enemy) rt.localScale = new Vector3(-1f, 1f, 1f);
            pic.gameObject.AddComponent<BattleFigureFit>().Fit();
            return pic;
        }

        private static string FirstGlyph(string name)
        {
            // Skip honorific prefixes so the big letter is a meaningful syllable.
            string[] skip = { "비풍도 ", "일도파쇄 ", "선풍검룡 ", "호아장주 ", "호아장 대사형 ", "청풍검 ", "기관 ", "염도 ", "패호 ", "패룡 " };
            foreach (var s in skip) if (name.StartsWith(s)) name = name.Substring(s.Length);
            return name.Length > 0 ? name.Substring(0, 1) : "?";
        }

        // ------------------------------------------------------------------ refresh

        /// <summary>True when the aimed attack can hit this enemy (it must be within the art's reach lines).</summary>
        private bool Hittable(Combatant enemy)
        {
            return Aiming && enemy.IsAlive && _battle != null && _battle.Depth(enemy) < _aimSkill.Range;
        }

        /// <summary>While aiming: green floor under the enemies that can be hit, dark red under the ones that cannot. Clear otherwise.</summary>
        private void RefreshBoard()
        {
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                    if (_tileHi[c, r] != null) _tileHi[c, r].color = new Color(0f, 0f, 0f, 0f);
            if (AimingAlly)
            {
                foreach (var pn in _panels)
                {
                    var m = pn.Unit;
                    if (m.Col < 0 || m.Col >= Board.Cols || m.Row < 0 || m.Row >= Board.Rows) continue;
                    _tileHi[m.Col, m.Row].color = m.IsAlive ? new Color(.39f, .62f, .55f, .24f) : new Color(.66f, .22f, .17f, .16f);
                }
                return;
            }
            if (!Aiming || _battle == null) return;
            foreach (var card in _cards)
            {
                var u = card.Unit;
                if (!u.IsAlive || u.Col < 0 || u.Col >= Board.Cols || u.Row < 0 || u.Row >= Board.Rows) continue;
                _tileHi[u.Col, u.Row].color = Hittable(u) ? new Color(.83f, .72f, .48f, .20f) : new Color(.66f, .22f, .17f, .16f);
            }
        }

        private void BeginAim(SkillSpec skill, Combatant partner)
        {
            _aimSkill = skill;
            _aimPartner = partner;
            _jointMode = false;
            SoundManager.Sfx("ui_click");
            _log.text = "「" + (partner != null ? _jointName : skill.Name) + "」 — 칠 적을 누르세요.  초록 = 공격 가능 · 빨강 = 공격 불가";
            RefreshCommands();
            RefreshCards();
        }

        private void BeginAllyAim(string what, Func<Combatant, PlayerCommand> build)
        {
            _allyAim = build;
            _itemMode = false;
            SoundManager.Sfx("ui_click");
            _log.text = "「" + what + "」 — 쓸 아군을 누르세요.  초록 = 선택 가능";
            RefreshCommands();
            RefreshParty();
            RefreshBoard();
        }

        private void OnAllyPressed(PartyPanel panel)
        {
            if (LongPress.JustFired) return;
            if (!AimingAlly) { ShowAllyTip(panel.Unit); return; }   // not choosing an ally: a tap reads their info
            if (_battle == null || !_battle.IsPlayerTurn || _hasCommand) return;
            if (!panel.Unit.IsAlive) { _log.text = panel.Unit.Name + "은(는) 쓰러져 있어 쓸 수 없습니다."; return; }
            var build = _allyAim;
            EndAim();
            RefreshParty();
            Issue(build(panel.Unit));
        }

        private void EndAim()
        {
            _aimSkill = null;
            _aimPartner = null;
            _allyAim = null;
        }

        private void CancelAim()
        {
            EndAim();
            _log.text = "";
            RefreshCommands();
            RefreshCards();
            RefreshParty();
        }

        private void OnEnemyPressed(EnemyCard card)
        {
            if (LongPress.JustFired) return;
            if (!Aiming) { ShowEnemyTip(card); return; }   // no command picked yet: a tap reads the enemy's info
            if (_battle == null || !_battle.IsPlayerTurn || _hasCommand || !card.Unit.IsAlive) return;
            if (!Hittable(card.Unit))
            {
                _log.text = card.Unit.Name + "에게는 닿지 않습니다. 앞 줄의 적을 먼저 쓰러뜨리거나, 뒷줄까지 닿는 무공을 쓰세요.";
                StartCoroutine(Shake(card.Root, 8f));
                return;
            }
            var skill = _aimSkill; var partner = _aimPartner;
            EndAim();
            RefreshCards();
            Issue(partner != null ? PlayerCommand.Joint(skill, partner, card.Unit) : PlayerCommand.UseSkill(skill, card.Unit));
        }

        private void RefreshCards()
        {
            foreach (var c in _cards)
            {
                var u = c.Unit;
                if (!_animating) { c.ShownHp = u.Hp; c.ShownPhase = u.PhaseIndex; }
                bool dead = c.ShownHp <= 0;
                c.Hp.SetPct(u.Stats.MaxHp > 0 ? (float)c.ShownHp / u.Stats.MaxHp : 0f);
                c.HpText.text = c.ShownHp.ToString("N0") + " / " + u.Stats.MaxHp.ToString("N0");
                c.Qi.SetPct(u.Stats.MaxQi > 0 ? (float)u.Qi / u.Stats.MaxQi : 0f);
                c.QiText.text = u.Qi + " / " + u.Stats.MaxQi;
                c.Spirit.SetPct(u.Spirit / (float)InteractiveBattle.MaxSpirit);
                c.SpiritText.text = u.Spirit + " / " + InteractiveBattle.MaxSpirit;
                bool acting = VisibleActor == u && !dead;
                c.ActionBadge.transform.parent.gameObject.SetActive(false);   // no "행동 중" badge: the ▶ name and red frame mark the acting enemy
                c.ActionBadge.text = "";
                c.ActionBadge.color = ThreatRed;
                c.Name.text = (acting ? "▶ " : "") + (c.Data.Rank == EnemyRank.Elite ? "◆ " : "") + u.Name;
                bool aimOk = Hittable(u), aimNo = Aiming && !dead && !aimOk;
                c.Frame.color = aimOk ? UiFactory.Gold : aimNo ? new Color(0.75f, 0.15f, 0.15f, 1f) : new Color(0f, 0f, 0f, 0f);
                c.TargetFrame.color = acting ? ThreatRed : aimOk ? UiFactory.Gold : Color.clear;
                c.IntentBack.color = (u.Charging || u.AwakeningStrike != null) && !dead ? new Color(.45f, .13f, .10f, .95f) : HudInk;
                c.Badge.text = aimOk ? "선택 가능" : aimNo ? "사거리 밖" : dead ? "" : Statuses.Line(u);
                c.Badge.color = aimOk ? UiFactory.Gold : aimNo ? new Color(1f, 0.45f, 0.45f, 1f) : StatusColor;
                if (c.Art != null) SetAlpha(c.Art, dead ? 0.25f : aimNo ? 0.35f : 1f);
                if (dead) c.Glyph.color = new Color(1f, 1f, 1f, 0.2f);
                if (dead) { c.Intent.text = "쓰러졌다"; continue; }
                if (u.Stunned) { c.Intent.text = "행동 불능"; c.Intent.color = GuardBlue; continue; }
                var move = u.PeekMove();
                if (u.AwakeningStrike != null)
                {
                    c.Intent.text = "각성 강타 " + (u.PhaseIndex == 1 ? "70%" : "100%");
                    c.Intent.color = ThreatRed;
                }
                else if (u.Charging)
                {
                    c.Intent.text = "강타" + (u.ChargeTarget != null ? " → " + ShortName(u.ChargeTarget.Name) : " 준비");
                    c.Intent.color = ThreatRed;
                }
                else if (move.Charge)
                {
                    c.Intent.text = "다음: 전조";
                    c.Intent.color = UiFactory.Gold;
                }
                else
                {
                    c.Intent.text = (move.Snipe ? "저격" : "공격") + (move.Hits > 1 ? " x" + move.Hits : "") + (move.Inflict != StatusKind.None ? " · " + Statuses.Name(move.Inflict) : "");
                    c.Intent.color = UiFactory.PaperText;
                }
                if (u.UsesResources && u.Spirit >= InteractiveBattle.MaxSpirit)
                {
                    c.Intent.text = u.AwakeningStrike != null ? "각성 · 폭발" : "기세 가득 · 위험";
                    c.Intent.color = ThreatRed;
                }
            }
            RefreshBoard();
            RefreshFocusHud();
        }

        private void RefreshParty()
        {
            foreach (var p in _panels)
            {
                var u = p.Unit;
                if (!_animating) { p.ShownHp = u.Hp; p.ShownQi = u.Qi; p.ShownSpirit = u.Spirit; }
                p.Hp.SetPct((float)p.ShownHp / Math.Max(1, u.Stats.MaxHp));
                p.Qi.SetPct((float)p.ShownQi / Math.Max(1, u.Stats.MaxQi));
                p.Spirit.SetPct(p.ShownSpirit / (float)InteractiveBattle.MaxSpirit);
                p.HpText.text = p.ShownHp + " / " + u.Stats.MaxHp;
                p.QiText.text = p.ShownQi + " / " + u.Stats.MaxQi;
                p.SpiritText.text = p.ShownSpirit + " / " + InteractiveBattle.MaxSpirit;
                string stance = !u.IsAlive ? "쓰러짐" : _battle != null && _battle.IsGuarded(u) ? "방어" : u.Evading ? "회피" : "";
                string afflicted = u.IsAlive ? Statuses.Line(u) : "";
                bool incoming = false;
                if (_battle != null && u.IsAlive)
                    foreach (var enemy in _battle.Enemies)
                        if (enemy.IsAlive && enemy.AwakeningStrike == null && enemy.Charging && enemy.ChargeTarget == u) { incoming = true; break; }
                p.Status.text = incoming ? "강타 대상" + (afflicted.Length > 0 ? "\n" + afflicted : "")
                    : stance + (stance.Length > 0 && afflicted.Length > 0 ? " · " : "") + afflicted;
                p.Name.color = u.IsAlive ? UiFactory.Gold : new Color(0.5f, 0.5f, 0.5f, 1f);
                bool current = VisibleActor == u && p.ShownHp > 0;
                p.ActionBadge.transform.parent.gameObject.SetActive(current && !_animating);   // only "내 차례"; no "행동 중" while the move plays
                p.ActionBadge.text = "내 차례";
                p.Name.text = (current ? "▶ " : "") + u.Name;
                p.Frame.color = current ? UiFactory.Gold : new Color(0f, 0f, 0f, 0f);
                p.ActiveFrame.color = current ? UiFactory.Gold : Color.clear;
                p.Status.color = incoming ? ThreatRed : afflicted.Length > 0 ? StatusColor : UiFactory.PaperText;
                if (AimingAlly && u.IsAlive && !current)
                {
                    p.Frame.color = UiFactory.Jade;
                    p.ActiveFrame.color = UiFactory.Jade;
                    p.Status.text = "선택 가능";
                    p.Status.color = UiFactory.Jade;
                }
            }
            RefreshActorHud();
        }

        private void RefreshTurnOrder()
        {
            if (_battle == null) return;
            var actor = VisibleActor;
            var order = _animating ? (actor != null ? new List<Combatant> { actor } : new List<Combatant>())
                : _battle.PreviewOrder(_turnChips.Count);
            for (int i = 0; i < _turnChips.Count; i++)
            {
                bool has = i < order.Count;
                _turnChips[i].gameObject.SetActive(has);
                if (!has) continue;
                bool ours = order[i].Team == Team.Player;
                _turnChips[i].color = i == 0 ? new Color(.29f, .27f, .18f, 1f) : HudInk;
                _turnEdges[i].color = i == 0 ? UiFactory.Gold : ours ? UiFactory.Jade : ThreatRed;
                _turnLabels[i].text = ShortName(order[i].Name);
                _turnLabels[i].color = i == 0 ? UiFactory.Gold : UiFactory.PaperText;
                var pic = UnitPortrait(order[i]);
                _turnPortraits[i].sprite = pic;
                _turnPortraits[i].gameObject.SetActive(pic != null);
                _turnGlyphs[i].text = pic == null ? FirstGlyph(order[i].Name) : "";
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
                if (item != null && item.Kind == ItemKind.Consumable && item.MasteryUses == 0 && s.Count > 0) list.Add(item);
            }
            return list;
        }

        private static bool HasLearnedUltimate()
        {
            foreach (var id in GameManager.Instance.Save.LearnedArts)
            {
                var art = EarlyGameContent.FindArt(id);
                if (art != null && art.OwnerId == null && Progression.IsUltimate(art)) return true;
            }
            return false;
        }

        private void RefreshCommands()
        {
            var me = _battle.Current;
            if (me == null) return;
            SetCommandsVisible(true);
            _hudActor = me;
            LayoutCommands(_itemMode || _jointMode);
            RefreshActorHud();

            if (Aiming || AimingAlly)
            {
                for (int i = 0; i < _commands.Length; i++) if (i != 7) _commands[i].Root.SetActive(false);
                SetCommand(7, "취소", "다시 고르기", true);
                return;
            }

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
                _commands[9].Root.SetActive(false);
                return;
            }

            if (_itemMode)
            {
                _commands[8].Root.SetActive(false);
                _commands[9].Root.SetActive(false);
                var items = UsableItems();
                for (int i = 0; i < 7; i++)
                {
                    if (i >= items.Count) { _commands[i].Root.SetActive(false); continue; }
                    var item = items[i];
                    SetCommand(i, item.Name + " x" + Inventory.Count(GameManager.Instance.Save, item.Id), ItemHint(item), true, ArtLibrary.Item(item));
                }
                SetCommand(7, "취소", "", true);
                return;
            }

            SetCommand(0, "공격", SkillSpec.ReachText(1), true, ArtLibrary.Icon("cmd_attack"));
            var arts = ArtsOf(me);
            for (int i = 0; i < Progression.MaxEquippedArts; i++)
            {
                int slot = 1 + i;
                if (i >= arts.Count) { _commands[slot].Root.SetActive(false); continue; }
                var s = arts[i];
                int cd = me.CooldownLeft(s);
                bool reach = _battle.CanReach(s);
                bool ok = _battle.CanUse(s) && reach;
                string sub = cd > 0 ? "재사용 " + cd + "턴" : s.SpiritCost > 0 ? "기세 " + s.SpiritCost : s.QiCost > 0 ? "내공 " + s.QiCost : "";
                if (cd == 0 && me.Qi < s.QiCost) sub = "내공 부족";
                else if (!reach) sub = "닿지 않음";
                SetCommand(slot, s.Name, sub, ok, ArtLibrary.Art(s.Id));
            }
            SetCommand(5, "방어", "피해 60% 감소", true, ArtLibrary.Icon("cmd_guard"));
            SetCommand(6, "운기", "내공 +" + InteractiveBattle.MeditateQi + "\n체력 소량 회복", true, ArtLibrary.Icon("cmd_meditate"));
            SetCommand(7, "아이템", UsableItems().Count > 0 ? "" : "없음", UsableItems().Count > 0, ArtLibrary.Icon("cmd_item"));

            RefreshJointPartners();
            var ult = UltimateOf(me);
            if (ult == null)
            {
                // The protagonist keeps an empty 필살기 slot on screen once a 비기 is learned, so it is clear one can be equipped.
                if (me.Id == "player" && HasLearnedUltimate()) SetCommand(9, "필살기", "비어 있음", false, ArtLibrary.Icon("ultimate_slot"));
                else _commands[9].Root.SetActive(false);
            }
            else
            {
                bool ready = _battle.CanUse(ult) && _battle.CanReach(ult);
                SetCommand(9, ult.Name, ready ? "기세 " + ult.SpiritCost : "기세 " + Mathf.Min(me.Spirit, ult.SpiritCost) + "/" + ult.SpiritCost, ready, ArtLibrary.Art(ult.Id) ?? ArtLibrary.Icon("ultimate_slot"));
            }
            if (_jointPartners.Count == 0) _commands[8].Root.SetActive(false);
            else SetCommand(8, "합공", me.Spirit >= InteractiveBattle.JointSpiritCost ? "기세 " + InteractiveBattle.JointSpiritCost : "기세 부족", me.Spirit >= InteractiveBattle.JointSpiritCost, ArtLibrary.Icon("cmd_joint"));
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

        private void IssueJoint(Combatant partner)
        {
            var companion = EarlyGameContent.FindCompanion(partner.Id);
            var spec = Bonds.JointSpec(companion, Bonds.Level(GameManager.Instance.Save, partner.Id), Bonds.ReferencePower(_battle.Player.Skills));
            if (spec == null || !_battle.CanJoint(partner)) return;
            spec.Power *= 1f + Factions.Bonus(GameManager.Instance.Save, PerkKind.JointPower);
            _jointName = spec.Name;
            if (_battle.EnemiesInRange(spec.Range).Count == 0)
            {
                _log.text = "합공이 닿는 적이 없습니다.";
                _jointMode = false;
                RefreshCommands();
                return;
            }
            BeginAim(spec, partner);
        }

        /// <summary>Hands the chosen command to the battle loop; a walk planned on the board is part of it.</summary>
        private void Issue(PlayerCommand cmd)
        {
            _command = cmd;
            _hasCommand = true;
            RefreshBoard();
        }

        private static string ItemHint(ItemData item)
        {
            if (item.HealPct >= 1f) return "완전 회복";
            if (item.HealPct > 0f) return "체력 " + Mathf.RoundToInt(item.HealPct * 100f) + "%";
            if (item.QiRestore > 0) return "내공 +" + item.QiRestore;
            if (item.Cleanse) return "상태 이상 해제";
            return "";
        }

        private void SetCommand(int slot, string name, string sub, bool enabled, Sprite icon = null)
        {
            var c = _commands[slot];
            c.Root.SetActive(true);
            c.Icon.gameObject.SetActive(icon != null);
            c.Icon.sprite = icon;
            c.Icon.color = enabled ? Color.white : new Color(1f, 1f, 1f, .35f);
            c.Fallback.gameObject.SetActive(icon == null);
            c.Fallback.Key = _itemMode ? "bag" : _jointMode ? "party" : CommandGlyph(slot);
            c.Fallback.SetVerticesDirty();
            c.Fallback.color = enabled ? UiFactory.Gold : UiFactory.Dim;
            c.Label.text = name;
            c.Detail.text = sub;
            c.Label.color = enabled ? UiFactory.PaperText : UiFactory.Dim;
            c.Detail.color = enabled ? (slot == 9 ? UiFactory.Gold : UiFactory.Dim) : new Color(.72f, .54f, .47f, 1f);
            c.Button.interactable = enabled;
            c.Image.color = !enabled ? Disabled : _itemMode || _jointMode ? UiFactory.InkSoft
                : slot == 0 ? new Color(.48f, .18f, .13f, 1f) : slot == 5 ? new Color(.15f, .27f, .32f, 1f)
                : slot == 6 ? new Color(.13f, .28f, .23f, 1f) : slot == 9 ? new Color(.37f, .28f, .12f, 1f) : UiFactory.InkSoft;
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
            // A quick second tap meant for the tip must not land on the command or card underneath it.
            close.onClick.AddListener(() => { _tipRoot.SetActive(false); StartCoroutine(GuardTaps(0.3f)); });
            var card = UiFactory.Panel("Card", rt, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.87f), new Color(0.05f, 0.07f, 0.11f, 0.98f));
            card.raycastTarget = true;
            var cardTap = card.gameObject.AddComponent<Button>();
            cardTap.targetGraphic = card;
            cardTap.transition = Selectable.Transition.None;
            UiFactory.Frame(card.transform, UiFactory.Gold, 3f);
            _tipIcon = UiFactory.Panel("Icon", card.transform, new Vector2(0.76f, 0.76f), new Vector2(0.95f, 0.98f), Color.white);
            _tipIcon.preserveAspect = true;
            _tipTitle = UiFactory.Label("Title", card.transform, new Vector2(0.05f, 0.86f), new Vector2(0.72f, 0.98f), "", 40, UiFactory.Gold, TextAnchor.MiddleLeft);
            _tipBody = UiFactory.ScrollBody(UiFactory.Label("Body", card.transform, new Vector2(0.06f, .18f), new Vector2(0.94f, 0.74f), "", 28, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal), 36);
            _tipBody.lineSpacing = 1.25f;
            UiFactory.Button("Close", card.transform, new Vector2(.30f, .02f), new Vector2(.70f, .16f), "닫기", UiFactory.InkSoft, () => { _tipRoot.SetActive(false); StartCoroutine(GuardTaps(.3f)); }, 34);
            _tipRoot.SetActive(false);
        }

        private Image _tipIcon;

        private void ShowTip(string title, string body, Sprite icon = null)
        {
            if (_tipRoot == null) return;
            _tipTitle.text = title;
            _tipBody.text = body;
            _tipIcon.sprite = icon;
            _tipIcon.gameObject.SetActive(icon != null);
            // With a picture the title and the first lines make room for it on the right.
            _tipTitle.rectTransform.anchorMax = new Vector2(icon != null ? 0.74f : 0.95f, _tipTitle.rectTransform.anchorMax.y);
            UiFactory.ReadingViewport(_tipBody).anchorMax = new Vector2(0.94f, icon != null ? 0.76f : 0.85f);
            _tipRoot.transform.SetAsLastSibling();
            _tipRoot.SetActive(true);
            SoundManager.Sfx("ui_click");
        }

        /// <summary>"위력 x2.4 · 3타 · 내공 30 · 재사용 3턴" plus what the effect does.</summary>
        private static string SkillDetails(SkillSpec s)
        {
            var parts = new List<string>();
            if (s.Effect == SkillEffect.Evade) parts.Add("다음 차례까지 공격을 " + Mathf.RoundToInt(s.EvadeChance * 100f) + "% 확률로 회피");
            else if (s.Effect == SkillEffect.GuardParty) parts.Add("파티 전체 방어 태세");
            else if (s.Effect == SkillEffect.HealAlly) parts.Add("가장 다친 동료 체력 " + Mathf.RoundToInt(s.HealPct * 100f) + "% 회복");
            else if (s.Effect == SkillEffect.Cleanse) parts.Add("고른 동료의 상태 이상을 모두 풀고 체력 " + Mathf.RoundToInt(s.HealPct * 100f) + "% 회복");
            else
            {
                parts.Add("위력 ×" + s.Power.ToString("0.0") + (s.Hits > 1 ? " · " + s.Hits + "타" : ""));
                if (s.AllEnemies) parts.Add("닿는 범위의 적 전체");
                parts.Add("닿는 범위 " + SkillSpec.ReachText(s.Range));
                if (s.Effect == SkillEffect.Stun) parts.Add("굳힘 (기를 모으는 적의 강타를 끊습니다)");
                if (s.Inflict != StatusKind.None && s.InflictChance > 0f) parts.Add(Statuses.Name(s.Inflict) + " " + Mathf.RoundToInt(s.InflictChance * 100f) + "%");
            }
            parts.Add(s.SpiritCost > 0 ? "기세 " + s.SpiritCost : s.QiCost > 0 ? "내공 " + s.QiCost : "내공 0");
            if (s.Cooldown > 0) parts.Add("재사용 " + s.Cooldown + "턴");
            return string.Join(" · ", parts);
        }

        private static string SkillBody(SkillSpec s)
        {
            string desc = s.Id == "basic" ? "기본 공격." : s.Description;
            string status = s.Inflict != StatusKind.None && s.InflictChance > 0f ? "\n" + Statuses.Describe(s.Inflict) : "";
            return SkillDetails(s) + (string.IsNullOrEmpty(desc) ? "" : "\n" + desc) + status;
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
                if (slot < items.Count) ShowTip(items[slot].Name, ItemTipText(items[slot]), ArtLibrary.Item(items[slot]));
                return;
            }
            if (slot == 0) ShowTip("공격", "기본 공격입니다. 내공을 쓰지 않고 기세가 오릅니다.", ArtLibrary.Icon("cmd_attack"));
            else if (slot >= 1 && slot <= Progression.MaxEquippedArts)
            {
                var arts = ArtsOf(me);
                if (slot - 1 < arts.Count) ShowTip(arts[slot - 1].Name, SkillBody(arts[slot - 1]), ArtLibrary.Art(arts[slot - 1].Id));
            }
            else if (slot == 9)
            {
                var ultimateSkill = UltimateOf(me);
                if (ultimateSkill == null) ShowTip("필살기 슬롯", "장착한 비기가 없습니다. 무공 탭에서 배운 비기를 장착하면 여기에서 쓸 수 있습니다. 기세가 가득 차면 쓸 수 있는 필살기 한 자리입니다.", ArtLibrary.Icon("ultimate_slot"));
                else ShowTip("필살기 · " + ultimateSkill.Name, SkillBody(ultimateSkill) + "\n기세가 가득 차면 쓸 수 있습니다.", ArtLibrary.Art(ultimateSkill.Id) ?? ArtLibrary.Icon("ultimate_slot"));
            }
            else if (slot == 5) ShowTip("방어", "다음 차례까지 받는 피해가 크게 줄어듭니다(피해 60% 감소). 기세가 조금 오릅니다.", ArtLibrary.Icon("cmd_guard"));
            else if (slot == 6) ShowTip("운기", "내공 +" + InteractiveBattle.MeditateQi + "과 체력을 조금 회복하고, 걸린 상태 이상을 풉니다.", ArtLibrary.Icon("cmd_meditate"));
            else if (slot == 7) ShowTip("아이템", "행낭의 소모품을 씁니다. 가장 다친 동료의 체력이 회복됩니다.", ArtLibrary.Icon("cmd_item"));
            else if (slot == 8 && _jointPartners.Count > 0)
            {
                if (_jointPartners.Count == 1) ShowJointTip(_jointPartners[0]);
                else ShowTip("합공", "기세 " + InteractiveBattle.JointSpiritCost + "를 쓰고 인연 있는 동료와 함께 연달아 칩니다. 동료는 다음 행동을 씁니다.", ArtLibrary.Icon("cmd_joint"));
            }
        }

        private static string ItemTipText(ItemData item)
        {
            var fx = new List<string>();
            if (item.HealPct > 0f) fx.Add("체력 " + Mathf.RoundToInt(item.HealPct * 100f) + "% 회복");
            if (item.QiRestore > 0) fx.Add("내공 " + (item.QiRestore >= 999 ? "가득" : "+" + item.QiRestore));
            if (item.SpiritGain > 0) fx.Add("기세 +" + item.SpiritGain);
            if (item.Cleanse) fx.Add("상태 이상 모두 해제");
            return (fx.Count > 0 ? string.Join(" · ", fx) + "\n" : "") + item.Description;
        }

        private void ShowJointTip(Combatant partner)
        {
            var companion = EarlyGameContent.FindCompanion(partner.Id);
            var spec = companion != null ? Bonds.JointSpec(companion, Bonds.Level(GameManager.Instance.Save, partner.Id), Bonds.ReferencePower(_battle.Player.Skills)) : null;
            string name = companion != null && !string.IsNullOrEmpty(companion.JointName) ? companion.JointName : "합공";
            string extra = spec == null ? "" : spec.Effect == SkillEffect.Stun ? "\n적을 굳혀 강타를 끊습니다." : spec.Effect == SkillEffect.GuardParty ? "\n파티 전체가 방어 태세를 취합니다." : spec.Effect == SkillEffect.HealAlly ? "\n가장 다친 동료를 치유합니다." : "";
            ShowTip(name + " (" + partner.Name + ")", "기세 " + InteractiveBattle.JointSpiritCost + " · 위력 ×" + (spec != null ? spec.Power.ToString("0.0") : "?") + "\n비류연과 " + partner.Name + "이(가) 연달아 칩니다. " + partner.Name + "은(는) 다음 행동을 씁니다." + extra, ArtLibrary.Icon("cmd_joint"));
        }

        /// <summary>Long press on an enemy: level, 무공 경지 and stats.</summary>
        private void ShowEnemyTip(EnemyCard card)
        {
            if (card == null || card.Data == null) return;
            var s = card.Unit.Stats;
            double power = card.Unit.RealmPowerBeforeBonus;
            var lines = new List<string>();
            lines.Add("Lv." + card.Data.DisplayLevel + "  ·  " + EnemyData.RankName(card.Data.Rank) + (card.Data.Rank == EnemyRank.Normal ? "" : "\n내공과 기세를 씁니다. 기세가 가득 차면 다음 공격이 강해지고, 내공이 모자라면 큰 기술의 힘이 빠집니다."));
            if (Realm.HasRealm(card.Data)) lines.Add("무공 경지  " + Realm.Label(Realm.StepOf(power)) + "\n종합 수치 " + System.Math.Round(power).ToString("N0"));
            else lines.Add("무공 경지  해당 없음");
            lines.Add("체력 " + card.Unit.Hp.ToString("N0") + " / " + s.MaxHp.ToString("N0") + "\n공격력 " + s.Attack.ToString("N0") + "   방어력 " + s.Defense.ToString("N0") + "   속도 " + s.Speed.ToString("N0"));
            string statusLines = StatusLines(card.Unit);
            if (statusLines.Length > 0) lines.Add(statusLines);
            string phaseLine = BossPhases.Describe(card.Unit);
            if (phaseLine.Length > 0) lines.Add(phaseLine);
            var moveStatus = new List<string>();
            foreach (var m in card.Unit.Moves ?? new List<EnemyMove>()) if (m.Inflict != StatusKind.None && !moveStatus.Contains(m.Name + "(" + Statuses.Name(m.Inflict) + ")")) moveStatus.Add(m.Name + "(" + Statuses.Name(m.Inflict) + ")");
            if (moveStatus.Count > 0) lines.Add("상태 이상을 거는 기술: " + string.Join(", ", moveStatus));
            ShowTip(card.Data.Name, string.Join("\n\n", lines), BattleFigure(card.Data.ArtKey));
        }

        /// <summary>"걸린 상태 이상" with each one's rule and turns left ("" when none).</summary>
        private static string StatusLines(Combatant unit)
        {
            if (unit.Statuses.Count == 0) return "";
            var list = new List<string>();
            foreach (var st in unit.Statuses) list.Add(Statuses.Describe(st.Kind) + " 남은 " + st.Turns + "턴");
            return "걸린 상태 이상\n" + string.Join("\n", list);
        }

        /// <summary>Tap on an ally: level, 경지, current gauges, stats and the skills they carry.</summary>
        private void ShowAllyTip(Combatant unit)
        {
            if (unit == null) return;
            var s = unit.Stats;
            var lines = new List<string>();
            bool hero = unit.Id == "player";
            var companion = hero ? null : EarlyGameContent.FindCompanion(unit.Id);
            int level = hero ? GameManager.Instance.Save.Level : CompanionGrowth.LevelOf(GameManager.Instance.Save, unit.Id);
            lines.Add("Lv." + level + "  ·  무공 경지 " + Realm.Label(unit.RealmStep) + "  (종합 " + System.Math.Round(unit.RealmPowerBeforeBonus).ToString("N0") + ")");
            lines.Add("체력 " + unit.Hp.ToString("N0") + " / " + s.MaxHp.ToString("N0") + "\n내공 " + unit.Qi.ToString("N0") + " / " + s.MaxQi.ToString("N0") + "\n기세 " + unit.Spirit + " / " + InteractiveBattle.MaxSpirit);
            lines.Add("공격력 " + s.Attack.ToString("N0") + "   방어력 " + s.Defense.ToString("N0") + "   속도 " + s.Speed.ToString("N0"));
            if (companion != null && !string.IsNullOrEmpty(companion.JointName))
            {
                int bond = Bonds.Level(GameManager.Instance.Save, companion.Id);
                lines.Add("인연 " + Bonds.LevelNames[bond] + " (" + Bonds.BonusText(bond) + ")  ·  합공 " + companion.JointName);
            }
            string statusLines = StatusLines(unit);
            if (statusLines.Length > 0) lines.Add(statusLines);
            var names = new List<string>();
            foreach (var k in unit.Skills) names.Add(k.Name);
            if (names.Count > 0) lines.Add("무공: " + string.Join(", ", names) + "\n(자세한 설명은 길게 누르세요)");
            var companionData = EarlyGameContent.FindCompanion(unit.Id);
            if (companionData != null)
            {
                var pending = CompanionGrowth.PendingArtLines(companionData, CompanionGrowth.LevelOf(GameManager.Instance.Save, unit.Id));
                if (pending.Count > 0) lines.Add("앞으로 배울 무공: " + string.Join(", ", pending));
            }
            ShowTip(unit.Name, string.Join("\n\n", lines), BattleFigure(hero ? "player" : unit.Id));
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
            ShowTip(unit.Name + " — 스킬", string.Join("\n\n", lines), BattleFigure(unit.Id));
        }

        /// <summary>A unit's battle picture: <c>Battle/&lt;id&gt;.png</c>, else the stand-in figure (default_assassin / default_woman / default_man, <see cref="Genders.DefaultFigure"/>). Never the <c>Characters/</c> portrait.</summary>
        private static Sprite BattleFigure(string artKey) { return ArtLibrary.Chibi(artKey) ?? ArtLibrary.DefaultChibi(artKey); }

        private void SetCommandsVisible(bool visible)
        {
            if (_commandHeader != null) _commandHeader.SetActive(visible);
            if (!visible && _turnNotice != null) _turnNotice.text = "";
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

            if (Aiming || AimingAlly)
            {
                if (slot == 7) CancelAim();
                return;
            }

            if (_jointMode)
            {
                if (slot == 7) { _jointMode = false; RefreshCommands(); return; }
                if (slot >= _jointPartners.Count) return;
                IssueJoint(_jointPartners[slot]);
                return;
            }

            if (_itemMode)
            {
                if (slot == 7) { _itemMode = false; RefreshCommands(); return; }
                var items = UsableItems();
                if (slot >= items.Count) return;
                var picked = items[slot];
                if (picked.HealPct > 0f || picked.QiRestore > 0 || picked.SpiritGain > 0 || picked.Cleanse) { BeginAllyAim(picked.Name, ally => PlayerCommand.UseItem(picked).WithAlly(ally)); return; }
                Issue(PlayerCommand.UseItem(picked));
                return;
            }

            SkillSpec chosen = null;
            if (slot == 0) chosen = SkillSpec.BasicAttack();
            else if (slot >= 1 && slot <= Progression.MaxEquippedArts)
            {
                var arts = ArtsOf(me);
                int i = slot - 1;
                if (i >= arts.Count || !_battle.CanUse(arts[i])) return;
                chosen = arts[i];
            }
            else if (slot == 9)
            {
                var ultimateSkill = UltimateOf(me);
                if (ultimateSkill == null || !_battle.CanUse(ultimateSkill)) return;
                chosen = ultimateSkill;
            }
            else if (slot == 8)
            {
                if (me.Spirit < InteractiveBattle.JointSpiritCost || _jointPartners.Count == 0) return;
                if (_jointPartners.Count == 1) { IssueJoint(_jointPartners[0]); return; }
                _jointMode = true;
                RefreshCommands();
                return;
            }
            else if (slot == 5) { Issue(PlayerCommand.Guard()); return; }
            else if (slot == 6) { Issue(PlayerCommand.Meditate()); return; }
            else if (slot == 7)
            {
                if (UsableItems().Count == 0) return;
                _itemMode = true;
                RefreshCommands();
                return;
            }
            else return;
            if (chosen == null) return;
            if (!_battle.CanReach(chosen))
            {
                _log.text = "닿는 적이 없습니다. 다른 무공을 골라 주세요.";
                return;
            }
            if (chosen.Effect == SkillEffect.HealAlly || chosen.Effect == SkillEffect.Cleanse)
            {
                var healArt = chosen;
                BeginAllyAim(healArt.Name, ally => PlayerCommand.UseSkill(healArt, null).WithAlly(ally));
                return;
            }
            // Attacks that need a target ask for one first; area arts, evasion, healing and guards go off at once.
            if (InteractiveBattle.NeedsTarget(chosen) && !chosen.AllEnemies) BeginAim(chosen, null);
            else Issue(PlayerCommand.UseSkill(chosen, null));
        }

        private void OnSpeedPressed()
        {
            _speed = _speed == 1 ? 2 : _speed == 2 ? 3 : 1;
            _speedLabel.text = "속도 x" + _speed;
            GameManager.Instance.Save.Settings.BattleSpeed = _speed;
        }

        private void OnFleePressed()
        {
            if (_fled || _fleeAsking) return;
            _fleeAsking = true;
            string body = _promotionRun ? "지금 비무를 그만두고 돌아갑니다.\n낸 입장료는 돌려받지 못합니다."
                : "지금 전투를 그만두고 돌아갑니다.\n이번 전투의 보상은 받지 못합니다. 이미 이긴 전투에서 얻은 경험과 두는 남습니다.";
            var window = UiFactory.Confirm(_root, "전투를 포기할까요?", body, "포기", () => { _fleeAsking = false; Flee(); });
            // 「취소」 destroys the window without a callback: clear the flag when it goes away
            window.gameObject.AddComponent<OnDestroyHook>().Done = () => _fleeAsking = false;
        }

        private void Flee()
        {
            _fled = true;
            CancelBasicMotion();
            _panelDone = true;
            _retry = false;
        }

        // ------------------------------------------------------------------ playback

        private float Wait(float seconds) { return seconds / Pace; }

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

        // The engine resolves a whole round (our action plus the enemy replies) before the UI animates it, so while
        // events play the party bars show the HP from the events rather than the already-final unit HP.
        private bool _animating;

        private IEnumerator PlayEvents(List<BattleEvent> events)
        {
            _animating = true;
            _hudActor = null; _actorAction = null;
            RefreshActorHud();
            RefreshTurnOrder();
            try { yield return PlayEventsInner(events); }
            finally
            {
                CancelBasicMotion();
                _animating = false;
                _hudActor = null; _actorAction = null;
            }
            RefreshParty();
            RefreshCards();
            RefreshTurnOrder();
        }

        private void ShowHpAfter(string unitId, int hp)
        {
            var panel = PanelOf(unitId);
            if (panel != null) panel.ShownHp = hp;
        }

        private IEnumerator PlayEventsInner(List<BattleEvent> events)
        {
            _cutInKey = "";
            for (int eventIndex = 0; eventIndex < events.Count; eventIndex++)
            {
                var ev = events[eventIndex];
                if (_fled) yield break;
                if (ev.Kind != EventKind.Attack) _cutInKey = "";
                var actorPanel = PanelOf(ev.ActorId);
                bool byParty = actorPanel != null;
                if (actorPanel != null && ev.Kind != EventKind.Status) { actorPanel.ShownQi = ev.ActorQiAfter; actorPanel.ShownSpirit = ev.ActorSpiritAfter; }   // status events carry no gauges
                string actorName = NameOf(ev.ActorId);
                PresentAction(ev);

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
                        bool finishMotion = EndsAttackSequence(events, eventIndex);
                        if (byParty) yield return PartyHit(ev, finishMotion);
                        else yield return EnemyHit(ev, finishMotion);
                        break;
                    case EventKind.Guard:
                        SoundManager.Sfx("guard");
                        _log.text = ev.Message;
                        foreach (var p in _panels) if (_battle.IsGuarded(p.Unit)) Float(p.Center, "방어!", GuardBlue, 52);
                        RefreshParty();
                        yield return new WaitForSeconds(Wait(SmallDelay));
                        break;
                    case EventKind.Meditate:
                        SoundManager.Sfx("heal");
                        _log.text = ev.Message;
                        if (actorPanel != null) Float(actorPanel.Center, "+" + ev.Heal, HpGreen, 60);
                        ShowHpAfter(ev.TargetId, ev.TargetHpAfter);
                        RefreshParty();
                        yield return new WaitForSeconds(Wait(SmallDelay));
                        break;
                    case EventKind.Heal:
                        SoundManager.Sfx("heal");
                        _log.text = ev.Message;
                        var healed = PanelOf(ev.TargetId);
                        if (healed != null) Float(healed.Center, "+" + ev.Heal, HpGreen, 70);
                        ShowHpAfter(ev.TargetId, ev.TargetHpAfter);
                        RefreshParty();
                        yield return new WaitForSeconds(Wait(StepDelay));
                        break;
                    case EventKind.Item:
                        SoundManager.Sfx("heal");
                        _log.text = ev.Message;
                        var itemTarget = PanelOf(ev.TargetId);
                        if (ev.Heal > 0 && itemTarget != null) Float(itemTarget.Center, "+" + ev.Heal, HpGreen, 70);
                        else if ((itemTarget ?? actorPanel) != null) Float((itemTarget ?? actorPanel).Center, "회복", QiBlue, 56);
                        ShowHpAfter(ev.TargetId, ev.TargetHpAfter);
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
                    case EventKind.Status:
                        {
                            var sufferPanel = PanelOf(ev.TargetId);
                            var sufferCard = CardOf(ev.TargetId);
                            Vector2? at = sufferPanel != null ? sufferPanel.Center : sufferCard != null ? sufferCard.Center : (Vector2?)null;
                            _log.text = ev.Message;
                            if (at != null)
                            {
                                if (ev.StatusCured) Float(at.Value, "해제", HpGreen, 58);
                                else if (ev.Damage > 0) Float(at.Value + new Vector2(0f, -0.03f), Statuses.Name(ev.Status) + " " + ev.Damage, ColorOf(ev.Status), 58);
                                else Float(at.Value + new Vector2(0f, 0.05f), Statuses.Name(ev.Status) + "!", ColorOf(ev.Status), 62);
                            }
                            if (ev.Damage > 0)
                            {
                                SoundManager.Sfx("hit");
                                if (sufferPanel != null) ShowHpAfter(ev.TargetId, ev.TargetHpAfter);
                                if (sufferCard != null)
                                {
                                    sufferCard.ShownHp = ev.TargetHpAfter;
                                    sufferCard.Hp.SetPct(sufferCard.Unit.Stats.MaxHp > 0 ? (float)ev.TargetHpAfter / sufferCard.Unit.Stats.MaxHp : 0f);
                                    sufferCard.HpText.text = ev.TargetHpAfter + " / " + sufferCard.Unit.Stats.MaxHp;
                                    if (ev.TargetDefeated) { StartCoroutine(Collapse(sufferCard)); sufferCard.Intent.text = ev.Retreated ? "물러났다" : "쓰러졌다"; }
                                }
                            }
                            RefreshCards();
                            RefreshParty();
                            yield return new WaitForSeconds(Wait(SmallDelay));
                            break;
                        }
                    case EventKind.Phase:
                        {
                            // 보스 단계 전환: a shake and a banner, then the boss's lines over the battle
                            SoundManager.Sfx("charge");
                            SoundManager.Haptic(80);
                            _log.text = ev.Message;
                            _phaseSeen = true;
                            var phaseCard = CardOf(ev.ActorId);
                            if (phaseCard != null) phaseCard.ShownPhase = ev.HitIndex;
                            RefreshFocusHud();
                            if (phaseCard != null) { Float(phaseCard.Center, "기세 변화!", UiFactory.Red, 70); yield return Shake(phaseCard.Root, 14f); }
                            yield return Banner(ev.Message, 0.9f);
                            if (ev.Lines != null && ev.Lines.Length > 0 && !_fled)
                                yield return ShowStory(phaseCard != null ? phaseCard.Unit.Name : "", ev.Lines);
                            RefreshCards();
                            break;
                        }
                    case EventKind.StunSkip:
                        _log.text = ev.Message;
                        var frozen = CardOf(ev.ActorId);
                        if (frozen != null) Float(frozen.Center, "굳음", GuardBlue, 64);
                        RefreshCards();
                        yield return new WaitForSeconds(Wait(SmallDelay));
                        break;
                }
            }
        }

        private bool _phaseSeen;   // a boss changed phase in this screen session (shows the 「보스 단계」 help once)

        private static readonly Color StatusColor = new Color(0.86f, 0.62f, 1f, 1f);

        private static Color ColorOf(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Poison: return new Color(0.55f, 0.95f, 0.35f, 1f);
                case StatusKind.Burn: return new Color(1f, 0.55f, 0.2f, 1f);
                case StatusKind.Chill: return new Color(0.6f, 0.85f, 1f, 1f);
                case StatusKind.InnerWound: return new Color(0.85f, 0.6f, 1f, 1f);
                case StatusKind.Bleed: return new Color(1f, 0.3f, 0.3f, 1f);
            }
            return StatusColor;
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

        private IEnumerator PartyHit(BattleEvent ev, bool finishMotion)
        {
            var card = CardOf(ev.TargetId);
            if (card == null) yield break;
            var striker = PanelOf(ev.ActorId);
            bool approach = IsApproachAttack(ev) && _basicMotion != null;
            if (approach && !_basicMotion.IsActive)
            {
                RefreshParty();
                yield return _basicMotion.Approach(PartyFigure(striker), EnemyFigure(card), _fx, Pace, striker != null ? striker.Root : null);
                if (_fled) { CancelBasicMotion(); yield break; }
            }
            else if (!approach && !ev.Ranged && striker != null && ev.HitIndex == 0) StartCoroutine(Lunge(striker.Root, new Vector2(130f, 0f)));
            SlashFx(card.Center, ev.HitIndex, ev.IsCrit, striker != null && striker.Unit.Skills.Find(x => x.Id == ev.SkillId) is SkillSpec sp && sp.SpiritCost >= 100);
            if (ev.IsCrit)
            {
                StartCoroutine(Punch(1.06f));
                if (!approach) yield return new WaitForSeconds(Wait(0.07f));
            }
            SoundManager.Sfx(ev.IsCrit ? "hit_crit" : "hit");
            if (ev.IsCrit) SoundManager.Haptic(40);
            Float(card.Center + new Vector2(UnityEngine.Random.Range(-0.05f, 0.05f), 0.02f * ev.HitIndex), ev.Damage + (ev.IsCrit ? "!" : ""), ev.IsCrit ? UiFactory.Gold : UiFactory.PaperText, ev.IsCrit ? 84 : 64);
            if (ev.Harmony) Float(card.Center + new Vector2(0f, 0.08f), "합격!", UiFactory.Gold, 80);
            if (ev.Interrupted) Float(card.Center + new Vector2(0f, -0.07f), "차단!", UiFactory.Gold, 72);
            else if (ev.Stunned) Float(card.Center + new Vector2(0f, -0.07f), "굳음", GuardBlue, 60);
            card.ShownHp = ev.TargetHpAfter;
            RefreshFocusHud();
            card.Hp.SetPct(card.Unit.Stats.MaxHp > 0 ? (float)ev.TargetHpAfter / card.Unit.Stats.MaxHp : 0f);
            card.HpText.text = ev.TargetHpAfter + " / " + card.Unit.Stats.MaxHp;
            card.Spirit.SetPct(card.Unit.Spirit / (float)InteractiveBattle.MaxSpirit);
            card.SpiritText.text = card.Unit.Spirit + " / " + InteractiveBattle.MaxSpirit;
            StartCoroutine(Shake(card.Root, ev.IsCrit ? 22f : 14f));
            if (ev.TargetDefeated) { StartCoroutine(Collapse(card)); card.Intent.text = ev.Retreated ? "물러났다" : "쓰러졌다"; if (ev.Retreated) _log.text = ev.Message; }
            RefreshParty();
            if (approach && finishMotion && _basicMotion.IsActive)
            {
                yield return new WaitForSeconds(Wait(.08f));
                yield return _basicMotion.Return(Pace);
            }
            else yield return new WaitForSeconds(Wait(ev.HitCount > 1 ? HitDelay : StepDelay));
        }

        private IEnumerator EnemyHit(BattleEvent ev, bool finishMotion)
        {
            var attacker = CardOf(ev.ActorId);
            var victim = PanelOf(ev.TargetId);
            if (ev.Awakening && ev.HitIndex == 0) yield return AwakeningCutIn(attacker, NameOf(ev.ActorId));
            bool approach = IsApproachAttack(ev) && _basicMotion != null;
            if (approach && !_basicMotion.IsActive)
            {
                RefreshCards();
                yield return _basicMotion.Approach(EnemyFigure(attacker), PartyFigure(victim), _fx, Pace, attacker != null ? attacker.Root : null);
                if (_fled) { CancelBasicMotion(); yield break; }
            }
            else if (!approach && !ev.Ranged && attacker != null && ev.HitIndex == 0) StartCoroutine(Lunge(attacker.Root, new Vector2(-130f, 0f)));
            if (attacker != null && ev.HitIndex == 0 && ev.Burst) { Float(attacker.Center + new Vector2(0f, 0.06f), "기세 폭발!", UiFactory.Red, 70); SoundManager.Haptic(40); }
            if (attacker != null && ev.HitIndex == 0 && ev.Weakened) Float(attacker.Center + new Vector2(0f, 0.06f), "내공 부족", GuardBlue, 56);
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
                    victim.ShownHp = ev.TargetHpAfter;
                    victim.ShownSpirit = ev.TargetSpiritAfter;
                    victim.Hp.SetPct((float)ev.TargetHpAfter / Math.Max(1, victim.Unit.Stats.MaxHp));
                    victim.HpText.text = ev.TargetHpAfter + " / " + victim.Unit.Stats.MaxHp;
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
            if (approach && finishMotion && _basicMotion.IsActive)
            {
                yield return new WaitForSeconds(Wait(.08f));
                yield return _basicMotion.Return(Pace);
            }
            else yield return new WaitForSeconds(Wait(ev.HitCount > 1 ? HitDelay : StepDelay));
        }

        private static bool IsApproachAttack(BattleEvent ev)
        {
            return ev.Kind == EventKind.Attack && !ev.AllEnemies && !ev.Ranged;
        }

        private static bool EndsAttackSequence(List<BattleEvent> events, int index)
        {
            var current = events[index];
            for (int next = index + 1; next < events.Count; next++)
            {
                var hit = events[next];
                if (hit.Kind != EventKind.Attack) continue;
                return hit.ActorId != current.ActorId || hit.SkillId != current.SkillId
                    || hit.TargetId != current.TargetId || hit.HitIndex <= current.HitIndex;
            }
            return true;
        }

        private static Graphic PartyFigure(PartyPanel panel)
        {
            return panel == null ? null : panel.Art != null ? (Graphic)panel.Art : panel.FigureGlyph;
        }

        private static Graphic EnemyFigure(EnemyCard card)
        {
            return card == null ? null : card.Art != null ? (Graphic)card.Art : card.Glyph;
        }

        private void CancelBasicMotion() { if (_basicMotion != null) _basicMotion.Cancel(); }

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
            float duration = 0.7f / Pace;
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

        // Several motions can overlap on one panel (an all-enemies art hits every card at once, a multi-hit art lunges and shakes together).
        // Each used to remember "where I am now" as its origin, so the one that finished last left the panel displaced for good.
        // Every motion now returns to the panel's resting position, recorded the first time it is moved.
        private readonly Dictionary<RectTransform, Vector2> _home = new Dictionary<RectTransform, Vector2>();

        private Vector2 HomeOf(RectTransform rt)
        {
            Vector2 home;
            if (!_home.TryGetValue(rt, out home)) { home = rt.anchoredPosition; _home[rt] = home; }
            return home;
        }

        private IEnumerator Shake(RectTransform rt, float amount)
        {
            if (rt == null) yield break;
            Vector2 origin = HomeOf(rt);
            for (int i = 0; i < 6; i++)
            {
                if (rt == null) yield break;
                rt.anchoredPosition = origin + new Vector2(UnityEngine.Random.Range(-amount, amount), UnityEngine.Random.Range(-amount, amount) * 0.5f);
                yield return new WaitForSeconds(0.02f / Pace);
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
            Vector2 origin = HomeOf(rt);
            float out_ = 0.09f / Pace, back = 0.14f / Pace;
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
            float d = 0.22f / Pace;
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
            float d = 0.30f / Pace;
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
            float d = 0.18f / Pace;
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
            float d = 0.45f / Pace;
            Vector2 origin = card.Root.anchoredPosition;
            for (float e = 0f; e < d; e += Time.deltaTime)
            {
                if (card.Root == null) yield break;
                float k = e / d;
                card.Root.anchoredPosition = origin + new Vector2(Mathf.Sin(k * 40f) * 10f * (1f - k), -40f * k);
                if (card.Art != null) SetAlpha(card.Art, Mathf.Lerp(1f, 0.25f, k));
                yield return null;
            }
            if (card.Root != null) card.Root.anchoredPosition = origin + new Vector2(0f, -40f);
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

        /// <summary>The boss phase strike gets its own loud cut-in: red full-width band, the boss's name and the strike's share of its heavy blow, so it never reads like an ordinary move.</summary>
        private IEnumerator AwakeningCutIn(EnemyCard attacker, string bossName)
        {
            string share = attacker != null && attacker.Unit.PhaseIndex == 1 ? "70%" : "100%";
            _log.text = bossName + "의 " + BossPhases.AwakeningName + "! (기존 강타의 " + share + ")";
            SoundManager.Sfx("ultimate");
            SoundManager.Haptic(120);
            if (attacker != null) Float(attacker.Center + new Vector2(0f, 0.08f), "각성!", UiFactory.Red, 84);
            _banner.fontSize = 92;
            _banner.color = new Color(1f, 0.42f, 0.32f, 1f);
            _banner.text = "\u300e " + BossPhases.AwakeningName + " \u300f\n<size=48>" + bossName + " · 위력 " + share + "</size>";
            _bannerBg.color = new Color(0.32f, 0.03f, 0.02f, 0.9f);
            StartCoroutine(Punch(1.12f));
            if (attacker != null) StartCoroutine(Shake(attacker.Root, 18f));
            yield return new WaitForSeconds(Wait(0.95f));
            _banner.text = "";
            _banner.fontSize = 64;
            _banner.color = UiFactory.Gold;
            _bannerBg.color = new Color(0f, 0f, 0f, 0f);
        }

        private IEnumerator Banner(string text, float seconds)
        {
            _banner.text = text;
            _bannerBg.color = new Color(0f, 0f, 0f, 0.65f);
            yield return new WaitForSeconds(seconds / Pace);
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
            _storyBackdrop.sprite = _backdropImage.sprite;
            _storyBackdrop.color = _backdropImage.sprite != null ? Color.white : Backdrop;
            _storyRoot.SetActive(true);
            Canvas.ForceUpdateCanvases();
            for (int i = 0; i < lines.Length && !_fled && !_storySkip; i++)
            {
                var line = StoryLine.Parse(lines[i]);
                if (line.IsDialogue && !line.IsProtagonist) right = line.Speaker;
                _storyTitle.text = title;
                ShowPortrait(0, line.IsDialogue || right != null ? left : null, line.IsProtagonist);
                ShowPortrait(1, right, line.IsDialogue && !line.IsProtagonist);

                bool rightSide = line.IsDialogue && !line.IsProtagonist;
                _namePlate.gameObject.SetActive(line.IsDialogue);
                _namePlate.anchorMin = new Vector2(rightSide ? .68f : .07f, .34f);
                _namePlate.anchorMax = new Vector2(rightSide ? .93f : .32f, .405f);
                _nameTag.text = line.IsDialogue ? line.Speaker : "";
                _nameTag.color = line.IsProtagonist ? UiFactory.Gold : UiFactory.PaperText;
                _storyText.fontStyle = FontStyle.Normal;
                _storyText.color = UiFactory.PaperText;
                int offset = 0, pageIndex = 0;
                bool firstPage = true;
                while ((firstPage || offset < line.Text.Length) && !_fled && !_storySkip)
                {
                    Canvas.ForceUpdateCanvases();
                    var size = _storyText.rectTransform.rect.size;
                    var pages = UiFactory.Pages(_storyText, line.Text.Substring(offset));
                    string page = pages[0];
                    _storyProgress.text = (i + 1) + " / " + lines.Length
                        + (pageIndex + pages.Count > 1 ? "  ·  " + (pageIndex + 1) + " / " + (pageIndex + pages.Count) + "쪽" : "")
                        + "    눌러서 계속";
                    _storyText.text = "";
                    _panelDone = false;
                    bool resized = false;
                    // Resize from a notch or window change repaginates from the current page's start.
                    // A tap reveals the page; the next tap advances. Never split a surrogate pair.
                    for (int c = 1; c <= page.Length && !_panelDone && !_storySkip && !_fled; c++)
                    {
                        if ((_storyText.rectTransform.rect.size - size).sqrMagnitude > 1f) { resized = true; break; }
                        if (char.IsHighSurrogate(page[c - 1]) && c < page.Length) c++;
                        _storyText.text = page.Substring(0, c);
                        yield return new WaitForSeconds(.022f);
                    }
                    if (_storySkip || _fled) break;
                    if (resized || (_storyText.rectTransform.rect.size - size).sqrMagnitude > 1f) continue;
                    _storyText.text = page;
                    _panelDone = false;
                    while (!_panelDone && !_storySkip && !_fled)
                    {
                        if ((_storyText.rectTransform.rect.size - size).sqrMagnitude > 1f) { resized = true; break; }
                        yield return null;
                    }
                    if (resized || (_storyText.rectTransform.rect.size - size).sqrMagnitude > 1f) continue;
                    offset += page.Length;
                    pageIndex++;
                    firstPage = false;
                }
            }
            _storyRoot.SetActive(false);
            _storySkip = false;
            StartCoroutine(GuardTaps());   // taps that were meant for the story must not fall through to the screen underneath
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
            StartCoroutine(GuardTaps());
        }
    }
}
