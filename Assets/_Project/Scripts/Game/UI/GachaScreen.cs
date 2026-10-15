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
            public Image Frame, Bg;
            public Text Name, Effect, Tag;
        }

        private Text _header, _hint;
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
            _header = UiFactory.Label("Header", root, new Vector2(0.06f, 0.925f), new Vector2(0.94f, 0.985f), "", 40, UiFactory.Gold, TextAnchor.MiddleLeft);
            _hint = UiFactory.Label("Hint", root, new Vector2(0.05f, 0.885f), new Vector2(0.95f, 0.925f), "두를 내고 장비와 영약을 뽑는다. 같은 장비가 나오면 분해되어 두가 돌아온다.", 24, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);

            // Odds
            var odds = UiFactory.Panel("Odds", root, new Vector2(0.04f, 0.735f), new Vector2(0.96f, 0.88f), UiFactory.Ink);
            UiFactory.Frame(odds.transform, UiFactory.GoldDim, 2f);
            for (int g = 0; g < Grades.Names.Length; g++)
            {
                var grade = (ItemGrade)g;
                int col = g % 3, row = g / 3;
                float x0 = 0.02f + col * 0.325f, y1 = row == 0 ? 0.94f : 0.48f;
                var chip = UiFactory.Panel("Chip" + g, odds.transform, new Vector2(x0, y1 - 0.42f), new Vector2(x0 + 0.31f, y1), new Color(0.04f, 0.05f, 0.08f, 0.9f));
                UiFactory.Frame(chip.transform, UiFactory.GradeColor(grade), 2f);
                UiFactory.Label("Name", chip.transform, new Vector2(0.04f, 0.5f), new Vector2(0.96f, 1f), Grades.Names[g] + "  " + Mathf.RoundToInt((float)Grades.Rates[g] * 100f) + "%", 28, UiFactory.GradeColor(grade));
                UiFactory.Label("Count", chip.transform, new Vector2(0.04f, 0f), new Vector2(0.96f, 0.5f), Gacha.Pool(grade).Count + "종", 22, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);
            }

            // Result cards: 2 columns x 5 rows
            _emptyNote = UiFactory.Label("Empty", root, new Vector2(0.06f, 0.35f), new Vector2(0.94f, 0.55f), "아직 뽑지 않았다.\n아래 버튼으로 뽑아 보자.", 34, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);
            for (int i = 0; i < Gacha.TenPullCount; i++)
            {
                int col = i % 2, row = i / 2;
                float x0 = col == 0 ? 0.04f : 0.52f;
                float y1 = 0.72f - row * 0.098f, y0 = y1 - 0.09f;
                var c = new Card();
                var rt = UiFactory.Rect("Card" + i, root, new Vector2(x0, y0), new Vector2(x0 + 0.44f, y1));
                c.Root = rt.gameObject;
                c.Frame = UiFactory.Panel("Frame", rt, Vector2.zero, Vector2.one, UiFactory.GoldDim);
                c.Bg = UiFactory.Panel("Bg", rt, new Vector2(0.015f, 0.04f), new Vector2(0.985f, 0.96f), new Color(0.06f, 0.08f, 0.12f, 0.96f));
                c.Name = UiFactory.Label("Name", rt, new Vector2(0.04f, 0.50f), new Vector2(0.78f, 0.96f), "", 26, UiFactory.PaperText, TextAnchor.MiddleLeft);
                c.Effect = UiFactory.Label("Effect", rt, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.50f), "", 18, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                c.Tag = UiFactory.Label("Tag", rt, new Vector2(0.76f, 0.52f), new Vector2(0.97f, 0.96f), "", 20, UiFactory.Gold, TextAnchor.MiddleRight);
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

        private void OnEnable() { if (_built) Refresh(); }

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
            var save = GameManager.Instance.Save;
            var rng = new SeededRandom(unchecked(Environment.TickCount * 397) ^ (_pullCounter++ * 7919));
            var results = Gacha.Pull(save, count, rng);
            if (results == null) { _hint.text = "두가 모자라다."; return; }
            GameManager.Instance.SaveNow();
            StartCoroutine(Reveal(results));
        }

        private IEnumerator Reveal(List<PullResult> results)
        {
            _busy = true;
            _emptyNote.gameObject.SetActive(false);
            foreach (var c in _cards) c.Root.SetActive(false);
            Refresh();

            // Bulk pulls show the 10 best results; the summary line counts everything.
            var counts = new int[Grades.Names.Length];
            long refunded = 0;
            foreach (var r in results) { counts[(int)r.Item.Grade]++; refunded += r.Doo; }
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
                card.Name.text = UiFactory.GradedName(item);
                card.Effect.text = item.IsGear ? StatusSheet.EffectText(item) : item.Description;
                card.Tag.text = r.Duplicate ? "분해 +" + r.Doo.ToString("N0") : item.IsGear ? "NEW" : "";
                card.Root.SetActive(true);
                if (item.Grade > best) best = item.Grade;
                SoundManager.Sfx(item.Grade >= ItemGrade.Legendary ? "gacha_mythic" : item.Grade >= ItemGrade.Epic ? "gacha_rare" : "gacha_pull");
                if (item.Grade >= ItemGrade.Epic) SoundManager.Haptic(item.Grade >= ItemGrade.Legendary ? 150 : 60);
                yield return new WaitForSeconds(shown.Count > 1 ? 0.14f : 0.05f);
            }

            var parts = new List<string>();
            for (int g = counts.Length - 1; g >= 0; g--) if (counts[g] > 0) parts.Add("<color=" + UiFactory.GradeHex((ItemGrade)g) + ">" + Grades.Names[g] + " " + counts[g] + "</color>");
            _hint.text = string.Join("  ", parts) + (refunded > 0 ? "  ·  분해 두 +" + refunded.ToString("N0") : "") + (results.Count > shown.Count ? "  (높은 등급 " + shown.Count + "개만 표시)" : "");
            _busy = false;
            Refresh();
        }
    }
}
