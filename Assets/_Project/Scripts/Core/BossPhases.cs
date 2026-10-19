using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>One stage of a boss fight: from <see cref="HpPct"/> of max HP down the boss fights with a new script and a harder hand.</summary>
    public sealed class BossPhase
    {
        public float HpPct;          // starts once HP falls to this fraction of max
        public string[] Lines;       // StoryLine lines played at the switch ("화자|대사" or narration)
        public EnemyMove[] Moves;    // the script from here on (null = keep the current one)
        public float AtkMul = 1f, DefMul = 1f;   // attack and defense from here on, as multiples of the boss's own
    }

    /// <summary>
    /// 보스 단계 전환: a story boss (<see cref="BossPhaseLines"/>) changes its fight twice, at <see cref="FirstPct"/> and <see cref="SecondPct"/> of its HP
    /// (a boss that retreats changes before it leaves, <see cref="RetreatPhasePct"/>). Each switch plays a few lines, costs the boss its action gauge
    /// (the change of stance is a beat the party can use) and reshapes the script from the boss's own moves (<see cref="Reshape"/>):
    /// phase 1 plays the light moves in reverse order before the wind-up and hits harder (<see cref="FirstAtk"/>),
    /// phase 2 opens with the wind-up at once, hits harder still and takes less (<see cref="SecondAtk"/>, <see cref="SecondDef"/>).
    /// <see cref="InteractiveBattle"/> applies it (<see cref="EventKind.Phase"/>); a switch during a wind-up waits until that heavy blow is done.
    /// </summary>
    public static class BossPhases
    {
        public const float FirstPct = 0.5f, SecondPct = 0.25f;
        public const float RetreatPhasePct = 0.4f;   // second switch of a boss that withdraws at 30%
        public const float FirstAtk = 1.1f, FirstDef = 1f;    // 50%: attack +10%
        public const float SecondAtk = 1.3f, SecondDef = 1.2f;  // 25%: attack +30% in all, defense +20%

        /// <summary>The phases of this enemy, in the order they trigger (null for anything but a story boss with lines).</summary>
        public static List<BossPhase> For(EnemyData enemy)
        {
            string[] first, second;
            if (enemy == null || !enemy.IsBoss || !BossPhaseLines.TryGet(enemy.Id, out first, out second)) return null;
            float secondPct = enemy.RetreatHpPct > 0f ? System.Math.Max(RetreatPhasePct, enemy.RetreatHpPct + 0.05f) : SecondPct;
            EnemyMove[] fierce, desperate;
            Reshape(enemy.Moves, out fierce, out desperate);
            return new List<BossPhase>
            {
                new BossPhase { HpPct = FirstPct, Lines = first, Moves = fierce, AtkMul = FirstAtk, DefMul = FirstDef },
                new BossPhase { HpPct = secondPct, Lines = second, Moves = desperate, AtkMul = SecondAtk, DefMul = SecondDef },
            };
        }

        /// <summary>
        /// From a script [light moves…, wind-up, heavy] (one wind-up followed by its heavy blow):
        /// phase 1 = [light moves reversed, wind-up, heavy], phase 2 = [wind-up, heavy, light moves]. A script without a wind-up is kept as it is.
        /// </summary>
        public static void Reshape(EnemyMove[] moves, out EnemyMove[] fierce, out EnemyMove[] desperate)
        {
            fierce = desperate = null;
            if (moves == null) return;
            int wind = -1;
            for (int i = 0; i < moves.Length; i++) if (moves[i].Charge) { wind = i; break; }
            if (wind < 0 || wind + 1 >= moves.Length) return;
            var light = new List<EnemyMove>();
            for (int i = 0; i < moves.Length; i++) if (i != wind && i != wind + 1) light.Add(moves[i]);
            if (light.Count == 0) return;
            var heavy = moves[wind + 1];
            // phase 1: the light moves in reverse order, then the wind-up; phase 2: the wind-up at once, then the light moves
            var f = new List<EnemyMove>(light); f.Reverse(); f.Add(moves[wind]); f.Add(heavy);
            var d = new List<EnemyMove> { moves[wind], heavy }; d.AddRange(light);
            fierce = f.ToArray();
            desperate = d.ToArray();
        }

        /// <summary>Player-facing summary for the enemy tip ("" for an enemy without phases).</summary>
        public static string Describe(Combatant unit)
        {
            if (unit.Phases == null || unit.Phases.Count == 0) return "";
            if (unit.PhaseIndex >= unit.Phases.Count) return "단계: 마지막 단계(공격력 +" + Pct(SecondAtk) + ", 방어력 +" + Pct(SecondDef) + ")";
            var next = unit.Phases[unit.PhaseIndex];
            return "단계 " + (unit.PhaseIndex + 1) + ": 체력 " + (int)System.Math.Round(next.HpPct * 100f) + "% 아래로 떨어지면 기세가 바뀝니다(공격력 +" + Pct(next.AtkMul)
                + (next.DefMul > 1f ? ", 방어력 +" + Pct(next.DefMul) : "") + ").";
        }

        private static string Pct(float mul) { return (int)System.Math.Round((mul - 1f) * 100f) + "%"; }
    }
}
