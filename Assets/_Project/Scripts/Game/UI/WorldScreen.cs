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
        private StarPanel _stars;
        private SaveData _syncedSave;
        private DailyPanel _daily;
        private Text _dailyRowDetail, _dailyBadge;
        private Action _onStatus;
        private Text _starRowDetail, _starBadge, _starsTitle;
        private readonly Image[] _diffTabs = new Image[StageStars.DifficultyCount];
        private Text _towerRowDetail, _lvText, _dooText, _rankText, _promoRowDetail, _partyRowDetail, _questRowDetail, _fortuneRowDetail;
        private Text _promoBadge, _partyBadge, _questBadge, _fortuneBadge, _featTitle, _featSub, _featBtnLabel;
        private UiFactory.Bar _expBar;
        private Image _featArt;
        private Button _featBtn;
        private StageData _featStage;
        private Action<StageData> _onStartRef;
        // Home / lobby card
        private Image _lobbyBg, _lobbyPortrait;
        private Text _lobbyGlyph, _lobbyName, _lobbyLine;
        private int _lobbyStep;
        private float _lobbyTimer;
        private RectTransform _promoOverlay, _partyOverlay, _questOverlay, _fortuneOverlay, _settingsOverlay, _towerOverlay;
        private bool _confirmReset;
        private Action<StageData> _onStart;
        private Action<PromotionOffer> _onPromote;
        private bool _built;

        public void Build(RectTransform root, Action<StageData> onStart, Action<PromotionOffer> onPromote, Action onStatus)
        {
            if (_built) return;
            _built = true;
            _onStatus = onStatus;
            _onPromote = onPromote;
            _onStart = onStart;

            BuildStatusBar(root);

            // Everything below the status bar scrolls: featured stage, quick menu, stage list.
            var viewport = UiFactory.Rect("Viewport", root, Vector2.zero, new Vector2(1f, 0.915f));
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UiFactory.Rect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f));
            content.pivot = new Vector2(0.5f, 1f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            const float gap = 16f, featH = 380f, tileH = 190f, cardH = 150f, labelH = 70f, lobbyH = 640f;
            var stages = EarlyGameContent.Stages;
            float y = gap;

            // Lobby card: a character stands in front of the latest scenery and says a short line. Tap for the next one.
            var lobby = Place(content, 0.04f, 0.96f, y, lobbyH, new Color(0.07f, 0.09f, 0.14f, 1f));
            _lobbyBg = UiFactory.Panel("Scenery", lobby, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0f));
            _lobbyBg.preserveAspect = false;
            UiFactory.Panel("Shade", lobby, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.30f));
            _lobbyPortrait = UiFactory.Panel("Portrait", lobby, new Vector2(0.02f, 0.02f), new Vector2(0.52f, 0.98f), Color.white);
            _lobbyPortrait.preserveAspect = true;
            _lobbyGlyph = UiFactory.Label("Glyph", _lobbyPortrait.transform, Vector2.zero, Vector2.one, "", 240, UiFactory.PaperText);
            var plate = UiFactory.Panel("NamePlate", lobby, new Vector2(0.50f, 0.74f), new Vector2(0.80f, 0.86f), new Color(0.20f, 0.16f, 0.07f, 0.97f));
            UiFactory.Frame(plate.transform, UiFactory.Gold, 2f);
            _lobbyName = UiFactory.Label("Name", plate.transform, new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), "", 36, UiFactory.Gold);
            var bubble = UiFactory.Panel("Bubble", lobby, new Vector2(0.50f, 0.30f), new Vector2(0.97f, 0.74f), new Color(0.04f, 0.06f, 0.10f, 0.90f));
            UiFactory.Frame(bubble.transform, UiFactory.Gold, 3f);
            _lobbyLine = UiFactory.Label("Line", bubble.transform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f), "", 34, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
            UiFactory.Label("Tap", lobby, new Vector2(0.52f, 0.02f), new Vector2(0.97f, 0.10f), "눌러서 다른 인물과 이야기하기", 22, UiFactory.Dim, TextAnchor.MiddleRight, FontStyle.Normal);
            UiFactory.Frame(lobby, UiFactory.Gold, 2f);
            var lobbyBtn = lobby.gameObject.AddComponent<Button>();
            lobbyBtn.targetGraphic = lobby.GetComponent<Image>();
            lobbyBtn.transition = Selectable.Transition.None;
            lobbyBtn.onClick.AddListener(() => { _lobbyStep++; _lobbyTimer = 0f; ShowLobby(true); });
            y += lobbyH + gap;

            // Featured card: the next stage to play, with one big button.
            var feat = Place(content, 0.04f, 0.96f, y, featH, new Color(0.08f, 0.10f, 0.15f, 0.97f));
            _featArt = UiFactory.Panel("Art", feat, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0f));
            _featArt.preserveAspect = false;
            UiFactory.Panel("Shade", feat, Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0.45f));
            UiFactory.Label("Tag", feat, new Vector2(0.04f, 0.82f), new Vector2(0.60f, 0.96f), "다음 목적지", 28, UiFactory.Gold, TextAnchor.MiddleLeft);
            _featTitle = UiFactory.Label("Title", feat, new Vector2(0.04f, 0.50f), new Vector2(0.96f, 0.82f), "", 58, UiFactory.PaperText, TextAnchor.MiddleLeft);
            _featSub = UiFactory.Label("Sub", feat, new Vector2(0.04f, 0.34f), new Vector2(0.96f, 0.52f), "", 28, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            _featBtn = UiFactory.Button("Go", feat, new Vector2(0.55f, 0.05f), new Vector2(0.96f, 0.30f), "출전", UiFactory.Red, OnFeaturedPressed, 44);
            _featBtnLabel = _featBtn.GetComponentInChildren<Text>();
            UiFactory.Frame(feat, UiFactory.Gold, 2f);
            y += featH + gap;

            // Quick menu: 승급 / 편성 / 의뢰 / 기연.
            _promoRowDetail = Tile(content, 0.04f, 0.49f, y, tileH, "승급 시험장", OpenPromotionOverlay, out _promoBadge);
            _partyRowDetail = Tile(content, 0.51f, 0.96f, y, tileH, "파티 편성", OpenPartyOverlay, out _partyBadge);
            y += tileH + gap;
            _questRowDetail = Tile(content, 0.04f, 0.49f, y, tileH, "의뢰 게시판", OpenQuestOverlay, out _questBadge);
            _fortuneRowDetail = Tile(content, 0.51f, 0.96f, y, tileH, "기연", OpenFortuneOverlay, out _fortuneBadge);
            y += tileH + gap;
            _dailyRowDetail = Tile(content, 0.04f, 0.96f, y, 150f, "일일 수련 임무", () => _daily.Open(), out _dailyBadge);
            y += 150f + gap;
            _starRowDetail = Tile(content, 0.04f, 0.96f, y, 150f, "별 보상", () => _stars.Open(), out _starBadge);
            y += 150f + gap;
            Text towerBadge;
            _towerRowDetail = Tile(content, 0.04f, 0.96f, y, 150f, "무한 비무대", OpenTowerOverlay, out towerBadge);
            towerBadge.transform.parent.gameObject.SetActive(false);
            y += 150f + gap;

            var sectionLabel = Place(content, 0.04f, 0.96f, y, labelH, new Color(0f, 0f, 0f, 0f));
            _starsTitle = UiFactory.Label("Stages", sectionLabel, new Vector2(0f, 0f), new Vector2(1f, 1f), "— 스테이지 —", 36, UiFactory.Gold, TextAnchor.MiddleLeft);
            y += labelH;

            // Difficulty picker for the whole list
            var diffRow = Place(content, 0.04f, 0.96f, y, 84f, new Color(0f, 0f, 0f, 0f));
            for (int d = 0; d < StageStars.DifficultyCount; d++)
            {
                int diffIndex = d;
                float x0 = d * 0.335f;
                var btn = UiFactory.Button("Diff" + d, diffRow, new Vector2(x0, 0.04f), new Vector2(x0 + 0.325f, 0.96f), StageStars.DifficultyNames[d], UiFactory.InkSoft, () => { GameManager.Instance.Save.Difficulty = diffIndex; GameManager.Instance.SaveNow(); Refresh(); }, 30);
                _diffTabs[d] = btn.GetComponent<Image>();
            }
            y += 84f + gap;

            for (int i = 0; i < stages.Length; i++)
            {
                var st = stages[i];
                var rt = Place(content, 0.04f, 0.96f, y, cardH, UiFactory.Ink);
                UiFactory.Frame(rt, UiFactory.GoldDim, 2f);
                var row = new Row { Stage = st };
                var badge = UiFactory.Panel("Badge", rt, new Vector2(0.025f, 0.14f), new Vector2(0.135f, 0.86f), new Color(0.20f, 0.17f, 0.10f, 1f));
                UiFactory.Frame(badge.transform, UiFactory.Gold, 2f);
                UiFactory.Label("Num", badge.transform, Vector2.zero, Vector2.one, st.Number.ToString(), 46, UiFactory.Gold);
                row.Title = UiFactory.Label("Title", rt, new Vector2(0.16f, 0.50f), new Vector2(0.64f, 0.96f), "", 36, UiFactory.PaperText, TextAnchor.MiddleLeft);
                row.Detail = UiFactory.Label("Detail", rt, new Vector2(0.16f, 0.04f), new Vector2(0.64f, 0.50f), "", 22, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                row.Button = UiFactory.Button("Go" + i, rt, new Vector2(0.66f, 0.15f), new Vector2(0.97f, 0.85f), "", UiFactory.Red, () => OnStagePressed(st, onStart), 34);
                row.ButtonImage = row.Button.GetComponent<Image>();
                row.ButtonLabel = row.Button.GetComponentInChildren<Text>();
                _rows.Add(row);
                y += cardH + gap;
            }
            content.sizeDelta = new Vector2(0f, y + gap);
            _onStartRef = onStart;

            _promoOverlay = UiFactory.Rect("PromotionOverlay", root, Vector2.zero, new Vector2(1f, 0.92f));
            var overlayBg = _promoOverlay.gameObject.AddComponent<Image>();
            overlayBg.color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
            _promoOverlay.gameObject.SetActive(false);

            _partyOverlay = UiFactory.Rect("PartyOverlay", root, Vector2.zero, new Vector2(1f, 0.92f));
            var partyBg = _partyOverlay.gameObject.AddComponent<Image>();
            partyBg.color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
            _partyOverlay.gameObject.SetActive(false);

            _questOverlay = UiFactory.Rect("QuestOverlay", root, Vector2.zero, new Vector2(1f, 0.92f));
            _questOverlay.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
            _questOverlay.gameObject.SetActive(false);

            _fortuneOverlay = UiFactory.Rect("FortuneOverlay", root, Vector2.zero, new Vector2(1f, 0.92f));
            _fortuneOverlay.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
            _fortuneOverlay.gameObject.SetActive(false);

            _settingsOverlay = UiFactory.Rect("SettingsOverlay", root, Vector2.zero, new Vector2(1f, 0.92f));
            _settingsOverlay.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
            _settingsOverlay.gameObject.SetActive(false);

            _towerOverlay = UiFactory.Rect("TowerOverlay", root, Vector2.zero, new Vector2(1f, 0.92f));
            _towerOverlay.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
            _towerOverlay.gameObject.SetActive(false);

            _daily = new DailyPanel(UiFactory.Rect("DailyOverlay", root, Vector2.zero, new Vector2(1f, 0.92f)), Refresh);
            _stars = new StarPanel(UiFactory.Rect("StarOverlay", root, Vector2.zero, new Vector2(1f, 0.92f)), Refresh);
            Refresh();
        }

        private void BuildStatusBar(RectTransform root)
        {
            // Lv + exp bar | 두 | 검룡위
            var lv = UiFactory.Panel("LvChip", root, new Vector2(0.03f, 0.932f), new Vector2(0.40f, 0.988f), UiFactory.Ink);
            UiFactory.Frame(lv.transform, UiFactory.GoldDim, 2f);
            _lvText = UiFactory.Label("Lv", lv.transform, new Vector2(0.06f, 0.35f), new Vector2(0.94f, 1f), "", 34, UiFactory.Gold, TextAnchor.MiddleLeft);
            _expBar = UiFactory.MakeBar("Exp", lv.transform, new Vector2(0.06f, 0.14f), new Vector2(0.94f, 0.30f), new Color(0f, 0f, 0f, 0.6f), new Color(0.38f, 0.62f, 0.95f, 1f));
            UiFactory.Button("StatusTap", root, new Vector2(0.03f, 0.932f), new Vector2(0.40f, 0.988f), "", new Color(0f, 0f, 0f, 0f), () => _onStatus?.Invoke(), 20);

            var doo = UiFactory.Panel("DooChip", root, new Vector2(0.42f, 0.932f), new Vector2(0.69f, 0.988f), UiFactory.Ink);
            UiFactory.Frame(doo.transform, UiFactory.GoldDim, 2f);
            UiFactory.Label("Icon", doo.transform, new Vector2(0.04f, 0f), new Vector2(0.28f, 1f), "두", 30, UiFactory.Gold);
            _dooText = UiFactory.Label("Doo", doo.transform, new Vector2(0.28f, 0f), new Vector2(0.96f, 1f), "", 30, UiFactory.PaperText, TextAnchor.MiddleRight);

            var rank = UiFactory.Panel("RankChip", root, new Vector2(0.71f, 0.932f), new Vector2(0.865f, 0.988f), UiFactory.Ink);
            UiFactory.Frame(rank.transform, UiFactory.GoldDim, 2f);
            _rankText = UiFactory.Label("Rank", rank.transform, new Vector2(0.04f, 0f), new Vector2(0.96f, 1f), "", 22, UiFactory.PaperText);
            UiFactory.Button("Settings", root, new Vector2(0.88f, 0.932f), new Vector2(0.97f, 0.988f), "설정", UiFactory.InkSoft, OpenSettingsOverlay, 22);
        }

        /// <summary>A row-like rectangle placed in the scroll content at a pixel offset from the top.</summary>
        private static RectTransform Place(RectTransform content, float x0, float x1, float top, float height, Color color)
        {
            var rt = UiFactory.Rect("Item", content, new Vector2(x0, 1f), new Vector2(x1, 1f));
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -top);
            rt.sizeDelta = new Vector2(0f, height);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;   // lets a drag that starts here still scroll
            return rt;
        }

        /// <summary>A square-ish menu tile (whole tile is the button) with a title, a detail line and a count badge; returns the detail label.</summary>
        private Text Tile(RectTransform content, float x0, float x1, float top, float height, string title, UnityEngine.Events.UnityAction onClick, out Text badge)
        {
            var rt = UiFactory.Rect("Tile", content, new Vector2(x0, 1f), new Vector2(x1, 1f));
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -top);
            rt.sizeDelta = new Vector2(0f, height);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UiFactory.Gradient;
            img.color = new Color(0.16f, 0.19f, 0.27f, 1f);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            UiFactory.Frame(rt, UiFactory.GoldDim, 2f);
            UiFactory.Label("Title", rt, new Vector2(0.08f, 0.62f), new Vector2(0.80f, 0.94f), title, 38, UiFactory.Gold, TextAnchor.MiddleLeft);
            var detail = UiFactory.Label("Detail", rt, new Vector2(0.08f, 0.06f), new Vector2(0.94f, 0.62f), "", 22, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal);
            var dot = UiFactory.Panel("Dot", rt, new Vector2(0.80f, 0.70f), new Vector2(0.95f, 0.94f), UiFactory.Red);
            badge = UiFactory.Label("Count", dot.transform, Vector2.zero, Vector2.one, "", 28, UiFactory.PaperText);
            badgeRoots.Add(dot.gameObject);
            return detail;
        }

        private readonly List<GameObject> badgeRoots = new List<GameObject>();

        private void Update()
        {
            if (!_built) return;
            _lobbyTimer += Time.unscaledDeltaTime;
            if (_lobbyTimer > 9f) { _lobbyTimer = 0f; _lobbyStep++; ShowLobby(true); }
        }

        /// <summary>Puts the current lobby character and line on the card.</summary>
        private void ShowLobby(bool animate)
        {
            if (_lobbyLine == null) return;
            var save = GameManager.Instance.Save;
            var scene = Lobby.Pick(save, _lobbyStep);
            _lobbyName.text = scene.Speaker;
            _lobbyLine.text = scene.Text;

            var bg = ArtLibrary.Background(Mathf.Max(1, Mathf.Min(save.ClearedStage, EarlyGameContent.Stages.Length)));
            _lobbyBg.sprite = bg;
            _lobbyBg.color = bg != null ? Color.white : new Color(1f, 1f, 1f, 0f);

            var sprite = ArtLibrary.Portrait(scene.Speaker);
            var rt = _lobbyPortrait.rectTransform;
            if (sprite != null)
            {
                _lobbyPortrait.sprite = sprite;
                _lobbyPortrait.color = Color.white;
                _lobbyGlyph.text = "";
                rt.anchorMin = new Vector2(0.02f, 0.02f); rt.anchorMax = new Vector2(0.52f, 0.98f);
            }
            else
            {
                _lobbyPortrait.sprite = null;
                var tint = ArtLibrary.TintFor(scene.Speaker); tint.a = 0.85f;
                _lobbyPortrait.color = tint;
                _lobbyGlyph.text = scene.Speaker.Substring(0, 1);
                rt.anchorMin = new Vector2(0.06f, 0.10f); rt.anchorMax = new Vector2(0.46f, 0.78f);
            }
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            if (animate && isActiveAndEnabled) StartCoroutine(LobbySlideIn());
        }

        private System.Collections.IEnumerator LobbySlideIn()
        {
            var rt = _lobbyPortrait.rectTransform;
            for (float t = 0f; t < 0.22f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.22f;
                rt.anchoredPosition = new Vector2(-60f * (1f - k), 0f);
                var c = _lobbyPortrait.color; c.a = Mathf.Lerp(0.2f, c.a < 0.99f ? 0.85f : 1f, k); _lobbyPortrait.color = c;
                yield return null;
            }
            rt.anchoredPosition = Vector2.zero;
        }

        private static void SetBadge(Text badge, int count)
        {
            if (badge == null) return;
            badge.text = count.ToString();
            badge.transform.parent.gameObject.SetActive(count > 0);
        }

        private void OnFeaturedPressed()
        {
            if (_featStage != null) OnStagePressed(_featStage, _onStartRef);
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
            UiFactory.Frame(rt, UiFactory.GoldDim, 2f);
            return rt;
        }

        private void OpenPromotionOverlay()
        {
            var save = GameManager.Instance.Save;
            ClearChildren(_promoOverlay);
            _promoOverlay.gameObject.SetActive(true);

            int rank = Promotion.EffectiveRank(save);
            UiFactory.Label("Title", _promoOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "승급 시험장 · 현재 " + Promotion.RankName(rank), 44, UiFactory.Gold);
            UiFactory.Label("Rules", _promoOverlay, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.92f),
                "상대 둘을 혼자서 연달아 꺾는다. 한 판은 " + Promotion.TurnLimit + "턴(상위 단계일수록 조금 길다) 안에 끝내야 하며, 시간 초과는 무승부(상금 절반)다. 패배하면 입장료는 돌려받지 못한다.\n단계가 오를수록 상대는 훨씬 강해지지만, 검룡위 1당 모든 능력치 +3% (지금 +" + Math.Round(Promotion.StatBonusPerRank * 100f * rank) + "%).", 26, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);

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


        // ------------------------------------------------------------------ 설정

        private void OpenSettingsOverlay()
        {
            _confirmReset = false;
            BuildSettings();
        }

        private void BuildSettings()
        {
            var save = GameManager.Instance.Save;
            if (save.Settings == null) save.Settings = new GameSettings();
            var st = save.Settings;
            ClearChildren(_settingsOverlay);
            _settingsOverlay.gameObject.SetActive(true);
            UiFactory.Label("Title", _settingsOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "설정", 44, UiFactory.Gold);

            float y = 0.88f;
            VolumeRow("배경음", st.BgmVolume, ref y, d => { st.BgmVolume = GameSettings.StepVolume(st.BgmVolume, d); SoundManager.RefreshVolumes(); });
            VolumeRow("효과음", st.SfxVolume, ref y, d => { st.SfxVolume = GameSettings.StepVolume(st.SfxVolume, d); });
            ToggleRow("진동", st.Vibration ? "켜짐" : "꺼짐", ref y, () => { st.Vibration = !st.Vibration; if (st.Vibration) SoundManager.Haptic(60); });
            ToggleRow("전투 시작 속도", "x" + st.BattleSpeed, ref y, () => { st.BattleSpeed = st.NextSpeed(); });

            var help = UiFactory.Button("Help", _settingsOverlay, new Vector2(0.08f, y - 0.09f), new Vector2(0.92f, y), "도움말", UiFactory.Red, OpenHelpList, 34);
            help.interactable = true;
            y -= 0.14f;
            var reset = UiFactory.Button("Reset", _settingsOverlay, new Vector2(0.08f, y - 0.09f), new Vector2(0.92f, y),
                _confirmReset ? "정말 초기화할까요? 한 번 더 누르면 모든 진행이 사라집니다" : "저장 데이터 초기화", _confirmReset ? UiFactory.Red : UiFactory.InkSoft, OnResetPressed, 28);
            reset.interactable = true;

            UiFactory.Label("Info", _settingsOverlay, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.22f),
                "소리 파일(Resources/Audio)을 넣으면 효과음과 배경음이 자동으로 나온다. 파일이 없으면 조용히 넘어간다.", 22, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);
            UiFactory.Button("Close", _settingsOverlay, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.08f), "닫기", UiFactory.InkSoft, () => { GameManager.Instance.SaveNow(); _settingsOverlay.gameObject.SetActive(false); Refresh(); }, 36);
        }

        private void OpenHelpList()
        {
            ClearChildren(_settingsOverlay);
            UiFactory.Label("Title", _settingsOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "도움말", 44, UiFactory.Gold);
            float y = 0.88f;
            foreach (var topic in Tutorial.All)
            {
                var id = topic.Id;
                UiFactory.Button("Topic_" + id, _settingsOverlay, new Vector2(0.08f, y - 0.085f), new Vector2(0.92f, y), topic.Title, UiFactory.InkSoft, () => HelpPanel.Instance.Show(id), 34);
                y -= 0.10f;
            }
            UiFactory.Button("Back", _settingsOverlay, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.08f), "돌아가기", UiFactory.InkSoft, BuildSettings, 36);
        }

        private void VolumeRow(string label, float value, ref float y, Action<int> change)
        {
            var band = UiFactory.Panel("Row", _settingsOverlay, new Vector2(0.05f, y - 0.09f), new Vector2(0.95f, y), UiFactory.Ink);
            UiFactory.Frame(band.transform, UiFactory.GoldDim, 2f);
            UiFactory.Label("Name", band.transform, new Vector2(0.04f, 0.1f), new Vector2(0.40f, 0.9f), label, 34, UiFactory.Gold, TextAnchor.MiddleLeft);
            UiFactory.Button("Minus", band.transform, new Vector2(0.44f, 0.15f), new Vector2(0.58f, 0.85f), "-", UiFactory.InkSoft, () => { change(-1); BuildSettings(); }, 40);
            UiFactory.Label("Value", band.transform, new Vector2(0.58f, 0.1f), new Vector2(0.80f, 0.9f), Mathf.RoundToInt(value * 100f) + "%", 34, UiFactory.PaperText);
            UiFactory.Button("Plus", band.transform, new Vector2(0.80f, 0.15f), new Vector2(0.94f, 0.85f), "+", UiFactory.InkSoft, () => { change(1); BuildSettings(); }, 40);
            y -= 0.105f;
        }

        private void ToggleRow(string label, string value, ref float y, Action toggle)
        {
            var band = UiFactory.Panel("Row", _settingsOverlay, new Vector2(0.05f, y - 0.09f), new Vector2(0.95f, y), UiFactory.Ink);
            UiFactory.Frame(band.transform, UiFactory.GoldDim, 2f);
            UiFactory.Label("Name", band.transform, new Vector2(0.04f, 0.1f), new Vector2(0.50f, 0.9f), label, 34, UiFactory.Gold, TextAnchor.MiddleLeft);
            UiFactory.Button("Toggle", band.transform, new Vector2(0.60f, 0.15f), new Vector2(0.94f, 0.85f), value, UiFactory.Red, () => { toggle(); BuildSettings(); }, 34);
            y -= 0.105f;
        }

        private void OnResetPressed()
        {
            if (!_confirmReset) { _confirmReset = true; BuildSettings(); return; }
            _confirmReset = false;
            GameManager.Instance.ResetSave();
            _settingsOverlay.gameObject.SetActive(false);
            Refresh();
        }

        // ------------------------------------------------------------------ 의뢰 / 기연

        private void OpenQuestOverlay()
        {
            var save = GameManager.Instance.Save;
            ClearChildren(_questOverlay);
            _questOverlay.gameObject.SetActive(true);
            if (!Tutorial.IsDone(save, "board") && HelpPanel.Instance != null) HelpPanel.Instance.Show("board", true);
            UiFactory.Label("Title", _questOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "의뢰 게시판", 44, UiFactory.Gold);
            UiFactory.Label("Rules", _questOverlay, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.92f),
                "이미 지나온 싸움에 조건이 붙어 다시 나온다. 한 번씩만 받을 수 있고, 두와 보상을 얻는다.", 26, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);

            var questList = CodexPanel.Scroller(_questOverlay, 0.10f, 0.84f);
            int shown = 0;
            foreach (var q in Quests.All)
            {
                if (!Quests.IsUnlocked(save, q)) continue;
                bool done = Quests.IsDone(save, q.Id);
                var band = CodexPanel.Band(questList, shown);
                UiFactory.Label("Name", band, new Vector2(0.03f, 0.54f), new Vector2(0.66f, 0.97f), q.Name + (done ? "  (완료)" : ""), 32, done ? UiFactory.InkSoft : UiFactory.PaperText, TextAnchor.MiddleLeft);
                UiFactory.Label("Info", band, new Vector2(0.03f, 0.03f), new Vector2(0.66f, 0.54f), q.Trial.Brief + " · 보상 " + q.RewardDoo.ToString("N0") + "두", 22, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                var quest = q;
                var go = UiFactory.Button("Go", band, new Vector2(0.69f, 0.12f), new Vector2(0.97f, 0.88f), done ? "완료" : "수락", done ? UiFactory.InkSoft : UiFactory.Red, () => AcceptQuest(quest), 32);
                go.interactable = !done;
                shown++;
            }
            questList.sizeDelta = new Vector2(0f, CodexPanel.RowGap + shown * (CodexPanel.RowH + CodexPanel.RowGap));
            if (shown == 0)
                UiFactory.Label("None", _questOverlay, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.7f), "아직 받을 수 있는 의뢰가 없다. 스테이지를 돌파하면 의뢰가 붙는다.", 34, UiFactory.PaperText);

            UiFactory.Button("Close", _questOverlay, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.08f), "닫기", UiFactory.InkSoft, () => { _questOverlay.gameObject.SetActive(false); Refresh(); }, 36);
        }

        // ------------------------------------------------------------------ 무한 비무대

        private void OpenTowerOverlay()
        {
            var save = GameManager.Instance.Save;
            ClearChildren(_towerOverlay);
            _towerOverlay.gameObject.SetActive(true);
            UiFactory.Label("Title", _towerOverlay, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "무한 비무대", 44, UiFactory.Gold);
            if (!Tower.Unlocked(save))
            {
                UiFactory.Label("Locked", _towerOverlay, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.75f),
                    "스테이지 " + Tower.UnlockStage + "를 돌파하면 열린다.", 34, UiFactory.PaperText);
            }
            else
            {
                int floor = Tower.NextFloor(save);
                bool boss = Tower.IsBossFloor(floor);
                UiFactory.Label("Best", _towerOverlay, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.92f),
                    "최고 기록 " + save.TowerBest + "층", 38, UiFactory.PaperText);
                var card = UiFactory.Panel("Card", _towerOverlay, new Vector2(0.05f, 0.48f), new Vector2(0.95f, 0.82f), UiFactory.Ink);
                UiFactory.Frame(card.transform, boss ? UiFactory.Red : UiFactory.Gold, 3f);
                UiFactory.Label("Floor", card.transform, new Vector2(0.05f, 0.62f), new Vector2(0.95f, 0.96f), floor + "층" + (boss ? "  (우두머리)" : ""), 56, boss ? UiFactory.Red : UiFactory.Gold);
                var enemies = Tower.BuildEnemies(floor);
                var names = new List<string>();
                foreach (var e in enemies) names.Add(e.Name);
                var items = new List<string>();
                foreach (var s in Tower.RewardItems(floor)) { var it = EarlyGameContent.FindItem(s.Id); if (it != null) items.Add(it.Name); }
                UiFactory.Label("Info", card.transform, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.62f),
                    "상대: " + string.Join(", ", names) + "\n권장 Lv." + Tower.FloorLevel(floor)
                    + "\n첫 돌파 보상: 두 " + Tower.RewardDoo(floor).ToString("N0") + (items.Count > 0 ? ", " + string.Join(", ", items) : "")
                    + " + 경험", 28, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);
                UiFactory.Button("Go", _towerOverlay, new Vector2(0.25f, 0.34f), new Vector2(0.75f, 0.44f), "도전", UiFactory.Red, () => StartTower(floor), 44);
                if (save.TowerBest > 0)
                {
                    UiFactory.Label("Again", _towerOverlay, new Vector2(0.05f, 0.24f), new Vector2(0.95f, 0.30f), "이미 돌파한 층은 보상 없이 다시 겨룰 수 있다", 24, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);
                    UiFactory.Button("Prev", _towerOverlay, new Vector2(0.25f, 0.15f), new Vector2(0.75f, 0.23f), save.TowerBest + "층 다시 도전", UiFactory.InkSoft, () => StartTower(save.TowerBest), 30);
                }
            }
            UiFactory.Button("Close", _towerOverlay, new Vector2(0.3f, 0.02f), new Vector2(0.7f, 0.08f), "닫기", UiFactory.InkSoft, () => { _towerOverlay.gameObject.SetActive(false); Refresh(); }, 36);
            if (!Tutorial.IsDone(save, "tower") && HelpPanel.Instance != null && Tower.Unlocked(save)) HelpPanel.Instance.Show("tower", true);
        }

        private void StartTower(int floor)
        {
            _towerOverlay.gameObject.SetActive(false);
            _onStart(Tower.ToStage(GameManager.Instance.Save, floor));
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
            var fortuneList = CodexPanel.Scroller(_fortuneOverlay, 0.10f, 0.90f);
            for (int i = 0; i < open.Count; i++)
            {
                var e = open[i];
                var band = CodexPanel.Band(fortuneList, i);
                UiFactory.Label("Name", band, new Vector2(0.03f, 0.10f), new Vector2(0.64f, 0.90f), e.Title, 34, UiFactory.PaperText, TextAnchor.MiddleLeft);
                UiFactory.Button("Go", band, new Vector2(0.68f, 0.12f), new Vector2(0.97f, 0.88f), "마주치다", UiFactory.Red, () => ShowFortune(e), 30);
            }
            fortuneList.sizeDelta = new Vector2(0f, CodexPanel.RowGap + open.Count * (CodexPanel.RowH + CodexPanel.RowGap));
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
            UiFactory.Frame(rt, UiFactory.GoldDim, 3f);   // panel border, re-added after every rebuild
        }

        /// <summary>Companions available now: everyone who has joined by the furthest stage reached.</summary>
        private static List<CompanionData> AvailableCompanions(SaveData save)
        {
            return EarlyGameContent.CompanionsFor(Math.Max(1, save.HighestUnlockedStage));
        }

        private void OpenPartyOverlay()
        {
            var save = GameManager.Instance.Save;
            ClearChildren(_partyOverlay);
            _partyOverlay.gameObject.SetActive(true);

            var available = AvailableCompanions(save);
            var active = EarlyGameContent.SelectCompanionsFor(Math.Max(1, save.HighestUnlockedStage), save);
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
                UiFactory.Label("Role", band.transform, new Vector2(0.03f, 0.03f), new Vector2(0.64f, 0.52f), c.Role + "  ·  인연 " + Bonds.LevelNames[Bonds.Level(save, c.Id)] + " (" + Bonds.BonusText(Bonds.Level(save, c.Id)) + ")", 20, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
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
            foreach (var x in EarlyGameContent.SelectCompanionsFor(stage, save)) picked.Add(x.Id);
            if (picked.Contains(c.Id))
            {
                picked.Remove(c.Id);                                   // 출전 → 대기
                if (!save.BenchedCompanions.Contains(c.Id)) save.BenchedCompanions.Add(c.Id);
            }
            else if (picked.Count < EarlyGameContent.MaxCompanions)
            {
                save.BenchedCompanions.Remove(c.Id);                   // 대기 → 출전
                picked.Add(c.Id);
            }
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

        private void OnDisable() { if (_built) CloseOverlays(); }

        /// <summary>Hides every pop-up panel of the 강호 tab (used when a fight starts or ends and when the tab is left).</summary>
        public void CloseOverlays()
        {
            foreach (var o in new[] { _promoOverlay, _partyOverlay, _questOverlay, _fortuneOverlay, _settingsOverlay, _towerOverlay })
                if (o != null) o.gameObject.SetActive(false);
            if (_stars != null) _stars.Hide();
            if (_daily != null) _daily.Hide();
        }

        private void OnStagePressed(StageData st, Action<StageData> onStart)
        {
            var save = GameManager.Instance.Save;
            if (save.HighestUnlockedStage < st.Number || !Promotion.StageAllowed(save, st.Number)) return;
            int diff = StageProgress.Has(save, st.Number) ? save.ProgressDifficulty : Mathf.Clamp(save.Difficulty, 0, StageStars.DifficultyCount - 1);
            if (!StageStars.Unlocked(save, st.Number, diff)) return;
            onStart(st.WithDifficulty(diff));
        }

        private static string PartyText(StageData st)
        {
            var companions = EarlyGameContent.SelectCompanionsFor(st.Number, GameManager.Instance.Save);
            if (companions.Count == 0) return "";
            var names = new List<string>();
            foreach (var c in companions) names.Add(c.Name);
            return " · 동료 " + string.Join(", ", names);
        }

        public void Refresh()
        {
            if (!_built) return;
            var save = GameManager.Instance.Save;
            ShowLobby(false);
            int rankNow = Promotion.EffectiveRank(save);
            _lvText.text = "Lv." + save.Level;
            long need = Progression.ExpToNext(save.Level);
            _expBar.SetPct(need > 0 ? Mathf.Clamp01((float)save.Exp / need) : 0f);
            _dooText.text = save.Doo.ToString("N0");
            _rankText.text = Promotion.RankName(rankNow);

            // Featured card = the furthest stage the player can attempt.
            var stagesAll = EarlyGameContent.Stages;
            _featStage = EarlyGameContent.FindStage(Math.Min(save.HighestUnlockedStage, stagesAll[stagesAll.Length - 1].Number));
            if (_featStage != null)
            {
                bool featCleared = save.ClearedStage >= _featStage.Number;
                bool featOk = Promotion.StageAllowed(save, _featStage.Number);
                _featTitle.text = "스테이지 " + _featStage.Number + " · " + _featStage.Name;
                _featSub.text = "권장 Lv." + _featStage.RecommendedLevel + " · 전투 " + _featStage.Encounters.Length + "회" + PartyText(_featStage)
                    + (featOk ? "" : "\n" + Promotion.RankName(Promotion.GateRank) + " 이상이어야 참가할 수 있다");
                _featBtnLabel.text = !featOk ? "승급 필요" : StageProgress.Has(save, _featStage.Number) ? "이어하기" : featCleared ? "재도전" : "출전";
                _featBtn.interactable = featOk;
                var art = ArtLibrary.Background(_featStage.Number);
                _featArt.sprite = art;
                _featArt.color = art != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            }
            _promoRowDetail.text = Promotion.RankName(Promotion.EffectiveRank(save)) + (Promotion.EffectiveRank(save) >= Promotion.MaxRank ? " · 최고 경지" : " · 한 계단 또는 두 계단 승급 도전")
                + (Promotion.EffectiveRank(save) < Promotion.GateRank ? "\n(" + Promotion.RankName(Promotion.GateRank) + " 이상이어야 삼성무제 참가)" : "");
            int openQuests = Quests.Open(save).Count, openFortunes = Fortunes.Open(save).Count;
            _questRowDetail.text = openQuests > 0 ? "받을 수 있는 의뢰 " + openQuests + "건" : "새 의뢰 없음 (스테이지를 돌파하면 늘어난다)";
            if (!ReferenceEquals(_syncedSave, save))
            {
                // Backfill records for saves from before these systems existed (once per loaded save).
                _syncedSave = save;
                Codex.Sync(save);
                StageStars.Sync(save);
                Factions.SyncFromStars(save);
            }
            var dailyList = Daily.Missions(save);
            int dailyDone = 0;
            for (int di = 0; di < dailyList.Count; di++) if (Daily.IsClaimed(save, di)) dailyDone++;
            _dailyRowDetail.text = "오늘 " + dailyDone + "/3 완료 · 연속 수련 " + save.DailyStreak + "일";
            SetBadge(_dailyBadge, Daily.ClaimableCount(save));
            int starChests = StageStars.ClaimableCount(save);
            _starRowDetail.text = "별 " + StageStars.Total(save) + " / " + StageStars.MaxStars + " · 받을 수 있는 보상 " + starChests + "개";
            SetBadge(_starBadge, starChests);
            _starsTitle.text = "— 스테이지 —   ★ " + StageStars.Total(save);
            int pickedDiff = Mathf.Clamp(save.Difficulty, 0, StageStars.DifficultyCount - 1);
            for (int d = 0; d < _diffTabs.Length; d++) _diffTabs[d].color = d == pickedDiff ? (d == 0 ? new Color(0.30f, 0.48f, 0.44f, 1f) : d == 1 ? new Color(0.70f, 0.45f, 0.12f, 1f) : UiFactory.Red) : UiFactory.InkSoft;
            _towerRowDetail.text = Tower.Unlocked(save) ? "최고 " + save.TowerBest + "층 · 다음 " + Tower.NextFloor(save) + "층" : "스테이지 " + Tower.UnlockStage + " 돌파 시 열린다";
            _fortuneRowDetail.text = openFortunes > 0 ? "마주칠 기연 " + openFortunes + "건" : "새 기연 없음";
            SetBadge(_questBadge, openQuests);
            SetBadge(_fortuneBadge, openFortunes);
            SetBadge(_promoBadge, 0);
            SetBadge(_partyBadge, 0);
            var current = new List<string>();
            foreach (var c in EarlyGameContent.SelectCompanionsFor(Math.Max(1, save.HighestUnlockedStage), save)) current.Add(c.Name + "(인연 " + Bonds.LevelNames[Bonds.Level(save, c.Id)] + ")");
            _partyRowDetail.text = current.Count == 0 ? "아직 함께할 동료가 없다" : "출전 동료: " + string.Join(", ", current)
                + (AvailableCompanions(save).Count > EarlyGameContent.MaxCompanions ? "\n(동료 " + AvailableCompanions(save).Count + "명 중 " + EarlyGameContent.MaxCompanions + "명만 출전)" : "");

            foreach (var r in _rows)
            {
                var st = r.Stage;
                bool reached = save.HighestUnlockedStage >= st.Number;
                bool rankOk = Promotion.StageAllowed(save, st.Number);
                bool unlocked = reached && rankOk;
                bool cleared = save.ClearedStage >= st.Number;
                int diff = StageProgress.Has(save, st.Number) ? save.ProgressDifficulty : Mathf.Clamp(save.Difficulty, 0, StageStars.DifficultyCount - 1);
                bool diffOpen = StageStars.Unlocked(save, st.Number, diff);
                bool clearedHere = StageStars.StageCleared(save, st.Number, diff) || (diff == 0 && cleared);
                int stageStarCount = StageStars.StarsOfStage(save, st.Number, diff);
                string starText = stageStarCount > 0 ? "  ★" + stageStarCount + "/" + StageStars.MaxStarsOfStage(st.Number) : "";
                r.Title.text = "스테이지 " + st.Number + " · " + st.Name + starText;
                r.Detail.text = reached && !rankOk ? Promotion.RankName(Promotion.GateRank) + " 이상이 되어야 참가할 수 있다 (승급 시험장)" : unlocked ? "권장 Lv." + st.RecommendedLevel + " · 전투 " + st.Encounters.Length + "회" + PartyText(st) + (cleared ? " · 돌파" : "") : "이전 스테이지를 돌파하면 열린다";
                bool playable = unlocked && diffOpen;
                r.ButtonLabel.text = !unlocked ? (reached ? "승급 필요" : "잠김") : !diffOpen ? StageStars.DifficultyNames[diff - 1] + "\n돌파 필요" : StageProgress.Has(save, st.Number) ? "이어하기" : clearedHere ? "재도전" : "출전";
                r.ButtonImage.color = !playable ? UiFactory.InkSoft : clearedHere ? new Color(0.35f, 0.35f, 0.32f, 1f) : UiFactory.Red;
                r.Button.interactable = playable;
            }
        }
    }
}
