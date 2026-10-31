using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>Preserve cleared stages, earned stars and boss-clear records when the early story fight lists change.</summary>
    public static class StoryContentMigration
    {
        public const int CurrentRevision = 13;
        public static void Apply(SaveData save)
        {
            if (save == null || save.StoryContentRevision >= CurrentRevision) return;
            if (save.StageStars == null) save.StageStars = new List<StarEntry>();
            if (save.StoryContentRevision < 1)
            {
                for (int i = save.StageStars.Count - 1; i >= 0; i--)
                {
                    var star = save.StageStars[i];
                    if (star.Stage == 2 && star.Fight == 2) star.Fight = 4;
                    else if (star.Stage == 11)
                    {
                        if (star.Fight == 2) star.Fight = 0;
                        else
                        {
                            save.RetiredStoryStars += StageStars.Count(star.Mask);
                            save.StageStars.RemoveAt(i);
                        }
                    }
                    else if (star.Stage == 12)
                    {
                        if (star.Fight == 1) star.Fight = 4;
                        else if (star.Fight == 2) star.Fight = 5;
                    }
                }
                // A saved between-fight index may refer to a different opponent now. Restart only the changed chapter.
                if (save.ProgressStage == 2 || save.ProgressStage == 11 || save.ProgressStage == 12)
                    StageProgress.Clear(save);
            }
            if (save.StoryContentRevision < 2)
            {
                foreach (var star in save.StageStars)
                {
                    if (star.Stage == 7) star.Fight += 4;
                    else if (star.Stage == 11 && star.Fight == 0) star.Fight = 3;
                    else if (star.Stage == 13 && star.Fight == 1) star.Fight = 3;
                }
                if (save.ProgressStage == 7 || save.ProgressStage == 11 || save.ProgressStage == 13)
                    StageProgress.Clear(save);
            }
            if (save.StoryContentRevision < 3)
            {
                for (int i = save.StageStars.Count - 1; i >= 0; i--)
                {
                    var star = save.StageStars[i];
                    if (star.Stage == 14) star.Fight += 5;
                    else if (star.Stage == 15) star.Fight += 1;
                    else if (star.Stage == 18 && star.Fight == 1) star.Fight = 2;
                    else if (star.Stage == 19 && star.Fight > 0) star.Fight += 2;
                    else if (star.Stage == 17)
                    {
                        if (star.Fight == 0) star.Fight = 1;
                        else if (star.Fight == 1)
                        {
                            // The mixed guard rehearsal is retired; retain its already earned stars once.
                            save.RetiredStoryStars += StageStars.Count(star.Mask);
                            save.StageStars.RemoveAt(i);
                        }
                        // The former final confrontation remains final-fight completion credit.
                    }
                }
                if (save.ProgressStage == 14 || save.ProgressStage == 15 || save.ProgressStage == 17
                    || save.ProgressStage == 18 || save.ProgressStage == 19) StageProgress.Clear(save);
            }
            if (save.StoryContentRevision < 4)
            {
                // Prior final-arc credit stays final; new deck/camp/separate-disguise fights remain unplayed.
                int[] chapter14 = { 0, 1, 3, 4, 6, 7, 9, 10 };
                foreach (var star in save.StageStars)
                    if (star.Stage == 14 && star.Fight >= 0 && star.Fight < chapter14.Length)
                        star.Fight = chapter14[star.Fight];
                if (save.ProgressStage == 14) StageProgress.Clear(save);
            }
            if (save.StoryContentRevision < 5)
            {
                foreach (var star in save.StageStars)
                {
                    if (star.Stage == 15 && star.Fight == 3) star.Fight = 4;
                    else if (star.Stage == 18 && star.Fight == 2) star.Fight = 3;
                    else if (star.Stage == 19 && star.Fight >= 3) star.Fight += 2;
                }
                if (save.ProgressStage == 15 || save.ProgressStage == 18 || save.ProgressStage == 19)
                    StageProgress.Clear(save);
            }
            if (save.StoryContentRevision < 6)
            {
                foreach (var star in save.StageStars)
                {
                    if (star.Stage == 22) star.Fight += 3;
                    else if (star.Stage >= 23 && star.Stage <= 25 && star.Fight == 2) star.Fight = 3;
                }
                // Stage 21 keeps chapter completion credit despite the reordered nonlethal confrontation.
                if (save.ProgressStage >= 21 && save.ProgressStage <= 25) StageProgress.Clear(save);
            }
            if (save.StoryContentRevision < 7)
            {
                // Pre-star saves receive only the old fight credits before the lobby's legacy backfill runs.
                int[] oldCounts = { 3, 3, 4, 5, 3 };
                for (int number = 26; number <= 30 && number <= save.ClearedStage; number++)
                {
                    bool recorded = false;
                    foreach (var star in save.StageStars)
                        if (star.Stage == number && star.Diff == 0) { recorded = true; break; }
                    if (recorded) continue;
                    for (int fight = 0; fight < oldCounts[number - 26]; fight++)
                        save.StageStars.Add(new StarEntry { Stage = number, Fight = fight, Diff = 0, Mask = 1 });
                }
                foreach (var star in save.StageStars)
                {
                    if (star.Stage == 26) star.Fight += 3;
                    else if (star.Stage == 27) star.Fight = star.Fight == 2 ? 6 : star.Fight == 1 ? 4 : 2;
                    else if (star.Stage == 28 && star.Fight >= 1) star.Fight += 1;
                    else if (star.Stage == 29 && star.Fight >= 3) star.Fight = star.Fight == 3 ? 4 : 6;
                    else if (star.Stage == 30 && star.Fight >= 1) star.Fight = star.Fight == 1 ? 2 : 3;
                }
                if (save.ProgressStage >= 26 && save.ProgressStage <= 30) StageProgress.Clear(save);
            }
            if (save.StoryContentRevision < 8)
            {
                int[] oldCounts = { 2, 2, 3, 2, 3, 2 };
                int[][] moves = { new[] { 0, 3 }, new[] { 1, 3 }, new[] { 0, 1, 3 }, new[] { 1, 2 }, new[] { 0, 2, 4 }, new[] { 0, 3 } };
                for (int number = 31; number <= 36 && number <= save.ClearedStage; number++)
                {
                    bool recorded = false;
                    foreach (var star in save.StageStars)
                        if (star.Stage == number && star.Diff == 0) { recorded = true; break; }
                    if (recorded) continue;
                    for (int fight = 0; fight < oldCounts[number - 31]; fight++)
                        save.StageStars.Add(new StarEntry { Stage = number, Fight = fight, Mask = 1 });
                }
                foreach (var star in save.StageStars)
                    if (star.Stage >= 31 && star.Stage <= 36 && star.Fight >= 0 && star.Fight < moves[star.Stage - 31].Length)
                        star.Fight = moves[star.Stage - 31][star.Fight];
                if (save.ProgressStage >= 31 && save.ProgressStage <= 36) StageProgress.Clear(save);
            }
            if (save.StoryContentRevision < 9) CompactFillerEncounters(save);
            if (save.StoryContentRevision < 10) CompactPair(save, 32, 35, 4);
            if (save.StoryContentRevision < 11)
            {
                RemoveEncounter(save, 47, 0);
                RemoveEncounter(save, 48, 3);
            }
            if (save.StoryContentRevision < 12) CompactPair(save, 57, 48, 3);
            if (save.StoryContentRevision < 13)
            {
                RemoveEncounter(save, 62, 0);
                RemoveEncounter(save, 65, 1);
            }
            save.StoryContentRevision = CurrentRevision;
        }

        private static void CompactFillerEncounters(SaveData save)
        {
            CompactPair(save, 19, 31, 3);
        }

        private static void CompactPair(SaveData save, int leadingChapter, int trailingChapter, int oldLast)
        {
            var newlyCleared = new bool[StageStars.DifficultyCount];
            for (int d = 0; d < newlyCleared.Length; d++)
                newlyCleared[d] = (StageStars.MaskOf(save, trailingChapter, oldLast - 1, d) & 1) != 0
                    && (StageStars.MaskOf(save, trailingChapter, oldLast, d) & 1) == 0;
            var compacted = new List<StarEntry>();
            int before = 0, after = 0;
            foreach (var entry in save.StageStars)
            {
                if (entry.Stage != leadingChapter && entry.Stage != trailingChapter) { compacted.Add(entry); continue; }
                before += StageStars.Count(entry.Mask);
                if (entry.Stage == leadingChapter && entry.Fight == 0) continue;
                int fight = entry.Stage == leadingChapter ? entry.Fight - 1 : entry.Fight == oldLast ? oldLast - 1 : entry.Fight;
                var existing = compacted.Find(e => e.Stage == entry.Stage && e.Fight == fight && e.Diff == entry.Diff);
                if (existing == null)
                    compacted.Add(new StarEntry { Stage = entry.Stage, Fight = fight, Diff = entry.Diff, Mask = entry.Mask });
                else existing.Mask |= entry.Mask;
            }
            foreach (var entry in compacted)
                if (entry.Stage == leadingChapter || entry.Stage == trailingChapter) after += StageStars.Count(entry.Mask);
            save.RetiredStoryStars += before - after;
            save.StageStars = compacted;
            // The real final fight now completes the chapter. Credit its unclaimed clear insight once.
            for (int d = 0; d < newlyCleared.Length; d++)
                if (newlyCleared[d]) StageStars.GrantClearInsight(save, trailingChapter, d, false);
            if (save.ProgressStage == leadingChapter) save.ProgressNext = System.Math.Max(0, save.ProgressNext - 1);
            // The old final filler becomes a completion-only resume, without replaying the retained last fight.
            if (save.ProgressStage == trailingChapter) save.ProgressNext = System.Math.Min(save.ProgressNext, oldLast);
        }

        private static void RemoveEncounter(SaveData save, int number, int removed)
        {
            for (int i = save.StageStars.Count - 1; i >= 0; i--)
            {
                var entry = save.StageStars[i];
                if (entry.Stage != number) continue;
                if (entry.Fight == removed)
                {
                    save.RetiredStoryStars += StageStars.Count(entry.Mask);
                    save.StageStars.RemoveAt(i);
                }
                else if (entry.Fight > removed) entry.Fight--;
            }
            if (save.ProgressStage == number && save.ProgressNext > removed) save.ProgressNext--;
        }
    }
}
