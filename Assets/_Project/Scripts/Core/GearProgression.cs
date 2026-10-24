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
        public int UnlockedGrade;
    }

    public static partial class Gear
    {
        public const int MaxLevel = 100, MaxStars = 5;
        public const string StoneId = "gear_polish";
        public static readonly int[] LevelCaps = { 10, 20, 30, 50, 70, 100 };
        private static readonly float[] StarBonuses = { 0f, .10f, .20f, .40f, .65f, 1.00f };
        public static readonly ItemData[] Materials =
        {
            Material(StoneId, "하급 영석", ItemGrade.Normal, "+1~+20"),
            Material("gear_spirit_mid", "중급 영석", ItemGrade.Fine, "+21~+40"),
            Material("gear_spirit_high", "고급 영석", ItemGrade.Rare, "+41~+60"),
            Material("gear_iron_high", "상급 현철", ItemGrade.Epic, "+61~+80"),
            Material("gear_iron_ancient", "만년 현철", ItemGrade.Legendary, "+81~+90"),
            Material("gear_celestial_stone", "신물:천외비석", ItemGrade.Mythic, "+91~+100")
        };
        public static readonly ItemData Polish = Materials[0]; // Keep legacy inventory IDs and rewards valid.

        private static ItemData Material(string id, string name, ItemGrade grade, string range)
        {
            return new ItemData { Id = id, Name = name, Kind = ItemKind.Material, Grade = grade,
                Description = range + " 부위 확정 강화 재료. " + (grade == ItemGrade.Mythic ? "만년 현철 3개 합성으로 얻는다. 전투에서는 나오지 않는다." : "이야기 스테이지 전투 승리·같은 등급 장비 분해로 얻는다. 3개로 다음 등급 재료 1개를 확정 합성한다.") + " 팔 수 없다." };
        }

        public static ItemData MaterialForLevel(int nextLevel)
        {
            int n = Math.Max(1, Math.Min(MaxLevel, nextLevel));
            return Materials[n <= 80 ? (n - 1) / 20 : n <= 90 ? 4 : 5];
        }

        public const int MaterialFusionCost = 3;

        public static ItemData NextMaterial(string id)
        {
            for (int i = 0; i < Materials.Length - 1; i++) if (Materials[i].Id == id) return Materials[i + 1];
            return null;
        }

        public static bool CanFuseMaterial(SaveData save, string id)
        {
            return NextMaterial(id) != null && Inventory.Count(save, id) >= MaterialFusionCost;
        }

        /// <summary>How many times this material can be fused right now (3 each).</summary>
        public static int MaxMaterialFusions(SaveData save, string id)
        {
            return NextMaterial(id) == null ? 0 : Inventory.Count(save, id) / MaterialFusionCost;
        }

        public static ItemData FuseMaterial(SaveData save, string id, out string error) { return FuseMaterial(save, id, 1, out error); }

        /// <summary>Fuses <paramref name="times"/> x 3 of a material into as many of the next one, all at once.</summary>
        public static ItemData FuseMaterial(SaveData save, string id, int times, out string error)
        {
            error = null;
            var result = NextMaterial(id);
            if (result == null) { error = "다음 등급으로 합성할 수 없는 재료입니다."; return null; }
            if (times < 1) { error = "합성할 개수를 골라 주세요."; return null; }
            if (Inventory.Count(save, id) < MaterialFusionCost * times) { error = times == 1 ? "합성하려면 같은 강화 재료 3개가 필요합니다." : "합성 재료가 부족합니다."; return null; }
            if (!Inventory.Remove(save, id, MaterialFusionCost * times)) { error = "합성 재료가 부족합니다."; return null; }
            Inventory.Add(save, result.Id, times);
            return result;
        }

        public static bool IsEnhancementMaterial(string id)
        {
            foreach (var material in Materials) if (material.Id == id) return true;
            return false;
        }

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
            if (save.GearProgressionMigrated) { MigrateGradeUnlocks(save); MigrateLowGradeUnlocks(save); return; }
            bool migrateGrades = !save.GearGradeUnlocksMigrated;
            // SyncSlots re-enters this method; finish legacy slot assignments before grade migration.
            save.GearGradeUnlocksMigrated = true;
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
                slot.UnlockedGrade = Math.Max(slot.UnlockedGrade, (int)item.Grade);
                // More than five consumed copies cannot become stars; return those copies.
                if (old > MaxStars) Inventory.Add(save, legacy.Id, old - MaxStars);
            }
            save.GearLevels.Clear();
            if (migrateGrades) { save.GearGradeUnlocksMigrated = false; MigrateGradeUnlocks(save); }
            MigrateLowGradeUnlocks(save);
        }

        /// <summary>
        /// 고급·희귀·영웅 used to be open for free. Once, keep an enhanced slot's current gear of those grades open so no bonus is lost;
        /// an unenhanced slot pays the new price when it is first enhanced.
        /// </summary>
        private static void MigrateLowGradeUnlocks(SaveData save)
        {
            if (save.GearLowGradeUnlocksMigrated) return;
            save.GearLowGradeUnlocksMigrated = true;
            foreach (var slot in save.GearSlots)
            {
                var item = EarlyGameContent.FindItem(slot.ItemId);
                if (slot.Level > 0 && item != null && item.IsGear && item.Grade <= ItemGrade.Epic && Inventory.GearOf(save, slot.Owner).Contains(item.Id))
                    slot.UnlockedGrade = Math.Max(slot.UnlockedGrade, (int)item.Grade);
            }
        }

        private static void MigrateGradeUnlocks(SaveData save)
        {
            if (save.GearGradeUnlocksMigrated) return;
            save.GearGradeUnlocksMigrated = true;
            // Preserve bonuses already applied to equipped, enhanced gear in existing saves.
            foreach (var slot in save.GearSlots)
            {
                var item = EarlyGameContent.FindItem(slot.ItemId);
                if (slot.Level > 0 && item != null && item.IsGear && Inventory.GearOf(save, slot.Owner).Contains(item.Id))
                    slot.UnlockedGrade = Math.Max(slot.UnlockedGrade, (int)item.Grade);
            }
        }

        public static ItemGrade UnlockedGrade(SaveData save, string owner, int index)
        {
            EnsureProgression(save);
            if (index < 0 || index >= 6) return ItemGrade.Normal;
            return (ItemGrade)Math.Max((int)ItemGrade.Normal, Math.Min((int)ItemGrade.Transcendent, Entry(save, owner, index).UnlockedGrade));
        }

        public static bool IsGradeUnlocked(SaveData save, string owner, int index, ItemGrade grade)
        {
            return index >= 0 && index < 6 && grade <= UnlockedGrade(save, owner, index);
        }

        /// <summary>은자 to open each grade from the one below it: 일반 free, 고급 1,000, 희귀 3,000, 영웅 5,000, 전설 15,000, 신화 30,000, 초월 60,000.</summary>
        public static readonly long[] GradeUnlockSteps = { 0, 1000, 3000, 5000, 15000, 30000, 60000 };

        /// <summary>Everything paid to have this grade open (the steps up to it): 고급 1,000 … 전설 24,000, 신화 54,000, 초월 114,000.</summary>
        public static long GradeUnlockTotal(ItemGrade grade)
        {
            long total = 0;
            for (int g = 1; g <= (int)grade && g < GradeUnlockSteps.Length; g++) total += GradeUnlockSteps[g];
            return total;
        }

        public static long GradeUnlockCost(SaveData save, string owner, int index, ItemGrade grade)
        {
            return Math.Max(0, GradeUnlockTotal(grade) - GradeUnlockTotal(UnlockedGrade(save, owner, index)));
        }

        public static bool UnlockSlotGrade(SaveData save, string owner, int index, out string error)
        {
            error = null;
            var item = ItemInSlot(save, owner, index);
            if (item == null) { error = "개방할 부위에 장비를 먼저 장착해 주세요."; return false; }
            long cost = GradeUnlockCost(save, owner, index, item.Grade);
            if (cost == 0) { error = "이미 개방한 등급입니다."; return false; }
            if (save.Doo < cost) { error = "등급 개방에 필요한 은자가 부족합니다."; return false; }
            save.Doo -= cost;
            Entry(save, owner, index).UnlockedGrade = (int)item.Grade;
            return true;
        }

        public static int AppliedSlotLevel(SaveData save, string owner, int index, ItemData item)
        {
            return item != null && IsGradeUnlocked(save, owner, index, item.Grade) ? Math.Min(SlotLevel(save, owner, index), Cap(save, item.Id)) : 0;
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
        public static float StarBonusFor(ItemData item, int stars) { return StarBonus(item != null && item.Grade == ItemGrade.Transcendent ? MaxStars : stars); }
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
            // Definitions use the old +30% five-star reference. Keep that denominator fixed so the current breakthrough bonus cannot reduce existing enhancement effects.
            return Multiplier(level, MaxStars) / 1.3f;
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
            return AppliedSlotLevel(save, owner, index, EarlyGameContent.FindItem(id));
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
        /// <summary>은자 for the next enhancement step n: 50 + 8n + 0.75n² (rounded), about 299,000 from +0 to +100.</summary>
        public static long DooCost(int nextLevel) { long n = Math.Max(1, Math.Min(MaxLevel, nextLevel)); return (long)Math.Round(50 + 8 * n + 0.75 * n * n, MidpointRounding.AwayFromZero); }
        public static bool CanEnhanceSlot(SaveData save, string owner, int index)
        {
            var item = ItemInSlot(save, owner, index);
            int current = SlotLevel(save, owner, index);
            return item != null && IsGradeUnlocked(save, owner, index, item.Grade) && current < Cap(save, item.Id)
                && Inventory.Count(save, MaterialForLevel(current + 1).Id) >= StoneCost(current + 1) && save.Doo >= DooCost(current + 1);
        }

        public static bool EnhanceSlot(SaveData save, string owner, int index, out string error)
        {
            var item = ItemInSlot(save, owner, index);
            int current = SlotLevel(save, owner, index);
            error = null;
            if (item == null) { error = "해당 부위에 장비를 먼저 장착합니다."; return false; }
            if (!IsGradeUnlocked(save, owner, index, item.Grade)) { error = "이 부위의 " + Grades.NameOf(item.Grade) + " 등급을 먼저 개방해 주세요."; return false; }
            if (current >= Cap(save, item.Id)) { error = "현재 장비의 강화 상한입니다. 한계돌파하면 상한이 늘어납니다."; return false; }
            var material = MaterialForLevel(current + 1);
            int stones = StoneCost(current + 1);
            long doo = DooCost(current + 1);
            if (Inventory.Count(save, material.Id) < stones || save.Doo < doo) { error = "강화에 필요한 " + material.Name + " 또는 은자가 부족합니다."; return false; }
            if (!Inventory.Remove(save, material.Id, stones)) { error = material.Name + "이(가) 부족합니다."; return false; }
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
