using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class BattleStatsTests
    {
        private static CombatStats Stats(int hp, int atk, int speed) { return new CombatStats { MaxHp = hp, MaxQi = 100, Attack = atk, Defense = 0, Speed = speed, CritRate = 0f, CritDamage = 1.5f }; }

        private static Combatant Enemy(string id, CombatStats s, params EnemyMove[] moves)
        {
            var c = new Combatant(id, id, Team.Enemy, s, null);
            c.Moves = new List<EnemyMove>(moves.Length > 0 ? moves : new[] { EnemyMove.Plain });
            return c;
        }

        [Test]
        public void DamageDealtAndTakenMatchTheEventsPerSkill()
        {
            var art = new SkillSpec { Id = "art", Name = "분뢰수", Power = 1f, Hits = 3, Tuned = true };
            var p = new Combatant("p", "비류연", Team.Player, Stats(100000, 300, 100), new[] { art });
            var e = Enemy("e", Stats(100000, 100, 100), EnemyMove.Attack("주먹", 1f));
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(3));
            var stats = new BattleStats(new[] { p }, new[] { e });
            var all = new List<BattleEvent>();
            var evs = b.Begin(); stats.Add(evs); all.AddRange(evs);
            var cmd = PlayerCommand.UseSkill(art, e);
            evs = b.PlayerAct(cmd); stats.Add(evs, cmd); all.AddRange(evs);
            cmd = PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e);
            evs = b.PlayerAct(cmd); stats.Add(evs, cmd); all.AddRange(evs);

            var hero = stats.Get("p");
            var foe = stats.Get("e");
            long heroDealt = all.Where(x => x.Kind == EventKind.Attack && x.ActorId == "p").Sum(x => (long)x.Damage);
            long foeDealt = all.Where(x => x.Kind == EventKind.Attack && x.ActorId == "e").Sum(x => (long)x.Damage);
            Assert.AreEqual(heroDealt, hero.Dealt);
            Assert.AreEqual(heroDealt, foe.Taken);
            Assert.AreEqual(foeDealt, foe.Dealt);
            Assert.AreEqual(foeDealt, hero.Taken);
            Assert.AreEqual(100000 - e.Hp, foe.Taken, "nothing overkilled: the total is the HP lost");

            var artLine = hero.DealtBy.First(l => l.Key == "art");
            Assert.AreEqual("분뢰수", artLine.Name);
            Assert.AreEqual(1, artLine.Uses, "three hits are one use");
            Assert.AreEqual(3, artLine.Hits);
            Assert.AreEqual("기본 공격", hero.DealtBy.First(l => l.Key == "basic").Name);
            Assert.AreEqual(hero.Dealt, hero.DealtBy.Sum(l => l.Amount));
            Assert.AreEqual(hero.Taken, hero.TakenFrom.Sum(l => l.Amount));
            StringAssert.Contains("e · 주먹", hero.TakenFrom[0].Name);
            Assert.AreSame(hero, stats.Mvp);
        }

        [Test]
        public void AreaArtCountsOneUseAcrossTargets()
        {
            var area = new SkillSpec { Id = "area", Name = "선풍뇌절", Power = 0.1f, Hits = 2, AllEnemies = true, Range = 3, Tuned = true };
            var p = new Combatant("p", "비류연", Team.Player, Stats(100000, 100, 100), new[] { area });
            var e1 = Enemy("a", Stats(100000, 1, 1));
            var e2 = Enemy("b", Stats(100000, 1, 1));
            var b = new InteractiveBattle(p, new[] { e1, e2 }, new SeededRandom(1));
            var stats = new BattleStats(new[] { p }, new[] { e1, e2 });
            stats.Add(b.Begin());
            var cmd = PlayerCommand.UseSkill(area, null);
            stats.Add(b.PlayerAct(cmd), cmd);
            var line = stats.Get("p").DealtBy.First(l => l.Key == "area");
            Assert.AreEqual(1, line.Uses);
            Assert.AreEqual(4, line.Hits);
        }

        [Test]
        public void DamageOverTimeIsCreditedToWhoeverInflictedIt()
        {
            var poison = new SkillSpec { Id = "p", Name = "독장", Power = 0.01f, Inflict = StatusKind.Poison, InflictChance = 1f, Tuned = true };
            var p = new Combatant("p", "비류연", Team.Player, Stats(1000, 1, 100), new[] { poison });
            var e = Enemy("e", Stats(10000, 1, 100), EnemyMove.Attack("x", 0.01f));
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1));
            var stats = new BattleStats(new[] { p }, new[] { e });
            stats.Add(b.Begin());
            var cmd = PlayerCommand.UseSkill(poison, e);
            var evs = b.PlayerAct(cmd);
            stats.Add(evs, cmd);
            int tick = evs.First(x => x.Kind == EventKind.Status && x.Damage > 0).Damage;
            var hero = stats.Get("p");
            Assert.AreEqual(1, hero.Inflicted);
            Assert.AreEqual(1, stats.Get("e").Afflicted);
            Assert.AreEqual(tick, hero.StatusDamage);
            var line = hero.DealtBy.First(l => l.IsStatus);
            StringAssert.Contains("중독", line.Name);
            Assert.AreEqual(tick, line.Amount);
            Assert.AreEqual(stats.Get("e").Taken, hero.Dealt);
        }

        [Test]
        public void GuardsDodgesAndHealsAreCounted()
        {
            var p = new Combatant("p", "비류연", Team.Player, Stats(100000, 1, 100), null);
            var e = Enemy("e", Stats(100000, 100, 100), EnemyMove.Wind("w"), EnemyMove.Attack("h", 1f));
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1));
            var stats = new BattleStats(new[] { p }, new[] { e });
            stats.Add(b.Begin());
            for (int i = 0; i < 3 && stats.Get("p").Guarded == 0; i++) stats.Add(b.PlayerAct(PlayerCommand.Guard()));   // the wind-up, then the guarded heavy blow
            p.Hp = 50000;
            stats.Add(b.PlayerAct(PlayerCommand.Meditate()));
            var hero = stats.Get("p");
            Assert.GreaterOrEqual(hero.Guarded, 1);
            Assert.AreEqual(1, hero.Reads, "a guarded charged blow is a 간파");
            Assert.Greater(hero.HealDone, 0);
            Assert.AreEqual(hero.HealDone, hero.HealTaken);
            Assert.AreEqual("운기조식", hero.HealsBy[0].Name);
        }

        [Test]
        public void KillsAreCreditedToTheFinishingBlow()
        {
            var p = new Combatant("p", "비류연", Team.Player, Stats(1000, 10000, 100), null);
            var e = Enemy("e", Stats(10, 1, 1));
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1));
            var stats = new BattleStats(new[] { p }, new[] { e });
            stats.Add(b.Begin());
            stats.Add(b.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e)));
            Assert.IsTrue(b.PlayerWon);
            Assert.AreEqual(1, stats.Get("p").Kills);
            Assert.Greater(stats.Get("e").Taken, 10, "the shown number counts in full, overkill included");
        }
    }
}
