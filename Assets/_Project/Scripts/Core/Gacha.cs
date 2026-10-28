using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>One pulled item. A duplicate piece of gear stays in the bag as a spare: enhance the owned copy with it or dismantle it (see <see cref="Salvage"/>).</summary>
    public sealed class PullResult
    {
        public ItemData Item;
        public bool Duplicate;
    }

    /// <summary>
    /// 뽑기: after the starting kit, gear comes from here, paid for with 두 (a repeat becomes a spare copy). Grade odds are fixed
    /// (<see cref="Grades.Rates"/>); within a grade every item is equally likely.
    /// </summary>
    public static class Gacha
    {
        public const int TwentyPullCount = 20, FiftyPullCount = 50;

        /// <summary>Fixed prices (they do not grow with progress): 1 pull 800, 20 pulls 15,000, 50 pulls 35,000 두.</summary>
        public const long SingleCost = 800, TwentyCost = 15000, FiftyCost = 35000;

        /// <summary>Price of a pull of the given size; other sizes cost 800 each.</summary>
        public static long CostFor(int count)
        {
            if (count == FiftyPullCount) return FiftyCost;
            if (count == TwentyPullCount) return TwentyCost;
            return SingleCost * count;
        }

        private static List<ItemData>[] _pools;

        /// <summary>Items that can be pulled at the given grade.</summary>
        public static List<ItemData> Pool(ItemGrade grade)
        {
            if (_pools == null)
            {
                _pools = new List<ItemData>[Grades.Names.Length];
                for (int i = 0; i < _pools.Length; i++) _pools[i] = new List<ItemData>();
                foreach (var item in EarlyGameContent.Items) if (item.InPool) _pools[(int)item.Grade].Add(item);
            }
            return _pools[(int)grade];
        }

        public static ItemGrade RollGrade(IRandom rng)
        {
            double roll = rng.NextDouble(), acc = 0;
            for (int i = 0; i < Grades.Rates.Length; i++)
            {
                acc += Grades.Rates[i];
                if (roll < acc && Pool((ItemGrade)i).Count > 0) return (ItemGrade)i;
            }
            return ItemGrade.Normal;
        }

        public static ItemData RollItem(IRandom rng)
        {
            var pool = Pool(RollGrade(rng));
            int index = (int)(rng.NextDouble() * pool.Count);
            return pool[Math.Min(index, pool.Count - 1)];
        }

        public static bool CanAfford(SaveData save, int count) { return save.Doo >= CostFor(count); }

        /// <summary>Pays and pulls <paramref name="count"/> times; returns null (and charges nothing) when 두 is short.</summary>
        public static List<PullResult> Pull(SaveData save, int count, IRandom rng)
        {
            if (count <= 0 || !CanAfford(save, count)) return null;
            save.Doo -= CostFor(count);
            save.GachaPulls += count;
            Daily.Record(save, DailyEvent.GachaPull, count);
            var results = new List<PullResult>();
            for (int i = 0; i < count; i++)
            {
                var item = RollItem(rng);
                var r = new PullResult { Item = item };
                r.Duplicate = item.IsGear && Inventory.Count(save, item.Id) > 0;
                Inventory.Add(save, item.Id, 1);
                results.Add(r);
            }
            return results;
        }
    }
}
