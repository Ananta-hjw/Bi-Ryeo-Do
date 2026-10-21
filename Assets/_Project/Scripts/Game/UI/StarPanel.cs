using System;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>별 보상 overlay: the star milestones and their chests.</summary>
    public sealed class StarPanel : OverlayPanel
    {

        public StarPanel(RectTransform root, Action onChange) : base(root, onChange)
        {
        }

        public void Open(string toast = null)
        {
            var save = GameManager.Instance.Save;
            Begin();
            if (!Tutorial.IsDone(save, "stars") && HelpPanel.Instance != null) HelpPanel.Instance.Show("stars", true);

            int total = StageStars.Total(save), ready = StageStars.ClaimableCount(save);
            UiFactory.Label("Title", Root, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "별 보상  ★ " + total + " / " + StageStars.MaxStars, 44, UiFactory.Gold);
            UiFactory.ScrollBody(UiFactory.Label("Hint", Root, new Vector2(.03f, .14f), new Vector2(.27f, .78f), toast ?? "전투(작은 스테이지)마다 별 3개: 돌파 · 전원 생존 · 신속. 난이도마다 따로 모읍니다.", 30, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal), 30);
            var all = UiFactory.Button("ClaimAll", Root, new Vector2(.03f, .80f), new Vector2(.27f, .90f), "모두 받기", ready > 0 ? UiFactory.Red : UiFactory.InkSoft, ClaimAll, 28);
            all.interactable = ready > 0;

            var content = CodexPanel.Scroller(Root, .10f, .90f);
            LandscapeMenu.Place((RectTransform)content.parent, .30f, .10f, .98f, .90f);
            for (int i = 0; i < StageStars.Chests.Length; i++)
            {
                var c = StageStars.Chests[i];
                var chest = c;
                var band = CodexPanel.Band(content, i);
                bool claimed = save.ClaimedStarChests.Contains(c.Stars), can = StageStars.CanClaim(save, c);
                UiFactory.Label("Name", band, new Vector2(0.04f, 0.54f), new Vector2(0.66f, 0.97f), "★ " + c.Stars + "개", 34, claimed ? UiFactory.InkSoft : UiFactory.Gold, TextAnchor.MiddleLeft);
                string item = "";
                if (!string.IsNullOrEmpty(c.ItemId)) { var it = EarlyGameContent.FindItem(c.ItemId); if (it != null) item = " + " + it.Name + (c.ItemCount > 1 ? " x" + c.ItemCount : ""); }
                UiFactory.Label("Reward", band, new Vector2(0.04f, 0.04f), new Vector2(0.66f, 0.54f), "두 " + c.Doo.ToString("N0") + item + (claimed || can ? "" : "  (" + Math.Max(0, c.Stars - total) + "개 더)"), 30, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                var btn = UiFactory.Button("Claim", band, new Vector2(0.69f, 0.14f), new Vector2(0.97f, 0.86f), claimed ? "받음" : can ? "받기" : "잠김", can ? UiFactory.Red : UiFactory.InkSoft, () => ClaimOne(chest), 26);
                btn.interactable = can;
            }
            SetListHeight(content, StageStars.Chests.Length);
            AddClose();
        }

        private void ClaimOne(StarChest chest)
        {
            var note = StageStars.Claim(GameManager.Instance.Save, chest);
            if (note == null) return;
            GameManager.Instance.SaveNow();
            SoundManager.Sfx("levelup");
            Open(note);
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
