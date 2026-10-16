using System;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Shared shell of the full-screen overlays (and the embedded pages of the 무공 tab) (보상·세력·수련·추억·임무 ...): dim background, hide, close button, list height.</summary>
    public abstract class OverlayPanel
    {
        protected readonly RectTransform Root;
        protected readonly Action OnChange;

        protected OverlayPanel(RectTransform root, Action onChange)
        {
            Root = root;
            OnChange = onChange;
            root.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
            root.gameObject.SetActive(false);
        }

        /// <summary>True when the panel is a page of a tab (no 닫기 button) instead of a pop-up.</summary>
        public bool Embedded;

        public void Hide() { Root.gameObject.SetActive(false); }

        /// <summary>Wipes the previous drawing and shows the panel (call at the start of Open).</summary>
        protected void Begin()
        {
            CodexPanel.Clear(Root);
            Root.gameObject.SetActive(true);
        }

        /// <summary>The 닫기 button at the bottom; closing refreshes the 강호 tab.</summary>
        protected void AddClose()
        {
            if (Embedded) return;
            UiFactory.Button("Close", Root, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.08f), "닫기", UiFactory.InkSoft, () => { Root.gameObject.SetActive(false); OnChange(); }, 36);
        }

        protected static void SetListHeight(RectTransform content, int rows)
        {
            content.sizeDelta = new Vector2(0f, CodexPanel.RowGap + rows * (CodexPanel.RowH + CodexPanel.RowGap));
        }
    }
}
