using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>HP of one party member at the moment the player stepped back from a stage.</summary>
    [Serializable]
    public sealed class MemberHp
    {
        public string Id;
        public int Hp;
    }

    /// <summary>
    /// Resume point inside a stage: after winning a non-final fight the player may leave and later continue from the next
    /// fight with the same HP. One slot only; starting another stage or losing clears it.
    /// </summary>
    public static class StageProgress
    {
        public static bool Has(SaveData save, int stageNumber)
        {
            return save.ProgressStage == stageNumber && stageNumber > 0 && save.ProgressNext > 0;
        }

        public static void Store(SaveData save, int stageNumber, int nextEncounter, IEnumerable<Combatant> party, int actions = 0, bool downed = false, int difficulty = 0)
        {
            save.ProgressActions = actions;
            save.ProgressDowned = downed;
            save.ProgressDifficulty = difficulty;
            save.ProgressStage = stageNumber;
            save.ProgressNext = nextEncounter;
            save.ProgressHp = new List<MemberHp>();
            foreach (var m in party) save.ProgressHp.Add(new MemberHp { Id = m.Id, Hp = m.Hp });
        }

        public static void Clear(SaveData save)
        {
            save.ProgressStage = 0;
            save.ProgressNext = 0;
            save.ProgressActions = 0;
            save.ProgressDowned = false;
            save.ProgressDifficulty = 0;
            save.ProgressHp = new List<MemberHp>();
        }

        /// <summary>
        /// Applies the stored HP to the rebuilt party (never above max, never dead). A member who was not in the stored party
        /// (the player changed companions while stepping back) starts at the average wound level of the others instead of full health.
        /// </summary>
        public static void Restore(SaveData save, IEnumerable<Combatant> party)
        {
            var members = new List<Combatant>(party);
            double ratioSum = 0; int ratioCount = 0;
            var unmatched = new List<Combatant>();
            foreach (var m in members)
            {
                MemberHp found = null;
                foreach (var h in save.ProgressHp) if (h.Id == m.Id) found = h;
                if (found == null) { unmatched.Add(m); continue; }
                m.Hp = Math.Max(1, Math.Min(m.Stats.MaxHp, found.Hp));
                ratioSum += (double)m.Hp / Math.Max(1, m.Stats.MaxHp);
                ratioCount++;
            }
            if (ratioCount == 0) return;
            double avg = ratioSum / ratioCount;
            foreach (var m in unmatched) m.Hp = Math.Max(1, (int)Math.Round(m.Stats.MaxHp * avg));
        }
    }
}
