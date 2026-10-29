using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// Companions have their own level and gear. A companion starts at the protagonist's level the first time it is needed after joining,
    /// then gains the same exp as the protagonist from every fight it takes part in. Gear comes from the shared bag (see <see cref="Inventory.EquipFor"/>).
    /// </summary>
    public static class CompanionGrowth
    {
        private static CompanionProgress Find(SaveData save, string id)
        {
            foreach (var p in save.CompanionLevels) if (p.Id == id) return p;
            return null;
        }

        /// <summary>The companion's level. One that has joined but has no record yet is created at the protagonist's current level and exp; one that has not joined follows the protagonist.</summary>
        public static int LevelOf(SaveData save, string id)
        {
            var p = Find(save, id);
            if (p != null) return p.Level;
            var c = EarlyGameContent.FindCompanion(id);
            if (c == null || c.JoinStage > save.HighestUnlockedStage) return save.Level;
            p = new CompanionProgress { Id = id, Level = save.Level, Exp = save.Exp };   // the protagonist's level and his progress into the next one
            save.CompanionLevels.Add(p);
            return p.Level;
        }

        /// <summary>
        /// Creates the level record of every joined companion that has none yet (old saves), at the protagonist's current level.
        /// Call before the protagonist gains exp, so a companion does not start at the new level and then also gain the same exp.
        /// </summary>
        public static void EnsureRecords(SaveData save)
        {
            foreach (var c in EarlyGameContent.Companions) if (c.JoinStage <= save.HighestUnlockedStage) LevelOf(save, c.Id);
        }

        public static long ExpOf(SaveData save, string id) { LevelOf(save, id); var p = Find(save, id); return p != null ? p.Exp : 0; }

        /// <summary>Adds exp and applies level-ups. Returns the number of levels gained.</summary>
        public static int AddExp(SaveData save, string id, long amount)
        {
            if (amount <= 0) return 0;
            LevelOf(save, id);
            var p = Find(save, id);
            if (p == null) return 0;
            p.Exp += amount;
            int gained = 0;
            while (p.Exp >= Progression.ExpToNext(p.Level))
            {
                p.Exp -= Progression.ExpToNext(p.Level);
                p.Level++;
                gained++;
            }
            return gained;
        }

        /// <summary>"윤준호  자하청심결 습득!" for each art the companion learns on the way from level <paramref name="before"/> to <paramref name="after"/>.</summary>
        public static List<string> NewArtLines(CompanionData c, int before, int after)
        {
            var lines = new List<string>();
            foreach (var a in EarlyGameContent.Arts)
                if (a.OwnerId == c.Id && a.OwnerLevel > before && a.OwnerLevel <= after) lines.Add(c.Name + "  " + a.Name + " 습득!" + ReplaceNote(a));
            return lines;
        }

        /// <summary>" · 백혼검뢰천검식 대신" when the art takes over an older one of the same line.</summary>
        public static string ReplaceNote(ArtData art)
        {
            var old = art == null || string.IsNullOrEmpty(art.Replaces) ? null : EarlyGameContent.FindArt(art.Replaces);
            return old == null ? "" : " · " + old.Name + " 대신";
        }

        /// <summary>The arts a companion will still learn at a higher level, as "자하청심결 (Lv.15)".</summary>
        public static List<string> PendingArtLines(CompanionData c, int level, int clearedStage = 0)
        {
            var lines = new List<string>();
            foreach (var a in EarlyGameContent.Arts)
                if (a.OwnerId == c.Id)
                {
                    if (a.OwnerStage > clearedStage) lines.Add(a.Name + " (여정 " + a.OwnerStage + " 완료)");
                    else if (a.OwnerLevel > level) lines.Add(a.Name + " (Lv." + a.OwnerLevel + ")" + ReplaceNote(a));
                }
            return lines;
        }

        /// <summary>
        /// Call now and then: one entry per joined companion whose 무공 경지 rose above the best announced so far or who learned an art
        /// since the last call. The first call for a companion (newly joined, or an older save) only records the current state.
        /// </summary>
        public static List<CompanionRise> PollRises(SaveData save)
        {
            var rises = new List<CompanionRise>();
            foreach (var c in EarlyGameContent.Companions)
            {
                if (c.JoinStage > save.HighestUnlockedStage) continue;
                int level = LevelOf(save, c.Id);
                var p = Find(save, c.Id);
                if (p == null) continue;
                int step = Create(save, c).RealmStep;
                var rise = new CompanionRise { Companion = c, RealmFrom = step, RealmTo = step };
                if (p.RealmMax < 0) p.RealmMax = step;
                else if (step > p.RealmMax) { rise.RealmFrom = p.RealmMax; p.RealmMax = step; }
                if (p.ArtsLevel < 0) p.ArtsLevel = level;
                else if (level > p.ArtsLevel)
                {
                    foreach (var a in EarlyGameContent.Arts)
                        if (a.OwnerId == c.Id && a.OwnerLevel > p.ArtsLevel && a.OwnerLevel <= level) rise.NewArts.Add(a);
                    p.ArtsLevel = level;
                }
                if (rise.RealmTo > rise.RealmFrom || rise.NewArts.Count > 0) rises.Add(rise);
            }
            return rises;
        }

        /// <summary>Share of a fight's exp that companions who sat the fight out still gain.</summary>
        public const double BenchExpShare = 0.5;

        /// <summary>
        /// Gives every companion who has joined but did not fight <see cref="BenchExpShare"/> of the exp.
        /// Returns "name  Lv.a → Lv.b" for each one who levelled up.
        /// </summary>
        public static List<string> ShareWithBench(SaveData save, ICollection<string> fielded, long exp)
        {
            var lines = new List<string>();
            long share = (long)Math.Round(exp * BenchExpShare);
            if (share <= 0) return lines;
            foreach (var c in EarlyGameContent.Companions)
            {
                if (c.JoinStage > save.HighestUnlockedStage || fielded.Contains(c.Id)) continue;
                int before = LevelOf(save, c.Id);
                int gained = AddExp(save, c.Id, share);
                if (gained > 0) { lines.Add(c.Name + "  Lv." + before + " → Lv." + (before + gained) + "  (대기)"); lines.AddRange(NewArtLines(c, before, before + gained)); }
            }
            return lines;
        }

        /// <summary>The fighting unit for a companion with everything the save gives it: own level, 인연, 수련 and gear.</summary>
        public static Combatant Create(SaveData save, CompanionData c)
        {
            var unit = c.CreateCombatant(LevelOf(save, c.Id), Bonds.Level(save, c.Id), CompanionTraining.LevelOf(save, c.Id), Inventory.GearOf(save, c.Id), Gear.LevelsFor(save, c.Id));
            unit.Skills.RemoveAll(skill => { var art = EarlyGameContent.FindArt(skill.Id); return art != null && art.OwnerStage > save.ClearedStage; });
            return unit;
        }
    }

    /// <summary>What <see cref="CompanionGrowth.PollRises"/> found for one companion.</summary>
    public sealed class CompanionRise
    {
        public CompanionData Companion;
        public int RealmFrom, RealmTo;   // equal when the realm did not rise
        public List<ArtData> NewArts = new List<ArtData>();
        public bool RealmRose { get { return RealmTo > RealmFrom; } }
    }
}
