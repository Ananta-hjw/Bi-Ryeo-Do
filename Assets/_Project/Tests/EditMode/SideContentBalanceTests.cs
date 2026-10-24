using System;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class SideContentBalanceTests
    {
        [TestCase("q_hogol")]
        [TestCase("q_yonggol")]
        [TestCase("q_hoajang")]
        public void EarlyQuestStatsKeepTheirExistingScaling(string id)
        {
            var q = Quests.Find(id);
            var stage = Quests.ToStage(q);
            foreach (string enemyId in stage.Encounters[0])
            {
                var raw = EarlyGameContent.FindEnemy(enemyId);
                var enemy = stage.EnemyFor(enemyId);
                Assert.AreEqual(raw.HpMul * Math.Pow(q.Edge, .7), enemy.HpMul, .001);
                Assert.AreEqual(raw.AtkMul * Math.Pow(q.Edge, .3), enemy.AtkMul, .001);
                Assert.AreEqual(raw.DefMul, enemy.DefMul);
            }
        }

        [TestCase("q_sadae")]
        [TestCase("q_baegin")]
        [TestCase("q_viper")]
        [TestCase("q_hyeolma")]
        [TestCase("q_salgwi")]
        [TestCase("q_salryong")]
        [TestCase("q_dojeon")]
        [TestCase("q_jungwon")]
        [TestCase("q_hyeol")]
        public void LaterQuestsTrackTheirMainFightOnceWithMatchingRecommendation(string id)
        {
            var q = Quests.Find(id);
            var reference = EarlyGameContent.FindStage(q.RefStage);
            var stage = Quests.ToStage(q);
            foreach (string enemyId in stage.Encounters[0])
            {
                var baseline = reference.EnemyFor(enemyId, q.Trial);
                float endurance = q.Trial.Kind == TrialKind.Survive ? 1.25f : 1;
                var enemy = stage.EnemyFor(enemyId);
                Assert.AreEqual(baseline.HpMul * Math.Pow(q.Edge, .7) * endurance, enemy.HpMul, .001);
                Assert.AreEqual(baseline.AtkMul * Math.Pow(q.Edge, .3), enemy.AtkMul, .001);
                Assert.AreEqual(baseline.DefMul, enemy.DefMul);
                Assert.AreEqual(baseline.Level, enemy.Level);
            }
            Assert.AreEqual(Recommend.ForStage(reference), Recommend.ForStage(stage));
            var save = SideContentSimulation.Prepared(q.RefStage + 1, BalanceSimulation.BotLevel(reference));
            var runs = Enumerable.Range(901, 40).Select(seed => SideContentSimulation.Play(stage, save, seed)).ToArray();
            Assert.GreaterOrEqual(runs.Count(r => r.Won), 34, id + ": prepared players need a clearable route");
            if (q.Trial.Kind == TrialKind.Deadline)
                Assert.That(runs.Average(r => r.Actions), Is.InRange(q.Trial.Limit * .65, (double)q.Trial.Limit));
        }

        [Test]
        public void ViperTrialActuallyLastsThroughTheAdvertisedEnemyActions()
        {
            var q = Quests.Find("q_viper");
            var save = SideContentSimulation.Prepared(21, BalanceSimulation.BotLevel(EarlyGameContent.FindStage(20)));
            var runs = Enumerable.Range(901, 40).Select(seed => SideContentSimulation.Play(Quests.ToStage(q), save, seed)).ToArray();
            Assert.IsTrue(runs.All(r => r.Won));
            Assert.GreaterOrEqual(runs.Count(r => r.EnemyActions >= q.Trial.Limit), 36);
        }

        [TestCase(1)]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(24)]
        [TestCase(40)]
        [TestCase(60)]
        public void PreparedTowerPartiesFaceRealFightsWithoutExcessiveLength(int floor)
        {
            var save = SideContentSimulation.Prepared(Tower.ReferenceStage(floor) + 1, Tower.RecommendedPlayerLevel(floor));
            var stage = Tower.ToStage(save, floor);
            var runs = Enumerable.Range(901, 40).Select(seed => SideContentSimulation.Play(stage, save, seed)).ToArray();
            Assert.GreaterOrEqual(runs.Count(r => r.Won), 34);
            Assert.That(runs.Average(r => r.Actions), Is.InRange(8.0, 100.0));
            Assert.AreEqual(Tower.RecommendedCurve(floor), stage.RecommendedLevel);
        }

        [TestCase(15)]
        [TestCase(25)]
        [TestCase(30)]
        [TestCase(35)]
        [TestCase(60)]
        public void RespondingToBossTelegraphsPreservesMoreOfTheParty(int floor)
        {
            var save = SideContentSimulation.Prepared(Tower.ReferenceStage(floor) + 1, Tower.RecommendedPlayerLevel(floor));
            var stage = Tower.ToStage(save, floor);
            var careful = Enumerable.Range(901, 40).Select(seed => SideContentSimulation.Play(stage, save, seed)).ToArray();
            var reckless = Enumerable.Range(901, 40).Select(seed => SideContentSimulation.Play(stage, save, seed, false)).ToArray();
            Assert.GreaterOrEqual(careful.Count(r => r.Won), 34);
            TestContext.Out.WriteLine("tower " + floor + " incoming damage: " + careful.Average(r => r.DamageTaken) + " vs " + reckless.Average(r => r.DamageTaken));
            Assert.Greater(careful.Average(r => r.HpLeft), reckless.Average(r => r.HpLeft));
            Assert.Less(careful.Average(r => r.DamageTaken), reckless.Average(r => r.DamageTaken) * .95);
        }

        [TestCase(40)]
        [TestCase(49)]
        [TestCase(50)]
        public void InvestmentLetsTheSameLevelPartyProgressPastTheFormerWall(int floor)
        {
            var plain = SideContentSimulation.Prepared(36, 46);
            var geared = SideContentSimulation.Prepared(36, 46, true);
            var stage = Tower.ToStage(plain, floor);
            var before = Enumerable.Range(901, 40).Select(seed => SideContentSimulation.Play(stage, plain, seed)).ToArray();
            var after = Enumerable.Range(901, 40).Select(seed => SideContentSimulation.Play(stage, geared, seed)).ToArray();
            Assert.GreaterOrEqual(after.Count(r => r.Won), 34);
            Assert.GreaterOrEqual(after.Count(r => r.Won), before.Count(r => r.Won));
            if (before.All(r => r.Won)) Assert.Less(after.Average(r => r.Actions), before.Average(r => r.Actions) * .95);
            else Assert.Greater(after.Average(r => r.HpLeft), before.Average(r => r.HpLeft));
        }

        [Test]
        public void BossPatternsAndAwakeningOfferDistinctPredictableThreats()
        {
            var bosses = new[] { 5, 10, 15 }.Select(f => Tower.BuildEnemies(f).Single(e => e.IsBoss)).ToArray();
            Assert.AreEqual(3, bosses.Select(b => string.Join(",", b.Moves.Select(m => m.Power + "/" + m.Hits + "/" + m.Snipe))).Distinct().Count());
            foreach (var boss in bosses)
            {
                var unit = boss.CreateCombatant(boss.Id);
                Assert.AreEqual(2, unit.Phases.Count);
                Assert.AreEqual(BossPhases.FirstPct, unit.Phases[0].HpPct);
                Assert.AreEqual(BossPhases.SecondPct, unit.Phases[1].HpPct);
                Assert.IsTrue(boss.Moves.Any(m => m.Charge));
            }
        }

        [Test]
        public void FixedFloorStatsNeverChaseThePlayersSaveAndDeepStatsKeepGrowing()
        {
            var weak = SideContentSimulation.Prepared(9, 10);
            var strong = SideContentSimulation.Prepared(36, 70, true);
            var a = Tower.ToStage(weak, 25).Encounters[0].Select(EarlyGameContent.FindEnemy).Select(e => e.BuildStats()).ToArray();
            var b = Tower.ToStage(strong, 25).Encounters[0].Select(EarlyGameContent.FindEnemy).Select(e => e.BuildStats()).ToArray();
            CollectionAssert.AreEqual(a, b);
            var previous = Tower.BuildEnemies(35)[0].BuildStats();
            foreach (int floor in new[] { 40, 60, 90, 95, 120, 150 })
            {
                var next = Tower.BuildEnemies(floor)[0].BuildStats();
                Assert.Greater(next.MaxHp, previous.MaxHp, "deep HP must not lose the shared scale at a legacy trial threshold");
                Assert.Greater(next.EffAttack, previous.EffAttack);
                Assert.Greater(next.EffDefense, previous.EffDefense);
                previous = next;
            }
        }
    }
}
