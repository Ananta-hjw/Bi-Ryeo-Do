using System;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// Diminishing crit chance (2026-11). The summed crit chance counts in full up to <see cref="SoftCap"/>; above it only half of the excess
    /// becomes crit chance and the other half becomes crit damage at 1:1 (1%p chance → +1%p damage). Whatever would push the effective chance
    /// past 100% becomes crit damage in full. Applies to every unit: allies, enemies and the balance bot.
    /// </summary>
    public static class CritRules
    {
        public const float SoftCap = 0.5f;
        public const float ExcessShare = 0.5f;

        /// <summary>The chance a hit actually crits for a summed crit stat.</summary>
        public static float Chance(float raw)
        {
            if (raw <= SoftCap) return Math.Max(0f, raw);
            return Math.Min(1f, SoftCap + (raw - SoftCap) * ExcessShare);
        }

        /// <summary>Crit damage added to the crit multiplier by the converted part of the crit stat (0 at or below the soft cap).</summary>
        public static float ExtraDamage(float raw)
        {
            return raw <= SoftCap ? 0f : raw - Chance(raw);
        }

        /// <summary>The crit damage multiplier including the converted part.</summary>
        public static float Damage(CombatStats s)
        {
            return s.CritDamage + ExtraDamage(s.CritRate);
        }
    }
}
