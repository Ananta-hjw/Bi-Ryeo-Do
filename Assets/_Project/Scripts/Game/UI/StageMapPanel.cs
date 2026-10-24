using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>A six-stop illustrated journey map with an inspector; geography is decorative, stage data stays authoritative.</summary>
    public sealed class StageMapPanel
    {
        public const int StagesPerPage = 6;
        private static readonly Vector2[] Stops =
        {
            new Vector2(.14f, .70f), new Vector2(.43f, .81f), new Vector2(.79f, .70f),
            new Vector2(.86f, .34f), new Vector2(.52f, .29f), new Vector2(.18f, .34f)
        };
        private static readonly Color Road = new Color(.62f, .28f, .16f, .94f);
        private static readonly Color Closed = new Color(.32f, .36f, .32f, .85f);
        private readonly RectTransform _nodes;
        private readonly Action<StageData> _onStage;
        private readonly Action _onChange;
        private readonly StageMapRoute _route;
        private readonly Text _pageTitle, _pageNumber, _totalStars, _stageTitle, _stageKind, _level, _fights, _stars, _reward, _condition;
        private readonly Button _previous, _next, _go;
        private readonly Text _goText;
        private readonly Image _preview;
        private readonly Image[] _difficulty = new Image[StageStars.DifficultyCount];
        private readonly List<Node> _visible = new List<Node>();
        private int _page = -1, _selected;

        private sealed class Node
        {
            public StageData Stage;
            public Image Seal, Plate;
            public RectTransform Halo;
            public Text Number, Name, Status;
        }

        public int Page { get { return _page; } }
        public int SelectedStage { get { return _selected; } }
        public int PageCount { get { return (EarlyGameContent.Stages.Length + StagesPerPage - 1) / StagesPerPage; } }

        public StageMapPanel(RectTransform root, Action<StageData> onStage, Action onChange, Action onStars, Action onInn)
        {
            _onStage = onStage; _onChange = onChange;
            var background = UiFactory.Panel("MapPainting", root, Vector2.zero, Vector2.one, Color.white);
            background.sprite = ArtLibrary.Environment("world_journey_map");
            if (background.sprite == null) background.color = new Color(.66f, .65f, .46f);
            BackgroundCover.Apply(background);
            UiFactory.Panel("MapWash", root, Vector2.zero, Vector2.one, new Color(.14f, .21f, .18f, .10f)).raycastTarget = false;
            var header = UiFactory.Panel("MapHeader", root, new Vector2(0, .89f), Vector2.one, UiFactory.Ink);
            UiFactory.Panel("Rule", header.transform, Vector2.zero, new Vector2(1, .015f), UiFactory.GoldDim);
            UiFactory.Heading(UiFactory.Label("Title", header.transform, new Vector2(.11f, .40f), new Vector2(.35f, .96f), "강호 여정도", 40, UiFactory.Gold, TextAnchor.MiddleLeft));
            _totalStars = Text("TotalStars", header.transform, new Vector2(.11f, .04f), new Vector2(.35f, .40f), "", 26, UiFactory.Dim);
            for (int i = 0; i < _difficulty.Length; i++)
            {
                int diff = i;
                float x = .38f + i * .105f;
                var button = UiFactory.Button("Difficulty" + i, header.transform, new Vector2(x, .15f), new Vector2(x + .098f, .85f), StageStars.DifficultyNames[i], UiFactory.InkSoft, () =>
                {
                    GameManager.Instance.Save.Difficulty = diff;
                    GameManager.Instance.SaveNow();
                    _onChange();
                }, 30);
                _difficulty[i] = button.GetComponent<Image>();
            }
            UiFactory.Button("StarRewards", header.transform, new Vector2(.72f, .15f), new Vector2(.85f, .85f), "별 보상", UiFactory.InkSoft, () => onStars(), 30);
            UiFactory.Button("Inn", header.transform, new Vector2(.87f, .15f), new Vector2(.98f, .85f), "객잔", UiFactory.InkSoft, () => onInn(), 30);

            var viewport = UiFactory.Rect("Viewport", root, new Vector2(.015f, .14f), new Vector2(.74f, .875f));
            viewport.gameObject.AddComponent<RectMask2D>();
            _route = UiFactory.Rect("Road", viewport, Vector2.zero, Vector2.one).gameObject.AddComponent<StageMapRoute>();
            _pageTitle = Text("PageTitle", viewport, new Vector2(.035f, .91f), new Vector2(.96f, .995f), "", 30, UiFactory.PaperText);
            _pageTitle.gameObject.AddComponent<Outline>().effectColor = new Color(.03f, .08f, .07f, .85f);
            _nodes = UiFactory.Rect("Nodes", viewport, Vector2.zero, Vector2.one);
            var footer = UiFactory.Panel("MapNavigation", root, new Vector2(.02f, .025f), new Vector2(.74f, .12f), UiFactory.Ink);
            UiFactory.Frame(footer.transform, UiFactory.GoldDim, 2);
            _previous = UiFactory.Button("Previous", footer.transform, new Vector2(.015f, .12f), new Vector2(.13f, .88f), "‹", UiFactory.InkSoft, () => ChangePage(-1), 44);
            _pageNumber = Text("PageNumber", footer.transform, new Vector2(.15f, .08f), new Vector2(.57f, .92f), "", 30, UiFactory.PaperText, TextAnchor.MiddleCenter);
            _next = UiFactory.Button("Next", footer.transform, new Vector2(.59f, .12f), new Vector2(.705f, .88f), "›", UiFactory.InkSoft, () => ChangePage(1), 44);
            UiFactory.Button("Locate", footer.transform, new Vector2(.735f, .12f), new Vector2(.985f, .88f), "현재 여정", UiFactory.Red, () => Open(GameManager.Instance.Save), 30);

            var detail = UiFactory.Panel("StageDetail", root, new Vector2(.755f, .025f), new Vector2(.985f, .875f), UiFactory.Ink);
            UiFactory.Frame(detail.transform, UiFactory.Gold, 2);
            _preview = UiFactory.Panel("Landscape", detail.transform, new Vector2(.025f, .74f), new Vector2(.975f, .985f), Color.white);
            BackgroundCover.Apply(_preview);
            var shade = UiFactory.Panel("LandscapeShade", _preview.transform, Vector2.zero, Vector2.one, new Color(.02f, .06f, .06f, .18f));
            shade.raycastTarget = false;
            _stageKind = Text("StageKind", detail.transform, new Vector2(.06f, .68f), new Vector2(.94f, .735f), "", 26, UiFactory.Gold);
            _stageTitle = Text("StageTitle", detail.transform, new Vector2(.06f, .56f), new Vector2(.94f, .68f), "", 32, UiFactory.PaperText);
            UiFactory.Heading(_stageTitle);
            UiFactory.Panel("Rule", detail.transform, new Vector2(.06f, .54f), new Vector2(.94f, .542f), UiFactory.GoldDim);
            _level = Text("Level", detail.transform, new Vector2(.06f, .475f), new Vector2(.51f, .535f), "", 28, UiFactory.Dim);
            _fights = Text("Fights", detail.transform, new Vector2(.53f, .475f), new Vector2(.94f, .535f), "", 28, UiFactory.Dim);
            _stars = Text("Stars", detail.transform, new Vector2(.06f, .41f), new Vector2(.94f, .475f), "", 30, UiFactory.Gold);
            _reward = Text("Reward", detail.transform, new Vector2(.06f, .235f), new Vector2(.94f, .405f), "", 28, UiFactory.PaperText);
            _condition = UiFactory.Body(UiFactory.ScrollBody(Text("Condition", detail.transform, new Vector2(.06f, .135f), new Vector2(.94f, .23f), "", 26, UiFactory.Dim), 26), 1.1f);
            _go = UiFactory.Button("Go", detail.transform, new Vector2(.06f, .025f), new Vector2(.94f, .12f), "출전 준비", UiFactory.Red, () =>
            {
                var stage = EarlyGameContent.FindStage(_selected);
                if (stage != null && Playable(GameManager.Instance.Save, stage)) _onStage(stage);
            }, 32);
            _goText = _go.GetComponentInChildren<Text>();
        }

        private static Text Text(string name, Transform parent, Vector2 min, Vector2 max, string value, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var text = UiFactory.Label(name, parent, min, max, value, size, color, align, FontStyle.Normal);
            text.resizeTextForBestFit = false;
            return text;
        }

        private static int Difficulty(SaveData save, StageData stage)
        {
            return Mathf.Clamp(StageProgress.Has(save, stage.Number) ? save.ProgressDifficulty : save.Difficulty, 0, StageStars.DifficultyCount - 1);
        }

        private static bool Playable(SaveData save, StageData stage)
        {
            return save.HighestUnlockedStage >= stage.Number && Promotion.StageAllowed(save, stage.Number)
                && StageStars.Unlocked(save, stage.Number, Difficulty(save, stage));
        }

        private static bool Cleared(SaveData save, StageData stage)
        {
            int diff = Difficulty(save, stage);
            return StageStars.StageCleared(save, stage.Number, diff) || (diff == 0 && save.ClearedStage >= stage.Number);
        }

        public void Open(SaveData save)
        {
            int target = save.HighestUnlockedStage;
            if (StageProgress.Has(save, save.ProgressStage)) target = save.ProgressStage;
            else
                foreach (var stage in EarlyGameContent.Stages)
                    if (Playable(save, stage) && !Cleared(save, stage)) { target = stage.Number; break; }
            Select(Mathf.Clamp(target, 1, EarlyGameContent.Stages.Length), save);
        }

        public void ChangePage(int direction)
        {
            int page = Mathf.Clamp(_page + direction, 0, PageCount - 1);
            if (page == _page) return;
            Select(EarlyGameContent.Stages[page * StagesPerPage].Number, GameManager.Instance.Save);
        }

        public void Select(int number, SaveData save)
        {
            int index = Array.FindIndex(EarlyGameContent.Stages, stage => stage.Number == number);
            if (index < 0) return;
            _selected = number;
            int page = index / StagesPerPage;
            if (page != _page) { _page = page; BuildNodes(); }
            Refresh(save);
        }

        private void BuildNodes()
        {
            LandscapeMenu.Clear(_nodes);
            _visible.Clear();
            int first = _page * StagesPerPage;
            for (int i = 0; i < StagesPerPage && first + i < EarlyGameContent.Stages.Length; i++)
            {
                var stage = EarlyGameContent.Stages[first + i];
                var point = Stops[i];
                var rect = UiFactory.Rect("Stage" + stage.Number, _nodes, new Vector2(point.x - .12f, point.y), new Vector2(point.x + .12f, point.y));
                rect.pivot = new Vector2(.5f, .84f);
                rect.sizeDelta = new Vector2(0, 220);
                var hit = rect.gameObject.AddComponent<Image>(); hit.color = Color.clear;
                var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = hit;
                button.onClick.AddListener(() => { Select(stage.Number, GameManager.Instance.Save); SoundManager.Sfx("ui_click"); });
                var halo = UiFactory.Glyph("icon_rim", rect, new Vector2(.5f, .84f), new Vector2(.5f, .84f), UiFactory.Gold);
                halo.rectTransform.sizeDelta = new Vector2(116, 116);
                var seal = UiFactory.Panel("Seal", rect, new Vector2(.5f, .84f), new Vector2(.5f, .84f), UiFactory.Ink);
                seal.rectTransform.sizeDelta = new Vector2(76, 76);
                seal.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                seal.raycastTarget = false;
                UiFactory.Frame(seal.transform, UiFactory.Gold, 3);
                var number = Text("Number", seal.transform, Vector2.zero, Vector2.one, stage.Number.ToString("00"), 36, UiFactory.PaperText, TextAnchor.MiddleCenter);
                number.rectTransform.localRotation = Quaternion.Euler(0, 0, -45);
                var plate = UiFactory.Panel("NamePlate", rect, new Vector2(.02f, .19f), new Vector2(.98f, .59f), UiFactory.Ink);
                plate.raycastTarget = false;
                UiFactory.Frame(plate.transform, UiFactory.GoldDim, 2);
                var name = Text("Name", plate.transform, new Vector2(.035f, .02f), new Vector2(.965f, .98f), stage.Name, 28, UiFactory.PaperText, TextAnchor.MiddleCenter);
                var status = Text("Status", rect, new Vector2(0, 0), new Vector2(1, .17f), "", 24, UiFactory.PaperText, TextAnchor.MiddleCenter);
                status.gameObject.AddComponent<Outline>().effectColor = new Color(.03f, .08f, .07f, 1);
                _visible.Add(new Node { Stage = stage, Seal = seal, Plate = plate, Halo = halo.rectTransform, Number = number, Name = name, Status = status });
            }
        }

        public void Refresh(SaveData save)
        {
            if (_page < 0) { Open(save); return; }
            _totalStars.text = "스테이지 · 모은 별 " + StageStars.Total(save);
            int picked = Mathf.Clamp(save.Difficulty, 0, StageStars.DifficultyCount - 1);
            for (int i = 0; i < _difficulty.Length; i++) _difficulty[i].color = i == picked ? UiFactory.Red : UiFactory.InkSoft;
            int first = _page * StagesPerPage, last = Math.Min(first + StagesPerPage, EarlyGameContent.Stages.Length);
            _pageTitle.text = "여정 " + (_page + 1).ToString("00") + "   /   스테이지 " + EarlyGameContent.Stages[first].Number.ToString("00") + " — " + EarlyGameContent.Stages[last - 1].Number.ToString("00");
            _pageNumber.text = (_page + 1).ToString("00") + "  /  " + PageCount.ToString("00") + "     ·     지역을 선택하세요";
            _previous.interactable = _page > 0;
            _next.interactable = _page + 1 < PageCount;
            var points = new Vector2[_visible.Count];
            var colors = new Color[Math.Max(0, points.Length - 1)];
            var open = new bool[colors.Length];
            for (int i = 0; i < _visible.Count; i++)
            {
                var node = _visible[i]; var stage = node.Stage;
                bool available = Playable(save, stage), cleared = Cleared(save, stage), selected = _selected == stage.Number;
                bool resume = StageProgress.Has(save, stage.Number);
                int stars = StageStars.StarsOfStage(save, stage.Number, Difficulty(save, stage));
                node.Halo.gameObject.SetActive(selected);
                node.Seal.color = !available && !cleared ? Closed : cleared ? new Color(.14f, .36f, .30f) : UiFactory.Red;
                node.Plate.color = selected ? new Color(.28f, .16f, .12f, .98f) : UiFactory.Ink;
                node.Number.color = selected ? UiFactory.Gold : UiFactory.PaperText;
                node.Name.color = !available && !cleared ? UiFactory.Dim : UiFactory.PaperText;
                node.Status.text = (resume ? "이어가기" : cleared ? "돌파" : available ? "출전 가능" : "잠김")
                    + (stars > 0 ? "  ·  ★ " + stars + "/" + StageStars.MaxStarsOfStage(stage.Number) : "");
                points[i] = Stops[i];
                if (i > 0) { colors[i - 1] = cleared ? new Color(.23f, .45f, .35f) : available ? Road : Closed; open[i - 1] = available || cleared; }
            }
            _route.SetRoute(points, colors, open);
            RefreshDetail(save);
        }

        private void RefreshDetail(SaveData save)
        {
            var stage = EarlyGameContent.FindStage(_selected);
            if (stage == null) return;
            int diff = Difficulty(save, stage);
            bool resume = StageProgress.Has(save, stage.Number), playable = Playable(save, stage);
            _preview.sprite = ArtLibrary.Background(stage.Number);
            _preview.color = _preview.sprite == null ? UiFactory.InkSoft : Color.white;
            _stageKind.text = "스테이지 " + stage.Number.ToString("00") + " · " + StageStars.DifficultyNames[diff] + (resume ? " · 저장됨" : Cleared(save, stage) ? " · 돌파" : "");
            _stageTitle.text = stage.Name;
            _level.text = "권장 Lv." + stage.RecommendedPlayerLevel;
            _fights.text = "전투 " + stage.Encounters.Length + "회";
            _stars.text = "★ " + StageStars.StarsOfStage(save, stage.Number, diff) + " / " + StageStars.MaxStarsOfStage(stage.Number);
            Recommend.StageReward(save, stage.WithDifficulty(diff), out long exp, out long doo);
            _reward.text = "예상 보상" + (Recommend.IsReplay(save, stage.WithDifficulty(diff)) ? " · 재도전" : "") + "\n경험 " + exp.ToString("N0") + "\n은자 " + doo.ToString("N0");
            string reason = save.HighestUnlockedStage < stage.Number ? "이전 스테이지를 돌파하면 열립니다."
                : !Promotion.StageAllowed(save, stage.Number) ? Promotion.RankName(Promotion.GateRank) + " 이상으로 승급해 주세요."
                : !StageStars.Unlocked(save, stage.Number, diff) ? StageStars.LockLabel(save, stage.Number, diff).Replace("\n", " ")
                : resume ? "전투 " + (save.ProgressNext + 1) + "부터 이어갑니다. 저장된 난이도로 출전합니다."
                : "출전 준비에서 적과 파티 배치를 확인할 수 있습니다.";
            _condition.text = reason;
            _go.interactable = playable;
            _go.GetComponent<Image>().color = playable ? UiFactory.Red : UiFactory.InkSoft;
            _goText.text = !playable ? "개방 조건 확인" : resume ? "이어하기" : Cleared(save, stage) ? "재도전 준비" : "출전 준비";
        }
    }
}
