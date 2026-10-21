using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>Enhancement material rewards: independent tier/count rolls, with a first-boss guarantee.</summary>
    public static class GearDrops
    {
        private static readonly int[] CountOdds = { 35, 15, 14, 13, 12, 11 };
        private static readonly int[][] Tiers =
        {
            new[] { 0 }, new[] { 0, 1 }, new[] { 0, 1, 2 },
            new[] { 1, 2, 3 }, new[] { 2, 3, 4 }, new[] { 3, 4 }
        };
        private static readonly int[][] TierOdds =
        {
            new[] { 100 }, new[] { 55, 45 }, new[] { 35, 35, 30 },
            new[] { 35, 35, 30 }, new[] { 35, 35, 30 }, new[] { 55, 45 }
        };

        private static int Pool(int stage, int difficulty)
        {
            if (difficulty >= 2) return 5;
            if (difficulty == 1) return stage <= 10 ? 3 : 4;
            return stage <= 10 ? 0 : stage <= 20 ? 1 : 2;
        }

        private static int Pick(int[] odds, IRandom rng)
        {
            double roll = rng.NextDouble() * 100;
            int sum = 0;
            for (int i = 0; i < odds.Length; i++)
            {
                sum += odds[i];
                if (roll < sum) return i;
            }
            return odds.Length - 1;
        }

        public static ItemData HighestMaterial(int stage, int difficulty)
        {
            var tiers = Tiers[Pool(stage, difficulty)];
            return Gear.Materials[tiers[tiers.Length - 1]];
        }

        /// <summary>A first boss gives the highest tier x7 in addition to the normal roll. Matching tiers are merged.</summary>
        public static List<ItemStack> Roll(int stage, int difficulty, bool firstBossClear, IRandom rng)
        {
            var drops = new List<ItemStack>();
            if (firstBossClear) drops.Add(new ItemStack(HighestMaterial(stage, difficulty).Id, 7));
            int pool = Pool(stage, difficulty);
            int tier = Tiers[pool][Pick(TierOdds[pool], rng)];
            int count = Pick(CountOdds, rng) * 2;
            if (count > 0)
            {
                string id = Gear.Materials[tier].Id;
                if (drops.Count > 0 && drops[0].Id == id) drops[0].Count += count;
                else drops.Add(new ItemStack(id, count));
            }
            return drops;
        }

        public static bool IsBossFight(StageData stage, int fight)
        {
            if (stage == null || stage.Encounters == null || fight < 0 || fight >= stage.Encounters.Length) return false;
            foreach (var id in stage.Encounters[fight])
            {
                var enemy = stage.EnemyFor(id);
                if (enemy != null && enemy.IsBoss) return true;
            }
            return false;
        }

        /// <summary>Call before recording the victory. Story first clears are tracked separately per difficulty and fight.</summary>
        public static List<ItemStack> RollFight(SaveData save, StageData stage, int fight, IRandom rng)
        {
            if (stage == null || stage.IsTower || stage.IsQuest || stage.Number <= 0
                || stage.Encounters == null || fight < 0 || fight >= stage.Encounters.Length) return new List<ItemStack>();
            bool first = !Recommend.IsReplay(save, stage) && !Recommend.WonBefore(save, stage, fight);
            return Roll(stage.Number, stage.Difficulty, first && IsBossFight(stage, fight), rng);
        }
    }
}
