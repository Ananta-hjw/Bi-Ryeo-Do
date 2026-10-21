using System;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class SharedRealmBonusTests
    {
        private static void EqualStats(CombatStats expected, CombatStats actual)
        {
            Assert.AreEqual(expected.MaxHp, actual.MaxHp);
            Assert.AreEqual(expected.MaxQi, actual.MaxQi);
            Assert.AreEqual(expected.Attack, actual.Attack);
            Assert.AreEqual(expected.Defense, actual.Defense);
            Assert.AreEqual(expected.Speed, actual.Speed);
            Assert.AreEqual(expected.CritRate, actual.CritRate);
            Assert.AreEqual(expected.CritDamage, actual.CritDamage);
            Assert.AreEqual(expected.AtkUnit, actual.AtkUnit);
            Assert.AreEqual(expected.DefUnit, actual.DefUnit);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void EnemyDifficultyAppliesBeforeOneRealmBonusAndDoesNotFeedBack(int difficulty)
        {
            var enemy = StageStars.ScaleEnemy(EarlyGameContent.FindEnemy("bi_daegongja"), 30, difficulty);
            var before = enemy.BuildStatsBeforeRealm();
            int step = Realm.StepOf(Realm.Power(before));
            var expected = Realm.ApplyBonus(before, step);
            var unit = enemy.CreateCombatant("enemy");
            Assert.Greater(step, 0);
            Assert.AreEqual(step, unit.RealmStep);
            Assert.AreEqual(Realm.Power(before), unit.RealmPowerBeforeBonus);
            EqualStats(expected, enemy.BuildStats());
            EqualStats(expected, unit.Stats);
            EqualStats(unit.Stats, enemy.CreateCombatant("again").Stats);
            Assert.AreEqual(expected.MaxHp, unit.Hp);
            Assert.AreEqual(expected.MaxQi, unit.Qi);
            Assert.AreEqual(Realm.Label(step), Realm.ForEnemy(enemy));
        }

        [Test]
        public void CompanionAppliesBondTrainingAndGearBeforeItsRealmBonus()
        {
            var c = new CompanionData { Id = "realm_test", Name = "Test" };
            const int level = 40, bond = 2, training = 5;
            var before = Progression.AllyStatsFor(level);
            float multiplier = Bonds.StatMultiplier(bond) * CompanionTraining.StatMultiplier(training);
            before.MaxHp = (int)Math.Round(before.MaxHp * multiplier);
            before.Attack = (int)Math.Round(before.Attack * multiplier);
            before.Defense = (int)Math.Round(before.Defense * multiplier);
            before.CritRate = .02f + Bonds.CritBonus(bond);
            before.CritDamage = 1.5f;
            var gear = new[] { "iron_sword" };
            before = Inventory.ApplyGear(before, gear);
            int step = Realm.StepOf(Realm.Power(before));
            var unit = c.CreateCombatant(level, bond, training, gear);
            Assert.Greater(step, 0);
            Assert.AreEqual(step, unit.RealmStep);
            Assert.AreEqual(Realm.Power(before), unit.RealmPowerBeforeBonus);
            EqualStats(Realm.ApplyBonus(before, step), unit.Stats);
        }

        [Test]
        public void AllNonMartialEnemiesKeepTheirUnboostedStats()
        {
            var excluded = EarlyGameContent.Enemies.Where(e => !Realm.HasRealm(e)).ToList();
            Assert.IsNotEmpty(excluded);
            foreach (var enemy in excluded)
            {
                var scaled = enemy.Scaled(20f, 10f, 5f, 60);
                EqualStats(scaled.BuildStatsBeforeRealm(), scaled.CreateCombatant(enemy.Id).Stats);
                EqualStats(scaled.BuildStatsBeforeRealm(), scaled.BuildStats());
                Assert.AreEqual("", Realm.ForEnemy(scaled));
            }
        }

        [Test]
        public void HeroBattleStatsStillMatchTheExistingStatusSheetCalculation()
        {
            var save = new SaveData { Level = 40, SwordRank = 8 };
            var hero = Formation.BuildHero(save);
            EqualStats(InnerArts.HeroStats(save), hero.Stats);
            Assert.AreEqual(Realm.StepOf(save), hero.RealmStep);
            Assert.AreEqual(Realm.Power(InnerArts.HeroStatsBeforeRealm(save)), hero.RealmPowerBeforeBonus);
        }

        [Test]
        public void BossPhaseDoesNotRecalculateOrCompoundItsRealm()
        {
            var unit = StageStars.ScaleEnemy(EarlyGameContent.FindEnemy("bi_daegongja"), 30, 2).CreateCombatant("boss");
            var original = unit.Stats;
            int step = unit.RealmStep;
            double power = unit.RealmPowerBeforeBonus;
            unit.ScaleAttackDefense(1.2f, 1.1f);
            unit.ScaleAttackDefense(1.5f, 1.3f);
            Assert.AreEqual((int)Math.Round(original.Attack * 1.5f), unit.Stats.Attack);
            Assert.AreEqual((int)Math.Round(original.Defense * 1.3f), unit.Stats.Defense);
            Assert.AreEqual(original.Speed, unit.Stats.Speed);
            Assert.AreEqual(step, unit.RealmStep);
            Assert.AreEqual(power, unit.RealmPowerBeforeBonus);
        }

        [Test]
        public void CompanionRiseNoticeUsesTheSamePreBonusRealmAsTheStatusSheet()
        {
            var save = new SaveData { Level = 40, ClearedStage = 29 };
            CompanionGrowth.PollRises(save);
            foreach (var companion in EarlyGameContent.Companions)
            {
                if (companion.JoinStage > save.HighestUnlockedStage) continue;
                var progress = save.CompanionLevels.First(p => p.Id == companion.Id);
                Assert.AreEqual(CompanionGrowth.Create(save, companion).RealmStep, progress.RealmMax);
            }
            Assert.IsEmpty(CompanionGrowth.PollRises(save));
        }

        [Test]
        public void CompanionRealmTextUsesTheSamePreBonusPowerAsTheBattleUnit()
        {
            var save = new SaveData { Level = 40, ClearedStage = 29 };
            var companion = EarlyGameContent.Companions.First();
            var unit = CompanionGrowth.Create(save, companion);
            Assert.AreEqual(Realm.Label(unit.RealmStep) + " (종합 " + Math.Round(unit.RealmPowerBeforeBonus).ToString("N0") + ")", StatusSheet.AllyRealm(save, companion));
        }
    }
}
