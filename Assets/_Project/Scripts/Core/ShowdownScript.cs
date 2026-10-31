namespace BiRyeoDo.Core
{
    /// <summary>
    /// The scene and the last art of a scripted showdown (<see cref="TrialKind.Finale"/>): the lines after the opening, the large icon the
    /// player presses, the art the cut-in names, and the lines after it. Only presentation and names; the rules are in InteractiveBattle.
    /// </summary>
    public sealed class ShowdownScript
    {
        public string Title;
        public string[] AfterBlow;        // the scene before the button ("화자|대사" or narration)
        public string IconArtId;          // ArtLibrary.Art id of the large button
        public string IconName, Hint;     // caption above the button / guidance below it
        public string SkillId, SkillName; // the last art (named by the 필살기 cut-in)
        public string BlowName;           // the enemy's blow
        public string FirstBlowQuip, BlowQuip, NoHarmQuip;   // Unavoidable: log lines for the first blow, later blows, and our blows that do nothing
        public string[] AfterStrike;      // the lines once it is over
        public bool HeroCollapses;        // 비류연 sinks down after the strike
        public bool EnemyLeaves;          // the opponent is beaten but not slain (shown as leaving)

        // Two-button showdowns (43 현천포월): the first button plays CutInName's 필살기 cut-in, InterruptLines break into it,
        // and the second button is the last action; StrikeHarmless leaves the opponent unhurt and costs the user SelfCostPct of max HP.
        public string CutInName;
        public string[] InterruptLines;
        public string SecondIconArtId, SecondIconName, SecondHint;
        public bool StrikeHarmless;
        public float SelfCostPct;
        public string StrikeQuip;
        public bool TwoStep { get { return !string.IsNullOrEmpty(SecondIconName); } }

        /// <summary>The last action (기세 100 so the screen plays the full 필살기 cut-in, unless the cut-in already played before a second button).</summary>
        public SkillSpec Skill()
        {
            return new SkillSpec { Id = SkillId, Name = SkillName, Power = 30f, AllEnemies = true, Range = 3, SpiritCost = TwoStep ? 0 : 100 };
        }
    }

    /// <summary>Stage-level helpers for scripted showdowns.</summary>
    public static class Showdowns
    {
        /// <summary>True when the fight is a scripted showdown (it always gives all three stars once won).</summary>
        public static bool Is(StageData stage, int fight)
        {
            var t = stage != null ? stage.TrialFor(fight) : null;
            return t != null && t.Kind == TrialKind.Finale;
        }

        /// <summary>
        /// Resume point of a stage. Only old saves store a point past the last fight; on a stage whose last fights were merged into a showdown
        /// (일반 30, 46) that point picks up at the showdown instead of skipping it.
        /// </summary>
        public static int ResumeAt(StageData stage, int next)
        {
            int last = stage.Encounters.Length - 1;
            if (next > last && Is(stage, last)) return last;
            return System.Math.Min(next, stage.Encounters.Length);
        }
    }
}
