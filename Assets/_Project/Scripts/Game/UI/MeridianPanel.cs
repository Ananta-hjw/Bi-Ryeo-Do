using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>기경팔맥 page of the 무공 tab: the eight meridians as point tracks on the left, the chosen one's effects and 타통 on the right.</summary>
    public sealed class MeridianPanel : OverlayPanel
    {
        public MeridianPanel(RectTransform root, Action onChange) : base(root, onChange) { }

        /// <summary>Meridians flagged new when the page was opened; filled by MartialArtsScreen.</summary>
        public readonly HashSet<string> NewIds = new HashSet<string>();

        private RectTransform _detail;
        private string _selected;

        private static readonly Color Opened = new Color(0.86f, 0.70f, 0.32f, 1f);
        private static readonly Color Ready = new Color(0.92f, 0.90f, 0.84f, 1f);
        private static readonly Color Closed = new Color(0.30f, 0.31f, 0.34f, 1f);

        public void Open(string toast = null)
        {
            var save = GameManager.Instance.Save;
            Begin();
            if (!Tutorial.IsDone(save, "meridian") && HelpPanel.Instance != null) HelpPanel.Instance.Show("meridian", true);
            if (_selected == null)
                foreach (var m in Meridians.All) if (Meridians.IsOpen(save, m) && !Meridians.IsComplete(save, m.Id)) { _selected = m.Id; break; }

            var list = UiFactory.Rect("List", Root, Vector2.zero, new Vector2(.645f, 1f));
            var header = UiFactory.TabHeader(list, "arts", "", .97f);
            UiFactory.SetTabHeader(header, "경맥", "타통 " + Meridians.TotalPoints(save) + "/" + Meridians.All.Length * Meridians.Points);
            list.offsetMax = Vector2.zero;
            float bodyTop = header.transform.IsChildOf(list) ? .84f : .99f;
            UiFactory.HelpButton(list, () => { if (HelpPanel.Instance != null) HelpPanel.Instance.Show("meridian"); });
            string circuits = "소주천 " + (Meridians.SmallCircuit(save) ? "<color=#D4B87A>완성</color>" : "미완") + "  ·  대주천 " + (Meridians.GrandCircuit(save) ? "<color=#D4B87A>완성</color>" : "미완 (" + Meridians.CompletedCount(save) + "/" + Meridians.All.Length + ")");
            UiFactory.ScrollBody(UiFactory.Label("Hint", list, new Vector2(0.05f, bodyTop - .085f), new Vector2(0.95f, bodyTop),
                toast ?? ("은자 " + save.Doo.ToString("N0") + "  ·  심득석 " + InnerArts.Stones(save) + "  ·  " + circuits), 30, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 30);

            var content = CodexPanel.Scroller(list, .02f, bodyTop - .095f);
            var grid = new ReadingGrid(content, 2, 230f);   // two meridians per row
            for (int i = 0; i < Meridians.All.Length; i++)
            {
                var m = Meridians.All[i];
                bool open = Meridians.IsOpen(save, m);
                int level = Meridians.Level(save, m.Id);
                bool complete = level >= Meridians.Points;
                var row = grid.Add("Meridian" + i);
                var bg = row.GetComponent<Image>();
                bg.color = m.Id == _selected ? new Color(0.12f, 0.17f, 0.18f, 0.97f) : UiFactory.Ink;
                var tap = row.gameObject.AddComponent<Button>();
                tap.targetGraphic = bg;
                tap.transition = Selectable.Transition.None;
                string id = m.Id;
                tap.onClick.AddListener(() => { _selected = id; Open(); });
                UiFactory.Frame(row, complete ? UiFactory.Gold : UiFactory.GoldDim, complete ? 3f : 2f);

                string tag = NewIds.Contains(m.Id) && level == 0 ? "  <color=#FF6A5A>새로 열림</color>" : complete ? "  <color=#D4B87A>타통</color>" : "";
                UiFactory.Label("Name", row, new Vector2(.05f, .70f), new Vector2(.95f, .95f), (open ? m.Name : "???") + "  " + level + "/" + Meridians.Points + tag, 30, open ? UiFactory.PaperText : UiFactory.InkSoft, TextAnchor.MiddleLeft);
                DrawTrack(row, level, open, .40f, .64f);
                UiFactory.Body(UiFactory.Label("Effect", row, new Vector2(.05f, .05f), new Vector2(.95f, .38f),
                    open ? Meridians.CurrentText(save, m).Replace("\n", "  ") : Meridians.UnlockText(m) + " 후 열립니다", 24, UiFactory.Dim, TextAnchor.UpperLeft, FontStyle.Normal), 1.1f);
            }
            Canvas.ForceUpdateCanvases();
            var layout = content.GetComponent<ReadingList>();
            if (layout != null) layout.Reflow();

            _detail = UiFactory.Rect("Inspector", Root, new Vector2(.66f, .023f), new Vector2(.985f, 1f));
            ShowDetail(save);
            AddClose();
        }

        /// <summary>Five points joined by a line: opened gold, the next one white (when open), the rest grey.</summary>
        private static void DrawTrack(RectTransform row, int level, bool open, float yMin, float yMax)
        {
            var line = UiFactory.Panel("Line", row, new Vector2(.10f, (yMin + yMax) / 2f - .01f), new Vector2(.90f, (yMin + yMax) / 2f + .01f), open ? UiFactory.GoldDim : Closed);
            line.raycastTarget = false;
            for (int p = 0; p < Meridians.Points; p++)
            {
                float x = .10f + .80f * p / (Meridians.Points - 1);
                Color c = !open ? Closed : p < level ? Opened : p == level ? Ready : Closed;
                var point = UiFactory.Panel("Point" + p, row, new Vector2(x - .045f, yMin), new Vector2(x + .045f, yMax), c);
                point.raycastTarget = false;
                if (p == Meridians.Points - 1) UiFactory.Frame(point.transform, open ? UiFactory.Gold : UiFactory.InkSoft, 2f);   // the completing point
            }
        }

        private void ShowDetail(SaveData save)
        {
            LandscapeMenu.Clear(_detail);
            var m = _selected != null ? Meridians.Find(_selected) : null;
            if (m == null)
            {
                var placeholder = UiFactory.Panel("Placeholder", _detail, Vector2.zero, Vector2.one, UiFactory.Ink);
                UiFactory.Frame(placeholder.transform, UiFactory.GoldDim, 2f);
                UiFactory.ScrollBody(UiFactory.Label("Hint", placeholder.transform, new Vector2(.06f, .30f), new Vector2(.94f, .70f), "경맥을 선택하면 혈자리 효과와 타통 비용을 확인할 수 있습니다.", 30, UiFactory.Dim), 30);
                return;
            }
            bool open = Meridians.IsOpen(save, m);
            int level = Meridians.Level(save, m.Id);
            bool complete = level >= Meridians.Points;
            var card = UiFactory.Panel("Card", _detail, Vector2.zero, Vector2.one, UiFactory.ModalInk);
            UiFactory.Frame(card.transform, complete ? UiFactory.Gold : UiFactory.GoldDim, 3f);
            UiFactory.Label("Title", card.transform, new Vector2(0.05f, 0.91f), new Vector2(0.95f, 0.985f), (open ? m.Name : "???") + "  " + level + "/" + Meridians.Points, 40, UiFactory.PaperText, TextAnchor.MiddleLeft);
            UiFactory.Label("Kind", card.transform, new Vector2(0.05f, 0.86f), new Vector2(0.95f, 0.91f), "기경팔맥" + (complete ? "  ·  타통 완료" : ""), 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            DrawTrack((RectTransform)card.transform, level, open, .78f, .84f);

            var lines = new List<string>();
            if (!open) lines.Add(Meridians.UnlockText(m) + " 후 열립니다.");
            lines.Add("<color=#D4B87A>혈자리 1~" + Meridians.StatPoints + "</color>  하나마다 " + m.StepText);
            lines.Add("<color=#D4B87A>혈자리 " + Meridians.Points + " (완성)</color>  " + m.FinishText);
            if (level > 0) lines.Add("지금 효과: " + Meridians.CurrentText(save, m).Replace("\n", " · "));
            if (open && !complete)
            {
                int next = level + 1;
                long doo = Meridians.DooCost(m, next);
                int stones = Meridians.StoneCost(m, next);
                lines.Add("다음 혈자리 " + next + ": 은자 " + doo.ToString("N0") + " (보유 " + save.Doo.ToString("N0") + ")"
                    + (stones > 0 ? "\n심득석 " + stones + "개 (보유 " + InnerArts.Stones(save) + "개)" : ""));
            }
            if (m.Id == "im" || m.Id == "dok") lines.Add("임맥과 독맥을 모두 타통하면 소주천: 체력·내공·공격·방어 +3%");
            lines.Add("여덟 경맥을 모두 타통하면 대주천: 필살기 위력 +15%");
            UiFactory.ScrollBody(UiFactory.Label("Body", card.transform, new Vector2(0.06f, 0.20f), new Vector2(0.94f, 0.76f), string.Join("\n\n", lines), 28, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal), 34);

            string label; bool enabled;
            if (!open) { label = "잠김"; enabled = false; }
            else if (complete) { label = "타통 완료"; enabled = false; }
            else
            {
                int next = level + 1;
                long doo = Meridians.DooCost(m, next);
                int stones = Meridians.StoneCost(m, next);
                label = "타통 " + doo.ToString("N0") + "은자" + (stones > 0 ? " · 심득석 " + stones : "");
                enabled = save.Doo >= doo && InnerArts.Stones(save) >= stones;
            }
            string id = m.Id;
            UiFactory.Button("Main", card.transform, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.16f), label, enabled ? UiFactory.Red : UiFactory.InkSoft, () => Press(id), 32).interactable = enabled;
            LandscapeMenu.Inspector(card.rectTransform);
        }

        private void Press(string id)
        {
            var save = GameManager.Instance.Save;
            var m = Meridians.Find(id);
            bool smallBefore = Meridians.SmallCircuit(save), grandBefore = Meridians.GrandCircuit(save);
            string error;
            if (!Meridians.Open(save, id, out error)) { Open(error); return; }
            GameManager.Instance.SaveNow();
            SoundManager.Sfx("levelup");
            string toast = m.Name + " 혈자리 " + Meridians.Level(save, id) + " 타통!";
            if (Meridians.IsComplete(save, id)) toast = "<color=#D4B87A>" + m.Name + " 타통 완성! " + m.FinishText + "</color>";
            if (!grandBefore && Meridians.GrandCircuit(save)) toast = "<color=#D4B87A>대주천 완성! 필살기 위력 +15%</color>";
            else if (!smallBefore && Meridians.SmallCircuit(save)) toast = "<color=#D4B87A>소주천 완성! 체력·내공·공격·방어 +3%</color>";
            Open(toast);
            OnChange();
        }
    }
}
