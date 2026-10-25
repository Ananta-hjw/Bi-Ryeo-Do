using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>일일 수련 임무 overlay: today's six missions, the all-clear bonus and the streak.</summary>
    public sealed class DailyPanel : OverlayPanel
    {

        public DailyPanel(RectTransform root, Action onChange) : base(root, onChange) { }

        public void Open(string toast = null)
        {
            var save = GameManager.Instance.Save;
            var missions = Daily.Missions(save);
            Begin();
            if (!Tutorial.IsDone(save, "daily") && HelpPanel.Instance != null) HelpPanel.Instance.Show("daily", true);

            UiFactory.Label("Title", Root, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "일일 수련 임무", 44, UiFactory.Gold);
            int next = Daily.NextMilestone(save);
            UiFactory.Label("Streak", Root, new Vector2(0.05f, 0.855f), new Vector2(0.95f, 0.92f),
                "연속 수련 " + save.DailyStreak + "일" + (next > 0 ? "  ·  " + next + "일째 보상까지 " + (next - save.DailyStreak) + "일" : ""), 28, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);

            var content = CodexPanel.Scroller(Root, .12f, .80f);
            var grid = new ReadingGrid(content, 3, 280f);
            // Ready to claim first, then still in progress, then already claimed (the claim keeps the mission's own index)
            var order = new List<int>();
            for (int rank = 0; rank < 3; rank++)
                for (int i = 0; i < missions.Count; i++)
                {
                    bool claimedNow = Daily.IsClaimed(save, i), doneNow = Daily.IsDone(save, missions[i]);
                    if ((claimedNow ? 2 : doneNow ? 0 : 1) == rank) order.Add(i);
                }
            foreach (int i in order)
            {
                var m = missions[i];
                var band = grid.Add("Mission" + i);
                bool done = Daily.IsDone(save, m), claimed = Daily.IsClaimed(save, i);
                string tier = m.Tier == 0 ? "쉬움" : m.Tier == 1 ? "보통" : "어려움";
                UiFactory.Label("Name", band, new Vector2(.04f, .68f), new Vector2(.96f, .96f), "[" + tier + "] " + m.Name, 30, claimed ? UiFactory.Dim : UiFactory.PaperText, TextAnchor.MiddleLeft);
                int prog = Daily.ProgressOf(save, m);
                var bar = UiFactory.MakeBar("Bar", band, new Vector2(.04f, .57f), new Vector2(.96f, .62f), new Color(0f, 0f, 0f, .7f), done ? UiFactory.Gold : new Color(.38f, .62f, .95f, 1f));
                bar.SetPct(m.Target > 0 ? (float)prog / m.Target : 0f);
                UiFactory.Label("Count", band, new Vector2(.04f, .42f), new Vector2(.96f, .55f), prog + " / " + m.Target, 28, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                UiFactory.Label("Reward", band, new Vector2(.04f, .28f), new Vector2(.96f, .41f), "보상 " + Daily.RewardDoo(save, m).ToString("N0") + "은자", 28, UiFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Normal);
                int index = i;
                var btn = UiFactory.Button("Claim", band, new Vector2(.04f, .04f), new Vector2(.96f, .25f), claimed ? "수령 완료" : done ? "받기" : "진행 중", done && !claimed ? UiFactory.Red : UiFactory.InkSoft, () => Claim(index), 28);
                btn.interactable = done && !claimed;
            }
            string bonus = Daily.AllClaimed(save) ? "오늘의 여섯 임무를 모두 끝냈습니다!" : "모두 수령 시: " + Daily.AllClearDoo(save).ToString("N0") + "은자 · 웅담 2개 · 심득석 1개 · 연속 수련 +1일";
            UiFactory.ScrollBody(UiFactory.Label("Bonus", Root, new Vector2(.04f, .02f), new Vector2(.96f, .11f), bonus, 28, UiFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Normal), 28);
            UiFactory.Label("Toast", Root, new Vector2(.04f, .80f), new Vector2(.96f, .85f), toast ?? "임무는 매일 0시에 바뀝니다 (기기 시간 기준)", 26, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);
            UiFactory.HelpButton(Root, () => { if (HelpPanel.Instance != null) HelpPanel.Instance.Show("daily"); });
            AddClose();
        }

        private void Claim(int index)
        {
            var save = GameManager.Instance.Save;
            var notes = Daily.Claim(save, index);
            if (notes.Count == 0) { Open(); return; }   // e.g. the day rolled over while the panel was open: redraw with the new missions
            GameManager.Instance.SaveNow();
            SoundManager.Sfx("levelup");
            Open(string.Join("\n", notes));
        }
    }
}
