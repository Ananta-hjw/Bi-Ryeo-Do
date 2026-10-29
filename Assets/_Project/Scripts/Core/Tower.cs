using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// Fixed floor progression: one fight per floor and a boss every fifth floor. Story scaling and preparation targets
    /// are shared, with separate deep-floor HP/attack growth. Enemy identity is cosmetic; scripts are tower-specific.
    /// Neither enemy strength nor first-clear rewards change with a player's equipment.
    /// </summary>
    public static class Tower
    {
        public const int UnlockStage = 8;   // the full party (효룡, 장홍, 윤준호) has joined; floors are tuned for it
        public const int BossEvery = 5;

        public static bool Unlocked(SaveData save) { return save.ClearedStage >= UnlockStage; }

        /// <summary>The next floor to challenge.</summary>
        public static int NextFloor(SaveData save) { return save.TowerBest + 1; }

        public static bool IsBossFloor(int floor) { return floor % BossEvery == 0; }

        /// <summary>Enemy level of a floor: +2 per floor; floor 24 is level 65, the level of stage 30.</summary>
        public static int FloorLevel(int floor) { return 17 + 2 * floor; }

        public static long RewardDoo(int floor) { return (long)Math.Round((600 + 350L * floor + (IsBossFloor(floor) ? 1500 : 0)) * 1.5); }

        public static ItemStack[] RewardItems(int floor)
        {
            if (floor % 10 == 0) return new[] { new ItemStack("ilgeup_yeongdan", 1) };
            if (IsBossFloor(floor)) return new[] { new ItemStack("ginseng", 1) };
            return new ItemStack[0];
        }

        public static long ExpReward(SaveData save, int floor)
        {
            return (long)Math.Round(Progression.ExpToNext(save.Level) * (IsBossFloor(floor) ? 0.30 : 0.15));
        }

        private static List<EnemyData> _normals, _bosses;

        private static void EnsurePools()
        {
            if (_normals != null) return;
            _normals = new List<EnemyData>();
            _bosses = new List<EnemyData>();
            foreach (var e in EarlyGameContent.Enemies)
            {
                if (e.StoryOnly || e.Moves == null || e.Moves.Length == 0 || e.RetreatHpPct > 0f || e.HpMul > 10f || e.IsMinion) continue;   // skip trial-only enemies and fight-filling followers (keeps the floors as they were)
                (e.IsBoss ? _bosses : _normals).Add(e);
            }
        }

        private static EnemyData PickNear(List<EnemyData> pool, int level, Random rng)
        {
            var sorted = new List<EnemyData>(pool);
            sorted.Sort((a, b) =>
            {
                int distance = Math.Abs(a.Level - level).CompareTo(Math.Abs(b.Level - level));
                return distance != 0 ? distance : StringComparer.Ordinal.Compare(a.Id, b.Id);
            });
            int window = Math.Min(5, sorted.Count);
            return sorted[rng.Next(window)];
        }

        private static readonly EnemyMove[] NormalMoves = { EnemyMove.Attack("연타", .8f, 2), EnemyMove.Attack("일격", 1.2f) };
        private static readonly EnemyMove[][] BossPatterns =
        {
            new[] { EnemyMove.Attack("연격", .6f, 3), EnemyMove.Attack("강타", 1.3f), EnemyMove.Wind("기를 모은다"), EnemyMove.Attack("필살 일격", 3f) },
            new[] { EnemyMove.Attack("일격", 1.2f).Sniping().FromDistance(), EnemyMove.Attack("강타", 1.3f), EnemyMove.Wind("기를 모은다"), EnemyMove.Attack("필살 일격", 3f) },
            new[] { EnemyMove.Attack("연격", .45f, 4), EnemyMove.Attack("연타", .65f, 2), EnemyMove.Wind("기를 모은다"), EnemyMove.Attack("필살 일격", 3f) }
        };
        public const float NormalHpMul = 1.7f, BossHpMul = 3.4f;

        /// <summary>Fixed story anchor for a floor; never follows the player's current save or equipment.</summary>
        private const int BalanceReferenceCap = 41;   // Keep the tuned tower stable when later story chapters are appended.

        public static int ReferenceStage(int floor)
        {
            int result = UnlockStage, distance = int.MaxValue;
            foreach (var stage in EarlyGameContent.Stages)
            {
                if (stage.Number < UnlockStage || stage.Number > BalanceReferenceCap) continue;
                int gap = Math.Abs(stage.RecommendedLevel - FloorLevel(floor));
                if (gap < distance) { result = stage.Number; distance = gap; }
            }
            return result;
        }

        public static int RecommendedPlayerLevel(int floor)
        {
            return Progression.PlayerLevelAt(FloorLevel(floor)) + StageStars.NormalEdgeLevels(ReferenceStage(floor));
        }

        public static int RecommendedCurve(int floor) { return (int)Math.Round(Progression.CurveLevel(RecommendedPlayerLevel(floor))); }

        // The underlying level curve keeps growing. Taper only the extra multiplier beyond floor 30 so
        // stronger defenses and longer fights do not compound into a sudden one-hit/attrition wall.
        public static float AtkMulFor(int floor, bool boss) { return (boss ? 3.0f : 3.3f) / (1f + .01f * Math.Max(0, floor - 30)); }
        public static float FloorHpTune(int floor) { return floor >= 13 ? 1f : 1.25f - .25f * (Math.Max(1, floor) - 1) / 12f; }

        internal static List<BossPhase> PhasesFor()
        {
            return new List<BossPhase>
            {
                new BossPhase { HpPct = BossPhases.FirstPct, AtkMul = BossPhases.FirstAtk, DefMul = BossPhases.FirstDef,
                    Lines = new[] { "비무 상대가 기세를 끌어올린다." } },
                new BossPhase { HpPct = BossPhases.SecondPct, AtkMul = BossPhases.SecondAtk, DefMul = BossPhases.SecondDef,
                    Lines = new[] { "상대의 공세가 더욱 거세진다." } }
            };
        }

        private static EnemyData Clone(EnemyData t, string id, int floor, int level, double hpFactor)
        {
            var enemy = new EnemyData
            {
                Id = id, Name = t.Name, ArtId = t.ArtKey, IsBoss = t.IsBoss, Level = level,
                HpMul = t.IsBoss ? BossHpMul : NormalHpMul * FloorHpTune(floor),
                AtkMul = AtkMulFor(floor, t.IsBoss),
                DefMul = 1f, SpeedAdd = 0, Moves = t.IsBoss ? BossPatterns[(floor / BossEvery) % BossPatterns.Length] : NormalMoves
            };
            var scaled = StageStars.ScaleEnemy(enemy, ReferenceStage(floor), 0).Scaled((float)hpFactor, 1f);
            var gear = GearLoadout.ForEnemiesAt(RecommendedPlayerLevel(floor));   // fixed by the floor's player level
            if (gear == null) return scaled;
            if (Realm.HasRealm(t)) return scaled.WithGear(gear);
            // Keep the floor's combat budget when a creature replaces a human opponent;
            // its equivalent bonuses are innate, so it never displays human equipment.
            return scaled.WithNaturalBonus(gear);
        }

        /// <summary>The (deterministic) enemies of a floor, registered so battle code can look them up by id.</summary>
        public static EnemyData[] BuildEnemies(int floor)
        {
            EnsurePools();
            var rng = new Random(floor * 7919 + 13);
            int level = FloorLevel(floor);
            double hpFactor = 1.0 / (1.0 + .02 * Math.Max(0, floor - 30));
            var list = new List<EnemyData>();
            if (IsBossFloor(floor))
            {
                list.Add(Clone(PickNear(_bosses, level, rng), "tower" + floor + "_0", floor, level, hpFactor));
                int adds = floor >= 15 ? 2 : 1;
                for (int i = 0; i < adds; i++) list.Add(Clone(PickNear(_normals, level, rng), "tower" + floor + "_" + (i + 1), floor, level, hpFactor));
            }
            else
            {
                int count = floor >= 8 ? 3 : 2;
                for (int i = 0; i < count; i++) list.Add(Clone(PickNear(_normals, level, rng), "tower" + floor + "_" + i, floor, level, hpFactor));
            }
            foreach (var e in list) EarlyGameContent.RegisterDynamicEnemy(e);
            return list.ToArray();
        }

        /// <summary>The one-fight stage that plays a floor (companions follow the player's story progress).</summary>
        public static StageData ToStage(SaveData save, int floor)
        {
            var enemies = BuildEnemies(floor);
            var ids = new string[enemies.Length];
            for (int i = 0; i < ids.Length; i++) ids[i] = enemies[i].Id;
            int story = Math.Max(1, Math.Min(save.ClearedStage, EarlyGameContent.Stages.Length));
            return new StageData
            {
                Number = story, Name = "무한 비무대 " + floor + "층", IsTower = true, TowerFloor = floor,
                Intro = new[] { IsBossFloor(floor) ? "이 층에는 강한 상대가 기다린다." : floor + "층. 위로 오를수록 상대는 강해진다." },
                Outro = new[] { floor + "층을 돌파했다." },
                Encounters = new[] { ids },
                RecommendedLevel = RecommendedCurve(floor),
                DooTotal = RewardDoo(floor)
            };
        }

        /// <summary>Records a floor as cleared (first clear only) and grants its items. Returns a one-line note.</summary>
        public static string Complete(SaveData save, int floor)
        {
            if (floor <= save.TowerBest) return "";
            save.TowerBest = floor;
            var notes = new List<string>();
            foreach (var s in RewardItems(floor))
            {
                var item = EarlyGameContent.FindItem(s.Id);
                if (item == null) continue;
                Inventory.Add(save, s.Id, s.Count);
                notes.Add(item.Name);
            }
            return notes.Count > 0 ? "층 보상: " + string.Join(", ", notes) : "";
        }
    }
}
