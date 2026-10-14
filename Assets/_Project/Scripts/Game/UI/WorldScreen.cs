using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>강호 탭: the story map. Pick an unlocked stage to fight it; cleared stages can be replayed for exp and 두.</summary>
    public class WorldScreen : MonoBehaviour
    {
        private sealed class Row
        {
            public StageData Stage;
            public Text Title, Detail, ButtonLabel;
            public Image ButtonImage, Band;
            public Button Button;
        }

        private readonly List<Row> _rows = new List<Row>();
        private Text _header, _hint;
        private GameObject _rebornButton;
        private bool _built;

        public void Build(RectTransform root, Action<StageData> onStart)
        {
            if (_built) return;
            _built = true;

            UiFactory.Panel("HeaderBand", root, new Vector2(0.04f, 0.925f), new Vector2(0.96f, 0.985f), UiFactory.Ink);
            _header = UiFactory.Label("Header", root, new Vector2(0.06f, 0.925f), new Vector2(0.62f, 0.985f), "", 40, UiFactory.Gold, TextAnchor.MiddleLeft);
            var reborn = UiFactory.Button("Reincarnate", root, new Vector2(0.66f, 0.932f), new Vector2(0.95f, 0.978f), "환생", UiFactory.Gold, OnRebornPressed, 36);
            reborn.GetComponentInChildren<Text>().color = UiFactory.Ink;
            _rebornButton = reborn.gameObject;
            _hint = UiFactory.Label("Hint", root, new Vector2(0.05f, 0.885f), new Vector2(0.95f, 0.925f), "스테이지를 골라 출전하자. 전투는 직접 무공을 골라 싸운다.", 28, UiFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);

            var stages = EarlyGameContent.Stages;
            const float top = 0.875f, rowH = 0.112f, gap = 0.008f;
            for (int i = 0; i < stages.Length; i++)
            {
                float y1 = top - i * (rowH + gap), y0 = y1 - rowH;
                var st = stages[i];
                var row = new Row { Stage = st };
                row.Band = UiFactory.Panel("Row" + i, root, new Vector2(0.04f, y0), new Vector2(0.96f, y1), UiFactory.Ink);
                row.Title = UiFactory.Label("Title", root, new Vector2(0.06f, y0 + rowH * 0.50f), new Vector2(0.62f, y1 - 0.004f), "", 40, UiFactory.PaperText, TextAnchor.MiddleLeft);
                row.Detail = UiFactory.Label("Detail", root, new Vector2(0.06f, y0 + 0.004f), new Vector2(0.62f, y0 + rowH * 0.50f), "", 26, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                row.Button = UiFactory.Button("Go" + i, root, new Vector2(0.65f, y0 + 0.012f), new Vector2(0.94f, y1 - 0.012f), "", UiFactory.Red, () => OnStagePressed(st, onStart), 36);
                row.ButtonImage = row.Button.GetComponent<Image>();
                row.ButtonLabel = row.Button.GetComponentInChildren<Text>();
                _rows.Add(row);
            }
            Refresh();
        }

        private void OnEnable() { if (_built) Refresh(); }

        private void OnStagePressed(StageData st, Action<StageData> onStart)
        {
            if (GameManager.Instance.Save.HighestUnlockedStage < st.Number) return;
            onStart(st);
        }

        private void OnRebornPressed()
        {
            var save = GameManager.Instance.Save;
            if (!Progression.Reincarnate(save)) return;
            GameManager.Instance.SaveNow();
            _hint.text = "환생! 제" + save.Life + "생이 시작된다. 두와 배운 무공은 그대로 이어진다.";
            Refresh();
        }

        private static string PartyText(StageData st)
        {
            var companions = EarlyGameContent.CompanionsFor(st.Number);
            if (companions.Count == 0) return "";
            var names = new List<string>();
            foreach (var c in companions) names.Add(c.Name);
            return " · 동료 " + string.Join(", ", names);
        }

        public void Refresh()
        {
            if (!_built) return;
            var save = GameManager.Instance.Save;
            _header.text = "제" + save.Life + "생 · Lv." + save.Level + " · " + save.Doo.ToString("N0") + "두";
            _rebornButton.SetActive(Progression.CanReincarnate(save));

            foreach (var r in _rows)
            {
                var st = r.Stage;
                bool unlocked = save.HighestUnlockedStage >= st.Number;
                bool cleared = save.ClearedStage >= st.Number;
                r.Title.text = "스테이지 " + st.Number + " · " + st.Name;
                r.Detail.text = unlocked ? "권장 Lv." + st.RecommendedLevel + " · 전투 " + st.Encounters.Length + "회" + PartyText(st) + (cleared ? " · 돌파" : "") : "이전 스테이지를 돌파하면 열린다";
                r.ButtonLabel.text = !unlocked ? "잠김" : cleared ? "재도전" : "출전";
                r.ButtonImage.color = !unlocked ? UiFactory.InkSoft : cleared ? new Color(0.35f, 0.35f, 0.32f, 1f) : UiFactory.Red;
                r.Button.interactable = unlocked;
            }
        }
    }
}
