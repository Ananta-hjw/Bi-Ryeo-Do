using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class EnemyRankTests
    {
        [Test]
        public void RanksSplitHenchmenEliteAndBosses()
        {
            Assert.AreEqual(EnemyRank.Normal, EarlyGameContent.FindEnemy("hogol_bandit").Rank);
            Assert.AreEqual(EnemyRank.Elite, EarlyGameContent.FindEnemy("so_cheongun").Rank);
            Assert.AreEqual(EnemyRank.Boss, EarlyGameContent.FindEnemy("bi_daegongja").Rank);
            Assert.AreEqual(EnemyRank.Normal, EarlyGameContent.FindEnemy("trap_needle").Rank);   // traps never use 내공
            foreach (var e in EarlyGameContent.Enemies)
            {
                var c = e.CreateCombatant("t");
                Assert.AreEqual(e.Rank != EnemyRank.Normal, c.UsesResources, e.Id);
                Assert.AreEqual(e.Rank == EnemyRank.Normal ? 0 : e.Rank == EnemyRank.Elite ? EnemyData.EliteQi : EnemyData.BossQi, c.Stats.MaxQi, e.Id);
            }
        }

        [Test]
        public void EliteAndBossEnemiesSpendQiAndReleaseSpirit()
        {
            // Fight a boss with a hero who never kills it fast: its 기세 must fill and burst at least once, and a heavy strike must cost 내공.
            var st = EarlyGameContent.FindStage(24);
            var party = SimpleBot.BuildParty(24, st.RecommendedLevel);
            var enemies = st.Encounters[2].Select((id, i) => EarlyGameContent.FindEnemy(id).Scaled(20f, 1f).CreateCombatant(id + i)).ToList();   // long fight
            var battle = new InteractiveBattle(party, enemies, new SeededRandom(3));
            battle.Begin();
            bool burst = false, lowered = false;
            for (int g = 0; g < 60 && !battle.IsOver; g++)
            {
                foreach (var ev in battle.PlayerAct(PlayerCommand.Guard())) burst |= ev.Burst;
                lowered |= enemies.Any(e => e.UsesResources && e.Qi < e.Stats.MaxQi);
                foreach (var m in party) m.Hp = m.Stats.MaxHp;   // keep the party alive for the whole test
            }
            Assert.IsTrue(burst, "no 기세 폭발 in 60 turns");
            Assert.IsTrue(lowered, "no 내공 was ever spent");
        }
    }
}
