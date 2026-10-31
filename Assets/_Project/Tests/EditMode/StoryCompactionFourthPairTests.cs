using System;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StoryCompactionFourthPairTests
    {
        [Test]
        public void ArmorChecksBecomeTheEndingAndBoardingIntroducesThePursuit()
        {
            var armor = EarlyGameContent.FindStage(48);
            var lake = EarlyGameContent.FindStage(57);
            Assert.AreEqual(3, armor.Encounters.Length); Assert.AreEqual(1, lake.Encounters.Length);
            Assert.IsFalse(armor.Encounters.SelectMany(x => x).Contains("rebound_second"));
            CollectionAssert.AreEqual(new[] { "lake_pursuit" }, lake.Encounters[0]);
            Assert.AreEqual(armor.Encounters.Length, armor.StoryActors.Length);
            Assert.AreEqual(lake.Encounters.Length, lake.StoryActors.Length);
            string ending = string.Join(" ", armor.Outro);
            Assert.Less(ending.IndexOf("아주 느린 주먹", StringComparison.Ordinal), ending.IndexOf("두 번째 주먹", StringComparison.Ordinal));
            StringAssert.Contains("락비오는 금 밖으로 밀려나지 않았다", ending);
            StringAssert.Contains("다섯 대를 앞뒀다", ending);
            StringAssert.DoesNotContain("반응폭약", ending);
            var save = new SaveData();
            for (int i = 0; i < armor.Encounters.Length; i++)
                Assert.IsFalse(BalanceSimulation.StoryParty(armor, i, save).Any(m => m.Skills.Any(s => s.Id == "scene_second_fist" || s.Id == "scene_slow_fist")));
            var party = BalanceSimulation.StoryParty(lake, 0, save);
            Assert.IsFalse(party.Any(m => m.Skills.Any(s => s.Id == "scene_umbrella_boat")));
            Assert.IsTrue(party.Any(m => m.Skills.Any(s => s.Id == lake.TrialFor(0).TechniqueId)));
            string intro = string.Join(" ", StoryEpisodes.BeforeFight(lake, 0, save).SelectMany(s => s.Lines));
            Assert.Less(intro.IndexOf("현천은린에 올라", StringComparison.Ordinal), intro.IndexOf("당문혜가 당삼을 부탁했다", StringComparison.Ordinal));
            Assert.Less(intro.IndexOf("당문혜가 당삼을 부탁했다", StringComparison.Ordinal), intro.IndexOf("추격선이 다시 둘러쌌다", StringComparison.Ordinal));
            StringAssert.Contains("왼쪽 발목의 묵룡환", intro);
        }

        [TestCase(10)] [TestCase(11)]
        public void OldAndCurrentCompactionSavesKeepEveryEarnedStarAndClear(int revision)
        {
            var save = new SaveData { StoryContentRevision = revision, ClearedStage = 57, Doo = 12345, Level = 65 };
            for (int d = 0; d < 3; d++)
            {
                for (int i = 0; i < (revision == 10 ? 5 : 4); i++) StageStars.Record(save, 48, i, d, 7);
                for (int i = 0; i < 2; i++) StageStars.Record(save, 57, i, d, 7);
            }
            int total = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(total, StageStars.Total(save));
            Assert.AreEqual(revision == 10 ? 27 : 18, save.RetiredStoryStars);
            for (int d = 0; d < 3; d++)
            {
                Assert.IsTrue(StageStars.StageCleared(save, 48, d));
                Assert.IsTrue(StageStars.StageCleared(save, 57, d));
            }
            Assert.AreEqual(12345, save.Doo); Assert.AreEqual(65, save.Level);
            StoryContentMigration.Apply(save); Assert.AreEqual(total, StageStars.Total(save));
        }

        [TestCase(48, 3, 3, true)] [TestCase(57, 1, 0, false)] [TestCase(47, 0, 0, false)]
        public void SavedResumePreservesWoundsAndSkipsOnlyDeletedContent(int number, int oldNext, int next, bool ending)
        {
            var save = new SaveData { StoryContentRevision = 11, ProgressStage = number, ProgressNext = oldNext, ProgressDifficulty = 2 };
            save.ProgressHp.Add(new MemberHp { Id = "player", Hp = 123, Qi = 17, HasQi = true, Spirit = 29 });
            StoryContentMigration.Apply(save);
            Assert.AreEqual(next, save.ProgressNext); Assert.AreEqual(number, save.ProgressStage);
            Assert.AreEqual(2, save.ProgressDifficulty); Assert.IsTrue(StageProgress.Has(save, number));
            Assert.AreEqual(123, save.ProgressHp[0].Hp); Assert.AreEqual(17, save.ProgressHp[0].Qi);
            Assert.AreEqual(29, save.ProgressHp[0].Spirit);
            Assert.AreEqual(ending, StageProgress.AtEnding(save, EarlyGameContent.FindStage(number)));
        }

        [TestCase(0, false)] [TestCase(1, false)] [TestCase(2, false)] [TestCase(2, true)]
        public void RealFinalFightKeepsItsMaskAndReceivesOnlyUnpaidClearInsight(int difficulty, bool lastWon)
        {
            var save = new SaveData { StoryContentRevision = 11 };
            StageStars.Record(save, 48, 2, difficulty, 3);
            if (lastWon) StageStars.Record(save, 48, 3, difficulty, 5);
            StageStars.Record(save, 57, 0, difficulty, 7);
            int total = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(total, StageStars.Total(save));
            Assert.AreEqual(lastWon ? 7 : 3, StageStars.MaskOf(save, 48, 2, difficulty));
            Assert.IsTrue(StageStars.StageCleared(save, 48, difficulty));
            Assert.IsFalse(StageStars.StageCleared(save, 57, difficulty));
            int expected = lastWon ? 0 : StageStars.ClearInsight(48, difficulty);
            Assert.AreEqual(expected, Inventory.Count(save, InnerArts.StoneId));
            StoryContentMigration.Apply(save);
            Assert.AreEqual(expected, Inventory.Count(save, InnerArts.StoneId));
        }

        [TestCase(48, 0)] [TestCase(48, 1)] [TestCase(48, 2)]
        [TestCase(57, 0)] [TestCase(57, 1)] [TestCase(57, 2)]
        public void RetainedFightsPreserveThePreviousChapterRewardBudget(int number, int difficulty)
        {
            var stage = EarlyGameContent.FindStage(number).WithDifficulty(difficulty);
            var legacy = new StageData { Number = number, RecommendedLevel = stage.RecommendedLevel,
                DooTotal = stage.DooTotal, RewardWeightBudget = stage.RewardWeightBudget,
                Difficulty = difficulty, RewardScale = number == 48 ? 13.0 / 12.0 : 1.0,
                Encounters = number == 48 ? new[] { new[] { "academy_alarm_guard", "academy_alarm_guard", "academy_alarm_guard" }, new[] { "cheolgap_front", "cheolgap_front" }, new[] { "cheolgap_rear", "cheolgap_rear" }, new[] { "rebound_second" } }
                    : new[] { new[] { "lake_crossing" }, new[] { "lake_pursuit" } } };
            Recommend.StageReward(new SaveData(), legacy, out long oldExp, out long oldDoo);
            Recommend.StageReward(new SaveData(), stage, out long exp, out long doo);
            // Per-enemy ceilings are scaled again by the harder-difficulty reward factor.
            Assert.That(Math.Abs(exp - oldExp), Is.LessThanOrEqualTo(20));
            Assert.That(Math.Abs(doo - oldDoo), Is.LessThanOrEqualTo(100));
        }
    }
}
