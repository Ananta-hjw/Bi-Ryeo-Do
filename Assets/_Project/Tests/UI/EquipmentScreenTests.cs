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
        public void NameDropdownChangesTheViewedPersonWhilePreservingTheAccessorySlot(float width)
        {
            _canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 960f);
            Assert.IsNull(Root.Find("People"));
            var companion = EarlyGameContent.Companions.First();
            var gear = _save.EquippedGear.ToArray();
            var inventory = _save.Items.Select(stack => stack.Id + ":" + stack.Count).ToArray();
            Click("Workspace/Character/WearSlot5");
            var header = (RectTransform)Root.Find("Workspace/Character/ChangeCharacter");
            Assert.Greater(header.rect.height, 64f);
            Assert.IsNotNull(header.Find("Arrow"));
            Assert.Less(Bounds(header).xMax, Bounds((RectTransform)Root.Find("Workspace/Character/Sets")).xMin);
            Click("Workspace/Character/ChangeCharacter");
            var picker = Root.Find("CharacterPicker");
            var options = picker.GetComponentsInChildren<Button>().Where(button => button.name.StartsWith("Person_")).ToArray();
            CollectionAssert.AreEquivalent(new[] { "Person_" + Inventory.Hero }.Concat(EarlyGameContent.Companions.Where(c => c.JoinStage <= _save.HighestUnlockedStage).Select(c => "Person_" + c.Id)), options.Select(button => button.name));
            Click("CharacterPicker/Card/Close");
            StringAssert.StartsWith("비류연", Root.Find("Workspace/Character/ChangeCharacter/Name").GetComponent<Text>().text);
            Click("Workspace/Character/ChangeCharacter");
            picker.GetComponentsInChildren<Button>().Single(button => button.name == "Person_" + companion.Id).onClick.Invoke();
            Canvas.ForceUpdateCanvases();
            Assert.IsFalse(picker.gameObject.activeSelf);
            StringAssert.StartsWith(companion.Name, Root.Find("Workspace/Character/ChangeCharacter/Name").GetComponent<Text>().text);
            Assert.IsNotNull(Root.Find("Workspace/Character/WearSlot5/Selected"));
            Assert.IsTrue(Content.GetComponentsInChildren<Button>().All(button => EarlyGameContent.FindItem(button.name.Substring("Item_".Length)).Kind == ItemKind.Accessory));
            Assert.IsFalse(Content.GetComponentsInChildren<Button>().Any(button => Gear.IsHeroOnly(button.name.Substring("Item_".Length))));
            CollectionAssert.AreEqual(gear, _save.EquippedGear);
            CollectionAssert.AreEqual(inventory, _save.Items.Select(stack => stack.Id + ":" + stack.Count).ToArray());
            Click("Workspace/Character/ChangeCharacter");
            _canvas.SetActive(false);
            Assert.IsFalse(picker.gameObject.activeSelf);
        }

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
                Assert.Greater(Bounds((RectTransform)Root.Find("Workspace/Inventory/Selection/" + name)).height, 64f);
            var row = Content.Find("Row0");
            Assert.AreEqual(7, row.GetComponentsInChildren<Button>().Length);
            var cards = row.GetComponentsInChildren<Button>().Select(button => (RectTransform)button.transform).ToArray();
            foreach (var card in cards)
            {
                var icon = Bounds((RectTransform)card.Find("Icon"));
                Assert.That(icon.width, Is.EqualTo(88f).Within(.1f));
                Assert.That(icon.height, Is.EqualTo(88f).Within(.1f));
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

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void EverySlotFiltersItsKindAndShowsExactlyOneSelectionOnEachSide(float width)
        {
            _canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 960f);
            foreach (var item in EarlyGameContent.Items.Where(item => item.IsGear)) Inventory.Add(_save, item.Id, 1);
            var originalGear = _save.EquippedGear.ToArray();
            for (int slot = 0; slot < 6; slot++)
            {
                Click("Workspace/Character/WearSlot" + slot);
                var cards = Content.GetComponentsInChildren<Button>();
                Assert.IsNotEmpty(cards);
                CollectionAssert.AreEquivalent(EarlyGameContent.Items.Where(item => item.IsGear && item.Kind == Gear.KindForSlot(slot)).Select(item => "Item_" + item.Id), cards.Select(card => card.name));
                Assert.AreEqual(1, Root.Find("Workspace/Character").GetComponentsInChildren<Text>().Count(text => text.text == "선택 부위"));
                Assert.IsNotNull(Root.Find("Workspace/Character/WearSlot" + slot + "/Selected"));
                for (int rowIndex = 0; rowIndex < Content.childCount; rowIndex++)
                    Assert.AreEqual(System.Math.Min(7, cards.Length - rowIndex * 7), Content.GetChild(rowIndex).GetComponentsInChildren<Button>().Length);
                var target = cards.Last();
                string targetName = target.name;
                target.onClick.Invoke(); Canvas.ForceUpdateCanvases();
                var selected = Content.GetComponentsInChildren<Button>().Single(card => card.transform.Find("Selected") != null);
                Assert.AreEqual(targetName, selected.name);
                Assert.IsNotNull(Root.Find("Workspace/Character/WearSlot" + slot + "/Selected"));
                var effects = Root.Find("Workspace/Inventory/Selection").GetComponentsInChildren<Text>().Single(text => text.name == "Effects");
                StringAssert.Contains("세트:", effects.text);
                var icon = Bounds((RectTransform)selected.transform.Find("Icon"));
                var cell = Bounds((RectTransform)selected.transform);
                Assert.GreaterOrEqual(icon.xMin, cell.xMin);
                Assert.LessOrEqual(icon.xMax, cell.xMax);
                Assert.Less(Bounds((RectTransform)selected.transform.Find("Name")).yMax, icon.yMin);
                Assert.Less(Bounds((RectTransform)selected.transform.Find("Icon")).yMax, Bounds((RectTransform)selected.transform.Find("Selected")).yMin);
            }
            CollectionAssert.AreEqual(originalGear, _save.EquippedGear);
        }

        [Test]
        public void ArmorSelectionTargetsItsActualSlotAndAccessoryChoiceKeepsTheChosenSlot()
        {
            var boots = EarlyGameContent.Items.First(item => item.Kind == ItemKind.Boots && item.IsGear && !Gear.IsHeroOnly(item.Id));
            Inventory.Add(_save, boots.Id, 1);
            Click("Workspace/Inventory/Group3");
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
