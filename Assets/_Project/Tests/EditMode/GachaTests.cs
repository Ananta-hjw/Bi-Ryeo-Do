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
            CollectionAssert.AreEqual(new[] { 0.61, 0.20, 0.10, 0.055, 0.028, 0.007 }, Grades.Rates);
            Assert.AreEqual(1.0, Grades.Rates.Sum(), 1e-9);
            CollectionAssert.AreEqual(new[] { "일반", "고급", "희귀", "영웅", "전설", "신화", "초월" }, Grades.Names);
        }

        [Test]
        public void EveryGradeHasPlentyOfItems()
        {
            for (int g = 0; g < Grades.Rates.Length; g++)
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
            long cost = Gacha.CostFor(1);
            Assert.AreEqual(500, cost);
            var r = Gacha.Pull(save, 1, new SeededRandom(1));
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual(100000 - cost, save.Doo);
            Assert.GreaterOrEqual(Inventory.Count(save, r[0].Item.Id), 1);
        }

        [Test]
        public void PricesAreFixedAndBulkIsCheaper()
        {
            Assert.AreEqual(500, Gacha.CostFor(1));
            Assert.AreEqual(4500, Gacha.CostFor(10));
            Assert.AreEqual(20000, Gacha.CostFor(50));
            Assert.Less(Gacha.CostFor(10), Gacha.CostFor(1) * 10);
            Assert.Less(Gacha.CostFor(50), Gacha.CostFor(10) * 5);
            // progress does not change the price
            Assert.AreEqual(Gacha.CostFor(10), Gacha.CostFor(10));
        }

        [Test]
        public void BulkPullsChargeTheFixedPriceAndReturnEveryResult()
        {
            foreach (int n in new[] { 10, 50 })
            {
                var save = new SaveData { ClearedStage = 30, Doo = 1000000 };
                long before = save.Doo;
                var r = Gacha.Pull(save, n, new SeededRandom(3));
                Assert.AreEqual(n, r.Count);
                Assert.AreEqual(before - Gacha.CostFor(n), save.Doo);
            }
        }

        [Test]
        public void CannotPullWithoutEnoughDoo()
        {
            var save = new SaveData { ClearedStage = 3, Doo = 499 };
            Assert.IsNull(Gacha.Pull(save, 1, new SeededRandom(1)));
            Assert.AreEqual(499, save.Doo);
            Assert.AreEqual(0, save.Items.Count);
        }

        [Test]
        public void DuplicateGearStaysAsASpareCopy()
        {
            var save = new SaveData { Doo = 1000000 };
            var rng = new SeededRandom(11);
            int dupes = 0;
            var pulled = new System.Collections.Generic.Dictionary<string, int>();
            for (int i = 0; i < 400; i++)
            {
                var r = Gacha.Pull(save, 1, rng)[0];
                if (r.Duplicate) { dupes++; Assert.IsTrue(r.Item.IsGear); }
                if (r.Item.IsGear) { pulled[r.Item.Id] = (pulled.ContainsKey(r.Item.Id) ? pulled[r.Item.Id] : 0) + 1; Assert.AreEqual(pulled[r.Item.Id], Inventory.Count(save, r.Item.Id)); }
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
    
        [Test]
        public void IncomeBoostRaisesBy30ThenBy20PercentAndRoundsToTens()
        {
            Assert.AreEqual(3360, Income.Boost(2150));   // 2150 -> 2800 -> 3360
            Assert.AreEqual(3340, Income.Boost(2142));   // 2142 -> 2780 -> 3336
            Assert.AreEqual(170, Income.Boost(105));
            Assert.AreEqual(0, Income.Boost(0));
        }
    }
}
