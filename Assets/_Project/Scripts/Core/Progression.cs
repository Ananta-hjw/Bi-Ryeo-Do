using System.Collections.Generic;
using System;

namespace BiRyeoDo.Core
{
    /// <summary>Growth rules: leveling and the stat curve. Numbers are placeholders to tune in Docs/GAME_DESIGN.md.</summary>
    public static class Progression
    {
        public const int MaxEquippedArts = 4;            // ordinary arts (the 필살기 has its own slot)
        public const int MaxEquippedUltimates = 1;       // one 비기 (기세 100) in the dedicated 필살기 button

        public static bool IsUltimate(ArtData art) { return art != null && art.SpiritCost > 0; }
        public static bool IsUltimate(SkillSpec skill) { return skill != null && skill.SpiritCost > 0; }

        public static int EquippedCount(SaveData save, bool ultimates)
        {
            int n = 0;
            foreach (var id in save.EquippedArts)
            {
                var art = EarlyGameContent.FindArt(id);
                if (art != null && art.OwnerId == null && IsUltimate(art) == ultimates) n++;
            }
            return n;
        }

        /// <summary>Equips an art into its slot kind. A new 비기 replaces the equipped one (returned in <paramref name="replacedId"/>); ordinary arts stop at the cap and return false.</summary>
        public static bool EquipArt(SaveData save, ArtData art, out string replacedId)
        {
            replacedId = null;
            if (art == null || art.OwnerId != null || save.EquippedArts.Contains(art.Id)) return false;
            if (IsUltimate(art))
            {
                foreach (var id in new List<string>(save.EquippedArts))
                {
                    var other = EarlyGameContent.FindArt(id);
                    if (other != null && other.OwnerId == null && IsUltimate(other) && EquippedCount(save, true) >= MaxEquippedUltimates)
                    {
                        save.EquippedArts.Remove(id);
                        replacedId = id;
                    }
                }
            }
            else if (EquippedCount(save, false) >= MaxEquippedArts) return false;
            save.EquippedArts.Add(art.Id);
            return true;
        }

        public static long ExpToNext(int level)
        {
            return (long)Math.Round(50 * Math.Pow(level, 1.5));
        }

        /// <summary>
        /// The stat curve. Enemies and balance clones climb it one step per level (their "curve level"). The protagonist and the companions climb it
        /// <see cref="CurvePerPlayerLevel"/> steps per level: stage 30's enemies stand at curve level 65, which a player reaches at level 25.
        /// </summary>
        public static CombatStats StatsFor(int level) { return StatsAt(level); }

        public static CombatStats StatsAt(double curveLevel)
        {
            double l = Math.Max(1.0, curveLevel) - 1.0;
            var s = new CombatStats
            {
                MaxHp = (int)Math.Round(100 + 20 * l),
                MaxQi = (int)Math.Round(50 + 5 * l),
                Attack = (int)Math.Round((10 + 3 * l) * BattleEngine.AttackScale),
                Defense = (int)Math.Round((50 + 20 * l) / BattleEngine.DefenseScale),
                Speed = 100 + (int)(l / 2),
                CritRate = 0.01f,
                CritDamage = 1.5f
            };
            return s;
        }

        /// <summary>Curve levels gained per player level: (65 - 1) / (25 - 1), so the player is on par with the stage-30 enemies at level 25.</summary>
        public const double CurvePerPlayerLevel = 64.0 / 24.0;

        public static double CurveLevel(int playerLevel) { return 1.0 + (Math.Max(1, playerLevel) - 1) * CurvePerPlayerLevel; }

        /// <summary>The player level that stands against enemies of a curve level (with the old one-level cushion folded in: level 65 maps to 25).</summary>
        public static int PlayerLevelAt(int curveLevel) { return Math.Max(1, 1 + (int)Math.Round(Math.Max(1, curveLevel) / CurvePerPlayerLevel)); }

        // The party is stronger than the plain level curve (which enemies and balance clones use): the protagonist most, companions a bit less.
        public const float HeroHp = 1.08f, HeroAtk = 1.06f, HeroDef = 1.04f, HeroQi = 1.296f;
        public const float AllyHp = 1.45f, AllyAtk = 1.50f, AllyDef = 1.30f, AllyQi = 1.68f;

        private static CombatStats Scaled(CombatStats b, float hp, float atk, float def, float qi)
        {
            b.MaxHp = (int)Math.Round(b.MaxHp * hp);
            b.Attack = (int)Math.Round(b.Attack * atk);
            b.Defense = (int)Math.Round(b.Defense * def);
            b.MaxQi = (int)Math.Round(b.MaxQi * qi);
            return b;
        }

        /// <summary>Base stats of the protagonist at a level (before gear).</summary>
        public static CombatStats HeroStatsFor(int level) { return HeroStatsAtCurve(CurveLevel(level)); }

        public static CombatStats HeroStatsAtCurve(double curveLevel) { return Scaled(StatsAt(curveLevel), HeroHp, HeroAtk, HeroDef, HeroQi); }

        /// <summary>Base stats a companion starts from at a level (their own multipliers apply on top).</summary>
        public static CombatStats AllyStatsFor(int level) { return Scaled(StatsAt(CurveLevel(level)), AllyHp, AllyAtk, AllyDef, AllyQi); }

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
