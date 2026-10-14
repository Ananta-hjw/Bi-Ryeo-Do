namespace BiRyeoDo.Core
{
    public enum TrialKind
    {
        None,
        Survive,    // 검후 십초지적: win by enduring Limit enemy actions with someone still standing
        Deadline,   // 목요관 경공계주: defeat the enemies within Limit party actions
        Lifeline,   // 금요관: every charged enemy move that lands cuts a lifeline; Limit cuts lose
        Harmony     // 수화관 내공합격: an unbroken chain of attacks by the whole party powers up the next hit; defeat the enemies within Limit party actions
    }

    /// <summary>A special rule for one fight of a story trial (시험). The fight still runs on the normal engine.</summary>
    public sealed class TrialRule
    {
        public TrialKind Kind;
        public int Limit;
        public string Title, Brief;

        public static TrialRule Survive(string title, string brief, int enemyActions) { return new TrialRule { Kind = TrialKind.Survive, Limit = enemyActions, Title = title, Brief = brief }; }
        public static TrialRule Deadline(string title, string brief, int partyActions) { return new TrialRule { Kind = TrialKind.Deadline, Limit = partyActions, Title = title, Brief = brief }; }
        public static TrialRule Lifeline(string title, string brief, int cuts) { return new TrialRule { Kind = TrialKind.Lifeline, Limit = cuts, Title = title, Brief = brief }; }
        public static TrialRule Harmony(string title, string brief, int partyActions) { return new TrialRule { Kind = TrialKind.Harmony, Limit = partyActions, Title = title, Brief = brief }; }
    }
}
