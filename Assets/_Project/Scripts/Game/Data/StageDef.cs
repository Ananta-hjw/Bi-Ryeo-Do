using UnityEngine;

namespace BiRyeoDo.Game.Data
{
    /// <summary>A stage (e.g. "스테이지 3 · 안개 대숲") with flavor text, normal enemies and a boss.</summary>
    [CreateAssetMenu(menuName = "BiRyeoDo/Stage", fileName = "Stage_New")]
    public class StageDef : ScriptableObject
    {
        public int number = 1;
        public string displayName;
        [TextArea] public string storyText;
        public Sprite background;
        public EnemyDef[] enemies;
        public EnemyDef boss;
        [Tooltip("Normal wins needed before the boss challenge unlocks.")]
        public int killsToBoss = 10;
    }
}
