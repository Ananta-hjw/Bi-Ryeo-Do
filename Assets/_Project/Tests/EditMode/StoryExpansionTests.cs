using System;
using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StoryExpansionTests
    {
        [Test]
        public void ExpandedStoryUsesActualOpponentsWithoutRepeatedOrCombinedTournamentDuels()
        {
            var escort = EarlyGameContent.FindStage(2);
            Assert.AreEqual(5, escort.Encounters.Length);
            CollectionAssert.AreEqual(new[] { "deung_yeoun", "deung_yeoho" }, escort.Encounters[3]);
            CollectionAssert.AreEqual(new[] { "jang_uyang" }, escort.Encounters[4]);
            var tournament = EarlyGameContent.FindStage(12);
            CollectionAssert.AreEqual(new[] { "dan_pyeong", "jo_yeonil", "jin_pae", "ilyeon", "eonga_yeong", "jeon_okgi" }, tournament.Encounters.SelectMany(x => x));
            Assert.IsTrue(tournament.Encounters.All(x => x.Length == 1));
            CollectionAssert.AreEqual(new[] { "dang_munceon" }, EarlyGameContent.FindStage(13).Encounters[0]);
            Assert.AreEqual(36, EarlyGameContent.Stages.Length, "Persisted stage and unlock IDs remain stable");
        }

        [Test]
        public void SaveMigrationPreservesEarnedStarsBossWinsAndProgressionAndIsIdempotent()
        {
            var save = new SaveData { StoryContentRevision = 0, ClearedStage = 13, Level = 18, Doo = 12345, ProgressStage = 12, ProgressNext = 1 };
            save.StageStars.AddRange(new[] {
                new StarEntry { Stage = 2, Fight = 2, Diff = 1, Mask = 7 },
                new StarEntry { Stage = 11, Fight = 0, Mask = 7 },
                new StarEntry { Stage = 11, Fight = 1, Mask = 3 },
                new StarEntry { Stage = 11, Fight = 2, Mask = 7 },
                new StarEntry { Stage = 12, Fight = 2, Diff = 2, Mask = 7 } });
            int stars = StageStars.Total(save);
            save.ClaimedStarChests.Add(15);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(stars, StageStars.Total(save));
            Assert.AreEqual(7, StageStars.MaskOf(save, 2, 4, 1));
            Assert.AreEqual(7, StageStars.MaskOf(save, 12, 5, 2));
            Assert.IsTrue(StageStars.StageCleared(save, 11, 0));
            Assert.AreEqual(0, save.ProgressStage);
            Assert.AreEqual(13, save.ClearedStage);
            Assert.AreEqual(18, save.Level);
            Assert.AreEqual(12345, save.Doo);
            CollectionAssert.Contains(save.ClaimedStarChests, 15);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(stars, StageStars.Total(save));
            Assert.AreEqual(5, save.RetiredStoryStars);
            var fresh = new SaveData { StoryContentRevision = 0 }; StoryContentMigration.Apply(fresh);
            Assert.AreEqual(0, StageStars.Total(fresh));
        }

        [Test]
        public void FormerSideStoriesAreRequiredMainStageFightsWithNormalRewardsAndResume()
        {
            var admission = EarlyGameContent.FindStage(7);
            Assert.AreEqual(8, admission.Encounters.Length);
            CollectionAssert.AreEqual(new[] { "admit_jeomchang", "admit_hwasan", "admit_geumbok", "admit_eonga" }, admission.Encounters.Take(4).SelectMany(x => x));
            CollectionAssert.AreEqual(new[] { "no_jusaeng", "chae_hagun" }, EarlyGameContent.FindStage(11).Encounters[0]);
            var final = EarlyGameContent.FindStage(13);
            Assert.AreEqual(4, final.Encounters.Length);
            Assert.AreEqual(TrialKind.Survive, final.TrialFor(1).Kind);
            Assert.AreEqual(TrialKind.Survive, final.TrialFor(2).Kind);
            var save = BalanceSimulation.Starter(13, 20);
            StageProgress.Store(save, 13, 2, BalanceSimulation.Party(save, final));
            Assert.IsTrue(StageProgress.Has(save, 13));
            foreach (int number in new[] { 7, 11, 13 })
            {
                var stage = EarlyGameContent.FindStage(number);
                for (int fight = 0; fight < stage.Encounters.Length; fight++)
                {
                    Recommend.FightReward(save, stage, fight, false, out long exp, out long doo);
                    Assert.Greater(exp, 0); Assert.Greater(doo, 0);
                    Assert.IsFalse(stage.IsQuest || stage.IsTower);
                }
            }
        }

        [TestCase(2, 9.0 / 7.0)]
        [TestCase(7, 12.0 / 8.0)]
        [TestCase(11, 7.0 / 3.0)]
        [TestCase(12, 8.0 / 6.0)]
        [TestCase(13, 6.0 / 4.0)]
        public void AddedFightsIncreaseTheStageRewardRatherThanDilutingExistingPayouts(int number, double factor)
        {
            var stage = EarlyGameContent.FindStage(number);
            var baseline = stage.WithDifficulty(0); baseline.RewardWeightBudget = 0;
            var ids = stage.Encounters.SelectMany(x => x).ToList();
            double originalDoo = ids.Sum(id => EarlyGameContent.DooReward(baseline, id));
            double originalExp = ids.Sum(id => EarlyGameContent.ExpReward(baseline, id));
            Assert.That(ids.Sum(id => EarlyGameContent.DooReward(stage, id)), Is.EqualTo(originalDoo * factor).Within(ids.Count * factor));
            Assert.That(ids.Sum(id => EarlyGameContent.ExpReward(stage, id)), Is.EqualTo(originalExp * factor).Within(ids.Count * factor));
            Assert.Greater(factor, 1);
        }

        [Test]
        public void RevisionOneSaveMovesExistingBossWinsAndDoesNotDeleteNewRewardOpportunities()
        {
            var save = new SaveData { StoryContentRevision = 1, ClearedStage = 13, ProgressStage = 13, ProgressNext = 1 };
            save.StageStars.AddRange(new[] {
                new StarEntry { Stage = 7, Fight = 3, Mask = 7 },
                new StarEntry { Stage = 11, Fight = 0, Mask = 7 },
                new StarEntry { Stage = 13, Fight = 1, Diff = 1, Mask = 7 } });
            StoryContentMigration.Apply(save);
            Assert.AreEqual(7, StageStars.MaskOf(save, 7, 7, 0));
            Assert.AreEqual(7, StageStars.MaskOf(save, 11, 3, 0));
            Assert.AreEqual(7, StageStars.MaskOf(save, 13, 3, 1));
            Assert.AreEqual(0, StageStars.MaskOf(save, 7, 0, 0));
            Assert.AreEqual(9, StageStars.Total(save));
            Assert.AreEqual(0, save.ProgressStage);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(9, StageStars.Total(save));
        }

        [TestCase(2)]
        [TestCase(7)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        public void ExpandedMainStoryRemainsClearableAtThePreparedLevel(int number)
        {
            var stage = EarlyGameContent.FindStage(number);
            var runs = Enumerable.Range(301, 40).Select(seed => BalanceSimulation.Play(stage,
                BalanceSimulation.Starter(number, BalanceSimulation.BotLevel(stage)), seed)).ToList();
            int wins = runs.Count(x => x.Won);
            TestContext.Out.WriteLine(number + ": " + wins + "/40, actions " + runs.Where(x => x.Won).Select(x => x.Actions).DefaultIfEmpty().Average() + ", HP " + runs.Where(x => x.Won).Select(x => x.HpLeft).DefaultIfEmpty().Average());
            Assert.GreaterOrEqual(wins, 34);
        }

    }
}
