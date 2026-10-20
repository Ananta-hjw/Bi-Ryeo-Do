#if UNITY_EDITOR
using BiRyeoDo.Core;
using NUnit.Framework;
using UnityEngine;

namespace BiRyeoDo.Tests.UI
{
    public class GearSaveTests
    {
        [Test]
        public void LegacyJsonWithoutNewFieldsMigratesWithoutResettingInventory()
        {
            var restored = JsonUtility.FromJson<SaveData>("{\"Version\":2,\"Items\":[{\"Id\":\"iron_sword\",\"Count\":1}],\"EquippedGear\":[\"iron_sword\"],\"GearLevels\":[{\"Id\":\"iron_sword\",\"Level\":10}]}");
            Gear.EnsureProgression(restored);
            Assert.AreEqual(5, Gear.Stars(restored, "iron_sword"));
            Assert.AreEqual(30, Gear.SlotLevel(restored, Inventory.Hero, 0));
            Assert.AreEqual(6, Inventory.Count(restored, "iron_sword"));
            Gear.EnsureProgression(restored);
            Assert.AreEqual(6, Inventory.Count(restored, "iron_sword"));
        }

        [Test]
        public void SlotLevelsAndAccessoryAssignmentsSurviveUnityJsonRoundTrip()
        {
            var save = new SaveData { Doo = 100000 };
            Inventory.Add(save, Gear.StoneId, 100);
            Inventory.Add(save, "mukryonghwan", 2);
            Inventory.Add(save, "claw_necklace", 1);
            string error;
            Inventory.Equip(save, "mukryonghwan", out error);
            Inventory.Equip(save, "claw_necklace", out error);
            Gear.Breakthrough(save, "mukryonghwan", out error);
            Gear.EnhanceSlot(save, Inventory.Hero, 4, out error);
            Gear.EnhanceSlot(save, Inventory.Hero, 5, out error);
            Gear.EnhanceSlot(save, Inventory.Hero, 5, out error);
            Inventory.Unequip(save, "mukryonghwan");
            var restored = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save));
            Gear.EnsureProgression(restored);
            Assert.IsTrue(restored.GearProgressionMigrated);
            Assert.AreEqual(1, Gear.SlotLevel(restored, Inventory.Hero, 4));
            Assert.AreEqual(2, Gear.SlotLevel(restored, Inventory.Hero, 5));
            Assert.IsNull(Gear.ItemInSlot(restored, Inventory.Hero, 4));
            Assert.AreEqual("claw_necklace", Gear.ItemInSlot(restored, Inventory.Hero, 5).Id);
            Assert.AreEqual(1, Gear.Stars(restored, "mukryonghwan"));
            Assert.AreEqual(save.Doo, restored.Doo);
            Assert.AreEqual(Inventory.Count(save, Gear.StoneId), Inventory.Count(restored, Gear.StoneId));
        }
    }
}
#endif
