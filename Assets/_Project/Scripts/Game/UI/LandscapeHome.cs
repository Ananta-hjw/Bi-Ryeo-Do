using System;
using System.Collections;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Landscape lobby. All actions use existing routes and all displayed values come from the save.</summary>
    public sealed class LandscapeHome : MonoBehaviour
    {
        private WorldScreen _world;
        private Action<WorldScreen.Menu> _openMenu;
        private Action<TabRouter.Tab> _select;
        private Text _identity, _doo, _name, _realm, _quote, _featuredTitle, _featuredDetail, _featuredAction;
        private Text _quest, _daily, _fortune, _stars, _arts, _missionsBadge, _artsBadge, _recordBadge;
        private RectTransform _flyoutLayer, _missionsFlyout, _challengeFlyout;
        private Image _missionsButton, _challengeButton;
        private Image _portrait, _scenery, _journeyArt;
        private Text _portraitGlyph;
        private UiFactory.Bar _experience;
        private bool _built;
        private float _refreshTimer, _lobbyTimer;
        private int _lobbyStep;
        // The speaker and line change with a short cross-fade: portrait, name card and speech fade out (the portrait drifting left),
        // then the next one fades in drifting from the right.
        private CanvasGroup _portraitFade, _identityFade, _quoteFade;
        private string _shownLine;   // "speaker|line" now on screen; null shows the next one at once
        private Coroutine _swap;
        private const float FadeOut = .22f, FadeIn = .38f, Drift = 28f;
        private SaveData _shownSave;

        public void Build(RectTransform root, WorldScreen world, Action<WorldScreen.Menu> openMenu, Action<TabRouter.Tab> select)
        {
            _world = world; _openMenu = openMenu; _select = select;
            _scenery = UiFactory.Panel("Scenery", root, Vector2.zero, Vector2.one, Color.white);
            BackgroundCover.Apply(_scenery);
            UiFactory.Panel("Wash", root, Vector2.zero, Vector2.one, new Color(.02f, .07f, .075f, .40f));
            UiFactory.Glyph("halo", root, new Vector2(-.08f, .10f), new Vector2(.45f, .95f), new Color(.75f, .66f, .43f, .14f));
            // Same height as the other screens' back bar (MainScreen.BackBarHeight)
            var header = UiFactory.Panel("Header", root, new Vector2(0f, 1f), Vector2.one, UiFactory.Ink);
            header.rectTransform.offsetMin = new Vector2(0f, -104f);
            _identity = HomeText("Identity", header.transform, .03f, .01f, .30f, .98f, "", 30, UiFactory.Gold);
            _experience = UiFactory.MakeBar("Experience", header.transform, new Vector2(.03f, .02f), new Vector2(.30f, .09f), Color.black, UiFactory.Jade);
            UiFactory.Button("Status", header.transform, new Vector2(.02f, .04f), new Vector2(.32f, .96f), "", Color.clear, () => _select(TabRouter.Tab.Companions), 28);
            _doo = HomeText("Doo", header.transform, .44f, .01f, .90f, .98f, "", 42, UiFactory.Gold, TextAnchor.MiddleRight);
            var settings = UiFactory.Button("Settings", header.transform, new Vector2(1f, .5f), new Vector2(1f, .5f), "", Color.clear, () => _openMenu(WorldScreen.Menu.Settings), 28);
            var settingsRect = (RectTransform)settings.transform;
            settingsRect.pivot = new Vector2(1f, .5f);
            settingsRect.anchoredPosition = new Vector2(-16f, 0f);
            settingsRect.sizeDelta = new Vector2(96f, 96f);
            UiFactory.Glyph("settings", settings.transform, new Vector2(.1f, .1f), new Vector2(.9f, .9f), UiFactory.Gold).raycastTarget = false;   // 설정: a gold cog

            HomeText("Masthead", root, .035f, .805f, .40f, .885f, "비 뢰 도", 58, UiFactory.PaperText);
            _portrait = UiFactory.Panel("Character", root, new Vector2(.115f, .16f), new Vector2(.43f, .79f), Color.white);
            _portrait.preserveAspect = true;
            _portraitFade = _portrait.gameObject.AddComponent<CanvasGroup>();
            _portraitGlyph = UiFactory.Label("Fallback", _portrait.transform, Vector2.zero, Vector2.one, "", 150, UiFactory.Gold);
            UiFactory.Button("CharacterTalk", root, new Vector2(.12f, .27f), new Vector2(.42f, .79f), "", Color.clear, () => { _lobbyStep++; _lobbyTimer = 0f; RefreshLobby(); }, 24);
            var identity = UiFactory.Panel("CharacterIdentity", root, new Vector2(.125f, .13f), new Vector2(.415f, .27f), new Color(.035f, .08f, .085f, .92f));
            _identityFade = identity.gameObject.AddComponent<CanvasGroup>();
            UiFactory.Panel("Seal", identity.transform, Vector2.zero, new Vector2(.012f, 1f), UiFactory.Red);
            _name = HomeText("Name", identity.transform, .04f, .44f, .97f, .98f, "", 44, UiFactory.PaperText);
            _realm = HomeText("Realm", identity.transform, .04f, .04f, .97f, .44f, "", 28, UiFactory.Gold);
            var quote = UiFactory.Panel("Dialogue", root, new Vector2(.125f, .018f), new Vector2(.415f, .117f), UiFactory.Ink);
            UiFactory.Frame(quote.transform, UiFactory.GoldDim, 1f);
            _quoteFade = quote.gameObject.AddComponent<CanvasGroup>();
            _quote = UiFactory.ScrollBody(UiFactory.Label("Quote", quote.transform, new Vector2(.025f, .04f), new Vector2(.98f, .96f), "", 28, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal), 28);

            HomeText("MenuTitle", root, .435f, .80f, .79f, .885f, "강호를 걷는 길", 40, UiFactory.Gold);
            var featured = UiFactory.Button("Journey", root, new Vector2(.435f, .635f), new Vector2(.985f, .775f), "", UiFactory.InkSoft, () =>
            {
                _openMenu(WorldScreen.Menu.Featured);
            }, 32);
            _journeyArt = UiFactory.Panel("Art", featured.transform, Vector2.zero, Vector2.one, new Color(.55f, .65f, .60f, .16f));
            BackgroundCover.Apply(_journeyArt);
            _journeyArt.raycastTarget = false;
            UiFactory.Frame(featured.transform, UiFactory.Gold, 2f);
            HomeText("Kicker", featured.transform, .035f, .75f, .96f, .98f, "다음 여정", 26, UiFactory.Gold);
            _featuredTitle = HomeText("Stage", featured.transform, .035f, .39f, .96f, .75f, "", 36, UiFactory.PaperText);
            _featuredDetail = HomeText("Detail", featured.transform, .035f, .04f, .70f, .39f, "", 26, UiFactory.Dim);
            _featuredAction = HomeText("Action", featured.transform, .73f, .04f, .965f, .39f, "", 28, UiFactory.Gold, TextAnchor.MiddleRight);

            // Six screens in two rows of three; this is the only way into them now that there is no bottom tab bar.
            Text unused;
            HomeCard(root, .435f, .612f, .325f, .615f, "협객", "상태 · 인연 · 수련", "companion", () => _select(TabRouter.Tab.Companions), out unused);
            HomeCard(root, .622f, .798f, .325f, .615f, "장비", "착용 · 강화 · 세트", "equipment", () => _select(TabRouter.Tab.Equipment), out unused);
            _arts = HomeCard(root, .808f, .985f, .325f, .615f, "무공", "무공 · 심법", "arts", () => _select(TabRouter.Tab.MartialArts), out _artsBadge);
            HomeCard(root, .435f, .612f, .015f, .305f, "뽑기", "장비 · 영약 뽑기", "gacha", () => _select(TabRouter.Tab.Gacha), out unused);
            HomeCard(root, .622f, .798f, .015f, .305f, "행낭", "돌파 · 분해 · 합성", "bag", () => _select(TabRouter.Tab.Bag), out unused);
            HomeCard(root, .808f, .985f, .015f, .305f, "기록", "도감 · 추억 · 세력", "record", () => _select(TabRouter.Tab.Record), out _recordBadge);

            BuildSideMenus(root);
            _built = true;
            Refresh();
        }

        private static Text HomeText(string name, Transform parent, float x0, float y0, float x1, float y1, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            return UiFactory.Body(UiFactory.Label(name, parent, new Vector2(x0, y0), new Vector2(x1, y1), text, size, color, align, FontStyle.Normal), 1.15f);
        }

        private static Text HomeCard(RectTransform root, float x0, float x1, float y0, float y1, string title, string detail, string icon, Action action, out Text badge)
        {
            var button = UiFactory.Button("Home_" + title, root, new Vector2(x0, y0), new Vector2(x1, y1), "", UiFactory.Ink, () => action(), 30);
            UiFactory.Frame(button.transform, UiFactory.GoldDim, 2f);
            UiFactory.Glyph(icon, button.transform, new Vector2(.05f, .48f), new Vector2(.36f, .93f), UiFactory.Gold);
            HomeText("Title", button.transform, .40f, .48f, .96f, .94f, title, 42, UiFactory.Gold);
            badge = Badge(button.transform);
            return HomeText("Detail", button.transform, .06f, .08f, .95f, .41f, detail, 26, UiFactory.PaperText);
        }

        private void BuildSideMenus(RectTransform root)
        {
            // Keep this layer below the side icons so another group can be chosen while it is open.
            _flyoutLayer = UiFactory.Rect("FlyoutLayer", root, Vector2.zero, Vector2.one);
            UiFactory.Button("Dismiss", _flyoutLayer, Vector2.zero, Vector2.one, "", new Color(0f, 0f, 0f, .35f), CloseFlyouts, 28);
            _missionsFlyout = UiFactory.Panel("Missions", _flyoutLayer, new Vector2(.125f, .20f), new Vector2(.415f, .82f), UiFactory.Ink).rectTransform;
            UiFactory.Frame(_missionsFlyout, UiFactory.GoldDim, 2f);
            HomeText("Title", _missionsFlyout, .06f, .83f, .94f, .98f, "임무/보상", 32, UiFactory.Gold);
            _quest = FlyoutItem(_missionsFlyout, "Quest", "의뢰", "quest", .63f, .80f, WorldScreen.Menu.Quests);
            _daily = FlyoutItem(_missionsFlyout, "Daily", "일일 임무", "daily", .435f, .605f, WorldScreen.Menu.Daily);
            _stars = FlyoutItem(_missionsFlyout, "Stars", "별 보상", "star", .24f, .41f, WorldScreen.Menu.Stars);
            _fortune = FlyoutItem(_missionsFlyout, "Fortunes", "기연", "fortune", .045f, .215f, WorldScreen.Menu.Fortunes);

            _challengeFlyout = UiFactory.Panel("Challenges", _flyoutLayer, new Vector2(.125f, .28f), new Vector2(.415f, .64f), UiFactory.Ink).rectTransform;
            UiFactory.Frame(_challengeFlyout, UiFactory.GoldDim, 2f);
            HomeText("Title", _challengeFlyout, .06f, .80f, .94f, .98f, "도전", 32, UiFactory.Gold);
            FlyoutItem(_challengeFlyout, "Tower", "무한 비무대", "tower", .43f, .75f, WorldScreen.Menu.Tower);
            FlyoutItem(_challengeFlyout, "Promotion", "승급 시험", "promotion", .055f, .375f, WorldScreen.Menu.Promotion);

            var missions = SideButton(root, "MissionsMenu", "임무/보상", "missions", .64f, () => ToggleFlyout(true));
            _missionsButton = missions.GetComponent<Image>();
            _missionsBadge = Badge(missions.transform);
            LandscapeMenu.Place((RectTransform)_missionsBadge.transform.parent, .64f, .71f, .99f, .99f);
            _challengeButton = SideButton(root, "ChallengeMenu", "도전", "tower", .45f, () => ToggleFlyout(false)).GetComponent<Image>();
            SideButton(root, "PartyMenu", "파티 편성", "party", .26f, () => OpenSideMenu(WorldScreen.Menu.Party));
            SideButton(root, "ShopMenu", "객잔", "coin", .07f, () => OpenSideMenu(WorldScreen.Menu.Shop));
            CloseFlyouts();
        }

        private static Button SideButton(RectTransform root, string name, string title, string icon, float bottom, UnityEngine.Events.UnityAction action)
        {
            var button = UiFactory.Button(name, root, new Vector2(.015f, bottom), new Vector2(.11f, bottom + .15f), "", Color.clear, action, 28);
            UiFactory.Glyph(icon, button.transform, new Vector2(.23f, .38f), new Vector2(.77f, .96f), UiFactory.Gold);
            UiFactory.Label("Title", button.transform, new Vector2(.015f, .015f), new Vector2(.985f, .35f), title, 28, UiFactory.PaperText);
            return button;
        }

        private Text FlyoutItem(RectTransform parent, string name, string title, string icon, float bottom, float top, WorldScreen.Menu route)
        {
            var button = UiFactory.Button(name, parent, new Vector2(.04f, bottom), new Vector2(.96f, top), "", UiFactory.InkSoft, () => OpenSideMenu(route), 28);
            UiFactory.Glyph(icon, button.transform, new Vector2(.04f, .22f), new Vector2(.17f, .78f), UiFactory.Gold);
            HomeText("Title", button.transform, .21f, .05f, .82f, .95f, title, 28, UiFactory.PaperText);
            return Badge(button.transform);
        }

        private static Text Badge(Transform parent)
        {
            var badge = UiFactory.Panel("Badge", parent, new Vector2(.82f, .64f), new Vector2(.99f, .98f), UiFactory.Red);
            var count = UiFactory.Label("Count", badge.transform, Vector2.zero, Vector2.one, "", 22, UiFactory.PaperText);
            badge.gameObject.SetActive(false);
            return count;
        }

        private void ToggleFlyout(bool missions)
        {
            var selected = missions ? _missionsFlyout : _challengeFlyout;
            bool wasOpen = _flyoutLayer.gameObject.activeSelf && selected.gameObject.activeSelf;
            CloseFlyouts();
            if (wasOpen) return;
            _flyoutLayer.gameObject.SetActive(true);
            selected.gameObject.SetActive(true);
            (missions ? _missionsButton : _challengeButton).color = UiFactory.InkSoft;
        }

        private void OpenSideMenu(WorldScreen.Menu route)
        {
            CloseFlyouts();
            _openMenu(route);
        }

        private void CloseFlyouts()
        {
            _flyoutLayer.gameObject.SetActive(false);
            _missionsFlyout.gameObject.SetActive(false);
            _challengeFlyout.gameObject.SetActive(false);
            _missionsButton.color = Color.clear;
            _challengeButton.color = Color.clear;
        }

        private void OnDisable() { if (_built) CloseFlyouts(); }

        private static void SetQuickCount(Text label, int count)
        {
            label.text = count > 99 ? "99+" : count.ToString();
            label.transform.parent.gameObject.SetActive(count > 0);
        }

        private void OnEnable()
        {
            if (!_built) return;
            // A fade cut short by leaving the tab must not leave the character half-faded.
            _swap = null;
            SetLobbyFade(1f, 0f);
            _shownLine = null;
            _world.Refresh();
            Refresh();
        }

        private void Update()
        {
            if (!_built || GameManager.Instance == null) return;
            _refreshTimer -= Time.unscaledDeltaTime;
            _lobbyTimer += Time.unscaledDeltaTime;
            if (_lobbyTimer >= 12f) { _lobbyTimer = 0f; _lobbyStep++; RefreshLobby(); }
            if (_refreshTimer <= 0f) { _refreshTimer = .5f; Refresh(); }
        }

        public void Refresh()
        {
            if (!_built || GameManager.Instance == null) return;
            var save = GameManager.Instance.Save;
            if (!ReferenceEquals(save, _shownSave)) { _shownSave = save; _world.Refresh(); _lobbyStep = 0; }
            _identity.text = "비류연  Lv." + save.Level + " · " + Promotion.RankName(Promotion.EffectiveRank(save));
            _doo.text = save.Doo.ToString("N0") + "은자";
            _experience.SetPct((float)save.Exp / Math.Max(1L, Progression.ExpToNext(save.Level)));
            var stage = _world.FeaturedStage;
            if (stage != null)
            {
                _featuredTitle.text = stage.Number.ToString("00") + " · " + stage.Name;
                _featuredDetail.text = "권장 Lv." + stage.RecommendedPlayerLevel + " · 전투 " + stage.Encounters.Length + "회";
                _featuredAction.text = (!Promotion.StageAllowed(save, stage.Number) ? "승급 필요" : StageProgress.Has(save, stage.Number) ? "이어하기" : save.ClearedStage >= stage.Number ? "재도전" : "출전") + "  ›";
                _journeyArt.sprite = ArtLibrary.Background(stage.Number) ?? ArtLibrary.Background(1);
            }
            int arts = Discovery.Count(save);
            _arts.text = arts > 0 ? "새 무공·심법 " + arts + "개" : "무공 · 심법";
            SetQuickCount(_artsBadge, arts);
            SetQuickCount(_recordBadge, RecordUpdates.Count(save));
            SetQuickCount(_quest, Quests.Open(save).Count);
            SetQuickCount(_daily, Daily.ClaimableCount(save));
            SetQuickCount(_fortune, Fortunes.Open(save).Count);
            SetQuickCount(_stars, StageStars.ClaimableCount(save));
            SetQuickCount(_missionsBadge, Quests.Open(save).Count + Daily.ClaimableCount(save) + Fortunes.Open(save).Count + StageStars.ClaimableCount(save));
            RefreshLobby();
        }

        private void RefreshLobby()
        {
            if (!_built || GameManager.Instance == null) return;
            var save = GameManager.Instance.Save;
            var scene = Lobby.Pick(save, _lobbyStep);
            _scenery.sprite = SceneryArt.Menu("world");
            string key = scene.Speaker + "|" + scene.Text;
            if (key == _shownLine)
            {
                if (_swap == null) ShowLobbyLine(scene, save);   // same line: only the realm can have moved on
                return;
            }
            bool first = _shownLine == null;
            _shownLine = key;
            if (first || !isActiveAndEnabled) { ShowLobbyLine(scene, save); return; }
            if (_swap != null) StopCoroutine(_swap);
            _swap = StartCoroutine(SwapLobbyLine(scene, save));
        }

        private void ShowLobbyLine(LobbyLine scene, SaveData save)
        {
            _name.text = scene.Speaker;
            _quote.text = scene.Text;
            _portrait.sprite = ArtLibrary.Feathered(ArtLibrary.Portrait(scene.Speaker));
            _portrait.color = _portrait.sprite != null ? Color.white : Color.clear;
            _portraitGlyph.text = _portrait.sprite == null ? scene.Speaker.Substring(0, 1) : "";
            _realm.text = scene.Speaker == StoryLine.Protagonist ? Realm.Label(Realm.StepOf(Realm.Power(InnerArts.HeroStatsBeforeRealm(save)))) : "";
        }

        private IEnumerator SwapLobbyLine(LobbyLine scene, SaveData save)
        {
            float from = _portraitFade.alpha;
            for (float t = 0f; t < FadeOut; t += Time.unscaledDeltaTime)
            {
                float k = t / FadeOut;
                SetLobbyFade(Mathf.Lerp(from, 0f, k), -Drift * k * k);
                yield return null;
            }
            ShowLobbyLine(scene, save);
            for (float t = 0f; t < FadeIn; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / FadeIn);
                SetLobbyFade(k, Drift * (1f - k));
                yield return null;
            }
            SetLobbyFade(1f, 0f);
            _swap = null;
        }

        /// <summary>Alpha of the portrait, name card and speech, and the portrait's sideways drift in pixels.</summary>
        private void SetLobbyFade(float alpha, float drift)
        {
            _portraitFade.alpha = _identityFade.alpha = _quoteFade.alpha = alpha;
            _portrait.rectTransform.anchoredPosition = new Vector2(drift, 0f);
        }
    }
}
