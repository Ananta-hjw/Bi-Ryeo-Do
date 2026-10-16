using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class CompanionTrainingTests
    {
        private static SaveData Joined(int bondPoints = 0)
        {
            var save = new SaveData { ClearedStage = 30, Doo = 1000000 };
            foreach (var c in EarlyGameContent.Companions) Bonds.Add(save, c.Id, bondPoints);
            return save;
        }

        [Test]
        public void CostsGrowAndStatsRiseByFourPercentPerLevel()
        {
            Assert.AreEqual(400, CompanionTraining.CostFor(1));
            Assert.Greater(CompanionTraining.CostFor(10), CompanionTraining.CostFor(5));
            Assert.AreEqual(-1, CompanionTraining.CostFor(11));
            Assert.AreEqual(1.4f, CompanionTraining.StatMultiplier(10), 0.0001f);
            var c = EarlyGameContent.FindCompanion("hyo_ryong");
            var plain = c.CreateCombatant(30).Stats;
            var trained = c.CreateCombatant(30, 0, 10).Stats;
            Assert.Greater(trained.MaxHp, plain.MaxHp);
            Assert.AreEqual((int)System.Math.Round(plain.Attack * 1.4), trained.Attack, 2);
        }

        [Test]
        public void BondCapsTheTrainingLevel()
        {
            var save = Joined(0);   // 서먹: cap 3
            string error;
            for (int i = 0; i < 3; i++) Assert.IsTrue(CompanionTraining.Train(save, "hyo_ryong", out error));
            Assert.IsFalse(CompanionTraining.Train(save, "hyo_ryong", out error));
            StringAssert.Contains("인연", error);
            Bonds.Add(save, "hyo_ryong", 3);   // 친분: cap 5
            Assert.AreEqual(5, CompanionTraining.Cap(save, "hyo_ryong"));
            Assert.IsTrue(CompanionTraining.Train(save, "hyo_ryong", out error));
            var full = Joined(15);
            for (int i = 0; i < 10; i++) Assert.IsTrue(CompanionTraining.Train(full, "jang_hong", out error));
            Assert.IsFalse(CompanionTraining.Train(full, "jang_hong", out error));
            Assert.AreEqual(10, CompanionTraining.LevelOf(full, "jang_hong"));
        }

        [Test]
        public void TrainingNeedsDooAndAJoinedCompanion()
        {
            var save = new SaveData { ClearedStage = 30, Doo = 100 };
            string error;
            Assert.IsFalse(CompanionTraining.Train(save, "hyo_ryong", out error));
            Assert.AreEqual(100, save.Doo);
            Assert.IsFalse(CompanionTraining.Train(new SaveData { ClearedStage = 1, Doo = 99999 }, "im_seongjin", out error));   // joins at stage 22
            Assert.IsFalse(CompanionTraining.Train(save, "nobody", out error));
        }

        [Test]
        public void MilestonesChangeTheSkills()
        {
            var c = EarlyGameContent.FindCompanion("hyo_ryong");
            var baseSkills = c.CreateCombatant(30).Skills;
            var l3 = c.CreateCombatant(30, 0, 3).Skills;
            var l6 = c.CreateCombatant(30, 0, 6).Skills;
            var l10 = c.CreateCombatant(30, 0, 10).Skills;
            var twin = baseSkills.First(s => s.Id == "xr_twin");
            Assert.AreEqual(twin.Power * 1.1f, l3.First(s => s.Id == "xr_twin").Power, 0.001f);
            Assert.Less(l6.First(s => s.Id == "xr_twin").QiCost, twin.QiCost);       // main art cheaper
            Assert.AreEqual(twin.Hits + 1, l10.First(s => s.Id == "xr_twin").Hits);   // awakened
            Assert.AreEqual(baseSkills.First(s => s.Id == "xr_barrier").Cooldown - 1, l10.First(s => s.Id == "xr_barrier").Cooldown);
        }

        [Test]
        public void EveryCompanionHasAnAwakeningAndAllUnitsKeepValidSkills()
        {
            foreach (var c in EarlyGameContent.Companions)
            {
                Assert.IsFalse(string.IsNullOrEmpty(CompanionTraining.AwakeningText(c.Id)), c.Id);
                var skills = c.CreateCombatant(40, 3, 10).Skills;
                foreach (var s in skills)
                {
                    Assert.GreaterOrEqual(s.Cooldown, 0, c.Id + " " + s.Id);
                    Assert.GreaterOrEqual(s.QiCost, 0, c.Id + " " + s.Id);
                    Assert.GreaterOrEqual(s.Hits, 1, c.Id + " " + s.Id);
                }
            }
            Assert.AreEqual("", CompanionTraining.NextMilestone("hyo_ryong", 10));
            StringAssert.Contains("각성", CompanionTraining.NextMilestone("hyo_ryong", 6));
        }

        [Test]
        public void StatusSheetShowsTrainedStats()
        {
            var c = EarlyGameContent.FindCompanion("jang_hong");
            var before = int.Parse(StatusSheet.Ally(c, 30, 0, 0)[0].Value.Replace(",", ""));
            var after = int.Parse(StatusSheet.Ally(c, 30, 0, 10)[0].Value.Replace(",", ""));
            Assert.Greater(after, before);
        }
    }
}
