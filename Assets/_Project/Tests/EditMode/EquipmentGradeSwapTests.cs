using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class EquipmentGradeSwapTests
    {
        [Test]
        public void CrimsonAndStormExchangeTheirGradeAndBaseEffectsWithoutChangingTheirSets()
        {
            var crimson = EarlyGameContent.FindItem("crimson_saber");
            var storm = EarlyGameContent.FindItem("storm_blade");
            Assert.AreEqual(ItemGrade.Legendary, crimson.Grade);
            Assert.AreEqual(.33f, crimson.AtkPct);   // 전설 x1.5 grade scale
            Assert.AreEqual(.045f, crimson.CritAdd);
            Assert.AreEqual(5, crimson.SpeedAdd);
            Assert.AreEqual(0f, crimson.HpPct);
            Assert.AreEqual(ItemGrade.Epic, storm.Grade);
            Assert.AreEqual(.208f, storm.AtkPct);   // 영웅 x1.3 grade scale
            Assert.AreEqual(.039f, storm.HpPct);
            Assert.AreEqual(0f, storm.CritAdd);
            Assert.AreEqual(0, storm.SpeedAdd);
            Assert.AreEqual("flame", Gear.SetOf(crimson.Id).Id);
            Assert.IsNull(Gear.SetOf(storm.Id));
            Assert.IsTrue(Gacha.Pool(ItemGrade.Legendary).Any(item => item.Id == crimson.Id));
            Assert.IsFalse(Gacha.Pool(ItemGrade.Epic).Any(item => item.Id == crimson.Id));
            Assert.IsTrue(Gacha.Pool(ItemGrade.Epic).Any(item => item.Id == storm.Id));
            Assert.IsFalse(Gacha.Pool(ItemGrade.Legendary).Any(item => item.Id == storm.Id));
        }

        [Test]
        public void BreakthroughTraitsFollowTheExchangedEffectPackages()
        {
            var crimson = Gear.BreakthroughTraits(EarlyGameContent.FindItem("crimson_saber"));
            CollectionAssert.AreEqual(new[] { GearTraitKind.CritDamage, GearTraitKind.Qi, GearTraitKind.Defense }, crimson.Select(trait => trait.Kind));
            var storm = Gear.BreakthroughTraits(EarlyGameContent.FindItem("storm_blade"));
            CollectionAssert.AreEqual(new[] { GearTraitKind.Crit, GearTraitKind.Speed, GearTraitKind.CritDamage }, storm.Select(trait => trait.Kind));
        }
    }
}
