using BiRyeoDo.Core;
using UnityEngine;

namespace BiRyeoDo.Game.Data
{
    [CreateAssetMenu(menuName = "BiRyeoDo/Enemy", fileName = "Enemy_New")]
    public class EnemyDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public bool isBoss;
        public Sprite portrait;
        public CombatStats stats = new CombatStats { MaxHp = 50, MaxQi = 0, Attack = 5, Defense = 0, Speed = 100, CritRate = 0, CritDamage = 1.5f };
        public MartialArtDef[] arts;
        public long expReward = 10;
        public long dooReward = 5;

        public Combatant Create()
        {
            var specs = new System.Collections.Generic.List<SkillSpec>();
            if (arts != null) foreach (var a in arts) if (a != null) specs.Add(a.ToSpec());
            return new Combatant(id, displayName, Team.Enemy, stats, specs);
        }
    }
}
