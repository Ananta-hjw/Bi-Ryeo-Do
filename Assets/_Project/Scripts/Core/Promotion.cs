using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>One 비무첩 (challenge card): a tier of opponents for a target rank, with its price and payout.</summary>
    public sealed class PromotionOffer
    {
        public int TargetRank, Tier;          // Tier 0 = 신진 (safe), 1 = 중견, 2 = 고수 (risky)
        public string Title;
        public bool Skip;                     // jumping two ranks at once
        public long Fee, DooReward, ExpReward;
        public EnemyData[] Opponents;         // fought one after another, protagonist alone
    }

    /// <summary>
    /// 승급 비무: the 검룡위 (rank 1-10) is independent of level. To rise, win two 1:1 duels against opponents of the target rank.
    /// A duel that is not decided within <see cref="TurnLimit"/> of the player's actions is a draw (still promoted, half pay).
    /// Pure C#; the UI only plays the duels (see BattleScreen.OpenPromotion).
    /// </summary>
    public static class Promotion
    {
        public const int MaxRank = 10;
        public const int GateRank = 5;                 // 오검룡: required for the 삼성무제 stages
        public const int GateStage = 12;
        public const int TurnLimit = 12;               // player actions per duel (stand-in for the 300-second rule)
        public const float DrawPayFactor = 0.5f;
        public const float SkipPayFactor = 1.5f;

        private static readonly string[] Digit = { "", "일", "이", "삼", "사", "오", "육", "칠", "팔", "구", "십" };
        private static readonly string[] TierName = { "신진", "중견", "고수" };
        private static readonly float[] TierAtk = { 0.9f, 1.0f, 1.1f };
        private static readonly float[] TierHp = { 0.9f, 1.0f, 1.1f };
        private static readonly float[] TierPay = { 0.8f, 1.0f, 1.5f };

        /// <summary>AtkMul of a normal-tier opponent per target rank (index = rank), tuned with the balance bot.</summary>
        public static readonly float[] AtkMulByRank = { 0f, 1.50f, 1.37f, 1.62f, 1.60f, 1.77f, 1.85f, 1.93f, 2.01f, 1.98f, 2.06f };

        public static int EffectiveRank(SaveData save) { return Math.Max(1, Math.Min(MaxRank, save.SwordRank)); }

        public static string RankName(int rank) { return Digit[Math.Max(1, Math.Min(MaxRank, rank))] + "검룡"; }

        public static int OpponentLevel(int rank) { return 5 + 4 * rank; }

        /// <summary>Opponents never fall below the player's own level, so out-levelling a rank does not make its duels trivial.</summary>
        public static int OpponentLevelFor(int rank, int playerLevel) { return Math.Max(OpponentLevel(rank), playerLevel); }

        /// <summary>HP and attack multiplier of a rank's opponents on top of the base tuning: gentle at the bottom, steep at the top (rank 2 x1.02, rank 6 x1.22, rank 10 x1.54, on both HP and attack).</summary>
        public static float HardnessCoef = 0.02f;
        public static float HardnessFor(int rank) { return 1f + HardnessCoef * (float)Math.Pow(Math.Max(1, rank) - 1, 1.5); }

        /// <summary>Player actions allowed per duel; harder ranks get a little longer so a win stays possible.</summary>
        public static int TurnLimitFor(int rank) { return TurnLimit + Math.Max(0, rank - 1); }

        public const float StatBonusPerRank = 0.03f;

        /// <summary>The reward for rising: every stat of the protagonist grows 3% per 검룡위 (rank 10 = +30%).</summary>
        public static CombatStats ApplyRankBonus(CombatStats stats, int rank)
        {
            float m = 1f + StatBonusPerRank * Math.Max(1, Math.Min(MaxRank, rank));
            stats.MaxHp = (int)Math.Round(stats.MaxHp * m);
            stats.MaxQi = (int)Math.Round(stats.MaxQi * m);
            stats.Attack = (int)Math.Round(stats.Attack * m);
            stats.Defense = (int)Math.Round(stats.Defense * m);
            stats.Speed = Math.Max(1, (int)Math.Round(stats.Speed * m));
            return stats;
        }

        /// <summary>True when the stage is open to the player's rank (only the 삼성무제 stages are gated for now).</summary>
        public static bool StageAllowed(SaveData save, int stageNumber)
        {
            return stageNumber < GateStage || EffectiveRank(save) >= GateRank;
        }

        public static long BaseDoo(int rank) { return (long)Math.Round(500 * Math.Pow(rank, 1.6)); }

        public static List<int> TargetRanks(SaveData save)
        {
            var list = new List<int>();
            int r = EffectiveRank(save);
            if (r + 1 <= MaxRank) list.Add(r + 1);
            if (r + 2 <= MaxRank) list.Add(r + 2);
            return list;
        }

        public static PromotionOffer BuildOffer(SaveData save, int targetRank, int tier)
        {
            int cur = EffectiveRank(save);
            if (targetRank <= cur || targetRank > cur + 2 || targetRank > MaxRank) throw new ArgumentException("Invalid target rank.");
            tier = Math.Max(0, Math.Min(2, tier));
            bool skip = targetRank == cur + 2;
            long doo = (long)Math.Round(BaseDoo(targetRank) * TierPay[tier] * (skip ? SkipPayFactor : 1f));
            int lvl = OpponentLevel(targetRank);
            return new PromotionOffer
            {
                TargetRank = targetRank, Tier = tier, Skip = skip,
                Title = RankName(targetRank) + " 승급 · " + TierName[tier] + (skip ? " (월반)" : ""),
                DooReward = doo,
                Fee = doo / 6,
                ExpReward = (long)Math.Round(Progression.ExpToNext(Math.Max(lvl, save.Level)) * 0.45 * TierPay[tier]),
                Opponents = new[] { MakeOpponent(targetRank, tier, 0, save.Level), MakeOpponent(targetRank, tier, 1, save.Level) }
            };
        }

        /// <summary>Two archetypes: a fast 쾌검수 and a slow, wind-up 중검수.</summary>
        public static EnemyData MakeOpponent(int rank, int tier, int index, int playerLevel = 0)
        {
            float hard = HardnessFor(rank);
            int level = OpponentLevelFor(rank, playerLevel);
            float atk = AtkMulByRank[rank] * TierAtk[tier] * hard;
            string t = TierName[tier];
            if (index == 0)
                return new EnemyData
                {
                    Id = "duel_fast_" + rank, Name = t + " 쾌검수", Level = level, HpMul = 1.0f * TierHp[tier] * hard, AtkMul = atk, SpeedAdd = 8,
                    Moves = new[] { EnemyMove.Attack("쾌검", 0.8f, 2), EnemyMove.Attack("찌르기", 1.1f) }
                };
            return new EnemyData
            {
                Id = "duel_heavy_" + rank, Name = t + " 중검수", Level = level, HpMul = 1.4f * TierHp[tier] * hard, AtkMul = atk, SpeedAdd = -4,
                Moves = new[] { EnemyMove.Attack("베기", 1.0f), EnemyMove.Wind("검을 높이 든다"), EnemyMove.Attack("중검 내려치기", 2.6f) }
            };
        }

        public static bool CanAfford(SaveData save, PromotionOffer offer) { return save.Doo >= offer.Fee; }

        public static bool Pay(SaveData save, PromotionOffer offer)
        {
            if (!CanAfford(save, offer)) return false;
            save.Doo -= offer.Fee;
            Daily.Record(save, DailyEvent.PromotionTry);
            return true;
        }

        /// <summary>Applies a finished challenge (both duels not lost). Returns the 두 paid out.</summary>
        public static long Complete(SaveData save, PromotionOffer offer, bool anyDraw)
        {
            save.SwordRank = Math.Max(EffectiveRank(save), offer.TargetRank);
            long doo = (long)Math.Round(offer.DooReward * (anyDraw ? DrawPayFactor : 1f));
            save.Doo += doo;
            return doo;
        }
    }
}
