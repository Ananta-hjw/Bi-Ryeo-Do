using System;
using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class ShowdownTests
    {
        private static StageData Final { get { return EarlyGameContent.FindStage(46); } }

        [Test]
        public void TheWristRingAndTheMukroesalFightsBecameOneShowdownOnEveryDifficulty()
        {
            for (int d = 0; d < StageStars.DifficultyCount; d++)
            {
                var st = Final.WithDifficulty(d);
                Assert.AreEqual(4, st.Encounters.Length);
                CollectionAssert.AreEqual(new[] { "chilsang_heart" }, st.Encounters[3]);
                Assert.AreEqual(TrialKind.Finale, st.TrialFor(3).Kind);
                Assert.AreEqual(FinaleStyle.Unavoidable, st.TrialFor(3).Style);
                Assert.IsTrue(Showdowns.Is(st, 3));
            }
            CollectionAssert.AreEqual(new[] { "unsealed_current", "chilsang_heart" }, Final.RewardEncounters[3], "pays what the two fights paid");
            Assert.AreEqual(3, Showdowns.ResumeAt(Final, 4), "an old resume at the removed fight picks up at the showdown");
            Assert.AreEqual(2, Showdowns.ResumeAt(Final, 2));
        }

        [Test]
        public void TheHeartBladeLandsThroughEveryDodgeThenMukroesalSparesHim()
        {
            var save = BalanceSimulation.Starter(46, 120);
            var party = BalanceSimulation.Party(save, Final, 3);
            var hero = party[0];
            var trial = Final.TrialFor(3);
            var chilsang = Final.EnemyFor("chilsang_heart", trial).CreateCombatant("chil");
            int hp = chilsang.Hp;
            var battle = new InteractiveBattle(party, new List<Combatant> { chilsang }, new SeededRandom(2)) { Trial = trial };
            var events = battle.Begin();
            Assert.AreEqual("거리 0", battle.TrialStatus);
            var evade = hero.Skills.First(s => s.Effect == SkillEffect.Evade);
            int actions = 0;
            while (!battle.FinaleStruck)
            {
                Assert.IsTrue(battle.IsPlayerTurn);
                events.AddRange(battle.PlayerAct(actions % 2 == 0 ? PlayerCommand.UseSkill(evade, null) : PlayerCommand.UseSkill(SkillSpec.BasicAttack(), chilsang)));
                actions++;
                Assert.LessOrEqual(actions, trial.FinaleTurns);
            }
            var blows = events.Where(e => e.SkillId == "천외일도").ToList();
            Assert.IsNotEmpty(blows);
            Assert.IsTrue(blows.All(e => e.Damage > 0 && e.TargetId == hero.Id), "dodging does not shorten the distance");
            Assert.AreEqual(StoryShowdowns.Cheonoe.FirstBlowQuip, blows[0].Quip);
            Assert.AreEqual(hp, chilsang.Hp, "no blow reaches the 심도");
            Assert.Greater(hero.Hp, 0);
            Assert.IsTrue(battle.FinaleReady);
            var strike = battle.PlayerAct(PlayerCommand.Guard());
            Assert.IsTrue(strike.All(e => e.SkillId == "scene_mukroesal_finale" && e.TargetDefeated && e.Retreated), "beaten, not slain");
            Assert.AreEqual("무명검 오의 · 묵뢰살", hero.Skills.Single(s => s.Id == "scene_mukroesal_finale").Name);
            Assert.IsTrue(battle.PlayerWon);
        }

        [Test]
        public void MoonShowdownTurnsTheLightningIntoTheGroundAtTheCostOfTheRightArm()
        {
            var stage = EarlyGameContent.FindStage(43);
            Assert.AreEqual(2, stage.Encounters.Length);
            CollectionAssert.AreEqual(new[] { "yecheong_twins", "unstable_black_lightning" }, stage.RewardEncounters[1]);
            Assert.AreEqual(1, Showdowns.ResumeAt(stage, 2), "an old resume at the removed fight picks up at the showdown");
            var trial = stage.TrialFor(1);
            Assert.IsTrue(trial.Script.TwoStep);
            var save = BalanceSimulation.Starter(43, 120);
            var party = BalanceSimulation.Party(save, stage, 1);
            var hero = party[0];
            var yecheong = stage.EnemyFor("yecheong_twins", trial).CreateCombatant("ye");
            var battle = new InteractiveBattle(party, new List<Combatant> { yecheong }, new SeededRandom(4)) { Trial = trial };
            var events = battle.Begin();
            while (!battle.FinaleStruck) events.AddRange(battle.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), yecheong)));
            Assert.IsTrue(events.Where(e => e.SkillId == "십이야").All(e => e.TargetId == hero.Id && e.Damage > 0));
            Assert.IsTrue(events.Where(e => e.ActorId == hero.Id && e.Kind == EventKind.Attack).All(e => e.Damage == 0 && e.Quip == "상대는 장모님이다."));
            int hp = hero.Hp;
            var strike = battle.PlayerAct(PlayerCommand.Guard());
            Assert.IsTrue(strike.All(e => e.SkillId == "scene_redirect_finale" && e.Damage == 0 && e.Retreated), "nobody is struck");
            Assert.AreEqual(Math.Max(1, hp - (int)Math.Round(hero.Stats.MaxHp * 0.3f)), hero.Hp, "the right arm pays for it");
            Assert.IsTrue(battle.PlayerWon);
            string story = string.Join(" ", StoryEpisodes.Replay(stage));
            foreach (var cue in new[] { "우리 아이를 꼬신 그 나쁜 년", "저 아이의 엄마다", "'저 같은' 아들은 어떠세요?", "그건 내가 인정 못해!", "나예린이 어머니 앞에 뛰어들었다", "땅으로 꺾여", "날아가거라, 딸아" })
                StringAssert.Contains(cue, story);
            Assert.IsTrue(BalanceSimulation.Play(stage, BalanceSimulation.Starter(43, StageStars.NormalBotLevel(43)), 7).Won);
        }

        [Test]
        public void TheSceneUsesTheOriginalLinesAndTheStoryLogKeepsIt()
        {
            string story = string.Join(" ", StoryEpisodes.Replay(Final));
            foreach (var cue in new[] { "그 아이", "비밀일세", "연비 '군'", "남은 묵룡환은 절대 풀지 마라", "봉황환을 풀었다", "무명검", "내가 왜 너희 재미를 위해 살인자가 돼야 하지?", "저, 바보 자식" })
                StringAssert.Contains(cue, story);
            Assert.IsTrue(BalanceSimulation.Play(Final, BalanceSimulation.Starter(46, StageStars.NormalBotLevel(46)), 7).Won);
        }
    }
}
