using System;
using System.Linq;
using NUnit.Framework;
using BiRyeoDo.Core;

namespace BiRyeoDo.Tests
{
    public class RescueStoryTests
    {
        private static string Replay(int number)
        {
            return string.Join("\n", StoryEpisodes.Replay(EarlyGameContent.FindStage(number)));
        }

        [Test]
        public void OrderedResponsesRequireOwnedSkillsAndMatchingEnemyCues()
        {
            var a = new Combatant("a", "응답자", Team.Player, new CombatStats { MaxHp = 100000, Attack = 1, Defense = 100, Speed = 100, MaxQi = 1000 },
                new[] { new SkillSpec { Id = "one", Name = "첫 대응", Effect = SkillEffect.Evade }, new SkillSpec { Id = "two", Name = "둘째 대응", Effect = SkillEffect.Evade }, new SkillSpec { Id = "three", Name = "셋째 대응", Effect = SkillEffect.Evade } });
            var moves = new[] { EnemyMove.Wind("첫 수"), EnemyMove.Attack("주먹", .1f), EnemyMove.Wind("둘째 수"), EnemyMove.Attack("장", .1f), EnemyMove.Wind("셋째 수"), EnemyMove.Attack("검", .1f) };
            var e = new Combatant("e", "시험자", Team.Enemy, new CombatStats { MaxHp = 100000, Attack = 1, Defense = 100, Speed = 100, MaxQi = 100 }, null) { Moves = new System.Collections.Generic.List<EnemyMove>(moves) };
            var b = new InteractiveBattle(new[] { a }, new[] { e }, new SeededRandom(42))
            { Trial = TrialRule.Responses("세 수", "차례로 대응합니다.", new[] { "one", "two", "three" }, new[] { "주먹", "장", "검" }, 40) };
            b.Begin();
            b.PlayerAct(PlayerCommand.UseSkill(a.Skills.First(s => s.Id == "three"), e));
            Assert.AreEqual(0, b.TechniqueUses);
            e.MoveIndex = 3; e.Charging = true;
            Assert.IsFalse(b.TechniqueWindowOpen, "The palm cue cannot satisfy the first fist response");
            b.PlayerAct(PlayerCommand.UseSkill(a.Skills.First(s => s.Id == "one"), e));
            Assert.AreEqual(0, b.TechniqueUses);
            for (int i = 0; i < 35 && !b.IsOver; i++)
            {
                if (b.TechniqueWindowOpen) b.PlayerAct(PlayerCommand.UseSkill(a.Skills.First(s => s.Id == b.RequiredTechniqueId), e));
                else b.PlayerAct(PlayerCommand.Guard());
            }
            Assert.IsTrue(b.PlayerWon); Assert.AreEqual(3, b.TechniqueUses);
            Assert.IsTrue(e.IsAlive); Assert.IsFalse(b.TrialFailed);
            Assert.Throws<ArgumentException>(() => TrialRule.Responses("", "", new[] { "one" }, new[] { "" }));
        }
        [TestCase(49, 0)] [TestCase(49, 1)] [TestCase(49, 2)]
        [TestCase(50, 0)] [TestCase(50, 1)] [TestCase(50, 2)]
        [TestCase(51, 0)] [TestCase(51, 1)] [TestCase(51, 2)]
        [TestCase(52, 0)] [TestCase(52, 1)] [TestCase(52, 2)]
        [TestCase(53, 0)] [TestCase(53, 1)] [TestCase(53, 2)]
        [TestCase(54, 0)] [TestCase(54, 1)] [TestCase(54, 2)]
        [TestCase(55, 0)] [TestCase(55, 1)] [TestCase(55, 2)]
        [TestCase(56, 0)] [TestCase(56, 1)] [TestCase(56, 2)]
        [TestCase(57, 0)] [TestCase(57, 1)] [TestCase(57, 2)]
        public void EveryNewPerspectiveCanCompleteItsActualObjective(int number, int difficulty)
        {
            var stage = EarlyGameContent.FindStage(number).WithDifficulty(difficulty);
            int level = difficulty == 0 ? BalanceSimulation.BotLevel(stage) : (int)Math.Round(StageStars.TargetLevel(number, difficulty));
            var runs = Enumerable.Range(1201, 20).Select(seed => BalanceSimulation.Play(stage, BalanceSimulation.Starter(number, level), seed)).ToArray();
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
            var save = SideContentSimulation.Prepared(57, 80, true);
            Inventory.Add(save, Ritual.RewardId, 1);
            var items = save.Items.Select(x => x.Id + ":" + x.Count).ToArray();
            var gear = save.GearSlots.Where(x => !string.IsNullOrEmpty(x.ItemId)).Select(x => x.Owner + ":" + x.ItemId + ":" + x.Level).ToArray();
            var arts = save.EquippedArts.ToArray(); var learned = save.LearnedArts.ToArray(); int fights = 0;
            foreach (var stage in EarlyGameContent.Stages.Skip(48).Take(9))
            {
                Assert.AreEqual(stage.Encounters.Length, stage.StoryActors.Length);
                for (int i = 0; i < stage.Encounters.Length; i++)
                {
                    var party = BalanceSimulation.FightParty(save, stage, i); fights++;
                    BalanceSimulation.AssertActors(stage, i, party);
                    foreach (var member in party) Assert.LessOrEqual(member.Skills.Count(s => s.Id != "basic" && s.SpiritCost == 0), Roster.Reenacted(stage) && member.Id != Inventory.Hero ? Progression.MaxEquippedArts + 1 : 4);
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
            Assert.AreEqual(36, fights);
            CollectionAssert.AreEqual(items, save.Items.Select(x => x.Id + ":" + x.Count));
            CollectionAssert.AreEqual(gear, save.GearSlots.Where(x => !string.IsNullOrEmpty(x.ItemId)).Select(x => x.Owner + ":" + x.ItemId + ":" + x.Level));
            CollectionAssert.AreEqual(arts, save.EquippedArts); CollectionAssert.AreEqual(learned, save.LearnedArts);
            Assert.IsTrue(save.CompanionLevels.All(x => EarlyGameContent.FindCompanion(x.Id) != null));
            Assert.AreEqual(41, Tower.ReferenceStage(70));
            Assert.IsTrue(Genders.IsFemale(Roster.StoryArtKey("story_namgung_sansan")));   // 51 is reenacted; her picture still reads as hers in its scenes
        }

        [Test]
        public void ReanalysisPreservesSelfRescueCooperationLossesAndTheIndependentWarTrap()
        {
            string west = Replay(49), east = Replay(50), south = Replay(51), eyes = Replay(52), nest = Replay(53), wolves = Replay(54), reunion = Replay(55), port = Replay(56), end = Replay(57);
            StringAssert.Contains("반응폭약", west); StringAssert.Contains("청동", west);
            StringAssert.Contains("27·가운데37·오른쪽47", EarlyGameContent.FindStage(49).TrialFor(2).Brief);
            Assert.Less(west.IndexOf("격공장", StringComparison.Ordinal), west.IndexOf("석괴압살관", StringComparison.Ordinal));
            StringAssert.Contains("아지랑이", east); StringAssert.Contains("염천빙지", east);
            StringAssert.Contains("풍우만곡", south); StringAssert.Contains("가짜", south);
            StringAssert.Contains("서른여섯", eyes); StringAssert.Contains("해제 완료와 탈출은 아직", eyes);
            StringAssert.Contains("효룡", nest); StringAssert.Contains("공저물사", nest);
            StringAssert.Contains("지존군림보", wolves); StringAssert.Contains("모용휘의 검에", wolves); StringAssert.Contains("창랑아", wolves);
            Assert.Less(reunion.IndexOf("마지막 두", StringComparison.Ordinal), reunion.IndexOf("검을 되찾", StringComparison.Ordinal));
            StringAssert.Contains("만해", reunion); StringAssert.Contains("삼초지적", reunion);
            StringAssert.Contains("고생했어요, 잘 돌아왔어요", reunion); StringAssert.Contains("다녀왔어요, 류연", reunion);
            StringAssert.Contains("출항", port); StringAssert.Contains("목숨까지 거두지 못했고", port);
            Assert.Less(end.IndexOf("당문혜가 당삼을 부탁", StringComparison.Ordinal), end.IndexOf("편수 풍신과 수룡", StringComparison.Ordinal));
            StringAssert.Contains("누명", end); StringAssert.Contains("쇄혼독비는 사용되지", end); StringAssert.Contains("정사대전", end);
            Assert.Less(end.IndexOf("구출 소식에 나백천은 안도", StringComparison.Ordinal), end.IndexOf("이미 백뢰에 찔린", StringComparison.Ordinal));
            Assert.IsFalse(end.Contains("임시 흑천맹주")); Assert.IsFalse(end.Contains("대난원"));
        }

        [Test]
        public void AppendedRewardsAndSavesKeepThePreviousEnding()
        {
            var save = new SaveData { StoryContentRevision = 8, ClearedStage = 48, ProgressStage = 48, ProgressNext = 4, Level = 65, Doo = 12345 };
            StageStars.Record(save, 48, 0, 0, 7); StoryContentMigration.Apply(save);
            Assert.AreEqual(48, save.ProgressStage); Assert.AreEqual(3, save.ProgressNext); Assert.AreEqual(12345, save.Doo);
            Assert.IsTrue(StageProgress.AtEnding(save, EarlyGameContent.FindStage(48)));
            for (int d = 0; d < 3; d++)
                foreach (var stage in EarlyGameContent.Stages.Skip(48).Take(9))
                {
                    var prepared = BalanceSimulation.Starter(stage.Number, stage.RecommendedPlayerLevel);
                    Recommend.StageReward(prepared, stage.WithDifficulty(d), out long exp, out long doo);
                    TestContext.Out.WriteLine(stage.Number + "/" + d + ": EXP " + exp + "; Doo " + doo);
                    Assert.Greater(exp, 0); Assert.Greater(doo, stage.DooTotal);
                    Assert.Greater(EarlyGameContent.StageRewards[stage.Number].Length, 0);
                    foreach (var row in stage.Encounters) foreach (var id in row) Assert.IsTrue(EarlyGameContent.FindEnemy(id).StoryOnly);
                }
            Assert.AreEqual(66, EarlyGameContent.Stages.Length);
            Assert.AreEqual(48, save.ClearedStage);
        }
    }
}
