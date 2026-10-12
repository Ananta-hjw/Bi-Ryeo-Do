using UnityEngine;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Bottom tab bar: 무공 / 경영 / 전투 / 기화 / 행낭. Shows one panel at a time.</summary>
    public class TabRouter : MonoBehaviour
    {
        public enum Tab { MartialArts, Management, Battle, Fortune, Inventory }

        [Tooltip("Panels indexed by the Tab enum order.")]
        public GameObject[] panels;
        public Tab startTab = Tab.Battle;

        private void Start() { Show((int)startTab); }

        /// <summary>Wire each tab button's OnClick to this with its enum index.</summary>
        public void Show(int index)
        {
            for (int i = 0; i < panels.Length; i++)
                if (panels[i] != null) panels[i].SetActive(i == index);
        }
    }
}
