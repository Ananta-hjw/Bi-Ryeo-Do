using BiRyeoDo.Core;
using UnityEngine;

namespace BiRyeoDo.Game.Data
{
    /// <summary>무공 definition authored in the editor. Converted to a Core SkillSpec for battle.</summary>
    [CreateAssetMenu(menuName = "BiRyeoDo/Martial Art", fileName = "Art_New")]
    public class MartialArtDef : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public float power = 1f;
        public int qiCost;
        public int cooldown;
        public long learnCost;

        public SkillSpec ToSpec()
        {
            return new SkillSpec { Id = id, Name = displayName, Power = power, QiCost = qiCost, Cooldown = cooldown };
        }
    }
}
