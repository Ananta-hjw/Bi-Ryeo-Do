using System;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>별 보상 overlay: the star milestones and their chests.</summary>
    public sealed class StarPanel
    {
        private readonly RectTransform _root;
        private readonly Action _onChange;

        public StarPanel(RectTransform root, Action onChange)
        {
            _root = root;
            _onChange = onChange;
            root.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
            root.gameObject.SetActive(false);
        }

        public void Hide() { _root.gameObject.SetActive(false); }

        public void Open(string toast = null)
        {
            var save = GameManager.Instance.Save;
            CodexPanel.Clear(_root);
            _root.gameObject.SetActive(true);
            if (!Tutorial.IsDone(save, "stars") && HelpPanel.Instance != null) HelpPanel.Instance.Show("stars", true);

            int total = StageStars.Total(save), ready = StageStars.ClaimableCount(save);
            UiFactory.Label("Title", _root, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "별 보상  ★ " + total + " / " + StageStars.MaxStars, 44, UiFactory.Gold);
            UiFactory.Label("Hint", _root, new Vector2(0.05f, 0.865f), new Vector2(0.62f, 0.92f), toast ?? "전투(작은 스테이지)마다 별 3개: 돌파 · 전원 생존 · 신속. 난이도마다 따로 모은다.", 22, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
            var all = UiFactory.Button("ClaimAll", _root, new Vector2(0.64f, 0.865f), new Vector2(0.95f, 0.92f), "모두 받기", ready > 0 ? UiFactory.Red : UiFactory.InkSoft, ClaimAll, 28);
            all.interactable = ready > 0;

            var content = CodexPanel.Scroller(_root, 0.09f, 0.855f);
            for (int i = 0; i < StageStars.Chests.Length; i++)
            {
                var c = StageStars.Chests[i];
                var band = CodexPanel.Band(content, i);
                bool claimed = save.ClaimedStarChests.Contains(c.Stars), can = StageStars.CanClaim(save, c);
                UiFactory.Label("Name", band, new Vector2(0.04f, 0.54f), new Vector2(0.66f, 0.97f), "★ " + c.Stars + "개", 34, claimed ? UiFactory.InkSoft : UiFactory.Gold, TextAnchor.MiddleLeft);
                string item = "";
                if (!string.IsNullOrEmpty(c.ItemId)) { var it = EarlyGameContent.FindItem(c.ItemId); if (it != null) item = " + " + it.Name + (c.ItemCount > 1 ? " x" + c.ItemCount : ""); }
                UiFactory.Label("Reward", band, new Vector2(0.04f, 0.04f), new Vector2(0.66f, 0.54f), "두 " + c.Doo.ToString("N0") + item + (claimed || can ? "" : "  (" + Math.Max(0, c.Stars - total) + "개 더)"), 22, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                var btn = UiFactory.Button("Claim", band, new Vector2(0.69f, 0.14f), new Vector2(0.97f, 0.86f), claimed ? "받음" : can ? "받기" : "잠김", can ? UiFactory.Red : UiFactory.InkSoft, ClaimAll, 26);
                btn.interactable = can;
            }
            content.sizeDelta = new Vector2(0f, CodexPanel.RowGap + StageStars.Chests.Length * (CodexPanel.RowH + CodexPanel.RowGap));
            UiFactory.Button("Close", _root, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.08f), "닫기", UiFactory.InkSoft, () => { _root.gameObject.SetActive(false); _onChange(); }, 36);
        }

        private void ClaimAll()
        {
            var note = StageStars.ClaimAll(GameManager.Instance.Save);
            if (note == null) return;
            GameManager.Instance.SaveNow();
            SoundManager.Sfx("levelup");
            Open(note);
        }
    }
}
