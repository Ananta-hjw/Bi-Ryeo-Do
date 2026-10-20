using System;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// 두 수입 상향: every payout of 두 is raised in two rounds, +30% and then +20%, each rounded to the nearest 10 (half up). Prices and fees are not touched.
    /// Fixed tables (star chests, memories, fortunes) carry the same raised values as plain data.
    /// </summary>
    public static class Income
    {
        public static readonly decimal[] Rounds = { 1.3m, 1.2m };

        /// <summary>2150 → 2800 → 3360, 2142 → 2780 → 3340, 105 → 140 → 170.</summary>
        public static long Boost(long doo)
        {
            if (doo <= 0) return doo;
            decimal v = doo;
            foreach (var factor in Rounds) v = Math.Round(v * factor / 10m, MidpointRounding.AwayFromZero) * 10m;
            return (long)v;
        }
    }
}
