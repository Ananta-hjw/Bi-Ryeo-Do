using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class BondBonusTests
    {
        [Test]
        public void BondRaisesCompanionStats()
        {
            var c = EarlyGameContent.Companions[0];
            var noneUnit = c.CreateCombatant(30);
            var friendlyUnit = c.CreateCombatant(30, 1);
            var none = noneUnit.Stats;
            var trust = c.CreateCombatant(30, 2).Stats;
            var full = c.CreateCombatant(30, 3).Stats;
            Assert.Greater(trust.MaxHp, none.MaxHp);
            Assert.Greater(full.Attack, trust.Attack);
            Assert.AreEqual(none.CritRate, full.CritRate - Bonds.CritBonus(3), 0.0001f);
            Assert.AreEqual(none.CritRate, c.CreateCombatant(30, 1).Stats.CritRate, 0.0001f);   // 친분 has no crit bonus yet
            double realmRatio = (1f + Realm.Bonus(friendlyUnit.RealmStep)) / (1f + Realm.Bonus(noneUnit.RealmStep));
            Assert.AreEqual((int)System.Math.Round(none.MaxHp * Bonds.StatMultiplier(1) * realmRatio), friendlyUnit.Stats.MaxHp, 2);
        }

        [Test]
        public void BonusTextDescribesTheLevel()
        {
            StringAssert.Contains("5%", Bonds.BonusText(1));
            StringAssert.Contains("치명타", Bonds.BonusText(2));
            StringAssert.Contains("구원", Bonds.BonusText(3));
            Assert.IsFalse(Bonds.BonusText(2).Contains("구원"));
        }

        private static InteractiveBattle Setup(bool rescuer, bool rescuerAlive, out Combatant hero, out Combatant buddy)
        {
            var heroStats = new CombatStats { MaxHp = 100, MaxQi = 50, Attack = 10, Defense = 0, Speed = 10, CritRate = 0f, CritDamage = 1.5f };
            var buddyStats = new CombatStats { MaxHp = 9999999, MaxQi = 50, Attack = 10, Defense = 0, Speed = 10, CritRate = 0f, CritDamage = 1.5f };
            var foeStats = new CombatStats { MaxHp = 99999, MaxQi = 0, Attack = 500, Defense = 0, Speed = 100, CritRate = 0f, CritDamage = 1.5f };
            hero = new Combatant("player", "비류연", Team.Player, heroStats, null);
            buddy = new Combatant("hyo_ryong", "효룡", Team.Player, buddyStats, null);
            if (rescuer) hero.RescuerId = "hyo_ryong";
            if (!rescuerAlive) buddy.Hp = 0;
            var foe = new Combatant("foe", "적", Team.Enemy, foeStats, null) { Moves = new System.Collections.Generic.List<EnemyMove> { EnemyMove.Attack("강타", 5f) } };
            return new InteractiveBattle(new[] { hero, buddy }, new[] { foe }, new SeededRandom(3));
        }

        [Test]
        public void BondedCompanionSavesTheProtagonistOnlyOncePerFight()
        {
            Combatant hero, buddy;
            var battle = Setup(true, true, out hero, out buddy);
            var events = battle.Begin();
            // The foe is much faster and aims at random members; the hero is hit by killing blows until the rescue is spent.
            int rescues = 0, deaths = 0;
            for (int i = 0; i < 200 && !battle.IsOver; i++)
            {
                var ev = battle.PlayerAct(PlayerCommand.Guard());
                rescues += ev.Count(e => e.Rescued);
                if (!hero.IsAlive) { deaths++; break; }
            }
            rescues += events.Count(e => e.Rescued);
            Assert.AreEqual(1, rescues);              // saved once...
            Assert.IsTrue(hero.RescueUsed);
            Assert.IsFalse(hero.IsAlive);             // ...and the next killing blow lands
        }

        [Test]
        public void NoRescueWithoutALivingBondedCompanion()
        {
            Combatant hero, buddy;
            var battle = Setup(true, false, out hero, out buddy);
            battle.Begin();
            for (int i = 0; i < 40 && hero.IsAlive && !battle.IsOver; i++) battle.PlayerAct(PlayerCommand.Guard());
            Assert.IsFalse(hero.IsAlive);
            Assert.IsFalse(hero.RescueUsed);
            var noBond = Setup(false, true, out hero, out buddy);
            noBond.Begin();
            for (int i = 0; i < 40 && hero.IsAlive && !noBond.IsOver; i++) noBond.PlayerAct(PlayerCommand.Guard());
            Assert.IsFalse(hero.RescueUsed);
        }
    }
}
