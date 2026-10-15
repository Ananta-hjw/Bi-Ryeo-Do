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

        // The party is stronger than the plain level curve (which enemies and balance clones use): the protagonist most, companions a bit less.
        public const float HeroHp = 1.08f, HeroAtk = 1.06f, HeroDef = 1.04f, HeroQi = 1.08f;
        public const float AllyHp = 1.04f, AllyAtk = 1.03f, AllyDef = 1.02f, AllyQi = 1.04f;

        private static CombatStats Scaled(CombatStats b, float hp, float atk, float def, float qi)
        {
            b.MaxHp = (int)Math.Round(b.MaxHp * hp);
            b.Attack = (int)Math.Round(b.Attack * atk);
            b.Defense = (int)Math.Round(b.Defense * def);
            b.MaxQi = (int)Math.Round(b.MaxQi * qi);
            return b;
        }

        /// <summary>Base stats of the protagonist at a level (before gear).</summary>
        public static CombatStats HeroStatsFor(int level) { return Scaled(StatsFor(level), HeroHp, HeroAtk, HeroDef, HeroQi); }

        /// <summary>Base stats a companion starts from at a level (their own multipliers apply on top).</summary>
        public static CombatStats AllyStatsFor(int level) { return Scaled(StatsFor(level), AllyHp, AllyAtk, AllyDef, AllyQi); }

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
