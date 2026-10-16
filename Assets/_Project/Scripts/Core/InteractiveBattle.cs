using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    public enum CommandKind { Skill, Guard, Meditate, Item, Joint }

    public struct PlayerCommand
    {
        public CommandKind Kind;
        public SkillSpec Skill;      // for Kind == Skill
        public Combatant Target;     // single-target skills; null/dead falls back to the first living enemy
        public ItemData Item;        // for Kind == Item (a consumable; the caller removes it from the bag)
        public Combatant Partner;    // for Kind == Joint: the ally who strikes together with the protagonist (Skill holds the joint spec)

        public static PlayerCommand UseSkill(SkillSpec skill, Combatant target)
        {
            return new PlayerCommand { Kind = CommandKind.Skill, Skill = skill, Target = target };
        }
        public static PlayerCommand Joint(SkillSpec spec, Combatant partner, Combatant target)
        {
            return new PlayerCommand { Kind = CommandKind.Joint, Skill = spec, Partner = partner, Target = target };
        }
        public static PlayerCommand UseItem(ItemData item) { return new PlayerCommand { Kind = CommandKind.Item, Item = item }; }
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
        public const float HarmonyDamageFactor = 2.5f;
        public const int JointSpiritCost = 50;

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

        // Story-trial rule (set before Begin). Null = a normal fight.
        public TrialRule Trial { get; set; }
        public int PartyActions { get; private set; }
        public int EnemyActions { get; private set; }
        public int LifelinesCut { get; private set; }
        public int Combo { get; private set; }
        public bool TrialFailed { get; private set; }
        private bool _harmonyBoost;

        public InteractiveBattle(Combatant player, IEnumerable<Combatant> enemies, IRandom rng)
            : this(new[] { player }, enemies, rng)
        {
        }

        public InteractiveBattle(IEnumerable<Combatant> party, IEnumerable<Combatant> enemies, IRandom rng)
        {
            foreach (var p in party) { _units.Add(p); _party.Add(p); }
            foreach (var e in enemies) { _units.Add(e); _enemies.Add(e); }
            for (int i = Formation.MaxFront; i < _enemies.Count; i++) _enemies[i].BackRow = true;   // the first two stand in front, the rest behind
            if (_party.Count == 0) throw new ArgumentException("Party is empty.");
            _rng = rng;
        }

        public bool AnyEnemyAlive { get { foreach (var e in _enemies) if (e.IsAlive) return true; return false; } }
        public bool AnyPartyAlive { get { foreach (var p in _party) if (p.IsAlive) return true; return false; } }

        public bool SurvivedTrial { get { return Trial != null && Trial.Kind == TrialKind.Survive && EnemyActions >= Trial.Limit; } }
        public bool IsOver { get { return !AnyPartyAlive || !AnyEnemyAlive || TrialFailed || SurvivedTrial || Turn >= MaxTurns; } }
        public bool PlayerWon { get { return AnyPartyAlive && !TrialFailed && (!AnyEnemyAlive || SurvivedTrial); } }

        /// <summary>One-line trial progress for the UI ("" for a normal fight).</summary>
        public string TrialStatus
        {
            get
            {
                if (Trial == null) return "";
                switch (Trial.Kind)
                {
                    case TrialKind.Survive: return "버텨라 " + Math.Min(EnemyActions, Trial.Limit) + "/" + Trial.Limit;
                    case TrialKind.Deadline: return "제한 " + PartyActions + "/" + Trial.Limit + "행동";
                    case TrialKind.Lifeline:
                        {
                            var sb = new System.Text.StringBuilder("생명줄 ");
                            for (int i = 0; i < Trial.Limit; i++) sb.Append(i < Trial.Limit - LifelinesCut ? "●" : "○");
                            return sb.ToString();
                        }
                    case TrialKind.Harmony:
                        {
                            int alive = 0; foreach (var m in _party) if (m.IsAlive) alive++;
                            return "합격 " + Math.Min(Combo, alive) + "/" + alive + (Combo >= alive ? " 다음 일격 강화!" : "") + "  " + PartyActions + "/" + Trial.Limit + "행동";
                        }
                }
                return "";
            }
        }
        public string TrialFailReason
        {
            get
            {
                if (!TrialFailed || Trial == null) return "";
                return Trial.Kind == TrialKind.Lifeline ? "생명줄이 끊어졌다!" : "제한 시간이 지났다!";
            }
        }
        public bool IsPlayerTurn { get { return Current != null && !IsOver; } }

        /// <summary>Runs enemy turns until a party member may act. Call once at the start of the fight.</summary>
        public List<BattleEvent> Begin()
        {
            _partyGuardOwner = null;
            foreach (var m in _party) m.RescueUsed = false;
            foreach (var m in _party) if (m.Spirit < m.StartSpirit) m.Spirit = m.StartSpirit;
            return RunUntilParty();
        }

        // 태을천강벽 style guards cover the whole party until the caster acts again. A per-unit flag was cleared when each member
        // acted, so allies who moved before the next enemy attack silently lost it.
        private Combatant _partyGuardOwner;

        public bool IsGuarded(Combatant unit)
        {
            if (unit.Guarding) return true;
            return _partyGuardOwner != null && unit.IsAlive && _party.Contains(unit);   // survives the caster falling mid-phase
        }

        public bool CanUse(SkillSpec skill) { return IsPlayerTurn && Current.CanUse(skill); }

        /// <summary>합공: the protagonist (on their turn) strikes together with a living ally for 기세 <see cref="JointSpiritCost"/>; the ally loses their next action.</summary>
        public bool CanJoint(Combatant partner)
        {
            return IsPlayerTurn && Current == _party[0] && partner != null && partner != Current && partner.IsAlive
                && _party.Contains(partner) && Current.Spirit >= JointSpiritCost && AnyEnemyAlive;
        }

        /// <summary>The first living enemy, front row first.</summary>
        public Combatant FirstLivingEnemy()
        {
            foreach (var e in _enemies) if (e.IsAlive && !e.BackRow) return e;
            foreach (var e in _enemies) if (e.IsAlive) return e;
            return null;
        }

        /// <summary>The enemy a single-target attack really hits: back-row enemies are shielded while any front-row enemy still stands.</summary>
        public Combatant ResolveEnemyTarget(Combatant wanted)
        {
            if (wanted == null || !wanted.IsAlive || wanted.Team != Team.Enemy) return FirstLivingEnemy();
            if (wanted.BackRow)
                foreach (var e in _enemies) if (e.IsAlive && !e.BackRow) return e;
            return wanted;
        }

        /// <summary>Resolves the current member's action, then every enemy turn up to the next party turn.</summary>
        public List<BattleEvent> PlayerAct(PlayerCommand cmd)
        {
            if (!IsPlayerTurn) throw new InvalidOperationException("Not the player's turn.");
            if (cmd.Kind == CommandKind.Item && (cmd.Item == null || cmd.Item.Kind != ItemKind.Consumable)) throw new InvalidOperationException("Not a consumable.");
            // Validate everything before touching any state, so a rejected command leaves the turn intact.
            if (cmd.Kind == CommandKind.Joint && (cmd.Skill == null || !CanJoint(cmd.Partner))) throw new InvalidOperationException("Joint attack unavailable.");
            if (cmd.Kind == CommandKind.Skill && (cmd.Skill == null || !Current.CanUse(cmd.Skill))) throw new InvalidOperationException("Skill unavailable.");
            var actor = Current;
            var events = new List<BattleEvent>();
            Current = null;
            PartyActions++;
            _harmonyBoost = false;
            if (Trial != null && Trial.Kind == TrialKind.Harmony)
            {
                bool attacking = cmd.Kind == CommandKind.Joint || (cmd.Kind == CommandKind.Skill && cmd.Skill != null && cmd.Skill.Effect != SkillEffect.Evade
                    && cmd.Skill.Effect != SkillEffect.GuardParty && cmd.Skill.Effect != SkillEffect.HealAlly);
                if (attacking)
                {
                    int alive = 0; foreach (var m in _party) if (m.IsAlive) alive++;
                    _harmonyBoost = Combo >= alive;
                    Combo = _harmonyBoost ? 0 : Combo + 1;
                }
                else Combo = 0;
            }

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
                case CommandKind.Item:
                    {
                        var item = cmd.Item;
                        if (item == null || item.Kind != ItemKind.Consumable) throw new InvalidOperationException("Not a consumable.");
                        var target = actor;
                        int heal = 0;
                        if (item.HealPct > 0f)
                        {
                            target = MostWounded();
                            heal = (int)Math.Round(target.Stats.MaxHp * item.HealPct);
                            target.Hp = Math.Min(target.Stats.MaxHp, target.Hp + heal);
                        }
                        if (item.QiRestore > 0) actor.Qi = Math.Min(actor.Stats.MaxQi, actor.Qi + item.QiRestore);
                        if (item.SpiritGain > 0) actor.Spirit = Math.Min(MaxSpirit, actor.Spirit + item.SpiritGain);
                        events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = target.Id, SkillId = item.Id, Kind = EventKind.Item, Heal = heal, TargetHpAfter = target.Hp, Message = actor.Name + ": " + item.Name + " 사용" });
                        break;
                    }
                case CommandKind.Joint:
                    UseJoint(actor, cmd, events);
                    break;
                default:
                    if (cmd.Skill == null || !actor.CanUse(cmd.Skill)) throw new InvalidOperationException("Skill unavailable.");
                    UseSkill(actor, cmd, events);
                    break;
            }

            if (Trial != null && (Trial.Kind == TrialKind.Deadline || Trial.Kind == TrialKind.Harmony) && PartyActions >= Trial.Limit && AnyEnemyAlive) TrialFailed = true;
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
                    _partyGuardOwner = actor;
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
                var t = ResolveEnemyTarget(cmd.Target);
                if (t != null) targets.Add(t);
            }

            int hits = Math.Max(1, skill.Hits);
            for (int h = 0; h < hits; h++)
            {
                foreach (var t in targets)
                {
                    if (!t.IsAlive) continue;
                    DealHit(actor, t, skill, h, hits, skill.Effect == SkillEffect.Stun && h == hits - 1, events);
                }
            }
            if (skill.SpiritCost == 0) actor.Spirit = Math.Min(MaxSpirit, actor.Spirit + SpiritOnDealt + actor.SpiritDealtBonus);
        }

        private BattleEvent DealHit(Combatant actor, Combatant t, SkillSpec skill, int h, int hits, bool stun, List<BattleEvent> events)
        {
            bool crit = _rng.NextDouble() < actor.Stats.CritRate;
            int dmg = BattleEngine.CalcDamage(actor.Stats, t.Stats, skill, crit);
            dmg = Math.Max(1, (int)Math.Round(dmg * (actor.BackRow ? Formation.BackDealtFactor : Formation.FrontDealtFactor)));
            if (t.BackRow) dmg = Math.Max(1, (int)Math.Round(dmg * Formation.BackTakenFactor));
            if (_harmonyBoost) dmg = (int)Math.Round(dmg * HarmonyDamageFactor);
            t.Hp = Math.Max(0, t.Hp - dmg);
            if (actor.Lifesteal > 0f && actor.IsAlive) actor.Hp = Math.Min(actor.Stats.MaxHp, actor.Hp + (int)Math.Round(dmg * actor.Lifesteal));
            var ev = new BattleEvent
            {
                Turn = Turn, ActorId = actor.Id, TargetId = t.Id, SkillId = skill.Id, Kind = EventKind.Attack,
                Damage = dmg, IsCrit = crit, HitIndex = h, HitCount = hits, Harmony = _harmonyBoost
            };
            if (t.IsAlive && t.RetreatHpPct > 0f && t.Hp <= t.Stats.MaxHp * t.RetreatHpPct)
            {
                // Story boss: it escapes rather than dying. The fight ends as a win but nobody is slain.
                t.Hp = 0;
                ev.Retreated = true;
                ev.Message = t.Name + "(이)가 물러난다!";
            }
            if (!t.IsAlive) { t.Charging = false; t.ChargeTarget = null; }
            else if (stun) ApplyStun(t, ev);
            ev.TargetHpAfter = t.Hp;
            ev.TargetDefeated = !t.IsAlive;
            events.Add(ev);
            return ev;
        }

        private void UseJoint(Combatant actor, PlayerCommand cmd, List<BattleEvent> events)
        {
            var spec = cmd.Skill;
            var partner = cmd.Partner;
            var t = ResolveEnemyTarget(cmd.Target);
            if (t == null) return;   // nothing to hit: nothing is paid
            actor.Spirit -= JointSpiritCost;
            partner.Gauge -= GaugeThreshold;   // the partner spends their next action on this strike

            var first = DealHit(actor, t, spec, 0, 1, false, events);
            first.Joint = true;
            var next = t.IsAlive ? t : FirstLivingEnemy();
            if (next != null) DealHit(partner, next, spec, 0, 1, spec.Effect == SkillEffect.Stun, events).Joint = true;

            switch (spec.Effect)
            {
                case SkillEffect.GuardParty:
                    _partyGuardOwner = partner;
                    events.Add(new BattleEvent { Turn = Turn, ActorId = partner.Id, TargetId = partner.Id, SkillId = spec.Id, Kind = EventKind.Guard, Joint = true, TargetHpAfter = partner.Hp, Message = spec.Name + "! \ubaa8\ub450\uac00 \ubc29\uc5b4 \ud0dc\uc138." });
                    break;
                case SkillEffect.HealAlly:
                    {
                        var weakest = MostWounded();
                        int heal = (int)Math.Round(weakest.Stats.MaxHp * spec.HealPct);
                        weakest.Hp = Math.Min(weakest.Stats.MaxHp, weakest.Hp + heal);
                        events.Add(new BattleEvent { Turn = Turn, ActorId = partner.Id, TargetId = weakest.Id, SkillId = spec.Id, Kind = EventKind.Heal, Joint = true, Heal = heal, TargetHpAfter = weakest.Hp, Message = spec.Name + "! " + weakest.Name + " \ud68c\ubcf5." });
                        break;
                    }
            }
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
                // A party-wide guard lasts until its caster's next turn; if the caster fell, until the next ally acts.
                if (actor == _partyGuardOwner || (_partyGuardOwner != null && !_partyGuardOwner.IsAlive && actor.Team == Team.Player)) _partyGuardOwner = null;

                if (actor.Team == Team.Player)
                {
                    actor.TickCooldowns();
                    actor.Qi = Math.Min(actor.Stats.MaxQi, actor.Qi + QiRegenPerTurn + actor.QiRegenBonus);
                    Current = actor;
                    return events;
                }
                EnemyAct(actor, events);
            }
            Current = null;
            return events;
        }

        /// <summary>A random living party member; the front row is hit first, the back row only once no front member is left standing.</summary>
        private Combatant RandomLivingMember()
        {
            var alive = new List<Combatant>();
            foreach (var m in _party) if (m.IsAlive && !m.BackRow) alive.Add(m);
            if (alive.Count == 0) foreach (var m in _party) if (m.IsAlive) alive.Add(m);
            if (alive.Count == 0) return null;
            int i = (int)(_rng.NextDouble() * alive.Count);
            return alive[Math.Min(i, alive.Count - 1)];
        }

        private void EnemyAct(Combatant enemy, List<BattleEvent> events)
        {
            EnemyActions++;
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

            bool heavy = enemy.Charging;
            if (heavy && Trial != null && Trial.Kind == TrialKind.Lifeline)
            {
                LifelinesCut++;
                if (LifelinesCut >= Trial.Limit) TrialFailed = true;
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
                    dmg = Math.Max(1, (int)Math.Round(dmg * (enemy.BackRow ? Formation.BackDealtFactor : Formation.FrontDealtFactor)));
                    if (target.BackRow) dmg = Math.Max(1, (int)Math.Round(dmg * Formation.BackTakenFactor));
                    if (IsGuarded(target)) { dmg = Math.Max(1, (int)Math.Round(dmg * GuardDamageFactor)); ev.Guarded = true; }
                    target.Hp = Math.Max(0, target.Hp - dmg);
                    if (target.Hp == 0) TryRescue(target, ev);
                    ev.Damage = dmg;
                    ev.TargetHpAfter = target.Hp;
                    ev.TargetDefeated = !target.IsAlive;
                    hurt = true;
                }
                events.Add(ev);
            }
            if (hurt) target.Spirit = Math.Min(MaxSpirit, target.Spirit + SpiritOnTaken + target.SpiritTakenBonus);
        }

        /// <summary>A bonded companion (의기투합) throws herself between the protagonist and a killing blow, once per fight.</summary>
        private bool TryRescue(Combatant victim, BattleEvent ev)
        {
            if (victim.RescueUsed || string.IsNullOrEmpty(victim.RescuerId)) return false;
            Combatant rescuer = null;
            foreach (var m in _party) if (m.Id == victim.RescuerId && m.IsAlive && m != victim) rescuer = m;
            if (rescuer == null) return false;
            victim.RescueUsed = true;
            victim.Hp = Math.Max(1, (int)Math.Round(victim.Stats.MaxHp * Bonds.RescueHpPct));
            ev.Rescued = true;
            ev.Message = rescuer.Name + "(이)가 몸을 던져 구했다!";
            return true;
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
