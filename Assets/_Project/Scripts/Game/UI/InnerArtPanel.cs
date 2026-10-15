using System;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>심법 overlay: learn passive methods with 두 and equip them into the build slots.</summary>
    public sealed class InnerArtPanel
    {
        private readonly RectTransform _root;
        private readonly Action _onChange;

        public InnerArtPanel(RectTransform root, Action onChange)
        {
            _root = root;
            _onChange = onChange;
            root.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
            root.gameObject.SetActive(false);
        }

        public void Open(string toast = null)
        {
            var save = GameManager.Instance.Save;
            if (!InnerArts.IsLearned(save, "breath")) save.LearnedInner.Add("breath");   // the free starter method
            CodexPanel.Clear(_root);
            _root.gameObject.SetActive(true);
            if (!Tutorial.IsDone(save, "inner") && HelpPanel.Instance != null) HelpPanel.Instance.Show("inner", true);

            int slots = InnerArts.SlotCount(save);
            UiFactory.Label("Title", _root, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "심법 · 장착 " + save.EquippedInner.Count + "/" + slots, 44, UiFactory.Gold);
            string slotNote = save.ClearedStage < 10 ? "칸이 늘어난다: 스테이지 10 돌파(2칸), 20 돌파(3칸)" : save.ClearedStage < 20 ? "세 번째 칸: 스테이지 20 돌파" : "모든 칸이 열렸다";
            UiFactory.Label("Hint", _root, new Vector2(0.05f, 0.855f), new Vector2(0.95f, 0.92f), toast ?? ("두 " + save.Doo.ToString("N0") + "  ·  " + slotNote), 26, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);

            var content = CodexPanel.Scroller(_root, 0.09f, 0.85f);
            for (int i = 0; i < InnerArts.All.Length; i++)
            {
                var a = InnerArts.All[i];
                bool unlocked = InnerArts.IsUnlocked(save, a), learned = InnerArts.IsLearned(save, a.Id), equipped = InnerArts.IsEquipped(save, a.Id);
                var band = CodexPanel.Band(content, i);
                UiFactory.Label("Name", band, new Vector2(0.04f, 0.58f), new Vector2(0.66f, 0.97f), (unlocked ? a.Name : "???") + (equipped ? "  [장착]" : ""), 32, unlocked ? UiFactory.PaperText : UiFactory.InkSoft, TextAnchor.MiddleLeft);
                UiFactory.Label("Effect", band, new Vector2(0.04f, 0.30f), new Vector2(0.66f, 0.60f), unlocked ? InnerArts.EffectText(a) : "스테이지 " + a.UnlockStage + "에 도달하면 열린다", 22, new Color(0.45f, 0.85f, 0.5f, 1f), TextAnchor.MiddleLeft, FontStyle.Normal);
                UiFactory.Label("Desc", band, new Vector2(0.04f, 0.02f), new Vector2(0.66f, 0.32f), unlocked ? a.Description : "", 18, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                var id = a.Id;
                string label; Color color; bool enabled;
                if (learned) { label = equipped ? "해제" : "장착"; color = equipped ? UiFactory.InkSoft : UiFactory.Red; enabled = true; }
                else if (!unlocked) { label = "잠김"; color = UiFactory.InkSoft; enabled = false; }
                else { label = "습득 " + a.LearnCost.ToString("N0") + "두"; enabled = save.Doo >= a.LearnCost; color = enabled ? UiFactory.Red : UiFactory.InkSoft; }
                var btn = UiFactory.Button("Act", band, new Vector2(0.69f, 0.14f), new Vector2(0.97f, 0.86f), label, color, () => Press(id), 26);
                btn.interactable = enabled;
            }
            content.sizeDelta = new Vector2(0f, CodexPanel.RowGap + InnerArts.All.Length * (CodexPanel.RowH + CodexPanel.RowGap));
            UiFactory.Button("Close", _root, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.08f), "닫기", UiFactory.InkSoft, () => { _root.gameObject.SetActive(false); _onChange(); }, 36);
        }

        private void Press(string id)
        {
            var save = GameManager.Instance.Save;
            var a = InnerArts.Find(id);
            string error = null, toast = null;
            if (!InnerArts.IsLearned(save, id))
            {
                if (InnerArts.Learn(save, id, out error)) { toast = a.Name + " 습득!"; SoundManager.Sfx("levelup"); }
            }
            else if (InnerArts.IsEquipped(save, id)) { InnerArts.Unequip(save, id); toast = a.Name + " 해제"; }
            else if (InnerArts.Equip(save, id, out error)) toast = a.Name + " 장착";
            GameManager.Instance.SaveNow();
            Open(toast ?? error);
        }
    }
}
