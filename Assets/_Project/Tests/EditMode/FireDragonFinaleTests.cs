using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class FireDragonFinaleTests
    {
        private static StageData Normal { get { return EarlyGameContent.FindStage(30); } }

        [Test]
        public void NormalHasThreeFightsWhileHardAndExtremeKeepTheEarlierFour()
        {
            CollectionAssert.AreEqual(new[] { "hwaryong_daejin" }, Normal.Encounters[2]);
            Assert.AreEqual(3, Normal.Encounters.Length);
            Assert.AreEqual(TrialKind.Finale, Normal.TrialFor(2).Kind);
            Assert.AreEqual(3, StageStars.FightCount(30));
            for (int d = 1; d <= 2; d++)
            {
                var hard = Normal.WithDifficulty(d);
                Assert.AreEqual(4, hard.Encounters.Length);
                CollectionAssert.AreEqual(new[] { "hwaryong_rescue" }, hard.Encounters[3]);
                Assert.AreEqual(TrialKind.Survive, hard.TrialFor(3).Kind);
                Assert.AreEqual(4, StageStars.FightCount(30, d));
                StringAssert.Contains("용권승룡", string.Join(" ", hard.Outro));
                Assert.AreEqual(0.3f, hard.EnemyFor("bi_daegongja").RetreatHpPct, 1e-6f);
            }
            Assert.IsFalse(string.Join(" ", Normal.Outro).Contains("용권승룡"), "the 풍신 already played in the fight");
            CollectionAssert.AreEqual(new[] { "hwaryong_rescue" }, Quests.ToStage(Quests.Find("q_fire_gather")).Encounters[0]);
        }

        [Test]
        public void NormalBiHoldsAtOneHpAndLeavesWithHisLine()
        {
            var bi = Normal.EnemyFor("bi_daegongja");
            Assert.IsTrue(bi.HoldsAtOneHp);
            Assert.AreEqual(0f, bi.RetreatHpPct);
            var save = BalanceSimulation.Starter(30, 160);
            var party = BalanceSimulation.Party(save, Normal, 1);
            var battle = new InteractiveBattle(party, new List<Combatant> { bi.CreateCombatant("bi") }, new SeededRandom(3));
            var events = battle.Begin();
            while (!battle.IsOver) events.AddRange(battle.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), null)));
            Assert.IsTrue(battle.PlayerWon);
            var exit = events.Single(e => e.Retreated);
            Assert.AreEqual(1, exit.TargetHpAfter, "shown at 1 HP as he leaves");
            CollectionAssert.AreEqual(new[] { "대공자 비|칫... 귀찮게 하는군." }, exit.Lines);
        }

        [Test]
        public void PartyActsFirstThenTheJinFellsAllButBiRyuyeonWhoseFengShenEndsIt()
        {
            var save = BalanceSimulation.Starter(30, 40);
            var party = BalanceSimulation.Party(save, Normal, 2);
            Assert.Greater(party.Count, 1);
            var jin = Normal.EnemyFor("hwaryong_daejin", Normal.TrialFor(2)).CreateCombatant("jin");
            Assert.AreEqual(999999, jin.Stats.MaxHp);
            var battle = new InteractiveBattle(party, new List<Combatant> { jin }, new SeededRandom(5)) { Trial = Normal.TrialFor(2) };
            var acted = new List<string>();
            battle.Begin();
            while (!battle.FinaleStruck)
            {
                Assert.IsTrue(battle.IsPlayerTurn);
                acted.Add(battle.Current.Id);
                battle.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), jin));
            }
            CollectionAssert.AreEquivalent(party.Select(m => m.Id), acted, "every member acts exactly once before the 진");
            Assert.Greater(jin.Hp, 0);
            Assert.AreEqual(1, party[0].Hp, "비류연 is left at 1 HP");
            Assert.IsTrue(party.Skip(1).All(m => !m.IsAlive));
            Assert.IsTrue(battle.FinaleReady);
            Assert.AreSame(party[0], battle.Current);
            var strike = battle.PlayerAct(PlayerCommand.Guard());   // any command: his last action is the 풍신
            Assert.IsTrue(strike.All(e => e.SkillId == InteractiveBattle.FinaleSkillId && e.TargetDefeated));
            Assert.AreEqual(InteractiveBattle.FinaleSkillName, party[0].Skills.Single(s => s.Id == InteractiveBattle.FinaleSkillId).Name);
            Assert.IsTrue(battle.IsOver);
            Assert.IsTrue(battle.PlayerWon);
        }

        [Test]
        public void TheBotClearsNormalThirtyAndItsStoryHasTheWholeScene()
        {
            var save = BalanceSimulation.Starter(30, StageStars.NormalBotLevel(30));
            Assert.IsTrue(BalanceSimulation.Play(Normal, save, 11).Won);
            string story = string.Join(" ", StoryEpisodes.Replay(Normal));
            foreach (var cue in new[] { "칫... 귀찮게 하는군", "화룡멸겁대진을 발동해라", "내력을 보태줄 수 있어요", "알았네, 내력을 보태지", "모든 묵룡환을 푼다", "하.. 죽겠네" })
                StringAssert.Contains(cue, story);
            CollectionAssert.AreEqual(FireDragonFinale.Departure, StoryEpisodes.BeforeFight(Normal, 2, new SaveData { ClearedStage = 30 }));
        }

        [Test]
        public void OldNormalSavesResumeAtTheFinaleAndKeepTheirClear()
        {
            var save = new SaveData { ProgressStage = 30, ProgressNext = 3 };
            Assert.AreEqual(2, FireDragonFinale.ResumeAt(Normal, save.ProgressNext));
            Assert.AreEqual(3, FireDragonFinale.ResumeAt(Normal.WithDifficulty(1), 3));
            for (int f = 0; f < 4; f++) StageStars.Record(save, 30, f, 0, 7);
            Assert.IsTrue(StageStars.StageCleared(save, 30, 0));
            Assert.AreEqual(9, StageStars.StarsOfStage(save, 30, 0), "the old fourth fight's stars no longer count for the stage");
        }
    }
}
