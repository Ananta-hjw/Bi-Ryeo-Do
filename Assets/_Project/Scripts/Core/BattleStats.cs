using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>One line of a unit's breakdown: a source of damage dealt (an art, the basic attack, a status it inflicted),
    /// of damage taken (who and with what) or of healing.</summary>
    public sealed class BattleStatLine
    {
        public string Key;
        public string Name;          // "기본 공격", "분뢰수", "중독 (지속 피해)", "염도 · 화령신공"
        public string ArtId;         // the skill id for its icon (null for statuses, 자해 and the like)
        public bool IsStatus;        // damage over time of a 상태 이상
        public long Amount;          // damage or healing as shown on screen (the last blow's overkill included)
        public int Uses;             // commands or moves (a multi-hit or area art counts once)
        public int Hits;             // landed hits (status ticks for a status line)
        public int Crits;
        public int MaxHit;           // the biggest single number
    }

    /// <summary>Everything one unit did and suffered in one fight.</summary>
    public sealed class UnitStats
    {
        public string Id;
        public string Name;
        public Team Team;
        public bool IsBoss;

        public long Dealt;           // to the other side: blows, joint strikes, 반격 and the damage over time of statuses it inflicted
        public long Taken;           // every HP loss shown on it: blows, damage over time, 자해, an ally's 주화입마
        public long HealDone;        // HP restored to anyone (arts, 운기조식 on itself, items used)
        public long HealTaken;
        public long FriendlyFire;    // its 주화입마 blows on its own side (not part of Dealt)
        public long StatusDamage;    // the share of Dealt that came from statuses it inflicted
        public int Kills;            // foes felled or driven off (retreats count) by its blow or its status
        public int Crits;
        public int HitsTaken;        // blows that landed on it (dodged ones excluded)
        public int Guarded;          // blows taken at reduced damage by a guard (방어·파티 방어)
        public int Dodged;           // blows that missed it entirely
        public int Reads;            // 간파: charged or awakening strikes it guarded or dodged (once per move)
        public int Inflicted;        // statuses it put on others
        public int Afflicted;        // statuses that took hold on it

        public readonly List<BattleStatLine> DealtBy = new List<BattleStatLine>();
        public readonly List<BattleStatLine> TakenFrom = new List<BattleStatLine>();
        public readonly List<BattleStatLine> HealsBy = new List<BattleStatLine>();

        /// <summary>Crit share of its landed hits on the other side (0..1).</summary>
        public float CritRate
        {
            get
            {
                int hits = 0;
                foreach (var l in DealtBy) if (!l.IsStatus) hits += l.Hits;
                return hits > 0 ? (float)Crits / hits : 0f;
            }
        }
    }

    /// <summary>
    /// Per-unit damage, healing and defence statistics of one fight, built only from the <see cref="BattleEvent"/>s the battle produced
    /// (no battle rule reads it). Numbers are the ones shown on screen: a finishing blow counts in full, overkill included, and so does
    /// a heal beyond max HP. 상태 이상 damage over time (중독·화상·출혈) is credited to whoever inflicted it; 환각·혈맥 역류 and 주화입마's
    /// own hurt are 자해, and a 주화입마 blow on one's own side is friendly fire, never damage dealt.
    /// </summary>
    public sealed class BattleStats
    {
        private readonly List<Combatant> _units = new List<Combatant>();
        private readonly Dictionary<string, UnitStats> _stats = new Dictionary<string, UnitStats>();
        private readonly List<string> _order = new List<string>();
        private readonly HashSet<string> _uses = new HashSet<string>();

        public BattleStats(IEnumerable<Combatant> party, IEnumerable<Combatant> enemies)
        {
            if (party != null) foreach (var u in party) Track(u);
            if (enemies != null) foreach (var u in enemies) Track(u);
        }

        /// <summary>Adds a unit (a member swapped in mid-fight, a summoned foe). Known ids keep their numbers.</summary>
        public void Track(Combatant unit)
        {
            if (unit == null) return;
            _units.RemoveAll(u => u.Id == unit.Id);
            _units.Add(unit);
            Of(unit.Id);
        }

        public IEnumerable<UnitStats> All { get { foreach (var id in _order) yield return _stats[id]; } }

        /// <summary>The party, most damage dealt first (ties: the order they were tracked).</summary>
        public List<UnitStats> Party { get { return Sorted(Team.Player); } }

        /// <summary>The foes, most damage dealt first.</summary>
        public List<UnitStats> Enemies { get { return Sorted(Team.Enemy); } }

        public UnitStats Get(string id)
        {
            UnitStats s;
            if (id == null || !_stats.TryGetValue(id, out s)) return null;
            Refresh(s);   // a story disguise may rename a unit mid-fight
            return s;
        }

        /// <summary>The party member who dealt the most (null when nobody dealt anything).</summary>
        public UnitStats Mvp
        {
            get
            {
                var party = Party;
                return party.Count > 0 && party[0].Dealt > 0 ? party[0] : null;
            }
        }

        public long TotalDealt(Team team)
        {
            long sum = 0;
            foreach (var s in _stats.Values) if (s.Team == team) sum += s.Dealt;
            return sum;
        }

        public long TotalTaken(Team team)
        {
            long sum = 0;
            foreach (var s in _stats.Values) if (s.Team == team) sum += s.Taken;
            return sum;
        }

        /// <summary>
        /// Records one batch of events. <paramref name="command"/> is the player's command that produced it, if any: it names
        /// skills the units do not carry (합공, the item used).
        /// </summary>
        public void Add(IList<BattleEvent> events, PlayerCommand? command = null)
        {
            if (events == null) return;
            SkillSpec skill = command.HasValue ? command.Value.Skill : null;
            ItemData item = command.HasValue ? command.Value.Item : null;
            foreach (var ev in events) AddOne(ev, skill, item);
        }

        private void AddOne(BattleEvent ev, SkillSpec skill, ItemData item)
        {
            if (ev == null) return;
            switch (ev.Kind)
            {
                case EventKind.Attack: AddAttack(ev, skill, item); break;
                case EventKind.Status: AddStatus(ev); break;
                case EventKind.Heal:
                case EventKind.Meditate:
                case EventKind.Item:
                    AddHeal(ev, skill, item); break;
            }
        }

        private void AddAttack(BattleEvent ev, SkillSpec skill, ItemData item)
        {
            var actor = Get(ev.ActorId);
            var target = Get(ev.TargetId);
            if (target == null) return;
            if (ev.Dodged)
            {
                target.Dodged++;
                if (ev.Heavy && ev.HitIndex == 0) target.Reads++;
                return;
            }
            int dmg = Math.Max(0, ev.Damage);
            target.Taken += dmg;
            target.HitsTaken++;
            if (ev.Guarded)
            {
                target.Guarded++;
                if (ev.Heavy && ev.HitIndex == 0) target.Reads++;
            }
            string skillName = SkillName(ev.ActorId, ev.SkillId, skill, item);
            bool friendly = actor != null && actor.Team == target.Team;
            string attacker = actor != null ? actor.Name : "?";
            var taken = Line(target.TakenFrom, (ev.ActorId ?? "?") + "|" + ev.SkillId, attacker + " · " + skillName, friendly ? null : ArtIdFor(ev));
            Tally(taken, ev, dmg, "t|" + target.Id);
            if (actor == null) return;
            if (friendly)
            {
                actor.FriendlyFire += dmg;
                return;
            }
            actor.Dealt += dmg;
            if (ev.IsCrit) actor.Crits++;
            if (ev.TargetDefeated) actor.Kills++;
            Tally(Line(actor.DealtBy, ev.SkillId, skillName, ArtIdFor(ev)), ev, dmg, "d|" + actor.Id);
        }

        private void AddStatus(BattleEvent ev)
        {
            var target = Get(ev.TargetId);
            if (target == null || ev.StatusCured) return;
            if (ev.Damage <= 0)
            {
                // a status took hold: ActorId put it there
                target.Afflicted++;
                var by = Get(ev.ActorId);
                if (by != null && by != target) by.Inflicted++;
                return;
            }
            int dmg = ev.Damage;
            target.Taken += dmg;
            string kind = Statuses.Name(ev.Status);
            var source = Get(ev.SourceId);
            if (source == null || source.Team == target.Team)
            {
                var self = Line(target.TakenFrom, "self|" + ev.Status, "자해 · " + kind, null);
                self.IsStatus = true;
                TallyTick(self, dmg);
                return;
            }
            var takenLine = Line(target.TakenFrom, source.Id + "|status|" + ev.Status, source.Name + " · " + kind + " (지속 피해)", null);
            takenLine.IsStatus = true;
            TallyTick(takenLine, dmg);
            source.Dealt += dmg;
            source.StatusDamage += dmg;
            if (ev.TargetDefeated) source.Kills++;
            var dealtLine = Line(source.DealtBy, "status|" + ev.Status, kind + " (지속 피해)", null);
            dealtLine.IsStatus = true;
            TallyTick(dealtLine, dmg);
        }

        private void AddHeal(BattleEvent ev, SkillSpec skill, ItemData item)
        {
            if (ev.Heal <= 0) return;
            var healer = Get(ev.ActorId);
            var target = Get(ev.TargetId);
            if (target != null) target.HealTaken += ev.Heal;
            if (healer == null) return;
            healer.HealDone += ev.Heal;
            string name = ev.Kind == EventKind.Meditate ? "운기조식" : SkillName(ev.ActorId, ev.SkillId, skill, item);
            var line = Line(healer.HealsBy, ev.Kind + "|" + ev.SkillId, name, ev.Kind == EventKind.Meditate ? "meditate" : ev.SkillId);
            line.Amount += ev.Heal;
            line.Hits++;
            if (ev.Heal > line.MaxHit) line.MaxHit = ev.Heal;
            if (_uses.Add("heal|" + ev.ActorId + "|" + ev.SkillId + "|" + ev.Turn)) line.Uses++;
        }

        private void Tally(BattleStatLine line, BattleEvent ev, int dmg, string scope)
        {
            line.Amount += dmg;
            line.Hits++;
            if (ev.IsCrit) line.Crits++;
            if (dmg > line.MaxHit) line.MaxHit = dmg;
            // one command or move is one use, however many hits or targets it had (Turn counts every action)
            if (_uses.Add(scope + "|" + line.Key + "|" + ev.ActorId + "|" + ev.Turn)) line.Uses++;
        }

        private static void TallyTick(BattleStatLine line, int dmg)
        {
            line.Amount += dmg;
            line.Hits++;
            line.Uses++;
            if (dmg > line.MaxHit) line.MaxHit = dmg;
        }

        private static string ArtIdFor(BattleEvent ev)
        {
            return ev.Joint ? "joint" : ev.SkillId;
        }

        private static BattleStatLine Line(List<BattleStatLine> lines, string key, string name, string artId)
        {
            foreach (var l in lines) if (l.Key == key) return l;
            var line = new BattleStatLine { Key = key, Name = name, ArtId = artId };
            lines.Add(line);
            return line;
        }

        private UnitStats Of(string id)
        {
            UnitStats s;
            if (_stats.TryGetValue(id, out s)) { Refresh(s); return s; }
            s = new UnitStats { Id = id };
            _stats[id] = s;
            _order.Add(id);
            Refresh(s);
            return s;
        }

        private void Refresh(UnitStats s)
        {
            var u = Unit(s.Id);
            if (u == null) return;
            s.Name = u.Name;
            s.Team = u.Team;
            s.IsBoss = u.IsBoss;
        }

        private Combatant Unit(string id)
        {
            foreach (var u in _units) if (u.Id == id) return u;
            return null;
        }

        /// <summary>What the player would call the skill: the unit's own art, the command's joint strike or item, or a fixed name.</summary>
        private string SkillName(string actorId, string skillId, SkillSpec skill, ItemData item)
        {
            if (string.IsNullOrEmpty(skillId)) return "공격";
            switch (skillId)
            {
                case "basic": return "기본 공격";
                case "counter_stance": return "반격 태세";
                case "deviation": return "주화입마";
                case "meditate": return "운기조식";
            }
            if (skill != null && skill.Id == skillId) return skill.Name;
            if (item != null && item.Id == skillId) return item.Name;
            var u = Unit(actorId);
            if (u != null) foreach (var sk in u.Skills) if (sk.Id == skillId) return sk.Name;
            if (skillId == "joint") return "합공";
            var known = EarlyGameContent.FindItem(skillId);
            if (known != null) return known.Name;
            return skillId;   // enemy moves are named by their id
        }

        private List<UnitStats> Sorted(Team team)
        {
            var list = new List<UnitStats>();
            foreach (var id in _order) if (_stats[id].Team == team) list.Add(_stats[id]);
            // stable: most dealt first, then the order they were tracked
            var indexed = new List<KeyValuePair<int, UnitStats>>();
            for (int i = 0; i < list.Count; i++) indexed.Add(new KeyValuePair<int, UnitStats>(i, list[i]));
            indexed.Sort((a, b) => b.Value.Dealt != a.Value.Dealt ? b.Value.Dealt.CompareTo(a.Value.Dealt) : a.Key.CompareTo(b.Key));
            list.Clear();
            foreach (var kv in indexed) list.Add(kv.Value);
            return list;
        }
    }
}
