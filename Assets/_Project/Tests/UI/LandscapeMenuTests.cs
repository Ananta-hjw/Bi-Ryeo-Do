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
                Assert.That(icon.width, Is.EqualTo(80f).Within(.1f));
                Assert.That(icon.height, Is.EqualTo(80f).Within(.1f));
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
            recordRoot.Find("MenuViewport/Viewport/Content/Menu_tower").GetComponent<Button>().onClick.Invoke();
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
            for (int i = 0; i < 3; i++) ComfortableButton("PeopleTest/Pages/Page" + i);
            var equipmentRoot = UiFactory.Rect("EquipmentTest", Root, Vector2.zero, Vector2.one);
            equipmentRoot.gameObject.AddComponent<EquipmentScreen>().Build(equipmentRoot);
            for (int i = 0; i < 6; i++) ComfortableButton("EquipmentTest/Workspace/Character/WearSlot" + i);
            foreach (var name in new[] { "Equip", "Remove", "Details", "Enhance" }) ComfortableButton("EquipmentTest/Workspace/Inventory/Selection/" + name);
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
            foreach (var name in new[] { "MissionsMenu", "ChallengeMenu", "PartyMenu" }) ComfortableButton("Home/" + name);
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
            string[] cards = { "협객", "무공", "행낭", "기록" };
            TabRouter.Tab[] routes = { TabRouter.Tab.Companions, TabRouter.Tab.MartialArts, TabRouter.Tab.Bag, TabRouter.Tab.Record };
            for (int i = 0; i < cards.Length; i++)
            {
                var card = homeRoot.Find("Home_" + cards[i]);
                Assert.GreaterOrEqual(Bounds((RectTransform)card).height, 190f);
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
        public void BottomNavigationIncludesEquipmentAndGachaWithoutChangingExistingRouteIds(float width)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            var main = _canvas.AddComponent<MainScreen>();
            Invoke(main, "BuildTabBar", Root);
            string[] names = { "강호", "협객", "장비", "무공", "뽑기", "행낭", "기록" };
            var buttons = _canvas.transform.Find("TabBar").GetComponentsInChildren<Button>();
            CollectionAssert.AreEqual(names.Select(name => "Tab_" + name).ToArray(), buttons.Select(button => button.name).ToArray());
            var navigation = (TabRouter.Tab[])typeof(MainScreen).GetField("NavigationTabs", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            CollectionAssert.AreEquivalent(System.Enum.GetValues(typeof(TabRouter.Tab)), navigation);
            foreach (var name in names) ComfortableButton("TabBar/Tab_" + name);
            var icons = (Graphic[])typeof(MainScreen).GetField("_tabIcons", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(main);
            var labels = (Text[])typeof(MainScreen).GetField("_tabLabels", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(main);
            for (int i = 0; i < icons.Length; i++)
            {
                var icon = Bounds(icons[i].rectTransform);
                Assert.That(icon.width, Is.EqualTo(64f).Within(.1f));
                Assert.That(icon.height, Is.EqualTo(64f).Within(.1f));
                Assert.Less(icon.xMax, Bounds(labels[i].rectTransform).xMin);
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
            Assert.AreEqual(7, content.GetComponentsInChildren<Button>().Length);
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
            var last = Bounds((RectTransform)content.Find("Menu_arts"));
            var visible = Bounds(viewport);
            Assert.GreaterOrEqual(last.yMin, visible.yMin - 1f);
            Assert.LessOrEqual(last.yMax, visible.yMax + 1f);
        }

        [Test]
        public void RecordSwitchClosesThePreviousPageAndStatusUsesTheSharedCharacterRoute()
        {
            bool openedCharacters = false;
            var record = _canvas.AddComponent<RecordScreen>();
            record.Build(Root, (title, lines, done) => { }, () => openedCharacters = true);
            _canvas.transform.Find("MenuViewport/Viewport/Content/Menu_quest").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(_canvas.transform.Find("Workspace/StoryLogOverlay").gameObject.activeSelf);
            _canvas.transform.Find("MenuViewport/Viewport/Content/Menu_record").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(_canvas.transform.Find("Workspace/StoryLogOverlay").gameObject.activeSelf);
            Assert.IsTrue(_canvas.transform.Find("Workspace/CodexOverlay").gameObject.activeSelf);
            Separate("MenuViewport", "Workspace");
            _canvas.transform.Find("MenuViewport/Viewport/Content/Menu_party").GetComponent<Button>().onClick.Invoke();
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
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void GachaResultsShowThreePerRowForSingleTenAndFiftyWithoutChangingTheSave(float width)
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
                Assert.AreEqual((count + 2) / 3, Enumerable.Range(0, content.childCount).Count(i => content.GetChild(i).gameObject.activeSelf));
                for (int i = 0; i < count; i++)
                {
                    var card = (RectTransform)content.Find("Row" + i / 3 + "/Card" + i);
                    Assert.IsTrue(card.gameObject.activeInHierarchy);
                    if (i % 3 != 0)
                    {
                        var previous = (RectTransform)content.Find("Row" + i / 3 + "/Card" + (i - 1));
                        Assert.Less(Bounds(previous).xMax, Bounds(card).xMin);
                        Assert.That(Bounds(previous).yMax, Is.EqualTo(Bounds(card).yMax).Within(.1f));
                    }
                    if (i >= 3)
                    {
                        var above = (RectTransform)content.Find("Row" + (i / 3 - 1) + "/Card" + (i - 3));
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

        [Test]
        public void GradeButtonsShowOnlyTheActualPoolAndPreviewDoesNotSpendOrGrantAnything()
        {
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
                var names = Enumerable.Range(0, content.childCount).Select(i => content.GetChild(i).Find("Name").GetComponent<Text>().text).ToArray();
                CollectionAssert.AreEquivalent(pool.Select(item => UiFactory.GradedName(item)).ToArray(), names);
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
