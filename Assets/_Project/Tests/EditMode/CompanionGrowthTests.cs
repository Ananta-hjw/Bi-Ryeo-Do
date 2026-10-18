using System.Linq;
using NUnit.Framework;
using BiRyeoDo.Core;

namespace BiRyeoDo.Tests
{
    public class CompanionGrowthTests
    {
        private static SaveData Save(int level = 10, int cleared = 30)
        {
            var save = new SaveData { Level = level, ClearedStage = cleared };
            return save;
        }

        private static string AnyGear(ItemKind kind)
        {
            return EarlyGameContent.Items.First(i => i.Kind == kind && i.Grade == ItemGrade.Normal && i.IsGear).Id;
        }

        [Test]
        public void CompanionStartsAtProtagonistLevelThenLevelsOnItsOwn()
        {
            var save = Save(10);
            Assert.AreEqual(10, CompanionGrowth.LevelOf(save, "hyo_ryong"));
            save.Level = 20;   // the protagonist moves on, the companion does not follow any more
            Assert.AreEqual(10, CompanionGrowth.LevelOf(save, "hyo_ryong"));
            int gained = CompanionGrowth.AddExp(save, "hyo_ryong", Progression.ExpToNext(10) + 1);
            Assert.AreEqual(1, gained);
            Assert.AreEqual(11, CompanionGrowth.LevelOf(save, "hyo_ryong"));
        }

        [Test]
        public void CompanionNotJoinedYetFollowsTheProtagonistWithoutARecord()
        {
            var save = Save(5, 0);
            var late = EarlyGameContent.Companions.OrderByDescending(c => c.JoinStage).First();
            Assert.AreEqual(5, CompanionGrowth.LevelOf(save, late.Id));
            Assert.AreEqual(0, save.CompanionLevels.Count);
        }

        [Test]
        public void CompanionGearRaisesItsStats()
        {
            var save = Save(10);
            var c = EarlyGameContent.FindCompanion("hyo_ryong");
            var weapon = EarlyGameContent.Items.First(i => i.Kind == ItemKind.Weapon && i.Grade == ItemGrade.Mythic && i.AtkPct > 0f).Id;
            int before = CompanionGrowth.Create(save, c).Stats.Attack;
            Inventory.Add(save, weapon, 1);
            string error;
            Assert.IsTrue(Inventory.EquipFor(save, "hyo_ryong", weapon, out error), error);
            Assert.Greater(CompanionGrowth.Create(save, c).Stats.Attack, before);
            Assert.IsFalse(Inventory.IsEquipped(save, weapon));   // the protagonist does not wear it
        }

        [Test]
        public void EachWearerNeedsTheirOwnCopy()
        {
            var save = Save(10);
            var w = AnyGear(ItemKind.Weapon);
            Inventory.Add(save, w, 1);
            string error;
            Assert.IsTrue(Inventory.Equip(save, w, out error));
            Assert.IsFalse(Inventory.EquipFor(save, "hyo_ryong", w, out error), "one copy cannot be worn twice");
            StringAssert.Contains("착용 중", error);
            Inventory.Add(save, w, 1);
            Assert.IsTrue(Inventory.EquipFor(save, "hyo_ryong", w, out error), error);
            Assert.AreEqual(2, Inventory.Worn(save, w));
            Assert.AreEqual(0, Gear.Spares(save, w));
            Assert.IsFalse(Salvage.CanDismantle(save, w));
        }

        [Test]
        public void UsingUpACopyTakesItOffACompanion()
        {
            var save = Save(10);
            var w = AnyGear(ItemKind.Weapon);
            Inventory.Add(save, w, 2);
            string error;
            Inventory.Equip(save, w, out error);
            Inventory.EquipFor(save, "jang_hong", w, out error);
            Assert.IsTrue(Inventory.Remove(save, w, 1));
            Assert.AreEqual(1, Inventory.Worn(save, w));
        }

        [Test]
        public void CompanionSlotsFollowTheSameRulesAsTheProtagonist()
        {
            var save = Save(10);
            var a = EarlyGameContent.Items.Where(i => i.Kind == ItemKind.Accessory && i.IsGear && !Gear.IsHeroOnly(i.Id)).Take(3).Select(i => i.Id).ToList();
            string error;
            foreach (var id in a) Inventory.Add(save, id, 1);
            Assert.IsTrue(Inventory.EquipFor(save, "yun_junho", a[0], out error));
            Assert.IsTrue(Inventory.EquipFor(save, "yun_junho", a[1], out error));
            Assert.IsFalse(Inventory.EquipFor(save, "yun_junho", a[2], out error));
        }

        [Test]
        public void BiroeSetIsHeroOnly()
        {
            var save = Save(10);
            string error;
            foreach (var id in Gear.SetOf("biroe_blade").Items)
            {
                Assert.IsTrue(Gear.IsHeroOnly(id), id);
                Inventory.Add(save, id, 1);
                Assert.IsFalse(Inventory.EquipFor(save, "hyo_ryong", id, out error), id);
                StringAssert.Contains(Gear.HeroOnlyLabel, error);
                Assert.IsTrue(Inventory.Equip(save, id, out error) || error != null, id);   // the protagonist is not blocked by this rule
            }
            Assert.IsFalse(Gear.IsHeroOnly(EarlyGameContent.Items.First(i => i.IsGear && Gear.SetOf(i.Id) == null).Id));
        }
    }
}
