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
        public int Qi;       // 내공 left after the rest
        public bool HasQi;   // false in saves from before 내공 was stored (then the member starts with full 내공); a stored 0 stays 0
        public int Spirit;   // 기세 carries over inside a stage, so a resumed stage keeps it
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

        public static void Store(SaveData save, int stageNumber, int nextEncounter, IEnumerable<Combatant> party, int difficulty = 0)
        {
            save.ProgressDifficulty = difficulty;
            save.ProgressStage = stageNumber;
            save.ProgressNext = nextEncounter;
            save.ProgressHp = new List<MemberHp>();
            foreach (var m in party) save.ProgressHp.Add(new MemberHp { Id = m.Id, Hp = m.Hp, Qi = m.Qi, HasQi = true, Spirit = m.Spirit });
        }

        public static void Clear(SaveData save)
        {
            save.ProgressStage = 0;
            save.ProgressNext = 0;
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
                if (found.HasQi || found.Qi > 0) m.Qi = Math.Max(0, Math.Min(m.Stats.MaxQi, found.Qi));
                m.Spirit = Math.Max(m.Spirit, Math.Min(InteractiveBattle.MaxSpirit, found.Spirit));
                ratioSum += (double)m.Hp / Math.Max(1, m.Stats.MaxHp);
                ratioCount++;
            }
            if (ratioCount == 0) return;
            double avg = ratioSum / ratioCount;
            foreach (var m in unmatched) m.Hp = Math.Max(1, (int)Math.Round(m.Stats.MaxHp * avg));
        }
    }
}
