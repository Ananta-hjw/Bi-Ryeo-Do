using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>무공 탭. Learn arts with 두 once their stage is unlocked, then equip up to <see cref="Progression.MaxEquippedArts"/> for battle.</summary>
    public class MartialArtsScreen : MonoBehaviour
    {
        private sealed class Row
        {
            public ArtData Art;
            public Text Title, Detail, ButtonLabel;
            public Button Button;
            public Image ButtonImage;
        }

        private readonly List<Row> _rows = new List<Row>();
        private Text _header, _hint;
        private bool _built;

        public void Build(RectTransform root)
        {
            if (_built) return;
            _built = true;

            UiFactory.Panel("HeaderBand", root, new Vector2(0.04f, 0.925f), new Vector2(0.96f, 0.985f), UiFactory.Ink);
            _header = UiFactory.Label("Header", root, new Vector2(0.06f, 0.925f), new Vector2(0.94f, 0.985f), "", 42, UiFactory.Gold);
            _hint = UiFactory.Label("Hint", root, new Vector2(0.05f, 0.885f), new Vector2(0.95f, 0.925f), "", 28, UiFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);

            var arts = EarlyGameContent.Arts;
            const float top = 0.875f, rowH = 0.098f, gap = 0.007f;
            for (int i = 0; i < arts.Length; i++)
            {
                float y1 = top - i * (rowH + gap), y0 = y1 - rowH;
                var art = arts[i];
                var row = new Row { Art = art };
                UiFactory.Panel("Row" + i, root, new Vector2(0.04f, y0), new Vector2(0.96f, y1), UiFactory.Ink);
                row.Title = UiFactory.Label("Title", root, new Vector2(0.06f, y0 + rowH * 0.52f), new Vector2(0.64f, y1 - 0.003f), "", 36, UiFactory.PaperText, TextAnchor.MiddleLeft);
                row.Detail = UiFactory.Label("Detail", root, new Vector2(0.06f, y0 + 0.003f), new Vector2(0.64f, y0 + rowH * 0.52f), "", 24, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                row.Button = UiFactory.Button("Btn" + i, root, new Vector2(0.66f, y0 + 0.010f), new Vector2(0.94f, y1 - 0.010f), "", UiFactory.Red, () => OnPressed(art), 30);
                row.ButtonImage = row.Button.GetComponent<Image>();
                row.ButtonLabel = row.Button.GetComponentInChildren<Text>();
                _rows.Add(row);
            }
            Refresh();
        }

        private void OnEnable() { if (_built) Refresh(); }

        private void OnPressed(ArtData art)
        {
            var save = GameManager.Instance.Save;
            if (!save.LearnedArts.Contains(art.Id))
            {
                if (save.HighestUnlockedStage < art.UnlockStage || save.Doo < art.LearnCost) return;
                save.Doo -= art.LearnCost;
                save.LearnedArts.Add(art.Id);
                if (save.EquippedArts.Count < Progression.MaxEquippedArts) save.EquippedArts.Add(art.Id);
                _hint.text = art.Name + " 습득!";
            }
            else if (save.EquippedArts.Contains(art.Id))
            {
                save.EquippedArts.Remove(art.Id);
                _hint.text = art.Name + " 장착 해제";
            }
            else if (save.EquippedArts.Count < Progression.MaxEquippedArts)
            {
                save.EquippedArts.Add(art.Id);
                _hint.text = art.Name + " 장착";
            }
            else
            {
                _hint.text = "장착 한도(" + Progression.MaxEquippedArts + "개) 초과 — 먼저 하나를 해제하세요.";
            }
            GameManager.Instance.SaveNow();
            Refresh();
        }

        private static string Describe(ArtData a)
        {
            string cost = a.SpiritCost > 0 ? "기세 " + a.SpiritCost : a.QiCost > 0 ? "내공 " + a.QiCost : "내공 0";
            string power = a.Effect == SkillEffect.Evade ? "회피" : "위력 ×" + a.Power.ToString("0.0") + (a.Hits > 1 ? " ×" + a.Hits + "타" : "") + (a.AllEnemies ? " 전체" : "");
            string cd = a.Cooldown > 0 ? " · 재사용 " + a.Cooldown + "턴" : "";
            return power + " · " + cost + cd;
        }

        private void Refresh()
        {
            var save = GameManager.Instance.Save;
            _header.text = "무공 · 장착 " + save.EquippedArts.Count + "/" + Progression.MaxEquippedArts + " · " + save.Doo.ToString("N0") + "두";
            if (string.IsNullOrEmpty(_hint.text)) _hint.text = "장착한 무공이 전투 명령에 나타난다. 비기는 기세 100이 차면 쓸 수 있다.";

            foreach (var r in _rows)
            {
                var a = r.Art;
                bool learned = save.LearnedArts.Contains(a.Id);
                bool equipped = save.EquippedArts.Contains(a.Id);
                bool unlocked = save.HighestUnlockedStage >= a.UnlockStage;

                r.Title.text = a.Name + (equipped ? "  [장착]" : "");
                r.Detail.text = learned || unlocked ? Describe(a) : "스테이지 " + a.UnlockStage + "에 도달하면 열린다";

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
                    bool can = save.Doo >= a.LearnCost;
                    r.ButtonLabel.text = "습득 " + a.LearnCost.ToString("N0") + "두";
                    r.ButtonImage.color = can ? UiFactory.Red : UiFactory.InkSoft;
                    r.Button.interactable = can;
                }
            }
        }
    }
}
