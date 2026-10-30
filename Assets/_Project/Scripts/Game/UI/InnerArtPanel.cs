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

        private RectTransform _detail;
        private ScrollRect _scroll;
        private string _selected;   // the method shown in the inspector on the right

        private const string Gain = "#8FDB98", Loss = "#FF7A6A";

        /// <summary>Effects coloured by whether they help (green) or cost (red), joined by the separator.</summary>
        internal static string Colored(InnerArt a, int level, string separator)
        {
            var parts = new List<string>();
            foreach (var p in InnerArts.EffectParts(a, level)) parts.Add("<color=" + (p.Value ? Gain : Loss) + ">" + p.Key + "</color>");
            return string.Join(separator, parts);
        }

        public void Open(string toast = null)
        {
            var save = GameManager.Instance.Save;
            if (!InnerArts.IsLearned(save, "breath")) save.LearnedInner.Add("breath");   // the free starter method
            float scrollPos = _scroll != null ? _scroll.verticalNormalizedPosition : 1f;
            Begin();
            if (!Tutorial.IsDone(save, "inner") && HelpPanel.Instance != null) HelpPanel.Instance.Show("inner", true);

            // Same layout as the 무공 page: the list on the left, the inspector on the right.
            var list = UiFactory.Rect("List", Root, Vector2.zero, new Vector2(.645f, 1f));
            int slots = InnerArts.SlotCount(save);
            var header = UiFactory.TabHeader(list, "arts", "", .97f);
            UiFactory.SetTabHeader(header, "심법", "장착 " + save.EquippedInner.Count + "/" + slots);
            list.offsetMax = Vector2.zero;
            float bodyTop = header.transform.IsChildOf(list) ? .84f : .99f;
            UiFactory.HelpButton(list, () => { if (HelpPanel.Instance != null) HelpPanel.Instance.Show("inner"); });
            string slotNote = slots >= 4 ? "모든 칸이 열렸습니다" : "다음 칸: " + (save.ClearedStage < 10 ? "스테이지 10 돌파" : save.ClearedStage < 20 ? "스테이지 20 돌파" : "어려움 스테이지 " + InnerArts.FourthSlotStage + " 돌파");
            UiFactory.ScrollBody(UiFactory.Label("Hint", list, new Vector2(0.05f, bodyTop - .085f), new Vector2(0.95f, bodyTop), toast ?? ("은자 " + save.Doo.ToString("N0") + "  ·  심득석 " + InnerArts.Stones(save) + "  ·  " + slotNote), 30, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 30);

            var content = CodexPanel.Scroller(list, .02f, bodyTop - .095f);
            _scroll = content.parent.GetComponent<ScrollRect>();
            const float icon = 100f;
            var grid = new ReadingGrid(content, 3, 340f);   // three cards per row
            for (int i = 0; i < InnerArts.All.Length; i++)
            {
                var a = InnerArts.All[i];
                bool unlocked = InnerArts.IsUnlocked(save, a), learned = InnerArts.IsLearned(save, a.Id), equipped = InnerArts.IsEquipped(save, a.Id);
                int level = InnerArts.Level(save, a.Id);
                var row = grid.Add("Card" + i);
                var bg = row.GetComponent<Image>();
                bg.color = a.Id == _selected ? new Color(0.12f, 0.17f, 0.18f, 0.97f) : UiFactory.Ink;
                var tap = row.gameObject.AddComponent<Button>();   // tapping the row opens the inspector
                tap.targetGraphic = bg;
                tap.transition = Selectable.Transition.None;
                var id = a.Id;
                tap.onClick.AddListener(() => { _selected = id; Open(); });
                UiFactory.Frame(row, equipped ? UiFactory.Gold : UiFactory.GoldDim, 2f);

                var iconRect = UiFactory.Rect("Icon", row, new Vector2(0f, 1f), new Vector2(0f, 1f));
                iconRect.pivot = new Vector2(0f, 1f);
                iconRect.anchoredPosition = new Vector2(16f, -16f);
                iconRect.sizeDelta = new Vector2(icon, icon);
                var picture = UiFactory.Panel("Picture", iconRect, Vector2.zero, Vector2.one, unlocked ? Color.white : new Color(.45f, .45f, .48f));
                picture.sprite = ArtLibrary.InnerArt(a.Id);
                picture.preserveAspect = true;
                picture.raycastTarget = false;
                if (picture.sprite == null)
                    UiFactory.Glyph("fortune", iconRect, Vector2.zero, Vector2.one, unlocked ? UiFactory.Gold : UiFactory.Dim);
                // 장착 중 / 새 심법 sit on the icon, like the 무공 list, so the name line stays short.
                string tag = equipped ? "장착 중" : NewIds.Contains(a.Id) && !learned ? "<color=#FF6A5A>새 심법</color>" : "";
                if (tag.Length > 0)
                {
                    var tagBack = UiFactory.Panel("TagBack", iconRect, Vector2.zero, new Vector2(1f, .26f), new Color(0f, 0f, 0f, .72f));
                    tagBack.raycastTarget = false;
                    UiFactory.Label("Tag", tagBack.transform, Vector2.zero, Vector2.one, tag, 22, UiFactory.Gold);
                }

                var name = UiFactory.Body(UiFactory.Label("Name", row, new Vector2(0f, 1f), new Vector2(.96f, 1f),
                    (unlocked ? a.Name : "???") + (level > 0 ? "  <color=#D4B87A>+" + level + "</color>" : ""), 30, unlocked ? UiFactory.PaperText : UiFactory.InkSoft, TextAnchor.MiddleLeft));
                name.rectTransform.offsetMin = new Vector2(icon + 32f, -140f);
                name.rectTransform.offsetMax = new Vector2(0f, -16f);
                UiFactory.Body(UiFactory.Label("Effect", row, new Vector2(.04f, .28f), new Vector2(.96f, .53f),
                    unlocked ? Colored(a, level, "  ") : InnerArts.UnlockText(a) + "하면 열립니다", 26, UiFactory.Dim, TextAnchor.UpperLeft, FontStyle.Normal), 1.15f);

                string label; Color color; bool enabled;
                if (learned) { label = equipped ? "해제" : "장착"; color = equipped ? UiFactory.InkSoft : UiFactory.Red; enabled = true; }
                else if (!unlocked) { label = "잠김"; color = UiFactory.InkSoft; enabled = false; }
                else { label = "습득 " + a.LearnCost.ToString("N0") + "은자"; enabled = save.Doo >= a.LearnCost; color = enabled ? UiFactory.Red : UiFactory.InkSoft; }
                var btn = UiFactory.Button("Act", row, new Vector2(.04f, .04f), new Vector2(.96f, .23f), label, color, () => Press(id), 26);
                btn.interactable = enabled;
            }
            Canvas.ForceUpdateCanvases();
            var layout = content.GetComponent<ReadingList>();
            if (layout != null) layout.Reflow();
            _scroll.verticalNormalizedPosition = scrollPos;

            _detail = UiFactory.Rect("Inspector", Root, new Vector2(.66f, .023f), new Vector2(.985f, 1f));
            ShowDetail(save);
            AddClose();
        }

        /// <summary>The right-hand inspector: every effect on its own line, the description, enhancement and the buttons.</summary>
        private void ShowDetail(SaveData save)
        {
            LandscapeMenu.Clear(_detail);
            var a = _selected != null ? InnerArts.Find(_selected) : null;
            if (a == null)
            {
                var placeholder = UiFactory.Panel("Placeholder", _detail, Vector2.zero, Vector2.one, UiFactory.Ink);
                UiFactory.Frame(placeholder.transform, UiFactory.GoldDim, 2f);
                UiFactory.ScrollBody(UiFactory.Label("Hint", placeholder.transform, new Vector2(.06f, .30f), new Vector2(.94f, .70f), "심법을 선택하면 효과·강화 단계·설명을 확인할 수 있습니다.", 30, UiFactory.Dim), 30);
                return;
            }
            bool unlocked = InnerArts.IsUnlocked(save, a), learned = InnerArts.IsLearned(save, a.Id), equipped = InnerArts.IsEquipped(save, a.Id);
            int level = InnerArts.Level(save, a.Id);
            var card = UiFactory.Panel("Card", _detail, Vector2.zero, Vector2.one, UiFactory.ModalInk);
            UiFactory.Frame(card.transform, equipped ? UiFactory.Gold : UiFactory.GoldDim, 3f);
            var big = UiFactory.Panel("Picture", card.transform, new Vector2(0.66f, 0.84f), new Vector2(0.96f, 0.985f), unlocked ? Color.white : new Color(.45f, .45f, .48f));
            big.sprite = ArtLibrary.InnerArt(a.Id);
            big.preserveAspect = true;
            if (big.sprite == null) UiFactory.Glyph("fortune", big.transform, Vector2.zero, Vector2.one, unlocked ? UiFactory.Gold : UiFactory.Dim);
            UiFactory.Label("Title", card.transform, new Vector2(0.05f, 0.91f), new Vector2(0.63f, 0.985f), (unlocked ? a.Name : "???") + (level > 0 ? " +" + level : ""), 40, UiFactory.PaperText, TextAnchor.MiddleLeft);
            UiFactory.Label("Kind", card.transform, new Vector2(0.05f, 0.86f), new Vector2(0.63f, 0.91f), "심법" + (equipped ? "  ·  장착 중" : learned ? "  ·  배움" : ""), 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);

            var lines = new List<string>();
            if (unlocked)
            {
                lines.Add(Colored(a, level, "\n"));
                if (a.Description.Length > 0) lines.Add(a.Description);
                int cost = learned ? InnerArts.NextCost(save, a.Id) : -1;
                if (learned) lines.Add(cost < 0 ? "최대 강화에 도달했습니다." : "다음 강화(+" + (level + 1) + "): 심득석 " + cost + "개 (보유 " + InnerArts.Stones(save) + "개)\n" + Colored(a, level + 1, "  "));
            }
            else lines.Add(InnerArts.UnlockText(a) + "하면 열립니다.");
            UiFactory.ScrollBody(UiFactory.Label("Body", card.transform, new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.85f), string.Join("\n\n", lines), 30, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal), 36);

            var id = a.Id;
            string label; Color color; bool enabled;
            if (learned) { label = equipped ? "해제" : "장착"; color = equipped ? UiFactory.InkSoft : UiFactory.Red; enabled = true; }
            else if (!unlocked) { label = "잠김"; color = UiFactory.InkSoft; enabled = false; }
            else { label = "습득 " + a.LearnCost.ToString("N0") + "은자"; enabled = save.Doo >= a.LearnCost; color = enabled ? UiFactory.Red : UiFactory.InkSoft; }
            UiFactory.Button("Main", card.transform, new Vector2(0.05f, 0.19f), new Vector2(0.95f, 0.285f), label, color, () => Press(id), 34).interactable = enabled;
            if (learned)
            {
                int cost = InnerArts.NextCost(save, id);
                bool can = cost >= 0 && InnerArts.Stones(save) >= cost;
                // "Variant" takes the right-hand button place in the landscape inspector.
                UiFactory.Button("Variant", card.transform, new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.18f), cost < 0 ? "최대 강화" : "강화 심득석 " + cost, can ? UiFactory.Gold : new Color(0.30f, 0.30f, 0.33f, 1f), () => Enhance(id), 30).interactable = can;
            }
            LandscapeMenu.Inspector(card.rectTransform);
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
