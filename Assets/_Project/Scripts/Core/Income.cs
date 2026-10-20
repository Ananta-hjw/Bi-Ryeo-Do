using System;

namespace BiRyeoDo.Core
{
    /// <summary>두 수입 상향: every payout of 두 is raised by <see cref="Factor"/> and rounded to the nearest 10 (half up). Prices and fees are not touched.</summary>
    public static class Income
    {
        public const decimal Factor = 1.3m;

        /// <summary>2150 → 2800, 2142 → 2780, 105 → 140.</summary>
        public static long Boost(long doo)
        {
            if (doo <= 0) return doo;
            return (long)(Math.Round(doo * Factor / 10m, MidpointRounding.AwayFromZero) * 10m);
        }
    }
}
