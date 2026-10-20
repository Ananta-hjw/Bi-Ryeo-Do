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

        [Test]
        public void JointPowerScalesWithTheProtagonistsBestArt()
        {
            var hyo = EarlyGameContent.FindCompanion("hyo_ryong");
            var strong = new[] { new SkillSpec { Id = "a", Power = 8f }, new SkillSpec { Id = "b", Power = 3f, Hits = 2 }, new SkillSpec { Id = "u", Power = 20f, SpiritCost = 100 } };
            Assert.AreEqual(8f, Bonds.ReferencePower(strong), 0.001f);   // ultimates are ignored
            var early = Bonds.JointSpec(hyo, 1);
            var late = Bonds.JointSpec(hyo, 1, 8f);
            Assert.Greater(late.Power, early.Power);
            Assert.AreEqual(6.4f, late.Power, 0.001f);   // 8 x 0.8
        }

        [Test]
        public void JointTriggersBleedOnlyOnce()
        {
            Combatant hero, friend, foe;
            var b = Setup(out hero, out friend, out foe);
            hero.Spirit = InteractiveBattle.JointSpiritCost;
            foe.AddStatus(StatusKind.Bleed, 3);
            var events = b.PlayerAct(PlayerCommand.Joint(SkillSpec.BasicAttack(), friend, foe));
            Assert.AreEqual(2, events.Count(e => e.Joint && e.Kind == EventKind.Attack));
            var bleed = events.Where(e => e.Kind == EventKind.Status && e.Status == StatusKind.Bleed && e.Damage > 0).ToList();
            Assert.AreEqual(1, bleed.Count);
            Assert.AreEqual(Statuses.DotDamage(foe, StatusKind.Bleed), bleed[0].Damage);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void JointRetargetingSharesOneBleedTrigger(bool firstDiesFromBleed)
        {
            var hero = new Combatant("p", "비류연", Team.Player, Stats(100000, 100, 100), null);
            var friend = new Combatant("f", "효룡", Team.Player, Stats(100000, 100, 60), null);
            var first = new Combatant("e1", "적1", Team.Enemy, Stats(100000, 1, 1), null);
            var second = new Combatant("e2", "적2", Team.Enemy, Stats(100000, 1, 1), null);
            var b = new InteractiveBattle(new[] { hero, friend }, new[] { first, second }, new SeededRandom(1));
            b.Begin();
            second.Col = first.Col;
            second.Row = first.Row;
            first.Hp = firstDiesFromBleed ? 1000 : 1;
            first.AddStatus(StatusKind.Bleed, 3);
            second.AddStatus(StatusKind.Bleed, 3);
            hero.Spirit = InteractiveBattle.JointSpiritCost;
            var events = b.PlayerAct(PlayerCommand.Joint(SkillSpec.BasicAttack(), friend, first));
            var attacks = events.Where(e => e.Joint && e.Kind == EventKind.Attack).ToList();
            Assert.AreEqual(2, attacks.Count);
            Assert.AreEqual(second.Id, attacks[1].TargetId);
            var bleed = events.Where(e => e.Kind == EventKind.Status && e.Status == StatusKind.Bleed && e.Damage > 0).ToList();
            Assert.AreEqual(1, bleed.Count);
            Assert.AreEqual(firstDiesFromBleed ? first.Id : second.Id, bleed[0].TargetId);
        }
    }
}
