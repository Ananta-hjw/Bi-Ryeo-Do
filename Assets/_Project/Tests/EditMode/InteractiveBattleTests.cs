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
        public void StoryBossRetreatsAtThresholdInsteadOfDying()
        {
            var p = Player(Stats(100000, 100, 0, 100));
            var e = Enemy("boss", Stats(1000, 1, 0, 1));
            e.RetreatHpPct = 0.3f;
            var b = Start(p, e);
            var events = b.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e));
            while (!b.IsOver) events.AddRange(b.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e)));
            Assert.IsTrue(b.PlayerWon);
            Assert.IsTrue(events.Any(x => x.Retreated && x.TargetDefeated));
            Assert.Greater(e.Stats.MaxHp * 0.31f, 0f);
        }

        [Test]
        public void SurviveTrialIsWonByEnduringEnoughEnemyActions()
        {
            var p = Player(Stats(1000000, 100, 0, 100));
            var e = Enemy("boss", Stats(1000000, 1, 0, 100));
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1)) { Trial = TrialRule.Survive("t", "b", 4) };
            b.Begin();
            for (int i = 0; i < 50 && !b.IsOver; i++) b.PlayerAct(PlayerCommand.Guard());
            Assert.IsTrue(b.IsOver);
            Assert.IsTrue(b.PlayerWon);
            Assert.IsTrue(e.IsAlive);
        }

        [Test]
        public void DeadlineTrialFailsWhenEnemiesSurviveTheActionLimit()
        {
            var p = Player(Stats(1000000, 100, 0, 100));
            var e = Enemy("wall", Stats(1000000, 1, 0, 1));
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1)) { Trial = TrialRule.Deadline("t", "b", 3) };
            b.Begin();
            for (int i = 0; i < 10 && !b.IsOver; i++) b.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e));
            Assert.IsTrue(b.TrialFailed);
            Assert.IsFalse(b.PlayerWon);
            Assert.AreEqual(3, b.PartyActions);
        }

        [Test]
        public void DeadlineTrialStillWinsWhenEnemiesFallInTime()
        {
            var p = Player(Stats(1000, 1000, 0, 100));
            var e = Enemy("weak", Stats(10, 1, 0, 1));
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1)) { Trial = TrialRule.Deadline("t", "b", 3) };
            b.Begin();
            b.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e));
            Assert.IsTrue(b.PlayerWon);
            Assert.IsFalse(b.TrialFailed);
        }

        [Test]
        public void LifelineTrialCutsOnlyWhenAChargedMoveLands()
        {
            var p = Player(Stats(1000000, 1, 0, 1));
            var e = Enemy("chain", Stats(1000000, 1, 0, 100), EnemyMove.Attack("a", 0.1f), EnemyMove.Wind("w"), EnemyMove.Attack("h", 0.1f));
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1)) { Trial = TrialRule.Lifeline("t", "b", 2) };
            b.Begin();
            for (int i = 0; i < 40 && !b.IsOver; i++) b.PlayerAct(PlayerCommand.Guard());
            Assert.IsTrue(b.TrialFailed);
            Assert.AreEqual(2, b.LifelinesCut);
            Assert.IsFalse(b.PlayerWon);
        }

        [Test]
        public void HarmonyChainBoostsTheNextHit()
        {
            var p = Player(Stats(1000000, 100, 0, 100));
            var e = Enemy("wall", Stats(1000000, 1, 0, 1));
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1)) { Trial = TrialRule.Harmony("t", "b", 50) };
            b.Begin();
            var first = b.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e)).First(x => x.ActorId == "p" && x.Kind == EventKind.Attack);
            var second = b.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e)).First(x => x.ActorId == "p" && x.Kind == EventKind.Attack);
            Assert.IsFalse(first.Harmony);
            Assert.IsTrue(second.Harmony);
            Assert.Greater(second.Damage, first.Damage * 2);
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

        // ---- party battles

        private static Combatant Ally(string id, CombatStats s, params SkillSpec[] skills)
        {
            return new Combatant(id, id, Team.Player, s, skills);
        }

        [Test]
        public void EveryLivingPartyMemberGetsATurn()
        {
            var a = Ally("a", Stats(1000, 1, 0, 100));
            var b = Ally("b", Stats(1000, 1, 0, 90));
            var e = Enemy("e", Stats(100000, 1, 0, 1));
            var battle = new InteractiveBattle(new[] { a, b }, new[] { e }, new SeededRandom(1));
            battle.Begin();
            var seen = new HashSet<string>();
            for (int i = 0; i < 12; i++) { seen.Add(battle.Current.Id); battle.PlayerAct(PlayerCommand.Guard()); }
            CollectionAssert.AreEquivalent(new[] { "a", "b" }, seen);
        }

        [Test]
        public void BattleIsLostOnlyWhenWholePartyFalls()
        {
            var a = Ally("a", Stats(5, 1, 0, 100));
            var b = Ally("b", Stats(100000, 1, 0, 100));
            var e = Enemy("e", Stats(100000, 500, 0, 300), EnemyMove.Attack("강타", 5f));
            var battle = new InteractiveBattle(new[] { a, b }, new[] { e }, new SeededRandom(3));
            battle.Begin();
            for (int i = 0; i < 20 && !battle.IsOver; i++) battle.PlayerAct(PlayerCommand.Guard());
            Assert.IsFalse(battle.IsOver, "one survivor keeps the fight going");
            Assert.IsFalse(a.IsAlive && b.IsAlive);
        }

        [Test]
        public void StunSkipsTheTargetsNextTurnAndInterruptsACharge()
        {
            var stun = new SkillSpec { Id = "stun", Name = "점혈", Power = 0.5f, Effect = SkillEffect.Stun };
            var a = Ally("a", Stats(100000, 5, 0, 100), stun);
            var e = Enemy("e", Stats(100000, 50, 0, 100), EnemyMove.Wind("준비"), EnemyMove.Attack("강타", 9f), EnemyMove.Attack("약타", 0.1f));
            var battle = new InteractiveBattle(new[] { a }, new[] { e }, new SeededRandom(1));
            battle.Begin();
            // Let the enemy start its charge.
            for (int i = 0; i < 6 && !e.Charging; i++) battle.PlayerAct(PlayerCommand.Guard());
            Assert.IsTrue(e.Charging);
            var ev = battle.PlayerAct(PlayerCommand.UseSkill(stun, e));
            Assert.IsTrue(ev.Any(x => x.Interrupted));
            Assert.IsFalse(e.Charging);
            Assert.IsTrue(ev.Any(x => x.Kind == EventKind.StunSkip), "stunned enemy loses its turn");
            Assert.IsFalse(ev.Any(x => x.Kind == EventKind.Attack && x.ActorId == "e" && x.Damage > 1000), "the heavy move never lands");
        }

        [Test]
        public void BossesAreInterruptedButNotStunLocked()
        {
            var stun = new SkillSpec { Id = "stun", Name = "점혈", Power = 0.5f, Effect = SkillEffect.Stun };
            var a = Ally("a", Stats(100000, 5, 0, 100), stun);
            var e = Enemy("e", Stats(100000, 50, 0, 100), EnemyMove.Wind("준비"), EnemyMove.Attack("강타", 9f));
            e.IsBoss = true;
            var battle = new InteractiveBattle(new[] { a }, new[] { e }, new SeededRandom(1));
            battle.Begin();
            for (int i = 0; i < 6 && !e.Charging; i++) battle.PlayerAct(PlayerCommand.Guard());
            var ev = battle.PlayerAct(PlayerCommand.UseSkill(stun, e));
            Assert.IsTrue(ev.Any(x => x.Interrupted));
            Assert.IsFalse(ev.Any(x => x.Stunned));
            Assert.IsFalse(ev.Any(x => x.Kind == EventKind.StunSkip));
        }

        [Test]
        public void GuardPartyProtectsEveryone()
        {
            var barrier = new SkillSpec { Id = "wall", Name = "벽", Effect = SkillEffect.GuardParty };
            var a = Ally("a", Stats(100000, 1, 0, 100), barrier);
            var b = Ally("b", Stats(100000, 1, 0, 1));
            var e = Enemy("e", Stats(100000, 100, 0, 100), EnemyMove.Attack("강타", 1f));
            var battle = new InteractiveBattle(new[] { a, b }, new[] { e }, new SeededRandom(2));
            battle.Begin();
            Assert.AreSame(a, battle.Current);
            var ev = battle.PlayerAct(PlayerCommand.UseSkill(barrier, null));
            Assert.IsTrue(ev.Where(x => x.Kind == EventKind.Attack && x.Damage > 0).All(x => x.Guarded));
        }

        [Test]
        public void HealAllyRestoresTheMostWoundedMember()
        {
            var heal = new SkillSpec { Id = "heal", Name = "치유", Effect = SkillEffect.HealAlly, HealPct = 0.5f };
            var a = Ally("a", Stats(1000, 1, 0, 100), heal);
            var b = Ally("b", Stats(1000, 1, 0, 1));
            var e = Enemy("e", Stats(100000, 1, 0, 1));
            var battle = new InteractiveBattle(new[] { a, b }, new[] { e }, new SeededRandom(1));
            battle.Begin();
            b.Hp = 100;
            var ev = battle.PlayerAct(PlayerCommand.UseSkill(heal, null));
            Assert.AreEqual(600, b.Hp);
            Assert.IsTrue(ev.Any(x => x.Kind == EventKind.Heal && x.TargetId == "b"));
        }

        [Test]
        public void ChargedMoveHitsTheMemberItAnnounced()
        {
            var a = Ally("a", Stats(100000, 1, 0, 100));
            var b = Ally("b", Stats(100000, 1, 0, 100));
            var e = Enemy("e", Stats(100000, 100, 0, 200), EnemyMove.Wind("준비"), EnemyMove.Attack("강타", 3f));
            var battle = new InteractiveBattle(new[] { a, b }, new[] { e }, new SeededRandom(5));
            battle.Begin();
            Combatant announced = null;
            for (int i = 0; i < 20 && announced == null; i++)
            {
                battle.PlayerAct(PlayerCommand.Guard());
                if (e.Charging) announced = e.ChargeTarget;
            }
            Assert.IsNotNull(announced);
            List<BattleEvent> after = null;
            for (int i = 0; i < 6 && after == null; i++)
            {
                var ev = battle.PlayerAct(PlayerCommand.Guard());
                if (ev.Any(x => x.Kind == EventKind.Attack && x.ActorId == "e")) after = ev;
            }
            Assert.IsNotNull(after);
            Assert.AreEqual(announced.Id, after.First(x => x.Kind == EventKind.Attack && x.ActorId == "e").TargetId);
        }

        [Test]
        public void RejectedCommandDoesNotCorruptTheTurn()
        {
            var spell = new SkillSpec { Id = "big", Name = "big", Power = 5f, QiCost = 999 };
            var p = Player(Stats(1000, 50, 0, 100), spell);
            var e = Enemy("e", Stats(1000, 1, 0, 1));
            var b = Start(p, e);
            Assert.AreSame(p, b.Current);
            int actions = b.PartyActions;
            Assert.Throws<System.InvalidOperationException>(() => b.PlayerAct(PlayerCommand.UseSkill(spell, e)));   // not enough 내공
            Assert.Throws<System.InvalidOperationException>(() => b.PlayerAct(new PlayerCommand { Kind = CommandKind.Skill }));   // no skill at all
            Assert.AreSame(p, b.Current);
            Assert.AreEqual(actions, b.PartyActions);
            Assert.DoesNotThrow(() => b.PlayerAct(PlayerCommand.Guard()));   // the battle still works afterwards
        }
    }
}
