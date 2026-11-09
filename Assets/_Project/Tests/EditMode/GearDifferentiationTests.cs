using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class GearDifferentiationTests
    {
        [Test]
        public void EveryEquipmentHasDistinctBaseEffectsAndDisplayedEffects()
        {
            var gear = EarlyGameContent.Items.Where(item => item.IsGear).ToArray();
            Assert.AreEqual(391, gear.Length);
            var duplicates = gear.GroupBy(item => (item.HpPct, item.QiPct, item.AtkPct, item.DefPct,
                item.SpeedAdd, item.CritAdd, item.CritDmgAdd)).Where(group => group.Count() > 1);
            Assert.IsEmpty(duplicates.Select(group => string.Join(", ", group.Select(item => item.Id))));
            Assert.AreEqual(gear.Length, gear.Select(item => StatusSheet.EffectText(item)).Distinct().Count());
        }

        [TestCase("gen_wea_nor01", 3.600000d)]
        [TestCase("gen_wea_nor06", 4.033333d)]
        [TestCase("gen_wea_nor02", 4.700000d)]
        [TestCase("gen_wea_nor07", 5.133333d)]
        [TestCase("gen_wea_fin01", 6.400000d)]
        [TestCase("gen_wea_fin06", 6.733334d)]
        [TestCase("gen_wea_rar01", 9.900000d)]
        [TestCase("gen_wea_rar06", 10.333334d)]
        [TestCase("gen_wea_rar03", 11.100000d)]
        [TestCase("gen_pan_nor02", 4.033333d)]
        [TestCase("gen_pan_nor06", 3.700000d)]
        [TestCase("gen_arm_nor01", 3.700000d)]
        [TestCase("gen_arm_nor06", 4.033333d)]
        [TestCase("jade_ring", 3.133333d)]
        [TestCase("gen_pan_nor01", 3.133333d)]
        [TestCase("gen_pan_nor05", 2.800000d)]
        [TestCase("gen_pan_nor09", 2.800000d)]
        [TestCase("gen_boo_nor01", 4.000000d)]
        [TestCase("gen_boo_nor04", 4.333333d)]
        [TestCase("gen_boo_nor07", 3.800000d)]
        [TestCase("gen_boo_nor10", 4.000000d)]
        [TestCase("gen_acc_fin04", 12.500000d)]   // 등급 편차 재조정 (2026-10 말)
        [TestCase("gen_wea_nor08", 3.900000d)]
        [TestCase("gen_wea_nor09", 4.700000d)]
        [TestCase("gen_wea_nor10", 3.200000d)]
        [TestCase("gen_arm_nor07", 3.633333d)]
        [TestCase("gen_arm_nor08", 5.133333d)]
        [TestCase("gen_arm_nor09", 5.700000d)]
        [TestCase("gen_arm_nor10", 4.133333d)]
        [TestCase("gen_acc_nor09", 4.133333d)]
        [TestCase("gen_pan_nor03", 3.800000d)]
        [TestCase("gen_pan_nor07", 3.800000d)]
        [TestCase("gen_pan_nor08", 3.333333d)]
        [TestCase("gen_boo_nor05", 4.800000d)]
        [TestCase("gen_boo_nor08", 4.200000d)]
        [TestCase("gen_boo_nor06", 5.133333d)]
        [TestCase("gen_boo_nor09", 4.800000d)]
        [TestCase("gen_acc_nor07", 4.133333d)]
        [TestCase("gen_acc_nor08", 5.800000d)]
        [TestCase("gen_acc_nor10", 4.333333d)]
        [TestCase("gen_wea_fin07", 9.600000d)]
        [TestCase("gen_wea_fin08", 7.000000d)]
        [TestCase("gen_arm_fin06", 6.900000d)]
        [TestCase("gen_arm_fin07", 7.233333d)]
        [TestCase("gen_arm_fin08", 7.800000d)]
        [TestCase("gen_pan_fin05", 6.833333d)]
        [TestCase("gen_pan_fin06", 6.933334d)]
        [TestCase("gen_pan_fin07", 7.033333d)]
        [TestCase("gen_pan_fin08", 8.333333d)]   // 등급 편차 재조정 (2026-10 말)
        [TestCase("gen_boo_fin04", 6.000000d)]
        [TestCase("gen_boo_fin07", 6.633333d)]
        [TestCase("gen_boo_fin05", 7.000000d)]
        [TestCase("gen_boo_fin06", 5.300000d)]
        [TestCase("gen_acc_fin07", 10.533333d)]
        [TestCase("gen_acc_fin08", 9.800000d)]
        [TestCase("gen_wea_rar07", 13.500000d)]
        [TestCase("gen_arm_rar06", 10.900000d)]
        [TestCase("gen_boo_rar04", 11.300000d)]
        [TestCase("gen_boo_rar07", 11.500000d)]   // 등급 편차 재조정 (2026-10 말)
        [TestCase("gen_boo_rar05", 13.800000d)]
        [TestCase("gen_boo_rar06", 13.900000d)]
        [TestCase("gen_arm_epi01", 21.100000d)]   // 18 before the 영웅 x1.3 grade scale
        // budgets after the 2026-11 장비 효과 재조정 (Docs/GEAR_EFFECT_REBALANCE.md)
        public void RedistributionPreservesThePreviousWeightedBudget(string id, double expected)
        {
            var item = EarlyGameContent.FindItem(id);
            // A tuning guard, not a simulation of combat effectiveness.
            // 내공 was halved and later cut to 30% (rounded to 0.001); the guard keeps the earlier budget, so it allows that rounding
            double budget = (item.HpPct + item.QiPct * 2d / .3d + item.AtkPct + item.DefPct) * 100d
                + item.SpeedAdd + item.CritAdd / .6d * 200d + item.CritDmgAdd * 25d;   // 2026-11: every base 치명타 was cut to x0.6 after these budgets
            Assert.AreEqual(expected, budget, item.QiPct > 0f ? .34d : .0001d, id);
        }
    }
}
