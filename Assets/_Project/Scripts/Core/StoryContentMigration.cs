using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>Preserve cleared stages, earned stars and boss-clear records when the early story fight lists change.</summary>
    public static class StoryContentMigration
    {
        public const int CurrentRevision = 7;
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
            save.StoryContentRevision = CurrentRevision;
        }
    }
}
