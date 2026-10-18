using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// 분해 and 합성. A spare copy of a piece of gear (일반 to 전설; 신화 and 초월 gear cannot be dismantled) can be dismantled into 1-7 materials of the same grade and slot
    /// (the odds of 1..7 are 10/15/20/20/15/10/10%); enough materials of one grade and slot fuse into a random piece of the next grade in that slot
    /// (일반 40, 고급 30, 희귀 20, 영웅 15, 전설 10). Neither costs 두.
    /// </summary>
    public static class Salvage
    {
        public static readonly int[] CountOdds = { 10, 15, 20, 20, 15, 10, 10 };   // percent for 1..7 materials
        public static readonly int[] FuseNeed = { 40, 30, 20, 15, 10 };            // 일반 .. 전설 -> next grade

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

        /// <summary>1..7 with the 10/15/20/20/15/10/10% odds.</summary>
        public static int RollCount(IRandom rng)
        {
            int roll = (int)(rng.NextDouble() * 100.0), acc = 0;
            for (int i = 0; i < CountOdds.Length; i++) { acc += CountOdds[i]; if (roll < acc) return i + 1; }
            return CountOdds.Length;
        }

        /// <summary>A copy can be dismantled when it is a spare, or when it is the only copy and is not worn. 초월 gear cannot.</summary>
        public static bool CanDismantle(SaveData save, string id)
        {
            var item = EarlyGameContent.FindItem(id);
            if (MaterialFor(item) == null) return false;
            int have = Inventory.Count(save, id);
            return have >= 2 || (have == 1 && !Inventory.IsEquipped(save, id));
        }

        /// <summary>분해: consumes one copy, returns the materials gained (null with an error when not allowed).</summary>
        public static ItemStack Dismantle(SaveData save, string id, IRandom rng, out string error)
        {
            error = null;
            var item = EarlyGameContent.FindItem(id);
            var mat = MaterialFor(item);
            if (mat == null) { error = "분해할 수 없는 물건입니다."; return null; }
            if (!CanDismantle(save, id)) { error = "착용 중인 하나뿐인 장비는 분해할 수 없습니다."; return null; }
            int n = RollCount(rng);
            Inventory.Remove(save, id, 1);
            Inventory.Add(save, mat.Id, n);
            return new ItemStack(mat.Id, n);
        }

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
    }
}
