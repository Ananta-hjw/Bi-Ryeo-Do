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

        [TestCase("gen_wea_nor01", 4.000000d)]
        [TestCase("gen_wea_nor06", 4.000000d)]
        [TestCase("gen_wea_nor02", 5.000000d)]
        [TestCase("gen_wea_nor07", 5.000000d)]
        [TestCase("gen_wea_fin01", 7.000000d)]
        [TestCase("gen_wea_fin06", 7.000000d)]
        [TestCase("gen_wea_rar01", 11.000000d)]
        [TestCase("gen_wea_rar06", 11.000000d)]
        [TestCase("gen_wea_rar03", 12.000000d)]
        [TestCase("gen_pan_nor02", 4.000000d)]
        [TestCase("gen_pan_nor06", 4.000000d)]
        [TestCase("gen_arm_nor01", 4.000000d)]
        [TestCase("gen_arm_nor06", 4.000000d)]
        [TestCase("jade_ring", 3.000000d)]
        [TestCase("gen_pan_nor01", 3.000000d)]
        [TestCase("gen_pan_nor05", 3.000000d)]
        [TestCase("gen_pan_nor09", 3.000000d)]
        [TestCase("gen_boo_nor01", 4.000000d)]
        [TestCase("gen_boo_nor04", 4.000000d)]
        [TestCase("gen_boo_nor07", 4.000000d)]
        [TestCase("gen_boo_nor10", 4.000000d)]
        [TestCase("gen_acc_fin04", 7.000000d)]
        [TestCase("gen_wea_nor08", 4.000000d)]
        [TestCase("gen_wea_nor09", 5.000000d)]
        [TestCase("gen_wea_nor10", 5.000000d)]
        [TestCase("gen_arm_nor07", 5.000000d)]
        [TestCase("gen_arm_nor08", 5.000000d)]
        [TestCase("gen_arm_nor09", 6.000000d)]
        [TestCase("gen_arm_nor10", 4.000000d)]
        [TestCase("gen_acc_nor09", 4.000000d)]
        [TestCase("gen_pan_nor03", 4.000000d)]
        [TestCase("gen_pan_nor07", 4.000000d)]
        [TestCase("gen_pan_nor08", 3.000000d)]
        [TestCase("gen_boo_nor05", 5.000000d)]
        [TestCase("gen_boo_nor08", 5.000000d)]
        [TestCase("gen_boo_nor06", 5.000000d)]
        [TestCase("gen_boo_nor09", 5.000000d)]
        [TestCase("gen_acc_nor07", 4.000000d)]
        [TestCase("gen_acc_nor08", 6.000000d)]
        [TestCase("gen_acc_nor10", 4.000000d)]
        [TestCase("gen_wea_fin07", 10.000000d)]
        [TestCase("gen_wea_fin08", 8.000000d)]
        [TestCase("gen_arm_fin06", 8.000000d)]
        [TestCase("gen_arm_fin07", 8.000000d)]
        [TestCase("gen_arm_fin08", 9.000000d)]
        [TestCase("gen_pan_fin05", 6.000000d)]
        [TestCase("gen_pan_fin06", 6.000000d)]
        [TestCase("gen_pan_fin07", 7.000000d)]
        [TestCase("gen_pan_fin08", 5.000000d)]
        [TestCase("gen_boo_fin04", 7.000000d)]
        [TestCase("gen_boo_fin07", 7.000000d)]
        [TestCase("gen_boo_fin05", 9.000000d)]
        [TestCase("gen_boo_fin06", 9.000000d)]
        [TestCase("gen_acc_fin07", 8.000000d)]
        [TestCase("gen_acc_fin08", 10.000000d)]
        [TestCase("gen_wea_rar07", 15.000000d)]
        [TestCase("gen_arm_rar06", 12.000000d)]
        [TestCase("gen_boo_rar04", 11.000000d)]
        [TestCase("gen_boo_rar07", 11.000000d)]
        [TestCase("gen_boo_rar05", 15.000000d)]
        [TestCase("gen_boo_rar06", 15.000000d)]
        [TestCase("gen_arm_epi01", 18.000000d)]
        public void RedistributionPreservesThePreviousWeightedBudget(string id, double expected)
        {
            var item = EarlyGameContent.FindItem(id);
            // A tuning guard, not a simulation of combat effectiveness.
            double budget = (item.HpPct + item.QiPct + item.AtkPct + item.DefPct) * 100d
                + item.SpeedAdd + item.CritAdd * 200d + item.CritDmgAdd * 25d;
            Assert.AreEqual(expected, budget, .0001d, id);
        }
    }
}
