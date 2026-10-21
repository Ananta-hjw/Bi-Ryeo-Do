using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// Turn-based battle using an action gauge: every tick each living unit gains Speed;
    /// the first to reach <see cref="GaugeThreshold"/> acts. Pure C#, deterministic given the RNG.
    /// </summary>
    public sealed class BattleEngine
    {
        public const int GaugeThreshold = 1000;
        public const int MaxTurns = 300;

        private readonly List<Combatant> _units = new List<Combatant>();
        private readonly IRandom _rng;
        private int _turn;

        public IReadOnlyList<Combatant> Units { get { return _units; } }

        public BattleEngine(IEnumerable<Combatant> players, IEnumerable<Combatant> enemies, IRandom rng)
        {
            _units.AddRange(players);
            _units.AddRange(enemies);
            _rng = rng;
        }

        public bool IsFinished
        {
            get { return !AnyAlive(Team.Player) || !AnyAlive(Team.Enemy) || _turn >= MaxTurns; }
        }

        public BattleResult Run()
        {
            var result = new BattleResult();
            while (!IsFinished)
            {
                var e = Step();
                if (e != null) result.Events.Add(e);
            }
            result.Turns = _turn;
            if (!AnyAlive(Team.Enemy)) result.Winner = Team.Player;
            else if (!AnyAlive(Team.Player)) result.Winner = Team.Enemy;
            return result;
        }

        /// <summary>Advances to the next action and performs it. Returns null if the battle is already over.</summary>
        public BattleEvent Step()
        {
            if (IsFinished) return null;

            Combatant actor = NextActor();
            _turn++;
            actor.Gauge -= GaugeThreshold;
            actor.TickCooldowns();
            actor.RegenQi();

            Combatant target = PickTarget(actor);
            SkillSpec skill = PickSkill(actor);

            var ev = new BattleEvent { Turn = _turn, ActorId = actor.Id, TargetId = target.Id, SkillId = skill.Id, Ranged = skill.Ranged, AllEnemies = skill.AllEnemies };

            actor.Qi -= skill.QiCost;
            if (skill.Cooldown > 0) actor.Cooldowns[skill.Id] = skill.Cooldown;

            ev.IsCrit = _rng.NextDouble() < actor.Stats.CritRate;
            ev.Damage = CalcDamage(actor.Stats, target.Stats, skill, ev.IsCrit);
            target.Hp = Math.Max(0, target.Hp - ev.Damage);
            ev.TargetHpAfter = target.Hp;
            ev.TargetDefeated = !target.IsAlive;
            return ev;
        }

        /// <summary>
        /// How the stat numbers are scaled for display: attack runs <see cref="AttackScale"/> times larger and defense <see cref="DefenseScale"/> times smaller
        /// than the original curve (10 + 3 per level attack, 50 + 20 per level defense), so a character's attack and defense read at a similar size.
        /// The damage formula undoes both, so fights play out exactly as before.
        /// </summary>
        public const double AttackScale = 3.0, DefenseScale = 3.0;

        /// <summary>Damage taken is x K/(K+Defense): defense 460 = x0.42 (1380 on the original scale).</summary>
        public const double DefenseConstant = 1000.0 / DefenseScale;

        public static int CalcDamage(CombatStats attacker, CombatStats defender, SkillSpec skill, bool crit)
        {
            double raw = attacker.EffAttack * skill.Power / AttackScale;
            if (crit) raw *= Math.Max(1f, attacker.CritDamage);
            double reduced = raw * DefenseConstant / (DefenseConstant + Math.Max(0.0, defender.EffDefense));
            return Math.Max(1, (int)Math.Round(reduced));
        }

        private Combatant NextActor()
        {
            while (true)
            {
                Combatant best = null;
                foreach (var u in _units)
                {
                    if (!u.IsAlive || u.Gauge < GaugeThreshold) continue;
                    if (best == null || u.Gauge > best.Gauge) best = u;
                }
                if (best != null) return best;

                foreach (var u in _units)
                    if (u.IsAlive) u.Gauge += Math.Max(1, u.Stats.Speed);
            }
        }

        private SkillSpec PickSkill(Combatant actor)
        {
            SkillSpec best = null;
            foreach (var s in actor.Skills)
            {
                if (!actor.CanUse(s)) continue;
                if (best == null || s.Power > best.Power) best = s;
            }
            return best ?? actor.Skills[0];
        }

        private Combatant PickTarget(Combatant actor)
        {
            Combatant best = null;
            foreach (var u in _units)
            {
                if (!u.IsAlive || u.Team == actor.Team) continue;
                if (best == null || u.Hp < best.Hp) best = u;
            }
            return best;
        }

        private bool AnyAlive(Team team)
        {
            foreach (var u in _units)
                if (u.Team == team && u.IsAlive) return true;
            return false;
        }
    }
}
