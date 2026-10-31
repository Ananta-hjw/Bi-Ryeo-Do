using System;
using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class TournamentStoryTests
    {
        private static InteractiveBattle Objective(out Combatant player, out Combatant enemy, int limit = 8, bool charge = false, int uses = 1, float power = 0)
        {
            var stats = new CombatStats { MaxHp = 100000, MaxQi = 100, Attack = 100, Defense = 10, Speed = 100, CritDamage = 1.5f };
            var skill = new SkillSpec { Id = "required", Name = "대응", Effect = power > 0 ? SkillEffect.None : SkillEffect.Evade, Power = power, QiCost = 0, Range = 3 };
            player = new Combatant("player", "아군", Team.Player, stats, new[] { skill });
            enemy = new Combatant("enemy", "상대", Team.Enemy, stats, null) { Moves = new List<EnemyMove> { EnemyMove.Wind("예고"), EnemyMove.Attack("공격", .1f) } };
            var battle = new InteractiveBattle(player, new[] { enemy }, new SeededRandom(17)) { Trial = TrialRule.Technique("대응", "안내", "required", limit, charge, uses) };
            battle.Begin();
            return battle;
        }

        [Test]
        public void ObjectiveNeedsTheOwnedTechniqueAndCanFinishWithAnEnemyStillAlive()
        {
            var b = Objective(out var actor, out var enemy);
            b.PlayerAct(PlayerCommand.Guard());
            Assert.AreEqual(0, b.TechniqueUses); Assert.IsFalse(b.PlayerWon);
            var fake = new SkillSpec { Id = "required", Name = "복제", Effect = SkillEffect.Evade };
            b.PlayerAct(PlayerCommand.UseSkill(fake, enemy));
            Assert.AreEqual(0, b.TechniqueUses);
            b.PlayerAct(PlayerCommand.UseSkill(actor.Skills.First(s => s.Id == "required"), enemy));
            Assert.AreEqual(1, b.TechniqueUses); Assert.IsTrue(b.PlayerWon); Assert.IsTrue(b.IsOver);
            Assert.IsTrue(enemy.IsAlive); Assert.IsNotEmpty(b.TrialStatus);
        }

        [Test]
        public void ChargeWindowIsCheckedBeforeTheCounterDestroysTheChargingTarget()
        {
            var b = Objective(out var actor, out var enemy, charge: true, power: 1000000);
            Assert.IsFalse(b.TechniqueWindowOpen);
            b.PlayerAct(PlayerCommand.Guard());
            Assert.IsTrue(b.TechniqueWindowOpen);
            b.PlayerAct(PlayerCommand.UseSkill(actor.Skills.First(s => s.Id == "required"), enemy));
            Assert.IsTrue(b.PlayerWon); Assert.IsFalse(b.TrialFailed);
        }

        [Test]
        public void EarlyUseDoesNotCountAndWrongKillsOrTimeoutsCannotBypassTheGoal()
        {
            var b = Objective(out var actor, out var enemy, charge: true);
            b.PlayerAct(PlayerCommand.UseSkill(actor.Skills.First(s => s.Id == "required"), enemy));
            Assert.AreEqual(0, b.TechniqueUses); Assert.IsFalse(b.PlayerWon);
            b = Objective(out actor, out enemy, limit: 1);
            b.PlayerAct(PlayerCommand.Guard());
            Assert.IsTrue(b.TrialFailed); Assert.IsFalse(b.PlayerWon); Assert.IsNotEmpty(b.TrialFailReason);
            b = Objective(out actor, out enemy);
            enemy.Hp = 1;
            b.PlayerAct(PlayerCommand.UseSkill(actor.Skills.First(s => s.Id == "basic"), enemy));
            Assert.IsTrue(b.IsOver); Assert.IsTrue(b.TrialFailed); Assert.IsFalse(b.PlayerWon);
        }

        [Test]
        public void RequiredUsesFinishOnTheLastAllowedActionWithoutAFalseTimeout()
        {
            var b = Objective(out var actor, out var enemy, limit: 2, uses: 2);
            var skill = actor.Skills.First(s => s.Id == "required");
            b.PlayerAct(PlayerCommand.UseSkill(skill, enemy)); Assert.IsFalse(b.IsOver);
            b.PlayerAct(PlayerCommand.UseSkill(skill, enemy));
            Assert.IsTrue(b.PlayerWon); Assert.IsFalse(b.TrialFailed);
            Assert.AreEqual(2, b.TechniqueUses);
            Assert.Throws<ArgumentException>(() => TrialRule.Technique("", "", "", 1));
            Assert.AreEqual(4, (int)TrialKind.Harmony);
        }

        [TestCase(42, 0)] [TestCase(42, 1)] [TestCase(42, 2)]
        [TestCase(43, 0)] [TestCase(43, 1)] [TestCase(43, 2)]
        [TestCase(44, 0)] [TestCase(44, 1)] [TestCase(44, 2)]
        [TestCase(45, 0)] [TestCase(45, 1)] [TestCase(45, 2)]
        [TestCase(46, 0)] [TestCase(46, 1)] [TestCase(46, 2)]
        [TestCase(47, 0)] [TestCase(47, 1)] [TestCase(47, 2)]
        [TestCase(48, 0)] [TestCase(48, 1)] [TestCase(48, 2)]
        public void NewChaptersAreClearableWithTheirActualActorsAndObjectives(int number, int difficulty)
        {
            var stage = EarlyGameContent.FindStage(number).WithDifficulty(difficulty);
            int level = difficulty == 0 ? BalanceSimulation.BotLevel(stage) : (int)Math.Round(StageStars.TargetLevel(number, difficulty));
            var runs = Enumerable.Range(901, 20).Select(seed => BalanceSimulation.Play(stage, BalanceSimulation.Starter(number, level), seed)).ToArray();
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
        public void SceneWeaponsKeepOwnedEquipmentAndArtsWhileEveryObjectiveHasAnAvailableCommand()
        {
            var save = SideContentSimulation.Prepared(48, 60, true);
            Inventory.Add(save, Ritual.RewardId, 1);
            var items = save.Items.Select(x => x.Id + ":" + x.Count).ToArray();
            var slots = save.GearSlots.Where(x => !string.IsNullOrEmpty(x.ItemId)).Select(x => x.Owner + ":" + x.ItemId + ":" + x.Level).ToArray();
            var equipped = save.EquippedArts.ToArray(); var learned = save.LearnedArts.ToArray();
            int fights = 0;
            for (int n = 42; n <= 48; n++)
            {
                var stage = EarlyGameContent.FindStage(n);
                Assert.AreEqual(stage.Encounters.Length, stage.StoryActors.Length);
                for (int i = 0; i < stage.Encounters.Length; i++)
                {
                    var party = Roster.CreateStoryParty(stage, i, save); fights++;
                    CollectionAssert.AreEqual(stage.StoryActors[i], party.Select(x => x.Id));
                    foreach (var member in party) Assert.LessOrEqual(member.Skills.Count(s => s.Id != "basic" && s.SpiritCost == 0), Progression.MaxEquippedArts + (member.Id == Inventory.Hero ? 0 : 1));
                    var rule = stage.TrialFor(i);
                    if (rule != null && rule.Kind == TrialKind.Technique)
                    {
                        Assert.IsTrue(party.Any(x => x.Skills.Any(s => s.Id == rule.TechniqueId)), n + "/" + i);
                        Assert.Greater(rule.Limit, 0); Assert.Greater(rule.TechniqueUses, 0);
                    }
                }
            }
            Assert.AreEqual(27, fights);   // 46's 봉황환 and 묵뢰살 fights became one showdown
            CollectionAssert.AreEqual(items, save.Items.Select(x => x.Id + ":" + x.Count));
            CollectionAssert.AreEqual(slots, save.GearSlots.Where(x => !string.IsNullOrEmpty(x.ItemId)).Select(x => x.Owner + ":" + x.ItemId + ":" + x.Level));
            CollectionAssert.AreEqual(equipped, save.EquippedArts); CollectionAssert.AreEqual(learned, save.LearnedArts);
            Assert.IsTrue(save.CompanionLevels.All(x => EarlyGameContent.FindCompanion(x.Id) != null));
        }

        [Test]
        public void YunmiUsesJunhosIdentityGrowthAndEquipmentButHerOwnScenePresentation()
        {
            var save = BalanceSimulation.Starter(44, 55);
            var regular = CompanionGrowth.Create(save, EarlyGameContent.FindCompanion("yun_junho"));
            var disguised = Roster.CreateStoryParty(EarlyGameContent.FindStage(44), 2, save).Single();
            Assert.AreEqual("yun_junho", disguised.Id); Assert.AreEqual("윤미", disguised.Name);
            Assert.AreEqual(regular.Stats.Attack, disguised.Stats.Attack);
            Assert.AreEqual(regular.Stats.MaxHp, disguised.Stats.MaxHp);
            Assert.AreEqual("yunmi", Roster.StoryArtKey(disguised));
            Assert.IsTrue(Genders.IsFemale(Roster.StoryArtKey(disguised)));
            Assert.AreEqual("윤준호", CompanionGrowth.Create(save, EarlyGameContent.FindCompanion("yun_junho")).Name);
        }

        [Test]
        public void SameBodyKeepsWoundsWhileFormsChangeAndRestrictedPeopleStayOffTheRescueParty()
        {
            var save = BalanceSimulation.Starter(48, 60);
            var stage = EarlyGameContent.FindStage(45);
            var party = Roster.CreateStoryParty(stage, 1, save); party[0].Hp = 123; party[0].Qi = 40; party[0].Spirit = 27;
            Roster.PrepareStoryEncounter(stage, 2, party);
            Assert.AreEqual(123, party[0].Hp); Assert.AreEqual(40, party[0].Qi); Assert.AreEqual(27, party[0].Spirit);
            Assert.IsTrue(party[0].Skills.Any(s => s.Id == "scene_hanbing"));
            Assert.IsFalse(party[0].Skills.Any(s => s.Id == "scene_soan"));
            StageProgress.Store(save, 45, 2, party, 1);
            var restored = Roster.CreateStoryParty(stage.WithDifficulty(1), 2, save); StageProgress.Restore(save, restored);
            Assert.AreEqual(123, restored[0].Hp); Assert.IsTrue(restored[0].Skills.Any(s => s.Id == "scene_hanbing"));
            Assert.IsFalse(CompanionGrowth.Create(save, EarlyGameContent.FindCompanion("na_yerin")).Skills.Any(s => s.Id.StartsWith("scene_")));
            foreach (int n in new[] { 47, 48 })
                Assert.IsFalse(EarlyGameContent.FindStage(n).StoryActors.SelectMany(x => x).Any(id => new[] { "na_yerin", "yun_junho", "story_namgung_sang", "story_ryu_eungyeong" }.Contains(id)));
        }

        [Test]
        public void PlotOrderKeepsTheUnfinishedRescueAndConfirmedRevelationsInTheirOwnChapters()
        {
            string Replay(int n) { return string.Join(" ", StoryEpisodes.Replay(EarlyGameContent.FindStage(n))); }
            var a = Replay(42); var b = Replay(43); var c = Replay(44); var d = Replay(45); var e = Replay(46); var f = Replay(47); var g = Replay(48);
            Assert.Less(a.IndexOf("술병 앞의 사부"), a.IndexOf("진비뢰도"));
            Assert.Less(a.IndexOf("첫 벽력포 ·"), a.IndexOf("둘째 뇌탄 ·"));
            Assert.Less(a.IndexOf("둘째 뇌탄 ·"), a.IndexOf("셋째 포구 ·"));
            StringAssert.Contains("현금 세 궤", a); StringAssert.Contains("큰절", b);
            StringAssert.Contains("기권", c); StringAssert.Contains("사채청산은 떨어", c);
            Assert.Less(d.IndexOf("처음 검은 기억했다"), d.IndexOf("한빙섬옥 ·"));
            Assert.Less(d.IndexOf("한빙섬옥 ·"), d.IndexOf("피리가 끊긴 뒤"));
            StringAssert.Contains("상대 진영에 남", d);
            Assert.Less(e.IndexOf("혈무막막이 드러낸"), e.IndexOf("관에서 나온"));
            Assert.Less(e.IndexOf("관에서 나온"), e.IndexOf("열세 번째 도"));
            Assert.Less(e.IndexOf("열세 번째 도"), e.IndexOf("봉황환을 풀었다"));
            Assert.Less(e.IndexOf("봉황환을 풀었다"), e.IndexOf("흑과 은의 뇌광"));
            Assert.Less(e.IndexOf("우승이 선포"), e.IndexOf("나예린을 데려갔다"));
            StringAssert.Contains("이벽한", e); StringAssert.Contains("꼭 돌려주겠다는 약속", f);
            StringAssert.Contains("나예린이 저항하며 남긴 흔적", f);
            StringAssert.Contains("제사십사계", g); StringAssert.Contains("아버지 북천멸겁", g);
            StringAssert.Contains("다섯 대를 앞뒀다", g);
            Assert.IsFalse(g.Contains("락비오가 패배")); Assert.IsFalse(g.Contains("반응폭약"));
            Assert.IsFalse(f.Contains("나예린을 구출했다"));
        }

        [Test]
        public void AppendedStoryOpponentsCannotRetuneExistingTowerFloors()
        {
            Assert.AreEqual(41, Tower.ReferenceStage(60));
            Assert.Greater(Tower.FloorLevel(60), EarlyGameContent.FindStage(41).RecommendedLevel);
            var enemies = Tower.BuildEnemies(60);
            Assert.IsTrue(enemies.All(e => e.Level == Tower.FloorLevel(60)));
            var storyArt = EarlyGameContent.Stages.Skip(41).SelectMany(s => s.Encounters).SelectMany(x => x)
                .Select(id => EarlyGameContent.FindEnemy(id)).ToArray();
            Assert.IsTrue(storyArt.All(e => e.StoryOnly));
            Assert.IsFalse(enemies.Any(e => storyArt.Any(s => s.Name == e.Name)));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void AppendedJourneyPreservesClearsAndPaysPositiveDifficultyRewardsWithoutReturningWeapons(int difficulty)
        {
            var save = new SaveData { StoryContentRevision = 8, ClearedStage = 41, ProgressStage = 41, ProgressNext = 1, Level = 55, Doo = 12345 };
            StageStars.Record(save, 41, 0, 0, 7);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(41, save.ProgressStage); Assert.AreEqual(41, save.ClearedStage);
            Assert.AreEqual(42, save.HighestUnlockedStage); Assert.AreEqual(12345, save.Doo);
            for (int n = 42; n <= 48; n++)
            {
                var stage = EarlyGameContent.FindStage(n).WithDifficulty(difficulty);
                Recommend.StageReward(new SaveData(), stage, out long exp, out long doo);
                TestContext.Out.WriteLine(n + "/" + difficulty + ": EXP " + exp + "; Doo " + doo);
                Assert.Greater(exp, 0); Assert.Greater(doo, 0);
                Assert.IsFalse(EarlyGameContent.StageRewards[n].Any(x => x.Id == Ritual.RewardId));
                StageStars.Record(save, n, stage.Encounters.Length - 1, difficulty, 1);
                Recommend.StageReward(save, stage, out long againExp, out long againDoo);
                Assert.Less(againExp, exp); Assert.Less(againDoo, doo);
            }
        }
    }
}
