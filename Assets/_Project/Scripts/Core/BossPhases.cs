using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>One stage of a boss fight: from <see cref="HpPct"/> of max HP the boss gains stats and queues one awakening strike.</summary>
    public sealed class BossPhase
    {
        public float HpPct;          // starts once HP falls to this fraction of max
        public string[] Lines;       // StoryLine lines played at the switch ("화자|대사" or narration)
        public float AtkMul = 1f, DefMul = 1f;   // attack and defense from here on, as multiples of the boss's own
    }

    /// <summary>Boss phases keep the turn gauge and script position, inserting one awakening strike at the next normal action.</summary>
    public static class BossPhases
    {
        public const string AwakeningName = "각성:강타";
        public const float FirstAwakeningFactor = .70f, SecondAwakeningFactor = 1f;
        public const float FirstPct = 0.5f, SecondPct = 0.25f;
        public const float RetreatPhasePct = 0.4f;   // second switch of a boss that withdraws at 30%
        public const float FirstAtk = 1.1f, FirstDef = 1f;    // 50%: attack +10%
        public const float SecondAtk = 1.3f, SecondDef = 1.2f;  // 25%: attack +30% in all, defense +20%

        /// <summary>The phases of this enemy in trigger order: authored story lines or the shared tower-boss presentation.</summary>
        public static List<BossPhase> For(EnemyData enemy)
        {
            if (enemy != null && enemy.IsBoss && enemy.Id.StartsWith("tower", System.StringComparison.Ordinal)) return Tower.PhasesFor();
            string[] first, second;
            if (enemy == null || !enemy.IsBoss || !BossPhaseLines.TryGet(enemy.Id, out first, out second)) return null;
            float secondPct = enemy.RetreatHpPct > 0f ? System.Math.Max(RetreatPhasePct, enemy.RetreatHpPct + 0.05f) : SecondPct;
            return new List<BossPhase>
            {
                new BossPhase { HpPct = FirstPct, Lines = first, AtkMul = FirstAtk, DefMul = FirstDef },
                new BossPhase { HpPct = secondPct, Lines = second, AtkMul = SecondAtk, DefMul = SecondDef },
            };
        }

        /// <summary>Copies the boss's original charged blow; the source script is never mutated.</summary>
        public static EnemyMove CreateAwakening(IList<EnemyMove> moves, int phaseIndex)
        {
            EnemyMove heavy = null;
            if (moves != null)
            {
                for (int i = 0; i < moves.Count; i++)
                {
                    if (!moves[i].Charge) continue;
                    var next = moves[(i + 1) % moves.Count];
                    if (!next.Charge) { heavy = next; break; }
                }
                // Custom bosses without a wind-up still use one of their own damaging moves.
                if (heavy == null)
                    foreach (var move in moves)
                        if (!move.Charge && (heavy == null || move.Power * System.Math.Max(1, move.Hits) > heavy.Power * System.Math.Max(1, heavy.Hits))) heavy = move;
            }
            if (heavy == null) heavy = EnemyMove.Plain;
            return new EnemyMove
            {
                Name = AwakeningName, Power = heavy.Power, Hits = heavy.Hits, Inflict = heavy.Inflict, Ranged = heavy.Ranged,
                Awakening = true, DamageFactor = heavy.DamageFactor * (phaseIndex == 1 ? FirstAwakeningFactor : SecondAwakeningFactor)
            };
        }

        /// <summary>Player-facing summary for the enemy tip ("" for an enemy without phases).</summary>
        public static string Describe(Combatant unit)
        {
            if (unit.Phases == null || unit.Phases.Count == 0) return "";
            string rule = "1단계는 기존 강타 위력의 70%, 2단계는 100%로 각성:강타를 사용합니다. 한 번에 두 단계를 넘으면 100% 공격 한 번만 사용합니다. 이후 원래 예정된 행동을 이어갑니다.";
            string current = unit.AwakeningStrike != null ? "다음 행동: 각성:강타 (" + (unit.PhaseIndex == 1 ? "70%" : "100%") + ")\n" : "";
            if (unit.PhaseIndex >= unit.Phases.Count)
                return current + "단계: 마지막 단계(공격력 +" + Pct(SecondAtk) + ", 방어력 +" + Pct(SecondDef) + ")\n" + rule;
            var next = unit.Phases[unit.PhaseIndex];
            return current + "단계 " + (unit.PhaseIndex + 1) + ": 체력 " + (int)System.Math.Round(next.HpPct * 100f) + "% 아래로 떨어지면 기세가 바뀝니다(공격력 +" + Pct(next.AtkMul)
                + (next.DefMul > 1f ? ", 방어력 +" + Pct(next.DefMul) : "") + ").\n" + rule;
        }

        private static string Pct(float mul) { return (int)System.Math.Round((mul - 1f) * 100f) + "%"; }
    }
}
