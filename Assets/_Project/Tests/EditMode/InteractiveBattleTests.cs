using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class InteractiveBattleTests
    {
        private static CombatStats Stats(int hp, int atk, int def, int speed, float crit = 0f)
        {
            return new CombatStats { MaxHp = hp, MaxQi = 100, Attack = atk, Defense = def, Speed = speed, CritRate = crit, CritDamage = 1.5f };
        }

        private static Combatant Player(CombatStats s, params SkillSpec[] skills)
        {
            return new Combatant("p", "비류연", Team.Player, s, skills);
        }

        private static Combatant Enemy(string id, CombatStats s, params EnemyMove[] moves)
        {
            var c = new Combatant(id, id, Team.Enemy, s, null);
            c.Moves = new List<EnemyMove>(moves.Length > 0 ? moves : new[] { EnemyMove.Plain });
            return c;
        }

        private static InteractiveBattle Start(Combatant p, params Combatant[] enemies)
        {
            var b = new InteractiveBattle(p, enemies, new SeededRandom(1));
            b.Begin();
            return b;
        }

        [Test]
        public void ActingOutOfTurnThrows()
        {
            var p = Player(Stats(1000, 100, 0, 100));
            var e = Enemy("e", Stats(10, 1, 0, 1));
            var b = Start(p, e);
            b.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e)); // kills the enemy, battle over
            Assert.Throws<System.InvalidOperationException>(() => b.PlayerAct(PlayerCommand.Guard()));
        }

        [Test]
        public void FasterEnemyActsBeforeThePlayerAndEventsAreReturned()
        {
            var p = Player(Stats(1000, 1, 0, 100));
            var b = new InteractiveBattle(p, new[] { Enemy("e", Stats(1000, 10, 0, 300)) }, new SeededRandom(1));
            var first = b.Begin();
            Assert.Greater(first.Count, 0);
            Assert.IsTrue(first.All(e => e.ActorId == "e"));
            Assert.IsTrue(b.IsPlayerTurn);
        }

        [Test]
        public void SkillDamagesTheChosenTargetOnly()
        {
            var big = new SkillSpec { Id = "big", Name = "강타", Power = 2f };
            var e1 = Enemy("a", Stats(500, 1, 0, 1));
            var e2 = Enemy("b", Stats(500, 1, 0, 1));
            var b = Start(Player(Stats(500, 20, 0, 100), big), e1, e2);
            b.PlayerAct(PlayerCommand.UseSkill(big, e2));
            Assert.AreEqual(500, e1.Hp);
            Assert.Less(e2.Hp, 500);
        }

        [Test]
        public void AreaSkillHitsEveryLivingEnemy()
        {
            var aoe = new SkillSpec { Id = "aoe", Name = "전체", Power = 1f, AllEnemies = true };
            var e1 = Enemy("a", Stats(500, 1, 0, 1));
            var e2 = Enemy("b", Stats(500, 1, 0, 1));
            var b = Start(Player(Stats(500, 20, 0, 100), aoe), e1, e2);
            var ev = b.PlayerAct(PlayerCommand.UseSkill(aoe, null));
            Assert.Less(e1.Hp, 500);
            Assert.Less(e2.Hp, 500);
            Assert.AreEqual(2, ev.Count(x => x.ActorId == "p"));
        }

        [Test]
        public void MultiHitSkillEmitsOneEventPerHit()
        {
            var multi = new SkillSpec { Id = "m", Name = "연타", Power = 1f, Hits = 3 };
            var e = Enemy("a", Stats(5000, 1, 0, 1));
            var b = Start(Player(Stats(500, 20, 0, 100), multi), e);
            var ev = b.PlayerAct(PlayerCommand.UseSkill(multi, e)).Where(x => x.ActorId == "p").ToList();
            Assert.AreEqual(3, ev.Count);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, ev.Select(x => x.HitIndex).ToArray());
        }

        [Test]
        public void GuardReducesIncomingDamage()
        {
            int Taken(bool guard)
            {
                var p = Player(Stats(10000, 1, 0, 100));
                var e = Enemy("e", Stats(10000, 100, 0, 100), EnemyMove.Attack("강타", 1f));
                var b = Start(p, e);
                b.PlayerAct(guard ? PlayerCommand.Guard() : PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e));
                return 10000 - p.Hp;
            }
            Assert.Less(Taken(true), Taken(false));
        }

        [Test]
        public void EvadeMakesTheNextEnemyHitsMiss()
        {
            var evade = new SkillSpec { Id = "ev", Name = "삼첩영", Effect = SkillEffect.Evade };
            var p = Player(Stats(1000, 1, 0, 100), evade);
            var e = Enemy("e", Stats(1000, 50, 0, 100), EnemyMove.Attack("강타", 1f));
            var b = Start(p, e);
            int before = p.Hp;
            var ev = b.PlayerAct(PlayerCommand.UseSkill(evade, null));
            Assert.IsTrue(ev.Any(x => x.Kind == EventKind.Attack && x.Dodged));
            Assert.AreEqual(before, p.Hp);
        }

        [Test]
        public void ChargeTelegraphsTheNextMove()
        {
            var e = Enemy("e", Stats(1000, 10, 0, 100), EnemyMove.Wind("기를 모은다"), EnemyMove.Attack("일격", 3f));
            var p = Player(Stats(1000, 1, 0, 100));
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1));
            b.Begin();
            // Player gauge wins ties first, so make the enemy act by passing a turn.
            var ev = b.PlayerAct(PlayerCommand.Guard());
            Assert.IsTrue(e.Charging || ev.Any(x => x.Kind == EventKind.Charge));
        }

        [Test]
        public void SpiritBuildsAndUltimateNeedsFullGauge()
        {
            var ult = new SkillSpec { Id = "ult", Name = "비기", Power = 9f, SpiritCost = 100 };
            var p = Player(Stats(10000, 20, 0, 100), ult);
            var e = Enemy("e", Stats(100000, 1, 0, 1));
            var b = Start(p, e);
            Assert.IsFalse(b.CanUse(ult));
            int guard = 0;
            while (p.Spirit < 100 && guard++ < 50) b.PlayerAct(PlayerCommand.Guard());
            Assert.AreEqual(100, p.Spirit);
            Assert.IsTrue(b.CanUse(ult));
            b.PlayerAct(PlayerCommand.UseSkill(ult, e));
            Assert.Less(p.Spirit, 100);
        }

        [Test]
        public void MeditateRestoresQiAndSomeHp()
        {
            var p = Player(Stats(1000, 1, 0, 100));
            var e = Enemy("e", Stats(100000, 1, 0, 1));
            var b = Start(p, e);
            p.Qi = 10;
            p.Hp = 500;
            b.PlayerAct(PlayerCommand.Meditate());
            Assert.GreaterOrEqual(p.Qi, 10 + InteractiveBattle.MeditateQi);
            Assert.Greater(p.Hp, 500);
        }

        [Test]
        public void PreviewOrderStartsWithThePlayerOnTheirTurnAndHasRequestedLength()
        {
            var p = Player(Stats(1000, 1, 0, 100));
            var e = Enemy("e", Stats(1000, 1, 0, 100));
            var b = Start(p, e);
            var order = b.PreviewOrder(6);
            Assert.AreEqual(6, order.Count);
            Assert.AreSame(p, order[0]);
        }

        [Test]
        public void BattleEndsWhenAllEnemiesDie()
        {
            var p = Player(Stats(1000, 100, 0, 100));
            var e = Enemy("e", Stats(10, 1, 0, 1));
            var b = Start(p, e);
            b.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e));
            Assert.IsTrue(b.IsOver);
            Assert.IsTrue(b.PlayerWon);
            Assert.IsFalse(b.IsPlayerTurn);
        }

        [Test]
        public void RestBetweenFightsHealsAndRefillsQi()
        {
            var p = Player(Stats(1000, 1, 0, 100));
            p.Hp = 100; p.Qi = 0; p.Spirit = 50;
            p.RestBetweenFights(0.5f);
            Assert.AreEqual(600, p.Hp);
            Assert.AreEqual(100, p.Qi);
            Assert.AreEqual(0, p.Spirit);
        }
    }
}
