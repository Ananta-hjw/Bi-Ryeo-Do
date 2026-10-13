using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Data;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>무공 탭. Lists every martial art: learn it with 두 once its stage is reached, then equip up to <see cref="Progression.MaxEquippedArts"/>.</summary>
    public class MartialArtsScreen : MonoBehaviour
    {
        private sealed class Row
        {
            public MartialArtDef Art;
            public Image Band;
            public Text Title, Detail, ButtonLabel;
            public Button Button;
            public Image ButtonImage;
        }

        private readonly List<Row> _rows = new List<Row>();
        private Text _header, _hint;
        private float _nextRefresh;
        private bool _built;

        public void Build(RectTransform root)
        {
            if (_built) return;
            _built = true;

            UiFactory.Panel("HeaderBand", root, new Vector2(0.04f, 0.925f), new Vector2(0.96f, 0.985f), UiFactory.Ink);
            _header = UiFactory.Label("Header", root, new Vector2(0.06f, 0.925f), new Vector2(0.94f, 0.985f), "", 42, UiFactory.Gold);
            _hint = UiFactory.Label("Hint", root, new Vector2(0.05f, 0.885f), new Vector2(0.95f, 0.925f), "", 28, UiFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);

            var arts = ContentDatabase.Arts;
            const float top = 0.875f, rowH = 0.108f, gap = 0.008f;
            for (int i = 0; i < arts.Count; i++)
            {
                float y1 = top - i * (rowH + gap), y0 = y1 - rowH;
                if (y0 < 0.02f) break; // out of room; a scroll view comes with the full 무공 screen
                var row = new Row { Art = arts[i] };
                row.Band = UiFactory.Panel("Row" + i, root, new Vector2(0.04f, y0), new Vector2(0.96f, y1), UiFactory.Ink);
                row.Title = UiFactory.Label("Title", root, new Vector2(0.06f, y0 + rowH * 0.50f), new Vector2(0.62f, y1 - 0.004f), "", 38, UiFactory.PaperText, TextAnchor.MiddleLeft);
                row.Detail = UiFactory.Label("Detail", root, new Vector2(0.06f, y0 + 0.004f), new Vector2(0.62f, y0 + rowH * 0.50f), "", 26, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                var art = arts[i];
                row.Button = UiFactory.Button("Btn" + i, root, new Vector2(0.64f, y0 + 0.012f), new Vector2(0.94f, y1 - 0.012f), "", UiFactory.Red, () => OnPressed(art), 32);
                row.ButtonImage = row.Button.GetComponent<Image>();
                row.ButtonLabel = row.Button.GetComponentInChildren<Text>();
                _rows.Add(row);
            }
            Refresh();
        }

        private void Update()
        {
            if (!_built || Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.4f; // 두 keeps changing from the battle tab
            Refresh();
        }

        private void OnPressed(MartialArtDef art)
        {
            var save = GameManager.Instance.Save;
            if (!save.LearnedArts.Contains(art.id))
            {
                if (save.Stage < art.unlockStage || save.Doo < art.learnCost) return;
                save.Doo -= art.learnCost;
                save.LearnedArts.Add(art.id);
                if (save.EquippedArts.Count < Progression.MaxEquippedArts) save.EquippedArts.Add(art.id);
                _hint.text = art.displayName + " 습득!";
            }
            else if (save.EquippedArts.Contains(art.id))
            {
                save.EquippedArts.Remove(art.id);
                _hint.text = art.displayName + " 장착 해제";
            }
            else if (save.EquippedArts.Count < Progression.MaxEquippedArts)
            {
                save.EquippedArts.Add(art.id);
                _hint.text = art.displayName + " 장착";
            }
            else
            {
                _hint.text = "장착 한도(" + Progression.MaxEquippedArts + "개) 초과 — 먼저 하나를 해제하세요.";
            }
            GameManager.Instance.SaveNow();
            Refresh();
        }

        private void Refresh()
        {
            var save = GameManager.Instance.Save;
            _header.text = "무공  장착 " + save.EquippedArts.Count + "/" + Progression.MaxEquippedArts + "  ·  " + save.Doo.ToString("N0") + " 두";
            if (string.IsNullOrEmpty(_hint.text)) _hint.text = "내공이 있으면 위력이 가장 큰 장착 무공을 자동으로 쓴다.";

            foreach (var r in _rows)
            {
                var a = r.Art;
                bool learned = save.LearnedArts.Contains(a.id);
                bool equipped = save.EquippedArts.Contains(a.id);
                bool unlocked = save.Stage >= a.unlockStage;

                r.Title.text = a.displayName + (equipped ? "  [장착]" : "");
                string cost = a.qiCost > 0 ? "내공 " + a.qiCost + " · 재사용 " + a.cooldown + "턴 · " : "";
                r.Detail.text = unlocked || learned ? "위력 ×" + a.power.ToString("0.0") + " · " + cost + a.description : "스테이지 " + a.unlockStage + "에서 해금";

                if (learned)
                {
                    r.ButtonLabel.text = equipped ? "해제" : "장착";
                    r.ButtonImage.color = equipped ? UiFactory.InkSoft : UiFactory.Red;
                    r.Button.interactable = true;
                }
                else if (!unlocked)
                {
                    r.ButtonLabel.text = "잠김";
                    r.ButtonImage.color = UiFactory.InkSoft;
                    r.Button.interactable = false;
                }
                else
                {
                    r.ButtonLabel.text = "습득 " + a.learnCost.ToString("N0") + "두";
                    r.ButtonImage.color = save.Doo >= a.learnCost ? UiFactory.Red : UiFactory.InkSoft;
                    r.Button.interactable = save.Doo >= a.learnCost;
                }
            }
        }
    }
}
