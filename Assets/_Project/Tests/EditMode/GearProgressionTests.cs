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
            foreach (var material in Gear.Materials) Inventory.Add(save, material.Id, 100000);
            return save;
        }

        private static void Equip(SaveData save, string id, string owner = Inventory.Hero)
        {
            Inventory.Add(save, id, 1);
            string error;
            Assert.IsTrue(Inventory.EquipFor(save, owner, id, out error), error);
        }

        [TestCase(1, "하급 영석")]
        [TestCase(20, "하급 영석")]
        [TestCase(21, "중급 영석")]
        [TestCase(40, "중급 영석")]
        [TestCase(41, "고급 영석")]
        [TestCase(60, "고급 영석")]
        [TestCase(61, "상급 현철")]
        [TestCase(80, "상급 현철")]
        [TestCase(81, "만년 현철")]
        [TestCase(90, "만년 현철")]
        [TestCase(91, "신물:천외비석")]
        [TestCase(100, "신물:천외비석")]
        public void RequestedMaterialsAreUsedAtEveryBoundary(int level, string name)
        {
            Assert.AreEqual(name, Gear.MaterialForLevel(level).Name);
            Assert.AreSame(Gear.MaterialForLevel(level), EarlyGameContent.FindItem(Gear.MaterialForLevel(level).Id));
        }

        private static string Weapon(ItemGrade grade)
        {
            return EarlyGameContent.Items.First(i => i.Kind == ItemKind.Weapon && i.Grade == grade).Id;
        }

        [TestCase(ItemGrade.Legendary, 24000)]
        [TestCase(ItemGrade.Mythic, 54000)]
        [TestCase(ItemGrade.Transcendent, 114000)]
        public void DirectUnlockChargesAllUnpaidGradesAndDoesNotRepeat(ItemGrade grade, long cost)
        {
            var save = Funded();
            Equip(save, "iron_sword");
            string error;
            Assert.IsTrue(Gear.EnhanceSlot(save, Inventory.Hero, 0, out error), error);
            string id = Weapon(grade);
            Equip(save, id);
            Assert.AreEqual(0, Gear.LevelFor(save, Inventory.Hero, id));
            Assert.AreEqual(1, Gear.SlotLevel(save, Inventory.Hero, 0));
            Assert.AreEqual(cost, Gear.GradeUnlockCost(save, Inventory.Hero, 0, grade));
            long before = save.Doo;
            Assert.IsTrue(Gear.UnlockSlotGrade(save, Inventory.Hero, 0, out error), error);
            Assert.AreEqual(before - cost, save.Doo);
            Assert.AreEqual(1, Gear.LevelFor(save, Inventory.Hero, id));
            Assert.IsTrue(Gear.IsGradeUnlocked(save, Inventory.Hero, 0, ItemGrade.Legendary));
            Assert.IsFalse(Gear.UnlockSlotGrade(save, Inventory.Hero, 0, out error));
            Assert.AreEqual(before - cost, save.Doo);
        }

        [Test]
        public void SequentialUnlocksCostEachUnpaidStepAndSurviveReplacement()
        {
            var save = Funded();
            string error;
            var grades = new[] { ItemGrade.Legendary, ItemGrade.Mythic, ItemGrade.Transcendent };
            var costs = new long[] { 24000, 30000, 60000 };
            long before = save.Doo;
            for (int i = 0; i < grades.Length; i++)
            {
                Equip(save, Weapon(grades[i]));
                Assert.AreEqual(costs[i], Gear.GradeUnlockCost(save, Inventory.Hero, 0, grades[i]));
                Assert.IsTrue(Gear.UnlockSlotGrade(save, Inventory.Hero, 0, out error), error);
            }
            Assert.AreEqual(before - 114000, save.Doo);
            Equip(save, Weapon(ItemGrade.Legendary));
            Assert.IsTrue(Gear.IsGradeUnlocked(save, Inventory.Hero, 0, ItemGrade.Legendary));
            Inventory.Unequip(save, Weapon(ItemGrade.Legendary));
            Assert.AreEqual(ItemGrade.Transcendent, Gear.UnlockedGrade(save, Inventory.Hero, 0));
            Assert.AreEqual(ItemGrade.Normal, Gear.UnlockedGrade(save, "hyo_ryong", 0));
            Assert.AreEqual(ItemGrade.Normal, Gear.UnlockedGrade(save, Inventory.Hero, 1));
        }

        [Test]
        public void LockedGearKeepsItsStarBonusAndCannotSpendEnhancementResources()
        {
            var save = Funded();
            string id = Weapon(ItemGrade.Legendary), error;
            Equip(save, id);
            Inventory.Add(save, id, 1);
            Assert.IsTrue(Gear.Breakthrough(save, id, out error), error);
            Assert.AreEqual(1, Gear.LevelsFor(save, Inventory.Hero).Single().Stars);
            Assert.AreEqual(0, Gear.LevelsFor(save, Inventory.Hero).Single().Level);
            long before = save.Doo;
            int stones = Inventory.Count(save, Gear.StoneId);
            Assert.IsFalse(Gear.CanEnhanceSlot(save, Inventory.Hero, 0));
            Assert.IsFalse(Gear.EnhanceSlot(save, Inventory.Hero, 0, out error));
            Assert.AreEqual(before, save.Doo);
            Assert.AreEqual(stones, Inventory.Count(save, Gear.StoneId));
        }

        [Test]
        public void InsufficientUnlockFundsLeaveGradeAndStoredLevelUntouched()
        {
            var save = Funded();
            Equip(save, Ritual.RewardId);
            save.Doo = 99999;
            string error;
            Assert.IsFalse(Gear.UnlockSlotGrade(save, Inventory.Hero, 0, out error));
            Assert.AreEqual(99999, save.Doo);
            Assert.AreEqual(ItemGrade.Normal, Gear.UnlockedGrade(save, Inventory.Hero, 0));
            Assert.IsFalse(Gear.UnlockSlotGrade(save, Inventory.Hero, -1, out error));
        }

        [TestCase(20)]
        [TestCase(40)]
        [TestCase(60)]
        [TestCase(80)]
        [TestCase(90)]
        public void CrossingMaterialBoundaryRequiresTheNewMaterialWithoutFallback(int level)
        {
            var save = Funded();
            Equip(save, "iron_sword");
            string error;
            Inventory.Add(save, "iron_sword", 5);
            for (int i = 0; i < 5; i++) Gear.Breakthrough(save, "iron_sword", out error);
            for (int i = 0; i < level; i++) Assert.IsTrue(Gear.EnhanceSlot(save, Inventory.Hero, 0, out error), error);
            var material = Gear.MaterialForLevel(level + 1);
            Inventory.Remove(save, material.Id, Inventory.Count(save, material.Id));
            long before = save.Doo;
            Assert.IsFalse(Gear.EnhanceSlot(save, Inventory.Hero, 0, out error));
            Assert.AreEqual(before, save.Doo);
            Assert.AreEqual(level, Gear.SlotLevel(save, Inventory.Hero, 0));
            Inventory.Add(save, material.Id, Gear.StoneCost(level + 1));
            Assert.IsTrue(Gear.EnhanceSlot(save, Inventory.Hero, 0, out error), error);
            Assert.AreEqual(0, Inventory.Count(save, material.Id));
            Assert.AreEqual(before - Gear.DooCost(level + 1), save.Doo);
        }

        [Test]
        public void ExistingEnhancedSlotsKeepTheirGradeBonusOnceWithoutSpendingDoo()
        {
            var save = new SaveData { GearProgressionMigrated = true, Doo = 321 };
            string id = Weapon(ItemGrade.Legendary);
            save.Items.Add(new ItemStack(id, 1));
            save.EquippedGear.Add(id);
            save.GearSlots.Add(new GearSlot { Owner = Inventory.Hero, Index = 0, ItemId = id, Level = 10 });
            Gear.EnsureProgression(save);
            Assert.AreEqual(10, Gear.LevelFor(save, Inventory.Hero, id));
            Assert.AreEqual(ItemGrade.Legendary, Gear.UnlockedGrade(save, Inventory.Hero, 0));
            Assert.AreEqual(321, save.Doo);
            Equip(save, Weapon(ItemGrade.Mythic));
            Assert.AreEqual(0, Gear.LevelFor(save, Inventory.Hero, Weapon(ItemGrade.Mythic)));
            Assert.AreEqual(30000, Gear.GradeUnlockCost(save, Inventory.Hero, 0, ItemGrade.Mythic));
        }

        [Test]
        public void EnhancedLowGradeSlotsStayUnlockedWhenUnlockPricesStart()
        {
            var save = new SaveData { GearProgressionMigrated = true, Doo = 321 };
            save.Items.Add(new ItemStack("mukryonghwan", 1));
            save.Items.Add(new ItemStack("claw_necklace", 1));
            save.EquippedGear.Add("mukryonghwan");
            save.EquippedGear.Add("claw_necklace");
            save.GearSlots.Add(new GearSlot { Owner = Inventory.Hero, Index = 4, ItemId = "mukryonghwan", Level = 5 });
            save.GearSlots.Add(new GearSlot { Owner = Inventory.Hero, Index = 5, ItemId = "claw_necklace", Level = 0 });
            Gear.EnsureProgression(save);
            Assert.AreEqual(ItemGrade.Rare, Gear.UnlockedGrade(save, Inventory.Hero, 4));
            Assert.AreEqual(5, Gear.LevelFor(save, Inventory.Hero, "mukryonghwan"));
            Assert.AreEqual(ItemGrade.Normal, Gear.UnlockedGrade(save, Inventory.Hero, 5));
            Assert.AreEqual(1000, Gear.GradeUnlockCost(save, Inventory.Hero, 5, ItemGrade.Fine));
            Assert.AreEqual(321, save.Doo);
        }

        [Test]
        public void LegacySecondAccessoryUnlockMigratesToTheCorrectSlot()
        {
            var save = new SaveData();
            string id = EarlyGameContent.Items.First(i => i.Kind == ItemKind.Accessory && i.Grade == ItemGrade.Legendary).Id;
            save.Items.Add(new ItemStack("mukryonghwan", 1));
            save.Items.Add(new ItemStack(id, 1));
            save.EquippedGear.Add("mukryonghwan");
            save.EquippedGear.Add(id);
            save.GearLevels.Add(new GearLevel { Id = id, Level = 1 });
            Gear.EnsureProgression(save);
            Assert.AreEqual(ItemGrade.Normal, Gear.UnlockedGrade(save, Inventory.Hero, 4));
            Assert.AreEqual(ItemGrade.Legendary, Gear.UnlockedGrade(save, Inventory.Hero, 5));
            Assert.AreEqual(5, Gear.LevelFor(save, Inventory.Hero, id));   // old +1 (10%) -> new +5 (x1.10)
        }

        [TestCase(1, 0)]
        [TestCase(5, 0)]
        [TestCase(6, 1)]
        [TestCase(10, 1)]
        [TestCase(11, 2)]
        [TestCase(16, 3)]
        [TestCase(21, 4)]
        [TestCase(26, 5)]
        [TestCase(30, 5)]
        public void DailyRewardsNeverGrantEnhancementMaterialsAtAnyProgression(int stage, int tier)
        {
            var save = new SaveData { ClearedStage = stage - 1 };
            var mission = Daily.Missions(save)[0];
            Daily.Record(save, mission.Event, mission.Target);
            Daily.Claim(save, 0);
            Assert.IsTrue(Gear.Materials.All(m => Inventory.Count(save, m.Id) == 0));
            Daily.Claim(save, 0);
            Assert.IsTrue(Gear.Materials.All(m => Inventory.Count(save, m.Id) == 0));
        }

        private sealed class MaterialRandom : IRandom
        {
            public double Value;
            public double NextDouble() { return Value; }
        }

        [TestCase(ItemGrade.Normal, "하급 영석", 7)]
        [TestCase(ItemGrade.Fine, "중급 영석", 6)]
        [TestCase(ItemGrade.Rare, "고급 영석", 5)]
        [TestCase(ItemGrade.Epic, "상급 현철", 4)]
        [TestCase(ItemGrade.Legendary, "만년 현철", 3)]
        public void DismantlingGrantsTheMatchingMaterialWithGradeSpecificEqualProbabilityBuckets(ItemGrade grade, string name, int max)
        {
            var save = new SaveData();
            var rng = new MaterialRandom();
            var seen = new int[max];
            string id = Weapon(grade), error;
            int total = 0;
            for (int roll = 0; roll < max * 20; roll++)
            {
                rng.Value = (roll + .5) / (max * 20);
                Inventory.Add(save, id, 1);
                ItemStack enhancement;
                var crafting = Salvage.Dismantle(save, id, rng, out enhancement, out error);
                Assert.IsNotNull(crafting, error);
                Assert.AreEqual(name, EarlyGameContent.FindItem(enhancement.Id).Name);
                Assert.That(enhancement.Count, Is.InRange(1, Salvage.MaxEnhancementCount(EarlyGameContent.FindItem(id).Grade)));
                seen[enhancement.Count - 1]++;
                total += enhancement.Count;
            }
            CollectionAssert.AreEqual(Enumerable.Repeat(20, max).ToArray(), seen);
            Assert.AreEqual(total, Inventory.Count(save, Salvage.EnhancementMaterialFor(EarlyGameContent.FindItem(id)).Id));
            Assert.AreEqual(0, Inventory.Count(save, id));
        }

        [TestCase(ItemGrade.Normal, 7)]
        [TestCase(ItemGrade.Fine, 6)]
        [TestCase(ItemGrade.Rare, 5)]
        [TestCase(ItemGrade.Epic, 4)]
        [TestCase(ItemGrade.Legendary, 3)]
        public void BulkDismantlePreviewAndActualYieldAgreeAtBothGradeSpecificExtremes(ItemGrade grade, int max)
        {
            foreach (double roll in new[] { 0.0, .999999999 })
            {
                var save = new SaveData();
                string id = Weapon(grade);
                Inventory.Add(save, id, 3);
                var plan = Salvage.BulkPlan(save, new[] { true, true, true, true, true }, new[] { true, true, true, true, true }, true);
                var material = Salvage.EnhancementMaterialFor(EarlyGameContent.FindItem(id));
                var preview = Salvage.BulkYield(plan).Single(e => e.Key.Id == material.Id);
                CollectionAssert.AreEqual(new[] { 3, 3 * max }, preview.Value);
                var gained = Salvage.DismantleBulk(save, plan, new MaterialRandom { Value = roll });
                int expected = roll == 0.0 ? 3 : 3 * max;
                Assert.AreEqual(expected, gained.Single(e => e.Id == material.Id).Count);
                Assert.AreEqual(expected, Inventory.Count(save, material.Id));
            }
        }

        [TestCase(1, 3)]
        [TestCase(10, 3)]
        [TestCase(11, 3)]
        [TestCase(20, 3)]
        [TestCase(21, 5)]
        [TestCase(30, 5)]
        [TestCase(31, 5)]
        [TestCase(40, 5)]
        [TestCase(41, 5)]
        [TestCase(50, 5)]
        [TestCase(51, 6)]
        [TestCase(60, 6)]
        [TestCase(61, 7)]
        [TestCase(70, 7)]
        [TestCase(71, 8)]
        [TestCase(80, 8)]
        [TestCase(81, 9)]
        [TestCase(90, 9)]
        [TestCase(91, 10)]
        [TestCase(100, 10)]
        public void EnhancementMaterialAmountsMatchEachTargetLevelBoundary(int target, int expected)
        {
            Assert.AreEqual(expected, Gear.StoneCost(target));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void ThreeMaterialsFuseIntoExactlyOneNextTierWithoutDoo(int tier)
        {
            var save = new SaveData { Doo = 42 };
            var source = Gear.Materials[tier];
            var target = Gear.Materials[tier + 1];
            Inventory.Add(save, source.Id, 5);
            string error;
            Assert.IsTrue(Gear.CanFuseMaterial(save, source.Id));
            Assert.AreSame(target, Gear.FuseMaterial(save, source.Id, out error), error);
            Assert.AreEqual(2, Inventory.Count(save, source.Id));
            Assert.AreEqual(1, Inventory.Count(save, target.Id));
            Assert.AreEqual(42, save.Doo);
            Assert.IsFalse(Gear.CanFuseMaterial(save, source.Id));
            Assert.IsNull(Gear.FuseMaterial(save, source.Id, out error));
            Assert.AreEqual(2, Inventory.Count(save, source.Id));
            Assert.AreEqual(1, Inventory.Count(save, target.Id));
        }

        [Test]
        public void ManyFusionsAtOnceUseThreeEachAndStopAtWhatTheBagHolds()
        {
            var save = new SaveData { Doo = 42 };
            var source = Gear.Materials[0];
            var target = Gear.Materials[1];
            Inventory.Add(save, source.Id, 20);
            Assert.AreEqual(6, Gear.MaxMaterialFusions(save, source.Id));
            string error;
            Assert.IsNull(Gear.FuseMaterial(save, source.Id, 7, out error));   // 21 needed, 20 held: nothing is used
            Assert.AreEqual(20, Inventory.Count(save, source.Id));
            Assert.IsNull(Gear.FuseMaterial(save, source.Id, 0, out error));
            Assert.AreSame(target, Gear.FuseMaterial(save, source.Id, 6, out error), error);
            Assert.AreEqual(2, Inventory.Count(save, source.Id));
            Assert.AreEqual(6, Inventory.Count(save, target.Id));
            Assert.AreEqual(0, Gear.MaxMaterialFusions(save, source.Id));
            Assert.AreEqual(42, save.Doo);
            Assert.AreEqual(0, Gear.MaxMaterialFusions(save, Gear.Materials[Gear.Materials.Length - 1].Id));
        }

        [Test]
        public void EnhancingFromZeroToHundredCostsAboutThreeHundredThousand()
        {
            long total = 0;
            for (int n = 1; n <= Gear.MaxLevel; n++) total += Gear.DooCost(n);
            Assert.AreEqual(299175, total);
            Assert.AreEqual(59, Gear.DooCost(1));
            Assert.AreEqual(8350, Gear.DooCost(100));
        }

        [Test]
        public void InvalidAndFinalTierMaterialsCannotBeFusedOrConsumed()
        {
            var save = new SaveData();
            string final = Gear.Materials.Last().Id, error;
            Inventory.Add(save, final, 3);
            Inventory.Add(save, "iron_sword", 3);
            Assert.IsNull(Gear.FuseMaterial(save, final, out error));
            Assert.IsNull(Gear.FuseMaterial(save, "iron_sword", out error));
            Assert.IsNull(Gear.FuseMaterial(save, "not_an_item", out error));
            Assert.AreEqual(3, Inventory.Count(save, final));
            Assert.AreEqual(3, Inventory.Count(save, "iron_sword"));
        }

        [Test]
        public void CelestialStoneCanBeCraftedFromThreeAncientIronButNeverGrantedByCombat()
        {
            var save = new SaveData();
            Inventory.Add(save, Gear.Materials[4].Id, 3);
            string error;
            Assert.AreSame(Gear.Materials[5], Gear.FuseMaterial(save, Gear.Materials[4].Id, out error), error);
            for (int stage = 1; stage <= 100; stage++)
                Assert.AreNotEqual(Gear.Materials[5].Id, GearDrops.HighestMaterial(stage, 2).Id);
            Assert.AreEqual("고급 영석", GearDrops.HighestMaterial(26, 0).Name);
            Assert.AreEqual("만년 현철", GearDrops.HighestMaterial(30, 1).Name);
            Assert.IsNull(Salvage.EnhancementMaterialFor(EarlyGameContent.FindItem(Weapon(ItemGrade.Mythic))));
            Assert.IsNull(Salvage.EnhancementMaterialFor(EarlyGameContent.FindItem(Ritual.RewardId)));
        }

        [TestCase(0, 1f)]
        [TestCase(1, 1.02f)]
        [TestCase(20, 1.4f)]
        [TestCase(21, 1.43f)]
        [TestCase(30, 1.7f)]
        [TestCase(31, 1.74f)]
        [TestCase(50, 2.5f)]
        [TestCase(51, 2.55f)]
        [TestCase(70, 3.5f)]
        [TestCase(71, 3.57f)]
        [TestCase(90, 4.9f)]
        [TestCase(91, 5f)]
        [TestCase(100, 5.9f)]
        [TestCase(101, 5.9f)]
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
            var bonuses = new[] { 0f, .10f, .20f, .40f, .65f, 1.00f };
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
            Assert.AreEqual(6.9f, Gear.Multiplier(100, 5), .0001f);
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
            Assert.AreEqual(610, totalStones);
            Assert.AreEqual(299175, totalDoo);
            Assert.AreEqual(100000000 - totalDoo, save.Doo);
            Assert.AreEqual(600000 - totalStones, Gear.Materials.Sum(m => Inventory.Count(save, m.Id)));
            var actual = Inventory.ApplyGear(new CombatStats { MaxHp = 1000, MaxQi = 100, Attack = 1000, Defense = 100, Speed = 100 }, save.EquippedGear, Gear.LevelsFor(save, Inventory.Hero));
            Assert.AreEqual(1276, actual.Attack, "4% base weapon x 6.9 contributes 27.6%");
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
            Assert.IsTrue(Gear.UnlockSlotGrade(save, Inventory.Hero, 4, out error), error);
            Assert.IsTrue(Gear.UnlockSlotGrade(save, Inventory.Hero, 5, out error), error);
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
            Assert.IsTrue(Gear.UnlockSlotGrade(save, Inventory.Hero, 4, out error), error);
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
            Assert.AreEqual(38, Gear.SlotLevel(save, Inventory.Hero, 0));   // old +10 (100%) -> new +38 (x2.02)
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
            Assert.AreEqual(15, Gear.SlotLevel(save, "hyo_ryong", 0));   // old +3 (30%) -> new +15 (x1.30)
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
            ItemStack enhancement;
            Assert.IsNotNull(Salvage.Dismantle(save, "iron_sword", new SeededRandom(4), out enhancement, out error), error);
            Assert.That(enhancement.Count, Is.InRange(1, 7));
            Assert.AreEqual(before + enhancement.Count, Inventory.Count(save, Gear.StoneId));
            Assert.AreEqual(1, Gear.SlotLevel(save, Inventory.Hero, 0));
        }

        [Test]
        public void StarterGrantsFiveLowStonesOnlyOnceAndDailyGrantsNone()
        {
            var clock = Daily.Clock;
            try
            {
                Daily.Clock = () => new DateTime(2026, 10, 20);
                var save = new SaveData();
                Inventory.EnsureStartKit(save);
                Assert.AreEqual(5, Inventory.Count(save, Gear.StoneId));
                Inventory.EnsureStartKit(save);
                Assert.AreEqual(5, Inventory.Count(save, Gear.StoneId));
                var mission = Daily.Missions(save)[0];
                Daily.Record(save, mission.Event, mission.Target);
                Daily.Claim(save, 0);
                Assert.AreEqual(5, Inventory.Count(save, Gear.StoneId));
                Assert.IsTrue(Gear.Materials.Skip(1).All(m => Inventory.Count(save, m.Id) == 0));
                Daily.Claim(save, 0);
                Assert.AreEqual(5, Inventory.Count(save, Gear.StoneId));
                Assert.IsTrue(Gear.Materials.Skip(1).All(m => Inventory.Count(save, m.Id) == 0));
                Assert.AreEqual(0, EarlyGameContent.FindItem(Gear.StoneId).SellValue);
            }
            finally { Daily.Clock = clock; }
        }

        [Test]
        public void StarterAndAllDailyClaimsPreserveExistingMaterialStacks()
        {
            var save = Funded();
            Inventory.EnsureStartKit(save);
            var missions = Daily.Missions(save);
            for (int i = 0; i < missions.Count; i++)
            {
                Daily.Record(save, missions[i].Event, missions[i].Target);
                Assert.IsNotEmpty(Daily.Claim(save, i));
            }
            foreach (var material in Gear.Materials) Assert.AreEqual(material.Id == Gear.StoneId ? 100005 : 100000, Inventory.Count(save, material.Id));
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
