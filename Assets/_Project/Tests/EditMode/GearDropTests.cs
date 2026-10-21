using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class GearDropTests
    {
        private sealed class TwoRolls : IRandom
        {
            private readonly double _tier, _count;
            private int _calls;
            public TwoRolls(double tier, double count) { _tier = tier; _count = count; }
            public double NextDouble() { return _calls++ == 0 ? _tier : _count; }
        }

        private sealed class NoRolls : IRandom
        {
            public double NextDouble() { Assert.Fail("Excluded battles must not roll."); return 0; }
        }

        [TestCase(1, 0, new[] { 100, 0, 0, 0, 0 })]
        [TestCase(10, 0, new[] { 100, 0, 0, 0, 0 })]
        [TestCase(11, 0, new[] { 55, 45, 0, 0, 0 })]
        [TestCase(20, 0, new[] { 55, 45, 0, 0, 0 })]
        [TestCase(21, 0, new[] { 35, 35, 30, 0, 0 })]
        [TestCase(30, 0, new[] { 35, 35, 30, 0, 0 })]
        [TestCase(1, 1, new[] { 0, 35, 35, 30, 0 })]
        [TestCase(10, 1, new[] { 0, 35, 35, 30, 0 })]
        [TestCase(11, 1, new[] { 0, 0, 35, 35, 30 })]
        [TestCase(30, 1, new[] { 0, 0, 35, 35, 30 })]
        [TestCase(1, 2, new[] { 0, 0, 0, 55, 45 })]
        [TestCase(30, 2, new[] { 0, 0, 0, 55, 45 })]
        [TestCase(30, 3, new[] { 0, 0, 0, 55, 45 })]
        public void TierWeightsMatchEveryDifficultyAndStageBoundary(int stage, int difficulty, int[] expected)
        {
            var seen = new int[5];
            for (int roll = 0; roll < 100; roll++)
            {
                var drop = GearDrops.Roll(stage, difficulty, false, new TwoRolls(roll / 100.0, .99)).Single();
                Assert.AreEqual(10, drop.Count);
                int tier = System.Array.FindIndex(Gear.Materials, m => m.Id == drop.Id);
                Assert.That(tier, Is.InRange(0, 4));
                seen[tier]++;
            }
            CollectionAssert.AreEqual(expected, seen);
        }

        [Test]
        public void QuantityUsesTheRequestedEvenCountsWithUnchangedWeightsIndependentlyOfTier()
        {
            var expected = new[] { 35, 15, 14, 13, 12, 11 };
            for (int tierRoll = 0; tierRoll < 100; tierRoll++)
            {
                var seen = new int[6];
                for (int countRoll = 0; countRoll < 100; countRoll++)
                {
                    var drop = GearDrops.Roll(5, 1, false, new TwoRolls(tierRoll / 100.0, countRoll / 100.0)).SingleOrDefault();
                    seen[drop == null ? 0 : drop.Count / 2]++;
                    if (drop != null) Assert.AreEqual(0, drop.Count % 2);
                    if (drop != null)
                        Assert.AreEqual(Gear.Materials[tierRoll < 35 ? 1 : tierRoll < 70 ? 2 : 3].Id, drop.Id);
                }
                CollectionAssert.AreEqual(expected, seen);
            }
        }

        [TestCase(1, 0, 0)]
        [TestCase(10, 0, 0)]
        [TestCase(11, 0, 1)]
        [TestCase(20, 0, 1)]
        [TestCase(21, 0, 2)]
        [TestCase(30, 0, 2)]
        [TestCase(10, 1, 3)]
        [TestCase(11, 1, 4)]
        [TestCase(30, 2, 4)]
        public void FirstBossGivesSevenHighestTierEvenWhenTheNormalRollGivesNothing(int stage, int difficulty, int tier)
        {
            var drop = GearDrops.Roll(stage, difficulty, true, new TwoRolls(0, 0)).Single();
            Assert.AreEqual(Gear.Materials[tier].Id, drop.Id);
            Assert.AreEqual(7, drop.Count);
        }

        [TestCase(1, 0)]
        [TestCase(11, 0)]
        [TestCase(21, 0)]
        [TestCase(1, 1)]
        [TestCase(11, 1)]
        [TestCase(1, 2)]
        public void FirstBossAddsTheIndependentNormalRollAndMergesOnlyMatchingMaterials(int stage, int difficulty)
        {
            foreach (double tier in new[] { 0.0, .34, .55, .99 })
                for (int count = 0; count < 100; count++)
                {
                    var normal = GearDrops.Roll(stage, difficulty, false, new TwoRolls(tier, count / 100.0));
                    var boss = GearDrops.Roll(stage, difficulty, true, new TwoRolls(tier, count / 100.0));
                    string highest = GearDrops.HighestMaterial(stage, difficulty).Id;
                    Assert.AreEqual(7 + normal.Sum(drop => drop.Count), boss.Sum(drop => drop.Count));
                    Assert.AreEqual(boss.Count, boss.Select(drop => drop.Id).Distinct().Count());
                    foreach (var drop in boss)
                        Assert.AreEqual((drop.Id == highest ? 7 : 0) + normal.Where(n => n.Id == drop.Id).Sum(n => n.Count), drop.Count);
                    Assert.That(boss.Sum(drop => drop.Count), Is.InRange(7, 17));
                    Assert.IsFalse(boss.Any(drop => drop.Id == Gear.Materials[5].Id));
                }
        }

        [Test]
        public void FirstBossCanGiveBothHighestTierAndAnotherTierWithoutChangingInventory()
        {
            var save = new SaveData();
            var drops = GearDrops.RollFight(save, StoryStage(), 1, new TwoRolls(0, .99));
            Assert.AreEqual(2, drops.Count);
            Assert.AreEqual(7, drops.Single(drop => drop.Id == Gear.Materials[4].Id).Count);
            Assert.AreEqual(10, drops.Single(drop => drop.Id == Gear.Materials[2].Id).Count);
            Assert.IsTrue(drops.All(drop => Inventory.Count(save, drop.Id) == 0));
        }

        private static StageData StoryStage()
        {
            string boss = EarlyGameContent.Enemies.First(e => e.IsBoss).Id;
            return new StageData { Number = 11, Difficulty = 1, Encounters = new[] { new[] { "tiger" }, new[] { boss, boss } } };
        }

        [Test]
        public void FirstWinIsPerFightAndDifficultyAndOneRewardCoversMultipleBosses()
        {
            var save = new SaveData();
            var stage = StoryStage();
            Assert.IsEmpty(GearDrops.RollFight(save, stage, 0, new TwoRolls(0, 0)), "non-boss first wins roll normally");
            var drop = GearDrops.RollFight(save, stage, 1, new TwoRolls(0, 0)).Single();
            Assert.AreEqual(Gear.Materials[4].Id, drop.Id);
            Assert.AreEqual(7, drop.Count);
            Assert.AreEqual(0, StageStars.MaskOf(save, stage.Number, 1, 1), "rolling does not record a victory");
            StageStars.Record(save, stage.Number, 1, 1, 1);
            Assert.IsEmpty(GearDrops.RollFight(save, stage, 1, new TwoRolls(0, 0)), "boss-only replay must not get the guarantee");
            Assert.AreEqual(7, GearDrops.RollFight(save, stage.WithDifficulty(2), 1, new TwoRolls(0, 0)).Single().Count);
        }

        [Test]
        public void CompletedLegacyNormalStagesCannotReceiveAnotherFirstBossGuarantee()
        {
            var stage = StoryStage().WithDifficulty(0);
            var save = new SaveData { ClearedStage = stage.Number };
            Assert.IsEmpty(GearDrops.RollFight(save, stage, 1, new TwoRolls(0, 0)));
        }

        [Test]
        public void NonzeroReplayRollGivesOneTierAndDoesNotChangeInventory()
        {
            var stage = StoryStage();
            var save = new SaveData();
            StageStars.Record(save, stage.Number, 1, 1, 1);
            var drop = GearDrops.RollFight(save, stage, 1, new TwoRolls(.99, .99)).Single();
            Assert.AreEqual(Gear.Materials[4].Id, drop.Id);
            Assert.AreEqual(10, drop.Count);
            Assert.AreEqual(0, Inventory.Count(save, drop.Id));
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void SideModesGrantNoMaterialOnFirstClearOrReplay(bool tower, bool quest)
        {
            var stage = StoryStage();
            stage.IsTower = tower;
            stage.IsQuest = quest;
            stage.TowerFloor = 5;
            stage.QuestId = "q_hogol";
            var save = new SaveData { ClearedStage = 20 };
            Assert.IsEmpty(GearDrops.RollFight(save, stage, 0, new NoRolls()));
            Assert.IsEmpty(GearDrops.RollFight(save, stage, 1, new NoRolls()));
            save.TowerBest = 5;
            save.DoneQuests.Add(stage.QuestId);
            Assert.IsEmpty(GearDrops.RollFight(save, stage, 1, new NoRolls()));
        }

        [Test]
        public void NonStageBattlesGrantNoMaterial()
        {
            var stage = StoryStage();
            stage.Number = 0;
            Assert.IsEmpty(GearDrops.RollFight(new SaveData(), stage, 1, new NoRolls()));
        }
    }
}
