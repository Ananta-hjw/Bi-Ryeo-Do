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
        public List<string> ActiveCompanions = new List<string>();   // chosen party members (max EarlyGameContent.MaxCompanions); empty = join order
        public List<ArtMasteryEntry> ArtMastery = new List<ArtMasteryEntry>();   // 무공 숙련 (additive field, no version bump)
        public List<BondEntry> Bonds = new List<BondEntry>();   // 인연 per companion (additive field)
        public List<string> DoneQuests = new List<string>();     // 의뢰 already completed (additive field)
        public List<string> DoneFortunes = new List<string>();   // 기연 already resolved (additive field)
        public int ProgressStage, ProgressNext;                    // resume point inside a stage (see StageProgress)
        public List<MemberHp> ProgressHp = new List<MemberHp>();
        public GameSettings Settings = new GameSettings();         // options (additive field)
        public List<string> TutorialDone = new List<string>();     // help topics already shown (additive field)
        public List<string> BenchedCompanions = new List<string>();   // companions the player explicitly sat out (never auto-filled)
        public int TowerBest;                                       // highest 무한 비무대 floor cleared (additive field)
        public List<string> SeenEnemies = new List<string>();      // 도감: enemies fought and beaten (additive field)
        public List<string> SeenItems = new List<string>();        // 도감: items ever owned or pulled (additive field)
        public List<string> ClaimedAchievements = new List<string>();   // 업적 rewards already taken (additive field)
        public int GachaPulls, BattleWins;                         // counters for achievements (additive fields)
        public List<GearLevel> GearLevels = new List<GearLevel>();   // 장비 강화 levels (additive field)
        public int SwordRank;           // 검룡위 (1-10), see Promotion; 0 in older saves counts as 1

        public List<string> LearnedArts = new List<string>();
        public List<string> EquippedArts = new List<string>();
    }
}
