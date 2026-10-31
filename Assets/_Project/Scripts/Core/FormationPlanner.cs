using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// Enumerates legal lines, shortlists by exposed survival and skill value, then forecasts the strongest candidates
    /// with the real battle engine. Forecasts use copies and independent fixed samples, never the live battle RNG.
    /// Front focus, reach, resources, cooldowns, statuses, protection, healing and awakening therefore follow combat rules.
    /// </summary>
    public static class FormationPlanner
    {
        private const int MaxEnumeratedEnemies = 5, PartyCandidates = 12, EnemyCandidates = 6;
        private const int ForecastTurns = 96, ForecastCommands = 48;
        private const int CampaignTurns = 384, CampaignCommands = 192;
        private static readonly int[] Samples = { 193, 887, 1601 };
        private static readonly int[] CampaignSamples = { 193, 887, 1601, 2007, 4099, 6151, 10721, 17453, 31349 };
        private static readonly Dictionary<string, int[]> EnemyCache = new Dictionary<string, int[]>();

        private sealed class Profile
        {
            public Combatant Unit;
            public double Ehp, Offense, Support, Value, Front, Snipe;
        }

        private sealed class Candidate
        {
            public int[] Lines;
            public double Score;
            public int Order;
        }

        private static double Crit(CombatStats s)
        {
            return 1.0 + Math.Max(0.0, Math.Min(1.0, s.CritRate)) * (Math.Max(1.0, s.CritDamage) - 1.0);
        }

        private static Profile Describe(Combatant c)
        {
            var s = c.Stats;
            var p = new Profile { Unit = c, Ehp = Math.Max(1, c.Hp) * (BattleEngine.DefenseConstant + Math.Max(0, s.EffDefense)) / BattleEngine.DefenseConstant };
            double hit = s.EffAttack / BattleEngine.AttackScale * Crit(s) * Math.Max(1, s.Speed) / 100.0;
            if (c.Team == Team.Enemy)
            {
                var moves = c.Moves;
                if (moves == null || moves.Count == 0) p.Front = hit;
                else foreach (var move in moves)
                {
                    if (move.Charge) continue;
                    double amount = hit * move.Power * Math.Max(1, move.Hits) * move.DamageFactor / moves.Count;
                    if (move.Snipe) p.Snipe += amount; else p.Front += amount;
                }
                // Later phases must matter even when the bounded forecast does not reach the final threshold.
                double phase = 1.0;
                if (c.Phases != null) foreach (var step in c.Phases) phase = Math.Max(phase, step.AtkMul);
                p.Front *= phase; p.Snipe *= phase;
                p.Offense = p.Front + p.Snipe;
            }
            else
            {
                double weight = 0.0;
                foreach (var skill in c.Skills)
                {
                    double resource = skill.QiCost <= c.Qi + InteractiveBattle.QiRegenFor(c.Stats.MaxQi) + c.QiRegenBonus ? 1.0 : .25;
                    double w = resource / (1 + Math.Max(skill.Cooldown, c.CooldownLeft(skill)));
                    if (skill.SpiritCost > c.Spirit) w *= .15;
                    weight += w;
                    if (InteractiveBattle.NeedsTarget(skill))
                        p.Offense += w * hit * skill.Power * Math.Max(1, skill.Hits) * (skill.AllEnemies ? 1.7 : 1.0);
                    else if (skill.Effect == SkillEffect.HealAlly || skill.Effect == SkillEffect.Cleanse || skill.Effect == SkillEffect.HealParty)
                        p.Support += w * skill.HealPct * s.MaxHp * Math.Max(1, s.Speed) / 100.0;
                    else if (skill.Effect == SkillEffect.GuardParty) p.Support += w * s.MaxHp * .2;
                    else if (skill.Effect == SkillEffect.Evade) p.Support += w * s.MaxHp * skill.EvadeChance * .1;
                }
                if (weight > 0) { p.Offense /= weight; p.Support /= weight; }
            }
            p.Value = Math.Max(1.0, p.Offense + p.Support);
            return p;
        }

        private static List<Profile> Profiles(IList<Combatant> group)
        {
            var result = new List<Profile>();
            foreach (var c in group) result.Add(Describe(c));
            return result;
        }

        private static void Drain(List<Profile> side, int[] lines, double[] hp, double amount, bool party, bool snipe)
        {
            while (amount > 0)
            {
                int target = -1;
                for (int i = 0; i < side.Count; i++)
                {
                    if (hp[i] <= 0) continue;
                    if (target < 0) { target = i; continue; }
                    bool nearer = party ? lines[i] > lines[target] : lines[i] < lines[target];
                    if (snipe) nearer = party ? lines[i] < lines[target] : lines[i] > lines[target];
                    double fraction = hp[i] / side[i].Ehp * side[i].Unit.Hp / Math.Max(1, side[i].Unit.Stats.MaxHp);
                    double previous = hp[target] / side[target].Ehp * side[target].Unit.Hp / Math.Max(1, side[target].Unit.Stats.MaxHp);
                    if (nearer || (lines[i] == lines[target] && fraction < previous)) target = i;
                }
                if (target < 0) return;
                double taken = Math.Min(hp[target], amount);
                hp[target] -= taken; amount -= taken;
            }
        }

        private static double Risk(List<Profile> side, int[] lines, double front, double snipe, bool party)
        {
            if (side.Count == 0) return 0;
            var hp = new double[side.Count];
            for (int i = 0; i < hp.Length; i++) hp[i] = side[i].Ehp;
            // Include the next defender after a collapse; merely hiding a healer behind one tank is not sufficient.
            for (int turn = 0; turn < 6; turn++)
            {
                Drain(side, lines, hp, front, party, false);
                Drain(side, lines, hp, snipe, party, true);
            }
            double loss = 0, values = 0, average = Math.Max(1.0, AverageValue(side));
            for (int i = 0; i < side.Count; i++)
            {
                double value = 1 + side[i].Value / average;
                loss += value * (1 - hp[i] / side[i].Ehp) + (hp[i] <= 0 ? value : 0);
                values += value;
            }
            return loss / values;
        }

        private static double AverageValue(List<Profile> side)
        {
            double result = 0;
            foreach (var p in side) result += p.Value;
            return result / Math.Max(1, side.Count);
        }

        private static double Quick(List<Profile> party, List<Profile> foes, int[] ours, int[] theirs)
        {
            double front = 0, snipe = 0, offense = 0;
            foreach (var p in foes) { front += p.Front; snipe += p.Snipe; }
            foreach (var p in party) offense += p.Offense;
            return Risk(foes, theirs, offense, 0, false) - Risk(party, ours, front, snipe, true);
        }

        private static List<int[]> Assignments(int n, int front, bool gaps = false)
        {
            var result = new List<int[]>();
            if (n > Board.Rows * Board.PartyCols) return result;
            Fill(new int[n], 0, new int[3], result, front, gaps);
            return result;
        }

        private static void Fill(int[] current, int index, int[] counts, List<int[]> result, int front, bool gaps)
        {
            if (index == current.Length)
            {
                int back = front == 0 ? 2 : 0;
                if (!gaps && ((counts[1] > 0 && counts[front] == 0) || (counts[back] > 0 && (counts[front] == 0 || counts[1] == 0)))) return;
                result.Add((int[])current.Clone());
                return;
            }
            for (int line = 0; line < 3; line++)
            {
                if (counts[line] >= Board.Rows) continue;
                current[index] = line; counts[line]++;
                Fill(current, index + 1, counts, result, front, gaps);
                counts[line]--;
            }
        }

        private static List<int[]> EnemySplits(IList<Combatant> enemies)
        {
            if (enemies.Count <= MaxEnumeratedEnemies) return Assignments(enemies.Count, 0);
            var copies = Copies(enemies);
            foreach (var copy in copies) { copy.Col = -1; copy.Row = -1; }
            Formation.PlaceEnemies(copies);
            var lines = new int[copies.Count];
            for (int i = 0; i < lines.Length; i++) lines[i] = copies[i].Col - Board.PartyCols;
            return new List<int[]> { lines };
        }

        private static void Keep(List<Candidate> list, int[] lines, double score, int maximum, int order)
        {
            list.Add(new Candidate { Lines = lines, Score = score, Order = order });
            list.Sort((a, b) => { int c = b.Score.CompareTo(a.Score); return c != 0 ? c : a.Order.CompareTo(b.Order); });
            if (list.Count > maximum) list.RemoveAt(list.Count - 1);
        }

        private static List<Combatant> Copies(IList<Combatant> source)
        {
            var result = new List<Combatant>();
            foreach (var unit in source) result.Add(unit.CopyForPlanning());
            return result;
        }

        private static void Place(List<Combatant> side, int[] lines, bool party)
        {
            if (!party)
            {
                var cols = new int[lines.Length];
                for (int i = 0; i < cols.Length; i++) cols[i] = lines[i] + Board.PartyCols;
                Formation.PlaceEnemyColumns(side, cols);
                return;
            }
            int[] counts = new int[3];
            for (int i = 0; i < side.Count; i++)
            {
                side[i].Col = lines[i] + (party ? 0 : Board.PartyCols);
                int row = counts[lines[i]]++;
                side[i].Row = row == 0 ? 1 : row == 1 ? 0 : 2;
            }
        }

        private static double SideScore(List<Combatant> side, List<Profile> profiles)
        {
            double alive = 0, hp = 0, protectedValue = 0, totalValue = 0;
            double average = Math.Max(1.0, AverageValue(profiles));
            for (int i = 0; i < side.Count; i++)
            {
                double value = 1 + profiles[i].Value / average;
                double fraction = Math.Max(0.0, (double)side[i].Hp / Math.Max(1, side[i].Stats.MaxHp));
                if (side[i].IsAlive) alive++;
                hp += fraction;
                protectedValue += value * Math.Sqrt(fraction);
                totalValue += value;
            }
            return 4 * alive / Math.Max(1, side.Count) + hp / Math.Max(1, side.Count) + 2 * protectedValue / Math.Max(1.0, totalValue);
        }

        private static double Forecast(IList<Combatant> party, IList<Combatant> enemies, int[] ours, int[] theirs,
            List<Profile> partyProfiles, List<Profile> enemyProfiles, TrialRule trial)
        {
            double score = 0;
            foreach (int seed in Samples)
            {
                var p = Copies(party); var e = Copies(enemies);
                Place(p, ours, true); Place(e, theirs, false);
                // Resolve charge references only inside the copied graph.
                for (int i = 0; i < enemies.Count; i++)
                    if (enemies[i].ChargeTarget != null)
                        foreach (var member in p) if (member.Id == enemies[i].ChargeTarget.Id) e[i].ChargeTarget = member;
                var battle = new InteractiveBattle(p, e, new SeededRandom(seed)) { Trial = trial };
                battle.Begin();
                for (int command = 0; !battle.IsOver && battle.Turn < ForecastTurns && command < ForecastCommands; command++)
                    battle.PlayerAct(ChooseTactical(battle));
                double outcome = SideScore(p, partyProfiles) - SideScore(e, enemyProfiles);
                if (battle.IsOver) outcome += battle.PlayerWon ? 3 : -3;
                score += outcome;
            }
            return score / Samples.Length;
        }

        private static double Damage(Combatant actor, Combatant target, SkillSpec skill)
        {
            double crit = Math.Max(0, Math.Min(1, actor.Stats.CritRate));
            return (BattleEngine.CalcDamage(actor.Stats, target.Stats, skill, false) * (1 - crit)
                + BattleEngine.CalcDamage(actor.Stats, target.Stats, skill, true) * crit) * Math.Max(1, skill.Hits);
        }

        // A conservative public-intent policy avoids relying on perfect target selection or future player commands.
        private static PlayerCommand Choose(InteractiveBattle battle)
        {
            var actor = battle.Current;
            Combatant winding = null, front = null, sick = null;
            bool targeted = false, wounded = false;
            foreach (var member in battle.Party)
            {
                if (!member.IsAlive) continue;
                if (front == null || battle.Depth(member) < battle.Depth(front)
                    || (battle.Depth(member) == battle.Depth(front) && (double)member.Hp / member.Stats.MaxHp < (double)front.Hp / front.Stats.MaxHp)) front = member;
                if (sick == null && (member.Has(StatusKind.Burn) || member.Has(StatusKind.Poison))) sick = member;
                wounded |= member.Hp < member.Stats.MaxHp * .55f;
            }
            foreach (var enemy in battle.Enemies)
            {
                if (!enemy.IsAlive || (!enemy.Charging && enemy.AwakeningStrike == null)) continue;
                if (winding == null) winding = enemy;
                targeted |= enemy.AwakeningStrike != null ? front == actor : enemy.ChargeTarget == actor;
            }
            if (winding != null)
            {
                foreach (var skill in actor.Skills)
                    if (skill.Effect == SkillEffect.Stun && battle.CanUse(skill) && battle.EnemiesInRange(skill.Range).Contains(winding)) return PlayerCommand.UseSkill(skill, winding);
                if (targeted)
                {
                    foreach (var effect in new[] { SkillEffect.Evade, SkillEffect.GuardParty })
                        foreach (var skill in actor.Skills)
                            if (skill.Effect == effect && battle.CanUse(skill)) return PlayerCommand.UseSkill(skill, null);
                    return PlayerCommand.Guard();
                }
            }
            foreach (var skill in actor.Skills)
                if (skill.Effect == SkillEffect.Cleanse && sick != null && battle.CanUse(skill)) return PlayerCommand.UseSkill(skill, null).WithAlly(sick);
            foreach (var skill in actor.Skills)
                if ((skill.Effect == SkillEffect.HealAlly || skill.Effect == SkillEffect.HealParty) && wounded && battle.CanUse(skill)) return PlayerCommand.UseSkill(skill, null);
            SkillSpec best = SkillSpec.BasicAttack();
            Combatant target = null;
            double score = -1;
            foreach (var skill in actor.Skills)
            {
                if (skill.Id == "basic" || skill.Effect != SkillEffect.None || !battle.CanUse(skill)) continue;
                var reachable = battle.EnemiesInRange(skill.Range);
                double value = skill.Power * Math.Max(1, skill.Hits) * (skill.AllEnemies ? reachable.Count : 1);
                if (reachable.Count == 0 || value <= score) continue;
                score = value; best = skill; target = null;
                foreach (var enemy in reachable) if (target == null || enemy.Hp < target.Hp) target = enemy;
            }
            if (target == null)
                foreach (var enemy in battle.EnemiesInRange(1)) if (target == null || enemy.Hp < target.Hp) target = enemy;
            return target == null ? PlayerCommand.Meditate() : PlayerCommand.UseSkill(best, target);
        }

        // This bounded policy observes only the current legal actions and public intents, not future rolls or commands.
        private static PlayerCommand ChooseTactical(InteractiveBattle battle)
        {
            var actor = battle.Current;
            Combatant threatened = null, wounded = null, sick = null;
            foreach (var member in battle.Party)
            {
                if (!member.IsAlive) continue;
                if (wounded == null || (double)member.Hp / member.Stats.MaxHp < (double)wounded.Hp / wounded.Stats.MaxHp) wounded = member;
                if (member.Statuses.Count > 0 && sick == null) sick = member;
                if (threatened == null || battle.Depth(member) < battle.Depth(threatened)
                    || (battle.Depth(member) == battle.Depth(threatened) && (double)member.Hp / member.Stats.MaxHp < (double)threatened.Hp / threatened.Stats.MaxHp)) threatened = member;
            }
            bool wind = false, targetsActor = false;
            Combatant winding = null;
            foreach (var enemy in battle.Enemies)
            {
                if (!enemy.IsAlive || (!enemy.Charging && enemy.AwakeningStrike == null)) continue;
                wind = true; winding = enemy;
                targetsActor |= enemy.AwakeningStrike != null ? threatened == actor : enemy.ChargeTarget == actor;
            }
            foreach (var skill in actor.Skills)
            {
                if (!battle.CanUse(skill)) continue;
                if (skill.Effect == SkillEffect.Cleanse && sick != null) return PlayerCommand.UseSkill(skill, null).WithAlly(sick);
                if ((skill.Effect == SkillEffect.HealAlly || skill.Effect == SkillEffect.HealParty) && wounded != null && wounded.Hp < wounded.Stats.MaxHp * .60f)
                    return PlayerCommand.UseSkill(skill, null).WithAlly(wounded);
                if (wind && skill.Effect == SkillEffect.Stun && battle.EnemiesInRange(skill.Range).Contains(winding)) return PlayerCommand.UseSkill(skill, winding);
                if (wind && skill.Effect == SkillEffect.GuardParty) return PlayerCommand.UseSkill(skill, null);
                if (targetsActor && skill.Effect == SkillEffect.Evade) return PlayerCommand.UseSkill(skill, null);
            }
            if (targetsActor) return PlayerCommand.Guard();
            SkillSpec best = null; Combatant target = null;
            double score = double.MinValue;
            foreach (var skill in actor.Skills)
            {
                if (!InteractiveBattle.NeedsTarget(skill) || !battle.CanUse(skill)) continue;
                var reachable = battle.EnemiesInRange(skill.Range);
                Combatant pick = null;
                double value = 0;
                foreach (var enemy in reachable)
                {
                    double damage = Damage(actor, enemy, skill);
                    double benefit = Math.Min(enemy.Hp, damage) + (damage >= enemy.Hp ? enemy.Stats.MaxHp * .3 : 0);
                    if (skill.Effect == SkillEffect.Stun && !enemy.IsBoss && !enemy.Stunned) benefit += enemy.Stats.MaxHp * .12;
                    if (skill.Inflict != StatusKind.None && enemy.Statuses.Count == 0) benefit += skill.InflictChance * enemy.Stats.MaxHp * .06;
                    if (skill.AllEnemies) value += benefit;
                    if (pick == null || benefit > (skill.AllEnemies ? 0 : value)) { pick = enemy; if (!skill.AllEnemies) value = benefit; }
                }
                value /= 1 + .15 * skill.QiCost / Math.Max(1.0, actor.Qi + InteractiveBattle.QiRegenFor(actor.Stats.MaxQi));
                if (pick != null && value > score) { score = value; best = skill; target = pick; }
            }
            if (best != null)
            {
                if (best.Id == "basic" && actor.Qi < 12 && actor.Hp > actor.Stats.MaxHp * .3)
                    foreach (var skill in actor.Skills)
                        if (skill.QiCost > actor.Qi && skill.QiCost <= actor.Stats.MaxQi && InteractiveBattle.NeedsTarget(skill)) return PlayerCommand.Meditate();
                return PlayerCommand.UseSkill(best, target);
            }
            return PlayerCommand.Meditate();
        }

        public static Dictionary<string, int> PlanPartyLines(IList<Combatant> party, IList<IList<Combatant>> fights,
            IDictionary<string, int> hint, IDictionary<string, int> fixedCols = null, IList<TrialRule> trials = null)
        {
            var result = new Dictionary<string, int>();
            if (party.Count == 0) return result;
            var p = Profiles(party);
            var enemyProfiles = new List<List<Profile>>();
            var splits = new List<List<int[]>>();
            foreach (var fight in fights) { enemyProfiles.Add(Profiles(fight)); splits.Add(EnemySplits(fight)); }
            var candidates = new List<Candidate>();
            int order = 0;
            foreach (var ours in Assignments(party.Count, 2, fixedCols != null && fixedCols.Count > 0))
            {
                bool valid = true;
                for (int i = 0; i < party.Count; i++)
                {
                    int column;
                    if (fixedCols != null && fixedCols.TryGetValue(party[i].Id, out column) && column != ours[i]) valid = false;
                }
                if (!valid) continue;
                double total = 0, worst = 0;
                for (int f = 0; f < fights.Count; f++)
                {
                    double value = double.MaxValue;
                    foreach (var theirs in splits[f]) value = Math.Min(value, Quick(p, enemyProfiles[f], ours, theirs));
                    if (value == double.MaxValue) value = 0;
                    total += value; worst = Math.Min(worst, value);
                }
                Keep(candidates, ours, total / Math.Max(1, fights.Count) + .35 * worst, PartyCandidates, order++);
            }
            // Keep diverse fallback shapes as well as the heuristic shortlist. The initial exposure estimate can
            // tie formations whose reserve defenders protect a healer very differently after the front collapses.
            foreach (var ours in Assignments(party.Count, 2, fixedCols != null && fixedCols.Count > 0))
            {
                bool valid = true, same = true;
                int maximum = 0, count = 0;
                for (int i = 0; i < party.Count; i++)
                {
                    int col;
                    if (fixedCols != null && fixedCols.TryGetValue(party[i].Id, out col) && col != ours[i]) valid = false;
                    same &= hint != null && hint.TryGetValue(party[i].Id, out col) && col == ours[i];
                    maximum = Math.Max(maximum, ours[i]);
                }
                foreach (int col in ours) if (col == maximum) count++;
                bool compressed = count == 1;
                int middle = 0;
                foreach (int col in ours)
                {
                    compressed &= col >= maximum - 1;
                    if (col == maximum - 1) middle++;
                }
                bool reserve = count == 1 && middle == 1;
                if (!valid || (!same && !compressed && !reserve) || candidates.Exists(c => Equal(c.Lines, ours))) continue;
                candidates.Add(new Candidate { Lines = ours, Order = order++ });
            }
            double best = double.MinValue;
            int[] chosen = null;
            var finalists = new List<Candidate>();
            foreach (var candidate in candidates)
                Keep(finalists, candidate.Lines, Campaign(party, fights, candidate.Lines, trials, Samples), 8, candidate.Order);
            foreach (var candidate in finalists)
            {
                double score = Campaign(party, fights, candidate.Lines, trials, CampaignSamples);
                for (int i = 0; i < party.Count; i++) { int col; if (hint != null && hint.TryGetValue(party[i].Id, out col) && col == candidate.Lines[i]) score += .001; }
                if (score > best) { best = score; chosen = candidate.Lines; }
            }
            if (chosen != null) for (int i = 0; i < party.Count; i++) result[party[i].Id] = chosen[i];
            return result;
        }

        private static bool Equal(int[] a, int[] b)
        {
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        private static double Campaign(IList<Combatant> party, IList<IList<Combatant>> fights, int[] ours, IList<TrialRule> trials, int[] samples)
        {
            double total = 0, worst = double.MaxValue;
            var originalProfiles = Profiles(party);
            var enemyProfiles = new List<List<Profile>>();
            foreach (var fight in fights) enemyProfiles.Add(Profiles(fight));
            foreach (int seed in samples)
            {
                var p = Copies(party);
                Place(p, ours, true);
                double score = 0;
                for (int f = 0; f < fights.Count; f++)
                {
                    if (fights[f].Count == 0) continue;
                    var e = Copies(fights[f]);
                    for (int i = 0; i < e.Count; i++)
                        if (fights[f][i].ChargeTarget != null)
                            foreach (var member in p) if (member.Id == fights[f][i].ChargeTarget.Id) e[i].ChargeTarget = member;
                    var columns = PlanEnemyCols(e, p);
                    if (columns == null)
                        Formation.PlaceEnemies(e);
                    else
                    {
                        var lines = new int[columns.Length];
                        for (int i = 0; i < lines.Length; i++) lines[i] = columns[i] - Board.PartyCols;
                        Place(e, lines, false);
                    }
                    var battle = new InteractiveBattle(p, e, new SeededRandom(seed + f * 7919))
                    { Trial = trials != null && f < trials.Count ? trials[f] : null };
                    battle.Begin();
                    for (int command = 0; !battle.IsOver && battle.Turn < CampaignTurns && command < CampaignCommands; command++)
                        battle.PlayerAct(Choose(battle));
                    score = f * 10.0 + SideScore(p, originalProfiles) - SideScore(e, enemyProfiles[f]);
                    if (!battle.PlayerWon) break;
                    score += 10;
                    if (f + 1 < fights.Count) foreach (var member in p) member.RestBetweenFights();
                }
                total += score;
                worst = Math.Min(worst, score);
            }
            return total / samples.Length + .25 * (worst == double.MaxValue ? 0 : worst);
        }

        public static int[] PlanEnemyCols(IList<Combatant> enemies, IList<Combatant> party)
        {
            if (enemies.Count == 0 || enemies.Count > MaxEnumeratedEnemies || party == null || party.Count == 0) return null;
            string key = Fingerprint(party, true) + "/" + Fingerprint(enemies, false);
            int[] cached;
            if (EnemyCache.TryGetValue(key, out cached)) return (int[])cached.Clone();
            var p = Profiles(party); var e = Profiles(enemies);
            var ours = new int[party.Count];
            for (int i = 0; i < ours.Length; i++) ours[i] = party[i].Col < 0 ? 2 : party[i].Col;
            var candidates = new List<Candidate>(); int order = 0;
            foreach (var theirs in Assignments(enemies.Count, 0)) Keep(candidates, theirs, -Quick(p, e, ours, theirs), EnemyCandidates, order++);
            double best = double.MaxValue; int[] chosen = null;
            foreach (var candidate in candidates)
            {
                double score = Forecast(party, enemies, ours, candidate.Lines, p, e, null) - candidate.Score * .001;
                if (score < best) { best = score; chosen = candidate.Lines; }
            }
            if (chosen == null) return null;
            var result = new int[chosen.Length];
            for (int i = 0; i < result.Length; i++) result[i] = Board.PartyCols + chosen[i];
            if (EnemyCache.Count >= 256) EnemyCache.Clear();
            EnemyCache[key] = (int[])result.Clone();
            return result;
        }

        // Include battle inputs, not just displayed attack/defense or skill ids. Definition changes invalidate cached plans.
        internal static string Fingerprint(IList<Combatant> group, bool positions)
        {
            var key = new StringBuilder();
            void Add(object value)
            {
                string text = value is float single ? single.ToString("R", CultureInfo.InvariantCulture)
                    : value is double number ? number.ToString("R", CultureInfo.InvariantCulture)
                    : value is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : value?.ToString() ?? "";
                key.Append(text.Length).Append(':').Append(text).Append('|');
            }
            void Stats(CombatStats s) { Add(s.MaxHp); Add(s.MaxQi); Add(s.Attack); Add(s.Defense); Add(s.Speed); Add(s.CritRate); Add(s.CritDamage); Add(s.AtkUnit); Add(s.DefUnit); Add(s.StartSpirit); }
            void Move(EnemyMove m) { if (m == null) { Add("none"); return; } Add(m.Name); Add(m.Power); Add(m.Hits); Add(m.Charge); Add(m.Snipe); Add(m.Awakening); Add(m.DamageFactor); Add(m.Inflict); }
            foreach (var c in group)
            {
                Add(c.Id); Add(c.Team); Stats(c.Stats); Add(c.PhaseBaseStats.HasValue); if (c.PhaseBaseStats.HasValue) Stats(c.PhaseBaseStats.Value);
                Add(c.Hp); Add(c.Qi); Add(c.Spirit); Add(c.Gauge); Add(c.StartSpirit); Add(c.QiRegenBonus); Add(c.SpiritDealtBonus); Add(c.SpiritTakenBonus); Add(c.Lifesteal);
                Add(c.Guarding); Add(c.Evading); Add(c.EvadeChance); Add(c.Stunned); Add(c.Charging); Add(c.ChargeTarget?.Id); Add(c.Rank); Add(c.IsBoss); Add(c.RetreatHpPct); Add(c.Range);
                Add(c.RescuerId); Add(c.RescueUsed); Add(c.MoveIndex); Add(c.PhaseIndex); Move(c.AwakeningStrike);
                if (positions) { Add(c.Col); Add(c.Row); }
                Add("skills"); foreach (var s in c.Skills)
                {
                    Add(s.Id); Add(s.Power); Add(s.Hits); Add(s.QiCost); Add(s.SpiritCost); Add(s.Cooldown); Add(s.Range); Add(s.AllEnemies); Add(s.Effect); Add(s.HealPct); Add(s.Inflict); Add(s.InflictChance); Add(s.EvadeChance); Add(c.CooldownLeft(s));
                }
                Add("moves"); if (c.Moves != null) foreach (var m in c.Moves) Move(m);
                Add("phases"); if (c.Phases != null) foreach (var phase in c.Phases) { Add(phase.HpPct); Add(phase.AtkMul); Add(phase.DefMul); }
                Add("statuses"); foreach (var status in c.Statuses) { Add(status.Kind); Add(status.Turns); }
                Add("end");
            }
            return key.ToString();
        }

        public static List<IList<Combatant>> FightsOf(StageData st)
        {
            var fights = new List<IList<Combatant>>();
            if (st == null || st.Encounters == null) return fights;
            for (int k = 0; k < st.Encounters.Length; k++)
            {
                var group = st.Encounters[k];
                var list = new List<Combatant>();
                for (int i = 0; i < group.Length; i++)
                {
                    var e = st.EnemyFor(group[i], st.TrialFor(k));
                    if (e != null) list.Add(e.CreateCombatant(e.Id + "#" + i));
                }
                fights.Add(list);
            }
            return fights;
        }
    }
}
