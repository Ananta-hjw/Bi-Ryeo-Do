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
        public Combatant AllyTarget;  // healing arts and healing items: the ally to heal (null/dead = the most wounded one)

        public PlayerCommand WithAlly(Combatant ally)
        {
            var c = this;
            c.AllyTarget = ally;
            return c;
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
        // 정예·보스 enemies: 내공 comes back every action; a heavy strike costs EnemyQiPerPower per point of power (x hits);
        // a full 기세 makes the next strike hit EnemyBurstFactor harder; running dry weakens a heavy strike.
        public const int EnemyQiRegen = 3, EnemyQiPerPower = 8, EnemySpiritOnDealt = 12;
        public const float EnemyHeavyPower = 2.5f;
        public const float EnemyBurstFactor = 1.5f, EnemyDryFactor = 0.6f;
        public const int SpiritOnEnemyHit = 6;
        public const int SnipeReach = 4;

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
        /// <summary>수화관: how many of the living members have attacked in the current unbroken chain.</summary>
        public int Combo { get { int n = 0; foreach (var m in _party) if (m.IsAlive && _chain.Contains(m.Id)) n++; return n; } }
        public bool TrialFailed { get; private set; }
        private bool _harmonyBoost;
        private readonly HashSet<string> _chain = new HashSet<string>();   // who has attacked in the current 수화관 chain

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
            // Anyone without a tile gets the default one (the UI places the party from the saved formation before this).
            bool unplaced = false;
            foreach (var p in _party) if (p.Col < 0) unplaced = true;
            if (unplaced) Formation.Apply(_party, null);
            Formation.PlaceEnemies(_enemies, _party);
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
            _chain.Clear();
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

        public Combatant FirstLivingEnemy()
        {
            foreach (var e in _enemies) if (e.IsAlive) return e;
            return null;
        }

        // ------------------------------------------------------------------ board

        /// <summary>
        /// How many lines a unit stands behind the front line of its own side (0 = front). Lines are the columns of the board:
        /// ours are 2 (front), 1, 0 (back); the enemy's are 3 (front), 4, 5 (back). The front line is the nearest column that still has a living unit.
        /// </summary>
        public int Depth(Combatant unit)
        {
            if (unit.Team == Team.Player)
            {
                int front = -1;
                foreach (var m in _party) if (m.IsAlive && m.Col > front) front = m.Col;
                return Math.Max(0, front - unit.Col);
            }
            int fe = int.MaxValue;
            foreach (var e in _enemies) if (e.IsAlive && e.Col < fe) fe = e.Col;
            return Math.Max(0, unit.Col - fe);
        }

        /// <summary>Living enemies an art of the given reach can touch (lines 0..reach-1 behind the enemy front), front line first.</summary>
        public List<Combatant> EnemiesInRange(int range)
        {
            var list = new List<Combatant>();
            foreach (var e in _enemies) if (e.IsAlive && Depth(e) < range) list.Add(e);
            list.Sort((x, y) => { int c = Depth(x).CompareTo(Depth(y)); return c != 0 ? c : x.Row.CompareTo(y.Row); });
            return list;
        }

        /// <summary>Attacks and area arts need a target; evade, party guard and healing do not.</summary>
        public static bool NeedsTarget(SkillSpec skill) { return skill.Effect == SkillEffect.None || skill.Effect == SkillEffect.Stun; }

        /// <summary>True when the art can be used now (arts without a target always can; there is always a front-line enemy while any lives).</summary>
        public bool CanReach(SkillSpec skill)
        {
            return !NeedsTarget(skill) || EnemiesInRange(skill.Range).Count > 0;
        }

        /// <summary>The enemy a single-target art really hits: the player's pick if it is within reach (so a back-line enemy only when the art reaches the back and no one is in front of the pick for arts that cannot), otherwise the front-line enemy.</summary>
        public Combatant ResolveTarget(Combatant wanted, SkillSpec skill)
        {
            if (wanted != null && wanted.IsAlive && wanted.Team == Team.Enemy && Depth(wanted) < skill.Range) return wanted;
            var near = EnemiesInRange(skill.Range);
            return near.Count == 0 ? null : near[0];
        }

        /// <summary>Resolves the current member's action, then every enemy turn up to the next party turn.</summary>
        public List<BattleEvent> PlayerAct(PlayerCommand cmd)
        {
            if (!IsPlayerTurn) throw new InvalidOperationException("Not the player's turn.");
            if (cmd.Kind == CommandKind.Item && (cmd.Item == null || cmd.Item.Kind != ItemKind.Consumable || cmd.Item.MasteryUses > 0)) throw new InvalidOperationException("Not a consumable.");
            // Validate everything before touching any state, so a rejected command leaves the turn intact.
            if (cmd.Kind == CommandKind.Joint && (cmd.Skill == null || !CanJoint(cmd.Partner))) throw new InvalidOperationException("Joint attack unavailable.");
            if (cmd.Kind == CommandKind.Skill && (cmd.Skill == null || !Current.CanUse(cmd.Skill))) throw new InvalidOperationException("Skill unavailable.");
            if (cmd.Kind == CommandKind.Skill && !CanReach(cmd.Skill)) throw new InvalidOperationException("Out of reach.");
            if (cmd.Kind == CommandKind.Joint && EnemiesInRange(cmd.Skill.Range).Count == 0) throw new InvalidOperationException("Out of reach.");
            var actor = Current;
            var events = new List<BattleEvent>();
            Current = null;
            PartyActions++;
            _harmonyBoost = false;
            if (Trial != null && Trial.Kind == TrialKind.Harmony)
            {
                bool attacking = cmd.Kind == CommandKind.Joint || (cmd.Kind == CommandKind.Skill && cmd.Skill != null && cmd.Skill.Effect != SkillEffect.Evade
                    && cmd.Skill.Effect != SkillEffect.GuardParty && cmd.Skill.Effect != SkillEffect.HealAlly && cmd.Skill.Effect != SkillEffect.Cleanse);
                if (attacking)
                {
                    // the chain is full once every living member has attacked in it (one fast member attacking again does not count twice);
                    // the attack after that is boosted and starts a new chain
                    int alive = 0; foreach (var m in _party) if (m.IsAlive) alive++;
                    _harmonyBoost = Combo >= alive;
                    if (_harmonyBoost) _chain.Clear();
                    else
                    {
                        _chain.Add(actor.Id);
                        if (cmd.Kind == CommandKind.Joint && cmd.Partner != null) _chain.Add(cmd.Partner.Id);
                    }
                }
                else _chain.Clear();
            }

            int firstEvent = events.Count;
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
                        // 운기 also clears one 상태 이상, the one that hurts most
                        foreach (var k in Statuses.CurePriority)
                            if (actor.RemoveStatus(k)) { events.Add(Cured(actor, actor, k, actor.Name + "의 " + Statuses.Name(k) + "이(가) 풀렸다.")); break; }
                        break;
                    }
                case CommandKind.Item:
                    {
                        var item = cmd.Item;
                        if (item == null || item.Kind != ItemKind.Consumable) throw new InvalidOperationException("Not a consumable.");
                        // Every effect of the item lands on the chosen ally (healing items default to the most wounded one, the rest to the user).
                        var target = cmd.AllyTarget != null && cmd.AllyTarget.IsAlive && _party.Contains(cmd.AllyTarget) ? cmd.AllyTarget
                            : item.HealPct > 0f ? MostWounded() : item.Cleanse ? MostAfflicted() ?? actor : actor;
                        int heal = 0;
                        if (item.HealPct > 0f)
                        {
                            heal = (int)Math.Round(target.Stats.MaxHp * item.HealPct);
                            target.Hp = Math.Min(target.Stats.MaxHp, target.Hp + heal);
                        }
                        if (item.QiRestore > 0) target.Qi = Math.Min(target.Stats.MaxQi, target.Qi + item.QiRestore);
                        if (item.SpiritGain > 0) target.Spirit = Math.Min(MaxSpirit, target.Spirit + item.SpiritGain);
                        events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = target.Id, SkillId = item.Id, Kind = EventKind.Item, Heal = heal, TargetHpAfter = target.Hp, Message = actor.Name + ": " + item.Name + (target != actor ? " → " + target.Name : "") + " 사용" });
                        if (item.Cleanse && target.Statuses.Count > 0) { target.Statuses.Clear(); events.Add(Cured(actor, target, StatusKind.None, target.Name + "의 상태 이상이 모두 풀렸다.")); }
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

            for (int i = firstEvent; i < events.Count; i++) { events[i].ActorQiAfter = actor.Qi; events[i].ActorSpiritAfter = actor.Spirit; }
            CheckPhases(events);
            if (Trial != null && (Trial.Kind == TrialKind.Deadline || Trial.Kind == TrialKind.Harmony) && PartyActions >= Trial.Limit && AnyEnemyAlive) TrialFailed = true;
            if (IsOver) return events;
            events.AddRange(RunUntilParty());
            return events;
        }

        /// <summary>The ally a heal goes to: the player's pick if that member is alive and in the party, otherwise the most wounded one.</summary>
        private Combatant ResolveAlly(Combatant wanted)
        {
            return wanted != null && wanted.IsAlive && _party.Contains(wanted) ? wanted : MostWounded();
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
                    actor.EvadeChance = skill.EvadeChance;
                    events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = actor.Id, SkillId = skill.Id, Kind = EventKind.Evade, TargetHpAfter = actor.Hp, Message = actor.Name + "의 " + skill.Name + "!" });
                    return;
                case SkillEffect.GuardParty:
                    _partyGuardOwner = actor;
                    events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = actor.Id, SkillId = skill.Id, Kind = EventKind.Guard, TargetHpAfter = actor.Hp, Message = actor.Name + "의 " + skill.Name + "! 모두가 방어 태세." });
                    return;
                case SkillEffect.HealAlly:
                    {
                        var weakest = ResolveAlly(cmd.AllyTarget);
                        int heal = (int)Math.Round(weakest.Stats.MaxHp * skill.HealPct);
                        weakest.Hp = Math.Min(weakest.Stats.MaxHp, weakest.Hp + heal);
                        events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = weakest.Id, SkillId = skill.Id, Kind = EventKind.Heal, Heal = heal, TargetHpAfter = weakest.Hp, Message = actor.Name + "의 " + skill.Name + "! " + weakest.Name + " 회복." });
                        return;
                    }
                case SkillEffect.Cleanse:
                    {
                        // the chosen ally, else the one carrying the most statuses (else the most wounded)
                        var who = cmd.AllyTarget != null && cmd.AllyTarget.IsAlive && _party.Contains(cmd.AllyTarget) ? cmd.AllyTarget : MostAfflicted() ?? MostWounded();
                        int heal = (int)Math.Round(who.Stats.MaxHp * skill.HealPct);
                        who.Hp = Math.Min(who.Stats.MaxHp, who.Hp + heal);
                        events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = who.Id, SkillId = skill.Id, Kind = EventKind.Heal, Heal = heal, TargetHpAfter = who.Hp, Message = actor.Name + "의 " + skill.Name + "! " + who.Name + "의 몸이 맑아진다." });
                        if (who.Statuses.Count > 0) { who.Statuses.Clear(); events.Add(Cured(actor, who, StatusKind.None, who.Name + "의 상태 이상이 모두 풀렸다.")); }
                        return;
                    }
            }

            var targets = new List<Combatant>();
            if (skill.AllEnemies) targets = EnemiesInRange(skill.Range);
            else
            {
                var t = ResolveTarget(cmd.Target, skill);
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
            // 상태 이상: one roll per enemy the art struck (and that is still standing)
            if (skill.Inflict != StatusKind.None && skill.InflictChance > 0f)
                foreach (var t in targets)
                    if (t.IsAlive && _rng.NextDouble() < skill.InflictChance) Inflict(actor, t, skill.Inflict, events);
            if (skill.SpiritCost == 0) actor.Spirit = Math.Min(MaxSpirit, actor.Spirit + SpiritOnDealt + actor.SpiritDealtBonus);
        }

        private BattleEvent DealHit(Combatant actor, Combatant t, SkillSpec skill, int h, int hits, bool stun, List<BattleEvent> events)
        {
            bool crit = _rng.NextDouble() < actor.Stats.CritRate;
            int dmg = BattleEngine.CalcDamage(actor.Stats, t.Stats, skill, crit);
            if (_harmonyBoost) dmg = (int)Math.Round(dmg * HarmonyDamageFactor);
            t.Hp = Math.Max(0, t.Hp - dmg);
            if (t.UsesResources && t.IsAlive) t.Spirit = Math.Min(MaxSpirit, t.Spirit + SpiritOnEnemyHit);
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
            if (h == 0 && t.IsAlive && t.Has(StatusKind.Bleed)) StatusHurt(t, StatusKind.Bleed, events);   // 출혈: once per attack that lands
            return ev;
        }

        private void UseJoint(Combatant actor, PlayerCommand cmd, List<BattleEvent> events)
        {
            var spec = cmd.Skill;
            var partner = cmd.Partner;
            var t = ResolveTarget(cmd.Target, spec);
            if (t == null) return;   // nothing to hit: nothing is paid
            actor.Spirit -= JointSpiritCost;
            partner.Gauge -= GaugeThreshold;   // the partner spends their next action on this strike

            var first = DealHit(actor, t, spec, 0, 1, false, events);
            first.Joint = true;
            var next = t.IsAlive ? t : ResolveTarget(null, spec);
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

        // ------------------------------------------------------------------ 상태 이상

        private static BattleEvent Cured(Combatant actor, Combatant target, StatusKind kind, string message)
        {
            return new BattleEvent { ActorId = actor.Id, TargetId = target.Id, SkillId = "status", Kind = EventKind.Status, Status = kind, StatusCured = true, TargetHpAfter = target.Hp, Message = message };
        }

        private void Inflict(Combatant source, Combatant target, StatusKind kind, List<BattleEvent> events)
        {
            if (kind == StatusKind.None || !target.IsAlive) return;
            if (!target.AddStatus(kind, Statuses.DefaultTurns(kind))) return;   // already carries a status: nothing new takes hold
            events.Add(new BattleEvent { Turn = Turn, ActorId = source.Id, TargetId = target.Id, SkillId = "status", Kind = EventKind.Status, Status = kind, TargetHpAfter = target.Hp, Message = target.Name + ": " + Statuses.Name(kind) + "!" });
        }

        /// <summary>중독·화상 at the unit's turn and 출혈 when struck: a share of max HP. Can fell the unit (a bonded companion may still save the protagonist; a story boss flees instead).</summary>
        private void StatusHurt(Combatant unit, StatusKind kind, List<BattleEvent> events)
        {
            int dmg = Statuses.DotDamage(unit, kind);
            if (dmg <= 0 || !unit.IsAlive) return;
            unit.Hp = Math.Max(0, unit.Hp - dmg);
            var ev = new BattleEvent { Turn = Turn, ActorId = unit.Id, TargetId = unit.Id, SkillId = "status", Kind = EventKind.Status, Status = kind, Damage = dmg, Message = unit.Name + ": " + Statuses.Name(kind) + " -" + dmg };
            if (unit.Hp == 0 && unit.Team == Team.Player) TryRescue(unit, ev);
            if (unit.IsAlive && unit.RetreatHpPct > 0f && unit.Hp <= unit.Stats.MaxHp * unit.RetreatHpPct)
            {
                unit.Hp = 0;
                ev.Retreated = true;
                ev.Message = unit.Name + "(이)가 물러난다!";
            }
            if (!unit.IsAlive) { unit.Charging = false; unit.ChargeTarget = null; }
            ev.TargetHpAfter = unit.Hp;
            ev.TargetDefeated = !unit.IsAlive;
            events.Add(ev);
        }

        /// <summary>Start of a unit's own turn: 중독·화상 hurt, then every status loses a turn.</summary>
        private void TickStatuses(Combatant unit, List<BattleEvent> events)
        {
            if (unit.Statuses.Count == 0) return;
            foreach (var k in new[] { StatusKind.Poison, StatusKind.Burn }) if (unit.Has(k)) StatusHurt(unit, k, events);
            for (int i = unit.Statuses.Count - 1; i >= 0; i--)
                if (--unit.Statuses[i].Turns <= 0) unit.Statuses.RemoveAt(i);
        }

        /// <summary>The living member carrying the most statuses, or null when nobody carries any.</summary>
        private Combatant MostAfflicted()
        {
            Combatant best = null;
            foreach (var m in _party) if (m.IsAlive && m.Statuses.Count > 0 && (best == null || m.Statuses.Count > best.Statuses.Count)) best = m;
            return best;
        }

        private static int GaugeGain(Combatant u) { return Math.Max(1, (int)Math.Round(u.Stats.Speed * Statuses.SpeedFactor(u))); }

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

                bool wounded = actor.Has(StatusKind.InnerWound);   // 내상 this turn (read before the statuses tick down)
                TickStatuses(actor, events);
                if (!actor.IsAlive) continue;   // fell to 중독·화상 before acting
                if (actor.Team == Team.Enemy) CheckPhases(events);   // 중독·화상 can push a boss into its next phase

                if (actor.Team == Team.Player)
                {
                    actor.TickCooldowns();
                    if (!wounded) actor.Qi = Math.Min(actor.Stats.MaxQi, actor.Qi + QiRegenPerTurn + actor.QiRegenBonus);
                    Current = actor;
                    return events;
                }
                EnemyAct(actor, events, wounded);
            }
            Current = null;
            return events;
        }

        /// <summary>The living party member an enemy hits by default: the front line (ties: the more wounded one, then list order).</summary>
        private Combatant FrontMember()
        {
            Combatant best = null;
            int bestDepth = int.MaxValue;
            foreach (var m in _party)
            {
                if (!m.IsAlive) continue;
                int d = Depth(m);
                if (best == null || d < bestDepth || (d == bestDepth && (float)m.Hp / m.Stats.MaxHp < (float)best.Hp / best.Stats.MaxHp)) { best = m; bestDepth = d; }
            }
            return best;
        }

        /// <summary>The member a 저격 move picks: the back-most one within reach (ties: the more wounded one).</summary>
        private Combatant BackMember(int reach)
        {
            Combatant far = null; int farDepth = -1;
            foreach (var m in _party)
            {
                if (!m.IsAlive) continue;
                int d = Depth(m);
                if (d >= reach) continue;
                if (d > farDepth || (d == farDepth && (float)m.Hp / m.Stats.MaxHp < (float)far.Hp / far.Stats.MaxHp)) { far = m; farDepth = d; }
            }
            return far;
        }

        /// <summary>보스 단계 전환: every boss whose HP has fallen to its next phase begins it (both at once if one blow crossed two).</summary>
        private void CheckPhases(List<BattleEvent> events)
        {
            foreach (var e in _enemies)
            {
                if (!e.IsAlive || e.Phases == null) continue;
                while (e.PhaseIndex < e.Phases.Count && e.Hp <= e.Stats.MaxHp * e.Phases[e.PhaseIndex].HpPct)
                {
                    var phase = e.Phases[e.PhaseIndex++];
                    e.ScaleAttackDefense(phase.AtkMul, phase.DefMul);
                    e.Gauge = 0;   // the change of stance costs it a beat
                    if (phase.Moves != null)
                    {
                        // a wind-up in progress still ends in its heavy blow; the new script starts after it
                        if (e.Charging) e.PendingMoves = new List<EnemyMove>(phase.Moves);
                        else { e.Moves = new List<EnemyMove>(phase.Moves); e.MoveIndex = 0; e.PendingMoves = null; }
                    }
                    events.Add(new BattleEvent
                    {
                        Turn = Turn, ActorId = e.Id, TargetId = e.Id, SkillId = "phase", Kind = EventKind.Phase, HitIndex = e.PhaseIndex,
                        Lines = phase.Lines, TargetHpAfter = e.Hp, Message = e.Name + "의 기세가 달라졌다!"
                    });
                }
            }
        }

        /// <summary>A phase's script that waited for a wind-up (see <see cref="CheckPhases"/>) starts once nothing is being wound up.</summary>
        private static void StartPendingScript(Combatant enemy)
        {
            if (enemy.Charging || enemy.PendingMoves == null) return;
            enemy.Moves = enemy.PendingMoves;
            enemy.MoveIndex = 0;
            enemy.PendingMoves = null;
        }

        private void EnemyAct(Combatant enemy, List<BattleEvent> events, bool wounded = false)
        {
            EnemyActions++;
            StartPendingScript(enemy);
            if (enemy.Stunned)
            {
                enemy.Stunned = false;
                events.Add(new BattleEvent { Turn = Turn, ActorId = enemy.Id, TargetId = enemy.Id, SkillId = "stun", Kind = EventKind.StunSkip, TargetHpAfter = enemy.Hp, Message = enemy.Name + "은(는) 몸이 굳어 움직이지 못한다!" });
                return;
            }

            if (enemy.UsesResources && !wounded) enemy.Qi = Math.Min(enemy.Stats.MaxQi, enemy.Qi + EnemyQiRegen);
            var move = enemy.PeekMove();

            if (move.Charge)
            {
                enemy.MoveIndex++;
                enemy.Charging = true;
                enemy.ChargeTarget = FrontMember();
                string who = enemy.ChargeTarget != null ? " → " + enemy.ChargeTarget.Name : "";
                events.Add(new BattleEvent { Turn = Turn, ActorId = enemy.Id, TargetId = enemy.ChargeTarget != null ? enemy.ChargeTarget.Id : enemy.Id, SkillId = move.Name, Kind = EventKind.Charge, TargetHpAfter = enemy.Hp, Message = enemy.Name + "이(가) " + move.Name + "!" + who });
                return;
            }

            var target = enemy.Charging && enemy.ChargeTarget != null && enemy.ChargeTarget.IsAlive ? enemy.ChargeTarget : FrontMember();
            if (target == null) return;
            // A 저격 move reaches every line and picks the back-most party member; everything else hits the front line.
            if (move.Snipe && !enemy.Charging)
            {
                var far = BackMember(Math.Max(enemy.Range, SnipeReach));
                if (far != null) target = far;
            }

            enemy.MoveIndex++;
            bool heavy = enemy.Charging;
            enemy.Charging = false;
            enemy.ChargeTarget = null;
            StartPendingScript(enemy);   // a phase that began during the wind-up takes over after this blow

            int hits = Math.Max(1, move.Hits);
            float power = move.Power;
            bool burst = false, dry = false;
            if (enemy.UsesResources)
            {
                int cost = EnemyQiCost(move);
                if (cost > 0)
                {
                    if (enemy.Qi >= cost) enemy.Qi -= cost;
                    else { dry = true; enemy.Qi = 0; power *= EnemyDryFactor; }
                }
                if (enemy.Spirit >= MaxSpirit) { burst = true; enemy.Spirit = 0; power *= EnemyBurstFactor; }
            }
            var spec = new SkillSpec { Id = move.Name, Name = move.Name, Power = power };
            bool hurt = false, struck = false;   // struck: at least one hit landed without a guard (only those leave a 상태 이상)
            bool bled = target.Has(StatusKind.Bleed);
            int firstHit = events.Count;
            for (int h = 0; h < hits && target.IsAlive; h++)
            {
                var ev = new BattleEvent
                {
                    Turn = Turn, ActorId = enemy.Id, TargetId = target.Id, SkillId = move.Name, Kind = EventKind.Attack,
                    HitIndex = h, HitCount = hits, Burst = burst && h == 0, Weakened = dry && h == 0
                };
                if (target.Evading && (target.EvadeChance >= 1f || _rng.NextDouble() < target.EvadeChance))
                {
                    ev.Dodged = true;
                    ev.TargetHpAfter = target.Hp;
                }
                else
                {
                    ev.IsCrit = _rng.NextDouble() < enemy.Stats.CritRate;
                    int dmg = BattleEngine.CalcDamage(enemy.Stats, target.Stats, spec, ev.IsCrit);
                    if (IsGuarded(target)) { dmg = Math.Max(1, (int)Math.Round(dmg * GuardDamageFactor)); ev.Guarded = true; }
                    target.Hp = Math.Max(0, target.Hp - dmg);
                    if (target.Hp == 0) TryRescue(target, ev);
                    ev.Damage = dmg;
                    ev.TargetHpAfter = target.Hp;
                    ev.TargetDefeated = !target.IsAlive;
                    hurt = true;
                    if (!ev.Guarded) struck = true;
                }
                events.Add(ev);
            }
            // 금요관: a charged blow that lands (even guarded) cuts one rope, however many hits it had; one fully dodged cuts nothing
            if (heavy && hurt && Trial != null && Trial.Kind == TrialKind.Lifeline)
            {
                LifelinesCut++;
                if (LifelinesCut >= Trial.Limit) TrialFailed = true;
            }
            if (hurt && bled && target.IsAlive) StatusHurt(target, StatusKind.Bleed, events);
            if (struck && target.IsAlive)
            {
                if (move.Inflict != StatusKind.None && _rng.NextDouble() < (heavy ? Statuses.ChargedChance : Statuses.MoveChance)) Inflict(enemy, target, move.Inflict, events);
                if (heavy) Inflict(enemy, target, StatusKind.InnerWound, events);   // a charged blow taken without a guard leaves 내상
            }
            if (hurt) target.Spirit = Math.Min(MaxSpirit, target.Spirit + SpiritOnTaken + target.SpiritTakenBonus);
            if (enemy.UsesResources && !burst) enemy.Spirit = Math.Min(MaxSpirit, enemy.Spirit + EnemySpiritOnDealt);
            for (int i = firstHit; i < events.Count; i++) events[i].TargetSpiritAfter = target.Spirit;
        }

        /// <summary>내공 a 정예/보스 spends on a move: heavy strikes (power x hits of 2 or more) cost, light ones are free.</summary>
        public static int EnemyQiCost(EnemyMove move)
        {
            float total = move.Power * Math.Max(1, move.Hits);
            return move.Charge || total < EnemyHeavyPower ? 0 : (int)Math.Round(total * EnemyQiPerPower);
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
                    if (u.IsAlive) u.Gauge += GaugeGain(u);
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
                        if (_units[i].IsAlive) gauges[i] += GaugeGain(_units[i]);
                    continue;
                }
                gauges[best] -= GaugeThreshold;
                order.Add(_units[best]);
            }
            return order;
        }
    }
}
