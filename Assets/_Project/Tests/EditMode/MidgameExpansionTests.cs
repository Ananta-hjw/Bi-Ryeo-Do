using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class MidgameExpansionTests
    {
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
            Assert.AreEqual(31, EarlyGameContent.Stages.Length);
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
            for (int fight = 0; fight < stage.Encounters.Length; fight++)
            {
                var actions = runs.Where(run => run.Won).Select(run => run.FightActions[fight]).OrderBy(x => x).ToArray();
                TestContext.Out.WriteLine("Fight " + fight + ": median " + actions[actions.Length / 2] + "; limit " + StageStars.FastLimit(number, fight));
            }
            Assert.GreaterOrEqual(wins, 34);
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

        [Test]
        public void AddedFightRewardsImproveContinuousProgressionWithoutTrivializingTheNextChapter()
        {
            int[] wins = new int[2];
            double[] hp = new double[2];
            var levels = new System.Collections.Generic.List<int>();
            for (int expanded = 0; expanded <= 1; expanded++)
            {
                foreach (int seed in Enumerable.Range(401, 40))
                {
                    var first = EarlyGameContent.FindStage(14);
                    var save = BalanceSimulation.Starter(14, BalanceSimulation.BotLevel(first));
                    bool won = true;
                    for (int number = 14; number <= 21; number++)
                    {
                        BalanceSimulation.BuyLoadout(save, number);
                        var stage = EarlyGameContent.FindStage(number);
                        var result = BalanceSimulation.Play(expanded == 1 ? stage : WithoutAddedFights(stage), save, seed, true);
                        if (!result.Won) { won = false; break; }
                        save.ClearedStage = number;
                        if (number == 21) hp[expanded] += result.HpLeft;
                    }
                    if (won)
                    {
                        wins[expanded]++;
                        if (expanded == 1) levels.Add(save.Level);
                    }
                }
            }
            TestContext.Out.WriteLine("Campaign without/with added fights: " + wins[0] + "/40 -> " + wins[1] + "/40; ending levels "
                + string.Join(",", levels.Distinct()) + "; next-chapter HP " + hp[1] / System.Math.Max(1, wins[1]));
            Assert.Greater(wins[1], 0);
            Assert.GreaterOrEqual(wins[1], wins[0]);
            Assert.IsTrue(levels.All(level => level > BalanceSimulation.BotLevel(EarlyGameContent.FindStage(14))));
        }
    }
}
