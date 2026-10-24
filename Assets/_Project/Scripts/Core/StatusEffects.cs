using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>상태 이상 a unit can carry in a fight. Every one lasts a number of the unit's own turns.</summary>
    public enum StatusKind { None, Poison, Burn, Chill, InnerWound, Bleed }

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
        public const float PoisonPct = 0.05f, BurnPct = 0.08f, BleedPct = 0.025f;
        public const float ChillSpeed = 0.7f, BossChillSpeed = 0.85f;   // action gauge speed while chilled
        public const float EliteDotFactor = 0.6f, BossDotFactor = 0.35f;
        public const float MoveChance = 0.4f;      // an enemy's status move that lands
        public const float ChargedChance = 1f;     // the heavy blow after a wind-up

        public static readonly StatusKind[] All = { StatusKind.Poison, StatusKind.Burn, StatusKind.Chill, StatusKind.InnerWound, StatusKind.Bleed };

        /// <summary>The order 운기 cures them in: the one that hurts most first.</summary>
        public static readonly StatusKind[] CurePriority = { StatusKind.Burn, StatusKind.Poison, StatusKind.Bleed, StatusKind.InnerWound, StatusKind.Chill };

        public static int DefaultTurns(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Poison: return 3;
                case StatusKind.Burn: return 2;
                case StatusKind.Chill: return 2;
                case StatusKind.InnerWound: return 2;
                case StatusKind.Bleed: return 3;
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
            }
            return "";
        }

        /// <summary>Player-facing rule of one status (for tips and help).</summary>
        public static string Describe(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Poison: return "중독: 자기 차례마다 최대 체력의 " + Pct(PoisonPct) + "를 잃습니다(" + DefaultTurns(kind) + "턴).";
                case StatusKind.Burn: return "화상: 자기 차례마다 최대 체력의 " + Pct(BurnPct) + "를 잃습니다(" + DefaultTurns(kind) + "턴).";
                case StatusKind.Chill: return "한기: 몸이 굳어 차례가 늦게 돌아옵니다(속도 -" + Pct(1f - ChillSpeed) + ", " + DefaultTurns(kind) + "턴).";
                case StatusKind.InnerWound: return "내상: 내공이 차오르지 않습니다(" + DefaultTurns(kind) + "턴).";
                case StatusKind.Bleed: return "출혈: 공격을 맞을 때마다 최대 체력의 " + Pct(BleedPct) + "를 더 잃습니다(" + DefaultTurns(kind) + "턴).";
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
            float pct = kind == StatusKind.Poison ? PoisonPct : kind == StatusKind.Burn ? BurnPct : kind == StatusKind.Bleed ? BleedPct : 0f;
            if (pct <= 0f) return 0;
            return Math.Max(1, (int)Math.Round(unit.Stats.MaxHp * pct * DotFactor(unit)));
        }

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
            { "물어뜯기", StatusKind.Bleed }, { "할퀴기", StatusKind.Bleed },
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
