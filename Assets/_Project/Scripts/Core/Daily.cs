using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    public enum DailyEvent { BattleWin, SkillUse, StageClear, Flawless, UltimateUse, JointUse, QuestDone, TowerClear, PromotionTry, GachaPull }

    public sealed class DailyMission
    {
        public string Id, Name;
        public DailyEvent Event;
        public int Target;
        public int Tier;          // 0 easy, 1 medium, 2 hard (sets the base reward)
    }

    /// <summary>
    /// 일일 수련 임무: six missions per day (two easy, two medium, two hard) chosen from a pool by the date. Progress is counted from play
    /// (see <see cref="Record"/>) and resets at midnight (device clock). Finishing all six pays a bonus and keeps the 연속 수련
    /// streak alive; the streak opens rewards at 3, 7, 14 and 30 days.
    /// </summary>
    public static class Daily
    {
        /// <summary>Today's date; tests replace it.</summary>
        public static Func<DateTime> Clock = () => DateTime.Now;

        public static readonly long[] BaseDoo = { 1050, 2250, 4500 };
        public const long AllClearBonus = 3000;
        public const int PerTier = 2, Count = PerTier * 3;
        public static readonly int[] StreakMilestones = { 3, 7, 14, 30 };

        private static DailyMission M(string id, string name, DailyEvent ev, int target, int tier)
        {
            return new DailyMission { Id = id, Name = name, Event = ev, Target = target, Tier = tier };
        }

        public static readonly DailyMission[] Pool =
        {
            M("win8", "전투 8회 승리", DailyEvent.BattleWin, 8, 0),
            M("skill20", "무공 20번 사용", DailyEvent.SkillUse, 20, 0),
            M("clear2", "스테이지 2번 돌파", DailyEvent.StageClear, 2, 0),
            M("flawless2", "아무도 쓰러지지 않고 2전 승리", DailyEvent.Flawless, 2, 0),
            M("win16", "전투 16회 승리", DailyEvent.BattleWin, 16, 1),
            M("skill40", "무공 40번 사용", DailyEvent.SkillUse, 40, 1),
            M("gacha4", "뽑기 4번", DailyEvent.GachaPull, 4, 1),
            M("flawless6", "아무도 쓰러지지 않고 6전 승리", DailyEvent.Flawless, 6, 1),
            M("clear3", "스테이지 3번 돌파", DailyEvent.StageClear, 3, 1),
            M("flawless12", "아무도 쓰러지지 않고 12전 승리", DailyEvent.Flawless, 12, 2),
            M("win24", "전투 24회 승리", DailyEvent.BattleWin, 24, 2),
            M("skill60", "무공 60번 사용", DailyEvent.SkillUse, 60, 2),
            M("clear4", "스테이지 4번 돌파", DailyEvent.StageClear, 4, 2),
        };

        public static DailyMission Find(string id)
        {
            foreach (var m in Pool) if (m.Id == id) return m;
            return null;
        }

        private static readonly int EventCount = Enum.GetValues(typeof(DailyEvent)).Length;

        public static int Today() { var d = Clock(); return d.Year * 10000 + d.Month * 100 + d.Day; }

        private static int DayNumber(int yyyymmdd)
        {
            return (int)(new DateTime(yyyymmdd / 10000, yyyymmdd / 100 % 100, yyyymmdd % 100) - new DateTime(2000, 1, 1)).TotalDays;
        }

        /// <summary>Missions that can be earned right now (a mission is skipped when its goal does not exist yet in the player's game).</summary>
        public static bool Available(SaveData save, DailyMission m)
        {
            switch (m.Event)
            {
                case DailyEvent.TowerClear: return Tower.Unlocked(save);
                case DailyEvent.QuestDone: return Quests.Open(save).Count > 0;
                case DailyEvent.JointUse:
                    foreach (var c in EarlyGameContent.SelectCompanionsFor(Math.Max(1, save.HighestUnlockedStage), save)) if (Bonds.Level(save, c.Id) >= 1) return true;
                    return false;   // a bonded companion has to be in the party
                case DailyEvent.UltimateUse: return Progression.EquippedCount(save, true) > 0;
                case DailyEvent.PromotionTry:
                    return save.ClearedStage >= 3 && Promotion.EffectiveRank(save) < Promotion.MaxRank
                        && save.Doo >= Promotion.BuildOffer(save, Promotion.EffectiveRank(save) + 1, 0).Fee;
                case DailyEvent.GachaPull: return save.Doo >= Gacha.CostFor(1);
                default: return true;
            }
        }

        /// <summary>Starts a new day when the date changed: new missions, cleared progress, streak check. Safe to call often.</summary>
        public static void Refresh(SaveData save)
        {
            int today = Today();
            // Only a later date starts a new day: setting the device clock back must not reopen claimed missions.
            if (save.DailyDate >= today && StoredMissionsValid(save))
            {
                while (save.DailyProgress.Count < EventCount) save.DailyProgress.Add(0);   // an update added counters
                return;
            }

            // The streak survives only if the last all-clear was yesterday (or today, which cannot happen on a new day).
            if (save.DailyDate != 0 && save.DailyLastClear != 0 && save.DailyLastClear != today && DayNumber(today) - DayNumber(save.DailyLastClear) > 1) save.DailyStreak = 0;
            if (save.DailyLastClear == 0) save.DailyStreak = 0;

            save.DailyDate = today;
            save.DailyProgress = new List<int>(new int[EventCount]);
            save.DailyClaimed = new List<int>();
            save.DailyMissionIds = new List<string>();
            var rng = new Random(today);
            for (int tier = 0; tier < 3; tier++)
            {
                var options = new List<DailyMission>();
                foreach (var m in Pool) if (m.Tier == tier && Available(save, m)) options.Add(m);
                // Not enough open goals for the tier: top up from the rest of its pool so there are always PerTier distinct missions.
                if (options.Count < PerTier) foreach (var m in Pool) if (m.Tier == tier && !options.Contains(m)) options.Add(m);
                for (int k = 0; k < PerTier; k++)
                {
                    int pick = rng.Next(options.Count);
                    save.DailyMissionIds.Add(options[pick].Id);
                    options.RemoveAt(pick);
                }
            }
        }

        private static bool StoredMissionsValid(SaveData save)
        {
            if (save.DailyMissionIds.Count != Count) return false;
            foreach (var id in save.DailyMissionIds) if (Find(id) == null) return false;   // a pool entry was renamed or removed
            return true;
        }

        public static bool AllClaimed(SaveData save) { return save.DailyClaimed.Count >= Count; }

        public static List<DailyMission> Missions(SaveData save)
        {
            Refresh(save);
            var list = new List<DailyMission>();
            foreach (var id in save.DailyMissionIds) { var m = Find(id); if (m != null) list.Add(m); }
            return list;
        }

        /// <summary>Counts progress for today's missions.</summary>
        public static void Record(SaveData save, DailyEvent ev, int amount = 1)
        {
            if (amount <= 0) return;
            Refresh(save);
            save.DailyProgress[(int)ev] += amount;
        }

        public static int ProgressOf(SaveData save, DailyMission m)
        {
            Refresh(save);
            return Math.Min(m.Target, save.DailyProgress[(int)m.Event]);
        }

        public static bool IsDone(SaveData save, DailyMission m) { return ProgressOf(save, m) >= m.Target; }

        public static bool IsClaimed(SaveData save, int index) { return save.DailyClaimed.Contains(index); }

        /// <summary>두 grows with progress: base x (1 + 0.15 per cleared stage).</summary>
        public static long RewardDoo(SaveData save, DailyMission m) { return Income.Boost((long)Math.Round(BaseDoo[m.Tier] * (1.0 + 0.15 * save.ClearedStage))); }

        public static long AllClearDoo(SaveData save) { return Income.Boost((long)Math.Round(AllClearBonus * (1.0 + 0.15 * save.ClearedStage))); }

        public static int ClaimableCount(SaveData save)
        {
            var ms = Missions(save);
            int n = 0;
            for (int i = 0; i < ms.Count; i++) if (IsDone(save, ms[i]) && !IsClaimed(save, i)) n++;
            return n;
        }

        /// <summary>Takes one mission's reward. Claiming the last of the three also pays the all-clear bonus and advances the streak. Returns notes (empty when not claimable).</summary>
        public static List<string> Claim(SaveData save, int index)
        {
            var notes = new List<string>();
            var ms = Missions(save);
            if (index < 0 || index >= ms.Count || !IsDone(save, ms[index]) || IsClaimed(save, index)) return notes;
            save.DailyClaimed.Add(index);
            long doo = RewardDoo(save, ms[index]);
            save.Doo += doo;
            var material = Gear.RewardMaterial(save.HighestUnlockedStage);
            Inventory.Add(save, material.Id, Gear.DailyPolish);
            notes.Add(ms[index].Name + ": 두 +" + doo.ToString("N0") + ", " + material.Name + " x" + Gear.DailyPolish);
            if (save.DailyClaimed.Count == Count)
            {
                long bonus = AllClearDoo(save);
                save.Doo += bonus;
                Inventory.Add(save, "bear_gall", 2);
                Inventory.Add(save, InnerArts.StoneId, 1);
                // Streak: yesterday's all-clear continues it, otherwise it starts again at 1.
                int today = Today();
                bool continues = save.DailyLastClear != 0 && DayNumber(today) - DayNumber(save.DailyLastClear) == 1;
                save.DailyStreak = continues ? save.DailyStreak + 1 : 1;
                save.DailyLastClear = today;
                save.DailyBest = Math.Max(save.DailyBest, save.DailyStreak);
                notes.Add("여섯 임무 완수! 두 +" + bonus.ToString("N0") + ", 웅담 x2, 심득석 x1 · 연속 수련 " + save.DailyStreak + "일");
                string reward = StreakReward(save);
                if (reward != null) notes.Add(reward);
            }
            return notes;
        }

        /// <summary>The reward for the streak value reached today (null when it is not a milestone). A rebuilt streak earns them again; 30 repeats every 30 days.</summary>
        private static string StreakReward(SaveData save)
        {
            int streak = save.DailyStreak >= 30 && save.DailyStreak % 30 == 0 ? 30 : save.DailyStreak;
            switch (streak)
            {
                case 3: Inventory.Add(save, "okruju", 3); return "연속 3일 보상: 옥루주 x3";
                case 7: Inventory.Add(save, "ginseng", 1); return "연속 7일 보상: 인형설삼";
                case 14: Inventory.Add(save, "ilgeup_yeongdan", 1); Inventory.Add(save, InnerArts.StoneId, 2); return "연속 14일 보상: 일급영단, 심득석 x2";
                case 30: Inventory.Add(save, "ilgeup_yeongdan", 2); save.Doo += 31200; Inventory.Add(save, InnerArts.StoneId, 4); return "연속 30일 보상: 일급영단 x2, 심득석 x4, 두 +31,200";
            }
            return null;
        }

        /// <summary>The streak length of the next reward (every 30 days after day 30).</summary>
        public static int NextMilestone(SaveData save)
        {
            foreach (var m in StreakMilestones) if (save.DailyStreak < m) return m;
            return (save.DailyStreak / 30 + 1) * 30;
        }
    }
}
