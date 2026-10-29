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
        public int StoryContentRevision = StoryContentMigration.CurrentRevision;
        public int RetiredStoryStars;
        public List<string> CompletedStoryBattles = new List<string>();   // legacy standalone-story completion; main stages now track every fight
        public int Level = 1;           // 경지/수련 단계 within the current life
        public long Exp;
        public long Doo;                // 두: main currency
        public int ClearedStage;        // highest stage cleared this life (0 = none); the next one is unlocked
        public int HighestUnlockedStage { get { return ClearedStage + 1; } }
        // Items (added without a version bump: older saves simply start with empty lists)
        public List<ItemStack> Items = new List<ItemStack>();
        public List<string> EquippedGear = new List<string>();
        public List<CompanionProgress> CompanionLevels = new List<CompanionProgress>();   // each companion's own level and exp (additive field, see CompanionGrowth)
        public List<CompanionGearEntry> CompanionGear = new List<CompanionGearEntry>();   // gear each companion wears, drawn from the shared bag
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
        public bool RitualDone;                                       // 전승의식 completed (see Ritual)
        public bool PlacementCustom;                                  // the player arranged the starting tiles (see Formation)
        public List<string> Placement = new List<string>();          // "id:col:row" of every party member when PlacementCustom
        public List<string> BenchedCompanions = new List<string>();   // companions the player explicitly sat out (never auto-filled)
        public int TowerBest;                                       // highest 무한 비무대 floor cleared (additive field)
        public List<string> SeenEnemies = new List<string>();      // 도감: enemies fought and beaten (additive field)
        public List<string> SeenItems = new List<string>();        // 도감: items ever owned or pulled (additive field)
        public List<string> ClaimedAchievements = new List<string>();   // 업적 rewards already taken (additive field)
        public int GachaPulls, BattleWins;                         // counters for achievements (additive fields)
        public List<GearLevel> InnerLevels = new List<GearLevel>();  // 심법 강화 (심득석) levels (additive field)
        public List<GearLevel> GearLevels = new List<GearLevel>();   // legacy item enhancements; migrated once
        public bool GearProgressionMigrated;
        public bool GearGradeUnlocksMigrated;
        public bool GearLowGradeUnlocksMigrated;   // 고급~영웅 개방이 유료가 된 뒤 한 번: 이미 강화한 부위의 장비 등급을 그대로 연 것으로 둔다
        public List<GearLevel> GearBreakthroughs = new List<GearLevel>();
        public List<GearSlot> GearSlots = new List<GearSlot>();
        public List<string> ReadRecordUpdates = new List<string>();   // acknowledged codex, story and faction record updates
        public List<string> SeenArts = new List<string>();         // arts whose "new" red dot was already looked at (additive field)
        public List<string> SeenForms = new List<string>();        // 무공 변화 forms already announced (additive field)
        public List<string> SeenInner = new List<string>();        // same for 심법 (additive field)
        public int RealmMax = -1;                                  // highest 무공 경지 step reached (-1 = not recorded yet, see Realm.PollRise)
        public bool DiscoveryInit;                                 // old saves are marked as seen once (see Discovery.Sync)
        public List<string> LearnedInner = new List<string>();     // 심법 learned (additive field)
        public List<string> EquippedInner = new List<string>();    // 심법 equipped (see InnerArts.SlotCount)
        public List<StarEntry> StageStars = new List<StarEntry>();   // best star mask per stage and difficulty (additive field)
        public List<int> ClaimedStarChests = new List<int>();       // star milestones already collected
        public int Difficulty;                                      // difficulty picked for the stage list (0-2)
        public int ProgressDifficulty;                              // difficulty of the resume point (see StageProgress)
        public List<ReputationEntry> Reputation = new List<ReputationEntry>();   // 세력 평판 points (additive field)
        public List<ShopPurchase> ShopBought = new List<ShopPurchase>();          // limited 객잔 goods already bought
        public List<TrainingEntry> CompanionTraining = new List<TrainingEntry>();   // 동료 수련 levels (additive field)
        public List<string> SeenMemories = new List<string>();            // 나예린 호감도 이벤트 already watched (additive field)
        public List<int> ClaimedMemoryMilestones = new List<int>();
        public int DailyDate, DailyLastClear, DailyStreak, DailyBest;                  // 일일 수련 임무 (additive fields), dates as yyyymmdd
        public List<string> DailyMissionIds = new List<string>();
        public List<int> DailyProgress = new List<int>();
        public List<int> DailyClaimed = new List<int>();
        public int SwordRank;           // 검룡위 (1-10), see Promotion; 0 in older saves counts as 1

        public List<string> LearnedArts = new List<string>();
        public List<string> EquippedArts = new List<string>();
    }

    [Serializable]
    public sealed class CompanionProgress
    {
        public string Id;
        public int Level = 1;
        public long Exp;
        public int RealmMax = -1;    // highest 무공 경지 step announced (-1 = not recorded yet, see CompanionGrowth.PollRises)
        public int ArtsLevel = -1;   // level up to which newly learned arts were announced (-1 = not recorded yet)
    }

    [Serializable]
    public sealed class CompanionGearEntry
    {
        public string Id;
        public List<string> Items = new List<string>();
    }
}
