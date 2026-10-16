using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// Plain-language explanations of each stage's story: a "이야기 배경" shown before the first attempt (who is who, why they fight, what the goal is)
    /// and a "그 뒤" shown after the first breakthrough. The text lives in <c>StoryBriefingData.cs</c> and follows <c>Story/analysis</c>.
    /// </summary>
    public static partial class StoryBriefings
    {
        /// <summary>Narration lines before the stage, or null when there is none.</summary>
        public static string[] Briefing(int stage)
        {
            string[] lines;
            return Before.TryGetValue(stage, out lines) ? lines : null;
        }

        /// <summary>Narration lines after the stage's first breakthrough, or null.</summary>
        public static string[] Aftermath(int stage)
        {
            string[] lines;
            return After.TryGetValue(stage, out lines) ? lines : null;
        }

        /// <summary>Stages whose summary can be read again (the cleared ones and the next one).</summary>
        public static List<int> Readable(SaveData save)
        {
            var list = new List<int>();
            foreach (var s in EarlyGameContent.Stages)
                if (s.Number <= save.ClearedStage + 1 && Briefing(s.Number) != null) list.Add(s.Number);
            return list;
        }
    }
}
