using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>심법 overlay: learn passive methods with 두 and equip them into the build slots.</summary>
    public sealed class InnerArtPanel : OverlayPanel
    {

        /// <summary>Methods flagged new when the page was opened (tagged "새 심법" in the list); filled by MartialArtsScreen.</summary>
        public readonly HashSet<string> NewIds = new HashSet<string>();

        public InnerArtPanel(RectTransform root, Action onChange) : base(root, onChange) { }

        public void Open(string toast = null)
        {
            var save = GameManager.Instance.Save;
            if (!InnerArts.IsLearned(save, "breath")) save.LearnedInner.Add("breath");   // the free starter method
            Begin();
            if (!Tutorial.IsDone(save, "inner") && HelpPanel.Instance != null) HelpPanel.Instance.Show("inner", true);

            int slots = InnerArts.SlotCount(save);
            UiFactory.Label("Title", Root, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "심법 · 장착 " + save.EquippedInner.Count + "/" + slots, 44, UiFactory.Gold);
            string slotNote = slots >= 4 ? "모든 칸이 열렸습니다" : "다음 칸: " + (save.ClearedStage < 10 ? "스테이지 10 돌파" : save.ClearedStage < 20 ? "스테이지 20 돌파" : "어려움 스테이지 " + InnerArts.FourthSlotStage + " 돌파");
            UiFactory.ScrollBody(UiFactory.Label("Hint", Root, new Vector2(0.05f, 0.855f), new Vector2(0.95f, 0.92f), toast ?? ("두 " + save.Doo.ToString("N0") + "  ·  심득석 " + InnerArts.Stones(save) + "  ·  " + slotNote), 30, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 30);

            var content = CodexPanel.Scroller(Root, Embedded ? .02f : .10f, .85f);
            const float rowHeight = 360f, gap = 16f;
            RectTransform row = null;
            for (int i = 0; i < InnerArts.All.Length; i++)
            {
                var a = InnerArts.All[i];
                bool unlocked = InnerArts.IsUnlocked(save, a), learned = InnerArts.IsLearned(save, a.Id), equipped = InnerArts.IsEquipped(save, a.Id);
                if (i % 2 == 0)
                {
                    int rowIndex = i / 2;
                    row = UiFactory.Rect("Row" + rowIndex, content, new Vector2(.02f, 1f), new Vector2(.98f, 1f));
                    row.pivot = new Vector2(.5f, 1f);
                    row.sizeDelta = new Vector2(0f, rowHeight);
                    row.anchoredPosition = new Vector2(0f, -gap - rowIndex * (rowHeight + gap));
                    ReadingList.Register(row, rowHeight, gap);
                }
                float left = i % 2 == 0 ? 0f : .51f;
                var band = UiFactory.Panel("Card" + i, row, new Vector2(left, 0f), new Vector2(left + .49f, 1f), UiFactory.Ink).rectTransform;
                band.GetComponent<Image>().raycastTarget = true;
                UiFactory.Frame(band, equipped ? UiFactory.Gold : UiFactory.GoldDim, 2f);
                UiFactory.Label("Name", band, new Vector2(.04f, .78f), new Vector2(.96f, .97f), (unlocked ? a.Name : "???") + (InnerArts.Level(save, a.Id) > 0 ? "  +" + InnerArts.Level(save, a.Id) : "") + (equipped ? "  [장착]" : "") + (NewIds.Contains(a.Id) && !learned ? "  <color=#FF6A5A>[새 심법]</color>" : ""), 32, unlocked ? UiFactory.PaperText : UiFactory.InkSoft, TextAnchor.MiddleLeft);
                UiFactory.Label("Effect", band, new Vector2(.04f, .50f), new Vector2(.96f, .77f), unlocked ? InnerArts.EffectText(a, InnerArts.Level(save, a.Id)) : InnerArts.UnlockText(a) + "하면 열립니다", 30, new Color(0.45f, 0.85f, 0.5f, 1f), TextAnchor.MiddleLeft, FontStyle.Normal);
                UiFactory.Label("Desc", band, new Vector2(.04f, .32f), new Vector2(.96f, .49f), unlocked ? a.Description : "", 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                var id = a.Id;
                string label; Color color; bool enabled;
                if (learned) { label = equipped ? "해제" : "장착"; color = equipped ? UiFactory.InkSoft : UiFactory.Red; enabled = true; }
                else if (!unlocked) { label = "잠김"; color = UiFactory.InkSoft; enabled = false; }
                else { label = "습득 " + a.LearnCost.ToString("N0") + "두"; enabled = save.Doo >= a.LearnCost; color = enabled ? UiFactory.Red : UiFactory.InkSoft; }
                var btn = UiFactory.Button("Act", band, new Vector2(.04f, .025f), new Vector2(learned ? .47f : .96f, .30f), label, color, () => Press(id), 26);
                btn.interactable = enabled;
                if (learned)
                {
                    int cost = InnerArts.NextCost(save, id);
                    bool can = cost >= 0 && InnerArts.Stones(save) >= cost;
                    var up = UiFactory.Button("Enhance", band, new Vector2(.51f, .025f), new Vector2(.96f, .30f), cost < 0 ? "최대 강화" : "강화 심득석 " + cost, can ? UiFactory.Gold : new Color(0.30f, 0.30f, 0.33f, 1f), () => Enhance(id), 24);
                    up.interactable = can;
                }
            }
            content.sizeDelta = new Vector2(0f, gap + ((InnerArts.All.Length + 1) / 2) * (rowHeight + gap));
            Canvas.ForceUpdateCanvases();
            var layout = content.GetComponent<ReadingList>();
            if (layout != null) layout.Reflow();
            AddClose();
        }

        private void Enhance(string id)
        {
            var save = GameManager.Instance.Save;
            var a = InnerArts.Find(id);
            string error;
            string toast = null;
            if (InnerArts.Enhance(save, id, out error)) { toast = a.Name + " +" + InnerArts.Level(save, id) + " 강화!"; SoundManager.Sfx("levelup"); GameManager.Instance.SaveNow(); }
            Open(toast ?? error);
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
