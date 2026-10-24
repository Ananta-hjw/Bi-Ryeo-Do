using System;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class EquipmentRecommendationTests
    {
        private static SaveData Save()
        {
            return new SaveData { Level = 20, ClearedStage = 29, Doo = 12345 };
        }

        private static void Add(SaveData save, params string[] ids)
        {
            foreach (string id in ids) Inventory.Add(save, id, 1);
        }

        private static string[] Slots(SaveData save, string owner)
        {
            return Enumerable.Range(0, 6).Select(i => Gear.ItemInSlot(save, owner, i)?.Id).ToArray();
        }

        [Test]
        public void SuggestIsDeterministicAndDoesNotChangeEquipmentInventoryOrInvestment()
        {
            var save = Save(); Inventory.EnsureStartKit(save);
            Add(save, "crimson_saber", "bronze_gauntlet", "silver_hairpin");
            string error;
            Inventory.EquipFor(save, "hyo_ryong", "crimson_saber", out error);
            var before = Slots(save, Inventory.Hero);
            var items = save.Items.Select(s => s.Id + ":" + s.Count).ToArray();
            var others = Inventory.GearOf(save, "hyo_ryong").ToArray();
            var investments = save.GearSlots.Select(s => s.Owner + ":" + s.Index + ":" + s.Level + ":" + s.UnlockedGrade).ToArray();
            var first = EquipmentRecommendation.Suggest(save, Inventory.Hero);
            var second = EquipmentRecommendation.Suggest(save, Inventory.Hero);
            CollectionAssert.AreEqual(first, second);
            CollectionAssert.AreEqual(before, Slots(save, Inventory.Hero));
            CollectionAssert.AreEqual(items, save.Items.Select(s => s.Id + ":" + s.Count));
            CollectionAssert.AreEqual(others, Inventory.GearOf(save, "hyo_ryong"));
            CollectionAssert.AreEqual(investments, save.GearSlots.Select(s => s.Owner + ":" + s.Index + ":" + s.Level + ":" + s.UnlockedGrade));
            Assert.AreEqual(12345, save.Doo);
        }

        [Test]
        public void EquipsAllSixAvailableSlotsWithoutSpendingCurrencyOrMaterials()
        {
            var save = Save();
            Add(save, "iron_sword", "cloth_robe", "train_pants", "straw_sandals", "silver_hairpin", "bronze_gauntlet");
            Inventory.Add(save, Gear.StoneId, 99);
            var counts = save.Items.Select(s => s.Id + ":" + s.Count).ToArray();
            Assert.IsTrue(EquipmentRecommendation.Equip(save, Inventory.Hero, out int changed, out string error), error);
            Assert.AreEqual(6, changed);
            Assert.IsTrue(Slots(save, Inventory.Hero).All(id => id != null));
            for (int i = 0; i < 6; i++) Assert.AreEqual(Gear.KindForSlot(i), Gear.ItemInSlot(save, Inventory.Hero, i).Kind);
            CollectionAssert.AreEqual(counts, save.Items.Select(s => s.Id + ":" + s.Count));
            Assert.AreEqual(12345, save.Doo);
        }

        [Test]
        public void DoesNotTakeAnotherPersonsOnlyCopyButMayUseASpare()
        {
            var save = Save();
            Add(save, "heaven_cutter", "iron_sword");
            Assert.IsTrue(Inventory.EquipFor(save, "hyo_ryong", "heaven_cutter", out string error), error);
            Assert.AreEqual("iron_sword", EquipmentRecommendation.Suggest(save, Inventory.Hero)[0]);
            Inventory.Add(save, "heaven_cutter", 1);
            Assert.IsTrue(EquipmentRecommendation.Equip(save, Inventory.Hero, out _, out error), error);
            Assert.AreEqual("heaven_cutter", Gear.ItemInSlot(save, Inventory.Hero, 0).Id);
            Assert.AreEqual("heaven_cutter", Gear.ItemInSlot(save, "hyo_ryong", 0).Id);
            Assert.AreEqual(2, Inventory.Count(save, "heaven_cutter"));
        }

        [Test]
        public void CompanionCannotReceiveHeroOnlyGearAndUnjoinedPeopleAreRejected()
        {
            var save = Save(); Add(save, "biroe_blade", "iron_sword");
            Assert.AreEqual("iron_sword", EquipmentRecommendation.Suggest(save, "hyo_ryong")[0]);
            save.ClearedStage = 0;
            Assert.IsNull(EquipmentRecommendation.Suggest(save, "hyo_ryong"));
            Assert.IsFalse(EquipmentRecommendation.Equip(save, "unknown", out int changed, out string error));
            Assert.AreEqual(0, changed); Assert.IsNotEmpty(error);
        }

        [Test]
        public void AccessoriesRemainDistinctEvenWithMultipleCopies()
        {
            var save = Save(); Inventory.Add(save, "emperor_seal", 3);
            Add(save, "bronze_gauntlet");
            Assert.IsTrue(EquipmentRecommendation.Equip(save, Inventory.Hero, out _, out string error), error);
            var slots = Slots(save, Inventory.Hero);
            Assert.IsNotNull(slots[4]); Assert.IsNotNull(slots[5]);
            Assert.AreNotEqual(slots[4], slots[5]);
            Assert.AreEqual(1, Inventory.Worn(save, "emperor_seal"));
        }

        [TestCase(false, 5, "bronze_gauntlet")]
        [TestCase(true, 0, "bronze_gauntlet")]
        [TestCase(true, 5, "phoenix_feather")]
        public void AccessoryPlacementUsesActualSlotGradeUnlockAndBreakthroughCap(bool unlocked, int featherStars, string expected)
        {
            var save = Save(); Add(save, "phoenix_feather", "bronze_gauntlet");
            Gear.EnsureProgression(save);
            save.GearBreakthroughs.Add(new GearLevel { Id = "bronze_gauntlet", Level = 5 });
            save.GearBreakthroughs.Add(new GearLevel { Id = "phoenix_feather", Level = featherStars });
            var enhanced = save.GearSlots.Single(slot => slot.Owner == Inventory.Hero && slot.Index == 4);
            enhanced.Level = 100;
            enhanced.UnlockedGrade = (int)(unlocked ? ItemGrade.Mythic : ItemGrade.Fine);
            var plan = EquipmentRecommendation.Suggest(save, Inventory.Hero);
            Assert.AreEqual(expected, plan[4]);
            Assert.IsTrue(EquipmentRecommendation.Equip(save, Inventory.Hero, out _, out string error), error);
            Assert.AreEqual(expected, Gear.ItemInSlot(save, Inventory.Hero, 4).Id);
            Assert.AreEqual(100, Gear.SlotLevel(save, Inventory.Hero, 4));
            Assert.AreEqual(12345, save.Doo);
        }

        [Test]
        public void SetCompletionCanBeatTheIndividuallyStrongerWeapon()
        {
            var save = Save(); Add(save, "laundry_bat", "twin_daggers");
            Assert.AreEqual("twin_daggers", EquipmentRecommendation.Suggest(save, Inventory.Hero)[0]);
            Add(save, "cloth_robe");
            var result = EquipmentRecommendation.Suggest(save, Inventory.Hero);
            Assert.AreEqual("laundry_bat", result[0]);
            Assert.AreEqual("cloth_robe", result[1]);
        }

        [Test]
        public void SupportAndAttackRolesPreferDifferentTradeoffs()
        {
            var save = Save(); Add(save, "cloth_robe", "gen_arm_nor05");
            Assert.AreEqual("cloth_robe", EquipmentRecommendation.Suggest(save, "hyo_ryong")[1]);
            Assert.AreEqual("gen_arm_nor05", EquipmentRecommendation.Suggest(save, Inventory.Hero)[1]);
            Assert.AreEqual("cloth_robe", EquipmentRecommendation.Suggest(save, "yun_junho")[1]);
        }

        [Test]
        public void HealersUseTheirLearnedSkillResourceNeeds()
        {
            var save = Save(); Add(save, "cloth_robe", "gen_arm_fin04");
            Assert.AreEqual("cloth_robe", EquipmentRecommendation.Suggest(save, Inventory.Hero)[1]);
            Assert.AreEqual("gen_arm_fin04", EquipmentRecommendation.Suggest(save, "yun_junho")[1]);
        }

        [Test]
        public void EquippedArtsChangeQiPrioritiesButMerelyLearningAnArtDoesNot()
        {
            var save = Save(); save.Level = 1;
            Add(save, "cloth_robe", "gen_arm_fin04");
            Assert.AreEqual("cloth_robe", EquipmentRecommendation.Suggest(save, Inventory.Hero)[1]);
            save.LearnedArts.Add("goengroe");
            Assert.AreEqual("cloth_robe", EquipmentRecommendation.Suggest(save, Inventory.Hero)[1]);
            save.EquippedArts.Add("goengroe");
            Assert.AreEqual("gen_arm_fin04", EquipmentRecommendation.Suggest(save, Inventory.Hero)[1]);
        }

        [TestCase("player")]
        [TestCase("hyo_ryong")]
        [TestCase("yun_junho")]
        public void FullCatalogRecommendationsRespectOwnershipAndAreStableAfterApplying(string owner)
        {
            var save = Save();
            foreach (var item in EarlyGameContent.Items.Where(i => i.IsGear)) Inventory.Add(save, item.Id, 1);
            Assert.IsTrue(EquipmentRecommendation.Equip(save, owner, out _, out string error), error);
            var original = Slots(save, owner);
            Assert.IsTrue(EquipmentRecommendation.Equip(save, owner, out int changed, out error), error);
            CollectionAssert.AreEqual(original, Slots(save, owner));
            Assert.AreEqual(0, changed);
            foreach (string id in original.Where(id => id != null))
            {
                Assert.LessOrEqual(Inventory.Worn(save, id), Inventory.Count(save, id));
                if (owner != Inventory.Hero) Assert.IsFalse(Gear.IsHeroOnly(id));
            }
        }

        [Test]
        public void WearerNamesIncludeHeroAndBenchCompanionsAndCanExcludeTheViewedOwner()
        {
            var save = Save(); Inventory.Add(save, "iron_sword", 3);
            Inventory.EquipFor(save, Inventory.Hero, "iron_sword", out _);
            Inventory.EquipFor(save, "hyo_ryong", "iron_sword", out _);
            Inventory.EquipFor(save, "yun_junho", "iron_sword", out _);
            save.BenchedCompanions.Add("yun_junho");
            CollectionAssert.AreEquivalent(new[] { "비류연", "효룡", "윤준호" }, Inventory.WearerNames(save, "iron_sword"));
            CollectionAssert.AreEquivalent(new[] { "비류연", "윤준호" }, Inventory.WearerNames(save, "iron_sword", "hyo_ryong"));
        }
    }
}
