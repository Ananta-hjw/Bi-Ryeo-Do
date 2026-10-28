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
        public void EnhancementSpendsPolishAndDooWithoutConsumingDuplicates()
        {
            var save = new SaveData { Doo = 100000 };
            Inventory.Add(save, "iron_sword", 1);
            Inventory.Add(save, Gear.StoneId, 30);
            string error;
            Assert.IsFalse(Gear.CanEnhance(save, "iron_sword"), "equip a slot first");
            Inventory.Equip(save, "iron_sword", out error);
            long before = save.Doo;
            Assert.IsTrue(Gear.Enhance(save, "iron_sword", out error), error);
            Assert.AreEqual(1, Gear.Level(save, "iron_sword"));
            Assert.AreEqual(1, Inventory.Count(save, "iron_sword"));
            Assert.AreEqual(27, Inventory.Count(save, Gear.StoneId));
            Assert.AreEqual(before - Gear.DooCost(1), save.Doo);
            for (int i = 1; i < 10; i++) Assert.IsTrue(Gear.Enhance(save, "iron_sword", out error), error);
            Assert.AreEqual(10, Gear.Level(save, "iron_sword"));
            Assert.IsFalse(Gear.Enhance(save, "iron_sword", out error), "unduplicated equipment caps at ten");
            Assert.AreEqual(105, Inventory.ApplyGear(Base(), save.EquippedGear, Gear.LevelsFor(save, Inventory.Hero)).Attack);
        }

        [Test]
        public void EnhancementRequiresOwnedEquippedGearAndResources()
        {
            var save = new SaveData();
            string error;
            Assert.IsFalse(Gear.Enhance(save, "iron_sword", out error));
            Inventory.Add(save, "bear_gall", 3);
            Assert.IsFalse(Gear.Enhance(save, "bear_gall", out error));
            Inventory.Add(save, "iron_sword", 1);
            Inventory.Equip(save, "iron_sword", out error);
            Assert.IsFalse(Gear.Enhance(save, "iron_sword", out error));
        }

        [Test]
        public void SlotEnhancementSurvivesRemovalOfLastItemCopy()
        {
            var save = new SaveData { Doo = 100000 };
            Inventory.Add(save, Gear.StoneId, 10);
            Inventory.Add(save, "iron_sword", 1);
            string error;
            Inventory.Equip(save, "iron_sword", out error);
            Assert.IsTrue(Gear.Enhance(save, "iron_sword", out error));
            Inventory.Unequip(save, "iron_sword");
            Inventory.Remove(save, "iron_sword", 1);
            Assert.AreEqual(1, Gear.SlotLevel(save, Inventory.Hero, 0));
            Assert.AreEqual(0, Gear.Stars(save, "iron_sword"));
        }

        [Test]
        public void DismantlingGivesOneToSevenMaterialsOfTheSameGradeAndSlot()
        {
            var save = new SaveData();
            var rng = new SeededRandom(11);
            Inventory.Add(save, "iron_sword", 2);
            string err;
            var got = Salvage.Dismantle(save, "iron_sword", rng, out err);
            Assert.IsNotNull(got, err);
            Assert.AreEqual(1, Inventory.Count(save, "iron_sword"));
            var mat = EarlyGameContent.FindItem(got.Id);
            Assert.AreEqual(ItemKind.Material, mat.Kind);
            Assert.AreEqual(ItemGrade.Normal, mat.Grade);
            Assert.AreEqual(Salvage.IdOf(ItemKind.Weapon, ItemGrade.Normal), mat.Id);
            Assert.That(got.Count, Is.InRange(7, 12));   // 일반: 7..12
            Assert.AreEqual(got.Count, Inventory.Count(save, mat.Id));
            // the last copy can go only when it is not worn
            Inventory.Equip(save, "iron_sword", out err);
            Assert.IsNull(Salvage.Dismantle(save, "iron_sword", rng, out err));
            Inventory.Unequip(save, "iron_sword");
            Assert.IsNotNull(Salvage.Dismantle(save, "iron_sword", rng, out err));
            Assert.AreEqual(0, Inventory.Count(save, "iron_sword"));
            Assert.IsNull(Salvage.Dismantle(save, "jin_biroedo", rng, out err), "초월 gear is never dismantled");
            foreach (var mythic in EarlyGameContent.Items.Where(i => i.IsGear && i.Grade == ItemGrade.Mythic))
            {
                Inventory.Add(save, mythic.Id, 2);
                Assert.IsFalse(Salvage.CanDismantle(save, mythic.Id), mythic.Id + " must not be dismantled");
                Assert.IsNull(Salvage.Dismantle(save, mythic.Id, rng, out err), mythic.Id);
                Assert.AreEqual(2, Inventory.Count(save, mythic.Id));
            }
        }

        [Test]
        public void MaterialCountFollowsTheRequestedRangesAndOdds()
        {
            Assert.AreEqual(new[] { 10, 20, 30, 20, 10, 10 }, Salvage.CountOdds);
            Assert.AreEqual(new[] { 7, 6, 5, 3, 1 }, Salvage.MinCount);
            var grades = new[] { ItemGrade.Normal, ItemGrade.Fine, ItemGrade.Rare, ItemGrade.Epic, ItemGrade.Legendary };
            foreach (var g in grades)
            {
                var rng = new SeededRandom(5);
                int min = Salvage.MinCount[(int)g];
                var seen = new int[6];
                const int n = 20000;
                for (int i = 0; i < n; i++)
                {
                    int c = Salvage.RollCount(g, rng);
                    Assert.That(c, Is.InRange(min, min + 5), g.ToString());
                    seen[c - min]++;
                }
                for (int k = 0; k < 6; k++) Assert.AreEqual(Salvage.CountOdds[k] / 100.0, seen[k] / (double)n, 0.02, g + " count " + (min + k));
                Assert.AreEqual(min + 5, Salvage.MaxCount(g));
            }
        }

        [Test]
        public void EveryGradeAndSlotHasItsOwnNamedMaterial()
        {
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (var m in Salvage.Materials) Assert.IsTrue(names.Add(m.Name), "duplicate name " + m.Name);
            Assert.AreEqual(25, Salvage.Materials.Length);
            foreach (var g in new[] { ItemGrade.Normal, ItemGrade.Fine, ItemGrade.Rare, ItemGrade.Epic, ItemGrade.Legendary })
                foreach (var k in new[] { ItemKind.Weapon, ItemKind.Armor, ItemKind.Pants, ItemKind.Boots, ItemKind.Accessory })
                {
                    var m = EarlyGameContent.FindItem(Salvage.IdOf(k, g));
                    Assert.IsNotNull(m, k + " " + g);
                    Assert.AreEqual(g, m.Grade);
                }
        }

        [Test]
        public void FusingSpendsTheNeededMaterialsForARandomPieceOfTheNextGrade()
        {
            Assert.AreEqual(new[] { 70, 60, 50, 30, 20 }, Salvage.FuseNeed);
            var save = new SaveData();
            string err;
            foreach (var grade in new[] { ItemGrade.Normal, ItemGrade.Fine, ItemGrade.Rare, ItemGrade.Epic, ItemGrade.Legendary })
                foreach (var kind in new[] { ItemKind.Weapon, ItemKind.Armor, ItemKind.Pants, ItemKind.Boots, ItemKind.Accessory })
                {
                    var mat = EarlyGameContent.FindItem(Salvage.IdOf(kind, grade));
                    Assert.IsTrue(Salvage.FuseTargets(mat).Count > 0, kind + " " + grade);
                    Inventory.Add(save, mat.Id, Salvage.Need(mat) - 1);
                    Assert.IsFalse(Salvage.CanFuse(save, mat));
                    Assert.IsNull(Salvage.Fuse(save, mat.Id, new SeededRandom(1), out err));
                    Inventory.Add(save, mat.Id, 1);
                    Assert.IsTrue(Salvage.CanFuse(save, mat));
                    var made = Salvage.Fuse(save, mat.Id, new SeededRandom(2), out err);
                    Assert.IsNotNull(made, err);
                    Assert.AreEqual(grade + 1, made.Grade);
                    Assert.AreEqual(kind, made.Kind);
                    Assert.AreEqual(0, Inventory.Count(save, mat.Id));
                    Assert.AreEqual(1, Inventory.Count(save, made.Id));
                }
            Assert.IsNull(EarlyGameContent.FindItem(Salvage.IdOf(ItemKind.Weapon, ItemGrade.Mythic)));   // nothing above 전설 to fuse from
        }

        [Test]
        public void BulkFusionUsesOnlyTickedGradesAndSlotsAndKeepsTheRemainder()
        {
            var save = new SaveData();
            string weaponN = Salvage.IdOf(ItemKind.Weapon, ItemGrade.Normal), bootsN = Salvage.IdOf(ItemKind.Boots, ItemGrade.Normal);
            string weaponF = Salvage.IdOf(ItemKind.Weapon, ItemGrade.Fine), armorN = Salvage.IdOf(ItemKind.Armor, ItemGrade.Normal);
            Inventory.Add(save, weaponN, 177);   // 2 fusions, 37 left
            Inventory.Add(save, bootsN, 50);     // slot not ticked
            Inventory.Add(save, weaponF, 80);    // grade not ticked
            Inventory.Add(save, armorN, 49);     // not enough for one
            var grades = new[] { true, false, false, false, false };
            var slots = new[] { true, true, false, false, true };

            var plan = Salvage.BulkFusePlan(save, grades, slots);
            Assert.AreEqual(1, plan.Count);
            Assert.AreEqual(weaponN, plan[0].Material.Id);
            Assert.AreEqual(177, plan[0].Have);
            Assert.AreEqual(2, plan[0].Times);
            Assert.AreEqual(140, plan[0].Spent);
            Assert.AreEqual(37, plan[0].Left);

            var made = Salvage.FuseBulk(save, plan, new SeededRandom(3));
            Assert.AreEqual(2, made.Count);
            Assert.IsTrue(made.All(m => m.Grade == ItemGrade.Fine && m.Kind == ItemKind.Weapon));
            Assert.AreEqual(37, Inventory.Count(save, weaponN));
            Assert.AreEqual(50, Inventory.Count(save, bootsN));
            Assert.AreEqual(80, Inventory.Count(save, weaponF));
            Assert.AreEqual(49, Inventory.Count(save, armorN));
            Assert.AreEqual(0, Salvage.BulkFusePlan(save, grades, slots).Count);
        }

        [Test]
        public void ADuplicatePullStaysInTheBagAsASpare()
        {
            var save = new SaveData { Doo = 400 * Gacha.SingleCost };
            var item = Gacha.Pool(ItemGrade.Normal).First(i => i.IsGear);
            Inventory.Add(save, item.Id, 1);
            long doo = save.Doo;
            // pull until that exact piece repeats
            var rng = new SeededRandom(7);
            bool saw = false;
            for (int i = 0; i < 400 && !saw; i++)
            {
                var r = Gacha.Pull(save, 1, rng);
                if (r[0].Item.Id == item.Id) { saw = true; Assert.IsTrue(r[0].Duplicate); }
            }
            Assert.IsTrue(saw);
            Assert.GreaterOrEqual(Inventory.Count(save, item.Id), 2);
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
            Inventory.Add(save, Gear.StoneId, 20);
            Inventory.Add(save, "iron_sword", 1);
            string err;
            Inventory.Equip(save, "iron_sword", out err);
            int before = int.Parse(StatusSheet.Hero(save)[2].Value.Replace(",", ""));
            Inventory.Add(save, "iron_sword", 10);
            for (int i = 0; i < 10; i++) Gear.Enhance(save, "iron_sword", out err);
            int after = int.Parse(StatusSheet.Hero(save)[2].Value.Replace(",", ""));
            Assert.Greater(after, before);
        }

        [TestCase(ItemGrade.Normal, 20)]
        [TestCase(ItemGrade.Fine, 17)]
        [TestCase(ItemGrade.Rare, 15)]
        [TestCase(ItemGrade.Epic, 12)]
        [TestCase(ItemGrade.Legendary, 8)]
        [TestCase(ItemGrade.Mythic, 6)]
        public void EverySlotHasTheRequestedNumberOfPullableItems(ItemGrade grade, int target)
        {
            foreach (var kind in new[] { ItemKind.Weapon, ItemKind.Armor, ItemKind.Pants, ItemKind.Boots, ItemKind.Accessory })
                Assert.AreEqual(target, Gacha.Pool(grade).Count(i => i.Kind == kind), grade + " " + kind);
            Assert.AreEqual(target * 5, Gacha.Pool(grade).Count(i => i.IsGear));
            Assert.AreEqual(162, GearCatalogExpansion.All.Length);
            Assert.AreEqual(1, EarlyGameContent.Items.Count(i => i.IsGear && i.Grade == ItemGrade.Transcendent));
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

        [Test]
        public void SetStatusListsReachedAndNextTiers()
        {
            var status = Gear.SetStatus(new[] { "biroe_blade", "biroe_robe" });
            Assert.AreEqual(1, status.Count);
            StringAssert.Contains("비뢰 세트  2 / 7", status[0][0]);
            StringAssert.Contains("2개:", status[0][1]);
            StringAssert.Contains("다음 3개:", status[0][2]);
            Assert.AreEqual(0, Gear.SetStatus(new string[0]).Count);
        }

        [Test]
        public void TheTranscendentSwordBeatsTheBestMythicWeaponOnEveryStat()
        {
            var jin = EarlyGameContent.FindItem("jin_biroedo");
            var blade = EarlyGameContent.FindItem("biroe_blade");
            Assert.Greater(jin.AtkPct, blade.AtkPct);
            Assert.Greater(jin.CritAdd, blade.CritAdd);
            Assert.Greater(jin.SpeedAdd, blade.SpeedAdd);
            Assert.Greater(jin.HpPct, blade.HpPct);
        }

        [Test]
        public void BulkDismantleTakesOnlyTheTickedGradesAndSlots()
        {
            var save = new SaveData();
            Inventory.Add(save, "iron_sword", 4);          // 일반 무기: 3 spares
            Inventory.Add(save, "cloth_robe", 2);          // 일반 상의: 1 spare
            Inventory.Add(save, "cheonggang_sword", 3);    // 고급 무기: 2 spares
            Inventory.Add(save, "taegeuk_jade", 5);        // 신화 장신구: never
            Inventory.Add(save, "oak_club", 1);            // 일반 무기, only copy
            var all = new[] { true, true, true, true, true };
            var onlyNormal = new[] { true, false, false, false, false };
            var onlyWeapon = new[] { true, false, false, false, false };

            var plan = Salvage.BulkPlan(save, onlyNormal, onlyWeapon, false);
            Assert.AreEqual(1, plan.Count);
            Assert.AreEqual("iron_sword", plan[0].Item.Id);
            Assert.AreEqual(3, plan[0].Count);

            var everything = Salvage.BulkPlan(save, all, all, false);
            Assert.AreEqual(3, everything.Count);   // iron_sword, cloth_robe, cheonggang_sword; no mythic, no lone copy
            Assert.IsFalse(everything.Any(e => e.Item.Grade == ItemGrade.Mythic));

            var withSingles = Salvage.BulkPlan(save, onlyNormal, onlyWeapon, true);
            Assert.AreEqual(2, withSingles.Count);   // oak_club joins; its only copy is not worn
            Assert.AreEqual(1, withSingles.First(e => e.Item.Id == "oak_club").Count);
            Inventory.Equip(save, "oak_club", out string err);
            Assert.IsFalse(Salvage.BulkPlan(save, onlyNormal, onlyWeapon, true).Any(e => e.Item.Id == "oak_club"), "a worn piece stays");

            var yield = Salvage.BulkYield(plan);
            Assert.AreEqual(2, yield.Count);
            Assert.AreEqual(7 * 3, yield[0].Value[0]);
            Assert.AreEqual(12 * 3, yield[0].Value[1]);
            Assert.AreEqual(Gear.StoneId, yield[1].Key.Id);
            CollectionAssert.AreEqual(new[] { 3, 21 }, yield[1].Value);

            var gained = Salvage.DismantleBulk(save, plan, new SeededRandom(3));
            Assert.AreEqual(1, Inventory.Count(save, "iron_sword"));
            Assert.AreEqual(2, gained.Count);
            Assert.That(gained[0].Count, Is.InRange(21, 36));
            Assert.That(gained[1].Count, Is.InRange(3, 21));
            Assert.AreEqual(gained[1].Count, Inventory.Count(save, Gear.StoneId));
            Assert.AreEqual(gained[0].Count, Inventory.Count(save, gained[0].Id));
            Assert.AreEqual(5, Inventory.Count(save, "taegeuk_jade"));
        }
    }
}