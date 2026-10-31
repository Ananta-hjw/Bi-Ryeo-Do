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
    // Noncombat event slots grant all three progress stars on completion; no survival or speed check.
    public static class StageStars
    {
        public const int DifficultyCount = 3;
        public static readonly string[] DifficultyNames = { "일반", "어려움", "극악" };
        /// <summary>
        /// 어려움·극악 재조정: a harder difficulty opens once <see cref="OpenStage"/> is cleared on the difficulty below.
        /// Its stage 1 is built to feel like the entry stage of the difficulty below (어려움 1 ≈ 일반 40, 극악 1 ≈ 어려움 40), and every later stage rises from there at <see cref="HardSlope"/> of the 일반 slope.
        /// The difficulty is measured in the level the balance bot needs (<see cref="TargetLevel"/>); the enemies are the 일반 enemies of the same stage,
        /// raised by the curve levels the bot gains over 일반 (<see cref="LevelAdd"/>), with a fitted HP/attack trade that keeps the fights about as long as on 일반 (<see cref="HardLengthTable"/>).
        /// </summary>
        public const int EntryStage = 40;
        /// <summary>
        /// Stage that must be cleared on the difficulty below to open a harder one (index = difficulty: 일반 30 → 어려움, 어려움 40 → 극악).
        /// 어려움 opens a little before its stage 1 (built like 일반 <see cref="EntryStage"/>); a save that already won on the harder difficulty
        /// under the old rule (일반/어려움 20) keeps it open (<see cref="AlreadyOpen"/>).
        /// </summary>
        public static readonly int[] OpenStages = { 0, 30, 40 };
        public static int OpenStage(int difficulty) { return OpenStages[Clamp(difficulty)]; }
        const int LegacyOpenStage = 20;
        public const double HardSlope = 0.75;

        /// <summary>
        /// Raised level for level, the enemies of a harder difficulty would last about twice (어려움) or three times (극악) as long as on 일반, because HP grows
        /// faster than damage up the curve. This table (index = difficulty, then stage - 1, all 66 stages; fitted with the balance bot for the 일반 40 anchor) shortens them back to about
        /// <see cref="HardLengthTarget"/> times the 일반 fight: HP / f, attack x f^<see cref="HardAtkExp"/> so the shorter fight still hurts as much.
        /// </summary>
        public static readonly float[][] HardLengthTable =
        {
            null,
            new[]
            {
                0.758f, 2.079f, 6.000f, 6.000f, 2.079f, 1.820f, 2.363f, 3.856f, 3.442f, 1.903f, 4.010f, 4.241f,
                4.079f, 4.765f, 4.708f, 4.280f, 4.439f, 3.735f, 4.069f, 1.118f, 4.679f, 4.335f, 2.396f, 4.263f,
                3.100f, 3.808f, 1.278f, 3.845f, 2.122f, 2.942f, 0.548f, 0.939f, 4.152f, 2.186f, 0.500f, 0.500f,
                1.543f, 3.467f, 4.407f, 0.888f, 0.985f, 0.500f, 0.517f, 2.972f, 3.074f, 2.303f, 0.658f, 2.920f,
                0.789f, 2.369f, 3.154f, 3.384f, 0.658f, 0.974f, 0.526f, 2.401f, 0.658f, 1.000f, 1.000f, 1.906f,
                0.500f, 1.775f, 0.658f, 0.658f, 0.500f, 0.684f,
            },
            new[]
            {
                1.020f, 2.949f, 6.000f, 6.000f, 2.705f, 2.151f, 3.850f, 5.940f, 5.532f, 1.709f, 6.000f, 6.000f,
                6.000f, 6.000f, 6.000f, 6.000f, 6.000f, 5.650f, 6.000f, 1.618f, 6.000f, 6.000f, 3.933f, 6.000f,
                4.871f, 6.000f, 1.278f, 6.000f, 2.875f, 4.650f, 0.500f, 1.554f, 6.000f, 3.511f, 0.500f, 0.500f,
                2.203f, 5.848f, 6.000f, 0.975f, 1.491f, 0.500f, 0.517f, 4.674f, 4.655f, 3.753f, 0.658f, 4.780f,
                0.658f, 3.532f, 4.978f, 5.524f, 0.658f, 1.344f, 0.526f, 3.813f, 0.658f, 1.000f, 1.000f, 3.096f,
                0.500f, 2.724f, 0.658f, 0.658f, 0.500f, 0.571f,
            },
        };
        public const float HardLengthTarget = 1.15f;
        public static double HardAtkExp = 0.85;

        static float HardLength(int stage, int difficulty)
        {
            var t = HardLengthTable[Clamp(difficulty)];
            return t == null ? 1f : t[Math.Max(1, Math.Min(t.Length, stage)) - 1];
        }

        /// <summary>Enemy HP factor of a harder difficulty on top of the raised 일반 enemy.</summary>
        public static float HardHpFix(int stage, int difficulty) { return 1f / HardLength(stage, difficulty); }

        /// <summary>Enemy attack factor of a harder difficulty on top of the raised 일반 enemy.</summary>
        public static float HardAtkFix(int stage, int difficulty)
        {
            var tune = HardAtkTune[Clamp(difficulty)];
            float t = tune == null ? 1f : tune[Math.Max(1, Math.Min(tune.Length, stage)) - 1];
            return (float)Math.Pow(HardLength(stage, difficulty), HardAtkExp) * t;
        }

        /// <summary>Per-stage attack tune of the harder difficulties (index = difficulty, then stage - 1), fitted with the balance bot so each stage leaves it about as much HP as the same 일반 stage.</summary>
        public static readonly float[][] HardAtkTune =
        {
            null,
            new[]
            {
                2.459f, 1.150f, 1.400f, 1.310f, 1.000f, 0.735f, 2.459f, 2.419f, 2.459f, 2.459f, 2.459f, 1.447f,
                1.572f, 1.572f, 1.950f, 1.129f, 1.918f, 1.625f, 2.049f, 2.459f, 0.710f, 1.332f, 1.377f, 1.148f,
                1.332f, 0.300f, 1.521f, 0.622f, 0.989f, 1.377f, 2.459f, 2.459f, 1.447f, 1.354f, 2.459f, 2.459f,
                2.459f, 0.811f, 1.129f, 0.425f, 1.625f, 2.459f, 2.459f, 1.056f, 2.459f, 2.459f, 2.459f, 2.459f,
                2.459f, 2.459f, 2.459f, 2.459f, 2.459f, 2.459f, 2.459f, 2.016f, 2.459f, 2.459f, 2.459f, 2.459f,
                2.459f, 2.459f, 2.459f, 2.459f, 2.459f, 2.459f,
            },
            new[]
            {
                2.459f, 1.307f, 1.521f, 1.766f, 1.000f, 0.879f, 2.459f, 2.459f, 2.459f, 2.459f, 2.340f, 2.190f,
                1.950f, 1.766f, 1.546f, 1.680f, 2.301f, 1.598f, 2.084f, 2.459f, 0.622f, 1.572f, 1.289f, 1.092f,
                1.423f, 0.300f, 1.983f, 0.501f, 0.925f, 1.289f, 2.459f, 2.459f, 1.471f, 1.226f, 2.459f, 2.459f,
                2.459f, 0.675f, 1.056f, 0.372f, 1.546f, 2.459f, 2.459f, 0.941f, 2.459f, 2.459f, 2.459f, 2.459f,
                2.459f, 2.459f, 2.459f, 2.459f, 2.459f, 2.459f, 2.459f, 1.795f, 2.459f, 2.459f, 2.459f, 2.459f,
                2.459f, 2.459f, 2.459f, 2.459f, 2.459f, 2.459f,
            },
        };
        public static readonly float[] RewardFactor = { 1.0f, 1.5f, 2.2f };   // 두 and exp
        public static readonly string[] StarNames = { "돌파", "전원 생존", "신속" };

        /// <summary>Party actions allowed for the 신속 star of each fight (index = stage - 1, then fight). About 90% of the balance bot's median, plus one; fights that gained followers (전투 인원 확장) were scaled by the bot's new median over its old one; the bot plays the 일반 enemies with their HP boost (<see cref="NormalHpBoost"/>) at <c>BalanceSimulation.BotLevel</c>.</summary>
        private static readonly int[][] FastLimits =
        {
            new[] { 4, 8, 7, 10 },   // 1
            new[] { 9, 10, 11, 9, 18 },   // 2
            new[] { 6, 9, 8 },   // 3
            new[] { 8, 10, 10 },   // 4
            new[] { 11, 10 },   // 5
            new[] { 8, 12, 8 },   // 6
            new[] { 5, 5, 6, 6, 5, 9, 11, 10 },   // 7
            new[] { 12, 13, 19 },   // 8
            new[] { 19, 19, 27, 13 },   // 9
            new[] { 10, 12, 18 },   // 10
            new[] { 9, 5, 5, 23 },   // 11
            new[] { 8, 7, 7, 8, 8, 19 },   // 12
            new[] { 12, 23, 27, 26 },   // 13
            new[] { 11, 17, 14, 11, 12, 22, 18, 22, 22, 16, 39 },   // 14
            new[] { 12, 21, 14, 8, 18 },   // 15
            new[] { 29, 22, 24 },   // 16
            new[] { 15, 10, 24 },   // 17
            new[] { 10, 16, 25, 30 },   // 18
            new[] { 21, 20, 21, 24, 18, 55 },   // 19
            new[] { 43, 26, 37 },   // 20
            new[] { 57, 43, 22 },   // 21
            new[] { 30, 61, 42, 61, 48, 50 },   // 22
            new[] { 46, 37, 22, 48 },   // 23
            new[] { 51, 56, 52, 28 },   // 24
            new[] { 11, 55, 38, 38 },   // 25
            new[] { 45, 28, 45, 66, 11, 45 },   // 26
            new[] { 28, 18, 18, 18, 18, 18, 18 },   // 27
            new[] { 28, 22, 46, 34, 36 },   // 28
            new[] { 38, 55, 27, 30, 28, 28, 32 },   // 29
            new[] { 80, 55, 50, 38 },   // 30
            new[] { 3, 7, 8 },   // 31
            new[] { 14, 5, 11 },   // 32
            new[] { 8, 8, 20, 5 },   // 33
            new[] { 8, 35, 12 },   // 34
            new[] { 34, 5, 6, 10 },   // 35
            new[] { 9, 5, 5, 8 },   // 36
            new[] { 6, 5 },   // 37
            new[] { 8, 16, 11 },   // 38
            new[] { 8, 8, 12 },   // 39
            new[] { 17, 18, 27, 7 },   // 40
            new[] { 8 },   // 41
            new[] { 7, 4, 4, 4 },   // 42
            new[] { 7, 9, 3 },   // 43
            new[] { 3, 13, 3, 11, 10 },   // 44
            new[] { 18, 3, 3, 3 },   // 45
            new[] { 14, 4, 10, 3, 4 },   // 46
            new[] { 21 },   // 47
            new[] { 22, 17, 17 },   // 48
            new[] { 8, 3, 4, 4, 4, 3, 4 },   // 49
            new[] { 25, 7, 3, 4 },   // 50
            new[] { 29, 17, 4, 4 },   // 51
            new[] { 27, 3 },   // 52
            new[] { 4, 3, 4 },   // 53
            new[] { 29, 6, 8, 7, 4 },   // 54
            new[] { 3, 3, 4, 4, 10 },   // 55
            new[] { 5, 7, 36, 4, 4 },   // 56
            new[] { 4 },   // 57
            new[] { 3, 3, 5 },   // 58
            new[] { 6, 6, 3 },   // 59
            new[] { 21, 7, 6, 5 },   // 60
            new[] { 4, 4, 4, 11 },   // 61
            new[] { 15, 11, 7 },   // 62
            new[] { 7, 8, 9, 11 },   // 63
            new[] { 4, 4, 4, 4 },   // 64
            new[] { 3, 6, 11 },   // 65
            new[] { 7, 4, 4, 4, 25 },   // 66
        };

        public static int FastLimit(int stage, int fight)
        {
            if (stage >= 1 && stage <= FastLimits.Length && fight >= 0 && fight < FastLimits[stage - 1].Length) return FastLimits[stage - 1][fight];
            return 12 + stage / 2;
        }

        /// <summary>The 신속 limit on a difficulty: harder fights are fitted to last about <see cref="HardLengthTarget"/> times the 일반 one (the bot measured x1.1-1.3; 극악 a little longer).</summary>
        public static int FastLimit(int stage, int fight, int difficulty)
        {
            if (difficulty <= 0) return FastLimit(stage, fight);
            return (int)Math.Ceiling(FastLimit(stage, fight) * FastLimitFactor[Clamp(difficulty)]);
        }

        static readonly float[] FastLimitFactor = { 1f, 1.2f, 1.3f };

        public static int FightCount(int stage, int difficulty = 0)
        {
            var st = EarlyGameContent.FindStage(stage);
            if (st == null) return 0;
            return difficulty > 0 ? st.WithDifficulty(difficulty).Encounters.Length : st.Encounters.Length;   // 일반 30 has fewer fights (FireDragonFinale)
        }

        /// <summary>Stars a stage can give on one difficulty (3 per fight).</summary>
        public static int MaxStarsOfStage(int stage, int difficulty = 0) { return FightCount(stage, difficulty) * 3; }

        /// <summary>The level the balance bot plays a 일반 stage at: the recommended level plus <see cref="NormalEdgeLevels"/>.</summary>
        public static int NormalBotLevel(int stage)
        {
            var st = EarlyGameContent.FindStage(stage);
            return st == null ? 1 : st.RecommendedPlayerLevel + NormalEdgeLevels(stage);
        }

        /// <summary>
        /// Bot level a stage is built for on a difficulty. 일반 = <see cref="NormalBotLevel"/>; a harder one starts at the entry stage of the difficulty below
        /// and climbs <see cref="HardSlope"/> as fast as 일반 does from its stage 1 (어려움 1 ≈ 일반 40, 극악 1 ≈ 어려움 40).
        /// </summary>
        public static double TargetLevel(int stage, int difficulty)
        {
            difficulty = Clamp(difficulty);
            if (difficulty == 0) return NormalBotLevel(stage);
            return TargetLevel(EntryStage, difficulty - 1) + HardSlope * (NormalBotLevel(stage) - NormalBotLevel(1));
        }

        /// <summary>권장 레벨 shown for a stage on a difficulty: the 일반 recommendation plus the levels the bot needs over 일반 there.</summary>
        public static int RecommendedPlayerLevel(int stage, int difficulty)
        {
            var st = EarlyGameContent.FindStage(stage);
            if (st == null) return 1;
            return st.RecommendedPlayerLevel + (Clamp(difficulty) == 0 ? 0 : (int)Math.Round(TargetLevel(stage, difficulty) - NormalBotLevel(stage)));
        }

        /// <summary>Curve levels every enemy of a stage gets on a difficulty: the bot's extra levels over 일반, in enemy curve levels (0 on 일반).</summary>
        public static int LevelAdd(int stage, int difficulty)
        {
            difficulty = Clamp(difficulty);
            if (difficulty == 0 || EarlyGameContent.FindStage(stage) == null) return 0;
            return Math.Max(0, (int)Math.Round((TargetLevel(stage, difficulty) - NormalBotLevel(stage)) * Progression.CurvePerPlayerLevel));
        }

        private static int Clamp(int difficulty) { return Math.Max(0, Math.Min(DifficultyCount - 1, difficulty)); }

        /// <summary>
        /// 일반 난이도 enemy boost on top of the enemy table: HP mostly (x1.1 on stage 1 up to x2.6 on stage 30, a little more every stage), attack a little.
        /// Stages 1-6 are fought alone and sit on the balance bot's cliff, so their HP is kept lower (see Docs/BALANCE.md). 어려움·극악 get the same boost on top of their own scaling.
        /// </summary>
        static readonly float[] NormalHpBoostTable =
        {
            1.00f, 1.25f, 1.25f, 1.20f, 1.10f, 1.00f, 1.45f, 1.50f, 1.55f, 1.60f, 1.65f, 1.70f, 1.75f, 1.80f, 1.85f,
            1.90f, 1.95f, 2.00f, 2.05f, 2.10f, 2.15f, 2.20f, 2.25f, 2.30f, 2.35f, 2.40f, 2.45f, 2.50f, 2.55f, 2.60f,
            2.65f, 2.70f, 2.75f, 2.80f, 2.85f, 2.90f,
        };
        /// <summary>
        /// Player levels a prepared player (gear, 심법, mastery, items) is expected to have over the stage's recommended level now that 일반 enemies have more HP:
        /// 0 for the solo stages 1-6, then (stage - 4) / 3 from stage 7 (+1) to stage 30 (+8). The balance bot is given the same levels, and the 추천 경지 of 일반 stages counts them.
        /// </summary>
        public static int NormalEdgeLevels(int stage) { return (stage <= 6 ? 0 : (stage - 4) / 3) + NormalExtraEdgeLevels(stage); }

        /// <summary>
        /// 일반 상향(2026-10 말): the balance bot plays stages 10+ this many more levels above the recommendation (+2 on stage 10, rising to +8 on stage 36),
        /// and the stage's enemies are hardened by <see cref="NormalHarden"/> so that the stronger bot wins about as often as before. A prepared player needs more.
        /// </summary>
        public static int NormalExtraEdgeLevels(int stage) { return stage < 10 ? 0 : (int)Math.Round(2.0 + 6.0 * (Math.Min(36, stage) - 10) / 26.0); }

        /// <summary>Enemy strength factor per 일반 stage (index = stage - 1; HP x f^HardenHpExp, attack x f^HardenAtkExp), fitted with the balance bot at the raised levels. 어려움·극악 carry it too.</summary>
        public static float[] NormalHardenTable =
        {
            1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1.93f, 2.32f, 1.94f, 2.03f, 1.89f, 2.08f, 3.11f, 2.49f, 1.59f,
            3.12f, 3.50f, 2.57f, 2.92f, 3.50f, 2.08f, 2.21f, 2.40f, 2.07f, 1.26f, 1.00f, 2.30f, 1.70f, 1.65f, 1.97f, 1.94f, 1.98f, 2.09f,
        };
        public static double HardenHpExp = 0.5, HardenAtkExp = 0.5;   // half HP, half attack: fights get longer and deadlier, but not slogs
        public static float NormalHarden(int stage) { return stage >= 1 && stage <= NormalHardenTable.Length ? NormalHardenTable[stage - 1] : NormalHardenTable[NormalHardenTable.Length - 1]; }

        public static float NormalHpBoost(int stage) { return NormalHpBoostTable[Math.Max(1, Math.Min(NormalHpBoostTable.Length, stage)) - 1] * (float)Math.Pow(NormalHarden(stage), HardenHpExp); }
        // Per-stage correction of the attack boost (stage 1-6 = 1): stages whose enemies hit far harder than their neighbours (a heavy blow of more than about
        // 100% / 75% / 45% of the protagonist's HP on 보스 / 정예 / 일반) are eased, at most x0.75; see Docs/BALANCE.md.
        static readonly float[] NormalAtkTuneTable =
        {
            1f, 1f, 1f, 1f, 1f, 1f, 0.75f, 1f, 0.75f, 0.87f, 0.98f, 0.82f, 0.89f, 0.98f, 0.97f,
            1.06f, 1f, 0.76f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 0.96f, 1f, 1f, 1f,
            1f, 1f, 1f, 1f, 1f, 1f,
        };
        public static float NormalAtkBoost(int stage)
        {
            if (stage <= 6) return 1f;
            return (1.05f + 0.005f * (Math.Min(36, stage) - 7)) * NormalAtkTuneTable[Math.Min(NormalAtkTuneTable.Length, stage) - 1] * (float)Math.Pow(NormalHarden(stage), HardenAtkExp);
        }

        /// <summary>일반 난이도 적 공격·방어 배율: 스테이지 7 x0.93, 8 x0.87, 9부터 x0.8로 점점 낮춘다(어려움·극악은 그대로).</summary>
        static readonly float[] NormalStatEaseRamp = { 0.93f, 0.87f };
        public const float NormalStatEase = 0.8f;
        public const int NormalStatEaseFromStage = 7;

        public static float NormalStatEaseFor(int stage)
        {
            if (stage < NormalStatEaseFromStage) return 1f;
            int i = stage - NormalStatEaseFromStage;
            return i < NormalStatEaseRamp.Length ? NormalStatEaseRamp[i] : NormalStatEase;
        }

        /// <summary>일반 난이도 적 체력 배율. 균형 봇으로 잰 현재 난이도에 맞춰 어려운 스테이지일수록 조금 더 줄인다
        /// (봇이 남기는 체력 30% 미만 x0.90, 72% 미만 x0.93, 85% 이하 x0.95, 그 위 x0.97). 버티기 시험 전투는 줄이지 않는다.</summary>
        static readonly float[] NormalHpTrimTable =
        {
            0.90f, 0.97f, 0.97f, 0.90f, 0.90f, 0.90f, 0.95f, 0.93f, 0.97f, 0.93f,   //  1-10
            0.93f, 0.95f, 0.93f, 0.93f, 0.95f, 0.97f, 0.97f, 0.95f, 0.97f, 0.97f,   // 11-20
            0.97f, 0.97f, 0.95f, 0.95f, 0.97f, 0.97f, 0.97f, 0.97f, 0.97f, 0.97f,   // 21-30
            0.95f, 0.97f, 0.95f, 0.97f, 0.97f, 0.95f,                               // 31-36
        };

        public static float NormalHpTrim(int stage) { return stage >= 1 ? NormalHpTrimTable[Math.Min(stage, NormalHpTrimTable.Length) - 1] : 1f; }

        /// <summary>Stage 6's boss 호천상 keeps full HP: the bot (solo, at its level) lost far more often with a weaker boss there (87% → 41%), one level up it made no difference.</summary>
        public static float NormalHpTrim(int stage, EnemyData enemy) { return stage == 6 && enemy != null && enemy.IsBoss ? 1f : NormalHpTrim(stage); }

        public static EnemyData ScaleEnemy(EnemyData enemy, int stage, int difficulty, bool survive = false)
        {
            difficulty = Clamp(difficulty);
            if (difficulty == 0)
            {
                // 일반 only: attack and defense eased from stage 7 (x0.93, x0.87, then x0.8); the harder difficulties keep their values.
                if (enemy.HpMul > 10f) return enemy;   // survival-only enemies keep their stats
                float soft = NormalStatEaseFor(stage);
                float hp = NormalHpBoost(stage), atk = NormalAtkBoost(stage);
                if (stage >= 24) { hp *= 0.85f; atk *= 1.25f; }
                if (!survive) hp *= NormalHpTrim(stage, enemy);   // a 버티기 fight keeps its enemy's HP
                return enemy.Scaled(hp, atk * soft, soft);
            }
            // 어려움·극악: the 일반 enemy of the same stage (and fight), raised by the bot's extra levels, times the fitted correction.
            return ScaleEnemy(enemy, stage, 0, survive).Scaled(HardHpFix(stage, difficulty), HardAtkFix(stage, difficulty), 1f, LevelAdd(stage, difficulty));
        }

        /// <summary>The boss of every fifth story stage from 10 (10, 15, … 35) has HP, attack and defense x1.3 on every difficulty (story stages only; 의뢰·무한 비무대 clones are unchanged).</summary>
        public const float MilestoneBossBoost = 1.3f;
        public const int MilestoneEvery = 5, MilestoneFromStage = 10;

        public static bool IsMilestoneBoss(EnemyData enemy, int stage)
        {
            return enemy != null && enemy.IsBoss && stage >= MilestoneFromStage && stage % MilestoneEvery == 0;
        }

        /// <summary>
        /// A story stage's enemy: <see cref="ScaleEnemy"/> for the difficulty, then dressed in <see cref="GearLoadout.ForEnemies"/> (real gear a little below
        /// what a player is expected to wear). Beasts, traps and objects (<see cref="Realm.HasRealm"/>) wear nothing, nor do the enemies of a 시험 other than
        /// 버티기 (제한 행동·생명줄·합심: their limits were set for the bare enemy); the solo 일반 stages 1-6 have no enemy gear.
        /// </summary>
        public static EnemyData StoryEnemy(EnemyData enemy, int stage, int difficulty, TrialRule trial = null)
        {
            bool survive = trial != null && trial.Kind == TrialKind.Survive;
            var scaled = ScaleEnemy(enemy, stage, difficulty, survive);
            if (IsMilestoneBoss(enemy, stage)) scaled = scaled.Scaled(MilestoneBossBoost, MilestoneBossBoost, MilestoneBossBoost);
            var gear = GearLoadout.ForEnemies(stage, difficulty);
            return gear == null || !Realm.HasRealm(enemy) || (trial != null && !survive) ? scaled : scaled.WithGear(gear);
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
            int n = 0, fights = FightCount(stage, difficulty);   // a fight a stage no longer has (일반 30's old fourth) is not counted here
            foreach (var e in save.StageStars) if (e.Stage == stage && e.Diff == difficulty && e.Fight < fights) n += Count(e.Mask);
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
            int last = FightCount(stage, difficulty) - 1;
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

        /// <summary>
        /// A harder difficulty opens once <see cref="OpenStage"/> is cleared on the difficulty below (일반 30 → 어려움, 어려움 40 → 극악),
        /// then stage by stage: the same stage must be cleared on the difficulty below AND the previous stage on this one.
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
            int open = OpenStage(difficulty);
            if (!StageCleared(save, open, difficulty - 1) && !AlreadyOpen(save, difficulty)) return DifficultyNames[difficulty - 1] + " " + open + "\n돌파 필요";
            if (!StageCleared(save, stage, difficulty - 1)) return DifficultyNames[difficulty - 1] + "\n돌파 필요";
            if (stage > 1 && !StageCleared(save, stage - 1, difficulty)) return "앞 스테이지\n돌파 필요";
            return "";
        }

        /// <summary>A save from before <see cref="OpenStage"/> rose (old rule: stage 20 of the difficulty below) that already won a fight on this difficulty keeps it open.</summary>
        public static bool AlreadyOpen(SaveData save, int difficulty)
        {
            if (!StageCleared(save, LegacyOpenStage, difficulty - 1)) return false;
            foreach (var e in save.StageStars) if (e.Diff == difficulty && e.Mask != 0) return true;
            return false;
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
                // Recorded stages may have newly inserted fights; backfill only entirely legacy stages.
                bool recorded = false;
                foreach (var entry in save.StageStars)
                    if (entry.Stage == st.Number && entry.Diff == 0) { recorded = true; break; }
                if (recorded) continue;
                // Preserve the removed normal-star slot when backfilling an entirely pre-star chapter.
                if (st.Number == 19 || st.Number == 31 || st.Number == 32 || st.Number == 35 || st.Number == 47 || st.Number == 48 || st.Number == 57 || st.Number == 62 || st.Number == 65) save.RetiredStoryStars += st.Number == 48 ? 2 : 1;
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
                foreach (var st in EarlyGameContent.Stages)
                    for (int d = 0; d < DifficultyCount; d++) fights += FightCount(st.Number, d);
                return fights * 3;
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
            string text = "별 " + c.Stars + "개 보상: 은자 +" + c.Doo.ToString("N0");
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
            return "별 보상 " + n + "개: 은자 +" + doo.ToString("N0") + (items.Count > 0 ? ", " + string.Join(", ", items) : "") + (insight > 0 ? ", 심득석 x" + insight : "");
        }

        /// <summary>Extra drop rolls on harder difficulties (1 + difficulty rolls per fight).</summary>
        public static int DropRolls(int difficulty) { return 1 + Math.Max(0, Math.Min(DifficultyCount - 1, difficulty)); }
    }
}
