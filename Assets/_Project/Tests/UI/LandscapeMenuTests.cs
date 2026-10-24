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
        public void TabHeadersShareHeightAndKeepIconsAndControlsAligned(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            foreach (var kind in new[] { "bag", "gacha", "record", "arts" })
            {
                var root = UiFactory.Rect("Screen_" + kind, Root, Vector2.zero, Vector2.one);
                if (kind == "bag") root.gameObject.AddComponent<BagScreen>().Build(root);
                else if (kind == "gacha") root.gameObject.AddComponent<GachaScreen>().Build(root);
                else if (kind == "record") root.gameObject.AddComponent<RecordScreen>().Build(root, null);
                else root.gameObject.AddComponent<MartialArtsScreen>().Build(root);
                var page = kind == "arts" ? root.Find("ArtsPage") : root;
                Canvas.ForceUpdateCanvases();
                var band = Bounds((RectTransform)page.Find("HeaderBand"));
                var icon = Bounds((RectTransform)page.Find("HeaderBand/Glyph_" + kind));
                var title = Bounds((RectTransform)page.Find("HeaderBand/Header"));
                Assert.That(band.height, Is.EqualTo(115f).Within(.1f));
                Assert.That(icon.width, Is.EqualTo(92f).Within(.1f));
                Assert.That(icon.height, Is.EqualTo(92f).Within(.1f));
                Assert.That(icon.center.y, Is.EqualTo(band.center.y).Within(.1f));
                Assert.Less(icon.xMax, title.xMin);
                Assert.Greater(icon.yMin, band.yMin);
                Assert.Less(icon.yMax, band.yMax);
                if (kind == "record")
                    Assert.Less(Bounds((RectTransform)root.Find("Workspace")).yMax, band.yMin);
                else
                {
                    var help = Bounds((RectTransform)page.Find("Help"));
                    Assert.That(help.center.y, Is.EqualTo(band.center.y).Within(.1f));
                    Assert.Less(title.xMax, help.xMin);
                    Assert.Less(Bounds((RectTransform)page.Find("Viewport")).yMax, band.yMin);
                }
                if (kind == "bag")
                    Assert.That(Bounds((RectTransform)root.Find("BulkDismantleButton")).center.y,
                        Is.EqualTo(band.center.y).Within(.1f));
                if (kind == "arts")
                {
                    root.Find("Page1").GetComponent<Button>().onClick.Invoke();
                    Canvas.ForceUpdateCanvases();
                    Assert.That(Bounds((RectTransform)root.Find("InnerArtPage/HeaderBand")).height,
                        Is.EqualTo(band.height).Within(.1f));
                }
            }
        }

        [TestCase(1440f)]
        [TestCase(2400f)]
        public void AllHelpButtonsAreCompactCirclesAndArtsOnlyShowsItsTwoPages(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            foreach (var name in new[] { "BagHelp", "GachaHelp", "ArtsHelp" })
            {
                var root = UiFactory.Rect(name, Root, Vector2.zero, Vector2.one);
                if (name == "BagHelp") root.gameObject.AddComponent<BagScreen>().Build(root);
                else if (name == "GachaHelp") root.gameObject.AddComponent<GachaScreen>().Build(root);
                else root.gameObject.AddComponent<MartialArtsScreen>().Build(root);
                Canvas.ForceUpdateCanvases();
                var help = root.Find(name == "ArtsHelp" ? "ArtsPage/Help" : "Help");
                var rect = (RectTransform)help;
                Assert.That(rect.rect.width, Is.EqualTo(76f).Within(.1f));
                Assert.That(rect.rect.height, Is.EqualTo(76f).Within(.1f));
                var image = help.GetComponent<Image>();
                Assert.IsNotNull(image.sprite);
                Assert.IsTrue(image.preserveAspect);
                Assert.Greater(image.alphaHitTestMinimumThreshold, 0f);
                Assert.IsNull(help.Find("Frame"), "A circular button must not retain the square frame");
                help.GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(root.Find("InfoWindow").gameObject.activeSelf);
                root.Find("InfoWindow/Card/Close").GetComponent<Button>().onClick.Invoke();
                Assert.IsFalse(root.Find("InfoWindow").gameObject.activeSelf);
                if (name == "ArtsHelp")
                {
                    Assert.IsNull(root.Find("CompanionTraining"));
                    Assert.AreEqual("무공", root.Find("Page0").GetComponentInChildren<Text>().text);
                    Assert.AreEqual("심법", root.Find("Page1").GetComponentInChildren<Text>().text);
                    Separate(name + "/Page0", name + "/Page1");
                }
            }
        }

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void StageMapKeepsEveryJourneyReachableAndStopsSeparateFromInspector(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            var world = _canvas.AddComponent<WorldScreen>();
            world.Build(Root, stage => { }, offer => { }, () => { });
            Invoke(world, "OpenStageOverlay");
            var map = (StageMapPanel)typeof(WorldScreen).GetField("_stageMap", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(world);
            var seen = new System.Collections.Generic.HashSet<int>();
            for (int page = 0; page < map.PageCount; page++)
            {
                map.Select(EarlyGameContent.Stages[page * StageMapPanel.StagesPerPage].Number, _save);
                Canvas.ForceUpdateCanvases();
                var nodes = Root.Find("StageOverlay/Viewport/Nodes").Cast<Transform>()
                    .Where(child => child.name.StartsWith("Stage")).ToArray();
                Assert.AreEqual(System.Math.Min(6, EarlyGameContent.Stages.Length - page * 6), nodes.Length);
                var viewport = Bounds((RectTransform)Root.Find("StageOverlay/Viewport"));
                var inspector = Bounds((RectTransform)Root.Find("StageOverlay/StageDetail"));
                foreach (var node in nodes)
                {
                    int number = int.Parse(node.name.Substring(5));
                    Assert.IsTrue(seen.Add(number));
                    var stage = EarlyGameContent.FindStage(number);
                    Assert.AreEqual(stage.Name, node.Find("NamePlate/Name").GetComponent<Text>().text);
                    var bounds = Bounds((RectTransform)node);
                    Assert.GreaterOrEqual(bounds.xMin, viewport.xMin);
                    Assert.LessOrEqual(bounds.xMax, viewport.xMax);
                    Assert.Less(bounds.xMax, inspector.xMin);
                    Assert.Greater(bounds.height, 150);
                    foreach (var other in nodes)
                        if (other != node) Assert.IsFalse(bounds.Overlaps(Bounds((RectTransform)other)), node.name + " overlaps " + other.name);
                }
                Assert.AreEqual(page > 0, Root.Find("StageOverlay/MapNavigation/Previous").GetComponent<Button>().interactable);
                Assert.AreEqual(page + 1 < map.PageCount, Root.Find("StageOverlay/MapNavigation/Next").GetComponent<Button>().interactable);
                ComfortableButton("StageOverlay/StageDetail/Go");
            }
            Assert.AreEqual(EarlyGameContent.Stages.Length, seen.Count);
        }

        [Test]
        public void MapInspectionDoesNotStartLockedStagesAndResumeUsesTheStoredDifficulty()
        {
            var overlay = UiFactory.Rect("Map", Root, Vector2.zero, Vector2.one);
            int started = 0;
            _save.ClearedStage = 0;
            var panel = new StageMapPanel(overlay, stage => started++, () => { }, () => { }, () => { });
            panel.Select(36, _save);
            Assert.AreEqual(0, started);
            Assert.IsFalse(overlay.Find("StageDetail/Go").GetComponent<Button>().interactable);
            overlay.Find("StageDetail/Go").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(0, started, "the callback must also guard against locked stages");
            StringAssert.Contains("이전 스테이지", overlay.Find("StageDetail/ConditionViewport/Condition").GetComponent<Text>().text);
            panel.Select(1, _save);
            overlay.Find("StageDetail/Go").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(1, started);
            _save.ProgressStage = 1; _save.ProgressNext = 1; _save.ProgressDifficulty = 0; _save.Difficulty = 2;
            panel.Open(_save);
            Assert.AreEqual(1, panel.SelectedStage);
            StringAssert.Contains("일반", overlay.Find("StageDetail/StageKind").GetComponent<Text>().text);
            Assert.AreEqual("이어하기", overlay.Find("StageDetail/Go").GetComponentInChildren<Text>().text);
        }

        [Test]
        public void MapShortcutsOpenAboveTheMapAndKeepItsSelection()
        {
            var world = _canvas.AddComponent<WorldScreen>();
            world.Build(Root, stage => { }, offer => { }, () => { });
            Invoke(world, "OpenStageOverlay");
            var map = (StageMapPanel)typeof(WorldScreen).GetField("_stageMap", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(world);
            map.Select(9, _save);
            foreach (var shortcut in new[] { new[] { "StarRewards", "StarOverlay" }, new[] { "Inn", "ShopOverlay" } })
            {
                Root.Find("StageOverlay/MapHeader/" + shortcut[0]).GetComponent<Button>().onClick.Invoke();
                var overlay = Root.Find(shortcut[1]);
                Assert.IsTrue(overlay.gameObject.activeSelf);
                Assert.Greater(overlay.GetSiblingIndex(), Root.Find("StageOverlay").GetSiblingIndex());
                overlay.Find("Back").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(9, map.SelectedStage);
                Assert.IsTrue(Root.Find("StageOverlay").gameObject.activeSelf);
            }
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
            // 닫기 is gone: each full-screen overlay has the 돌아가기 arrow in its top-left corner, above its list.
            foreach (var name in new[] { "StageOverlay", "QuestOverlay", "TowerOverlay", "FortuneOverlay", "PartyOverlay", "SettingsOverlay" })
            {
                ComfortableButton("World/" + name + "/Back");
                Assert.IsNull(worldRoot.Find(name + "/Close"), name);
                var overlay = Bounds((RectTransform)worldRoot.Find(name));
                Assert.GreaterOrEqual(Bounds((RectTransform)worldRoot.Find(name + "/Back")).yMin, overlay.yMax - 110f, name);
            }
            Assert.Less(Bounds((RectTransform)worldRoot.Find("StageOverlay/Viewport")).yMax,
                Bounds((RectTransform)worldRoot.Find("StageOverlay/Back")).yMin + 1f);
            ComfortableButton("World/PartyOverlay/Formation/FormAuto");

            var bagRoot = UiFactory.Rect("Bag", Root, Vector2.zero, Vector2.one);
            bagRoot.gameObject.AddComponent<BagScreen>().Build(bagRoot);
            Canvas.ForceUpdateCanvases();
            Assert.That(Bounds((RectTransform)bagRoot.Find("BulkDismantleButton")).height, Is.InRange(64f, 90f));
            Assert.That(Bounds((RectTransform)bagRoot.Find("Help")).height, Is.EqualTo(76f).Within(.1f));
            Assert.Less(Bounds((RectTransform)bagRoot.Find("ItemDetail")).yMax,
                Bounds((RectTransform)bagRoot.Find("BulkDismantleButton")).yMin);
            for (int i = 0; i < 4; i++) ComfortableButton("Bag/Filter" + i);
            for (int i = 0; i < 5; i++) ComfortableButton("Bag/GearTabs/GearFilter" + i);
            bagRoot.Find("BulkDismantleButton").GetComponent<Button>().onClick.Invoke();
            ComfortableButton("Bag/BulkDismantle/Card/Go"); ComfortableButton("Bag/BulkDismantle/Card/Close");

            var recordRoot = UiFactory.Rect("Record", Root, Vector2.zero, Vector2.one);
            recordRoot.gameObject.AddComponent<RecordScreen>().Build(recordRoot, (title, lines, done) => { });
            recordRoot.Find("MenuViewport/Viewport/Content/Menu_faction").GetComponent<Button>().onClick.Invoke();
            string faction = "Record/Workspace/FactionOverlay/";
            ComfortableButton(faction + "Back");   // 세력 평판 only: the 객잔 is its own panel now, so there are no tabs
            var factionRoot = recordRoot.Find("Workspace/FactionOverlay");
            Assert.IsNull(factionRoot.Find("Tab0"));
            Assert.Less(Bounds((RectTransform)factionRoot.Find("Viewport")).yMax,
                Bounds((RectTransform)factionRoot.Find("ToastViewport")).yMin);

            var peopleRoot = UiFactory.Rect("PeopleTest", Root, Vector2.zero, Vector2.one);
            peopleRoot.gameObject.AddComponent<CompanionScreen>().Build(peopleRoot, () => { });
            ComfortableButton("PeopleTest/Identity/ChangeCharacter");
            for (int i = 0; i < 3; i++) ComfortableButton("PeopleTest/Pages/Page" + i);
            var equipmentRoot = UiFactory.Rect("EquipmentTest", Root, Vector2.zero, Vector2.one);
            equipmentRoot.gameObject.AddComponent<EquipmentScreen>().Build(equipmentRoot);
            for (int i = 0; i < 6; i++) ComfortableButton("EquipmentTest/Workspace/Character/WearSlot" + i);
            foreach (var name in new[] { "Equip", "Remove", "Details", "Enhance" }) ComfortableButton("EquipmentTest/Workspace/Inventory/Selection/" + name);
        }

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void WholeJourneyCardOpensTheFeaturedRouteWithoutASeparateGoButton(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            var routes = UiFactory.Rect("Routes", Root, Vector2.zero, Vector2.one);
            var world = routes.gameObject.AddComponent<WorldScreen>();
            world.Build(routes, stage => { }, offer => { }, () => { });
            routes.gameObject.SetActive(false);
            var homeRoot = UiFactory.Rect("Home", Root, Vector2.zero, Vector2.one);
            var home = homeRoot.gameObject.AddComponent<LandscapeHome>();
            WorldScreen.Menu? opened = null;
            home.Build(homeRoot, world, menu => opened = menu, tab => { });
            Canvas.ForceUpdateCanvases();
            var journey = (RectTransform)homeRoot.Find("Journey");
            Assert.IsNotNull(journey.GetComponent<Button>());
            Assert.IsNull(journey.Find("Go"));
            Assert.AreEqual(1, journey.GetComponentsInChildren<Button>(true).Length);
            Assert.Greater(Bounds(journey).yMin, Bounds((RectTransform)homeRoot.Find("Home_협객")).yMax);
            Assert.Less(Bounds(journey).yMax, Bounds((RectTransform)homeRoot.Find("StageList")).yMin);
            Assert.Greater(Bounds((RectTransform)homeRoot.Find("Home_협객")).yMin, Bounds((RectTransform)homeRoot.Find("Home_행낭")).yMax);

            _save.SwordRank = Promotion.GateRank;
            world.Refresh(); home.Refresh();
            journey.GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(WorldScreen.Menu.Featured, opened);
            StringAssert.Contains("출전", journey.Find("Action").GetComponent<Text>().text);
            _save.ProgressStage = world.FeaturedStage.Number; _save.ProgressNext = 1;
            home.Refresh();
            StringAssert.Contains("이어하기", journey.Find("Action").GetComponent<Text>().text);
            _save.SwordRank = 1;
            home.Refresh();
            journey.GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(WorldScreen.Menu.Promotion, opened);
            StringAssert.Contains("승급 필요", journey.Find("Action").GetComponent<Text>().text);
        }

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void LobbySideGroupsOpenToTheRightPreserveRoutesAndDismissCleanly(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            var routeRoot = UiFactory.Rect("WorldRoutes", Root, Vector2.zero, Vector2.one);
            var world = routeRoot.gameObject.AddComponent<WorldScreen>();
            world.Build(routeRoot, stage => { }, offer => { }, () => { });
            routeRoot.gameObject.SetActive(false);
            var homeRoot = UiFactory.Rect("Home", Root, Vector2.zero, Vector2.one);
            var home = homeRoot.gameObject.AddComponent<LandscapeHome>();
            WorldScreen.Menu? opened = null;
            TabRouter.Tab? tab = null;
            home.Build(homeRoot, world, menu => opened = menu, selected => tab = selected);
            Canvas.ForceUpdateCanvases();
            ComfortableButton("Home/StageList"); ComfortableButton("Home/Header/Settings");
            foreach (var name in new[] { "MissionsMenu", "ChallengeMenu", "PartyMenu", "ShopMenu" }) ComfortableButton("Home/" + name);
            var layer = homeRoot.Find("FlyoutLayer");
            Assert.IsFalse(layer.gameObject.activeSelf);
            Assert.IsNull(homeRoot.Find("Quick_뽑기"));
            string[] missionNames = { "Quest", "Daily", "Stars", "Fortunes" };
            WorldScreen.Menu[] missionRoutes = { WorldScreen.Menu.Quests, WorldScreen.Menu.Daily, WorldScreen.Menu.Stars, WorldScreen.Menu.Fortunes };
            for (int i = 0; i < missionNames.Length; i++)
            {
                homeRoot.Find("MissionsMenu").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(layer.gameObject.activeSelf);
                Assert.IsTrue(layer.Find("Missions").gameObject.activeSelf);
                Assert.IsFalse(layer.Find("Challenges").gameObject.activeSelf);
                ComfortableButton("Home/FlyoutLayer/Missions/" + missionNames[i]);
                Assert.Less(Bounds((RectTransform)homeRoot.Find("MissionsMenu")).xMax, Bounds((RectTransform)layer.Find("Missions")).xMin);
                layer.Find("Missions/" + missionNames[i]).GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(missionRoutes[i], opened);
                Assert.IsFalse(layer.gameObject.activeSelf);
            }
            homeRoot.Find("MissionsMenu").GetComponent<Button>().onClick.Invoke();
            homeRoot.Find("ChallengeMenu").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(layer.Find("Missions").gameObject.activeSelf);
            Assert.IsTrue(layer.Find("Challenges").gameObject.activeSelf);
            homeRoot.Find("ChallengeMenu").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(layer.gameObject.activeSelf, "Tapping the open group again closes it");
            foreach (var name in new[] { "Tower", "Promotion" })
            {
                homeRoot.Find("ChallengeMenu").GetComponent<Button>().onClick.Invoke();
                ComfortableButton("Home/FlyoutLayer/Challenges/" + name);
                layer.Find("Challenges/" + name).GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(name == "Tower" ? WorldScreen.Menu.Tower : WorldScreen.Menu.Promotion, opened);
                Assert.IsFalse(layer.gameObject.activeSelf);
            }
            homeRoot.Find("MissionsMenu").GetComponent<Button>().onClick.Invoke();
            layer.Find("Dismiss").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(layer.gameObject.activeSelf);
            homeRoot.Find("PartyMenu").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(WorldScreen.Menu.Party, opened);
            homeRoot.Find("ShopMenu").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(WorldScreen.Menu.Shop, opened);
            // Six screen cards replace the bottom tab bar: every route but 강호 is reachable from here.
            string[] cards = { "협객", "장비", "무공", "뽑기", "행낭", "기록" };
            TabRouter.Tab[] routes = { TabRouter.Tab.Companions, TabRouter.Tab.Equipment, TabRouter.Tab.MartialArts, TabRouter.Tab.Gacha, TabRouter.Tab.Bag, TabRouter.Tab.Record };
            CollectionAssert.AreEquivalent(System.Enum.GetValues(typeof(TabRouter.Tab)).Cast<TabRouter.Tab>().Where(t => t != TabRouter.Tab.World), routes);
            for (int i = 0; i < cards.Length; i++)
            {
                var card = homeRoot.Find("Home_" + cards[i]);
                Assert.GreaterOrEqual(Bounds((RectTransform)card).height, 275f);
                Assert.GreaterOrEqual(Bounds((RectTransform)card).width, width * .17f);
                Assert.GreaterOrEqual(card.Find("Title").GetComponent<Text>().fontSize, 42);
                card.GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(routes[i], tab);
            }
            int total = Quests.Open(_save).Count + Daily.ClaimableCount(_save) + StageStars.ClaimableCount(_save) + Fortunes.Open(_save).Count;
            var badge = homeRoot.Find("MissionsMenu/Badge");
            Assert.AreEqual(total > 0, badge.gameObject.activeSelf);
            if (total > 0) Assert.AreEqual(total > 99 ? "99+" : total.ToString(), badge.Find("Count").GetComponent<Text>().text);
            homeRoot.Find("MissionsMenu").GetComponent<Button>().onClick.Invoke();
            home.Refresh();
            Assert.IsTrue(layer.gameObject.activeSelf, "Refreshing counts must not close the open group");
            homeRoot.gameObject.SetActive(false);
            Assert.IsFalse(layer.gameObject.activeSelf, "Changing tabs dismisses the submenu");
        }

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void EveryScreenButTheLobbyHasTheSameBackBarAboveItsPanel(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            var main = _canvas.AddComponent<MainScreen>();
            var panel = UiFactory.Rect("Panel", Root, Vector2.zero, Vector2.one);
            panel.offsetMax = new Vector2(0f, -104f);
            Invoke(main, "BuildBackBar", panel, "협객");
            ComfortableButton("Panel/BackBar/Back");
            Canvas.ForceUpdateCanvases();
            var bar = Bounds((RectTransform)panel.Find("BackBar"));
            Assert.That(bar.yMin, Is.EqualTo(Bounds(panel).yMax).Within(.5f), "the bar sits right above the panel");
            Assert.That(bar.width, Is.EqualTo(width).Within(.5f));
            Assert.AreEqual("back", panel.Find("BackBar/Back").GetComponentInChildren<WuxiaGlyph>().Key);
            Assert.Less(Bounds((RectTransform)panel.Find("BackBar/Back")).xMax, Bounds((RectTransform)panel.Find("BackBar/Title")).xMin + 1f);
            Assert.IsNull(typeof(MainScreen).GetMethod("BuildTabBar", BindingFlags.Instance | BindingFlags.NonPublic), "there is no bottom tab bar any more");
        }

        [TestCase(1440f)]
        [TestCase(2400f)]
        public void ScreenHeadersMoveIntoTheTopBarAndLeaveNoBandBehind(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            var main = _canvas.AddComponent<MainScreen>();
            foreach (var kind in new[] { "bag", "gacha", "record", "arts" })
            {
                var panel = UiFactory.Rect("Panel_" + kind, Root, Vector2.zero, Vector2.one);
                panel.offsetMax = new Vector2(0f, -104f);
                Invoke(main, "BuildBackBar", panel, kind);
                var root = UiFactory.Rect("Screen", panel, Vector2.zero, Vector2.one);
                if (kind == "bag") root.gameObject.AddComponent<BagScreen>().Build(root);
                else if (kind == "gacha") root.gameObject.AddComponent<GachaScreen>().Build(root);
                else if (kind == "record") root.gameObject.AddComponent<RecordScreen>().Build(root, null);
                else root.gameObject.AddComponent<MartialArtsScreen>().Build(root);
                panel.Find("BackBar").SetAsLastSibling();
                Canvas.ForceUpdateCanvases();
                var page = kind == "arts" ? root.Find("ArtsPage") : root;
                Assert.IsNull(page.Find("HeaderBand"), kind + ": the old band is gone");
                var info = panel.Find("BackBar/Info");
                Assert.AreEqual(1, info.childCount, kind);
                var host = info.GetChild(0);
                var bar = Bounds((RectTransform)panel.Find("BackBar"));
                var header = Bounds((RectTransform)host.Find("Header"));
                Assert.That(header.center.y, Is.EqualTo(bar.center.y).Within(1f), kind);
                if (kind != "record")
                {
                    var help = Bounds((RectTransform)host.Find("Help"));
                    Assert.That(help.center.y, Is.EqualTo(bar.center.y).Within(1f), kind);
                    Assert.Less(header.xMin, help.xMin);
                }
                if (kind == "bag") Assert.IsNotNull(host.Find("BulkDismantleButton"));
                if (kind == "arts")
                {
                    // The 무공/심법 page buttons now sit right under the top bar, above the art list
                    var pageButton = Bounds((RectTransform)root.Find("Page0"));
                    Assert.Less(pageButton.yMax, bar.yMin + 1f);
                    Assert.Less(Bounds((RectTransform)page.Find("Viewport")).yMax, pageButton.yMin + 1f);
                }
            }
        }

        [Test]
        public void LobbyRecordCardBadgeReflectsUnreadPages()
        {
            var routes = UiFactory.Rect("Routes", Root, Vector2.zero, Vector2.one);
            var world = routes.gameObject.AddComponent<WorldScreen>();
            world.Build(routes, stage => { }, offer => { }, () => { });
            routes.gameObject.SetActive(false);
            var homeRoot = UiFactory.Rect("Home", Root, Vector2.zero, Vector2.one);
            var home = homeRoot.gameObject.AddComponent<LandscapeHome>();
            home.Build(homeRoot, world, menu => { }, tab => { });
            home.Refresh();
            var dot = homeRoot.Find("Home_기록/Badge");
            int count = RecordUpdates.Count(_save);
            Assert.AreEqual(count > 0, dot.gameObject.activeSelf);
            if (count > 0) Assert.AreEqual(count > 99 ? "99+" : count.ToString(), dot.Find("Count").GetComponent<Text>().text);
            foreach (var page in new[] { RecordUpdates.CodexPage, RecordUpdates.StoryPage, RecordUpdates.FactionPage }) RecordUpdates.Acknowledge(_save, page);
            foreach (var achievement in Achievements.All) _save.ClaimedAchievements.Add(achievement.Id);
            foreach (var memory in Memories.All) _save.SeenMemories.Add(memory.Id);
            home.Refresh();
            Assert.IsFalse(dot.gameObject.activeSelf);
            // Start-kit items may already have been discovered; force a fresh valid item.
            Codex.SeeItem(_save, EarlyGameContent.Items.First(item => !_save.SeenItems.Contains(item.Id)).Id);
            home.Refresh();
            Assert.IsTrue(dot.gameObject.activeSelf);
        }

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void FortuneChoicesHaveVisibleGapsAndComfortableTouchTargets(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            var world = _canvas.AddComponent<WorldScreen>();
            world.Build(Root, stage => { }, offer => { }, () => { });
            Invoke(world, "OpenFortuneOverlay");
            foreach (var fortune in Fortunes.All)
            {
                Invoke(world, "ShowFortune", fortune);
                Canvas.ForceUpdateCanvases();
                var choices = Root.Find("FortuneOverlay").GetComponentsInChildren<Button>().Where(button => button.name == "Choice").ToArray();
                Assert.AreEqual(fortune.Choices.Length, choices.Length);
                float previousBottom = float.PositiveInfinity;
                foreach (var choice in choices)
                {
                    var bounds = Bounds((RectTransform)choice.transform);
                    Assert.GreaterOrEqual(bounds.height, 80f);
                    Assert.LessOrEqual(bounds.yMax + 20f, previousBottom);
                    Assert.GreaterOrEqual(bounds.yMin, Bounds((RectTransform)Root.Find("FortuneOverlay")).yMin);
                    previousBottom = bounds.yMin;
                }
            }
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
            var bagIcon = Bounds((RectTransform)content.Find("Item0/Icon"));
            Assert.That(bagIcon.width, Is.EqualTo(132f).Within(.1f));
            Assert.Less(Bounds((RectTransform)content.Find("Item0/Name")).yMax, bagIcon.yMin);
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
        public void MartialArtsUseThreeCardsPerRowAndKeepTheirHeightAcrossRefreshes(float width)
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
            int total = EarlyGameContent.Arts.Count(a => a.OwnerId == null);
            Assert.AreEqual((total + 2) / 3, rows.Length);
            Assert.AreEqual(3, rows[0].childCount);
            var firstCard = rows[0].Find("Card0");
            var artIcon = Bounds((RectTransform)firstCard.Find("Icon"));
            Assert.That(artIcon.width, Is.EqualTo(120f).Within(.1f));
            Assert.That(artIcon.height, Is.EqualTo(120f).Within(.1f));
            Assert.Less(artIcon.xMax, Bounds((RectTransform)firstCard.Find("Title")).xMin);
            for (int index = 0; index < total; index++)
            {
                var card = (RectTransform)rows[index / 3].Find("Card" + index);
                var button = Bounds((RectTransform)card.Find("Btn" + index));
                Assert.GreaterOrEqual(button.height, 70f);
                foreach (RectTransform chip in card.Find("Chips"))
                {
                    if (!chip.gameObject.activeSelf) continue;
                    var bounds = Bounds(chip);
                    Assert.LessOrEqual(bounds.xMax, Bounds(card).xMax);
                    Assert.GreaterOrEqual(bounds.xMin, Bounds(card).xMin);
                    Assert.Greater(bounds.yMin, button.yMax);
                }
                if (index % 3 != 0) Assert.Less(Bounds((RectTransform)rows[index / 3].Find("Card" + (index - 1))).xMax, Bounds(card).xMin);
            }
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

        [Test]
        public void TappingAnEquippedSlotTakesThatArtOff()
        {
            var art = EarlyGameContent.Arts.First(a => a.OwnerId == null && !Progression.IsUltimate(a));
            if (!_save.LearnedArts.Contains(art.Id)) _save.LearnedArts.Add(art.Id);
            _save.EquippedArts.Clear(); _save.EquippedArts.Add(art.Id);
            var arts = _canvas.AddComponent<MartialArtsScreen>(); arts.Build(Root);
            Invoke(arts, "Refresh");
            var slot = _canvas.transform.Find("ArtsPage/SlotBar/Slot0");
            Assert.IsNotNull(slot.Find("Unequip"));
            slot.GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(_save.EquippedArts.Contains(art.Id));
            Assert.IsTrue(_save.LearnedArts.Contains(art.Id), "unequipping keeps the art learned");
        }

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void InnerArtsUseThreeCardsPerRowWithStableHeightAndSeparateActions(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            var arts = _canvas.AddComponent<MartialArtsScreen>(); arts.Build(Root);
            Invoke(arts, "ShowPage", 1, false);
            var content = (RectTransform)_canvas.transform.Find("InnerArtPage/List/Viewport/Content");
            var layout = content.GetComponent<ReadingList>();
            Canvas.ForceUpdateCanvases(); layout.Reflow();
            var rows = Enumerable.Range(0, content.childCount).Select(i => (RectTransform)content.GetChild(i)).ToArray();
            Assert.AreEqual((InnerArts.All.Length + 2) / 3, rows.Length);
            for (int index = 0; index < InnerArts.All.Length; index++)
            {
                var card = rows[index / 3].Find("Card" + index);
                Assert.IsNotNull(card);
                var innerIcon = Bounds((RectTransform)card.Find("Icon"));
                Assert.That(innerIcon.width, Is.EqualTo(100f).Within(.1f));
                Assert.Less(innerIcon.xMax, Bounds((RectTransform)card.Find("Name")).xMin);
                Assert.Less(Bounds((RectTransform)card.Find("Effect")).yMax, innerIcon.yMin);
                var act = Bounds((RectTransform)card.Find("Act"));
                Assert.Less(act.yMax, Bounds((RectTransform)card.Find("Effect")).yMin);
                Assert.GreaterOrEqual(act.height, 60f);
                if (index % 3 != 0)
                {
                    var left = rows[index / 3].Find("Card" + (index - 1));
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

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void RecordMenuScrollsAsOneListWithLargerReadableEntries(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            _canvas.AddComponent<RecordScreen>().Build(Root, null);
            Canvas.ForceUpdateCanvases();
            var viewport = (RectTransform)Root.Find("MenuViewport/Viewport");
            var scroll = viewport.GetComponent<ScrollRect>();
            var content = scroll.content;
            content.GetComponent<ReadingList>().Reflow(true);
            Canvas.ForceUpdateCanvases();
            Assert.IsTrue(scroll.vertical);
            Assert.IsFalse(scroll.horizontal);
            Assert.Greater(content.rect.height, viewport.rect.height);
            Assert.AreEqual(6, content.GetComponentsInChildren<Button>().Length);
            Assert.AreEqual(1, Root.Find("MenuViewport").GetComponentsInChildren<ScrollRect>().Length,
                "Descriptions must scroll with the list instead of consuming drags separately");
            Separate("MenuViewport", "Workspace");
            float previousBottom = float.PositiveInfinity;
            foreach (RectTransform row in content)
            {
                var bounds = Bounds(row);
                Assert.GreaterOrEqual(bounds.height, 180f);
                Assert.Less(bounds.yMax, previousBottom);
                previousBottom = bounds.yMin;
                Assert.IsFalse(row.Find("Detail").GetComponent<Text>().resizeTextForBestFit);
                Assert.IsTrue(row.GetComponent<Button>().targetGraphic.raycastTarget);
            }
            scroll.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
            var last = Bounds((RectTransform)content.Find("Menu_ritual"));
            var visible = Bounds(viewport);
            Assert.GreaterOrEqual(last.yMin, visible.yMin - 1f);
            Assert.LessOrEqual(last.yMax, visible.yMax + 1f);
        }

        [Test]
        public void RecordSwitchClosesPreviousPageAndRemovesDuplicateStatusEntry()
        {
            bool openedCharacters = false;
            var record = _canvas.AddComponent<RecordScreen>();
            record.Build(Root, (title, lines, done) => { }, () => openedCharacters = true);
            _canvas.transform.Find("MenuViewport/Viewport/Content/Menu_story").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(_canvas.transform.Find("Workspace/StoryLogOverlay").gameObject.activeSelf);
            _canvas.transform.Find("MenuViewport/Viewport/Content/Menu_codex").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(_canvas.transform.Find("Workspace/StoryLogOverlay").gameObject.activeSelf);
            Assert.IsTrue(_canvas.transform.Find("Workspace/CodexOverlay").gameObject.activeSelf);
            Separate("MenuViewport", "Workspace");
            Assert.IsNull(_canvas.transform.Find("MenuViewport/Viewport/Content/Menu_party"));
            Assert.IsFalse(openedCharacters);
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
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void GachaResultsShowFivePerRowForSingleTenAndFiftyWithoutChangingTheSave(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            var screen = _canvas.AddComponent<GachaScreen>(); screen.Build(Root);
            long doo = _save.Doo;
            int pulls = _save.GachaPulls;
            var content = _canvas.transform.Find("Viewport/Content");
            foreach (int count in new[] { 1, 10, 50, 1 })
            {
                var results = Enumerable.Range(0, count).Select(i => new PullResult
                {
                    Item = Gacha.Pool((ItemGrade)(i % Grades.Rates.Length))[0], Duplicate = i % 2 == 0
                }).ToList();
                var reveal = (System.Collections.IEnumerator)Invoke(screen, "ShowResults", results, false);
                while (reveal.MoveNext()) { }
                Canvas.ForceUpdateCanvases();
                var layout = content.GetComponent<ReadingList>(); layout.Reflow(true);
                Assert.AreEqual(count, content.GetComponentsInChildren<RectTransform>().Count(rt => rt.name.StartsWith("Card")));
                Assert.AreEqual((count + 4) / 5, Enumerable.Range(0, content.childCount).Count(i => content.GetChild(i).gameObject.activeSelf));
                for (int i = 0; i < count; i++)
                {
                    var card = (RectTransform)content.Find("Row" + i / 5 + "/Card" + i);
                    Assert.IsTrue(card.gameObject.activeInHierarchy);
                    var resultIcon = Bounds((RectTransform)card.Find("Icon"));
                    Assert.That(resultIcon.width, Is.EqualTo(104f).Within(.1f));
                    Assert.Less(Bounds((RectTransform)card.Find("Name")).yMax, resultIcon.yMin);
                    if (i % 5 != 0)
                    {
                        var previous = (RectTransform)content.Find("Row" + i / 5 + "/Card" + (i - 1));
                        Assert.Less(Bounds(previous).xMax, Bounds(card).xMin);
                        Assert.That(Bounds(previous).yMax, Is.EqualTo(Bounds(card).yMax).Within(.1f));
                    }
                    if (i >= 5)
                    {
                        var above = (RectTransform)content.Find("Row" + (i / 5 - 1) + "/Card" + (i - 5));
                        Assert.Less(Bounds(card).yMax, Bounds(above).yMin);
                    }
                }
                float height = ((RectTransform)content).sizeDelta.y;
                for (int repeat = 0; repeat < 10; repeat++)
                {
                    Canvas.ForceUpdateCanvases(); layout.Reflow(true);
                    Assert.That(((RectTransform)content).sizeDelta.y, Is.EqualTo(height).Within(.5f));
                }
            }
            Assert.AreEqual(doo, _save.Doo); Assert.AreEqual(pulls, _save.GachaPulls);
        }

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void GradeButtonsShowOnlyTheActualPoolAndPreviewDoesNotSpendOrGrantAnything(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            var screen = _canvas.AddComponent<GachaScreen>(); screen.Build(Root);
            long doo = _save.Doo;
            int pulls = _save.GachaPulls;
            var inventory = _save.Items.Select(stack => stack.Id + ":" + stack.Count).ToArray();
            var modal = _canvas.transform.Find("GachaPool");
            Assert.IsFalse(modal.gameObject.activeSelf);
            for (int grade = 0; grade < Grades.Rates.Length; grade++)
            {
                _canvas.transform.Find("Odds/Chip" + grade).GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(modal.gameObject.activeSelf);
                var pool = Gacha.Pool((ItemGrade)grade);
                var content = modal.Find("Card/Viewport/Content");
                var names = content.GetComponentsInChildren<Text>().Where(label => label.name == "Name").Select(label => label.text).ToArray();
                CollectionAssert.AreEquivalent(pool.Select(item => item.Name).ToArray(), names);
                Assert.AreEqual((pool.Count + 4) / 5, content.childCount);
                Canvas.ForceUpdateCanvases();
                for (int rowIndex = 0; rowIndex < content.childCount; rowIndex++)
                {
                    var row = content.GetChild(rowIndex);
                    Assert.AreEqual(Math.Min(5, pool.Count - rowIndex * 5), row.childCount);
                    float previousRight = float.MinValue;
                    foreach (RectTransform cell in row)
                    {
                        var area = Bounds(cell);
                        Assert.Greater(area.xMin, previousRight);
                        previousRight = area.xMax;
                        var icon = cell.Find("Icon").GetComponent<Image>();
                        Assert.IsNotNull(icon.sprite);
                        Assert.GreaterOrEqual(icon.rectTransform.rect.width, 104f);
                        Assert.Greater(Bounds(icon.rectTransform).yMin, Bounds((RectTransform)cell.Find("Name")).yMax);
                    }
                }
                StringAssert.Contains(pool.Count + "종", modal.Find("Card/Title").GetComponent<Text>().text);
                ComfortableButton("GachaPool/Card/Close");
                Assert.Greater(Bounds((RectTransform)modal.Find("Card/Viewport")).yMin, Bounds((RectTransform)modal.Find("Card/Close")).yMax);
                modal.Find("Card/Close").GetComponent<Button>().onClick.Invoke();
                Assert.IsFalse(modal.gameObject.activeSelf);
            }
            Assert.AreEqual(doo, _save.Doo); Assert.AreEqual(pulls, _save.GachaPulls);
            CollectionAssert.AreEqual(inventory, _save.Items.Select(stack => stack.Id + ":" + stack.Count).ToArray());
            _canvas.transform.Find("Odds/Chip0").GetComponent<Button>().onClick.Invoke();
            modal.Find("Dismiss").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(modal.gameObject.activeSelf);
            _canvas.transform.Find("Odds/Chip0").GetComponent<Button>().onClick.Invoke();
            _canvas.SetActive(false);
            Assert.IsFalse(modal.gameObject.activeSelf);
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
