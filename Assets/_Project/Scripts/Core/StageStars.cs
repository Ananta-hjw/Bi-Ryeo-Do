using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>Best star mask of one stage at one difficulty (bit 0 clear, bit 1 everyone survived, bit 2 fast). Public fields for JsonUtility.</summary>
    [Serializable]
    public sealed class StarEntry
    {
        public int Stage, Diff, Mask;
    }

    /// <summary>A star milestone: collect this many stars in total to open the chest.</summary>
    public sealed class StarChest
    {
        public int Stars;
        public long Doo;
        public string ItemId;
        public int ItemCount = 1;
    }

    /// <summary>
    /// 별점과 고난도: every stage can be cleared on three difficulties, and each (stage, difficulty) holds three stars:
    /// 돌파, 전원 생존 (nobody falls in any fight) and 신속 (within the stage's action limit). Harder difficulties scale the
    /// enemies and pay more. Total stars open milestone chests.
    /// </summary>
    public static class StageStars
    {
        public const int DifficultyCount = 3;
        public static readonly string[] DifficultyNames = { "일반", "어려움", "극악" };
        public static readonly float[] HpScale = { 1.0f, 1.6f, 2.0f };
        public static readonly float[] AtkScale = { 1.0f, 1.4f, 1.6f };
        public static readonly float[] RewardFactor = { 1.0f, 1.5f, 2.2f };   // 두 and exp
        public static readonly string[] StarNames = { "돌파", "전원 생존", "신속" };

        /// <summary>Party actions allowed for the 신속 star, per stage number (index = stage). About 90% of what the balance bot needs.</summary>
        private static readonly int[] FastLimits =
        {
            0, 28, 21, 19, 19, 7, 18, 23, 20, 20, 19, 20, 22, 21, 36, 22, 25, 30, 18, 38, 35, 40, 43, 44, 53, 48, 43, 50, 66, 110, 58
        };

        public static int FastLimit(int stage)
        {
            if (stage >= 1 && stage < FastLimits.Length) return FastLimits[stage];
            return 30 + 2 * Math.Max(0, stage);
        }

        // The protagonist fights alone until stage 7 and the party then grows stronger than the stage curve alone suggests,
        // so the extra difficulty ramps up with the stage: gentle in the solo stages, steep late.
        public static float EarlyRamp = 0.15f, SoloRamp = 0.05f, LateBase = 0.7f, LateStep = 0.03f;

        public static float Ramp(int stage)
        {
            return stage <= 4 ? EarlyRamp : stage <= 6 ? SoloRamp : LateBase + LateStep * (stage - 7);
        }

        public static float HpFor(int stage, int difficulty) { return 1f + (HpScale[Clamp(difficulty)] - 1f) * Ramp(stage); }
        public static float AtkFor(int stage, int difficulty) { return 1f + (AtkScale[Clamp(difficulty)] - 1f) * Ramp(stage); }
        private static int Clamp(int difficulty) { return Math.Max(0, Math.Min(DifficultyCount - 1, difficulty)); }

        public static EnemyData ScaleEnemy(EnemyData enemy, int stage, int difficulty)
        {
            difficulty = Clamp(difficulty);
            return difficulty == 0 ? enemy : enemy.Scaled(HpFor(stage, difficulty), AtkFor(stage, difficulty));
        }

        public static string TagFor(int difficulty) { return difficulty <= 0 ? "" : "[" + DifficultyNames[Math.Min(DifficultyCount - 1, difficulty)] + "] "; }

        public static int Count(int mask)
        {
            int n = 0;
            for (int i = 0; i < 3; i++) if ((mask & (1 << i)) != 0) n++;
            return n;
        }

        /// <summary>The star mask earned by a cleared stage run.</summary>
        public static int Evaluate(int stage, int actions, bool anyoneFell)
        {
            int mask = 1;
            if (!anyoneFell) mask |= 2;
            if (actions <= FastLimit(stage)) mask |= 4;
            return mask;
        }

        /// <summary>Older saves have cleared stages but no star records: count each cleared stage as cleared on 일반.</summary>
        public static void Sync(SaveData save)
        {
            foreach (var st in EarlyGameContent.Stages)
            {
                if (st.Number > save.ClearedStage) break;
                if ((MaskOf(save, st.Number, 0) & 1) == 0) Record(save, st.Number, 0, 1);
            }
        }

        public static StarEntry Find(SaveData save, int stage, int difficulty)
        {
            foreach (var e in save.StageStars) if (e.Stage == stage && e.Diff == difficulty) return e;
            return null;
        }

        public static int MaskOf(SaveData save, int stage, int difficulty) { var e = Find(save, stage, difficulty); return e != null ? e.Mask : 0; }
        public static int StarsOf(SaveData save, int stage, int difficulty) { return Count(MaskOf(save, stage, difficulty)); }

        public static int Total(SaveData save)
        {
            int n = 0;
            foreach (var e in save.StageStars) n += Count(e.Mask);
            return n;
        }

        /// <summary>A harder difficulty opens once the stage is cleared on the one below.</summary>
        public static bool Unlocked(SaveData save, int stage, int difficulty)
        {
            if (difficulty <= 0) return true;
            return (MaskOf(save, stage, difficulty - 1) & 1) != 0;
        }

        /// <summary>Stores the best result; returns how many new stars it earned.</summary>
        public static int Record(SaveData save, int stage, int difficulty, int mask)
        {
            var e = Find(save, stage, difficulty);
            if (e == null) { e = new StarEntry { Stage = stage, Diff = difficulty }; save.StageStars.Add(e); }
            int before = Count(e.Mask);
            e.Mask |= mask;
            return Count(e.Mask) - before;
        }

        // ------------------------------------------------------------------ milestone chests

        public static readonly StarChest[] Chests =
        {
            new StarChest { Stars = 10, Doo = 3000 },
            new StarChest { Stars = 25, Doo = 6000, ItemId = "ginseng" },
            new StarChest { Stars = 45, Doo = 10000, ItemId = "ginseng", ItemCount = 2 },
            new StarChest { Stars = 70, Doo = 16000, ItemId = "ilgeup_yeongdan" },
            new StarChest { Stars = 100, Doo = 25000, ItemId = "ilgeup_yeongdan" },
            new StarChest { Stars = 140, Doo = 40000, ItemId = "ilgeup_yeongdan", ItemCount = 2 },
            new StarChest { Stars = 200, Doo = 60000, ItemId = "ilgeup_yeongdan", ItemCount = 3 },
        };

        public static int MaxStars { get { return EarlyGameContent.Stages.Length * 3 * DifficultyCount; } }

        public static bool CanClaim(SaveData save, StarChest c) { return Total(save) >= c.Stars && !save.ClaimedStarChests.Contains(c.Stars); }

        public static int ClaimableCount(SaveData save)
        {
            int n = 0;
            foreach (var c in Chests) if (CanClaim(save, c)) n++;
            return n;
        }

        /// <summary>Opens every chest that is ready; returns a one-line note, or null when nothing was ready.</summary>
        public static string ClaimAll(SaveData save)
        {
            long doo = 0; int n = 0;
            var items = new List<string>();
            foreach (var c in Chests)
            {
                if (!CanClaim(save, c)) continue;
                save.ClaimedStarChests.Add(c.Stars);
                save.Doo += c.Doo;
                doo += c.Doo; n++;
                if (!string.IsNullOrEmpty(c.ItemId))
                {
                    var item = EarlyGameContent.FindItem(c.ItemId);
                    if (item != null) { Inventory.Add(save, c.ItemId, c.ItemCount); items.Add(item.Name + (c.ItemCount > 1 ? " x" + c.ItemCount : "")); }
                }
            }
            if (n == 0) return null;
            return "별 보상 " + n + "개: 두 +" + doo.ToString("N0") + (items.Count > 0 ? ", " + string.Join(", ", items) : "");
        }

        /// <summary>Extra drop rolls on harder difficulties (1 + difficulty rolls per fight).</summary>
        public static int DropRolls(int difficulty) { return 1 + Math.Max(0, Math.Min(DifficultyCount - 1, difficulty)); }
    }
}
