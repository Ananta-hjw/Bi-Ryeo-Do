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
        public bool Stealthed;             // out of the enemies' single-target picks until this unit's next turn (신은술)
        public bool StealthCrit;           // the next attack is a sure critical hit (신은술)
        public bool Covering;              // draws every single-target enemy attack until this unit's next turn, at reduced damage (원의 곤)
        public bool CounterStance;         // strikes back at whoever hits it until this unit's next turn (성막밀밀)
        public int WardTurns;              // own turns left with no 상태 이상 taking hold (은하밀밀)
        public bool Focused;               // the next attack deals more, and this unit takes more until then (유성보)
        public float EvadeChance = 1f;     // chance each hit misses while Evading
        public float ShellFactor = 1f;     // 영수 등껍질: the next hit that lands is multiplied by this (then back to 1)
        public float DodgeBonus;           // 영수 학의 춤: chance each hit misses until this unit's next turn
        // 사부의 시련 가르침 (MasterTrial; neutral by default, so every other fight is unchanged)
        public float DamageDealtMul = 1f, DamageTakenMul = 1f;
        public float VsAfflictedBonus;     // extra share of damage on a target carrying a 상태 이상
        public float HealTakenBonus;       // extra share of every heal this unit receives
        public int MeditateSpiritBonus;    // 기세 gained when meditating
        public float GuardStrikeBonus;     // after a guard, the next attack deals this much more
        public bool GuardStrikeReady;
        public int JointCostCut;           // 기세 off the 합공 cost
        // 기경팔맥 완성 효과 (Meridians; neutral by default)
        public float GuardCut;             // taken off the guard damage factor (0.4 → 0.35)
        public float MeditateHealBonus;    // extra share of max HP healed by 운기
        public int StartGauge;             // action gauge at the start of a fight
        public float StatusResist;         // share taken off the chance of a 상태 이상 taking hold
        public int StatusTurnCut;          // turns taken off every 상태 이상 that takes hold (at least 1 remains)
        public float UltimateDamageBonus;  // extra damage of 비기 (기세 100)
        public bool Charging;              // enemy is winding up its next move (telegraph)
        public Combatant ChargeTarget;     // who the wound-up move will hit
        public bool Stunned;               // loses its next action
        public string StunReason;          // "빙결" or "점혈": shown when the turn is lost
        public int ChillHits;              // 한기: attacks taken since it took hold (ChillFreezeHits freeze the unit)
        public float MovePowerFactor = 1f;   // enemies: every move's power by story rank (Statuses.BossMovePower …)
        public int StatusTier;             // 0 = ordinary, 1 = 강력한 보스 (status chance x0.5), 2 = 정말 강력한 보스 / 시험 wall (x0.2)
        public int CleansesLeft;           // enemies: times this fight it may spend a turn to shake off a status (운기 해독)
        public float CleanseChance;        // chance per afflicted turn of doing so
        public EnemyRank Rank;             // enemies: 일반 / 정예 / 보스 (정예 and 보스 use 내공 and 기세)
        public bool IsBoss;                // bosses cannot be stun-locked, only interrupted
        public float RetreatHpPct;         // >0: a story boss that flees (leaves the fight) once its HP drops to this fraction of max
        public bool HoldsAtOneHp;          // a story boss that leaves instead of falling below 1 HP
        public string[] RetreatLines;      // what it says as it leaves ("화자|대사")
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
                Stealthed = Stealthed, StealthCrit = StealthCrit, Covering = Covering, CounterStance = CounterStance, WardTurns = WardTurns, Focused = Focused, ShellFactor = ShellFactor, DodgeBonus = DodgeBonus, DamageDealtMul = DamageDealtMul, DamageTakenMul = DamageTakenMul, VsAfflictedBonus = VsAfflictedBonus, HealTakenBonus = HealTakenBonus, MeditateSpiritBonus = MeditateSpiritBonus, GuardStrikeBonus = GuardStrikeBonus, GuardStrikeReady = GuardStrikeReady, JointCostCut = JointCostCut, GuardCut = GuardCut, MeditateHealBonus = MeditateHealBonus, StartGauge = StartGauge, StatusResist = StatusResist, StatusTurnCut = StatusTurnCut, UltimateDamageBonus = UltimateDamageBonus,
                Charging = Charging, Stunned = Stunned, StunReason = StunReason, ChillHits = ChillHits, MovePowerFactor = MovePowerFactor, StatusTier = StatusTier, CleansesLeft = CleansesLeft, CleanseChance = CleanseChance, Rank = Rank, IsBoss = IsBoss, RetreatHpPct = RetreatHpPct, HoldsAtOneHp = HoldsAtOneHp, RetreatLines = RetreatLines,
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
            if (Has(StatusKind.Seal) && skill.Id != "basic") return false;   // 봉혈: only the basic attack
            return cd <= 0 && Qi >= QiCostOf(skill) && Spirit >= skill.SpiritCost;
        }

        /// <summary>내공 the art costs this unit now (내공 역류 doubles it).</summary>
        public int QiCostOf(SkillSpec skill)
        {
            return Has(StatusKind.QiBackflow) ? (int)Math.Round(skill.QiCost * BiRyeoDo.Core.Statuses.QiBackflowFactor) : skill.QiCost;
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
            ShellFactor = 1f;
            DodgeBonus = 0f;
            Statuses.Clear();
            Cooldowns.Clear();
        }

        /// <summary>사부의 시련: replaces the stats for the run's 가르침, keeping the share of HP and 내공 the unit had.</summary>
        public void ReplaceStats(CombatStats stats)
        {
            float hp = _stats.MaxHp > 0 ? (float)Hp / _stats.MaxHp : 1f, qi = _stats.MaxQi > 0 ? (float)Qi / _stats.MaxQi : 1f;
            _stats = stats;
            Hp = Math.Max(IsAlive ? 1 : 0, (int)Math.Round(stats.MaxHp * hp));
            Qi = (int)Math.Round(stats.MaxQi * qi);
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

        /// <summary>Who inflicted the unit's status of this kind (null when it carries none or nobody is known).</summary>
        public string StatusSource(StatusKind kind) { foreach (var s in Statuses) if (s.Kind == kind) return s.SourceId; return null; }

        /// <summary>
        /// Gives the unit a status. A unit carries one status at a time: while it has one (even the same kind), nothing new takes hold
        /// and the current one is not lengthened, so it always runs out. Returns false when it did not take.
        /// </summary>
        public bool AddStatus(StatusKind kind, int turns, string sourceId = null)
        {
            if (kind == StatusKind.None || turns <= 0 || Statuses.Count > 0) return false;
            Statuses.Add(new StatusEffect { Kind = kind, Turns = turns, SourceId = sourceId });
            if (kind == StatusKind.Chill) ChillHits = 0;   // the blow that chilled the unit is not counted
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
