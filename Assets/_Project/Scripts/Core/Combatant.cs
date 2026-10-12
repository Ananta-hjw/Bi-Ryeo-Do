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
            return cd <= 0 && Qi >= skill.QiCost;
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
