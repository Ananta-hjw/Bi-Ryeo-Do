using System;

namespace BiRyeoDo.Core
{
    /// <summary>Armor is the 상의 (top); Pants is the 하의 and Boots the 신발. One of each is worn, plus a weapon and two accessories.</summary>
    public enum ItemKind { Consumable, Weapon, Accessory, Material, Key, Armor, Pants, Boots }

    /// <summary>Static item definition. Percent fields are fractions (0.10 = +10%).</summary>
    public sealed class ItemData
    {
        public string Id, Name, Description;
        public ItemKind Kind;
        public ItemGrade Grade;   // rarity (defaults to 일반)
        public bool InGacha;      // consumables that can come out of the 뽑기 (all gear can)
        public long SellValue;

        // Consumables (used on a party member's turn)
        public float HealPct;      // restores this share of max HP to the most wounded ally
        public int QiRestore;      // 내공 for the user
        public int SpiritGain;     // 기세 for the user
        public bool Cleanse;       // cures every 상태 이상 of the chosen ally (해독 영약)
        public int MasteryUses;    // 수련서: used from the bag, adds this many uses to one learned art the player picks (never usable in battle)

        // Gear (protagonist only)
        public float AtkPct, DefPct, HpPct, QiPct, CritAdd;
        public float CritDmgAdd;   // added to the 치명타 피해 multiplier (0.30 = +30%p, so 150% becomes 180%)
        public int SpeedAdd;

        public bool IsGear { get { return Kind == ItemKind.Weapon || Kind == ItemKind.Accessory || Kind == ItemKind.Armor || Kind == ItemKind.Pants || Kind == ItemKind.Boots; } }

        /// <summary>Name of the gear slot ("무기", "상의", "하의", "신발", "장신구"), or "" for things that are not gear.</summary>
        public static string SlotName(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Weapon: return "무기";
                case ItemKind.Armor: return "상의";
                case ItemKind.Pants: return "하의";
                case ItemKind.Boots: return "신발";
                case ItemKind.Accessory: return "장신구";
                default: return "";
            }
        }
        public bool InPool { get { return (IsGear || InGacha) && Grade != ItemGrade.Transcendent; } }   // 초월 gear is earned, never pulled
    }

    /// <summary>A stack in the bag. Public fields so JsonUtility can persist it.</summary>
    [Serializable]
    public sealed class ItemStack
    {
        public string Id;
        public int Count;

        public ItemStack() { }
        public ItemStack(string id, int count) { Id = id; Count = count; }
    }

    /// <summary>A chance for an enemy to drop an item when beaten.</summary>
    public sealed class DropData
    {
        public string EnemyId, ItemId;
        public float Chance;
        public int Count = 1;
    }
}
