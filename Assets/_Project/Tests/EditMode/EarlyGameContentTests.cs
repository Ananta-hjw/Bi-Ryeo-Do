using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class EarlyGameContentTests
    {
        [Test]
        public void StarterArtExistsAndIsFree()
        {
            var starter = EarlyGameContent.Arts.Single(a => a.Id == EarlyGameContent.StarterArtId);
            Assert.AreEqual(0, starter.LearnCost);
            Assert.AreEqual(0, starter.QiCost);
        }

        [Test]
        public void IdsAreUnique()
        {
            Assert.AreEqual(EarlyGameContent.Arts.Length, EarlyGameContent.Arts.Select(a => a.Id).Distinct().Count());
            Assert.AreEqual(EarlyGameContent.Enemies.Length, EarlyGameContent.Enemies.Select(e => e.Id).Distinct().Count());
        }

        [Test]
        public void StagesReferenceExistingEnemiesAndBossIsBoss()
        {
            var numbers = new HashSet<int>();
            foreach (var s in EarlyGameContent.Stages)
            {
                Assert.IsTrue(numbers.Add(s.Number), "duplicate stage " + s.Number);
                Assert.IsTrue(s.EnemyIds.Length > 0);
                foreach (var id in s.EnemyIds)
                {
                    var e = EarlyGameContent.FindEnemy(id);
                    Assert.IsNotNull(e, id);
                    Assert.IsFalse(e.IsBoss, id + " is a boss used as a normal enemy");
                }
                var boss = EarlyGameContent.FindEnemy(s.BossId);
                Assert.IsNotNull(boss, s.BossId);
                Assert.IsTrue(boss.IsBoss);
            }
        }

        [Test]
        public void BossesAreStrongerThanTheirStageEnemies()
        {
            foreach (var s in EarlyGameContent.Stages)
            {
                var boss = EarlyGameContent.FindEnemy(s.BossId);
                foreach (var id in s.EnemyIds)
                    Assert.Greater(boss.BuildStats().MaxHp, EarlyGameContent.FindEnemy(id).BuildStats().MaxHp, s.Name);
            }
        }

        [Test]
        public void ArtsAreUnlockedWithinStageRange()
        {
            int max = EarlyGameContent.Stages.Max(s => s.Number);
            foreach (var a in EarlyGameContent.Arts) Assert.LessOrEqual(a.UnlockStage, max, a.Id);
        }
    }
}
