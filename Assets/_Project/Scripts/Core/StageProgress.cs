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

        public static void Store(SaveData save, int stageNumber, int nextEncounter, IEnumerable<Combatant> party)
        {
            save.ProgressStage = stageNumber;
            save.ProgressNext = nextEncounter;
            save.ProgressHp = new List<MemberHp>();
            foreach (var m in party) save.ProgressHp.Add(new MemberHp { Id = m.Id, Hp = m.Hp });
        }

        public static void Clear(SaveData save)
        {
            save.ProgressStage = 0;
            save.ProgressNext = 0;
            save.ProgressHp = new List<MemberHp>();
        }

        /// <summary>Applies the stored HP to the rebuilt party (never above max, never dead).</summary>
        public static void Restore(SaveData save, IEnumerable<Combatant> party)
        {
            foreach (var m in party)
                foreach (var h in save.ProgressHp)
                    if (h.Id == m.Id) m.Hp = Math.Max(1, Math.Min(m.Stats.MaxHp, h.Hp));
        }
    }
}
