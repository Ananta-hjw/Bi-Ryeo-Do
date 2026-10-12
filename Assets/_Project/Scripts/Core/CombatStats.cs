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

        public static CombatStats Scale(CombatStats s, float multiplier)
        {
            s.MaxHp = (int)Math.Round(s.MaxHp * multiplier);
            s.Attack = (int)Math.Round(s.Attack * multiplier);
            s.Defense = (int)Math.Round(s.Defense * multiplier);
            return s;
        }
    }
}
