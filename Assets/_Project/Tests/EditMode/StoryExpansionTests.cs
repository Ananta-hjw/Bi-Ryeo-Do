using System;
using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StoryExpansionTests
    {
        [TestCase(37, 0)] [TestCase(37, 1)] [TestCase(37, 2)]
        [TestCase(38, 0)] [TestCase(38, 1)] [TestCase(38, 2)]
        [TestCase(39, 0)] [TestCase(39, 1)] [TestCase(39, 2)]
        [TestCase(40, 0)] [TestCase(40, 1)] [TestCase(40, 2)]
        [TestCase(41, 0)] [TestCase(41, 1)] [TestCase(41, 2)]
        public void DongjeongExpansionIsClearableAtEachDifficultyWithItsActualActors(int number, int difficulty)
        {
            var stage = EarlyGameContent.FindStage(number).WithDifficulty(difficulty);
            int level = difficulty == 0 ? BalanceSimulation.BotLevel(stage) : (int)Math.Round(StageStars.TargetLevel(number, difficulty));
            var runs = Enumerable.Range(901, 20).Select(seed => BalanceSimulation.Play(stage, BalanceSimulation.Starter(number, level), seed)).ToArray();
            TestContext.Out.WriteLine(number + "/" + difficulty + ": " + runs.Count(x => x.Won) + "/20; actions " + runs.Where(x => x.Won).Select(x => x.Actions).DefaultIfEmpty().Average());
            TestContext.Out.WriteLine("failures " + string.Join(",", runs.Where(x => !x.Won).GroupBy(x => x.FailedFight).Select(g => g.Key + ":" + g.Count())));
            for (int fight = 0; fight < stage.Encounters.Length; fight++)
            {
                var actions = runs.Where(x => x.FightActions.Count > fight).Select(x => x.FightActions[fight]).OrderBy(x => x).ToArray();
                if (actions.Length > 0) TestContext.Out.WriteLine("fight " + fight + ": median " + actions[actions.Length / 2] + "; fast " + StageStars.FastLimit(number, fight, difficulty));
            }
            Assert.GreaterOrEqual(runs.Count(x => x.Won), 17);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void YoungYeonbiNeverBorrowsAdultLevelsGearOrArts(int difficulty)
        {
            var stage = EarlyGameContent.FindStage(37).WithDifficulty(difficulty);
            var plain = BalanceSimulation.Starter(37, 40);
            var grown = SideContentSimulation.Prepared(37, 80, true);
            var first = Roster.CreateStoryParty(stage, 0, plain).Single();
            var second = Roster.CreateStoryParty(stage, 0, grown).Single();
            Assert.AreEqual(first.Stats.MaxHp, second.Stats.MaxHp); Assert.AreEqual(first.Stats.Attack, second.Stats.Attack);
            Assert.AreEqual(first.Stats.Defense, second.Stats.Defense); Assert.AreEqual(first.Stats.Speed, second.Stats.Speed);
            CollectionAssert.AreEqual(first.Skills.Select(x => x.Id), second.Skills.Select(x => x.Id));
            Assert.IsFalse(first.Skills.Any(x => plain.EquippedArts.Contains(x.Id)));
            Assert.IsTrue(first.Skills.Any(x => x.Id == "scene_palhwa"));
            first.Hp = 123; first.Qi = 40; first.Spirit = 35;
            Roster.PrepareStoryEncounter(stage, 1, new[] { first });
            Assert.AreEqual(123, first.Hp); Assert.AreEqual(40, first.Qi); Assert.AreEqual(35, first.Spirit);
            Assert.IsTrue(first.Skills.Any(x => x.Id == "scene_ssangseom"));
            Assert.IsFalse(first.Skills.Any(x => x.Id == "scene_palhwa"));
            var focused = Roster.CreateStoryParty(stage, 1, plain).Single();
            Assert.IsTrue(focused.Skills.Any(x => x.Id == "scene_ssangseom"));
            Assert.IsFalse(focused.Skills.Any(x => x.Id == "scene_palhwa"));
            Assert.IsFalse(plain.CompanionLevels.Any(x => x.Id == first.Id));
        }

        [Test]
        public void HerUnnamedSwordIsVisibleOnlyInTheActualRopeBreakthrough()
        {
            var save = BalanceSimulation.Starter(40, 44); var stage = EarlyGameContent.FindStage(40);
            var earlier = Roster.CreateStoryParty(stage, 2, save).First(x => x.Id == "na_yerin");
            Assert.IsFalse(earlier.Skills.Any(x => x.Id == "scene_silver_snow"));
            var finisher = Roster.CreateStoryParty(stage, 3, save).Single();
            Assert.AreEqual("na_yerin", finisher.Id);
            Assert.IsTrue(finisher.Skills.Any(x => x.Id == "scene_silver_snow"));
            Assert.LessOrEqual(finisher.Skills.Count(x => x.Id != "basic" && x.SpiritCost == 0), Progression.MaxEquippedArts + 1);
            Assert.IsFalse(CompanionGrowth.Create(save, EarlyGameContent.FindCompanion("na_yerin")).Skills.Any(x => x.Id == "scene_silver_snow"));
            Assert.IsNull(Roster.CreateStoryParty(Quests.ToStage(Quests.Find("q_rope_pattern")), 0, save));
            Assert.IsFalse(save.LearnedArts.Contains("scene_silver_snow"));
            Assert.IsFalse(save.EquippedArts.Contains("scene_silver_snow"));
        }

        [Test]
        public void NewJourneyPreservesOldCompletionResumeAndPendingRewards()
        {
            var save = new SaveData { StoryContentRevision = 8, ClearedStage = 36, ProgressStage = 36, ProgressNext = 2, Level = 40, Doo = 12345 };
            for (int number = 1; number <= 36; number++) StageStars.Record(save, number, StageStars.FightCount(number) - 1, 0, 7);
            save.DoneQuests.Add("q_hyeol"); save.SeenMemories.Add("m_hairpin"); save.DoneFortunes.Add("f_wager");
            int stars = StageStars.Total(save);
            StoryContentMigration.Apply(save); StageStars.Sync(save);
            Assert.AreEqual(stars, StageStars.Total(save)); Assert.AreEqual(36, save.ClearedStage);
            Assert.AreEqual(36, save.ProgressStage); Assert.AreEqual(2, save.ProgressNext);
            Assert.AreEqual(12345, save.Doo); Assert.AreEqual(40, save.Level);
            Assert.AreEqual(37, save.HighestUnlockedStage);
            for (int number = 37; number <= 41; number++) Assert.IsFalse(StageStars.StageCleared(save, number, 0));
            CollectionAssert.AreEqual(new[] { "q_hyeol" }, save.DoneQuests);
            CollectionAssert.AreEqual(new[] { "m_hairpin" }, save.SeenMemories);
            CollectionAssert.AreEqual(new[] { "f_wager" }, save.DoneFortunes);
            Assert.AreEqual(13, EarlyGameContent.Stages.Skip(36).Take(5).Sum(x => x.Encounters.Length));
        }

        [Test]
        public void DongjeongStorySeparatesPastKnowledgeAndUnpaidPromisesFromFutureResults()
        {
            string Replay(int number) { return string.Join(" ", StoryEpisodes.Replay(EarlyGameContent.FindStage(number))); }
            var past = Replay(37); var leaders = Replay(38); var contract = Replay(39); var ambush = Replay(40); var door = Replay(41);
            StringAssert.Contains("열한 살", past); StringAssert.Contains("나예린은 아직 그 사실을 몰랐다", past);
            Assert.Less(past.IndexOf("회상 · 천향루"), past.IndexOf("회상 · 붉은 수건"));
            StringAssert.Contains("독니", past); StringAssert.Contains("노사부", past);
            StringAssert.Contains("한 번도 싸우지 않고 결승", leaders); StringAssert.Contains("기권", leaders);
            StringAssert.Contains("독고령을 아직 찾지 못했다", leaders);
            StringAssert.Contains("삼십만 냥", contract); StringAssert.Contains("오십만 냥에 합의", contract);
            StringAssert.Contains("세 번째 참가자도 우승도 아직", contract);
            Assert.IsFalse(contract.Contains("오만 냥의 빚"));
            Assert.IsFalse(ambush.Contains("새 검기라는 정식 무공"));
            StringAssert.Contains("해어화", ambush); StringAssert.Contains("나예린이 섰다", ambush);
            StringAssert.Contains("타격각성 정신봉", door);
            Assert.IsFalse(door.Contains("진비뢰도 열 자루를 돌려"));
            Assert.IsFalse(door.Contains("천겁혈신이 사부였다"));
            Assert.AreEqual(.2f, EarlyGameContent.FindEnemy("baekmu_ju").RetreatHpPct);
        }

        [Test]
        public void DongjeongPartiesKeepTheirAuthoredRolesAndFitTheCommandBar()
        {
            var save = BalanceSimulation.Starter(41, 44);
            var active = save.ActiveCompanions.ToArray();
            for (int number = 37; number <= 41; number++)
            {
                var stage = EarlyGameContent.FindStage(number);
                Assert.AreEqual(stage.Encounters.Length, stage.StoryActors.Length);
                for (int fight = 0; fight < stage.Encounters.Length; fight++)
                {
                    var party = Roster.CreateStoryParty(stage, fight, save);
                    CollectionAssert.AreEqual(stage.StoryActors[fight], party.Select(x => x.Id));
                    Assert.AreEqual(party.Count, party.Select(x => x.Id).Distinct().Count());
                    foreach (var member in party)
                    {
                        Assert.IsTrue(member.StoryControlled);
                        Assert.LessOrEqual(member.Skills.Count(x => x.Id != "basic" && x.SpiritCost <= 0), Progression.MaxEquippedArts + (member.Id == Inventory.Hero ? 0 : 1));
                    }
                }
            }
            CollectionAssert.AreEqual(active, save.ActiveCompanions);
            Assert.IsTrue(save.CompanionLevels.All(x => EarlyGameContent.FindCompanion(x.Id) != null));
            CollectionAssert.AreEqual(new[] { "jang_hong" }, EarlyGameContent.FindStage(38).StoryActors[0]);
            CollectionAssert.AreEqual(new[] { "yun_junho" }, EarlyGameContent.FindStage(38).StoryActors[1]);
            CollectionAssert.AreEqual(new[] { "story_ma_haryeong" }, EarlyGameContent.FindStage(38).StoryActors[2]);
        }

        [TestCase(31, 0)] [TestCase(31, 1)] [TestCase(31, 2)]
        [TestCase(32, 0)] [TestCase(32, 1)] [TestCase(32, 2)]
        [TestCase(33, 0)] [TestCase(33, 1)] [TestCase(33, 2)]
        [TestCase(34, 0)] [TestCase(34, 1)] [TestCase(34, 2)]
        [TestCase(35, 0)] [TestCase(35, 1)] [TestCase(35, 2)]
        [TestCase(36, 0)] [TestCase(36, 1)] [TestCase(36, 2)]
        public void NamchangExpansionIsClearableWithItsActualStoryActors(int number, int difficulty)
        {
            var stage = EarlyGameContent.FindStage(number).WithDifficulty(difficulty);
            int level = difficulty == 0 ? BalanceSimulation.BotLevel(stage) : (int)Math.Round(StageStars.TargetLevel(number, difficulty));
            int count = 20;
            var runs = Enumerable.Range(901, count).Select(seed => BalanceSimulation.Play(stage, BalanceSimulation.Starter(number, level), seed)).ToArray();
            int wins = runs.Count(run => run.Won);
            TestContext.Out.WriteLine(number + "/" + difficulty + ": " + wins + "/" + count + "; actions " + runs.Where(run => run.Won).Select(run => run.Actions).DefaultIfEmpty().Average());
            TestContext.Out.WriteLine("failures " + string.Join(",", runs.Where(run => !run.Won).GroupBy(run => run.FailedFight).Select(group => group.Key + ":" + group.Count())));
            foreach (int fight in Enumerable.Range(0, stage.Encounters.Length))
            {
                var actions = runs.Where(run => run.FightActions.Count > fight).Select(run => run.FightActions[fight]).OrderBy(x => x).ToArray();
                if (actions.Length > 0) TestContext.Out.WriteLine("fight " + fight + ": median " + actions[actions.Length / 2] + "; fast " + StageStars.FastLimit(number, fight, difficulty));
            }
            Assert.GreaterOrEqual(wins, 17);
        }

        [TestCase(31, 2, 2, 1)] [TestCase(32, 2, 3, 0)] [TestCase(33, 3, 3, 2)]
        [TestCase(34, 2, 2, 0)] [TestCase(35, 3, 4, 1)] [TestCase(36, 2, 3, 1)]
        public void RevisionEightKeepsEarnedStarsAndFinalCreditWithoutClaimingNewFights(int number, int oldCount, int final, int inserted)
        {
            var save = new SaveData { StoryContentRevision = 7, ClearedStage = 36, ProgressStage = number, ProgressNext = 1, Doo = 12345, Level = 40 };
            for (int other = 31; other <= 36; other++)
                if (other != number) save.StageStars.Add(new StarEntry { Stage = other, Mask = 1 });
            for (int diff = 0; diff < 3; diff++)
                for (int fight = 0; fight < oldCount; fight++) save.StageStars.Add(new StarEntry { Stage = number, Fight = fight, Diff = diff, Mask = 7 });
            save.DoneQuests.Add("q_hyeol"); save.DoneFortunes.Add("f_pillow"); save.SeenMemories.Add("m_hairpin");
            int earned = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(earned, StageStars.Total(save)); Assert.AreEqual(0, save.ProgressStage);
            Assert.AreEqual(36, save.ClearedStage); Assert.AreEqual(12345, save.Doo); Assert.AreEqual(40, save.Level);
            for (int diff = 0; diff < 3; diff++)
            {
                Assert.AreEqual(7, StageStars.MaskOf(save, number, final, diff));
                Assert.AreEqual(0, StageStars.MaskOf(save, number, inserted, diff));
                Assert.IsTrue(StageStars.StageCleared(save, number, diff));
                Assert.IsTrue(Recommend.IsReplay(save, EarlyGameContent.FindStage(number).WithDifficulty(diff)));
            }
            StoryContentMigration.Apply(save); Assert.AreEqual(earned, StageStars.Total(save));
            CollectionAssert.AreEqual(new[] { "q_hyeol" }, save.DoneQuests);
            CollectionAssert.AreEqual(new[] { "f_pillow" }, save.DoneFortunes);
            CollectionAssert.AreEqual(new[] { "m_hairpin" }, save.SeenMemories);
            var earlier = new SaveData { StoryContentRevision = 7, ProgressStage = 30, ProgressNext = 1 };
            StoryContentMigration.Apply(earlier); Assert.AreEqual(30, earlier.ProgressStage);
        }

        [Test]
        public void PreStarNamchangSaveBackfillsOnlyItsFourteenOriginalWins()
        {
            var save = new SaveData { StoryContentRevision = 7, ClearedStage = 36 };
            StoryContentMigration.Apply(save); StageStars.Sync(save);
            Assert.AreEqual(14, save.StageStars.Where(x => x.Stage >= 31).Sum(x => StageStars.Count(x.Mask)));
            for (int number = 31; number <= 36; number++)
            {
                Assert.IsTrue(StageStars.StageCleared(save, number, 0));
                Assert.IsFalse(StageStars.StageCleared(save, number, 1));
            }
            int total = StageStars.Total(save);
            StoryContentMigration.Apply(save); StageStars.Sync(save); Assert.AreEqual(total, StageStars.Total(save));
        }

        [Test]
        public void FixedStoryPartiesNeverChangeSavedPicksOrAddTemporaryGrowthRecords()
        {
            var save = BalanceSimulation.Starter(36, 40);
            save.ActiveCompanions.Add("im_seongjin"); save.BenchedCompanions.Add("na_yerin");
            var hero = Formation.BuildHero(save);
            int fights = 0;
            for (int number = 31; number <= 36; number++)
            {
                var stage = EarlyGameContent.FindStage(number);
                Assert.AreEqual(stage.Encounters.Length, stage.StoryActors.Length);
                for (int fight = 0; fight < stage.Encounters.Length; fight++)
                {
                    var party = Roster.CreateStoryParty(stage, fight, save); fights++;
                    CollectionAssert.AreEqual(stage.StoryActors[fight], party.Select(x => x.Id));
                    Assert.IsTrue(party.Count >= 1 && party.Count <= 4);
                    Assert.IsTrue(party.All(x => x.StoryControlled));
                    foreach (var actor in party.Where(x => Roster.IsStoryActor(x.Id)))
                    {
                        Assert.AreEqual(hero.Stats.MaxHp, actor.Stats.MaxHp);
                        Assert.AreEqual(hero.Stats.Attack, actor.Stats.Attack);
                        Assert.IsFalse(actor.Skills.Any(skill => save.EquippedArts.Contains(skill.Id)));
                    }
                }
            }
            Assert.AreEqual(23, fights);
            CollectionAssert.AreEqual(new[] { "im_seongjin" }, save.ActiveCompanions);
            CollectionAssert.AreEqual(new[] { "na_yerin" }, save.BenchedCompanions);
            Assert.IsFalse(save.CompanionLevels.Any(x => Roster.IsStoryActor(x.Id)));
            Assert.IsEmpty(save.ArtMastery);
            var stronger = BalanceSimulation.Starter(36, 45);
            Assert.Greater(Roster.CreateStoryParty(EarlyGameContent.FindStage(32), 0, stronger)[0].Stats.Attack,
                Roster.CreateStoryParty(EarlyGameContent.FindStage(32), 0, save)[0].Stats.Attack);
            var quest = Quests.ToStage(Quests.Find("q_hyeol"));
            Assert.IsNull(Roster.CreateStoryParty(quest, 0, save));
            Assert.IsNull(Roster.CreateStoryParty(Tower.ToStage(save, 35), 0, save));
        }

        [Test]
        public void PerspectiveChangesSaveTheNextActorsAndSamePerspectiveKeepsWounds()
        {
            var save = BalanceSimulation.Starter(35, 40); var stage = EarlyGameContent.FindStage(35);
            Assert.IsFalse(Roster.SamePerspective(stage, 0, 1)); Assert.IsTrue(Roster.SamePerspective(stage, 2, 3));
            var same = Roster.CreateStoryParty(stage, 2, save);
            same[0].Hp = 123; same[0].Qi = 0; same[0].Spirit = 50;
            StageProgress.Store(save, 35, 3, same, 2);
            var resumed = Roster.CreateStoryParty(stage, 3, save); StageProgress.Restore(save, resumed);
            Assert.AreEqual(123, resumed[0].Hp); Assert.AreEqual(0, resumed[0].Qi); Assert.AreEqual(50, resumed[0].Spirit);
            var next = Roster.CreateStoryParty(stage, 4, save);
            StageProgress.Store(save, 35, 4, next, 2);
            resumed = Roster.CreateStoryParty(stage, 4, save); StageProgress.Restore(save, resumed);
            Assert.AreEqual("story_yu_gyeongyeong", resumed.Single().Id);
            Assert.AreEqual(resumed[0].Stats.MaxHp, resumed[0].Hp);
            Assert.AreEqual(2, save.ProgressDifficulty);
        }

        [Test]
        public void ImprisonedHeroAndProtectedChildrenDoNotReplaceTheActualCombatants()
        {
            var save = BalanceSimulation.Starter(34, 40); var stage = EarlyGameContent.FindStage(34);
            var party = Roster.CreateStoryParty(stage, 0, save);
            CollectionAssert.AreEqual(new[] { "na_yerin", "hyo_ryong", "story_i_jinseol" }, party.Select(x => x.Id));
            party[0].Spirit = 100;
            var battle = new InteractiveBattle(party, new[] { stage.EnemyFor(stage.Encounters[0][0]).CreateCombatant("test") }, new SeededRandom(3));
            battle.Begin();
            for (int turn = 0; battle.Current != party[0] && !battle.IsOver && turn < 20; turn++) battle.PlayerAct(PlayerCommand.Guard());
            Assert.AreSame(party[0], battle.Current);
            party[0].Spirit = 100;
            Assert.IsFalse(battle.CanJoint(party[1]));
            party[0].StoryControlled = false;
            Assert.IsTrue(battle.CanJoint(party[1]));
            var child = Roster.CreateStoryParty(EarlyGameContent.FindStage(35), 4, save).Single();
            Assert.IsTrue(child.Skills.Where(x => x.Id != "basic").All(x => x.Effect == SkillEffect.Evade));
            CollectionAssert.AreEqual(new[] { "closed_room_threat" }, EarlyGameContent.FindStage(35).Encounters.Last());
            Assert.IsFalse(Realm.HasRealm(EarlyGameContent.FindEnemy("closed_room_threat")));
        }

        [TestCase(20)]
        [TestCase(80)]
        public void BiseolboRequiresBothItsLevelAndHerStoryLesson(int level)
        {
            var save = BalanceSimulation.Starter(32, level);
            var heroine = EarlyGameContent.FindCompanion("na_yerin");
            Assert.IsFalse(CompanionGrowth.Create(save, heroine).Skills.Any(x => x.Id == "ny_biseol" || CompanionTraining.Succeeds(x.Id, "ny_biseol")));
            Assert.IsTrue(CompanionGrowth.PendingArtLines(heroine, level, 31).Any(x => x.StartsWith("비설보 (")));
            save.ClearedStage = 32;
            Assert.IsTrue(CompanionGrowth.Create(save, heroine).Skills.Any(x => x.Id == "ny_biseol" || CompanionTraining.Succeeds(x.Id, "ny_biseol")));
            Assert.IsFalse(CompanionGrowth.PendingArtLines(heroine, level, 32).Any(x => x.StartsWith("비설보 (")));
            Assert.AreSame(EarlyGameContent.FindArt("ny_biseol"), EarlyGameContent.FindArt("ny_biseolbo"));
            Assert.AreEqual(1, EarlyGameContent.Arts.Count(x => x.OwnerId == "na_yerin" && x.Name == "비설보"));
        }

        [Test]
        public void PerspectiveRewardsGrowTheHeroAndRealCompanionsWithoutInventingNpcProgress()
        {
            var stage = EarlyGameContent.FindStage(34);
            var save = BalanceSimulation.Starter(34, BalanceSimulation.BotLevel(stage));
            CompanionGrowth.EnsureRecords(save);
            long before = CompanionGrowth.ExpOf(save, "na_yerin"); int level = CompanionGrowth.LevelOf(save, "na_yerin");
            long heroExp = save.Exp; int heroLevel = save.Level;
            long expected = 0;
            for (int fight = 0; fight < stage.Encounters.Length; fight++)
            { Recommend.FightReward(save, stage, fight, false, out long exp, out long doo); expected += exp; }
            Assert.IsTrue(BalanceSimulation.Play(stage, save, 901, true).Won);
            long Growth(int from, long expFrom, int to, long expTo)
            {
                long total = expTo - expFrom;
                for (int l = from; l < to; l++) total += Progression.ExpToNext(l);
                return total;
            }
            Assert.AreEqual(expected, Growth(heroLevel, heroExp, save.Level, save.Exp));
            Assert.AreEqual(expected, Growth(level, before, CompanionGrowth.LevelOf(save, "na_yerin"), CompanionGrowth.ExpOf(save, "na_yerin")));
            Assert.IsTrue(save.CompanionLevels.All(x => EarlyGameContent.FindCompanion(x.Id) != null));
            Assert.IsTrue(save.ArtMastery.All(x => EarlyGameContent.FindArt(x.Id) != null));
        }

        [Test]
        public void NamchangStoryKeepsKnownOutcomesAndLeavesFutureRevelationsClosed()
        {
            string Replay(int number) { return string.Join(" ", StoryEpisodes.Replay(EarlyGameContent.FindStage(number))); }
            var bank = Replay(31); var training = Replay(32); var accusation = Replay(33); var choice = Replay(34); var dawn = Replay(35); var departure = Replay(36);
            StringAssert.Contains("겨울을 지나 봄", bank);
            StringAssert.Contains("답하지 못한 과제", bank);
            StringAssert.Contains("용린폭", bank);
            StringAssert.Contains("아이들", bank);
            StringAssert.Contains("비설보", training);
            StringAssert.Contains("최종 합격은 아니었다", training);
            StringAssert.Contains("남궁상", training);
            StringAssert.Contains("다섯 건", accusation);
            StringAssert.Contains("사건을 나눠", accusation);
            StringAssert.Contains("피살당했다는 소식", accusation);
            StringAssert.Contains("위장", choice);
            StringAssert.Contains("스스로 움직였으니 성공", choice);
            StringAssert.Contains("십삼혈은 전멸", dawn);
            StringAssert.Contains("윤이정의 최종 생사는 남았다", dawn);
            StringAssert.Contains("목덜미 뒤에서 멈췄다", departure);
            StringAssert.Contains("나예린|연비!", departure);
            Assert.IsFalse(departure.Contains("연비의 정체는 비류연"));
            Assert.IsFalse(departure.Contains("청룡은장의 재건이 끝났다"));
            Assert.AreEqual(TrialKind.Survive, EarlyGameContent.FindStage(35).TrialFor(4).Kind);
            Assert.AreEqual(TrialKind.Survive, EarlyGameContent.FindStage(36).TrialFor(3).Kind);
        }

        [TestCase("f_unfamiliar_staff")] [TestCase("f_leave_nest")] [TestCase("f_free_help")]
        [TestCase("f_unfinished_memory")] [TestCase("f_spared_witness")] [TestCase("f_asked_help")] [TestCase("f_hidden_hand")]
        [TestCase("f_rule_and_reason")] [TestCase("f_own_answer")] [TestCase("f_curved_wind")] [TestCase("f_unfinished_name")] [TestCase("f_empty_cup")] [TestCase("f_trusted_back")] [TestCase("f_life_pledge")] [TestCase("f_captain_order")]
        [TestCase("f_red_signal")] [TestCase("f_guest_map")] [TestCase("f_samjae_question")]
        [TestCase("f_staked_name")] [TestCase("f_unopened_door")]
        [TestCase("f_unanswered_commission")] [TestCase("f_sealed_legacy")] [TestCase("f_two_cases")]
        [TestCase("f_rebuild_credit")] [TestCase("f_own_choice")]
        [TestCase("f_inherited_weight")] [TestCase("f_fifth_contact")] [TestCase("f_living_captives")] [TestCase("f_retreat_path")] [TestCase("f_bent_hunt")] [TestCase("f_shared_command")] [TestCase("f_returned_choice")] [TestCase("f_red_flower_trace")] [TestCase("f_southern_invitation")]
        public void ReanalysisFortunesDoNotPayAgainOrUnlockBeforeTheirChapters(string id)
        {
            var fortune = Fortunes.Find(id);
            var save = new SaveData { Level = 35, ClearedStage = fortune.UnlockStage - 1 };
            Assert.IsFalse(Fortunes.Open(save).Contains(fortune));
            save.ClearedStage++;
            Assert.IsTrue(Fortunes.Open(save).Contains(fortune));
            Assert.IsNotEmpty(Fortunes.Resolve(save, fortune, fortune.Choices[0], new[] { "na_yerin" }));
            long exp = save.Exp, doo = save.Doo;
            Assert.IsEmpty(Fortunes.Resolve(save, fortune, fortune.Choices[1], new[] { "na_yerin" }));
            Assert.AreEqual(exp, save.Exp); Assert.AreEqual(doo, save.Doo);
        }

        [TestCase("m_refused_name")] [TestCase("m_wings_granted")] [TestCase("m_shared_ice")]
        [TestCase("m_thanks_ryuyeon")] [TestCase("m_black_horn_trace")]
        [TestCase("m_opened_eyes")] [TestCase("m_self_return")] [TestCase("m_sister_hand")] [TestCase("m_trembling_hand")] [TestCase("m_stood_with_you")] [TestCase("m_umbrella_question")]
        [TestCase("m_red_contract")] [TestCase("m_returned_hawk")] [TestCase("m_market_stroll")]
        [TestCase("m_silver_step")] [TestCase("m_facing_fear")]
        [TestCase("m_after_fire")] [TestCase("m_asked_for_help")]
        [TestCase("m_face_in_quiet")] [TestCase("m_chosen_seat")] [TestCase("m_sister_responsibility")] [TestCase("m_inn_certainty")] [TestCase("m_storm_wings")] [TestCase("m_returned_samae")]
        public void ReanalysisMemoriesPayOnlyAfterTheirStoryAndOnlyOnce(string id)
        {
            var memory = Memories.Find(id);
            var save = new SaveData { ClearedStage = memory.UnlockStage - 1 };
            Assert.IsEmpty(Memories.Complete(save, id));
            save.ClearedStage++;
            Assert.IsNotEmpty(Memories.Complete(save, id));
            long doo = save.Doo; int points = Bonds.Points(save, "na_yerin");
            Assert.IsEmpty(Memories.Complete(save, id));
            Assert.AreEqual(doo, save.Doo); Assert.AreEqual(points, Bonds.Points(save, "na_yerin"));
        }

        [TestCase(26, 3, 5, 0)]
        [TestCase(27, 3, 6, 0)]
        [TestCase(28, 4, 4, 1)]
        [TestCase(29, 5, 6, 3)]
        [TestCase(30, 3, 3, 1)]
        public void RevisionSevenPreservesCompletionButLeavesNewTrialsUnclaimed(int number, int oldCount, int final, int unplayed)
        {
            var save = new SaveData { StoryContentRevision = 6, ClearedStage = 30, Level = 60, Doo = 12345, ProgressStage = number, ProgressNext = oldCount - 1 };
            for (int diff = 0; diff < 3; diff++)
                for (int fight = 0; fight < oldCount; fight++)
                    save.StageStars.Add(new StarEntry { Stage = number, Fight = fight, Diff = diff, Mask = 7 });
            // Other cleared chapters already have records; isolate the edited chapter from pre-star backfill.
            for (int otherNumber = 26; otherNumber <= 30; otherNumber++)
                if (otherNumber != number) save.StageStars.Add(new StarEntry { Stage = otherNumber, Fight = 0, Mask = 1 });
            save.DoneQuests.Add("q_salgwi"); save.SeenMemories.Add("m_blood"); save.DoneFortunes.Add("f_eve");
            int total = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(total, StageStars.Total(save)); Assert.AreEqual(0, save.ProgressStage);
            Assert.AreEqual(30, save.ClearedStage); Assert.AreEqual(60, save.Level); Assert.AreEqual(12345, save.Doo);
            for (int diff = 0; diff < 3; diff++)
            {
                var stage = EarlyGameContent.FindStage(number).WithDifficulty(diff);
                Assert.AreEqual(7, StageStars.MaskOf(save, number, final, diff));
                Assert.AreEqual(0, StageStars.MaskOf(save, number, unplayed, diff));
                Assert.IsTrue(StageStars.StageCleared(save, number, diff));
                for (int fight = 0; fight < stage.Encounters.Length; fight++)
                    CollectionAssert.AreEqual(GearDrops.Roll(number, diff, false, new SeededRandom(42)).Select(x => x.Id + ":" + x.Count),
                        GearDrops.RollFight(save, stage, fight, new SeededRandom(42)).Select(x => x.Id + ":" + x.Count));
            }
            CollectionAssert.AreEqual(new[] { "q_salgwi" }, save.DoneQuests);
            CollectionAssert.AreEqual(new[] { "m_blood" }, save.SeenMemories);
            CollectionAssert.AreEqual(new[] { "f_eve" }, save.DoneFortunes);
            StoryContentMigration.Apply(save); Assert.AreEqual(total, StageStars.Total(save));
            var other = new SaveData { StoryContentRevision = 6, ProgressStage = 25, ProgressNext = 1 };
            StoryContentMigration.Apply(other); Assert.AreEqual(25, other.ProgressStage);
        }

        [Test]
        public void PreStarHwasanSaveBackfillsOnlyOriginalWinsWithoutClaimingInsertedFights()
        {
            var save = new SaveData { StoryContentRevision = 6, ClearedStage = 30, Level = 60, Doo = 12345 };
            StoryContentMigration.Apply(save);
            Assert.AreEqual(18, save.StageStars.Sum(x => StageStars.Count(x.Mask)));
            var expected = new Dictionary<int, int[]> {
                { 26, new[] { 3, 4, 5 } }, { 27, new[] { 2, 4, 6 } }, { 28, new[] { 0, 2, 3, 4 } },
                { 29, new[] { 0, 1, 2, 4, 6 } }, { 30, new[] { 0, 2, 3 } } };
            StageStars.Sync(save);
            foreach (var pair in expected)
            {
                Assert.IsTrue(StageStars.StageCleared(save, pair.Key, 0));
                for (int fight = 0; fight < StageStars.FightCount(pair.Key); fight++)
                    Assert.AreEqual(pair.Value.Contains(fight) ? 1 : 0, StageStars.MaskOf(save, pair.Key, fight, 0), pair.Key + "/" + fight);
                Assert.IsFalse(StageStars.StageCleared(save, pair.Key, 1));
            }
            int total = StageStars.Total(save);
            StoryContentMigration.Apply(save); StageStars.Sync(save);
            Assert.AreEqual(total, StageStars.Total(save));
            Assert.AreEqual(12345, save.Doo); Assert.AreEqual(60, save.Level);
        }

        [Test]
        public void HwasanExpansionUsesTheActualLeaderAndKeepsTrialsNonlethal()
        {
            var escape = EarlyGameContent.FindStage(26);
            Assert.AreEqual(6, escape.Encounters.Length);
            CollectionAssert.AreEqual(new[] { "salhon_sipgwi" }, escape.Encounters.Last());
            Assert.AreEqual(TrialKind.Survive, escape.TrialFor(1).Kind);
            CollectionAssert.AreEqual(escape.Encounters[3], Quests.ToStage(Quests.Find("q_salgwi")).Encounters[0]);
            var seven = EarlyGameContent.FindStage(27);
            Assert.AreEqual(7, seven.Encounters.Length);
            Assert.IsTrue(seven.Encounters.All(x => x.Length == 1));
            Assert.IsTrue(seven.Trials.All(t => t.Kind == TrialKind.Survive));
            Assert.AreEqual(TrialKind.Deadline, EarlyGameContent.FindStage(28).TrialFor(0).Kind);
            var trial = EarlyGameContent.FindStage(29);
            Assert.AreEqual(TrialKind.Survive, trial.TrialFor(0).Kind);
            Assert.AreEqual(TrialKind.Harmony, trial.TrialFor(2).Kind);
            CollectionAssert.AreEqual(new[] { "mask_bitan" }, trial.Encounters[4]);
            CollectionAssert.AreEqual(new[] { "mu_jeong" }, trial.Encounters[5]);
            CollectionAssert.AreEqual(new[] { "witness_pursuer" }, trial.Encounters[6]);
            var fire = EarlyGameContent.FindStage(30).WithDifficulty(1);   // 일반 30 ends in the scripted finale (FireDragonFinaleTests)
            CollectionAssert.AreEqual(new[] { "bi_daegongja" }, fire.Encounters[1]);
            Assert.AreEqual(TrialKind.Survive, fire.TrialFor(3).Kind);
            Assert.IsFalse(GearDrops.IsBossFight(fire, 3));
            foreach (var id in seven.Encounters.SelectMany(x => x).Concat(new[] { "bi_first_clash", "wiji_hyeolpok", "mu_jeong", "witness_pursuer" }))
                Assert.Greater(EarlyGameContent.FindEnemy(id).RetreatHpPct, 0f, id);
            Assert.AreEqual("천율패 기록", EarlyGameContent.FindItem("cheonyul_pae").Name);
            Assert.IsTrue(Genders.IsFemale(EarlyGameContent.FindEnemy("gyook_public").ArtKey));
        }

        [TestCase(26, 0)]
        [TestCase(26, 1)]
        [TestCase(26, 2)]
        [TestCase(27, 0)]
        [TestCase(27, 1)]
        [TestCase(27, 2)]
        [TestCase(28, 0)]
        [TestCase(28, 1)]
        [TestCase(28, 2)]
        [TestCase(29, 0)]
        [TestCase(29, 1)]
        [TestCase(29, 2)]
        [TestCase(30, 0)]
        [TestCase(30, 1)]
        [TestCase(30, 2)]
        public void HwasanExpansionIsClearableAtEachDifficultyPreparationLevel(int number, int difficulty)
        {
            var stage = EarlyGameContent.FindStage(number).WithDifficulty(difficulty);
            int level = difficulty == 0 ? BalanceSimulation.BotLevel(stage) : (int)Math.Round(StageStars.TargetLevel(number, difficulty));
            int count = difficulty == 0 ? 20 : 10;
            var runs = Enumerable.Range(901, count).Select(seed => BalanceSimulation.Play(stage, BalanceSimulation.Starter(number, level), seed)).ToArray();
            int wins = runs.Count(run => run.Won);
            TestContext.Out.WriteLine(number + "/" + difficulty + ": " + wins + "/" + count + "; actions " + runs.Where(run => run.Won).Select(run => run.Actions).DefaultIfEmpty().Average());
            TestContext.Out.WriteLine("failures " + string.Join(",", runs.Where(run => !run.Won).GroupBy(run => run.FailedFight).Select(group => group.Key + ":" + group.Count())));
            Assert.GreaterOrEqual(wins, (int)Math.Ceiling(count * .85));
            for (int fight = 0; fight < stage.Encounters.Length; fight++)
            {
                var actions = runs.Where(run => run.Won).Select(run => run.FightActions[fight]).OrderBy(x => x).ToArray();
                TestContext.Out.WriteLine("fight " + fight + ": median " + actions[actions.Length / 2] + "; fast " + StageStars.FastLimit(number, fight, difficulty));
            }
        }

        [TestCase(21, 2, 2, -1)]
        [TestCase(22, 2, 5, 0)]
        [TestCase(23, 2, 3, 2)]
        [TestCase(24, 2, 3, 2)]
        [TestCase(25, 2, 3, 2)]
        public void RevisionSixPreservesClearsWithoutGrantingInsertedFightRewards(int number, int oldFinal, int final, int unplayed)
        {
            var save = new SaveData { StoryContentRevision = 5, ClearedStage = 25, ProgressStage = number, ProgressNext = 2, Level = 60, Doo = 12345 };
            for (int diff = 0; diff < 3; diff++)
                for (int fight = 0; fight <= oldFinal; fight++)
                    save.StageStars.Add(new StarEntry { Stage = number, Fight = fight, Diff = diff, Mask = 7 });
            save.DoneQuests.Add("q_hyeolma"); save.DoneFortunes.Add("f_mirror_return"); save.SeenMemories.Add("m_talk");
            int total = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(total, StageStars.Total(save));
            Assert.AreEqual(0, save.ProgressStage);
            Assert.AreEqual(25, save.ClearedStage); Assert.AreEqual(60, save.Level); Assert.AreEqual(12345, save.Doo);
            for (int diff = 0; diff < 3; diff++)
            {
                var stage = EarlyGameContent.FindStage(number).WithDifficulty(diff);
                Assert.AreEqual(7, StageStars.MaskOf(save, number, final, diff));
                Assert.IsTrue(StageStars.StageCleared(save, number, diff));
                Assert.IsTrue(Recommend.IsReplay(save, stage));
                for (int fight = 0; fight < stage.Encounters.Length; fight++)
                {
                    var actual = GearDrops.RollFight(save, stage, fight, new SeededRandom(42));
                    var repeat = GearDrops.Roll(number, diff, false, new SeededRandom(42));
                    CollectionAssert.AreEqual(repeat.Select(x => x.Id + ":" + x.Count), actual.Select(x => x.Id + ":" + x.Count));
                }
                if (unplayed >= 0) Assert.AreEqual(0, StageStars.MaskOf(save, number, unplayed, diff));
            }
            Assert.AreEqual(1, save.DoneQuests.Count); Assert.AreEqual(1, save.DoneFortunes.Count); Assert.AreEqual(1, save.SeenMemories.Count);
            StoryContentMigration.Apply(save); Assert.AreEqual(total, StageStars.Total(save));
            var untouched = new SaveData { StoryContentRevision = 5, ProgressStage = 20, ProgressNext = 1 };
            StoryContentMigration.Apply(untouched); Assert.AreEqual(20, untouched.ProgressStage);
        }

        [Test]
        public void VolumesElevenToThirteenKeepEscortTrialsAndLivingOpponentsDistinct()
        {
            var bandits = EarlyGameContent.FindStage(21);
            CollectionAssert.AreEqual(new[] { "im_gae" }, bandits.Encounters[1]);
            CollectionAssert.AreEqual(new[] { "lee_songhak" }, bandits.Encounters[2]);
            var valley = EarlyGameContent.FindStage(22);
            Assert.AreEqual(6, valley.Encounters.Length);
            Assert.AreEqual(TrialKind.Survive, valley.TrialFor(0).Kind);
            Assert.AreEqual(5, valley.Encounters[1].Length + valley.Encounters[2].Length);
            CollectionAssert.AreEqual(valley.Encounters[3], Quests.ToStage(Quests.Find("q_hyeolma")).Encounters[0]);
            var masked = EarlyGameContent.FindStage(24);
            Assert.IsTrue(GearDrops.IsBossFight(masked, 2));
            Assert.IsFalse(GearDrops.IsBossFight(masked, 3));
            Assert.AreEqual(TrialKind.Survive, masked.TrialFor(3).Kind);
            var rescue = EarlyGameContent.FindStage(25);
            Assert.AreEqual(TrialKind.Survive, rescue.TrialFor(3).Kind);
            CollectionAssert.Contains(rescue.Encounters[1], "cheonsal_hwapyeong");
            CollectionAssert.Contains(rescue.Encounters[1], "jisal_hwapyeong");
            foreach (string id in new[] { "im_deokseong_story", "im_gae", "lee_songhak", "cheonghong_ssanggaksa", "mo_saryeong", "gong_pae", "maehwa_mask_trial", "cheonsal_hwapyeong", "jisal_hwapyeong", "baek_son_gosu" })
                Assert.Greater(EarlyGameContent.FindEnemy(id).RetreatHpPct, 0f, id);
        }

        [TestCase(15, 4, 3, 4, 3)]
        [TestCase(18, 3, 2, 3, 2)]
        [TestCase(19, 5, 4, 5, 2)]
        public void RevisionFiveKeepsFinalCreditAndLeavesInsertedFightsUnclaimed(int number, int oldCount, int oldLast, int newLast, int added)
        {
            var save = new SaveData { StoryContentRevision = 4, ClearedStage = 20, ProgressStage = number, ProgressNext = oldLast, Doo = 12345, Level = 24 };
            for (int diff = 0; diff < 3; diff++)
                for (int fight = 0; fight < oldCount; fight++)
                    save.StageStars.Add(new StarEntry { Stage = number, Fight = fight, Diff = diff, Mask = 7 });
            save.DoneQuests.Add("q_baegin"); save.SeenMemories.Add("m_visit");
            int stars = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            for (int diff = 0; diff < 3; diff++)
            {
                Assert.AreEqual(7, StageStars.MaskOf(save, number, newLast, diff));
                Assert.AreEqual(0, StageStars.MaskOf(save, number, added, diff));
                Assert.IsTrue(StageStars.StageCleared(save, number, diff));
            }
            if (number == 19) Assert.AreEqual(0, StageStars.MaskOf(save, 19, 3, 0));
            Assert.AreEqual(stars, StageStars.Total(save));
            Assert.AreEqual(0, save.ProgressStage);
            Assert.AreEqual(20, save.ClearedStage); Assert.AreEqual(12345, save.Doo); Assert.AreEqual(24, save.Level);
            CollectionAssert.AreEqual(new[] { "q_baegin" }, save.DoneQuests);
            CollectionAssert.AreEqual(new[] { "m_visit" }, save.SeenMemories);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(stars, StageStars.Total(save));
        }

        [Test]
        public void VolumesEightToTenKeepNonlethalAndPrivateGoalsDistinct()
        {
            var cavalry = EarlyGameContent.FindStage(15);
            Assert.AreEqual(5, cavalry.Encounters.Length);
            CollectionAssert.AreEqual(new[] { "wi_musang" }, cavalry.Encounters[3]);
            CollectionAssert.AreEqual(new[] { "gu_cheonhak" }, cavalry.Encounters[4]);
            Assert.AreEqual(TrialKind.Survive, EarlyGameContent.FindStage(17).TrialFor(1).Kind);
            var ice = EarlyGameContent.FindStage(18);
            Assert.AreEqual(4, ice.Encounters.Length);
            Assert.AreEqual(TrialKind.Survive, ice.TrialFor(0).Kind);
            Assert.AreEqual(TrialKind.Survive, ice.TrialFor(2).Kind);
            Assert.IsFalse(GearDrops.IsBossFight(ice, 2));
            Assert.IsTrue(GearDrops.IsBossFight(ice, 3));
            var trial = EarlyGameContent.FindStage(19);
            Assert.AreEqual(6, trial.Encounters.Length);
            foreach (int i in new[] { 0, 2, 3, 4 }) Assert.AreEqual(TrialKind.Survive, trial.TrialFor(i).Kind);
            foreach (string id in new[] { "gu_cheonhak", "wi_musang", "baek_tanjeo", "pae_hyeokseong", "hyo_ryong_rope" })
                Assert.Greater(EarlyGameContent.FindEnemy(id).RetreatHpPct, 0f, id);
            Assert.IsNull(EarlyGameContent.FindArt("saengsa_seomroe"));   // 섬뢰창 was taken out with the 66-stage ladder (Mastery.RemovedArts)
            Assert.IsFalse(EarlyGameContent.StageRewards[20].Any(item => item.Id == "mukrin_talisman"));
            string story = string.Join(" ", StoryEpisodes.Replay(EarlyGameContent.FindStage(20)));
            Assert.Less(story.IndexOf("이십일 일째", StringComparison.Ordinal), story.IndexOf("이십육 일째", StringComparison.Ordinal));
            Assert.Less(story.IndexOf("나예린이 먼저 입을 맞췄다", StringComparison.Ordinal), story.IndexOf("벽이 뚫리고", StringComparison.Ordinal));
            StringAssert.Contains("최종 집계", story);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void RevisedChaptersPayOnTheirOwnDifficultyAndReplayRate(int difficulty)
        {
            foreach (int number in new[] { 15, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41 })
            {
                var stage = EarlyGameContent.FindStage(number).WithDifficulty(difficulty);
                Recommend.StageReward(new SaveData(), stage, out long exp, out long doo);
                var completed = new SaveData { ClearedStage = number };
                StageStars.Record(completed, number, stage.Encounters.Length - 1, difficulty, 1);
                Recommend.StageReward(completed, stage, out long repeatExp, out long repeatDoo);
                Assert.Greater(exp, repeatExp); Assert.Greater(doo, repeatDoo);
                for (int i = 0; i < stage.Encounters.Length; i++)
                {
                    Recommend.FightReward(new SaveData(), stage, i, false, out long fightExp, out long fightDoo);
                    Assert.Greater(fightExp, 0); Assert.Greater(fightDoo, 0);
                }
                TestContext.Out.WriteLine(number + "/" + difficulty + ": EXP " + exp + "; Doo " + doo);
            }
        }

        [Test]
        public void VolumesFourToSevenSeparateEscapeRetreatAndMentalRestoration()
        {
            var infiltration = EarlyGameContent.FindStage(10);
            Assert.IsTrue(infiltration.Trials.All(t => t.Kind == TrialKind.Survive));
            var wudang = EarlyGameContent.FindStage(14);
            Assert.AreEqual(11, wudang.Encounters.Length);
            CollectionAssert.AreEqual(new[] { "chohon_gwigeom", "chohon_gwigeom" }, wudang.Encounters[7]);
            CollectionAssert.AreEqual(new[] { "myeongbu_jang", "myeongbu_jang" }, wudang.Encounters[8]);
            Assert.AreEqual(TrialKind.Survive, wudang.TrialFor(6).Kind);
            Assert.AreEqual(TrialKind.Survive, wudang.TrialFor(9).Kind);
            CollectionAssert.AreEqual(new[] { "cheonsal", "jisal" }, wudang.Encounters.Last());
            foreach (string id in new[] { "chohon_gwigeom", "myeongbu_jang", "cheonsal", "jisal" })
                Assert.Greater(EarlyGameContent.FindEnemy(id).RetreatHpPct, 0f, id);
            string text = string.Join(" ", StoryEpisodes.Replay(wudang));
            Assert.Less(text.IndexOf("윤준호는 아홉 시진", StringComparison.Ordinal), text.IndexOf("열두 시진", StringComparison.Ordinal));
            StringAssert.Contains("갈효봉|효룡", text);
            StringAssert.Contains("마진가는 학생들의 증언을 믿고 거절했다", text);
        }

        [Test]
        public void RevisionFourPreservesEveryDifficultyAndDoesNotAwardNewFightOrMemoryCredit()
        {
            var save = new SaveData { StoryContentRevision = 3, ClearedStage = 20, ProgressStage = 14, ProgressNext = 6, Doo = 12345, Level = 20 };
            for (int diff = 0; diff < 3; diff++)
                for (int fight = 0; fight < 8; fight++)
                    save.StageStars.Add(new StarEntry { Stage = 14, Fight = fight, Diff = diff, Mask = 7 });
            save.SeenMemories.Add("m_cave");
            save.DoneFortunes.Add("f_cave");
            int total = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(total, StageStars.Total(save));
            foreach (int diff in new[] { 0, 1, 2 })
            {
                Assert.IsTrue(StageStars.StageCleared(save, 14, diff));
                foreach (int added in new[] { 2, 5, 8 }) Assert.AreEqual(0, StageStars.MaskOf(save, 14, added, diff));
                Assert.AreEqual(7, StageStars.MaskOf(save, 14, 10, diff));
            }
            Assert.AreEqual(0, save.ProgressStage);
            Assert.AreEqual(20, save.ClearedStage); Assert.AreEqual(12345, save.Doo); Assert.AreEqual(20, save.Level);
            CollectionAssert.AreEqual(new[] { "m_cave" }, save.SeenMemories);
            CollectionAssert.AreEqual(new[] { "f_cave" }, save.DoneFortunes);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(total, StageStars.Total(save));
        }

        [TestCase(10, 1)]
        [TestCase(10, 2)]
        [TestCase(14, 1)]
        [TestCase(14, 2)]
        [TestCase(15, 1)]
        [TestCase(15, 2)]
        [TestCase(16, 1)]
        [TestCase(16, 2)]
        [TestCase(17, 1)]
        [TestCase(17, 2)]
        [TestCase(18, 1)]
        [TestCase(18, 2)]
        [TestCase(19, 1)]
        [TestCase(19, 2)]
        [TestCase(20, 1)]
        [TestCase(20, 2)]
        [TestCase(21, 1)]
        [TestCase(21, 2)]
        [TestCase(22, 1)]
        [TestCase(22, 2)]
        [TestCase(23, 1)]
        [TestCase(23, 2)]
        [TestCase(24, 1)]
        [TestCase(24, 2)]
        [TestCase(25, 1)]
        [TestCase(25, 2)]
        public void RevisedChapterDifficultyStillSupportsItsPreparedBuild(int number, int difficulty)
        {
            var stage = EarlyGameContent.FindStage(number).WithDifficulty(difficulty);
            int wins = Enumerable.Range(801, 10).Count(seed => BalanceSimulation.Play(stage,
                BalanceSimulation.Starter(number, (int)Math.Round(StageStars.TargetLevel(number, difficulty))), seed).Won);
            TestContext.Out.WriteLine(number + " difficulty " + difficulty + ": " + wins + "/10");
            Assert.GreaterOrEqual(wins, 8);
        }

        [TestCase(10)]
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
        public void RevisedChaptersRemainClearableWithPreparedStartKitBuilds(int number)
        {
            var stage = EarlyGameContent.FindStage(number);
            int wins = Enumerable.Range(701, 20).Count(seed => BalanceSimulation.Play(stage,
                BalanceSimulation.Starter(number, BalanceSimulation.BotLevel(stage)), seed).Won);
            TestContext.Out.WriteLine(number + ": revised chapter " + wins + "/20");
            Assert.GreaterOrEqual(wins, 17);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void AddedWudangFightsPayAtEachDifficultyWithoutDilutingOldShares(int difficulty)
        {
            var stage = EarlyGameContent.FindStage(14).WithDifficulty(difficulty);
            var save = new SaveData();
            foreach (int fight in new[] { 2, 5, 8 })
            {
                Recommend.FightReward(save, stage, fight, false, out long exp, out long doo);
                Assert.Greater(exp, 0); Assert.Greater(doo, 0);
                Recommend.FightReward(save, stage, fight, true, out long replayExp, out long replayDoo);
                Assert.Less(replayExp, exp); Assert.Less(replayDoo, doo);
            }
            Recommend.StageReward(save, stage, out long totalExp, out long totalDoo);
            TestContext.Out.WriteLine("Wudang difficulty " + difficulty + ": EXP " + totalExp + ", Doo " + totalDoo);
        }

        [Test]
        public void ExpandedStoryUsesActualOpponentsWithoutRepeatedOrCombinedTournamentDuels()
        {
            var escort = EarlyGameContent.FindStage(2);
            Assert.AreEqual(5, escort.Encounters.Length);
            // 전투 인원 확장 adds 표사 followers behind the real opponents
            CollectionAssert.AreEqual(new[] { "deung_yeoun", "deung_yeoho" }, escort.Encounters[3].Take(2));
            Assert.AreEqual("jang_uyang", escort.Encounters[4][0]);
            Assert.IsTrue(escort.Encounters[4].Skip(1).All(id => id == "biaosa"));
            var tournament = EarlyGameContent.FindStage(12);
            CollectionAssert.AreEqual(new[] { "dan_pyeong", "jo_yeonil", "jin_pae", "ilyeon", "eonga_yeong", "jeon_okgi" }, tournament.Encounters.SelectMany(x => x));
            Assert.IsTrue(tournament.Encounters.All(x => x.Length == 1));
            CollectionAssert.AreEqual(new[] { "dang_munceon" }, EarlyGameContent.FindStage(13).Encounters[0]);
            Assert.AreEqual(66, EarlyGameContent.Stages.Length);
            CollectionAssert.AreEqual(Enumerable.Range(1, 36), EarlyGameContent.Stages.Take(36).Select(x => x.Number));
        }

        [Test]
        public void SaveMigrationPreservesEarnedStarsBossWinsAndProgressionAndIsIdempotent()
        {
            var save = new SaveData { StoryContentRevision = 0, ClearedStage = 13, Level = 18, Doo = 12345, ProgressStage = 12, ProgressNext = 1 };
            save.StageStars.AddRange(new[] {
                new StarEntry { Stage = 2, Fight = 2, Diff = 1, Mask = 7 },
                new StarEntry { Stage = 11, Fight = 0, Mask = 7 },
                new StarEntry { Stage = 11, Fight = 1, Mask = 3 },
                new StarEntry { Stage = 11, Fight = 2, Mask = 7 },
                new StarEntry { Stage = 12, Fight = 2, Diff = 2, Mask = 7 } });
            int stars = StageStars.Total(save);
            save.ClaimedStarChests.Add(15);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(stars, StageStars.Total(save));
            Assert.AreEqual(7, StageStars.MaskOf(save, 2, 4, 1));
            Assert.AreEqual(7, StageStars.MaskOf(save, 12, 5, 2));
            Assert.IsTrue(StageStars.StageCleared(save, 11, 0));
            Assert.AreEqual(0, save.ProgressStage);
            Assert.AreEqual(13, save.ClearedStage);
            Assert.AreEqual(18, save.Level);
            Assert.AreEqual(12345, save.Doo);
            CollectionAssert.Contains(save.ClaimedStarChests, 15);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(stars, StageStars.Total(save));
            Assert.AreEqual(5, save.RetiredStoryStars);
            var fresh = new SaveData { StoryContentRevision = 0 }; StoryContentMigration.Apply(fresh);
            Assert.AreEqual(0, StageStars.Total(fresh));
        }

        [Test]
        public void FormerSideStoriesAreRequiredMainStageFightsWithNormalRewardsAndResume()
        {
            var admission = EarlyGameContent.FindStage(7);
            Assert.AreEqual(8, admission.Encounters.Length);
            CollectionAssert.AreEqual(new[] { "admit_jeomchang", "admit_hwasan", "admit_geumbok", "admit_eonga" }, admission.Encounters.Take(4).SelectMany(x => x));
            CollectionAssert.AreEqual(new[] { "no_jusaeng", "chae_hagun" }, EarlyGameContent.FindStage(11).Encounters[0]);
            var final = EarlyGameContent.FindStage(13);
            Assert.AreEqual(4, final.Encounters.Length);
            Assert.AreEqual(TrialKind.Survive, final.TrialFor(1).Kind);
            Assert.AreEqual(TrialKind.Survive, final.TrialFor(2).Kind);
            var save = BalanceSimulation.Starter(13, 20);
            StageProgress.Store(save, 13, 2, BalanceSimulation.Party(save, final));
            Assert.IsTrue(StageProgress.Has(save, 13));
            foreach (int number in new[] { 7, 11, 13 })
            {
                var stage = EarlyGameContent.FindStage(number);
                for (int fight = 0; fight < stage.Encounters.Length; fight++)
                {
                    Recommend.FightReward(save, stage, fight, false, out long exp, out long doo);
                    Assert.Greater(exp, 0); Assert.Greater(doo, 0);
                    Assert.IsFalse(stage.IsQuest || stage.IsTower);
                }
            }
        }

        [TestCase(2, 9.0 / 7.0)]
        [TestCase(7, 12.0 / 8.0)]
        [TestCase(11, 7.0 / 3.0)]
        [TestCase(12, 8.0 / 6.0)]
        [TestCase(13, 6.0 / 4.0)]
        public void AddedFightsIncreaseTheStageRewardRatherThanDilutingExistingPayouts(int number, double factor)
        {
            var stage = EarlyGameContent.FindStage(number);
            var baseline = stage.WithDifficulty(0); baseline.RewardWeightBudget = 0;
            var ids = stage.Encounters.SelectMany(x => x).ToList();
            double originalDoo = ids.Sum(id => EarlyGameContent.DooReward(baseline, id));
            double originalExp = ids.Sum(id => EarlyGameContent.ExpReward(baseline, id));
            Assert.That(ids.Sum(id => EarlyGameContent.DooReward(stage, id)), Is.EqualTo(originalDoo * factor).Within(ids.Count * factor));
            Assert.That(ids.Sum(id => EarlyGameContent.ExpReward(stage, id)), Is.EqualTo(originalExp * factor).Within(ids.Count * factor));
            Assert.Greater(factor, 1);
        }

        [Test]
        public void RevisionOneSaveMovesExistingBossWinsAndDoesNotDeleteNewRewardOpportunities()
        {
            var save = new SaveData { StoryContentRevision = 1, ClearedStage = 13, ProgressStage = 13, ProgressNext = 1 };
            save.StageStars.AddRange(new[] {
                new StarEntry { Stage = 7, Fight = 3, Mask = 7 },
                new StarEntry { Stage = 11, Fight = 0, Mask = 7 },
                new StarEntry { Stage = 13, Fight = 1, Diff = 1, Mask = 7 } });
            StoryContentMigration.Apply(save);
            Assert.AreEqual(7, StageStars.MaskOf(save, 7, 7, 0));
            Assert.AreEqual(7, StageStars.MaskOf(save, 11, 3, 0));
            Assert.AreEqual(7, StageStars.MaskOf(save, 13, 3, 1));
            Assert.AreEqual(0, StageStars.MaskOf(save, 7, 0, 0));
            Assert.AreEqual(9, StageStars.Total(save));
            Assert.AreEqual(0, save.ProgressStage);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(9, StageStars.Total(save));
        }

        [TestCase(2)]
        [TestCase(7)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        public void ExpandedMainStoryRemainsClearableAtThePreparedLevel(int number)
        {
            var stage = EarlyGameContent.FindStage(number);
            var runs = Enumerable.Range(301, 40).Select(seed => BalanceSimulation.Play(stage,
                BalanceSimulation.Starter(number, BalanceSimulation.BotLevel(stage)), seed)).ToList();
            int wins = runs.Count(x => x.Won);
            TestContext.Out.WriteLine(number + ": " + wins + "/40, actions " + runs.Where(x => x.Won).Select(x => x.Actions).DefaultIfEmpty().Average() + ", HP " + runs.Where(x => x.Won).Select(x => x.HpLeft).DefaultIfEmpty().Average());
            Assert.GreaterOrEqual(wins, 34);
        }

    }
}
