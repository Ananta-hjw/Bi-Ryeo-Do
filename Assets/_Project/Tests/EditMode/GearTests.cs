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
        public void EnhancingMeltsASpareCopyAndRaisesStats()
        {
            var save = new SaveData { Doo = 0 };
            Inventory.Add(save, "iron_sword", 1);
            string err;
            Assert.IsFalse(Gear.Enhance(save, "iron_sword", out err), "a lone copy cannot be enhanced");
            Assert.IsFalse(Gear.CanEnhance(save, "iron_sword"));
            Inventory.Add(save, "iron_sword", 1);   // a duplicate from the 뽑기 stays as a spare
            Assert.AreEqual(2, Inventory.Count(save, "iron_sword"));
            Assert.IsTrue(Gear.Enhance(save, "iron_sword", out err));
            Assert.AreEqual(1, Gear.Level(save, "iron_sword"));
            Assert.AreEqual(1, Inventory.Count(save, "iron_sword"));   // the spare was used up
            Assert.AreEqual(0, save.Doo);                              // no 두 involved
            var plain = Inventory.ApplyGear(Base(), new[] { "iron_sword" });
            var plus = Inventory.ApplyGear(Base(), new[] { "iron_sword" }, save.GearLevels);
            Assert.AreEqual(104, plain.Attack);
            Assert.AreEqual(104, plus.Attack);   // +4% x1.1 = 4.4% rounds to 104
            Inventory.Add(save, "iron_sword", 9);
            for (int i = 0; i < 9; i++) Assert.IsTrue(Gear.Enhance(save, "iron_sword", out err));
            Assert.AreEqual(Gear.MaxLevel, Gear.Level(save, "iron_sword"));
            Assert.AreEqual(108, Inventory.ApplyGear(Base(), new[] { "iron_sword" }, save.GearLevels).Attack);   // doubled
            Assert.IsFalse(Gear.Enhance(save, "iron_sword", out err));   // max level
        }

        [Test]
        public void EnhanceNeedsAnOwnedSpareGearPiece()
        {
            var save = new SaveData();
            string err;
            Assert.IsFalse(Gear.Enhance(save, "iron_sword", out err));   // not owned
            Inventory.Add(save, "bear_gall", 3);
            Assert.IsFalse(Gear.Enhance(save, "bear_gall", out err));    // not gear
        }

        [Test]
        public void TheLevelIsForgottenWhenTheLastCopyIsGone()
        {
            var save = new SaveData();
            Inventory.Add(save, "iron_sword", 2);
            string err;
            Assert.IsTrue(Gear.Enhance(save, "iron_sword", out err));
            Inventory.Remove(save, "iron_sword", 1);
            Assert.AreEqual(0, Gear.Level(save, "iron_sword"));
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
            Assert.AreEqual(new[] { 50, 40, 30, 20, 10 }, Salvage.FuseNeed);
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
        public void ADuplicatePullStaysInTheBagAsASpare()
        {
            var save = new SaveData { Doo = 100000 };
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
            Inventory.Add(save, "iron_sword", 1);
            string err;
            Inventory.Equip(save, "iron_sword", out err);
            int before = int.Parse(StatusSheet.Hero(save)[2].Value.Replace(",", ""));
            Inventory.Add(save, "iron_sword", 10);
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
    }
}