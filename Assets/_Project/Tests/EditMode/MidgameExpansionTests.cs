using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class MidgameExpansionTests
    {
        // Keep paired content probes independent of cached plans, even when their combat inputs happen to match.
        [SetUp]
        [TearDown]
        public void ClearPlanningCache()
        {
            var field = typeof(Formation).GetField("PlanCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            ((System.Collections.IDictionary)field.GetValue(null)).Clear();
        }

        [TestCase(14, 8)]
        [TestCase(15, 4)]
        [TestCase(17, 3)]
        [TestCase(18, 3)]
        [TestCase(19, 5)]
        public void MainStoryExpansionHasGrowthRewardsAndCorrectFinalOpponent(int number, int fights)
        {
            var stage = EarlyGameContent.FindStage(number);
            Assert.AreEqual(fights, stage.Encounters.Length);
            Assert.IsFalse(stage.IsQuest || stage.IsTower);
            for (int i = 0; i < fights; i++)
            {
                Recommend.FightReward(new SaveData(), stage, i, false, out long exp, out long doo);
                Assert.Greater(exp, 0); Assert.Greater(doo, 0);
                Assert.IsNotNull(StoryEpisodes.BeforeFight(stage, i, new SaveData()));
                Assert.Greater(StageStars.FastLimit(number, i), 0);
            }
            Assert.IsTrue(GearDrops.IsBossFight(stage, fights - 1));
            Assert.AreEqual(36, EarlyGameContent.Stages.Length);
            Recommend.StageReward(new SaveData(), stage, out long newExp, out long newDoo);
            var previous = WithoutAddedFights(stage);
            long oldExp, oldDoo;
            if (number == 17)
            {
                previous.RewardWeightBudget = 8;
                long e = EarlyGameContent.ExpReward(previous, "sinung_daewon"), d = EarlyGameContent.DooReward(previous, "sinung_daewon");
                oldExp = e * 5 + EarlyGameContent.ExpReward(previous, "pae_hyeokseong");
                oldDoo = Income.Boost(d * 3) + Income.Boost(d * 2) + Income.Boost(EarlyGameContent.DooReward(previous, "pae_hyeokseong"));
            }
            else Recommend.StageReward(new SaveData(), previous, out oldExp, out oldDoo);
            TestContext.Out.WriteLine("Rewards " + number + ": XP " + oldExp + " -> " + newExp + "; Doo " + oldDoo + " -> " + newDoo);
        }

        [Test]
        public void RealGuardsReplaceTheFictitiousMaharyeongDuelAndTheQuestKeepsItsScouts()
        {
            CollectionAssert.AreEqual(new[] { "pae_yugyeong", "sinung_daewon", "sinung_daewon", "sinung_daewon", "pae_hyeokseong" }, EarlyGameContent.FindStage(17).Encounters.SelectMany(x => x));
            var quest = Quests.All.Single(q => q.Id == "q_sadae");
            CollectionAssert.AreEqual(new[] { "sadae_jilpung", "sadae_jilpung", "sadae_jilpung" }, Quests.ToStage(quest).Encounters[0]);
            Assert.AreEqual(TrialKind.Survive, EarlyGameContent.FindStage(14).TrialFor(4).Kind);
            Assert.AreEqual(8, EarlyGameContent.FindStage(14).TrialFor(4).Limit);
        }

        [TestCase(14, 10, 17)]
        [TestCase(15, 9, 11)]
        [TestCase(18, 4, 5)]
        [TestCase(19, 8, 10)]
        public void NewFightsIncreaseTotalPayoutWithoutDilutingExistingFightShares(int number, int oldBudget, int newWeight)
        {
            var stage = EarlyGameContent.FindStage(number);
            Assert.AreEqual(oldBudget, stage.RewardWeightBudget);
            var baseline = stage.WithDifficulty(0); baseline.RewardWeightBudget = 0;
            var ids = stage.Encounters.SelectMany(x => x).ToArray();
            double factor = (double)newWeight / oldBudget;
            Assert.That(ids.Sum(id => EarlyGameContent.DooReward(stage, id)), Is.EqualTo(ids.Sum(id => EarlyGameContent.DooReward(baseline, id)) * factor).Within(ids.Length * factor));
            Assert.That(ids.Sum(id => EarlyGameContent.ExpReward(stage, id)), Is.EqualTo(ids.Sum(id => EarlyGameContent.ExpReward(baseline, id)) * factor).Within(ids.Length * factor));
        }

        [Test]
        public void RevisionTwoMigrationPreservesStarsDifficultyClearsAndPaidBossMaterials()
        {
            var save = new SaveData { StoryContentRevision = 2, ClearedStage = 20, ProgressStage = 14, ProgressNext = 2, Level = 24, Doo = 12345 };
            save.StageStars.AddRange(new[] {
                new StarEntry { Stage = 14, Fight = 0, Mask = 7 },
                new StarEntry { Stage = 14, Fight = 2, Diff = 1, Mask = 7 },
                new StarEntry { Stage = 15, Fight = 2, Diff = 2, Mask = 7 },
                new StarEntry { Stage = 17, Fight = 0, Mask = 7 },
                new StarEntry { Stage = 17, Fight = 1, Mask = 7 },
                new StarEntry { Stage = 17, Fight = 2, Diff = 1, Mask = 7 },
                new StarEntry { Stage = 18, Fight = 1, Diff = 1, Mask = 7 },
                new StarEntry { Stage = 19, Fight = 2, Diff = 2, Mask = 7 } });
            int stars = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(stars, StageStars.Total(save));
            Assert.AreEqual(3, save.RetiredStoryStars);
            Assert.AreEqual(7, StageStars.MaskOf(save, 14, 5, 0));
            foreach (int stage in new[] { 14, 17, 18 }) Assert.IsTrue(StageStars.StageCleared(save, stage, 1));
            foreach (int stage in new[] { 15, 19 }) Assert.IsTrue(StageStars.StageCleared(save, stage, 2));
            Assert.AreEqual(0, StageStars.MaskOf(save, 14, 0, 0));
            Assert.AreEqual(0, StageStars.MaskOf(save, 17, 0, 0));
            Assert.AreEqual(0, save.ProgressStage);
            Assert.AreEqual(20, save.ClearedStage); Assert.AreEqual(24, save.Level); Assert.AreEqual(12345, save.Doo);
            StoryContentMigration.Apply(save); Assert.AreEqual(stars, StageStars.Total(save));
            StageStars.Sync(save);
            Assert.AreEqual(0, StageStars.MaskOf(save, 14, 0, 0), "Lobby sync must not award unplayed inserted fights");
            Assert.AreEqual(0, StageStars.MaskOf(save, 17, 0, 0));
        }

        [TestCase(14)]
        [TestCase(15)]
        [TestCase(17)]
        [TestCase(18)]
        [TestCase(19)]
        public void PreparedGrowthPartyCanClearExpandedStagesWithoutProgressionOrRandomGear(int number)
        {
            var stage = EarlyGameContent.FindStage(number);
            var runs = Enumerable.Range(401, 40).Select(seed => BalanceSimulation.Play(stage, BalanceSimulation.Starter(number, BalanceSimulation.BotLevel(stage)), seed)).ToArray();
            int wins = runs.Count(run => run.Won);
            TestContext.Out.WriteLine(number + ": " + wins + "/40; actions " + runs.Where(run => run.Won).Select(run => run.Actions).DefaultIfEmpty().Average()
                + "; failures " + string.Join(",", runs.Where(run => !run.Won).GroupBy(run => run.FailedFight).Select(group => group.Key + ":" + group.Count())));
            Assert.GreaterOrEqual(wins, 34);
            for (int fight = 0; fight < stage.Encounters.Length; fight++)
            {
                var actions = runs.Where(run => run.Won).Select(run => run.FightActions[fight]).OrderBy(x => x).ToArray();
                TestContext.Out.WriteLine("Fight " + fight + ": median " + actions[actions.Length / 2] + "; limit " + StageStars.FastLimit(number, fight));
            }
        }
        private static StageData WithoutAddedFights(StageData stage)
        {
            var copy = stage.WithDifficulty(0);
            if (stage.Number == 14) { copy.Encounters = stage.Encounters.Skip(5).ToArray(); copy.Trials = null; }
            else if (stage.Number == 15) copy.Encounters = stage.Encounters.Skip(1).ToArray();
            else if (stage.Number == 18) copy.Encounters = new[] { stage.Encounters[0], stage.Encounters[2] };
            else if (stage.Number == 19) copy.Encounters = new[] { stage.Encounters[0], stage.Encounters[3], stage.Encounters[4] };
            return copy;
        }

        [TestCase(14)]
        [TestCase(15)]
        [TestCase(18)]
        [TestCase(19)]
        public void ExpandedFightsPayActualGrowthAtTheRaisedPreparationLevel(int number)
        {
            var stage = EarlyGameContent.FindStage(number);
            int expandedWins = 0, oldWins = 0;
            long addedExp = 0, addedDoo = 0;
            foreach (int seed in Enumerable.Range(401, 40))
            {
                int level = BalanceSimulation.BotLevel(stage);
                var current = BalanceSimulation.Starter(number, level);
                var previous = BalanceSimulation.Starter(number, level);
                ClearPlanningCache();
                var expandedRun = BalanceSimulation.Play(stage, current, seed, true);
                ClearPlanningCache();
                var oldRun = BalanceSimulation.Play(WithoutAddedFights(stage), previous, seed, true);
                if (expandedRun.Won) expandedWins++;
                if (oldRun.Won) oldWins++;
                if (!expandedRun.Won || !oldRun.Won) continue;
                Assert.GreaterOrEqual(current.Level, previous.Level);
                long currentExp = current.Exp, previousExp = previous.Exp;
                for (int earnedLevel = level; earnedLevel < current.Level; earnedLevel++) currentExp += Progression.ExpToNext(earnedLevel);
                for (int earnedLevel = level; earnedLevel < previous.Level; earnedLevel++) previousExp += Progression.ExpToNext(earnedLevel);
                Assert.Greater(currentExp, previousExp);
                Assert.Greater(current.Doo, previous.Doo);
                addedExp += currentExp - previousExp;
                addedDoo += current.Doo - previous.Doo;
            }
            TestContext.Out.WriteLine("Growth " + number + ": old/expanded wins " + oldWins + "/40 -> " + expandedWins
                + "/40; pooled added EXP " + addedExp + "; pooled added Doo " + addedDoo);
            Assert.GreaterOrEqual(expandedWins, 34);
            Assert.Greater(addedDoo, 0);
        }

        [TestCase(14, "maeng_yeonho")]
        [TestCase(14, "amryong_salsu")]
        [TestCase(14, "mak_jeok")]
        [TestCase(14, "heuk_sang")]
        [TestCase(14, "gal_hyobong")]
        [TestCase(15, "bikwae_ssangchang")]
        [TestCase(17, "pae_yugyeong")]
        [TestCase(17, "pae_hyeokseong")]
        [TestCase(18, "hyo_ryong_rope")]
        [TestCase(19, "hwan_geomseong")]
        [TestCase(19, "hwan_gal_hyobong")]
        public void AddedOpponentsUseTheSharedDifficultyAndRecommendationPaths(int number, string id)
        {
            var stage = EarlyGameContent.FindStage(number);
            var raw = EarlyGameContent.FindEnemy(id);
            Assert.Contains(id, stage.Encounters.SelectMany(fight => fight).ToArray());
            Assert.Less(raw.HpMul, 10, "The opponent must not accidentally take the fixed survival-enemy bypass");
            Assert.Greater(StageStars.NormalHarden(number), 1);
            Assert.Greater(StageStars.NormalExtraEdgeLevels(number), 0);
            EnemyData prior = null;
            for (int difficulty = 0; difficulty < StageStars.DifficultyCount; difficulty++)
            {
                var scaledStage = stage.WithDifficulty(difficulty);
                var actual = scaledStage.EnemyFor(id);
                var shared = StageStars.ScaleEnemy(raw, number, difficulty);
                Assert.AreEqual(shared.HpMul, actual.HpMul);
                Assert.AreEqual(shared.AtkMul, actual.AtkMul);
                Assert.AreEqual(shared.BuildStats().MaxHp, actual.BuildStats().MaxHp);
                if (difficulty == 0)
                {
                    Assert.Greater(actual.HpMul, raw.HpMul);
                    Assert.Greater(actual.AtkMul, raw.AtkMul * StageStars.NormalStatEaseFor(number));   // 일반 attack is eased from stage 7
                }
                else Assert.Greater(actual.Level, raw.Level);   // harder: the same enemy raised by level (HP traded for attack to keep the fight length)
                if (prior != null)
                {
                    Assert.Greater(actual.BuildStats().MaxHp, prior.BuildStats().MaxHp);
                    Assert.Greater(actual.BuildStats().EffAttack, prior.BuildStats().EffAttack);
                    Assert.Greater(StageStars.FastLimit(number, 0, difficulty), StageStars.FastLimit(number, 0, difficulty - 1));
                }
                prior = actual;
            }
        }
    }
}
