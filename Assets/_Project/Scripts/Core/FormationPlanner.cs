using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// Chooses lines by expected damage instead of a fixed rule. A position only decides who gets hit:
    /// an enemy's ordinary blow (and every charged heavy blow) lands on our front line, a 저격 move on our back-most line;
    /// our reach-1 arts land on the enemy front line, while reach-2+ arts and area arts reach everyone regardless of position.
    /// <para>
    /// Every unit is turned into damage streams per unit of time (attack x skill power x hits x speed, using its own arts or
    /// enemy script) and an effective HP (HP / damage taken per point, i.e. HP x (K + defense) / K). A small event-driven
    /// simulation then lets both sides drain each other: a line shares the blows aimed at it so its members fall together
    /// (the blows are split in proportion to each member's remaining effective HP), our side focuses the enemy that deals the most damage per remaining HP,
    /// and healing restores the front line. The score is how much of our side is left minus how much of theirs is left (-1..1).
    /// </para>
    /// Our party tries every way to split itself over the three lines and keeps the one whose worst case (the enemies answering
    /// with their own best lines) is best, summed over the fights of the stage. The enemies take the best answer to the lines
    /// they see us standing in. Ties keep the simple stat rule of <see cref="Formation"/>.
    /// </summary>
    public static class FormationPlanner
    {
        private sealed class Unit
        {
            public string Id;
            public int Col;
            public double Ehp0, Ehp;
            public double Mitigation;                 // HP lost per raw point of damage: K / (K + defense)
            public double Front, Any, AreaFront, AreaAll, Snipe, Heal;   // output per unit of time (raw damage, or HP for Heal)
            public double Offense { get { return Front + Any + AreaFront + AreaAll + Snipe; } }
            public bool Alive { get { return Ehp > 1e-9; } }
        }

        // ------------------------------------------------------------------ profiles

        private static Unit Profile(Combatant c, bool enemy, double partyHp)
        {
            var s = c.Stats;
            var u = new Unit { Id = c.Id };
            u.Mitigation = BattleEngine.DefenseConstant / (BattleEngine.DefenseConstant + Math.Max(0.0, s.EffDefense));
            u.Ehp0 = Math.Max(1.0, s.MaxHp) / u.Mitigation;
            double actions = Math.Max(1, s.Speed) / 100.0;
            double hit = s.EffAttack / BattleEngine.AttackScale * actions;
            if (enemy)
            {
                var moves = c.Moves != null && c.Moves.Count > 0 ? c.Moves : new List<EnemyMove> { EnemyMove.Plain };
                foreach (var m in moves)
                {
                    if (m.Charge) continue;
                    double p = m.Power * Math.Max(1, m.Hits) * hit / moves.Count;
                    if (m.Snipe) u.Snipe += p; else u.Front += p;
                }
                return u;
            }
            // Our side: every art it may use (the 비기 aside: it comes rarely), picked about as often as its cooldown allows.
            double weight = 0.0;
            var use = new List<KeyValuePair<SkillSpec, double>>();
            foreach (var k in c.Skills)
            {
                if (k.SpiritCost > 0) continue;
                bool hurts = k.Power > 0f && k.Effect != SkillEffect.HealAlly && k.Effect != SkillEffect.GuardParty && k.Effect != SkillEffect.Evade;
                bool heals = k.Effect == SkillEffect.HealAlly && k.HealPct > 0f;
                if (!hurts && !heals) continue;
                double w = 1.0 / (1 + Math.Max(0, k.Cooldown));
                use.Add(new KeyValuePair<SkillSpec, double>(k, w));
                weight += w;
            }
            foreach (var kv in use)
            {
                var k = kv.Key;
                double share = kv.Value / weight;
                if (k.Effect == SkillEffect.HealAlly) { u.Heal += share * k.HealPct * partyHp * actions; continue; }
                double p = share * k.Power * Math.Max(1, k.Hits) * hit;
                if (k.AllEnemies) { if (k.Range >= 2) u.AreaAll += p; else u.AreaFront += p; }
                else if (k.Range >= 2) u.Any += p;
                else u.Front += p;
            }
            return u;
        }

        // ------------------------------------------------------------------ the fluid fight

        /// <summary>Lines of a side, front first (party: higher column = nearer the enemy; enemies: lower column).</summary>
        private static int FrontCol(List<Unit> side, bool party)
        {
            int best = party ? int.MinValue : int.MaxValue;
            foreach (var u in side) if (u.Alive) best = party ? Math.Max(best, u.Col) : Math.Min(best, u.Col);
            return best;
        }

        private static int BackCol(List<Unit> side, bool party)
        {
            int best = party ? int.MaxValue : int.MinValue;
            foreach (var u in side) if (u.Alive) best = party ? Math.Min(best, u.Col) : Math.Max(best, u.Col);
            return best;
        }

        private static Unit Focus(List<Unit> foes, int onlyCol)
        {
            Unit best = null; double bestKey = -1.0;
            foreach (var f in foes)
            {
                if (!f.Alive || (onlyCol != int.MinValue && f.Col != onlyCol)) continue;
                double key = (f.Offense + 1e-6) / f.Ehp;   // damage removed per point of work
                if (key > bestKey) { bestKey = key; best = f; }
            }
            return best;
        }

        private static void ShareOverLine(List<Unit> side, int col, double stream, Dictionary<Unit, double> rate)
        {
            if (stream <= 0) return;
            double total = 0.0;
            foreach (var u in side) if (u.Alive && u.Col == col) total += u.Ehp;
            if (total <= 0) return;
            foreach (var u in side) if (u.Alive && u.Col == col) rate[u] += stream * u.Ehp / total;
        }

        /// <summary>Plays the two sides against each other; returns our left share minus theirs (-1..1).</summary>
        private static double Simulate(List<Unit> party, List<Unit> foes)
        {
            foreach (var u in party) u.Ehp = u.Ehp0;
            foreach (var u in foes) u.Ehp = u.Ehp0;
            var rate = new Dictionary<Unit, double>();
            for (int step = 0; step < 64; step++)
            {
                bool anyParty = false, anyFoe = false;
                foreach (var u in party) anyParty |= u.Alive;
                foreach (var u in foes) anyFoe |= u.Alive;
                if (!anyParty || !anyFoe) break;
                rate.Clear();
                foreach (var u in party) rate[u] = 0.0;
                foreach (var u in foes) rate[u] = 0.0;

                // our streams
                double front = 0, any = 0, areaFront = 0, areaAll = 0, heal = 0;
                foreach (var u in party) if (u.Alive) { front += u.Front; any += u.Any; areaFront += u.AreaFront; areaAll += u.AreaAll; heal += u.Heal; }
                int foeFront = FrontCol(foes, false);
                foreach (var f in foes) if (f.Alive) { rate[f] += areaAll; if (f.Col == foeFront) rate[f] += areaFront; }
                var t1 = Focus(foes, foeFront); if (t1 != null) rate[t1] += front;
                var t2 = Focus(foes, int.MinValue); if (t2 != null) rate[t2] += any;

                // their streams (healing first takes the edge off the blows on our front line)
                double eFront = 0, eSnipe = 0;
                foreach (var f in foes) if (f.Alive) { eFront += f.Front; eSnipe += f.Snipe; }
                int ourFront = FrontCol(party, true), ourBack = BackCol(party, true);
                double frontMit = 0; int frontN = 0;
                foreach (var u in party) if (u.Alive && u.Col == ourFront) { frontMit += u.Mitigation; frontN++; }
                if (frontN > 0) eFront = Math.Max(0.0, eFront - heal / (frontMit / frontN));
                ShareOverLine(party, ourFront, eFront, rate);
                ShareOverLine(party, ourBack, eSnipe, rate);

                double dt = double.MaxValue;
                foreach (var kv in rate) if (kv.Key.Alive && kv.Value > 0) dt = Math.Min(dt, kv.Key.Ehp / kv.Value);
                if (dt == double.MaxValue) break;   // nobody can hurt anybody any more
                foreach (var kv in rate)
                {
                    if (!kv.Key.Alive || kv.Value <= 0) continue;
                    kv.Key.Ehp -= kv.Value * dt;
                    if (kv.Key.Ehp < kv.Key.Ehp0 * 1e-6) kv.Key.Ehp = 0.0;
                }
            }
            return Left(party) - Left(foes);
        }

        private static double Left(List<Unit> side)
        {
            double s = 0;
            foreach (var u in side) s += u.Ehp / u.Ehp0;
            return side.Count == 0 ? 0 : s / side.Count;
        }

        // ------------------------------------------------------------------ arrangements

        /// <summary>
        /// Every way to give n units a line (0, 1, 2) with at most three per line and no empty line in front of an occupied one
        /// (only the distance from the front line matters; <paramref name="frontLine"/> is 2 for our party, 0 for the enemies).
        /// </summary>
        private static List<int[]> Splits(int n, int frontLine)
        {
            var all = new List<int[]>();
            var cur = new int[n];
            Fill(cur, 0, new int[3], all);
            var list = new List<int[]>();
            foreach (var s in all)
            {
                var used = new bool[3];
                foreach (int line in s) used[line] = true;
                int mid = 1, back = frontLine == 0 ? 2 : 0;
                if (used[mid] && !used[frontLine]) continue;
                if (used[back] && !(used[mid] && used[frontLine])) continue;
                list.Add(s);
            }
            return list;
        }

        /// <summary>Every way to give n units a line (0, 1, 2) with at most three per line, empty lines anywhere.</summary>
        private static List<int[]> AllAssignments(int n)
        {
            var all = new List<int[]>();
            Fill(new int[n], 0, new int[3], all);
            return all;
        }

        private static void Fill(int[] cur, int i, int[] count, List<int[]> list)
        {
            if (i == cur.Length) { list.Add((int[])cur.Clone()); return; }
            for (int line = 0; line < 3; line++)
            {
                if (count[line] >= Board.Rows) continue;
                cur[i] = line; count[line]++;
                Fill(cur, i + 1, count, list);
                count[line]--;
            }
        }

        private static double PartyAvgHp(IList<Combatant> party)
        {
            double s = 0; foreach (var c in party) s += c.Stats.MaxHp;
            return party.Count == 0 ? 0 : s / party.Count;
        }

        /// <summary>The worst our lines can do against these enemies (they answer with their best lines).</summary>
        private static double WorstCase(List<Unit> party, List<Unit> foes, List<int[]> enemySplits)
        {
            double worst = double.MaxValue;
            foreach (var split in enemySplits)
            {
                for (int i = 0; i < foes.Count; i++) foes[i].Col = Board.PartyCols + split[i];
                worst = Math.Min(worst, Simulate(party, foes));
            }
            return worst;
        }

        private const int MaxEnumeratedEnemies = 5;

        private static List<int[]> EnemySplitsFor(IList<Combatant> enemies)
        {
            if (enemies.Count <= MaxEnumeratedEnemies) return Splits(enemies.Count, 0);
            // a big group: only the simple rule's lines (enumerating them all would take too long)
            var copy = new List<Combatant>();
            foreach (var e in enemies) copy.Add(new Combatant(e.Id, e.Name, Team.Enemy, e.Stats, null) { Moves = e.Moves });
            Formation.PlaceEnemies(copy);
            var split = new int[copy.Count];
            for (int i = 0; i < split.Length; i++) split[i] = copy[i].Col - Board.PartyCols;
            return new List<int[]> { split };
        }

        /// <summary>
        /// Lines (0 back .. 2 front) for our party against the fights of a stage, keyed by member id. <paramref name="fixedCols"/> pins members who
        /// already stand somewhere (a newcomer joining a saved formation); <paramref name="hint"/> is the simple rule's lines, used to break ties.
        /// </summary>
        public static Dictionary<string, int> PlanPartyLines(IList<Combatant> party, IList<IList<Combatant>> fights, IDictionary<string, int> hint, IDictionary<string, int> fixedCols = null)
        {
            var result = new Dictionary<string, int>();
            if (party.Count == 0) return result;
            double hp = PartyAvgHp(party);
            var ours = new List<Unit>();
            foreach (var c in party) ours.Add(Profile(c, false, hp));
            var theirs = new List<List<Unit>>();
            var theirSplits = new List<List<int[]>>();
            foreach (var fight in fights)
            {
                if (fight == null || fight.Count == 0) continue;
                var list = new List<Unit>();
                foreach (var e in fight) list.Add(Profile(e, true, hp));
                theirs.Add(list);
                theirSplits.Add(EnemySplitsFor(fight));
            }

            double bestScore = double.MinValue;
            int[] best = null;
            // With pinned members (they keep the tiles the player chose) every assignment is tried, gaps in front included:
            // the simulation reads the front line from whoever stands, so a lone 비류연 in the back column is still the front.
            var candidates = fixedCols != null && fixedCols.Count > 0 ? AllAssignments(party.Count) : Splits(party.Count, 2);
            foreach (var split in candidates)
            {
                bool ok = true;
                for (int i = 0; i < party.Count && ok; i++)
                {
                    int pin;
                    if (fixedCols != null && fixedCols.TryGetValue(party[i].Id, out pin) && pin != split[i]) ok = false;
                }
                if (!ok) continue;
                for (int i = 0; i < ours.Count; i++) ours[i].Col = split[i];
                double score = 0.0;
                for (int f = 0; f < theirs.Count; f++) score += WorstCase(ours, theirs[f], theirSplits[f]);
                // ties (and near ties) keep the simple rule's lines
                int same = 0;
                for (int i = 0; i < party.Count; i++) { int h; if (hint != null && hint.TryGetValue(party[i].Id, out h) && h == split[i]) same++; }
                score += 1e-4 * same;
                if (score > bestScore) { bestScore = score; best = split; }
            }
            if (best == null) return result;
            for (int i = 0; i < party.Count; i++) result[party[i].Id] = best[i];
            return result;
        }

        /// <summary>The enemies' best answer to the lines our party stands in (their columns 3..5), or null when the group is too big to search.</summary>
        public static int[] PlanEnemyCols(IList<Combatant> enemies, IList<Combatant> party)
        {
            if (enemies.Count == 0 || enemies.Count > MaxEnumeratedEnemies || party == null || party.Count == 0) return null;
            double hp = PartyAvgHp(party);
            var ours = new List<Unit>();
            foreach (var c in party) { var u = Profile(c, false, hp); u.Col = c.Col < 0 ? Board.PartyCols - 1 : c.Col; ours.Add(u); }
            var theirs = new List<Unit>();
            foreach (var e in enemies) theirs.Add(Profile(e, true, hp));
            // the simple rule's columns break ties
            var copy = new List<Combatant>();
            foreach (var e in enemies) copy.Add(new Combatant(e.Id, e.Name, Team.Enemy, e.Stats, null) { Moves = e.Moves });
            Formation.PlaceEnemies(copy);

            double bestScore = double.MaxValue;
            int[] best = null;
            foreach (var split in Splits(enemies.Count, 0))
            {
                for (int i = 0; i < theirs.Count; i++) theirs[i].Col = Board.PartyCols + split[i];
                double score = Simulate(ours, theirs);   // ours: lower is better for them
                int same = 0;
                for (int i = 0; i < enemies.Count; i++) if (copy[i].Col == Board.PartyCols + split[i]) same++;
                score -= 1e-4 * same;
                if (score < bestScore) { bestScore = score; best = split; }
            }
            if (best == null) return null;
            var cols = new int[enemies.Count];
            for (int i = 0; i < cols.Length; i++) cols[i] = Board.PartyCols + best[i];
            return cols;
        }

        /// <summary>The enemies of every fight of a stage, scaled the way the fight will scale them (difficulty, quest edge).</summary>
        public static List<IList<Combatant>> FightsOf(StageData st)
        {
            var fights = new List<IList<Combatant>>();
            if (st == null || st.Encounters == null) return fights;
            int diff = st.IsQuest || st.IsTower ? 0 : st.Difficulty;
            foreach (var group in st.Encounters)
            {
                var list = new List<Combatant>();
                for (int i = 0; i < group.Length; i++)
                {
                    var e = EarlyGameContent.FindEnemy(group[i]);
                    if (e == null) continue;
                    e = StageStars.ScaleEnemy(e, st.Number, diff);
                    if (st.IsQuest) e = Quests.Scale(st.QuestId, e);
                    list.Add(e.CreateCombatant(e.Id + "#" + i));
                }
                fights.Add(list);
            }
            return fights;
        }
    }
}
