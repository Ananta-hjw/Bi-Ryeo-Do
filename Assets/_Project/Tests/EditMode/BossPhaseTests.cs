using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class BossPhaseTests
    {
        private static CombatStats Stats(int hp, int atk, int speed)
        {
            return new CombatStats { MaxHp = hp, MaxQi = 100, Attack = atk, Defense = 0, Speed = speed, CritRate = 0f, CritDamage = 1.5f };
        }

        private static readonly EnemyMove Opener = EnemyMove.Attack("a", .1f, 3), Middle = EnemyMove.Attack("b", .2f),
            Wind = EnemyMove.Wind("w"), Heavy = EnemyMove.Attack("h", 3f);

        private static Combatant Boss(int hp = 1000)
        {
            var stats = Stats(hp, 300, 100);
            stats.Defense = 100;
            return new Combatant("boss", "보스", Team.Enemy, stats, null)
            {
                IsBoss = true, Rank = EnemyRank.Boss, Moves = new List<EnemyMove> { Opener, Middle, Wind, Heavy },
                Phases = new List<BossPhase>
                {
                    new BossPhase { HpPct = .5f, Lines = new[] { "보스|첫째" }, AtkMul = 1.1f },
                    new BossPhase { HpPct = .25f, Lines = new[] { "보스|둘째" }, AtkMul = 1.3f, DefMul = 1.2f }
                }
            };
        }

        private static InteractiveBattle Start(out Combatant hero, out Combatant boss, int attack = 1)
        {
            hero = new Combatant("p", "비류연", Team.Player, Stats(1000000, attack, 1000), null);
            boss = Boss();
            var battle = new InteractiveBattle(hero, new[] { boss }, new SeededRandom(1));
            battle.Begin();
            return battle;
        }

        private static List<BattleEvent> EnemyTurn(InteractiveBattle battle, Combatant hero, Combatant boss)
        {
            hero.Gauge = 0;
            boss.Gauge = 1000;
            return battle.PlayerAct(PlayerCommand.Meditate());
        }

        private static List<BattleEvent> QueuePhase(InteractiveBattle battle, Combatant hero, Combatant boss, int hp)
        {
            boss.Hp = hp;
            hero.Gauge = 1000;
            boss.Gauge = 0;
            return battle.PlayerAct(PlayerCommand.Meditate());
        }

        [Test]
        public void EveryStoryBossHasTwoPhasesWithLinesSpokenOnlyByItselfOrTheHero()
        {
            var bosses = EarlyGameContent.Enemies.Where(e => e.IsBoss).ToList();
            CollectionAssert.AreEquivalent(bosses.Select(b => b.Id), BossPhaseLines.Ids);
            foreach (var boss in bosses)
            {
                var phases = BossPhases.For(boss);
                Assert.AreEqual(2, phases.Count, boss.Id);
                Assert.Greater(phases[0].HpPct, phases[1].HpPct, boss.Id);
                if (boss.RetreatHpPct > 0f) Assert.Greater(phases[1].HpPct, boss.RetreatHpPct);
                foreach (var phase in phases)
                {
                    Assert.That(phase.Lines.Length, Is.InRange(1, 4), boss.Id);
                    foreach (var raw in phase.Lines)
                    {
                        var line = StoryLine.Parse(raw);
                        if (line.IsDialogue) Assert.IsTrue(line.IsProtagonist || boss.Name.Contains(line.Speaker), boss.Id + ": " + raw);
                    }
                }
                var awakened = BossPhases.CreateAwakening(boss.Moves, 1);
                int wind = System.Array.FindIndex(boss.Moves, m => m.Charge);
                Assert.GreaterOrEqual(wind, 0, boss.Id);
                var original = boss.Moves[(wind + 1) % boss.Moves.Length];
                Assert.AreEqual(original.Power, awakened.Power, boss.Id);
                Assert.AreEqual(original.Hits, awakened.Hits, boss.Id);
                Assert.AreEqual(original.Inflict, awakened.Inflict, boss.Id);
                Assert.AreEqual(.7f, awakened.DamageFactor, .0001f, boss.Id);
            }
            Assert.IsNull(BossPhases.For(EarlyGameContent.FindEnemy("tiger")));
        }

        [Test]
        public void FallingBelowHalfQueuesOneStrikeAndPreservesScriptAndGauge()
        {
            Combatant hero, boss;
            var battle = Start(out hero, out boss);
            var original = boss.Moves;
            boss.MoveIndex = 1;
            var events = QueuePhase(battle, hero, boss, 500);
            Assert.AreEqual(1, events.Count(e => e.Kind == EventKind.Phase));
            Assert.AreEqual(1, boss.PhaseIndex);
            Assert.AreEqual(330, boss.Stats.Attack);
            Assert.AreEqual(100, boss.Stats.Defense);
            Assert.AreSame(original, boss.Moves);
            Assert.AreEqual(1, boss.MoveIndex);
            Assert.AreEqual(0, boss.Gauge, "no extra action and no gauge reset");
            Assert.AreEqual(BossPhases.AwakeningName, boss.PeekMove().Name);
            Assert.IsFalse(boss.PeekMove().Charge);
            events = QueuePhase(battle, hero, boss, 499);
            Assert.IsFalse(events.Any(e => e.Kind == EventKind.Phase));
        }

        [TestCase(500, .7f, 1)]
        [TestCase(250, 1f, 2)]
        public void PhaseStrikeUsesRequestedHeavyDamageWithoutSpendingScriptPosition(int hp, float factor, int phases)
        {
            Combatant hero, boss;
            var battle = Start(out hero, out boss);
            boss.MoveIndex = 1;
            var events = QueuePhase(battle, hero, boss, hp);
            Assert.AreEqual(phases, events.Count(e => e.Kind == EventKind.Phase));
            events = EnemyTurn(battle, hero, boss);
            var hit = events.Single(e => e.ActorId == boss.Id && e.Kind == EventKind.Attack);
            Assert.IsTrue(hit.Awakening);
            Assert.AreEqual(BossPhases.AwakeningName, hit.SkillId);
            int expected = BattleEngine.CalcDamage(boss.Stats, hero.Stats, new SkillSpec { Power = Heavy.Power * factor }, false);
            Assert.AreEqual(expected, hit.Damage);
            Assert.AreEqual(1, boss.MoveIndex);
            Assert.IsNull(boss.AwakeningStrike);
            Assert.AreEqual(100 - InteractiveBattle.EnemyQiCost(Heavy), boss.Qi, "same resource cost as the original heavy");
            events = EnemyTurn(battle, hero, boss);
            Assert.AreEqual(Middle.Name, events.First(e => e.ActorId == boss.Id && e.Kind == EventKind.Attack).SkillId);
            Assert.AreEqual(2, boss.MoveIndex);
        }

        [Test]
        public void OneActualBlowAcrossBothThresholdsQueuesOnlyTheFullStrengthStrike()
        {
            Combatant hero, boss;
            var battle = Start(out hero, out boss, 3000);
            var skill = new SkillSpec { Id = "test", Name = "공격", Power = 1f };
            hero.Skills.Add(skill);
            hero.Gauge = 1000;
            boss.Gauge = 0;
            var events = battle.PlayerAct(PlayerCommand.UseSkill(skill, boss));
            Assert.That(boss.Hp, Is.GreaterThan(0).And.LessThanOrEqualTo(250));
            Assert.AreEqual(2, events.Count(e => e.Kind == EventKind.Phase));
            Assert.AreEqual(390, boss.Stats.Attack);
            Assert.AreEqual(120, boss.Stats.Defense);
            Assert.AreEqual(1f, boss.AwakeningStrike.DamageFactor);
            events = EnemyTurn(battle, hero, boss);
            Assert.AreEqual(1, events.Count(e => e.Awakening));
            Assert.AreEqual(0, boss.MoveIndex);
            events = EnemyTurn(battle, hero, boss);
            Assert.IsFalse(events.Any(e => e.Awakening));
            Assert.AreEqual(Opener.Name, events.First(e => e.Kind == EventKind.Attack && e.ActorId == boss.Id).SkillId);
        }

        [Test]
        public void SecondThresholdUpgradesAnUnspentFirstStrikeRatherThanQueuingTwo()
        {
            Combatant hero, boss;
            var battle = Start(out hero, out boss);
            QueuePhase(battle, hero, boss, 400);
            Assert.AreEqual(.7f, boss.AwakeningStrike.DamageFactor, .0001f);
            QueuePhase(battle, hero, boss, 200);
            Assert.AreEqual(1f, boss.AwakeningStrike.DamageFactor);
            var events = EnemyTurn(battle, hero, boss);
            Assert.AreEqual(1, events.Count(e => e.Awakening));
            Assert.IsNull(boss.AwakeningStrike);
        }

        [Test]
        public void SeparateTransitionsEachGrantExactlyOneStrike()
        {
            Combatant hero, boss;
            var battle = Start(out hero, out boss);
            QueuePhase(battle, hero, boss, 400);
            Assert.AreEqual(1, EnemyTurn(battle, hero, boss).Count(e => e.Awakening));
            QueuePhase(battle, hero, boss, 200);
            Assert.AreEqual(1, EnemyTurn(battle, hero, boss).Count(e => e.Awakening));
            Assert.IsFalse(EnemyTurn(battle, hero, boss).Any(e => e.Awakening));
        }

        [Test]
        public void BalanceBotProtectsTheFrontMemberAgainstAnAwakeningWithoutAWindUp()
        {
            Combatant hero, boss;
            var battle = Start(out hero, out boss);
            QueuePhase(battle, hero, boss, 400);
            Assert.IsFalse(boss.Charging);
            Assert.AreEqual(CommandKind.Guard, SimpleBot.Choose(battle).Kind);
        }

        [Test]
        public void WindUpAndItsOriginalTargetSurviveAwakeningAndThenResolve()
        {
            Combatant hero, boss;
            var battle = Start(out hero, out boss);
            boss.MoveIndex = 3;
            boss.Charging = true;
            boss.ChargeTarget = hero;
            QueuePhase(battle, hero, boss, 400);
            Assert.IsTrue(EnemyTurn(battle, hero, boss).Any(e => e.Awakening));
            Assert.IsTrue(boss.Charging);
            Assert.AreSame(hero, boss.ChargeTarget);
            Assert.AreEqual(3, boss.MoveIndex);
            var events = EnemyTurn(battle, hero, boss);
            Assert.AreEqual(Heavy.Name, events.First(e => e.Kind == EventKind.Attack && e.ActorId == boss.Id).SkillId);
            Assert.IsFalse(boss.Charging);
            Assert.IsNull(boss.ChargeTarget);
            Assert.AreEqual(4, boss.MoveIndex);
        }

        [Test]
        public void PlannedWindUpStillRunsOnTheTurnAfterAwakening()
        {
            Combatant hero, boss;
            var battle = Start(out hero, out boss);
            boss.MoveIndex = 2;
            QueuePhase(battle, hero, boss, 400);
            EnemyTurn(battle, hero, boss);
            var events = EnemyTurn(battle, hero, boss);
            Assert.AreEqual(Wind.Name, events.Single(e => e.ActorId == boss.Id && e.Kind == EventKind.Charge).SkillId);
            Assert.IsTrue(boss.Charging);
            Assert.AreEqual(3, boss.MoveIndex);
        }

        [Test]
        public void PhaseChangeDoesNotDelayAnAlreadyDueBossTurn()
        {
            Combatant hero, boss;
            var battle = Start(out hero, out boss);
            boss.Hp = 400;
            var events = EnemyTurn(battle, hero, boss);
            int phase = events.FindIndex(e => e.Kind == EventKind.Phase);
            Assert.IsTrue(events.Skip(phase + 1).Any(e => e.ActorId == boss.Id && e.Awakening));
        }

        [Test]
        public void DamageOverTimeCanTriggerAndUseAwakeningOnTheSameNormalTurn()
        {
            Combatant hero, boss;
            var battle = Start(out hero, out boss);
            boss.Hp = 515;
            boss.AddStatus(StatusKind.Burn, 3);
            var events = EnemyTurn(battle, hero, boss);
            int tick = events.FindIndex(e => e.Kind == EventKind.Status && e.TargetId == boss.Id && e.Damage > 0);
            int phase = events.FindIndex(e => e.Kind == EventKind.Phase);
            int strike = events.FindIndex(e => e.Awakening);
            Assert.That(tick, Is.GreaterThanOrEqualTo(0).And.LessThan(phase));
            Assert.Greater(strike, phase);
            Assert.AreEqual(1, boss.PhaseIndex);
            Assert.AreEqual(0, boss.MoveIndex);
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void AwakeningRetainsExistingDryAndBurstModifiers(bool dry, bool burst)
        {
            Combatant hero, boss;
            var battle = Start(out hero, out boss);
            QueuePhase(battle, hero, boss, 400);
            if (dry) boss.Qi = 0;
            if (burst) boss.Spirit = InteractiveBattle.MaxSpirit;
            var hit = EnemyTurn(battle, hero, boss).Single(e => e.Awakening);
            float power = Heavy.Power * .7f * (dry ? InteractiveBattle.EnemyDryFactor : 1f) * (burst ? InteractiveBattle.EnemyBurstFactor : 1f);
            Assert.AreEqual(BattleEngine.CalcDamage(boss.Stats, hero.Stats, new SkillSpec { Power = power }, false), hit.Damage);
            Assert.AreEqual(dry, hit.Weakened);
            Assert.AreEqual(burst, hit.Burst);
        }

        [Test]
        public void MultiHitAwakeningIsOneActionAndLeavesTheNormalScriptUntouched()
        {
            Combatant hero, boss;
            var battle = Start(out hero, out boss);
            boss.Moves[3] = EnemyMove.Attack("three", 1f, 3);
            QueuePhase(battle, hero, boss, 400);
            int actions = battle.EnemyActions;
            var hits = EnemyTurn(battle, hero, boss).Where(e => e.Awakening).ToList();
            Assert.AreEqual(3, hits.Count);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, hits.Select(e => e.HitIndex));
            Assert.IsTrue(hits.All(e => e.HitCount == 3));
            Assert.AreEqual(actions + 1, battle.EnemyActions);
            Assert.AreEqual(0, boss.MoveIndex);
            Assert.IsNull(boss.AwakeningStrike);
        }

        [Test]
        public void AwakeningKeepsMultiHitsAndCanBeInterruptedWithoutLosingTheOldCharge()
        {
            Combatant hero, boss;
            var battle = Start(out hero, out boss);
            var multihit = EnemyMove.Attack("three", 1f, 3);
            boss.Moves[3] = multihit;
            var copy = BossPhases.CreateAwakening(boss.Moves, 1);
            Assert.AreEqual(3, copy.Hits);
            Assert.AreEqual(1f, copy.Power);
            boss.MoveIndex = 3;
            boss.Charging = true;
            boss.ChargeTarget = hero;
            QueuePhase(battle, hero, boss, 400);
            var stun = new SkillSpec { Id = "stun", Name = "점혈", Power = .01f, Effect = SkillEffect.Stun };
            hero.Skills.Add(stun);
            hero.Gauge = 1000;
            boss.Gauge = 0;
            var events = battle.PlayerAct(PlayerCommand.UseSkill(stun, boss));
            Assert.IsTrue(events.Any(e => e.Interrupted));
            Assert.IsNull(boss.AwakeningStrike);
            Assert.IsTrue(boss.Charging, "only the pending awakening is interrupted");
            Assert.AreEqual(3, boss.MoveIndex);
            Assert.IsFalse(boss.Stunned, "no action delay is added to a boss");
        }
    }
}
