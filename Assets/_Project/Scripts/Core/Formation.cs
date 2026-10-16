using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// 진형: where the party and the enemies stand when a fight starts (see <see cref="Board"/>).
    /// The party's tiles are the player's choice (<see cref="SaveData.Placement"/>, set in the 파티 편성 screen); without a valid custom
    /// choice an automatic arrangement puts the protagonist and the guard in front and the healer behind. Enemies are placed by rule:
    /// the ordinary ones fill the column facing us, bosses always stand one column behind them.
    /// </summary>
    public static class Formation
    {
        public const string HeroId = "player";

        /// <summary>Front-line priority: 0 hero, 1 guards (태을천강벽), 2 fighters, 3 healers (stay back).</summary>
        private static int Priority(string id)
        {
            if (id == HeroId) return 0;
            int p = 2;
            foreach (var a in EarlyGameContent.Arts)
            {
                if (a.OwnerId != id) continue;
                if (a.Effect == SkillEffect.GuardParty) p = Math.Min(p, 1);
                if (a.Effect == SkillEffect.HealAlly) return 3;
            }
            return p;
        }

        private static readonly int[][] PartySlots =
        {
            new[] { 2, 1 }, new[] { 2, 0 }, new[] { 2, 2 }, new[] { 1, 1 }, new[] { 1, 0 }, new[] { 1, 2 }, new[] { 0, 1 }, new[] { 0, 0 }, new[] { 0, 2 },
        };

        /// <summary>The automatic arrangement: tile of every id.</summary>
        public static Dictionary<string, int[]> Auto(IList<string> ids)
        {
            var sorted = new List<string>();
            for (int p = 0; p <= 3; p++) foreach (var id in ids) if (Priority(id) == p) sorted.Add(id);
            var map = new Dictionary<string, int[]>();
            for (int i = 0; i < sorted.Count && i < PartySlots.Length; i++) map[sorted[i]] = new[] { PartySlots[i][0], PartySlots[i][1] };
            return map;
        }

        private static bool TryParse(string entry, out string id, out int col, out int row)
        {
            id = null; col = row = -1;
            if (string.IsNullOrEmpty(entry)) return false;
            var parts = entry.Split(':');
            if (parts.Length != 3 || !int.TryParse(parts[1], out col) || !int.TryParse(parts[2], out row)) return false;
            id = parts[0];
            return true;
        }

        /// <summary>The tile of every party id: the saved custom arrangement when it still fits (every member on his own tile inside our half), otherwise the automatic one.</summary>
        public static Dictionary<string, int[]> Resolve(SaveData save, IList<string> ids)
        {
            if (save != null && save.PlacementCustom)
            {
                var saved = new Dictionary<string, int[]>();
                foreach (var entry in save.Placement)
                {
                    string id; int col, row;
                    if (TryParse(entry, out id, out col, out row)) saved[id] = new[] { col, row };
                }
                var map = new Dictionary<string, int[]>();
                var used = new HashSet<int>();
                bool ok = true;
                foreach (var id in ids)
                {
                    int[] t;
                    if (!saved.TryGetValue(id, out t) || t[0] < 0 || t[0] >= Board.PartyCols || t[1] < 0 || t[1] >= Board.Rows || !used.Add(t[0] * 10 + t[1])) { ok = false; break; }
                    map[id] = t;
                }
                if (ok) return map;
            }
            return Auto(ids);
        }

        /// <summary>Puts the party on its tiles (the protagonist first in the list).</summary>
        public static void Apply(IList<Combatant> party, SaveData save)
        {
            var ids = new List<string>();
            foreach (var m in party) ids.Add(m.Id);
            var map = Resolve(save, ids);
            foreach (var m in party) { var t = map[m.Id]; m.Col = t[0]; m.Row = t[1]; }
        }

        /// <summary>Moves <paramref name="id"/> to a tile of our half; a member already standing there swaps places.</summary>
        public static bool SetTile(SaveData save, IList<string> ids, string id, int col, int row, out string error)
        {
            error = null;
            if (col < 0 || col >= Board.PartyCols || row < 0 || row >= Board.Rows) { error = "그 칸에는 설 수 없습니다."; return false; }
            var map = Resolve(save, ids);
            int[] mine;
            if (!map.TryGetValue(id, out mine)) { error = "출전하지 않는 인물입니다."; return false; }
            string other = null;
            foreach (var kv in map) if (kv.Key != id && kv.Value[0] == col && kv.Value[1] == row) other = kv.Key;
            if (other != null) map[other] = new[] { mine[0], mine[1] };
            map[id] = new[] { col, row };
            save.PlacementCustom = true;
            save.Placement = new List<string>();
            foreach (var kv in map) save.Placement.Add(kv.Key + ":" + kv.Value[0] + ":" + kv.Value[1]);
            return true;
        }

        public static void Reset(SaveData save)
        {
            save.PlacementCustom = false;
            save.Placement = new List<string>();
        }

        // ------------------------------------------------------------------ enemies

        private static readonly int[][] EnemyFrontSlots = { new[] { 3, 1 }, new[] { 3, 0 }, new[] { 3, 2 }, new[] { 4, 0 }, new[] { 4, 2 }, new[] { 5, 1 }, new[] { 5, 0 }, new[] { 5, 2 } };
        private static readonly int[][] BossSlots = { new[] { 4, 1 }, new[] { 4, 0 }, new[] { 4, 2 }, new[] { 5, 1 } };

        /// <summary>Places enemies that have no tile yet: ordinary ones in the column facing us, bosses one column behind (a lone enemy always stands in front of us).</summary>
        public static void PlaceEnemies(IList<Combatant> enemies)
        {
            var used = new HashSet<int>();
            foreach (var e in enemies) if (e.Col >= 0) used.Add(e.Col * 10 + e.Row);
            if (enemies.Count == 1 && enemies[0].Col < 0)
            {
                enemies[0].Col = 3; enemies[0].Row = 1;
                return;
            }
            foreach (var e in enemies)
            {
                if (e.Col >= 0 || !e.IsBoss) continue;
                foreach (var s in BossSlots) if (used.Add(s[0] * 10 + s[1])) { e.Col = s[0]; e.Row = s[1]; break; }
            }
            foreach (var e in enemies)
            {
                if (e.Col >= 0) continue;
                foreach (var s in EnemyFrontSlots) if (used.Add(s[0] * 10 + s[1])) { e.Col = s[0]; e.Row = s[1]; break; }
            }
        }
    }
}
