using System;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StoryChoiceEventTests
    {
        [TestCase(0)] [TestCase(1)]
        public void BothDepartureChoicesKeepYerinsAgencyAndTheCanonicalSeatExchange(int choice)
        {
            var ev = StoryChoiceEvents.DepartingFerry;
            string text = string.Join(" ", ev.Opening.Concat(StoryChoiceEvents.Outcome(ev, choice)).Concat(ev.Ending));
            foreach (string detail in new[] { "나예린", "절벽", "십여 장", "스스로", "예청", "나백천", "구출", "곁의 자리" })
                StringAssert.Contains(detail, text);
            foreach (string inventedOutcome in new[] { "예청이 동행을 허락했다", "교제를 정식으로 승낙했다", "물결을 쓰러뜨렸다", "대난원" })
                StringAssert.DoesNotContain(inventedOutcome, text);
            var question = StoryLine.Parse(ev.Ending[0]);
            var answer = StoryLine.Parse(ev.Ending[1]);
            Assert.AreEqual("나예린", question.Speaker);
            Assert.AreEqual("내릴 자리는 없느냐?", question.Text);
            Assert.IsTrue(answer.IsProtagonist);
            Assert.AreEqual("내 옆자리는 언제나 예린의 자리.", answer.Text);
            string replay = string.Join(" ", StoryEpisodes.Replay(EarlyGameContent.FindStage(58)));
            StringAssert.Contains(ev.Choices[choice].Outcome[0], replay);
            Assert.Less(replay.IndexOf(ev.Ending[2], StringComparison.Ordinal),
                replay.IndexOf(StoryChoiceEvents.HubeiEscort.Opening[0], StringComparison.Ordinal));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void DepartureKeepsFirstRewardAndResumesAtEscortWithoutEnteringCombat(int difficulty)
        {
            var stage = EarlyGameContent.FindStage(58).WithDifficulty(difficulty);
            var save = new SaveData();
            Assert.IsEmpty(stage.Encounters[0]);
            Assert.IsNull(stage.TrialFor(0));
            Assert.AreSame(StoryChoiceEvents.DepartingFerry, stage.EventFor(0));
            Assert.IsNull(StoryEpisodes.BeforeFight(stage, 0, save));
            var party = Roster.CreateStoryParty(stage, 0, save);
            CollectionAssert.AreEqual(new[] { "na_yerin" }, party.Select(m => m.Id));
            Assert.IsFalse(party.Any(m => m.Skills.Any(s => s.Id == "scene_escape_seat")));
            Recommend.FightReward(save, stage, 0, false, out long exp, out long doo);
            var legacy = new StageData { Number = 58, RecommendedLevel = stage.RecommendedLevel,
                DooTotal = stage.DooTotal, RewardWeightBudget = 3, Difficulty = difficulty,
                Encounters = stage.RewardEncounters };
            Recommend.FightReward(save, legacy, 0, false, out long oldExp, out long oldDoo);
            Assert.Greater(exp, 0); Assert.Greater(doo, 0);
            Assert.AreEqual(oldExp, exp); Assert.AreEqual(oldDoo, doo);
            Assert.IsEmpty(GearDrops.RollFight(save, stage, 0, new SeededRandom(42)));
            Assert.AreEqual(3, StageStars.Record(save, 58, 0, difficulty, 7));
            Assert.IsFalse(StageStars.StageCleared(save, 58, difficulty));
            Recommend.FightReward(save, stage, 0, Recommend.WonBefore(save, stage, 0), out exp, out doo);
            Assert.AreEqual(0, exp); Assert.AreEqual(0, doo);
            Assert.IsFalse(Roster.SamePerspective(stage, 0, 1));
            var continuation = Roster.CreateStoryParty(stage, 1, save);
            StageProgress.Store(save, 58, 1, continuation, difficulty);
            Assert.AreSame(StoryChoiceEvents.HubeiEscort, stage.EventFor(save.ProgressNext));
            CollectionAssert.AreEqual(new[] { "story_biry_escape", "jang_hong" }, continuation.Select(m => m.Id));
            var run = BalanceSimulation.Play(stage, save, 42);
            Assert.IsTrue(run.Won);
            Assert.AreEqual(0, run.Actions);
            CollectionAssert.AreEqual(new[] { 0, 0, 0 }, run.FightActions);
            Assert.AreEqual(1d, run.HpLeft);
        }

        [TestCase(0)] [TestCase(1)]
        public void BothEscortChoicesPrepareTheCoverWithoutSolvingTheLaterMystery(int choice)
        {
            var ev = StoryChoiceEvents.HubeiEscort;
            string text = string.Join(" ", ev.Opening.Concat(StoryChoiceEvents.Outcome(ev, choice)).Concat(ev.Ending));
            foreach (string detail in new[] { "옥유경", "영령", "무명", "장소옥", "나예린", "기억이 모두 돌아온 것은 아니었다", "강목", "연화검패", "장우양", "장이하", "표승", "푸른 장인", "장홍", "무창행 표행", "죽립", "변장" })
                StringAssert.Contains(detail, text);
            foreach (string laterReveal in new[] { "초운락", "대난원", "칠성좌", "흑견을 쓰러뜨렸다", "남문을 통과했다" })
                StringAssert.DoesNotContain(laterReveal, text);
            string replay = string.Join(" ", StoryEpisodes.Replay(EarlyGameContent.FindStage(58)));
            StringAssert.Contains(ev.Choices[choice].Outcome[0], replay);
            Assert.Less(replay.IndexOf(ev.Ending[0], StringComparison.Ordinal),
                replay.IndexOf(StoryChoiceEvents.SouthCheckpoint.Opening[0], StringComparison.Ordinal));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void EscortPreservesItsRewardAndCanResumeAtTheCheckpoint(int difficulty)
        {
            var stage = EarlyGameContent.FindStage(58).WithDifficulty(difficulty);
            var save = new SaveData();
            Assert.IsEmpty(stage.Encounters[1]);
            Assert.IsNull(stage.TrialFor(1));
            Assert.AreSame(StoryChoiceEvents.HubeiEscort, stage.EventFor(1));
            var party = Roster.CreateStoryParty(stage, 1, save);
            CollectionAssert.AreEqual(new[] { "story_biry_escape", "jang_hong" }, party.Select(m => m.Id));
            Assert.IsFalse(party.Any(m => m.Skills.Any(s => s.Id == "scene_escape_badge")));
            StageProgress.Store(save, 58, 1, party, difficulty);
            Assert.AreSame(StoryChoiceEvents.HubeiEscort, stage.EventFor(save.ProgressNext));
            Assert.IsNull(StoryEpisodes.BeforeFight(stage, 1, save));
            Recommend.FightReward(save, stage, 1, false, out long exp, out long doo);
            var legacy = new StageData { Number = 58, RecommendedLevel = stage.RecommendedLevel,
                DooTotal = stage.DooTotal, RewardWeightBudget = 3, Difficulty = difficulty,
                Encounters = stage.RewardEncounters };
            Recommend.FightReward(save, legacy, 1, false, out long oldExp, out long oldDoo);
            Assert.Greater(exp, 0); Assert.Greater(doo, 0);
            Assert.AreEqual(oldExp, exp); Assert.AreEqual(oldDoo, doo);
            Assert.IsEmpty(GearDrops.RollFight(save, stage, 1, new SeededRandom(42)));
            StageStars.Record(save, 58, 1, difficulty, 7);
            Assert.IsFalse(StageStars.StageCleared(save, 58, difficulty));
            Recommend.FightReward(save, stage, 1, Recommend.WonBefore(save, stage, 1), out exp, out doo);
            Assert.AreEqual(0, exp); Assert.AreEqual(0, doo);
            Assert.IsFalse(Roster.SamePerspective(stage, 1, 2));
            var continuation = Roster.CreateStoryParty(stage, 2, save);
            StageProgress.Store(save, 58, 2, continuation, difficulty);
            Assert.AreSame(StoryChoiceEvents.SouthCheckpoint, stage.EventFor(save.ProgressNext));
            var restored = Roster.CreateStoryParty(stage, 2, save);
            StageProgress.Restore(save, restored);
            CollectionAssert.AreEqual(new[] { "story_biry_escape", "story_bing_geom" }, restored.Select(m => m.Id));
        }

        [TestCase(0)] [TestCase(1)]
        public void BothCheckpointChoicesPreserveTheCoverAndMuwhaOpensTheWay(int choice)
        {
            var ev = StoryChoiceEvents.SouthCheckpoint;
            string text = string.Join(" ", ev.Opening.Concat(StoryChoiceEvents.Outcome(ev, choice)).Concat(ev.Ending));
            foreach (string detail in new[] { "형산십이검", "곽현", "응징한 사람은 무화", "장이하", "빙검", "수레", "남희성", "흑견", "삿갓", "오체투지", "큰마님", "동행한 사이", "여표사", "충돌 없이", "용산객잔", "감시의 위험" })
                StringAssert.Contains(detail, text);
            foreach (string inventedOutcome in new[] { "흑견을 쓰러뜨렸다", "검문관을 처치했다", "정체를 들키지 않았음이 확실했다", "대난원" })
                StringAssert.DoesNotContain(inventedOutcome, text);
            string replay = string.Join(" ", StoryEpisodes.Replay(EarlyGameContent.FindStage(58)));
            StringAssert.Contains(ev.Choices[choice].Outcome[0], replay);
            StringAssert.Contains(ev.Ending[0], replay);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void CheckpointRetainsTheSavedFinalSlotPerspectiveAndFirstClearReward(int difficulty)
        {
            var stage = EarlyGameContent.FindStage(58).WithDifficulty(difficulty);
            Assert.AreEqual(3, stage.Encounters.Length);
            Assert.IsEmpty(stage.Encounters[0]);
            Assert.IsNull(stage.TrialFor(0));
            Assert.AreSame(StoryChoiceEvents.DepartingFerry, stage.EventFor(0));
            Assert.IsEmpty(stage.Encounters[1]);
            Assert.AreSame(StoryChoiceEvents.HubeiEscort, stage.EventFor(1));
            Assert.IsEmpty(stage.Encounters[2]);
            Assert.IsNull(stage.TrialFor(2));
            Assert.AreSame(StoryChoiceEvents.SouthCheckpoint, stage.EventFor(2));
            var save = BalanceSimulation.Starter(58, BalanceSimulation.BotLevel(stage));
            var party = Roster.CreateStoryParty(stage, 2, save);
            CollectionAssert.AreEqual(new[] { "story_biry_escape", "story_bing_geom" }, party.Select(m => m.Id));
            Assert.IsFalse(party.Any(m => m.Skills.Any(s => s.Id == "scene_escape_cover")));
            StageProgress.Store(save, 58, 2, party, difficulty);
            Assert.AreSame(StoryChoiceEvents.SouthCheckpoint, stage.EventFor(save.ProgressNext));
            Assert.IsNull(StoryEpisodes.BeforeFight(stage, 2, save));
            Recommend.FightReward(save, stage, 2, false, out long exp, out long doo);
            var legacy = new StageData { Number = 58, RecommendedLevel = stage.RecommendedLevel,
                DooTotal = stage.DooTotal, RewardWeightBudget = 3, Difficulty = difficulty,
                Encounters = stage.RewardEncounters };
            Recommend.FightReward(save, legacy, 2, false, out long oldExp, out long oldDoo);
            Assert.Greater(exp, 0); Assert.Greater(doo, 0);
            Assert.AreEqual(oldExp, exp); Assert.AreEqual(oldDoo, doo);
            Assert.IsEmpty(GearDrops.RollFight(save, stage, 2, new SeededRandom(42)));
            Assert.IsFalse(StageStars.StageCleared(save, 58, difficulty));
            Assert.AreEqual(3, StageStars.Record(save, 58, 2, difficulty, 7));
            Assert.IsTrue(StageStars.StageCleared(save, 58, difficulty));
            Assert.AreEqual(0, StageStars.Record(save, 58, 2, difficulty, 7));
            Recommend.FightReward(save, stage, 2, Recommend.WonBefore(save, stage, 2), out exp, out doo);
            Assert.AreEqual(0, exp); Assert.AreEqual(0, doo);
            var run = BalanceSimulation.Play(stage, save, 42);
            Assert.IsTrue(run.Won);
            Assert.AreEqual(0, run.FightActions[0]);
            Assert.AreEqual(0, run.FightActions[1]);
            Assert.AreEqual(0, run.FightActions[2]);
            Assert.Greater(run.HpLeft, 0);
        }

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
