using System;
using System.Linq;
using NUnit.Framework;
using BiRyeoDo.Core;

namespace BiRyeoDo.Tests
{
    public class EscapeStoryTests
    {
        [TestCase(58, 0)] [TestCase(58, 1)] [TestCase(58, 2)]
        [TestCase(59, 0)] [TestCase(59, 1)] [TestCase(59, 2)]
        [TestCase(60, 0)] [TestCase(60, 1)] [TestCase(60, 2)]
        [TestCase(61, 0)] [TestCase(61, 1)] [TestCase(61, 2)]
        [TestCase(62, 0)] [TestCase(62, 1)] [TestCase(62, 2)]
        [TestCase(63, 0)] [TestCase(63, 1)] [TestCase(63, 2)]
        [TestCase(64, 0)] [TestCase(64, 1)] [TestCase(64, 2)]
        [TestCase(65, 0)] [TestCase(65, 1)] [TestCase(65, 2)]
        [TestCase(66, 0)] [TestCase(66, 1)] [TestCase(66, 2)]
        public void LateRescueChaptersCompleteTheirActualObjectives(int number, int difficulty)
        {
            var stage = EarlyGameContent.FindStage(number).WithDifficulty(difficulty);
            int level = difficulty == 0 ? BalanceSimulation.BotLevel(stage) : (int)Math.Round(StageStars.TargetLevel(number, difficulty));
            var runs = Enumerable.Range(1501, 20).Select(seed => BalanceSimulation.Play(stage, BalanceSimulation.Starter(number, level), seed)).ToArray();
            TestContext.Out.WriteLine(number + "/" + difficulty + ": " + runs.Count(x => x.Won) + "/20; actions " + runs.Where(x => x.Won).Select(x => x.Actions).DefaultIfEmpty().Average());
            TestContext.Out.WriteLine("failures " + string.Join(",", runs.Where(x => !x.Won).GroupBy(x => x.FailedFight).Select(g => g.Key + ":" + g.Count())));
            for (int i = 0; i < stage.Encounters.Length; i++)
            {
                var actions = runs.Where(x => x.FightActions.Count > i).Select(x => x.FightActions[i]).OrderBy(x => x).ToArray();
                if (actions.Length > 0) TestContext.Out.WriteLine("fight " + i + ": median " + actions[actions.Length / 2] + "; fast " + StageStars.FastLimit(number, i, difficulty));
            }
            Assert.GreaterOrEqual(runs.Count(x => x.Won), 17);
        }

        [Test]
        public void SceneCommandsPreserveInventoryGrowthAndWoundsAndAreAvailableInEveryEncounter()
        {
            var save = SideContentSimulation.Prepared(66, 80, true);
            Inventory.Add(save, Ritual.RewardId, 1);
            var items = save.Items.Select(x => x.Id + ":" + x.Count).ToArray();
            var gear = save.GearSlots.Where(x => !string.IsNullOrEmpty(x.ItemId)).Select(x => x.Owner + ":" + x.ItemId + ":" + x.Level).ToArray();
            var arts = save.EquippedArts.ToArray(); var learned = save.LearnedArts.ToArray(); int fights = 0;
            foreach (var stage in EarlyGameContent.Stages.Skip(57))
            {
                Assert.AreEqual(stage.Encounters.Length, stage.StoryActors.Length);
                for (int i = 0; i < stage.Encounters.Length; i++)
                {
                    var party = Roster.CreateStoryParty(stage, i, save); fights++;
                    CollectionAssert.AreEqual(stage.StoryActors[i], party.Select(x => x.Id));
                    foreach (var member in party) Assert.LessOrEqual(member.Skills.Count(s => s.Id != "basic" && s.SpiritCost == 0), 4);
                    var rule = stage.TrialFor(i);
                    if (rule != null && rule.Kind == TrialKind.Technique)
                        foreach (var id in rule.TechniqueSequence ?? new[] { rule.TechniqueId })
                            Assert.IsTrue(party.Any(x => x.Skills.Any(s => s.Id == id)), stage.Number + "/" + i + ":" + id);
                    var a = party[0]; a.Hp = 123; a.Qi = 19; a.Spirit = 29;
                    Roster.PrepareStoryEncounter(stage, i, party);
                    Assert.AreEqual(123, a.Hp); Assert.AreEqual(19, a.Qi); Assert.AreEqual(29, a.Spirit);
                    Assert.AreEqual(a.Skills.Count, a.Skills.Select(x => x.Id).Distinct().Count());
                }
            }
            Assert.AreEqual(33, fights);
            CollectionAssert.AreEqual(items, save.Items.Select(x => x.Id + ":" + x.Count));
            CollectionAssert.AreEqual(gear, save.GearSlots.Where(x => !string.IsNullOrEmpty(x.ItemId)).Select(x => x.Owner + ":" + x.ItemId + ":" + x.Level));
            CollectionAssert.AreEqual(arts, save.EquippedArts); CollectionAssert.AreEqual(learned, save.LearnedArts);
            Assert.IsTrue(save.CompanionLevels.All(x => EarlyGameContent.FindCompanion(x.Id) != null));
            Assert.AreEqual(41, Tower.ReferenceStage(70));
            var sansan = Roster.CreateStoryParty(EarlyGameContent.FindStage(64), 3, save).Single();
            Assert.IsTrue(Genders.IsFemale(Roster.StoryArtKey(sansan)));
        }

        [Test]
        public void AppendedRewardsAndSavesKeepThePreviousEnding()
        {
            var save = new SaveData { StoryContentRevision = 8, ClearedStage = 57, ProgressStage = 57, ProgressNext = 1, Level = 65, Doo = 12345 };
            StageStars.Record(save, 57, 0, 0, 7); StoryContentMigration.Apply(save);
            Assert.AreEqual(57, save.ProgressStage); Assert.AreEqual(0, save.ProgressNext); Assert.AreEqual(12345, save.Doo);
            Assert.AreEqual("lake_pursuit", EarlyGameContent.FindStage(57).Encounters[save.ProgressNext][0]);
            for (int d = 0; d < 3; d++)
                foreach (var stage in EarlyGameContent.Stages.Skip(57))
                {
                    var prepared = BalanceSimulation.Starter(stage.Number, stage.RecommendedPlayerLevel);
                    Recommend.StageReward(prepared, stage.WithDifficulty(d), out long exp, out long doo);
                    TestContext.Out.WriteLine(stage.Number + "/" + d + ": EXP " + exp + "; Doo " + doo);
                    Assert.Greater(exp, 0); Assert.Greater(doo, stage.DooTotal);
                    Assert.Greater(EarlyGameContent.StageRewards[stage.Number].Length, 0);
                    foreach (var row in stage.Encounters) foreach (var id in row) Assert.IsTrue(EarlyGameContent.FindEnemy(id).StoryOnly);
                }
            Assert.AreEqual(66, EarlyGameContent.Stages.Length);
            Assert.AreEqual(57, save.ClearedStage);
        }

        [Test]
        public void ParallelRoutesKeepThePeopleWhoActuallyMadeEachBreakthrough()
        {
            string[] searchers = { "story_biry_escape", "jang_hong", "story_mo_yonghwi", "story_namgung_sang" };
            foreach (int n in new[] { 62, 65 })
                foreach (var ids in EarlyGameContent.FindStage(n).StoryActors) CollectionAssert.AreEqual(searchers, ids);
            foreach (var ids in EarlyGameContent.FindStage(63).StoryActors)
                CollectionAssert.AreEqual(new[] { "story_baek_muyeong", "story_cheong_heun", "story_yong_cheongmyeong", "story_ma_haryeong" }, ids);
            for (int i = 0; i < 3; i++) CollectionAssert.AreEqual(new[] { "na_yerin" }, EarlyGameContent.FindStage(64).StoryActors[i]);
            CollectionAssert.AreEqual(new[] { "story_yeongryeong" }, EarlyGameContent.FindStage(64).StoryActors[3]);
            foreach (var stage in EarlyGameContent.Stages.Skip(57))
                Assert.IsFalse(stage.StoryActors.SelectMany(x => x).Contains("hyo_ryong"), "Hyo Ryong is training, not on this rescue route");
            Assert.IsFalse(EarlyGameContent.FindStage(60).Trials.Skip(1).Any(t => t == null || t.Kind != TrialKind.Technique));
            Assert.AreEqual(TrialKind.Survive, EarlyGameContent.FindStage(66).TrialFor(4).Kind);
        }

        [Test]
        public void StoryKeepsLossesAgencyUnknownsAndTheActualSouthernEnding()
        {
            string Text(int n) { return string.Join(" ", StoryEpisodes.Replay(EarlyGameContent.FindStage(n))); }
            string departure = Text(58), warning = Text(59), captives = Text(60), gate = Text(61), dogs = Text(62), chorus = Text(63), sister = Text(64), river = Text(65), end = Text(66);
            StringAssert.Contains("성좌 문양", departure); StringAssert.Contains("조부의 품", departure); StringAssert.Contains("쌍붕도 중천", departure); StringAssert.Contains("수련", departure); StringAssert.Contains("나예린 자신의 뜻", departure);
            StringAssert.Contains("응징한 사람은 무화", departure); StringAssert.Contains("은설란", departure);
            StringAssert.Contains("대난원", warning); StringAssert.Contains("상현은 숨을 거뒀다", warning); StringAssert.Contains("영령을 깨우고 진령에게", warning);
            StringAssert.Contains("미완성", captives); StringAssert.Contains("살아 있는 포로", captives);
            StringAssert.Contains("임시 흑천맹주", gate); StringAssert.Contains("추적 계획은 끝나지", gate);
            StringAssert.Contains("차음막", dogs); StringAssert.Contains("숨대롱", dogs);
            StringAssert.Contains("사상자 없이", chorus); StringAssert.Contains("마하령", chorus);
            StringAssert.Contains("폭풍비상", sister); StringAssert.Contains("검만큼은 놓지", sister); StringAssert.Contains("일부러 붙잡", sister);
            StringAssert.Contains("불신", river); StringAssert.Contains("중상 입은 나백천을 찾았다", river);
            Assert.Less(end.IndexOf("안도의 시간", StringComparison.Ordinal), end.IndexOf("천무봉", StringComparison.Ordinal));
            StringAssert.Contains("첫 자물쇠", end); StringAssert.Contains("장문령부", end); StringAssert.Contains("빈 신무림기", end);
            StringAssert.Contains("장홍이 나백천을 업고", end); StringAssert.Contains("서찰은 무당산", end); StringAssert.Contains("실제로는 남쪽 무한", end);
            Assert.Less(end.IndexOf("서찰은 무당산", StringComparison.Ordinal), end.IndexOf("실제로는 남쪽 무한", StringComparison.Ordinal));
            StringAssert.Contains("갈중하가 합석", end); StringAssert.Contains("염도·빙검은 여전히 포로", end);
            Assert.IsFalse(end.Contains("무당에 도착했다")); Assert.IsFalse(end.Contains("갈중하가 동료로 합류"));
        }
    }
}
