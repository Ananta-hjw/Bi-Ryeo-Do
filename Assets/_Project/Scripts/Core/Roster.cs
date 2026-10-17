using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// Who is in the party when companions arrive: a newcomer never pushes anyone out. He joins on the bench (대기) and the player
    /// decides in 파티 편성 whether and where he fights.
    /// </summary>
    public static class Roster
    {
        /// <summary>Companions who become available when the furthest stage reached grows from <paramref name="oldHighest"/> to <paramref name="newHighest"/>.</summary>
        public static List<CompanionData> NewJoiners(int oldHighest, int newHighest)
        {
            var list = new List<CompanionData>();
            foreach (var c in EarlyGameContent.Companions)
                if (c.JoinStage > oldHighest && c.JoinStage <= newHighest) list.Add(c);
            return list;
        }

        /// <summary>Seats the newcomers on the bench so the current party and its formation stay exactly as the player set them.</summary>
        public static void BenchNewcomers(SaveData save, IEnumerable<CompanionData> joined)
        {
            foreach (var c in joined)
                if (!save.BenchedCompanions.Contains(c.Id) && !save.ActiveCompanions.Contains(c.Id)) save.BenchedCompanions.Add(c.Id);
        }

        /// <summary>The stage number whose companions travel along: a story stage uses its own, quests and tower floors use everyone who has joined so far.</summary>
        public static int PartyStage(StageData stage, SaveData save)
        {
            return stage.IsQuest || stage.IsTower ? System.Math.Max(1, save.HighestUnlockedStage) : stage.Number;
        }
    }
}
