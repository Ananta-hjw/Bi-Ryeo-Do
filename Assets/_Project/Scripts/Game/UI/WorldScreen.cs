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
        private Text _header, _hint, _promoRowDetail, _partyRowDetail, _questRowDetail, _fortuneRowDetail;
        private RectTransform _promoOverlay, _partyOverlay, _questOverlay, _fortuneOverlay;
        private Action<StageData> _onStart;
        private Action<PromotionOffer> _onPromote;
        private bool _built;

        public void Build(RectTransform root, Action<StageData> onStart, Action<PromotionOffer> onPromote)
        {
            if (_built) return;
            _built = true;
            _onPromote = onPromote;
            _onStart = onStart;

            UiFactory.Panel("HeaderBand", root, new Vector2(0.04f, 0.925f), new Vector2(0.96f, 0.985f), UiFactory.Ink);
            _header = UiFactory.Label("Header", root, new Vector2(0.06f, 0.925f), new Vector2(0.94f, 0.985f), "", 34, UiFactory.Gold, TextAnchor.MiddleLeft);
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
            int total = stages.Length + 4;
            content.sizeDelta = new Vector2(0f, total * (rowH + gap) + gap);

            // Row 0: 승급 시험장
            var promo = NewRow(content, 0, rowH, gap, new Color(0.30f, 0.22f, 0.06f, 0.92f));
            UiFactory.Label("Title", promo, new Vector2(0.03f, 0.50f), new Vector2(0.64f, 0.97f), "승급 시험장", 40, UiFactory.Gold, TextAnchor.MiddleLeft);
            _promoRowDetail = UiFactory.Label("Detail", promo, new Vector2(0.03f, 0.03f), new Vector2(0.64f, 0.50f), "", 26, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
            var promoBtn = UiFactory.Button("Promo", promo, new Vector2(0.66f, 0.12f), new Vector2(0.97f, 0.88f), "비무첩", UiFactory.Red, OpenPromotionOverlay, 36);
            promoBtn.GetComponent<Image>().color = UiFactory.Red;

            // Row 1: 파티 편성 (only three companions fight at once)
            var partyRow = NewRow(content, 1, rowH, gap, new Color(0.10f, 0.20f, 0.24f, 0.92f));
            UiFactory.Label("Title", partyRow, new Vector2(0.03f, 0.50f), new Vector2(0.64f, 0.97f), "파티 편성", 40, UiFactory.Gold, TextAnchor.MiddleLeft);
            _partyRowDetail = UiFactory.Label("Detail", partyRow, new Vector2(0.03f, 0.03f), new Vector2(0.64f, 0.50f), "", 26, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
            UiFactory.Button("Party", partyRow, new Vector2(0.66f, 0.12f), new Vector2(0.97f, 0.88f), "편성", UiFactory.Red, OpenPartyOverlay, 36);

            // Row 2: 의뢰 게시판, Row 3: 기연
            var questRow = NewRow(content, 2, rowH, gap, new Color(0.20f, 0.16f, 0.08f, 0.92f));
            UiFactory.Label("Title", questRow, new Vector2(0.03f, 0.50f), new Vector2(0.64f, 0.97f), "의뢰 게시판", 40, UiFactory.Gold, TextAnchor.MiddleLeft);
            _questRowDetail = UiFactory.Label("Detail", questRow, new Vector2(0.03f, 0.03f), new Vector2(0.64f, 0.50f), "", 26, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
            UiFactory.Button("Quests", questRow, new Vector2(0.66f, 0.12f), new Vector2(0.97f, 0.88f), "의뢰", UiFactory.Red, OpenQuestOverlay, 36);

            var fortuneRow = NewRow(content, 3, rowH, gap, new Color(0.14f, 0.12f, 0.22f, 0.92f));
            UiFactory.Label("Title", fortuneRow, new Vector2(0.03f, 0.50f), new Vector2(0.64f, 0.97f), "기연", 40, UiFactory.Gold, TextAnchor.MiddleLeft);
            _fortuneRowDetail = UiFactory.Label("Detail", fortuneRow, new Vector2(0.03f, 0.03f), new Vector2(0.64f, 0.50f), "", 26, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
            UiFactory.Button("Fortunes", fortuneRow, new Vector2(0.66f, 0.12f), new Vector2(0.97f, 0.88f), "만나다", UiFactory.Red, OpenFortuneOverlay, 36);

            for (int i = 0; i < stages.Length; i++)
            {
                var st = stages[i];
                var rt = NewRow(content, i + 4, rowH, gap, UiFactory.Ink);
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

            _partyOverlay = UiFactory.Rect("PartyOverlay", root, Vector2.zero, new Vector2(1f, 0.92f));
            var partyBg = _partyOverlay.gameObject.AddComponent<Image>();
            partyBg.color = new Color(0.06f, 0.05f, 0.04f, 0.97f);
            _partyOverlay.gameObject.SetActive(false);

            _questOverlay = UiFactory.Rect("QuestOverlay", root, Vector2.zero, new Vector2(1f, 0.92f));
            _questOverlay.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.05f, 0.04f, 0.97f);
            _questOverlay.gameObject.SetActive(false);

            _fortuneOverlay = UiFactory.Rect("FortuneOverlay", root, Vector2.zero, new Vector2(1f, 0.92f));
            _fortuneOverlay.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.05f, 0.04f, 0.97f);
            _fortuneOverlay.gameObject.SetActive(false);
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


        // ------------------------------------------------------------------ 의뢰 / 기연

        private void OpenQuestOverlay()
        {
            var save = GameManager.Instance.Save;
            ClearChildren(_questOverlay);
            _questOverlay.gameObject.SetActive(true);
            UiFactory.Label("Title", _questOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "의뢰 게시판", 44, UiFactory.Gold);
            UiFactory.Label("Rules", _questOverlay, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.92f),
                "이미 지나온 싸움에 조건이 붙어 다시 나온다. 한 번씩만 받을 수 있고, 두와 보상을 얻는다.", 26, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);

            float y = 0.84f;
            int shown = 0;
            foreach (var q in Quests.All)
            {
                if (!Quests.IsUnlocked(save, q)) continue;
                bool done = Quests.IsDone(save, q.Id);
                if (y < 0.14f) break;
                var band = UiFactory.Panel("Card", _questOverlay, new Vector2(0.05f, y - 0.10f), new Vector2(0.95f, y), UiFactory.Ink);
                UiFactory.Label("Name", band.transform, new Vector2(0.03f, 0.54f), new Vector2(0.66f, 0.97f), q.Name + (done ? "  (완료)" : ""), 32, done ? UiFactory.InkSoft : UiFactory.PaperText, TextAnchor.MiddleLeft);
                UiFactory.Label("Info", band.transform, new Vector2(0.03f, 0.03f), new Vector2(0.66f, 0.54f), q.Trial.Brief + " · 보상 " + q.RewardDoo.ToString("N0") + "두", 22, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                var quest = q;
                var go = UiFactory.Button("Go", band.transform, new Vector2(0.69f, 0.12f), new Vector2(0.97f, 0.88f), done ? "완료" : "수락", done ? UiFactory.InkSoft : UiFactory.Red, () => AcceptQuest(quest), 32);
                go.interactable = !done;
                y -= 0.11f;
                shown++;
            }
            if (shown == 0)
                UiFactory.Label("None", _questOverlay, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.7f), "아직 받을 수 있는 의뢰가 없다. 스테이지를 돌파하면 의뢰가 붙는다.", 34, UiFactory.PaperText);

            UiFactory.Button("Close", _questOverlay, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.08f), "닫기", UiFactory.InkSoft, () => { _questOverlay.gameObject.SetActive(false); Refresh(); }, 36);
        }

        private void AcceptQuest(QuestData q)
        {
            if (Quests.IsDone(GameManager.Instance.Save, q.Id)) return;
            _questOverlay.gameObject.SetActive(false);
            _onStart(Quests.ToStage(q));
        }

        private void OpenFortuneOverlay()
        {
            ClearChildren(_fortuneOverlay);
            _fortuneOverlay.gameObject.SetActive(true);
            var save = GameManager.Instance.Save;
            var open = Fortunes.Open(save);
            UiFactory.Label("Title", _fortuneOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "기연", 44, UiFactory.Gold);
            if (open.Count == 0)
            {
                UiFactory.Label("None", _fortuneOverlay, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.7f), "지금은 마주칠 인연이 없다. 스테이지를 돌파하면 새로운 기연이 찾아온다.", 34, UiFactory.PaperText);
            }
            float y = 0.88f;
            foreach (var ev in open)
            {
                if (y < 0.14f) break;
                var e = ev;
                var band = UiFactory.Panel("Card", _fortuneOverlay, new Vector2(0.05f, y - 0.09f), new Vector2(0.95f, y), UiFactory.Ink);
                UiFactory.Label("Name", band.transform, new Vector2(0.03f, 0.10f), new Vector2(0.64f, 0.90f), e.Title, 34, UiFactory.PaperText, TextAnchor.MiddleLeft);
                UiFactory.Button("Go", band.transform, new Vector2(0.68f, 0.12f), new Vector2(0.97f, 0.88f), "마주치다", UiFactory.Red, () => ShowFortune(e), 30);
                y -= 0.10f;
            }
            UiFactory.Button("Close", _fortuneOverlay, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.08f), "닫기", UiFactory.InkSoft, () => { _fortuneOverlay.gameObject.SetActive(false); Refresh(); }, 36);
        }

        private void ShowFortune(FortuneEvent ev)
        {
            var save = GameManager.Instance.Save;
            ClearChildren(_fortuneOverlay);
            UiFactory.Label("Title", _fortuneOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), ev.Title, 44, UiFactory.Gold);
            UiFactory.Label("Text", _fortuneOverlay, new Vector2(0.07f, 0.52f), new Vector2(0.93f, 0.90f), string.Join("\n\n", ev.Text), 34, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);
            float y = 0.46f;
            foreach (var ch in ev.Choices)
            {
                var choice = ch;
                bool ok = Fortunes.CanChoose(save, choice);
                var btn = UiFactory.Button("Choice", _fortuneOverlay, new Vector2(0.08f, y - 0.09f), new Vector2(0.92f, y), choice.Label + (ok ? "" : " (두 부족)"), ok ? UiFactory.Red : UiFactory.InkSoft, () => ResolveFortune(ev, choice), 30);
                btn.interactable = ok;
                y -= 0.11f;
            }
        }

        private void ResolveFortune(FortuneEvent ev, FortuneChoice choice)
        {
            var save = GameManager.Instance.Save;
            var companions = new List<string>();
            foreach (var c in AvailableCompanions(save)) companions.Add(c.Id);
            string result = Fortunes.Resolve(save, ev, choice, companions);
            GameManager.Instance.SaveNow();
            ClearChildren(_fortuneOverlay);
            UiFactory.Label("Title", _fortuneOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), ev.Title, 44, UiFactory.Gold);
            UiFactory.Label("Result", _fortuneOverlay, new Vector2(0.07f, 0.35f), new Vector2(0.93f, 0.85f), result, 36, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);
            UiFactory.Button("Done", _fortuneOverlay, new Vector2(0.3f, 0.12f), new Vector2(0.7f, 0.22f), "확인", UiFactory.Red, OpenFortuneOverlay, 40);
        }

        private static void ClearChildren(RectTransform rt)
        {
            for (int i = rt.childCount - 1; i >= 0; i--) Destroy(rt.GetChild(i).gameObject);
        }

        /// <summary>Companions available now: everyone who has joined by the furthest stage reached.</summary>
        private static List<CompanionData> AvailableCompanions(SaveData save)
        {
            return EarlyGameContent.CompanionsFor(Math.Max(1, save.HighestUnlockedStage));
        }

        private void OpenPartyOverlay()
        {
            var save = GameManager.Instance.Save;
            for (int i = _partyOverlay.childCount - 1; i >= 0; i--) Destroy(_partyOverlay.GetChild(i).gameObject);
            _partyOverlay.gameObject.SetActive(true);

            var available = AvailableCompanions(save);
            var active = EarlyGameContent.SelectCompanions(Math.Max(1, save.HighestUnlockedStage), save.ActiveCompanions);
            UiFactory.Label("Title", _partyOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "파티 편성", 44, UiFactory.Gold);
            UiFactory.Label("Rules", _partyOverlay, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.92f),
                "비류연과 함께 싸울 동료는 최대 " + EarlyGameContent.MaxCompanions + "명이다. 눌러서 출전과 대기를 바꾼다.", 28, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);

            if (available.Count == 0)
                UiFactory.Label("None", _partyOverlay, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.7f), "아직 함께할 동료가 없다.", 40, UiFactory.PaperText);

            float y = 0.82f;
            foreach (var c in available)
            {
                bool on = active.Contains(c);
                var band = UiFactory.Panel("Card", _partyOverlay, new Vector2(0.05f, y - 0.10f), new Vector2(0.95f, y), UiFactory.Ink);
                UiFactory.Label("Name", band.transform, new Vector2(0.03f, 0.52f), new Vector2(0.64f, 0.97f), c.Name, 36, on ? UiFactory.Gold : UiFactory.PaperText, TextAnchor.MiddleLeft);
                UiFactory.Label("Role", band.transform, new Vector2(0.03f, 0.03f), new Vector2(0.64f, 0.52f), c.Role, 24, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                var companion = c;
                UiFactory.Button("Toggle", band.transform, new Vector2(0.68f, 0.12f), new Vector2(0.97f, 0.88f), on ? "출전" : "대기", on ? UiFactory.Red : UiFactory.InkSoft, () => ToggleCompanion(companion), 32);
                y -= 0.11f;
            }

            UiFactory.Button("Close", _partyOverlay, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.08f), "닫기", UiFactory.InkSoft, () => { _partyOverlay.gameObject.SetActive(false); Refresh(); }, 36);
        }

        private void ToggleCompanion(CompanionData c)
        {
            var save = GameManager.Instance.Save;
            int stage = Math.Max(1, save.HighestUnlockedStage);
            var picked = new List<string>();
            foreach (var x in EarlyGameContent.SelectCompanions(stage, save.ActiveCompanions)) picked.Add(x.Id);
            if (picked.Contains(c.Id)) picked.Remove(c.Id);
            else if (picked.Count < EarlyGameContent.MaxCompanions) picked.Add(c.Id);
            else return;   // already full: bench someone first
            save.ActiveCompanions = picked;
            GameManager.Instance.SaveNow();
            OpenPartyOverlay();
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

        private static string PartyText(StageData st)
        {
            var companions = EarlyGameContent.SelectCompanions(st.Number, GameManager.Instance.Save.ActiveCompanions);
            if (companions.Count == 0) return "";
            var names = new List<string>();
            foreach (var c in companions) names.Add(c.Name);
            return " · 동료 " + string.Join(", ", names);
        }

        public void Refresh()
        {
            if (!_built) return;
            var save = GameManager.Instance.Save;
            _header.text = Promotion.RankName(Promotion.EffectiveRank(save)) + " · Lv." + save.Level + " · " + save.Doo.ToString("N0") + "두";
            _promoRowDetail.text = Promotion.RankName(Promotion.EffectiveRank(save)) + (Promotion.EffectiveRank(save) >= Promotion.MaxRank ? " · 최고 경지" : " · 한 계단 또는 두 계단 승급 도전")
                + (Promotion.EffectiveRank(save) < Promotion.GateRank ? "\n(" + Promotion.RankName(Promotion.GateRank) + " 이상이어야 삼성무제 참가)" : "");
            int openQuests = Quests.Open(save).Count, openFortunes = Fortunes.Open(save).Count;
            _questRowDetail.text = openQuests > 0 ? "받을 수 있는 의뢰 " + openQuests + "건" : "새 의뢰 없음 (스테이지를 돌파하면 늘어난다)";
            _fortuneRowDetail.text = openFortunes > 0 ? "마주칠 기연 " + openFortunes + "건" : "새 기연 없음";
            var current = new List<string>();
            foreach (var c in EarlyGameContent.SelectCompanions(Math.Max(1, save.HighestUnlockedStage), save.ActiveCompanions)) current.Add(c.Name + "(인연 " + Bonds.LevelNames[Bonds.Level(save, c.Id)] + ")");
            _partyRowDetail.text = current.Count == 0 ? "아직 함께할 동료가 없다" : "출전 동료: " + string.Join(", ", current)
                + (AvailableCompanions(save).Count > EarlyGameContent.MaxCompanions ? "\n(동료 " + AvailableCompanions(save).Count + "명 중 " + EarlyGameContent.MaxCompanions + "명만 출전)" : "");

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
