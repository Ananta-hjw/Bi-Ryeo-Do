using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>Preserve cleared stages, earned stars and boss-clear records when the early story fight lists change.</summary>
    public static class StoryContentMigration
    {
        public const int CurrentRevision = 2;
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
            save.StoryContentRevision = CurrentRevision;
        }
    }
}
