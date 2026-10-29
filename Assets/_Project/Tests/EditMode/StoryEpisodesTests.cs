using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StoryEpisodesTests
    {
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
            for (int i = 0; i < 3; i++) Assert.IsNull(StoryEpisodes.BeforeFight(stage, i, save));
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
            StringAssert.Contains("단독 잠입", string.Join(" ", stage.Intro));
            var replay = string.Join(" ", StoryEpisodes.Replay(stage));
            Assert.Less(replay.IndexOf("검후처의 다섯 종소리"), replay.IndexOf("비류연은 쓰러진 진령"));
            StringAssert.Contains("무흔일호", replay);
            StringAssert.Contains("삼공자", replay);
        }

        [Test]
        public void PromotionContainsOnlyTheActualOpponentAndPreservesTheForfeit()
        {
            var stage = EarlyGameContent.FindStage(11);
            Assert.IsNull(StoryEpisodes.BeforeFight(stage, 0, new SaveData { ClearedStage = 10 }));
            StringAssert.Contains("싸우지 않고 기권", string.Join(" ", stage.Outro));
            StringAssert.Contains("오문형·장대평", string.Join(" ", stage.Outro));
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
            StringAssert.Contains("불명", replay);
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
