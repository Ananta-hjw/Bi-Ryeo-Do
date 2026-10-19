using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    public enum Team { Player, Enemy }

    public sealed class Combatant
    {
        public const int QiRegenPerTurn = 10;

        public readonly string Id;
        public readonly string Name;
        public readonly Team Team;
        public readonly CombatStats Stats;
        public readonly List<SkillSpec> Skills = new List<SkillSpec>();

        public int Hp;
        public int Qi;
        public int Gauge;

        // Interactive-battle state
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
        internal readonly Dictionary<string, int> Cooldowns = new Dictionary<string, int>();
        public readonly List<StatusEffect> Statuses = new List<StatusEffect>();   // 상태 이상 (see Statuses)

        public bool IsAlive { get { return Hp > 0; } }

        public Combatant(string id, string name, Team team, CombatStats stats, IEnumerable<SkillSpec> skills)
        {
            Id = id;
            Name = name;
            Team = team;
            Stats = stats;
            Hp = stats.MaxHp;
            Qi = stats.MaxQi;
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

        public bool Has(StatusKind kind) { foreach (var s in Statuses) if (s.Kind == kind) return true; return false; }

        /// <summary>Adds a status, or refreshes it to the longer duration when it is already there (statuses do not stack).</summary>
        public void AddStatus(StatusKind kind, int turns)
        {
            if (kind == StatusKind.None || turns <= 0) return;
            foreach (var s in Statuses) if (s.Kind == kind) { s.Turns = Math.Max(s.Turns, turns); return; }
            Statuses.Add(new StatusEffect { Kind = kind, Turns = turns });
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
