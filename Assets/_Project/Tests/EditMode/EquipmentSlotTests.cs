using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class EquipmentSlotTests
    {
        private SaveData _save;
        private ItemData[] _accessories;
        private ItemData[] _weapons;

        [SetUp]
        public void SetUp()
        {
            _save = new SaveData();
            _accessories = EarlyGameContent.Items.Where(i => i.Kind == ItemKind.Accessory && !Gear.IsHeroOnly(i.Id)).Take(3).ToArray();
            _weapons = EarlyGameContent.Items.Where(i => i.Kind == ItemKind.Weapon && !Gear.IsHeroOnly(i.Id)).Take(2).ToArray();
            foreach (var item in _accessories.Concat(_weapons)) Inventory.Add(_save, item.Id, 1);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ReplacesOnlySelectedAccessory(int slot)
        {
            string error;
            Inventory.Equip(_save, _accessories[0].Id, out error);
            Inventory.Equip(_save, _accessories[1].Id, out error);
            Assert.IsTrue(Inventory.EquipInSlot(_save, Inventory.Hero, ItemKind.Accessory, slot, _accessories[2].Id, out error), error);
            Assert.AreEqual(_accessories[2].Id, Inventory.InSlot(_save, Inventory.Hero, ItemKind.Accessory, slot).Id);
            Assert.AreEqual(_accessories[1 - slot].Id, Inventory.InSlot(_save, Inventory.Hero, ItemKind.Accessory, 1 - slot).Id);
            Assert.AreEqual(2, _save.EquippedGear.Count);
        }

        [Test]
        public void UnavailableCopyRestoresOriginalLoadoutAndOrder()
        {
            string error;
            Inventory.Equip(_save, _accessories[0].Id, out error);
            Inventory.Equip(_save, _accessories[1].Id, out error);
            Inventory.EquipFor(_save, "hyoryong", _accessories[2].Id, out error);
            var original = _save.EquippedGear.ToArray();
            Assert.IsFalse(Inventory.EquipInSlot(_save, Inventory.Hero, ItemKind.Accessory, 0, _accessories[2].Id, out error));
            CollectionAssert.AreEqual(original, _save.EquippedGear);
            CollectionAssert.AreEqual(new[] { _accessories[2].Id }, Inventory.GearOf(_save, "hyoryong"));
        }

        [Test]
        public void UnownedReplacementRestoresExistingWeapon()
        {
            string error;
            Inventory.Equip(_save, _weapons[0].Id, out error);
            Inventory.Remove(_save, _weapons[1].Id, 1);
            Assert.IsFalse(Inventory.EquipInSlot(_save, Inventory.Hero, ItemKind.Weapon, 0, _weapons[1].Id, out error));
            Assert.AreEqual(_weapons[0].Id, Inventory.InSlot(_save, Inventory.Hero, ItemKind.Weapon).Id);
        }

        [Test]
        public void DoesNotDuplicateAccessoryAlreadyInOtherSlot()
        {
            string error;
            Inventory.Equip(_save, _accessories[0].Id, out error);
            Inventory.Equip(_save, _accessories[1].Id, out error);
            var original = _save.EquippedGear.ToArray();
            Assert.IsFalse(Inventory.EquipInSlot(_save, Inventory.Hero, ItemKind.Accessory, 1, _accessories[0].Id, out error));
            CollectionAssert.AreEqual(original, _save.EquippedGear);
        }

        [Test]
        public void CompanionWeaponReplacementPreservesOtherGearAndHero()
        {
            string error;
            Inventory.EquipFor(_save, "hyoryong", _weapons[0].Id, out error);
            Inventory.EquipFor(_save, "hyoryong", _accessories[0].Id, out error);
            Assert.IsTrue(Inventory.EquipInSlot(_save, "hyoryong", ItemKind.Weapon, 0, _weapons[1].Id, out error), error);
            CollectionAssert.AreEqual(new[] { _weapons[1].Id, _accessories[0].Id }, Inventory.GearOf(_save, "hyoryong"));
            Assert.IsEmpty(_save.EquippedGear);
        }

        [TestCase(ItemKind.Weapon, 0)]
        [TestCase(ItemKind.Accessory, -1)]
        [TestCase(ItemKind.Accessory, 2)]
        public void RejectsWrongKindOrInvalidSlot(ItemKind kind, int slot)
        {
            string error;
            Inventory.Equip(_save, _weapons[0].Id, out error);
            var original = _save.EquippedGear.ToArray();
            Assert.IsFalse(Inventory.EquipInSlot(_save, Inventory.Hero, kind, slot, _accessories[0].Id, out error));
            CollectionAssert.AreEqual(original, _save.EquippedGear);
        }

        [Test]
        public void HeroOnlyRestrictionLeavesCompanionWeaponIntact()
        {
            var heroOnly = EarlyGameContent.Items.First(i => i.Kind == ItemKind.Weapon && Gear.IsHeroOnly(i.Id));
            Inventory.Add(_save, heroOnly.Id, 1);
            string error;
            Inventory.EquipFor(_save, "hyoryong", _weapons[0].Id, out error);
            Assert.IsFalse(Inventory.EquipInSlot(_save, "hyoryong", ItemKind.Weapon, 0, heroOnly.Id, out error));
            Assert.AreEqual(_weapons[0].Id, Inventory.InSlot(_save, "hyoryong", ItemKind.Weapon).Id);
        }

        [Test]
        public void SelectingCurrentItemIsIdempotentAndEmptySlotCanBeEquipped()
        {
            string error;
            Assert.IsNull(Inventory.InSlot(_save, Inventory.Hero, ItemKind.Weapon));
            Assert.IsTrue(Inventory.EquipInSlot(_save, Inventory.Hero, ItemKind.Weapon, 0, _weapons[0].Id, out error));
            Assert.IsTrue(Inventory.EquipInSlot(_save, Inventory.Hero, ItemKind.Weapon, 0, _weapons[0].Id, out error));
            CollectionAssert.AreEqual(new[] { _weapons[0].Id }, _save.EquippedGear);
        }
    }
}
