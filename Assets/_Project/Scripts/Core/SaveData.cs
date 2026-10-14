using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>Everything persisted between sessions. Public fields so Unity's JsonUtility can serialize it.</summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 2;

        public int Version = CurrentVersion;
        public int Life = 1;            // 제N생: reincarnation count
        public int Level = 1;           // 경지/수련 단계 within the current life
        public long Exp;
        public long Doo;                // 두: main currency
        public int ClearedStage;        // highest stage cleared this life (0 = none); the next one is unlocked
        public int HighestUnlockedStage { get { return ClearedStage + 1; } }
        // Items (added without a version bump: older saves simply start with empty lists)
        public List<ItemStack> Items = new List<ItemStack>();
        public List<string> EquippedGear = new List<string>();
        public List<int> RewardedStages = new List<int>();   // stages whose first-clear reward was already claimed
        public bool StartKitGiven;
        public int SwordRank;           // 검룡위 (1-10), see Promotion; 0 in older saves counts as 1

        public List<string> LearnedArts = new List<string>();
        public List<string> EquippedArts = new List<string>();
    }
}
