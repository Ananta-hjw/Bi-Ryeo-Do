#if UNITY_EDITOR
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
    public class EquipmentScreenTests
    {
        private GameObject _manager, _canvas;
        private GameManager _previous;
        private SaveData _save;
        private EquipmentScreen _screen;
        private static readonly FieldInfo Instance = typeof(GameManager).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        private Transform Root { get { return _canvas.transform; } }

        [SetUp]
        public void SetUp()
        {
            _previous = GameManager.Instance;
            _manager = new GameObject("EquipmentTestManager"); _manager.SetActive(false);
            var manager = _manager.AddComponent<GameManager>();
            _save = new SaveData { ClearedStage = 29, Doo = 100000 };
            Inventory.EnsureStartKit(_save);
            typeof(GameManager).GetField("<Save>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, _save);
            Instance.SetValue(null, manager);
            _canvas = new GameObject("EquipmentTest", typeof(RectTransform), typeof(Canvas));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            _canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 960f);
            _screen = _canvas.AddComponent<EquipmentScreen>();
            _screen.Build(_canvas.GetComponent<RectTransform>());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvas); Object.DestroyImmediate(_manager); Instance.SetValue(null, _previous);
        }

        private void Click(string path)
        {
            var button = Root.Find(path).GetComponent<Button>();
            Assert.IsTrue(button.interactable, path); button.onClick.Invoke(); Canvas.ForceUpdateCanvases();
        }

        private static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private bool Enabled(string name) { return Root.Find("Workspace/Inventory/Selection/" + name).GetComponent<Button>().interactable; }
        private Transform Content { get { return Root.Find("Workspace/Inventory/Viewport/Content"); } }

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void FigureSlotsInventoryAndActionsStaySeparateAndPreviewDoesNotMutateTheSave(float width)
        {
            _canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 960f);
            var current = Gear.ItemInSlot(_save, Inventory.Hero, 0);
            var alternatives = EarlyGameContent.Items.Where(item => item.IsGear && item.Kind == ItemKind.Weapon && item.Id != current.Id).Take(8).ToList();
            foreach (var item in alternatives) Inventory.Add(_save, item.Id, 1);
            long doo = _save.Doo;
            var inventory = _save.Items.Select(stack => stack.Id + ":" + stack.Count).ToArray();
            var worn = _save.EquippedGear.ToArray();
            _screen.Refresh(); Canvas.ForceUpdateCanvases();
            Assert.Less(Bounds((RectTransform)Root.Find("Workspace/Character")).xMax, Bounds((RectTransform)Root.Find("Workspace/Inventory")).xMin);
            var viewport = Bounds((RectTransform)Root.Find("Workspace/Inventory/Viewport"));
            var actions = Bounds((RectTransform)Root.Find("Workspace/Inventory/Selection"));
            Assert.Greater(viewport.yMin, actions.yMax);
            for (int i = 0; i < 6; i++) Assert.Greater(Bounds((RectTransform)Root.Find("Workspace/Character/WearSlot" + i)).height, 100f);
            foreach (var name in new[] { "Equip", "Remove", "Details", "Enhance" })
                Assert.Greater(Bounds((RectTransform)Root.Find("Workspace/Inventory/Selection/" + name)).height, 94f);
            var row = Content.Find("Row0");
            Assert.AreEqual(3, row.GetComponentsInChildren<Button>().Length);
            var cards = row.GetComponentsInChildren<Button>().Select(button => (RectTransform)button.transform).ToArray();
            foreach (var card in cards)
            {
                var icon = Bounds((RectTransform)card.Find("Icon"));
                Assert.That(icon.width, Is.EqualTo(160f).Within(.1f));
                Assert.That(icon.height, Is.EqualTo(160f).Within(.1f));
                Assert.Less(Bounds((RectTransform)card.Find("Name")).yMax, icon.yMin);
            }
            Assert.Less(Bounds(cards[0]).xMax, Bounds(cards[1]).xMin); Assert.Less(Bounds(cards[1]).xMax, Bounds(cards[2]).xMin);
            Assert.IsFalse(Enabled("Equip")); Assert.IsTrue(Enabled("Remove"));
            Content.GetComponentsInChildren<Button>().First(button => button.name == "Item_" + alternatives[0].Id).onClick.Invoke();
            Assert.IsTrue(Enabled("Equip")); Assert.IsFalse(Enabled("Remove")); Assert.IsFalse(Enabled("Enhance"));
            Click("Workspace/Inventory/Selection/Details");
            Assert.IsTrue(Root.Find("EquipmentInfo").gameObject.activeSelf);
            Click("EquipmentInfo/Card/Close");
            Click("Workspace/Character/Sets");
            StringAssert.Contains("세트", Root.Find("EquipmentInfo/Card/Title").GetComponent<Text>().text);
            Click("EquipmentInfo/Card/Close");
            Assert.AreEqual(doo, _save.Doo); CollectionAssert.AreEqual(worn, _save.EquippedGear);
            CollectionAssert.AreEqual(inventory, _save.Items.Select(stack => stack.Id + ":" + stack.Count).ToArray());
            Assert.AreEqual(ArtLibrary.Chibi(Inventory.Hero) ?? ArtLibrary.DefaultChibi(Inventory.Hero), Root.Find("Workspace/Character/BattleFigure").GetComponent<Image>().sprite);
        }

        [Test]
        public void CompanionSelectionExcludesHeroOnlyGearAndReflectsInMemoryEquipmentChanges()
        {
            Inventory.Add(_save, Ritual.RewardId, 1); _screen.Refresh();
            Assert.IsTrue(Content.GetComponentsInChildren<Button>().Any(button => button.name == "Item_" + Ritual.RewardId));
            var companion = EarlyGameContent.Companions.First();
            _screen.SelectOwner(companion.Id);
            Assert.IsFalse(Content.GetComponentsInChildren<Button>().Any(button => button.name == "Item_" + Ritual.RewardId));
            var weapon = EarlyGameContent.Items.First(item => item.IsGear && item.Kind == ItemKind.Weapon && !Gear.IsHeroOnly(item.Id));
            Inventory.Add(_save, weapon.Id, 1);
            string error;
            Assert.IsTrue(Inventory.EquipInSlot(_save, companion.Id, ItemKind.Weapon, 0, weapon.Id, out error), error);
            _screen.SelectOwner(companion.Id);
            Assert.IsTrue(Enabled("Remove")); Assert.IsFalse(Enabled("Equip"));
            Assert.AreEqual(ArtLibrary.Chibi(companion.Id) ?? ArtLibrary.DefaultChibi(companion.Id), Root.Find("Workspace/Character/BattleFigure").GetComponent<Image>().sprite);
            Inventory.UnequipFor(_save, companion.Id, weapon.Id); _screen.Refresh();
            Assert.IsFalse(Enabled("Remove")); Assert.IsTrue(Enabled("Equip"));
            Click("Workspace/Inventory/Selection/Details");
            _canvas.SetActive(false);
            Assert.IsFalse(Root.Find("EquipmentInfo").gameObject.activeSelf);
        }

        [Test]
        public void ArmorSelectionTargetsItsActualSlotAndAccessoryChoiceKeepsTheChosenSlot()
        {
            var boots = EarlyGameContent.Items.First(item => item.Kind == ItemKind.Boots && item.IsGear && !Gear.IsHeroOnly(item.Id));
            Inventory.Add(_save, boots.Id, 1);
            Click("Workspace/Inventory/Group1");
            Content.GetComponentsInChildren<Button>().First(button => button.name == "Item_" + boots.Id).onClick.Invoke();
            StringAssert.StartsWith("신발", Root.Find("Workspace/Inventory/Selection/State").GetComponent<Text>().text);
            Click("Workspace/Character/WearSlot5");
            var accessory = EarlyGameContent.Items.First(item => item.Kind == ItemKind.Accessory && item.IsGear && !Gear.IsHeroOnly(item.Id));
            Inventory.Add(_save, accessory.Id, 1); _screen.Refresh();
            Content.GetComponentsInChildren<Button>().First(button => button.name == "Item_" + accessory.Id).onClick.Invoke();
            StringAssert.StartsWith("장신구 2", Root.Find("Workspace/Inventory/Selection/State").GetComponent<Text>().text);
        }
    }
}
#endif
