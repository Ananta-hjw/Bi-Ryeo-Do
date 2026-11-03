using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StoryEpisodesTests
    {
        [Test]
        public void SouthernMarketAndTheThreeYearWindMemoryReturnToTheWoundedPresent()
        {
            var stage = EarlyGameContent.FindStage(51);
            var opening = EarlyStory.Opening(stage);
            CollectionAssert.AreEqual(stage.Prologue[0].Lines.Take(7), opening[0].Lines);
            CollectionAssert.AreEqual(stage.Prologue[0].Lines.Skip(7), opening[2].Lines);
            var wind = StoryEpisodes.BeforeFight(stage, 3, new SaveData());
            StringAssert.Contains("삼 년 전 무당산", wind[1].Title);
            StringAssert.StartsWith("현재로", wind[2].Title);
            var ending = EarlyStory.Ending(stage);
            CollectionAssert.AreEqual(stage.Outro.Take(3), ending[0].Lines);
            CollectionAssert.AreEqual(stage.Outro.Skip(3), ending[2].Lines);
        }

        [Test]
        public void NorthernRoutePrecedesYeongryeongAndThePerspectiveSwitchToTheSealedRoom()
        {
            var stage = EarlyGameContent.FindStage(52);
            var opening = EarlyStory.Opening(stage).SelectMany(x => x.Lines).ToArray();
            CollectionAssert.AreEqual(stage.Prologue[0].Lines.Take(3), opening);
            Assert.IsFalse(opening.Contains(stage.Prologue[0].Lines[4]));
            CollectionAssert.AreEqual(StoryBriefings.Briefing(52).Take(2), StoryEpisodes.BeforeFight(stage, 0, new SaveData())[0].Lines);
            var room = StoryEpisodes.BeforeFight(stage, 1, new SaveData());
            CollectionAssert.AreEqual(stage.Prologue[0].Lines.Skip(3), room[1].Lines);
            CollectionAssert.AreEqual(stage.Intro.Take(3), room[3].Lines);
            CollectionAssert.AreEqual(new[] { stage.Outro[0] }, room[6].Lines);
            StringAssert.Contains("교차 시점", room[8].Title);
            StringAssert.Contains("붉은 비단방", room.Last().Title);
            var ending = EarlyStory.Ending(stage);
            StringAssert.Contains("강호란도", ending[0].Title);
            CollectionAssert.AreEqual(stage.Outro.Skip(10).Take(4), ending[2].Lines);
            CollectionAssert.AreEqual(stage.Outro.Skip(14), ending[4].Lines);
        }

        [Test]
        public void AssassinsDeathPrecedesThePillRequestAndTheEmptyVesselMemory()
        {
            var stage = EarlyGameContent.FindStage(53);
            var opening = EarlyStory.Opening(stage).SelectMany(x => x.Lines).ToArray();
            Assert.IsFalse(opening.Contains(stage.Intro[3]));
            var currents = StoryEpisodes.BeforeFight(stage, 1, new SaveData());
            StringAssert.Contains("숨이 끊어졌거든", currents[0].Lines[1]);
            CollectionAssert.AreEqual(stage.Intro.Skip(1), currents[1].Lines);
            StringAssert.Contains("소생단 두 알", currents[3].Lines[0]);
            StringAssert.StartsWith("회상", currents[5].Title);
            StringAssert.StartsWith("현재로", currents[6].Title);
            CollectionAssert.AreEqual(StoryBriefings.Briefing(53).Skip(6), currents[4].Lines);
        }

        [Test]
        public void SouthernExplosionEasternTrapAndLaterSupportKeepTheirSeparateRoles()
        {
            var stage = EarlyGameContent.FindStage(54);
            var opening = EarlyStory.Opening(stage);
            CollectionAssert.AreEqual(stage.Prologue[0].Lines.Take(9), opening[0].Lines);
            StringAssert.Contains("동해도", opening[1].Title);
            CollectionAssert.AreEqual(stage.Prologue[0].Lines.Skip(9), opening[1].Lines);
            Assert.AreSame(stage.Prologue[1], opening[3]);
            var ending = EarlyStory.Ending(stage);
            CollectionAssert.AreEqual(stage.Outro.Take(4), ending[0].Lines);
            CollectionAssert.AreEqual(new[] { stage.Outro[4] }, ending[2].Lines);
            CollectionAssert.AreEqual(stage.Outro.Skip(5), ending[4].Lines);
            Assert.AreEqual("story_biry_rescue", stage.StoryActors[4][0]);
            Assert.Contains("story_mo_yonghwi", stage.StoryActors[3]);
        }

        [Test]
        public void EscapePlansFollowTheNeedlesAndRecoveryDoesNotEraseTheSistersChoice()
        {
            var stage = EarlyGameContent.FindStage(55);
            var opening = EarlyStory.Opening(stage).SelectMany(x => x.Lines).ToArray();
            Assert.IsFalse(opening.Contains(stage.Intro[0]));
            Assert.IsFalse(opening.Contains(StoryBriefings.Briefing(55)[4]));
            var escape = StoryEpisodes.BeforeFight(stage, 1, new SaveData());
            CollectionAssert.AreEqual(new[] { stage.Intro[0] }, escape[0].Lines);
            StringAssert.StartsWith("회상", escape[2].Title);
            StringAssert.StartsWith("현재로", escape[3].Title);
            var clinic = StoryEpisodes.BeforeFight(stage, 3, new SaveData());
            StringAssert.Contains(" 기절", clinic[0].Lines[3]);
            StringAssert.Contains("한편 사번대", clinic[1].Title);
            StringAssert.Contains("나예린을 데려왔다", clinic[2].Lines[0]);
            var ending = EarlyStory.Ending(stage);
            CollectionAssert.AreEqual(new[] { stage.Outro[2] }, ending[2].Lines);
            CollectionAssert.AreEqual(stage.Outro.Skip(3).Take(4), ending[5].Lines);
            CollectionAssert.AreEqual(stage.Outro.Skip(7), ending[7].Lines);
            CollectionAssert.AreEqual(StoryBriefings.Aftermath(55).Skip(11), ending[8].Lines);
        }

        [Test]
        public void FinalSwordSummaryFollowsTheShowdownAndVictoryPrecedesPoisonAndAbduction()
        {
            var stage = EarlyGameContent.FindStage(46);
            var background = StoryBriefings.Briefing(46);
            Assert.IsFalse(EarlyStory.Opening(stage).SelectMany(x => x.Lines).Contains(background[5]));
            Assert.IsFalse(StoryEpisodes.BeforeFight(stage, 3, new SaveData()).SelectMany(x => x.Lines).Contains(background[6]));
            var replay = StoryEpisodes.ReplayScenes(stage).SelectMany(x => x.Lines).ToArray();
            Assert.Less(System.Array.IndexOf(replay, StoryShowdowns.Cheonoe.AfterStrike[0]), System.Array.IndexOf(replay, background[5]));
            var ending = EarlyStory.Ending(stage);
            CollectionAssert.AreEqual(new[] { stage.Outro[0] }, ending[1].Lines);
            CollectionAssert.AreEqual(stage.Outro.Skip(1).Take(4), ending[3].Lines);
            CollectionAssert.AreEqual(stage.Outro.Skip(5).Take(3), ending[5].Lines);
            CollectionAssert.AreEqual(stage.Outro.Skip(8), ending[7].Lines);
        }

        [Test]
        public void SearchReturnsFromAbductionMemoryBeforeTheTrapAndTheMastersLoan()
        {
            var stage = EarlyGameContent.FindStage(47);
            var opening = EarlyStory.Opening(stage);
            StringAssert.StartsWith("회상", opening[2].Title);
            StringAssert.Contains("납치 당시", opening[3].Title);
            StringAssert.StartsWith("현재로", opening[4].Title);
            CollectionAssert.AreEqual(stage.Prologue[1].Lines.Take(4), opening[2].Lines);
            CollectionAssert.AreEqual(stage.Prologue[1].Lines.Skip(4), opening[3].Lines);
            Assert.IsFalse(opening.SelectMany(x => x.Lines).Contains(StoryBriefings.Briefing(47)[6]));
            var trail = StoryEpisodes.BeforeFight(stage, 0, new SaveData());
            CollectionAssert.AreEqual(StoryBriefings.Briefing(47).Skip(5).Take(2), trail.Last().Lines);
            var ending = EarlyStory.Ending(stage);
            CollectionAssert.AreEqual(stage.Outro.Take(2), ending[0].Lines);
            CollectionAssert.AreEqual(new[] { stage.Outro[2] }, ending[2].Lines);
            CollectionAssert.AreEqual(stage.Outro.Skip(3), ending[5].Lines);
        }

        [Test]
        public void FourFrontsStaySeparateAndFiveBlowsRemainPendingAtTheChapterBoundary()
        {
            var stage = EarlyGameContent.FindStage(48);
            var opening = EarlyStory.Opening(stage).SelectMany(x => x.Lines).ToArray();
            var background = StoryBriefings.Briefing(48);
            Assert.IsFalse(opening.Contains(background[6]));
            CollectionAssert.AreEqual(background.Skip(4).Take(2), StoryEpisodes.BeforeFight(stage, 1, new SaveData()).Last().Lines);
            var ending = EarlyStory.Ending(stage);
            CollectionAssert.AreEqual(stage.Outro.Take(3), ending[0].Lines);
            CollectionAssert.AreEqual(stage.Outro.Skip(3).Take(2), ending[2].Lines);
            StringAssert.Contains("다른 섬", ending[5].Title);
            StringAssert.Contains("구출대가 듣지 못한", ending[7].Title);
            CollectionAssert.AreEqual(stage.Outro.Skip(6), ending[7].Lines);
        }

        [Test]
        public void WesternDuelReturnsFromNineYearMemoryAndDistinguishesArmorFromTheBody()
        {
            var stage = EarlyGameContent.FindStage(49);
            var opening = EarlyStory.Opening(stage);
            CollectionAssert.AreEqual(stage.Prologue[0].Lines.Take(8), opening[0].Lines);
            StringAssert.Contains("다른 장소의 현재", opening[1].Title);
            Assert.AreSame(stage.Prologue[1], opening[2]);
            Assert.IsFalse(opening.SelectMany(x => x.Lines).Contains(stage.Intro[2]));
            var air = StoryEpisodes.BeforeFight(stage, 1, new SaveData());
            CollectionAssert.AreEqual(stage.Intro.Skip(2), air[0].Lines);
            StringAssert.Contains("충격에 반응하는 갑옷", air[1].Lines[0]);
            Assert.IsFalse(StoryBriefings.Briefing(49).Any(x => x.Contains("작은 충격에도")));
            var mind = StoryEpisodes.BeforeFight(stage, 5, new SaveData());
            StringAssert.Contains("갑옷 아래", mind[0].Title);
            CollectionAssert.AreEqual(StoryBriefings.Briefing(49).Skip(6), mind[1].Lines);
            Assert.IsTrue(mind[2].Lines.Any(x => x.Contains("아버지의 그림자")));
        }

        [Test]
        public void EasternDuelShowsTheRealWeaponAfterTheFanAndMemoryAfterTheHeatDiscovery()
        {
            var stage = EarlyGameContent.FindStage(50);
            var opening = EarlyStory.Opening(stage).SelectMany(x => x.Lines).ToArray();
            Assert.IsFalse(opening.Contains(stage.Intro[1]));
            CollectionAssert.AreEqual(stage.Intro.Skip(1), StoryEpisodes.BeforeFight(stage, 1, new SaveData())[0].Lines);
            Assert.IsFalse(opening.Contains(StoryBriefings.Briefing(50)[6]));
            var method = StoryEpisodes.BeforeFight(stage, 3, new SaveData());
            StringAssert.StartsWith("회상", method[1].Title);
            CollectionAssert.AreEqual(StoryBriefings.Briefing(50).Skip(6), method[2].Lines);
            StringAssert.StartsWith("현재로", method[3].Title);
            CollectionAssert.AreEqual(StoryBriefings.Aftermath(50).Skip(2), EarlyStory.Ending(stage)[3].Lines);
        }

        [Test]
        public void TigerMemoryReturnsToThePresentBeforeSubdualAndTheClosedDoor()
        {
            var stage = EarlyGameContent.FindStage(41);
            var opening = EarlyStory.Opening(stage);
            StringAssert.Contains("회상", opening[4].Title);
            CollectionAssert.AreEqual(stage.Prologue[2].Lines, opening[4].Lines);
            StringAssert.StartsWith("현재로", opening.Last().Title);
            Assert.IsFalse(opening.SelectMany(x => x.Lines).Contains(StoryBriefings.Briefing(41)[6]));
            var ending = EarlyStory.Ending(stage);
            CollectionAssert.AreEqual(stage.Outro.Take(4), ending[0].Lines);
            CollectionAssert.AreEqual(StoryBriefings.Briefing(41).Skip(6), ending[2].Lines);
            CollectionAssert.AreEqual(stage.Outro.Skip(4), ending[3].Lines);
        }

        [Test]
        public void InvitationFlashbackRegistrationAndDockDuelHaveSeparateBoundaries()
        {
            var stage = EarlyGameContent.FindStage(42);
            var opening = EarlyStory.Opening(stage);
            Assert.AreSame(stage.Prologue[0], opening[0]);
            Assert.AreSame(stage.Prologue[1], opening[1]);
            StringAssert.Contains("일주일 전", opening[1].Title);
            StringAssert.Contains("협상 다음 날", opening[3].Title);
            StringAssert.Contains("마천각", opening[4].Title);
            StringAssert.Contains("등록 마감", opening[6].Title);
            StringAssert.Contains("그날 밤", opening.Last().Title);
            Assert.IsFalse(opening.SelectMany(x => x.Lines).Contains(StoryBriefings.Briefing(42)[6]));
            CollectionAssert.AreEqual(StoryBriefings.Briefing(42).Skip(5), StoryEpisodes.BeforeFight(stage, 0, new SaveData()).Last().Lines);
        }

        [Test]
        public void MoonFinaleTurnsIntoTheGroundAndPrivateOrdersWaitForTheMainTournament()
        {
            var stage = EarlyGameContent.FindStage(43);
            var opening = EarlyStory.Opening(stage);
            Assert.IsFalse(opening.SelectMany(x => x.Lines).Contains(StoryBriefings.Briefing(43)[5]));
            var twins = StoryEpisodes.BeforeFight(stage, 1, new SaveData());
            Assert.IsTrue(twins.SelectMany(x => x.Lines).Contains(StoryBriefings.Briefing(43)[5]));
            var ending = EarlyStory.Ending(stage);
            StringAssert.Contains("힘을 땅으로 돌렸다", ending[0].Lines[0]);
            Assert.IsFalse(ending.SelectMany(x => x.Lines).Any(x => x.Contains("대신 그 힘을 받았다")));
            Assert.IsFalse(ending.SelectMany(x => x.Lines).Contains(stage.Outro[12]));
            Assert.IsFalse(ending.SelectMany(x => x.Lines).Contains(StoryBriefings.Aftermath(43)[3]));
            var tournament = EarlyStory.Opening(EarlyGameContent.FindStage(44));
            CollectionAssert.AreEqual(stage.Outro.Skip(12), tournament[2].Lines);
            StringAssert.StartsWith("본선 개막", tournament[2].Lines[0]);
            CollectionAssert.AreEqual(new[] { StoryBriefings.Aftermath(43)[3] }, tournament[3].Lines);
        }

        [Test]
        public void TournamentBackgroundTracksEachFighterAndReconciliationFollowsTheBout()
        {
            var stage = EarlyGameContent.FindStage(44);
            var background = StoryBriefings.Briefing(44);
            var opening = EarlyStory.Opening(stage).SelectMany(x => x.Lines).ToArray();
            Assert.IsFalse(opening.Contains(background[7]));
            for (int fight = 0; fight < 3; fight++)
                CollectionAssert.AreEqual(new[] { background[fight + 1] }, StoryEpisodes.BeforeFight(stage, fight, new SaveData()).Last().Lines);
            var ny = StoryEpisodes.BeforeFight(stage, 3, new SaveData());
            CollectionAssert.AreEqual(new[] { background[5] }, ny[1].Lines);
            CollectionAssert.AreEqual(new[] { background[4] }, ny[2].Lines);
            CollectionAssert.AreEqual(new[] { background[6] }, StoryEpisodes.BeforeFight(stage, 4, new SaveData())[0].Lines);
            CollectionAssert.AreEqual(new[] { stage.Outro[0] }, EarlyStory.Ending(stage)[0].Lines);
            CollectionAssert.AreEqual(new[] { background[7] }, EarlyStory.Ending(stage)[1].Lines);
        }

        [Test]
        public void SistersDuelFollowsTheGuardAndTheFluteFollowsItsBloodTrigger()
        {
            var stage = EarlyGameContent.FindStage(45);
            var background = StoryBriefings.Briefing(45);
            Assert.IsFalse(EarlyStory.Opening(stage).SelectMany(x => x.Lines).Contains(background[6]));
            var guard = StoryEpisodes.BeforeFight(stage, 0, new SaveData());
            Assert.IsFalse(guard.SelectMany(x => x.Lines).Any(x => x.Contains("이제 린 차례")));
            StringAssert.Contains("이제 린 차례", StoryEpisodes.BeforeFight(stage, 1, new SaveData())[0].Lines[0]);
            var ice = StoryEpisodes.BeforeFight(stage, 2, new SaveData());
            StringAssert.StartsWith("회상", ice[0].Title);
            CollectionAssert.AreEqual(new[] { background[5] }, ice[1].Lines);
            StringAssert.StartsWith("현재로", ice[2].Title);
            var flute = StoryEpisodes.BeforeFight(stage, 3, new SaveData());
            StringAssert.Contains("옆구리를 찔렸다", flute[0].Lines[1]);
            CollectionAssert.AreEqual(new[] { background[6] }, flute[1].Lines);
            StringAssert.Contains("피리를 부쉈다", flute[2].Lines[1]);
            CollectionAssert.AreEqual(new[] { background[7] }, flute[3].Lines);
            CollectionAssert.AreEqual(StoryBriefings.Aftermath(45).Take(2), EarlyStory.Ending(stage)[1].Lines);
        }

        [Test]
        public void ChildhoodMemoriesKeepTheirNestedMemoryAndReturnToTheBoat()
        {
            var stage = EarlyGameContent.FindStage(37);
            var opening = EarlyStory.Opening(stage);
            var lines = opening.SelectMany(x => x.Lines).ToArray();
            Assert.IsFalse(lines.Contains(stage.Prologue[0].Lines[1]), "The hawk returns after the captain election in chapter 38");
            Assert.IsFalse(lines.Contains(stage.Prologue[0].Lines[2]));
            Assert.IsTrue(lines.Contains(stage.Prologue[0].Lines[3]), "The player's identity reveal must precede the childhood memory");
            CollectionAssert.AreEqual(stage.Prologue[3].Lines.Skip(7), opening[9].Lines);
            StringAssert.StartsWith("회상 속 회상", opening[9].Title);
            StringAssert.Contains("십 년 전 아미산", opening[10].Title);
            var ending = EarlyStory.Ending(stage);
            CollectionAssert.AreEqual(stage.Outro.Take(9), ending[0].Lines);
            CollectionAssert.AreEqual(StoryBriefings.Aftermath(37).Take(2), ending[1].Lines);
            StringAssert.StartsWith("현재로", ending[2].Title);
        }

        [Test]
        public void CorridorFightIsYesterdayAndTheHawkReturnsAfterTheElection()
        {
            var stage = EarlyGameContent.FindStage(38);
            var opening = EarlyStory.Opening(stage);
            StringAssert.Contains("도착 다음 날", opening[3].Title);
            StringAssert.Contains("전날 복도", opening.Last().Title);
            var corridor = StoryEpisodes.BeforeFight(stage, 0, new SaveData());
            Assert.IsTrue(corridor.All(x => x.Title.StartsWith("회상")));
            StringAssert.StartsWith("현재로", StoryEpisodes.BeforeFight(stage, 1, new SaveData())[0].Title);
            Assert.IsFalse(opening.SelectMany(x => x.Lines).Contains(StoryBriefings.Briefing(38)[7]));
            var ending = EarlyStory.Ending(stage);
            CollectionAssert.AreEqual(stage.Outro.Take(3), ending[0].Lines);
            CollectionAssert.AreEqual(new[] { stage.Outro[3] }, ending[3].Lines);
            CollectionAssert.AreEqual(EarlyGameContent.FindStage(37).Prologue[0].Lines.Skip(1).Take(2), ending[4].Lines);
            CollectionAssert.AreEqual(StoryBriefings.Aftermath(38).Skip(1).Take(1), ending[5].Lines);
            StringAssert.Contains("천무학관", ending[6].Title);
            StringAssert.StartsWith("마천각으로", ending.Last().Title);
        }

        [Test]
        public void IslandNegotiationKeepsTermsPrivateReportAndOtherGuestsDistinct()
        {
            var stage = EarlyGameContent.FindStage(39);
            var opening = EarlyStory.Opening(stage);
            Assert.AreSame(stage.Prologue[1], opening[2]);
            CollectionAssert.AreEqual(StoryBriefings.Briefing(39).Skip(2).Take(2), opening[3].Lines);
            Assert.AreSame(stage.Prologue[2], opening[4]);
            CollectionAssert.AreEqual(StoryBriefings.Briefing(39).Skip(4).Take(2), opening[5].Lines);
            Assert.AreSame(stage.Prologue[3], opening[6]);
            var ending = EarlyStory.Ending(stage);
            CollectionAssert.AreEqual(stage.Outro.Take(14), ending[0].Lines);
            CollectionAssert.AreEqual(StoryBriefings.Aftermath(39).Take(2), ending[1].Lines);
            CollectionAssert.AreEqual(stage.Outro.Skip(14).Take(4), ending[2].Lines);
            StringAssert.Contains("듣지 못한", ending[2].Title);
            StringAssert.Contains("다른 일행", ending[4].Title);
        }

        [Test]
        public void AmbushRevealsItsSponsorBeforeWaitingForTheCompleteFormation()
        {
            var stage = EarlyGameContent.FindStage(40);
            var opening = EarlyStory.Opening(stage);
            var background = StoryBriefings.Briefing(40);
            Assert.IsFalse(opening.SelectMany(x => x.Lines).Contains(background[5]));
            CollectionAssert.AreEqual(background.Skip(2).Take(2), StoryEpisodes.BeforeFight(stage, 1, new SaveData()).Last().Lines);
            var capture = StoryEpisodes.BeforeFight(stage, 2, new SaveData());
            Assert.AreEqual("배후의 이름은 해어화였다.", capture[0].Lines.Last());
            CollectionAssert.AreEqual(background.Skip(4).Take(2), capture[1].Lines);
            Assert.IsFalse(capture.SelectMany(x => x.Lines).Any(x => x.Contains("구두룡이 될 때까지")));
            var breakFormation = StoryEpisodes.BeforeFight(stage, 3, new SaveData());
            StringAssert.Contains("구두룡이 될 때까지", breakFormation[0].Lines[0]);
            CollectionAssert.AreEqual(background.Skip(6), breakFormation[1].Lines);
            Assert.AreEqual("현재 · 진법의 중심에 선 검객", breakFormation[2].Title);
            CollectionAssert.AreEqual(StoryBriefings.Aftermath(40).Take(2), EarlyStory.Ending(stage)[1].Lines);
        }

        [Test]
        public void WinterReturnDreamAndBankMemoryHaveSeparateTimeBoundaries()
        {
            var stage = EarlyGameContent.FindStage(31);
            var opening = EarlyStory.Opening(stage);
            StringAssert.Contains("겨울을 지나 봄", opening[0].Title);
            CollectionAssert.AreEqual(stage.Prologue[2].Lines.Take(12), opening[5].Lines);
            Assert.AreEqual(StoryBriefings.Interlude(31).Title, opening[6].Title);
            StringAssert.StartsWith("꿈", opening[9].Title);
            StringAssert.Contains("다른 곳의 현재", opening[10].Title);
            StringAssert.StartsWith("회상", opening[11].Title);
            var ending = EarlyStory.Ending(stage);
            Assert.AreEqual(StoryBriefings.Interlude(32).Title, ending[2].Title);
            StringAssert.Contains("표행에 합류", ending[3].Title);
            StringAssert.Contains("한 달 뒤", ending[5].Title);
        }

        [Test]
        public void FireReportIsAMemoryAndTheArmbandChallengeReturnsToThePresent()
        {
            var stage = EarlyGameContent.FindStage(32);
            var opening = EarlyStory.Opening(stage);
            StringAssert.StartsWith("회상", opening[0].Title);
            StringAssert.StartsWith("현재", opening[1].Title);
            Assert.IsFalse(opening.Any(x => x.Title == StoryBriefings.Interlude(32).Title));
            var challenge = StoryEpisodes.BeforeFight(stage, 0, new SaveData());
            StringAssert.StartsWith("회상", challenge[0].Title);
            Assert.AreEqual("완장이 합격은 아니었다", challenge[1].Title);
            CollectionAssert.AreEqual(StoryBriefings.Briefing(32).Skip(6), challenge.Last().Lines);
            CollectionAssert.AreEqual(StoryBriefings.Aftermath(32).Take(3), EarlyStory.Ending(stage)[0].Lines);
        }

        [Test]
        public void EnvoyBadgeAndRebuildingPledgeAreShownBeforeTheAccusation()
        {
            var stage = EarlyGameContent.FindStage(33);
            var opening = EarlyStory.Opening(stage);
            StringAssert.StartsWith("회상", opening[0].Title);
            Assert.IsFalse(opening.Any(x => x.Title == StoryBriefings.Interlude(33).Title));
            Assert.IsFalse(opening.SelectMany(x => x.Lines).Contains(StoryBriefings.Briefing(33)[4]));
            var badge = StoryEpisodes.BeforeFight(stage, 0, new SaveData());
            CollectionAssert.AreEqual(StoryBriefings.Briefing(33), badge.Last().Lines);
            var accusation = StoryEpisodes.BeforeFight(stage, 2, new SaveData());
            Assert.AreEqual(StoryBriefings.Interlude(33).Title, accusation[0].Title);
            Assert.AreEqual("혈선은 의심, 유일한 증명은 아니었다", accusation[1].Title);
        }

        [Test]
        public void FoodArrivesAfterTheVisitAndDoesNotPrecedeYerinTakingAction()
        {
            var stage = EarlyGameContent.FindStage(34);
            var opening = EarlyStory.Opening(stage);
            StringAssert.Contains("감옥", opening[1].Title);
            CollectionAssert.AreEqual(stage.Prologue[0].Lines.Skip(4).Take(6), opening[1].Lines);
            Assert.IsFalse(StoryEpisodes.ReplayScenes(stage).Any(x => x.Title == StoryBriefings.Interlude(34).Title));
            StringAssert.Contains("이튿날 밤", EarlyStory.Ending(stage).Last().Title);
            var meal = EarlyStory.Opening(EarlyGameContent.FindStage(35));
            CollectionAssert.AreEqual(EarlyGameContent.FindStage(35).Prologue[0].Lines.Take(4), meal[0].Lines);
            Assert.AreEqual(StoryBriefings.Interlude(34).Title, meal[1].Title);
            Assert.AreEqual("그물이 되어야 하는 친구", meal[2].Title);
        }

        [Test]
        public void ForgedLettersFollowTheHostagesAndPrecedeTheYardInvasion()
        {
            var stage = EarlyGameContent.FindStage(35);
            Assert.IsFalse(EarlyStory.Opening(stage).Any(x => x.Title == StoryBriefings.Interlude(35).Title));
            var invasion = StoryEpisodes.BeforeFight(stage, 2, new SaveData());
            StringAssert.Contains("인질", invasion[0].Title);
            StringAssert.Contains("이틀 혼수 뒤", invasion[1].Title);
            Assert.AreEqual(StoryBriefings.Interlude(35).Title, invasion[2].Title);
            CollectionAssert.AreEqual(stage.Outro.Take(2), invasion[3].Lines);
            StringAssert.Contains("십삼혈이 들어왔다", invasion[4].Lines[0]);
            Assert.IsFalse(EarlyStory.Ending(stage).SelectMany(x => x.Lines).Contains(stage.Outro[0]));
        }

        [Test]
        public void LimitedDuelAndOtherShorePrecedeDepartureAndYeonbiReunion()
        {
            var stage = EarlyGameContent.FindStage(36);
            var opening = EarlyStory.Opening(stage);
            StringAssert.Contains("습격 다음 날", opening[0].Title);
            Assert.IsFalse(opening.Any(x => x.Title == StoryBriefings.Interlude(36).Title));
            Assert.IsFalse(opening.SelectMany(x => x.Lines).Contains(StoryBriefings.Briefing(36)[6]));
            var duel = StoryEpisodes.BeforeFight(stage, 1, new SaveData());
            StringAssert.Contains("그 다음 날", duel[1].Title);
            CollectionAssert.AreEqual(StoryBriefings.Briefing(36).Skip(5), duel[1].Lines);
            var ending = EarlyStory.Ending(stage);
            CollectionAssert.AreEqual(stage.Outro.Take(5), ending[0].Lines);
            CollectionAssert.AreEqual(StoryBriefings.Aftermath(36).Take(2), ending[1].Lines);
            CollectionAssert.AreEqual(new[] { stage.Outro[8] }, ending[4].Lines);
            CollectionAssert.AreEqual(StoryBriefings.Interlude(36).Lines.Take(5), ending[6].Lines);
            CollectionAssert.AreEqual(stage.Outro.Skip(12), ending[7].Lines);
        }

        [Test]
        public void GatePreparationAndCarriageSearchPrecedeTheirResults()
        {
            var stage = EarlyGameContent.FindStage(25);
            var opening = EarlyStory.Opening(stage);
            CollectionAssert.AreEqual(stage.Prologue[0].Lines.Take(4), opening[0].Lines);
            Assert.AreEqual(StoryBriefings.Briefing(25)[0], opening[1].Lines[0]);
            CollectionAssert.AreEqual(stage.Prologue[0].Lines.Skip(4), opening[2].Lines);
            Assert.Less(System.Array.FindIndex(opening, x => x.Title == StoryBriefings.Interlude(25).Title),
                System.Array.FindIndex(opening, x => x.Title == stage.Prologue[5].Title));
            CollectionAssert.AreEqual(StoryBriefings.Aftermath(25).Take(2), StoryEpisodes.BeforeFight(stage, 3, new SaveData())[0].Lines);
            Assert.IsFalse(EarlyStory.Ending(stage).SelectMany(x => x.Lines).Contains(StoryBriefings.Aftermath(25)[0]));
        }

        [Test]
        public void ReturnAndSharedWatchFollowTheEscapeAndOpeningCeremony()
        {
            var escape = EarlyGameContent.FindStage(26);
            Assert.IsFalse(EarlyStory.Opening(escape).Any(x => x.Title == StoryBriefings.Interlude(26).Title));
            StringAssert.StartsWith("회상", EarlyStory.Opening(escape)[2].Title);
            var ending = EarlyStory.Ending(escape);
            Assert.AreEqual(StoryBriefings.Interlude(26).Title, ending[5].Title);
            Assert.IsFalse(ending.SelectMany(x => x.Lines).Contains(escape.Outro[9]));
            var seven = EarlyGameContent.FindStage(27);
            ending = EarlyStory.Ending(seven);
            Assert.IsFalse(EarlyStory.Opening(seven).Any(x => x.Title == StoryBriefings.Interlude(27).Title));
            CollectionAssert.AreEqual(seven.Outro.Skip(26).Take(5), ending[3].Lines);
            Assert.AreEqual(StoryBriefings.Interlude(27).Title, ending[5].Title);
            StringAssert.Contains("일주일 뒤", ending[6].Title);
        }

        [Test]
        public void RaceJudgmentAndCliffAftercareFollowTheirChallenges()
        {
            var race = EarlyGameContent.FindStage(28);
            var opening = EarlyStory.Opening(race);
            StringAssert.Contains("이 년 뒤", opening[1].Title);
            StringAssert.StartsWith("현재", opening[2].Title);
            StringAssert.StartsWith("회상", opening[5].Title);
            StringAssert.Contains("일 년 뒤", opening[6].Title);
            Assert.IsFalse(opening.Any(x => x.Title == StoryBriefings.Interlude(28).Title));
            var finish = StoryEpisodes.BeforeFight(race, 1, new SaveData());
            Assert.AreEqual(StoryBriefings.Interlude(28).Title, finish[1].Title);
            Assert.IsFalse(EarlyStory.Ending(race).SelectMany(x => x.Lines).Contains(race.Outro[10]));
            var rescue = EarlyGameContent.FindStage(29);
            opening = EarlyStory.Opening(rescue);
            StringAssert.Contains("전날 밤", opening[0].Title);
            CollectionAssert.AreEqual(race.Outro.Skip(10).Take(2), opening[1].Lines);
            Assert.IsFalse(opening.Any(x => x.Title == StoryBriefings.Interlude(29).Title));
            Assert.AreEqual(StoryBriefings.Interlude(29).Title, StoryEpisodes.BeforeFight(rescue, 1, new SaveData())[0].Title);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void FireEpilogueFollowsWindRescueAndPreservesEveryDifficulty(int difficulty)
        {
            var stage = EarlyGameContent.FindStage(30).WithDifficulty(difficulty);
            var opening = EarlyStory.Opening(stage);
            StringAssert.Contains("종료 사흘 뒤", opening[2].Title);
            Assert.IsFalse(opening.Any(x => x.Title == StoryBriefings.Interlude(30).Title));
            var ending = EarlyStory.Ending(stage);
            var lines = ending.SelectMany(x => x.Lines).ToArray();
            CollectionAssert.IsSubsetOf(stage.Outro, lines);
            int interlude = System.Array.FindIndex(ending, x => x.Title == StoryBriefings.Interlude(30).Title);
            Assert.Greater(interlude, 0);
            if (difficulty > 0) CollectionAssert.AreEqual(FireDragonFinale.HardOutroHead, ending[0].Lines);
            var replay = StoryEpisodes.Replay(stage);
            int rescue = System.Array.FindIndex(replay, x => x == "나예린|수고하셨어요, 류연.");
            int grief = System.Array.FindIndex(replay, x => x == StoryBriefings.Interlude(30).Lines[0]);
            Assert.Greater(rescue, 0);
            Assert.Greater(grief, rescue);
        }

        [Test]
        public void TrialAndRescueKeepHospitalScenesAndUndergroundDaysInOrder()
        {
            var trial = EarlyGameContent.FindStage(19);
            var hospital = EarlyStory.Opening(trial);
            Assert.AreSame(trial.Prologue[0], hospital[0]);
            Assert.AreEqual(StoryBriefings.Interlude(19).Title, hospital[1].Title);
            Assert.AreSame(trial.Prologue[1], hospital[2]);
            StringAssert.Contains("사흘 뒤", hospital[4].Title);
            var cave = EarlyGameContent.FindStage(20);
            var opening = EarlyStory.Opening(cave);
            CollectionAssert.AreEqual(cave.Prologue[0].Lines.Take(8), opening[0].Lines);
            Assert.AreEqual("지상에 남은 사람들", opening[1].Title);
            CollectionAssert.AreEqual(StoryBriefings.Briefing(20), opening[2].Lines);
            CollectionAssert.AreEqual(cave.Prologue[0].Lines.Skip(8), opening[4].Lines);
            Assert.AreSame(cave.Prologue[1], opening[5]);
            CollectionAssert.AreEqual(cave.Intro, StoryEpisodes.BeforeFight(cave, 0, new SaveData())[0].Lines);
            Assert.AreEqual(1, StoryEpisodes.ReplayScenes(cave).Count(x => x.Title == "지상에 남은 사람들"));
        }

        [Test]
        public void RoadTrainingAndFamilyFarewellPrecedeTheNextConflict()
        {
            var proposal = EarlyGameContent.FindStage(21);
            var opening = EarlyStory.Opening(proposal);
            Assert.AreEqual(StoryBriefings.Interlude(21).Title, opening[3].Title);
            Assert.AreSame(proposal.Prologue[2], opening[4]);
            var valley = EarlyGameContent.FindStage(22);
            var departure = StoryEpisodes.BeforeFight(valley, 1, new SaveData());
            CollectionAssert.AreEqual(valley.Outro.Skip(2).Take(3), departure[1].Lines);
            Assert.AreEqual("숲에서 만난 생존자", departure[2].Title);
            Assert.IsFalse(EarlyStory.Ending(valley).SelectMany(x => x.Lines).Contains(valley.Outro[3]));
            Assert.IsTrue(StoryEpisodes.ReplayScenes(valley).SelectMany(x => x.Lines).Contains(valley.Outro[3]));
        }

        [Test]
        public void CampAndMaskRecognitionDoNotPrecedeTheirBattles()
        {
            var snake = EarlyGameContent.FindStage(23);
            Assert.IsFalse(EarlyStory.Opening(snake).Any(x => x.Title == StoryBriefings.Interlude(23).Title));
            Assert.AreEqual(StoryBriefings.Interlude(23).Title, StoryEpisodes.BeforeFight(snake, 1, new SaveData())[0].Title);
            var stage = EarlyGameContent.FindStage(24);
            var opening = EarlyStory.Opening(stage);
            StringAssert.StartsWith("회상", opening[0].Title);
            StringAssert.StartsWith("현재", opening[1].Title);
            Assert.AreSame(stage.Prologue[1], opening[3]);
            Assert.IsFalse(opening.Any(x => x.Title == StoryBriefings.Interlude(24).Title));
            var camp = StoryEpisodes.BeforeFight(stage, 3, new SaveData());
            CollectionAssert.AreEqual(StoryBriefings.Aftermath(23).Take(2), camp[1].Lines);
            Assert.AreEqual("세 가지 질문", camp[2].Title);
            Assert.AreEqual(StoryBriefings.Interlude(24).Title, camp[3].Title);
            Assert.AreEqual("열 수와 옷깃", camp[5].Title);
            Assert.IsFalse(EarlyStory.Ending(snake).SelectMany(x => x.Lines).Contains(StoryBriefings.Aftermath(23)[0]));
            var ending = EarlyStory.Ending(stage);
            CollectionAssert.AreEqual(StoryBriefings.Aftermath(24).Take(3), ending[1].Lines);
            StringAssert.StartsWith("다음 날", ending[2].Title);
            CollectionAssert.AreEqual(stage.Outro.Skip(stage.Outro.Length - 1), ending[2].Lines);
        }

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
        [TestCase(19)]
        [TestCase(20)]
        [TestCase(21)]
        [TestCase(22)]
        [TestCase(23)]
        [TestCase(24)]
        [TestCase(25)]
        [TestCase(26)]
        [TestCase(27)]
        [TestCase(28)]
        [TestCase(29)]
        [TestCase(30)]
        [TestCase(31)]
        [TestCase(32)]
        [TestCase(33)]
        [TestCase(34)]
        [TestCase(35)]
        [TestCase(36)]
        [TestCase(37)]
        [TestCase(38)]
        [TestCase(39)]
        [TestCase(40)]
        [TestCase(41)]
        [TestCase(42)]
        [TestCase(43)]
        [TestCase(44)]
        [TestCase(45)]
        [TestCase(46)]
        [TestCase(47)]
        [TestCase(48)]
        [TestCase(49)]
        [TestCase(50)]
        [TestCase(51)]
        [TestCase(52)]
        [TestCase(53)]
        [TestCase(54)]
        [TestCase(55)]
        public void DetailedReplayUsesTheSameOpeningEncountersAndEndingAsBattle(int number)
        {
            var stage = EarlyGameContent.FindStage(number);
            var completed = new SaveData { ClearedStage = 55 };
            var expected = new System.Collections.Generic.List<StoryScene>(EarlyStory.Opening(stage));
            for (int fight = 0; fight < stage.Encounters.Length; fight++)
            {
                if (!FireDragonFinale.IsFinale(stage, fight)) Assert.IsNull(StoryEpisodes.BeforeFight(stage, fight, completed));
                var scenes = StoryEpisodes.BeforeFight(stage, fight, completed, false, true);
                if (scenes != null) expected.AddRange(scenes);
                if (Showdowns.Is(stage, fight))
                {
                    var script = stage.TrialFor(fight).Script;
                    foreach (var lines in new[] { script.ArmorLines, script.AfterBlow, script.InterruptLines, script.AfterStrike })
                        if (lines != null) expected.Add(new StoryScene { Lines = lines });
                }
                if (number == 30 && fight == 1) expected.Add(new StoryScene { Lines = FireDragonFinale.RetreatLines });
                if (!FireDragonFinale.IsFinale(stage, fight)) Assert.IsNull(StoryEpisodes.BeforeFight(stage, fight, completed, true, true));
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
            Assert.IsNull(EarlyStory.Opening(EarlyGameContent.FindStage(56)));
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
