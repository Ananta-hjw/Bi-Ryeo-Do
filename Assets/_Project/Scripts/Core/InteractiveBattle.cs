using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    public enum CommandKind { Skill, Guard, Meditate, Item, Joint, Finale, Beast }

    public struct PlayerCommand
    {
        public CommandKind Kind;
        public SkillSpec Skill;      // for Kind == Skill
        public Combatant Target;     // single-target skills; null/dead falls back to the first living enemy
        public ItemData Item;        // for Kind == Item (a consumable; the caller removes it from the bag)
        public Combatant Partner;    // for Kind == Joint: the ally who strikes together with the protagonist (Skill holds the joint spec)
        public BeastSkill BeastSkill; // for Kind == Beast: the 영수 skill (Target / AllyTarget as for arts)

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
        /// <summary>The showdown's art while its window is open (<see cref="InteractiveBattle.FinaleWindowOpen"/>, 20 섬뢰창).</summary>
        public static PlayerCommand Finale() { return new PlayerCommand { Kind = CommandKind.Finale }; }
        /// <summary>Calls the fight's 영수 (a free action on 비류연's turn, see <see cref="InteractiveBattle.CallBeast"/>).</summary>
        public static PlayerCommand CallBeast(BeastSkill skill, Combatant target) { return new PlayerCommand { Kind = CommandKind.Beast, BeastSkill = skill, Target = target }; }
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
        /// <summary>A party member regains the larger of <see cref="QiRegenPerTurn"/> and this share of max 내공 each turn (plus 심법 bonuses).</summary>
        public const float QiRegenPct = 0.03f;
        /// <summary>운기 restores the larger of <see cref="MeditateQi"/> and this share of max 내공.</summary>
        public const float MeditateQiPct = 0.20f;

        public static int QiRegenFor(int maxQi) { return Math.Max(QiRegenPerTurn, (int)Math.Round(maxQi * QiRegenPct)); }
        public static int MeditateQiFor(int maxQi) { return Math.Max(MeditateQi, (int)Math.Round(maxQi * MeditateQiPct)); }
        public const int MeditateQi = 40;
        public const float MeditateHealPct = 0.08f;
        public const float GuardDamageFactor = 0.4f;
        /// <summary>태을천강벽 (and every party-wide guard): damage x0.7 (half the cut of a guard) for the caster's next three turns.</summary>
        public const float PartyGuardDamageFactor = 0.7f;
        public const int PartyGuardTurns = 3;
        public const float CoverDamageFactor = 0.7f;          // 원의 곤: the covering member takes 30% less
        public const float FocusDamageFactor = 2.3f;          // 유성보: the next attack +130%
        public const float FocusTakenFactor = 1.2f;           // … and +20% taken until then
        public const int WardTurns = 2;                       // 은하밀밀: own turns with no 상태 이상
        public const int PartySpiritGain = 25;                // 뇌광산란
        public const float CounterStancePower = 1.5f;         // 성막밀밀: the strike back
        public const float ChillAttackFactor = 0.9f;          // 한기: the chilled unit deals -10%
        public const int SpiritOnDealt = 10, SpiritOnTaken = 8, SpiritOnGuard = 12;
        public const float HarmonyDamageFactor = 2.5f;
        public const int JointSpiritCost = 50;
        public const float CounterDamageFactor = 1.25f;
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

        // ------------------------------------------------------------------ 화룡멸겁대진 (TrialKind.Finale, 일반 30)

        public const string FinaleSkillId = "scene_pungsin_finale";
        public const string FinaleSkillName = "풍뢰의 장 · 나선의 인 · 용권승룡";
        public const string FinaleBlowName = "화룡멸겁";
        private readonly HashSet<Combatant> _finaleOpened = new HashSet<Combatant>();
        private bool IsFinale { get { return Trial != null && Trial.Kind == TrialKind.Finale; } }
        private bool IsUnavoidable { get { return IsFinale && Trial.Style == FinaleStyle.Unavoidable; } }
        private bool IsWindow { get { return IsFinale && Trial.Style == FinaleStyle.Window; } }
        /// <summary>24 동전 대결: 비류연's turn from the start; the screen times the press, <see cref="DuelMiss"/> answers a late one.</summary>
        public bool IsDuel { get { return IsFinale && Trial.Style == FinaleStyle.Duel; } }
        public int DuelMisses { get; private set; }

        /// <summary>
        /// A late press in the duel: the opponent's blade lands first for <see cref="TrialRule.BlowPct"/> of max HP (never below 1),
        /// and the <see cref="TrialRule.Limit"/>-th miss fells 비류연 and loses the fight. The turn stays his for the next coin.
        /// </summary>
        public List<BattleEvent> DuelMiss()
        {
            if (!IsDuel || !IsPlayerTurn) throw new InvalidOperationException("Not a duel turn.");
            var hero = Current;
            var enemy = FirstLivingEnemy();
            DuelMisses++;
            EnemyActions++;
            bool last = DuelMisses >= Trial.Limit;
            int before = hero.Hp;
            hero.Hp = last ? 0 : Math.Max(1, hero.Hp - Math.Max(1, (int)Math.Round(hero.Stats.MaxHp * Trial.BlowPct)));
            if (last) { TrialFailed = true; Current = null; }
            var script = Trial.Script;
            return new List<BattleEvent> { new BattleEvent
            {
                Turn = Turn, ActorId = enemy != null ? enemy.Id : null, TargetId = hero.Id, SkillId = script.BlowName, Kind = EventKind.Attack,
                Damage = before - hero.Hp, HitIndex = 0, HitCount = 1, TargetHpAfter = hero.Hp, TargetDefeated = !hero.IsAlive,
                Message = (enemy != null ? enemy.Name + "의 " : "") + script.BlowName + "!", Quip = last ? script.LastQuip : script.LateQuip
            } };
        }
        private bool _armorNoted;
        /// <summary>20: 비류연's turn while the enemy winds up (its mouth is open): <see cref="PlayerCommand.Finale"/> ends it.</summary>
        /// <summary>20: how many times the mouth opened on 비류연's turn without 섬뢰창 (the fight's 신속 star asks for none).</summary>
        public int WindowsMissed { get; private set; }

        public bool FinaleWindowOpen
        {
            get
            {
                if (!IsWindow || !IsPlayerTurn || Current != Player) return false;
                foreach (var e in _enemies) if (e.IsAlive && e.Charging) return true;
                return false;
            }
        }
        private int _finaleBlows;
        /// <summary>The opening is over (일반 30: the 진 has felled all but 비류연; 46: the 심도 has done its work): the scene comes next.</summary>
        public bool FinaleStruck { get; private set; }
        /// <summary>비류연's last action: whatever is commanded, it is the showdown's art (<see cref="ShowdownScript.Skill"/>) that ends it.</summary>
        public bool FinaleReady { get { return IsFinale && FinaleStruck && IsPlayerTurn && Current == Player; } }   // companions keep acting until 비류연's turn

        /// <summary>Unavoidable: the enemy's blow lands through guard and dodge for a share of max HP, never below 1.</summary>
        private void UnavoidableBlow(Combatant enemy, List<BattleEvent> events)
        {
            EnemyActions++;
            var target = Player.IsAlive ? Player : FrontMember();
            if (target == null) return;
            int dmg = Math.Max(1, (int)Math.Round(target.Stats.MaxHp * Trial.BlowPct));
            target.Hp = Math.Max(1, target.Hp - dmg);
            target.Guarding = false;
            target.Evading = false;
            var script = Trial.Script;
            events.Add(new BattleEvent
            {
                Turn = Turn, ActorId = enemy.Id, TargetId = target.Id, SkillId = script.BlowName, Kind = EventKind.Attack, Damage = dmg, HitIndex = 0, HitCount = 1,
                TargetHpAfter = target.Hp, Message = enemy.Name + "의 " + script.BlowName + "!", Quip = _finaleBlows++ == 0 && script.FirstBlowQuip != null ? script.FirstBlowQuip : script.BlowQuip
            });
        }

        /// <summary>Finale turns: every living member acts once first, then the 진 strikes once, then only 비류연 acts.</summary>
        private void FinaleTurn(List<BattleEvent> events)
        {
            if (!FinaleStruck)
            {
                Combatant next = null;
                foreach (var m in _party)
                    if (m.IsAlive && !_finaleOpened.Contains(m) && (next == null || m.Stats.Speed > next.Stats.Speed)) next = m;
                if (next != null) { _finaleOpened.Add(next); StartPartyTurn(next); return; }
                var jin = FirstLivingEnemy();
                if (jin != null) { Turn++; EnemyActions++; Annihilate(jin, events); }
                FinaleStruck = true;
            }
            var hero = Player.IsAlive ? Player : null;
            if (hero == null) foreach (var m in _party) if (m.IsAlive) { hero = m; break; }
            if (hero != null && !IsOver) StartPartyTurn(hero);
            else Current = null;
        }

        private void StartPartyTurn(Combatant actor)
        {
            Turn++;
            actor.Guarding = false;
            actor.Evading = false;
            actor.TickCooldowns();
            actor.Qi = Math.Min(actor.Stats.MaxQi, actor.Qi + QiRegenFor(actor.Stats.MaxQi) + actor.QiRegenBonus);
            Current = actor;
        }

        /// <summary>The 진's one blow: every member falls, 비류연 alone is left at 1 HP (no guard, dodge or rescue holds against it).</summary>
        private void Annihilate(Combatant jin, List<BattleEvent> events)
        {
            _partyGuardOwner = null;
            foreach (var m in _party)
            {
                if (!m.IsAlive) continue;
                int left = m == Player ? 1 : 0;
                int dmg = Math.Max(0, m.Hp - left);
                m.Hp = left;
                m.Guarding = false;
                m.Evading = false;
                events.Add(new BattleEvent
                {
                    Turn = Turn, ActorId = jin.Id, TargetId = m.Id, SkillId = FinaleBlowName, Kind = EventKind.Attack, Damage = dmg, HitIndex = 0, HitCount = 1,
                    AllEnemies = true, TargetHpAfter = m.Hp, TargetDefeated = !m.IsAlive, Message = jin.Name + "의 " + FinaleBlowName + "!"
                });
            }
        }

        /// <summary>비류연's 풍신: the 진 is gone at once.</summary>
        private List<BattleEvent> FinaleStrike()
        {
            var actor = Current;
            Current = null;
            PartyActions++;
            var spec = actor.Skills.Find(s => s.Id == Trial.Script.SkillId);
            if (spec == null) { spec = Trial.Script.Skill(); actor.Skills.Add(spec); }
            var events = new List<BattleEvent>();
            var script = Trial.Script;
            if (script.SelfCostPct > 0f) actor.Hp = Math.Max(1, actor.Hp - (int)Math.Round(actor.Stats.MaxHp * script.SelfCostPct));   // 43: the blow 비류연 turns on himself
            foreach (var e in _enemies)
            {
                if (!e.IsAlive) continue;
                int dmg = script.StrikeHarmless ? 0 : e.Hp;   // 43: the lightning goes into the ground, nobody is struck
                e.Hp = 0;
                e.Charging = false; e.ChargeTarget = null; e.AwakeningStrike = null;
                events.Add(new BattleEvent
                {
                    Turn = Turn, ActorId = actor.Id, TargetId = e.Id, SkillId = spec.Id, Kind = EventKind.Attack, Damage = dmg, HitIndex = 0, HitCount = 1,
                    AllEnemies = true, TargetHpAfter = 0, TargetDefeated = true, ActorQiAfter = actor.Qi, ActorSpiritAfter = actor.Spirit,
                    Retreated = script.EnemyLeaves, Message = script.EnemyLeaves ? e.Name + (script.StrikeHarmless ? "(이)가 물러섰다." : "(이)가 쓰러졌다.") : null,
                    Quip = script.StrikeQuip
                });
            }
            return events;
        }
        public int EnemyActions { get; private set; }
        public int TechniqueUses { get; private set; }
        public bool TechniqueSucceeded { get { return Trial != null && Trial.Kind == TrialKind.Technique && TechniqueUses >= Trial.TechniqueUses; } }
        public string RequiredTechniqueId { get { return Trial == null ? null : Trial.TechniqueSequence == null ? Trial.TechniqueId : Trial.TechniqueSequence[Math.Min(TechniqueUses, Trial.TechniqueSequence.Length - 1)]; } }
        public bool TechniqueWindowOpen
        {
            get
            {
                if (Trial == null || !Trial.RequiresCharge) return true;
                string cue = Trial.ResponseCues == null ? null : Trial.ResponseCues[Math.Min(TechniqueUses, Trial.ResponseCues.Length - 1)];
                foreach (var e in _enemies)
                    if (e.IsAlive && e.Charging && (cue == null || e.PeekMove().Name == cue)) return true;
                return false;
            }
        }
        public int LifelinesCut { get; private set; }
        /// <summary>수화관: how many of the living members have attacked in the current unbroken chain.</summary>
        public int Combo { get { int n = 0; foreach (var m in _party) if (m.IsAlive && _chain.Contains(m.Id)) n++; return n; } }
        public bool TrialFailed { get; private set; }
        private bool _harmonyBoost;
        private readonly HashSet<string> _chain = new HashSet<string>();   // who has attacked in the current 수화관 chain
        // Openings belong to a responder and one enemy. Snapshot them before a command so an
        // interruption during that command cannot empower its remaining hits retroactively.
        private readonly Dictionary<Combatant, Combatant> _counterOpenings = new Dictionary<Combatant, Combatant>();
        private readonly Dictionary<Combatant, Combatant> _counterStrikes = new Dictionary<Combatant, Combatant>();

        public Combatant CounterTargetOf(Combatant member)
        {
            Combatant target;
            return member != null && member.IsAlive && _counterOpenings.TryGetValue(member, out target) && target.IsAlive ? target : null;
        }

        private void PrepareCounter(Combatant member)
        {
            var target = CounterTargetOf(member);
            if (target != null) _counterStrikes[member] = target;
            _counterOpenings.Remove(member);
        }

        private void GrantCounter(Combatant member, Combatant enemy, BattleEvent ev)
        {
            if (!member.IsAlive || !enemy.IsAlive) return;
            _counterOpenings[member] = enemy;  // refresh/replace, never stack
            ev.CounterReady = true;
            AddNotice(ev, (ev.Interrupted ? member.Name + "의 파훼!" : member.Name + "의 대응 성공!") + " 반격 기회 — 다음 행동에서 " + enemy.Name + "에게 피해 +25%");
        }

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
        public bool IsOver { get { return !AnyPartyAlive || !AnyEnemyAlive || TrialFailed || SurvivedTrial || TechniqueSucceeded || Turn >= MaxTurns; } }
        public bool PlayerWon { get { return AnyPartyAlive && !TrialFailed && (Trial != null && Trial.Kind == TrialKind.Technique ? TechniqueSucceeded : !AnyEnemyAlive || SurvivedTrial); } }

        /// <summary>One-line trial progress for the UI ("" for a normal fight).</summary>
        public string TrialStatus
        {
            get
            {
                if (Trial == null) return "";
                switch (Trial.Kind)
                {
                    case TrialKind.Survive: return "버텨라 " + Math.Min(EnemyActions, Trial.Limit) + "/" + Trial.Limit;
                    case TrialKind.Technique: return "대응 " + TechniqueUses + "/" + Trial.TechniqueUses + " · " + PartyActions + "/" + Trial.Limit + "행동";
                    case TrialKind.Deadline: return "제한 " + PartyActions + "/" + Trial.Limit + "행동";
                    case TrialKind.Finale: return IsUnavoidable && !FinaleStruck ? "거리 0" : IsDuel ? "기회 " + Math.Max(0, Trial.Limit - DuelMisses) + "/" + Trial.Limit : "";
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
                if (Trial.Kind == TrialKind.Technique) return "정해진 대응을 마치지 못했습니다.";
                if (IsDuel) return "상대의 검이 먼저 닿았습니다.";
                return Trial.Kind == TrialKind.Lifeline ? "생명줄이 끊어졌다!" : "제한 시간이 지났다!";
            }
        }
        public bool IsPlayerTurn { get { return Current != null && !IsOver; } }

        /// <summary>Runs enemy turns until a party member may act. Call once at the start of the fight.</summary>
        public List<BattleEvent> Begin()
        {
            _partyGuardOwner = null;
            _chain.Clear();
            _counterOpenings.Clear();
            _counterStrikes.Clear();
            foreach (var m in _party) m.RescueUsed = false;
            foreach (var m in _party) if (m.Spirit < m.StartSpirit) m.Spirit = m.StartSpirit;
            return RunUntilParty();
        }

        // 태을천강벽 style guards cover the whole party until the caster acts again. A per-unit flag was cleared when each member
        // acted, so allies who moved before the next enemy attack silently lost it.
        private Combatant _partyGuardOwner;
        private int _partyGuardTurns;   // the caster's turns left before the party guard ends

        private void StartPartyGuard(Combatant caster) { _partyGuardOwner = caster; _partyGuardTurns = PartyGuardTurns; }

        /// <summary>Damage factor a guard gives this unit: its own guard (x0.4) or the party guard (x0.7), whichever cuts more; 1 when unguarded.</summary>
        public float GuardFactorFor(Combatant unit)
        {
            float f = 1f;
            if (unit.Guarding) f = GuardDamageFactor;
            if (_partyGuardOwner != null && unit.IsAlive && _party.Contains(unit)) f = Math.Min(f, PartyGuardDamageFactor);
            return f;
        }

        public bool IsGuarded(Combatant unit)
        {
            if (unit.Guarding) return true;
            return _partyGuardOwner != null && unit.IsAlive && _party.Contains(unit);   // survives the caster falling mid-phase
        }

        public bool CanUse(SkillSpec skill) { return IsPlayerTurn && Current.CanUse(skill); }

        /// <summary>기세 a 합공 costs this member (사부의 시련 can lower it).</summary>
        public static int JointCostFor(Combatant actor) { return Math.Max(0, JointSpiritCost - (actor != null ? actor.JointCostCut : 0)); }

        /// <summary>합공: the protagonist (on their turn) strikes together with a living ally for 기세 <see cref="JointCostFor"/>; the ally loses their next action.</summary>
        public bool CanJoint(Combatant partner)
        {
            if (Player.StoryControlled && Player.Id != Inventory.Hero) return false;
            return IsPlayerTurn && Current == _party[0] && partner != null && partner != Current && partner.IsAlive
                && _party.Contains(partner) && Current.Spirit >= JointCostFor(Current) && AnyEnemyAlive
                && !Current.Has(StatusKind.Seal) && !partner.Has(StatusKind.Seal) && !partner.Has(StatusKind.Deviation);
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
            if (cmd.Kind == CommandKind.Beast) return CallBeast(cmd);   // a free action: the turn stays
            if (FinaleReady) return FinaleStrike();
            if (FinaleWindowOpen && cmd.Kind != CommandKind.Finale) WindowsMissed++;   // 20: the mouth was open and 섬뢰창 was not thrown
            if (cmd.Kind == CommandKind.Finale)
            {
                if (!FinaleWindowOpen) throw new InvalidOperationException("The window is closed.");
                return FinaleStrike();
            }
            // 봉혈: a sealed member can only throw the basic attack (the bot and any stale button press fall back to it)
            if (Current.Has(StatusKind.Seal) && ((cmd.Kind == CommandKind.Skill && cmd.Skill != null && cmd.Skill.Id != "basic") || cmd.Kind == CommandKind.Joint))
            {
                var basic = Current.Skills.Find(x => x.Id == "basic") ?? SkillSpec.BasicAttack();
                cmd = PlayerCommand.UseSkill(basic, cmd.Target);
            }
            if (cmd.Kind == CommandKind.Item && (cmd.Item == null || cmd.Item.Kind != ItemKind.Consumable || cmd.Item.MasteryUses > 0)) throw new InvalidOperationException("Not a consumable.");
            // Validate everything before touching any state, so a rejected command leaves the turn intact.
            if (cmd.Kind == CommandKind.Joint && (cmd.Skill == null || !CanJoint(cmd.Partner))) throw new InvalidOperationException("Joint attack unavailable.");
            if (cmd.Kind == CommandKind.Skill && (cmd.Skill == null || !Current.CanUse(cmd.Skill))) throw new InvalidOperationException("Skill unavailable.");
            if (cmd.Kind == CommandKind.Skill && !CanReach(cmd.Skill)) throw new InvalidOperationException("Out of reach.");
            if (cmd.Kind == CommandKind.Joint && EnemiesInRange(cmd.Skill.Range).Count == 0) throw new InvalidOperationException("Out of reach.");
            var actor = Current;
            bool technique = Trial != null && Trial.Kind == TrialKind.Technique && TechniqueWindowOpen
                && cmd.Kind == CommandKind.Skill && cmd.Skill != null && cmd.Skill.Id == RequiredTechniqueId && actor.Skills.Contains(cmd.Skill);
            var events = new List<BattleEvent>();
            Current = null;
            PartyActions++;
            _counterStrikes.Clear();
            PrepareCounter(actor);
            if (cmd.Kind == CommandKind.Joint) PrepareCounter(cmd.Partner);
            _harmonyBoost = false;
            if (Trial != null && Trial.Kind == TrialKind.Harmony)
            {
                bool attacking = cmd.Kind == CommandKind.Joint || (cmd.Kind == CommandKind.Skill && cmd.Skill != null && (cmd.Skill.Effect == SkillEffect.None || cmd.Skill.Effect == SkillEffect.Stun));
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
                    if (actor.GuardStrikeBonus > 0f) actor.GuardStrikeReady = true;   // 사부의 시련: the next attack hits harder
                    events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = actor.Id, SkillId = "guard", Kind = EventKind.Guard, Message = actor.Name + "이(가) 자세를 가다듬는다.", TargetHpAfter = actor.Hp });
                    break;
                case CommandKind.Meditate:
                    {
                        int heal = Healed(actor, (int)Math.Round(actor.Stats.MaxHp * MeditateHealPct));
                        actor.Hp = Math.Min(actor.Stats.MaxHp, actor.Hp + heal);
                        int qi = MeditateQiFor(actor.Stats.MaxQi);
                        actor.Qi = Math.Min(actor.Stats.MaxQi, actor.Qi + qi);
                        if (actor.MeditateSpiritBonus > 0) actor.Spirit = Math.Min(MaxSpirit, actor.Spirit + actor.MeditateSpiritBonus);
                        events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = actor.Id, SkillId = "meditate", Kind = EventKind.Meditate, Heal = heal, TargetHpAfter = actor.Hp, Message = "운기조식. 내공 +" + qi });
                        // 운기 also clears one 상태 이상, the one that hurts most
                        if (actor.Statuses.Count > 0 && _rng.NextDouble() < Statuses.MeditateCureChance) CureOne(actor, actor, events);   // 운기: 40% to shake off one
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
                            heal = Healed(target, (int)Math.Round(target.Stats.MaxHp * item.HealPct));
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
            _counterStrikes.Clear();
            CheckPhases(events);
            if (technique) TechniqueUses++;
            if (Trial != null && Trial.Kind == TrialKind.Technique && !TechniqueSucceeded && (PartyActions >= Trial.Limit || !AnyEnemyAlive)) TrialFailed = true;
            if (Trial != null && (Trial.Kind == TrialKind.Deadline || Trial.Kind == TrialKind.Harmony) && PartyActions >= Trial.Limit && AnyEnemyAlive) TrialFailed = true;
            if (IsOver) return events;
            events.AddRange(RunUntilParty());
            return events;
        }

        // ------------------------------------------------------------------ 영수 (see Beasts)

        /// <summary>The 영수 this fight takes along (set before Begin; null = none, e.g. 승급 비무 and scripted showdowns).</summary>
        public BeastLoadout Beast { get; set; }
        /// <summary>비류연's own turns left before the 영수 can be called again (0 = ready).</summary>
        public int BeastCooldownLeft { get; private set; }
        public int BeastCalls { get; private set; }
        private Combatant _beastUnit;
        /// <summary>The 영수's stand-in unit (for statistics and names), or null without a 영수.</summary>
        public Combatant BeastBody { get { return Beast == null ? null : BeastUnit(); } }

        /// <summary>The 영수 can be called now: 비류연's turn, a 영수 along, off cooldown, and not a scripted showdown.</summary>
        public bool CanCallBeast
        {
            get { return Beast != null && IsPlayerTurn && Current == Player && Player.Id == Inventory.Hero && BeastCooldownLeft <= 0 && !IsFinale; }
        }

        /// <summary>The skill can be used now (a target it needs exists).</summary>
        public bool CanUseBeastSkill(BeastSkill skill)
        {
            if (skill == null || !CanCallBeast || !Beast.Skills.Contains(skill)) return false;
            if (skill.Target == BeastTarget.FrontEnemy) return EnemiesInRange(1).Count > 0;
            if (skill.Target == BeastTarget.Enemy || skill.Target == BeastTarget.AllEnemies) return AnyEnemyAlive;
            return true;
        }

        /// <summary>The 영수's own body for damage: 비류연's stats with a plain crit rate (never on the board, never targeted).</summary>
        private Combatant BeastUnit()
        {
            if (_beastUnit != null) return _beastUnit;
            var stats = Player.Stats;
            stats.CritRate = 0.10f;
            stats.CritDamage = 1.5f;
            _beastUnit = new Combatant(Beasts.UnitPrefix + Beast.Data.Id, Beast.Data.Name, Team.Player, stats, null);
            return _beastUnit;
        }

        /// <summary>
        /// Calls the 영수: a free action on 비류연's turn. The turn stays his (no enemy acts, no party action is counted),
        /// and the 영수 rests for <see cref="BeastLoadout.Cooldown"/> of his own turns.
        /// </summary>
        public List<BattleEvent> CallBeast(PlayerCommand cmd)
        {
            var skill = cmd.BeastSkill;
            if (!CanUseBeastSkill(skill)) throw new InvalidOperationException("The 영수 cannot be called now.");
            var events = new List<BattleEvent>();
            var beast = BeastUnit();
            float f = Beast.PowerFactor;
            BeastCooldownLeft = Beast.Cooldown;
            BeastCalls++;
            _harmonyBoost = false;   // the chain belongs to the party's own attacks
            string head = Beast.Data.Name + "의 " + skill.Name + "!";
            Func<Combatant, string, BattleEvent> support = (who, tag) => new BattleEvent { Turn = Turn, ActorId = beast.Id, TargetId = who.Id, SkillId = skill.Id, Kind = EventKind.Guard, Beast = true, Tag = tag, TargetHpAfter = who.Hp, Message = head };
            switch (skill.Effect)
            {
                case BeastEffect.Spirit:
                    {
                        var who = ResolveAlly(cmd.AllyTarget);
                        int gain = (int)Math.Round(skill.Value * f);
                        who.Spirit = Math.Min(MaxSpirit, who.Spirit + gain);
                        events.Add(support(who, "기세 +" + gain));
                        break;
                    }
                case BeastEffect.Heal:
                case BeastEffect.CleanseHeal:
                    {
                        var who = ResolveAlly(cmd.AllyTarget);
                        int heal = Healed(who, (int)Math.Round(who.Stats.MaxHp * skill.Value * f));
                        who.Hp = Math.Min(who.Stats.MaxHp, who.Hp + heal);
                        events.Add(new BattleEvent { Turn = Turn, ActorId = beast.Id, TargetId = who.Id, SkillId = skill.Id, Kind = EventKind.Heal, Beast = true, Heal = heal, TargetHpAfter = who.Hp, Message = head + " " + who.Name + " 회복." });
                        if (skill.Effect == BeastEffect.CleanseHeal && who.Statuses.Count > 0) { who.Statuses.Clear(); events.Add(Cured(beast, who, StatusKind.None, who.Name + "의 상태 이상이 모두 풀렸다.")); }
                        break;
                    }
                case BeastEffect.Shell:
                    {
                        var who = ResolveAlly(cmd.AllyTarget);
                        who.ShellFactor = Math.Min(who.ShellFactor, skill.Value);
                        events.Add(support(who, "등껍질"));
                        break;
                    }
                case BeastEffect.PartyShell:
                    foreach (var m in _party) if (m.IsAlive) m.ShellFactor = Math.Min(m.ShellFactor, skill.Value);
                    events.Add(support(Player, "바위 진"));
                    break;
                case BeastEffect.Dodge:
                    foreach (var m in _party) if (m.IsAlive) m.DodgeBonus = Math.Max(m.DodgeBonus, skill.Value);
                    events.Add(support(Player, "회피 +" + Math.Round(skill.Value * 100f) + "%"));
                    break;
                case BeastEffect.PartyQi:
                    foreach (var m in _party)
                    {
                        if (!m.IsAlive) continue;
                        m.Qi = Math.Min(m.Stats.MaxQi, m.Qi + (int)Math.Round(m.Stats.MaxQi * skill.Value * f));
                    }
                    events.Add(support(Player, "내공 회복"));
                    foreach (var m in _party) if (m.IsAlive) CureOne(beast, m, events);
                    break;
                default:
                    BeastStrike(beast, skill, cmd.Target, f, events);
                    break;
            }
            CheckPhases(events);
            return events;
        }

        private void BeastStrike(Combatant beast, BeastSkill skill, Combatant wanted, float f, List<BattleEvent> events)
        {
            var spec = new SkillSpec { Id = skill.Id, Name = skill.Name, Power = skill.Power * f, Tuned = true, Hits = Math.Max(1, skill.Hits),
                Range = skill.Target == BeastTarget.FrontEnemy ? 1 : 5, AllEnemies = skill.Target == BeastTarget.AllEnemies, Ranged = true };
            var targets = new List<Combatant>();
            if (spec.AllEnemies) targets = EnemiesInRange(spec.Range);
            else { var t = ResolveTarget(wanted, spec); if (t != null) targets.Add(t); }
            beast.StealthCrit = skill.SureCrit;
            bool first = true;
            for (int h = 0; h < spec.Hits; h++)
                foreach (var t in targets)
                {
                    if (!t.IsAlive) continue;
                    var ev = DealHit(beast, t, spec, h, spec.Hits, skill.Effect == BeastEffect.Paralyze && h == spec.Hits - 1, events);
                    ev.Beast = true;
                    if (first) { ev.Message = Beast.Data.Name + "의 " + skill.Name + "!"; first = false; }
                }
            beast.StealthCrit = false;
            foreach (var t in targets)
            {
                if (!t.IsAlive) continue;
                switch (skill.Effect)
                {
                    case BeastEffect.Poison: Inflict(beast, t, StatusKind.Poison, events); break;
                    case BeastEffect.Bleed: Inflict(beast, t, StatusKind.Bleed, events); break;
                    case BeastEffect.Roar:
                        if (t.UsesResources) t.Spirit = Math.Max(0, t.Spirit - (int)Math.Round(skill.Value));
                        break;
                    case BeastEffect.Scout:
                        events.Add(new BattleEvent { Turn = Turn, ActorId = beast.Id, TargetId = t.Id, SkillId = skill.Id, Kind = EventKind.Guard, Beast = true, Tag = "정찰", TargetHpAfter = t.Hp, Message = ScoutReport(t), Notice = ScoutReport(t) });
                        break;
                }
            }
            if (skill.Effect == BeastEffect.Roar)
            {
                foreach (var m in _party) if (m.IsAlive) m.Spirit = Math.Min(MaxSpirit, m.Spirit + Beasts.RoarPartySpirit);
                events.Add(new BattleEvent { Turn = Turn, ActorId = beast.Id, TargetId = Player.Id, SkillId = skill.Id, Kind = EventKind.Guard, Beast = true, Tag = "기세 +" + Beasts.RoarPartySpirit, TargetHpAfter = Player.Hp, Message = "포효에 적의 기세가 꺾이고 일행의 기세가 오른다." });
            }
        }

        /// <summary>우뢰 정찰: what the enemy will do next and what it carries.</summary>
        public static string ScoutReport(Combatant enemy)
        {
            var move = enemy.PeekMove();
            string next = enemy.Charging ? "준비한 강타" + (enemy.ChargeTarget != null ? " → " + enemy.ChargeTarget.Name : "")
                : move.Charge ? move.Name + " (강타 준비)" : move.Name;
            if (enemy.AwakeningStrike != null) next = "각성:강타";
            if (enemy.Stunned) next = "행동 불능";
            var sb = new System.Text.StringBuilder("정찰 — " + enemy.Name + "의 다음 행동: " + next);
            if (enemy.Statuses.Count > 0)
            {
                sb.Append(" · 상태 이상: ");
                for (int i = 0; i < enemy.Statuses.Count; i++) sb.Append((i > 0 ? ", " : "") + Statuses.Name(enemy.Statuses[i].Kind) + " " + enemy.Statuses[i].Turns + "턴");
            }
            return sb.ToString();
        }

        /// <summary>The ally a heal goes to: the player's pick if that member is alive and in the party, otherwise the most wounded one.</summary>
        private Combatant ResolveAlly(Combatant wanted)
        {
            return wanted != null && wanted.IsAlive && _party.Contains(wanted) ? wanted : MostWounded();
        }

        private void UseSkill(Combatant actor, PlayerCommand cmd, List<BattleEvent> events)
        {
            var skill = cmd.Skill;
            actor.Qi -= actor.QiCostOf(skill);
            if (skill.Id != "basic" && actor.Has(StatusKind.BloodBackflow)) StatusHurt(actor, StatusKind.BloodBackflow, events);   // 혈맥 역류
            if (!actor.IsAlive) return;
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
                    StartPartyGuard(actor);
                    events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = actor.Id, SkillId = skill.Id, Kind = EventKind.Guard, TargetHpAfter = actor.Hp, Message = actor.Name + "의 " + skill.Name + "! 모두가 방어 태세." });
                    CureOne(actor, actor, events);   // 태을천강벽 steadies its caster: one 상태 이상 of its own goes
                    return;
                case SkillEffect.Stealth:
                    actor.Stealthed = true; actor.StealthCrit = true;
                    events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = actor.Id, SkillId = skill.Id, Kind = EventKind.Evade, Tag = "은신", TargetHpAfter = actor.Hp, Message = actor.Name + "의 " + skill.Name + "! 그림자 속으로 사라졌다." });
                    return;
                case SkillEffect.HealParty:
                    foreach (var m in _party)
                    {
                        if (!m.IsAlive) continue;
                        int heal = Healed(m, (int)Math.Round(m.Stats.MaxHp * skill.HealPct));
                        m.Hp = Math.Min(m.Stats.MaxHp, m.Hp + heal);
                        events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = m.Id, SkillId = skill.Id, Kind = EventKind.Heal, Heal = heal, TargetHpAfter = m.Hp, Message = actor.Name + "의 " + skill.Name + "! 일행 모두 회복." });
                    }
                    return;
                case SkillEffect.Cover:
                    actor.Covering = true;
                    events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = actor.Id, SkillId = skill.Id, Kind = EventKind.Guard, Tag = "대신 막기", TargetHpAfter = actor.Hp, Message = actor.Name + "의 " + skill.Name + "! 일행 앞을 막아선다." });
                    return;
                case SkillEffect.CounterStance:
                    actor.CounterStance = true;
                    events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = actor.Id, SkillId = skill.Id, Kind = EventKind.Guard, Tag = "반격 태세", TargetHpAfter = actor.Hp, Message = actor.Name + "의 " + skill.Name + "! 빈틈을 노리는 검막." });
                    return;
                case SkillEffect.PartySpirit:
                    foreach (var m in _party) if (m.IsAlive) m.Spirit = Math.Min(MaxSpirit, m.Spirit + PartySpiritGain);
                    events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = actor.Id, SkillId = skill.Id, Kind = EventKind.Guard, Tag = "기세 +" + PartySpiritGain, TargetHpAfter = actor.Hp, Message = actor.Name + "의 " + skill.Name + "! 일행의 기세가 오른다." });
                    return;
                case SkillEffect.Ward:
                    foreach (var m in _party) if (m.IsAlive) { m.WardTurns = WardTurns; CureOne(actor, m, events); }   // 은하밀밀 also lifts one 상태 이상 from each
                    events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = actor.Id, SkillId = skill.Id, Kind = EventKind.Guard, Tag = "결계", TargetHpAfter = actor.Hp, Message = actor.Name + "의 " + skill.Name + "! 일행이 상태 이상에 걸리지 않는다." });
                    return;
                case SkillEffect.Focus:
                    actor.Focused = true;
                    events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = actor.Id, SkillId = skill.Id, Kind = EventKind.Guard, Tag = "집중", TargetHpAfter = actor.Hp, Message = actor.Name + "의 " + skill.Name + "! 다음 일격에 모든 것을 모은다." });
                    return;
                case SkillEffect.HealAlly:
                    {
                        var weakest = ResolveAlly(cmd.AllyTarget);
                        int heal = Healed(weakest, (int)Math.Round(weakest.Stats.MaxHp * skill.HealPct));
                        weakest.Hp = Math.Min(weakest.Stats.MaxHp, weakest.Hp + heal);
                        events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = weakest.Id, SkillId = skill.Id, Kind = EventKind.Heal, Heal = heal, TargetHpAfter = weakest.Hp, Message = actor.Name + "의 " + skill.Name + "! " + weakest.Name + " 회복." });
                        return;
                    }
                case SkillEffect.Cleanse:
                    {
                        // the chosen ally, else the one carrying the most statuses (else the most wounded)
                        var who = cmd.AllyTarget != null && cmd.AllyTarget.IsAlive && _party.Contains(cmd.AllyTarget) ? cmd.AllyTarget : MostAfflicted() ?? MostWounded();
                        int heal = Healed(who, (int)Math.Round(who.Stats.MaxHp * skill.HealPct));
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
                    if (t.IsAlive && Rolls(t, skill.Inflict, skill.InflictChance)) Inflict(actor, t, skill.Inflict, events);
            if (skill.SpiritCost == 0) actor.Spirit = Math.Min(MaxSpirit, actor.Spirit + SpiritOnDealt + actor.SpiritDealtBonus);
            if (targets.Count > 0) { actor.Focused = false; actor.StealthCrit = false; actor.GuardStrikeReady = false; }   // 유성보·신은술 (and a 시련 guard strike) are spent on this attack
        }

        private BattleEvent DealHit(Combatant actor, Combatant t, SkillSpec skill, int h, int hits, bool stun, List<BattleEvent> events, bool triggerBleed = true)
        {
            bool crit = actor.StealthCrit || _rng.NextDouble() < actor.Stats.CritRate;
            int dmg = BattleEngine.CalcDamage(actor.Stats, t.Stats, skill, crit);
            if (actor.Focused) dmg = (int)Math.Round(dmg * FocusDamageFactor);
            // the guard strike waits for the unit's own attack (not a 반격 태세 counter)
            float run = actor.DamageDealtMul * (actor.GuardStrikeReady && skill.Id != "counter_stance" ? 1f + actor.GuardStrikeBonus : 1f) * (t.Statuses.Count > 0 ? 1f + actor.VsAfflictedBonus : 1f);
            if (run != 1f) dmg = (int)Math.Round(dmg * run);   // 사부의 시련 가르침
            if (actor.Has(StatusKind.Chill)) dmg = (int)Math.Round(dmg * ChillAttackFactor);
            if (actor.Has(StatusKind.Disarm)) dmg = (int)Math.Round(dmg * Statuses.DisarmFactor);
            int illusionBack = actor.Has(StatusKind.Illusion) ? (int)Math.Round(dmg * Statuses.IllusionSelfShare) : 0;   // 환각: part of the blow lands on the attacker
            dmg -= illusionBack;
            if (_harmonyBoost) dmg = (int)Math.Round(dmg * HarmonyDamageFactor);
            Combatant counterTarget;
            bool counter = _counterStrikes.TryGetValue(actor, out counterTarget) && counterTarget == t;
            if (counter) dmg = (int)Math.Round(dmg * CounterDamageFactor);
            bool announceCounter = counter && h == 0;
            if (IsUnavoidable && t.Team == Team.Enemy) dmg = 0;   // 46: no blow reaches the 심도
            if (IsWindow && t.Team == Team.Enemy) dmg = Math.Max(1, (int)Math.Round(dmg * Trial.ArmorFactor));   // 20: the old scales throw most of it back
            t.Hp = Math.Max(0, t.Hp - dmg);
            if (IsFinale && t.Team == Team.Enemy && t.Hp < 1) t.Hp = 1;   // the 진 cannot be worn down
            if (t.UsesResources && t.IsAlive) t.Spirit = Math.Min(MaxSpirit, t.Spirit + SpiritOnEnemyHit);
            if (actor.Lifesteal > 0f && actor.IsAlive) actor.Hp = Math.Min(actor.Stats.MaxHp, actor.Hp + Healed(actor, (int)Math.Round(dmg * actor.Lifesteal)));
            var ev = new BattleEvent
            {
                Turn = Turn, ActorId = actor.Id, TargetId = t.Id, SkillId = skill.Id, Kind = EventKind.Attack,
                Damage = dmg, IsCrit = crit, HitIndex = h, HitCount = hits, Harmony = _harmonyBoost, Counter = counter, AllEnemies = skill.AllEnemies, Ranged = skill.Ranged
            };
            if (IsUnavoidable && t.Team == Team.Enemy) ev.Quip = Trial.Script.NoHarmQuip;
            if (IsWindow && t.Team == Team.Enemy)
            {
                ev.Quip = _armorNoted ? Trial.Script.NoHarmQuip : Trial.Script.FirstNoHarmQuip;
                if (!_armorNoted) ev.Lines = Trial.Script.ArmorLines;   // the first bounce: 나예린 explains the scales
                _armorNoted = true;
            }
            if (t.RetreatHpPct > 0f && t.Hp <= t.Stats.MaxHp * t.RetreatHpPct)
            {
                // Story boss: it escapes rather than dying. The fight ends as a win but nobody is slain.
                t.Hp = 0;
                ev.Retreated = true;
                ev.Message = t.Name + "(이)가 물러난다!";
            }
            else if (t.HoldsAtOneHp && t.Hp <= 0) Leave(t, ev);
            if (!t.IsAlive) { t.Charging = false; t.ChargeTarget = null; t.AwakeningStrike = null; }
            else if (stun) ApplyStun(t, ev);
            if (ev.Interrupted && _party.Contains(actor)) GrantCounter(actor, t, ev);   // a 영수 opens no counter for anyone
            ev.TargetHpAfter = ev.Retreated && t.HoldsAtOneHp ? 1 : t.Hp;
            ev.TargetDefeated = !t.IsAlive;
            if (announceCounter) AddNotice(ev, actor.Name + "의 반격! 피해 +25%");
            if (h == 0 && t.IsAlive && ChillStruck(t)) ApplyStun(t, ev, true);   // 한기: the third attack taken freezes it (once per attack command), bosses too
            events.Add(ev);
            if (illusionBack > 0 && actor.IsAlive) SelfHurt(actor, illusionBack, StatusKind.Illusion, events);
            if (triggerBleed && h == 0 && t.IsAlive && t.Has(StatusKind.Bleed)) StatusHurt(t, StatusKind.Bleed, events);   // once per attack command
            if (!t.IsAlive && ev.CounterReady)
            {
                ev.Notice = null;   // the opening died with the enemy
                _counterOpenings.Remove(actor);
                ev.CounterReady = false;
            }
            return ev;
        }

        private void UseJoint(Combatant actor, PlayerCommand cmd, List<BattleEvent> events)
        {
            var spec = cmd.Skill;
            var partner = cmd.Partner;
            var t = ResolveTarget(cmd.Target, spec);
            if (t == null) return;   // nothing to hit: nothing is paid
            actor.Spirit -= JointCostFor(actor);
            partner.Gauge -= GaugeThreshold;   // the partner spends their next action on this strike

            var first = DealHit(actor, t, spec, 0, 1, false, events, false);
            first.Joint = true;
            bool bled = t.IsAlive && t.Has(StatusKind.Bleed);
            if (bled) StatusHurt(t, StatusKind.Bleed, events);
            var next = t.IsAlive ? t : ResolveTarget(null, spec);
            if (next != null) DealHit(partner, next, spec, 0, 1, spec.Effect == SkillEffect.Stun, events, !bled).Joint = true;
            if (next != null && spec.Inflict != StatusKind.None && next.IsAlive && Rolls(next, spec.Inflict, spec.InflictChance)) Inflict(partner, next, spec.Inflict, events);   // 백혼합벽: 한기
            actor.Focused = false; actor.StealthCrit = false; partner.Focused = false; partner.StealthCrit = false;
            actor.GuardStrikeReady = false; partner.GuardStrikeReady = false;

            switch (spec.Effect)
            {
                case SkillEffect.GuardParty:
                    StartPartyGuard(partner);
                    events.Add(new BattleEvent { Turn = Turn, ActorId = partner.Id, TargetId = partner.Id, SkillId = spec.Id, Kind = EventKind.Guard, Joint = true, TargetHpAfter = partner.Hp, Message = spec.Name + "! \ubaa8\ub450\uac00 \ubc29\uc5b4 \ud0dc\uc138." });
                    break;
                case SkillEffect.HealAlly:
                    {
                        var weakest = MostWounded();
                        int heal = Healed(weakest, (int)Math.Round(weakest.Stats.MaxHp * spec.HealPct));
                        weakest.Hp = Math.Min(weakest.Stats.MaxHp, weakest.Hp + heal);
                        events.Add(new BattleEvent { Turn = Turn, ActorId = partner.Id, TargetId = weakest.Id, SkillId = spec.Id, Kind = EventKind.Heal, Joint = true, Heal = heal, TargetHpAfter = weakest.Hp, Message = spec.Name + "! " + weakest.Name + " \ud68c\ubcf5." });
                        break;
                    }
            }
        }

        private void ApplyStun(Combatant target, BattleEvent ev, bool freeze = false)
        {
            string lost = null;
            if (target.AwakeningStrike != null)
            {
                target.AwakeningStrike = null;
                ev.Interrupted = true;
                lost = "각성:강타";
            }
            else if (target.Charging && !IsWindow)   // 20: a stun cannot shut the opened mouth (the showdown's window)
            {
                // Interrupt the wind-up: the heavy move that follows is skipped.
                target.Charging = false;
                target.ChargeTarget = null;
                target.MoveIndex++;
                ev.Interrupted = true;
                lost = "준비하던 강타";
            }
            bool stunned = !target.IsBoss || freeze;   // 점혈 never stuns a boss; 한기's freeze does
            if (stunned)
            {
                target.Stunned = true;
                target.StunReason = freeze ? "빙결" : "점혈";
                ev.Stunned = true;
            }
            // what the player should notice: the banner text of the blow
            if (freeze) ev.Tag = "빙결";
            if (freeze || lost != null)
                AddNotice(ev, target.Name + (freeze ? "이(가) 얼어붙었다! 다음 차례를 잃는다" : stunned ? "의 혈도가 막혔다! 다음 차례를 잃는다" : "의 기세가 끊겼다")
                    + (lost != null ? " (" + lost + " 취소)" : ""));
        }

        // ------------------------------------------------------------------ 상태 이상

        private static BattleEvent Cured(Combatant actor, Combatant target, StatusKind kind, string message)
        {
            return new BattleEvent { ActorId = actor.Id, TargetId = target.Id, SkillId = "status", Kind = EventKind.Status, Status = kind, StatusCured = true, TargetHpAfter = target.Hp, Message = message };
        }

        /// <summary>Adds a banner line to the event (several reasons on one blow are joined).</summary>
        private static void AddNotice(BattleEvent ev, string line)
        {
            ev.Notice = string.IsNullOrEmpty(ev.Notice) ? line : ev.Notice + "\n" + line;
        }

        /// <summary>Does a status take hold: 주화입마 always at its own 20%, the rest at the source's chance; 강력한 보스 x0.5, 정말 강력한 보스 x0.2.</summary>
        private bool Rolls(Combatant target, StatusKind kind, float chance)
        {
            if (kind == StatusKind.Deviation) chance = Statuses.DeviationChance;
            chance *= Statuses.ChanceFactor(target);
            return chance >= 1f || _rng.NextDouble() < chance;
        }

        /// <summary>Lifts one 상태 이상 (the one that hurts most) from the unit.</summary>
        private void CureOne(Combatant actor, Combatant who, List<BattleEvent> events)
        {
            foreach (var k in Statuses.CurePriority)
                if (who.RemoveStatus(k)) { events.Add(Cured(actor, who, k, who.Name + "의 " + Statuses.Name(k) + "이(가) 풀렸다.")); return; }
        }

        /// <summary>주화입마: the unit burns its own blood and throws a double basic attack at a random ally of its own side (none: the turn is wasted).</summary>
        private void DeviationTurn(Combatant actor, List<BattleEvent> events)
        {
            if (actor.Charging) { actor.Charging = false; actor.ChargeTarget = null; actor.MoveIndex++; }   // the wound-up blow is lost
            StatusHurt(actor, StatusKind.Deviation, events);
            if (!actor.IsAlive) return;
            var side = actor.Team == Team.Player ? _party : _enemies;
            var allies = side.FindAll(x => x != actor && x.IsAlive);
            if (allies.Count == 0)
            {
                events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = actor.Id, SkillId = "deviation", Kind = EventKind.StunSkip, Tag = "주화입마", TargetHpAfter = actor.Hp, Message = actor.Name + "이(가) 주화입마에 빠져 허공에 손을 휘두른다! (차례를 넘깁니다)" });
                return;
            }
            var victim = allies[Math.Min(allies.Count - 1, (int)(_rng.NextDouble() * allies.Count))];
            var spec = new SkillSpec { Id = "deviation", Name = "주화입마", Power = Statuses.DeviationPower };
            int dmg = BattleEngine.CalcDamage(actor.Stats, victim.Stats, spec, false);
            if (actor.Has(StatusKind.Chill)) dmg = (int)Math.Round(dmg * ChillAttackFactor);
            if (victim.DamageTakenMul != 1f) dmg = Math.Max(1, (int)Math.Round(dmg * victim.DamageTakenMul));
            victim.Hp = Math.Max(0, victim.Hp - dmg);
            if (IsFinale && victim.Team == Team.Enemy && victim.Hp < 1) victim.Hp = 1;
            var ev = new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = victim.Id, SkillId = "deviation", Kind = EventKind.Attack, Tag = "주화입마", Damage = dmg, HitCount = 1, Message = actor.Name + "이(가) 주화입마에 빠져 " + victim.Name + "을(를) 공격한다!", Notice = actor.Name + " — 주화입마! 제어를 잃고 " + victim.Name + "을(를) 공격한다" };
            if (victim.Hp == 0 && victim.Team == Team.Player) TryRescue(victim, ev);
            if (victim.RetreatHpPct > 0f && victim.Hp <= victim.Stats.MaxHp * victim.RetreatHpPct) { victim.Hp = 0; ev.Retreated = true; }
            else if (victim.HoldsAtOneHp && victim.Hp <= 0) Leave(victim, ev);
            if (!victim.IsAlive) { victim.Charging = false; victim.ChargeTarget = null; victim.AwakeningStrike = null; }
            ev.TargetHpAfter = ev.Retreated && victim.HoldsAtOneHp ? 1 : victim.Hp;
            ev.TargetDefeated = !victim.IsAlive;
            events.Add(ev);
        }

        /// <summary>중독 halves every heal the unit receives.</summary>
        private static int Healed(Combatant who, int heal)
        {
            if (who != null && who.HealTakenBonus > 0f) heal = (int)Math.Round(heal * (1f + who.HealTakenBonus));   // 사부의 시련
            return who != null && who.Has(StatusKind.Poison) ? (int)Math.Round(heal * Statuses.PoisonHealFactor) : heal;
        }

        /// <summary>한기: counts an attack taken while chilled; true on the one that freezes the unit (the count starts again).</summary>
        private static bool ChillStruck(Combatant t)
        {
            if (!t.Has(StatusKind.Chill)) return false;
            if (++t.ChillHits < Statuses.ChillFreezeHits) return false;
            t.ChillHits = 0;
            return true;
        }

        private void Inflict(Combatant source, Combatant target, StatusKind kind, List<BattleEvent> events)
        {
            if (kind == StatusKind.None || !target.IsAlive) return;
            if (target.WardTurns > 0) return;   // 은하밀밀
            if (!target.AddStatus(kind, Statuses.TurnsFor(target, kind), source.Id)) return;   // already carries a status: nothing new takes hold
            events.Add(new BattleEvent { Turn = Turn, ActorId = source.Id, TargetId = target.Id, SkillId = "status", Kind = EventKind.Status, Status = kind, TargetHpAfter = target.Hp, Message = target.Name + ": " + Statuses.Name(kind) + "!" });
        }

        /// <summary>중독·화상 at the unit's turn and 출혈 when struck: a share of max HP. Can fell the unit (a bonded companion may still save the protagonist; a story boss flees instead).</summary>
        private void StatusHurt(Combatant unit, StatusKind kind, List<BattleEvent> events)
        {
            SelfHurt(unit, Statuses.DotDamage(unit, kind), kind, events);
        }

        /// <summary>HP a unit loses to a 상태 이상 (중독·화상·출혈·혈맥 역류·주화입마, or its own blow coming back under 환각).</summary>
        private void SelfHurt(Combatant unit, int dmg, StatusKind kind, List<BattleEvent> events)
        {
            if (dmg <= 0 || !unit.IsAlive) return;
            if (unit.DamageTakenMul != 1f) dmg = Math.Max(1, (int)Math.Round(dmg * unit.DamageTakenMul));   // 사부의 시련: every damage taken
            unit.Hp = Math.Max(0, unit.Hp - dmg);
            if (IsFinale && unit.Team == Team.Enemy && unit.Hp < 1) unit.Hp = 1;
            var ev = new BattleEvent { Turn = Turn, ActorId = unit.Id, TargetId = unit.Id, SkillId = "status", Kind = EventKind.Status, Status = kind, Damage = dmg, Message = unit.Name + ": " + Statuses.Name(kind) + " -" + dmg, SourceId = kind == StatusKind.Poison || kind == StatusKind.Burn || kind == StatusKind.Bleed ? unit.StatusSource(kind) : null };
            if (unit.Hp == 0 && unit.Team == Team.Player) TryRescue(unit, ev);
            if (unit.RetreatHpPct > 0f && unit.Hp <= unit.Stats.MaxHp * unit.RetreatHpPct)
            {
                unit.Hp = 0;
                ev.Retreated = true;
                ev.Message = unit.Name + "(이)가 물러난다!";
            }
            else if (unit.HoldsAtOneHp && unit.Hp <= 0) Leave(unit, ev);
            if (!unit.IsAlive) { unit.Charging = false; unit.ChargeTarget = null; unit.AwakeningStrike = null; }
            ev.TargetHpAfter = ev.Retreated && unit.HoldsAtOneHp ? 1 : unit.Hp;
            ev.TargetDefeated = !unit.IsAlive;
            events.Add(ev);
        }

        /// <summary>A boss that holds at 1 HP: the blow that would fell it ends the fight with its exit lines instead (shown at 1 HP).</summary>
        private static void Leave(Combatant unit, BattleEvent ev)
        {
            unit.Hp = 0;
            ev.Retreated = true;
            ev.Lines = unit.RetreatLines;
            ev.Message = unit.Name + "(이)가 물러난다!";
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

        private static int GaugeGain(Combatant u) { return GaugeGain(u, u.Has(StatusKind.Chill)); }

        private static int GaugeGain(Combatant u, bool chilled)
        {
            float factor = chilled ? (u.IsBoss ? Statuses.BossChillSpeed : Statuses.ChillSpeed) : 1f;
            return Math.Max(1, (int)Math.Round(u.Stats.Speed * factor));
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
            if (IsFinale && Trial.Style == FinaleStyle.PartyFirst && !IsOver) { FinaleTurn(events); return events; }
            if (IsDuel && !IsOver)   // 24: no gauge race, the coin decides; 비류연 holds the turn until the strike
            {
                FinaleStruck = true;
                if (Player.IsAlive) StartPartyTurn(Player);
                else Current = null;
                return events;
            }
            while (!IsOver)
            {
                var actor = NextActor();
                actor.Gauge -= GaugeThreshold;
                Turn++;
                actor.Guarding = false;
                actor.Evading = false;
                actor.Stealthed = false; actor.Covering = false; actor.CounterStance = false;   // stances last until the unit's next turn
                actor.DodgeBonus = 0f;   // 학의 춤
                if (actor.WardTurns > 0) actor.WardTurns--;
                // A party-wide guard lasts for three of its caster's turns; if the caster fell, until the next ally acts.
                if (actor == _partyGuardOwner && --_partyGuardTurns <= 0) _partyGuardOwner = null;
                if (_partyGuardOwner != null && !_partyGuardOwner.IsAlive && actor.Team == Team.Player) _partyGuardOwner = null;

                bool wounded = actor.Has(StatusKind.InnerWound);   // 내상 this turn (read before the statuses tick down)
                TickStatuses(actor, events);
                if (!actor.IsAlive) continue;   // fell to 중독·화상 before acting
                if (actor.Team == Team.Enemy) CheckPhases(events);   // 중독·화상 can push a boss into its next phase

                if (actor.Team == Team.Player && !actor.Stunned && actor.Has(StatusKind.Deviation))
                {
                    DeviationTurn(actor, events);   // 주화입마: the member is not the player's to command this turn
                    continue;
                }
                if (actor.Team == Team.Player && actor.Stunned)
                {
                    // 한기로 얼어붙은 아군: this turn is lost (its cooldowns and 내공 do not move either)
                    actor.Stunned = false;
                    events.Add(new BattleEvent { Turn = Turn, ActorId = actor.Id, TargetId = actor.Id, SkillId = "stun", Kind = EventKind.StunSkip, Tag = "빙결", TargetHpAfter = actor.Hp, Message = actor.Name + "은(는) 얼어붙어 움직이지 못한다! (차례를 넘깁니다)" });
                    continue;
                }
                if (actor.Team == Team.Player)
                {
                    // 46·43: enough of the 심도 (three of the enemy's blows, or 비류연 below the trigger) and the scene begins
                    // (counted in the enemy's blows, so a party of four still feels the 심도 a few times before the scene)
                    if (IsUnavoidable && !FinaleStruck && (EnemyActions >= Trial.FinaleTurns || Player.Hp <= Player.Stats.MaxHp * Trial.TriggerHpPct)) FinaleStruck = true;
                    actor.TickCooldowns();
                    if (actor == Player && BeastCooldownLeft > 0) BeastCooldownLeft--;
                    if (!wounded) actor.Qi = Math.Min(actor.Stats.MaxQi, actor.Qi + QiRegenFor(actor.Stats.MaxQi) + actor.QiRegenBonus);
                    Current = actor;
                    return events;
                }
                if (IsUnavoidable) UnavoidableBlow(actor, events);
                else EnemyAct(actor, events, wounded);
            }
            Current = null;
            return events;
        }

        /// <summary>The living party member an enemy hits by default: the front line (ties: the more wounded one, then list order).</summary>
        private Combatant FrontMember()
        {
            Combatant best = null;
            int bestDepth = int.MaxValue;
            bool hideStealthed = AnyVisibleMember;
            foreach (var m in _party)
            {
                if (!m.IsAlive || (hideStealthed && m.Stealthed)) continue;
                int d = Depth(m);
                if (best == null || d < bestDepth || (d == bestDepth && (float)m.Hp / m.Stats.MaxHp < (float)best.Hp / best.Stats.MaxHp)) { best = m; bestDepth = d; }
            }
            return best;
        }

        /// <summary>Some living member is not hidden by 신은술 (if every one is, nobody can be hidden).</summary>
        private bool AnyVisibleMember { get { foreach (var m in _party) if (m.IsAlive && !m.Stealthed) return true; return false; } }

        /// <summary>원의 곤: a covering member takes the blow meant for anyone else; 신은술 sends a blow meant for the hidden one to the front line.</summary>
        private Combatant Redirect(Combatant target)
        {
            foreach (var m in _party) if (m.IsAlive && m.Covering) return m;
            if (target != null && target.Stealthed && AnyVisibleMember) return FrontMember();
            return target;
        }

        /// <summary>The member a 저격 move picks: the back-most one within reach (ties: the more wounded one).</summary>
        private Combatant BackMember(int reach)
        {
            Combatant far = null; int farDepth = -1;
            bool hideStealthed = AnyVisibleMember;
            foreach (var m in _party)
            {
                if (!m.IsAlive || (hideStealthed && m.Stealthed)) continue;
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
                    // A second threshold replaces a pending first strike; it never queues an extra action.
                    e.AwakeningStrike = BossPhases.CreateAwakening(e.Moves, e.PhaseIndex);
                    events.Add(new BattleEvent
                    {
                        Turn = Turn, ActorId = e.Id, TargetId = e.Id, SkillId = "phase", Kind = EventKind.Phase, HitIndex = e.PhaseIndex,
                        Lines = phase.Lines, TargetHpAfter = e.Hp, Message = e.Name + "의 기세가 달라졌다!"
                    });
                }
            }
        }

        private void EnemyAct(Combatant enemy, List<BattleEvent> events, bool wounded = false)
        {
            EnemyActions++;
            if (enemy.Stunned)
            {
                enemy.Stunned = false;
                string why = enemy.StunReason == "빙결" ? "얼어붙어" : "몸이 굳어";
                events.Add(new BattleEvent { Turn = Turn, ActorId = enemy.Id, TargetId = enemy.Id, SkillId = "stun", Kind = EventKind.StunSkip, Tag = enemy.StunReason, TargetHpAfter = enemy.Hp, Message = enemy.Name + "은(는) " + why + " 움직이지 못한다! (차례를 넘깁니다)" });
                enemy.StunReason = null;
                return;
            }
            // 운기 해독: a boss or veteran may spend the turn shaking off what ails it
            if (enemy.Statuses.Count > 0 && enemy.CleansesLeft > 0 && _rng.NextDouble() < enemy.CleanseChance)
            {
                enemy.CleansesLeft--;
                enemy.Statuses.Clear();
                enemy.ChillHits = 0;
                var cleansed = Cured(enemy, enemy, StatusKind.None, enemy.Name + "이(가) 운기하여 상태 이상을 몰아냈다!");
                cleansed.Notice = enemy.Name + " — 운기 해독! 이번 차례를 써서 상태 이상을 풀었다";
                events.Add(cleansed);
                return;
            }
            if (enemy.Has(StatusKind.Deviation)) { DeviationTurn(enemy, events); return; }

            if (enemy.UsesResources && !wounded) enemy.Qi = Math.Min(enemy.Stats.MaxQi, enemy.Qi + EnemyQiRegen);
            bool sealedTurn = enemy.Has(StatusKind.Seal);
            if (sealedTurn && enemy.Charging)
            {
                // 봉혈: the wound-up blow cannot be thrown; the script moves past it
                enemy.Charging = false;
                enemy.ChargeTarget = null;
                enemy.MoveIndex++;
            }
            var move = sealedTurn ? EnemyMove.Plain : enemy.PeekMove();
            bool awakening = !sealedTurn && move.Awakening;

            if (!sealedTurn && move.Charge)
            {
                enemy.MoveIndex++;
                enemy.Charging = true;
                enemy.ChargeTarget = FrontMember();
                // The message names the wind-up but not its victim: who it is aimed at stays hidden from the player.
                events.Add(new BattleEvent { Turn = Turn, ActorId = enemy.Id, TargetId = enemy.ChargeTarget != null ? enemy.ChargeTarget.Id : enemy.Id, SkillId = move.Name, Kind = EventKind.Charge, TargetHpAfter = enemy.Hp, Message = enemy.Name + "이(가) " + move.Name + "!" });
                return;
            }

            var target = !awakening && enemy.Charging && enemy.ChargeTarget != null && enemy.ChargeTarget.IsAlive ? enemy.ChargeTarget : FrontMember();
            if (target == null) return;
            // A 저격 move reaches every line and picks the back-most party member; everything else hits the front line.
            if (!sealedTurn && move.Snipe && !enemy.Charging && !awakening)
            {
                var far = BackMember(Math.Max(enemy.Range, SnipeReach));
                if (far != null) target = far;
            }
            target = Redirect(target);

            bool heavy = !sealedTurn && (awakening || enemy.Charging);
            if (sealedTurn) { }   // 봉혈: a plain blow; the script (and any 각성 strike) waits
            else if (awakening) enemy.AwakeningStrike = null;
            else
            {
                enemy.MoveIndex++;
                enemy.Charging = false;
                enemy.ChargeTarget = null;
            }

            int hits = Math.Max(1, move.Hits);
            float power = move.Power * move.DamageFactor * enemy.MovePowerFactor;   // story rank
            bool burst = false, dry = false;
            bool backflow = enemy.Has(StatusKind.QiBackflow);
            if (backflow && !enemy.UsesResources && !sealedTurn) power *= Statuses.QiBackflowNoQiPower;   // 내공 역류 on a body without 내공
            if (enemy.UsesResources)
            {
                int cost = EnemyQiCost(move);
                if (backflow) cost = (int)Math.Round(cost * Statuses.QiBackflowFactor);
                if (cost > 0)
                {
                    if (enemy.Qi >= cost) enemy.Qi -= cost;
                    else { dry = true; enemy.Qi = 0; power *= EnemyDryFactor; }
                }
                if (enemy.Spirit >= MaxSpirit) { burst = true; enemy.Spirit = 0; power *= EnemyBurstFactor; }
            }
            var spec = new SkillSpec { Id = move.Name, Name = move.Name, Power = power };
            bool hurt = false, struck = false;   // struck: at least one hit landed without a guard (only those leave a 상태 이상)
            int selfHurt = 0;   // 환각: what the enemy's own blows send back to it
            bool bled = target.Has(StatusKind.Bleed);
            int firstHit = events.Count;
            BattleEvent lastHit = null;
            for (int h = 0; h < hits && target.IsAlive; h++)
            {
                var ev = new BattleEvent
                {
                    Turn = Turn, ActorId = enemy.Id, TargetId = target.Id, SkillId = move.Name, Kind = EventKind.Attack,
                    HitIndex = h, HitCount = hits, Ranged = move.Ranged, Awakening = awakening, Heavy = heavy, Burst = burst && h == 0, Weakened = dry && h == 0
                };
                if ((target.Evading && (target.EvadeChance >= 1f || _rng.NextDouble() < target.EvadeChance))
                    || (target.DodgeBonus > 0f && _rng.NextDouble() < target.DodgeBonus))
                {
                    ev.Dodged = true;
                    ev.TargetHpAfter = target.Hp;
                }
                else
                {
                    ev.IsCrit = _rng.NextDouble() < enemy.Stats.CritRate;
                    int dmg = BattleEngine.CalcDamage(enemy.Stats, target.Stats, spec, ev.IsCrit);
                    if (enemy.Has(StatusKind.Chill)) dmg = (int)Math.Round(dmg * ChillAttackFactor);
                    if (enemy.Has(StatusKind.Disarm)) dmg = (int)Math.Round(dmg * Statuses.DisarmFactor);
                    if (enemy.Has(StatusKind.Illusion)) { int back = (int)Math.Round(dmg * Statuses.IllusionSelfShare); selfHurt += back; dmg -= back; }
                    if (target.Focused) dmg = (int)Math.Round(dmg * FocusTakenFactor);
                    if (target.DamageTakenMul != 1f) dmg = Math.Max(1, (int)Math.Round(dmg * target.DamageTakenMul));   // 사부의 시련 가르침
                    if (target.Covering) dmg = Math.Max(1, (int)Math.Round(dmg * CoverDamageFactor));
                    float guard = GuardFactorFor(target);
                    if (guard < 1f) { dmg = Math.Max(1, (int)Math.Round(dmg * guard)); ev.Guarded = true; }
                    if (target.ShellFactor < 1f) { dmg = Math.Max(1, (int)Math.Round(dmg * target.ShellFactor)); target.ShellFactor = 1f; ev.Tag = "등껍질"; }   // 영수: one hit
                    target.Hp = Math.Max(0, target.Hp - dmg);
                    if (target.Hp == 0) TryRescue(target, ev);
                    ev.Damage = dmg;
                    ev.TargetHpAfter = target.Hp;
                    ev.TargetDefeated = !target.IsAlive;
                    hurt = true;
                    if (!ev.Guarded) struck = true;
                }
                events.Add(ev);
                lastHit = ev;
            }
            // 금요관: a charged blow that lands (even guarded) cuts one rope, however many hits it had; one fully dodged cuts nothing
            if (heavy && hurt && Trial != null && Trial.Kind == TrialKind.Lifeline)
            {
                LifelinesCut++;
                if (LifelinesCut >= Trial.Limit) TrialFailed = true;
            }
            if (hurt && bled && target.IsAlive) StatusHurt(target, StatusKind.Bleed, events);
            if (hurt && target.IsAlive && lastHit != null && ChillStruck(target))   // 한기: the third move that lands freezes the member
            {
                target.Stunned = true;
                target.StunReason = "빙결";
                lastHit.Stunned = true;
                lastHit.Notice = target.Name + "이(가) 얼어붙었다! 다음 차례를 잃는다";
            }
            if (struck && target.IsAlive)
            {
                if (move.Inflict != StatusKind.None && Rolls(target, move.Inflict, heavy ? Statuses.ChargedChance : Statuses.MoveChance)) Inflict(enemy, target, move.Inflict, events);
                if (heavy) Inflict(enemy, target, StatusKind.InnerWound, events);   // a charged blow taken without a guard leaves 내상
            }
            // Resolve the entire move first: partial evasion or dying to its last hit/bleed
            // must not grant a successful response. Mark one event, never one per hit.
            if (heavy && !struck && target.IsAlive && lastHit != null)
                GrantCounter(target, enemy, lastHit);
            if (hurt) target.Spirit = Math.Min(MaxSpirit, target.Spirit + SpiritOnTaken + target.SpiritTakenBonus);
            if (target.CounterStance && target.IsAlive && enemy.IsAlive && lastHit != null)   // 성막밀밀: the striker is struck back once per move
            {
                var back = DealHit(target, enemy, new SkillSpec { Id = "counter_stance", Name = "반격", Power = CounterStancePower, Tuned = true }, 0, 1, false, events);
                back.Counter = true;
                AddNotice(back, target.Name + "의 반격 태세! " + enemy.Name + "에게 되받아친다");
            }
            if (enemy.UsesResources && !burst) enemy.Spirit = Math.Min(MaxSpirit, enemy.Spirit + EnemySpiritOnDealt);
            for (int i = firstHit; i < events.Count; i++) events[i].TargetSpiritAfter = target.Spirit;
            if (selfHurt > 0 && enemy.IsAlive) SelfHurt(enemy, selfHurt, StatusKind.Illusion, events);
            if (!sealedTurn && enemy.IsAlive && enemy.Has(StatusKind.BloodBackflow)) StatusHurt(enemy, StatusKind.BloodBackflow, events);   // 혈맥 역류: every technique costs blood
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
            var chillTurns = new int[_units.Count];
            for (int i = 0; i < _units.Count; i++)
            {
                gauges[i] = _units[i].Gauge;
                foreach (var status in _units[i].Statuses)
                    if (status.Kind == StatusKind.Chill) chillTurns[i] = status.Turns;
            }
            // Current has already spent its gauge and ticked its statuses in RunUntilParty.
            if (IsPlayerTurn && count > 0) order.Add(Current);

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
                        if (_units[i].IsAlive) gauges[i] += GaugeGain(_units[i], chillTurns[i] > 0);
                    continue;
                }
                gauges[best] -= GaugeThreshold;
                if (chillTurns[best] > 0) chillTurns[best]--;
                order.Add(_units[best]);
            }
            return order;
        }
    }
}
