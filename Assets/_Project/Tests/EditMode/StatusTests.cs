using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StatusTests
    {
        private static CombatStats Stats(int hp, int atk, int speed) { return new CombatStats { MaxHp = hp, MaxQi = 100, Attack = atk, Defense = 0, Speed = speed, CritRate = 0f, CritDamage = 1.5f }; }

        private static Combatant Enemy(string id, CombatStats s, params EnemyMove[] moves)
        {
            var c = new Combatant(id, id, Team.Enemy, s, null);
            c.Moves = new List<EnemyMove>(moves.Length > 0 ? moves : new[] { EnemyMove.Plain });
            return c;
        }

        [Test]
        public void EnemyMovesTakeTheirStatusFromTheName()
        {
            Assert.AreEqual(StatusKind.Poison, EnemyMove.Attack("독침", 1f).Inflict);
            Assert.AreEqual(StatusKind.Chill, EnemyMove.Attack("백빙신침", 1f).Inflict);
            Assert.AreEqual(StatusKind.Burn, EnemyMove.Attack("화령염천도세", 1f).Inflict);
            Assert.AreEqual(StatusKind.None, EnemyMove.Attack("화살", 1f).Inflict, "화살 is not fire");
            Assert.IsTrue(EarlyGameContent.Enemies.Any(e => e.Moves.Any(m => m.Inflict == StatusKind.Bleed)));
        }

        [Test]
        public void PoisonTicksAtTheUnitsTurnAndWearsOff()
        {
            var poison = new SkillSpec { Id = "p", Name = "p", Power = 0.01f, Inflict = StatusKind.Poison, InflictChance = 1f };
            var p = new Combatant("p", "비류연", Team.Player, Stats(1000, 1, 100), new[] { poison });
            var e = Enemy("e", Stats(10000, 1, 100), EnemyMove.Attack("x", 0.01f));
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1));
            b.Begin();
            var ev = b.PlayerAct(PlayerCommand.UseSkill(poison, e));
            Assert.IsTrue(ev.Any(x => x.Kind == EventKind.Status && x.Status == StatusKind.Poison && x.Damage == 0), "applied");
            var tick = ev.First(x => x.Kind == EventKind.Status && x.Damage > 0);
            Assert.AreEqual(Statuses.DotDamage(e, StatusKind.Poison), tick.Damage);
            Assert.AreEqual(500, tick.Damage, "5% of max HP");
            for (int i = 0; i < 4; i++) b.PlayerAct(PlayerCommand.Guard());
            Assert.IsFalse(e.Has(StatusKind.Poison), "lasts three of its own turns");
        }

        [Test]
        public void BossesTakeLessFromDamageOverTime()
        {
            var e = Enemy("e", Stats(10000, 1, 100));
            var boss = Enemy("b", Stats(10000, 1, 100)); boss.IsBoss = true; boss.Rank = EnemyRank.Boss;
            Assert.Less(Statuses.DotDamage(boss, StatusKind.Burn), Statuses.DotDamage(e, StatusKind.Burn));
        }

        [Test]
        public void ChargedBlowsLeaveInnerWoundUnlessGuardedAndItStopsQi()
        {
            var p = new Combatant("p", "비류연", Team.Player, Stats(100000, 1, 100), null);
            var e = Enemy("e", Stats(100000, 1, 100), EnemyMove.Wind("w"), EnemyMove.Attack("h", 0.5f));
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1));
            b.Begin();
            b.PlayerAct(PlayerCommand.Guard());   // the wind-up comes, then the guarded blow
            for (int i = 0; i < 3 && !p.Has(StatusKind.InnerWound); i++) b.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e));
            Assert.IsTrue(p.Has(StatusKind.InnerWound), "an unguarded charged blow leaves 내상");
            p.Qi = 0;
            b.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e));
            Assert.AreEqual(0, p.Qi, "no 내공 comes back while wounded");
        }

        [Test]
        public void GuardBlocksTheStatusOfAMove()
        {
            var p = new Combatant("p", "비류연", Team.Player, Stats(100000, 1, 100), null);
            var e = Enemy("e", Stats(100000, 1, 100), EnemyMove.Attack("독침", 0.1f));
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1));
            b.Begin();
            for (int i = 0; i < 20; i++) b.PlayerAct(PlayerCommand.Guard());
            Assert.IsFalse(p.Has(StatusKind.Poison));
        }

        [Test]
        public void MeditateCuresOneAndTheAntidoteCuresAll()
        {
            var p = new Combatant("p", "비류연", Team.Player, Stats(100000, 1, 100), null);
            var e = Enemy("e", Stats(100000, 1, 1));
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1));
            b.Begin();
            p.AddStatus(StatusKind.Poison, 9); p.AddStatus(StatusKind.Burn, 9); p.AddStatus(StatusKind.Chill, 9);
            b.PlayerAct(PlayerCommand.Meditate());
            Assert.IsFalse(p.Has(StatusKind.Burn), "화상 goes first");
            Assert.IsTrue(p.Has(StatusKind.Poison));
            b.PlayerAct(PlayerCommand.UseItem(EarlyGameContent.FindItem("antidote")));
            Assert.AreEqual(0, p.Statuses.Count);
        }

        [Test]
        public void YunJunhosCleanseClearsTheChosenAlly()
        {
            var yun = EarlyGameContent.FindCompanion("yun_junho").CreateCombatant(10);
            var cleanse = yun.Skills.First(s => s.Effect == SkillEffect.Cleanse);
            var p = new Combatant("p", "비류연", Team.Player, Stats(100000, 1, 1), null);
            var e = Enemy("e", Stats(100000, 1, 1));
            var b = new InteractiveBattle(new[] { p, yun }, new[] { e }, new SeededRandom(1));
            b.Begin();
            while (b.Current != yun) b.PlayerAct(PlayerCommand.Guard());
            p.AddStatus(StatusKind.Bleed, 3); p.AddStatus(StatusKind.InnerWound, 3);
            b.PlayerAct(PlayerCommand.UseSkill(cleanse, null).WithAlly(p));
            Assert.AreEqual(0, p.Statuses.Count);
        }

        [Test]
        public void ChillSlowsTheActionGauge()
        {
            var p = new Combatant("p", "비류연", Team.Player, Stats(100000, 1, 100), null);
            var e = Enemy("e", Stats(100000, 1, 100));
            Assert.AreEqual(1f, Statuses.SpeedFactor(e));
            e.AddStatus(StatusKind.Chill, 2);
            Assert.Less(Statuses.SpeedFactor(e), 1f);
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1));
            b.Begin();
            var order = b.PreviewOrder(6);
            Assert.Greater(order.Count(u => u == p), order.Count(u => u == e));
        }

        [Test]
        public void StatusesEndWithTheFight()
        {
            var p = new Combatant("p", "비류연", Team.Player, Stats(1000, 1, 100), null);
            p.AddStatus(StatusKind.Poison, 3);
            p.RestBetweenFights();
            Assert.AreEqual(0, p.Statuses.Count);
        }
    }
}
