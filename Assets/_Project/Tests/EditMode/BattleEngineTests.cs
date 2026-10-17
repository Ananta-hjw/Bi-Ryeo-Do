using System.Collections.Generic;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class BattleEngineTests
    {
        private static CombatStats Stats(int hp, int atk, int def, int speed, float crit = 0f)
        {
            return new CombatStats { MaxHp = hp, MaxQi = 50, Attack = atk, Defense = def, Speed = speed, CritRate = crit, CritDamage = 1.5f };
        }

        private static BattleResult Fight(CombatStats player, CombatStats enemy, int seed = 1)
        {
            var p = new Combatant("p", "협객", Team.Player, player, null);
            var e = new Combatant("e", "괴물", Team.Enemy, enemy, null);
            return new BattleEngine(new[] { p }, new[] { e }, new SeededRandom(seed)).Run();
        }

        [Test]
        public void StrongerPlayerWins()
        {
            var r = Fight(Stats(200, 30, 5, 100), Stats(50, 5, 0, 100));
            Assert.IsTrue(r.PlayerWon);
        }

        [Test]
        public void WeakerPlayerLoses()
        {
            var r = Fight(Stats(30, 3, 0, 100), Stats(500, 40, 0, 100));
            Assert.AreEqual(Team.Enemy, r.Winner);
        }

        [Test]
        public void FasterUnitActsMoreOften()
        {
            var r = Fight(Stats(100000, 1, 0, 300), Stats(100000, 1, 0, 100));
            int p = 0, e = 0;
            foreach (var ev in r.Events) { if (ev.ActorId == "p") p++; else e++; }
            Assert.Greater(p, e * 2);
        }

        [Test]
        public void SameSeedIsDeterministic()
        {
            var a = Fight(Stats(300, 20, 5, 100, 0.5f), Stats(300, 20, 5, 100, 0.5f), 42);
            var b = Fight(Stats(300, 20, 5, 100, 0.5f), Stats(300, 20, 5, 100, 0.5f), 42);
            Assert.AreEqual(a.Events.Count, b.Events.Count);
            for (int i = 0; i < a.Events.Count; i++)
                Assert.AreEqual(a.Events[i].Damage, b.Events[i].Damage);
        }

        [Test]
        public void SkillRespectsCooldownAndQi()
        {
            var big = new SkillSpec { Id = "big", Name = "강타", Power = 3f, QiCost = 10, Cooldown = 2 };
            var p = new Combatant("p", "협객", Team.Player, Stats(100000, 10, 0, 100), new[] { big });
            var e = new Combatant("e", "괴물", Team.Enemy, Stats(100000, 1, 0, 1), null);
            var engine = new BattleEngine(new[] { p }, new[] { e }, new SeededRandom(1));
            var used = new List<string>();
            for (int i = 0; i < 6; i++) used.Add(engine.Step().SkillId);
            CollectionAssert.AreEqual(new[] { "big", "basic", "big", "basic", "big", "basic" }, used);
        }

        [Test]
        public void DamageIsAtLeastOne()
        {
            Assert.AreEqual(1, BattleEngine.CalcDamage(Stats(1, 1, 0, 1), Stats(1, 1, 10000, 1), SkillSpec.BasicAttack(), false));
        }

        [Test]
        public void LevelUpFromExp()
        {
            var save = new SaveData();
            int gained = Progression.AddExp(save, Progression.ExpToNext(1) + Progression.ExpToNext(2));
            Assert.AreEqual(2, gained);
            Assert.AreEqual(3, save.Level);
        }

        [Test]
        public void DefenseOf1380TakesFortyTwoPercentAndFinerStepsStillCount()
        {
            var attacker = new CombatStats { MaxHp = 100, MaxQi = 0, Attack = 10000, Defense = 0, Speed = 100, CritRate = 0f, CritDamage = 1.5f };
            var skill = new SkillSpec { Id = "t", Name = "t", Power = 1f };
            int Taken(int def) { return BattleEngine.CalcDamage(attacker, new CombatStats { MaxHp = 100, Defense = def, Speed = 100 }, skill, false); }
            Assert.AreEqual(10000, Taken(0));
            Assert.AreEqual(4202, Taken(1380), 1);   // x1000/2380
            Assert.AreEqual(5000, Taken(1000));
            Assert.Less(Taken(1381), Taken(1380));   // one point of defense is still a step
            Assert.Less(Taken(1385), Taken(1381));
        }
    }
}
