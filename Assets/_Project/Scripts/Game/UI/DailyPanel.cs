using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>일일 수련 임무 overlay: today's three missions, the all-clear bonus and the streak.</summary>
    public sealed class DailyPanel
    {
        private readonly RectTransform _root;
        private readonly Action _onChange;

        public DailyPanel(RectTransform root, Action onChange)
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
            var missions = Daily.Missions(save);
            CodexPanel.Clear(_root);
            _root.gameObject.SetActive(true);
            if (!Tutorial.IsDone(save, "daily") && HelpPanel.Instance != null) HelpPanel.Instance.Show("daily", true);

            UiFactory.Label("Title", _root, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "일일 수련 임무", 44, UiFactory.Gold);
            int next = Daily.NextMilestone(save);
            UiFactory.Label("Streak", _root, new Vector2(0.05f, 0.855f), new Vector2(0.95f, 0.92f),
                "연속 수련 " + save.DailyStreak + "일" + (next > 0 ? "  ·  " + next + "일째 보상까지 " + (next - save.DailyStreak) + "일" : "  ·  모든 연속 보상 달성"), 28, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);

            for (int i = 0; i < missions.Count; i++)
            {
                var m = missions[i];
                float y1 = 0.83f - i * 0.17f;
                var band = UiFactory.Panel("Mission" + i, _root, new Vector2(0.05f, y1 - 0.15f), new Vector2(0.95f, y1), UiFactory.Ink);
                UiFactory.Frame(band.transform, UiFactory.GoldDim, 2f);
                bool done = Daily.IsDone(save, m), claimed = Daily.IsClaimed(save, i);
                string tier = m.Tier == 0 ? "쉬움" : m.Tier == 1 ? "보통" : "어려움";
                UiFactory.Label("Name", band.transform, new Vector2(0.04f, 0.56f), new Vector2(0.66f, 0.96f), "[" + tier + "] " + m.Name, 30, claimed ? UiFactory.InkSoft : UiFactory.PaperText, TextAnchor.MiddleLeft);
                int prog = Daily.ProgressOf(save, m);
                var bar = UiFactory.MakeBar("Bar", band.transform, new Vector2(0.04f, 0.32f), new Vector2(0.62f, 0.46f), new Color(0f, 0f, 0f, 0.7f), done ? UiFactory.Gold : new Color(0.38f, 0.62f, 0.95f, 1f));
                bar.SetPct(m.Target > 0 ? (float)prog / m.Target : 0f);
                UiFactory.Label("Count", band.transform, new Vector2(0.64f, 0.28f), new Vector2(0.70f, 0.50f), prog + "/" + m.Target, 20, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                UiFactory.Label("Reward", band.transform, new Vector2(0.04f, 0.04f), new Vector2(0.66f, 0.30f), "보상 두 " + Daily.RewardDoo(save, m).ToString("N0"), 20, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                int index = i;
                var btn = UiFactory.Button("Claim", band.transform, new Vector2(0.72f, 0.16f), new Vector2(0.97f, 0.84f), claimed ? "수령 완료" : done ? "받기" : "진행 중", done && !claimed ? UiFactory.Red : UiFactory.InkSoft, () => Claim(index), 26);
                btn.interactable = done && !claimed;
            }

            string bonus = save.DailyAllClaimed ? "오늘의 세 임무를 모두 끝냈다!" : "세 임무를 모두 받으면 보너스: 두 " + Daily.AllClearDoo(save).ToString("N0") + " + 웅담 2개, 연속 수련 +1일";
            UiFactory.Label("Bonus", _root, new Vector2(0.05f, 0.26f), new Vector2(0.95f, 0.33f), bonus, 24, UiFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Normal);
            UiFactory.Label("Milestones", _root, new Vector2(0.05f, 0.17f), new Vector2(0.95f, 0.26f), "연속 보상: 3일 옥루주 3 · 7일 인형설삼 · 14일 일급영단 · 30일 일급영단 2 + 두 20,000", 20, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);
            UiFactory.Label("Toast", _root, new Vector2(0.05f, 0.09f), new Vector2(0.95f, 0.17f), toast ?? "임무는 내일 0시에 새로 바뀐다 (기기 시간 기준)", 22, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);
            UiFactory.Button("Close", _root, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.08f), "닫기", UiFactory.InkSoft, () => { _root.gameObject.SetActive(false); _onChange(); }, 36);
        }

        private void Claim(int index)
        {
            var save = GameManager.Instance.Save;
            var notes = Daily.Claim(save, index);
            if (notes.Count == 0) return;
            GameManager.Instance.SaveNow();
            SoundManager.Sfx("levelup");
            Open(string.Join("\n", notes));
        }
    }
}
