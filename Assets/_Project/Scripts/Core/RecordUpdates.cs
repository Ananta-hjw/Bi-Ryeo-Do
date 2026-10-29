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

        /// <summary>The page's entries not read yet, as keys ("enemy:id", "item:id", ...), so the page can mark them before acknowledging.</summary>
        public static List<string> UnreadKeys(SaveData save, string page)
        {
            var read = new HashSet<string>(save.ReadRecordUpdates ?? new List<string>());
            return Keys(save, page).Distinct().Where(key => !read.Contains(key)).ToList();
        }

        /// <summary>A faction whose 평판 단계 rose since the 세력 평판 page was last read: from the last read 단계 (0 if none) to the current one.</summary>
        public struct FactionRise
        {
            public FactionData Faction;
            public int From, To;
        }

        /// <summary>The unread 단계 rises, in faction order; call before <see cref="Acknowledge"/>.</summary>
        public static List<FactionRise> FactionRises(SaveData save)
        {
            var read = new HashSet<string>(save.ReadRecordUpdates ?? new List<string>());
            var rises = new List<FactionRise>();
            foreach (var faction in Factions.All)
            {
                int tier = Factions.Tier(save, faction.Id);
                if (tier <= 0 || read.Contains("faction:" + faction.Id + ":" + tier)) continue;
                int from = 0;
                for (int t = tier - 1; t > 0; t--)
                    if (read.Contains("faction:" + faction.Id + ":" + t)) { from = t; break; }
                rises.Add(new FactionRise { Faction = faction, From = from, To = tier });
            }
            return rises;
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
