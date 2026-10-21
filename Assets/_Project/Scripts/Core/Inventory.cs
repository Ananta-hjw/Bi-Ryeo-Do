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
            if (item.Grade == ItemGrade.Transcendent) Gear.GrantTranscendentStars(save, id);
            foreach (var s in save.Items)
            {
                if (s.Id != id) continue;
                if (!IsUnique(item)) s.Count += count;   // spare copies of pullable gear stack (see Gear.Breakthrough / Salvage)
                return;
            }
            save.Items.Add(new ItemStack(id, IsUnique(item) ? 1 : count));
        }

        /// <summary>Key items and 초월 gear (earned once) never stack.</summary>
        private static bool IsUnique(ItemData item) { return item.Kind == ItemKind.Key || item.Grade == ItemGrade.Transcendent; }

        public static bool Remove(SaveData save, string id, int count)
        {
            Gear.EnsureProgression(save);
            for (int i = 0; i < save.Items.Count; i++)
            {
                var s = save.Items[i];
                if (s.Id != id) continue;
                if (s.Count < count) return false;
                s.Count -= count;
                if (s.Count == 0)
                {
                    save.Items.RemoveAt(i);
                    Gear.ForgetLevel(save, id);
                }
                TrimWorn(save, id);
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
            long gold = Income.Boost(item.SellValue) * count;
            save.Doo += gold;
            return gold;
        }

        /// <summary>The protagonist's gear.</summary>
        public const string Hero = "player";

        /// <summary>The gear list of the protagonist ("player") or of a companion (created on demand).</summary>
        public static List<string> GearOf(SaveData save, string owner)
        {
            if (string.IsNullOrEmpty(owner) || owner == Hero) return save.EquippedGear;
            foreach (var e in save.CompanionGear) if (e.Id == owner) return e.Items;
            var made = new CompanionGearEntry { Id = owner };
            save.CompanionGear.Add(made);
            return made.Items;
        }

        /// <summary>How many people (protagonist and companions) currently wear this piece. Each needs its own copy.</summary>
        public static int Worn(SaveData save, string id)
        {
            int n = save.EquippedGear.Contains(id) ? 1 : 0;
            foreach (var e in save.CompanionGear) if (e.Items.Contains(id)) n++;
            return n;
        }

        /// <summary>Copies that nobody wears.</summary>
        public static int Free(SaveData save, string id) { return Math.Max(0, Count(save, id) - Worn(save, id)); }

        /// <summary>Names of the companions wearing a piece, for the bag screen.</summary>
        public static List<string> WearingCompanions(SaveData save, string id)
        {
            var names = new List<string>();
            foreach (var e in save.CompanionGear)
            {
                if (!e.Items.Contains(id)) continue;
                var c = EarlyGameContent.FindCompanion(e.Id);
                names.Add(c != null ? c.Name : e.Id);
            }
            return names;
        }

        /// <summary>If copies were used up while more people wear the piece than copies remain, companions give it back.</summary>
        private static void TrimWorn(SaveData save, string id)
        {
            int count = Count(save, id);
            while (Worn(save, id) > count)
            {
                bool removed = false;
                foreach (var e in save.CompanionGear) if (e.Items.Remove(id)) { removed = true; break; }
                if (!removed) { save.EquippedGear.Remove(id); }
            }
        }

        public static bool IsEquipped(SaveData save, string id) { return save.EquippedGear.Contains(id); }

        /// <summary>Equips owned gear: one weapon, one 상의, one 하의, one 신발, <see cref="AccessorySlots"/> accessories. A new weapon replaces the old one.</summary>
        public static bool Equip(SaveData save, string id, out string error) { return EquipFor(save, Hero, id, out error); }

        /// <summary>Equips owned gear on the protagonist ("player") or a companion; the bag is shared, so every wearer needs a copy of their own.</summary>
        public static bool EquipFor(SaveData save, string owner, string id, out string error)
        {
            error = null;
            var item = EarlyGameContent.FindItem(id);
            if (item == null || !item.IsGear) { error = "장비할 수 없는 물건입니다."; return false; }
            if (Count(save, id) == 0) { error = "가지고 있지 않습니다."; return false; }
            if (owner != Hero && !string.IsNullOrEmpty(owner) && Gear.IsHeroOnly(id)) { error = item.Name + "은(는) " + Gear.HeroOnlyLabel + " 장비입니다."; return false; }
            Gear.SyncSlots(save, owner);
            var gear = GearOf(save, owner);
            if (gear.Contains(id)) return true;
            if (Free(save, id) <= 0)
            {
                var others = WearingCompanions(save, id);
                if (gear != save.EquippedGear && save.EquippedGear.Contains(id)) others.Insert(0, "비류연");
                error = string.Join(", ", others) + "이(가) 착용 중입니다. 같은 장비가 하나 더 있거나 먼저 해제해야 합니다.";
                return false;
            }

            if (item.Kind != ItemKind.Accessory)
            {
                // one weapon, one 상의, one 하의, one 신발: a new piece replaces the old one of the same kind
                foreach (var other in new List<string>(gear))
                {
                    var o = EarlyGameContent.FindItem(other);
                    if (o != null && o.Kind == item.Kind) gear.Remove(other);
                }
            }
            else
            {
                int worn = 0;
                foreach (var other in gear)
                {
                    var o = EarlyGameContent.FindItem(other);
                    if (o != null && o.Kind == ItemKind.Accessory) worn++;
                }
                if (worn >= AccessorySlots) { error = "장신구는 " + AccessorySlots + "개까지 착용할 수 있습니다."; return false; }
            }
            gear.Add(id);
            Gear.SyncSlots(save, owner);
            return true;
        }

        /// <summary>Returns the item in a kind-specific slot; accessories use indices 0 and 1.</summary>
        public static ItemData InSlot(SaveData save, string owner, ItemKind kind, int slot = 0)
        {
            if (slot < 0 || slot >= (kind == ItemKind.Accessory ? AccessorySlots : 1)) return null;
            return Gear.ItemInSlot(save, owner, Gear.SlotIndex(kind, slot));
        }

        /// <summary>Replaces exactly the selected slot, restoring the original loadout on failure.</summary>
        public static bool EquipInSlot(SaveData save, string owner, ItemKind kind, int slot, string id, out string error)
        {
            var item = EarlyGameContent.FindItem(id);
            if (item == null || !item.IsGear || item.Kind != kind || slot < 0 || slot >= (kind == ItemKind.Accessory ? AccessorySlots : 1))
            {
                error = "선택한 부위에 장착할 수 없는 장비입니다.";
                return false;
            }
            var gear = GearOf(save, owner);
            var previous = InSlot(save, owner, kind, slot);
            if (previous != null && previous.Id == id) { error = null; return true; }
            if (gear.Contains(id)) { error = "다른 장신구 칸에 이미 착용 중입니다."; return false; }
            var original = new List<string>(gear);
            var assignments = new string[6];
            for (int i = 0; i < 6; i++) assignments[i] = Gear.ItemInSlot(save, owner, i)?.Id;
            int position = previous != null ? gear.IndexOf(previous.Id) : gear.Count;
            if (previous != null) gear.Remove(previous.Id);
            if (!EquipFor(save, owner, id, out error))
            {
                gear.Clear();
                gear.AddRange(original);
                for (int i = 0; i < 6; i++) Gear.AssignSlot(save, owner, i, assignments[i]);
                return false;
            }
            // Keep the other accessory in its original slot when replacing the first one.
            gear.Remove(id);
            gear.Insert(Math.Min(position, gear.Count), id);
            Gear.AssignSlot(save, owner, Gear.SlotIndex(kind, slot), id);
            return true;
        }

        public static void Unequip(SaveData save, string id) { UnequipFor(save, Hero, id); }

        public static void UnequipFor(SaveData save, string owner, string id) { Gear.SyncSlots(save, owner); GearOf(save, owner).Remove(id); Gear.SyncSlots(save, owner); }

        public static CombatStats ApplyGear(CombatStats stats, IEnumerable<string> equipped, IList<GearLevel> levels = null)
        {
            float atk = 0f, def = 0f, hp = 0f, qi = 0f, crit = 0f, critDmg = 0f;
            float speed = 0f;
            int startSpirit = 0;
            var worn = new List<string>();
            foreach (var id in equipped)
            {
                var item = EarlyGameContent.FindItem(id);
                if (item == null || !item.IsGear) continue;
                worn.Add(id);
                int stars = 0;
                if (levels != null) foreach (var entry in levels) if (entry.Id == id) { stars = entry.Stars; break; }
                var effects = Gear.EffectsFor(item, Gear.LevelOf(levels, id), stars);
                atk += effects.AtkPct; def += effects.DefPct; hp += effects.HpPct; qi += effects.QiPct;
                crit += effects.CritAdd; critDmg += effects.CritDmgAdd; speed += effects.SpeedAdd;
                startSpirit += effects.StartSpirit;
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
            stats.CritDamage += critDmg;
            stats.StartSpirit += startSpirit;
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
