using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// 진형: where the party and the enemies stand when a fight starts (see <see cref="Board"/>).
    /// The party's tiles are the player's choice (<see cref="SaveData.Placement"/>, set in the 파티 편성 screen); without a valid custom
    /// choice the automatic arrangement picks the lines by combat forecasts against the stage's enemies (<see cref="FormationPlanner"/>), falling back to the
    /// stat rule (<see cref="FrontScores"/>: the sturdiest in front, the strongest attackers at the back, healers in the middle). Enemies answer our lines the
    /// same way, or by the stat rule for big groups; bosses follow the same rule as everyone else.
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

        /// <summary>How much punishment a unit soaks up: HP x (K + defense), defense in fight values.</summary>
        private static double Toughness(CombatStats s) { return (double)s.MaxHp * (BattleEngine.DefenseConstant + s.EffDefense); }

        /// <summary>How much damage a unit deals over time: attack x speed (a fast unit acts more often).</summary>
        private static double Offense(CombatStats s) { return s.EffAttack * Math.Max(1, s.Speed); }

        /// <summary>
        /// How suited each unit of a group is to the front line, from its four stats: sturdiness (HP x defense) relative to the sturdiest of the group
        /// minus damage rate (attack x speed) relative to the best attacker. Positive = a defender (front), negative = an attacker (back).
        /// The same rule arranges our party and the enemies.
        /// </summary>
        public static double[] FrontScores(IList<CombatStats> group)
        {
            double maxT = 1.0, maxO = 1.0;
            foreach (var s in group) { maxT = Math.Max(maxT, Toughness(s)); maxO = Math.Max(maxO, Offense(s)); }
            var scores = new double[group.Count];
            for (int i = 0; i < scores.Length; i++) scores[i] = Toughness(group[i]) / maxT - Offense(group[i]) / maxO;
            return scores;
        }

        /// <summary>The front-line score of one member of our party (healers always stay behind).</summary>
        public static double FrontScore(SaveData save, IList<string> ids, string id)
        {
            if (IsHealer(id)) return -10.0;
            var group = new List<CombatStats>();
            int mine = -1;
            foreach (var other in ids)
            {
                var s = StatsOf(save, other);
                if (s == null) continue;
                if (other == id) mine = group.Count;
                group.Add(s.Value);
            }
            return mine < 0 ? 0.0 : FrontScores(group)[mine];
        }

        private static readonly int[] PartyRowOrder = { 1, 0, 2 };

        /// <summary>How many of <paramref name="n"/> members stand in the front, middle and back line: 1 / 0 / 0, 1 / 0 / 1, 1 / 1 / 1, 2 / 1 / 1, 2 / 2 / 1, ...</summary>
        private static int[] LineSizes(int n)
        {
            if (n <= 1) return new[] { n, 0, 0 };
            if (n == 2) return new[] { 1, 0, 1 };
            int back = Math.Max(1, n / 4 + (n >= 3 ? 1 : 0) - (n >= 7 ? 1 : 0));
            back = Math.Min(back, (n - 1) / 2);
            int front = (n - back + 1) / 2;
            return new[] { front, n - front - back, back };
        }

        /// <summary>
        /// The automatic arrangement by role: the best defenders stand in the front line, the strongest attackers in the back line and the rest in the middle line.
        /// A healer stands just in front of the attackers' line (middle), or at the back when the party is only two. Within a line the most extreme member takes the middle row.
        /// </summary>
        public static Dictionary<string, int[]> Auto(IList<string> ids, SaveData save)
        {
            if (save == null) return Auto(ids);
            var scores = new Dictionary<string, double>();
            double lowestFighter = double.MaxValue;
            foreach (var id in ids)
            {
                scores[id] = FrontScore(save, ids, id);
                if (!IsHealer(id)) lowestFighter = Math.Min(lowestFighter, scores[id]);
            }
            if (lowestFighter == double.MaxValue) lowestFighter = 0.0;
            // a healer is ranked just above the weakest fighter: one step in front of the back line
            foreach (var id in ids) if (IsHealer(id)) scores[id] = lowestFighter + 0.0001;
            var order = new List<string>(ids);
            // sturdiest first; the protagonist wins ties, then the list order
            order.Sort((a, b) =>
            {
                int c = scores[b].CompareTo(scores[a]);
                if (c != 0) return c;
                if (a == HeroId) return -1;
                if (b == HeroId) return 1;
                return 0;
            });
            var map = new Dictionary<string, int[]>();
            if (order.Count == 1) { map[order[0]] = new[] { Board.PartyCols - 1, 1 }; return map; }
            var sizes = LineSizes(order.Count);
            int next = 0;
            for (int line = 0; line < 3; line++)
            {
                int col = Board.PartyCols - 1 - line;   // front line first
                for (int k = 0; k < sizes[line]; k++, next++)
                {
                    // front line: the sturdiest in the middle row; middle and back lines: the one sorted last in the line takes the middle row (the strongest attacker)
                    int idx = line == 0 ? k : sizes[line] - 1 - k;
                    map[order[next]] = new[] { col, PartyRowOrder[idx % PartyRowOrder.Length] };
                }
            }
            return map;
        }

        /// <summary>
        /// Gives <paramref name="id"/> the best free tile next to the members already standing: the line it would stand in if the whole group were arranged automatically,
        /// the nearest line with room otherwise, the middle row first. The members in <paramref name="map"/> stay where they are.
        /// </summary>
        public static void PlaceNewcomer(SaveData save, IList<string> ids, Dictionary<string, int[]> map, string id, StageData st = null)
        {
            var used = new HashSet<int>();
            foreach (var kv in map) if (kv.Key != id) used.Add(kv.Value[0] * 10 + kv.Value[1]);
            int idealCol = Board.PartyCols - 1;
            var lines = save != null ? PlanLines(save, ids, st, map, id) : null;
            int planned;
            if (lines != null && lines.TryGetValue(id, out planned)) idealCol = planned;
            else
            {
                var ideal = Auto(ids, save);
                int[] want;
                if (ideal.TryGetValue(id, out want)) idealCol = want[0];
            }
            var cols = new List<int> { 0, 1, Board.PartyCols - 1 };
            cols.Sort((a, b) => Math.Abs(a - idealCol).CompareTo(Math.Abs(b - idealCol)));
            foreach (int col in cols)
                foreach (int row in PartyRowOrder)
                    if (used.Add(col * 10 + row)) { map[id] = new[] { col, row }; return; }
        }

        /// <summary>
        /// Call when the party changes (a companion sits out or joins the fight): everybody who stays keeps their tile, a newcomer takes the best free tile.
        /// The result is stored as the player's arrangement.
        /// </summary>
        public static void OnPartyChanged(SaveData save, IList<string> oldIds, IList<string> newIds, StageData st = null)
        {
            var before = Resolve(save, oldIds, st);
            var map = new Dictionary<string, int[]>();
            foreach (var id in newIds) { int[] t; if (before.TryGetValue(id, out t)) map[id] = t; }
            foreach (var id in newIds) if (!map.ContainsKey(id)) PlaceNewcomer(save, newIds, map, id, st);
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
        public static Dictionary<string, int[]> Resolve(SaveData save, IList<string> ids, StageData st = null)
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
                foreach (var id in missing) PlaceNewcomer(save, ids, map, id, st);
                return map;
            }
            return AutoFor(save, ids, st);
        }

        // ------------------------------------------------------------------ planned arrangement (combat forecasts, see FormationPlanner)

        /// <summary>The protagonist as he fights: equipped arts (four ordinary + one 비기), stats and 심법 perks.</summary>
        public static Combatant BuildHero(SaveData save) { return BuildHero(save, InnerArts.HeroStatsBeforeRealm(save)); }

        /// <summary>The protagonist with the given stats before the 경지 bonus (the balance bot passes its expected gear here), and the save's arts and 심법 perks.</summary>
        public static Combatant BuildHero(SaveData save, CombatStats statsBeforeRealm)
        {
            var skills = new List<SkillSpec>();
            foreach (var id in save.EquippedArts)
            {
                var art = EarlyGameContent.FindArt(id);
                if (art == null || art.OwnerId != null) continue;
                bool ultimate = Progression.IsUltimate(art);
                int have = 0;
                foreach (var s in skills) if ((s.SpiritCost > 0) == ultimate) have++;
                if (have < (ultimate ? Progression.MaxEquippedUltimates : Progression.MaxEquippedArts)) skills.Add(Mastery.Apply(save, art));
            }
            var hero = new Combatant(HeroId, "비류연", Team.Player, statsBeforeRealm, skills, true);
            InnerArts.ApplyPerks(hero, save.EquippedInner, save.InnerLevels);
            return hero;
        }

        private static List<Combatant> BuildParty(SaveData save, IList<string> ids)
        {
            var party = new List<Combatant>();
            foreach (var id in ids)
            {
                if (id == HeroId) { party.Add(BuildHero(save)); continue; }
                var c = EarlyGameContent.FindCompanion(id);
                if (c != null) party.Add(CompanionGrowth.Create(save, c));
            }
            return party;
        }

        /// <summary>The stage the party screen plans against when no fight is chosen: the next stage to clear.</summary>
        public static StageData PlanningStage(SaveData save)
        {
            return EarlyGameContent.FindStage(Math.Max(1, Math.Min(save.HighestUnlockedStage, EarlyGameContent.Stages.Length)));
        }

        private static readonly Dictionary<string, Dictionary<string, int>> PlanCache = new Dictionary<string, Dictionary<string, int>>();

        /// <summary>Planned lines (0..2) of every id against the stage's fights; members already in <paramref name="pinned"/> keep their lines.</summary>
        private static Dictionary<string, int> PlanLines(SaveData save, IList<string> ids, StageData st, Dictionary<string, int[]> pinned = null, string except = null)
        {
            st = st ?? PlanningStage(save);
            if (st == null) return null;
            var party = BuildParty(save, ids);
            if (party.Count != ids.Count) return null;
            bool resume = !st.IsQuest && !st.IsTower && StageProgress.Has(save, st.Number) && save.ProgressDifficulty == st.Difficulty;
            int first = resume ? FireDragonFinale.ResumeAt(st, save.ProgressNext) : 0;
            if (resume) StageProgress.Restore(save, party);
            var fights = FormationPlanner.FightsOf(st);
            if (first > 0) fights.RemoveRange(0, first);
            if (fights.Count == 0) return null;
            var trials = new List<TrialRule>();
            var key = new System.Text.StringBuilder();
            key.Append(st.Number).Append('/').Append(st.Difficulty).Append('/').Append(st.IsQuest ? st.QuestId : "").Append('/').Append(st.IsTower ? st.TowerFloor : 0);
            key.Append(FormationPlanner.Fingerprint(party, false));
            for (int f = 0; f < fights.Count; f++)
            {
                var trial = st.TrialFor(first + f);
                trials.Add(trial);
                key.Append("/fight/").Append(FormationPlanner.Fingerprint(fights[f], false));
                if (trial != null) key.Append("/trial/").Append((int)trial.Kind).Append('/').Append(trial.Limit);
            }
            Dictionary<string, int> fixedCols = null;
            if (pinned != null)
            {
                fixedCols = new Dictionary<string, int>();
                foreach (var kv in pinned) if (kv.Key != except) { fixedCols[kv.Key] = kv.Value[0]; key.Append("#").Append(kv.Key).Append(kv.Value[0]); }
            }
            Dictionary<string, int> lines;
            string k = key.ToString();
            if (PlanCache.TryGetValue(k, out lines)) return lines;
            var hint = new Dictionary<string, int>();
            foreach (var kv in Auto(ids, save)) hint[kv.Key] = kv.Value[0];
            lines = FormationPlanner.PlanPartyLines(party, fights, hint, fixedCols, trials);
            if (PlanCache.Count > 64) PlanCache.Clear();
            PlanCache[k] = lines;
            return lines;
        }

        /// <summary>
        /// The automatic arrangement: lines chosen by combat forecasts against the stage's enemies (<see cref="FormationPlanner"/>; the next stage to clear
        /// when none is given), rows by the simple stat rule (the most defensive member in the middle of the front line).
        /// </summary>
        public static Dictionary<string, int[]> AutoFor(SaveData save, IList<string> ids, StageData st = null)
        {
            if (save == null) return Auto(ids);
            var lines = PlanLines(save, ids, st);
            if (lines == null || lines.Count != ids.Count) return Auto(ids, save);
            var scores = new Dictionary<string, double>();
            foreach (var id in ids) scores[id] = FrontScore(save, ids, id);
            return ArrangeRows(ids, lines, scores);
        }

        /// <summary>The planned lines for combatants built elsewhere (the balance bot): no save, the fights given directly.</summary>
        public static void ApplyPlanned(IList<Combatant> party, IList<IList<Combatant>> fights)
        {
            var ids = new List<string>();
            foreach (var m in party) ids.Add(m.Id);
            var hint = new Dictionary<string, int>();
            foreach (var kv in Auto(ids)) hint[kv.Key] = kv.Value[0];
            var lines = FormationPlanner.PlanPartyLines(party, fights, hint);
            var stats = new List<CombatStats>();
            foreach (var m in party) stats.Add(m.Stats);
            var fs = FrontScores(stats);
            var scores = new Dictionary<string, double>();
            for (int i = 0; i < party.Count; i++) scores[party[i].Id] = IsHealer(party[i].Id) ? -10.0 : fs[i];
            var map = lines.Count == party.Count ? ArrangeRows(ids, lines, scores) : Auto(ids);
            foreach (var m in party) { var t = map[m.Id]; m.Col = t[0]; m.Row = t[1]; }
        }

        private static Dictionary<string, int[]> ArrangeRows(IList<string> ids, Dictionary<string, int> lines, Dictionary<string, double> scores)
        {
            var map = new Dictionary<string, int[]>();
            for (int line = 0; line < Board.PartyCols; line++)
            {
                var members = new List<string>();
                foreach (var id in ids) if (lines[id] == line) members.Add(id);
                bool front = line == Board.PartyCols - 1;
                members.Sort((a, b) => front ? scores[b].CompareTo(scores[a]) : scores[a].CompareTo(scores[b]));
                for (int k = 0; k < members.Count; k++) map[members[k]] = new[] { line, PartyRowOrder[k % PartyRowOrder.Length] };
            }
            return map;
        }

        /// <summary>Puts the party on its tiles (the protagonist first in the list).</summary>
        public static void Apply(IList<Combatant> party, SaveData save, StageData st = null)
        {
            var ids = new List<string>();
            foreach (var m in party) ids.Add(m.Id);
            var map = Resolve(save, ids, st);
            foreach (var m in party) { var t = map[m.Id]; m.Col = t[0]; m.Row = t[1]; }
        }

        /// <summary>Moves <paramref name="id"/> to a tile of our half; a member already standing there swaps places.</summary>
        public static bool SetTile(SaveData save, IList<string> ids, string id, int col, int row, out string error, StageData st = null)
        {
            error = null;
            if (col < 0 || col >= Board.PartyCols || row < 0 || row >= Board.Rows) { error = "그 칸에는 설 수 없습니다."; return false; }
            var map = Resolve(save, ids, st);
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

        /// <summary>How many of <paramref name="n"/> enemies stand in the front, middle and back column: 1/0/0, 1/1/0, 2/1/0, 2/1/1, then the front column fills up to three.</summary>
        private static int[] EnemyLineSizes(int n)
        {
            if (n <= 1) return new[] { n, 0, 0 };
            if (n == 2) return new[] { 1, 1, 0 };
            if (n == 3) return new[] { 2, 1, 0 };
            int front = Math.Min(Board.Rows, (n + 1) / 2), rest = n - front;
            int back = Math.Min(Board.Rows, rest >= 2 ? Math.Max(1, rest / 2) : 0), mid = rest - back;
            if (mid > Board.Rows) { back += mid - Board.Rows; mid = Board.Rows; }
            return new[] { front, mid, back };
        }

        /// <summary>
        /// Places the enemies as the best answer to the lines our party stands in (combat forecasts, <see cref="FormationPlanner.PlanEnemyCols"/>);
        /// falls back to the simple stat rule for big groups, a lone enemy, or when some enemies already have a tile.
        /// </summary>
        public static void PlaceEnemies(IList<Combatant> enemies, IList<Combatant> party)
        {
            bool anyPlaced = false;
            foreach (var e in enemies) if (e.Col >= 0) anyPlaced = true;
            var cols = !anyPlaced && enemies.Count > 1 ? FormationPlanner.PlanEnemyCols(enemies, party) : null;
            if (cols == null) { PlaceEnemies(enemies); return; }
            PlaceEnemyColumns(enemies, cols);
        }

        internal static void PlaceEnemyColumns(IList<Combatant> enemies, int[] cols)
        {
            var stats = new List<CombatStats>();
            foreach (var e in enemies) stats.Add(e.Stats);
            var scores = FrontScores(stats);
            for (int col = Board.PartyCols; col < Board.Cols; col++)
            {
                var members = new List<int>();
                for (int i = 0; i < enemies.Count; i++) if (cols[i] == col) members.Add(i);
                bool front = col == Board.PartyCols;
                members.Sort((a, b) => front ? scores[b].CompareTo(scores[a]) : scores[a].CompareTo(scores[b]));
                for (int k = 0; k < members.Count; k++) { enemies[members[k]].Col = col; enemies[members[k]].Row = RowOrder[k % RowOrder.Length]; }
            }
        }

        /// <summary>
        /// Places enemies that have no tile yet by their stats, the way a sensible commander would: ordinary arts only reach the front line, so the best defenders
        /// (sturdiness against damage rate, <see cref="FrontScores"/>) hold the front column, the strongest attackers stay at the back, and units that shoot at our
        /// back line (<see cref="EnemyMove.Snipe"/>) always stand in the deepest column. A boss is just another unit. A lone enemy stands right in front of us.
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

            var stats = new List<CombatStats>();
            foreach (var e in todo) stats.Add(e.Stats);
            var scores = FrontScores(stats);
            int shooterCount = 0;
            foreach (var e in todo) if (IsSniper(e)) shooterCount++;
            bool shootersOnly = shooterCount == todo.Count;   // an all-sniper group still needs a front line
            var keys = new Dictionary<Combatant, double>();
            for (int i = 0; i < todo.Count; i++) keys[todo[i]] = !shootersOnly && IsSniper(todo[i]) ? -100.0 : scores[i];
            // best defenders first, shooters last; ties keep the list order
            var order = new List<KeyValuePair<int, Combatant>>();
            for (int i = 0; i < todo.Count; i++) order.Add(new KeyValuePair<int, Combatant>(i, todo[i]));
            order.Sort((x, y) => { int c = keys[y.Value].CompareTo(keys[x.Value]); return c != 0 ? c : x.Key.CompareTo(y.Key); });

            var sizes = EnemyLineSizes(todo.Count);
            if (!shootersOnly && shooterCount > sizes[2])   // every shooter stands in the deepest column
            {
                int extra = Math.Min(Board.Rows, shooterCount) - sizes[2];
                sizes[2] += extra; sizes[1] = Math.Max(0, sizes[1] - extra);
                sizes[0] = todo.Count - sizes[1] - sizes[2];
            }
            int next = 0;
            for (int line = 0; line < 3; line++)
            {
                int col = Board.PartyCols + line;
                for (int k = 0; k < sizes[line] && next < order.Count; k++, next++)
                {
                    // front line: the sturdiest takes the middle row; the other lines: the unit sorted last (the strongest attacker) does
                    int idx = line == 0 ? k : sizes[line] - 1 - k;
                    Place(order[next].Value, col, idx, used);
                }
            }
        }

        private static void Place(Combatant e, int startCol, int rowRank, HashSet<int> used)
        {
            for (int col = startCol; col < Board.Cols; col++)
            {
                for (int r = 0; r < RowOrder.Length; r++)
                {
                    int row = RowOrder[(rowRank + r) % RowOrder.Length];
                    if (used.Add(col * 10 + row)) { e.Col = col; e.Row = row; return; }
                }
            }
            for (int col = Board.PartyCols; col < Board.Cols; col++)   // everything behind was full: take any free tile
                foreach (int row in RowOrder)
                    if (used.Add(col * 10 + row)) { e.Col = col; e.Row = row; return; }
        }
    }
}
