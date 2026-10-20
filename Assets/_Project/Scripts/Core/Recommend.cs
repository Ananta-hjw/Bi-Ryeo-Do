using System;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// What a fight pays and which 무공 경지 it is meant for. Shown on the 출전 준비 window, the 승급 시험장 and the 무한 비무대,
    /// and used by the battle screen so the preview and the real payout never drift apart.
    /// </summary>
    public static class Recommend
    {
        /// <summary>Harder difficulties are tuned against the enemy line rather than the plain level curve: the hero should reach this share of the strongest enemy's 종합 수치.</summary>
        public const double VersusStrongest = 0.55;
        /// <summary>A duel is one-on-one against an opponent who was hardened for the rank; the hero should reach this share of the stronger opponent.</summary>
        public const double VersusDuelist = 0.7;

        // ------------------------------------------------------------------ rewards

        /// <summary>True when the stage was already beaten (on its difficulty), so its fights pay less.</summary>
        public static bool IsReplay(SaveData save, StageData st)
        {
            if (st.IsQuest || st.IsTower) return false;
            return st.Difficulty == 0 ? save.ClearedStage >= st.Number : StageStars.StageCleared(save, st.Number, st.Difficulty);
        }

        /// <summary>경험 and 두 of one fight of a stage, exactly as the battle screen pays them.</summary>
        public static void FightReward(SaveData save, StageData st, int fight, bool replay, out long exp, out long doo)
        {
            exp = 0; doo = 0;
            int diff = st.IsTower || st.IsQuest ? 0 : st.Difficulty;
            bool towerFirst = st.IsTower && st.TowerFloor > save.TowerBest;
            foreach (var id in st.Encounters[fight])
            {
                exp += EarlyGameContent.ExpReward(st, id);
                doo += EarlyGameContent.DooReward(st, id);
            }
            if (st.IsQuest) exp = Quests.ExpReward(save);
            if (st.IsTower) { exp = towerFirst ? Tower.ExpReward(save, st.TowerFloor) : 0; if (!towerFirst) doo = 0; }
            if (diff > 0) { exp = (long)Math.Round(exp * StageStars.RewardFactor[diff]); doo = (long)Math.Round(doo * StageStars.RewardFactor[diff]); }
            if (!st.IsTower)
            {
                exp = (long)Math.Round(exp * (1f + Factions.Bonus(save, PerkKind.ExpPct)));
                doo = (long)Math.Round(doo * (1f + Factions.Bonus(save, PerkKind.DooPct)));
            }
            if (replay)
            {
                exp = (long)Math.Round(exp * EarlyGameContent.ReplayExpFactor);
                doo = (long)Math.Round(doo * EarlyGameContent.ReplayDooFactor);
            }
            doo = Income.Boost(doo);
        }

        /// <summary>경험 and 두 for winning every fight of the stage in one go.</summary>
        public static void StageReward(SaveData save, StageData st, out long exp, out long doo)
        {
            exp = 0; doo = 0;
            bool replay = IsReplay(save, st);
            for (int i = 0; i < st.Encounters.Length; i++)
            {
                FightReward(save, st, i, replay || WonBefore(save, st, i), out long e, out long d);
                exp += e; doo += d;
            }
        }

        /// <summary>True when this fight of a story stage was already won on the stage's difficulty, so it pays the replay rate (also before the stage is cleared, e.g. retrying after losing a later fight).</summary>
        public static bool WonBefore(SaveData save, StageData st, int fight)
        {
            if (st.IsQuest || st.IsTower) return false;
            return (StageStars.MaskOf(save, st.Number, fight, st.Difficulty) & 1) != 0;
        }

        // ------------------------------------------------------------------ recommended 경지

        public static double HeroPowerAtLevel(int level) { return Realm.Power(Progression.HeroStatsFor(level)); }

        /// <summary>The hero's 종합 수치 on the enemy stat curve (a stage's <see cref="StageData.RecommendedLevel"/> is a curve level).</summary>
        public static double HeroPowerAtCurve(double curveLevel) { return Realm.Power(Progression.HeroStatsAtCurve(curveLevel)); }

        private static double StrongestEnemy(StageData st)
        {
            double best = 0.0;
            foreach (var fight in st.Encounters)
                foreach (var id in fight)
                {
                    var e = st.EnemyFor(id);
                    if (e == null || !Realm.HasRealm(e)) continue;
                    best = Math.Max(best, Realm.Power(e.CreateCombatant("rec").Stats));
                }
            return best;
        }

        /// <summary>The 종합 수치 a hero should have to take the stage comfortably (compared with the 종합 수치 on the 상태창, before the 경지 bonus).</summary>
        public static double ForStage(StageData st)
        {
            if (st.IsTower) return ForTowerFloor(st.TowerFloor);
            int diff = st.IsQuest ? 0 : st.Difficulty;
            double byLevel = HeroPowerAtCurve(st.RecommendedLevel + StageStars.LevelAdd(st.Number, diff)
                + (st.IsQuest ? 0.0 : Progression.CurvePerPlayerLevel * StageStars.NormalEdgeLevels(st.Number)));
            return diff == 0 ? byLevel : Math.Max(byLevel, VersusStrongest * StrongestEnemy(st));
        }

        public static double ForTowerFloor(int floor) { return HeroPowerAtCurve(Tower.FloorLevel(floor)); }

        public static double ForPromotion(PromotionOffer offer)
        {
            double best = 0.0;
            foreach (var e in offer.Opponents) best = Math.Max(best, Realm.Power(e.CreateCombatant("rec").Stats));
            return VersusDuelist * best;
        }

        /// <summary>"절정 완숙 (종합 123)".</summary>
        public static string Text(double power)
        {
            return Realm.Label(Realm.StepOf(power)) + " (종합 " + Math.Round(power).ToString("N0") + ")";
        }

        /// <summary>The protagonist's own 종합 수치 as the 상태창 reads it.</summary>
        public static double Mine(SaveData save) { return Realm.Power(InnerArts.HeroStatsBeforeRealm(save)); }

        public static bool Meets(SaveData save, double power) { return Mine(save) >= power; }

        /// <summary>One line for the windows: "추천 최소 경지 절정 완숙 (종합 123)  ·  내 경지 초일급 (종합 99)".</summary>
        public static string Line(SaveData save, double power)
        {
            double mine = Mine(save);
            return "추천 최소 경지  " + Text(power) + "\n내 경지  " + Text(mine) + (mine >= power ? "  (충분합니다)" : "  (아직 부족합니다)");
        }
    }
}
