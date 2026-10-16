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
    /// 일일 수련 임무: three missions per day (easy, medium, hard) chosen from a pool by the date. Progress is counted from play
    /// (see <see cref="Record"/>) and resets at midnight (device clock). Finishing all three pays a bonus and keeps the 연속 수련
    /// streak alive; the streak opens rewards at 3, 7, 14 and 30 days.
    /// </summary>
    public static class Daily
    {
        /// <summary>Today's date; tests replace it.</summary>
        public static Func<DateTime> Clock = () => DateTime.Now;

        public static readonly long[] BaseDoo = { 400, 800, 1500 };
        public const long AllClearBonus = 1000;
        public static readonly int[] StreakMilestones = { 3, 7, 14, 30 };

        private static DailyMission M(string id, string name, DailyEvent ev, int target, int tier)
        {
            return new DailyMission { Id = id, Name = name, Event = ev, Target = target, Tier = tier };
        }

        public static readonly DailyMission[] Pool =
        {
            M("win3", "전투 3회 승리", DailyEvent.BattleWin, 3, 0),
            M("skill10", "무공 10번 사용", DailyEvent.SkillUse, 10, 0),
            M("clear1", "스테이지 1번 돌파", DailyEvent.StageClear, 1, 0),
            M("win8", "전투 8회 승리", DailyEvent.BattleWin, 8, 1),
            M("skill25", "무공 25번 사용", DailyEvent.SkillUse, 25, 1),
            M("flawless3", "아무도 쓰러지지 않고 3전 승리", DailyEvent.Flawless, 3, 1),
            M("clear2", "스테이지 2번 돌파", DailyEvent.StageClear, 2, 1),
            M("ultimate3", "필살기 3번 사용", DailyEvent.UltimateUse, 3, 2),
            M("joint3", "합공 3번 사용", DailyEvent.JointUse, 3, 2),
            M("flawless6", "아무도 쓰러지지 않고 6전 승리", DailyEvent.Flawless, 6, 2),
            M("quest1", "의뢰 1건 완수", DailyEvent.QuestDone, 1, 2),
            M("tower1", "무한 비무대 1층 돌파", DailyEvent.TowerClear, 1, 2),
            M("promo1", "승급 비무 도전 1번", DailyEvent.PromotionTry, 1, 2),
        };

        public static DailyMission Find(string id)
        {
            foreach (var m in Pool) if (m.Id == id) return m;
            return null;
        }

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
                case DailyEvent.JointUse: foreach (var c in EarlyGameContent.CompanionsFor(Math.Max(1, save.HighestUnlockedStage))) if (Bonds.Level(save, c.Id) >= 1) return true; return false;
                case DailyEvent.UltimateUse: return save.HighestUnlockedStage >= 4;
                case DailyEvent.PromotionTry: return save.ClearedStage >= 3 && Promotion.EffectiveRank(save) < Promotion.MaxRank;
                case DailyEvent.Flawless: return true;
                default: return true;
            }
        }

        /// <summary>Starts a new day when the date changed: new missions, cleared progress, streak check. Safe to call often.</summary>
        public static void Refresh(SaveData save)
        {
            int today = Today();
            int kinds = Enum.GetValues(typeof(DailyEvent)).Length;
            if (save.DailyDate == today && save.DailyMissionIds.Count == 3)
            {
                while (save.DailyProgress.Count < kinds) save.DailyProgress.Add(0);   // an update added counters
                return;
            }

            // The streak survives only if the last all-clear was yesterday (or today, which cannot happen on a new day).
            if (save.DailyDate != 0 && save.DailyLastClear != 0 && save.DailyLastClear != today && DayNumber(today) - DayNumber(save.DailyLastClear) > 1) save.DailyStreak = 0;
            if (save.DailyLastClear == 0) save.DailyStreak = 0;

            save.DailyDate = today;
            save.DailyProgress = new List<int>(new int[Enum.GetValues(typeof(DailyEvent)).Length]);
            save.DailyClaimed = new List<int>();
            save.DailyAllClaimed = false;
            save.DailyMissionIds = new List<string>();
            var rng = new Random(today);
            for (int tier = 0; tier < 3; tier++)
            {
                var options = new List<DailyMission>();
                foreach (var m in Pool) if (m.Tier == tier && Available(save, m)) options.Add(m);
                if (options.Count == 0) foreach (var m in Pool) if (m.Tier == tier) options.Add(m);
                save.DailyMissionIds.Add(options[rng.Next(options.Count)].Id);
            }
        }

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
        public static long RewardDoo(SaveData save, DailyMission m) { return (long)Math.Round(BaseDoo[m.Tier] * (1.0 + 0.15 * save.ClearedStage)); }

        public static long AllClearDoo(SaveData save) { return (long)Math.Round(AllClearBonus * (1.0 + 0.15 * save.ClearedStage)); }

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
            notes.Add(ms[index].Name + ": 두 +" + doo.ToString("N0"));
            if (save.DailyClaimed.Count == 3 && !save.DailyAllClaimed)
            {
                save.DailyAllClaimed = true;
                long bonus = AllClearDoo(save);
                save.Doo += bonus;
                Inventory.Add(save, "bear_gall", 2);
                // Streak: yesterday's all-clear continues it, otherwise it starts again at 1.
                int today = Today();
                bool continues = save.DailyLastClear != 0 && DayNumber(today) - DayNumber(save.DailyLastClear) == 1;
                save.DailyStreak = continues ? save.DailyStreak + 1 : 1;
                save.DailyLastClear = today;
                save.DailyBest = Math.Max(save.DailyBest, save.DailyStreak);
                notes.Add("세 임무 완수! 두 +" + bonus.ToString("N0") + ", 웅담 x2 · 연속 수련 " + save.DailyStreak + "일");
                string reward = StreakReward(save);
                if (reward != null) notes.Add(reward);
            }
            return notes;
        }

        /// <summary>The one-time reward for reaching a streak milestone today (null when this day is not one).</summary>
        private static string StreakReward(SaveData save)
        {
            switch (save.DailyStreak)
            {
                case 3: Inventory.Add(save, "okruju", 3); return "연속 3일 보상: 옥루주 x3";
                case 7: Inventory.Add(save, "ginseng", 1); return "연속 7일 보상: 인형설삼";
                case 14: Inventory.Add(save, "ilgeup_yeongdan", 1); return "연속 14일 보상: 일급영단";
                case 30: Inventory.Add(save, "ilgeup_yeongdan", 2); save.Doo += 20000; return "연속 30일 보상: 일급영단 x2, 두 +20,000";
            }
            return null;
        }

        public static int NextMilestone(SaveData save)
        {
            foreach (var m in StreakMilestones) if (save.DailyStreak < m) return m;
            return -1;
        }
    }
}
