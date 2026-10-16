using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>나예린 추억 overlay: the scenes unlocked by story progress. Watching one for the first time pays out.</summary>
    public sealed class MemoryPanel
    {
        private readonly RectTransform _root;
        private readonly Action _onChange;
        private readonly Action<string, string[], Action> _play;

        public MemoryPanel(RectTransform root, Action onChange, Action<string, string[], Action> play)
        {
            _root = root;
            _onChange = onChange;
            _play = play;
            root.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
            root.gameObject.SetActive(false);
        }

        public void Hide() { _root.gameObject.SetActive(false); }

        public void Open(string toast = null)
        {
            var save = GameManager.Instance.Save;
            CodexPanel.Clear(_root);
            _root.gameObject.SetActive(true);
            if (!Tutorial.IsDone(save, "memories") && HelpPanel.Instance != null) HelpPanel.Instance.Show("memories", true);

            var next = Memories.NextMilestone(save);
            UiFactory.Label("Title", _root, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "나예린 추억  " + Memories.SeenCount(save) + " / " + Memories.All.Length, 40, UiFactory.Gold);
            UiFactory.Label("Hint", _root, new Vector2(0.05f, 0.845f), new Vector2(0.95f, 0.92f),
                toast ?? ("원작의 장면을 다시 본다. 처음 볼 때 두와 인연 +" + Memories.BondPerMemory + "." + (next != null ? "  추억 " + next.Count + "개를 보면 특별 보상." : "")), 22, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);

            var content = CodexPanel.Scroller(_root, 0.09f, 0.84f);
            for (int i = 0; i < Memories.All.Length; i++)
            {
                var m = Memories.All[i];
                var band = CodexPanel.Band(content, i);
                bool open = Memories.IsUnlocked(save, m), seen = Memories.IsSeen(save, m.Id);
                UiFactory.Label("Name", band, new Vector2(0.04f, 0.54f), new Vector2(0.66f, 0.97f), open ? m.Title : "???", 32, open ? (seen ? UiFactory.InkSoft : UiFactory.PaperText) : UiFactory.InkSoft, TextAnchor.MiddleLeft);
                UiFactory.Label("Info", band, new Vector2(0.04f, 0.04f), new Vector2(0.66f, 0.54f),
                    open ? m.Volume + "권" + (seen ? " · 본 장면" : " · 새 장면  보상 두 " + m.RewardDoo.ToString("N0")) : "스테이지 " + m.UnlockStage + " 돌파 후 열린다", 20, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                var memory = m;
                var btn = UiFactory.Button("Play", band, new Vector2(0.69f, 0.14f), new Vector2(0.97f, 0.86f), !open ? "잠김" : seen ? "다시 보기" : "보기", open && !seen ? UiFactory.Red : UiFactory.InkSoft, () => Play(memory), 28);
                btn.interactable = open;
            }
            content.sizeDelta = new Vector2(0f, CodexPanel.RowGap + Memories.All.Length * (CodexPanel.RowH + CodexPanel.RowGap));
            UiFactory.Button("Close", _root, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.08f), "닫기", UiFactory.InkSoft, () => { _root.gameObject.SetActive(false); _onChange(); }, 36);
        }

        private void Play(MemoryData m)
        {
            _root.gameObject.SetActive(false);
            _play(m.Title, m.Lines, () =>
            {
                var save = GameManager.Instance.Save;
                var notes = Memories.Complete(save, m.Id);
                if (notes.Count > 0) { GameManager.Instance.SaveNow(); SoundManager.Sfx("levelup"); }
                Open(notes.Count > 0 ? string.Join(" / ", notes) : null);
            });
        }
    }
}
