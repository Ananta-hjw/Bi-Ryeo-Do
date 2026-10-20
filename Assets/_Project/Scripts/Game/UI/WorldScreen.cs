using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>강호 탭: the story map. Pick an unlocked stage to fight it; cleared stages can be replayed for exp and 두.</summary>
    public class WorldScreen : MonoBehaviour
    {
        private sealed class Row
        {
            public StageData Stage;
            public Text Title, Detail, ButtonLabel;
            public Image ButtonImage, Band;
            public Button Button;
        }

        private readonly List<Row> _rows = new List<Row>();
        private StarPanel _stars;
        private SaveData _syncedSave;
        private DailyPanel _daily;
        private Text _dailyRowDetail, _dailyBadge;
        private Action _onStatus;
        private Text _starRowDetail, _starBadge, _starsTitle;
        private readonly Image[] _diffTabs = new Image[StageStars.DifficultyCount];
        private Text _towerRowDetail, _lvText, _dooText, _rankText, _promoRowDetail, _partyRowDetail, _questRowDetail, _fortuneRowDetail;
        private Text _promoBadge, _partyBadge, _questBadge, _fortuneBadge, _featTitle, _featSub, _featBtnLabel;
        private UiFactory.Bar _expBar;
        private Image _featArt;
        private Button _featBtn;
        private StageData _featStage;
        private Action<StageData> _onStartRef;
        // Home / lobby card
        private Image _lobbyBg, _lobbyPortrait;
        private Text _lobbyGlyph, _lobbyName, _lobbyLine, _lobbyRealm, _lobbyPower;
        private int _lobbyStep;
        private float _lobbyTimer, _lobbyReadTime = 12f;
        private RectTransform _promoOverlay, _partyOverlay, _questOverlay, _fortuneOverlay, _settingsOverlay, _towerOverlay;
        private bool _confirmReset;
        private Action<StageData> _onStart;
        private Action<PromotionOffer> _onPromote;
        private bool _built;
        private RectTransform _homeRoot, _stageOverlay;
        private Text _stageRowDetail;
        private bool _reopenStages;

        public void Build(RectTransform root, Action<StageData> onStart, Action<PromotionOffer> onPromote, Action onStatus)
        {
            if (_built) return;
            _built = true;
            _onStatus = onStatus;
            _onPromote = onPromote;
            _onStart = onStart;

            _homeRoot = root;
            _onStartRef = onStart;
            var stages = EarlyGameContent.Stages;

            // Portrait-led portrait layout: identity, character, utility rail, journey and activity cards.
            _lobbyBg = UiFactory.Panel("Scenery", root, Vector2.zero, Vector2.one, UiFactory.Parchment);
            _lobbyBg.preserveAspect = false;
            UiFactory.Panel("InkWash", root, Vector2.zero, Vector2.one, new Color(.02f, .07f, .075f, .62f));
            UiFactory.Panel("FloorShade", root, Vector2.zero, new Vector2(1f, .44f), new Color(.025f, .06f, .065f, .84f));
            UiFactory.Glyph("halo", root, new Vector2(-.08f, .43f), new Vector2(.78f, .91f), new Color(.75f, .66f, .43f, .16f));
            UiFactory.Heading(UiFactory.Label("Masthead", root, new Vector2(.04f, .873f), new Vector2(.54f, .927f), "비뢰도", 58, UiFactory.PaperText, TextAnchor.MiddleLeft));
            UiFactory.Label("ChapterLabel", root, new Vector2(.57f, .88f), new Vector2(.96f, .916f), "강호 · 나의 여정", 30, UiFactory.Gold, TextAnchor.MiddleRight, FontStyle.Normal);
            UiFactory.Panel("MastheadRule", root, new Vector2(.04f, .865f), new Vector2(.96f, .866f), UiFactory.GoldDim);

            _lobbyPortrait = UiFactory.Panel("Portrait", root, new Vector2(.01f, .44f), new Vector2(.75f, .86f), Color.white);
            _lobbyPortrait.preserveAspect = true;
            _lobbyGlyph = UiFactory.Heading(UiFactory.Label("Glyph", _lobbyPortrait.transform, Vector2.zero, Vector2.one, "", 240, UiFactory.PaperText));
            var talk = UiFactory.Rect("TalkTap", root, new Vector2(.01f, .445f), new Vector2(.75f, .86f));
            var talkImg = talk.gameObject.AddComponent<Image>();
            talkImg.color = Color.clear;
            var talkBtn = talk.gameObject.AddComponent<Button>();
            talkBtn.targetGraphic = talkImg;
            talkBtn.transition = Selectable.Transition.None;
            talkBtn.onClick.AddListener(() => { _lobbyStep++; _lobbyTimer = 0f; ShowLobby(true); });
            UiFactory.Label("TapHint", root, new Vector2(.06f, .505f), new Vector2(.69f, .529f), "인물을 눌러 이야기하기", 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);

            var plate = UiFactory.Panel("NamePlate", root, new Vector2(.035f, .445f), new Vector2(.74f, .50f), new Color(.055f, .10f, .105f, .88f));
            UiFactory.Panel("IdentityAccent", plate.transform, Vector2.zero, new Vector2(.008f, 1f), UiFactory.Red);
            _lobbyName = UiFactory.Heading(UiFactory.Label("Name", plate.transform, new Vector2(.04f, .08f), new Vector2(.43f, .94f), "", 38, UiFactory.PaperText, TextAnchor.MiddleLeft));
            _lobbyRealm = UiFactory.Label("Realm", plate.transform, new Vector2(.46f, .43f), new Vector2(.98f, .96f), "", 30, UiFactory.Gold, TextAnchor.MiddleLeft);
            _lobbyPower = UiFactory.Label("Power", plate.transform, new Vector2(.46f, .03f), new Vector2(.98f, .43f), "", 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);

            var bubble = UiFactory.Panel("Dialogue", root, new Vector2(.035f, .352f), new Vector2(.965f, .433f), UiFactory.Ink);
            UiFactory.Frame(bubble.transform, UiFactory.GoldDim, 2f);
            UiFactory.Heading(UiFactory.Label("Quote", bubble.transform, new Vector2(.02f, .25f), new Vector2(.10f, .95f), "“", 62, UiFactory.Gold));
            _lobbyLine = UiFactory.Body(UiFactory.ScrollBody(UiFactory.Label("Line", bubble.transform, new Vector2(.12f, .1f), new Vector2(.95f, .91f), "", 30, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal), 30), 1.2f);

            var journey = UiFactory.Panel("Journey", root, new Vector2(.035f, .213f), new Vector2(.965f, .340f), UiFactory.InkSoft);
            _featArt = UiFactory.Panel("Landscape", journey.transform, Vector2.zero, Vector2.one, new Color(.36f, .43f, .36f, .25f));
            _featArt.sprite = ArtLibrary.Background(1);
            UiFactory.Frame(journey.transform, UiFactory.Gold, 3f);
            UiFactory.Label("JourneyKicker", journey.transform, new Vector2(.035f, .76f), new Vector2(.7f, .96f), "다 음  여 정", 30, UiFactory.Gold, TextAnchor.MiddleLeft);
            _featTitle = UiFactory.Heading(UiFactory.Label("FeatTitle", journey.transform, new Vector2(.035f, .43f), new Vector2(.73f, .77f), "", 36, UiFactory.PaperText, TextAnchor.MiddleLeft));
            _featSub = UiFactory.Body(UiFactory.ScrollBody(UiFactory.Label("FeatSub", journey.transform, new Vector2(.035f, .06f), new Vector2(.73f, .43f), "", 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal), 30), 1.1f);
            _featBtn = UiFactory.Button("Go", journey.transform, new Vector2(.76f, .17f), new Vector2(.96f, .79f), "출전", UiFactory.Red, OnFeaturedPressed, 32);
            _featBtnLabel = _featBtn.GetComponentInChildren<Text>();

            _dailyRowDetail = Icon(root, .78f, .965f, .731f, .837f, "일일 임무", () => _daily.Open(), out _dailyBadge, 24, "daily");
            _questRowDetail = Icon(root, .78f, .965f, .613f, .719f, "의뢰 게시판", OpenQuestOverlay, out _questBadge, 24, "quest");
            _fortuneRowDetail = Icon(root, .78f, .965f, .495f, .601f, "기연", OpenFortuneOverlay, out _fortuneBadge, 24, "fortune");

            Text stageBadge, towerBadge;
            _stageRowDetail = Icon(root, .035f, .49f, .115f, .201f, "스테이지", OpenStageOverlay, out stageBadge, 30, "stage");
            stageBadge.transform.parent.gameObject.SetActive(false);
            _promoRowDetail = Icon(root, .505f, .965f, .115f, .201f, "승급 시험장", OpenPromotionOverlay, out _promoBadge, 30, "promotion");
            _partyRowDetail = Icon(root, .035f, .335f, .019f, .103f, "파티 편성", OpenPartyOverlay, out _partyBadge, 24, "party");
            _towerRowDetail = Icon(root, .35f, .65f, .019f, .103f, "무한 비무대", OpenTowerOverlay, out towerBadge, 24, "tower");
            towerBadge.transform.parent.gameObject.SetActive(false);
            _starRowDetail = Icon(root, .665f, .965f, .019f, .103f, "별 보상", () => _stars.Open(), out _starBadge, 24, "star");

            BuildStatusBar(root);

            // ---- Stage list (opened by 스테이지 출전): difficulty picker and every stage.
            const float gap = 16f, cardH = 150f;
            _stageOverlay = UiFactory.Rect("StageOverlay", root, Vector2.zero, new Vector2(1f, 0.92f));
            _stageOverlay.gameObject.AddComponent<Image>().color = UiFactory.Parchment;
            _starsTitle = UiFactory.Label("Title", _stageOverlay, new Vector2(0.05f, 0.925f), new Vector2(0.95f, 0.995f), "스테이지", 42, UiFactory.Gold);
            var viewport = UiFactory.Rect("Viewport", _stageOverlay, new Vector2(0f, 0.085f), new Vector2(1f, 0.92f));
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UiFactory.Rect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f));
            content.pivot = new Vector2(0.5f, 1f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            UiFactory.ScrollIndicator(scroll, viewport);
            float y = gap;

            // Difficulty picker for the whole list
            var diffRow = Place(content, 0.04f, 0.96f, y, 84f, new Color(0f, 0f, 0f, 0f));
            for (int d = 0; d < StageStars.DifficultyCount; d++)
            {
                int diffIndex = d;
                float x0 = d * 0.335f;
                var btn = UiFactory.Button("Diff" + d, diffRow, new Vector2(x0, 0.04f), new Vector2(x0 + 0.325f, 0.96f), StageStars.DifficultyNames[d], UiFactory.InkSoft, () => { GameManager.Instance.Save.Difficulty = diffIndex; GameManager.Instance.SaveNow(); Refresh(); }, 30);
                _diffTabs[d] = btn.GetComponent<Image>();
            }
            ReadingList.Register(diffRow, 100f, gap);
            y += 84f + gap;

            for (int i = 0; i < stages.Length; i++)
            {
                var st = stages[i];
                var rt = Place(content, 0.04f, 0.96f, y, cardH, UiFactory.Ink);
                UiFactory.Frame(rt, UiFactory.GoldDim, 2f);
                var row = new Row { Stage = st };
                var badge = UiFactory.Panel("Badge", rt, new Vector2(0.025f, 0.14f), new Vector2(0.135f, 0.86f), new Color(0.20f, 0.17f, 0.10f, 1f));
                UiFactory.Frame(badge.transform, UiFactory.Gold, 2f);
                UiFactory.Label("Num", badge.transform, Vector2.zero, Vector2.one, st.Number.ToString(), 46, UiFactory.Gold);
                row.Title = UiFactory.Label("Title", rt, new Vector2(0.16f, 0.50f), new Vector2(0.64f, 0.96f), "", 36, UiFactory.PaperText, TextAnchor.MiddleLeft);
                row.Detail = UiFactory.Label("Detail", rt, new Vector2(0.16f, 0.04f), new Vector2(0.64f, 0.50f), "", 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                row.Button = UiFactory.Button("Go" + i, rt, new Vector2(0.66f, 0.15f), new Vector2(0.97f, 0.85f), "", UiFactory.Red, () => OnStagePressed(st, onStart, true), 34);
                row.ButtonImage = row.Button.GetComponent<Image>();
                row.ButtonLabel = row.Button.GetComponentInChildren<Text>();
                _rows.Add(row);
                ReadingList.Register(rt, Mathf.Max(240f, cardH), gap);
                y += cardH + gap;
            }
            content.sizeDelta = new Vector2(0f, y + gap);
            UiFactory.Button("Close", _stageOverlay, new Vector2(0.3f, 0.012f), new Vector2(0.7f, 0.072f), "닫기", UiFactory.InkSoft, () => { _stageOverlay.gameObject.SetActive(false); Refresh(); }, 34);
            _stageOverlay.gameObject.SetActive(false);

            _promoOverlay = UiFactory.Rect("PromotionOverlay", root, Vector2.zero, new Vector2(1f, 0.92f));
            var overlayBg = _promoOverlay.gameObject.AddComponent<Image>();
            overlayBg.color = UiFactory.Parchment;
            _promoOverlay.gameObject.SetActive(false);

            _partyOverlay = UiFactory.Rect("PartyOverlay", root, Vector2.zero, new Vector2(1f, 0.92f));
            var partyBg = _partyOverlay.gameObject.AddComponent<Image>();
            partyBg.color = UiFactory.Parchment;
            _partyOverlay.gameObject.SetActive(false);

            _prepOverlay = UiFactory.Rect("PrepOverlay", root, Vector2.zero, new Vector2(1f, 0.92f));
            _prepOverlay.gameObject.AddComponent<Image>().color = UiFactory.Parchment;
            _prepOverlay.gameObject.SetActive(false);

            _questOverlay = UiFactory.Rect("QuestOverlay", root, Vector2.zero, new Vector2(1f, 0.92f));
            _questOverlay.gameObject.AddComponent<Image>().color = UiFactory.Parchment;
            _questOverlay.gameObject.SetActive(false);

            _fortuneOverlay = UiFactory.Rect("FortuneOverlay", root, Vector2.zero, new Vector2(1f, 0.92f));
            _fortuneOverlay.gameObject.AddComponent<Image>().color = UiFactory.Parchment;
            _fortuneOverlay.gameObject.SetActive(false);

            _settingsOverlay = UiFactory.Rect("SettingsOverlay", root, Vector2.zero, new Vector2(1f, 0.92f));
            _settingsOverlay.gameObject.AddComponent<Image>().color = UiFactory.Parchment;
            _settingsOverlay.gameObject.SetActive(false);

            _towerOverlay = UiFactory.Rect("TowerOverlay", root, Vector2.zero, new Vector2(1f, 0.92f));
            _towerOverlay.gameObject.AddComponent<Image>().color = UiFactory.Parchment;
            _towerOverlay.gameObject.SetActive(false);

            _daily = new DailyPanel(UiFactory.Rect("DailyOverlay", root, Vector2.zero, new Vector2(1f, 0.92f)), Refresh);
            _stars = new StarPanel(UiFactory.Rect("StarOverlay", root, Vector2.zero, new Vector2(1f, 0.92f)), Refresh);
            Refresh();
        }

        private void BuildStatusBar(RectTransform root)
        {
            // Lv + exp bar | 두 | 검룡위
            UiFactory.Panel("StatusBand", root, new Vector2(0f, .928f), Vector2.one, new Color(.025f, .055f, .06f, .96f));
            var lv = UiFactory.Panel("LvChip", root, new Vector2(0.03f, 0.932f), new Vector2(0.40f, 0.988f), UiFactory.Ink);
            UiFactory.Frame(lv.transform, UiFactory.GoldDim, 2f);
            _lvText = UiFactory.Label("Lv", lv.transform, new Vector2(0.06f, 0.35f), new Vector2(0.94f, 1f), "", 34, UiFactory.Gold, TextAnchor.MiddleLeft);
            _expBar = UiFactory.MakeBar("Exp", lv.transform, new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.30f), new Color(0f, 0f, 0f, 0.6f), UiFactory.Jade);
            UiFactory.Button("StatusTap", root, new Vector2(0.03f, 0.932f), new Vector2(0.40f, 0.988f), "", new Color(0f, 0f, 0f, 0f), () => _onStatus?.Invoke(), 20);

            var doo = UiFactory.Panel("DooChip", root, new Vector2(0.42f, 0.932f), new Vector2(0.69f, 0.988f), UiFactory.Ink);
            UiFactory.Frame(doo.transform, UiFactory.GoldDim, 2f);
            UiFactory.Label("Icon", doo.transform, new Vector2(0.04f, 0f), new Vector2(0.28f, 1f), "두", 30, UiFactory.Gold);
            _dooText = UiFactory.Label("Doo", doo.transform, new Vector2(0.28f, 0f), new Vector2(0.96f, 1f), "", 30, UiFactory.PaperText, TextAnchor.MiddleRight);

            var rank = UiFactory.Panel("RankChip", root, new Vector2(0.71f, 0.932f), new Vector2(0.865f, 0.988f), UiFactory.Ink);
            UiFactory.Frame(rank.transform, UiFactory.GoldDim, 2f);
            _rankText = UiFactory.Label("Rank", rank.transform, new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), "", 30, UiFactory.PaperText);
            var settings = UiFactory.Button("Settings", root, new Vector2(.88f, .932f), new Vector2(.97f, .988f), "설정", UiFactory.InkSoft, OpenSettingsOverlay, 16);
            settings.GetComponentInChildren<Text>().rectTransform.anchorMax = new Vector2(.96f, .28f);
            UiFactory.Glyph("settings", settings.transform, new Vector2(.15f, .28f), new Vector2(.85f, .96f), UiFactory.Gold);
        }

        /// <summary>A row-like rectangle placed in the scroll content at a pixel offset from the top.</summary>
        private static RectTransform Place(RectTransform content, float x0, float x1, float top, float height, Color color)
        {
            var rt = UiFactory.Rect("Item", content, new Vector2(x0, 1f), new Vector2(x1, 1f));
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -top);
            rt.sizeDelta = new Vector2(0f, height);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;   // lets a drag that starts here still scroll
            return rt;
        }

        /// <summary>A home-screen button (whole button is the tap target) with a title and a red count badge; returns an (unused, hidden) detail label that Refresh still fills.</summary>
        private Text Icon(RectTransform parent, float x0, float x1, float y0, float y1, string title, UnityEngine.Events.UnityAction onClick, out Text badge, int size, string iconKey)
        {
            var rt = UiFactory.Rect("Icon", parent, new Vector2(x0, y0), new Vector2(x1, y1));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UiFactory.Gradient;
            img.color = UiFactory.Ink;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            btn.onClick.AddListener(() => SoundManager.Sfx("ui_click"));
            UiFactory.Frame(rt, UiFactory.GoldDim, 2f);
            bool wide = x1 - x0 > .4f;
            var art = ArtLibrary.Icon(iconKey);
            var iconMin = wide ? new Vector2(.025f, .17f) : new Vector2(.27f, .37f);
            var iconMax = wide ? new Vector2(.25f, .87f) : new Vector2(.73f, .94f);
            if (art != null)
            {
                var pic = UiFactory.Panel("IconArt", rt, iconMin, iconMax, UiFactory.Gold);
                pic.sprite = art;
                pic.preserveAspect = true;
            }
            else UiFactory.Glyph(iconKey, rt, iconMin, iconMax, UiFactory.Gold);
            UiFactory.Heading(UiFactory.Label("Title", rt, wide ? new Vector2(.29f, .38f) : new Vector2(.04f, .03f),
                wide ? new Vector2(.94f, .90f) : new Vector2(.96f, .37f), title.Replace("\n", " "), Mathf.Max(30, size), UiFactory.PaperText,
                wide ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter));
            var detail = UiFactory.Body(UiFactory.Label("Detail", rt, new Vector2(.29f, .08f), new Vector2(.94f, .39f), "", 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal), 1.05f);
            if (wide) UiFactory.ScrollBody(detail, 30);
            detail.gameObject.SetActive(wide);
            var dot = UiFactory.Panel("Dot", rt, new Vector2(.76f, .72f), new Vector2(.97f, .97f), UiFactory.Red);
            badge = UiFactory.Label("Count", dot.transform, Vector2.zero, Vector2.one, "", 30, UiFactory.PaperText);
            badgeRoots.Add(dot.gameObject);
            return detail;
        }

        private readonly List<GameObject> badgeRoots = new List<GameObject>();

        private void Update()
        {
            if (!_built) return;
            _lobbyTimer += Time.unscaledDeltaTime;
            if (_lobbyTimer > _lobbyReadTime) { _lobbyTimer = 0f; _lobbyStep++; ShowLobby(true); }
            RefreshStatusChips();   // 두/경험치 earned inside an overlay (일일 임무, 의뢰, 별 보상...) shows at once
        }

        /// <summary>Keeps the top chips (Lv, 경험치, 두, 검룡위) in step with the save; cheap enough to call every frame.</summary>
        private void RefreshStatusChips()
        {
            if (_dooText == null || GameManager.Instance == null) return;
            var save = GameManager.Instance.Save;
            string doo = save.Doo.ToString("N0");
            if (_dooText.text != doo) _dooText.text = doo;
            string lv = "Lv." + save.Level;
            if (_lvText.text != lv) _lvText.text = lv;
            long need = Progression.ExpToNext(save.Level);
            _expBar.SetPct(need > 0 ? Mathf.Clamp01((float)save.Exp / need) : 0f);
            string rank = Promotion.RankName(Promotion.EffectiveRank(save));
            if (_rankText.text != rank) _rankText.text = rank;
        }

        /// <summary>Puts the current lobby character and line on the card.</summary>
        private void ShowLobby(bool animate)
        {
            if (_lobbyLine == null) return;
            var save = GameManager.Instance.Save;
            var scene = Lobby.Pick(save, _lobbyStep);
            _lobbyName.text = scene.Speaker;
            bool hero = scene.Speaker == StoryLine.Protagonist;
            if (hero)
            {
                double power = Realm.Power(InnerArts.HeroStatsBeforeRealm(save));
                _lobbyRealm.text = Realm.Label(Realm.StepOf(power));
                _lobbyPower.text = "종합 " + Math.Round(power).ToString("N0");
            }
            else { _lobbyRealm.text = ""; _lobbyPower.text = ""; }
            _lobbyLine.text = scene.Text;
            _lobbyReadTime = Mathf.Max(12f, scene.Text.Length * .14f + 5f);

            var bg = ArtLibrary.Background(Mathf.Max(1, Mathf.Min(save.ClearedStage, EarlyGameContent.Stages.Length))) ?? ArtLibrary.Background(1);
            _lobbyBg.sprite = bg;
            _lobbyBg.color = bg != null ? Color.white : new Color(0.10f, 0.13f, 0.20f, 1f);

            var sprite = ArtLibrary.Feathered(ArtLibrary.Portrait(scene.Speaker));   // soft edges so the picture blends into the stage background
            var rt = _lobbyPortrait.rectTransform;
            if (sprite != null)
            {
                _lobbyPortrait.sprite = sprite;
                _lobbyPortrait.color = Color.white;
                _lobbyGlyph.text = "";
                FitPortrait(sprite);
            }
            else
            {
                _lobbyPortrait.sprite = null;
                var tint = ArtLibrary.TintFor(scene.Speaker); tint.a = 0.85f;
                _lobbyPortrait.color = tint;
                _lobbyGlyph.text = scene.Speaker.Substring(0, 1);
                rt.anchorMin = new Vector2(0.05f, 0.53f); rt.anchorMax = new Vector2(0.69f, 0.83f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            }
            if (animate && isActiveAndEnabled) StartCoroutine(LobbySlideIn());
        }

        /// <summary>Draws the character as large as the left side allows, keeping the aspect ratio and standing on the floor line above the menu row.</summary>
        private void FitPortrait(Sprite sprite)
        {
            var size = _homeRoot != null ? _homeRoot.rect.size : Vector2.zero;
            if (size.x < 1f || size.y < 1f) size = new Vector2(1080f, 1750f);
            float maxW = size.x * 0.86f, maxH = size.y * 0.43f;
            float aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            float h = maxH, w = h * aspect;
            if (w > maxW) { w = maxW; h = w / aspect; }
            var rt = _lobbyPortrait.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.35f, 0.445f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
        }

        private System.Collections.IEnumerator LobbySlideIn()
        {
            var rt = _lobbyPortrait.rectTransform;
            for (float t = 0f; t < 0.22f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.22f;
                rt.anchoredPosition = new Vector2(-60f * (1f - k), 0f);
                var c = _lobbyPortrait.color; c.a = Mathf.Lerp(0.2f, c.a < 0.99f ? 0.85f : 1f, k); _lobbyPortrait.color = c;
                yield return null;
            }
            rt.anchoredPosition = Vector2.zero;
        }

        private static void SetBadge(Text badge, int count)
        {
            if (badge == null) return;
            badge.text = count.ToString();
            badge.transform.parent.gameObject.SetActive(count > 0);
        }

        private void OnFeaturedPressed()
        {
            if (_featStage != null) OnStagePressed(_featStage, _onStartRef, false);
        }

        private static RectTransform NewRow(RectTransform content, int index, float h, float gap, Color color)
        {
            var rt = UiFactory.Rect("Row" + index, content, new Vector2(0.04f, 1f), new Vector2(0.96f, 1f));
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -(gap + index * (h + gap)));
            rt.sizeDelta = new Vector2(0f, h);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;   // lets a drag that starts on a row still scroll
            UiFactory.Frame(rt, UiFactory.GoldDim, 2f);
            ReadingList.Register(rt, h, gap);
            return rt;
        }

        private void OpenPromotionOverlay()
        {
            var save = GameManager.Instance.Save;
            ClearChildren(_promoOverlay);
            _promoOverlay.gameObject.SetActive(true);

            int rank = Promotion.EffectiveRank(save);
            UiFactory.Label("Title", _promoOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "승급 시험장 · 현재 " + Promotion.RankName(rank), 44, UiFactory.Gold);
            UiFactory.ScrollBody(UiFactory.Label("Rules", _promoOverlay, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.92f),
                "상대 둘을 혼자서 연달아 꺾어야 합니다. 한 판은 " + Promotion.TurnLimit + "턴(상위 단계일수록 조금 깁니다) 안에 끝내야 하며, 시간 초과는 무승부(상금 절반)입니다. 패배하면 입장료는 돌려받지 못합니다.\n단계가 오를수록 상대는 훨씬 강해지지만, 검룡위 1당 모든 능력치 +3% (지금 +" + Math.Round(Promotion.StatBonusPerRank * 100f * rank) + "%).", 30, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 32);

            var targets = Promotion.TargetRanks(save);
            if (targets.Count == 0)
                UiFactory.Label("Max", _promoOverlay, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.7f), "이미 최고 경지의 검룡입니다.", 40, UiFactory.PaperText);

            var offers = CodexPanel.Scroller(_promoOverlay, .10f, .835f);
            int offerIndex = 0;
            foreach (int target in targets)
            {
                bool skip = target == rank + 2;
                var heading = UiFactory.Rect("Rank", offers, new Vector2(.04f, 1f), new Vector2(.96f, 1f));
                heading.pivot = new Vector2(.5f, 1f);
                heading.sizeDelta = new Vector2(0f, 90f);
                ReadingList.Register(heading, 90f, CodexPanel.RowGap);
                offerIndex++;
                UiFactory.Label("Target" + target, heading, Vector2.zero, Vector2.one,
                    Promotion.RankName(target) + (skip ? "  (월반: 상금 x" + Promotion.SkipPayFactor + ")" : ""), 34, UiFactory.Gold, TextAnchor.MiddleLeft);
                for (int tier = 0; tier < 3; tier++)
                {
                    var offer = Promotion.BuildOffer(save, target, tier);
                    bool afford = Promotion.CanAfford(save, offer);
                    var band = CodexPanel.Band(offers, offerIndex++);
                    UiFactory.Label("Name", band, new Vector2(0.03f, 0.52f), new Vector2(0.64f, 0.97f), offer.Opponents[0].Name.Split(' ')[0] + " 비무첩", 32, UiFactory.PaperText, TextAnchor.MiddleLeft);
                    UiFactory.Label("Info", band, new Vector2(0.03f, 0.27f), new Vector2(0.64f, 0.52f),
                        "입장료 " + offer.Fee.ToString("N0") + "두 · 상금 " + offer.DooReward.ToString("N0") + "두", 30, afford ? UiFactory.PaperText : UiFactory.Red, TextAnchor.MiddleLeft, FontStyle.Normal);
                    double need = Recommend.ForPromotion(offer);
                    UiFactory.Label("Need", band, new Vector2(0.03f, 0.02f), new Vector2(0.66f, 0.28f),
                        "추천 최소 " + Recommend.Text(need), 30, Recommend.Meets(save, need) ? UiFactory.Dim : UiFactory.Red, TextAnchor.MiddleLeft, FontStyle.Normal);
                    var offerCopy = offer;
                    var go = UiFactory.Button("Go", band, new Vector2(0.68f, 0.12f), new Vector2(0.97f, 0.88f), afford ? "도전" : "두 부족", afford ? UiFactory.Red : UiFactory.InkSoft, () => OnChallenge(offerCopy), 32);
                    go.interactable = afford;
                }
            }

            UiFactory.Button("Close", _promoOverlay, new Vector2(0.25f, 0.01f), new Vector2(0.75f, 0.09f), "닫기", UiFactory.InkSoft, () => _promoOverlay.gameObject.SetActive(false), 36);
        }


        // ------------------------------------------------------------------ 설정

        private void OpenSettingsOverlay()
        {
            _confirmReset = false;
            BuildSettings();
        }

        private void BuildSettings()
        {
            var save = GameManager.Instance.Save;
            if (save.Settings == null) save.Settings = new GameSettings();
            var st = save.Settings;
            ClearChildren(_settingsOverlay);
            _settingsOverlay.gameObject.SetActive(true);
            UiFactory.Label("Title", _settingsOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "설정", 44, UiFactory.Gold);

            float y = 0.88f;
            VolumeRow("배경음", st.BgmVolume, ref y, d => { st.BgmVolume = GameSettings.StepVolume(st.BgmVolume, d); SoundManager.RefreshVolumes(); });
            VolumeRow("효과음", st.SfxVolume, ref y, d => { st.SfxVolume = GameSettings.StepVolume(st.SfxVolume, d); });
            ToggleRow("진동", st.Vibration ? "켜짐" : "꺼짐", ref y, () => { st.Vibration = !st.Vibration; if (st.Vibration) SoundManager.Haptic(60); });
            ToggleRow("전투 시작 속도", "x" + st.BattleSpeed, ref y, () => { st.BattleSpeed = st.NextSpeed(); });

            var help = UiFactory.Button("Help", _settingsOverlay, new Vector2(0.08f, y - 0.09f), new Vector2(0.92f, y), "도움말", UiFactory.Red, OpenHelpList, 34);
            help.interactable = true;
            y -= 0.14f;
            var reset = UiFactory.Button("Reset", _settingsOverlay, new Vector2(0.08f, y - 0.09f), new Vector2(0.92f, y),
                _confirmReset ? "정말 초기화할까요? 한 번 더 누르면 모든 진행이 사라집니다" : "저장 데이터 초기화", _confirmReset ? UiFactory.Red : UiFactory.InkSoft, OnResetPressed, 28);
            reset.interactable = true;

            UiFactory.Label("Info", _settingsOverlay, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.22f),
                "효과음과 배경음의 크기, 전투 속도와 진동을 설정할 수 있습니다.", 30, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);
            UiFactory.Button("Close", _settingsOverlay, new Vector2(0.25f, 0.01f), new Vector2(0.75f, 0.09f), "닫기", UiFactory.InkSoft, () => { GameManager.Instance.SaveNow(); _settingsOverlay.gameObject.SetActive(false); Refresh(); }, 36);
        }

        private void OpenHelpList()
        {
            ClearChildren(_settingsOverlay);
            UiFactory.Label("Title", _settingsOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "도움말", 44, UiFactory.Gold);
            var content = CodexPanel.Scroller(_settingsOverlay, .10f, .91f);
            int index = 0;
            foreach (var topic in Tutorial.All)
            {
                var id = topic.Id;
                var row = CodexPanel.Band(content, index++);
                UiFactory.Button("Topic_" + id, row, new Vector2(.02f, .08f), new Vector2(.98f, .92f), topic.Title, UiFactory.InkSoft, () => HelpPanel.Instance.Show(id), 36);
            }
            content.sizeDelta = new Vector2(0f, index * (CodexPanel.RowH + CodexPanel.RowGap));
            UiFactory.Button("Back", _settingsOverlay, new Vector2(0.25f, 0.01f), new Vector2(0.75f, 0.09f), "돌아가기", UiFactory.InkSoft, BuildSettings, 36);
        }

        private void VolumeRow(string label, float value, ref float y, Action<int> change)
        {
            var band = UiFactory.Panel("Row", _settingsOverlay, new Vector2(0.05f, y - 0.09f), new Vector2(0.95f, y), UiFactory.Ink);
            UiFactory.Frame(band.transform, UiFactory.GoldDim, 2f);
            UiFactory.Label("Name", band.transform, new Vector2(0.04f, 0.1f), new Vector2(0.40f, 0.9f), label, 34, UiFactory.Gold, TextAnchor.MiddleLeft);
            UiFactory.Button("Minus", band.transform, new Vector2(0.44f, 0.15f), new Vector2(0.58f, 0.85f), "-", UiFactory.InkSoft, () => { change(-1); BuildSettings(); }, 40);
            UiFactory.Label("Value", band.transform, new Vector2(0.58f, 0.1f), new Vector2(0.80f, 0.9f), Mathf.RoundToInt(value * 100f) + "%", 34, UiFactory.PaperText);
            UiFactory.Button("Plus", band.transform, new Vector2(0.80f, 0.15f), new Vector2(0.94f, 0.85f), "+", UiFactory.InkSoft, () => { change(1); BuildSettings(); }, 40);
            y -= 0.105f;
        }

        private void ToggleRow(string label, string value, ref float y, Action toggle)
        {
            var band = UiFactory.Panel("Row", _settingsOverlay, new Vector2(0.05f, y - 0.09f), new Vector2(0.95f, y), UiFactory.Ink);
            UiFactory.Frame(band.transform, UiFactory.GoldDim, 2f);
            UiFactory.Label("Name", band.transform, new Vector2(0.04f, 0.1f), new Vector2(0.50f, 0.9f), label, 34, UiFactory.Gold, TextAnchor.MiddleLeft);
            UiFactory.Button("Toggle", band.transform, new Vector2(0.60f, 0.15f), new Vector2(0.94f, 0.85f), value, UiFactory.Red, () => { toggle(); BuildSettings(); }, 34);
            y -= 0.105f;
        }

        private void OnResetPressed()
        {
            if (!_confirmReset) { _confirmReset = true; BuildSettings(); return; }
            _confirmReset = false;
            GameManager.Instance.ResetSave();
            _settingsOverlay.gameObject.SetActive(false);
            Refresh();
        }

        // ------------------------------------------------------------------ 의뢰 / 기연

        private void OpenQuestOverlay()
        {
            var save = GameManager.Instance.Save;
            ClearChildren(_questOverlay);
            _questOverlay.gameObject.SetActive(true);
            if (!Tutorial.IsDone(save, "board") && HelpPanel.Instance != null) HelpPanel.Instance.Show("board", true);
            UiFactory.Label("Title", _questOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "의뢰 게시판", 44, UiFactory.Gold);
            UiFactory.ScrollBody(UiFactory.Label("Rules", _questOverlay, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.92f),
                "이미 지나온 싸움에 조건이 붙어 다시 나옵니다. 한 번씩만 받을 수 있고, 두와 보상을 얻습니다.", 30, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 32);

            var partyNow = new List<string>();
            foreach (var c in EarlyGameContent.SelectCompanionsFor(Math.Max(1, save.HighestUnlockedStage), save)) partyNow.Add(c.Name);
            UiFactory.Label("Party", _questOverlay, new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.85f),
                partyNow.Count > 0 ? "함께 싸울 동료: " + string.Join(", ", partyNow) : "함께 싸울 동료가 없습니다 (비류연 혼자 나갑니다)", 30, UiFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Normal);
            var questList = CodexPanel.Scroller(_questOverlay, 0.10f, 0.80f);
            int shown = 0;
            foreach (var q in Quests.All)
            {
                if (!Quests.IsUnlocked(save, q)) continue;
                bool done = Quests.IsDone(save, q.Id);
                var band = CodexPanel.Band(questList, shown);
                UiFactory.Label("Name", band, new Vector2(0.03f, 0.54f), new Vector2(0.66f, 0.97f), q.Name + (done ? "  (완료)" : ""), 32, done ? UiFactory.InkSoft : UiFactory.PaperText, TextAnchor.MiddleLeft);
                UiFactory.Label("Info", band, new Vector2(0.03f, 0.03f), new Vector2(0.66f, 0.54f), q.Trial.Brief + " · 보상 " + Income.Boost(q.RewardDoo).ToString("N0") + "두", 30, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                var quest = q;
                var go = UiFactory.Button("Go", band, new Vector2(0.69f, 0.12f), new Vector2(0.97f, 0.88f), done ? "완료" : "수락", done ? UiFactory.InkSoft : UiFactory.Red, () => AcceptQuest(quest), 32);
                go.interactable = !done;
                shown++;
            }
            questList.sizeDelta = new Vector2(0f, CodexPanel.RowGap + shown * (CodexPanel.RowH + CodexPanel.RowGap));
            if (shown == 0)
                UiFactory.Label("None", _questOverlay, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.7f), "아직 받을 수 있는 의뢰가 없습니다. 스테이지를 돌파하면 의뢰가 생깁니다.", 34, UiFactory.PaperText);

            UiFactory.Button("Close", _questOverlay, new Vector2(0.25f, 0.01f), new Vector2(0.75f, 0.09f), "닫기", UiFactory.InkSoft, () => { _questOverlay.gameObject.SetActive(false); Refresh(); }, 36);
        }

        // ------------------------------------------------------------------ 무한 비무대

        private void OpenStageOverlay()
        {
            Refresh();
            _stageOverlay.gameObject.SetActive(true);
            _stageOverlay.SetAsLastSibling();
        }

        private void OpenTowerOverlay()
        {
            var save = GameManager.Instance.Save;
            ClearChildren(_towerOverlay);
            _towerOverlay.gameObject.SetActive(true);
            UiFactory.Label("Title", _towerOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "무한 비무대", 44, UiFactory.Gold);
            if (!Tower.Unlocked(save))
            {
                UiFactory.Label("Locked", _towerOverlay, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.75f),
                    "스테이지 " + Tower.UnlockStage + "를 돌파하면 열립니다.", 34, UiFactory.PaperText);
            }
            else
            {
                int floor = Tower.NextFloor(save);
                bool boss = Tower.IsBossFloor(floor);
                UiFactory.Label("Best", _towerOverlay, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.92f),
                    "최고 기록 " + save.TowerBest + "층", 38, UiFactory.PaperText);
                var card = UiFactory.Panel("Card", _towerOverlay, new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.82f), UiFactory.Ink);
                UiFactory.Frame(card.transform, boss ? UiFactory.Red : UiFactory.Gold, 3f);
                UiFactory.Label("Floor", card.transform, new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.96f), floor + "층" + (boss ? "  (우두머리)" : ""), 56, boss ? UiFactory.Red : UiFactory.Gold);
                var enemies = Tower.BuildEnemies(floor);
                var names = new List<string>();
                foreach (var e in enemies) names.Add(e.Name);
                var items = new List<string>();
                foreach (var s in Tower.RewardItems(floor)) { var it = EarlyGameContent.FindItem(s.Id); if (it != null) items.Add(it.Name); }
                UiFactory.Label("Info", card.transform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.62f),
                    "상대: " + string.Join(", ", names) + "\n권장 Lv." + Tower.FloorLevel(floor)
                    + "\n첫 돌파 보상: 두 " + Income.Boost(Tower.RewardDoo(floor)).ToString("N0") + (items.Count > 0 ? ", " + string.Join(", ", items) : "")
                    + " + 경험\n" + Recommend.Line(save, Recommend.ForTowerFloor(floor)), 30, Recommend.Meets(save, Recommend.ForTowerFloor(floor)) ? UiFactory.PaperText : UiFactory.Red, TextAnchor.MiddleCenter, FontStyle.Normal);
                UiFactory.Button("Go", _towerOverlay, new Vector2(0.25f, 0.34f), new Vector2(0.75f, 0.44f), "도전", UiFactory.Red, () => StartTower(floor), 44);
                if (save.TowerBest > 0)
                {
                    UiFactory.Label("Again", _towerOverlay, new Vector2(0.05f, 0.24f), new Vector2(0.95f, 0.30f), "이미 돌파한 층은 보상 없이 다시 겨룰 수 있습니다", 30, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);
                    UiFactory.Button("Prev", _towerOverlay, new Vector2(0.25f, 0.15f), new Vector2(0.75f, 0.23f), save.TowerBest + "층 다시 도전", UiFactory.InkSoft, () => StartTower(save.TowerBest), 30);
                }
            }
            UiFactory.Button("Close", _towerOverlay, new Vector2(0.25f, 0.01f), new Vector2(0.75f, 0.09f), "닫기", UiFactory.InkSoft, () => { _towerOverlay.gameObject.SetActive(false); Refresh(); }, 36);
            if (!Tutorial.IsDone(save, "tower") && HelpPanel.Instance != null && Tower.Unlocked(save)) HelpPanel.Instance.Show("tower", true);
        }

        private bool _reopenTower;

        /// <summary>Called when a fight ends: closes pop-ups, refreshes, and brings the 무한 비무대 window back after a tower fight so the next floor is one tap away.</summary>
        public void AfterBattle()
        {
            CloseOverlays();
            Refresh();
            if (_reopenTower) { _reopenTower = false; OpenTowerOverlay(); }
            else if (_reopenStages) { _reopenStages = false; OpenStageOverlay(); }
        }

        private void StartTower(int floor)
        {
            var st = Tower.ToStage(GameManager.Instance.Save, floor);
            OpenPrep(st, () =>
            {
                _towerOverlay.gameObject.SetActive(false);
                _reopenTower = true;
                _reopenStages = false;
                _onStart(st);
            });
        }

        private void AcceptQuest(QuestData q)
        {
            if (Quests.IsDone(GameManager.Instance.Save, q.Id)) return;
            var st = Quests.ToStage(q);
            OpenPrep(st, () =>
            {
                _questOverlay.gameObject.SetActive(false);
                _reopenTower = false;
                _reopenStages = false;
                _onStart(st);
            });
        }

        private void OpenFortuneOverlay()
        {
            ClearChildren(_fortuneOverlay);
            _fortuneOverlay.gameObject.SetActive(true);
            var save = GameManager.Instance.Save;
            var open = Fortunes.Open(save);
            UiFactory.Label("Title", _fortuneOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "기연", 44, UiFactory.Gold);
            if (open.Count == 0)
            {
                UiFactory.Label("None", _fortuneOverlay, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.7f), "지금은 마주칠 인연이 없습니다. 스테이지를 돌파하면 새로운 기연이 찾아옵니다.", 34, UiFactory.PaperText);
            }
            var fortuneList = CodexPanel.Scroller(_fortuneOverlay, 0.10f, 0.90f);
            for (int i = 0; i < open.Count; i++)
            {
                var e = open[i];
                var band = CodexPanel.Band(fortuneList, i);
                UiFactory.Label("Name", band, new Vector2(0.03f, 0.10f), new Vector2(0.64f, 0.90f), e.Title, 34, UiFactory.PaperText, TextAnchor.MiddleLeft);
                UiFactory.Button("Go", band, new Vector2(0.68f, 0.12f), new Vector2(0.97f, 0.88f), "마주치기", UiFactory.Red, () => ShowFortune(e), 30);
            }
            fortuneList.sizeDelta = new Vector2(0f, CodexPanel.RowGap + open.Count * (CodexPanel.RowH + CodexPanel.RowGap));
            UiFactory.Button("Close", _fortuneOverlay, new Vector2(0.25f, 0.01f), new Vector2(0.75f, 0.09f), "닫기", UiFactory.InkSoft, () => { _fortuneOverlay.gameObject.SetActive(false); Refresh(); }, 36);
        }

        private void ShowFortune(FortuneEvent ev)
        {
            var save = GameManager.Instance.Save;
            ClearChildren(_fortuneOverlay);
            UiFactory.Label("Title", _fortuneOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), ev.Title, 44, UiFactory.Gold);
            UiFactory.ScrollBody(UiFactory.Label("Text", _fortuneOverlay, new Vector2(0.07f, 0.52f), new Vector2(0.93f, 0.90f), string.Join("\n\n", ev.Text), 34, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 32);
            float y = 0.46f;
            foreach (var ch in ev.Choices)
            {
                var choice = ch;
                bool ok = Fortunes.CanChoose(save, choice);
                var btn = UiFactory.Button("Choice", _fortuneOverlay, new Vector2(0.08f, y - 0.09f), new Vector2(0.92f, y), choice.Label + (ok ? "" : " (두 부족)"), ok ? UiFactory.Red : UiFactory.InkSoft, () => ResolveFortune(ev, choice), 30);
                btn.interactable = ok;
                y -= 0.11f;
            }
        }

        private void ResolveFortune(FortuneEvent ev, FortuneChoice choice)
        {
            var save = GameManager.Instance.Save;
            var companions = new List<string>();
            foreach (var c in AvailableCompanions(save)) companions.Add(c.Id);
            string result = Fortunes.Resolve(save, ev, choice, companions);
            GameManager.Instance.SaveNow();
            ClearChildren(_fortuneOverlay);
            UiFactory.Label("Title", _fortuneOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), ev.Title, 44, UiFactory.Gold);
            UiFactory.ScrollBody(UiFactory.Label("Result", _fortuneOverlay, new Vector2(0.07f, 0.35f), new Vector2(0.93f, 0.85f), result, 36, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 32);
            UiFactory.Button("Done", _fortuneOverlay, new Vector2(0.3f, 0.12f), new Vector2(0.7f, 0.22f), "확인", UiFactory.Red, OpenFortuneOverlay, 40);
        }

        private static void ClearChildren(RectTransform rt)
        {
            for (int i = rt.childCount - 1; i >= 0; i--) Destroy(rt.GetChild(i).gameObject);
            UiFactory.Frame(rt, UiFactory.GoldDim, 3f);   // panel border, re-added after every rebuild
        }

        /// <summary>Companions available now: everyone who has joined by the furthest stage reached.</summary>
        private static List<CompanionData> AvailableCompanions(SaveData save)
        {
            return EarlyGameContent.CompanionsFor(Math.Max(1, save.HighestUnlockedStage));
        }

        private void OpenPartyOverlay()
        {
            var save = GameManager.Instance.Save;
            ClearChildren(_partyOverlay);
            _partyOverlay.gameObject.SetActive(true);

            if (!Tutorial.IsDone(save, "formation") && HelpPanel.Instance != null) HelpPanel.Instance.Show("formation", true);
            var available = AvailableCompanions(save);
            var active = EarlyGameContent.SelectCompanionsFor(Math.Max(1, save.HighestUnlockedStage), save);
            UiFactory.Label("Title", _partyOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "파티 편성", 44, UiFactory.Gold);
            UiFactory.ScrollBody(UiFactory.Label("Rules", _partyOverlay, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.92f),
                "비류연과 함께 싸울 동료는 최대 " + EarlyGameContent.MaxCompanions + "명입니다. 눌러서 출전과 대기를 바꿉니다.", 28, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 32);

            if (available.Count == 0)
                UiFactory.Label("None", _partyOverlay, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.7f), "아직 함께할 동료가 없습니다.", 40, UiFactory.PaperText);

            var roster = CodexPanel.Scroller(_partyOverlay, .34f, .835f);
            int memberIndex = 0;
            foreach (var c in available)
            {
                bool on = active.Contains(c);
                var band = CodexPanel.Band(roster, memberIndex++);
                UiFactory.Label("Name", band, new Vector2(0.03f, 0.52f), new Vector2(0.64f, 0.97f), c.Name, 36, on ? UiFactory.Gold : UiFactory.PaperText, TextAnchor.MiddleLeft);
                UiFactory.Label("Role", band, new Vector2(0.03f, 0.03f), new Vector2(0.64f, 0.52f), c.Role + "  ·  인연 " + Bonds.LevelNames[Bonds.Level(save, c.Id)] + " (" + Bonds.BonusText(Bonds.Level(save, c.Id)) + ")", 30, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                var companion = c;
                UiFactory.Button("Toggle", band, new Vector2(0.68f, 0.12f), new Vector2(0.97f, 0.88f), on ? "출전" : "대기", on ? UiFactory.Red : UiFactory.InkSoft, () => ToggleCompanion(companion), 32);
            }

            BuildFormationBlock(_partyOverlay, active, OpenPartyOverlay, 0.13f, 0.29f);
            UiFactory.Button("Close", _partyOverlay, new Vector2(0.25f, 0.01f), new Vector2(0.75f, 0.09f), "닫기", UiFactory.InkSoft, () => { _partyOverlay.gameObject.SetActive(false); Refresh(); }, 36);
        }

        private string _placeSelected;   // the member picked in the placement grid (tap a tile next to move or swap)

        /// <summary>
        /// 진형: a 3 x 3 grid of our half of the board (the right column is the front line facing the enemy); tap a member, then a tile, to move or swap.
        /// Positions are fixed for the whole fight, so this is the place to decide who tanks in front and who attacks from behind.
        /// </summary>
        private void BuildFormationBlock(RectTransform parent, List<CompanionData> active, Action redraw, float gy0, float gy1, StageData st = null)
        {
            var save = GameManager.Instance.Save;
            var ids = new List<string> { Formation.HeroId };
            var names = new Dictionary<string, string> { { Formation.HeroId, "비류연" } };
            foreach (var c in active) { ids.Add(c.Id); names[c.Id] = c.Name; }
            var map = Formation.Resolve(save, ids, st);   // auto lines are planned against this stage (the next one to clear on the party screen)
            if (_placeSelected != null && !map.ContainsKey(_placeSelected)) _placeSelected = null;
            UiFactory.Label("FormTitle", parent, new Vector2(0.05f, gy1 + 0.035f), new Vector2(0.60f, gy1 + 0.085f), "진형 · 인물 선택 후 칸 선택", 30, UiFactory.Gold, TextAnchor.MiddleLeft);
            UiFactory.Button("FormAuto", parent, new Vector2(0.64f, gy1 + 0.035f), new Vector2(0.95f, gy1 + 0.085f), save.PlacementCustom ? "자동 배치" : "자동 배치 중", save.PlacementCustom ? UiFactory.InkSoft : UiFactory.Gold, () => { Formation.Reset(save); _placeSelected = null; GameManager.Instance.SaveNow(); redraw(); }, 24);
            UiFactory.Label("FormHint", parent, new Vector2(0.05f, gy0 - 0.045f), new Vector2(0.95f, gy0 - 0.005f), "배치는 전투 전에만 바꿀 수 있습니다. 오른쪽이 앞줄입니다.", 30, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);
            float gx0 = 0.05f, gx1 = 0.95f;
            float cw = (gx1 - gx0) / Board.PartyCols, ch = (gy1 - gy0) / Board.Rows;
            string[] lineNames = { "뒤 줄", "중 줄", "앞 줄" };
            for (int col = 0; col < Board.PartyCols; col++)
                UiFactory.Label("Line" + col, parent, new Vector2(gx0 + col * cw, gy1), new Vector2(gx0 + (col + 1) * cw, gy1 + 0.03f), lineNames[col], 30, col == Board.PartyCols - 1 ? UiFactory.Gold : UiFactory.PaperText);
            for (int col = 0; col < Board.PartyCols; col++)
                for (int row = 0; row < Board.Rows; row++)
                {
                    string here = null;
                    foreach (var kv in map) if (kv.Value[0] == col && kv.Value[1] == row) here = kv.Key;
                    float x0 = gx0 + col * cw, y1 = gy1 - row * ch;
                    bool selected = here != null && here == _placeSelected;
                    var tint = here == null ? new Color(0.28f, 0.50f, 0.85f, 0.25f) : selected ? UiFactory.Gold : UiFactory.Ink;
                    int cc = col, rr = row;
                    UiFactory.Button("Tile" + col + rr, parent, new Vector2(x0 + 0.004f, y1 - ch + 0.003f), new Vector2(x0 + cw - 0.004f, y1 - 0.003f), here == null ? "" : names[here], tint, () => OnPlacementTile(ids, cc, rr, redraw, st), 26);
                }
        }

        private void OnPlacementTile(List<string> ids, int col, int row, Action redraw, StageData st = null)
        {
            var save = GameManager.Instance.Save;
            var map = Formation.Resolve(save, ids, st);
            string here = null;
            foreach (var kv in map) if (kv.Value[0] == col && kv.Value[1] == row) here = kv.Key;
            if (_placeSelected == null)
            {
                _placeSelected = here;   // pick up a member (an empty tile selects nothing)
            }
            else if (here == _placeSelected)
            {
                _placeSelected = null;
            }
            else
            {
                string error;
                if (Formation.SetTile(save, ids, _placeSelected, col, row, out error, st)) GameManager.Instance.SaveNow();
                _placeSelected = null;
            }
            redraw();
        }

        // ------------------------------------------------------------------ 출전 준비 (formation check before every fight)

        private RectTransform _prepOverlay;

        /// <summary>The enemies of every fight in the stage, once each, with a note for the ones that shoot at the back line.</summary>
        private static List<string> EnemyBriefing(StageData st, out bool snipers)
        {
            snipers = false;
            var lines = new List<string>();
            var seen = new HashSet<string>();
            foreach (var group in st.Encounters)
                foreach (var id in group)
                {
                    if (!seen.Add(id)) continue;
                    var e = EarlyGameContent.FindEnemy(id);
                    if (e == null) continue;
                    bool snipes = false;
                    if (e.Moves != null) foreach (var m in e.Moves) if (m.Snipe) snipes = true;
                    if (snipes) snipers = true;
                    lines.Add((e.IsBoss ? "[우두머리] " : "") + e.Name + (snipes ? "  — 뒷줄을 노립니다" : "  — 앞 줄부터 공격"));
                }
            return lines;
        }

        /// <summary>Shown before a fight: look at the enemies, set who stands in front (defence) and who behind (attack), then start.</summary>
        private void OpenPrep(StageData st, Action go)
        {
            var save = GameManager.Instance.Save;
            ClearChildren(_prepOverlay);
            _prepOverlay.gameObject.SetActive(true);
            _prepOverlay.SetAsLastSibling();
            var active = EarlyGameContent.SelectCompanionsFor(Roster.PartyStage(st, save), save);
            UiFactory.Label("Title", _prepOverlay, new Vector2(0.05f, 0.93f), new Vector2(0.95f, 0.99f), "출전 준비", 44, UiFactory.Gold);
            UiFactory.Label("Stage", _prepOverlay, new Vector2(0.05f, 0.885f), new Vector2(0.95f, 0.93f), st.Name, 28, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);

            bool snipers;
            var lines = EnemyBriefing(st, out snipers);
            var body = new System.Text.StringBuilder();
            body.Append("적의 공격은 기본적으로 앞 줄부터 향합니다. 앞 줄에는 맞아도 버티는 인물을, 뒷줄에는 공격이 강한 인물을 세우세요.");
            if (snipers) body.Append("\n뒷줄을 노리는 적이 있습니다. 이번에는 뒷줄에도 버틸 수 있는 인물을 두는 편이 좋습니다.");
            UiFactory.Body(UiFactory.ScrollBody(UiFactory.Label("Advice", _prepOverlay, new Vector2(0.05f, 0.775f), new Vector2(0.95f, 0.88f), body.ToString(), 30, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal), 32));

            Recommend.StageReward(save, st, out long rewardExp, out long rewardDoo);
            double need = Recommend.ForStage(st);
            bool enough = Recommend.Meets(save, need);
            string payNote = Recommend.IsReplay(save, st) ? "  (이미 깬 곳이라 절반)" : "";
            UiFactory.Body(UiFactory.ScrollBody(UiFactory.Label("Reward", _prepOverlay, new Vector2(0.05f, 0.645f), new Vector2(0.95f, 0.775f),
                "예상 보상  경험 " + rewardExp.ToString("N0") + " · 두 " + rewardDoo.ToString("N0") + payNote + "\n" + Recommend.Line(save, need), 30, enough ? UiFactory.PaperText : UiFactory.Red, TextAnchor.UpperLeft, FontStyle.Normal), 32), 1.15f);

            var enemyText = new System.Text.StringBuilder("나올 적\n");
            foreach (var l in lines) enemyText.Append(l).Append('\n');
            UiFactory.Body(UiFactory.ScrollBody(UiFactory.Label("Enemies", _prepOverlay, new Vector2(0.05f, 0.44f), new Vector2(0.95f, 0.645f), enemyText.ToString().TrimEnd('\n'), 30, UiFactory.Gold, TextAnchor.UpperLeft, FontStyle.Normal), 32), 1.15f);

            BuildFormationBlock(_prepOverlay, new List<CompanionData>(active), () => OpenPrep(st, go), 0.15f, 0.36f, st);
            UiFactory.Button("Go", _prepOverlay, new Vector2(0.52f, 0.02f), new Vector2(0.95f, 0.095f), "출전", UiFactory.Red, () => { _prepOverlay.gameObject.SetActive(false); go(); }, 38);
            UiFactory.Button("Cancel", _prepOverlay, new Vector2(0.05f, 0.02f), new Vector2(0.48f, 0.095f), "취소", UiFactory.InkSoft, () => { _prepOverlay.gameObject.SetActive(false); _placeSelected = null; }, 34);
        }

        private void ToggleCompanion(CompanionData c)
        {
            var save = GameManager.Instance.Save;
            int stage = Math.Max(1, save.HighestUnlockedStage);
            var picked = new List<string>();
            foreach (var x in EarlyGameContent.SelectCompanionsFor(stage, save)) picked.Add(x.Id);
            var oldIds = new List<string> { Formation.HeroId };
            oldIds.AddRange(picked);
            if (picked.Contains(c.Id))
            {
                picked.Remove(c.Id);                                   // 출전 → 대기
                if (!save.BenchedCompanions.Contains(c.Id)) save.BenchedCompanions.Add(c.Id);
            }
            else if (picked.Count < EarlyGameContent.MaxCompanions)
            {
                save.BenchedCompanions.Remove(c.Id);                   // 대기 → 출전
                picked.Add(c.Id);
            }
            else return;   // already full: bench someone first
            save.ActiveCompanions = picked;
            var newIds = new List<string> { Formation.HeroId };
            newIds.AddRange(picked);
            Formation.OnPartyChanged(save, oldIds, newIds);   // the others stay put, a newcomer takes the best free tile
            GameManager.Instance.SaveNow();
            OpenPartyOverlay();
        }

        private void OnChallenge(PromotionOffer offer)
        {
            var save = GameManager.Instance.Save;
            if (!Promotion.Pay(save, offer)) return;
            GameManager.Instance.SaveNow();
            _promoOverlay.gameObject.SetActive(false);
            _onPromote(offer);
        }

        private void OnEnable() { if (_built) Refresh(); }

        private void OnDisable() { if (_built) CloseOverlays(); }

        /// <summary>Hides every pop-up panel of the 강호 tab (used when a fight starts or ends and when the tab is left).</summary>
        public void CloseOverlays()
        {
            foreach (var o in new[] { _promoOverlay, _partyOverlay, _prepOverlay, _questOverlay, _fortuneOverlay, _settingsOverlay, _towerOverlay, _stageOverlay })
                if (o != null) o.gameObject.SetActive(false);
            if (_stars != null) _stars.Hide();
            if (_daily != null) _daily.Hide();
        }

        private void OnStagePressed(StageData st, Action<StageData> onStart, bool fromList)
        {
            var save = GameManager.Instance.Save;
            if (save.HighestUnlockedStage < st.Number || !Promotion.StageAllowed(save, st.Number)) return;
            int diff = StageProgress.Has(save, st.Number) ? save.ProgressDifficulty : Mathf.Clamp(save.Difficulty, 0, StageStars.DifficultyCount - 1);
            if (!StageStars.Unlocked(save, st.Number, diff)) return;
            var run = st.WithDifficulty(diff);
            OpenPrep(run, () =>
            {
                _reopenTower = false;
                _reopenStages = fromList;
                onStart(run);
            });
        }

        private static string PartyText(StageData st)
        {
            var companions = EarlyGameContent.SelectCompanionsFor(Roster.PartyStage(st, GameManager.Instance.Save), GameManager.Instance.Save);
            if (companions.Count == 0) return "";
            var names = new List<string>();
            foreach (var c in companions) names.Add(c.Name);
            return " · 동료 " + string.Join(", ", names);
        }

        public void Refresh()
        {
            if (!_built) return;
            var save = GameManager.Instance.Save;
            ShowLobby(false);
            int rankNow = Promotion.EffectiveRank(save);
            _lvText.text = "Lv." + save.Level;
            long need = Progression.ExpToNext(save.Level);
            _expBar.SetPct(need > 0 ? Mathf.Clamp01((float)save.Exp / need) : 0f);
            _dooText.text = save.Doo.ToString("N0");
            _rankText.text = Promotion.RankName(rankNow);

            // Featured card = the furthest stage the player can attempt.
            var stagesAll = EarlyGameContent.Stages;
            _featStage = EarlyGameContent.FindStage(Math.Min(save.HighestUnlockedStage, stagesAll[stagesAll.Length - 1].Number));
            if (_featStage != null)
            {
                _featArt.sprite = ArtLibrary.Background(_featStage.Number) ?? ArtLibrary.Background(1);
                bool featCleared = save.ClearedStage >= _featStage.Number;
                bool featOk = Promotion.StageAllowed(save, _featStage.Number);
                _featTitle.text = _featStage.Number.ToString("00") + " · " + _featStage.Name;
                _featSub.text = "권장 Lv." + _featStage.RecommendedPlayerLevel + " · 전투 " + _featStage.Encounters.Length + "회" + (PartyText(_featStage).Length > 0 ? "\n" + PartyText(_featStage).TrimStart(' ', '·') : "")
                    + (featOk ? "" : "\n" + Promotion.RankName(Promotion.GateRank) + " 이상이어야 참가할 수 있습니다");
                _featBtnLabel.text = !featOk ? "승급 필요" : StageProgress.Has(save, _featStage.Number) ? "이어하기" : featCleared ? "재도전" : "출전";
                _featBtn.interactable = featOk;
            }
            _promoRowDetail.text = Promotion.RankName(Promotion.EffectiveRank(save)) + (Promotion.EffectiveRank(save) >= Promotion.MaxRank ? " · 최고 경지" : " · 승급 도전")
                + (Promotion.EffectiveRank(save) < Promotion.GateRank ? "\n(" + Promotion.RankName(Promotion.GateRank) + " 이상이어야 삼성무제 참가)" : "");
            int openQuests = Quests.Open(save).Count, openFortunes = Fortunes.Open(save).Count;
            _questRowDetail.text = openQuests > 0 ? "받을 수 있는 의뢰 " + openQuests + "건" : "새 의뢰 없음 (스테이지를 돌파하면 늘어납니다)";
            if (!ReferenceEquals(_syncedSave, save))
            {
                // Backfill records for saves from before these systems existed (once per loaded save).
                _syncedSave = save;
                Codex.Sync(save);
                StageStars.Sync(save);
                Factions.SyncFromStars(save);
            }
            var dailyList = Daily.Missions(save);
            int dailyDone = 0;
            for (int di = 0; di < dailyList.Count; di++) if (Daily.IsClaimed(save, di)) dailyDone++;
            _dailyRowDetail.text = "오늘 " + dailyDone + "/3 완료 · 연속 수련 " + save.DailyStreak + "일";
            SetBadge(_dailyBadge, Daily.ClaimableCount(save));
            int starChests = StageStars.ClaimableCount(save);
            _starRowDetail.text = "별 " + StageStars.Total(save) + " / " + StageStars.MaxStars + " · 받을 수 있는 보상 " + starChests + "개";
            SetBadge(_starBadge, starChests);
            _starsTitle.text = "스테이지   ★ " + StageStars.Total(save);
            if (_stageRowDetail != null) _stageRowDetail.text = _featStage != null ? "스테이지 " + _featStage.Number : "";
            int pickedDiff = Mathf.Clamp(save.Difficulty, 0, StageStars.DifficultyCount - 1);
            for (int d = 0; d < _diffTabs.Length; d++) _diffTabs[d].color = d == pickedDiff ? (d == 0 ? new Color(0.30f, 0.48f, 0.44f, 1f) : d == 1 ? new Color(0.70f, 0.45f, 0.12f, 1f) : UiFactory.Red) : UiFactory.InkSoft;
            _towerRowDetail.text = Tower.Unlocked(save) ? "최고 " + save.TowerBest + "층 · 다음 " + Tower.NextFloor(save) + "층" : "스테이지 " + Tower.UnlockStage + " 돌파 시 열립니다";
            _fortuneRowDetail.text = openFortunes > 0 ? "마주칠 기연 " + openFortunes + "건" : "새 기연 없음";
            SetBadge(_questBadge, openQuests);
            SetBadge(_fortuneBadge, openFortunes);
            SetBadge(_promoBadge, 0);
            SetBadge(_partyBadge, 0);
            var current = new List<string>();
            foreach (var c in EarlyGameContent.SelectCompanionsFor(Math.Max(1, save.HighestUnlockedStage), save)) current.Add(c.Name + "(인연 " + Bonds.LevelNames[Bonds.Level(save, c.Id)] + ")");
            _partyRowDetail.text = current.Count == 0 ? "아직 함께할 동료가 없습니다" : "출전 동료: " + string.Join(", ", current)
                + (AvailableCompanions(save).Count > EarlyGameContent.MaxCompanions ? "\n(동료 " + AvailableCompanions(save).Count + "명 중 " + EarlyGameContent.MaxCompanions + "명만 출전)" : "");

            foreach (var r in _rows)
            {
                var st = r.Stage;
                bool reached = save.HighestUnlockedStage >= st.Number;
                bool rankOk = Promotion.StageAllowed(save, st.Number);
                bool unlocked = reached && rankOk;
                bool cleared = save.ClearedStage >= st.Number;
                int diff = StageProgress.Has(save, st.Number) ? save.ProgressDifficulty : Mathf.Clamp(save.Difficulty, 0, StageStars.DifficultyCount - 1);
                bool diffOpen = StageStars.Unlocked(save, st.Number, diff);
                bool clearedHere = StageStars.StageCleared(save, st.Number, diff) || (diff == 0 && cleared);
                int stageStarCount = StageStars.StarsOfStage(save, st.Number, diff);
                string starText = stageStarCount > 0 ? "  ★" + stageStarCount + "/" + StageStars.MaxStarsOfStage(st.Number) : "";
                r.Title.text = "스테이지 " + st.Number + " · " + st.Name + starText;
                r.Detail.text = reached && !rankOk ? Promotion.RankName(Promotion.GateRank) + " 이상이 되어야 참가할 수 있습니다 (승급 시험장)" : unlocked ? "권장 Lv." + st.RecommendedLevel + " · 전투 " + st.Encounters.Length + "회" + PartyText(st) + (cleared ? " · 돌파" : "") : "이전 스테이지를 돌파하면 열립니다";
                bool playable = unlocked && diffOpen;
                r.ButtonLabel.text = !unlocked ? (reached ? "승급 필요" : "잠김") : !diffOpen ? StageStars.LockLabel(save, st.Number, diff) : StageProgress.Has(save, st.Number) ? "이어하기" : clearedHere ? "재도전" : "출전";
                r.ButtonImage.color = !playable ? UiFactory.InkSoft : clearedHere ? new Color(0.35f, 0.35f, 0.32f, 1f) : UiFactory.Red;
                r.Button.interactable = playable;
            }
        }
    }
}
