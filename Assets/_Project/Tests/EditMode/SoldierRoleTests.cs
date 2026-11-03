using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class SoldierRoleTests
    {
        [Test]
        public void EveryListedSoldierIsARealUnnamedNonBossEnemy()
        {
            foreach (var kv in SoldierRoles.All)
            {
                var e = EarlyGameContent.FindEnemy(kv.Key);
                Assert.IsNotNull(e, kv.Key);
                Assert.IsFalse(e.IsBoss, kv.Key);
                Assert.LessOrEqual(e.HpMul, 10f, kv.Key + " is a survival wall");
            }
            Assert.AreEqual(10, SoldierRoles.All.Count(kv => kv.Value == SoldierRole.Archer));
            Assert.AreEqual(11, SoldierRoles.All.Count(kv => kv.Value == SoldierRole.Shield));
        }

        [Test]
        public void ArchersHitHarderAndShieldsLastLonger()
        {
            var archer = EarlyGameContent.FindEnemy("hogol_archer");
            var plain = archer.Scaled(1f, 1f);
            plain.Id = "plain_probe";
            CombatStats a = archer.BuildStatsBeforeRealm(), p = plain.BuildStatsBeforeRealm();
            Assert.Greater(a.EffAttack, p.EffAttack);
            Assert.Less(a.MaxHp, p.MaxHp);
            Assert.Less(a.EffDefense, p.EffDefense);

            var shield = EarlyGameContent.FindEnemy("sinung_daewon");
            var plain2 = shield.Scaled(1f, 1f);
            plain2.Id = "plain_probe";
            CombatStats s = shield.BuildStatsBeforeRealm(), q = plain2.BuildStatsBeforeRealm();
            Assert.Less(s.EffAttack, q.EffAttack);
            Assert.Greater(s.MaxHp, q.MaxHp);
            Assert.Greater(s.EffDefense, q.EffDefense);
        }

        [Test]
        public void TowerClonesKeepTheirSourceSoldierRole()
        {
            int found = 0;
            for (int floor = 1; floor <= 60; floor++)
                foreach (var e in Tower.BuildEnemies(floor))
                {
                    Assert.IsNotNull(e.RoleOf, e.Id);
                    if (SoldierRoles.Of(e.RoleOf) != SoldierRole.None) found++;
                }
            Assert.Greater(found, 0);
        }
    }
}
