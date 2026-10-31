using System;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StoryCompactionThirdPairTests
    {
        [Test]
        public void TracesAndTheWeakContactRemainAsStoryBeforeTheirRetainedObjectives()
        {
            var search = EarlyGameContent.FindStage(47);
            var armor = EarlyGameContent.FindStage(48);
            Assert.AreEqual(1, search.Encounters.Length);
            Assert.AreEqual(3, armor.Encounters.Length);
            Assert.IsFalse(search.Encounters.SelectMany(x => x).Contains("vent_traces"));
            Assert.IsFalse(armor.Encounters.SelectMany(x => x).Contains("rebound_probe"));
            Assert.AreEqual(search.Encounters.Length, search.StoryActors.Length);
            Assert.AreEqual(armor.Encounters.Length, armor.StoryActors.Length);
            string traces = string.Join(" ", StoryEpisodes.BeforeFight(search, 0, new SaveData()).SelectMany(s => s.Lines));
            Assert.Less(traces.IndexOf("장홍의 흔적분", StringComparison.Ordinal), traces.IndexOf("검은 뿔 장식", StringComparison.Ordinal));
            StringAssert.Contains("나백천이 벽을 열고", traces);
            StringAssert.Contains("나예린이 저항하며 남긴 흔적", traces);
            string probe = string.Join(" ", armor.Outro);
            Assert.Less(probe.IndexOf("아주 느린 주먹", StringComparison.Ordinal), probe.IndexOf("두 번째 주먹", StringComparison.Ordinal));
            StringAssert.Contains("사실을 확인했다", probe);
            StringAssert.Contains("금 밖으로 밀려나지 않았다", probe);
            StringAssert.DoesNotContain("반응폭약", probe);
            StringAssert.DoesNotContain("락비오가 패배", probe);
            var save = new SaveData();
            Assert.IsFalse(BalanceSimulation.StoryParty(search, 0, save).Any(m => m.Skills.Any(s => s.Id == "scene_trace")));
            var party = BalanceSimulation.StoryParty(armor, 2, save);
            Assert.IsFalse(party.Any(m => m.Skills.Any(s => s.Id == "scene_slow_fist")));
            Assert.IsFalse(party.Any(m => m.Skills.Any(s => s.Id == "scene_second_fist")));
            CollectionAssert.AreEqual(new[] { "horn_trap" }, Quests.ToStage(Quests.Find("q_explosion_exit")).Encounters[0]);
        }

        [Test]
        public void RevisionElevenPreservesEarnedStarsAndEveryFinalClearExactlyOnce()
        {
            var save = new SaveData { StoryContentRevision = 10, ClearedStage = 48, Level = 60, Doo = 12345 };
            StageStars.Record(save, 32, 0, 0, 3); StageStars.Record(save, 35, 3, 2, 5);
            for (int d = 0; d < 3; d++)
                foreach (int n in new[] { 47, 48 })
                    for (int i = 0; i < (n == 47 ? 2 : 5); i++) StageStars.Record(save, n, i, d, 7);
            int total = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(total, StageStars.Total(save)); Assert.AreEqual(27, save.RetiredStoryStars);
            Assert.AreEqual(3, StageStars.MaskOf(save, 32, 0, 0));
            Assert.AreEqual(5, StageStars.MaskOf(save, 35, 3, 2));
            for (int d = 0; d < 3; d++)
                foreach (int n in new[] { 47, 48 }) Assert.IsTrue(StageStars.StageCleared(save, n, d));
            Assert.AreEqual(12345, save.Doo); Assert.AreEqual(60, save.Level);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(total, StageStars.Total(save)); Assert.AreEqual(27, save.RetiredStoryStars);
        }

        [Test]
        public void ACompletedRemovedProbeDoesNotClaimTheUnplayedRealFinalFight()
        {
            var save = new SaveData { StoryContentRevision = 10 };
            StageStars.Record(save, 47, 0, 1, 7); StageStars.Record(save, 48, 3, 2, 3);
            int total = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(total, StageStars.Total(save)); Assert.AreEqual(5, save.RetiredStoryStars);
            Assert.AreEqual(0, StageStars.MaskOf(save, 47, 0, 1));
            Assert.AreEqual(0, StageStars.MaskOf(save, 48, 3, 2));
            Assert.IsFalse(StageStars.StageCleared(save, 47, 1));
            Assert.IsFalse(StageStars.StageCleared(save, 48, 2));
            Assert.AreEqual(0, Inventory.Count(save, InnerArts.StoneId));
        }

        [TestCase(47, 1, 0)] [TestCase(48, 3, 3)] [TestCase(48, 4, 3)] [TestCase(35, 4, 4)]
        public void SavedResumeKeepsWoundsAndTargetsTheRetainedEncounter(int number, int oldNext, int next)
        {
            var save = new SaveData { StoryContentRevision = 10, ProgressStage = number, ProgressNext = oldNext, ProgressDifficulty = 2 };
            save.ProgressHp.Add(new MemberHp { Id = "player", Hp = 123, Qi = 17, HasQi = true, Spirit = 29 });
            StoryContentMigration.Apply(save);
            Assert.AreEqual(next, save.ProgressNext); Assert.AreEqual(number, save.ProgressStage);
            Assert.AreEqual(2, save.ProgressDifficulty); Assert.IsTrue(StageProgress.Has(save, number));
            Assert.AreEqual(123, save.ProgressHp[0].Hp); Assert.AreEqual(17, save.ProgressHp[0].Qi);
            Assert.AreEqual(29, save.ProgressHp[0].Spirit);
            var stage = EarlyGameContent.FindStage(number);
            if (number == 47)
            {
                Assert.IsFalse(StageProgress.AtEnding(save, stage));
                Assert.AreEqual("horn_trap", stage.Encounters[next][0]);
            }
            else if (number == 48) Assert.IsTrue(StageProgress.AtEnding(save, stage));
        }

        [TestCase(47, 0)] [TestCase(47, 1)] [TestCase(47, 2)]
        [TestCase(48, 0)] [TestCase(48, 1)] [TestCase(48, 2)]
        public void CompactedChapterRetainsItsExperienceAndCurrencyBudget(int number, int difficulty)
        {
            var stage = EarlyGameContent.FindStage(number).WithDifficulty(difficulty);
            var legacy = new StageData { Number = number, RecommendedLevel = stage.RecommendedLevel,
                RewardWeightBudget = stage.RewardWeightBudget, DooTotal = stage.DooTotal, Difficulty = difficulty,
                Encounters = number == 47 ? new[] { new[] { "vent_traces" }, new[] { "horn_trap" } }
                    : new[] { new[] { "academy_alarm_guard", "academy_alarm_guard", "academy_alarm_guard" }, new[] { "cheolgap_front", "cheolgap_front" }, new[] { "cheolgap_rear", "cheolgap_rear" }, new[] { "rebound_probe" }, new[] { "rebound_second" } } };
            Recommend.StageReward(new SaveData(), legacy, out long oldExp, out long oldDoo);
            Recommend.StageReward(new SaveData(), stage, out long exp, out long doo);
            Assert.That(Math.Abs(exp - oldExp), Is.LessThanOrEqualTo(10));
            Assert.That(Math.Abs(doo - oldDoo), Is.LessThanOrEqualTo(100));
        }
    }
}
