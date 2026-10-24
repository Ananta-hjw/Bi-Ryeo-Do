using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>뽑기 탭: spend 두 to pull gear and potions. Odds are shown on screen; results appear as grade-coloured cards.</summary>
    public class GachaScreen : MonoBehaviour
    {
        private sealed class Card
        {
            public GameObject Root;
            public Image Frame, Bg, Icon;
            public Text Name, Effect, Tag, IconGlyph;
        }

        private Text _header, _hint;
        private readonly int[] _sizes = { 1, Gacha.TenPullCount, Gacha.FiftyPullCount };
        private readonly Button[] _buttons = new Button[3];
        private readonly Image[] _buttonImages = new Image[3];
        private readonly Text[] _buttonLabels = new Text[3];
        private Text _emptyNote;
        private readonly List<Card> _cards = new List<Card>();
        private readonly List<RectTransform> _resultRows = new List<RectTransform>();
        private RectTransform _resultsContent;
        private const float ResultRowHeight = 320f, ResultGap = 16f;

        /// <summary>Scroll height for the visible result rows (rows have a fixed height).</summary>
        private void FitResults(int rows)
        {
            _resultsContent.sizeDelta = new Vector2(_resultsContent.sizeDelta.x, ResultGap + rows * (ResultRowHeight + ResultGap));
        }
        private ScrollRect _resultsScroll;
        private RectTransform _poolRoot;
        private bool _built, _busy;
        private int _pullCounter;

        public void Build(RectTransform root)
        {
            if (_built) return;
            _built = true;

            _header = UiFactory.TabHeader(root, "gacha");
            UiFactory.HelpButton(root, () => { if (HelpPanel.Instance != null) HelpPanel.Instance.Show("gacha"); });   // at the right end of the header band, next to the 두 figure
            _hint = UiFactory.ScrollBody(UiFactory.Label("Hint", root, new Vector2(.34f, .02f), new Vector2(.98f, .12f), "", 30, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal), 30);

            // Odds
            var odds = UiFactory.Panel("Odds", root, new Vector2(.025f, .42f), new Vector2(.315f, .855f), UiFactory.Ink);
            UiFactory.Frame(odds.transform, UiFactory.GoldDim, 2f);
            for (int g = 0; g < Grades.Rates.Length; g++)   // 초월 cannot be pulled, so it has no chip
            {
                var grade = (ItemGrade)g;
                int col = g % 3, gradeRow = g / 3;
                float x0 = 0.02f + col * 0.325f, y1 = gradeRow == 0 ? 0.94f : 0.48f;
                var chip = UiFactory.Panel("Chip" + g, odds.transform, new Vector2(x0, y1 - 0.42f), new Vector2(x0 + 0.31f, y1), new Color(0.04f, 0.05f, 0.08f, 0.9f));
                chip.raycastTarget = true;
                var openPool = chip.gameObject.AddComponent<Button>();
                openPool.targetGraphic = chip;
                openPool.onClick.AddListener(() => OpenPool(grade));
                UiFactory.Frame(chip.transform, UiFactory.GradeColor(grade), 2f);
                UiFactory.Label("Name", chip.transform, new Vector2(0.04f, 0.5f), new Vector2(0.96f, 1f), Grades.Names[g] + "  " + ((float)Grades.Rates[g] * 100f).ToString("0.#") + "%", 28, UiFactory.GradeColor(grade));
                UiFactory.Label("Count", chip.transform, new Vector2(0.04f, 0f), new Vector2(0.96f, 0.5f), Gacha.Pool(grade).Count + "종", 30, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);
            }

            // Result cards sit in fixed-height rows of five (a long description must not stretch a row; consumables show a short summary).
            _emptyNote = UiFactory.Label("Empty", root, new Vector2(.35f, .28f), new Vector2(.96f, .80f), "아직 뽑지 않았습니다.\n왼쪽 버튼으로 뽑아 보세요.\n등급·확률을 누르면 획득 가능한 목록이 열립니다.", 32, UiFactory.Dim, TextAnchor.LowerCenter, FontStyle.Normal);
            UiFactory.Glyph("gacha", _emptyNote.transform, new Vector2(.3f, .35f), new Vector2(.7f, .94f), new Color(.83f, .72f, .48f, .35f));
            var resultsContent = CodexPanel.Scroller(root, .14f, .855f);
            LandscapeMenu.Place((RectTransform)resultsContent.parent, .33f, .14f, .985f, .855f);
            _resultsScroll = resultsContent.parent.GetComponent<ScrollRect>();
            const float rowHeight = ResultRowHeight, gap = ResultGap;
            RectTransform row = null;
            for (int i = 0; i < Gacha.FiftyPullCount; i++)
            {
                if (i % 5 == 0)
                {
                    int rowIndex = i / 5;
                    row = UiFactory.Rect("Row" + rowIndex, resultsContent, new Vector2(.02f, 1f), new Vector2(.98f, 1f));
                    row.pivot = new Vector2(.5f, 1f);
                    row.sizeDelta = new Vector2(0f, rowHeight);
                    row.anchoredPosition = new Vector2(0f, -gap - rowIndex * (rowHeight + gap));
                    _resultRows.Add(row);
                    row.gameObject.SetActive(false);
                }
                float left = i % 5 * .202f;
                var rt = UiFactory.Rect("Card" + i, row, new Vector2(left, 0f), new Vector2(left + .192f, 1f));
                var c = new Card { Root = rt.gameObject };
                c.Frame = UiFactory.Panel("Frame", rt, Vector2.zero, Vector2.one, UiFactory.GoldDim);
                c.Bg = UiFactory.Panel("Bg", rt, new Vector2(.015f, .015f), new Vector2(.985f, .985f), new Color(.06f, .08f, .12f, .96f));
                c.Icon = UiFactory.TopIcon(rt, 104f, 34f);
                c.IconGlyph = UiFactory.Label("Glyph", c.Icon.transform, Vector2.zero, Vector2.one, "", 34, UiFactory.PaperText);
                c.Name = UiFactory.Body(UiFactory.Label("Name", rt, new Vector2(.04f, .30f), new Vector2(.96f, .53f), "", 22, UiFactory.PaperText));
                c.Effect = UiFactory.Body(UiFactory.Label("Effect", rt, new Vector2(.04f, .02f), new Vector2(.96f, .26f), "", 22, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal));
                c.Tag = UiFactory.Label("Tag", rt, new Vector2(.04f, .915f), new Vector2(.96f, .995f), "", 18, UiFactory.Gold, TextAnchor.MiddleRight);
                c.Tag.rectTransform.anchorMin = new Vector2(.04f, 1f);
                c.Tag.rectTransform.anchorMax = new Vector2(.96f, 1f);
                c.Tag.rectTransform.pivot = new Vector2(.5f, 1f);
                c.Tag.rectTransform.sizeDelta = new Vector2(0f, 28f);
                c.Tag.rectTransform.anchoredPosition = new Vector2(0f, -2f);
                c.Root.SetActive(false);
                _cards.Add(c);
            }
            _resultsContent = resultsContent;

            // Buttons: 1 / 10 / 50 pulls
            for (int i = 0; i < 3; i++)
            {
                int count = _sizes[i];
                float x0 = .025f;
                float top = .39f - i * .125f;
                var btn = UiFactory.Button("Pull" + count, root, new Vector2(x0, top - .11f), new Vector2(.315f, top), "", UiFactory.Red, () => StartPull(count), 28);
                _buttons[i] = btn;
                _buttonImages[i] = btn.GetComponent<Image>();
                _buttonLabels[i] = btn.GetComponentInChildren<Text>();
            }
            _poolRoot = UiFactory.Rect("GachaPool", root, Vector2.zero, Vector2.one);
            _poolRoot.gameObject.SetActive(false);
            Refresh();
        }

        private void OpenPool(ItemGrade grade)
        {
            LandscapeMenu.Clear(_poolRoot);
            _poolRoot.gameObject.SetActive(true);
            _poolRoot.SetAsLastSibling();
            UiFactory.Button("Dismiss", _poolRoot, Vector2.zero, Vector2.one, "", new Color(0f, 0f, 0f, .8f), ClosePool, 28);
            var card = UiFactory.Panel("Card", _poolRoot, new Vector2(.10f, .05f), new Vector2(.90f, .95f), UiFactory.Ink);
            card.raycastTarget = true;
            UiFactory.Frame(card.transform, UiFactory.GradeColor(grade), 3f);
            var pool = Gacha.Pool(grade).OrderBy(item => item.Kind).ThenBy(item => item.Name).ToList();
            UiFactory.Label("Title", card.transform, new Vector2(.04f, .87f), new Vector2(.96f, .98f), Grades.NameOf(grade) + " · 획득 가능한 아이템 " + pool.Count + "종", 36, UiFactory.GradeColor(grade));
            double rate = (int)grade < Grades.Rates.Length ? Grades.Rates[(int)grade] : 0d;
            UiFactory.Label("Odds", card.transform, new Vector2(.04f, .79f), new Vector2(.96f, .86f),
                "등급 확률 " + (rate * 100d).ToString("0.#") + "% · 같은 등급 안에서는 모든 아이템이 동일한 확률입니다.", 26, UiFactory.PaperText);
            var content = CodexPanel.Scroller(card.rectTransform, .16f, .78f);
            const float rowHeight = 320f, gap = 16f;
            RectTransform row = null;
            for (int i = 0; i < pool.Count; i++)
            {
                if (i % 5 == 0)
                {
                    int rowIndex = i / 5;
                    row = UiFactory.Rect("Row" + rowIndex, content, new Vector2(.025f, 1f), new Vector2(.975f, 1f));
                    row.pivot = new Vector2(.5f, 1f);
                    row.sizeDelta = new Vector2(0f, rowHeight);
                    row.anchoredPosition = new Vector2(0f, -gap - rowIndex * (rowHeight + gap));
                    ReadingList.Register(row, rowHeight, gap);
                }
                var item = pool[i];
                float left = i % 5 * .202f;
                var cell = UiFactory.Rect("Item" + i, row, new Vector2(left, 0f), new Vector2(left + .192f, 1f));
                var background = cell.gameObject.AddComponent<Image>();
                background.color = UiFactory.InkSoft;
                background.raycastTarget = true;
                UiFactory.Frame(cell, UiFactory.GradeColor(grade), 2f);
                var icon = UiFactory.TopIcon(cell, 104f, 16f);
                ItemArt.Apply(icon, UiFactory.Label("Glyph", icon.transform, Vector2.zero, Vector2.one, "", 40, UiFactory.Gold), item);
                UiFactory.Body(UiFactory.Label("Name", cell, new Vector2(.05f, .38f), new Vector2(.95f, .59f), item.Name, 22, UiFactory.PaperText, TextAnchor.MiddleCenter));
                UiFactory.Body(UiFactory.Label("Effect", cell, new Vector2(.05f, .12f), new Vector2(.95f, .36f), item.IsGear ? UiFactory.GearEffectText(item) : item.Description, 22, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal));
                UiFactory.Label("Set", cell, new Vector2(.05f, .015f), new Vector2(.95f, .11f), item.IsGear ? "세트: " + (Gear.SetOf(item.Id)?.Name ?? "없음") : "소모품", 20, UiFactory.Gold, TextAnchor.MiddleLeft);
            }
            UiFactory.Button("Close", card.transform, new Vector2(.30f, .015f), new Vector2(.70f, .14f), "닫기", UiFactory.InkSoft, ClosePool, 32);
            Canvas.ForceUpdateCanvases();
            var layout = content.GetComponent<ReadingList>();
            if (layout != null) layout.Reflow();
        }

        private void ClosePool() { if (_poolRoot != null) _poolRoot.gameObject.SetActive(false); }

        private List<PullResult> _pending;   // results being revealed; finished instantly if the tab was left mid-animation

        private void OnEnable()
        {
            if (!_built) return;
            _busy = false;
            if (_pending != null) { var done = _pending; _pending = null; StartCoroutine(ShowResults(done, false)); }
            Refresh();
        }

        // Leaving the tab mid-reveal kills the coroutine before it can clear the flag, which used to leave every pull button disabled.
        private void OnDisable()
        {
            if (!_built) return;
            StopAllCoroutines();
            ClosePool();
            _busy = false;
        }

        private void Refresh()
        {
            var save = GameManager.Instance.Save;
            _header.text = "뽑기 · " + save.Doo.ToString("N0") + "두";
            for (int i = 0; i < 3; i++)
            {
                int n = _sizes[i];
                bool can = Gacha.CanAfford(save, n);
                _buttonLabels[i].text = n + "회 뽑기\n" + Gacha.CostFor(n).ToString("N0") + "두";
                _buttons[i].interactable = can && !_busy;
                _buttonImages[i].color = can ? UiFactory.Red : UiFactory.InkSoft;
            }
        }

        private void StartPull(int count)
        {
            if (_busy) return;
            _pending = null;
            var save = GameManager.Instance.Save;
            var rng = new SeededRandom(unchecked(Environment.TickCount * 397) ^ (_pullCounter++ * 7919));
            var results = Gacha.Pull(save, count, rng);
            if (results == null) { _hint.text = "두가 모자랍니다."; return; }
            GameManager.Instance.SaveNow();
            _pending = results;
            StartCoroutine(Reveal(results));
        }

        private IEnumerator Reveal(List<PullResult> results) { return ShowResults(results, true); }

        private IEnumerator ShowResults(List<PullResult> results, bool animate)
        {
            _busy = true;
            _emptyNote.gameObject.SetActive(false);
            foreach (var c in _cards) c.Root.SetActive(false);
            foreach (var row in _resultRows) row.gameObject.SetActive(false);
            _resultsScroll.StopMovement();
            FitResults(0);
            Canvas.ForceUpdateCanvases();
            _resultsScroll.verticalNormalizedPosition = 1f;
            Refresh();

            // All results, including fifty-pulls, remain available in the scrolling grid.
            var counts = new int[Grades.Names.Length];
            int dupes = 0;
            foreach (var r in results) { counts[(int)r.Item.Grade]++; if (r.Duplicate) dupes++; }
            var shown = new List<PullResult>(results);
            shown.Sort((a, b) => ((int)b.Item.Grade).CompareTo((int)a.Item.Grade));
            if (shown.Count > _cards.Count) shown.RemoveRange(_cards.Count, shown.Count - _cards.Count);

            for (int i = 0; i < shown.Count; i++)
            {
                var r = shown[i];
                var card = _cards[i];
                var item = r.Item;
                card.Frame.color = UiFactory.GradeColor(item.Grade);
                ItemArt.Apply(card.Icon, card.IconGlyph, item);
                card.Name.text = UiFactory.GradedName(item);
                card.Effect.text = item.IsGear ? UiFactory.GearEffectText(item) : item.Kind == ItemKind.Consumable ? BagScreen.ConsumableSummary(item, "  ") : item.Kind == ItemKind.Material ? "재료" : "중요 물품";
                card.Tag.text = r.Duplicate ? "여분 +1" : item.IsGear ? "NEW" : "";
                _resultRows[i / 5].gameObject.SetActive(true);
                card.Root.SetActive(true);
                FitResults(i / 5 + 1);
                if (animate) SoundManager.Sfx(item.Grade >= ItemGrade.Legendary ? "gacha_mythic" : item.Grade >= ItemGrade.Epic ? "gacha_rare" : "gacha_pull");
                if (animate && item.Grade >= ItemGrade.Epic) SoundManager.Haptic(item.Grade >= ItemGrade.Legendary ? 150 : 60);
                if (animate) yield return new WaitForSeconds(shown.Count > 10 ? .04f : shown.Count > 1 ? .14f : .05f);
            }

            if (!animate)
            {
                Canvas.ForceUpdateCanvases();
                _resultsScroll.verticalNormalizedPosition = 1f;
            }
            var parts = new List<string>();
            for (int g = counts.Length - 1; g >= 0; g--) if (counts[g] > 0) parts.Add("<color=" + UiFactory.GradeHex((ItemGrade)g) + ">" + Grades.Names[g] + " " + counts[g] + "</color>");
            _hint.text = string.Join("  ", parts) + (dupes > 0 ? "  ·  같은 장비 " + dupes + "개는 행낭에 여분으로 쌓였습니다 (한계돌파 또는 분해)" : "") + (results.Count > shown.Count ? "  (높은 등급 " + shown.Count + "개만 표시)" : "");
            _busy = false;
            _pending = null;
            Refresh();
        }
    }
}
