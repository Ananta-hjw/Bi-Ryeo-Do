using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>The first three chapters share one detailed timeline between battle and the record.</summary>
    public static class EarlyStory
    {
        public static bool Supports(StageData stage)
        {
            return stage != null && !stage.IsQuest && !stage.IsTower && stage.Number >= 1 && stage.Number <= 3;
        }

        public static StoryScene Background(StageData stage)
        {
            if (!Supports(stage)) return null;
            return new StoryScene
            {
                Title = stage.Number == 1 ? "현재 — 열아홉 살의 일상" : "이야기 배경",
                Lines = StoryBriefings.Briefing(stage.Number),
                HeroArtKey = stage.Number == 3 ? null : stage.IntroHeroArtKey
            };
        }

        public static StoryScene[] Opening(StageData stage)
        {
            if (!Supports(stage)) return null;
            var scenes = new List<StoryScene>();
            if (stage.Prologue != null) scenes.AddRange(stage.Prologue);
            // The farewell precedes the account of the journey without the master.
            if (stage.Number == 3) scenes.Add(new StoryScene { Title = "이별의 아침", Lines = stage.Intro, HeroArtKey = stage.IntroHeroArtKey });
            scenes.Add(Background(stage));
            var interlude = StoryBriefings.Interlude(stage.Number);
            if (interlude != null) scenes.Add(interlude);
            if (stage.Number != 3) scenes.Add(new StoryScene { Title = stage.Name, Lines = stage.Intro, HeroArtKey = stage.IntroHeroArtKey });
            return scenes.ToArray();
        }
    }
}
