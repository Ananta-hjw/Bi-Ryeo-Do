using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    /// <summary>Action-shaping statuses count down when the unit's turn ends, so they shape as many actions as their turns (a boss's shortened one at least one).</summary>
    public class StatusDurationTests
    {
        private static CombatStats S(int hp, int atk, int spd) { return new CombatStats { MaxHp = hp, MaxQi = 100, Attack = atk, Speed = spd, CritDamage = 1.5f }; }

        private static int SealedActions(bool boss)
        {
            var hero = new Combatant(Inventory.Hero, "h", Team.Player, S(10000000, 100, 100), null);
            var e = new Combatant("e", "e", Team.Enemy, S(10000000, 100, 100), null) { IsBoss = boss, Moves = new List<EnemyMove> { EnemyMove.Attack("강공", 3f) } };
            var b = new InteractiveBattle(hero, new[] { e }, new SeededRandom(1));
            b.Begin();
            e.AddStatus(StatusKind.Seal, Statuses.TurnsFor(e, StatusKind.Seal), "h");
            int sealedBlows = 0;
            for (int i = 0; i < 5; i++)
                sealedBlows += b.PlayerAct(PlayerCommand.Guard()).Count(x => x.ActorId == "e" && x.Kind == EventKind.Attack && x.SkillId == EnemyMove.Plain.Name);
            return sealedBlows;
        }

        [Test]
        public void SealShapesAsManyActionsAsItsTurnsAndBossesAreNotImmune()
        {
            Assert.AreEqual(Statuses.DefaultTurns(StatusKind.Seal), SealedActions(false));
            Assert.AreEqual(1, SealedActions(true), "a boss's shortened 봉혈 still holds for one action");
        }

        [Test]
        public void BurstOnlyHitsTheFirstBlowHarder()
        {
            var hero = new Combatant(Inventory.Hero, "h", Team.Player, S(10000000, 100, 1), null);
            var e = new Combatant("e", "e", Team.Enemy, S(10000000, 1000, 100), null) { Rank = EnemyRank.Elite, Moves = new List<EnemyMove> { EnemyMove.Attack("연타", .5f, 3) } };
            var b = new InteractiveBattle(hero, new[] { e }, new SeededRandom(1));
            e.Spirit = InteractiveBattle.MaxSpirit;
            var hits = b.Begin().Where(x => x.ActorId == "e" && x.Kind == EventKind.Attack && !x.Dodged && !x.IsCrit).ToList();
            Assert.GreaterOrEqual(hits.Count, 3);
            Assert.IsTrue(hits[0].Burst);
            Assert.Greater(hits[0].Damage, hits[1].Damage);
            Assert.AreEqual(hits[1].Damage, hits[2].Damage);
        }

        [Test]
        public void NoBeastInATechniqueTrialAndMightyFoesResistItsPoison()
        {
            var hero = new Combatant(Inventory.Hero, "h", Team.Player, S(10000000, 100, 100), null);
            var e = new Combatant("e", "e", Team.Enemy, S(10000000, 100, 10), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("x", 1f) } };
            var b = new InteractiveBattle(hero, new[] { e }, new SeededRandom(1))
            {
                Beast = Beasts.Loadout(Beasts.Find("urwe"), 0),
                Trial = new TrialRule { Kind = TrialKind.Technique, TechniqueId = "basic", TechniqueUses = 1, Limit = 10 }
            };
            b.Begin();
            Assert.IsFalse(b.CanCallBeast);

            int landed = 0;
            for (int seed = 1; seed <= 40; seed++)
            {
                var h2 = new Combatant(Inventory.Hero, "h", Team.Player, S(10000000, 100, 100), null);
                var wall = new Combatant("w", "w", Team.Enemy, S(10000000, 100, 10), null) { StatusTier = 2, Moves = new List<EnemyMove> { EnemyMove.Attack("x", 1f) } };
                var bb = new InteractiveBattle(h2, new[] { wall }, new SeededRandom(seed)) { Beast = Beasts.Loadout(Beasts.Find("ssanggaksa"), 0) };
                bb.Begin();
                bb.CallBeast(PlayerCommand.CallBeast(Beasts.FindSkill("ssang_fang"), wall));
                if (wall.Has(StatusKind.Poison)) landed++;
            }
            Assert.That(landed, Is.InRange(1, 20), "about 20% against a 절대고수");
        }
    }
}
