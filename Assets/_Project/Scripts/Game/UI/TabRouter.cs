using UnityEngine;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Bottom tab bar: 강호 (stage select) / 무공 (learn and equip) / 행낭 (items and gear) / 뽑기 (gacha). Shows one panel at a time.</summary>
    public class TabRouter : MonoBehaviour
    {
        public enum Tab { World, MartialArts, Bag, Gacha }

        [Tooltip("Panels indexed by the Tab enum order.")]
        public GameObject[] panels;
        public Tab startTab = Tab.World;

        private void Start() { Show((int)startTab); } // no-op until MainScreen has assigned panels

        public void Show(int index)
        {
            if (panels == null) return;
            for (int i = 0; i < panels.Length; i++)
                if (panels[i] != null) panels[i].SetActive(i == index);
        }
    }
}
