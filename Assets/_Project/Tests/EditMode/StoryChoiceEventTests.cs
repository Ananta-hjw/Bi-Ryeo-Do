using System;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StoryChoiceEventTests
    {
        [Test]
        public void AllThreeEventsKeepSavedEncounterSlotsAndPerspectives()
        {
            var stage = EarlyGameContent.FindStage(59);
            Assert.AreEqual(3, stage.Encounters.Length);
            Assert.IsEmpty(stage.Encounters[0]);
            Assert.IsNull(stage.TrialFor(0));
            Assert.AreSame(StoryChoiceEvents.RuinedContacts, stage.EventFor(0));
            Assert.IsEmpty(stage.Encounters[1]);
            Assert.IsNull(stage.TrialFor(1));
            Assert.IsEmpty(stage.Encounters[2]);
            Assert.IsNull(stage.TrialFor(2));
            Assert.AreSame(StoryChoiceEvents.InnEncirclement, stage.EventFor(2));
            for (int d = 0; d < 3; d++)
            {
                var scaled = stage.WithDifficulty(d);
                var save = new SaveData();
                var party = Roster.CreateStoryParty(scaled, 1, save);
                StageProgress.Store(save, 59, 1, party, d);
                Assert.IsTrue(StageProgress.Has(save, 59));
                Assert.AreSame(StoryChoiceEvents.LastReport, scaled.EventFor(save.ProgressNext));
                Assert.IsNull(StoryEpisodes.BeforeFight(scaled, 1, save));
                Assert.IsFalse(party.Any(m => m.Skills.Any(s => s.Id == "scene_escape_report")));
                Assert.IsFalse(Roster.CreateStoryParty(scaled, 0, save).Any(m => m.Skills.Any(s => s.Id == "scene_escape_contacts")));
            }
        }

        [TestCase(0)] [TestCase(1)]
        public void BothChoicesKeepTheReportAndReturnToTheInn(int choice)
        {
            var ev = StoryChoiceEvents.LastReport;
            string text = string.Join(" ", ev.Opening.Concat(StoryChoiceEvents.Outcome(ev, choice)).Concat(ev.Ending));
            foreach (string clue in new[] { "대난원", "천라지망", "팔 할", "칠성좌", "숨을 거뒀다", "신용산객잔", "서둘러 돌아갔다" })
                StringAssert.Contains(clue, text);
            StringAssert.Contains(ev.Choices[choice].Outcome[0], string.Join(" ", StoryEpisodes.Replay(EarlyGameContent.FindStage(59))));
            Assert.Throws<ArgumentOutOfRangeException>(() => StoryChoiceEvents.Outcome(ev, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => StoryChoiceEvents.Outcome(ev, ev.Choices.Length));
        }

        [TestCase(0)] [TestCase(1)]
        public void BothInvestigationChoicesFindTheInformantBeforeRevealingHisReport(int choice)
        {
            var ev = StoryChoiceEvents.RuinedContacts;
            string text = string.Join(" ", ev.Opening.Concat(StoryChoiceEvents.Outcome(ev, choice)).Concat(ev.Ending));
            foreach (string detail in new[] { "장홍", "비류연", "모용휘", "남궁상", "태원잡화점", "다관", "포목점", "푸줏간", "신용산객잔", "돼지 몸통", "상현", "무영 백이십칠호" })
                StringAssert.Contains(detail, text);
            foreach (string laterClue in new[] { "대난원", "천라지망", "칠성좌", "팔 할", "숨을 거뒀다" })
                StringAssert.DoesNotContain(laterClue, text);
            string replay = string.Join(" ", StoryEpisodes.Replay(EarlyGameContent.FindStage(59)));
            StringAssert.Contains(ev.Choices[choice].Outcome[0], replay);
            Assert.Less(replay.IndexOf(ev.Ending[0], StringComparison.Ordinal),
                replay.IndexOf(StoryChoiceEvents.LastReport.Opening[0], StringComparison.Ordinal));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void InvestigationCanResumeAtReportAndDoesNotEnterCombat(int difficulty)
        {
            var stage = EarlyGameContent.FindStage(59).WithDifficulty(difficulty);
            var save = BalanceSimulation.Starter(59, BalanceSimulation.BotLevel(stage));
            var party = Roster.CreateStoryParty(stage, 0, save);
            StageStars.Record(save, 59, 0, difficulty, 7);
            StageProgress.Store(save, 59, 1, party, difficulty);
            var restored = Roster.CreateStoryParty(stage, save.ProgressNext, save);
            StageProgress.Restore(save, restored);
            Assert.IsTrue(Roster.SamePerspective(stage, 0, 1));
            Assert.AreSame(StoryChoiceEvents.LastReport, stage.EventFor(save.ProgressNext));
            Assert.IsFalse(Recommend.WonBefore(save, stage, 1));
            Recommend.FightReward(save, stage, 0, Recommend.WonBefore(save, stage, 0), out long exp, out long doo);
            Assert.AreEqual(0, exp); Assert.AreEqual(0, doo);
            for (int seed = 100; seed < 110; seed++)
            {
                var run = BalanceSimulation.Play(stage, save, seed);
                Assert.IsTrue(run.Won);
                Assert.AreEqual(0, run.FightActions[0]);
                Assert.AreEqual(0, run.FightActions[1]);
                Assert.AreEqual(0, run.FightActions[2]);
                Assert.AreEqual(0, run.Actions);
                Assert.AreEqual(1d, run.HpLeft);
            }
        }

        [TestCase(0, 0)] [TestCase(1, 0)] [TestCase(2, 0)]
        [TestCase(0, 1)] [TestCase(1, 1)] [TestCase(2, 1)]
        [TestCase(0, 2)] [TestCase(1, 2)] [TestCase(2, 2)]
        public void EventPreservesFirstRewardAndStarsButCannotFarmRepeatRewards(int difficulty, int encounter)
        {
            var stage = EarlyGameContent.FindStage(59).WithDifficulty(difficulty);
            var save = new SaveData();
            Recommend.FightReward(save, stage, encounter, false, out long exp, out long doo);
            Assert.Greater(exp, 0); Assert.Greater(doo, 0);
            // The legacy reward id preserves the original first-clear budget without spawning an enemy.
            var legacy = new StageData { Number = 59, RecommendedLevel = stage.RecommendedLevel,
                DooTotal = stage.DooTotal, RewardWeightBudget = 3, Difficulty = difficulty,
                Encounters = stage.RewardEncounters };
            Recommend.FightReward(save, legacy, encounter, false, out long oldExp, out long oldDoo);
            Assert.AreEqual(oldExp, exp); Assert.AreEqual(oldDoo, doo);
            Assert.IsEmpty(GearDrops.RollFight(save, stage, encounter, new SeededRandom(42)));
            Assert.AreEqual(3, StageStars.Record(save, 59, encounter, difficulty, 7));
            Assert.AreEqual(0, StageStars.Record(save, 59, encounter, difficulty, 7));
            Assert.AreEqual(encounter == 2, StageStars.StageCleared(save, 59, difficulty));
            Recommend.FightReward(save, stage, encounter, Recommend.WonBefore(save, stage, encounter), out exp, out doo);
            Assert.AreEqual(0, exp); Assert.AreEqual(0, doo);
            for (int i = 0; i < 3; i++) StageStars.Record(save, 59, i, difficulty, 7);
            Assert.IsTrue(StageStars.StageCleared(save, 59, difficulty));
            Assert.AreEqual(9, StageStars.MaxStarsOfStage(59));
            Recommend.FightReward(new SaveData(), stage, encounter, true, out exp, out doo);
            Assert.AreEqual(0, exp); Assert.AreEqual(0, doo);
        }

        [TestCase(0)] [TestCase(1)]
        public void BothInnChoicesPrepareTheCanonicalEscapeWithoutWinningTheNextBattle(int choice)
        {
            var ev = StoryChoiceEvents.InnEncirclement;
            string text = string.Join(" ", ev.Opening.Concat(StoryChoiceEvents.Outcome(ev, choice)).Concat(ev.Ending));
            foreach (string detail in new[] { "나예린", "영령을 깨우고 진령에게", "조용히 무장", "식당", "담환", "확신", "포위", "염도와 빙검", "비밀 통로", "성 밖", "시간을 벌기로", "엄호전은 이제부터" })
                StringAssert.Contains(detail, text);
            foreach (string inventedOutcome in new[] { "포위를 격파했다", "손광을 쓰러뜨렸다", "쌍둥이를 격파했다", "무화를 물리쳤다" })
                StringAssert.DoesNotContain(inventedOutcome, text);
            string replay = string.Join(" ", StoryEpisodes.Replay(EarlyGameContent.FindStage(59)));
            StringAssert.Contains(ev.Choices[choice].Outcome[0], replay);
            Assert.Less(replay.IndexOf(StoryChoiceEvents.LastReport.Ending[0], StringComparison.Ordinal),
                replay.IndexOf(ev.Opening[0], StringComparison.Ordinal));
            var next = EarlyGameContent.FindStage(60);
            CollectionAssert.AreEqual(new[] { "son_gwang_patrol" }, next.Encounters[0]);
            Assert.IsNull(next.EventFor(0));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void SavedInnPerspectiveCompletesTheStageAndGrantsClearInsightOnce(int difficulty)
        {
            var stage = EarlyGameContent.FindStage(59).WithDifficulty(difficulty);
            var save = new SaveData();
            var party = Roster.CreateStoryParty(stage, 2, save);
            CollectionAssert.AreEqual(new[] { "na_yerin" }, party.Select(m => m.Id));
            Assert.IsFalse(party.Any(m => m.Skills.Any(s => s.Id == "scene_escape_warning")));
            StageStars.Record(save, 59, 0, difficulty, 7);
            StageStars.Record(save, 59, 1, difficulty, 7);
            StageProgress.Store(save, 59, 2, party, difficulty);
            Assert.AreSame(StoryChoiceEvents.InnEncirclement, stage.EventFor(save.ProgressNext));
            Assert.IsFalse(StageStars.StageCleared(save, 59, difficulty));
            bool wasCleared = StageStars.StageCleared(save, 59, difficulty);
            StageStars.Record(save, 59, 2, difficulty, 7);
            Assert.IsTrue(StageStars.StageCleared(save, 59, difficulty));
            int stones = StageStars.GrantClearInsight(save, 59, difficulty, wasCleared);
            Assert.AreEqual(StageStars.ClearInsight(59, difficulty), stones);
            Assert.AreEqual(difficulty, stones);
            Assert.AreEqual(0, StageStars.GrantClearInsight(save, 59, difficulty, true));
            StageProgress.Clear(save);
            Assert.IsFalse(StageProgress.Has(save, 59));
        }
    }
}
