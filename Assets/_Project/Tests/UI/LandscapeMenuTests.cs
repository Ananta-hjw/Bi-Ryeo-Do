#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using BiRyeoDo.Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BiRyeoDo.Tests.UI
{
    public class LandscapeMenuTests
    {
        private GameObject _managerObject, _canvas;
        private GameManager _previous;
        private HelpPanel _previousHelp;
        private SaveData _save;
        private RectTransform Root { get { return _canvas.GetComponent<RectTransform>(); } }
        private static readonly FieldInfo InstanceField = typeof(GameManager).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo HelpField = typeof(HelpPanel).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);

        [SetUp]
        public void SetUp()
        {
            _previous = GameManager.Instance; _previousHelp = HelpPanel.Instance;
            HelpField.SetValue(null, null);
            _managerObject = new GameObject("MenuTestManager");
            _managerObject.SetActive(false);
            var manager = _managerObject.AddComponent<GameManager>();
            _save = new SaveData { ClearedStage = 29, Doo = 100000 };
            Inventory.EnsureStartKit(_save);
            Discovery.Sync(_save);
            foreach (var topic in Tutorial.All) Tutorial.MarkDone(_save, topic.Id);
            // Inject an in-memory save; these tests never invoke a purchase, reward, reset, or save button.
            typeof(GameManager).GetField("<Save>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, _save);
            InstanceField.SetValue(null, manager);
            _canvas = new GameObject("MenuWorkspace", typeof(RectTransform), typeof(Canvas));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            Root.sizeDelta = new Vector2(1920f, 960f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvas); Object.DestroyImmediate(_managerObject);
            InstanceField.SetValue(null, _previous); HelpField.SetValue(null, _previousHelp);
        }

        private static object Invoke(object screen, string method, params object[] args)
        {
            return screen.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(screen, args);
        }

        private Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            var min = Root.InverseTransformPoint(corners[0]); var max = Root.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void Separate(string leftPath, string rightPath)
        {
            Canvas.ForceUpdateCanvases();
            var left = Bounds((RectTransform)_canvas.transform.Find(leftPath));
            var right = Bounds((RectTransform)_canvas.transform.Find(rightPath));
            Assert.Less(left.xMax, right.xMin, leftPath + " overlaps " + rightPath);
            Assert.Greater(left.height, 20f); Assert.Greater(right.height, 20f);
        }

        private void ComfortableButton(string path)
        {
            Canvas.ForceUpdateCanvases();
            var button = _canvas.transform.Find(path).GetComponent<Button>();
            Assert.IsNotNull(button, path);
            Assert.GreaterOrEqual(Bounds((RectTransform)button.transform).height, 94f, path + " is too short to tap comfortably");
        }

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void SmallMenuActionsHaveLargerVisibleHeightsAndDoNotCoverTheirLists(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            var worldRoot = UiFactory.Rect("World", Root, Vector2.zero, Vector2.one);
            var world = worldRoot.gameObject.AddComponent<WorldScreen>();
            world.Build(worldRoot, stage => { }, offer => { }, () => { });
            foreach (var method in new[] { "OpenStageOverlay", "OpenQuestOverlay", "OpenTowerOverlay", "OpenFortuneOverlay", "OpenPartyOverlay", "OpenSettingsOverlay" })
            {
                Invoke(world, method);
            }
            foreach (var name in new[] { "StageOverlay", "QuestOverlay", "TowerOverlay", "FortuneOverlay", "PartyOverlay", "SettingsOverlay" })
                ComfortableButton("World/" + name + "/Close");
            Assert.Greater(Bounds((RectTransform)worldRoot.Find("StageOverlay/Viewport")).yMin,
                Bounds((RectTransform)worldRoot.Find("StageOverlay/Close")).yMax);
            ComfortableButton("World/PartyOverlay/Formation/FormAuto");

            var bagRoot = UiFactory.Rect("Bag", Root, Vector2.zero, Vector2.one);
            bagRoot.gameObject.AddComponent<BagScreen>().Build(bagRoot);
            ComfortableButton("Bag/BulkDismantleButton"); ComfortableButton("Bag/Help");
            Assert.Less(Bounds((RectTransform)bagRoot.Find("ItemDetail")).yMax,
                Bounds((RectTransform)bagRoot.Find("BulkDismantleButton")).yMin);
            for (int i = 0; i < 4; i++) ComfortableButton("Bag/Filter" + i);
            for (int i = 0; i < 5; i++) ComfortableButton("Bag/GearTabs/GearFilter" + i);
            bagRoot.Find("BulkDismantleButton").GetComponent<Button>().onClick.Invoke();
            ComfortableButton("Bag/BulkDismantle/Card/Go"); ComfortableButton("Bag/BulkDismantle/Card/Close");

            var recordRoot = UiFactory.Rect("Record", Root, Vector2.zero, Vector2.one);
            recordRoot.gameObject.AddComponent<RecordScreen>().Build(recordRoot, (title, lines, done) => { });
            recordRoot.Find("Menu_tower").GetComponent<Button>().onClick.Invoke();
            string faction = "Record/Workspace/FactionOverlay/";
            ComfortableButton(faction + "Tab0"); ComfortableButton(faction + "Tab1"); ComfortableButton(faction + "Close");
            var factionRoot = recordRoot.Find("Workspace/FactionOverlay");
            Assert.Greater(Bounds((RectTransform)factionRoot.Find("Viewport")).yMin,
                Bounds((RectTransform)factionRoot.Find("Close")).yMax);
            Assert.Less(Bounds((RectTransform)factionRoot.Find("Viewport")).yMax,
                Bounds((RectTransform)factionRoot.Find("ToastViewport")).yMin);
            Assert.Less(Bounds((RectTransform)factionRoot.Find("ToastViewport")).yMax,
                Bounds((RectTransform)factionRoot.Find("Tab0")).yMin);

            var peopleRoot = UiFactory.Rect("PeopleTest", Root, Vector2.zero, Vector2.one);
            peopleRoot.gameObject.AddComponent<CompanionScreen>().Build(peopleRoot, () => { }, () => { });
            ComfortableButton("PeopleTest/Identity/Formation");
            for (int i = 0; i < 4; i++) ComfortableButton("PeopleTest/Pages/Page" + i);
            peopleRoot.Find("Pages/Page1").GetComponent<Button>().onClick.Invoke();
            for (int i = 0; i < 6; i++) ComfortableButton("PeopleTest/Page/Slot" + i);
            foreach (var name in new[] { "Enhance", "Remove", "Details" }) ComfortableButton("PeopleTest/Page/Equipped/" + name);
        }

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void LobbyQuickMenusHaveSeparateTitlesBadgesAndLargeTargetsWithoutChangingRoutes(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            var routeRoot = UiFactory.Rect("WorldRoutes", Root, Vector2.zero, Vector2.one);
            var world = routeRoot.gameObject.AddComponent<WorldScreen>();
            world.Build(routeRoot, stage => { }, offer => { }, () => { });
            routeRoot.gameObject.SetActive(false);
            var homeRoot = UiFactory.Rect("Home", Root, Vector2.zero, Vector2.one);
            var home = homeRoot.gameObject.AddComponent<LandscapeHome>();
            WorldScreen.Menu? openedMenu = null;
            TabRouter.Tab? selectedTab = null;
            home.Build(homeRoot, world, menu => openedMenu = menu, tab => selectedTab = tab);
            Canvas.ForceUpdateCanvases();
            ComfortableButton("Home/StageList");
            ComfortableButton("Home/Header/Settings");
            ComfortableButton("Home/Header/Status");
            string[] names = { "의뢰", "무한 비무대", "파티 편성", "뽑기", "일일 임무", "별 보상", "기연", "승급 시험" };
            WorldScreen.Menu?[] routes = { WorldScreen.Menu.Quests, WorldScreen.Menu.Tower, WorldScreen.Menu.Party, null,
                WorldScreen.Menu.Daily, WorldScreen.Menu.Stars, WorldScreen.Menu.Fortunes, WorldScreen.Menu.Promotion };
            for (int i = 0; i < names.Length; i++)
            {
                var menu = homeRoot.Find("Quick_" + names[i]);
                Assert.IsNotNull(menu);
                var bounds = Bounds((RectTransform)menu);
                Assert.Greater(bounds.height, 120f);
                Assert.Greater(bounds.width, 170f);
                Assert.AreEqual(names[i], menu.Find("Title").GetComponent<Text>().text);
                var title = (RectTransform)menu.Find("Title");
                var badge = (RectTransform)menu.Find("Badge");
                var description = (RectTransform)menu.Find("Description");
                Assert.Less(description.anchorMax.y, title.anchorMin.y);
                Assert.Less(title.anchorMax.y, badge.anchorMin.y);
                if (i % 4 != 3)
                    Assert.Less(bounds.xMax, Bounds((RectTransform)homeRoot.Find("Quick_" + names[i + 1])).xMin);
                if (i < 4)
                    Assert.Greater(bounds.yMin, Bounds((RectTransform)homeRoot.Find("Quick_" + names[i + 4])).yMax);
                openedMenu = null; selectedTab = null;
                menu.GetComponent<Button>().onClick.Invoke();
                if (routes[i].HasValue) Assert.AreEqual(routes[i], openedMenu);
                else Assert.AreEqual(TabRouter.Tab.Gacha, selectedTab);
            }
            var gear = Bounds((RectTransform)homeRoot.Find("Home_행낭"));
            Assert.Greater(gear.yMin, Bounds((RectTransform)homeRoot.Find("Quick_의뢰")).yMax);
            var questBadge = homeRoot.Find("Quick_의뢰/Badge");
            int quests = Quests.Open(_save).Count;
            Assert.AreEqual(quests > 0, questBadge.gameObject.activeSelf);
            if (quests > 0) Assert.AreEqual(quests > 99 ? "99+" : quests.ToString(), questBadge.Find("Count").GetComponent<Text>().text);
            home.Refresh();
            Assert.AreEqual("의뢰", homeRoot.Find("Quick_의뢰/Title").GetComponent<Text>().text);
        }

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void BagKeepsThreeColumnsAndInspectorClearOfTheListWhenFiltersChange(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            foreach (var item in EarlyGameContent.Items.Where(i => i.Kind == ItemKind.Weapon).Take(9)) Inventory.Add(_save, item.Id, 1);
            var bag = _canvas.AddComponent<BagScreen>(); bag.Build(Root);
            Canvas.ForceUpdateCanvases(); Invoke(bag, "LateUpdate");
            Separate("Viewport", "ItemDetail");
            var content = _canvas.transform.Find("Viewport/Content");
            var first = Bounds((RectTransform)content.Find("Item0"));
            var second = Bounds((RectTransform)content.Find("Item1"));
            var third = Bounds((RectTransform)content.Find("Item2"));
            var fourth = Bounds((RectTransform)content.Find("Item3"));
            Assert.Less(first.xMax, second.xMin); Assert.Less(second.xMax, third.xMin);
            Assert.That(first.yMax, Is.EqualTo(third.yMax).Within(.1f));
            Assert.Less(fourth.yMax, first.yMin);
            Invoke(bag, "OpenDetail", "iron_sword");
            var detail = _canvas.transform.Find("ItemDetail");
            Assert.IsTrue(detail.gameObject.activeSelf);
            Assert.IsNotNull(detail.Find("Card/BodyViewport"));
            _canvas.transform.Find("Filter2").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(detail.gameObject.activeSelf);
        }

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void BulkDismantleIsReachableFromBagAndOnlyPreviewsUntilConfirmed(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            Inventory.Add(_save, "iron_sword", 3);
            var bag = _canvas.AddComponent<BagScreen>(); bag.Build(Root);
            _canvas.transform.Find("Filter2").GetComponent<Button>().onClick.Invoke();
            var launch = _canvas.transform.Find("BulkDismantleButton").GetComponent<Button>();
            Assert.IsTrue(launch.interactable);
            launch.onClick.Invoke();
            var modal = _canvas.transform.Find("BulkDismantle");
            Assert.IsTrue(modal.gameObject.activeSelf);
            Assert.IsTrue(modal.GetComponent<Image>().raycastTarget);
            Canvas.ForceUpdateCanvases();
            var list = Bounds((RectTransform)modal.Find("Card/ListViewport"));
            var options = Bounds((RectTransform)modal.Find("Card/Singles"));
            Assert.Less(options.xMax, list.xMin);
            StringAssert.StartsWith("☐", modal.Find("Card/Singles").GetComponentInChildren<Text>().text);
            StringAssert.StartsWith("☑", modal.Find("Card/G0").GetComponentInChildren<Text>().text);
            StringAssert.StartsWith("☐", modal.Find("Card/G2").GetComponentInChildren<Text>().text);
            Assert.IsTrue(modal.Find("Card/ListViewport/ListContent").GetComponentsInChildren<Text>().Any(t => t.text.Contains(EarlyGameContent.FindItem("iron_sword").Name)));
            Assert.AreEqual(3, Inventory.Count(_save, "iron_sword"), "Opening the preview must not dismantle anything");
            _canvas.SetActive(false);
            Assert.IsFalse(modal.gameObject.activeSelf, "Leaving the bag must dismiss the bulk preview");
        }

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void MartialArtRowsKeepTheirHeightAndPositionAcrossRepeatedRefreshes(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            var art = EarlyGameContent.Arts.First(a => a.OwnerId == null);
            if (!_save.LearnedArts.Contains(art.Id)) _save.LearnedArts.Add(art.Id);
            if (!_save.EquippedArts.Contains(art.Id)) _save.EquippedArts.Add(art.Id);
            var arts = _canvas.AddComponent<MartialArtsScreen>(); arts.Build(Root);
            var content = (RectTransform)_canvas.transform.Find("ArtsPage/Viewport/Content");
            var layout = content.GetComponent<ReadingList>();
            Canvas.ForceUpdateCanvases(); layout.Reflow();
            var rows = Enumerable.Range(0, content.childCount).Select(i => (RectTransform)content.GetChild(i)).ToArray();
            var heights = rows.Select(row => row.rect.height).ToArray();
            var positions = rows.Select(row => row.anchoredPosition).ToArray();
            float contentHeight = content.sizeDelta.y;
            Assert.Less(heights[0], 500f, "The first card must not consume the whole screen");
            for (int pass = 0; pass < 30; pass++)
            {
                Invoke(arts, "Refresh");
                layout.SendMessage("Invalidate");
                Canvas.ForceUpdateCanvases(); layout.Reflow();
                for (int i = 0; i < rows.Length; i++)
                {
                    Assert.That(rows[i].rect.height, Is.EqualTo(heights[i]).Within(.5f));
                    Assert.That(rows[i].anchoredPosition.y, Is.EqualTo(positions[i].y).Within(.5f));
                }
                Assert.That(content.sizeDelta.y, Is.EqualTo(contentHeight).Within(.5f));
            }
        }

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void InnerArtsUseTwoCardsPerRowWithStableHeightAndSeparateActions(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            var arts = _canvas.AddComponent<MartialArtsScreen>(); arts.Build(Root);
            Invoke(arts, "ShowPage", 1, false);
            var content = (RectTransform)_canvas.transform.Find("InnerArtPage/Viewport/Content");
            var layout = content.GetComponent<ReadingList>();
            Canvas.ForceUpdateCanvases(); layout.Reflow();
            var rows = Enumerable.Range(0, content.childCount).Select(i => (RectTransform)content.GetChild(i)).ToArray();
            Assert.AreEqual((InnerArts.All.Length + 1) / 2, rows.Length);
            for (int index = 0; index < InnerArts.All.Length; index++)
            {
                var card = rows[index / 2].Find("Card" + index);
                Assert.IsNotNull(card);
                var act = Bounds((RectTransform)card.Find("Act"));
                Assert.Less(act.yMax, Bounds((RectTransform)card.Find("Desc")).yMin);
                if (index % 2 == 1)
                {
                    var left = rows[index / 2].Find("Card" + (index - 1));
                    Assert.Less(Bounds((RectTransform)left).xMax, Bounds((RectTransform)card).xMin);
                    Assert.That(Bounds((RectTransform)left).yMax, Is.EqualTo(Bounds((RectTransform)card).yMax).Within(.1f));
                }
            }
            var heights = rows.Select(row => row.rect.height).ToArray();
            float totalHeight = content.sizeDelta.y;
            for (int pass = 0; pass < 20; pass++)
            {
                layout.SendMessage("Invalidate"); Canvas.ForceUpdateCanvases(); layout.Reflow();
                for (int i = 0; i < rows.Length; i++) Assert.That(rows[i].rect.height, Is.EqualTo(heights[i]).Within(.5f));
                Assert.That(content.sizeDelta.y, Is.EqualTo(totalHeight).Within(.5f));
            }
        }

        [Test]
        public void ArtDetailsStayBesideTheListAndInnerArtsDoNotLeaveTheInspectorVisible()
        {
            var arts = _canvas.AddComponent<MartialArtsScreen>(); arts.Build(Root);
            Separate("ArtsPage/Viewport", "ArtDetail");
            var art = EarlyGameContent.Arts.First(a => a.OwnerId == null);
            Invoke(arts, "OpenDetail", art);
            Assert.IsTrue(_canvas.transform.Find("ArtDetail").gameObject.activeSelf);
            Assert.IsNotNull(_canvas.transform.Find("ArtDetail/Card/BodyViewport"));
            Invoke(arts, "ShowPage", 1, false);
            Assert.IsFalse(_canvas.transform.Find("ArtDetail").gameObject.activeSelf);
            Assert.IsFalse(_canvas.transform.Find("InspectorPlaceholder").gameObject.activeSelf);
            Assert.IsTrue(_canvas.transform.Find("InnerArtPage").gameObject.activeSelf);
        }

        [Test]
        public void RecordSwitchClosesThePreviousPageAndStatusUsesTheSharedCharacterRoute()
        {
            bool openedCharacters = false;
            var record = _canvas.AddComponent<RecordScreen>();
            record.Build(Root, (title, lines, done) => { }, () => openedCharacters = true);
            _canvas.transform.Find("Menu_quest").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(_canvas.transform.Find("Workspace/StoryLogOverlay").gameObject.activeSelf);
            _canvas.transform.Find("Menu_record").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(_canvas.transform.Find("Workspace/StoryLogOverlay").gameObject.activeSelf);
            Assert.IsTrue(_canvas.transform.Find("Workspace/CodexOverlay").gameObject.activeSelf);
            Separate("Menu_record", "Workspace");
            _canvas.transform.Find("Menu_party").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(openedCharacters);
        }

        [TestCase(1440f, 0)]
        [TestCase(1920f, 0)]
        [TestCase(2400f, 0)]
        [TestCase(1440f, 29)]
        [TestCase(1920f, 29)]
        [TestCase(2400f, 29)]
        public void PartyRosterAndEmptyMessageStayClearOfTheFormation(float width, int clearedStage)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            _save.ClearedStage = clearedStage;
            var world = _canvas.AddComponent<WorldScreen>();
            world.Build(Root, stage => { }, offer => { }, () => { });
            for (int pass = 0; pass < 2; pass++)
            {
                Invoke(world, "OpenPartyOverlay");
                Separate("PartyOverlay/Roster", "PartyOverlay/Formation");
                Canvas.ForceUpdateCanvases();
                var roster = (RectTransform)_canvas.transform.Find("PartyOverlay/Roster");
                var none = roster.Find("None");
                if (clearedStage == 0)
                {
                    Assert.IsNotNull(none);
                    var message = none.GetComponent<Text>();
                    Assert.AreEqual(TextAnchor.MiddleCenter, message.alignment);
                    var bounds = Bounds(message.rectTransform);
                    Assert.IsTrue(Bounds(roster).Contains(bounds.min));
                    Assert.IsTrue(Bounds(roster).Contains(bounds.max));
                    Separate("PartyOverlay/Roster/None", "PartyOverlay/Formation");
                }
                else
                {
                    Assert.IsNull(none);
                    var content = roster.Find("Viewport/Content");
                    Assert.Greater(content.GetComponentsInChildren<Button>().Length, 0);
                    content.GetComponent<ReadingList>().Reflow();
                    foreach (var button in content.GetComponentsInChildren<Button>())
                        Assert.Less(Bounds((RectTransform)button.transform).xMax,
                            Bounds((RectTransform)_canvas.transform.Find("PartyOverlay/Formation")).xMin);
                }
            }
        }

        [TestCase(1440f)]
        [TestCase(2400f)]
        public void PrepRebuildKeepsLargeFormationTilesSeparateFromEnemyInformation(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            var world = _canvas.AddComponent<WorldScreen>();
            world.Build(Root, stage => { }, offer => { }, () => { });
            for (int pass = 0; pass < 2; pass++)
            {
                Invoke(world, "OpenPrep", EarlyGameContent.Stages[0], (Action)(() => { }));
                Separate("PrepOverlay/EnemiesViewport", "PrepOverlay/Formation");
                Canvas.ForceUpdateCanvases();
                for (int col = 0; col < 3; col++)
                    for (int row = 0; row < 3; row++)
                    {
                        var tile = (RectTransform)_canvas.transform.Find("PrepOverlay/Formation/Tile" + col + row);
                        Assert.Greater(tile.rect.height, 100f); Assert.Greater(tile.rect.width, 150f);
                    }
            }
        }

        [TestCase(1440f)]
        [TestCase(2400f)]
        public void GachaOddsAndPurchaseButtonsNeverCoverTheResults(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            _canvas.AddComponent<GachaScreen>().Build(Root);
            Separate("Odds", "Viewport");
            var results = Bounds((RectTransform)_canvas.transform.Find("Viewport"));
            var odds = Bounds((RectTransform)_canvas.transform.Find("Odds"));
            foreach (int count in new[] { 1, 10, 50 })
            {
                var button = Bounds((RectTransform)_canvas.transform.Find("Pull" + count));
                Assert.Less(button.xMax, results.xMin);
                Assert.Less(button.yMax, odds.yMin);
                Assert.Greater(button.height, 80f);
            }
        }

        [Test]
        public void RitualConditionsStaySeparateFromRewardDescriptionAndItsAction()
        {
            var panel = new RitualPanel(Root, () => { }, null);
            panel.Open();
            Separate("Viewport", "RewardEffectsViewport");
            var list = Bounds((RectTransform)_canvas.transform.Find("Viewport"));
            var start = Bounds((RectTransform)_canvas.transform.Find("Start"));
            Assert.Less(list.xMax, start.xMin);
        }

        [Test]
        public void LongHelpAndInfoTextUseMaskedScrollingAndKeepTheirActionsOutsideTheBody()
        {
            _canvas.AddComponent<HelpPanel>().Build(Root);
            Separate("Help/Card/TitleViewport", "Help/Card/BodyViewport");
            var window = UiFactory.InfoWindow(Root, "안내", new string('가', 3000));
            UiFactory.ShowInfo(window); Canvas.ForceUpdateCanvases();
            var reading = window.Find("Card/BodyViewport/Body").GetComponent<ReadingText>();
            reading.SendMessage("LateUpdate");
            Assert.Greater(reading.Scroll.content.rect.height, reading.Viewport.rect.height);
            Assert.IsNotNull(window.Find("Card").GetComponent<Button>());
            var body = Bounds(reading.Viewport); var close = Bounds((RectTransform)window.Find("Card/Close"));
            Assert.Less(close.yMax, body.yMin);
        }
    }
}
#endif
