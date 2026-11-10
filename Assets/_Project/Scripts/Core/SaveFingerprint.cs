namespace BiRyeoDo.Core
{
    /// <summary>
    /// A cheap number that changes whenever something the lobby badges and header read is likely to have changed
    /// (currency, level, progress, claims, things seen). Screens poll it instead of recomputing every badge on a timer.
    /// It only counts list lengths and counters, so it costs well under a microsecond.
    /// </summary>
    public static class SaveFingerprint
    {
        public static long Of(SaveData save)
        {
            if (save == null) return 0;
            unchecked
            {
                long h = 17;
                h = h * 31 + save.Doo;
                h = h * 31 + save.Exp;
                h = h * 31 + save.Level;
                h = h * 31 + save.ClearedStage;
                h = h * 31 + save.SwordRank;
                h = h * 31 + save.BattleWins;
                h = h * 31 + save.GachaPulls;
                h = h * 31 + save.Items.Count;
                h = h * 31 + save.DoneQuests.Count;
                h = h * 31 + save.DoneFortunes.Count;
                h = h * 31 + save.StageStars.Count;
                h = h * 31 + save.ClaimedStarChests.Count;
                h = h * 31 + save.ClaimedAchievements.Count;
                h = h * 31 + save.ReadRecordUpdates.Count;
                h = h * 31 + save.SeenArts.Count;
                h = h * 31 + save.SeenInner.Count;
                h = h * 31 + save.SeenMeridians.Count;
                h = h * 31 + save.SeenMemories.Count;
                h = h * 31 + save.SeenEnemies.Count;
                h = h * 31 + save.SeenItems.Count;
                h = h * 31 + save.LearnedArts.Count;
                h = h * 31 + save.LearnedInner.Count;
                h = h * 31 + save.DailyDate;
                h = h * 31 + save.DailyClaimed.Count;
                foreach (int p in save.DailyProgress) h = h * 31 + p;
                return h;
            }
        }
    }
}
