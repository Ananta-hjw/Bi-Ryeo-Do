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

        /// <summary>A short comedy scene shown after the briefing, or null.</summary>
        public static StoryScene Interlude(int stage)
        {
            StoryScene scene;
            return Between.TryGetValue(stage, out scene) ? scene : null;
        }

        /// <summary>Early chapters unlock only after clearing; later chapters keep their existing preview rule.</summary>
        public static bool CanRead(SaveData save, int stageNumber)
        {
            if (save == null) return false;
            var stage = EarlyGameContent.FindStage(stageNumber);
            if (stage == null || Briefing(stageNumber) == null) return false;
            return stageNumber <= save.ClearedStage + (EarlyStory.Supports(stage) ? 0 : 1);
        }

        /// <summary>Chapters available in the story record.</summary>
        public static List<int> Readable(SaveData save)
        {
            var list = new List<int>();
            foreach (var s in EarlyGameContent.Stages)
                if (CanRead(save, s.Number)) list.Add(s.Number);
            return list;
        }
    }
}
