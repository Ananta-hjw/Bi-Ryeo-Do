using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class BossPhaseTests
    {
        private static CombatStats Stats(int hp, int atk, int speed) { return new CombatStats { MaxHp = hp, MaxQi = 100, Attack = atk, Defense = 0, Speed = speed, CritRate = 0f, CritDamage = 1.5f }; }

        private static readonly EnemyMove Opener = EnemyMove.Attack("a", 0.01f, 3), Middle = EnemyMove.Attack("b", 0.01f),
            Wind = EnemyMove.Wind("w"), Heavy = EnemyMove.Attack("h", 0.01f);

        private static Combatant Boss(int hp)
        {
            var s = Stats(hp, 100, 100); s.Defense = 100;
            var c = new Combatant("boss", "보스", Team.Enemy, s, null) { IsBoss = true, Rank = EnemyRank.Boss };
            c.Moves = new List<EnemyMove> { Opener, Middle, Wind, Heavy };
            EnemyMove[] fierce, desperate;
            BossPhases.Reshape(c.Moves.ToArray(), out fierce, out desperate);
            c.Phases = new List<BossPhase>
            {
                new BossPhase { HpPct = 0.5f, Lines = new[] { "보스|첫째" }, Moves = fierce, AtkMul = 1.1f },
                new BossPhase { HpPct = 0.25f, Lines = new[] { "보스|둘째" }, Moves = desperate, AtkMul = 1.3f, DefMul = 1.2f },
            };
            return c;
        }

        [Test]
        public void EveryStoryBossHasTwoPhasesWithLinesSpokenOnlyByItselfOrTheHero()
        {
            var bosses = EarlyGameContent.Enemies.Where(e => e.IsBoss).ToList();
            CollectionAssert.AreEquivalent(bosses.Select(b => b.Id), BossPhaseLines.Ids, "every story boss and nothing else");
            foreach (var boss in bosses)
            {
                var phases = BossPhases.For(boss);
                Assert.AreEqual(2, phases.Count, boss.Id);
                Assert.Greater(phases[0].HpPct, phases[1].HpPct, boss.Id);
                if (boss.RetreatHpPct > 0f) Assert.Greater(phases[1].HpPct, boss.RetreatHpPct, boss.Id + " changes before it withdraws");
                foreach (var phase in phases)
                {
                    Assert.IsNotNull(phase.Moves, boss.Id + " has a wind-up to reshape around");
                    Assert.That(phase.Lines.Length, Is.InRange(1, 4), boss.Id);
                    foreach (var raw in phase.Lines)
                    {
                        var line = StoryLine.Parse(raw);
                        if (!line.IsDialogue) continue;
                        Assert.LessOrEqual(line.Speaker.Length, 8, raw);
                        Assert.IsTrue(line.IsProtagonist || boss.Name.Contains(line.Speaker), boss.Id + ": " + raw);
                    }
                }
            }
            Assert.IsNull(BossPhases.For(EarlyGameContent.FindEnemy("tiger")), "henchmen have none");
        }

        [Test]
        public void ReshapeKeepsTheBossesOwnMoves()
        {
            EnemyMove[] fierce, desperate;
            BossPhases.Reshape(new[] { Opener, Middle, Wind, Heavy }, out fierce, out desperate);
            CollectionAssert.AreEqual(new[] { Middle, Opener, Wind, Heavy }, fierce);
            CollectionAssert.AreEqual(new[] { Wind, Heavy, Opener, Middle }, desperate);
            BossPhases.Reshape(new[] { Opener, Middle }, out fierce, out desperate);
            Assert.IsNull(fierce, "no wind-up: the script stays");
        }

        [Test]
        public void FallingBelowHalfStartsTheNextPhaseOnce()
        {
            var hit = new SkillSpec { Id = "x", Name = "x", Power = 1f };
            var p = new Combatant("p", "비류연", Team.Player, Stats(100000, 1, 1000), new[] { hit });
            var boss = Boss(1000);
            var b = new InteractiveBattle(p, new[] { boss }, new SeededRandom(1));
            b.Begin();
            boss.Hp = 520;
            var ev = b.PlayerAct(PlayerCommand.Guard());
            Assert.IsFalse(ev.Any(x => x.Kind == EventKind.Phase), "still above half");

            boss.Hp = 500;
            ev = b.PlayerAct(PlayerCommand.Guard());
            var phase = ev.Single(x => x.Kind == EventKind.Phase);
            Assert.AreEqual(1, phase.HitIndex);
            CollectionAssert.AreEqual(new[] { "보스|첫째" }, phase.Lines);
            Assert.AreEqual(1, boss.PhaseIndex);
            Assert.AreEqual(Middle, boss.Moves[0], "phase 1 script");
            Assert.AreEqual(110, boss.Stats.Attack, "attack +10%");
            Assert.AreEqual(100, boss.Stats.Defense, "defense unchanged until 25%");

            ev = b.PlayerAct(PlayerCommand.Guard());
            Assert.IsFalse(ev.Any(x => x.Kind == EventKind.Phase), "only once");
        }

        [Test]
        public void OneBlowAcrossBothThresholdsStartsBothAndTheLastPhaseIsStrongest()
        {
            var p = new Combatant("p", "비류연", Team.Player, Stats(100000, 1, 1000), null);
            var boss = Boss(1000);
            var b = new InteractiveBattle(p, new[] { boss }, new SeededRandom(1));
            b.Begin();
            boss.Hp = 200;
            var ev = b.PlayerAct(PlayerCommand.Guard());
            Assert.AreEqual(2, ev.Count(x => x.Kind == EventKind.Phase));
            Assert.AreEqual(130, boss.Stats.Attack, "+30% in all, not 1.1 x 1.3");
            Assert.AreEqual(120, boss.Stats.Defense);
            Assert.AreEqual(Wind, boss.PeekMove(), "phase 2 opens with the wind-up");
        }

        [Test]
        public void ThePhaseChangeDoesNotCostTheBossItsTurn()
        {
            var p = new Combatant("p", "비류연", Team.Player, Stats(100000, 1, 100), null);
            var boss = Boss(1000);
            var b = new InteractiveBattle(p, new[] { boss }, new SeededRandom(1));
            b.Begin();
            boss.Hp = 400;
            boss.Gauge = 900;   // the boss is next to act after the hero's move
            var ev = b.PlayerAct(PlayerCommand.Guard());
            int phaseAt = ev.FindIndex(x => x.Kind == EventKind.Phase);
            Assert.GreaterOrEqual(phaseAt, 0);
            Assert.IsTrue(ev.Skip(phaseAt + 1).Any(x => x.ActorId == boss.Id), "the boss still takes its turn right after the change");
        }

        [Test]
        public void AWindUpInProgressStillEndsInItsHeavyBlow()
        {
            var p = new Combatant("p", "비류연", Team.Player, Stats(100000, 1, 100), null);
            var boss = Boss(1000);
            boss.MoveIndex = 2;   // next: the wind-up
            var b = new InteractiveBattle(p, new[] { boss }, new SeededRandom(1));
            b.Begin();
            while (!boss.Charging) b.PlayerAct(PlayerCommand.Guard());
            boss.Hp = 450;
            var ev = b.PlayerAct(PlayerCommand.Guard());
            int phaseAt = ev.FindIndex(x => x.Kind == EventKind.Phase);
            Assert.GreaterOrEqual(phaseAt, 0);
            var next = ev.Skip(phaseAt + 1).First(x => x.ActorId == boss.Id);
            Assert.AreEqual(Heavy.Name, next.SkillId, "the telegraphed blow still comes after the change");
            Assert.IsNull(boss.PendingMoves, "the new script starts once the blow is done");
            Assert.AreEqual(Middle, boss.Moves[0]);
        }
    }
}
