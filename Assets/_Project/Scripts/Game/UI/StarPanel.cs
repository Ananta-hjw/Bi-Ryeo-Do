using System;
using System.Collections.Generic;
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
            UiFactory.ScrollBody(UiFactory.Label("Hint", Root, new Vector2(.04f, .81f), new Vector2(.74f, .91f), (toast ?? "전투마다 돌파 · 전원 생존 · 신속 별을 모아 보상을 받습니다.")
                + (save.RetiredStoryStars > 0 ? "\n개편 전 획득 별 " + save.RetiredStoryStars + "개 포함" : ""), 28, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal), 28);
            var all = UiFactory.Button("ClaimAll", Root, new Vector2(.77f, .81f), new Vector2(.97f, .91f), "모두 받기", ready > 0 ? UiFactory.Red : UiFactory.InkSoft, ClaimAll, 28);
            all.interactable = ready > 0;
            var content = CodexPanel.Scroller(Root, .16f, .79f);
            var grid = new ReadingGrid(content, 3, 300f);
            // Ready to open first, then the ones still needing stars, then the ones already taken
            var order = new List<int>();
            for (int rank = 0; rank < 3; rank++)
                for (int i = 0; i < StageStars.Chests.Length; i++)
                {
                    var chestNow = StageStars.Chests[i];
                    bool claimedNow = save.ClaimedStarChests.Contains(chestNow.Stars);
                    if ((claimedNow ? 2 : StageStars.CanClaim(save, chestNow) ? 0 : 1) == rank) order.Add(i);
                }
            foreach (int i in order)
            {
                var c = StageStars.Chests[i];
                var chest = c;
                var band = grid.Add("Chest" + i);
                bool claimed = save.ClaimedStarChests.Contains(c.Stars), can = StageStars.CanClaim(save, c);
                UiFactory.Label("Name", band, new Vector2(.04f, .74f), new Vector2(.96f, .96f), "★ " + c.Stars + "개", 34, claimed ? UiFactory.Dim : UiFactory.Gold, TextAnchor.MiddleLeft);
                string reward = c.Doo.ToString("N0") + "은자";
                if (!string.IsNullOrEmpty(c.ItemId)) reward += "\n" + EarlyGameContent.FindItem(c.ItemId)?.Name + " ×" + c.ItemCount;
                if (c.Insight > 0) reward += " · 심득석 " + c.Insight + "개";
                UiFactory.Label("Reward", band, new Vector2(.04f, .42f), new Vector2(.96f, .72f), reward, 28, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                UiFactory.Label("Progress", band, new Vector2(.04f, .28f), new Vector2(.96f, .41f), claimed ? "수령 완료" : can ? "수령 가능" : "별 " + Math.Max(0, c.Stars - total) + "개 더 필요", 26, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                var btn = UiFactory.Button("Claim", band, new Vector2(.04f, .04f), new Vector2(.96f, .25f), claimed ? "받음" : can ? "받기" : "잠김", can ? UiFactory.Red : UiFactory.InkSoft, () => ClaimOne(chest), 28);
                btn.interactable = can;
            }
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
