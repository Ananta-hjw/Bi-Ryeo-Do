using System;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StoryCompactionFifthPairTests
    {
        [Test]
        public void PursuitSetupAndDistrustOutcomeRemainInTheirCorrectStoryOrder()
        {
            var dogs = EarlyGameContent.FindStage(62);
            var sects = EarlyGameContent.FindStage(65);
            Assert.AreEqual(3, dogs.Encounters.Length); Assert.AreEqual(3, sects.Encounters.Length);
            Assert.IsFalse(dogs.Encounters.SelectMany(x => x).Contains("tracking_scent"));
            Assert.IsFalse(sects.Encounters.SelectMany(x => x).Contains("distrust_corridor"));
            string pursuit = string.Join(" ", StoryEpisodes.BeforeFight(dogs, 0, new SaveData()).SelectMany(s => s.Lines));
            Assert.Less(pursuit.IndexOf("긴장의 냄새", StringComparison.Ordinal), pursuit.IndexOf("최와와의 제삼대", StringComparison.Ordinal));
            string setup = string.Join(" ", StoryEpisodes.BeforeFight(sects, 0, new SaveData()).SelectMany(s => s.Lines));
            StringAssert.DoesNotContain("나전의 검이 흑저를 베었다", setup);
            string result = string.Join(" ", StoryEpisodes.BeforeFight(sects, 1, new SaveData()).SelectMany(s => s.Lines));
            Assert.Less(result.IndexOf("나전의 검이 흑저를 베었다", StringComparison.Ordinal), result.IndexOf("지나치게 쉬운 통과", StringComparison.Ordinal));
            StringAssert.Contains("암시 봉인", result);
            Assert.IsNotNull(EarlyGameContent.FindArt("umin_gunmu"));
            foreach (var stage in new[] { dogs, sects })
            {
                Assert.AreEqual(stage.Encounters.Length, stage.StoryActors.Length);
                for (int i = 0; i < stage.Encounters.Length; i++)
                {
                    var party = Roster.CreateStoryParty(stage, i, new SaveData());
                    Assert.IsFalse(party.Any(m => m.Skills.Any(s => s.Id == "scene_escape_laughter" || s.Id == "scene_escape_distrust")));
                    var rule = stage.TrialFor(i);
                    if (rule != null && rule.Kind == TrialKind.Technique)
                        Assert.IsTrue(party.Any(m => m.Skills.Any(s => s.Id == rule.TechniqueId)), stage.Number + "/" + i);
                }
            }
            CollectionAssert.AreEqual(new[] { "third_dog_squad", "third_dog_squad", "third_dog_squad" }, Quests.ToStage(Quests.Find("q_shadow_deception")).Encounters[0]);
            CollectionAssert.AreEqual(new[] { "dae_nan_current" }, Quests.ToStage(Quests.Find("q_current_guard")).Encounters[0]);
        }

        [Test]
        public void RevisionThirteenKeepsAllEarnedStarsAndEveryFinalClearOnce()
        {
            var save = new SaveData { StoryContentRevision = 12, ClearedStage = 65, Level = 70, Doo = 12345 };
            StageStars.Record(save, 48, 2, 0, 3);
            for (int d = 0; d < 3; d++)
                foreach (int number in new[] { 62, 65 })
                    for (int i = 0; i < 4; i++) StageStars.Record(save, number, i, d, 7);
            int total = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(total, StageStars.Total(save)); Assert.AreEqual(18, save.RetiredStoryStars);
            Assert.AreEqual(3, StageStars.MaskOf(save, 48, 2, 0));
            for (int d = 0; d < 3; d++)
                foreach (int number in new[] { 62, 65 }) Assert.IsTrue(StageStars.StageCleared(save, number, d));
            Assert.AreEqual(12345, save.Doo); Assert.AreEqual(70, save.Level);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(total, StageStars.Total(save)); Assert.AreEqual(18, save.RetiredStoryStars);
        }

        [Test]
        public void RemovedSlotsDoNotClaimTheNextClueOrCapture()
        {
            var save = new SaveData { StoryContentRevision = 12 };
            StageStars.Record(save, 62, 0, 1, 7); StageStars.Record(save, 62, 1, 1, 3);
            StageStars.Record(save, 65, 1, 2, 5); StageStars.Record(save, 65, 2, 2, 3);
            int total = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(total, StageStars.Total(save)); Assert.AreEqual(5, save.RetiredStoryStars);
            Assert.AreEqual(3, StageStars.MaskOf(save, 62, 0, 1));
            Assert.AreEqual(0, StageStars.MaskOf(save, 62, 1, 1));
            Assert.AreEqual(0, StageStars.MaskOf(save, 65, 0, 2));
            Assert.AreEqual(3, StageStars.MaskOf(save, 65, 1, 2));
            Assert.IsFalse(StageStars.StageCleared(save, 62, 1));
            Assert.IsFalse(StageStars.StageCleared(save, 65, 2));
            Assert.AreEqual(0, Inventory.Count(save, InnerArts.StoneId));
        }

        [TestCase(62, 1, 0)] [TestCase(62, 3, 2)]
        [TestCase(65, 1, 1)] [TestCase(65, 2, 1)] [TestCase(65, 3, 2)]
        [TestCase(48, 3, 3)]
        public void ResumeKeepsWoundsAndMapsOnlyTheAffectedChapters(int number, int oldNext, int next)
        {
            var save = new SaveData { StoryContentRevision = 12, ProgressStage = number, ProgressNext = oldNext, ProgressDifficulty = 2 };
            save.ProgressHp.Add(new MemberHp { Id = "player", Hp = 123, Qi = 17, HasQi = true, Spirit = 29 });
            StoryContentMigration.Apply(save);
            Assert.AreEqual(next, save.ProgressNext); Assert.AreEqual(number, save.ProgressStage);
            Assert.AreEqual(2, save.ProgressDifficulty); Assert.IsTrue(StageProgress.Has(save, number));
            Assert.AreEqual(123, save.ProgressHp[0].Hp); Assert.AreEqual(17, save.ProgressHp[0].Qi);
            Assert.AreEqual(29, save.ProgressHp[0].Spirit);
        }

        [TestCase(62, 0)] [TestCase(62, 1)] [TestCase(62, 2)]
        [TestCase(65, 0)] [TestCase(65, 1)] [TestCase(65, 2)]
        public void ShortenedChaptersKeepTheirExperienceAndCurrencyBudgets(int number, int difficulty)
        {
            var stage = EarlyGameContent.FindStage(number).WithDifficulty(difficulty);
            var legacy = new StageData { Number = number, RecommendedLevel = stage.RecommendedLevel,
                DooTotal = stage.DooTotal, RewardWeightBudget = stage.RewardWeightBudget, Difficulty = difficulty,
                Encounters = number == 62 ? new[] { new[] { "tracking_scent" }, new[] { "third_dog_squad", "third_dog_squad", "third_dog_squad" }, new[] { "baekgu_signal" }, new[] { "heukgyeon_captive" } }
                    : new[] { new[] { "three_sects" }, new[] { "distrust_corridor" }, new[] { "sealed_hint" }, new[] { "dae_nan_current" } } };
            Recommend.StageReward(new SaveData(), legacy, out long oldExp, out long oldDoo);
            Recommend.StageReward(new SaveData(), stage, out long exp, out long doo);
            Assert.That(Math.Abs(exp - oldExp), Is.LessThanOrEqualTo(20));
            Assert.That(Math.Abs(doo - oldDoo), Is.LessThanOrEqualTo(100));
        }
    }
}
