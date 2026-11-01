using System;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StoryCompactionSixthPairTests
    {
        [Test]
        public void RiverEntryAndFatherEvacuationFollowTheRetainedFinalEncounters()
        {
            var river = EarlyGameContent.FindStage(65);
            var escape = EarlyGameContent.FindStage(66);
            CollectionAssert.AreEqual(new[] { "three_sects", "sealed_hint" }, river.Encounters.SelectMany(e => e));
            CollectionAssert.AreEqual(new[] { "mothers_fire_ice", "muhwa_hostage", "flame_prison", "ice_dragon_wall" }, escape.Encounters.SelectMany(e => e));
            string riverEnd = string.Join(" ", river.Outro);
            StringAssert.Contains("장홍이 먼저 물속", riverEnd);
            Assert.Less(riverEnd.IndexOf("생존을 확인", StringComparison.Ordinal), riverEnd.IndexOf("중상 입은 나백천", StringComparison.Ordinal));
            string ending = string.Join(" ", escape.Outro);
            StringAssert.Contains("장홍이 혼절한 나백천을 업었다", ending);
            Assert.Less(ending.IndexOf("혼절한 나백천", StringComparison.Ordinal), ending.IndexOf("무당산", StringComparison.Ordinal));
            Assert.Less(ending.IndexOf("무당산", StringComparison.Ordinal), ending.IndexOf("남쪽 무한", StringComparison.Ordinal));
            foreach (var stage in new[] { river, escape })
            {
                Assert.AreEqual(stage.Encounters.Length, stage.StoryActors.Length);
                Assert.AreEqual(stage.Encounters.Length, stage.Trials.Length);
                for (int i = 0; i < stage.Encounters.Length; i++)
                    Assert.IsFalse(BalanceSimulation.FightParty(SideContentSimulation.Prepared(66, 80, true), stage, i).Any(m => m.Skills.Any(s => s.Id == "scene_escape_current")));
            }
            Assert.IsFalse(StoryEscape.Episodes.ContainsKey(6502));
            Assert.IsFalse(StoryEscape.Episodes.ContainsKey(6604));
            CollectionAssert.AreEqual(new[] { "three_sects" }, Quests.ToStage(Quests.Find("q_current_guard")).Encounters[0]);
            CollectionAssert.AreEqual(new[] { "ice_dragon_wall" }, Quests.ToStage(Quests.Find("q_father_carry")).Encounters[0]);
        }

        [Test]
        public void RevisionFourteenMergesFinalStarsAcrossAllDifficultiesWithoutLossOrRepeatPayment()
        {
            var save = new SaveData { StoryContentRevision = 13, ClearedStage = 66 };
            save.DoneQuests.Add("q_current_guard"); save.DoneQuests.Add("q_father_carry");
            for (int d = 0; d < 3; d++)
                foreach (int number in new[] { 65, 66 })
                    for (int i = 0; i < (number == 65 ? 3 : 5); i++) StageStars.Record(save, number, i, d, 7);
            int total = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(total, StageStars.Total(save));
            Assert.AreEqual(18, save.RetiredStoryStars);
            Assert.AreEqual(0, Inventory.Count(save, InnerArts.StoneId));
            for (int d = 0; d < 3; d++)
                foreach (int number in new[] { 65, 66 }) Assert.IsTrue(StageStars.StageCleared(save, number, d));
            StoryContentMigration.Apply(save);
            Assert.AreEqual(total, StageStars.Total(save)); Assert.AreEqual(18, save.RetiredStoryStars);
            CollectionAssert.AreEqual(new[] { "q_current_guard", "q_father_carry" }, save.DoneQuests);
        }

        [TestCase(65, 1)] [TestCase(65, 2)] [TestCase(66, 1)] [TestCase(66, 2)]
        public void NewlyFinalCompletionCreditsUnpaidInsightOnlyOnce(int number, int difficulty)
        {
            var save = new SaveData { StoryContentRevision = 13 };
            int final = EarlyGameContent.FindStage(number).Encounters.Length - 1;
            StageStars.Record(save, number, final, difficulty, 3);
            StoryContentMigration.Apply(save);
            Assert.IsTrue(StageStars.StageCleared(save, number, difficulty));
            Assert.AreEqual(difficulty, Inventory.Count(save, InnerArts.StoneId));
            StoryContentMigration.Apply(save);
            Assert.AreEqual(difficulty, Inventory.Count(save, InnerArts.StoneId));
        }

        [TestCase(65, 2)] [TestCase(65, 3)] [TestCase(66, 4)] [TestCase(66, 5)]
        public void RemovedFinalResumeGoesToEndingWithWoundsIntact(int number, int oldNext)
        {
            var save = new SaveData { StoryContentRevision = 13, ProgressStage = number, ProgressNext = oldNext, ProgressDifficulty = 2 };
            save.ProgressHp.Add(new MemberHp { Id = "player", Hp = 101, Qi = 17, HasQi = true, Spirit = 29 });
            StoryContentMigration.Apply(save);
            Assert.AreEqual(EarlyGameContent.FindStage(number).Encounters.Length, save.ProgressNext);
            Assert.IsTrue(StageProgress.AtEnding(save, EarlyGameContent.FindStage(number)));
            Assert.AreEqual(2, save.ProgressDifficulty); Assert.AreEqual(101, save.ProgressHp[0].Hp);
            Assert.AreEqual(17, save.ProgressHp[0].Qi); Assert.AreEqual(29, save.ProgressHp[0].Spirit);
        }

        [TestCase("q_current_guard")] [TestCase("q_father_carry")]
        public void RetargetedRehearsalsRemainClearable(string id)
        {
            var quest = Quests.Find(id);
            var stage = Quests.ToStage(quest);
            int wins = Enumerable.Range(1, 20).Count(seed => BalanceSimulation.Play(stage,
                BalanceSimulation.Starter(quest.RefStage + 1, BalanceSimulation.BotLevel(stage)), seed).Won);
            Assert.That(wins, Is.GreaterThanOrEqualTo(17), id);
        }

        [Test]
        public void LegacyBackfillIncludesBothRetiredRiverSlotsAndTheEvacuationSlotOnce()
        {
            var save = new SaveData { StoryContentRevision = 13, ClearedStage = 66 };
            StageStars.Sync(save);
            Assert.IsTrue(StageStars.StageCleared(save, 65, 0));
            Assert.IsTrue(StageStars.StageCleared(save, 66, 0));
            Assert.AreEqual(12, save.RetiredStoryStars);
            int total = StageStars.Total(save);
            StageStars.Sync(save);
            Assert.AreEqual(total, StageStars.Total(save)); Assert.AreEqual(12, save.RetiredStoryStars);
        }

        [TestCase(65)] [TestCase(66)]
        public void EarlierIncompleteEncountersDoNotGrantFinalCompletionOrInsight(int number)
        {
            var save = new SaveData { StoryContentRevision = 13 };
            StageStars.Record(save, number, 0, 2, 7);
            StoryContentMigration.Apply(save);
            Assert.IsFalse(StageStars.StageCleared(save, number, 2));
            Assert.AreEqual(0, Inventory.Count(save, InnerArts.StoneId));
        }

        [TestCase(65, 0)] [TestCase(65, 1)] [TestCase(65, 2)]
        [TestCase(66, 0)] [TestCase(66, 1)] [TestCase(66, 2)]
        public void RemovingTraversalKeepsTheChapterGrowthBudget(int number, int difficulty)
        {
            var stage = EarlyGameContent.FindStage(number).WithDifficulty(difficulty);
            var legacy = new StageData { Number = number, RecommendedLevel = stage.RecommendedLevel,
                DooTotal = stage.DooTotal, RewardWeightBudget = stage.RewardWeightBudget, Difficulty = difficulty,
                RewardScale = number == 65 ? 4.0 / 3.0 : 1.0,
                Encounters = number == 65 ? new[] { new[] { "three_sects" }, new[] { "sealed_hint" }, new[] { "dae_nan_current" } }
                    : new[] { new[] { "mothers_fire_ice" }, new[] { "muhwa_hostage" }, new[] { "flame_prison" }, new[] { "ice_dragon_wall" }, new[] { "rescue_exit" } } };
            Recommend.StageReward(new SaveData(), legacy, out long oldExp, out long oldDoo);
            Recommend.StageReward(new SaveData(), stage, out long exp, out long doo);
            Assert.That(Math.Abs(exp - oldExp), Is.LessThanOrEqualTo(20));
            Assert.That(Math.Abs(doo - oldDoo), Is.LessThanOrEqualTo(100));
        }
    }
}
