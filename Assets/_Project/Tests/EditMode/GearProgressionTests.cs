using System;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class GearProgressionTests
    {
        private static SaveData Funded()
        {
            var save = new SaveData { Doo = 100000000 };
            Inventory.Add(save, Gear.StoneId, 100000);
            return save;
        }

        private static void Equip(SaveData save, string id, string owner = Inventory.Hero)
        {
            Inventory.Add(save, id, 1);
            string error;
            Assert.IsTrue(Inventory.EquipFor(save, owner, id, out error), error);
        }

        [TestCase(0, 1f)]
        [TestCase(1, 1.03f)]
        [TestCase(20, 1.6f)]
        [TestCase(21, 1.64f)]
        [TestCase(30, 2f)]
        [TestCase(31, 2.05f)]
        [TestCase(50, 3f)]
        [TestCase(51, 3.07f)]
        [TestCase(70, 4.4f)]
        [TestCase(71, 4.5f)]
        [TestCase(90, 6.4f)]
        [TestCase(91, 6.55f)]
        [TestCase(100, 7.9f)]
        [TestCase(101, 7.9f)]
        [TestCase(-1, 1f)]
        public void EnhancementUsesRequestedPiecewiseBaseBonus(int level, float expected)
        {
            Assert.AreEqual(expected, Gear.Multiplier(level), .0001f);
        }

        [Test]
        public void FiveBreakthroughsSpendExactlyFiveSparesAndUnlockRequestedCaps()
        {
            var save = Funded();
            Equip(save, "iron_sword");
            Inventory.Add(save, "iron_sword", 6);
            var caps = new[] { 10, 20, 30, 50, 70, 100 };
            var bonuses = new[] { 0f, .05f, .10f, .15f, .20f, .30f };
            string error;
            for (int star = 0; star <= 5; star++)
            {
                Assert.AreEqual(star, Gear.Stars(save, "iron_sword"));
                Assert.AreEqual(caps[star], Gear.Cap(save, "iron_sword"));
                Assert.AreEqual(bonuses[star], Gear.StarBonus(star));
                Assert.AreEqual(7 - star, Inventory.Count(save, "iron_sword"));
                if (star < 5) Assert.IsTrue(Gear.Breakthrough(save, "iron_sword", out error), error);
            }
            Assert.IsFalse(Gear.Breakthrough(save, "iron_sword", out error));
            Assert.AreEqual(2, Inventory.Count(save, "iron_sword"));
            Assert.AreEqual(8.2f, Gear.Multiplier(100, 5), .0001f);
        }

        [Test]
        public void BreakthroughNeverConsumesAnotherWearersOnlyCopy()
        {
            var save = Funded();
            Equip(save, "iron_sword");
            Equip(save, "iron_sword", "hyo_ryong");
            string error;
            Assert.IsFalse(Gear.Breakthrough(save, "iron_sword", out error));
            Inventory.Add(save, "iron_sword", 1);
            Assert.IsTrue(Gear.Breakthrough(save, "iron_sword", out error));
            Assert.AreEqual(2, Inventory.Worn(save, "iron_sword"));
            Assert.AreEqual(2, Inventory.Count(save, "iron_sword"));
        }

        [Test]
        public void UpgradeToEachCapThenBreakthroughUntilHundred()
        {
            var save = Funded();
            Equip(save, "iron_sword");
            Inventory.Add(save, "iron_sword", 5);
            string error;
            long totalDoo = 0;
            int totalStones = 0;
            for (int star = 0; star <= 5; star++)
            {
                int cap = Gear.Cap(save, "iron_sword");
                while (Gear.SlotLevel(save, Inventory.Hero, 0) < cap)
                {
                    int next = Gear.SlotLevel(save, Inventory.Hero, 0) + 1;
                    totalDoo += Gear.DooCost(next);
                    totalStones += Gear.StoneCost(next);
                    Assert.IsTrue(Gear.EnhanceSlot(save, Inventory.Hero, 0, out error), error);
                }
                long doo = save.Doo;
                int stones = Inventory.Count(save, Gear.StoneId);
                Assert.IsFalse(Gear.EnhanceSlot(save, Inventory.Hero, 0, out error));
                Assert.AreEqual(doo, save.Doo);
                Assert.AreEqual(stones, Inventory.Count(save, Gear.StoneId));
                if (star < 5) Assert.IsTrue(Gear.Breakthrough(save, "iron_sword", out error), error);
            }
            Assert.AreEqual(100, Gear.LevelFor(save, Inventory.Hero, "iron_sword"));
            Assert.AreEqual(550, totalStones);
            Assert.AreEqual(787700, totalDoo);
            Assert.AreEqual(100000000 - totalDoo, save.Doo);
            Assert.AreEqual(100000 - totalStones, Inventory.Count(save, Gear.StoneId));
            var actual = Inventory.ApplyGear(new CombatStats { MaxHp = 1000, MaxQi = 100, Attack = 1000, Defense = 100, Speed = 100 }, save.EquippedGear, Gear.LevelsFor(save, Inventory.Hero));
            Assert.AreEqual(1328, actual.Attack, "4% base weapon x 8.2 contributes 32.8%");
        }

        [Test]
        public void SwitchingToLowerCapPreservesStoredLevelAndReequippingRestoresIt()
        {
            var save = Funded();
            Equip(save, "iron_sword");
            Inventory.Add(save, "iron_sword", 5);
            string error;
            for (int i = 0; i < 5; i++) Gear.Breakthrough(save, "iron_sword", out error);
            for (int i = 0; i < 80; i++) Gear.EnhanceSlot(save, Inventory.Hero, 0, out error);
            Equip(save, "oak_club");
            Assert.AreEqual(80, Gear.SlotLevel(save, Inventory.Hero, 0));
            Assert.AreEqual(10, Gear.LevelFor(save, Inventory.Hero, "oak_club"));
            Assert.IsFalse(Gear.EnhanceSlot(save, Inventory.Hero, 0, out error));
            Assert.IsTrue(Inventory.Equip(save, "iron_sword", out error));
            Assert.AreEqual(80, Gear.LevelFor(save, Inventory.Hero, "iron_sword"));
            Assert.IsTrue(Gear.EnhanceSlot(save, Inventory.Hero, 0, out error));
        }

        [Test]
        public void AccessorySlotsStayIndependentWhenFirstIsUnequippedAndReplaced()
        {
            var save = Funded();
            Equip(save, "mukryonghwan");
            Equip(save, "claw_necklace");
            string error;
            for (int i = 0; i < 3; i++) Gear.EnhanceSlot(save, Inventory.Hero, 4, out error);
            for (int i = 0; i < 7; i++) Gear.EnhanceSlot(save, Inventory.Hero, 5, out error);
            Inventory.Unequip(save, "mukryonghwan");
            Assert.IsNull(Gear.ItemInSlot(save, Inventory.Hero, 4));
            Assert.AreEqual("claw_necklace", Gear.ItemInSlot(save, Inventory.Hero, 5).Id);
            Assert.AreEqual(7, Gear.LevelFor(save, Inventory.Hero, "claw_necklace"));
            Inventory.Add(save, "mukrin_talisman", 1);
            Assert.IsTrue(Inventory.EquipInSlot(save, Inventory.Hero, ItemKind.Accessory, 0, "mukrin_talisman", out error), error);
            Assert.AreEqual(3, Gear.LevelFor(save, Inventory.Hero, "mukrin_talisman"));
            Assert.AreEqual(7, Gear.LevelFor(save, Inventory.Hero, "claw_necklace"));
        }

        [Test]
        public void FailedReplacementRestoresAssignmentsAndLevels()
        {
            var save = Funded();
            Equip(save, "claw_necklace");
            Equip(save, "mukryonghwan");
            Equip(save, "iron_bracer", "hyo_ryong");
            string error;
            Gear.EnhanceSlot(save, Inventory.Hero, 4, out error);
            Assert.IsFalse(Inventory.EquipInSlot(save, Inventory.Hero, ItemKind.Accessory, 0, "iron_bracer", out error));
            Assert.AreEqual("claw_necklace", Gear.ItemInSlot(save, Inventory.Hero, 4).Id);
            Assert.AreEqual("mukryonghwan", Gear.ItemInSlot(save, Inventory.Hero, 5).Id);
            Assert.AreEqual(1, Gear.SlotLevel(save, Inventory.Hero, 4));
        }

        [TestCase(0, 1000)]
        [TestCase(1, 0)]
        public void InsufficientResourceDoesNotSpendTheOther(int stones, long doo)
        {
            var save = new SaveData { Doo = doo };
            Equip(save, "iron_sword");
            Inventory.Add(save, Gear.StoneId, stones);
            string error;
            Assert.IsFalse(Gear.EnhanceSlot(save, Inventory.Hero, 0, out error));
            Assert.AreEqual(stones, Inventory.Count(save, Gear.StoneId));
            Assert.AreEqual(doo, save.Doo);
            Assert.AreEqual(0, Gear.SlotLevel(save, Inventory.Hero, 0));
        }

        [Test]
        public void LegacySaveConversionIsIdempotentAndRefundsExcessDuplicates()
        {
            var save = new SaveData();
            Inventory.Add(save, "iron_sword", 1);
            save.EquippedGear.Add("iron_sword");
            save.GearLevels.Add(new GearLevel { Id = "iron_sword", Level = 10 });
            save.GearSlots = null;
            save.GearBreakthroughs = null;
            save.CompanionGear = null;
            Gear.EnsureProgression(save);
            Assert.AreEqual(5, Gear.Stars(save, "iron_sword"));
            Assert.AreEqual(30, Gear.SlotLevel(save, Inventory.Hero, 0));
            Assert.AreEqual(6, Inventory.Count(save, "iron_sword"));
            Assert.IsEmpty(save.GearLevels);
            Gear.EnsureProgression(save);
            Assert.AreEqual(6, Inventory.Count(save, "iron_sword"));
            Assert.AreEqual(1, save.GearBreakthroughs.Count);
        }

        [Test]
        public void LegacyCompanionEnhancementMigratesToTheWearersSlot()
        {
            var save = new SaveData();
            Inventory.Add(save, "iron_sword", 1);
            Inventory.GearOf(save, "hyo_ryong").Add("iron_sword");
            save.GearLevels.Add(new GearLevel { Id = "iron_sword", Level = 3 });
            Gear.EnsureProgression(save);
            Assert.AreEqual(10, Gear.SlotLevel(save, "hyo_ryong", 0));
            Assert.AreEqual(0, Gear.SlotLevel(save, Inventory.Hero, 0));
            Assert.AreEqual(3, Gear.Stars(save, "iron_sword"));
        }

        [Test]
        public void DismantlingReturnsPolishAndNeverErasesSlotEnhancement()
        {
            var save = Funded();
            Equip(save, "iron_sword");
            string error;
            Gear.EnhanceSlot(save, Inventory.Hero, 0, out error);
            Inventory.Unequip(save, "iron_sword");
            int before = Inventory.Count(save, Gear.StoneId);
            Assert.IsNotNull(Salvage.Dismantle(save, "iron_sword", new SeededRandom(4), out error), error);
            Assert.AreEqual(before + 1, Inventory.Count(save, Gear.StoneId));
            Assert.AreEqual(1, Gear.SlotLevel(save, Inventory.Hero, 0));
        }

        [Test]
        public void DailyPolishCanOnlyBeClaimedOnceAndStarterIncludesPolish()
        {
            var clock = Daily.Clock;
            try
            {
                Daily.Clock = () => new DateTime(2026, 10, 20);
                var save = new SaveData();
                Inventory.EnsureStartKit(save);
                Assert.AreEqual(Gear.StarterPolish, Inventory.Count(save, Gear.StoneId));
                Inventory.EnsureStartKit(save);
                Assert.AreEqual(Gear.StarterPolish, Inventory.Count(save, Gear.StoneId));
                var mission = Daily.Missions(save)[0];
                Daily.Record(save, mission.Event, mission.Target);
                Daily.Claim(save, 0);
                Assert.AreEqual(Gear.StarterPolish + Gear.DailyPolish, Inventory.Count(save, Gear.StoneId));
                Daily.Claim(save, 0);
                Assert.AreEqual(Gear.StarterPolish + Gear.DailyPolish, Inventory.Count(save, Gear.StoneId));
                Assert.AreEqual(0, EarlyGameContent.FindItem(Gear.StoneId).SellValue);
            }
            finally { Daily.Clock = clock; }
        }

        [Test]
        public void NewAndExistingTranscendentRewardAlwaysHasFiveStars()
        {
            var fresh = new SaveData();
            Inventory.Add(fresh, Ritual.RewardId, 1);
            Assert.AreEqual(5, Gear.Stars(fresh, Ritual.RewardId));
            Assert.AreEqual(100, Gear.Cap(fresh, Ritual.RewardId));
            var old = new SaveData();
            old.Items.Add(new ItemStack(Ritual.RewardId, 1));
            Gear.EnsureProgression(old);
            Assert.AreEqual(5, Gear.Stars(old, Ritual.RewardId));
            string error;
            Assert.IsFalse(Gear.Breakthrough(old, Ritual.RewardId, out error));
        }
    }
}
