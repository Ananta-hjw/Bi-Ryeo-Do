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
        Finale      // scripted 화룡멸겁대진 (일반 30): the party acts once, the 진 fells everyone but 비류연 (left at 1 HP), then his one action is the 풍신 that ends it
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

        public static TrialRule Finale(string title, string brief) { return new TrialRule { Kind = TrialKind.Finale, Title = title, Brief = brief }; }
        public static TrialRule Survive(string title, string brief, int enemyActions) { return new TrialRule { Kind = TrialKind.Survive, Limit = enemyActions, Title = title, Brief = brief }; }
        public static TrialRule Deadline(string title, string brief, int partyActions) { return new TrialRule { Kind = TrialKind.Deadline, Limit = partyActions, Title = title, Brief = brief }; }
        public static TrialRule Lifeline(string title, string brief, int cuts) { return new TrialRule { Kind = TrialKind.Lifeline, Limit = cuts, Title = title, Brief = brief }; }
        public static TrialRule Harmony(string title, string brief, int partyActions) { return new TrialRule { Kind = TrialKind.Harmony, Limit = partyActions, Title = title, Brief = brief }; }
    }
}
