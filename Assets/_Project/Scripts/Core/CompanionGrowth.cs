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

        /// <summary>The companion's level. One that has joined but has no record yet is created at the protagonist's current level; one that has not joined follows the protagonist.</summary>
        public static int LevelOf(SaveData save, string id)
        {
            var p = Find(save, id);
            if (p != null) return p.Level;
            var c = EarlyGameContent.FindCompanion(id);
            if (c == null || c.JoinStage > save.HighestUnlockedStage) return save.Level;
            p = new CompanionProgress { Id = id, Level = save.Level };
            save.CompanionLevels.Add(p);
            return p.Level;
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

        /// <summary>The fighting unit for a companion with everything the save gives it: own level, 인연, 수련 and gear.</summary>
        public static Combatant Create(SaveData save, CompanionData c)
        {
            return c.CreateCombatant(LevelOf(save, c.Id), Bonds.Level(save, c.Id), CompanionTraining.LevelOf(save, c.Id), Inventory.GearOf(save, c.Id), save.GearLevels);
        }
    }
}
