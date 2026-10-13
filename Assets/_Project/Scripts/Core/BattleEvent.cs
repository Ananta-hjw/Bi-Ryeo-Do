using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    public enum EventKind { Attack, Guard, Meditate, Charge, Evade }

    /// <summary>One action in a battle. UI plays these back; the engine never touches UI.</summary>
    public sealed class BattleEvent
    {
        public int Turn;
        public string ActorId;
        public string TargetId;
        public string SkillId;
        public int Damage;
        public bool IsCrit;
        public int TargetHpAfter;
        public bool TargetDefeated;

        // Interactive battle extras (the auto engine leaves these at defaults).
        public EventKind Kind;
        public string Message;       // e.g. "호랑이가 기를 모은다!"
        public bool Dodged;          // the hit was evaded entirely
        public bool Guarded;         // damage was reduced by a guard
        public int HitIndex;         // 0-based within a multi-hit skill
        public int HitCount = 1;
        public int Heal;             // HP restored to the actor (meditate)
    }

    public sealed class BattleResult
    {
        public Team? Winner;   // null = timed out (treated as a loss for the player)
        public int Turns;
        public List<BattleEvent> Events = new List<BattleEvent>();

        public bool PlayerWon { get { return Winner == Team.Player; } }
    }
}
