using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class GearTests
    {
        private static CombatStats Base()
        {
            return new CombatStats { MaxHp = 1000, MaxQi = 100, Attack = 100, Defense = 100, Speed = 100, CritRate = 0.01f, CritDamage = 1.5f };
        }

        [Test]
        public void SetsReferenceRealGearAndFitTheSlots()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (var set in Gear.Sets)
            {
                int w = 0, a = 0, pa = 0, bo = 0, acc = 0;
                foreach (var id in set.Items)
                {
                    var it = EarlyGameContent.FindItem(id);
                    Assert.IsNotNull(it, set.Id + ":" + id);
                    Assert.IsTrue(it.IsGear, id);
                    Assert.IsTrue(seen.Add(id), id + " is in two sets");
                    if (it.Grade == ItemGrade.Transcendent) continue;   // a 초월 weapon stands beside the weapon it replaces
                    if (it.Kind == ItemKind.Weapon) w++; else if (it.Kind == ItemKind.Armor) a++; else if (it.Kind == ItemKind.Pants) pa++; else if (it.Kind == ItemKind.Boots) bo++; else acc++;
                }
                Assert.LessOrEqual(w, 1, set.Id); Assert.LessOrEqual(a, 1, set.Id); Assert.LessOrEqual(pa, 1, set.Id); Assert.LessOrEqual(bo, 1, set.Id); Assert.LessOrEqual(acc, Inventory.AccessorySlots, set.Id);
                foreach (var t in set.Tiers) Assert.LessOrEqual(t.Pieces, set.Items.Length, set.Id);
            }
        }

        [Test]
        public void EnhancingCostsDooAndRaisesStats()
        {
            var save = new SaveData { Doo = 100000 };
            Inventory.Add(save, "iron_sword", 1);
            string err;
            long before = save.Doo;
            Assert.IsTrue(Gear.Enhance(save, "iron_sword", out err));
            Assert.AreEqual(1, Gear.Level(save, "iron_sword"));
            Assert.AreEqual(75, before - save.Doo);
            var plain = Inventory.ApplyGear(Base(), new[] { "iron_sword" });
            var plus = Inventory.ApplyGear(Base(), new[] { "iron_sword" }, save.GearLevels);
            Assert.AreEqual(104, plain.Attack);
            Assert.AreEqual(104, plus.Attack);   // +4% x1.1 = 4.4% rounds to 104
            for (int i = 0; i < 9; i++) Assert.IsTrue(Gear.Enhance(save, "iron_sword", out err));
            Assert.AreEqual(Gear.MaxLevel, Gear.Level(save, "iron_sword"));
            Assert.AreEqual(108, Inventory.ApplyGear(Base(), new[] { "iron_sword" }, save.GearLevels).Attack);   // doubled
            Assert.IsFalse(Gear.Enhance(save, "iron_sword", out err));
            Assert.AreEqual(-1, Gear.NextCost(save, "iron_sword"));
        }

        [Test]
        public void EnhanceNeedsTheItemAndEnoughDoo()
        {
            var save = new SaveData { Doo = 10 };
            string err;
            Assert.IsFalse(Gear.Enhance(save, "iron_sword", out err));   // not owned
            Inventory.Add(save, "iron_sword", 1);
            Assert.IsFalse(Gear.Enhance(save, "iron_sword", out err));   // too poor
            Assert.AreEqual(10, save.Doo);
            Assert.IsFalse(Gear.Enhance(save, "bear_gall", out err));    // not gear
        }

        [Test]
        public void CostsGrowWithLevelAndGrade()
        {
            var normal = EarlyGameContent.FindItem("iron_sword");
            var mythic = EarlyGameContent.FindItem("heaven_cutter");
            Assert.Greater(Gear.CostFor(normal, 5), Gear.CostFor(normal, 1));
            Assert.Greater(Gear.CostFor(mythic, 1), Gear.CostFor(normal, 1));
            Assert.AreEqual(-1, Gear.CostFor(normal, Gear.MaxLevel + 1));
        }

        [Test]
        public void SetBonusNeedsTwoPiecesAndStacksTiers()
        {
            var one = Inventory.ApplyGear(Base(), new[] { "jin_biroedo" });
            var two = Inventory.ApplyGear(Base(), new[] { "jin_biroedo", "roegeum" });
            var three = Inventory.ApplyGear(Base(), new[] { "jin_biroedo", "roegeum", "mukrin_talisman" });
            var plainTwo = Inventory.ApplyGear(Base(), new[] { "roegeum" });
            Assert.Greater(two.Attack, one.Attack + plainTwo.Attack - 100);   // bonus beyond the plain pieces
            Assert.Greater(three.Attack, two.Attack);
            Assert.AreEqual(2, Gear.SetLines(new[] { "jin_biroedo", "roegeum", "dumok_spear" }).Count);
            Assert.IsTrue(Gear.SetLines(new[] { "jin_biroedo" })[0].Contains("2개부터"));
        }

        [Test]
        public void StatusSheetShowsEnhancedNumbers()
        {
            var save = new SaveData { Level = 10, Doo = 100000 };
            Inventory.Add(save, "iron_sword", 1);
            string err;
            Inventory.Equip(save, "iron_sword", out err);
            int before = int.Parse(StatusSheet.Hero(save)[2].Value.Replace(",", ""));
            for (int i = 0; i < 10; i++) Gear.Enhance(save, "iron_sword", out err);
            int after = int.Parse(StatusSheet.Hero(save)[2].Value.Replace(",", ""));
            Assert.Greater(after, before);
        }

        [Test]
        public void TheCatalogHasTheRequestedNumberOfPiecesPerGrade()
        {
            int Added(ItemGrade g) { return GachaItemsMore.All.Count(i => i.Grade == g); }
            Assert.AreEqual(50, Added(ItemGrade.Normal));
            Assert.AreEqual(40, Added(ItemGrade.Fine));
            Assert.AreEqual(35, Added(ItemGrade.Rare));
            Assert.AreEqual(20, Added(ItemGrade.Epic));
            Assert.AreEqual(13, Added(ItemGrade.Legendary));
            Assert.AreEqual(10, Added(ItemGrade.Mythic));
            Assert.AreEqual(EarlyGameContent.Items.Length, EarlyGameContent.Items.Select(i => i.Id).Distinct().Count(), "duplicate item id");
        }

        [Test]
        public void OnePieceOfEachSlotIsWornAndANewOneReplacesTheOld()
        {
            var save = new SaveData();
            foreach (var id in new[] { "cloth_robe", "snakescale_vest", "train_pants", "ironwall_greaves", "straw_sandals", "wind_boots" }) Inventory.Add(save, id, 1);
            string err;
            foreach (var id in new[] { "cloth_robe", "snakescale_vest", "train_pants", "ironwall_greaves", "straw_sandals", "wind_boots" }) Assert.IsTrue(Inventory.Equip(save, id, out err), id);
            Assert.AreEqual(3, save.EquippedGear.Count);
            Assert.IsTrue(save.EquippedGear.Contains("snakescale_vest"));
            Assert.IsTrue(save.EquippedGear.Contains("ironwall_greaves"));
            Assert.IsTrue(save.EquippedGear.Contains("wind_boots"));
        }

        [Test]
        public void TheBiroeSetIsTheStrongestAndItsMythicPiecesBeatTheirKind()
        {
            float Total(GearSet s) { var b = Gear.ActiveBonus(s, 6); return b.AtkPct + b.DefPct + b.HpPct + b.QiPct + b.CritAdd * 4f + b.SpeedAdd * 0.02f; }
            var biroe = Gear.Sets.First(s => s.Id == "biroe");
            foreach (var s in Gear.Sets) if (s != biroe) Assert.Greater(Total(biroe), Total(s), s.Id);
            Assert.AreEqual(6, biroe.Tiers.Max(t => t.Pieces));
            float Score(ItemData i) { return i.AtkPct + i.DefPct + i.HpPct + i.QiPct + i.CritAdd * 4f + i.SpeedAdd * 0.02f; }
            foreach (var id in new[] { "biroe_blade", "biroe_robe", "biroe_pants", "biroe_boots", "roegeum" })
            {
                var piece = EarlyGameContent.FindItem(id);
                Assert.AreEqual(ItemGrade.Mythic, piece.Grade, id);
                foreach (var other in EarlyGameContent.Items.Where(i => i.Grade == ItemGrade.Mythic && i.Kind == piece.Kind && i.Id != id))
                    Assert.Greater(Score(piece), Score(other), id + " vs " + other.Id);
            }
        }
    }
}