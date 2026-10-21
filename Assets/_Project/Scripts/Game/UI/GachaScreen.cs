using System;
using System.Collections;
using System.Collections.Generic;
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
        private RectTransform _help;   // the ? window
        private readonly int[] _sizes = { 1, Gacha.TenPullCount, Gacha.FiftyPullCount };
        private readonly Button[] _buttons = new Button[3];
        private readonly Image[] _buttonImages = new Image[3];
        private readonly Text[] _buttonLabels = new Text[3];
        private Text _emptyNote;
        private readonly List<Card> _cards = new List<Card>();
        private bool _built, _busy;
        private int _pullCounter;

        public void Build(RectTransform root)
        {
            if (_built) return;
            _built = true;

            UiFactory.Frame(UiFactory.Panel("HeaderBand", root, new Vector2(0.04f, 0.925f), new Vector2(0.96f, 0.985f), UiFactory.Ink).transform, UiFactory.GoldDim, 2f);
            UiFactory.Glyph("gacha", root, new Vector2(.065f, .936f), new Vector2(.13f, .977f), UiFactory.Gold);
            _header = UiFactory.Label("Header", root, new Vector2(0.16f, 0.925f), new Vector2(0.84f, 0.985f), "", 40, UiFactory.Gold, TextAnchor.MiddleLeft);
            UiFactory.HelpButton(root, () => UiFactory.ShowInfo(_help));   // at the right end of the header band, next to the 두 figure
            _hint = UiFactory.ScrollBody(UiFactory.Label("Hint", root, new Vector2(0.05f, 0.885f), new Vector2(0.95f, 0.925f), "", 30, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal), 30);
            _help = UiFactory.InfoWindow(root, "뽑기 안내", "같은 장비는 여분으로 쌓입니다.\n한계돌파하거나 분해해 재료를 얻을 수 있습니다.\n뽑은 결과는 행낭에 들어갑니다.");

            // Odds
            var odds = UiFactory.Panel("Odds", root, new Vector2(0.04f, 0.735f), new Vector2(0.96f, 0.88f), UiFactory.Ink);
            UiFactory.Frame(odds.transform, UiFactory.GoldDim, 2f);
            for (int g = 0; g < Grades.Rates.Length; g++)   // 초월 cannot be pulled, so it has no chip
            {
                var grade = (ItemGrade)g;
                int col = g % 3, row = g / 3;
                float x0 = 0.02f + col * 0.325f, y1 = row == 0 ? 0.94f : 0.48f;
                var chip = UiFactory.Panel("Chip" + g, odds.transform, new Vector2(x0, y1 - 0.42f), new Vector2(x0 + 0.31f, y1), new Color(0.04f, 0.05f, 0.08f, 0.9f));
                UiFactory.Frame(chip.transform, UiFactory.GradeColor(grade), 2f);
                UiFactory.Label("Name", chip.transform, new Vector2(0.04f, 0.5f), new Vector2(0.96f, 1f), Grades.Names[g] + "  " + ((float)Grades.Rates[g] * 100f).ToString("0.#") + "%", 28, UiFactory.GradeColor(grade));
                UiFactory.Label("Count", chip.transform, new Vector2(0.04f, 0f), new Vector2(0.96f, 0.5f), Gacha.Pool(grade).Count + "종", 30, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);
            }

            // Full-width result rows keep long names and effects readable.
            _emptyNote = UiFactory.Label("Empty", root, new Vector2(0.06f, 0.26f), new Vector2(0.94f, 0.68f), "아직 뽑지 않았습니다.\n아래 버튼으로 뽑아 보세요.", 32, UiFactory.Dim, TextAnchor.LowerCenter, FontStyle.Normal);
            UiFactory.Glyph("gacha", _emptyNote.transform, new Vector2(.3f, .35f), new Vector2(.7f, .94f), new Color(.83f, .72f, .48f, .35f));
            var resultsContent = CodexPanel.Scroller(root, .21f, .72f);
            for (int i = 0; i < Gacha.TenPullCount; i++)
            {
                var c = new Card();
                var rt = UiFactory.Rect("Card" + i, resultsContent, new Vector2(.04f, 1f), new Vector2(.96f, 1f));
                rt.pivot = new Vector2(.5f, 1f);
                rt.sizeDelta = new Vector2(0f, 230f);
                ReadingList.Register(rt, 230f, 16f);
                rt.gameObject.AddComponent<Image>().color = UiFactory.Ink;
                c.Root = rt.gameObject;
                c.Frame = UiFactory.Panel("Frame", rt, Vector2.zero, Vector2.one, UiFactory.GoldDim);
                c.Bg = UiFactory.Panel("Bg", rt, new Vector2(0.015f, 0.04f), new Vector2(0.985f, 0.96f), new Color(0.06f, 0.08f, 0.12f, 0.96f));
                c.Icon = UiFactory.Panel("Icon", rt, new Vector2(0.03f, 0.10f), new Vector2(0.20f, 0.90f), Color.white);
                c.IconGlyph = UiFactory.Label("Glyph", c.Icon.transform, Vector2.zero, Vector2.one, "", 34, UiFactory.PaperText);
                c.Name = UiFactory.Label("Name", rt, new Vector2(0.22f, 0.50f), new Vector2(0.78f, 0.96f), "", 30, UiFactory.PaperText, TextAnchor.MiddleLeft);
                c.Effect = UiFactory.Label("Effect", rt, new Vector2(0.22f, 0.04f), new Vector2(0.97f, 0.50f), "", 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                c.Tag = UiFactory.Label("Tag", rt, new Vector2(0.76f, 0.52f), new Vector2(0.97f, 0.96f), "", 30, UiFactory.Gold, TextAnchor.MiddleRight);
                c.Root.SetActive(false);
                _cards.Add(c);
            }

            // Buttons: 1 / 10 / 50 pulls
            for (int i = 0; i < 3; i++)
            {
                int count = _sizes[i];
                float x0 = 0.04f + i * 0.32f;
                var btn = UiFactory.Button("Pull" + count, root, new Vector2(x0, 0.04f), new Vector2(x0 + 0.30f, 0.19f), "", UiFactory.Red, () => StartPull(count), 28);
                _buttons[i] = btn;
                _buttonImages[i] = btn.GetComponent<Image>();
                _buttonLabels[i] = btn.GetComponentInChildren<Text>();
            }
            Refresh();
        }

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
            Refresh();

            // Bulk pulls show the 10 best results; the summary line counts everything.
            var counts = new int[Grades.Names.Length];
                        int dupes = 0;
            foreach (var r in results) { counts[(int)r.Item.Grade]++; if (r.Duplicate) dupes++; }
            var shown = new List<PullResult>(results);
            shown.Sort((a, b) => ((int)b.Item.Grade).CompareTo((int)a.Item.Grade));
            if (shown.Count > _cards.Count) shown.RemoveRange(_cards.Count, shown.Count - _cards.Count);

            ItemGrade best = ItemGrade.Normal;
            for (int i = 0; i < shown.Count; i++)
            {
                var r = shown[i];
                var card = _cards[i];
                var item = r.Item;
                card.Frame.color = UiFactory.GradeColor(item.Grade);
                ItemArt.Apply(card.Icon, card.IconGlyph, item);
                card.Name.text = UiFactory.GradedName(item);
                card.Effect.text = item.IsGear ? UiFactory.GearEffectText(item) : item.Description;
                card.Tag.text = r.Duplicate ? "여분 +1" : item.IsGear ? "NEW" : "";
                card.Root.SetActive(true);
                if (item.Grade > best) best = item.Grade;
                if (animate) SoundManager.Sfx(item.Grade >= ItemGrade.Legendary ? "gacha_mythic" : item.Grade >= ItemGrade.Epic ? "gacha_rare" : "gacha_pull");
                if (animate && item.Grade >= ItemGrade.Epic) SoundManager.Haptic(item.Grade >= ItemGrade.Legendary ? 150 : 60);
                if (animate) yield return new WaitForSeconds(shown.Count > 1 ? 0.14f : 0.05f);
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
