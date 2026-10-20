using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    [Serializable]
    public sealed class GearSlot
    {
        public string Owner;
        public int Index;
        public string ItemId;
        public int Level;
    }

    public static partial class Gear
    {
        public const int MaxLevel = 100, MaxStars = 5;
        public const int BattlePolish = 2, DailyPolish = 5, StarterPolish = 30;
        public const string StoneId = "gear_polish";
        public static readonly int[] LevelCaps = { 10, 20, 30, 50, 70, 100 };
        private static readonly float[] StarBonuses = { 0f, .05f, .10f, .15f, .20f, .30f };
        public static readonly ItemData Polish = new ItemData { Id = StoneId, Name = "연마석", Kind = ItemKind.Material, Grade = ItemGrade.Normal,
            Description = "장착 부위를 확정 강화하는 공용 재료. 전투 승리·일일 임무·장비 분해로 얻는다. 장비를 바꿔도 부위 강화는 유지된다." };

        public static ItemKind KindForSlot(int index)
        {
            switch (index)
            {
                case 0: return ItemKind.Weapon;
                case 1: return ItemKind.Armor;
                case 2: return ItemKind.Pants;
                case 3: return ItemKind.Boots;
                default: return ItemKind.Accessory;
            }
        }

        public static int SlotIndex(ItemKind kind, int accessory = 0)
        {
            switch (kind)
            {
                case ItemKind.Weapon: return 0;
                case ItemKind.Armor: return 1;
                case ItemKind.Pants: return 2;
                case ItemKind.Boots: return 3;
                case ItemKind.Accessory: return 4 + accessory;
                default: return -1;
            }
        }

        private static string OwnerId(string owner) { return string.IsNullOrEmpty(owner) ? Inventory.Hero : owner; }
        private static GearSlot Entry(SaveData save, string owner, int index)
        {
            owner = OwnerId(owner);
            foreach (var entry in save.GearSlots) if (entry.Owner == owner && entry.Index == index) return entry;
            var made = new GearSlot { Owner = owner, Index = index };
            save.GearSlots.Add(made);
            return made;
        }

        /// <summary>Converts consumed duplicate copies to stars and preserves old enhancement investment once.</summary>
        public static void EnsureProgression(SaveData save)
        {
            if (save.Items == null) save.Items = new List<ItemStack>();
            if (save.EquippedGear == null) save.EquippedGear = new List<string>();
            if (save.CompanionGear == null) save.CompanionGear = new List<CompanionGearEntry>();
            if (save.GearSlots == null) save.GearSlots = new List<GearSlot>();
            if (save.GearBreakthroughs == null) save.GearBreakthroughs = new List<GearLevel>();
            if (save.GearLevels == null) save.GearLevels = new List<GearLevel>();
            if (Inventory.Count(save, Ritual.RewardId) > 0) GrantTranscendentStarsRaw(save, Ritual.RewardId);
            if (save.GearProgressionMigrated) return;
            save.GearProgressionMigrated = true;
            SyncSlots(save, Inventory.Hero);
            foreach (var owner in new List<CompanionGearEntry>(save.CompanionGear)) SyncSlots(save, owner.Id);
            foreach (var legacy in save.GearLevels)
            {
                var item = EarlyGameContent.FindItem(legacy.Id);
                if (item == null || !item.IsGear || Inventory.Count(save, legacy.Id) <= 0 || legacy.Level <= 0) continue;
                int old = Math.Min(10, legacy.Level);
                if (item.Grade == ItemGrade.Transcendent) GrantTranscendentStarsRaw(save, legacy.Id);
                else save.GearBreakthroughs.Add(new GearLevel { Id = legacy.Id, Level = Math.Min(MaxStars, old) });
                string owner = HolderOf(save, legacy.Id) ?? Inventory.Hero;
                int index = FindItemSlot(save, owner, legacy.Id);
                if (index < 0) index = SlotIndex(item.Kind);
                var slot = Entry(save, owner, index);
                int converted = 0;
                while (converted < MaxLevel && Multiplier(converted) + .00001f < 1f + old * .10f) converted++;
                slot.Level = Math.Max(slot.Level, converted);
                // More than five consumed copies cannot become stars; return those copies.
                if (old > MaxStars) Inventory.Add(save, legacy.Id, old - MaxStars);
            }
            save.GearLevels.Clear();
        }

        /// <summary>Keeps accessory identities stable even if the other accessory is removed.</summary>
        public static void SyncSlots(SaveData save, string owner)
        {
            EnsureProgression(save);
            owner = OwnerId(owner);
            var worn = Inventory.GearOf(save, owner);
            for (int i = 0; i < 6; i++)
            {
                var entry = Entry(save, owner, i);
                var item = EarlyGameContent.FindItem(entry.ItemId);
                if (item == null || item.Kind != KindForSlot(i) || !worn.Contains(entry.ItemId)) entry.ItemId = null;
            }
            foreach (var id in worn)
            {
                var item = EarlyGameContent.FindItem(id);
                if (item == null || !item.IsGear || FindItemSlot(save, owner, id) >= 0) continue;
                int index = SlotIndex(item.Kind);
                if (index == 4 && !string.IsNullOrEmpty(Entry(save, owner, 4).ItemId)) index = 5;
                Entry(save, owner, index).ItemId = id;
            }
        }

        private static int FindItemSlot(SaveData save, string owner, string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            owner = OwnerId(owner);
            foreach (var slot in save.GearSlots) if (slot.Owner == owner && slot.ItemId == id) return slot.Index;
            return -1;
        }

        public static void AssignSlot(SaveData save, string owner, int index, string id)
        {
            EnsureProgression(save);
            owner = OwnerId(owner);
            foreach (var slot in save.GearSlots) if (slot.Owner == owner && slot.ItemId == id) slot.ItemId = null;
            Entry(save, owner, index).ItemId = id;
        }

        public static ItemData ItemInSlot(SaveData save, string owner, int index)
        {
            if (index < 0 || index >= 6) return null;
            SyncSlots(save, owner);
            return EarlyGameContent.FindItem(Entry(save, owner, index).ItemId);
        }

        public static int SlotLevel(SaveData save, string owner, int index)
        {
            EnsureProgression(save);
            if (index < 0 || index >= 6) return 0;
            return Math.Max(0, Math.Min(MaxLevel, Entry(save, owner, index).Level));
        }

        public static int Stars(SaveData save, string id)
        {
            EnsureProgression(save);
            return Math.Max(0, Math.Min(MaxStars, LevelOf(save.GearBreakthroughs, id)));
        }

        private static void GrantTranscendentStarsRaw(SaveData save, string id)
        {
            foreach (var entry in save.GearBreakthroughs) if (entry.Id == id) { entry.Level = MaxStars; return; }
            save.GearBreakthroughs.Add(new GearLevel { Id = id, Level = MaxStars });
        }

        public static void GrantTranscendentStars(SaveData save, string id)
        {
            EnsureProgression(save);
            GrantTranscendentStarsRaw(save, id);
        }

        public static int Cap(SaveData save, string id) { return LevelCaps[Stars(save, id)]; }
        public static float StarBonus(int stars) { return StarBonuses[Math.Max(0, Math.Min(MaxStars, stars))]; }
        public static string StarsText(SaveData save, string id) { int n = Stars(save, id); return n == 0 ? "미돌파" : n + "성"; }

        public static float Multiplier(int level, int stars = 0)
        {
            int n = Math.Max(0, Math.Min(MaxLevel, level));
            return 1f + Math.Min(n, 20) * .03f + Math.Max(0, Math.Min(n, 30) - 20) * .04f
                + Math.Max(0, Math.Min(n, 50) - 30) * .05f + Math.Max(0, Math.Min(n, 70) - 50) * .07f
                + Math.Max(0, Math.Min(n, 90) - 70) * .10f + Math.Max(0, n - 90) * .15f + StarBonus(stars);
        }

        /// <summary>Transcendent definitions already describe their innate five-star effects.</summary>
        public static float EffectMultiplier(ItemData item, int level, int stars)
        {
            if (item.Grade != ItemGrade.Transcendent) return Multiplier(level, stars);
            // Normalize five-star reference values so the innate bonus is not applied twice.
            return Multiplier(level, MaxStars) / Multiplier(0, MaxStars);
        }

        public static string HolderOf(SaveData save, string id)
        {
            if (save.EquippedGear.Contains(id)) return Inventory.Hero;
            foreach (var entry in save.CompanionGear) if (entry.Items.Contains(id)) return entry.Id;
            return null;
        }

        public static int Level(SaveData save, string id) { return LevelFor(save, HolderOf(save, id), id); }
        public static int LevelFor(SaveData save, string owner, string id)
        {
            SyncSlots(save, owner);
            int index = FindItemSlot(save, owner, id);
            return index < 0 ? 0 : Math.Min(SlotLevel(save, owner, index), Cap(save, id));
        }

        public static List<GearLevel> LevelsFor(SaveData save, string owner)
        {
            SyncSlots(save, owner);
            var list = new List<GearLevel>();
            foreach (var id in Inventory.GearOf(save, owner))
                list.Add(new GearLevel { Id = id, Level = LevelFor(save, owner, id), Stars = Stars(save, id) });
            return list;
        }

        public static int Spares(SaveData save, string id) { return Math.Max(0, Inventory.Count(save, id) - Math.Max(1, Inventory.Worn(save, id))); }
        public static bool CanBreakthrough(SaveData save, string id)
        {
            var item = EarlyGameContent.FindItem(id);
            return item != null && item.IsGear && Stars(save, id) < MaxStars && Spares(save, id) > 0;
        }

        public static bool Breakthrough(SaveData save, string id, out string error)
        {
            EnsureProgression(save);
            error = null;
            if (!CanBreakthrough(save, id)) { error = Stars(save, id) >= MaxStars ? "이미 최대 5성입니다." : "한계돌파하려면 착용할 장비 외에 같은 장비 여분 1개가 필요합니다."; return false; }
            int next = Stars(save, id) + 1;
            if (!Inventory.Remove(save, id, 1)) { error = "장비 여분이 부족합니다."; return false; }
            foreach (var entry in save.GearBreakthroughs) if (entry.Id == id) { entry.Level = next; return true; }
            save.GearBreakthroughs.Add(new GearLevel { Id = id, Level = next });
            return true;
        }

        public static int StoneCost(int nextLevel) { return 1 + (Math.Max(1, Math.Min(MaxLevel, nextLevel)) - 1) / 10; }
        public static long DooCost(int nextLevel) { long n = Math.Max(1, Math.Min(MaxLevel, nextLevel)); return 100 + 20 * n + 2 * n * n; }
        public static bool CanEnhanceSlot(SaveData save, string owner, int index)
        {
            var item = ItemInSlot(save, owner, index);
            int current = SlotLevel(save, owner, index);
            return item != null && current < Cap(save, item.Id) && Inventory.Count(save, StoneId) >= StoneCost(current + 1) && save.Doo >= DooCost(current + 1);
        }

        public static bool EnhanceSlot(SaveData save, string owner, int index, out string error)
        {
            var item = ItemInSlot(save, owner, index);
            int current = SlotLevel(save, owner, index);
            error = null;
            if (item == null) { error = "해당 부위에 장비를 먼저 장착합니다."; return false; }
            if (current >= Cap(save, item.Id)) { error = "현재 장비의 강화 상한입니다. 한계돌파하면 상한이 늘어납니다."; return false; }
            int stones = StoneCost(current + 1);
            long doo = DooCost(current + 1);
            if (Inventory.Count(save, StoneId) < stones || save.Doo < doo) { error = "강화에 필요한 연마석 또는 두가 부족합니다."; return false; }
            if (!Inventory.Remove(save, StoneId, stones)) { error = "연마석이 부족합니다."; return false; }
            save.Doo -= doo;
            Entry(save, owner, index).Level = current + 1;
            return true;
        }

        // Item-based callers target its current wearer; UI enhancement uses an explicit owner and slot.
        public static bool CanEnhance(SaveData save, string id)
        {
            string owner = HolderOf(save, id);
            if (owner == null) return false;
            SyncSlots(save, owner);
            return CanEnhanceSlot(save, owner, FindItemSlot(save, owner, id));
        }
        public static bool Enhance(SaveData save, string id, out string error)
        {
            string owner = HolderOf(save, id);
            if (owner == null) { error = "상태창에서 해당 부위에 장비를 먼저 장착합니다."; return false; }
            SyncSlots(save, owner);
            return EnhanceSlot(save, owner, FindItemSlot(save, owner, id), out error);
        }

        public static void ForgetLevel(SaveData save, string id)
        {
            EnsureProgression(save);
            save.GearBreakthroughs.RemoveAll(entry => entry.Id == id);
        }
    }
}
