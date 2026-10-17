using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// "새로 배울 수 있는" notice: a martial art or 심법 that has just been unlocked (and is not learned yet) shows a red dot on the
    /// tab until the player has looked at that page once. Remembered in <see cref="SaveData.SeenArts"/> / <see cref="SaveData.SeenInner"/>.
    /// </summary>
    public static class Discovery
    {
        public static List<ArtData> NewArts(SaveData save)
        {
            var list = new List<ArtData>();
            foreach (var art in EarlyGameContent.Arts)
                if (art.OwnerId == null && art.IsUnlocked(save) && !save.LearnedArts.Contains(art.Id) && !save.SeenArts.Contains(art.Id)) list.Add(art);
            return list;
        }

        public static List<InnerArt> NewInner(SaveData save)
        {
            var list = new List<InnerArt>();
            foreach (var a in InnerArts.All)
                if (InnerArts.IsUnlocked(save, a) && !InnerArts.IsLearned(save, a.Id) && !save.SeenInner.Contains(a.Id)) list.Add(a);
            return list;
        }

        public static int Count(SaveData save) { return NewArts(save).Count + NewInner(save).Count; }

        public static void MarkArtsSeen(SaveData save) { foreach (var a in NewArts(save)) save.SeenArts.Add(a.Id); }
        public static void MarkInnerSeen(SaveData save) { foreach (var a in NewInner(save)) save.SeenInner.Add(a.Id); }

        /// <summary>Saves from before this notice already know everything unlocked so far, so they are not flooded with dots once.</summary>
        public static void Sync(SaveData save)
        {
            if (save.DiscoveryInit) return;
            save.DiscoveryInit = true;
            MarkArtsSeen(save);
            MarkInnerSeen(save);
        }
    }
}
