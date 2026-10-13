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
    /// Player-driven turn battle on top of the same action-gauge order as <see cref="BattleEngine"/>.
    /// Enemies act on their own scripts (<see cref="EnemyMove"/>) and telegraph their heavy moves; whenever it is the
    /// player's turn the battle pauses until <see cref="PlayerAct"/> is called. Pure C#, deterministic given the RNG.
    /// </summary>
    public sealed class InteractiveBattle
    {
        public const int GaugeThreshold = 1000;
        public const int MaxTurns = 400;
        public const int MaxSpirit = 100;
        public const int QiRegenPerTurn = 6;
        public const int MeditateQi = 40;
        public const float MeditateHealPct = 0.08f;
        public const float GuardDamageFactor = 0.4f;
        public const int SpiritOnDealt = 10, SpiritOnTaken = 8, SpiritOnGuard = 12;

        private readonly List<Combatant> _units = new List<Combatant>();
        private readonly List<Combatant> _enemies = new List<Combatant>();
        private readonly IRandom _rng;

        public Combatant Player { get; private set; }
        public IReadOnlyList<Combatant> Enemies { get { return _enemies; } }
        public int Turn { get; private set; }

        private bool _playerTurn;

        public InteractiveBattle(Combatant player, IEnumerable<Combatant> enemies, IRandom rng)
        {
            Player = player;
            _units.Add(player);
            foreach (var e in enemies) { _units.Add(e); _enemies.Add(e); }
            _rng = rng;
        }

        public bool AnyEnemyAlive
        {
            get { foreach (var e in _enemies) if (e.IsAlive) return true; return false; }
        }

        public bool IsOver { get { return !Player.IsAlive || !AnyEnemyAlive || Turn >= MaxTurns; } }
        public bool PlayerWon { get { return Player.IsAlive && !AnyEnemyAlive; } }
        public bool IsPlayerTurn { get { return _playerTurn && !IsOver; } }

        /// <summary>Runs enemy turns until the player may act. Call once at the start of the fight.</summary>
        public List<BattleEvent> Begin()
        {
            return RunUntilPlayer();
        }

        public bool CanUse(SkillSpec skill) { return IsPlayerTurn && Player.CanUse(skill); }

        public Combatant FirstLivingEnemy()
        {
            foreach (var e in _enemies) if (e.IsAlive) return e;
            return null;
        }

        /// <summary>Resolves the player's action, then every enemy turn up to the player's next one.</summary>
        public List<BattleEvent> PlayerAct(PlayerCommand cmd)
        {
            if (!IsPlayerTurn) throw new InvalidOperationException("Not the player's turn.");
            var events = new List<BattleEvent>();
            _playerTurn = false;

            switch (cmd.Kind)
            {
                case CommandKind.Guard:
                    Player.Guarding = true;
                    Player.Spirit = Math.Min(MaxSpirit, Player.Spirit + SpiritOnGuard);
                    events.Add(new BattleEvent { Turn = Turn, ActorId = Player.Id, TargetId = Player.Id, SkillId = "guard", Kind = EventKind.Guard, Message = Player.Name + "이(가) 자세를 가다듬는다.", TargetHpAfter = Player.Hp });
                    break;
                case CommandKind.Meditate:
                    {
                        int heal = (int)Math.Round(Player.Stats.MaxHp * MeditateHealPct);
                        Player.Hp = Math.Min(Player.Stats.MaxHp, Player.Hp + heal);
                        Player.Qi = Math.Min(Player.Stats.MaxQi, Player.Qi + MeditateQi);
                        events.Add(new BattleEvent { Turn = Turn, ActorId = Player.Id, TargetId = Player.Id, SkillId = "meditate", Kind = EventKind.Meditate, Heal = heal, TargetHpAfter = Player.Hp, Message = "운기조식. 내공 +" + MeditateQi });
                        break;
                    }
                default:
                    if (cmd.Skill == null || !Player.CanUse(cmd.Skill)) throw new InvalidOperationException("Skill unavailable.");
                    UseSkill(cmd, events);
                    break;
            }

            if (IsOver) return events;
            events.AddRange(RunUntilPlayer());
            return events;
        }

        private void UseSkill(PlayerCommand cmd, List<BattleEvent> events)
        {
            var skill = cmd.Skill;
            Player.Qi -= skill.QiCost;
            Player.Spirit -= skill.SpiritCost;
            if (skill.Cooldown > 0) Player.Cooldowns[skill.Id] = skill.Cooldown;

            if (skill.Effect == SkillEffect.Evade)
            {
                Player.Evading = true;
                events.Add(new BattleEvent { Turn = Turn, ActorId = Player.Id, TargetId = Player.Id, SkillId = skill.Id, Kind = EventKind.Evade, TargetHpAfter = Player.Hp, Message = Player.Name + "의 " + skill.Name + "!" });
                return;
            }

            var targets = new List<Combatant>();
            if (skill.AllEnemies) { foreach (var e in _enemies) if (e.IsAlive) targets.Add(e); }
            else
            {
                var t = cmd.Target != null && cmd.Target.IsAlive ? cmd.Target : FirstLivingEnemy();
                if (t != null) targets.Add(t);
            }

            int hits = Math.Max(1, skill.Hits);
            for (int h = 0; h < hits; h++)
            {
                foreach (var t in targets)
                {
                    if (!t.IsAlive) continue;
                    bool crit = _rng.NextDouble() < Player.Stats.CritRate;
                    int dmg = BattleEngine.CalcDamage(Player.Stats, t.Stats, skill, crit);
                    t.Hp = Math.Max(0, t.Hp - dmg);
                    if (!t.IsAlive) t.Charging = false;
                    events.Add(new BattleEvent
                    {
                        Turn = Turn, ActorId = Player.Id, TargetId = t.Id, SkillId = skill.Id, Kind = EventKind.Attack,
                        Damage = dmg, IsCrit = crit, TargetHpAfter = t.Hp, TargetDefeated = !t.IsAlive,
                        HitIndex = h, HitCount = hits
                    });
                }
            }
            if (skill.SpiritCost == 0) Player.Spirit = Math.Min(MaxSpirit, Player.Spirit + SpiritOnDealt);
        }

        private List<BattleEvent> RunUntilPlayer()
        {
            var events = new List<BattleEvent>();
            while (!IsOver)
            {
                var actor = NextActor();
                actor.Gauge -= GaugeThreshold;
                Turn++;
                actor.Guarding = false;
                actor.Evading = false;

                if (actor == Player)
                {
                    Player.TickCooldowns();
                    Player.Qi = Math.Min(Player.Stats.MaxQi, Player.Qi + QiRegenPerTurn);
                    _playerTurn = true;
                    return events;
                }
                EnemyAct(actor, events);
            }
            _playerTurn = false;
            return events;
        }

        private void EnemyAct(Combatant enemy, List<BattleEvent> events)
        {
            var move = enemy.PeekMove();
            enemy.MoveIndex++;

            if (move.Charge)
            {
                enemy.Charging = true;
                events.Add(new BattleEvent { Turn = Turn, ActorId = enemy.Id, TargetId = enemy.Id, SkillId = move.Name, Kind = EventKind.Charge, TargetHpAfter = enemy.Hp, Message = enemy.Name + "이(가) " + move.Name + "!" });
                return;
            }

            enemy.Charging = false;
            var spec = new SkillSpec { Id = move.Name, Name = move.Name, Power = move.Power };
            int hits = Math.Max(1, move.Hits);
            bool hurt = false;
            for (int h = 0; h < hits && Player.IsAlive; h++)
            {
                var ev = new BattleEvent
                {
                    Turn = Turn, ActorId = enemy.Id, TargetId = Player.Id, SkillId = move.Name, Kind = EventKind.Attack,
                    HitIndex = h, HitCount = hits
                };
                if (Player.Evading)
                {
                    ev.Dodged = true;
                    ev.TargetHpAfter = Player.Hp;
                }
                else
                {
                    ev.IsCrit = _rng.NextDouble() < enemy.Stats.CritRate;
                    int dmg = BattleEngine.CalcDamage(enemy.Stats, Player.Stats, spec, ev.IsCrit);
                    if (Player.Guarding) { dmg = Math.Max(1, (int)Math.Round(dmg * GuardDamageFactor)); ev.Guarded = true; }
                    Player.Hp = Math.Max(0, Player.Hp - dmg);
                    ev.Damage = dmg;
                    ev.TargetHpAfter = Player.Hp;
                    ev.TargetDefeated = !Player.IsAlive;
                    hurt = true;
                }
                events.Add(ev);
            }
            if (hurt) Player.Spirit = Math.Min(MaxSpirit, Player.Spirit + SpiritOnTaken);
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

        /// <summary>Who acts next, in order (the first entry is the unit acting right now if it is the player's turn). Does not change state.</summary>
        public List<Combatant> PreviewOrder(int count)
        {
            var order = new List<Combatant>();
            var gauges = new int[_units.Count];
            for (int i = 0; i < _units.Count; i++) gauges[i] = _units[i].Gauge;
            if (IsPlayerTurn) order.Add(Player);

            int guard = 0;
            while (order.Count < count && !IsOver && guard++ < 2000)
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
