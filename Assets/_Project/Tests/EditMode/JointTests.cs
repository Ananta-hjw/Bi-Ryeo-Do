using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class JointTests
    {
        private static CombatStats Stats(int hp, int atk, int speed)
        {
            return new CombatStats { MaxHp = hp, MaxQi = 100, Attack = atk, Defense = 0, Speed = speed, CritRate = 0f, CritDamage = 1.5f };
        }

        private static InteractiveBattle Setup(out Combatant hero, out Combatant friend, out Combatant foe, int foeHp = 100000)
        {
            hero = new Combatant("p", "비류연", Team.Player, Stats(100000, 100, 100), null);
            friend = new Combatant("f", "효룡", Team.Player, Stats(100000, 100, 60), null);
            foe = new Combatant("e", "적", Team.Enemy, Stats(foeHp, 1, 1), null);
            foe.Moves = new List<EnemyMove> { EnemyMove.Plain };
            var b = new InteractiveBattle(new[] { hero, friend }, new[] { foe }, new SeededRandom(1));
            b.Begin();
            return b;
        }

        [Test]
        public void BondLevelsRiseWithPoints()
        {
            var save = new SaveData();
            Assert.AreEqual(0, Bonds.Level(save, "hyo_ryong"));
            Assert.AreEqual(-1, Bonds.Add(save, "hyo_ryong", 2));
            Assert.AreEqual(1, Bonds.Add(save, "hyo_ryong", 1));
            Assert.AreEqual(1, Bonds.Level(save, "hyo_ryong"));
            Assert.IsNull(Bonds.JointSpec(EarlyGameContent.FindCompanion("hyo_ryong"), 0));
            Assert.Greater(Bonds.JointSpec(EarlyGameContent.FindCompanion("hyo_ryong"), 3).Power, Bonds.JointSpec(EarlyGameContent.FindCompanion("hyo_ryong"), 1).Power);
        }

        [Test]
        public void EveryCompanionHasAJointAttack()
        {
            foreach (var c in EarlyGameContent.Companions) Assert.IsNotNull(Bonds.JointSpec(c, 1), c.Id);
        }

        [Test]
        public void JointNeedsSpiritAndLivingPartner()
        {
            Combatant hero, friend, foe;
            var b = Setup(out hero, out friend, out foe);
            Assert.AreSame(hero, b.Current);
            Assert.IsFalse(b.CanJoint(friend));   // no 기세 yet
            hero.Spirit = InteractiveBattle.JointSpiritCost;
            Assert.IsTrue(b.CanJoint(friend));
            Assert.IsFalse(b.CanJoint(hero));
            friend.Hp = 0;
            Assert.IsFalse(b.CanJoint(friend));
        }

        [Test]
        public void JointHitsTwiceCostsSpiritAndDelaysThePartner()
        {
            Combatant hero, friend, foe;
            var b = Setup(out hero, out friend, out foe);
            hero.Spirit = 80;
            int partnerGauge = friend.Gauge;
            var spec = Bonds.JointSpec(EarlyGameContent.FindCompanion("jang_hong"), 1);
            var events = b.PlayerAct(PlayerCommand.Joint(spec, friend, foe));
            var joint = events.Where(e => e.Joint && e.Kind == EventKind.Attack).ToList();
            Assert.AreEqual(2, joint.Count);
            Assert.AreEqual("p", joint[0].ActorId);
            Assert.AreEqual("f", joint[1].ActorId);
            Assert.AreEqual(80 - InteractiveBattle.JointSpiritCost, hero.Spirit);
            Assert.IsTrue(joint[1].Stunned || foe.IsBoss);   // 점혈 effect rides on the partner's strike
            Assert.Less(friend.Gauge, partnerGauge + 1000);
        }

        [Test]
        public void JointStopsAfterTheLastEnemyFalls()
        {
            Combatant hero, friend, foe;
            var b = Setup(out hero, out friend, out foe, 10);
            hero.Spirit = 100;
            var spec = Bonds.JointSpec(EarlyGameContent.FindCompanion("hyo_ryong"), 1);
            var events = b.PlayerAct(PlayerCommand.Joint(spec, friend, foe));
            Assert.AreEqual(1, events.Count(e => e.Joint && e.Kind == EventKind.Attack));
            Assert.IsTrue(b.PlayerWon);
        }
    }
}
