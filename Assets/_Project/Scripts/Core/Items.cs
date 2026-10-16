using System;

namespace BiRyeoDo.Core
{
    public enum ItemKind { Consumable, Weapon, Accessory, Material, Key, Armor }

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

        // Gear (protagonist only)
        public float AtkPct, DefPct, HpPct, QiPct, CritAdd;
        public int SpeedAdd;

        public bool IsGear { get { return Kind == ItemKind.Weapon || Kind == ItemKind.Accessory || Kind == ItemKind.Armor; } }
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
