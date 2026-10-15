using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class MasteryTests
    {
        private static ArtData Art(string id) { return EarlyGameContent.FindArt(id); }

        [Test]
        public void UsesRaiseLevelAtThresholds()
        {
            var save = new SaveData();
            int rose = -1;
            for (int i = 0; i < 10; i++) rose = Mastery.RecordUse(save, "samcheopyeong");
            Assert.AreEqual(1, rose);
            Assert.AreEqual(1, Mastery.Level(save, "samcheopyeong"));
            for (int i = 0; i < 20; i++) Mastery.RecordUse(save, "samcheopyeong");
            Assert.AreEqual(2, Mastery.Level(save, "samcheopyeong"));
        }

        [Test]
        public void PowerGrowsWithLevelAndVariantNeedsDaeseong()
        {
            var save = new SaveData();
            var art = Mastery_AttackArtWithCost();
            float basePower = art.Power;
            for (int i = 0; i < 10; i++) Mastery.RecordUse(save, art.Id);
            Assert.AreEqual(basePower * 1.05f, Mastery.Apply(save, art).Power, 0.001f);
            Assert.AreEqual(ArtVariant.None, Mastery.CycleVariant(save, art));   // not 대성 yet
            for (int i = 0; i < 20; i++) Mastery.RecordUse(save, art.Id);
            Assert.AreNotEqual(ArtVariant.None, Mastery.CycleVariant(save, art));
        }

        [Test]
        public void VariantsReshapeTheSkill()
        {
            var art = Mastery_AttackArtWithCost();
            var save = new SaveData();
            for (int i = 0; i < 30; i++) Mastery.RecordUse(save, art.Id);
            var plain = Mastery.Apply(save, art);
            Mastery.Find(save, art.Id).Variant = (int)ArtVariant.Flurry;
            var flurry = Mastery.Apply(save, art);
            Assert.AreEqual(plain.Hits + 1, flurry.Hits);
            Assert.Less(flurry.Power, plain.Power);
            Mastery.Find(save, art.Id).Variant = (int)ArtVariant.Swift;
            var swift = Mastery.Apply(save, art);
            Assert.Less(swift.QiCost, plain.QiCost);
            Mastery.Find(save, art.Id).Variant = (int)ArtVariant.Power;
            var power = Mastery.Apply(save, art);
            Assert.Greater(power.Power, plain.Power);
            Assert.GreaterOrEqual(power.QiCost, plain.QiCost);
        }

        [Test]
        public void CompanionArtsAndUltimatesHaveNoVariants()
        {
            foreach (var a in EarlyGameContent.Arts.Where(x => x.OwnerId != null || x.SpiritCost > 0))
                Assert.AreEqual(0, Mastery.AvailableVariants(a).Count, a.Id);
        }

        private static ArtData Mastery_AttackArtWithCost()
        {
            return EarlyGameContent.Arts.First(a => a.OwnerId == null && a.SpiritCost == 0 && a.Effect == SkillEffect.None && a.QiCost > 0);
        }

        [Test]
        public void VariantCostsUseExactIntegerRounding()
        {
            var save = new SaveData();
            var art = new ArtData { Id = "t", Name = "t", Power = 2f, QiCost = 20, Cooldown = 2 };
            for (int i = 0; i < 30; i++) Mastery.RecordUse(save, art.Id);
            Mastery.Find(save, art.Id).Variant = (int)ArtVariant.Power;
            Assert.AreEqual(24, Mastery.Apply(save, art).QiCost);   // 20 x 1.2, never 25
            Mastery.Find(save, art.Id).Variant = (int)ArtVariant.Swift;
            Assert.AreEqual(14, Mastery.Apply(save, art).QiCost);   // 20 x 0.7
            Mastery.Find(save, art.Id).Variant = (int)ArtVariant.Flurry;
            Assert.AreEqual(2, Mastery.Apply(save, art).Hits);      // unset Hits counts as one
        }

        [Test]
        public void ThresholdsAreTenTwentyFifty()
        {
            CollectionAssert.AreEqual(new[] { 0, 10, 20, 50 }, Mastery.Thresholds);
            var save = new SaveData();
            for (int i = 0; i < 19; i++) Mastery.RecordUse(save, "samcheopyeong");
            Assert.AreEqual(1, Mastery.Level(save, "samcheopyeong"));
            Assert.AreEqual(1, Mastery.UsesToNext(save, "samcheopyeong"));
            Mastery.RecordUse(save, "samcheopyeong");
            Assert.AreEqual(2, Mastery.Level(save, "samcheopyeong"));
            for (int i = 0; i < 30; i++) Mastery.RecordUse(save, "samcheopyeong");
            Assert.AreEqual(3, Mastery.Level(save, "samcheopyeong"));
            Assert.AreEqual(-1, Mastery.UsesToNext(save, "samcheopyeong"));
            StringAssert.Contains("50회", Mastery.ConditionText());
        }

        [Test]
        public void MasterLevelShortensTheCooldownByOne()
        {
            var art = EarlyGameContent.Arts.First(a => a.OwnerId == null && a.Cooldown >= 2 && a.SpiritCost == 0);
            var save = new SaveData();
            for (int i = 0; i < 19; i++) Mastery.RecordUse(save, art.Id);
            Assert.AreEqual(art.Cooldown, Mastery.Apply(save, art).Cooldown);
            for (int i = 0; i < 31; i++) Mastery.RecordUse(save, art.Id);
            Assert.AreEqual(art.Cooldown - 1, Mastery.Apply(save, art).Cooldown);
            var instant = EarlyGameContent.Arts.First(a => a.OwnerId == null && a.Cooldown == 0);
            for (int i = 0; i < 50; i++) Mastery.RecordUse(save, instant.Id);
            Assert.AreEqual(0, Mastery.Apply(save, instant).Cooldown);
        }
    }
}
