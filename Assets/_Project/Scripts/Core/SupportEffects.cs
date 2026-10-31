using System;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// Player-facing words for the companion support effects (one owner each: 은신 장홍, 일행 치유 윤준호, 대신 막기 임성진, 반격 태세·기세 충전 남궁상,
    /// 결계·집중 모용휘) and for the party guard of 효룡. Numbers come from <see cref="InteractiveBattle"/>.
    /// </summary>
    public static class SupportEffects
    {
        /// <summary>One line of what the art does, or null for an effect the screens already describe (attack, 회피, 치유, 정화, 점혈).</summary>
        public static string Describe(SkillSpec s)
        {
            if (s == null) return null;
            switch (s.Effect)
            {
                case SkillEffect.GuardParty:
                    return "파티 전체 방어: 시전자의 다음 " + InteractiveBattle.PartyGuardTurns + "차례 동안 받는 피해 -" + Pct(1f - InteractiveBattle.PartyGuardDamageFactor);
                case SkillEffect.Stealth: return "다음 차례까지 적의 표적에서 사라짐 · 다음 공격 확정 치명타";
                case SkillEffect.HealParty: return "일행 모두 체력 " + Pct(s.HealPct) + " 회복";
                case SkillEffect.Cover: return "다음 차례까지 적의 단일 공격을 모두 대신 받음 · 받는 피해 -" + Pct(1f - InteractiveBattle.CoverDamageFactor);
                case SkillEffect.CounterStance: return "다음 차례까지 자신을 친 적에게 반격(위력 x" + InteractiveBattle.CounterStancePower.ToString("0.#") + ")";
                case SkillEffect.PartySpirit: return "일행 모두 기세 +" + InteractiveBattle.PartySpiritGain;
                case SkillEffect.Ward: return "일행 모두 " + InteractiveBattle.WardTurns + "차례 동안 상태 이상에 걸리지 않음";
                case SkillEffect.Focus: return "다음 공격 피해 +" + Pct(InteractiveBattle.FocusDamageFactor - 1f) + " · 그때까지 받는 피해 +" + Pct(InteractiveBattle.FocusTakenFactor - 1f);
                default: return null;
            }
        }

        /// <summary>A short chip label (「은신」, 「일행 치유 15%」 …), or null as for <see cref="Describe"/>.</summary>
        public static string Chip(SkillSpec s)
        {
            if (s == null) return null;
            switch (s.Effect)
            {
                case SkillEffect.GuardParty: return "파티 방어";
                case SkillEffect.Stealth: return "은신";
                case SkillEffect.HealParty: return "일행 치유 " + Pct(s.HealPct);
                case SkillEffect.Cover: return "대신 막기";
                case SkillEffect.CounterStance: return "반격 태세";
                case SkillEffect.PartySpirit: return "기세 +" + InteractiveBattle.PartySpiritGain;
                case SkillEffect.Ward: return "결계";
                case SkillEffect.Focus: return "집중";
                default: return null;
            }
        }

        /// <summary>True for an art that does not strike (the screens tint it as support).</summary>
        public static bool IsSupport(SkillEffect e) { return e != SkillEffect.None && e != SkillEffect.Stun; }

        private static string Pct(float f) { return Math.Round(f * 100f) + "%"; }
    }
}
