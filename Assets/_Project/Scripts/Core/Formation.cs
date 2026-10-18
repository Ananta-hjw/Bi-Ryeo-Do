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

        // ------------------------------------------------------------------ strategic arrangement (needs the save, for the units' real stats)

        private static bool IsHealer(string id) { return Priority(id) == 3; }

        private static CombatStats? StatsOf(SaveData save, string id)
        {
            if (save == null) return null;
            if (id == HeroId) return InnerArts.HeroStats(save);
            var c = EarlyGameContent.FindCompanion(id);
            return c != null ? CompanionGrowth.Create(save, c).Stats : (CombatStats?)null;
        }

        /// <summary>
        /// How suited a member is to the front line: sturdiness (HP x (1000 + defense)) relative to the sturdiest of the group minus attack relative to the strongest,
        /// so the best defenders score high and the best attackers low. Healers always stay behind.
        /// </summary>
        public static double FrontScore(SaveData save, IList<string> ids, string id)
        {
            if (IsHealer(id)) return -10.0;
            double maxTough = 1, maxAtk = 1;
            foreach (var other in ids)
            {
                var s = StatsOf(save, other);
                if (s == null) continue;
                maxTough = Math.Max(maxTough, Toughness(s.Value));
                maxAtk = Math.Max(maxAtk, s.Value.Attack);
            }
            var mine = StatsOf(save, id);
            if (mine == null) return 0.0;
            return Toughness(mine.Value) / maxTough - mine.Value.Attack / maxAtk;
        }

        private static long Toughness(CombatStats s) { return (long)s.MaxHp * ((long)BattleEngine.DefenseConstant + s.Defense); }

        private static readonly int[] PartyRowOrder = { 1, 0, 2 };

        /// <summary>
        /// The automatic arrangement by role: the better defenders (the top half of the group, at least one) stand in the front line, the strongest attackers and
        /// the healers stand in the back line. Within a line the most extreme member takes the middle row.
        /// </summary>
        public static Dictionary<string, int[]> Auto(IList<string> ids, SaveData save)
        {
            if (save == null) return Auto(ids);
            var scored = new List<KeyValuePair<string, double>>();
            foreach (var id in ids) scored.Add(new KeyValuePair<string, double>(id, FrontScore(save, ids, id)));
            // sturdiest first; the protagonist wins ties, then the list order
            scored.Sort((a, b) =>
            {
                int c = b.Value.CompareTo(a.Value);
                if (c != 0) return c;
                if (a.Key == HeroId) return -1;
                if (b.Key == HeroId) return 1;
                return 0;
            });
            var map = new Dictionary<string, int[]>();
            if (scored.Count == 1) { map[scored[0].Key] = new[] { Board.PartyCols - 1, 1 }; return map; }
            int front = (scored.Count + 1) / 2;
            for (int i = 0; i < scored.Count; i++)
            {
                bool inFront = i < front;
                int col = inFront ? Board.PartyCols - 1 : 0;
                int idx = inFront ? i : scored.Count - 1 - i;   // back line: the strongest attacker (lowest score) is the one in the middle row
                map[scored[i].Key] = new[] { col, PartyRowOrder[idx % PartyRowOrder.Length] };
            }
            return map;
        }

        /// <summary>
        /// Gives <paramref name="id"/> the best free tile next to the members already standing: the front line when its role ranks in the better-defending half of the
        /// group, the back line otherwise (healers always), the middle row first. The members in <paramref name="map"/> stay where they are.
        /// </summary>
        public static void PlaceNewcomer(SaveData save, IList<string> ids, Dictionary<string, int[]> map, string id)
        {
            var used = new HashSet<int>();
            foreach (var kv in map) if (kv.Key != id) used.Add(kv.Value[0] * 10 + kv.Value[1]);
            double mine = FrontScore(save, ids, id);
            int better = 0;
            foreach (var other in ids) if (other != id && FrontScore(save, ids, other) > mine) better++;
            bool wantFront = !IsHealer(id) && better < (ids.Count + 1) / 2;
            int[] cols = wantFront ? new[] { Board.PartyCols - 1, 1, 0 } : new[] { 0, 1, Board.PartyCols - 1 };
            foreach (int col in cols)
                foreach (int row in PartyRowOrder)
                    if (used.Add(col * 10 + row)) { map[id] = new[] { col, row }; return; }
        }

        /// <summary>
        /// Call when the party changes (a companion sits out or joins the fight): everybody who stays keeps their tile, a newcomer takes the best free tile.
        /// The result is stored as the player's arrangement.
        /// </summary>
        public static void OnPartyChanged(SaveData save, IList<string> oldIds, IList<string> newIds)
        {
            var before = Resolve(save, oldIds);
            var map = new Dictionary<string, int[]>();
            foreach (var id in newIds) { int[] t; if (before.TryGetValue(id, out t)) map[id] = t; }
            foreach (var id in newIds) if (!map.ContainsKey(id)) PlaceNewcomer(save, newIds, map, id);
            save.PlacementCustom = true;
            save.Placement = new List<string>();
            foreach (var kv in map) save.Placement.Add(kv.Key + ":" + kv.Value[0] + ":" + kv.Value[1]);
        }

        /// <summary>The automatic arrangement without stats (the balance bot): tile of every id by the fixed priority.</summary>
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

        /// <summary>
        /// The tile of every party id: the saved custom arrangement (members without a valid saved tile, e.g. a newcomer, take the best free tile),
        /// or the strategic automatic arrangement.
        /// </summary>
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
                var missing = new List<string>();
                foreach (var id in ids)
                {
                    int[] t;
                    if (saved.TryGetValue(id, out t) && t[0] >= 0 && t[0] < Board.PartyCols && t[1] >= 0 && t[1] < Board.Rows && used.Add(t[0] * 10 + t[1])) map[id] = t;
                    else missing.Add(id);
                }
                foreach (var id in missing) PlaceNewcomer(save, ids, map, id);
                return map;
            }
            return Auto(ids, save);
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
        private static long Toughness(Combatant e) { return (long)e.Stats.MaxHp * ((long)BattleEngine.DefenseConstant + e.Stats.Defense); }

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
