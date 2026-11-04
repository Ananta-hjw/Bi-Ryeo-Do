using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>상태 이상 a unit can carry in a fight. Every one lasts a number of the unit's own turns.</summary>
    public enum StatusKind { None, Poison, Burn, Chill, InnerWound, Bleed, Seal, Disarm, QiBackflow, BloodBackflow, Deviation, Illusion }

    public sealed class StatusEffect
    {
        public StatusKind Kind;
        public int Turns;   // own turns left
    }

    /// <summary>
    /// Rules and numbers of the 상태 이상 (see <see cref="InteractiveBattle"/>):
    /// 중독·화상 lose a share of max HP at the start of each own turn, 한기 slows the action gauge, 내상 stops 내공 from coming back,
    /// 출혈 adds a share of max HP to every attack that lands on the unit. A unit carries at most one status: until it runs out (or is cured)
    /// nothing else takes hold and it is not lengthened (<see cref="Combatant.AddStatus"/>). Damage over time is weaker on 정예 and 보스, so it cannot melt a boss.
    /// Enemy moves get their status from their name (독 moves poison, 한빙 moves chill and so on, <see cref="ForMove"/>); every charged heavy
    /// blow that lands unguarded also leaves 내상. 운기 cures one status, 해독 영약 and 윤준호's 자하청심결 cure them all.
    /// </summary>
    public static class Statuses
    {
        public const float PoisonPct = 0.04f, BurnPct = 0.06f, BleedPct = 0.025f;
        public const float PoisonHealFactor = 0.5f;   // 중독: every heal the unit receives (arts, items, 운기, lifesteal) is halved
        public const int ChillFreezeHits = 3;         // 한기: three more attacks taken while chilled freeze the unit for one turn (the one that chilled it does not count)
        public const float ChillSpeed = 0.7f, BossChillSpeed = 0.85f;   // action gauge speed while chilled
        public const float EliteDotFactor = 0.65f, BossDotFactor = 0.35f;
        public const float MeditateCureChance = 0.40f;       // 운기 lifts one 상태 이상 only at this chance
        public const float DisarmFactor = 0.75f;             // 탈기: damage dealt
        public const float QiBackflowFactor = 2f;            // 내공 역류: 내공 a martial art costs
        public const float QiBackflowNoQiPower = 0.75f;      // 내공 역류 on an enemy without 내공 (일반 rank): its moves hit x0.75
        public const float BloodBackflowPct = 0.10f;         // 혈맥 역류: max HP lost per martial art (x DotFactor)
        public const float DeviationPct = 0.10f;             // 주화입마: max HP lost each own turn (x DotFactor)
        public const float DeviationPower = 2f;              // 주화입마: the basic attack it throws at an ally
        public const float DeviationChance = 0.20f;          // 주화입마 takes hold at this chance whoever inflicts it
        public const float IllusionSelfShare = 0.30f;        // 환각: share of every blow it lands that comes back to the attacker
        public const float StrongBossChance = 0.5f, MightyBossChance = 0.2f;   // 강력한 / 정말 강력한 보스: status chance factor
        public const float MoveChance = 0.4f;      // an enemy's status move that lands
        public const float ChargedChance = 1f;     // the heavy blow after a wind-up

        public static readonly StatusKind[] All = { StatusKind.Poison, StatusKind.Burn, StatusKind.Chill, StatusKind.InnerWound, StatusKind.Bleed, StatusKind.Seal, StatusKind.Disarm, StatusKind.QiBackflow, StatusKind.BloodBackflow, StatusKind.Deviation, StatusKind.Illusion };

        /// <summary>The order 운기 cures them in: the one that hurts most first.</summary>
        public static readonly StatusKind[] CurePriority = { StatusKind.Deviation, StatusKind.Burn, StatusKind.Poison, StatusKind.BloodBackflow, StatusKind.Bleed, StatusKind.Illusion, StatusKind.Seal, StatusKind.Disarm, StatusKind.QiBackflow, StatusKind.InnerWound, StatusKind.Chill };

        public static int DefaultTurns(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Poison: return 4;
                case StatusKind.Burn: return 3;
                case StatusKind.Chill: return 3;
                case StatusKind.InnerWound: return 3;
                case StatusKind.Bleed: return 3;
                case StatusKind.Seal: return 2;
                case StatusKind.Disarm: return 2;
                case StatusKind.QiBackflow: return 3;
                case StatusKind.BloodBackflow: return 3;
                case StatusKind.Deviation: return 2;
                case StatusKind.Illusion: return 2;
            }
            return 0;
        }

        public static string Name(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Poison: return "중독";
                case StatusKind.Burn: return "화상";
                case StatusKind.Chill: return "한기";
                case StatusKind.InnerWound: return "내상";
                case StatusKind.Bleed: return "출혈";
                case StatusKind.Seal: return "봉혈";
                case StatusKind.Disarm: return "탈기";
                case StatusKind.QiBackflow: return "내공 역류";
                case StatusKind.BloodBackflow: return "혈맥 역류";
                case StatusKind.Deviation: return "주화입마";
                case StatusKind.Illusion: return "환각";
            }
            return "";
        }

        /// <summary>Player-facing rule of one status (for tips and help).</summary>
        public static string Describe(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Poison: return "중독: 자기 차례마다 최대 체력의 " + Pct(PoisonPct) + "를 잃고, 받는 회복이 " + Pct(1f - PoisonHealFactor) + " 줄어듭니다(" + DefaultTurns(kind) + "턴).";
                case StatusKind.Burn: return "화상: 자기 차례마다 최대 체력의 " + Pct(BurnPct) + "를 잃습니다(" + DefaultTurns(kind) + "턴).";
                case StatusKind.Chill: return "한기: 몸이 굳어 차례가 늦게 돌아오고 주는 피해가 줄어듭니다(속도 -" + Pct(1f - ChillSpeed) + ", 공격 -" + Pct(1f - InteractiveBattle.ChillAttackFactor) + ", " + DefaultTurns(kind) + "턴). 걸린 뒤 공격을 " + ChillFreezeHits + "번 더 맞으면 1턴 동안 얼어붙습니다.";
                case StatusKind.InnerWound: return "내상: 내공이 차오르지 않습니다(" + DefaultTurns(kind) + "턴).";
                case StatusKind.Bleed: return "출혈: 공격을 맞을 때마다 최대 체력의 " + Pct(BleedPct) + "를 더 잃습니다(" + DefaultTurns(kind) + "턴).";
                case StatusKind.Seal: return "봉혈: 혈도가 막혀 기본 공격만 할 수 있습니다(방어·운기·아이템은 가능, " + DefaultTurns(kind) + "턴).";
                case StatusKind.Disarm: return "탈기: 손아귀에 힘이 빠져 주는 피해가 " + Pct(1f - DisarmFactor) + " 줄어듭니다(" + DefaultTurns(kind) + "턴).";
                case StatusKind.QiBackflow: return "내공 역류: 무공을 쓸 때 내공이 2배로 듭니다. 내공이 없는 적은 기술 위력이 " + Pct(1f - QiBackflowNoQiPower) + " 줄어듭니다(" + DefaultTurns(kind) + "턴).";
                case StatusKind.BloodBackflow: return "혈맥 역류: 무공을 쓸 때마다 최대 체력의 " + Pct(BloodBackflowPct) + "를 잃습니다(" + DefaultTurns(kind) + "턴).";
                case StatusKind.Deviation: return "주화입마: 제어를 잃고 자기 편을 기본 공격의 2배로 치며, 자기 차례마다 최대 체력의 " + Pct(DeviationPct) + "를 잃습니다(" + DefaultTurns(kind) + "턴).";
                case StatusKind.Illusion: return "환각: 공격 피해의 " + Pct(IllusionSelfShare) + "는 자신이, 나머지만 상대가 받습니다(" + DefaultTurns(kind) + "턴).";
            }
            return "";
        }

        private static string Pct(float f) { return (f * 100f).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%"; }

        /// <summary>Share of max HP that 중독/화상/출혈 take from this unit (weaker on 정예 and 보스).</summary>
        public static float DotFactor(Combatant unit)
        {
            if (unit.Team == Team.Player) return 1f;
            if (unit.IsBoss || unit.Rank == EnemyRank.Boss) return BossDotFactor;
            return unit.Rank == EnemyRank.Elite ? EliteDotFactor : 1f;
        }

        public static int DotDamage(Combatant unit, StatusKind kind)
        {
            float pct = kind == StatusKind.Poison ? PoisonPct : kind == StatusKind.Burn ? BurnPct : kind == StatusKind.Bleed ? BleedPct
                : kind == StatusKind.BloodBackflow ? BloodBackflowPct : kind == StatusKind.Deviation ? DeviationPct : 0f;
            if (pct <= 0f) return 0;
            return Math.Max(1, (int)Math.Round(unit.Stats.MaxHp * pct * DotFactor(unit)));
        }

        /// <summary>Own turns a status lasts on this unit: bosses shake every status off one turn sooner (at least one turn).</summary>
        public static int TurnsFor(Combatant unit, StatusKind kind)
        {
            int turns = DefaultTurns(kind);
            return unit != null && unit.IsBoss ? Math.Max(1, turns - 1) : turns;
        }

        /// <summary>Chance factor of a status taking hold on this unit: 강력한 보스 x0.5, 정말 강력한 보스 (and the 시험 walls) x0.2.</summary>
        public static float ChanceFactor(Combatant unit)
        {
            if (unit == null) return 1f;
            return unit.StatusTier >= 2 ? MightyBossChance : unit.StatusTier == 1 ? StrongBossChance : 1f;
        }

        /// <summary>Enemies the original treats as beyond reach (절대고수): statuses land at x0.2 and their moves hit x1.5.</summary>
        public static readonly HashSet<string> MightyBosses = new HashSet<string>
        {
            "bi_daegongja", "chilsang_heart", "seocheon_scar", "seocheon_delay", "seocheon_whirl", "ice_dragon_wall",
            "geomhu_ioksang", "geomjon_gongson", "muhwa_drain_fire", "muhwa_drain_ice", "mumyeong_escape", "mumyeong_steps", "mumyeong_three"
        };

        /// <summary>2026-11: every move of a story enemy hits harder by rank: 보스 x1.15, 강력한 보스 (every fifth stage) x1.3, 절대고수 x1.5.</summary>
        public const float BossMovePower = 1.15f, StrongBossMovePower = 1.3f, MightyMovePower = 1.5f;

        /// <summary>Speed factor of the action gauge (한기 slows it).</summary>
        public static float SpeedFactor(Combatant unit)
        {
            if (!unit.Has(StatusKind.Chill)) return 1f;
            return unit.IsBoss ? BossChillSpeed : ChillSpeed;
        }

        // Enemy move names that carry a status. The names come from the story's techniques; which status a name gives is the game's reading of it
        // (독 = poison, 한빙 = chill, 불·염 = burn, 혈 and claws/bites = bleed).
        private static readonly Dictionary<string, StatusKind> MoveStatus = new Dictionary<string, StatusKind>
        {
            { "독니", StatusKind.Poison }, { "독검", StatusKind.Poison }, { "절독난무", StatusKind.Poison }, { "무언독 암기", StatusKind.Poison },
            { "맹독 난자", StatusKind.Poison }, { "독편 난무", StatusKind.Poison }, { "독침", StatusKind.Poison }, { "독조", StatusKind.Poison },
            { "독액", StatusKind.Poison }, { "극독 일섬", StatusKind.Poison },
            { "화령염천도세", StatusKind.Burn }, { "화룡의 숨결", StatusKind.Burn }, { "진홍십칠염", StatusKind.Burn }, { "명염참", StatusKind.Burn },
            { "불의 성벽", StatusKind.Burn }, { "검염기", StatusKind.Burn },
            { "빙령수혼", StatusKind.Chill }, { "백빙신침", StatusKind.Chill }, { "백빙신첨", StatusKind.Chill }, { "백빙신정", StatusKind.Chill },
            { "혈검", StatusKind.Bleed }, { "혈풍무", StatusKind.Bleed }, { "혈폭 난사", StatusKind.Bleed }, { "혈야구성", StatusKind.Bleed },
            { "혈마전", StatusKind.Bleed }, { "만사혈장", StatusKind.Bleed }, { "굉천혈류패왕십자인", StatusKind.Bleed }, { "혈겁쇄법 노룡분쇄", StatusKind.Bleed },
            { "물어뜯기", StatusKind.Bleed }, { "할퀴기", StatusKind.Bleed }, { "사검 그물", StatusKind.Bleed },
            { "난화수", StatusKind.Seal }, { "포위검", StatusKind.Seal }, { "검은 장갑", StatusKind.Seal }, { "내려치기", StatusKind.Disarm }, { "맹호삼십육검", StatusKind.Disarm }, { "살기 어린 일검", StatusKind.Disarm }, { "천풍마뢰참", StatusKind.Disarm }, { "파랑분쇄격", StatusKind.Disarm }, { "군자일검", StatusKind.Disarm }, { "정석의 검로", StatusKind.Disarm }, { "매화만개", StatusKind.Bleed }, { "팔비신검", StatusKind.Bleed }, { "참룡해체", StatusKind.Bleed }, { "호조 검기", StatusKind.Bleed }, { "백호조", StatusKind.Bleed }, { "선풍참혼", StatusKind.QiBackflow }, { "무영인", StatusKind.Illusion },   // bosses that had none
            // 2026-11: 봉혈 (binding · point strikes · needles), 탈기 (crushing blows), 내공·혈맥 역류 (soul-shaking and blood arts), 주화입마 (madness), 환각 (phantoms)
            { "철쇄", StatusKind.Seal }, { "주박인", StatusKind.Seal }, { "사슬낫", StatusKind.Seal }, { "쇄풍 난무", StatusKind.Seal }, { "아홉 갈래 삭영", StatusKind.Seal }, { "구두룡살진", StatusKind.Seal }, { "낚싯줄", StatusKind.Seal }, { "손가락 검기", StatusKind.Seal }, { "일점혈", StatusKind.Seal }, { "검로 봉쇄", StatusKind.Seal }, { "퇴로 봉쇄", StatusKind.Seal }, { "퇴로 봉쇄검", StatusKind.Seal }, { "봉쇄 일격", StatusKind.Seal }, { "철신관 봉쇄", StatusKind.Seal }, { "길목 봉쇄", StatusKind.Seal }, { "출항 봉쇄", StatusKind.Seal }, { "마비 안개", StatusKind.Seal }, { "오색 투침", StatusKind.Seal }, { "혈심란의 침", StatusKind.Seal }, { "암기", StatusKind.Seal }, { "철쇄선풍", StatusKind.Seal },
            { "일도파쇄", StatusKind.Disarm }, { "내려찍기", StatusKind.Disarm }, { "패룡격", StatusKind.Disarm }, { "패왕노호검 일격", StatusKind.Disarm }, { "검풍", StatusKind.Disarm }, { "천원삼절검격", StatusKind.Disarm }, { "파풍창", StatusKind.Disarm }, { "적웅산도", StatusKind.Disarm }, { "기창충격", StatusKind.Disarm }, { "단옥도 일격", StatusKind.Disarm }, { "맹호출림", StatusKind.Disarm }, { "백룡검 일섬", StatusKind.Disarm }, { "태산양분", StatusKind.Disarm }, { "잔뢰번천지", StatusKind.Disarm }, { "천겁 일섬", StatusKind.Disarm }, { "거력장", StatusKind.Disarm }, { "전신 압박", StatusKind.Disarm }, { "거력권", StatusKind.Disarm }, { "풍압권", StatusKind.Disarm }, { "수문장의 주먹", StatusKind.Disarm }, { "일격필살", StatusKind.Disarm }, { "붉은 도강", StatusKind.Disarm }, { "광풍노도", StatusKind.Disarm }, { "해폭난파", StatusKind.Disarm }, { "정면 돌진", StatusKind.Disarm }, { "묵린 강타", StatusKind.Disarm }, { "무극검 일관", StatusKind.Disarm }, { "북두칠성 투창", StatusKind.Disarm }, { "만근추", StatusKind.Disarm }, { "정면의 일검", StatusKind.Disarm }, { "거창돌격", StatusKind.Disarm },
            { "귀령압혼", StatusKind.QiBackflow }, { "소혼붕렬", StatusKind.QiBackflow }, { "무형 칼날", StatusKind.QiBackflow }, { "단월인", StatusKind.QiBackflow }, { "영뢰 잔흔", StatusKind.QiBackflow }, { "비탄의 일검", StatusKind.QiBackflow }, { "흑풍난무", StatusKind.QiBackflow }, { "광풍난주", StatusKind.QiBackflow }, { "자운", StatusKind.QiBackflow }, { "참천무뢰", StatusKind.QiBackflow },
            { "혈왕살", StatusKind.BloodBackflow }, { "혈설십삼식", StatusKind.BloodBackflow }, { "핏빛 손톱", StatusKind.BloodBackflow }, { "혈강", StatusKind.BloodBackflow }, { "혈창의 찌르기", StatusKind.BloodBackflow }, { "지사진", StatusKind.BloodBackflow }, { "천지합살", StatusKind.BloodBackflow }, { "생명을 불사른 일격", StatusKind.BloodBackflow },
            { "초혼귀령검", StatusKind.Deviation }, { "귀곡의 합격", StatusKind.Deviation }, { "악몽의 검", StatusKind.Deviation }, { "광풍장", StatusKind.Deviation }, { "폭주 진기", StatusKind.Deviation }, { "명왕참", StatusKind.Deviation },
            { "몽환쌍영무", StatusKind.Illusion }, { "몽영", StatusKind.Illusion }, { "쌍영무", StatusKind.Illusion }, { "환영", StatusKind.Illusion }, { "환영의 검", StatusKind.Illusion }, { "삼중 잔영", StatusKind.Illusion }, { "허상 일검", StatusKind.Illusion }, { "이분영", StatusKind.Illusion }, { "사련화영", StatusKind.Illusion }, { "팔련화린 난화만천", StatusKind.Illusion }, { "사분영의 검", StatusKind.Illusion }, { "이분영 쌍화엽", StatusKind.Illusion }, { "어검분영술", StatusKind.Illusion }, { "영뢰 천잔", StatusKind.Illusion }, { "휘선의 변화", StatusKind.Illusion }, { "은신검", StatusKind.Illusion }, { "기척 없는 찌르기", StatusKind.Illusion }, { "그림자 비수", StatusKind.Illusion }, { "암영검", StatusKind.Illusion }, { "흑살도", StatusKind.Illusion }, { "탈혼지천락 파산선고", StatusKind.Illusion }, { "쾌영만천", StatusKind.Illusion },
            { "암습", StatusKind.Bleed }, { "암살", StatusKind.Bleed }, { "연사", StatusKind.Bleed },
        };

        /// <summary>The status an enemy move of this name leaves (None for most moves).</summary>
        public static StatusKind ForMove(string name)
        {
            StatusKind k;
            return name != null && MoveStatus.TryGetValue(name, out k) ? k : StatusKind.None;
        }

        /// <summary>"중독2 화상1": the statuses a unit carries with the turns left ("" when none).</summary>
        public static string Line(Combatant unit)
        {
            if (unit.Statuses.Count == 0) return "";
            var parts = new List<string>();
            foreach (var s in unit.Statuses) parts.Add(Name(s.Kind) + s.Turns);
            return string.Join(" ", parts);
        }
    }
}
