using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// 일괄 합성: tick the grades and gear slots of the crafting materials to use, see per material how many it spends, how many are left
    /// and how many pieces of the next grade it makes, then fuse them all at once. Mirrors <see cref="BulkDismantlePanel"/>.
    /// </summary>
    public sealed class BulkFusePanel
    {
        private readonly RectTransform _root;
        private readonly Action<string, string> _notice;   // title, body
        private readonly Action _changed;
        private readonly bool[] _grades = { true, true, false, false, false };   // 일반·고급 ticked at first, like 일괄 분해
        private readonly bool[] _slots = { true, true, true, true, true };
        private static readonly string[] SlotNames = { "무기", "상의", "하의", "신발", "장신구" };

        public BulkFusePanel(RectTransform parent, Action<string, string> notice, Action changed)
        {
            _notice = notice;
            _changed = changed;
            _root = UiFactory.Rect("BulkFuse", parent, Vector2.zero, Vector2.one);
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
            LandscapeMenu.Clear(_root);
            var save = GameManager.Instance.Save;
            var card = UiFactory.Panel("Card", _root, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f), UiFactory.ModalInk);
            UiFactory.Frame(card.transform, UiFactory.Gold, 3f);
            UiFactory.Label("Title", card.transform, new Vector2(0.05f, 0.93f), new Vector2(0.95f, 0.99f), "일괄 합성", 42, UiFactory.Gold);

            UiFactory.Label("GradeLabel", card.transform, new Vector2(.035f, .83f), new Vector2(.395f, .91f), "재료 등급", 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            for (int i = 0; i < Salvage.BulkGrades.Length; i++)
            {
                int idx = i;
                var grade = Salvage.BulkGrades[i];
                Toggle(card.transform, "G" + i, .035f + i % 3 * .121f, .147f + i % 3 * .121f, .69f - i / 3 * .125f, .81f - i / 3 * .125f,
                    Grades.NameOf(grade) + " " + Salvage.FuseNeed[i] + "개", _grades[i], UiFactory.GradeColor(grade), () => { _grades[idx] = !_grades[idx]; Rebuild(); }, 24);
            }
            UiFactory.Label("SlotLabel", card.transform, new Vector2(.035f, .50f), new Vector2(.395f, .56f), "부위", 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            for (int i = 0; i < SlotNames.Length; i++)
            {
                int idx = i;
                Toggle(card.transform, "S" + i, .035f + i % 3 * .121f, .147f + i % 3 * .121f, .37f - i / 3 * .135f, .49f - i / 3 * .135f, SlotNames[i], _slots[i], new Color(0.30f, 0.48f, 0.44f, 1f), () => { _slots[idx] = !_slots[idx]; Rebuild(); });
            }
            UiFactory.Body(UiFactory.Label("Rule", card.transform, new Vector2(.035f, .035f), new Vector2(.389f, .22f),
                "같은 재료를 일반 50 · 고급 40 · 희귀 30 · 영웅 20 · 전설 10개 모으면\n한 등급 위 같은 부위 장비 1개가 무작위로 나옵니다.", 22, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal), 1.15f);

            var plan = Salvage.BulkFusePlan(save, _grades, _slots);
            int made = 0, spent = 0;
            foreach (var e in plan) { made += e.Times; spent += e.Spent; }
            UiFactory.Label("Summary", card.transform, new Vector2(.43f, .84f), new Vector2(.965f, .91f),
                plan.Count == 0 ? "합성할 재료가 없습니다" : "재료 " + plan.Count + "종 · " + spent + "개 사용 → 장비 " + made + "개", 28, plan.Count == 0 ? UiFactory.Dim : UiFactory.PaperText, TextAnchor.MiddleLeft);

            // One line per material: what it is, how many you hold, what it spends and what it makes
            var viewport = UiFactory.Rect("ListViewport", card.transform, new Vector2(.43f, .37f), new Vector2(.97f, .83f));
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UiFactory.Rect("ListContent", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f));
            content.pivot = new Vector2(0.5f, 1f);
            const float rowH = 58f, gap = 4f;
            content.sizeDelta = new Vector2(0f, Mathf.Max(1, plan.Count) * (rowH + gap) + gap);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40f;
            UiFactory.ScrollIndicator(scroll, viewport);
            for (int i = 0; i < plan.Count; i++)
            {
                var e = plan[i];
                var next = e.Material.Grade + 1;
                var rt = UiFactory.Rect("Row" + i, content, new Vector2(0.01f, 1f), new Vector2(0.99f, 1f));
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -(gap + i * (rowH + gap)));
                rt.sizeDelta = new Vector2(0f, rowH);
                rt.gameObject.AddComponent<Image>().color = UiFactory.Ink;
                UiFactory.Panel("Accent", rt, new Vector2(0f, 0.08f), new Vector2(0.012f, 0.92f), UiFactory.GradeColor(e.Material.Grade));
                UiFactory.Label("Name", rt, new Vector2(0.025f, 0f), new Vector2(0.36f, 1f), UiFactory.GradedName(e.Material) + " <size=22>" + Salvage.SlotLabel(Salvage.SlotOf(e.Material)) + "</size>", 26, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                UiFactory.Label("Spend", rt, new Vector2(0.36f, 0f), new Vector2(0.68f, 1f), e.Have + "개 중 " + e.Spent + "개 사용 · 남음 " + e.Left, 24, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                UiFactory.Label("Make", rt, new Vector2(0.68f, 0f), new Vector2(0.99f, 1f), "→ <color=" + UiFactory.GradeHex(next) + ">" + Grades.NameOf(next) + " " + Salvage.SlotLabel(Salvage.SlotOf(e.Material)) + "</color> " + e.Times + "개", 26, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
            }
            if (plan.Count == 0)
                UiFactory.Label("Empty", viewport, Vector2.zero, Vector2.one, "고른 등급·부위에 합성할 만큼 모인 재료가 없습니다.", 30, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);

            // What comes out, summed by grade and slot
            var outputs = new List<string>();
            foreach (var e in plan)
            {
                var next = e.Material.Grade + 1;
                outputs.Add("<color=" + UiFactory.GradeHex(next) + ">" + Grades.NameOf(next) + " " + Salvage.SlotLabel(Salvage.SlotOf(e.Material)) + "</color> " + e.Times + "개");
            }
            UiFactory.Body(UiFactory.ScrollBody(UiFactory.Label("Yield", card.transform, new Vector2(.43f, .18f), new Vector2(.97f, .35f),
                outputs.Count == 0 ? "" : "만들어질 장비 (부위 안에서 무작위)\n" + string.Join(" · ", outputs), 28, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal), 28));

            var go = UiFactory.Button("Go", card.transform, new Vector2(.43f, .025f), new Vector2(.76f, .155f), plan.Count == 0 ? "합성할 재료 없음" : "장비 " + made + "개 합성", plan.Count == 0 ? UiFactory.InkSoft : UiFactory.Red,
                () => UiFactory.Confirm(_root, "일괄 합성할까요?", "재료 " + spent + "개를 써서 장비 " + made + "개를 만듭니다.\n쓴 재료는 되돌릴 수 없습니다.", made + "개 합성", () => Perform(plan)), 32);
            go.interactable = plan.Count > 0;
            UiFactory.BackButton(card.transform, new Vector2(.79f, .025f), new Vector2(.97f, .155f), () => { Hide(); _changed(); });
        }

        private static void Toggle(Transform parent, string name, float x0, float x1, float y0, float y1, string label, bool on, Color accent, UnityEngine.Events.UnityAction onClick, int size = 28)
        {
            var color = on ? accent : new Color(0.12f, 0.15f, 0.20f, 0.95f);
            UiFactory.Button(name, parent, new Vector2(x0, y0), new Vector2(x1, y1), (on ? "☑ " : "☐ ") + label, color, onClick, size);
        }

        private void Perform(List<Salvage.FuseEntry> plan)
        {
            var save = GameManager.Instance.Save;
            var made = Salvage.FuseBulk(save, plan, new SeededRandom(Environment.TickCount));
            GameManager.Instance.SaveNow();
            SoundManager.Sfx("win");
            // Same piece made more than once shows as one line with a count
            var counts = new Dictionary<string, int>();
            var order = new List<ItemData>();
            foreach (var piece in made)
            {
                if (!counts.ContainsKey(piece.Id)) { counts[piece.Id] = 0; order.Add(piece); }
                counts[piece.Id]++;
            }
            var lines = new List<string>();
            foreach (var piece in order) lines.Add(UiFactory.GradedName(piece) + " <b>" + counts[piece.Id] + "개</b>");
            _notice("일괄 합성 완료", "장비 " + made.Count + "개를 만들었습니다.\n\n" + string.Join("\n", lines));
            _changed();
            Rebuild();
        }
    }
}
