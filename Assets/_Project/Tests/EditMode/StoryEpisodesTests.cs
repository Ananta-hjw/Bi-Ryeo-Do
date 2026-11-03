using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StoryEpisodesTests
    {
        [Test]
        public void TheYearEndMatchPrecedesTheNewYearCampAndFirstAttack()
        {
            var stage = EarlyGameContent.FindStage(14);
            var opening = EarlyStory.Opening(stage);
            StringAssert.StartsWith("회상", opening[0].Title);
            StringAssert.StartsWith("현재", opening[1].Title);
            Assert.IsFalse(opening.SelectMany(x => x.Lines).Contains(StoryBriefings.Briefing(14)[0]));
            var departure = StoryEpisodes.BeforeFight(stage, 1, new SaveData());
            StringAssert.Contains("맹연호|", departure[0].Lines[0]);
            Assert.AreEqual(StoryBriefings.Interlude(14).Title, departure[1].Title);
            Assert.AreEqual("새해의 목적지", departure[2].Title);
            Assert.AreEqual("장강, 첫 번째 위기", departure.Last().Title);
            var secondAttack = StoryEpisodes.BeforeFight(stage, 2, new SaveData());
            CollectionAssert.AreEqual(StoryBriefings.Briefing(14).Skip(4), secondAttack[0].Lines);
            var ending = EarlyStory.Ending(stage);
            StringAssert.Contains("정신을 되찾았지", string.Join(" ", ending[1].Lines));
            StringAssert.Contains("관주실", ending.Last().Lines[0]);
        }

        [Test]
        public void CavalryMemoryReturnsToTheMemorialAfterItsRetreatAndHomewardPath()
        {
            var stage = EarlyGameContent.FindStage(15);
            var opening = EarlyStory.Opening(stage);
            Assert.AreSame(stage.Prologue[0], opening[0]);
            StringAssert.Contains("회상", opening[1].Title);
            var ending = EarlyStory.Ending(stage);
            CollectionAssert.AreEqual(stage.Outro.Take(4), ending[0].Lines);
            CollectionAssert.AreEqual(StoryBriefings.Aftermath(15), ending[1].Lines);
            StringAssert.Contains("현재", ending[2].Title);
            CollectionAssert.AreEqual(stage.Outro.Skip(4), ending[2].Lines);
        }

        [Test]
        public void InspectorArrivalAndTheWhiteDragonDrawHaveTheirOwnBoundaries()
        {
            var inspector = EarlyGameContent.FindStage(16);
            var arrival = EarlyStory.Opening(inspector);
            Assert.AreSame(inspector.Prologue[1], arrival[1]);
            StringAssert.Contains("조사관을 맞은 뒤", arrival[2].Title);
            var assembly = EarlyStory.Opening(EarlyGameContent.FindStage(17));
            StringAssert.Contains("회상", assembly.Single(x => x.Title.Contains("지키기로 한 맹세")).Title);
            StringAssert.StartsWith("현재", assembly.Single(x => x.Title.Contains("전달되지 못한 경고")).Title);
            var stage = EarlyGameContent.FindStage(18);
            var draw = EarlyStory.Opening(stage);
            CollectionAssert.AreEqual(StoryBriefings.Briefing(18).Take(5), draw[1].Lines);
            CollectionAssert.AreEqual(stage.Intro, draw[2].Lines);
            CollectionAssert.AreEqual(StoryBriefings.Briefing(18).Skip(5), draw[3].Lines);
            Assert.AreEqual(StoryBriefings.Interlude(18).Title, draw[4].Title);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        [TestCase(14)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(17)]
        [TestCase(18)]
        public void DetailedReplayUsesTheSameOpeningEncountersAndEndingAsBattle(int number)
        {
            var stage = EarlyGameContent.FindStage(number);
            var completed = new SaveData { ClearedStage = 18 };
            var expected = new System.Collections.Generic.List<StoryScene>(EarlyStory.Opening(stage));
            for (int fight = 0; fight < stage.Encounters.Length; fight++)
            {
                Assert.IsNull(StoryEpisodes.BeforeFight(stage, fight, completed));
                var scenes = StoryEpisodes.BeforeFight(stage, fight, completed, false, true);
                if (scenes != null) expected.AddRange(scenes);
                Assert.IsNull(StoryEpisodes.BeforeFight(stage, fight, completed, true, true));
            }
            expected.AddRange(EarlyStory.Ending(stage));
            var actual = StoryEpisodes.ReplayScenes(stage);
            CollectionAssert.AreEqual(expected.SelectMany(x => x.Lines), actual.SelectMany(x => x.Lines));
            CollectionAssert.AreEqual(expected.Select(x => x.HeroArtKey), actual.Select(x => x.HeroArtKey));
        }

        [TestCase(9)]
        [TestCase(10)]
        public void PreparationAndInformationBuyingPrecedeTheDiscovery(int number)
        {
            var stage = EarlyGameContent.FindStage(number);
            var opening = EarlyStory.Opening(stage);
            CollectionAssert.AreEqual(StoryBriefings.Briefing(number), opening[0].Lines);
            CollectionAssert.AreEqual(StoryBriefings.Interlude(number).Lines, opening[1].Lines);
            Assert.AreSame(stage.Prologue[0], opening[2]);
        }

        [Test]
        public void SettlementAndOpeningCeremonyDoNotRunAheadOfTheirChapters()
        {
            var tenth = EarlyGameContent.FindStage(10);
            var eleventh = EarlyGameContent.FindStage(11);
            var twelfth = EarlyGameContent.FindStage(12);
            var night = EarlyStory.Ending(tenth).SelectMany(x => x.Lines).ToArray();
            Assert.IsTrue(night.Contains(StoryBriefings.Aftermath(10)[1]));
            Assert.IsFalse(night.Contains(StoryBriefings.Aftermath(10)[3]));
            var nextMorning = EarlyStory.Opening(eleventh).SelectMany(x => x.Lines).ToArray();
            Assert.Less(System.Array.IndexOf(nextMorning, eleventh.Prologue[0].Lines.Last()),
                System.Array.IndexOf(nextMorning, StoryBriefings.Aftermath(10)[3]));
            Assert.IsFalse(EarlyStory.Ending(eleventh).SelectMany(x => x.Lines).Contains(eleventh.Outro[5]));
            var opening = EarlyStory.Opening(twelfth);
            CollectionAssert.AreEqual(eleventh.Outro.Skip(5), opening[0].Lines);
            Assert.AreSame(twelfth.Prologue[0], opening[1]);
            CollectionAssert.AreEqual(twelfth.Intro.Take(3), opening[2].Lines);
            CollectionAssert.AreEqual(StoryBriefings.Briefing(12), opening[3].Lines);
            Assert.IsFalse(opening.Any(x => x.Title == StoryBriefings.Interlude(12).Title));
            Assert.AreEqual(StoryBriefings.Interlude(12).Title, StoryEpisodes.BeforeFight(twelfth, 1, new SaveData())[0].Title);
        }

        [Test]
        public void CafeteriaFightTransitionsRemainImaginedUntilMoyongHwiIntervenes()
        {
            var stage = EarlyGameContent.FindStage(8);
            foreach (int fight in new[] { 1, 2 })
                StringAssert.Contains("머릿속", string.Join(" ", StoryEpisodes.BeforeFight(stage, fight, new SaveData()).SelectMany(x => x.Lines)));
            StringAssert.Contains("모용휘|그만두시오!", string.Join(" ", EarlyStory.Ending(stage).SelectMany(x => x.Lines)));
        }

        [Test]
        public void FourthAndFifthChaptersPlaceRecordDetailsBetweenTheirFights()
        {
            var fourth = EarlyGameContent.FindStage(4);
            Assert.IsFalse(EarlyStory.Opening(fourth).Any(x => x.Title == StoryBriefings.Interlude(4).Title));
            Assert.AreEqual(StoryBriefings.Interlude(4).Title, StoryEpisodes.BeforeFight(fourth, 1, new SaveData())[0].Title);
            StringAssert.Contains("패룡 우악", StoryEpisodes.BeforeFight(fourth, 2, new SaveData())[0].Title);
            var fifth = EarlyGameContent.FindStage(5);
            var opening = EarlyStory.Opening(fifth);
            StringAssert.Contains("회상", opening[0].Title);
            StringAssert.Contains("계절이 흘러", opening[1].Title);
            Assert.IsFalse(opening.Any(x => x.Title == StoryBriefings.Interlude(5).Title));
            var inn = StoryEpisodes.BeforeFight(fifth, 1, new SaveData());
            Assert.AreEqual("현재 — 윤하루에서 들은 이름", inn[0].Title);
            Assert.AreEqual(StoryBriefings.Interlude(5).Title, inn[1].Title);
            Assert.AreEqual("염도와 내기", inn[2].Title);
            var all = StoryEpisodes.ReplayScenes(fifth).SelectMany(x => x.Lines).ToArray();
            foreach (var line in StoryBriefings.Briefing(5).Concat(StoryBriefings.Interlude(5).Lines).Concat(StoryBriefings.Aftermath(5)))
                Assert.AreEqual(1, all.Count(x => x == line), line);
            Assert.IsTrue(StoryEpisodes.ReplayScenes(fifth).All(x => x.HeroArtKey == null));
        }

        [Test]
        public void SixthChapterIntroducesTheLordAfterTheDisciplesWithoutRepeatingTheGate()
        {
            var sixth = EarlyGameContent.FindStage(6);
            var opening = EarlyStory.Opening(sixth);
            Assert.Less(System.Array.FindIndex(opening, x => x.Title == StoryBriefings.Interlude(6).Title),
                System.Array.FindIndex(opening, x => x.Title == "호아장 정문"));
            Assert.IsFalse(opening.SelectMany(x => x.Lines).Any(x => x.StartsWith("호천상|")));
            var lord = StoryEpisodes.BeforeFight(sixth, 2, new SaveData());
            Assert.IsTrue(lord.SelectMany(x => x.Lines).Any(x => x.StartsWith("호천상|")));
            var all = StoryEpisodes.ReplayScenes(sixth).SelectMany(x => x.Lines).ToArray();
            foreach (var line in sixth.Intro.Concat(StoryBriefings.Briefing(6)).Concat(StoryBriefings.Interlude(6).Lines).Concat(StoryBriefings.Aftermath(6)))
                Assert.AreEqual(1, all.Count(x => x == line), line);
            Assert.IsTrue(StoryEpisodes.ReplayScenes(sixth).All(x => x.HeroArtKey == null));
        }

        [Test]
        public void EarlyChaptersExplainTimeChangesAndSeparateTheFarewellFromTheRoad()
        {
            var first = StoryEpisodes.ReplayScenes(EarlyGameContent.FindStage(1));
            StringAssert.Contains("회상", first[0].Title);
            StringAssert.Contains("열아홉", string.Join(" ", first[1].Lines));
            StringAssert.Contains("두 달 뒤", string.Join(" ", first.SelectMany(x => x.Lines)));
            var second = StoryEpisodes.ReplayScenes(EarlyGameContent.FindStage(2));
            StringAssert.Contains("회상", second[0].Title);
            StringAssert.Contains("현재", second[1].Title);
            var text = string.Join(" ", second.SelectMany(x => x.Lines));
            Assert.Less(text.IndexOf("거의 돌아가실 모습"), text.IndexOf("아미파 속가제자"));
            Assert.Less(text.IndexOf("중양표국 정문 앞"), text.IndexOf("합숙 마지막 밤"));
            StringAssert.DoesNotContain("월급 석 냥", string.Join(" ", StoryBriefings.Interlude(1).Lines));
            var third = StoryEpisodes.ReplayScenes(EarlyGameContent.FindStage(3));
            Assert.AreEqual(StoryAppearance.OldMasterDisguise, third[0].HeroArtKey);
            Assert.IsTrue(third.Skip(1).All(x => x.HeroArtKey == null));
            Assert.IsNull(EarlyStory.Background(EarlyGameContent.FindStage(3)).HeroArtKey);
            Assert.IsNull(EarlyStory.Opening(EarlyGameContent.FindStage(19)));
            Assert.IsFalse(EarlyStory.Supports(new StageData { Number = 1, IsQuest = true }));
            Assert.IsFalse(EarlyStory.Supports(new StageData { Number = 1, IsTower = true }));
        }

        [Test]
        public void FirstStageReplayChangesAppearanceOnlyAfterTheWaterfall()
        {
            var stage = EarlyGameContent.FindStage(1);
            var scenes = StoryEpisodes.ReplayScenes(stage);
            Assert.IsNull(scenes[0].HeroArtKey, "The actual master's training precedes the disguise");
            var waterfall = scenes.Single(s => s.Title == "폭포에서 얻은 서찰");
            Assert.IsNull(waterfall.HeroArtKey);
            var camp = scenes.Single(s => s.Title == "노사부와 열여섯 제자");
            Assert.AreEqual(StoryAppearance.OldMasterDisguise, camp.HeroArtKey);
            var tiger = scenes.Single(s => s.Title == "뜻밖의 백호");
            Assert.AreEqual(StoryAppearance.OldMasterDisguise, tiger.HeroArtKey);
            Assert.AreEqual(StoryAppearance.OldMasterDisguise, scenes.Last().HeroArtKey);
            Assert.Less(System.Array.IndexOf(scenes, waterfall), System.Array.IndexOf(scenes, camp));
            Assert.Less(System.Array.IndexOf(scenes, camp), System.Array.IndexOf(scenes, tiger));
            Assert.IsNull(StoryBriefings.Interlude(1).HeroArtKey, "The real master remains a different person");
        }

        [Test]
        public void AgencyReplayKeepsTheDisguiseAndTheFollowingStageEndsIt()
        {
            var agency = EarlyGameContent.FindStage(2);
            Assert.IsTrue(StoryEpisodes.ReplayScenes(agency).All(s => s.HeroArtKey == StoryAppearance.OldMasterDisguise));
            Assert.AreEqual(StoryAppearance.OldMasterDisguise, StoryBriefings.Interlude(2).HeroArtKey);
            Assert.AreEqual(StoryAppearance.OldMasterDisguise, agency.IntroHeroArtKey);
            Assert.AreEqual(StoryAppearance.OldMasterDisguise, agency.OutroHeroArtKey);
            for (int i = 0; i < agency.Encounters.Length; i++)
                Assert.AreEqual(StoryAppearance.OldMasterDisguise, StoryAppearance.HeroBattleKey(agency, i));
            var farewell = StoryEpisodes.ReplayScenes(EarlyGameContent.FindStage(3));
            Assert.AreEqual(StoryAppearance.OldMasterDisguise, farewell.First().HeroArtKey);
            Assert.IsNull(farewell.Last().HeroArtKey);
            Assert.AreEqual("player", StoryAppearance.HeroBattleKey(EarlyGameContent.FindStage(3), 0));
            Assert.IsNull(StoryEpisodes.ReplayScenes(null));
            Assert.IsNull(StoryEpisodes.ReplayScenes(new StageData { IsQuest = true }));
            Assert.IsNull(StoryEpisodes.ReplayScenes(new StageData { IsTower = true }));
        }

        [Test]
        public void EpisodesAppearForUnfinishedRunsButNeverInReplaysQuestsTowerOrIsolatedFights()
        {
            var stage = EarlyGameContent.FindStage(9);
            var save = new SaveData { ClearedStage = 8 };
            Assert.IsNotNull(StoryEpisodes.BeforeFight(stage, 3, save), "A resume at the boss still needs its transition");
            Assert.IsNull(StoryEpisodes.BeforeFight(stage, 3, save, true));
            Assert.IsNull(StoryEpisodes.BeforeFight(stage, -1, save));
            Assert.IsNull(StoryEpisodes.BeforeFight(stage, stage.Encounters.Length, save));
            var quest = stage.WithDifficulty(0); quest.IsQuest = true;
            Assert.IsNull(StoryEpisodes.BeforeFight(quest, 0, save));
            var tower = stage.WithDifficulty(0); tower.IsTower = true;
            Assert.IsNull(StoryEpisodes.BeforeFight(tower, 0, save));
            save.ClearedStage = 9;
            Assert.IsNull(StoryEpisodes.BeforeFight(stage, 0, save));
            Assert.IsNull(StoryEpisodes.BeforeFight(null, 0, save));
        }

        [Test]
        public void JinJounAndTheCampPrecedeTheWhiteTigerAndAreAvailableInStoryReplay()
        {
            var stage = EarlyGameContent.FindStage(1);
            var save = new SaveData();
            Assert.IsNull(StoryEpisodes.BeforeFight(stage, 0, save));
            Assert.IsNotNull(StoryEpisodes.BeforeFight(stage, 1, save));
            Assert.IsNotNull(StoryEpisodes.BeforeFight(stage, 2, save));
            var scenes = StoryEpisodes.BeforeFight(stage, 3, save);
            Assert.AreEqual(3, scenes.Length);
            StringAssert.Contains("진조운", string.Join(" ", scenes[0].Lines));
            StringAssert.Contains("주작단", string.Join(" ", scenes[1].Lines));
            StringAssert.Contains("남궁상", string.Join(" ", scenes[2].Lines));
            var replay = string.Join(" ", StoryEpisodes.Replay(stage));
            Assert.Less(replay.IndexOf("죽은 사람은"), replay.IndexOf("뜻밖의 백호"));
        }

        [Test]
        public void CurriculumAndClubEventsPrecedeNeedleClassAndTheUnnamedGirl()
        {
            var stage = EarlyGameContent.FindStage(9);
            var save = new SaveData { ClearedStage = 8 };
            Assert.IsFalse(string.Join(" ", StoryBriefings.Briefing(9)).Contains("혁월린"), "History is first learned in class");
            var early = StoryEpisodes.BeforeFight(stage, 0, save);
            StringAssert.Contains("임성진", string.Join(" ", early.SelectMany(scene => scene.Lines)));
            var boss = StoryEpisodes.BeforeFight(stage, 3, save);
            Assert.AreEqual("구정회의 긴급회의", boss[0].Title);
            Assert.AreEqual("비봉침 서른두 개", boss[1].Title);
            Assert.AreEqual("운향정의 소녀", boss[2].Title);
            string girl = string.Join(" ", boss[2].Lines);
            StringAssert.Contains("용안으로도 그의 움직임을 읽지 못했다", girl);
            Assert.IsFalse(girl.Contains("나예린"), "Her name is not revealed at this meeting");
            var replay = string.Join(" ", StoryEpisodes.Replay(stage));
            Assert.Less(replay.IndexOf("처음 듣는 백 년사"), replay.IndexOf("동호회 거리의 대치"));
            Assert.Less(replay.IndexOf("애소저회"), replay.IndexOf("비봉침 서른두 개"));
            Assert.Less(replay.IndexOf("비봉침 서른두 개"), replay.IndexOf("운향정의 소녀"));
        }

        [Test]
        public void EscortScenesKeepTheMasterAbsentAndKeepTheBattleScheduleUnchanged()
        {
            foreach (int number in new[] { 3, 4 })
            {
                var stage = EarlyGameContent.FindStage(number);
                var all = StoryBriefings.Briefing(number).Concat(StoryBriefings.Interlude(number).Lines)
                    .Concat(stage.Intro).Concat(stage.Outro).Concat(StoryBriefings.Aftermath(number));
                Assert.IsFalse(all.Any(line => StoryLine.Parse(line).IsProtagonist), "The master already parted with his disciples");
                Assert.AreEqual(3, stage.Encounters.Length);
            }
            Assert.AreEqual("gu_chang", EarlyGameContent.FindStage(9).Encounters[0][0]);   // with 도백회 followers since 전투 인원 확장
            CollectionAssert.AreEqual(new[] { "wiji_cheon" }, EarlyGameContent.FindStage(9).Encounters[3]);
        }

        [Test]
        public void BaekhyangAlarmPrecedesTheRoomFightAndDoesNotRecruitCompanionLookouts()
        {
            var stage = EarlyGameContent.FindStage(10);
            var save = new SaveData { ClearedStage = 9 };
            var boss = StoryEpisodes.BeforeFight(stage, 2, save);
            StringAssert.Contains("다섯 번", string.Join(" ", boss.SelectMany(scene => scene.Lines)));
            StringAssert.Contains("홀로", string.Join(" ", stage.Intro));
            Assert.IsFalse(stage.Intro.Any(line => StoryLine.Parse(line).IsDialogue && !StoryLine.Parse(line).IsProtagonist), "no companion lookouts");
            var replay = string.Join(" ", StoryEpisodes.Replay(stage));
            Assert.Less(replay.IndexOf("다섯 번의 종소리"), replay.IndexOf("꼭두각시처럼"));
            StringAssert.Contains("무흔일호", replay);
            StringAssert.Contains("삼공자", replay);
        }

        [Test]
        public void PromotionContainsOnlyTheActualOpponentAndPreservesTheForfeit()
        {
            var stage = EarlyGameContent.FindStage(11);
            Assert.IsNull(StoryEpisodes.BeforeFight(stage, 0, new SaveData { ClearedStage = 10 }));
            StringAssert.Contains("싸우지 않고 기권", string.Join(" ", stage.Outro));
            var replay = string.Join(" ", StoryEpisodes.Replay(stage));
            Assert.Less(replay.IndexOf("오문형"), replay.IndexOf("장대평"));
            CollectionAssert.AreEqual(new[] { "dan_daepung" }, stage.Encounters[3]);
            Assert.AreEqual(4, stage.Encounters.Length, "Three Moyong Hwi fights precede Dan Daepung; Gang Hyeonchu forfeits");
        }

        [Test]
        public void TournamentResultsFollowTheSemifinalAndDoNotInventConsentOrHiddenHistory()
        {
            var stage = EarlyGameContent.FindStage(13);
            var save = new SaveData { ClearedStage = 12 };
            Assert.IsFalse(string.Join(" ", stage.Prologue.SelectMany(scene => scene.Lines)).Contains("무승부"));
            var semifinal = StoryEpisodes.BeforeFight(stage, 1, save);
            var swordFinal = StoryEpisodes.BeforeFight(stage, 2, save);
            var final = StoryEpisodes.BeforeFight(stage, 3, save);
            StringAssert.Contains("독랄수의 패배", semifinal[0].Title);
            Assert.AreEqual("검후전의 무승부, 검성전의 시작", swordFinal[0].Title);
            Assert.AreEqual("세 부문의 결말", final[0].Title);
            Assert.AreEqual("설욕을 건 위지천", final[1].Title);
            var replay = string.Join(" ", StoryEpisodes.Replay(stage));
            Assert.Less(replay.IndexOf("독랄수의 패배"), replay.IndexOf("검후전의 무승부"));
            Assert.Less(replay.IndexOf("검후전의 무승부"), replay.IndexOf("세 부문의 결말"));
            Assert.Less(replay.IndexOf("설욕을 건 위지천"), replay.IndexOf("뢰광류하곡"));
            Assert.IsFalse(replay.Contains("그녀도 저항하지 않았다"));
            StringAssert.Contains("이번에도 성공하지 못했군요", replay, "vol5: her sword answers the second kiss");
        }

        [Test]
        public void NewScenesUseShortNonemptyLinesAndParseableSpeakers()
        {
            var save = new SaveData();
            foreach (var stage in EarlyGameContent.Stages)
                for (int i = 0; i < stage.Encounters.Length; i++)
                {
                    var scenes = StoryEpisodes.BeforeFight(stage, i, save);
                    if (scenes == null) continue;
                    foreach (var scene in scenes)
                    {
                        Assert.IsNotEmpty(scene.Title);
                        foreach (var line in scene.Lines)
                        {
                            Assert.IsNotEmpty(line);
                            Assert.LessOrEqual(line.Length, 160);
                            if (line.Contains("|")) Assert.LessOrEqual(StoryLine.Parse(line).Speaker.Length, 8);
                        }
                    }
                }
        }
    }
}
