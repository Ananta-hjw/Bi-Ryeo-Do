using UnityEngine;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Shows one route at a time. All seven routes are available from the bottom navigation.</summary>
    public class TabRouter : MonoBehaviour
    {
        // Append new routes so existing inspector values keep their original meaning.
        public enum Tab { World, MartialArts, Bag, Gacha, Record, Companions, Equipment }

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
