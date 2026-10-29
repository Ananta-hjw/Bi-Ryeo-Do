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

        /// <summary>
        /// The stage number whose companions travel along: a 일반 story stage uses its own; quests, tower floors and the 어려움 / 극악 stages
        /// use everyone who has joined so far (on a harder difficulty the early solo stages are fought with the party the player already leads).
        /// </summary>
        public static int PartyStage(StageData stage, SaveData save)
        {
            if (stage.IsQuest || stage.IsTower) return System.Math.Max(1, save.HighestUnlockedStage);
            return stage.Difficulty > 0 ? System.Math.Max(stage.Number, save.HighestUnlockedStage) : stage.Number;
        }
    }
}
