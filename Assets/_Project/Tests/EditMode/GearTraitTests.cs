using System;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class GearTraitTests
    {
        private static float Base(ItemData item, GearTraitKind kind)
        {
            switch (kind)
            {
                case GearTraitKind.Hp: return item.HpPct;
                case GearTraitKind.Qi: return item.QiPct;
                case GearTraitKind.Attack: return item.AtkPct;
                case GearTraitKind.Defense: return item.DefPct;
                case GearTraitKind.Speed: return item.SpeedAdd;
                case GearTraitKind.Crit: return item.CritAdd;
                case GearTraitKind.CritDamage: return item.CritDmgAdd;
                default: return 0f;
            }
        }

        [Test]
        public void EveryEligibleGearHasThreeDistinctNewPositiveTraitsAtOneThreeAndFiveStars()
        {
            foreach (var item in EarlyGameContent.Items.Where(i => i.IsGear && i.Grade >= ItemGrade.Epic && i.Id != Ritual.RewardId))
            {
                var traits = Gear.BreakthroughTraits(item);
                CollectionAssert.AreEqual(new[] { 1, 3, 5 }, traits.Select(t => t.Stars), item.Id);
                Assert.AreEqual(3, traits.Select(t => t.Kind).Distinct().Count(), item.Id);
                foreach (var trait in traits)
                {
                    Assert.AreEqual(0f, Base(item, trait.Kind), item.Id + ": " + trait.Kind);
                    Assert.Greater(trait.Value, 0f, item.Id);
                }
            }
        }

        [TestCase(ItemGrade.Normal)]
        [TestCase(ItemGrade.Fine)]
        [TestCase(ItemGrade.Rare)]
        public void BelowEpicGearHasNoTraitsButRetainsEnhancementAndStarBonuses(ItemGrade grade)
        {
            foreach (var item in EarlyGameContent.Items.Where(i => i.IsGear && i.Grade == grade))
            {
                Assert.IsEmpty(Gear.BreakthroughTraits(item), item.Id);
                Assert.IsEmpty(Gear.TraitsText(item, 5), item.Id);
                var effects = Gear.EffectsFor(item, 100, 5);
                float multiplier = Gear.Multiplier(100, 5);
                Assert.AreEqual(item.HpPct * multiplier, effects.HpPct, .00001f, item.Id);
                Assert.AreEqual(item.QiPct * multiplier, effects.QiPct, .00001f, item.Id);
                Assert.AreEqual(item.AtkPct * multiplier, effects.AtkPct, .00001f, item.Id);
                Assert.AreEqual(item.DefPct * multiplier, effects.DefPct, .00001f, item.Id);
                Assert.AreEqual(item.SpeedAdd * multiplier, effects.SpeedAdd, .00001f, item.Id);
                Assert.AreEqual(item.CritAdd * (1.5f + 6.9f * .10f), effects.CritAdd, .00001f, item.Id);
                Assert.AreEqual(item.CritDmgAdd * multiplier, effects.CritDmgAdd, .00001f, item.Id);
                Assert.AreEqual(0, effects.StartSpirit, item.Id);
            }
        }

        [TestCase(ItemGrade.Epic, .05f, .0125f, .10f, 2, 4, 1.2f, 1.4f, 1.7f)]
        [TestCase(ItemGrade.Legendary, .07f, .015f, .14f, 3, 6, 1.4f, 1.6f, 2f)]
        [TestCase(ItemGrade.Mythic, .10f, .02f, .20f, 4, 8, 1.6f, 1.8f, 2.3f)]
        public void GradeMilestoneBoostsApplyToAllNewTraitsExceptCriticalChance(
            ItemGrade grade, float percent, float crit, float critDamage, int speed, int spirit, float boostOne, float boostThree, float boostFive)
        {
            var boosts = new[] { boostOne, boostThree, boostFive };
            foreach (var item in EarlyGameContent.Items.Where(i => i.IsGear && i.Grade == grade))
            {
                var traits = Gear.BreakthroughTraits(item);
                for (int i = 0; i < traits.Length; i++)
                {
                    var trait = traits[i];
                    float baseline = trait.Kind == GearTraitKind.Crit ? crit
                        : trait.Kind == GearTraitKind.CritDamage ? critDamage
                        : trait.Kind == GearTraitKind.Speed ? speed
                        : trait.Kind == GearTraitKind.StartSpirit ? spirit : percent;
                    float expected = baseline * (i + 1) * (trait.Kind == GearTraitKind.Crit ? 1f : boosts[i]);
                    if (trait.Kind == GearTraitKind.Speed || trait.Kind == GearTraitKind.StartSpirit)
                        expected = (float)Math.Round(expected, MidpointRounding.AwayFromZero);
                    Assert.AreEqual(expected, trait.Value, .00001f, item.Id + ": " + trait.Kind);
                }
            }
        }

        [TestCase(0, 0f, 0f, 0f)]
        [TestCase(1, .196f, 0f, 0f)]
        [TestCase(2, .196f, 0f, 0f)]
        [TestCase(3, .196f, .224f, 0f)]
        [TestCase(4, .196f, .224f, 0f)]
        [TestCase(5, .196f, .224f, .42f)]
        public void MilestonesUnlockCumulativelyOnlyAtTheirRequestedStar(int stars, float critDamage, float qi, float defense)
        {
            var effects = Gear.EffectsFor(EarlyGameContent.FindItem("crimson_saber"), 0, stars);
            Assert.AreEqual(critDamage, effects.CritDmgAdd, .00001f);
            Assert.AreEqual(qi, effects.QiPct, .00001f);
            Assert.AreEqual(defense, effects.DefPct, .00001f);
        }

        [Test]
        public void NewlyUnlockedTraitsAreNotMultipliedByEnhancement()
        {
            var item = EarlyGameContent.FindItem("biroe_blade");
            var fresh = Gear.EffectsFor(item, 0, 5);
            var enhanced = Gear.EffectsFor(item, 100, 5);
            Assert.AreEqual(.32f, fresh.CritDmgAdd, .00001f);
            Assert.AreEqual(.36f, fresh.QiPct, .00001f);
            Assert.AreEqual(.69f, fresh.DefPct, .00001f);
            Assert.AreEqual(fresh.CritDmgAdd, Gear.EffectsFor(item, 0, 1).CritDmgAdd);
            Assert.AreEqual(fresh.CritDmgAdd, enhanced.CritDmgAdd);
            Assert.AreEqual(fresh.QiPct, Gear.EffectsFor(item, 0, 3).QiPct);
            Assert.AreEqual(fresh.QiPct, enhanced.QiPct);
            Assert.AreEqual(fresh.DefPct, enhanced.DefPct);
            Assert.Greater(enhanced.AtkPct, fresh.AtkPct);
        }

        [Test]
        public void StrongerGradesNeverHaveWeakerMatchingMilestoneBudgets()
        {
            GearTrait[] previous = null;
            foreach (ItemGrade grade in Enum.GetValues(typeof(ItemGrade)).Cast<ItemGrade>().Where(g => g >= ItemGrade.Epic && g <= ItemGrade.Mythic))
            {
                var item = new ItemData { Id = "custom", Kind = ItemKind.Weapon, Grade = grade, AtkPct = .1f };
                var traits = Gear.BreakthroughTraits(item);
                if (previous != null)
                    for (int i = 0; i < traits.Length; i++)
                    {
                        Assert.AreEqual(previous[i].Kind, traits[i].Kind);
                        Assert.GreaterOrEqual(traits[i].Value, previous[i].Value);
                    }
                previous = traits;
            }
        }

        [Test]
        public void ZeroStarGearPreservesItsOriginalEffectsAndNegativeTradeoffs()
        {
            foreach (var item in EarlyGameContent.Items.Where(i => i.IsGear && i.Grade != ItemGrade.Transcendent))
            {
                var effects = Gear.EffectsFor(item);
                Assert.AreEqual(item.HpPct, effects.HpPct, item.Id);
                Assert.AreEqual(item.QiPct, effects.QiPct, item.Id);
                Assert.AreEqual(item.AtkPct, effects.AtkPct, item.Id);
                Assert.AreEqual(item.DefPct, effects.DefPct, item.Id);
                Assert.AreEqual(item.SpeedAdd, effects.SpeedAdd, item.Id);
                Assert.AreEqual(item.CritAdd, effects.CritAdd, item.Id);
                Assert.AreEqual(item.CritDmgAdd, effects.CritDmgAdd, item.Id);
                Assert.AreEqual(0, effects.StartSpirit, item.Id);
            }
        }

        [Test]
        public void StartingSpiritAndNewStatsReachTheHeroAndBattleWithoutGradeUnlock()
        {
            var save = new SaveData();
            Inventory.Add(save, "roegeum", 6);
            string error;
            Assert.IsTrue(Inventory.Equip(save, "roegeum", out error), error);
            for (int i = 0; i < 5; i++) Assert.IsTrue(Gear.Breakthrough(save, "roegeum", out error), error);
            Assert.AreEqual(0, Gear.LevelFor(save, Inventory.Hero, "roegeum"));
            var hero = Formation.BuildHero(save);
            Assert.AreEqual(55, hero.StartSpirit);
            var enemy = new Combatant("e", "e", Team.Enemy,
                new CombatStats { MaxHp = 10000, Attack = 1, Defense = 1, Speed = 1 }, null);
            var battle = new InteractiveBattle(hero, new[] { enemy }, new SeededRandom(1));
            battle.Begin();
            Assert.AreEqual(55, hero.Spirit);
            StringAssert.Contains("방어 +36%", StatusSheet.EffectText(EarlyGameContent.FindItem("roegeum"), 0, 5));
            StringAssert.Contains("시작 기세 +55", StatusSheet.EffectText(EarlyGameContent.FindItem("roegeum"), 0, 5));
        }

        [TestCase(0)]
        [TestCase(10)]
        [TestCase(30)]
        [TestCase(70)]
        [TestCase(100)]
        public void JinBiroedoUsesCurrentFiveStarBonusAndReducedCriticalScalingWithoutNewTraits(int level)
        {
            var item = EarlyGameContent.FindItem(Ritual.RewardId);
            Assert.IsEmpty(Gear.BreakthroughTraits(item));
            var effects = Gear.EffectsFor(item, level, 5);
            Assert.AreEqual(1f, Gear.StarBonusFor(item, 5));
            Assert.AreEqual(1f, Gear.StarBonusFor(item, 0), "innate five-star previews use the current bonus");
            Assert.AreEqual(Gear.EffectsFor(item, level).AtkPct, effects.AtkPct);
            float current = (Gear.Multiplier(level) + 1f) / 1.3f;
            Assert.AreEqual(item.AtkPct * current, effects.AtkPct, .00001f);
            Assert.AreEqual(item.HpPct * current, effects.HpPct, .00001f);
            Assert.AreEqual(item.SpeedAdd * current, effects.SpeedAdd, .00001f);
            Assert.AreEqual(item.CritAdd * ((1.5f + (Gear.Multiplier(level) - 1f) * .10f) / 1.3f), effects.CritAdd, .00001f);
            Assert.AreEqual(item.CritDmgAdd * current, effects.CritDmgAdd, .00001f);
            Assert.AreEqual(.70f * (Gear.Multiplier(level) + 1f) / 2f, effects.DefPct, .00001f);
            Assert.AreEqual(.50f * (Gear.Multiplier(level) + 1f) / 2f, effects.QiPct, .00001f);
            Assert.AreEqual(0, effects.StartSpirit);
        }

        [Test]
        public void JinBiroedoBaseQiAndDefenseMatchFiveStarTargetsAndScaleWithEnhancement()
        {
            var item = EarlyGameContent.FindItem(Ritual.RewardId);
            var fresh = Gear.EffectsFor(item);
            var enhanced = Gear.EffectsFor(item, 100);
            Assert.AreEqual(.50f, fresh.QiPct, .00001f);
            Assert.AreEqual(.70f, fresh.DefPct, .00001f);
            Assert.AreEqual(2.225f, enhanced.QiPct, .00001f);
            Assert.AreEqual(3.115f, enhanced.DefPct, .00001f);
            StringAssert.Contains("내공 +222.5%", StatusSheet.EffectText(item, 100));
            StringAssert.Contains("방어 +311.5%", StatusSheet.EffectText(item, 100));
            Assert.IsEmpty(Gear.BreakthroughTraits(item), "these are base effects, not fixed breakthrough traits");
        }

        [TestCase(0, 0, .08f)]
        [TestCase(20, 0, .0848f)]
        [TestCase(30, 0, .088f)]
        [TestCase(50, 0, .096f)]
        [TestCase(70, 0, .1072f)]
        [TestCase(90, 0, .1232f)]
        [TestCase(100, 0, .1352f)]
        [TestCase(0, 1, .084f)]
        [TestCase(100, 1, .1392f)]
        [TestCase(0, 2, .088f)]
        [TestCase(0, 3, .096f)]
        [TestCase(0, 4, .106f)]
        [TestCase(0, 5, .12f)]
        [TestCase(100, 5, .1752f)]
        public void CriticalChanceKeepsBaseWithHalfStarBonusAndOneTenthEnhancement(int level, int stars, float expected)
        {
            var item = EarlyGameContent.FindItem("biroe_blade");
            Assert.AreEqual(expected, Gear.EffectsFor(item, level, stars).CritAdd, .00001f);
            Assert.AreEqual(item.CritDmgAdd * Gear.EffectMultiplier(item, level, stars)
                + (stars >= 1 ? .32f : 0f), Gear.EffectsFor(item, level, stars).CritDmgAdd, .00001f);
        }

        [Test]
        public void FixedCriticalChanceTraitNeverScalesWithEnhancementOrLaterStars()
        {
            var item = EarlyGameContent.FindItem("taegeuk_jade");
            Assert.AreEqual(0f, item.CritAdd);
            Assert.AreEqual(.04f, Gear.EffectsFor(item, 0, 3).CritAdd, .00001f);
            Assert.AreEqual(.04f, Gear.EffectsFor(item, 100, 3).CritAdd, .00001f);
            Assert.AreEqual(.04f, Gear.EffectsFor(item, 100, 5).CritAdd, .00001f);
        }

        [Test]
        public void SharedStarsApplyTraitsToEachWearerWithoutLeakingToReplacementGear()
        {
            var save = new SaveData();
            Inventory.Add(save, "crimson_saber", 5);
            string error;
            Assert.IsTrue(Inventory.Equip(save, "crimson_saber", out error), error);
            Assert.IsTrue(Inventory.EquipFor(save, "hyo_ryong", "crimson_saber", out error), error);
            for (int i = 0; i < 3; i++) Assert.IsTrue(Gear.Breakthrough(save, "crimson_saber", out error), error);
            var stats = new CombatStats { MaxHp = 1000, MaxQi = 100, Attack = 100, Defense = 100, Speed = 100, CritDamage = 1.5f };
            var hero = Inventory.ApplyGear(stats, Inventory.GearOf(save, Inventory.Hero), Gear.LevelsFor(save, Inventory.Hero));
            var companion = Inventory.ApplyGear(stats, Inventory.GearOf(save, "hyo_ryong"), Gear.LevelsFor(save, "hyo_ryong"));
            Assert.AreEqual(122, hero.MaxQi);
            Assert.AreEqual(hero.MaxQi, companion.MaxQi);
            Assert.AreEqual(1.696f, companion.CritDamage, .00001f);
            Inventory.Add(save, "iron_sword", 1);
            Assert.IsTrue(Inventory.Equip(save, "iron_sword", out error), error);
            var replaced = Inventory.ApplyGear(stats, Inventory.GearOf(save, Inventory.Hero), Gear.LevelsFor(save, Inventory.Hero));
            Assert.AreEqual(100, replaced.MaxQi);
            Assert.AreEqual(1.5f, replaced.CritDamage);
        }
    }
}
