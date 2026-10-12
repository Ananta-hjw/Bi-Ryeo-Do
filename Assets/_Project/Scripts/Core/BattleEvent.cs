using System.Collections.Generic;

namespace BiRyeoDo.Core
{
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
    }

    public sealed class BattleResult
    {
        public Team? Winner;   // null = timed out (treated as a loss for the player)
        public int Turns;
        public List<BattleEvent> Events = new List<BattleEvent>();

        public bool PlayerWon { get { return Winner == Team.Player; } }
    }
}
