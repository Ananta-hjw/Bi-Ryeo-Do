using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>Bag, equipment, drops and first-clear rewards, all operating on the persisted <see cref="SaveData"/>.</summary>
    public static class Inventory
    {
        public const int AccessorySlots = 2;

        public static int Count(SaveData save, string id)
        {
            foreach (var s in save.Items) if (s.Id == id) return s.Count;
            return 0;
        }

        public static void Add(SaveData save, string id, int count)
        {
            if (count <= 0) return;
            var item = EarlyGameContent.FindItem(id);
            if (item == null) return;
            Codex.SeeItem(save, id);
            foreach (var s in save.Items)
            {
                if (s.Id != id) continue;
                if (!item.IsGear && item.Kind != ItemKind.Key) s.Count += count; // gear and key items are unique
                return;
            }
            save.Items.Add(new ItemStack(id, item.IsGear || item.Kind == ItemKind.Key ? 1 : count));
        }

        public static bool Remove(SaveData save, string id, int count)
        {
            for (int i = 0; i < save.Items.Count; i++)
            {
                var s = save.Items[i];
                if (s.Id != id) continue;
                if (s.Count < count) return false;
                s.Count -= count;
                if (s.Count == 0)
                {
                    save.Items.RemoveAt(i);
                    save.EquippedGear.Remove(id);
                }
                return true;
            }
            return false;
        }

        /// <summary>Sells materials for 두. Returns the 두 gained (0 if the item cannot be sold).</summary>
        public static long Sell(SaveData save, string id, int count)
        {
            var item = EarlyGameContent.FindItem(id);
            if (item == null || item.Kind != ItemKind.Material || item.SellValue <= 0) return 0;
            count = Math.Min(count, Count(save, id));
            if (count <= 0 || !Remove(save, id, count)) return 0;
            long gold = item.SellValue * count;
            save.Doo += gold;
            return gold;
        }

        public static bool IsEquipped(SaveData save, string id) { return save.EquippedGear.Contains(id); }

        /// <summary>Equips owned gear: one weapon, one armor, <see cref="AccessorySlots"/> accessories. A new weapon replaces the old one.</summary>
        public static bool Equip(SaveData save, string id, out string error)
        {
            error = null;
            var item = EarlyGameContent.FindItem(id);
            if (item == null || !item.IsGear) { error = "장비할 수 없는 물건입니다."; return false; }
            if (Count(save, id) == 0) { error = "가지고 있지 않습니다."; return false; }
            if (IsEquipped(save, id)) return true;

            if (item.Kind == ItemKind.Weapon || item.Kind == ItemKind.Armor)
            {
                // one weapon and one armor: a new piece replaces the old one of the same kind
                foreach (var other in new List<string>(save.EquippedGear))
                {
                    var o = EarlyGameContent.FindItem(other);
                    if (o != null && o.Kind == item.Kind) save.EquippedGear.Remove(other);
                }
            }
            else
            {
                int worn = 0;
                foreach (var other in save.EquippedGear)
                {
                    var o = EarlyGameContent.FindItem(other);
                    if (o != null && o.Kind == ItemKind.Accessory) worn++;
                }
                if (worn >= AccessorySlots) { error = "장신구는 " + AccessorySlots + "개까지 착용할 수 있습니다."; return false; }
            }
            save.EquippedGear.Add(id);
            return true;
        }

        public static void Unequip(SaveData save, string id) { save.EquippedGear.Remove(id); }

        public static CombatStats ApplyGear(CombatStats stats, IEnumerable<string> equipped, IList<GearLevel> levels = null)
        {
            float atk = 0f, def = 0f, hp = 0f, qi = 0f, crit = 0f;
            float speed = 0f;
            var worn = new List<string>();
            foreach (var id in equipped)
            {
                var item = EarlyGameContent.FindItem(id);
                if (item == null || !item.IsGear) continue;
                worn.Add(id);
                float m = Gear.Multiplier(Gear.LevelOf(levels, id));
                atk += item.AtkPct * m; def += item.DefPct * m; hp += item.HpPct * m; qi += item.QiPct * m; crit += item.CritAdd * m; speed += item.SpeedAdd * m;
            }
            foreach (var set in Gear.Sets)
            {
                int n = Gear.WornPieces(set, worn);
                if (n < 2) continue;
                var b = Gear.ActiveBonus(set, n);
                atk += b.AtkPct; def += b.DefPct; hp += b.HpPct; qi += b.QiPct; crit += b.CritAdd; speed += b.SpeedAdd;
            }
            stats.MaxHp = (int)Math.Round(stats.MaxHp * (1f + hp));
            stats.MaxQi = (int)Math.Round(stats.MaxQi * (1f + qi));
            stats.Attack = (int)Math.Round(stats.Attack * (1f + atk));
            stats.Defense = (int)Math.Round(stats.Defense * (1f + def));
            stats.Speed = Math.Max(1, stats.Speed + (int)Math.Round(speed));
            stats.CritRate = Math.Max(0f, Math.Min(1f, stats.CritRate + crit));
            return stats;
        }

        /// <summary>Rolls the drop table for one beaten enemy.</summary>
        public static List<ItemStack> RollDrops(string enemyId, IRandom rng, float chanceMultiplier = 1f)
        {
            var result = new List<ItemStack>();
            foreach (var d in EarlyGameContent.Drops)
            {
                if (d.EnemyId != enemyId) continue;
                if (rng.NextDouble() < d.Chance * chanceMultiplier) result.Add(new ItemStack(d.ItemId, d.Count));
            }
            return result;
        }

        /// <summary>Gives the one-time reward for first clearing a stage. Returns what was granted (empty if already claimed).</summary>
        public static List<ItemStack> GrantStageReward(SaveData save, int stageNumber)
        {
            var granted = new List<ItemStack>();
            if (save.RewardedStages.Contains(stageNumber)) return granted;
            save.RewardedStages.Add(stageNumber);
            foreach (var r in EarlyGameContent.StageRewards)
            {
                if (r.Key != stageNumber) continue;
                foreach (var stack in r.Value)
                {
                    Add(save, stack.Id, stack.Count);
                    granted.Add(new ItemStack(stack.Id, stack.Count));
                }
            }
            return granted;
        }

        public static void EnsureStartKit(SaveData save)
        {
            if (save.StartKitGiven) return;
            save.StartKitGiven = true;
            foreach (var stack in EarlyGameContent.StartKit) Add(save, stack.Id, stack.Count);
            EquipStartLoadout(save);
        }

        /// <summary>A new game begins wearing the starting gear and with the free 기초 호흡법 equipped.</summary>
        public static void EquipStartLoadout(SaveData save)
        {
            string error;
            foreach (var stack in EarlyGameContent.StartKit)
            {
                var item = EarlyGameContent.FindItem(stack.Id);
                if (item != null && item.IsGear) Equip(save, stack.Id, out error);
            }
            if (!InnerArts.IsLearned(save, "breath")) save.LearnedInner.Add("breath");
            InnerArts.Equip(save, "breath", out error);
        }

        public static string Describe(ItemStack stack)
        {
            var item = EarlyGameContent.FindItem(stack.Id);
            return item == null ? stack.Id : item.Name + (stack.Count > 1 ? " x" + stack.Count : "");
        }
    }
}
