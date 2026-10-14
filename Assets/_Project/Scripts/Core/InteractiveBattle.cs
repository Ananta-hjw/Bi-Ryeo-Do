using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    public enum CommandKind { Skill, Guard, Meditate }

    public struct PlayerCommand
    {
        public CommandKind Kind;
        public SkillSpec Skill;      // for Kind == Skill
        public Combatant Target;     // single-target skills; null/dead falls back to the first living enemy

        public static PlayerCommand UseSkill(SkillSpec skill, Combatant target)
        {
            return new PlayerCommand { Kind = CommandKind.Skill, Skill = skill, Target = target };
        }
        public static PlayerCommand Guard() { return new PlayerCommand { Kind = CommandKind.Guard }; }
        public static PlayerCommand Meditate() { return new PlayerCommand { Kind = CommandKind.Meditate }; }
    }

    /// <summary>
    /// Player-driven turn battle for a party of 1-4 on top of the same action-gauge order as <see cref="BattleEngine"/>.
    /// Enemies act on their own scripts (<see cref="EnemyMove"/>) and telegraph their heavy moves (including who they will hit);
    /// whenever a party member's turn comes the battle pauses until <see cref="PlayerAct"/> is called for them.
    /// Pure C#, deterministic given the RNG.
    /// </summary>
    public sealed class InteractiveBattle
    {
        public const int GaugeThreshold = 1000;
        public const int MaxTurns = 600;
        public const int MaxSpirit = 100;
        public const int QiRegenPerTurn = 6;
        public const int MeditateQi = 40;
        public const float MeditateHealPct = 0.08f;
        public const float GuardDamageFactor = 0.4f;
        public const int SpiritOnDealt = 10, SpiritOnTaken = 8, SpiritOnGuard = 12;

        private readonly List<Combatant> _units = new List<Combatant>();
        private readonly List<Combatant> _party = new List<Combatant>();
        private readonly List<Combatant> _enemies = new List<Combatant>();
        private readonly IRandom _rng;

        public IReadOnlyList<Combatant> Party { get { return _party; } }
        public IReadOnlyList<Combatant> Enemies { get { return _enemies; } }
        /// <summary>The first party member (the protagonist).</summary>
        public Combatant Player { get { return _party[0]; } }
        /// <summary>The party member whose turn it is, or null while the battle is resolving or over.</summary>
        public Combatant Current { get; private set; }
        public int Turn { get; private set; }

        public InteractiveBattle(Combatant player, IEnumerable<Combatant> enemies, IRandom rng)
            : this(new[] { player }, enemies, rng)
        {
        }

        public InteractiveBattle(IEnumerable<Combatant> party, IEnumerable<Combatant> enemies, IRandom rng)
        {
            foreach (var p in party) { _units.Add(p); _party.Add(p); }
            foreach (var e in enemies) { _units.Add(e); _enemies.Add(e); }
            if (_party.Count == 0) throw new ArgumentException("Party is empty.");
            _rng = rng;
        }

        public bool AnyEnemyAlive { get { foreach (var e in _enemies) if (e.IsAlive) return true; return false; } }
        public bool AnyPartyAlive { get { foreach (var p in _party) if (p.IsAlive) return true; return false; } }

        public bool IsOver { get { return !AnyPartyAlive || !AnyEnemyAlive || Turn >= MaxTurns; } }
        public bool PlayerWon { get { return AnyPartyAlive && !AnyEnemyAlive; } }
        public bool IsPlayerTurn { get { return Current != null && !IsOver; } }

        /// <summary>Runs enemy turns until a party member may act. Call once at the start of the fight.</summary>
        public List<BattleEvent> Begin()
        {
            return RunUntilParty();
        }

        public bool CanUse(SkillSpec skill) { return IsPlayerTurn && Current.CanUse(skill); }

        public Combatant FirstLivingEnemy()
        {
            foreach (var e in _enemies) if (e.IsAlive) return e;
            return null;
        }

        /// <summary>Resolves the current member's action, then every enemy turn up to the next party turn.</summary>
        public List<BattleEvent> PlayerAct(PlayerCommand cmd)
        {
            if (!IsPlayerTurn) throw new InvalidOperationException("Not the player's turn.");
            var actor = Current;
            var events = new List<BattleEvent>();
            Current = null;

            switch (cmd.Kind)
            {
                case CommandKind.Guard:
                    actor.Guarding = true;
                    actor.Spirit = Math.Min(MaxSpirit, actor.Spirit + SpiritOnGuard);
                    events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = actor.Id, SkillId = "guard", Kind = EventKind.Guard, Message = actor.Name + "이(가) 자세를 가다듬는다.", TargetHpAfter = actor.Hp });
                    break;
                case CommandKind.Meditate:
                    {
                        int heal = (int)Math.Round(actor.Stats.MaxHp * MeditateHealPct);
                        actor.Hp = Math.Min(actor.Stats.MaxHp, actor.Hp + heal);
                        actor.Qi = Math.Min(actor.Stats.MaxQi, actor.Qi + MeditateQi);
                        events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = actor.Id, SkillId = "meditate", Kind = EventKind.Meditate, Heal = heal, TargetHpAfter = actor.Hp, Message = "운기조식. 내공 +" + MeditateQi });
                        break;
                    }
                default:
                    if (cmd.Skill == null || !actor.CanUse(cmd.Skill)) throw new InvalidOperationException("Skill unavailable.");
                    UseSkill(actor, cmd, events);
                    break;
            }

            if (IsOver) return events;
            events.AddRange(RunUntilParty());
            return events;
        }

        private void UseSkill(Combatant actor, PlayerCommand cmd, List<BattleEvent> events)
        {
            var skill = cmd.Skill;
            actor.Qi -= skill.QiCost;
            actor.Spirit -= skill.SpiritCost;
            if (skill.Cooldown > 0) actor.Cooldowns[skill.Id] = skill.Cooldown;

            switch (skill.Effect)
            {
                case SkillEffect.Evade:
                    actor.Evading = true;
                    events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = actor.Id, SkillId = skill.Id, Kind = EventKind.Evade, TargetHpAfter = actor.Hp, Message = actor.Name + "의 " + skill.Name + "!" });
                    return;
                case SkillEffect.GuardParty:
                    foreach (var m in _party) if (m.IsAlive) m.Guarding = true;
                    events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = actor.Id, SkillId = skill.Id, Kind = EventKind.Guard, TargetHpAfter = actor.Hp, Message = actor.Name + "의 " + skill.Name + "! 모두가 방어 태세." });
                    return;
                case SkillEffect.HealAlly:
                    {
                        var weakest = MostWounded();
                        int heal = (int)Math.Round(weakest.Stats.MaxHp * skill.HealPct);
                        weakest.Hp = Math.Min(weakest.Stats.MaxHp, weakest.Hp + heal);
                        events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = weakest.Id, SkillId = skill.Id, Kind = EventKind.Heal, Heal = heal, TargetHpAfter = weakest.Hp, Message = actor.Name + "의 " + skill.Name + "! " + weakest.Name + " 회복." });
                        return;
                    }
            }

            var targets = new List<Combatant>();
            if (skill.AllEnemies) { foreach (var e in _enemies) if (e.IsAlive) targets.Add(e); }
            else
            {
                var t = cmd.Target != null && cmd.Target.IsAlive && cmd.Target.Team == Team.Enemy ? cmd.Target : FirstLivingEnemy();
                if (t != null) targets.Add(t);
            }

            int hits = Math.Max(1, skill.Hits);
            for (int h = 0; h < hits; h++)
            {
                foreach (var t in targets)
                {
                    if (!t.IsAlive) continue;
                    bool crit = _rng.NextDouble() < actor.Stats.CritRate;
                    int dmg = BattleEngine.CalcDamage(actor.Stats, t.Stats, skill, crit);
                    t.Hp = Math.Max(0, t.Hp - dmg);
                    var ev = new BattleEvent
                    {
                        Turn = Turn, ActorId = actor.Id, TargetId = t.Id, SkillId = skill.Id, Kind = EventKind.Attack,
                        Damage = dmg, IsCrit = crit, HitIndex = h, HitCount = hits
                    };
                    if (!t.IsAlive) { t.Charging = false; t.ChargeTarget = null; }
                    else if (skill.Effect == SkillEffect.Stun && h == hits - 1) ApplyStun(t, ev);
                    ev.TargetHpAfter = t.Hp;
                    ev.TargetDefeated = !t.IsAlive;
                    events.Add(ev);
                }
            }
            if (skill.SpiritCost == 0) actor.Spirit = Math.Min(MaxSpirit, actor.Spirit + SpiritOnDealt);
        }

        private void ApplyStun(Combatant target, BattleEvent ev)
        {
            if (target.Charging)
            {
                // Interrupt the wind-up: the heavy move that follows is skipped.
                target.Charging = false;
                target.ChargeTarget = null;
                target.MoveIndex++;
                ev.Interrupted = true;
            }
            if (!target.IsBoss)
            {
                target.Stunned = true;
                ev.Stunned = true;
            }
        }

        private Combatant MostWounded()
        {
            Combatant best = null;
            foreach (var m in _party)
            {
                if (!m.IsAlive) continue;
                if (best == null || (float)m.Hp / m.Stats.MaxHp < (float)best.Hp / best.Stats.MaxHp) best = m;
            }
            return best;
        }

        private List<BattleEvent> RunUntilParty()
        {
            var events = new List<BattleEvent>();
            while (!IsOver)
            {
                var actor = NextActor();
                actor.Gauge -= GaugeThreshold;
                Turn++;
                actor.Guarding = false;
                actor.Evading = false;

                if (actor.Team == Team.Player)
                {
                    actor.TickCooldowns();
                    actor.Qi = Math.Min(actor.Stats.MaxQi, actor.Qi + QiRegenPerTurn);
                    Current = actor;
                    return events;
                }
                EnemyAct(actor, events);
            }
            Current = null;
            return events;
        }

        private Combatant RandomLivingMember()
        {
            var alive = new List<Combatant>();
            foreach (var m in _party) if (m.IsAlive) alive.Add(m);
            if (alive.Count == 0) return null;
            int i = (int)(_rng.NextDouble() * alive.Count);
            return alive[Math.Min(i, alive.Count - 1)];
        }

        private void EnemyAct(Combatant enemy, List<BattleEvent> events)
        {
            if (enemy.Stunned)
            {
                enemy.Stunned = false;
                events.Add(new BattleEvent { Turn = Turn, ActorId = enemy.Id, TargetId = enemy.Id, SkillId = "stun", Kind = EventKind.StunSkip, TargetHpAfter = enemy.Hp, Message = enemy.Name + "은(는) 몸이 굳어 움직이지 못한다!" });
                return;
            }

            var move = enemy.PeekMove();
            enemy.MoveIndex++;

            if (move.Charge)
            {
                enemy.Charging = true;
                enemy.ChargeTarget = RandomLivingMember();
                string who = enemy.ChargeTarget != null ? " → " + enemy.ChargeTarget.Name : "";
                events.Add(new BattleEvent { Turn = Turn, ActorId = enemy.Id, TargetId = enemy.ChargeTarget != null ? enemy.ChargeTarget.Id : enemy.Id, SkillId = move.Name, Kind = EventKind.Charge, TargetHpAfter = enemy.Hp, Message = enemy.Name + "이(가) " + move.Name + "!" + who });
                return;
            }

            var target = enemy.Charging && enemy.ChargeTarget != null && enemy.ChargeTarget.IsAlive ? enemy.ChargeTarget : RandomLivingMember();
            enemy.Charging = false;
            enemy.ChargeTarget = null;
            if (target == null) return;

            var spec = new SkillSpec { Id = move.Name, Name = move.Name, Power = move.Power };
            int hits = Math.Max(1, move.Hits);
            bool hurt = false;
            for (int h = 0; h < hits && target.IsAlive; h++)
            {
                var ev = new BattleEvent
                {
                    Turn = Turn, ActorId = enemy.Id, TargetId = target.Id, SkillId = move.Name, Kind = EventKind.Attack,
                    HitIndex = h, HitCount = hits
                };
                if (target.Evading)
                {
                    ev.Dodged = true;
                    ev.TargetHpAfter = target.Hp;
                }
                else
                {
                    ev.IsCrit = _rng.NextDouble() < enemy.Stats.CritRate;
                    int dmg = BattleEngine.CalcDamage(enemy.Stats, target.Stats, spec, ev.IsCrit);
                    if (target.Guarding) { dmg = Math.Max(1, (int)Math.Round(dmg * GuardDamageFactor)); ev.Guarded = true; }
                    target.Hp = Math.Max(0, target.Hp - dmg);
                    ev.Damage = dmg;
                    ev.TargetHpAfter = target.Hp;
                    ev.TargetDefeated = !target.IsAlive;
                    hurt = true;
                }
                events.Add(ev);
            }
            if (hurt) target.Spirit = Math.Min(MaxSpirit, target.Spirit + SpiritOnTaken);
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

        /// <summary>Who acts next, in order (the first entry is the member acting right now on a party turn). Does not change state.</summary>
        public List<Combatant> PreviewOrder(int count)
        {
            var order = new List<Combatant>();
            var gauges = new int[_units.Count];
            for (int i = 0; i < _units.Count; i++) gauges[i] = _units[i].Gauge;
            if (IsPlayerTurn) order.Add(Current);

            int guard = 0;
            while (order.Count < count && !IsOver && guard++ < 3000)
            {
                int best = -1;
                for (int i = 0; i < _units.Count; i++)
                {
                    if (!_units[i].IsAlive || gauges[i] < GaugeThreshold) continue;
                    if (best < 0 || gauges[i] > gauges[best]) best = i;
                }
                if (best < 0)
                {
                    for (int i = 0; i < _units.Count; i++)
                        if (_units[i].IsAlive) gauges[i] += Math.Max(1, _units[i].Stats.Speed);
                    continue;
                }
                gauges[best] -= GaugeThreshold;
                order.Add(_units[best]);
            }
            return order;
        }
    }
}
