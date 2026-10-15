using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class GachaTests
    {
        [Test]
        public void GradeOddsAreExactlyTheRequestedOnes()
        {
            CollectionAssert.AreEqual(new[] { 0.60, 0.20, 0.10, 0.06, 0.03, 0.01 }, Grades.Rates);
            Assert.AreEqual(1.0, Grades.Rates.Sum(), 1e-9);
            CollectionAssert.AreEqual(new[] { "일반", "고급", "희귀", "영웅", "전설", "신화" }, Grades.Names);
        }

        [Test]
        public void EveryGradeHasPlentyOfItems()
        {
            for (int g = 0; g < Grades.Names.Length; g++)
                Assert.GreaterOrEqual(Gacha.Pool((ItemGrade)g).Count, g >= 5 ? 5 : 8, Grades.Names[g]);
            Assert.GreaterOrEqual(EarlyGameContent.Items.Length, 70);
        }

        [Test]
        public void ItemIdsAreUnique()
        {
            var ids = EarlyGameContent.Items.Select(i => i.Id).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), string.Join(",", ids.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key)));
        }

        [Test]
        public void RolledGradesFollowTheOddsOverManyPulls()
        {
            var rng = new SeededRandom(7);
            var counts = new int[6];
            const int n = 200000;
            for (int i = 0; i < n; i++) counts[(int)Gacha.RollGrade(rng)]++;
            for (int g = 0; g < 6; g++) Assert.AreEqual(Grades.Rates[g], counts[g] / (double)n, 0.004, Grades.Names[g]);
        }

        [Test]
        public void StrongerGradesGiveStrongerGear()
        {
            double Power(ItemData i) { return i.AtkPct + i.DefPct + i.HpPct + i.QiPct + i.CritAdd * 2 + i.SpeedAdd * 0.01; }
            for (int g = 0; g < 5; g++)
            {
                double lo = Gacha.Pool((ItemGrade)g).Where(i => i.IsGear).Average(Power);
                double hi = Gacha.Pool((ItemGrade)(g + 1)).Where(i => i.IsGear).Average(Power);
                Assert.Greater(hi, lo, Grades.Names[g]);
            }
        }

        [Test]
        public void PullChargesAndGrantsItems()
        {
            var save = new SaveData { ClearedStage = 4, Doo = 100000 };
            long cost = Gacha.PullCost(save);
            Assert.AreEqual(200 + 150 * 4, cost);
            var r = Gacha.Pull(save, 1, new SeededRandom(1));
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual(100000 - cost + r[0].Doo, save.Doo);
            Assert.GreaterOrEqual(Inventory.Count(save, r[0].Item.Id), 1);
        }

        [Test]
        public void TenPullIsDiscounted()
        {
            var save = new SaveData { ClearedStage = 10, Doo = 1000000 };
            Assert.Less(Gacha.TenPullCost(save), Gacha.PullCost(save) * 10);
            long before = save.Doo;
            var r = Gacha.Pull(save, 10, new SeededRandom(3));
            Assert.AreEqual(10, r.Count);
            Assert.AreEqual(before - Gacha.TenPullCost(save) + r.Sum(x => x.Doo), save.Doo);
        }

        [Test]
        public void CannotPullWithoutEnoughDoo()
        {
            var save = new SaveData { ClearedStage = 3, Doo = 10 };
            Assert.IsNull(Gacha.Pull(save, 1, new SeededRandom(1)));
            Assert.AreEqual(10, save.Doo);
            Assert.AreEqual(0, save.Items.Count);
        }

        [Test]
        public void DuplicateGearTurnsIntoDoo()
        {
            var save = new SaveData { Doo = 1000000 };
            var rng = new SeededRandom(11);
            int dupes = 0;
            for (int i = 0; i < 400; i++)
            {
                var r = Gacha.Pull(save, 1, rng)[0];
                if (r.Duplicate) { dupes++; Assert.Greater(r.Doo, 0); Assert.IsTrue(r.Item.IsGear); }
                if (r.Item.IsGear) Assert.AreEqual(1, Inventory.Count(save, r.Item.Id));
            }
            Assert.Greater(dupes, 0);
        }

        [Test]
        public void GearOnlyComesFromTheStartKitAndGacha()
        {
            foreach (var d in EarlyGameContent.Drops) Assert.IsFalse(EarlyGameContent.FindItem(d.ItemId).IsGear, d.EnemyId + " drops gear");
            foreach (var r in EarlyGameContent.StageRewards)
                foreach (var s in r.Value) Assert.IsFalse(EarlyGameContent.FindItem(s.Id).IsGear, "stage " + r.Key + " rewards gear");
        }

        [Test]
        public void ArmorHasItsOwnSlot()
        {
            var save = new SaveData();
            Inventory.Add(save, "cloth_robe", 1);
            Inventory.Add(save, "leather_vest", 1);
            string error;
            Assert.IsTrue(Inventory.Equip(save, "cloth_robe", out error));
            Assert.IsTrue(Inventory.Equip(save, "leather_vest", out error));   // replaces the first armor
            Assert.IsFalse(Inventory.IsEquipped(save, "cloth_robe"));
            Assert.IsTrue(Inventory.IsEquipped(save, "leather_vest"));
        }
    }
}
