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
            while (!battle.FinaleReady)   // the whole party acts; the scene waits for 비류연's turn
            {
                Assert.IsTrue(battle.IsPlayerTurn);
                bool mine = battle.Current == hero;
                events.AddRange(battle.PlayerAct(mine && battle.CanUse(evade) ? PlayerCommand.UseSkill(evade, null) : PlayerCommand.UseSkill(SkillSpec.BasicAttack(), chilsang)));
                actions++;
                Assert.LessOrEqual(actions, 40, "the scene comes after a few rounds");
            }
            Assert.GreaterOrEqual(events.Count(e => e.SkillId == "천외일도"), trial.FinaleTurns, "three blows of the 심도 before the scene");
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
            while (!battle.FinaleReady) events.AddRange(battle.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), yecheong)));
            Assert.IsTrue(events.Where(e => e.SkillId == "십이야").All(e => e.TargetId == hero.Id && e.Damage > 0));
            Assert.IsTrue(events.Where(e => e.TargetId == yecheong.Id && e.Kind == EventKind.Attack).All(e => e.Damage == 0 && e.Quip == "상대는 장모님이다."), "no one in the party harms her");
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
        public void ScalesTurnBlowsAsideUntilTheMouthOpensForSeomroechang()
        {
            var stage = EarlyGameContent.FindStage(20);
            Assert.AreEqual(3, stage.Encounters.Length);
            var trial = stage.TrialFor(2);
            Assert.AreEqual(FinaleStyle.Window, trial.Style);
            Assert.IsFalse(Showdowns.FixedStars(stage, 2), "a real fight keeps its usual stars");
            var save = BalanceSimulation.Starter(20, StageStars.NormalBotLevel(20));
            var party = BalanceSimulation.Party(save, stage, 2);
            var snake = stage.EnemyFor("mukrin_hyeolmang", trial).CreateCombatant("snake");
            var battle = new InteractiveBattle(party, new List<Combatant> { snake }, new SeededRandom(8)) { Trial = trial };
            var events = battle.Begin();
            int guard = 0;
            while (!battle.IsOver && !battle.FinaleWindowOpen && guard++ < 200)
            {
                Assert.Throws<InvalidOperationException>(() => battle.PlayerAct(PlayerCommand.Finale()), "closed until the mouth opens");
                events.AddRange(battle.PlayerAct(SimpleBot.Choose(battle)));
            }
            Assert.IsTrue(battle.FinaleWindowOpen, "the wind-up opens the window on 비류연's turn");
            var hits = events.Where(e => e.TargetId == snake.Id && e.Kind == EventKind.Attack).ToList();
            Assert.AreEqual("비늘에 튕긴다!", hits[0].Quip);
            Assert.IsNotNull(hits[0].Lines, "the first bounce brings 나예린's explanation");
            Assert.IsTrue(hits.Skip(1).All(e => e.Quip == "검은 비늘에 막힌다." && e.Lines == null));
            var strike = battle.PlayerAct(PlayerCommand.Finale());
            Assert.IsTrue(strike.All(e => e.SkillId == "scene_seomroechang_finale" && e.TargetDefeated && !e.Retreated));
            Assert.IsTrue(battle.PlayerWon);
            Assert.AreEqual("비뢰도 오의 검기 · 생사결 섬뢰창", party[0].Skills.Single(sk => sk.Id == "scene_seomroechang_finale").Name);
        }

        [Test]
        public void CoinDuelIsBiryuyeonAloneAndFiveLatePressesLoseIt()
        {
            var stage = EarlyGameContent.FindStage(24);
            Assert.AreEqual(4, stage.Encounters.Length, "the stage keeps its four fights");
            var trial = stage.TrialFor(1);
            Assert.AreEqual(FinaleStyle.Duel, trial.Style);
            Assert.IsTrue(Showdowns.FixedStars(stage, 1));
            CollectionAssert.AreEqual(new[] { "dongbang_hak" }, stage.Encounters[1]);
            CollectionAssert.AreEqual(new[] { "dongbang_hak", "hyeolma_dae_b" }, stage.RewardEncounters[1], "it still pays for the 혈마대원");
            var save = BalanceSimulation.Starter(24, StageStars.NormalBotLevel(24));
            var party = BalanceSimulation.Party(save, stage, 1);
            Assert.AreEqual(1, party.Count, "one on one");
            var hak = stage.EnemyFor("dongbang_hak", trial).CreateCombatant("hak");
            var battle = new InteractiveBattle(party, new List<Combatant> { hak }, new SeededRandom(4)) { Trial = trial };
            battle.Begin();
            Assert.AreSame(party[0], battle.Current, "비류연's turn from the start, no gauge race");
            Assert.IsTrue(battle.FinaleReady);
            Assert.AreEqual("기회 5/5", battle.TrialStatus);
            int max = party[0].Stats.MaxHp;
            party[0].Hp = max / 2;   // even half-hurt he gets all five chances
            for (int i = 1; i <= 4; i++)
            {
                var miss = battle.DuelMiss().Single();
                Assert.AreEqual("붉은 검이 먼저 닿았다!", miss.Quip);
                Assert.IsTrue(party[0].IsAlive, "miss " + i);
                Assert.IsFalse(battle.IsOver);
                Assert.AreSame(party[0], battle.Current, "the turn stays his for the next coin");
            }
            Assert.AreEqual(1, party[0].Hp, "never below 1 before the last chance");
            Assert.AreEqual("기회 1/5", battle.TrialStatus);
            var last = battle.DuelMiss().Single();
            Assert.IsTrue(last.TargetDefeated);
            Assert.IsTrue(battle.IsOver);
            Assert.IsFalse(battle.PlayerWon);
            Assert.IsTrue(battle.TrialFailed);
            Assert.Throws<InvalidOperationException>(() => battle.DuelMiss());
        }

        [Test]
        public void EachLatePressCostsAFifthAndTheStrikeEndsTheDuel()
        {
            var stage = EarlyGameContent.FindStage(24);
            var trial = stage.TrialFor(1);
            var party = BalanceSimulation.Party(BalanceSimulation.Starter(24, StageStars.NormalBotLevel(24)), stage, 1);
            var hak = stage.EnemyFor("dongbang_hak", trial).CreateCombatant("hak");
            var battle = new InteractiveBattle(party, new List<Combatant> { hak }, new SeededRandom(4)) { Trial = trial };
            battle.Begin();
            int max = party[0].Stats.MaxHp;
            party[0].Hp = max;
            var miss = battle.DuelMiss().Single();
            Assert.AreEqual((int)Math.Round(max * 0.2f), miss.Damage);
            Assert.AreEqual(max - miss.Damage, party[0].Hp);
            Assert.AreEqual(0.1f, trial.Script.DuelWindowStep, 1e-6f, "each miss widens the window by 0.1 s");
            var strike = battle.PlayerAct(PlayerCommand.Guard());   // whatever is sent, it is 섬뢰
            Assert.IsTrue(strike.All(e => e.SkillId == "scene_seomroe_finale" && e.TargetDefeated && !e.Retreated));
            Assert.IsTrue(battle.PlayerWon);
            Assert.AreEqual(0, party[0].Skills.Single(sk => sk.Id == "scene_seomroe_finale").SpiritCost, "a white flash, not the 필살기 cut-in");
            string story = string.Join(" ", StoryEpisodes.Replay(stage));
            foreach (var cue in new[] { "뢰(雷)의 어느 얼굴", "빛을 뒤쫓는 쾌", "두 개의 묵룡환", "이 동전에 떨어지는 걸로 신호를 삼죠", "봤나?", "열십자", "뇌의 이름을 인정하죠", "섬뢰(閃雷)!", "학(鶴)! 이, 이 노옴!" })
                StringAssert.Contains(cue, story);
            Assert.IsTrue(BalanceSimulation.Play(stage, BalanceSimulation.Starter(24, StageStars.NormalBotLevel(24)), 5).Won);
            for (int d = 1; d <= 2; d++)
            {
                Assert.AreEqual(FinaleStyle.Duel, stage.WithDifficulty(d).TrialFor(1).Style, "the same duel on every difficulty");
                CollectionAssert.AreEqual(new[] { "player" }, Roster.StoryActorsFor(stage.WithDifficulty(d), 1));
            }
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
