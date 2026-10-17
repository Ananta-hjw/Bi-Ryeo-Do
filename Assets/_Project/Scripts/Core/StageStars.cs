using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>Best star mask of one fight of a stage at one difficulty (bit 0 won, bit 1 everyone survived, bit 2 fast). Public fields for JsonUtility.</summary>
    [Serializable]
    public sealed class StarEntry
    {
        public int Stage, Fight, Diff, Mask;
    }

    /// <summary>A star milestone: collect this many stars in total to open the chest.</summary>
    public sealed class StarChest
    {
        public int Stars;
        public long Doo;
        public string ItemId;
        public int ItemCount = 1;
        public int Insight;   // 심득석
    }

    /// <summary>
    /// 별점과 고난도: every stage can be cleared on three difficulties, and every single fight of a stage (each "small stage")
    /// holds its own three stars per difficulty: 돌파, 전원 생존 (nobody falls in that fight) and 신속 (within that fight's
    /// action limit). Harder difficulties scale the enemies and pay more. Total stars open milestone chests.
    /// </summary>
    public static class StageStars
    {
        public const int DifficultyCount = 3;
        public static readonly string[] DifficultyNames = { "일반", "어려움", "극악" };
        public static readonly float[] HpScale = { 1.0f, 1.9f, 2.6f };
        public static readonly float[] AtkScale = { 1.0f, 1.55f, 1.95f };
        public static readonly float[] RewardFactor = { 1.0f, 1.5f, 2.2f };   // 두 and exp
        public static readonly string[] StarNames = { "돌파", "전원 생존", "신속" };

        /// <summary>Party actions allowed for the 신속 star of each fight (index = stage - 1, then fight). About 90% of the balance bot's median, plus one.</summary>
        private static readonly int[][] FastLimits =
        {
            new[] { 5, 8, 10, 13 },   // 1
            new[] { 4, 8, 13 },   // 2
            new[] { 5, 9, 9 },   // 3
            new[] { 6, 9, 9 },   // 4
            new[] { 3, 7 },   // 5
            new[] { 5, 7, 8 },   // 6
            new[] { 5, 9, 12, 10 },   // 7
            new[] { 6, 11, 21 },   // 8
            new[] { 9, 8, 18, 18 },   // 9
            new[] { 8, 15, 18 },   // 10
            new[] { 5, 5, 14 },   // 11
            new[] { 8, 19, 18 },   // 12
            new[] { 22, 27 },   // 13
            new[] { 20, 17, 35 },   // 14
            new[] { 14, 19, 9 },   // 15
            new[] { 17, 13, 12 },   // 16
            new[] { 26, 21, 19 },   // 17
            new[] { 16, 25 },   // 18
            new[] { 29, 26, 24 },   // 19
            new[] { 27, 17, 21 },   // 20
            new[] { 28, 26, 24 },   // 21
            new[] { 31, 24, 24 },   // 22
            new[] { 28, 26, 23 },   // 23
            new[] { 35, 38, 30 },   // 24
            new[] { 33, 36, 28 },   // 25
            new[] { 39, 24, 28 },   // 26
            new[] { 27, 34, 29 },   // 27
            new[] { 23, 44, 30, 28 },   // 28
            new[] { 28, 39, 26, 14, 44 },   // 29
            new[] { 52, 37, 24 },   // 30
        };

        public static int FastLimit(int stage, int fight)
        {
            if (stage >= 1 && stage <= FastLimits.Length && fight >= 0 && fight < FastLimits[stage - 1].Length) return FastLimits[stage - 1][fight];
            return 12 + stage / 2;
        }

        /// <summary>The 신속 limit on a difficulty: harder enemies have more HP, so the limit grows with <see cref="HpFor"/>.</summary>
        public static int FastLimit(int stage, int fight, int difficulty)
        {
            return (int)Math.Ceiling(FastLimit(stage, fight) * Math.Max(1f, HpFor(stage, difficulty)));
        }

        public static int FightCount(int stage)
        {
            var st = EarlyGameContent.FindStage(stage);
            return st != null ? st.Encounters.Length : 0;
        }

        /// <summary>Stars a stage can give on one difficulty (3 per fight).</summary>
        public static int MaxStarsOfStage(int stage) { return FightCount(stage) * 3; }

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

        /// <summary>The star mask earned by a won fight.</summary>
        public static int Evaluate(int stage, int fight, int actions, bool anyoneFell, int difficulty = 0)
        {
            int mask = 1;
            if (!anyoneFell) mask |= 2;
            if (actions <= FastLimit(stage, fight, difficulty)) mask |= 4;
            return mask;
        }

        public static StarEntry Find(SaveData save, int stage, int fight, int difficulty)
        {
            foreach (var e in save.StageStars) if (e.Stage == stage && e.Fight == fight && e.Diff == difficulty) return e;
            return null;
        }

        public static int MaskOf(SaveData save, int stage, int fight, int difficulty) { var e = Find(save, stage, fight, difficulty); return e != null ? e.Mask : 0; }
        public static int StarsOf(SaveData save, int stage, int fight, int difficulty) { return Count(MaskOf(save, stage, fight, difficulty)); }

        /// <summary>Stars earned in one stage on one difficulty.</summary>
        public static int StarsOfStage(SaveData save, int stage, int difficulty)
        {
            int n = 0;
            foreach (var e in save.StageStars) if (e.Stage == stage && e.Diff == difficulty) n += Count(e.Mask);
            return n;
        }

        public static int Total(SaveData save)
        {
            int n = 0;
            foreach (var e in save.StageStars) n += Count(e.Mask);
            return n;
        }

        /// <summary>A stage counts as cleared on a difficulty once its last fight was won there.</summary>
        public static bool StageCleared(SaveData save, int stage, int difficulty)
        {
            int last = FightCount(stage) - 1;
            return last >= 0 && (MaskOf(save, stage, last, difficulty) & 1) != 0;
        }

        /// <summary>
        /// Learning gate shared by martial arts and 심법: on 일반 the stage only has to be reached (unlocked); on a harder
        /// difficulty it has to be cleared there (so late learnings sit on the 어려움 / 극악 ladder).
        /// </summary>
        public static bool Reached(SaveData save, int stage, int difficulty)
        {
            if (difficulty <= 0) return save.HighestUnlockedStage >= stage;
            return StageCleared(save, stage, difficulty);
        }

        /// <summary>"스테이지 12에 도달" / "어려움 스테이지 30 돌파"</summary>
        public static string ReachText(int stage, int difficulty)
        {
            return difficulty <= 0 ? "스테이지 " + stage + "에 도달" : DifficultyNames[Math.Min(difficulty, DifficultyNames.Length - 1)] + " 스테이지 " + stage + " 돌파";
        }

        /// <summary>A harder difficulty opens once the stage is cleared on the one below.</summary>
        public static bool Unlocked(SaveData save, int stage, int difficulty)
        {
            if (difficulty <= 0) return true;
            return StageCleared(save, stage, difficulty - 1);
        }

        /// <summary>Stores the best result of a fight; returns how many new stars it earned.</summary>
        public static int Record(SaveData save, int stage, int fight, int difficulty, int mask)
        {
            var e = Find(save, stage, fight, difficulty);
            if (e == null) { e = new StarEntry { Stage = stage, Fight = fight, Diff = difficulty }; save.StageStars.Add(e); }
            int before = Count(e.Mask);
            e.Mask |= mask;
            return Count(e.Mask) - before;
        }

        /// <summary>심득석 for the first clear of a stage: 어려움 1, 극악 2 (+1 more on every 10th stage), 일반 only on every 5th stage.</summary>
        public static int ClearInsight(int stage, int difficulty)
        {
            int n = difficulty <= 0 ? (stage % 5 == 0 ? 1 : 0) : difficulty;
            if (difficulty >= 1 && stage % 10 == 0) n++;
            return n;
        }

        /// <summary>Pays the first-clear 심득석 (call after recording the last fight); returns the amount, 0 if the stage was already cleared before.</summary>
        public static int GrantClearInsight(SaveData save, int stage, int difficulty, bool wasCleared)
        {
            if (wasCleared || !StageCleared(save, stage, difficulty)) return 0;
            int n = ClearInsight(stage, difficulty);
            if (n > 0) Inventory.Add(save, InnerArts.StoneId, n);
            return n;
        }

        /// <summary>Older saves have cleared stages but no star records: count every fight of a cleared stage as won on 일반.</summary>
        public static void Sync(SaveData save)
        {
            foreach (var st in EarlyGameContent.Stages)
            {
                if (st.Number > save.ClearedStage) break;
                for (int f = 0; f < st.Encounters.Length; f++)
                    if ((MaskOf(save, st.Number, f, 0) & 1) == 0) Record(save, st.Number, f, 0, 1);
            }
        }

        // ------------------------------------------------------------------ milestone chests

        public static readonly StarChest[] Chests =
        {
            new StarChest { Stars = 15, Doo = 3000, Insight = 1 },
            new StarChest { Stars = 40, Doo = 6000, ItemId = "ginseng", Insight = 2 },
            new StarChest { Stars = 80, Doo = 10000, ItemId = "ginseng", ItemCount = 2, Insight = 2 },
            new StarChest { Stars = 130, Doo = 16000, ItemId = "ilgeup_yeongdan", Insight = 3 },
            new StarChest { Stars = 200, Doo = 25000, ItemId = "ilgeup_yeongdan", Insight = 3 },
            new StarChest { Stars = 300, Doo = 40000, ItemId = "ilgeup_yeongdan", ItemCount = 2, Insight = 4 },
            new StarChest { Stars = 420, Doo = 60000, ItemId = "ilgeup_yeongdan", ItemCount = 3, Insight = 4 },
            new StarChest { Stars = 560, Doo = 80000, ItemId = "ilgeup_yeongdan", ItemCount = 3, Insight = 5 },
            new StarChest { Stars = 700, Doo = 120000, ItemId = "ilgeup_yeongdan", ItemCount = 5, Insight = 6 },
        };

        public static int MaxStars
        {
            get
            {
                int fights = 0;
                foreach (var st in EarlyGameContent.Stages) fights += st.Encounters.Length;
                return fights * 3 * DifficultyCount;
            }
        }

        public static bool CanClaim(SaveData save, StarChest c) { return Total(save) >= c.Stars && !save.ClaimedStarChests.Contains(c.Stars); }

        public static int ClaimableCount(SaveData save)
        {
            int n = 0;
            foreach (var c in Chests) if (CanClaim(save, c)) n++;
            return n;
        }

        /// <summary>Opens one chest if it is ready; returns a one-line note or null.</summary>
        public static string Claim(SaveData save, StarChest c)
        {
            if (!CanClaim(save, c)) return null;
            save.ClaimedStarChests.Add(c.Stars);
            save.Doo += c.Doo;
            string text = "별 " + c.Stars + "개 보상: 두 +" + c.Doo.ToString("N0");
            var item = string.IsNullOrEmpty(c.ItemId) ? null : EarlyGameContent.FindItem(c.ItemId);
            if (item != null) { Inventory.Add(save, c.ItemId, c.ItemCount); text += ", " + item.Name + (c.ItemCount > 1 ? " x" + c.ItemCount : ""); }
            if (c.Insight > 0) { Inventory.Add(save, InnerArts.StoneId, c.Insight); text += ", 심득석 x" + c.Insight; }
            return text;
        }

        /// <summary>Opens every chest that is ready; returns a one-line note, or null when nothing was ready.</summary>
        public static string ClaimAll(SaveData save)
        {
            long doo = 0; int n = 0, insight = 0;
            var items = new List<string>();
            foreach (var c in Chests)
            {
                if (!CanClaim(save, c)) continue;
                save.ClaimedStarChests.Add(c.Stars);
                save.Doo += c.Doo;
                doo += c.Doo; n++;
                if (c.Insight > 0) { Inventory.Add(save, InnerArts.StoneId, c.Insight); insight += c.Insight; }
                if (!string.IsNullOrEmpty(c.ItemId))
                {
                    var item = EarlyGameContent.FindItem(c.ItemId);
                    if (item != null) { Inventory.Add(save, c.ItemId, c.ItemCount); items.Add(item.Name + (c.ItemCount > 1 ? " x" + c.ItemCount : "")); }
                }
            }
            if (n == 0) return null;
            return "별 보상 " + n + "개: 두 +" + doo.ToString("N0") + (items.Count > 0 ? ", " + string.Join(", ", items) : "") + (insight > 0 ? ", 심득석 x" + insight : "");
        }

        /// <summary>Extra drop rolls on harder difficulties (1 + difficulty rolls per fight).</summary>
        public static int DropRolls(int difficulty) { return 1 + Math.Max(0, Math.Min(DifficultyCount - 1, difficulty)); }
    }
}
