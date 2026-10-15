using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>One pulled item. A duplicate piece of gear is dismantled into 두 instead of being added again.</summary>
    public sealed class PullResult
    {
        public ItemData Item;
        public bool Duplicate;
        public long Doo;   // 두 refunded for a duplicate
    }

    /// <summary>
    /// 뽑기: after the starting kit, gear comes from here, paid for with 두. Grade odds are fixed
    /// (<see cref="Grades.Rates"/>); within a grade every item is equally likely.
    /// </summary>
    public static class Gacha
    {
        public const int TenPullCount = 10;
        public const double TenPullDiscount = 0.9;

        /// <summary>Price of one pull: grows with progress so it stays a meaningful sink next to stage income.</summary>
        public static long PullCost(SaveData save) { return 200 + 150L * save.ClearedStage; }

        public static long TenPullCost(SaveData save) { return (long)Math.Round(PullCost(save) * TenPullCount * TenPullDiscount); }

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

        public static bool CanAfford(SaveData save, int count)
        {
            return save.Doo >= (count == TenPullCount ? TenPullCost(save) : PullCost(save) * count);
        }

        /// <summary>Pays and pulls <paramref name="count"/> times; returns null (and charges nothing) when 두 is short.</summary>
        public static List<PullResult> Pull(SaveData save, int count, IRandom rng)
        {
            if (count <= 0 || !CanAfford(save, count)) return null;
            save.Doo -= count == TenPullCount ? TenPullCost(save) : PullCost(save) * count;
            var results = new List<PullResult>();
            for (int i = 0; i < count; i++)
            {
                var item = RollItem(rng);
                var r = new PullResult { Item = item };
                if (item.IsGear && Inventory.Count(save, item.Id) > 0)
                {
                    r.Duplicate = true;
                    r.Doo = Grades.DismantleDoo[(int)item.Grade];
                    save.Doo += r.Doo;
                }
                else Inventory.Add(save, item.Id, 1);
                results.Add(r);
            }
            return results;
        }
    }
}
