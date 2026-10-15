using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class ItemTests
    {
        private sealed class FixedRandom : IRandom
        {
            private readonly double _value;
            public FixedRandom(double value) { _value = value; }
            public double NextDouble() { return _value; }
        }

        [Test]
        public void ContentReferencesAreValid()
        {
            Assert.AreEqual(EarlyGameContent.Items.Length, EarlyGameContent.Items.Select(i => i.Id).Distinct().Count());
            foreach (var d in EarlyGameContent.Drops)
            {
                Assert.IsNotNull(EarlyGameContent.FindEnemy(d.EnemyId), d.EnemyId);
                Assert.IsNotNull(EarlyGameContent.FindItem(d.ItemId), d.ItemId);
                Assert.That(d.Chance, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f));
            }
            foreach (var r in EarlyGameContent.StageRewards)
            {
                Assert.IsNotNull(EarlyGameContent.FindStage(r.Key));
                foreach (var s in r.Value) Assert.IsNotNull(EarlyGameContent.FindItem(s.Id), s.Id);
            }
            foreach (var s in EarlyGameContent.StartKit) Assert.IsNotNull(EarlyGameContent.FindItem(s.Id), s.Id);
        }

        [Test]
        public void EveryItemDoesSomethingForItsKind()
        {
            foreach (var i in EarlyGameContent.Items)
            {
                switch (i.Kind)
                {
                    case ItemKind.Consumable: Assert.IsTrue(i.HealPct > 0 || i.QiRestore > 0 || i.SpiritGain > 0, i.Id); break;
                    case ItemKind.Weapon:
                    case ItemKind.Armor:
                    case ItemKind.Accessory: Assert.IsTrue(i.AtkPct != 0 || i.DefPct != 0 || i.HpPct != 0 || i.QiPct != 0 || i.CritAdd != 0 || i.SpeedAdd != 0, i.Id); break;
                    case ItemKind.Material: Assert.Greater(i.SellValue, 0, i.Id); break;
                }
                Assert.IsFalse(string.IsNullOrEmpty(i.Description), i.Id);
            }
        }

        [Test]
        public void ConsumablesStackAndGearIsUnique()
        {
            var save = new SaveData();
            Inventory.Add(save, "bear_gall", 2);
            Inventory.Add(save, "bear_gall", 3);
            Assert.AreEqual(5, Inventory.Count(save, "bear_gall"));
            Inventory.Add(save, "jin_biroedo", 1);
            Inventory.Add(save, "jin_biroedo", 1);
            Assert.AreEqual(1, Inventory.Count(save, "jin_biroedo"));
            Assert.IsTrue(Inventory.Remove(save, "bear_gall", 5));
            Assert.AreEqual(0, Inventory.Count(save, "bear_gall"));
            Assert.IsFalse(Inventory.Remove(save, "bear_gall", 1));
        }

        [Test]
        public void WeaponReplacesAndAccessoriesAreLimited()
        {
            var save = new SaveData();
            foreach (var id in new[] { "laundry_bat", "jin_biroedo", "mukryonghwan", "roegeum", "claw_necklace" }) Inventory.Add(save, id, 1);
            string err;
            Assert.IsTrue(Inventory.Equip(save, "laundry_bat", out err));
            Assert.IsTrue(Inventory.Equip(save, "jin_biroedo", out err));
            Assert.IsFalse(Inventory.IsEquipped(save, "laundry_bat"), "the new weapon replaces the old one");
            Assert.IsTrue(Inventory.Equip(save, "mukryonghwan", out err));
            Assert.IsTrue(Inventory.Equip(save, "roegeum", out err));
            Assert.IsFalse(Inventory.Equip(save, "claw_necklace", out err));
            Assert.IsNotNull(err);
            Inventory.Unequip(save, "roegeum");
            Assert.IsTrue(Inventory.Equip(save, "claw_necklace", out err));
        }

        [Test]
        public void CannotEquipWhatYouDoNotOwnOrNonGear()
        {
            var save = new SaveData();
            string err;
            Assert.IsFalse(Inventory.Equip(save, "jin_biroedo", out err));
            Inventory.Add(save, "bear_gall", 1);
            Assert.IsFalse(Inventory.Equip(save, "bear_gall", out err));
        }

        [Test]
        public void RemovingEquippedGearUnequipsIt()
        {
            var save = new SaveData();
            Inventory.Add(save, "jin_biroedo", 1);
            string err;
            Inventory.Equip(save, "jin_biroedo", out err);
            Inventory.Remove(save, "jin_biroedo", 1);
            Assert.IsFalse(Inventory.IsEquipped(save, "jin_biroedo"));
        }

        [Test]
        public void GearBonusesAreApplied()
        {
            var baseStats = new CombatStats { MaxHp = 1000, MaxQi = 100, Attack = 100, Defense = 50, Speed = 100, CritRate = 0.01f, CritDamage = 1.5f };
            var s = Inventory.ApplyGear(baseStats, new[] { "jin_biroedo", "mukryonghwan" });
            Assert.AreEqual(132, s.Attack);   // +22% +10%
            Assert.AreEqual(55, s.Defense);   // +10%
            Assert.AreEqual(94, s.Speed);     // -6
            Assert.AreEqual(0.05f, s.CritRate, 0.0001f);
            Assert.AreEqual(1000, s.MaxHp);
        }

        [Test]
        public void OnlyMaterialsCanBeSold()
        {
            var save = new SaveData();
            Inventory.Add(save, "leather", 3);
            Inventory.Add(save, "bear_gall", 1);
            Assert.AreEqual(0, Inventory.Sell(save, "bear_gall", 1));
            Assert.AreEqual(210, Inventory.Sell(save, "leather", 3));
            Assert.AreEqual(210, save.Doo);
            Assert.AreEqual(0, Inventory.Count(save, "leather"));
        }

        [Test]
        public void StageRewardIsGrantedOnlyOnce()
        {
            var save = new SaveData();
            var first = Inventory.GrantStageReward(save, 5);
            Assert.AreEqual(2, first.Count);
            Assert.AreEqual(1, Inventory.Count(save, "ginseng"));
            Assert.AreEqual(0, Inventory.GrantStageReward(save, 5).Count);
        }

        [Test]
        public void StartKitIsGivenOnce()
        {
            var save = new SaveData();
            Inventory.EnsureStartKit(save);
            Inventory.EnsureStartKit(save);
            Assert.AreEqual(1, Inventory.Count(save, "mukryonghwan"));
            Assert.AreEqual(2, Inventory.Count(save, "bear_gall"));
        }

        [Test]
        public void DropsFollowTheRoll()
        {
            var always = Inventory.RollDrops("white_tiger", new FixedRandom(0.0));
            Assert.IsTrue(always.Any(s => s.Id == "leather" && s.Count == 3));
            var never = Inventory.RollDrops("tiger", new FixedRandom(0.999));
            Assert.AreEqual(0, never.Count);
            Assert.AreEqual(0, Inventory.RollDrops("unknown_enemy", new FixedRandom(0.0)).Count);
        }

        [Test]
        public void SaveDataRoundTripsItemsThroughDefaults()
        {
            var save = new SaveData();
            Assert.IsNotNull(save.Items);
            Assert.IsNotNull(save.EquippedGear);
            Assert.IsNotNull(save.RewardedStages);
        }

        // ---- using items in battle

        private static Combatant Member(string id, int hp, int speed)
        {
            return new Combatant(id, id, Team.Player, new CombatStats { MaxHp = hp, MaxQi = 100, Attack = 1, Defense = 0, Speed = speed, CritDamage = 1.5f }, null);
        }

        private static Combatant Dummy()
        {
            var e = new Combatant("e", "e", Team.Enemy, new CombatStats { MaxHp = 100000, MaxQi = 0, Attack = 1, Defense = 0, Speed = 1, CritDamage = 1.5f }, null);
            e.Moves = new List<EnemyMove> { EnemyMove.Plain };
            return e;
        }

        [Test]
        public void HealingItemRestoresTheMostWoundedAlly()
        {
            var a = Member("a", 1000, 100);
            var b = Member("b", 1000, 1);
            var battle = new InteractiveBattle(new[] { a, b }, new[] { Dummy() }, new SeededRandom(1));
            battle.Begin();
            b.Hp = 100;
            var gall = EarlyGameContent.FindItem("bear_gall");
            var ev = battle.PlayerAct(PlayerCommand.UseItem(gall));
            Assert.AreEqual(500, b.Hp);
            Assert.IsTrue(ev.Any(x => x.Kind == EventKind.Item && x.TargetId == "b" && x.Heal == 400));
        }

        [Test]
        public void LiquorGivesQiAndSpiritToTheUser()
        {
            var a = Member("a", 1000, 100);
            var battle = new InteractiveBattle(a, new[] { Dummy() }, new SeededRandom(1));
            battle.Begin();
            a.Qi = 10; a.Spirit = 0;
            battle.PlayerAct(PlayerCommand.UseItem(EarlyGameContent.FindItem("okruju")));
            Assert.GreaterOrEqual(a.Qi, 70);
            Assert.GreaterOrEqual(a.Spirit, 25);
        }

        [Test]
        public void NonConsumablesCannotBeUsedInBattle()
        {
            var a = Member("a", 1000, 100);
            var battle = new InteractiveBattle(a, new[] { Dummy() }, new SeededRandom(1));
            battle.Begin();
            Assert.Throws<System.InvalidOperationException>(() => battle.PlayerAct(PlayerCommand.UseItem(EarlyGameContent.FindItem("leather"))));
        }
    }
}
