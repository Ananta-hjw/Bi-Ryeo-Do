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
        public bool Guarding;              // damage taken is reduced until this unit's next turn
        public bool Evading;               // all hits miss until this unit's next turn
        public bool Charging;              // enemy is winding up its next move (telegraph)
        public int MoveIndex;              // enemy script position
        public List<EnemyMove> Moves;      // enemy attack script, cycled in order
        internal readonly Dictionary<string, int> Cooldowns = new Dictionary<string, int>();

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

        /// <summary>Between fights of one stage: partial heal, full 내공, fresh gauge, cooldowns and 기세 cleared.</summary>
        public void RestBetweenFights(float healPct)
        {
            Hp = Math.Min(Stats.MaxHp, Hp + (int)Math.Round(Stats.MaxHp * healPct));
            Qi = Stats.MaxQi;
            Gauge = 0;
            Spirit = 0;
            Guarding = false;
            Evading = false;
            Cooldowns.Clear();
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

        internal void RegenQi()
        {
            Qi = Math.Min(Stats.MaxQi, Qi + QiRegenPerTurn);
        }
    }
}
