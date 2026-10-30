using System;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StoryCompactionTests
    {
        [Test]
        public void FillerEnemiesDisappearWhilePrivateTrialsAndTheWhiteWindRemain()
        {
            var trial = EarlyGameContent.FindStage(19);
            Assert.AreEqual(6, trial.Encounters.Length);
            CollectionAssert.AreEqual(new[] { "hwan_geomseong", "hwan_gal_hyobong", "hwan_family_duty", "hwan_nameless_blade", "hwan_sukbu", "hwan_sabu" }, trial.Encounters.SelectMany(x => x));
            string replay = string.Join(" ", StoryEpisodes.Replay(trial));
            StringAssert.Contains("그 아이를 베는 대신 손을 내미는 것이 출구", replay);
            StringAssert.Contains("야광주 목걸이", replay);
            StringAssert.Contains("책임은 가해자에게", replay);
            var bank = EarlyGameContent.FindStage(31);
            Assert.AreEqual(3, bank.Encounters.Length);
            Assert.IsFalse(bank.Encounters.SelectMany(x => x).Contains("caravan_ambush"));
            Assert.AreEqual(bank.Encounters.Length, bank.StoryActors.Length);
            string ending = string.Join(" ", bank.Outro);
            Assert.Less(ending.IndexOf("유재룡은 그 손에 죽었다", StringComparison.Ordinal), ending.IndexOf("하얀 질풍", StringComparison.Ordinal));
            StringAssert.Contains("유란·유운비가 실전 승리를 거둔 것이 아니었다", ending);
            StringAssert.Contains("한 마리만", ending);
            Assert.AreEqual("hwan_nameless_blade", Quests.ToStage(Quests.Find("q_illusion_breath")).Encounters[0][0]);
        }

        [Test]
        public void RevisionNinePreservesEveryEarnedStarAndDifficultyClearExactlyOnce()
        {
            var save = new SaveData { StoryContentRevision = 8, ClearedStage = 31, Doo = 12345, Level = 40 };
            foreach (int number in new[] { 19, 31 })
                for (int d = 0; d < 3; d++)
                    for (int i = 0; i < (number == 19 ? 7 : 4); i++) StageStars.Record(save, number, i, d, 7);
            int before = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(before, StageStars.Total(save));
            Assert.AreEqual(18, save.RetiredStoryStars);
            foreach (int number in new[] { 19, 31 })
                for (int d = 0; d < 3; d++) Assert.IsTrue(StageStars.StageCleared(save, number, d));
            Assert.AreEqual(12345, save.Doo); Assert.AreEqual(40, save.Level); Assert.AreEqual(31, save.ClearedStage);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(before, StageStars.Total(save)); Assert.AreEqual(18, save.RetiredStoryStars);
        }

        [Test]
        public void PartialMasksMergeWithoutLosingStarsOrClaimingUnplayedTrials()
        {
            var save = new SaveData { StoryContentRevision = 8 };
            StageStars.Record(save, 19, 0, 1, 7); StageStars.Record(save, 19, 2, 1, 3);
            StageStars.Record(save, 31, 2, 2, 3); StageStars.Record(save, 31, 3, 2, 5);
            int before = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(before, StageStars.Total(save));
            Assert.AreEqual(4, save.RetiredStoryStars);
            Assert.AreEqual(0, StageStars.MaskOf(save, 19, 0, 1));
            Assert.AreEqual(3, StageStars.MaskOf(save, 19, 1, 1));
            Assert.AreEqual(7, StageStars.MaskOf(save, 31, 2, 2));
        }

        [TestCase(19, 1, 0, false)] [TestCase(19, 6, 5, false)]
        [TestCase(31, 2, 2, false)] [TestCase(31, 3, 3, true)]
        [TestCase(58, 1, 1, false)]
        public void SavedResumeKeepsWoundsAndSkipsAnAlreadyCompletedFinalBattle(int number, int oldNext, int newNext, bool ending)
        {
            var save = new SaveData { StoryContentRevision = 8, ProgressStage = number, ProgressNext = oldNext, ProgressDifficulty = 2 };
            save.ProgressHp.Add(new MemberHp { Id = "player", Hp = 123, Qi = 17, HasQi = true, Spirit = 29 });
            StoryContentMigration.Apply(save);
            Assert.AreEqual(number, save.ProgressStage); Assert.AreEqual(newNext, save.ProgressNext);
            Assert.AreEqual(2, save.ProgressDifficulty);
            Assert.AreEqual(123, save.ProgressHp[0].Hp); Assert.AreEqual(17, save.ProgressHp[0].Qi);
            Assert.AreEqual(29, save.ProgressHp[0].Spirit);
            Assert.IsTrue(StageProgress.Has(save, number));
            Assert.AreEqual(ending, StageProgress.AtEnding(save, EarlyGameContent.FindStage(number)));
        }

        [Test]
        public void PreStarSavesKeepTheirImplicitNormalStarsAfterBackfill()
        {
            var save = new SaveData { StoryContentRevision = 8, ClearedStage = 31 };
            StoryContentMigration.Apply(save); StageStars.Sync(save);
            Assert.AreEqual(6, StageStars.StarsOfStage(save, 19, 0));
            Assert.AreEqual(3, StageStars.StarsOfStage(save, 31, 0));
            Assert.AreEqual(2, save.RetiredStoryStars);
            int total = StageStars.Total(save);
            StageStars.Sync(save); StoryContentMigration.Apply(save);
            Assert.AreEqual(total, StageStars.Total(save));
        }

        [TestCase(19, 0)] [TestCase(19, 1)] [TestCase(19, 2)]
        [TestCase(31, 0)] [TestCase(31, 1)] [TestCase(31, 2)]
        public void ShortenedChaptersRedistributeTheOriginalExperienceAndCurrencyBudget(int number, int difficulty)
        {
            var stage = EarlyGameContent.FindStage(number).WithDifficulty(difficulty);
            var legacy = new StageData { Number = number, RecommendedLevel = stage.RecommendedLevel, DooTotal = stage.DooTotal,
                RewardWeightBudget = stage.RewardWeightBudget, Difficulty = difficulty,
                Encounters = number == 19 ? new[] { new[] { "hwan_ying", "hwan_ying", "hwan_ying" }, new[] { "hwan_geomseong" }, new[] { "hwan_gal_hyobong" }, new[] { "hwan_family_duty" }, new[] { "hwan_nameless_blade" }, new[] { "hwan_sukbu", "hwan_ying" }, new[] { "hwan_sabu" } }
                    : new[] { new[] { "salryong_daewon", "salryong_daewon", "salryong_daewon" }, new[] { "cheongryong_escape" }, new[] { "salryong_yun" }, new[] { "caravan_ambush" } } };
            Recommend.StageReward(new SaveData(), legacy, out long oldExp, out long oldDoo);
            Recommend.StageReward(new SaveData(), stage, out long exp, out long doo);
            Assert.That(Math.Abs(exp - oldExp), Is.LessThanOrEqualTo(10));
            // Currency is rounded per fight again by the income boost.
            Assert.That(Math.Abs(doo - oldDoo), Is.LessThanOrEqualTo(100));
        }

        [TestCase(0, false)] [TestCase(1, false)] [TestCase(2, false)]
        [TestCase(1, true)] [TestCase(2, true)]
        public void PromotedFinalBattleCreditsOnlyPreviouslyUnclaimedClearInsight(int difficulty, bool oldFinalWon)
        {
            var save = new SaveData { StoryContentRevision = 8 };
            StageStars.Record(save, 31, 2, difficulty, 7);
            if (oldFinalWon) StageStars.Record(save, 31, 3, difficulty, 7);
            StoryContentMigration.Apply(save);
            int expected = oldFinalWon ? 0 : StageStars.ClearInsight(31, difficulty);
            Assert.AreEqual(expected, Inventory.Count(save, InnerArts.StoneId));
            StoryContentMigration.Apply(save);
            Assert.AreEqual(expected, Inventory.Count(save, InnerArts.StoneId));
        }
    }
}
