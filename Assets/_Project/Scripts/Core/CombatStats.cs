using System;

namespace BiRyeoDo.Core
{
    /// <summary>Base combat numbers. Speed drives turn order (replaces the reference game's "attacks per second").</summary>
    [Serializable]
    public struct CombatStats
    {
        public int MaxHp;
        public int MaxQi;       // 내공: resource spent by martial arts
        public int Attack;
        public int Defense;
        public int Speed;       // gauge gained per tick; higher acts more often
        public float CritRate;  // 0..1
        public float CritDamage; // multiplier, e.g. 1.5
        /// <summary>
        /// Display units: what one shown point of attack / defense is worth in a fight (0 = 1). An enemy's attack and defense are shown re-balanced against each other
        /// (see <see cref="EnemyData.DisplayRatio"/>), so a point of its attack counts for more than 1 and a point of its defense for less; the damage formula and
        /// <see cref="Realm.Power"/> read <see cref="EffAttack"/> / <see cref="EffDefense"/>, which are the same fight values as before.
        /// </summary>
        public float AtkUnit, DefUnit;

        public double EffAttack { get { return Attack * (AtkUnit > 0f ? AtkUnit : 1f); } }
        public double EffDefense { get { return Defense * (DefUnit > 0f ? DefUnit : 1f); } }

        public static CombatStats Scale(CombatStats s, float multiplier)
        {
            s.MaxHp = (int)Math.Round(s.MaxHp * multiplier);
            s.Attack = (int)Math.Round(s.Attack * multiplier);
            s.Defense = (int)Math.Round(s.Defense * multiplier);
            return s;
        }
    }
}
