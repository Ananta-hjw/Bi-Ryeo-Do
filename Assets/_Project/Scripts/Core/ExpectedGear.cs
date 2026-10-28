using System;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// The gear a player is assumed to wear on a story stage, as multipliers on gearless stats (HP, 내공, attack, defense) plus flat speed and crit.
    /// Measured with a player who earns 은자 from stages, quests, star chests and achievements (no daily missions), pulls, enhances and equips by
    /// <see cref="EquipmentRecommendation"/>; only <see cref="Share"/> of that bonus is assumed so that unlucky pulls still get through.
    /// The 일반 추천 경지 counts it; <see cref="GearLoadout"/> turns it into real gear for the balance bot and (a little lower) for story enemies. See Docs/BALANCE.md.
    /// </summary>
    public static class ExpectedGear
    {
        /// <summary>Share of the measured gear bonus the enemies assume.</summary>
        public const float Share = 0.7f;

        // Measured gear (before <see cref="Share"/>) by player level: level, HP, 내공, attack, defense (ratios), speed, crit (flat); party average,
        // 8 campaigns, the first time the level is reached (일반 1-36, then 어려움 and 극악 7-36), never falling. The first row is the start kit.
        private static readonly float[][] Table =
        {
            new[] { 2f, 1.153f, 1.000f, 1.220f, 1.139f, -2.0f, 0.000f },
            new[] { 3f, 1.153f, 1.000f, 1.235f, 1.139f, -2.0f, 0.000f },
            new[] { 4f, 1.158f, 1.000f, 1.238f, 1.139f, -2.0f, 0.000f },
            new[] { 6f, 1.160f, 1.000f, 1.240f, 1.150f, -2.0f, 0.000f },
            new[] { 7.5f, 1.279f, 1.064f, 1.388f, 1.287f, 8.6f, 0.076f },
            new[] { 8.5f, 1.396f, 1.064f, 1.436f, 1.295f, 12.3f, 0.083f },
            new[] { 10f, 1.396f, 1.070f, 1.436f, 1.295f, 12.3f, 0.083f },
            new[] { 11f, 1.396f, 1.070f, 1.436f, 1.295f, 12.3f, 0.083f },
            new[] { 12f, 1.396f, 1.070f, 1.436f, 1.295f, 12.3f, 0.083f },
            new[] { 13f, 1.396f, 1.070f, 1.436f, 1.295f, 12.3f, 0.083f },
            new[] { 14f, 1.396f, 1.084f, 1.436f, 1.295f, 12.3f, 0.083f },
            new[] { 15f, 1.462f, 1.095f, 1.436f, 1.375f, 16.1f, 0.083f },
            new[] { 16f, 1.481f, 1.110f, 1.463f, 1.419f, 18.1f, 0.091f },
            new[] { 17f, 1.485f, 1.110f, 1.466f, 1.422f, 18.2f, 0.091f },
            new[] { 18.3f, 1.690f, 1.141f, 1.595f, 1.478f, 23.0f, 0.108f },
            new[] { 20f, 1.770f, 1.159f, 1.623f, 1.536f, 25.8f, 0.119f },
            new[] { 21.3f, 1.821f, 1.171f, 1.689f, 1.567f, 27.2f, 0.133f },
            new[] { 23f, 1.873f, 1.185f, 1.732f, 1.623f, 29.0f, 0.145f },
            new[] { 24f, 1.916f, 1.185f, 1.742f, 1.665f, 29.5f, 0.145f },
            new[] { 25f, 1.931f, 1.189f, 1.753f, 1.693f, 30.2f, 0.149f },
            new[] { 26f, 1.982f, 1.192f, 1.802f, 1.714f, 31.6f, 0.165f },
            new[] { 27f, 1.989f, 1.197f, 1.841f, 1.787f, 32.4f, 0.172f },
            new[] { 28f, 2.017f, 1.197f, 1.842f, 1.819f, 33.3f, 0.175f },
            new[] { 29f, 2.079f, 1.221f, 1.906f, 1.854f, 35.0f, 0.187f },
            new[] { 30f, 2.146f, 1.226f, 1.931f, 1.923f, 36.8f, 0.196f },
            new[] { 31f, 2.284f, 1.267f, 1.999f, 2.001f, 41.3f, 0.200f },
            new[] { 32f, 2.342f, 1.275f, 2.206f, 2.026f, 43.5f, 0.253f },
            new[] { 33f, 2.358f, 1.289f, 2.206f, 2.039f, 43.5f, 0.253f },
            new[] { 34f, 2.386f, 1.293f, 2.206f, 2.059f, 43.5f, 0.253f },
            new[] { 35f, 2.418f, 1.301f, 2.206f, 2.088f, 43.5f, 0.253f },
            new[] { 36f, 2.459f, 1.319f, 2.206f, 2.142f, 43.8f, 0.253f },
            new[] { 37f, 2.495f, 1.332f, 2.206f, 2.185f, 45.6f, 0.253f },
            new[] { 38f, 2.571f, 1.346f, 2.206f, 2.235f, 46.2f, 0.253f },
            new[] { 39f, 2.633f, 1.362f, 2.207f, 2.247f, 47.5f, 0.253f },
            new[] { 40f, 2.692f, 1.366f, 2.208f, 2.302f, 50.1f, 0.253f },
            new[] { 41f, 2.719f, 1.366f, 2.248f, 2.341f, 52.3f, 0.253f },
            new[] { 42f, 2.785f, 1.366f, 2.277f, 2.380f, 55.2f, 0.253f },
            new[] { 43f, 2.858f, 1.409f, 2.354f, 2.455f, 59.6f, 0.253f },
            new[] { 44f, 2.923f, 1.414f, 2.697f, 2.495f, 61.1f, 0.310f },
            new[] { 45f, 3.033f, 1.439f, 2.697f, 2.529f, 64.6f, 0.310f },
            new[] { 46f, 3.042f, 1.471f, 2.697f, 2.576f, 66.9f, 0.310f },
            new[] { 47f, 3.110f, 1.474f, 2.697f, 2.614f, 68.9f, 0.310f },
            new[] { 48f, 3.180f, 1.482f, 2.697f, 2.654f, 70.5f, 0.310f },
            new[] { 49f, 3.249f, 1.505f, 2.709f, 2.695f, 72.5f, 0.310f },
            new[] { 50f, 3.255f, 1.522f, 2.813f, 2.744f, 76.0f, 0.310f },
            new[] { 51f, 3.285f, 1.544f, 2.881f, 2.793f, 76.7f, 0.310f },
            new[] { 52f, 3.372f, 1.548f, 2.918f, 2.851f, 79.1f, 0.310f },
            new[] { 53f, 3.402f, 1.552f, 2.987f, 2.893f, 81.1f, 0.310f },
            new[] { 54f, 3.467f, 1.607f, 3.086f, 2.914f, 84.6f, 0.326f },
            new[] { 55f, 3.525f, 1.607f, 3.294f, 2.965f, 87.8f, 0.334f },
        };

        /// <summary>Measured gear at a player level (linear between table rows, flat beyond the ends), before <see cref="Share"/>.</summary>
        public static float[] Measured(double level)
        {
            if (level <= Table[0][0]) return Table[0];
            for (int i = 1; i < Table.Length; i++)
            {
                if (level > Table[i][0]) continue;
                var a = Table[i - 1]; var b = Table[i];
                double t = (level - a[0]) / Math.Max(1e-6, b[0] - a[0]);
                var r = new float[a.Length];
                for (int k = 0; k < a.Length; k++) r[k] = (float)(a[k] + (b[k] - a[k]) * t);
                return r;
            }
            return Table[Table.Length - 1];
        }

        /// <summary>일반 stages before this one are fought alone right after the start; they expect only the start kit (the old balance).</summary>
        public const int FirstPartyStage = 7;

        /// <summary>The player level a story stage's gear is read at: its 권장 레벨 on that difficulty.</summary>
        public static int LevelFor(int stage, int difficulty) { return StageStars.RecommendedPlayerLevel(stage, difficulty); }

        /// <summary>Measured gear of a story stage (the start kit for the solo 일반 stages 1-6).</summary>
        public static float[] MeasuredFor(int stage, int difficulty) { return difficulty <= 0 && stage < FirstPartyStage ? Table[0] : Measured(LevelFor(stage, difficulty)); }

        /// <summary>Gearless stats with the expected gear of a story stage put on.</summary>
        public static CombatStats Apply(CombatStats bare, int stage, int difficulty) { return Apply(bare, MeasuredFor(stage, difficulty)); }

        /// <summary>The start kit as a measured row (the first row of the table).</summary>
        public static float[] StartKit { get { return Table[0]; } }

        /// <summary>A share of the measured bonus, but never less than the start kit (the first row).</summary>
        public static float Expected(float[] measured, int k, float share = Share)
        {
            float zero = k >= 5 ? 0f : 1f;
            return Math.Max(Table[0][k], zero + share * (measured[k] - zero));
        }

        public static CombatStats Apply(CombatStats bare, float[] measured) { return Apply(bare, measured, Share); }

        /// <summary>Gearless stats with <paramref name="share"/> of the measured gear put on (at least the start kit).</summary>
        public static CombatStats Apply(CombatStats bare, float[] measured, float share)
        {
            Func<int, float> mul = k => Expected(measured, k, share);
            var s = bare;
            s.MaxHp = (int)Math.Round(s.MaxHp * mul(1));
            s.MaxQi = (int)Math.Round(s.MaxQi * mul(2));
            s.Attack = (int)Math.Round(s.Attack * mul(3));
            s.Defense = (int)Math.Round(s.Defense * mul(4));
            s.Speed = Math.Max(1, s.Speed + (int)Math.Round(Expected(measured, 5, share)));
            s.CritRate = Math.Min(1f, s.CritRate + Expected(measured, 6, share));
            return s;
        }
    }
}
