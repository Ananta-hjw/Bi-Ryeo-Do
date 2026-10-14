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
        private Text _header, _hint, _promoRowDetail;
        private GameObject _rebornButton;
        private RectTransform _promoOverlay;
        private Action<PromotionOffer> _onPromote;
        private bool _built;

        public void Build(RectTransform root, Action<StageData> onStart, Action<PromotionOffer> onPromote)
        {
            if (_built) return;
            _built = true;
            _onPromote = onPromote;

            UiFactory.Panel("HeaderBand", root, new Vector2(0.04f, 0.925f), new Vector2(0.96f, 0.985f), UiFactory.Ink);
            _header = UiFactory.Label("Header", root, new Vector2(0.06f, 0.925f), new Vector2(0.62f, 0.985f), "", 34, UiFactory.Gold, TextAnchor.MiddleLeft);
            var reborn = UiFactory.Button("Reincarnate", root, new Vector2(0.66f, 0.932f), new Vector2(0.95f, 0.978f), "환생", UiFactory.Gold, OnRebornPressed, 36);
            reborn.GetComponentInChildren<Text>().color = UiFactory.Ink;
            _rebornButton = reborn.gameObject;
            _hint = UiFactory.Label("Hint", root, new Vector2(0.05f, 0.885f), new Vector2(0.95f, 0.925f), "스테이지를 골라 출전하자. 전투는 직접 무공을 골라 싸운다.", 28, UiFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);

            // The stage list scrolls: 13 stages plus the 승급 시험장 do not fit on one screen.
            var viewport = UiFactory.Rect("Viewport", root, Vector2.zero, new Vector2(1f, 0.88f));
            var vpImage = viewport.gameObject.AddComponent<Image>();
            vpImage.color = new Color(0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UiFactory.Rect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f));
            content.pivot = new Vector2(0.5f, 1f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            const float rowH = 190f, gap = 14f;
            var stages = EarlyGameContent.Stages;
            int total = stages.Length + 1;
            content.sizeDelta = new Vector2(0f, total * (rowH + gap) + gap);

            // Row 0: 승급 시험장
            var promo = NewRow(content, 0, rowH, gap, new Color(0.30f, 0.22f, 0.06f, 0.92f));
            UiFactory.Label("Title", promo, new Vector2(0.03f, 0.50f), new Vector2(0.64f, 0.97f), "승급 시험장", 40, UiFactory.Gold, TextAnchor.MiddleLeft);
            _promoRowDetail = UiFactory.Label("Detail", promo, new Vector2(0.03f, 0.03f), new Vector2(0.64f, 0.50f), "", 26, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
            var promoBtn = UiFactory.Button("Promo", promo, new Vector2(0.66f, 0.12f), new Vector2(0.97f, 0.88f), "비무첩", UiFactory.Red, OpenPromotionOverlay, 36);
            promoBtn.GetComponent<Image>().color = UiFactory.Red;

            for (int i = 0; i < stages.Length; i++)
            {
                var st = stages[i];
                var rt = NewRow(content, i + 1, rowH, gap, UiFactory.Ink);
                var row = new Row { Stage = st };
                row.Title = UiFactory.Label("Title", rt, new Vector2(0.03f, 0.50f), new Vector2(0.64f, 0.97f), "", 40, UiFactory.PaperText, TextAnchor.MiddleLeft);
                row.Detail = UiFactory.Label("Detail", rt, new Vector2(0.03f, 0.03f), new Vector2(0.64f, 0.50f), "", 26, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                row.Button = UiFactory.Button("Go" + i, rt, new Vector2(0.66f, 0.12f), new Vector2(0.97f, 0.88f), "", UiFactory.Red, () => OnStagePressed(st, onStart), 36);
                row.ButtonImage = row.Button.GetComponent<Image>();
                row.ButtonLabel = row.Button.GetComponentInChildren<Text>();
                _rows.Add(row);
            }

            _promoOverlay = UiFactory.Rect("PromotionOverlay", root, Vector2.zero, new Vector2(1f, 0.92f));
            var overlayBg = _promoOverlay.gameObject.AddComponent<Image>();
            overlayBg.color = new Color(0.06f, 0.05f, 0.04f, 0.97f);
            _promoOverlay.gameObject.SetActive(false);
            Refresh();
        }

        private static RectTransform NewRow(RectTransform content, int index, float h, float gap, Color color)
        {
            var rt = UiFactory.Rect("Row" + index, content, new Vector2(0.04f, 1f), new Vector2(0.96f, 1f));
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -(gap + index * (h + gap)));
            rt.sizeDelta = new Vector2(0f, h);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;   // lets a drag that starts on a row still scroll
            return rt;
        }

        private void OpenPromotionOverlay()
        {
            var save = GameManager.Instance.Save;
            for (int i = _promoOverlay.childCount - 1; i >= 0; i--) Destroy(_promoOverlay.GetChild(i).gameObject);
            _promoOverlay.gameObject.SetActive(true);

            int rank = Promotion.EffectiveRank(save);
            UiFactory.Label("Title", _promoOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "승급 시험장 · 현재 " + Promotion.RankName(rank), 44, UiFactory.Gold);
            UiFactory.Label("Rules", _promoOverlay, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.92f),
                "상대 둘을 혼자서 연달아 꺾는다. 한 판은 " + Promotion.TurnLimit + "턴 안에 끝내야 하며, 시간 초과는 무승부(상금 절반)다. 패배하면 입장료는 돌려받지 못한다.", 26, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);

            var targets = Promotion.TargetRanks(save);
            if (targets.Count == 0)
                UiFactory.Label("Max", _promoOverlay, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.7f), "이미 최고 경지의 검룡이다.", 40, UiFactory.PaperText);

            float y = 0.82f;
            foreach (int target in targets)
            {
                bool skip = target == rank + 2;
                UiFactory.Label("Target" + target, _promoOverlay, new Vector2(0.05f, y - 0.05f), new Vector2(0.95f, y),
                    Promotion.RankName(target) + (skip ? "  (월반: 상금 x" + Promotion.SkipPayFactor + ")" : ""), 34, UiFactory.Gold, TextAnchor.MiddleLeft);
                y -= 0.055f;
                for (int tier = 0; tier < 3; tier++)
                {
                    var offer = Promotion.BuildOffer(save, target, tier);
                    bool afford = Promotion.CanAfford(save, offer);
                    float y1 = y, y0 = y - 0.085f;
                    var band = UiFactory.Panel("Card", _promoOverlay, new Vector2(0.05f, y0), new Vector2(0.95f, y1), UiFactory.Ink);
                    UiFactory.Label("Name", band.transform, new Vector2(0.03f, 0.52f), new Vector2(0.64f, 0.97f), offer.Opponents[0].Name.Split(' ')[0] + " 비무첩", 32, UiFactory.PaperText, TextAnchor.MiddleLeft);
                    UiFactory.Label("Info", band.transform, new Vector2(0.03f, 0.03f), new Vector2(0.64f, 0.52f),
                        "입장료 " + offer.Fee.ToString("N0") + "두 · 상금 " + offer.DooReward.ToString("N0") + "두", 24, afford ? UiFactory.PaperText : UiFactory.Red, TextAnchor.MiddleLeft, FontStyle.Normal);
                    var offerCopy = offer;
                    var go = UiFactory.Button("Go", band.transform, new Vector2(0.68f, 0.12f), new Vector2(0.97f, 0.88f), afford ? "도전" : "두 부족", afford ? UiFactory.Red : UiFactory.InkSoft, () => OnChallenge(offerCopy), 32);
                    go.interactable = afford;
                    y = y0 - 0.008f;
                }
                y -= 0.02f;
            }

            UiFactory.Button("Close", _promoOverlay, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.08f), "닫기", UiFactory.InkSoft, () => _promoOverlay.gameObject.SetActive(false), 36);
        }

        private void OnChallenge(PromotionOffer offer)
        {
            var save = GameManager.Instance.Save;
            if (!Promotion.Pay(save, offer)) return;
            GameManager.Instance.SaveNow();
            _promoOverlay.gameObject.SetActive(false);
            _onPromote(offer);
        }

        private void OnEnable() { if (_built) Refresh(); }

        private void OnStagePressed(StageData st, Action<StageData> onStart)
        {
            var save = GameManager.Instance.Save;
            if (save.HighestUnlockedStage < st.Number || !Promotion.StageAllowed(save, st.Number)) return;
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
            _header.text = "제" + save.Life + "생 · " + Promotion.RankName(Promotion.EffectiveRank(save)) + " · Lv." + save.Level + " · " + save.Doo.ToString("N0") + "두";
            _promoRowDetail.text = Promotion.RankName(Promotion.EffectiveRank(save)) + (Promotion.EffectiveRank(save) >= Promotion.MaxRank ? " · 최고 경지" : " · 한 계단 또는 두 계단 승급 도전")
                + (Promotion.EffectiveRank(save) < Promotion.GateRank ? "\n(" + Promotion.RankName(Promotion.GateRank) + " 이상이어야 삼성무제 참가)" : "");
            _rebornButton.SetActive(Progression.CanReincarnate(save));

            foreach (var r in _rows)
            {
                var st = r.Stage;
                bool reached = save.HighestUnlockedStage >= st.Number;
                bool rankOk = Promotion.StageAllowed(save, st.Number);
                bool unlocked = reached && rankOk;
                bool cleared = save.ClearedStage >= st.Number;
                r.Title.text = "스테이지 " + st.Number + " · " + st.Name;
                r.Detail.text = reached && !rankOk ? Promotion.RankName(Promotion.GateRank) + " 이상이 되어야 참가할 수 있다 (승급 시험장)" : unlocked ? "권장 Lv." + st.RecommendedLevel + " · 전투 " + st.Encounters.Length + "회" + PartyText(st) + (cleared ? " · 돌파" : "") : "이전 스테이지를 돌파하면 열린다";
                r.ButtonLabel.text = !unlocked ? (reached ? "승급 필요" : "잠김") : cleared ? "재도전" : "출전";
                r.ButtonImage.color = !unlocked ? UiFactory.InkSoft : cleared ? new Color(0.35f, 0.35f, 0.32f, 1f) : UiFactory.Red;
                r.Button.interactable = unlocked;
            }
        }
    }
}
