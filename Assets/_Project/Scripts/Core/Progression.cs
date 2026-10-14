using System;

namespace BiRyeoDo.Core
{
    /// <summary>Growth rules: leveling and the stat curve. Numbers are placeholders to tune in Docs/GAME_DESIGN.md.</summary>
    public static class Progression
    {
        public const int MaxEquippedArts = 4;

        public static long ExpToNext(int level)
        {
            return (long)Math.Round(50 * Math.Pow(level, 1.5));
        }

        public static CombatStats StatsFor(int level)
        {
            int l = Math.Max(1, level) - 1;
            var s = new CombatStats
            {
                MaxHp = 100 + 20 * l,
                MaxQi = 50 + 5 * l,
                Attack = 10 + 3 * l,
                Defense = 5 + 2 * l,
                Speed = 100 + l / 2,
                CritRate = 0.01f,
                CritDamage = 1.5f
            };
            return s;
        }

        /// <summary>Adds exp and applies any level-ups. Returns number of levels gained.</summary>
        public static int AddExp(SaveData save, long amount)
        {
            if (amount <= 0) return 0;
            save.Exp += amount;
            int gained = 0;
            while (save.Exp >= ExpToNext(save.Level))
            {
                save.Exp -= ExpToNext(save.Level);
                save.Level++;
                gained++;
            }
            return gained;
        }
    }
}
