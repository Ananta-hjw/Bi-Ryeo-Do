using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// 분해 and 합성. A spare copy of a piece of gear (일반 to 전설; 신화 and 초월 gear cannot be dismantled) can be dismantled into materials of the same grade and slot
    /// (일반 7-12, 고급 6-11, 희귀 5-10, 영웅 3-8, 전설 1-6, with odds 10/20/30/20/10/10% across the six counts); enough materials of one grade and slot fuse into a random piece
    /// of the next grade in that slot (일반 50, 고급 40, 희귀 30, 영웅 20, 전설 10). Neither costs 두.
    /// </summary>
    public static class Salvage
    {
        public static readonly int[] CountOdds = { 10, 20, 30, 20, 10, 10 };   // percent for the lowest count .. lowest + 5
        public static readonly int[] MinCount = { 7, 6, 5, 3, 1 };             // lowest count per grade (일반 7-12, 고급 6-11, 희귀 5-10, 영웅 3-8, 전설 1-6)
        public static readonly int[] FuseNeed = { 50, 40, 30, 20, 10 };         // 일반 .. 전설 -> next grade

        private static readonly ItemKind[] Kinds = { ItemKind.Weapon, ItemKind.Armor, ItemKind.Pants, ItemKind.Boots, ItemKind.Accessory };
        private static readonly string[] KindKeys = { "weapon", "armor", "pants", "boots", "accessory" };
        private static readonly string[] KindLabels = { "무기", "상의", "하의", "신발", "장신구" };

        // names[grade][slot]: 무기, 상의, 하의, 신발, 장신구 (일반 .. 전설; 신화 장비와 초월 장비는 분해할 수 없어 재료가 없다)
        private static readonly string[][] Names =
        {
            new[] { "철 부스러기", "삼베 조각", "광목 자투리", "가죽 밑창", "구슬 부스러기" },
            new[] { "정련 철괴", "질긴 가죽", "누빔 솜", "징 쇠조각", "옥 조각" },
            new[] { "흑철 정", "은사 실타래", "흑견 천", "경공 밑창", "보석 가루" },
            new[] { "현철 조각", "자운 비단", "청룡 직물", "운보 가죽", "영석" },
            new[] { "용린 강철", "천잠 명주", "금강사 천", "천리 밑창", "정령석" },
        };

        public static readonly ItemData[] Materials = Build();

        private static ItemData[] Build()
        {
            var list = new List<ItemData>();
            for (int g = 0; g <= (int)ItemGrade.Legendary; g++)
                for (int k = 0; k < Kinds.Length; k++)
                {
                    string grade = Grades.Names[g];
                    string next = Grades.Names[g + 1];
                    string desc = grade + " " + KindLabels[k] + "을(를) 분해해서 나온 재료. " +
                        "같은 재료 " + FuseNeed[g] + "개를 모아 합성하면 " + next + " " + KindLabels[k] + " 하나가 무작위로 나온다.";
                    list.Add(new ItemData { Id = IdOf(Kinds[k], (ItemGrade)g), Name = Names[g][k], Kind = ItemKind.Material, Grade = (ItemGrade)g, SellValue = 0, Description = desc });
                }
            return list.ToArray();
        }

        public static string IdOf(ItemKind slot, ItemGrade grade) { return "mat_" + KindKeys[SlotIndex(slot)] + "_" + (int)grade; }

        private static int SlotIndex(ItemKind slot)
        {
            for (int i = 0; i < Kinds.Length; i++) if (Kinds[i] == slot) return i;
            return 0;
        }

        public static string SlotLabel(ItemKind slot) { return KindLabels[SlotIndex(slot)]; }

        /// <summary>The material a piece of this slot and grade dismantles into (null for 신화 and 초월 gear and for non-gear).</summary>
        public static ItemData MaterialFor(ItemData gear)
        {
            if (gear == null || !gear.IsGear || gear.Grade > ItemGrade.Legendary) return null;
            return EarlyGameContent.FindItem(IdOf(gear.Kind, gear.Grade));
        }

        /// <summary>True for the 25 dismantling materials (일반 .. 전설).</summary>
        public static bool IsFusable(ItemData item) { return item != null && item.Id.StartsWith("mat_") && item.Grade <= ItemGrade.Legendary; }

        /// <summary>Slot a material belongs to (Weapon, Armor, ...).</summary>
        public static ItemKind SlotOf(ItemData material)
        {
            for (int i = 0; i < KindKeys.Length; i++) if (material.Id.StartsWith("mat_" + KindKeys[i] + "_")) return Kinds[i];
            return ItemKind.Weapon;
        }

        public static int Need(ItemData material) { return IsFusable(material) ? FuseNeed[(int)material.Grade] : 0; }

        /// <summary>How many materials a dismantled piece of this grade gives (e.g. 일반 7..12, odds 10/20/30/20/10/10%).</summary>
        public static int RollCount(ItemGrade grade, IRandom rng)
        {
            int min = MinCount[Math.Max(0, Math.Min(MinCount.Length - 1, (int)grade))];
            int roll = (int)(rng.NextDouble() * 100.0), acc = 0;
            for (int i = 0; i < CountOdds.Length; i++) { acc += CountOdds[i]; if (roll < acc) return min + i; }
            return min + CountOdds.Length - 1;
        }

        public static int MaxCount(ItemGrade grade) { return MinCount[(int)grade] + CountOdds.Length - 1; }

        /// <summary>A copy can be dismantled when it is a spare, or when it is the only copy and is not worn. 초월 gear cannot.</summary>
        /// <summary>True when dismantling now would take the last copy of a breakthrough piece, so its stars would be lost (the UI asks first).</summary>
        public static bool LosesEnhancement(SaveData save, string id) { return Gear.Spares(save, id) <= 0 && Gear.Stars(save, id) > 0; }

        public static bool CanDismantle(SaveData save, string id)
        {
            var item = EarlyGameContent.FindItem(id);
            if (MaterialFor(item) == null) return false;
            return Inventory.Free(save, id) > 0;
        }

        /// <summary>분해: consumes one copy, returns the materials gained (null with an error when not allowed).</summary>
        public static ItemStack Dismantle(SaveData save, string id, IRandom rng, out string error)
        {
            error = null;
            var item = EarlyGameContent.FindItem(id);
            var mat = MaterialFor(item);
            if (mat == null) { error = "분해할 수 없는 물건입니다."; return null; }
            if (!CanDismantle(save, id)) { error = "착용 중인 장비는 분해할 수 없습니다. (비류연이나 동료가 입은 장비는 같은 장비가 더 있어야 분해할 수 있습니다.)"; return null; }
            int n = RollCount(item.Grade, rng);
            Inventory.Remove(save, id, 1);
            Inventory.Add(save, mat.Id, n);
            Inventory.Add(save, Gear.StoneId, PolishYield(item));
            return new ItemStack(mat.Id, n);
        }

        public static int PolishYield(ItemData item) { return MaterialFor(item) != null ? 1 + (int)item.Grade : 0; }

        public static bool CanFuse(SaveData save, ItemData material) { return IsFusable(material) && Inventory.Count(save, material.Id) >= Need(material); }

        /// <summary>The gear a fusion can produce: every pullable piece of the next grade in the same slot.</summary>
        public static List<ItemData> FuseTargets(ItemData material)
        {
            var list = new List<ItemData>();
            if (!IsFusable(material)) return list;
            var slot = SlotOf(material);
            foreach (var it in Gacha.Pool(material.Grade + 1)) if (it.IsGear && it.Kind == slot) list.Add(it);
            return list;
        }

        /// <summary>합성: spends the needed materials and hands out a random piece of the next grade in the slot.</summary>
        public static ItemData Fuse(SaveData save, string materialId, IRandom rng, out string error)
        {
            error = null;
            var mat = EarlyGameContent.FindItem(materialId);
            if (!IsFusable(mat)) { error = "합성할 수 없는 재료입니다."; return null; }
            int need = Need(mat);
            if (Inventory.Count(save, materialId) < need) { error = "재료가 모자랍니다. (" + Inventory.Count(save, materialId) + " / " + need + ")"; return null; }
            var targets = FuseTargets(mat);
            if (targets.Count == 0) { error = "만들 수 있는 장비가 없습니다."; return null; }
            Inventory.Remove(save, materialId, need);
            var result = targets[Math.Min(targets.Count - 1, (int)(rng.NextDouble() * targets.Count))];
            Inventory.Add(save, result.Id, 1);
            return result;
        }

        // ------------------------------------------------------------------ 일괄 분해

        /// <summary>One line of a bulk dismantle: a piece of gear and how many of its copies would go.</summary>
        public sealed class BulkEntry
        {
            public ItemData Item;
            public int Count;
        }

        /// <summary>The grades a bulk dismantle can pick (일반 .. 전설) and the five gear slots, in list order.</summary>
        public static readonly ItemGrade[] BulkGrades = { ItemGrade.Normal, ItemGrade.Fine, ItemGrade.Rare, ItemGrade.Epic, ItemGrade.Legendary };
        public static readonly ItemKind[] BulkKinds = Kinds;

        /// <summary>
        /// Which copies a bulk dismantle would take for the ticked grades and slots: spare copies always; with <paramref name="includeSingles"/> also the only copy
        /// of a piece that is not worn (never a breakthrough piece, and never a worn piece). Sorted by grade, then slot, then name.
        /// </summary>
        public static List<BulkEntry> BulkPlan(SaveData save, bool[] grades, bool[] slots, bool includeSingles)
        {
            var list = new List<BulkEntry>();
            foreach (var stack in save.Items)
            {
                var item = EarlyGameContent.FindItem(stack.Id);
                if (MaterialFor(item) == null) continue;
                int g = Array.IndexOf(BulkGrades, item.Grade), k = Array.IndexOf(BulkKinds, item.Kind);
                if (g < 0 || k < 0 || !grades[g] || !slots[k]) continue;
                int worn = Inventory.Worn(save, item.Id);
                int take = stack.Count - Math.Max(1, worn);
                if (includeSingles && stack.Count >= 1 && worn == 0 && Gear.Stars(save, item.Id) == 0) take = stack.Count;
                if (take > 0) list.Add(new BulkEntry { Item = item, Count = take });
            }
            list.Sort((a, b) =>
            {
                int c = ((int)a.Item.Grade).CompareTo((int)b.Item.Grade);
                if (c != 0) return c;
                c = Array.IndexOf(BulkKinds, a.Item.Kind).CompareTo(Array.IndexOf(BulkKinds, b.Item.Kind));
                return c != 0 ? c : string.CompareOrdinal(a.Item.Name, b.Item.Name);
            });
            return list;
        }

        /// <summary>Total materials a plan will give: min and max per material (the real roll lands in between, 9.3 on average for 일반).</summary>
        public static List<KeyValuePair<ItemData, int[]>> BulkYield(List<BulkEntry> plan)
        {
            var map = new Dictionary<string, int[]>();
            var order = new List<ItemData>();
            foreach (var e in plan)
            {
                var mat = MaterialFor(e.Item);
                if (!map.ContainsKey(mat.Id)) { map[mat.Id] = new[] { 0, 0 }; order.Add(mat); }
                map[mat.Id][0] += MinCount[(int)e.Item.Grade] * e.Count;
                map[mat.Id][1] += MaxCount(e.Item.Grade) * e.Count;
            }
            var result = new List<KeyValuePair<ItemData, int[]>>();
            foreach (var mat in order) result.Add(new KeyValuePair<ItemData, int[]>(mat, map[mat.Id]));
            return result;
        }

        /// <summary>Dismantles every entry of a plan; returns the materials actually gained (in the order they first appeared).</summary>
        public static List<ItemStack> DismantleBulk(SaveData save, List<BulkEntry> plan, IRandom rng)
        {
            var gained = new List<ItemStack>();
            foreach (var e in plan)
                for (int i = 0; i < e.Count; i++)
                {
                    string error;
                    var got = Dismantle(save, e.Item.Id, rng, out error);
                    if (got == null) break;
                    ItemStack sum = null;
                    foreach (var g in gained) if (g.Id == got.Id) sum = g;
                    if (sum == null) gained.Add(new ItemStack(got.Id, got.Count)); else sum.Count += got.Count;
                }
            return gained;
        }
    }
}
