using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// 무한 비무대 (infinite tower): one fight per floor, every 5th floor a boss. The first floors are about as hard as the early
    /// stages and floor 30 about as hard as stage 30; beyond that the enemies keep growing. Enemies are copies of the story
    /// enemies set to the floor's level, so the same balance rules apply. The only reward for a floor is the first clear.
    /// </summary>
    public static class Tower
    {
        public const int UnlockStage = 8;   // the full party (효룡, 장홍, 윤준호) has joined; floors are tuned for it
        public const int BossEvery = 5;

        /// <summary>Floors that were tuned directly with the bot; deeper floors keep the last values and grow 3% per floor.</summary>
        public const int TunedFloors = 24;

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
                if (e.Moves == null || e.Moves.Length == 0 || e.RetreatHpPct > 0f || e.HpMul > 10f) continue;   // skip trial-only enemies
                (e.IsBoss ? _bosses : _normals).Add(e);
            }
        }

        private static EnemyData PickNear(List<EnemyData> pool, int level, Random rng)
        {
            var sorted = new List<EnemyData>(pool);
            sorted.Sort((a, b) => Math.Abs(a.Level - level).CompareTo(Math.Abs(b.Level - level)));
            int window = Math.Min(5, sorted.Count);
            return sorted[rng.Next(window)];
        }

        // Every tower enemy uses the same simple scripts (the story enemies' own scripts vary a lot in difficulty), so floors differ only by numbers.
        private static readonly EnemyMove[] NormalMoves = { EnemyMove.Attack("연타", 0.8f, 2), EnemyMove.Attack("일격", 1.2f) };
        private static readonly EnemyMove[] BossMoves = { EnemyMove.Attack("연격", 0.7f, 3), EnemyMove.Attack("강타", 1.3f), EnemyMove.Wind("기를 모은다"), EnemyMove.Attack("필살 일격", 3.0f) };

        public const float NormalHpMul = 1.7f, BossHpMul = 3.4f;

        // Attack multipliers per floor (index = floor, 0..TunedFloors), fitted with the balance bot so that a party whose level equals
        // the floor level wins most of the time on normal floors and about half the time on bosses.
        public static float[] NormalAtk = { 16.5f, 16.5f, 16.5f, 16.5f, 16.5f, 16.5f, 15.4f, 13.75f, 8.25f, 8.25f, 8.25f, 8.53f, 8.53f, 8.53f, 8.53f, 8.53f, 8.53f, 8.25f, 6.32f, 6.32f, 6.32f, 6.6f, 6.6f, 6.6f, 6.6f };
        public static float[] BossAtk = { 13.75f, 13.75f, 13.75f, 13.75f, 13.75f, 11.83f, 11.83f, 11.83f, 11.83f, 11.83f, 13.2f, 11f, 9.62f, 8.8f, 8.25f, 7.97f, 7.7f, 7.15f, 6.6f, 6.32f, 6.05f, 6.32f, 6.32f, 6.32f, 6.32f };

        /// <summary>Test hook for tuning: when set, replaces the fitted attack multiplier of a floor.</summary>
        public static Dictionary<int, float> AtkOverride;

        public static float AtkMulFor(int floor, bool boss)
        {
            float v;
            if (AtkOverride != null && AtkOverride.TryGetValue(floor, out v)) return v;
            var table = boss ? BossAtk : NormalAtk;
            // Correct only the measured boss-floor spikes, including their escorts. Do not ease ordinary floors.
            // Beyond 30 keep the final correction while Clone's 3%-per-floor growth continues.
            float pressure = floor == 5 ? 0.8f : floor == 15 ? 0.9f : floor == 25 ? 0.95f
                : floor >= 30 && IsBossFloor(floor) ? 0.8f : 1f;
            return table[Math.Max(0, Math.Min(floor, TunedFloors))] * pressure;
        }

        private static EnemyData Clone(EnemyData t, string id, int floor, int level, double creep)
        {
            return new EnemyData
            {
                Id = id, Name = t.Name, ArtId = t.ArtKey, IsBoss = t.IsBoss, Level = level,
                HpMul = (float)((t.IsBoss ? BossHpMul : NormalHpMul) * creep), AtkMul = (float)(AtkMulFor(floor, t.IsBoss) * creep),
                DefMul = 1f, SpeedAdd = 0, Moves = t.IsBoss ? BossMoves : NormalMoves
            };
        }

        /// <summary>The (deterministic) enemies of a floor, registered so battle code can look them up by id.</summary>
        public static EnemyData[] BuildEnemies(int floor)
        {
            EnsurePools();
            var rng = new Random(floor * 7919 + 13);
            int level = FloorLevel(floor);
            double creep = floor > TunedFloors ? 1.0 + 0.03 * (floor - TunedFloors) : 1.0;
            var list = new List<EnemyData>();
            if (IsBossFloor(floor))
            {
                list.Add(Clone(PickNear(_bosses, level, rng), "tower" + floor + "_0", floor, level, creep));
                int adds = floor >= 15 ? 2 : 1;
                for (int i = 0; i < adds; i++) list.Add(Clone(PickNear(_normals, level, rng), "tower" + floor + "_" + (i + 1), floor, level, creep));
            }
            else
            {
                int count = floor >= 8 ? 3 : 2;
                for (int i = 0; i < count; i++) list.Add(Clone(PickNear(_normals, level, rng), "tower" + floor + "_" + i, floor, level, creep));
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
                RecommendedLevel = FloorLevel(floor),
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
