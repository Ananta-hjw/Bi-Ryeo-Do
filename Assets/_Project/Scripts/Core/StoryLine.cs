namespace BiRyeoDo.Core
{
    /// <summary>
    /// One line of a story scene. Plain text is narration; <c>"이름|대사"</c> is a character speaking (the UI shows a portrait
    /// and a name tag). Stage and scene text stays plain strings so content remains simple data.
    /// </summary>
    public struct StoryLine
    {
        public const string Protagonist = "비류연";

        public string Speaker;   // null/empty = narration
        public string Text;

        public bool IsDialogue { get { return !string.IsNullOrEmpty(Speaker); } }
        public bool IsProtagonist { get { return Speaker == Protagonist; } }

        public static StoryLine Parse(string raw)
        {
            if (raw == null) return new StoryLine { Text = "" };
            int bar = raw.IndexOf('|');
            if (bar <= 0 || bar > 12) return new StoryLine { Text = raw };
            return new StoryLine { Speaker = raw.Substring(0, bar).Trim(), Text = raw.Substring(bar + 1).Trim() };
        }
    }
}
