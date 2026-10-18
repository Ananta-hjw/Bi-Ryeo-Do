using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// 일괄 분해: tick the grades and gear slots to take, see exactly which pieces would be dismantled (and the materials they give),
    /// then dismantle them all at once. Spare copies only by default; 「하나뿐인 장비도 포함」 also takes the only copy of pieces that are not worn.
    /// </summary>
    public sealed class BulkDismantlePanel
    {
        private readonly RectTransform _root;
        private readonly Action<string, string> _notice;   // title, body
        private readonly Action _changed;
        private readonly bool[] _grades = { true, true, false, false, false };   // 일반·고급 ticked at first
        private readonly bool[] _slots = { true, true, true, true, true };
        private bool _singles;
        private static readonly string[] SlotNames = { "무기", "상의", "하의", "신발", "장신구" };

        public BulkDismantlePanel(RectTransform parent, Action<string, string> notice, Action changed)
        {
            _notice = notice;
            _changed = changed;
            _root = UiFactory.Rect("BulkDismantle", parent, Vector2.zero, Vector2.one);
            _root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);   // dims and blocks the bag underneath
            _root.gameObject.SetActive(false);
        }

        public void Hide() { _root.gameObject.SetActive(false); }

        public void Show()
        {
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            Rebuild();
        }

        private void Rebuild()
        {
            for (int i = _root.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(_root.GetChild(i).gameObject);
            var save = GameManager.Instance.Save;
            var card = UiFactory.Panel("Card", _root, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f), new Color(0.05f, 0.07f, 0.11f, 0.99f));
            UiFactory.Frame(card.transform, UiFactory.Gold, 3f);
            UiFactory.Label("Title", card.transform, new Vector2(0.05f, 0.93f), new Vector2(0.95f, 0.99f), "일괄 분해", 42, UiFactory.Gold);

            UiFactory.Label("GradeLabel", card.transform, new Vector2(0.05f, 0.885f), new Vector2(0.95f, 0.93f), "등급 (신화·초월은 분해할 수 없습니다)", 24, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            for (int i = 0; i < Salvage.BulkGrades.Length; i++)
            {
                int idx = i;
                var grade = Salvage.BulkGrades[i];
                Toggle(card.transform, "G" + i, 0.04f + i * 0.186f, 0.04f + i * 0.186f + 0.176f, 0.815f, 0.88f, Grades.NameOf(grade), _grades[i], UiFactory.GradeColor(grade), () => { _grades[idx] = !_grades[idx]; Rebuild(); });
            }
            UiFactory.Label("SlotLabel", card.transform, new Vector2(0.05f, 0.765f), new Vector2(0.95f, 0.81f), "부위", 24, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            for (int i = 0; i < SlotNames.Length; i++)
            {
                int idx = i;
                Toggle(card.transform, "S" + i, 0.04f + i * 0.186f, 0.04f + i * 0.186f + 0.176f, 0.695f, 0.76f, SlotNames[i], _slots[i], new Color(0.30f, 0.48f, 0.44f, 1f), () => { _slots[idx] = !_slots[idx]; Rebuild(); });
            }
            Toggle(card.transform, "Singles", 0.04f, 0.96f, 0.625f, 0.685f, "하나뿐인 장비도 포함 (착용 중이거나 강화한 장비는 제외)", _singles, new Color(0.55f, 0.36f, 0.14f, 1f), () => { _singles = !_singles; Rebuild(); }, 24);

            var plan = Salvage.BulkPlan(save, _grades, _slots, _singles);
            int total = 0;
            foreach (var e in plan) total += e.Count;
            UiFactory.Label("Summary", card.transform, new Vector2(0.05f, 0.565f), new Vector2(0.95f, 0.62f),
                plan.Count == 0 ? "분해될 장비가 없습니다" : "분해될 장비  " + plan.Count + "종 · 총 " + total + "개", 28, plan.Count == 0 ? UiFactory.Dim : UiFactory.PaperText, TextAnchor.MiddleLeft);

            // The list of exactly what would go, one line per piece
            var viewport = UiFactory.Rect("ListViewport", card.transform, new Vector2(0.03f, 0.215f), new Vector2(0.97f, 0.56f));
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UiFactory.Rect("ListContent", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f));
            content.pivot = new Vector2(0.5f, 1f);
            const float rowH = 58f, gap = 4f;
            content.sizeDelta = new Vector2(0f, Mathf.Max(1, plan.Count) * (rowH + gap) + gap);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40f;
            for (int i = 0; i < plan.Count; i++)
            {
                var e = plan[i];
                var mat = Salvage.MaterialFor(e.Item);
                var rt = UiFactory.Rect("Row" + i, content, new Vector2(0.01f, 1f), new Vector2(0.99f, 1f));
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -(gap + i * (rowH + gap)));
                rt.sizeDelta = new Vector2(0f, rowH);
                var band = rt.gameObject.AddComponent<Image>();
                band.color = UiFactory.Ink;
                var accent = UiFactory.Panel("Accent", rt, new Vector2(0f, 0.08f), new Vector2(0.012f, 0.92f), UiFactory.GradeColor(e.Item.Grade));
                UiFactory.Label("Name", rt, new Vector2(0.03f, 0f), new Vector2(0.60f, 1f), UiFactory.GradedName(e.Item) + "  <size=20>" + Salvage.SlotLabel(e.Item.Kind) + "</size>", 26, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                UiFactory.Label("Count", rt, new Vector2(0.60f, 0f), new Vector2(0.72f, 1f), "x" + e.Count, 28, UiFactory.Gold);
                UiFactory.Label("Mat", rt, new Vector2(0.72f, 0f), new Vector2(0.99f, 1f), "→ " + mat.Name, 20, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            }
            if (plan.Count == 0)
                UiFactory.Label("Empty", viewport, Vector2.zero, Vector2.one, _singles ? "고른 등급·부위에 분해할 장비가 없습니다." : "고른 등급·부위에 여분이 없습니다.\n(「하나뿐인 장비도 포함」을 켜면 안 입은 장비도 나옵니다)", 24, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);

            // Expected materials
            var yield = Salvage.BulkYield(plan);
            var lines = new List<string>();
            foreach (var kv in yield) lines.Add(kv.Key.Name + " " + kv.Value[0] + "~" + kv.Value[1] + "개");
            UiFactory.Body(UiFactory.Label("Yield", card.transform, new Vector2(0.05f, 0.105f), new Vector2(0.95f, 0.21f),
                lines.Count == 0 ? "" : "예상 재료  " + string.Join(" · ", lines), 22, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal));

            var go = UiFactory.Button("Go", card.transform, new Vector2(0.05f, 0.02f), new Vector2(0.62f, 0.10f), plan.Count == 0 ? "분해할 장비 없음" : "선택한 " + total + "개 분해", plan.Count == 0 ? UiFactory.InkSoft : UiFactory.Red, () => Perform(plan), 32);
            go.interactable = plan.Count > 0;
            UiFactory.Button("Close", card.transform, new Vector2(0.66f, 0.02f), new Vector2(0.95f, 0.10f), "닫기", UiFactory.InkSoft, () => { Hide(); _changed(); }, 32);
        }

        private static void Toggle(Transform parent, string name, float x0, float x1, float y0, float y1, string label, bool on, Color accent, UnityEngine.Events.UnityAction onClick, int size = 28)
        {
            var color = on ? accent : new Color(0.12f, 0.15f, 0.20f, 0.95f);
            UiFactory.Button(name, parent, new Vector2(x0, y0), new Vector2(x1, y1), (on ? "☑ " : "☐ ") + label, color, onClick, size);
        }

        private void Perform(List<Salvage.BulkEntry> plan)
        {
            var save = GameManager.Instance.Save;
            int pieces = 0;
            foreach (var e in plan) pieces += e.Count;
            var gained = Salvage.DismantleBulk(save, plan, new SeededRandom(Environment.TickCount));
            GameManager.Instance.SaveNow();
            SoundManager.Sfx("win");
            var lines = new List<string>();
            foreach (var g in gained)
            {
                var mat = EarlyGameContent.FindItem(g.Id);
                lines.Add("<color=" + UiFactory.GradeHex(mat.Grade) + ">" + mat.Name + "</color> <b>" + g.Count + "개</b> (보유 " + Inventory.Count(save, g.Id) + "개)");
            }
            _notice("일괄 분해 완료", "장비 " + pieces + "개를 분해했습니다.\n\n" + string.Join("\n", lines));
            _changed();
            Rebuild();   // the list now shows what is left
        }
    }
}
