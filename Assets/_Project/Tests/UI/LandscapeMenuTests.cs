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
            Root.sizeDelta = new Vector2(1920f, 984f);
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

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void BagKeepsThreeColumnsAndInspectorClearOfTheListWhenFiltersChange(float width)
        {
            Root.sizeDelta = new Vector2(width, 984f);
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
            Root.sizeDelta = new Vector2(width, 984f);
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

        [TestCase(1440f)]
        [TestCase(2400f)]
        public void PrepRebuildKeepsLargeFormationTilesSeparateFromEnemyInformation(float width)
        {
            Root.sizeDelta = new Vector2(width, 984f);
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
            Root.sizeDelta = new Vector2(width, 984f);
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
