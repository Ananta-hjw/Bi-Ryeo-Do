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

            // Six missions do not fit one screen: they scroll between the streak line and the bonus text.
            const float bandHeight = 210f, bandGap = 14f;
            var viewport = UiFactory.Rect("MissionViewport", Root, new Vector2(0.05f, 0.34f), new Vector2(0.95f, 0.85f));
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UiFactory.Rect("MissionContent", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f));
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(0f, missions.Count * (bandHeight + bandGap));
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            UiFactory.ScrollIndicator(scroll, viewport);

            for (int i = 0; i < missions.Count; i++)
            {
                var m = missions[i];
                var band = UiFactory.Panel("Mission" + i, content, Vector2.zero, Vector2.one, UiFactory.Ink);
                var bandRect = band.rectTransform;
                bandRect.anchorMin = new Vector2(0f, 1f);
                bandRect.anchorMax = new Vector2(1f, 1f);
                bandRect.pivot = new Vector2(0.5f, 1f);
                bandRect.offsetMin = new Vector2(0f, -(i * (bandHeight + bandGap) + bandHeight));
                bandRect.offsetMax = new Vector2(0f, -(i * (bandHeight + bandGap)));
                UiFactory.Frame(band.transform, UiFactory.GoldDim, 2f);
                bool done = Daily.IsDone(save, m), claimed = Daily.IsClaimed(save, i);
                string tier = m.Tier == 0 ? "쉬움" : m.Tier == 1 ? "보통" : "어려움";
                UiFactory.Label("Name", band.transform, new Vector2(0.04f, 0.56f), new Vector2(0.66f, 0.96f), "[" + tier + "] " + m.Name, 30, claimed ? UiFactory.InkSoft : UiFactory.PaperText, TextAnchor.MiddleLeft);
                int prog = Daily.ProgressOf(save, m);
                var bar = UiFactory.MakeBar("Bar", band.transform, new Vector2(0.04f, 0.32f), new Vector2(0.62f, 0.46f), new Color(0f, 0f, 0f, 0.7f), done ? UiFactory.Gold : new Color(0.38f, 0.62f, 0.95f, 1f));
                bar.SetPct(m.Target > 0 ? (float)prog / m.Target : 0f);
                UiFactory.Label("Count", band.transform, new Vector2(0.04f, 0.28f), new Vector2(0.66f, 0.50f), prog + "/" + m.Target, 30, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                UiFactory.ScrollBody(UiFactory.Label("Reward", band.transform, new Vector2(0.04f, 0.04f), new Vector2(0.66f, 0.30f), "보상 두 " + Daily.RewardDoo(save, m).ToString("N0") + "\n" + Gear.RewardMaterial(save.HighestUnlockedStage).Name + " " + Gear.DailyPolish + "개", 28, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal), 28);
                int index = i;
                var btn = UiFactory.Button("Claim", band.transform, new Vector2(0.72f, 0.16f), new Vector2(0.97f, 0.84f), claimed ? "수령 완료" : done ? "받기" : "진행 중", done && !claimed ? UiFactory.Red : UiFactory.InkSoft, () => Claim(index), 26);
                btn.interactable = done && !claimed;
            }

            string bonus = Daily.AllClaimed(save) ? "오늘의 여섯 임무를 모두 끝냈습니다!" : "여섯 임무를 모두 받으면 보너스: 두 " + Daily.AllClearDoo(save).ToString("N0") + " + 웅담 2개, 연속 수련 +1일";
            UiFactory.ScrollBody(UiFactory.Label("Bonus", Root, new Vector2(0.05f, 0.26f), new Vector2(0.95f, 0.33f), bonus, 30, UiFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Normal), 30);
            UiFactory.ScrollBody(UiFactory.Label("Milestones", Root, new Vector2(0.05f, 0.17f), new Vector2(0.95f, 0.26f), "연속 보상: 3일 옥루주 3 · 7일 인형설삼 · 14일 일급영단+심득석 2 · 30일마다 일급영단 2+심득석 4 + 두 31,200", 30, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal), 30);
            UiFactory.ScrollBody(UiFactory.Label("Toast", Root, new Vector2(0.05f, 0.09f), new Vector2(0.95f, 0.17f), toast ?? "임무는 내일 0시에 새로 바뀝니다 (기기 시간 기준)", 30, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 30);
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
