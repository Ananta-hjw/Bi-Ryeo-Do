namespace BiRyeoDo.Core
{
    /// <summary>
    /// One line of a story scene. Plain text is narration; <c>"이름|대사"</c> is a character speaking (the UI shows a portrait
    /// and a name tag). Stage and scene text stays plain strings so content remains simple data.
    /// A leading <see cref="InventedMark"/> (<c>"+비류연|…"</c>, <c>"+지문"</c>) marks a line written for the game, not taken
    /// from the novel; it is stripped before display and kept so those lines can be listed or removed later.
    /// </summary>
    public struct StoryLine
    {
        public const string Protagonist = "비류연";

        public const char InventedMark = '+';

        public string Speaker;   // null/empty = narration
        public string Text;
        public bool Invented;    // written for the game (marked with InventedMark)

        public bool IsDialogue { get { return !string.IsNullOrEmpty(Speaker); } }
        public bool IsProtagonist { get { return Speaker == Protagonist; } }

        public static StoryLine Parse(string raw)
        {
            if (raw == null) return new StoryLine { Text = "" };
            bool invented = IsInvented(raw);
            if (invented) raw = raw.Substring(1);
            int bar = raw.IndexOf('|');
            if (bar <= 0 || bar > 12) return new StoryLine { Text = raw, Invented = invented };
            return new StoryLine { Speaker = raw.Substring(0, bar).Trim(), Text = raw.Substring(bar + 1).Trim(), Invented = invented };
        }

        public static bool IsInvented(string raw) { return !string.IsNullOrEmpty(raw) && raw[0] == InventedMark; }

        /// <summary>The line as shown, without the invented mark.</summary>
        public static string Clean(string raw) { return IsInvented(raw) ? raw.Substring(1) : raw; }
    }
    /// <summary>Authored appearances distinguish a borrowed name from the person wearing it.</summary>
    public static class StoryAppearance
    {
        public const string OldMasterDisguise = "biry_old_master";

        public static bool IsHeroSpeaking(StoryLine line, string heroArtKey)
        {
            if (line.IsProtagonist) return true;
            if (!line.IsDialogue || heroArtKey != OldMasterDisguise) return false;
            return line.Speaker == "사부" || line.Speaker == "사부님" || line.Speaker == "노사부" || line.Speaker == "노사부님";
        }

        public static string HeroBattleKey(StageData stage, int encounter)
        {
            if (stage == null || stage.IsQuest || stage.IsTower) return "player";
            // Volume 1: the waterfall precedes the disguise; the white tiger and escort-agency fights follow it.
            if (stage.Number == 2 || (stage.Number == 1 && encounter == 3)) return OldMasterDisguise;
            return "player";
        }

        public static void ApplyBattle(StageData stage, int encounter, System.Collections.Generic.IEnumerable<Combatant> party)
        {
            if (party == null) return;
            foreach (var member in party)
                if (member.Id == Inventory.Hero)
                {
                    if (HeroBattleKey(stage, encounter) == OldMasterDisguise) member.StoryArtId = OldMasterDisguise;
                    else if (member.StoryArtId == OldMasterDisguise) member.StoryArtId = null;
                }
        }
    }
}
