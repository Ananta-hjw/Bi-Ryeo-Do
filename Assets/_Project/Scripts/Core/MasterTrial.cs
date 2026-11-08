using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>The state of one 사부의 시련 run, kept in the save so a run can be resumed (additive field).</summary>
    [Serializable]
    public sealed class MasterTrialRun
    {
        public bool Active;
        public int Tier = 1;
        public int Seed;
        public int Floor = 1;                                   // the next 관문 (1..Floors)
        public string Node = "";                                // the 관문 chosen on this floor ("fight:stage:fight", "rest", "event:id", …)
        public string Pending = "";                             // "" = choose a path; "pick:<rarity>" = choose a 가르침; "rest"; "event:<id>"
        public List<string> Offer = new List<string>();         // the 가르침 on offer while Pending is a pick
        public List<string> Blessings = new List<string>();     // "id|arg:stacks"
        public List<string> Party = new List<string>();         // "unitId:hpPermil:qiPermil:spirit" after the last fight (empty = fresh)
        public long EarnedDoo, EarnedExp;
        public int Wins;
        public bool Repeat;                                     // the tier was already cleared when the run began (rewards halved)
    }

    public enum TrialNodeKind { Fight, Elite, Rest, Event, Boss }

    /// <summary>One 관문 on offer.</summary>
    public sealed class TrialNode
    {
        public TrialNodeKind Kind;
        public int Stage, Fight;   // fights: the story fight it borrows
        public string EventId;
        public string Code;        // how it is stored in MasterTrialRun.Node

        public string KindName
        {
            get
            {
                switch (Kind)
                {
                    case TrialNodeKind.Elite: return "강적";
                    case TrialNodeKind.Rest: return "휴식";
                    case TrialNodeKind.Event: return "기연";
                    case TrialNodeKind.Boss: return "우두머리";
                    default: return "전투";
                }
            }
        }
    }

    /// <summary>A 가르침 (run-only blessing).</summary>
    public sealed class TrialBlessing
    {
        public string Id, Name;
        public int Rarity;          // 0 보통, 1 희귀, 2 전설
        public bool Cost;           // 대가형: a gain paid for with a loss
        public bool NeedsArt;       // names one of 비류연's equipped arts (arg)
        public Func<int, string, string> Text;   // (stacks, art name) → effect line
    }

    /// <summary>
    /// 사부의 시련 (game design; 노사부 is only the novel's name and temper): a run of seven 관문 — six chosen paths (전투·강적·휴식·기연)
    /// and a 우두머리 — whose HP, 내공 and 기세 carry over. Wins grant run-only 가르침. Ten tiers open one by one; a tier already cleared
    /// pays half. Enemies are the player's recent story fights (<see cref="ReferenceStage"/>), scaled by tier and 관문.
    /// </summary>
    public static class MasterTrial
    {
        public const int UnlockStage = 31;   // 노사부 appears in person (18권)
        public const int Floors = 7;
        public const int MaxTier = 10;
        public const int RangeBack = 4;      // borrow fights from the last five cleared stages
        public static readonly string[] RarityNames = { "보통", "희귀", "전설" };

        public static bool Unlocked(SaveData save) { return save.ClearedStage >= UnlockStage; }
        public static int OpenTier(SaveData save) { return Math.Min(MaxTier, save.MasterTrialBest + 1); }
        public static bool Repeat(SaveData save, int tier) { return tier <= save.MasterTrialBest; }

        public static float TierHp(int tier) { return 1f + .10f * (tier - 1); }
        public static float TierAtk(int tier) { return 1f + .06f * (tier - 1); }
        public static float FloorMul(int floor) { return 1f + .04f * (floor - 1); }
        public const float EliteHp = 1.35f, EliteAtk = 1.15f;
        /// <summary>휴식 heals this share of max HP (and 1.5x of max 내공), less on higher tiers.</summary>
        public static float RestHeal(int tier) { return Math.Max(.2f, .40f - .02f * (tier - 1)); }
        public const float FallenRevive = .10f;   // a member who fell in a won fight stands up with this share

        // ------------------------------------------------------------------ 가르침

        private static string P(float v) { return Math.Round(v * 100f) + "%"; }

        public static readonly TrialBlessing[] Blessings =
        {
            new TrialBlessing { Id = "hp_up", Name = "철골", Rarity = 0, Text = (s, a) => "아군 모두 최대 체력 +" + P(.12f * s) },
            new TrialBlessing { Id = "atk_up", Name = "강권", Rarity = 0, Text = (s, a) => "아군 모두 공격 +" + P(.10f * s) },
            new TrialBlessing { Id = "def_up", Name = "금종조", Rarity = 0, Text = (s, a) => "아군 모두 방어 +" + P(.12f * s) },
            new TrialBlessing { Id = "speed_up", Name = "경신", Rarity = 0, Text = (s, a) => "아군 모두 속도 +" + P(.08f * s) },
            new TrialBlessing { Id = "crit_up", Name = "급소 찌르기", Rarity = 0, Text = (s, a) => "아군 모두 치명타 +" + P(.06f * s) },
            new TrialBlessing { Id = "start_spirit", Name = "선기", Rarity = 0, Text = (s, a) => "전투 시작 기세 +" + Math.Min(100, 25 * s) },
            new TrialBlessing { Id = "qi_regen", Name = "운기행공", Rarity = 0, Text = (s, a) => "차례마다 내공 회복 +" + 50 * s + "%" },
            new TrialBlessing { Id = "meditate_spirit", Name = "정좌", Rarity = 0, Text = (s, a) => "운기할 때 기세 +" + 20 * s },
            new TrialBlessing { Id = "heal_taken", Name = "생기", Rarity = 0, Text = (s, a) => "아군이 받는 회복 +" + P(.30f * s) },
            new TrialBlessing { Id = "art_power", Name = "초식 다듬기", Rarity = 0, NeedsArt = true, Text = (s, a) => a + " 위력 +" + P(.30f * s) },
            new TrialBlessing { Id = "basic_bleed", Name = "날 선 손끝", Rarity = 0, Text = (s, a) => "기본 공격에 출혈 " + P(Math.Min(.9f, .30f * s)) },
            new TrialBlessing { Id = "lifesteal", Name = "흡정", Rarity = 0, Text = (s, a) => "비류연 흡혈 +" + P(.05f * s) },
            new TrialBlessing { Id = "rest_plus", Name = "깊은 호흡", Rarity = 0, Text = (s, a) => "휴식 회복량 +" + 50 * s + "%" },
            new TrialBlessing { Id = "beast_cd", Name = "영수와의 호흡", Rarity = 0, Text = (s, a) => "영수 휴식 -" + s },

            new TrialBlessing { Id = "art_hits", Name = "연환", Rarity = 1, NeedsArt = true, Text = (s, a) => a + " 타수 +" + s },
            new TrialBlessing { Id = "art_cd", Name = "쾌속 전환", Rarity = 1, NeedsArt = true, Text = (s, a) => a + " 재사용 -" + s + "턴" },
            new TrialBlessing { Id = "guard_strike", Name = "후발선지", Rarity = 1, Text = (s, a) => "방어한 뒤 다음 공격 피해 +" + P(.40f * s) },
            new TrialBlessing { Id = "taken_down", Name = "호신강기", Rarity = 1, Text = (s, a) => "아군이 받는 피해 -" + P(1f - (float)Math.Max(.5, Math.Pow(.9, s))) },
            new TrialBlessing { Id = "vs_status", Name = "약점 공략", Rarity = 1, Text = (s, a) => "상태 이상에 걸린 적에게 주는 피해 +" + P(.20f * s) },
            new TrialBlessing { Id = "joint_cost", Name = "호흡 맞추기", Rarity = 1, Text = (s, a) => "합공 기세 -" + Math.Min(30, 15 * s) },
            new TrialBlessing { Id = "beast_power", Name = "영수의 분노", Rarity = 1, Text = (s, a) => "영수 위력 +" + P(.40f * s) },
            new TrialBlessing { Id = "companion_atk", Name = "동문의 의기", Rarity = 1, Text = (s, a) => "동료 공격 +" + P(.20f * s) },
            new TrialBlessing { Id = "crit_dmg", Name = "일격필살", Rarity = 1, Text = (s, a) => "아군 모두 치명타 피해 +" + P(.30f * s) },
            new TrialBlessing { Id = "first_strike", Name = "선수필승", Rarity = 1, Text = (s, a) => "전투 시작 행동 게이지 +" + Math.Min(90, 50 * s) + "%" },
            new TrialBlessing { Id = "after_heal", Name = "숨 고르기", Rarity = 1, Text = (s, a) => "관문을 이길 때마다 체력 " + P(.15f * s) + " 회복" },

            new TrialBlessing { Id = "dmg_up", Name = "뇌정", Rarity = 2, Text = (s, a) => "아군이 주는 피해 +" + P(.20f * s) },
            new TrialBlessing { Id = "all_power", Name = "사부의 손길", Rarity = 2, Text = (s, a) => "아군의 모든 무공 위력 +" + P(.20f * s) },
            new TrialBlessing { Id = "ult_spirit", Name = "뇌성벽력", Rarity = 2, Text = (s, a) => "필살기 기세 " + Math.Max(50, 100 - 30 * s) },

            new TrialBlessing { Id = "glass", Name = "배수의 진", Rarity = 1, Cost = true, Text = (s, a) => "공격 +" + P(.25f * s) + " · 최대 체력 -" + P(1f - (float)Math.Pow(.85, s)) },
            new TrialBlessing { Id = "reckless", Name = "광검", Rarity = 1, Cost = true, Text = (s, a) => "주는 피해 +" + P(.25f * s) + " · 받는 피해 +" + P(.15f * s) },
            new TrialBlessing { Id = "slow_strong", Name = "중검", Rarity = 0, Cost = true, Text = (s, a) => "공격 +" + P(.20f * s) + " · 속도 -" + P(1f - (float)Math.Pow(.9, s)) },
        };

        public static TrialBlessing FindBlessing(string id)
        {
            foreach (var b in Blessings) if (b.Id == id) return b;
            return null;
        }

        /// <summary>Splits a stored entry "id|arg:stacks".</summary>
        public static void Parse(string entry, out string id, out string arg, out int stacks)
        {
            stacks = 1;
            int colon = entry.LastIndexOf(':');
            string head = entry;
            if (colon > 0) { int.TryParse(entry.Substring(colon + 1), out stacks); head = entry.Substring(0, colon); }
            int bar = head.IndexOf('|');
            id = bar >= 0 ? head.Substring(0, bar) : head;
            arg = bar >= 0 ? head.Substring(bar + 1) : "";
        }

        /// <summary>The key of a stored 가르침 ("id" or "id|arg", without the stacks).</summary>
        public static string KeyOf(string entry) { string id, arg; int s; Parse(entry, out id, out arg, out s); return arg.Length > 0 ? id + "|" + arg : id; }

        public static int Stacks(MasterTrialRun run, string id, string arg = "")
        {
            string key = arg.Length > 0 ? id + "|" + arg : id;
            foreach (var e in run.Blessings) if (KeyOf(e) == key) { string i, a; int s; Parse(e, out i, out a, out s); return s; }
            return 0;
        }

        /// <summary>Adds one stack of a 가르침 ("id" or "id|arg").</summary>
        public static void AddBlessing(MasterTrialRun run, string key)
        {
            int have = 0;
            for (int i = run.Blessings.Count - 1; i >= 0; i--)
                if (KeyOf(run.Blessings[i]) == key) { string id, arg; Parse(run.Blessings[i], out id, out arg, out have); run.Blessings.RemoveAt(i); }
            run.Blessings.Add(key + ":" + (have + 1));
        }

        /// <summary>The name and effect of an offer or owned entry ("id|arg" or "id|arg:stacks"), at the given stack count.</summary>
        public static string Describe(string entry, int stacks = -1)
        {
            string id, arg; int s;
            Parse(entry, out id, out arg, out s);
            if (stacks > 0) s = stacks;
            var b = FindBlessing(id);
            if (b == null) return entry;
            var art = arg.Length > 0 ? EarlyGameContent.FindArt(arg) : null;
            return b.Text(s, art != null ? art.Name : arg);
        }

        public static string NameOf(string entry)
        {
            string id, arg; int s;
            Parse(entry, out id, out arg, out s);
            var b = FindBlessing(id);
            return b != null ? b.Name : id;
        }

        public static int RarityOf(string entry) { string id, arg; int s; Parse(entry, out id, out arg, out s); var b = FindBlessing(id); return b != null ? b.Rarity : 0; }
        public static bool IsCost(string entry) { string id, arg; int s; Parse(entry, out id, out arg, out s); var b = FindBlessing(id); return b != null && b.Cost; }

        // ------------------------------------------------------------------ the run

        public static MasterTrialRun Run(SaveData save) { if (save.MasterTrialRun == null) save.MasterTrialRun = new MasterTrialRun(); return save.MasterTrialRun; }

        public static bool Start(SaveData save, int tier, int seed)
        {
            if (!Unlocked(save) || tier < 1 || tier > OpenTier(save) || Run(save).Active) return false;
            save.MasterTrialRun = new MasterTrialRun { Active = true, Tier = tier, Seed = seed & 0x7fffffff, Repeat = Repeat(save, tier) };
            return true;
        }

        /// <summary>The story stage the fights come from: the last cleared one (at least <see cref="UnlockStage"/>).</summary>
        public static int ReferenceStage(SaveData save) { return Math.Max(UnlockStage, Math.Min(save.ClearedStage, EarlyGameContent.Stages.Length)); }

        private static bool Ordinary(StageData st, int fight, bool boss)
        {
            if (st.TrialFor(fight) != null || st.EventFor(fight) != null || st.Encounters[fight] == null || st.Encounters[fight].Length == 0) return false;
            bool hasBoss = false;
            foreach (var id in st.Encounters[fight])
            {
                var e = EarlyGameContent.FindEnemy(id);
                if (e == null || e.HpMul > 10f || e.FixedHp > 0) return false;
                if (e.IsBoss) hasBoss = true;
            }
            return hasBoss == boss;
        }

        /// <summary>Story fights a 관문 may borrow ("stage:fight"), from the reference stage back <see cref="RangeBack"/> (further while none fits).</summary>
        public static List<int[]> Candidates(SaveData save, bool boss)
        {
            var list = new List<int[]>();
            int top = ReferenceStage(save);
            for (int back = RangeBack; list.Count == 0 && back <= top; back += 5)
                for (int n = Math.Max(1, top - back); n <= top; n++)
                {
                    var st = EarlyGameContent.FindStage(n);
                    if (st == null) continue;
                    for (int f = 0; f < st.Encounters.Length; f++) if (Ordinary(st, f, boss)) list.Add(new[] { n, f });
                }
            return list;
        }

        private static Random Rng(MasterTrialRun run, int salt) { return new Random(unchecked(run.Seed * 31 + run.Floor * 7919 + salt)); }

        private static TrialNode FightNode(List<int[]> ordinary, List<int[]> bosses, TrialNodeKind kind, Random rng)
        {
            var pool = kind == TrialNodeKind.Boss && bosses.Count > 0 ? bosses : ordinary;
            var pick = pool[rng.Next(pool.Count)];
            string tag = kind == TrialNodeKind.Boss ? "boss" : kind == TrialNodeKind.Elite ? "elite" : "fight";
            return new TrialNode { Kind = kind, Stage = pick[0], Fight = pick[1], Code = tag + ":" + pick[0] + ":" + pick[1] };
        }

        public static readonly string[] EventIds = { "test", "herbs", "wanderer" };

        /// <summary>The 관문 on offer on the current floor (deterministic for the run).</summary>
        public static List<TrialNode> Options(SaveData save)
        {
            var run = Run(save);
            var rng = Rng(run, 1);
            var list = new List<TrialNode>();
            var ordinary = Candidates(save, false);
            var bosses = run.Floor >= Floors ? Candidates(save, true) : new List<int[]>();
            if (run.Floor >= Floors) { list.Add(FightNode(ordinary, bosses, TrialNodeKind.Boss, rng)); return list; }
            if (run.Floor == 1)
            {
                list.Add(FightNode(ordinary, bosses, TrialNodeKind.Fight, rng));
                list.Add(FightNode(ordinary, bosses, TrialNodeKind.Fight, rng));
                return list;
            }
            int count = rng.Next(2) == 0 ? 2 : 3;
            list.Add(FightNode(ordinary, bosses, TrialNodeKind.Fight, rng));   // a plain fight is always there
            bool rest = false, ev = false, elite = false;
            while (list.Count < count)
            {
                int roll = rng.Next(100);
                if (roll < 30 && !elite) { list.Add(FightNode(ordinary, bosses, TrialNodeKind.Elite, rng)); elite = true; }
                else if (roll < 60 && !rest && run.Floor > 2) { list.Add(new TrialNode { Kind = TrialNodeKind.Rest, Code = "rest" }); rest = true; }
                else if (roll < 85 && !ev) { string id = EventIds[rng.Next(EventIds.Length)]; list.Add(new TrialNode { Kind = TrialNodeKind.Event, EventId = id, Code = "event:" + id }); ev = true; }
                else list.Add(FightNode(ordinary, bosses, TrialNodeKind.Fight, rng));
            }
            return list;
        }

        public static TrialNode Decode(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            var parts = code.Split(':');
            switch (parts[0])
            {
                case "rest": return new TrialNode { Kind = TrialNodeKind.Rest, Code = code };
                case "event": return new TrialNode { Kind = TrialNodeKind.Event, EventId = parts.Length > 1 ? parts[1] : "", Code = code };
                default:
                    if (parts.Length < 3) return null;
                    var kind = parts[0] == "boss" ? TrialNodeKind.Boss : parts[0] == "elite" ? TrialNodeKind.Elite : TrialNodeKind.Fight;
                    return new TrialNode { Kind = kind, Stage = int.Parse(parts[1]), Fight = int.Parse(parts[2]), Code = code };
            }
        }

        /// <summary>Picks a 관문. A fight returns the stage to play; 휴식 and 기연 wait for their choice (<see cref="MasterTrialRun.Pending"/>).</summary>
        /// <summary>A 관문 fight has begun: until it is won or lost the run cannot pick a path again (leaving the game mid-fight ends the run).</summary>
        public static void BeginFight(SaveData save)
        {
            var run = Run(save);
            if (run.Active && run.Pending.Length == 0 && run.Node.Length > 0) run.Pending = "fight";
        }

        /// <summary>A fight that was started but never finished (the game was closed mid-fight) ends the run. Returns the note, or null.</summary>
        public static string FailAbandoned(SaveData save)
        {
            var run = Run(save);
            if (!run.Active || run.Pending != "fight") return null;
            return Fail(save) + " 전투 도중 시련을 떠났습니다.";
        }

        public static StageData Choose(SaveData save, TrialNode node)
        {
            var run = Run(save);
            if (!run.Active || run.Pending.Length > 0) return null;
            run.Node = node.Code;
            if (node.Kind == TrialNodeKind.Rest) { run.Pending = "rest"; return null; }
            if (node.Kind == TrialNodeKind.Event) { run.Pending = "event:" + node.EventId; return null; }
            return ToStage(save, node);
        }

        /// <summary>The one-fight stage of a fighting 관문 (also rebuilt when a fight is retried after closing the game).</summary>
        public static StageData ToStage(SaveData save, TrialNode node)
        {
            var run = Run(save);
            var src = EarlyGameContent.FindStage(node.Stage);
            if (src == null) return null;
            float hp = TierHp(run.Tier) * FloorMul(run.Floor), atk = TierAtk(run.Tier) * FloorMul(run.Floor);
            double pay = node.Kind == TrialNodeKind.Boss ? 2.0 : node.Kind == TrialNodeKind.Elite ? 1.4 : 1.0;
            if (node.Kind == TrialNodeKind.Elite) { hp *= EliteHp; atk *= EliteAtk; }
            if (run.Repeat) pay *= .5;
            return new StageData
            {
                Number = node.Stage, Name = "사부의 시련 " + run.Tier + "단계 · " + run.Floor + "관문 (" + node.KindName + ")", IsQuest = true,
                MasterTrialFloor = run.Floor, RunHpMul = hp, RunAtkMul = atk,
                Intro = new[] { "+사부|" + (node.Kind == TrialNodeKind.Boss ? "마지막 관문이다. 여기까지 와서 쓰러지면 처음부터 다시다." : node.Kind == TrialNodeKind.Elite ? "이번 놈들은 좀 맵다. 각오해라." : "쉬지 말고 덤벼라. 숨은 이기고 나서 고르는 거다.") },
                Outro = new[] { run.Floor + "관문을 넘었다." },
                Encounters = new[] { src.Encounters[node.Fight] },
                RecommendedLevel = src.RecommendedLevel,
                DooTotal = (long)Math.Round(src.DooTotal / (double)Math.Max(1, src.Encounters.Length) * pay),
            };
        }

        /// <summary>Enemy of a 관문: the story fight's enemy scaled by tier, 관문 and (강적) the elite factor.</summary>
        public static EnemyData ScaleEnemy(EnemyData enemy, StageData st)
        {
            return StageStars.StoryEnemy(enemy, st.Number, 0, null).Scaled(st.RunHpMul, st.RunAtkMul);
        }

        /// <summary>Exp of a won 관문: a share of the current level-up cost (half on a tier already cleared).</summary>
        public static long NodeExp(SaveData save, StageData st)
        {
            var node = Decode(Run(save).Node);
            double share = node != null && node.Kind == TrialNodeKind.Boss ? .20 : node != null && node.Kind == TrialNodeKind.Elite ? .14 : .10;
            if (Run(save).Repeat) share *= .5;   // fixed at the start, so the boss win (which raises the best tier) still pays in full
            return (long)Math.Round(Progression.ExpToNext(save.Level) * share);
        }

        // ------------------------------------------------------------------ the party in a run

        /// <summary>Records the fielded members' state; members who sat this fight out keep theirs (swapping companions does not heal them).</summary>
        private static void Snapshot(MasterTrialRun run, IList<Combatant> party)
        {
            foreach (var m in party) run.Party.RemoveAll(e => e.StartsWith(m.Id + ":", StringComparison.Ordinal));
            foreach (var m in party)
            {
                int hp = m.Stats.MaxHp > 0 ? (int)Math.Round(1000.0 * Math.Max(0, m.Hp) / m.Stats.MaxHp) : 1000;
                int qi = m.Stats.MaxQi > 0 ? (int)Math.Round(1000.0 * Math.Max(0, m.Qi) / m.Stats.MaxQi) : 1000;
                run.Party.Add(m.Id + ":" + hp + ":" + qi + ":" + m.Spirit);
            }
        }

        /// <summary>One member's carried state: HP and 내공 in per mille of max, and 기세.</summary>
        public sealed class MemberState { public string Id; public int HpPermil, QiPermil, Spirit; }

        public static List<MemberState> PartyState(MasterTrialRun run)
        {
            var list = new List<MemberState>();
            foreach (var e in run.Party)
            {
                var p = e.Split(':');
                if (p.Length < 4) continue;
                var m = new MemberState { Id = p[0] };
                int.TryParse(p[1], out m.HpPermil); int.TryParse(p[2], out m.QiPermil); int.TryParse(p[3], out m.Spirit);
                list.Add(m);
            }
            return list;
        }

        private static bool TryGetSnapshot(MasterTrialRun run, string id, out int hp, out int qi, out int spirit)
        {
            hp = qi = 1000; spirit = 0;
            foreach (var m in PartyState(run))
            {
                if (m.Id != id) continue;
                hp = m.HpPermil; qi = m.QiPermil; spirit = m.Spirit;
                return true;
            }
            return false;
        }

        private static SkillSpec Copy(SkillSpec s) { return s.Copy(); }

        private static void EditSkills(Combatant unit, Func<SkillSpec, bool> which, Action<SkillSpec> edit)
        {
            for (int i = 0; i < unit.Skills.Count; i++)
            {
                if (!which(unit.Skills[i])) continue;
                var c = Copy(unit.Skills[i]);
                edit(c);
                unit.Skills[i] = c;
            }
        }

        /// <summary>
        /// Applies the run to a freshly built party before a 관문 fight: the 가르침 (stats, arts, battle perks), then the HP, 내공 and 기세
        /// carried from the last fight (a fallen member stands up at <see cref="FallenRevive"/>).
        /// </summary>
        public static void ApplyToParty(SaveData save, IList<Combatant> party)
        {
            var run = Run(save);
            if (!run.Active) return;
            foreach (var m in party)
            {
                bool hero = m.Id == Inventory.Hero;
                var st = m.Stats;
                float atk = 1f, hp = 1f, def = 1f, spd = 1f;
                foreach (var entry in run.Blessings)
                {
                    string id, arg; int s;
                    Parse(entry, out id, out arg, out s);
                    switch (id)
                    {
                        case "hp_up": hp *= 1f + .12f * s; break;
                        case "atk_up": atk *= 1f + .10f * s; break;
                        case "def_up": def *= 1f + .12f * s; break;
                        case "speed_up": spd *= 1f + .08f * s; break;
                        case "crit_up": st.CritRate += .06f * s; break;
                        case "crit_dmg": st.CritDamage += .30f * s; break;
                        case "companion_atk": if (!hero) atk *= 1f + .20f * s; break;
                        case "glass": atk *= 1f + .25f * s; hp *= (float)Math.Pow(.85, s); break;
                        case "slow_strong": atk *= 1f + .20f * s; spd *= (float)Math.Pow(.9, s); break;
                        case "start_spirit": m.StartSpirit = Math.Min(100, m.StartSpirit + 25 * s); break;
                        case "qi_regen": m.QiRegenBonus += (int)Math.Round(InteractiveBattle.QiRegenFor(st.MaxQi) * .5f * s); break;
                        case "meditate_spirit": m.MeditateSpiritBonus += 20 * s; break;
                        case "heal_taken": m.HealTakenBonus += .30f * s; break;
                        case "lifesteal": if (hero) m.Lifesteal += .05f * s; break;
                        case "guard_strike": m.GuardStrikeBonus += .40f * s; break;
                        case "taken_down": m.DamageTakenMul *= (float)Math.Max(.5, Math.Pow(.9, s)); break;
                        case "reckless": m.DamageDealtMul *= 1f + .25f * s; m.DamageTakenMul *= 1f + .15f * s; break;
                        case "dmg_up": m.DamageDealtMul *= 1f + .20f * s; break;
                        case "vs_status": m.VsAfflictedBonus += .20f * s; break;
                        case "joint_cost": if (hero) m.JointCostCut = Math.Min(30, m.JointCostCut + 15 * s); break;
                        case "first_strike": m.Gauge = Math.Max(m.Gauge, Math.Min(900, 500 * s)); break;
                        case "basic_bleed":
                            { float c = Math.Min(.9f, .30f * s); EditSkills(m, k => k.Id == "basic", k => { k.Inflict = StatusKind.Bleed; k.InflictChance = c; }); break; }
                        case "all_power":
                            { float f = 1f + .20f * s; EditSkills(m, k => k.Id != "basic" && k.Power > 0f, k => k.Power *= f); break; }
                        case "ult_spirit":
                            if (hero) { int cost = Math.Max(50, 100 - 30 * s); EditSkills(m, k => k.SpiritCost >= 100, k => k.SpiritCost = cost); }
                            break;
                        case "art_power":
                            if (hero) { float f = 1f + .30f * s; EditSkills(m, k => k.Id == arg, k => k.Power *= f); }
                            break;
                        case "art_hits":
                            if (hero) { int add = s; EditSkills(m, k => k.Id == arg, k => k.Hits = Math.Max(1, k.Hits) + add); }
                            break;
                        case "art_cd":
                            if (hero) { int cut = s; EditSkills(m, k => k.Id == arg, k => k.Cooldown = Math.Max(0, k.Cooldown - cut)); }
                            break;
                    }
                }
                st.MaxHp = Math.Max(1, (int)Math.Round(st.MaxHp * hp));
                st.Attack = (int)Math.Round(st.Attack * atk);
                st.Defense = (int)Math.Round(st.Defense * def);
                st.Speed = Math.Max(1, (int)Math.Round(st.Speed * spd));
                m.ReplaceStats(st);
                int hpP, qiP, spirit;
                if (TryGetSnapshot(run, m.Id, out hpP, out qiP, out spirit))
                {
                    m.Hp = Math.Max(1, (int)Math.Round(m.Stats.MaxHp * Math.Max(hpP <= 0 ? FallenRevive * 1000 : hpP, 1) / 1000.0));
                    m.Qi = (int)Math.Round(m.Stats.MaxQi * qiP / 1000.0);
                    m.Spirit = Math.Max(0, Math.Min(InteractiveBattle.MaxSpirit, spirit));
                }
            }
        }

        public static void ApplyToBeast(SaveData save, BeastLoadout beast)
        {
            var run = Run(save);
            if (beast == null || !run.Active) return;
            int cd = Stacks(run, "beast_cd"), power = Stacks(run, "beast_power");
            beast.Cooldown = Math.Max(1, beast.Cooldown - cd);
            beast.PowerFactor *= 1f + .40f * power;
        }

        /// <summary>A won 관문 fight: keeps the party's state (plus 숨 고르기), then a 가르침 pick or, after the 우두머리, the run's end.</summary>
        public static List<string> CompleteFight(SaveData save, IList<Combatant> party, IRandom rng)
        {
            var run = Run(save);
            var notes = new List<string>();
            if (!run.Active) return notes;
            int afterHeal = Stacks(run, "after_heal");
            if (afterHeal > 0) foreach (var m in party) if (m.IsAlive) m.Hp = Math.Min(m.Stats.MaxHp, m.Hp + (int)Math.Round(m.Stats.MaxHp * .15f * afterHeal));
            Snapshot(run, party);
            run.Wins++;
            var node = Decode(run.Node);
            if (node != null && node.Kind == TrialNodeKind.Boss) { notes.AddRange(Finish(save)); return notes; }
            OfferPick(save, node != null && node.Kind == TrialNodeKind.Elite ? 1 : 0, rng);
            notes.Add("가르침 하나를 고를 수 있습니다.");
            return notes;
        }

        /// <summary>Fills <see cref="MasterTrialRun.Offer"/> with three 가르침 (level 0 = after a fight, 1 = after an 강적 or a rare event).</summary>
        public static void OfferPick(SaveData save, int level, IRandom rng)
        {
            var run = Run(save);
            run.Offer.Clear();
            var arts = new List<string>();
            foreach (var id in save.EquippedArts) { var a = EarlyGameContent.FindArt(id); if (a != null && a.OwnerId == null && a.Effect == SkillEffect.None) arts.Add(id); }
            bool beast = Beasts.LoadoutFor(save) != null;
            int guard = 0;
            while (run.Offer.Count < 3 && guard++ < 200)
            {
                double roll = rng.NextDouble();
                int rarity = level == 0 ? (roll < .70 ? 0 : roll < .95 ? 1 : 2) : (roll < .30 ? 0 : roll < .85 ? 1 : 2);
                var pool = new List<TrialBlessing>();
                foreach (var b in Blessings)
                {
                    if (b.Rarity != rarity) continue;
                    if (b.NeedsArt && arts.Count == 0) continue;
                    if ((b.Id == "beast_cd" || b.Id == "beast_power") && !beast) continue;
                    pool.Add(b);
                }
                if (pool.Count == 0) continue;
                var pick = pool[(int)(rng.NextDouble() * pool.Count) % pool.Count];
                string key = pick.NeedsArt ? pick.Id + "|" + arts[(int)(rng.NextDouble() * arts.Count) % arts.Count] : pick.Id;
                if (run.Offer.Contains(key)) continue;
                run.Offer.Add(key);
            }
            run.Pending = "pick:" + level;
        }

        /// <summary>Takes one 가르침 on offer (index -1 = take none) and moves to the next floor.</summary>
        public static bool Pick(SaveData save, int index)
        {
            var run = Run(save);
            if (!run.Pending.StartsWith("pick", StringComparison.Ordinal)) return false;
            if (index >= 0 && index < run.Offer.Count) AddBlessing(run, run.Offer[index]);
            run.Offer.Clear();
            Advance(run);
            return true;
        }

        private static void Advance(MasterTrialRun run)
        {
            run.Pending = "";
            run.Node = "";
            run.Floor++;
        }

        /// <summary>휴식: heal everyone (and lift statuses: they never carry between fights anyway).</summary>
        public static void RestHealAll(SaveData save)
        {
            var run = Run(save);
            if (run.Pending != "rest") return;
            float heal = RestHeal(run.Tier) * (1f + .5f * Stacks(run, "rest_plus"));
            Heal(run, heal, heal * 1.5f);
            Advance(run);
        }

        /// <summary>휴식 instead spent training: one more stack of an owned 가르침.</summary>
        public static bool RestTrain(SaveData save, string key)
        {
            var run = Run(save);
            if (run.Pending != "rest") return false;
            bool owned = false;
            foreach (var e in run.Blessings) if (KeyOf(e) == key) owned = true;
            if (!owned) return false;
            AddBlessing(run, key);
            Advance(run);
            return true;
        }

        private static void Heal(MasterTrialRun run, float hpShare, float qiShare)
        {
            var members = PartyState(run);
            run.Party.Clear();
            foreach (var m in members)
            {
                int hp = m.HpPermil, delta = (int)Math.Round(hpShare * 1000);
                if (delta >= 0) hp = Math.Min(1000, Math.Max(hp, (int)(FallenRevive * 1000)) + delta);   // a heal also stands the fallen up
                else if (hp > 0) hp = Math.Max(1, hp + delta);                                       // a cost never fells anyone
                int qi = Math.Max(0, Math.Min(1000, m.QiPermil + (int)Math.Round(qiShare * 1000)));
                run.Party.Add(m.Id + ":" + hp + ":" + qi + ":" + m.Spirit);
            }
        }

        public sealed class EventChoice { public string Label, Result; }

        public static string EventTitle(string id)
        {
            switch (id)
            {
                case "test": return "사부의 손찌검";
                case "herbs": return "약초밭";
                default: return "떠돌이 무인";
            }
        }

        public static string EventText(string id)
        {
            switch (id)
            {
                case "test": return "+사부|어디 맞으면서 배워 봐라. 아픈 만큼 남는다.";
                case "herbs": return "+길가 바위 틈에 약초가 무성하다. 다 캐 가기엔 시간이 아깝다.";
                default: return "+떠돌이 무인|은자만 내면 내 비전 하나 가르쳐 주지.";
            }
        }

        public const long WandererPrice = 2000;
        public const float TestCost = .20f, HerbHeal = .25f;

        public static string[] EventOptions(string id)
        {
            switch (id)
            {
                case "test": return new[] { "맞는다 (아군 모두 체력 -" + Math.Round(TestCost * 100) + "%, 희귀 이상 가르침)", "도망간다" };
                case "herbs": return new[] { "약초를 달여 먹는다 (아군 모두 체력 +" + Math.Round(HerbHeal * 100) + "%)", "영수 먹이를 챙긴다 (2개)" };
                default: return new[] { "은자 " + WandererPrice.ToString("N0") + "을 낸다 (가르침 고르기)", "거절한다" };
            }
        }

        /// <summary>Resolves a 기연 choice (0 or 1). Returns a note, or null when the choice is not possible (not enough 은자).</summary>
        public static string ChooseEvent(SaveData save, int option, IRandom rng)
        {
            var run = Run(save);
            if (!run.Pending.StartsWith("event:", StringComparison.Ordinal)) return null;
            string id = run.Pending.Substring(6);
            switch (id)
            {
                case "test":
                    if (option == 0) { Heal(run, -TestCost, 0f); OfferPick(save, 1, rng); return "사부의 주먹이 매웠다. 가르침을 고르세요."; }
                    break;
                case "herbs":
                    if (option == 0) { Heal(run, HerbHeal, 0f); Advance(run); return "기운이 돌아왔습니다."; }
                    Inventory.Add(save, Beasts.FoodId, 2); Advance(run); return "영수 먹이 2개를 얻었습니다.";
                default:
                    if (option == 0)
                    {
                        if (save.Doo < WandererPrice) return null;
                        save.Doo -= WandererPrice;
                        OfferPick(save, 0, rng);
                        return "가르침을 고르세요.";
                    }
                    break;
            }
            Advance(run);
            return "그냥 지나갔습니다.";
        }

        // ------------------------------------------------------------------ the end of a run

        public static long ClearDoo(int tier) { return 3000 + 1500L * tier; }
        public static int ClearInsight(int tier) { return tier >= 8 ? 2 : tier >= 4 ? 1 : 0; }

        /// <summary>The 우두머리 fell: the tier's reward (double the first time, half on a repeat) and the next tier opens.</summary>
        public static List<string> Finish(SaveData save)
        {
            var run = Run(save);
            var notes = new List<string>();
            bool first = !run.Repeat && run.Tier > save.MasterTrialBest;
            long doo = ClearDoo(run.Tier) * (first ? 2 : 1) / (first ? 1 : 2);
            doo = Income.Boost(doo);
            int stones = ClearInsight(run.Tier) + (first && run.Tier >= 4 ? 1 : 0);
            if (!first) stones /= 2;
            save.Doo += doo;
            run.EarnedDoo += doo;
            if (stones > 0) Inventory.Add(save, InnerArts.StoneId, stones);
            notes.Add("시련 " + run.Tier + "단계 돌파" + (first ? " (첫 돌파)" : " (다시 돌파: 보상 절반)") + " · 은자 " + doo.ToString("N0") + (stones > 0 ? " · 심득석 " + stones + "개" : ""));
            if (first) { save.MasterTrialBest = run.Tier; if (run.Tier < MaxTier) notes.Add((run.Tier + 1) + "단계가 열렸습니다."); }
            save.MasterTrialRuns++;
            run.Active = false;
            run.Pending = "done";
            return notes;
        }

        /// <summary>The party fell (or gave up): the run ends; what each 관문 paid stays.</summary>
        public static string Fail(SaveData save)
        {
            var run = Run(save);
            if (!run.Active) return "";
            run.Active = false;
            run.Pending = "failed";
            save.MasterTrialRuns++;
            return "시련 " + run.Tier + "단계 · " + run.Floor + "관문에서 쓰러졌습니다. (돌파한 관문 " + run.Wins + "개)";
        }

        /// <summary>Clears a finished run so a new one can start.</summary>
        public static void Close(SaveData save) { if (!Run(save).Active) save.MasterTrialRun = new MasterTrialRun(); }
    }
}
