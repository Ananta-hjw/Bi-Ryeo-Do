using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// Real gear (items of the catalogue, enhancement and stars, worked out exactly like a player's) worn by enemies (<see cref="ForEnemies"/> on story
    /// stages, <see cref="ForEnemiesAt"/> by player level for quests, the tower and 승급 비무; a little below the gear a player is expected to have)
    /// and by the balance bot (<see cref="ForParty"/>, <see cref="ForPartyAt"/>).
    /// Grades follow what a simulated player wears at the stage's level (mixed: some slots one grade higher); the enhancement is then chosen so the
    /// loadout raises the 종합 수치 by its target (<see cref="ExpectedGear"/>). Six slots: weapon, 상의, 하의, 신발, two accessories.
    /// </summary>
    public static class GearLoadout
    {
        /// <summary>Enemies assume this share of the measured gear bonus (players are assumed to have <see cref="ExpectedGear.Share"/>).</summary>
        public const float EnemyShare = 0.6f;

        /// <summary>Enemies wear this many grades below the simulated player's average.</summary>
        public const float EnemyGradeGap = 0.5f;

        // Average grade (일반 0 .. 신화 5) the simulated player wears by level (party average, first time reached, never falling).
        private static readonly float[][] GradeByLevel =
        {
            new[] { 1f, 0.60f }, new[] { 5f, 0.60f }, new[] { 6f, 1.48f }, new[] { 7.5f, 1.98f }, new[] { 13f, 2.00f }, new[] { 14f, 2.48f },
            new[] { 15f, 2.67f }, new[] { 17f, 2.88f }, new[] { 18f, 3.18f }, new[] { 19f, 3.36f }, new[] { 21f, 3.49f }, new[] { 22f, 3.60f },
            new[] { 25f, 3.80f }, new[] { 27f, 3.88f }, new[] { 30f, 4.03f }, new[] { 32f, 4.22f }, new[] { 44f, 4.33f },
        };

        private static readonly ItemKind[] SlotKinds = { ItemKind.Weapon, ItemKind.Armor, ItemKind.Pants, ItemKind.Boots, ItemKind.Accessory, ItemKind.Accessory };
        // Slots that take the higher grade first when the grade is mixed: weapon, 상의, first accessory, 하의, 신발, second accessory.
        private static readonly int[] UpgradeOrder = { 0, 1, 4, 2, 3, 5 };

        private static readonly Dictionary<string, List<GearLevel>> Cache = new Dictionary<string, List<GearLevel>>();

        public static float GradeAt(double level)
        {
            if (level <= GradeByLevel[0][0]) return GradeByLevel[0][1];
            for (int i = 1; i < GradeByLevel.Length; i++)
            {
                if (level > GradeByLevel[i][0]) continue;
                var a = GradeByLevel[i - 1]; var b = GradeByLevel[i];
                return (float)(a[1] + (b[1] - a[1]) * (level - a[0]) / (b[0] - a[0]));
            }
            return GradeByLevel[GradeByLevel.Length - 1][1];
        }

        /// <summary>What the enemies of a story stage wear; null on the solo 일반 stages 1-6 (they assume the start kit only).</summary>
        public static List<GearLevel> ForEnemies(int stage, int difficulty)
        {
            return difficulty <= 0 && stage < ExpectedGear.FirstPartyStage ? null : ForEnemiesAt(ExpectedGear.LevelFor(stage, difficulty));
        }

        /// <summary>What enemies built for a player of this level wear (quests, the tower, 승급 비무); null below the first party stage's level.</summary>
        public static List<GearLevel> ForEnemiesAt(int level)
        {
            if (level < FirstLevel) return null;
            var bare = Progression.HeroStatsFor(level);
            // the gain the expected gear brings over the start kit (the old balance assumed a start-kit player against bare enemies)
            double target = Realm.Power(ExpectedGear.Apply(bare, ExpectedGear.Measured(level), EnemyShare)) / Realm.Power(ExpectedGear.Apply(bare, ExpectedGear.StartKit));
            return Build("e", level, target, GradeAt(level) - EnemyGradeGap);
        }

        /// <summary>What the balance bot's party wears on a story stage instead of the start kit; null on the solo 일반 stages 1-6 (start kit).</summary>
        public static List<GearLevel> ForParty(int stage, int difficulty)
        {
            return difficulty <= 0 && stage < ExpectedGear.FirstPartyStage ? null : ForPartyAt(ExpectedGear.LevelFor(stage, difficulty));
        }

        /// <summary>The balance bot's gear for a player of this level (<see cref="ExpectedGear.Share"/> of the measured gear); null below the first party stage's level.</summary>
        public static List<GearLevel> ForPartyAt(int level)
        {
            if (level < FirstLevel) return null;
            var bare = Progression.HeroStatsFor(level);
            double target = Realm.Power(ExpectedGear.Apply(bare, ExpectedGear.Measured(level))) / Realm.Power(bare);
            return Build("p", level, target, GradeAt(level));
        }

        /// <summary>Gear starts at the level of the first stage fought with companions (일반 7).</summary>
        public static int FirstLevel { get { return ExpectedGear.LevelFor(ExpectedGear.FirstPartyStage, 0); } }

        public static IEnumerable<string> Ids(List<GearLevel> gear) { foreach (var g in gear) yield return g.Id; }

        /// <summary>Lowest star count that allows an enhancement level (+10 needs none, +11 one star, ...).</summary>
        public static int StarsFor(int level)
        {
            int s = 0;
            while (s < Gear.MaxStars && Gear.LevelCaps[s] < level) s++;
            return s;
        }

        private static List<GearLevel> Build(string who, int level, double target, float grade)
        {
            string key = who + level;
            List<GearLevel> cached;
            if (Cache.TryGetValue(key, out cached)) return cached;
            var bare = Progression.HeroStatsFor(level);
            double basePower = Realm.Power(bare);
            // Six-steps of grade: 6 x grade slot-grades in total. Lower it until even +0 does not overshoot the target.
            int steps = Math.Max(0, Math.Min(6 * 5, (int)Math.Round(grade * 6)));
            List<GearLevel> best = null;
            for (; steps >= 0; steps--)
            {
                var ids = Pick(steps);
                int lo = 0, hi = Gear.MaxLevel;
                if (Ratio(bare, ids, 0, basePower) > target && steps > 0) continue;
                while (lo < hi)   // smallest level reaching the target
                {
                    int mid = (lo + hi) / 2;
                    if (Ratio(bare, ids, mid, basePower) >= target) hi = mid; else lo = mid + 1;
                }
                int enhance = lo;
                if (enhance > 0 && Math.Abs(Math.Log(Ratio(bare, ids, enhance - 1, basePower) / target)) < Math.Abs(Math.Log(Ratio(bare, ids, enhance, basePower) / target))) enhance--;
                best = Levels(ids, enhance);
                break;
            }
            Cache[key] = best;
            return best;
        }

        private static float Core(ItemData item)
        {
            var e = Gear.EffectsFor(item);
            return e.HpPct + 2f * e.AtkPct + 0.5f * e.DefPct;
        }

        private static List<GearLevel> Levels(List<string> ids, int level)
        {
            var list = new List<GearLevel>();
            foreach (var id in ids) list.Add(new GearLevel { Id = id, Level = level, Stars = StarsFor(level) });
            return list;
        }

        private static double Ratio(CombatStats bare, List<string> ids, int level, double basePower)
        {
            return Realm.Power(Inventory.ApplyGear(bare, ids, Levels(ids, level))) / basePower;
        }

        // Slot grades from the six-step total (higher grade first in UpgradeOrder), then per slot the piece of that grade and kind with the most
        // HP + 2 x attack + defense / 2 (attack first: more HP alone only makes fights longer). Every stage has the same make-up; no hero-only pieces,
        // and the two accessories differ.
        private static List<string> Pick(int steps)
        {
            int baseGrade = steps / 6, up = steps % 6;
            var grades = new int[6];
            for (int i = 0; i < 6; i++) grades[i] = baseGrade;
            for (int i = 0; i < up; i++) grades[UpgradeOrder[i]]++;
            var ids = new List<string>();
            for (int slot = 0; slot < 6; slot++)
            {
                var pool = new List<ItemData>();
                foreach (var item in Gacha.Pool((ItemGrade)Math.Min((int)ItemGrade.Mythic, grades[slot])))
                    if (item.Kind == SlotKinds[slot] && !Gear.IsHeroOnly(item.Id) && !ids.Contains(item.Id)) pool.Add(item);
                if (pool.Count == 0) continue;
                pool.Sort((a, b) =>
                {
                    int c = Core(b).CompareTo(Core(a));
                    return c != 0 ? c : string.CompareOrdinal(a.Id, b.Id);
                });
                ids.Add(pool[0].Id);
            }
            return ids;
        }
    }
}
