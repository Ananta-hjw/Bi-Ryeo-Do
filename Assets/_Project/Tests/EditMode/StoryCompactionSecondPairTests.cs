using System;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StoryCompactionSecondPairTests
    {
        [Test]
        public void TrainingBecomesAFlashbackAndTheChildrensRescueRemainsInTheEnding()
        {
            var training = EarlyGameContent.FindStage(32);
            var dawn = EarlyGameContent.FindStage(35);
            Assert.AreEqual(3, training.Encounters.Length);
            Assert.AreEqual(4, dawn.Encounters.Length);
            Assert.IsFalse(training.Encounters.SelectMany(x => x).Contains("ichon_training"));
            Assert.IsFalse(dawn.Encounters.SelectMany(x => x).Contains("closed_room_threat"));
            Assert.AreEqual(training.Encounters.Length, training.StoryActors.Length);
            Assert.AreEqual(dawn.Encounters.Length, dawn.StoryActors.Length);
            string intro = string.Join(" ", StoryEpisodes.BeforeFight(training, 0, new SaveData()).SelectMany(s => s.Lines));
            StringAssert.Contains("일곱 연속 비도", intro);
            StringAssert.Contains("정체를 확신하지 못했다", intro);
            string ending = string.Join(" ", dawn.Outro);
            StringAssert.Contains("용린폭 상처", ending);
            StringAssert.Contains("소년은 선아를 감쌌다", ending);
            Assert.Less(ending.IndexOf("용린폭 상처", StringComparison.Ordinal), ending.IndexOf("백무후가 문을 부수고", StringComparison.Ordinal));
            StringAssert.Contains("윤이정의 최종 생사는 남았다", ending);
        }

        [Test]
        public void RevisionTenKeepsStarsAndClearCreditWithoutReapplyingRevisionNine()
        {
            var save = new SaveData { StoryContentRevision = 9, ClearedStage = 35, Level = 40, Doo = 12345 };
            StageStars.Record(save, 19, 0, 0, 3);
            StageStars.Record(save, 31, 2, 1, 5);
            for (int d = 0; d < 3; d++)
                foreach (int number in new[] { 32, 35 })
                    for (int i = 0; i < (number == 32 ? 4 : 5); i++) StageStars.Record(save, number, i, d, 7);
            int before = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(before, StageStars.Total(save));
            Assert.AreEqual(18, save.RetiredStoryStars);
            Assert.AreEqual(3, StageStars.MaskOf(save, 19, 0, 0));
            Assert.AreEqual(5, StageStars.MaskOf(save, 31, 2, 1));
            for (int d = 0; d < 3; d++)
                foreach (int number in new[] { 32, 35 }) Assert.IsTrue(StageStars.StageCleared(save, number, d));
            Assert.AreEqual(12345, save.Doo); Assert.AreEqual(40, save.Level);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(before, StageStars.Total(save)); Assert.AreEqual(18, save.RetiredStoryStars);
        }

        [TestCase(32, 1, 0, false)] [TestCase(32, 3, 2, false)]
        [TestCase(35, 3, 3, false)] [TestCase(35, 4, 4, true)]
        [TestCase(31, 3, 3, true)]
        public void SavedResumeKeepsWoundsAndMapsDeletedSlots(int number, int oldNext, int next, bool ending)
        {
            var save = new SaveData { StoryContentRevision = 9, ProgressStage = number, ProgressNext = oldNext, ProgressDifficulty = 1 };
            save.ProgressHp.Add(new MemberHp { Id = "player", Hp = 123, Qi = 17, HasQi = true, Spirit = 29 });
            StoryContentMigration.Apply(save);
            Assert.AreEqual(next, save.ProgressNext); Assert.AreEqual(number, save.ProgressStage);
            Assert.AreEqual(1, save.ProgressDifficulty);
            Assert.AreEqual(123, save.ProgressHp[0].Hp); Assert.AreEqual(17, save.ProgressHp[0].Qi);
            Assert.AreEqual(29, save.ProgressHp[0].Spirit);
            Assert.IsTrue(StageProgress.Has(save, number));
            Assert.AreEqual(ending, StageProgress.AtEnding(save, EarlyGameContent.FindStage(number)));
        }

        [TestCase(0, false)] [TestCase(1, false)] [TestCase(2, false)]
        [TestCase(0, true)] [TestCase(2, true)]
        public void NewlyPromotedFinalBattleReceivesItsUnpaidInsightOnlyOnce(int difficulty, bool finalWon)
        {
            var save = new SaveData { StoryContentRevision = 9 };
            StageStars.Record(save, 35, 3, difficulty, 3);
            if (finalWon) StageStars.Record(save, 35, 4, difficulty, 5);
            int stars = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            int expected = finalWon ? 0 : StageStars.ClearInsight(35, difficulty);
            Assert.AreEqual(expected, Inventory.Count(save, InnerArts.StoneId));
            Assert.AreEqual(stars, StageStars.Total(save));
            Assert.AreEqual(finalWon ? 7 : 3, StageStars.MaskOf(save, 35, 3, difficulty));
            StoryContentMigration.Apply(save);
            Assert.AreEqual(expected, Inventory.Count(save, InnerArts.StoneId));
        }

        [TestCase(32, 0)] [TestCase(32, 1)] [TestCase(32, 2)]
        [TestCase(35, 0)] [TestCase(35, 1)] [TestCase(35, 2)]
        public void CompactedChapterPreservesItsExperienceAndCurrencyBudget(int number, int difficulty)
        {
            var stage = EarlyGameContent.FindStage(number).WithDifficulty(difficulty);
            var legacy = new StageData { Number = number, RecommendedLevel = stage.RecommendedLevel,
                RewardWeightBudget = stage.RewardWeightBudget, DooTotal = stage.DooTotal, Difficulty = difficulty,
                Encounters = number == 32 ? new[] { new[] { "ichon_training" }, new[] { "yu_ran", "yu_unbi" }, new[] { "gwan_eogeom_first" }, new[] { "gwan_cheolsu_eogeom" } }
                    : new[] { new[] { "i_sigeon_bait" }, new[] { "i_sigeon_arrow" }, new[] { "sipsamhyeol", "sipsamhyeol" }, new[] { "il_hyeol" }, new[] { "closed_room_threat" } } };
            Recommend.StageReward(new SaveData(), legacy, out long oldExp, out long oldDoo);
            Recommend.StageReward(new SaveData(), stage, out long exp, out long doo);
            Assert.That(Math.Abs(exp - oldExp), Is.LessThanOrEqualTo(10));
            Assert.That(Math.Abs(doo - oldDoo), Is.LessThanOrEqualTo(100));
        }

        [Test]
        public void IntervalQuestUsesActualChallengersAndKeepsItsCompletionId()
        {
            var quest = Quests.Find("q_ichon");
            var stage = Quests.ToStage(quest);
            CollectionAssert.AreEqual(new[] { "yu_ran", "yu_unbi" }, stage.Encounters[0]);
            var save = new SaveData { StoryContentRevision = 9 };
            save.DoneQuests.Add("q_ichon"); StoryContentMigration.Apply(save);
            CollectionAssert.Contains(save.DoneQuests, "q_ichon");
            int wins = Enumerable.Range(1, 20).Count(seed => BalanceSimulation.Play(stage,
                BalanceSimulation.Starter(quest.RefStage + 1, BalanceSimulation.BotLevel(stage)), seed).Won);
            Assert.GreaterOrEqual(wins, 17);
        }
    }
}
