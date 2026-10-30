using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    public enum Team { Player, Enemy }

    public sealed class Combatant
    {
        public const int QiRegenPerTurn = 10;

        public readonly string Id;
        public string Name { get; internal set; }   // a story disguise changes presentation, never the growth Id
        public string StoryArtId { get; set; }   // a story figure override; settable so balance tests (another assembly) can copy it
        public readonly Team Team;
        public CombatStats Stats { get { return _stats; } }
        public readonly double RealmPowerBeforeBonus;
        public readonly int RealmStep;
        private CombatStats _stats;
        private CombatStats? _baseStats;   // the stats before a boss phase changed them
        public readonly List<SkillSpec> Skills = new List<SkillSpec>();

        public int Hp;
        public int Qi;
        public int Gauge;

        // Interactive-battle state
        public bool StoryControlled;   // fixed story perspective; non-hero leads cannot use hero-only commands
        public int Spirit;                 // 기세 0..100, builds by fighting; 비기 spend it
        // 심법 perks (protagonist only, see InnerArts)
        public int QiRegenBonus, SpiritDealtBonus, SpiritTakenBonus, StartSpirit;
        public float Lifesteal;            // share of dealt damage healed
        public string RescuerId;           // 의기투합 companion who can save this unit once per fight (protagonist only)
        public bool RescueUsed;
        public int Col = -1, Row = -1;     // tile on the battle board (see Board); -1 = not placed yet
        public int Range = 1;              // enemies: how many tiles away their attacks reach
        public bool Guarding;              // damage taken is reduced until this unit's next turn
        public bool Evading;               // hits can miss until this unit's next turn
        public float EvadeChance = 1f;     // chance each hit misses while Evading
        public bool Charging;              // enemy is winding up its next move (telegraph)
        public Combatant ChargeTarget;     // who the wound-up move will hit
        public bool Stunned;               // loses its next action
        public EnemyRank Rank;             // enemies: 일반 / 정예 / 보스 (정예 and 보스 use 내공 and 기세)
        public bool IsBoss;                // bosses cannot be stun-locked, only interrupted
        public float RetreatHpPct;         // >0: a story boss that flees (leaves the fight) once its HP drops to this fraction of max
        public int MoveIndex;              // enemy script position
        public List<EnemyMove> Moves;      // enemy attack script, cycled in order
        public List<BossPhase> Phases;     // story bosses: 단계 전환 still to come, in order (see BossPhases)
        public List<GearLevel> Gear;       // enemies: what they wear (shown in the info window); party members read their save instead
        public int PhaseIndex;             // how many phases have begun
        public EnemyMove AwakeningStrike;   // phase strike that runs once before the unchanged normal script
        internal readonly Dictionary<string, int> Cooldowns = new Dictionary<string, int>();
        public readonly List<StatusEffect> Statuses = new List<StatusEffect>();   // 상태 이상 (see Statuses)

        public bool IsAlive { get { return Hp > 0; } }

        /// <summary>A private-state copy for formation forecasts; skill/move definitions are read-only in battle.</summary>
        internal Combatant CopyForPlanning()
        {
            var copy = new Combatant(Id, Name, Team, _stats, null)
            {
                _baseStats = _baseStats, Hp = Hp, Qi = Qi, Gauge = Gauge, Spirit = Spirit, StoryControlled = StoryControlled,
                QiRegenBonus = QiRegenBonus, SpiritDealtBonus = SpiritDealtBonus, SpiritTakenBonus = SpiritTakenBonus,
                StoryArtId = StoryArtId, StartSpirit = StartSpirit, Lifesteal = Lifesteal, RescuerId = RescuerId, RescueUsed = RescueUsed,
                Col = Col, Row = Row, Range = Range, Guarding = Guarding, Evading = Evading, EvadeChance = EvadeChance,
                Charging = Charging, Stunned = Stunned, Rank = Rank, IsBoss = IsBoss, RetreatHpPct = RetreatHpPct,
                MoveIndex = MoveIndex, Moves = Moves, Phases = Phases, PhaseIndex = PhaseIndex, AwakeningStrike = AwakeningStrike
            };
            copy.Skills.Clear();
            copy.Skills.AddRange(Skills);
            foreach (var entry in Cooldowns) copy.Cooldowns.Add(entry.Key, entry.Value);
            foreach (var status in Statuses) copy.Statuses.Add(new StatusEffect { Kind = status.Kind, Turns = status.Turns });
            return copy;
        }

        internal CombatStats? PhaseBaseStats { get { return _baseStats; } }

        public Combatant(string id, string name, Team team, CombatStats stats, IEnumerable<SkillSpec> skills, bool applyRealmBonus = false)
        {
            Id = id;
            Name = name;
            Team = team;
            // Factories opt in with unboosted stats; snapshots/clones keep the supplied values unchanged.
            RealmPowerBeforeBonus = Realm.Power(stats);
            RealmStep = Realm.StepOf(RealmPowerBeforeBonus);
            _stats = applyRealmBonus ? Realm.ApplyBonus(stats, RealmStep) : stats;
            Hp = _stats.MaxHp;
            Qi = _stats.MaxQi;
            StartSpirit = _stats.StartSpirit;
            Skills.Add(SkillSpec.BasicAttack());
            if (skills != null) Skills.AddRange(skills);
        }

        public bool CanUse(SkillSpec skill)
        {
            int cd;
            Cooldowns.TryGetValue(skill.Id, out cd);
            return cd <= 0 && Qi >= skill.QiCost && Spirit >= skill.SpiritCost;
        }

        public const float RestHealPct = 0.30f;   // share of max HP regained between fights of one stage
        public const float RestQiPct = 0.50f;     // share of max 내공 regained between fights

        /// <summary>Between fights of one stage: partial heal (<see cref="RestHealPct"/> plus any bonus), half the 내공 back, fresh gauge and cleared cooldowns. 기세 is kept for the whole stage so a 비기 can be saved up for the boss.</summary>
        public void RestBetweenFights(float healPct = RestHealPct)
        {
            Hp = Math.Min(Stats.MaxHp, Hp + (int)Math.Round(Stats.MaxHp * healPct));
            Qi = Math.Min(Stats.MaxQi, Qi + (int)Math.Round(Stats.MaxQi * RestQiPct));
            Gauge = 0;
            Guarding = false;
            Evading = false;
            Stunned = false;
            Statuses.Clear();
            Cooldowns.Clear();
        }

        /// <summary>보스 단계: attack and defense become these multiples of what the unit started the fight with (not compounded phase on phase).</summary>
        public void ScaleAttackDefense(float attack, float defense)
        {
            if (_baseStats == null) _baseStats = _stats;
            var s = _baseStats.Value;
            s.Attack = Math.Max(1, (int)Math.Round(s.Attack * attack));
            s.Defense = Math.Max(1, (int)Math.Round(s.Defense * defense));
            _stats = s;
        }

        public bool Has(StatusKind kind) { foreach (var s in Statuses) if (s.Kind == kind) return true; return false; }

        /// <summary>
        /// Gives the unit a status. A unit carries one status at a time: while it has one (even the same kind), nothing new takes hold
        /// and the current one is not lengthened, so it always runs out. Returns false when it did not take.
        /// </summary>
        public bool AddStatus(StatusKind kind, int turns)
        {
            if (kind == StatusKind.None || turns <= 0 || Statuses.Count > 0) return false;
            Statuses.Add(new StatusEffect { Kind = kind, Turns = turns });
            return true;
        }

        public bool RemoveStatus(StatusKind kind)
        {
            for (int i = 0; i < Statuses.Count; i++) if (Statuses[i].Kind == kind) { Statuses.RemoveAt(i); return true; }
            return false;
        }

        public int CooldownLeft(SkillSpec skill)
        {
            int cd;
            Cooldowns.TryGetValue(skill.Id, out cd);
            return cd;
        }

        /// <summary>The move this enemy will make on its next turn (for the intent display).</summary>
        public EnemyMove PeekMove()
        {
            if (AwakeningStrike != null) return AwakeningStrike;
            if (Moves == null || Moves.Count == 0) return EnemyMove.Plain;
            return Moves[MoveIndex % Moves.Count];
        }

        internal void TickCooldowns()
        {
            var keys = new List<string>(Cooldowns.Keys);
            foreach (var k in keys)
                if (Cooldowns[k] > 0) Cooldowns[k]--;
        }

        /// <summary>정예 and 보스 enemies spend 내공 and build 기세; 일반 ones do not.</summary>
        public bool UsesResources { get { return Team == Team.Enemy && Rank != EnemyRank.Normal; } }

        internal void RegenQi()
        {
            Qi = Math.Min(Stats.MaxQi, Qi + QiRegenPerTurn);
        }
    }
}
