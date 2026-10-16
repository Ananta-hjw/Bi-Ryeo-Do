using UnityEngine;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Bottom tab bar: 강호 (battles) / 무공 (arts, 심법, 동료 수련) / 행낭 (items and gear) / 뽑기 (gacha) / 기록 (상태창, 도감, 업적, 추억, 세력). Shows one panel at a time.</summary>
    public class TabRouter : MonoBehaviour
    {
        public enum Tab { World, MartialArts, Bag, Gacha, Record }

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
