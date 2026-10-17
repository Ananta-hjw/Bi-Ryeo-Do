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

        private static readonly int[] RowOrder = { 1, 0, 2 };

        private static bool IsSniper(Combatant e)
        {
            if (e.Moves == null) return false;
            foreach (var m in e.Moves) if (m.Snipe) return true;
            return false;
        }

        /// <summary>How much punishment a unit soaks up: the enemy puts its sturdiest units where the front-line-only attacks land.</summary>
        private static long Toughness(Combatant e) { return (long)e.Stats.MaxHp * (100 + e.Stats.Defense); }

        /// <summary>
        /// Places enemies that have no tile yet the way a sensible commander would. Ordinary arts only reach the front line, so the front
        /// column (about half of the group, at most three) holds the sturdiest units, bosses included when they are the sturdiest; the others
        /// stay behind it, and units that shoot at our back line (<see cref="EnemyMove.Snipe"/>) stand in the deepest column.
        /// A lone enemy stands right in front of us.
        /// </summary>
        public static void PlaceEnemies(IList<Combatant> enemies)
        {
            var used = new HashSet<int>();
            foreach (var e in enemies) if (e.Col >= 0) used.Add(e.Col * 10 + e.Row);
            var todo = new List<Combatant>();
            foreach (var e in enemies) if (e.Col < 0) todo.Add(e);
            if (todo.Count == 0) return;
            if (enemies.Count == 1)
            {
                todo[0].Col = 3; todo[0].Row = 1;
                return;
            }

            var shooters = new List<Combatant>();
            var others = new List<Combatant>();
            foreach (var e in todo) (IsSniper(e) ? shooters : others).Add(e);
            if (others.Count == 0) { others.AddRange(shooters); shooters.Clear(); }   // an all-sniper group still needs a front line
            // Stable sort by toughness, sturdiest first (ties keep the list order).
            var order = new List<KeyValuePair<int, Combatant>>();
            for (int i = 0; i < others.Count; i++) order.Add(new KeyValuePair<int, Combatant>(i, others[i]));
            order.Sort((x, y) => { int c = Toughness(y.Value).CompareTo(Toughness(x.Value)); return c != 0 ? c : x.Key.CompareTo(y.Key); });

            int frontCount = Math.Max(1, Math.Min(Math.Min(3, (todo.Count + 1) / 2), order.Count));
            var next = new Dictionary<int, int>();   // per column: how many rows are taken
            Action<Combatant, int> put = (e, startCol) =>
            {
                for (int col = startCol; col < Board.Cols; col++)
                    foreach (int row in RowOrder)
                        if (used.Add(col * 10 + row)) { e.Col = col; e.Row = row; return; }
            };
            for (int i = 0; i < order.Count; i++) put(order[i].Value, i < frontCount ? Board.PartyCols : Board.PartyCols + 1);
            foreach (var e in shooters) put(e, order.Count > frontCount ? Board.PartyCols + 2 : Board.PartyCols + 1);
        }
    }
}
