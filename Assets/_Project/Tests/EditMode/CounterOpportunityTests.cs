using System;
using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class CounterOpportunityTests
    {
        private static Combatant Unit(string id, Team team, params SkillSpec[] skills)
        {
            return new Combatant(id, id, team, new CombatStats
            {
                MaxHp = 100000, MaxQi = 1000, Attack = 100, Defense = 0,
                Speed = 100, CritRate = 0f, CritDamage = 1.5f
            }, skills);
        }

        private static Combatant Foe(string id = "enemy", int hits = 3)
        {
            var e = Unit(id, Team.Enemy);
            e.IsBoss = true;
            e.Moves = new List<EnemyMove> { EnemyMove.Wind("준비"), EnemyMove.Attack("강타", 2f, hits), EnemyMove.Plain };
            return e;
        }

        private static InteractiveBattle Start(Combatant p, params Combatant[] enemies)
        {
            var b = new InteractiveBattle(p, enemies, new SeededRandom(12));
            b.Begin();
            return b;
        }

        private static void Charge(InteractiveBattle b, Combatant e)
        {
            for (int i = 0; i < 10 && !e.Charging; i++) b.PlayerAct(PlayerCommand.Guard());
            Assert.IsTrue(e.Charging);
        }

        [Test]
        public void InterruptGrantsOpeningButDoesNotBoostTheSameMultiHitCommand()
        {
            var stun = new SkillSpec { Id = "stun", Name = "점혈", Effect = SkillEffect.Stun, Hits = 3 };
            var p = Unit("p", Team.Player, stun);
            var e = Foe();
            var b = Start(p, e);
            Charge(b, e);
            var hits = b.PlayerAct(PlayerCommand.UseSkill(stun, e)).Where(x => x.ActorId == p.Id && x.Kind == EventKind.Attack).ToList();
            Assert.AreEqual(1, hits.Count(x => x.Interrupted && x.CounterReady));
            Assert.IsFalse(hits.Any(x => x.Counter));
            Assert.AreSame(e, b.CounterTargetOf(p));
            var counter = b.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e)).First(x => x.ActorId == p.Id);
            Assert.IsTrue(counter.Counter);
            Assert.AreEqual((int)Math.Round(hits[0].Damage * InteractiveBattle.CounterDamageFactor), counter.Damage);
            Assert.IsNull(b.CounterTargetOf(p));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GuardOrFullEvasionOfChargedMultiHitGrantsExactlyOneOpening(bool evade)
        {
            var footwork = new SkillSpec { Id = "evade", Effect = SkillEffect.Evade, EvadeChance = 1f };
            var p = Unit("p", Team.Player, footwork);
            var e = Foe();
            var b = Start(p, e);
            Charge(b, e);
            var events = b.PlayerAct(evade ? PlayerCommand.UseSkill(footwork, null) : PlayerCommand.Guard());
            var incoming = events.Where(x => x.ActorId == e.Id && x.Kind == EventKind.Attack).ToList();
            Assert.AreEqual(3, incoming.Count);
            Assert.AreEqual(1, incoming.Count(x => x.CounterReady));
            Assert.IsTrue(incoming.Last().CounterReady);
            Assert.AreSame(e, b.CounterTargetOf(p));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OrdinaryAttackOrUnprotectedHeavyDoesNotGrantOpening(bool charged)
        {
            var p = Unit("p", Team.Player);
            var e = Foe();
            if (!charged) e.Moves = new List<EnemyMove> { EnemyMove.Plain };
            var b = Start(p, e);
            if (charged) Charge(b, e);
            var events = b.PlayerAct(charged ? PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e) : PlayerCommand.Guard());
            Assert.IsFalse(events.Any(x => x.CounterReady));
            Assert.IsNull(b.CounterTargetOf(p));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OpeningExpiresOnNextNonAttackOrWrongTargetAction(bool wrongTarget)
        {
            var p = Unit("p", Team.Player);
            var e = Foe();
            var other = Foe("other");
            other.Moves = new List<EnemyMove> { EnemyMove.Plain };
            var b = Start(p, e, other);
            Charge(b, e);
            b.PlayerAct(PlayerCommand.Guard());
            Assert.AreSame(e, b.CounterTargetOf(p));
            var events = b.PlayerAct(wrongTarget ? PlayerCommand.UseSkill(new SkillSpec { Id = "reach", Range = 4 }, other) : PlayerCommand.Meditate());
            Assert.IsFalse(events.Any(x => x.Counter));
            Assert.IsNull(b.CounterTargetOf(p));
        }

        [Test]
        public void AreaMultiHitBoostsOnlyTheOpeningTargetWithoutCompounding()
        {
            var art = new SkillSpec { Id = "area", AllEnemies = true, Range = 4, Hits = 4 };
            var p = Unit("p", Team.Player, art);
            var e = Foe();
            var other = Foe("other");
            other.Moves = new List<EnemyMove> { EnemyMove.Plain };
            var b = Start(p, e, other);
            Charge(b, e);
            b.PlayerAct(PlayerCommand.Guard());
            var hits = b.PlayerAct(PlayerCommand.UseSkill(art, e)).Where(x => x.ActorId == p.Id && x.Kind == EventKind.Attack).ToList();
            var empowered = hits.Where(x => x.TargetId == e.Id).ToList();
            var plain = hits.Where(x => x.TargetId == other.Id).ToList();
            Assert.AreEqual(4, empowered.Count);
            Assert.IsTrue(empowered.All(x => x.Counter));
            Assert.IsTrue(plain.All(x => !x.Counter));
            Assert.AreEqual(1, empowered.Select(x => x.Damage).Distinct().Count());
            Assert.AreEqual((int)Math.Round(plain[0].Damage * 1.25f), empowered[0].Damage);
        }

        [Test]
        public void FailedCommandDoesNotConsumeOpening()
        {
            var p = Unit("p", Team.Player);
            var e = Foe();
            var b = Start(p, e);
            Charge(b, e);
            b.PlayerAct(PlayerCommand.Guard());
            Assert.Throws<InvalidOperationException>(() => b.PlayerAct(PlayerCommand.UseSkill(new SkillSpec { Id = "unavailable", QiCost = 10000 }, e)));
            Assert.AreSame(e, b.CounterTargetOf(p));
        }

        [Test]
        public void GuardedLethalBlowDoesNotGrantOpening()
        {
            var p = Unit("p", Team.Player);
            var e = Foe();
            var b = Start(p, e);
            Charge(b, e);
            p.Hp = 1;
            var events = b.PlayerAct(PlayerCommand.Guard());
            Assert.IsFalse(p.IsAlive);
            Assert.IsFalse(events.Any(x => x.CounterReady));
            Assert.IsNull(b.CounterTargetOf(p));
        }

        [Test]
        public void AwakeningCanBeInterruptedWithoutChangingNormalChargeOrTrialProgress()
        {
            var stun = new SkillSpec { Id = "stun", Effect = SkillEffect.Stun };
            var p = Unit("p", Team.Player, stun);
            var e = Foe();
            var b = Start(p, e);
            Charge(b, e);
            e.AwakeningStrike = new EnemyMove { Name = "각성", Awakening = true, Power = 3f };
            b.Trial = TrialRule.Technique("test", "test", "stun", 3, true);
            var events = b.PlayerAct(PlayerCommand.UseSkill(stun, e));
            Assert.IsTrue(events.Any(x => x.Interrupted && x.CounterReady));
            Assert.IsNull(e.AwakeningStrike);
            Assert.AreEqual(1, b.TechniqueUses);
            Assert.IsTrue(b.TechniqueSucceeded);
        }

        private sealed class AlternatingRandom : IRandom
        {
            private int _calls;
            public double NextDouble() { return _calls++ % 2 == 0 ? .1 : .9; }
        }

        [Test]
        public void PartialEvasionDoesNotCountAsSuccessfullyAnsweringTheWholeMove()
        {
            var footwork = new SkillSpec { Id = "footwork", Effect = SkillEffect.Evade, EvadeChance = .5f };
            var p = Unit("p", Team.Player, footwork);
            var e = Foe();
            var b = new InteractiveBattle(p, new[] { e }, new AlternatingRandom());
            b.Begin();
            Charge(b, e);
            var hits = b.PlayerAct(PlayerCommand.UseSkill(footwork, null)).Where(x => x.Kind == EventKind.Attack).ToList();
            Assert.IsTrue(hits.Any(x => x.Dodged));
            Assert.IsTrue(hits.Any(x => !x.Dodged));
            Assert.IsFalse(hits.Any(x => x.CounterReady));
            Assert.IsNull(b.CounterTargetOf(p));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LifelineRuleStillCountsGuardedHitsButNotFullyEvadedMoves(bool evade)
        {
            var footwork = new SkillSpec { Id = "footwork", Effect = SkillEffect.Evade };
            var p = Unit("p", Team.Player, footwork);
            var e = Foe();
            var b = Start(p, e);
            b.Trial = TrialRule.Lifeline("test", "test", 5);
            Charge(b, e);
            b.PlayerAct(evade ? PlayerCommand.UseSkill(footwork, null) : PlayerCommand.Guard());
            Assert.AreEqual(evade ? 0 : 1, b.LifelinesCut);
            Assert.AreSame(e, b.CounterTargetOf(p));
        }

        [Test]
        public void CounterCriticalHarmonyAndMultiHitApplyEachMultiplierOnlyOnce()
        {
            var art = new SkillSpec { Id = "chain", Hits = 3 };
            var stun = new SkillSpec { Id = "stun", Effect = SkillEffect.Stun };
            var p = Unit("p", Team.Player, art, stun);
            var stats = p.Stats;
            stats.CritRate = 1f;
            p = new Combatant("p", "p", Team.Player, stats, new[] { art, stun });
            var e = Foe();
            var b = Start(p, e);
            Charge(b, e);
            b.Trial = TrialRule.Harmony("test", "test", 20);
            b.PlayerAct(PlayerCommand.UseSkill(stun, e));
            Assert.AreSame(e, b.CounterTargetOf(p));
            var hits = b.PlayerAct(PlayerCommand.UseSkill(art, e)).Where(x => x.ActorId == p.Id && x.Kind == EventKind.Attack).ToList();
            int ordinary = BattleEngine.CalcDamage(p.Stats, e.Stats, art, true);
            Assert.IsTrue(hits.All(x => x.IsCrit && x.Counter && x.Harmony));
            int expected = (int)Math.Round(ordinary * InteractiveBattle.HarmonyDamageFactor);
            expected = (int)Math.Round(expected * InteractiveBattle.CounterDamageFactor);
            Assert.IsTrue(hits.All(x => x.Damage == expected));
        }

        [Test]
        public void JointAttackDoesNotShareOneRespondersOpeningOrGrantExtraTurns()
        {
            var p = Unit("p", Team.Player);
            var ally = Unit("ally", Team.Player);
            var e = Foe();
            var b = new InteractiveBattle(new[] { p, ally }, new[] { e }, new SeededRandom(12));
            b.Begin();
            for (int i = 0; i < 20 && (b.Current != p || b.CounterTargetOf(p) == null); i++)
                b.PlayerAct(PlayerCommand.Guard());
            Assert.AreSame(p, b.Current);
            Assert.AreSame(e, b.CounterTargetOf(p));
            p.Spirit = 100;
            int actions = b.PartyActions;
            var events = b.PlayerAct(PlayerCommand.Joint(new SkillSpec { Id = "joint", Range = 4 }, ally, e));
            var joint = events.Where(x => x.Joint && x.Kind == EventKind.Attack).ToList();
            Assert.AreEqual(2, joint.Count);
            Assert.IsTrue(joint.First().Counter);
            Assert.IsFalse(joint.Last().Counter);
            Assert.AreEqual(actions + 1, b.PartyActions);
            Assert.IsNull(b.CounterTargetOf(p));
            Assert.IsNull(b.CounterTargetOf(ally));
        }

        [Test]
        public void OpeningIsNotGrantedForAnInterruptedTargetThatRetreatsOrDies()
        {
            var stun = new SkillSpec { Id = "stun", Effect = SkillEffect.Stun, Power = 10f };
            var p = Unit("p", Team.Player, stun);
            var e = Foe();
            var b = Start(p, e);
            Charge(b, e);
            e.RetreatHpPct = .999f;
            var hit = b.PlayerAct(PlayerCommand.UseSkill(stun, e)).First(x => x.Kind == EventKind.Attack);
            Assert.IsTrue(hit.Retreated);
            Assert.IsTrue(hit.TargetDefeated);
            Assert.IsFalse(hit.CounterReady);
            Assert.IsNull(b.CounterTargetOf(p));
        }

        [Test]
        public void InterruptionDoesNotAdvertiseAnOpeningWhenBleedKillsTheTarget()
        {
            var stun = new SkillSpec { Id = "stun", Effect = SkillEffect.Stun, Power = .1f };
            var p = Unit("p", Team.Player, stun);
            var e = Foe();
            var b = Start(p, e);
            Charge(b, e);
            e.Hp = 100;
            e.AddStatus(StatusKind.Bleed, 3);
            var events = b.PlayerAct(PlayerCommand.UseSkill(stun, e));
            Assert.IsTrue(events.Any(x => x.Interrupted));
            Assert.IsFalse(events.Any(x => x.CounterReady));
            Assert.IsFalse(e.IsAlive);
            Assert.IsNull(b.CounterTargetOf(p));
        }

        [Test]
        public void BossPhaseChangePreservesTheEarnedOpeningAndStillSchedulesItsStrike()
        {
            var stun = new SkillSpec { Id = "stun", Effect = SkillEffect.Stun };
            var p = Unit("p", Team.Player, stun);
            var e = Foe();
            e.Phases = new List<BossPhase> { new BossPhase { HpPct = .9995f, AtkMul = 1.1f, DefMul = 1f } };
            var b = Start(p, e);
            Charge(b, e);
            e.Hp = (int)(e.Stats.MaxHp * e.Phases[0].HpPct) + 1;
            var events = b.PlayerAct(PlayerCommand.UseSkill(stun, e));
            Assert.IsTrue(events.Any(x => x.Kind == EventKind.Phase));
            Assert.IsTrue(events.Any(x => x.Awakening));
            Assert.AreSame(e, b.CounterTargetOf(p));
            Assert.IsTrue(b.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e)).Any(x => x.Counter));
        }
    }
}
