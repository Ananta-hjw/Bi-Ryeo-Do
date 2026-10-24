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
        public static readonly float[] HpScale = { 1.0f, 2.005f, 3.461f };
        public static readonly float[] AtkScale = { 1.0f, 1.602f, 2.434f };
        // Defense is raised only a little: it takes damage x K/(K+Defense), so a few percent already shows (극악 검후 이옥상 takes x0.4 = defense 1500).
        public static readonly float[] DefScale = { 1.0f, 1.0207f, 1.0579f };
        public static readonly float[] RewardFactor = { 1.0f, 1.5f, 2.2f };   // 두 and exp
        public static readonly string[] StarNames = { "돌파", "전원 생존", "신속" };

        /// <summary>Party actions allowed for the 신속 star of each fight (index = stage - 1, then fight). About 90% of the balance bot's median, plus one; the bot plays the 일반 enemies with their HP boost (<see cref="NormalHpBoost"/>) at <c>BalanceSimulation.BotLevel</c>.</summary>
        private static readonly int[][] FastLimits =
        {
            new[] { 4, 8, 7, 10 },   // 1
            new[] { 3, 5, 7, 9, 10 },   // 2
            new[] { 4, 5, 5 },   // 3
            new[] { 5, 8, 7 },   // 4
            new[] { 3, 10 },   // 5
            new[] { 6, 7, 8 },   // 6
            new[] { 5, 5, 6, 6, 5, 9, 11, 10 },   // 7
            new[] { 5, 10, 13 },   // 8
            new[] { 7, 7, 15, 13 },   // 9
            new[] { 6, 14, 13 },   // 10
            new[] { 9, 6, 6, 18 },   // 11
            new[] { 6, 7, 7, 8, 8, 12 },   // 12
            new[] { 12, 18, 18, 17 },   // 13
            new[] { 19, 17, 28 },   // 14
            new[] { 16, 17, 10 },   // 15
            new[] { 18, 15, 12 },   // 16
            new[] { 21, 17, 13 },   // 17
            new[] { 18, 21 },   // 18
            new[] { 28, 21, 19 },   // 19
            new[] { 28, 18, 26 },   // 20
            new[] { 28, 22, 19 },   // 21
            new[] { 33, 27, 28 },   // 22
            new[] { 33, 28, 28 },   // 23
            new[] { 37, 37, 34 },   // 24
            new[] { 35, 32, 26 },   // 25
            new[] { 39, 26, 28 },   // 26
            new[] { 32, 39, 33 },   // 27
            new[] { 26, 39, 31, 29 },   // 28
            new[] { 35, 48, 29, 17, 61 },   // 29
            new[] { 52, 38, 28 },   // 30
            new[] { 49, 47 },   // 31
            new[] { 26, 43 },   // 32
            new[] { 34, 44, 55 },   // 33
        };

        public static int FastLimit(int stage, int fight)
        {
            if (stage >= 1 && stage <= FastLimits.Length && fight >= 0 && fight < FastLimits[stage - 1].Length) return FastLimits[stage - 1][fight];
            return 12 + stage / 2;
        }

        /// <summary>The 신속 limit on a difficulty: harder enemies have more HP, so the limit grows with <see cref="HpFor"/>.</summary>
        public static int FastLimit(int stage, int fight, int difficulty)
        {
            if (difficulty <= 0) return FastLimit(stage, fight);
            // More HP and a little more defense both lengthen the fight. The defense is approximated by the curve at the stage's 일반 level
            // The higher enemy level of the harder difficulties (LevelAdd) and the HP it brings are not counted, so on the early stages,
            // where LevelAdd is largest, the limit mostly follows HpFor.
            var st = EarlyGameContent.FindStage(stage);
            double d0 = st != null ? Progression.StatsFor(st.RecommendedLevel).Defense : 0.0;
            double defEffect = (BattleEngine.DefenseConstant + d0 * DefFor(stage, difficulty)) / (BattleEngine.DefenseConstant + d0);
            // The table is measured on the eased 일반 enemies, while the harder difficulties scale the original ones (see NormalEase); both carry the same stage boost (NormalHpBoost).
            double hp = HpFor(stage, difficulty) / Math.Pow(NormalEase(stage), 0.7);
            return (int)Math.Ceiling(FastLimit(stage, fight) * Math.Max(1.0, hp) * Math.Max(1.0, defEffect));
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
        // The harder difficulties are meant for a protagonist who already cleared the easier one, so their enemies start at a higher level:
        // 어려움 stage 1 is built for level 30 and 극악 stage 1 for level 60, rising in a straight line to the level the last stage already has.
        public static readonly int[] BaseLevel = { 0, 33, 63 };

        /// <summary>The level line of the harder difficulties ends at this stage (the end of 1부); later stages already sit above it.</summary>
        public const int LevelLineStage = 30;

        /// <summary>How many levels every enemy of a stage gets on a difficulty (0 on 일반 or when the stage already is past the line).</summary>
        public static int LevelAdd(int stage, int difficulty)
        {
            difficulty = Clamp(difficulty);
            var st = EarlyGameContent.FindStage(stage);
            var last = EarlyGameContent.FindStage(LevelLineStage);
            if (difficulty == 0 || st == null || last == null) return 0;
            double t = Math.Min(1.0, (stage - 1.0) / (LevelLineStage - 1.0));
            double floor = BaseLevel[difficulty] + (last.RecommendedLevel - BaseLevel[difficulty]) * t;
            return Math.Max(0, (int)Math.Round(floor - st.RecommendedLevel));
        }

        public static float DefFor(int stage, int difficulty) { return 1f + (DefScale[Clamp(difficulty)] - 1f) * Ramp(stage); }
        private static int Clamp(int difficulty) { return Math.Max(0, Math.Min(DifficultyCount - 1, difficulty)); }

        // 일반 난이도를 손본 배율(1~6은 조금 더 세게, 7부터는 동료가 나온 뒤라 많이 약하게). 어려움·극악은 이 조정 전의 원래 수치에서 출발하므로 ScaleEnemy가 되돌려 주고, 그 뒤의 일반 상향(스테이지별 x1.03~1.12)은 어려움·극악에도 그대로 이어진다.
        static readonly float[] NormalEaseTable =
        {
            1f, 1f, 1f, 1.3f, 1.5f, 1.5f, 0.6f, 0.6f, 0.6f, 0.6f, 0.6f, 0.65f, 0.65f, 0.6f, 0.6f,
            0.6f, 0.6f, 0.65f, 0.6f, 0.6f, 0.6f, 0.6f, 0.6f, 0.6f, 0.6f, 0.6f, 0.6f, 0.6f, 0.6f, 0.65f,
            0.6f, 0.6f, 0.6f,
        };

        /// <summary>How much stronger (&gt;1) or weaker (&lt;1) a stage's 일반 enemies are than the enemy table the harder difficulties were tuned against.</summary>
        public static float NormalEase(int stage) { return stage >= 1 && stage <= NormalEaseTable.Length ? NormalEaseTable[stage - 1] : 1f; }

        /// <summary>
        /// 일반 난이도 enemy boost on top of the enemy table: HP mostly (x1.1 on stage 1 up to x2.6 on stage 30, a little more every stage), attack a little.
        /// Stages 1-6 are fought alone and sit on the balance bot's cliff, so their HP is kept lower (see Docs/BALANCE.md). 어려움·극악 get the same boost on top of their own scaling.
        /// </summary>
        static readonly float[] NormalHpBoostTable =
        {
            1.00f, 1.25f, 1.25f, 1.20f, 1.10f, 1.00f, 1.45f, 1.50f, 1.55f, 1.60f, 1.65f, 1.70f, 1.75f, 1.80f, 1.85f,
            1.90f, 1.95f, 2.00f, 2.05f, 2.10f, 2.15f, 2.20f, 2.25f, 2.30f, 2.35f, 2.40f, 2.45f, 2.50f, 2.55f, 2.60f,
            2.65f, 2.70f, 2.75f,
        };
        /// <summary>
        /// Player levels a prepared player (gear, 심법, mastery, items) is expected to have over the stage's recommended level now that 일반 enemies have more HP:
        /// 0 for the solo stages 1-6, then (stage - 4) / 3 from stage 7 (+1) to stage 30 (+8). The balance bot is given the same levels, and the 추천 경지 of 일반 stages counts them.
        /// </summary>
        public static int NormalEdgeLevels(int stage) { return stage <= 6 ? 0 : (stage - 4) / 3; }

        public static float NormalHpBoost(int stage) { return NormalHpBoostTable[Math.Max(1, Math.Min(NormalHpBoostTable.Length, stage)) - 1]; }
        // Per-stage correction of the attack boost (stage 1-6 = 1): stages whose enemies hit far harder than their neighbours (a heavy blow of more than about
        // 100% / 75% / 45% of the protagonist's HP on 보스 / 정예 / 일반) are eased, at most x0.75; see Docs/BALANCE.md.
        static readonly float[] NormalAtkTuneTable =
        {
            1f, 1f, 1f, 1f, 1f, 1f, 0.75f, 1f, 0.75f, 0.87f, 0.98f, 0.82f, 0.89f, 0.98f, 0.97f,
            1.06f, 1f, 0.76f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 0.96f, 1f, 1f, 1f,
            1f, 1f, 1f,
        };
        public static float NormalAtkBoost(int stage)
        {
            if (stage <= 6) return 1f;
            return (1.05f + 0.005f * (Math.Min(33, stage) - 7)) * NormalAtkTuneTable[Math.Min(NormalAtkTuneTable.Length, stage) - 1];
        }

        public static EnemyData ScaleEnemy(EnemyData enemy, int stage, int difficulty)
        {
            difficulty = Clamp(difficulty);
            // Shorten the longest normal stages without removing pressure. Survival-only enemies keep their rules/stats.
            // Hard difficulties still start from the original table, so this does not move their progression targets.
            if (difficulty == 0)
            {
                if (enemy.HpMul > 10f) return enemy;   // survival-only enemies keep their stats
                float hp = NormalHpBoost(stage), atk = NormalAtkBoost(stage);
                if (stage >= 24 && stage <= 33) { hp *= 0.85f; atk *= 1.25f; }
                return enemy.Scaled(hp, atk);
            }
            float ease = NormalEase(stage);
            if (enemy.HpMul > 10f) return enemy.Scaled(HpFor(stage, difficulty) / (float)Math.Pow(ease, 0.7), AtkFor(stage, difficulty) / (float)Math.Pow(ease, 0.3), DefFor(stage, difficulty), LevelAdd(stage, difficulty));   // survival-only enemies are not boosted
            return enemy.Scaled(HpFor(stage, difficulty) / (float)Math.Pow(ease, 0.7) * NormalHpBoost(stage), AtkFor(stage, difficulty) / (float)Math.Pow(ease, 0.3) * NormalAtkBoost(stage), DefFor(stage, difficulty), LevelAdd(stage, difficulty));
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
            int n = save.RetiredStoryStars;
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

        /// <summary>Protagonist level needed before a difficulty can be entered at all: 어려움 from level 20, 극악 from level 40.</summary>
        public static readonly int[] MinLevel = { 0, 10, 20 };   // player levels (stage 30 on 일반 is level 25)

        /// <summary>
        /// A harder difficulty needs the protagonist level, then opens stage by stage: the same stage must be cleared on the difficulty
        /// below AND the previous stage on this one.
        /// </summary>
        public static bool Unlocked(SaveData save, int stage, int difficulty)
        {
            return LockLabel(save, stage, difficulty).Length == 0;
        }

        /// <summary>Short button text for why a stage is closed on a difficulty ("" when it is open).</summary>
        public static string LockLabel(SaveData save, int stage, int difficulty)
        {
            difficulty = Clamp(difficulty);
            if (difficulty <= 0) return "";
            if (save.Level < MinLevel[difficulty]) return "Lv." + MinLevel[difficulty] + "\n필요";
            if (!StageCleared(save, stage, difficulty - 1)) return DifficultyNames[difficulty - 1] + "\n돌파 필요";
            if (stage > 1 && !StageCleared(save, stage - 1, difficulty)) return "앞 스테이지\n돌파 필요";
            return "";
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
            new StarChest { Stars = 15, Doo = 4680, Insight = 1 },
            new StarChest { Stars = 40, Doo = 9360, ItemId = "ginseng", Insight = 2 },
            new StarChest { Stars = 80, Doo = 15600, ItemId = "ginseng", ItemCount = 2, Insight = 2 },
            new StarChest { Stars = 130, Doo = 24960, ItemId = "ilgeup_yeongdan", Insight = 3 },
            new StarChest { Stars = 200, Doo = 39000, ItemId = "ilgeup_yeongdan", Insight = 3 },
            new StarChest { Stars = 300, Doo = 62400, ItemId = "ilgeup_yeongdan", ItemCount = 2, Insight = 4 },
            new StarChest { Stars = 420, Doo = 93600, ItemId = "ilgeup_yeongdan", ItemCount = 3, Insight = 4 },
            new StarChest { Stars = 560, Doo = 124800, ItemId = "ilgeup_yeongdan", ItemCount = 3, Insight = 5 },
            new StarChest { Stars = 700, Doo = 187200, ItemId = "ilgeup_yeongdan", ItemCount = 5, Insight = 6 },
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
