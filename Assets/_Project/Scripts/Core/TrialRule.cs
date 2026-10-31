namespace BiRyeoDo.Core
{
    public enum TrialKind
    {
        None,
        Survive,    // 검후 십초지적: win by enduring Limit enemy actions with someone still standing
        Deadline,   // 목요관 경공계주: defeat the enemies within Limit party actions
        Lifeline,   // 금요관: every charged enemy move that lands cuts a lifeline; Limit cuts lose
        Harmony,    // 수화관 내공합격: an unbroken chain of attacks by the whole party powers up the next hit; defeat the enemies within Limit party actions
        Technique,  // use an owned scene technique in the required window before the party-action limit
        Finale      // scripted showdown (일반 30 화룡멸겁대진, 46 천외일도): a fixed opening, the scene, then 비류연's one action is the art that ends it (TrialRule.Script)
    }

    /// <summary>How a scripted showdown (<see cref="TrialKind.Finale"/>) opens before 비류연's last action.</summary>
    public enum FinaleStyle
    {
        PartyFirst,   // every living member acts once, then the enemy's one blow fells all but 비류연 (left at 1 HP)
        Unavoidable,  // the enemy's every blow lands through guard and dodge, ours do nothing; after FinaleTurns actions or below TriggerHpPct the scene begins
        Window,       // an ordinary fight where our blows lose most of their force (ArmorFactor); while the enemy winds up, 비류연's turn offers the art that ends it
        Duel,         // 비류연 alone, his turn from the start: the screen times the strike (24 동전); each late press costs BlowPct of max HP, the Limit-th one loses
    }

    /// <summary>A special rule for one fight of a story trial (시험). The fight still runs on the normal engine.</summary>
    public sealed class TrialRule
    {
        public TrialKind Kind;
        public int Limit;
        public string Title, Brief;
        public string TechniqueId;
        public string[] TechniqueSequence, ResponseCues;
        public int TechniqueUses = 1;
        public bool RequiresCharge;
        public FinaleStyle Style;
        public ShowdownScript Script;
        public int FinaleTurns = 3;
        public float BlowPct = 0.2f, TriggerHpPct = 0.3f;
        public float ArmorFactor = 0.25f;

        public static TrialRule Technique(string title, string brief, string skillId, int partyActions = 20, bool requiresCharge = false, int uses = 1)
        {
            if (string.IsNullOrEmpty(skillId) || uses < 1 || partyActions < uses) throw new System.ArgumentException("Invalid technique objective");
            return new TrialRule { Kind = TrialKind.Technique, Title = title, Brief = brief, TechniqueId = skillId, Limit = partyActions, RequiresCharge = requiresCharge, TechniqueUses = uses };
        }

        /// <summary>Ordered responses to named wind-ups; a body can outlast all three without defeating the opponent.</summary>
        public static TrialRule Responses(string title, string brief, string[] skills, string[] cues, int partyActions = 30)
        {
            if (skills == null || cues == null || skills.Length == 0 || skills.Length != cues.Length || partyActions < skills.Length)
                throw new System.ArgumentException("Invalid response sequence");
            foreach (var id in skills) if (string.IsNullOrEmpty(id)) throw new System.ArgumentException("Missing response skill");
            foreach (var cue in cues) if (string.IsNullOrEmpty(cue)) throw new System.ArgumentException("Missing response cue");
            return new TrialRule { Kind = TrialKind.Technique, Title = title, Brief = brief, TechniqueId = skills[0],
                TechniqueSequence = (string[])skills.Clone(), ResponseCues = (string[])cues.Clone(), TechniqueUses = skills.Length,
                RequiresCharge = true, Limit = partyActions };
        }

        public static TrialRule Finale(string title, string brief, ShowdownScript script, FinaleStyle style = FinaleStyle.PartyFirst)
        {
            if (script == null) throw new System.ArgumentException("A showdown needs its script");
            return new TrialRule { Kind = TrialKind.Finale, Title = title, Brief = brief, Script = script, Style = style };
        }

        /// <summary>A timed one-on-one (24 동방학): <paramref name="misses"/> late presses lose the fight, each one costs <paramref name="blowPct"/> of max HP.</summary>
        public static TrialRule Duel(string title, string brief, ShowdownScript script, int misses = 5, float blowPct = 0.2f)
        {
            if (script == null || misses < 1) throw new System.ArgumentException("A duel needs its script and at least one chance");
            return new TrialRule { Kind = TrialKind.Finale, Title = title, Brief = brief, Script = script, Style = FinaleStyle.Duel, Limit = misses, BlowPct = blowPct };
        }
        public static TrialRule Survive(string title, string brief, int enemyActions) { return new TrialRule { Kind = TrialKind.Survive, Limit = enemyActions, Title = title, Brief = brief }; }
        public static TrialRule Deadline(string title, string brief, int partyActions) { return new TrialRule { Kind = TrialKind.Deadline, Limit = partyActions, Title = title, Brief = brief }; }
        public static TrialRule Lifeline(string title, string brief, int cuts) { return new TrialRule { Kind = TrialKind.Lifeline, Limit = cuts, Title = title, Brief = brief }; }
        public static TrialRule Harmony(string title, string brief, int partyActions) { return new TrialRule { Kind = TrialKind.Harmony, Limit = partyActions, Title = title, Brief = brief }; }
    }
}
