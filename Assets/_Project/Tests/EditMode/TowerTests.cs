using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class TowerTests
    {
        [Test]
        public void FloorsAreDeterministicAndScale()
        {
            var a = Tower.BuildEnemies(7);
            var b = Tower.BuildEnemies(7);
            CollectionAssert.AreEqual(a.Select(e => e.Name), b.Select(e => e.Name));
            Assert.Greater(Tower.FloorLevel(10), Tower.FloorLevel(9));
            Assert.Greater(Tower.RewardDoo(20), Tower.RewardDoo(19));
        }

        [Test]
        public void BossFloorsHaveABossAndOthersDoNot()
        {
            for (int f = 1; f <= 40; f++)
            {
                var es = Tower.BuildEnemies(f);
                Assert.IsTrue(es.Length >= 1 && es.Length <= 3, "floor " + f);
                Assert.AreEqual(Tower.IsBossFloor(f), es.Any(e => e.IsBoss), "floor " + f);
                foreach (var e in es) Assert.IsNotNull(EarlyGameContent.FindEnemy(e.Id), e.Id);
            }
        }

        [Test]
        public void ToStageIsAOneFightTowerStage()
        {
            var save = new SaveData { ClearedStage = 8 };
            var st = Tower.ToStage(save, 5);
            Assert.IsTrue(st.IsTower);
            Assert.AreEqual(5, st.TowerFloor);
            Assert.AreEqual(1, st.Encounters.Length);
            Assert.AreEqual(Tower.RecommendedCurve(5), st.RecommendedLevel);
        }

        [Test]
        public void CompleteRewardsOnlyTheFirstClear()
        {
            var save = new SaveData { ClearedStage = 8 };
            Tower.Complete(save, 1);
            Assert.AreEqual(1, save.TowerBest);
            Assert.AreEqual(2, Tower.NextFloor(save));
            Tower.Complete(save, 10);
            int potions = Inventory.Count(save, "ilgeup_yeongdan");
            Assert.AreEqual(1, potions);
            Assert.AreEqual("", Tower.Complete(save, 10));
            Assert.AreEqual(1, Inventory.Count(save, "ilgeup_yeongdan"));
            Tower.Complete(save, 3);
            Assert.AreEqual(10, save.TowerBest);
        }

        [Test]
        public void UnlocksAfterStageEight()
        {
            Assert.IsFalse(Tower.Unlocked(new SaveData { ClearedStage = 7 }));
            Assert.IsTrue(Tower.Unlocked(new SaveData { ClearedStage = 8 }));
        }
    }
}
