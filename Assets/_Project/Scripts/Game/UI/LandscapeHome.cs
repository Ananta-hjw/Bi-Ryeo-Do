using System;
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
        private Text _identity, _doo, _material, _name, _realm, _quote, _featuredTitle, _featuredDetail, _featuredAction;
        private Text _quest, _daily, _fortune, _stars, _arts;
        private Image _portrait, _scenery, _journeyArt;
        private Text _portraitGlyph;
        private UiFactory.Bar _experience;
        private bool _built;
        private float _refreshTimer, _lobbyTimer;
        private int _lobbyStep;
        private SaveData _shownSave;

        public void Build(RectTransform root, WorldScreen world, Action<WorldScreen.Menu> openMenu, Action<TabRouter.Tab> select)
        {
            _world = world; _openMenu = openMenu; _select = select;
            _scenery = UiFactory.Panel("Scenery", root, Vector2.zero, Vector2.one, Color.white);
            UiFactory.Panel("Wash", root, Vector2.zero, Vector2.one, new Color(.02f, .07f, .075f, .76f));
            UiFactory.Glyph("halo", root, new Vector2(-.08f, .10f), new Vector2(.45f, .95f), new Color(.75f, .66f, .43f, .14f));
            var header = UiFactory.Panel("Header", root, new Vector2(0f, .89f), Vector2.one, UiFactory.Ink);
            _identity = HomeText("Identity", header.transform, .03f, .01f, .30f, .98f, "", 30, UiFactory.Gold);
            _experience = UiFactory.MakeBar("Experience", header.transform, new Vector2(.03f, .02f), new Vector2(.30f, .09f), Color.black, UiFactory.Jade);
            UiFactory.Button("Status", header.transform, new Vector2(.02f, .04f), new Vector2(.32f, .96f), "", Color.clear, () => _select(TabRouter.Tab.Companions), 28);
            _material = HomeText("Material", header.transform, .40f, .01f, .62f, .98f, "", 28, UiFactory.PaperText);
            _doo = HomeText("Doo", header.transform, .64f, .01f, .84f, .98f, "", 30, UiFactory.Gold, TextAnchor.MiddleRight);
            UiFactory.Button("Settings", header.transform, new Vector2(.87f, .04f), new Vector2(.98f, .96f), "설정", UiFactory.InkSoft, () => _openMenu(WorldScreen.Menu.Settings), 28);

            HomeText("Masthead", root, .035f, .805f, .40f, .885f, "비 뢰 도", 58, UiFactory.PaperText);
            _portrait = UiFactory.Panel("Character", root, new Vector2(.015f, .16f), new Vector2(.43f, .79f), Color.white);
            _portrait.preserveAspect = true;
            _portraitGlyph = UiFactory.Label("Fallback", _portrait.transform, Vector2.zero, Vector2.one, "", 150, UiFactory.Gold);
            UiFactory.Button("CharacterTalk", root, new Vector2(.02f, .27f), new Vector2(.42f, .79f), "", Color.clear, () => { _lobbyStep++; _lobbyTimer = 0f; RefreshLobby(); }, 24);
            var identity = UiFactory.Panel("CharacterIdentity", root, new Vector2(.035f, .13f), new Vector2(.415f, .27f), new Color(.035f, .08f, .085f, .92f));
            UiFactory.Panel("Seal", identity.transform, Vector2.zero, new Vector2(.012f, 1f), UiFactory.Red);
            _name = HomeText("Name", identity.transform, .04f, .44f, .97f, .98f, "", 44, UiFactory.PaperText);
            _realm = HomeText("Realm", identity.transform, .04f, .04f, .97f, .44f, "", 28, UiFactory.Gold);
            var quote = UiFactory.Panel("Dialogue", root, new Vector2(.035f, .018f), new Vector2(.415f, .117f), UiFactory.Ink);
            UiFactory.Frame(quote.transform, UiFactory.GoldDim, 1f);
            _quote = UiFactory.ScrollBody(UiFactory.Label("Quote", quote.transform, new Vector2(.025f, .04f), new Vector2(.98f, .96f), "", 28, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal), 28);

            HomeText("MenuTitle", root, .45f, .80f, .79f, .885f, "강호를 걷는 길", 40, UiFactory.Gold);
            var featured = UiFactory.Panel("Journey", root, new Vector2(.45f, .61f), new Vector2(.975f, .775f), UiFactory.InkSoft);
            _journeyArt = UiFactory.Panel("Art", featured.transform, Vector2.zero, Vector2.one, new Color(.55f, .65f, .60f, .16f));
            UiFactory.Frame(featured.transform, UiFactory.Gold, 2f);
            HomeText("Kicker", featured.transform, .035f, .76f, .72f, .98f, "다음 여정", 26, UiFactory.Gold);
            _featuredTitle = HomeText("Stage", featured.transform, .035f, .43f, .74f, .77f, "", 36, UiFactory.PaperText);
            _featuredDetail = HomeText("Detail", featured.transform, .035f, .04f, .74f, .43f, "", 26, UiFactory.Dim);
            var go = UiFactory.Button("Go", featured.transform, new Vector2(.77f, .20f), new Vector2(.97f, .78f), "출전", UiFactory.Red, () =>
            {
                var stage = _world.FeaturedStage;
                _openMenu(stage != null && !Promotion.StageAllowed(GameManager.Instance.Save, stage.Number) ? WorldScreen.Menu.Promotion : WorldScreen.Menu.Featured);
            }, 32);
            _featuredAction = go.GetComponentInChildren<Text>();

            HomeCard(root, .45f, .70f, .46f, .59f, "협객", "상태 · 장비 · 무공 · 인연", "party", () => _select(TabRouter.Tab.Companions));
            _arts = HomeCard(root, .725f, .975f, .46f, .59f, "무공", "무공 · 심법 관리", "arts", () => _select(TabRouter.Tab.MartialArts));
            HomeCard(root, .45f, .70f, .31f, .44f, "행낭", "돌파 · 분해 · 합성", "bag", () => _select(TabRouter.Tab.Bag));
            HomeCard(root, .725f, .975f, .31f, .44f, "기록", "도감 · 추억 · 세력", "record", () => _select(TabRouter.Tab.Record));

            _quest = Quick(root, 0, 0, "의뢰", "임무와 보수", "quest", () => _openMenu(WorldScreen.Menu.Quests));
            Quick(root, 1, 0, "무한 비무대", "층별 연속 도전", "tower", () => _openMenu(WorldScreen.Menu.Tower));
            Quick(root, 2, 0, "파티 편성", "출전 동료 배치", "party", () => _openMenu(WorldScreen.Menu.Party));
            Quick(root, 3, 0, "뽑기", "장비 모집", "gacha", () => _select(TabRouter.Tab.Gacha));
            _daily = Quick(root, 0, 1, "일일 임무", "오늘의 보상", "daily", () => _openMenu(WorldScreen.Menu.Daily));
            _stars = Quick(root, 1, 1, "별 보상", "별 달성 보상", "star", () => _openMenu(WorldScreen.Menu.Stars));
            _fortune = Quick(root, 2, 1, "기연", "강호의 인연", "fortune", () => _openMenu(WorldScreen.Menu.Fortunes));
            Quick(root, 3, 1, "승급 시험", "검룡위 승급", "promotion", () => _openMenu(WorldScreen.Menu.Promotion));
            // The full stage list remains available independently of the featured stage.
            UiFactory.Button("StageList", root, new Vector2(.81f, .787f), new Vector2(.975f, .885f), "스테이지 목록", UiFactory.InkSoft, () => _openMenu(WorldScreen.Menu.Stages), 26);
            _built = true;
            Refresh();
        }

        private static Text HomeText(string name, Transform parent, float x0, float y0, float x1, float y1, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            return UiFactory.Body(UiFactory.Label(name, parent, new Vector2(x0, y0), new Vector2(x1, y1), text, size, color, align, FontStyle.Normal), 1.15f);
        }

        private static Text HomeCard(RectTransform root, float x0, float x1, float y0, float y1, string title, string detail, string icon, Action action)
        {
            var button = UiFactory.Button("Home_" + title, root, new Vector2(x0, y0), new Vector2(x1, y1), "", UiFactory.Ink, () => action(), 30);
            UiFactory.Frame(button.transform, UiFactory.GoldDim, 2f);
            UiFactory.Glyph(icon, button.transform, new Vector2(.035f, .48f), new Vector2(.17f, .90f), UiFactory.Gold);
            HomeText("Title", button.transform, .20f, .48f, .95f, .94f, title, 34, UiFactory.Gold);
            return HomeText("Detail", button.transform, .045f, .07f, .95f, .43f, detail, 24, UiFactory.PaperText);
        }

        private static Text Quick(RectTransform root, int column, int row, string title, string description, string icon, Action action)
        {
            float x = .45f + column * .134f, y = row == 0 ? .16f : .017f;
            var button = UiFactory.Button("Quick_" + title, root, new Vector2(x, y), new Vector2(x + .123f, y + .13f), "", UiFactory.Ink, () => action(), 30);
            UiFactory.Frame(button.transform, UiFactory.GoldDim, 2f);
            UiFactory.Panel("Accent", button.transform, Vector2.zero, new Vector2(.018f, 1f), UiFactory.GoldDim);
            UiFactory.Glyph(icon, button.transform, new Vector2(.06f, .73f), new Vector2(.26f, .97f), UiFactory.Gold);
            UiFactory.Label("Title", button.transform, new Vector2(.045f, .36f), new Vector2(.955f, .73f), title, 32, UiFactory.PaperText);
            HomeText("Description", button.transform, .045f, .055f, .955f, .34f, description, 22, UiFactory.Gold, TextAnchor.MiddleCenter);
            var badge = UiFactory.Panel("Badge", button.transform, new Vector2(.63f, .75f), new Vector2(.95f, .98f), UiFactory.Red);
            var count = UiFactory.Label("Count", badge.transform, Vector2.zero, Vector2.one, "", 24, UiFactory.PaperText);
            badge.gameObject.SetActive(false);
            return count;
        }

        private static void SetQuickCount(Text label, int count)
        {
            label.text = count > 99 ? "99+" : count.ToString();
            label.transform.parent.gameObject.SetActive(count > 0);
        }

        private void OnEnable() { if (_built) { _world.Refresh(); Refresh(); } }

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
            _doo.text = save.Doo.ToString("N0") + "두";
            _material.text = Gear.Polish.Name + "  " + Inventory.Count(save, Gear.StoneId).ToString("N0");
            _experience.SetPct((float)save.Exp / Math.Max(1L, Progression.ExpToNext(save.Level)));
            var stage = _world.FeaturedStage;
            if (stage != null)
            {
                _featuredTitle.text = stage.Number.ToString("00") + " · " + stage.Name;
                _featuredDetail.text = "권장 Lv." + stage.RecommendedPlayerLevel + " · 전투 " + stage.Encounters.Length + "회";
                _featuredAction.text = !Promotion.StageAllowed(save, stage.Number) ? "승급 필요" : StageProgress.Has(save, stage.Number) ? "이어하기" : save.ClearedStage >= stage.Number ? "재도전" : "출전";
                _journeyArt.sprite = ArtLibrary.Background(stage.Number) ?? ArtLibrary.Background(1);
            }
            int arts = Discovery.Count(save);
            _arts.text = arts > 0 ? "새 무공·심법 " + arts + "개" : "무공 · 심법 관리";
            SetQuickCount(_quest, Quests.Open(save).Count);
            SetQuickCount(_daily, Daily.ClaimableCount(save));
            SetQuickCount(_fortune, Fortunes.Open(save).Count);
            SetQuickCount(_stars, StageStars.ClaimableCount(save));
            RefreshLobby();
        }

        private void RefreshLobby()
        {
            if (!_built || GameManager.Instance == null) return;
            var save = GameManager.Instance.Save;
            var scene = Lobby.Pick(save, _lobbyStep);
            _name.text = scene.Speaker;
            _quote.text = scene.Text;
            _portrait.sprite = ArtLibrary.Feathered(ArtLibrary.Portrait(scene.Speaker));
            _portrait.color = _portrait.sprite != null ? Color.white : Color.clear;
            _portraitGlyph.text = _portrait.sprite == null ? scene.Speaker.Substring(0, 1) : "";
            _realm.text = scene.Speaker == StoryLine.Protagonist ? Realm.Label(Realm.StepOf(Realm.Power(InnerArts.HeroStatsBeforeRealm(save)))) : "";
            _scenery.sprite = ArtLibrary.Background(Math.Max(1, Math.Min(save.ClearedStage, EarlyGameContent.Stages.Length))) ?? ArtLibrary.Background(1);
        }
    }
}
