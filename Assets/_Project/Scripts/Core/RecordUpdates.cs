using System.Collections.Generic;
using System.Linq;

namespace BiRyeoDo.Core
{
    /// <summary>Record-page discoveries are acknowledged independently from rewards and martial-art notices.</summary>
    public static class RecordUpdates
    {
        public const string CodexPage = "codex", StoryPage = "story", FactionPage = "faction";

        private static IEnumerable<string> Keys(SaveData save, string page)
        {
            if (page == CodexPage)
            {
                foreach (var id in save.SeenEnemies) yield return "enemy:" + id;
                foreach (var id in save.SeenItems) yield return "item:" + id;
            }
            else if (page == StoryPage)
            {
                foreach (int stage in StoryBriefings.Readable(save)) yield return "story:" + stage;
            }
            else if (page == FactionPage)
            {
                foreach (var faction in Factions.All)
                {
                    int tier = Factions.Tier(save, faction.Id);
                    if (tier > 0) yield return "faction:" + faction.Id + ":" + tier;
                }
            }
        }

        public static int Unread(SaveData save, string page)
        {
            var read = new HashSet<string>(save.ReadRecordUpdates ?? new List<string>());
            return Keys(save, page).Distinct().Count(key => !read.Contains(key));
        }

        public static bool Acknowledge(SaveData save, string page)
        {
            if (save.ReadRecordUpdates == null) save.ReadRecordUpdates = new List<string>();
            var read = new HashSet<string>(save.ReadRecordUpdates);
            bool changed = false;
            foreach (var key in Keys(save, page))
                if (read.Add(key)) { save.ReadRecordUpdates.Add(key); changed = true; }
            return changed;
        }

        public static int Count(SaveData save)
        {
            return Unread(save, CodexPage) + Unread(save, StoryPage) + Unread(save, FactionPage)
                + Achievements.ClaimableCount(save) + Memories.UnseenCount(save) + (Ritual.CanComplete(save) ? 1 : 0);
        }
    }
}
