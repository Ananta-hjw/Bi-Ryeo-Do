using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>기록 tab: the non-combat records and menus — 상태창, 도감, 업적, 나예린 추억, 세력과 객잔. Each tile opens a pop-up panel inside this tab.</summary>
    public class RecordScreen : MonoBehaviour
    {
        private sealed class Entry
        {
            public Text Detail, Badge;
        }

        private Action _openStatus;
        private CodexPanel _codex;
        private MemoryPanel _memories;
        private StoryLogPanel _storyLog;
        private FactionPanel _factions;
        private Entry _statusE, _codexE, _achE, _storyE, _memoryE, _factionE, _ritualE;
        private RitualPanel _ritual;
        private bool _built;

        public void Build(RectTransform root, Action<string, string[], Action> onStory, Action openStatus = null)
        {
            if (_built) return;
            _built = true;
            _openStatus = openStatus;

            UiFactory.Frame(UiFactory.Panel("HeaderBand", root, new Vector2(0.04f, 0.925f), new Vector2(0.96f, 0.985f), UiFactory.Ink).transform, UiFactory.GoldDim, 2f);
            UiFactory.Glyph("record", root, new Vector2(.065f, .936f), new Vector2(.13f, .977f), UiFactory.Gold);
            UiFactory.Label("Header", root, new Vector2(0.16f, 0.925f), new Vector2(0.94f, 0.985f), "기록", 42, UiFactory.Gold);

            var workspace = UiFactory.Rect("Workspace", root, new Vector2(.27f, .025f), new Vector2(.98f, .905f));
            UiFactory.Panel("Background", workspace, Vector2.zero, Vector2.one, UiFactory.Ink);
            UiFactory.Frame(workspace, UiFactory.GoldDim, 2f);
            UiFactory.ScrollBody(UiFactory.Label("Hint", workspace, new Vector2(.08f, .30f), new Vector2(.92f, .70f), "왼쪽에서 기록을 선택해 주세요.\n상태와 장비 관리는 협객 화면에서 이어집니다.", 32, UiFactory.Dim), 32);
            _statusE = Tile(root, 0, "협객 · 상태와 장비", "party", () => _openStatus?.Invoke());
            _codexE = Tile(root, 1, "도감", "record", () => { CloseOverlays(); _codex.OpenCodex(); });
            _achE = Tile(root, 2, "업적", "star", () => { CloseOverlays(); _codex.OpenAchievements(); });
            _storyE = Tile(root, 3, "이야기 줄거리", "quest", () => { CloseOverlays(); _storyLog.Open(); });
            _memoryE = Tile(root, 4, "나예린 추억", "fortune", () => { CloseOverlays(); _memories.Open(); });
            _factionE = Tile(root, 5, "세력과 객잔", "tower", () => { CloseOverlays(); _factions.Open(); });
            _ritualE = Tile(root, 6, "전승의식", "arts", () => { CloseOverlays(); _ritual.Open(); });
            _codex = new CodexPanel(UiFactory.Rect("CodexOverlay", workspace, Vector2.zero, Vector2.one), UiFactory.Rect("AchievementOverlay", workspace, Vector2.zero, Vector2.one), Refresh);
            _storyLog = new StoryLogPanel(UiFactory.Rect("StoryLogOverlay", workspace, Vector2.zero, Vector2.one), Refresh, onStory);
            _memories = new MemoryPanel(UiFactory.Rect("MemoryOverlay", workspace, Vector2.zero, Vector2.one), Refresh, onStory);
            _factions = new FactionPanel(UiFactory.Rect("FactionOverlay", workspace, Vector2.zero, Vector2.one), Refresh);
            _ritual = new RitualPanel(UiFactory.Rect("RitualOverlay", workspace, Vector2.zero, Vector2.one), Refresh, onStory);
            Refresh();
        }

        private static Entry Tile(RectTransform root, int index, string title, string icon, UnityEngine.Events.UnityAction onClick)
        {
            float top = .905f - index * .124f;
            var rt = UiFactory.Panel("Menu_" + icon, root, new Vector2(.02f, top - .112f), new Vector2(.25f, top), UiFactory.Ink).rectTransform;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = rt.GetComponent<Image>();
            button.onClick.AddListener(onClick);
            UiFactory.Frame(rt, UiFactory.GoldDim, 2f);
            UiFactory.Glyph(icon, rt, new Vector2(.035f, .60f), new Vector2(.14f, .95f), UiFactory.Gold);
            UiFactory.Label("Title", rt, new Vector2(.17f, .55f), new Vector2(.96f, .97f), title, 28, UiFactory.PaperText, TextAnchor.MiddleLeft);
            var e = new Entry();
            e.Detail = UiFactory.ScrollBody(UiFactory.Label("Detail", rt, new Vector2(.025f, .04f), new Vector2(.97f, .52f), "", 23, UiFactory.Dim, TextAnchor.UpperLeft, FontStyle.Normal), 23);
            var dot = UiFactory.Panel("Dot", rt, new Vector2(.84f, .72f), new Vector2(.98f, .98f), UiFactory.Red);
            e.Badge = UiFactory.Label("Count", dot.transform, Vector2.zero, Vector2.one, "", 22, UiFactory.PaperText);
            return e;
        }

        private static void SetBadge(Entry e, int count)
        {
            e.Badge.text = count.ToString();
            e.Badge.transform.parent.gameObject.SetActive(count > 0);
        }

        private void OnEnable() { if (_built) Refresh(); }

        private void OnDisable() { if (_built) CloseOverlays(); }

        /// <summary>Opens the 상태창 pop-up (used by the Lv chip of the 강호 tab).</summary>
        public void OpenStatus() { if (_built) _openStatus?.Invoke(); }

        public void CloseOverlays()
        {
            _storyLog.Hide();
            _codex.HideAll();
            _memories.Hide();
            _factions.Hide();
            _ritual.Hide();
        }

        public void Refresh()
        {
            if (!_built) return;
            var save = GameManager.Instance.Save;
            var hero = StatusSheet.Hero(save);
            _statusE.Detail.text = "경지 " + Realm.Steps[Realm.StepOf(save)].Name + " · " + hero[0].Label + " " + hero[0].Value;
            SetBadge(_statusE, 0);
            _codexE.Detail.text = "적 " + save.SeenEnemies.Count + "/" + Codex.EnemyTotal + " · 물품 " + save.SeenItems.Count + "/" + Codex.ItemTotal;
            SetBadge(_codexE, 0);
            _achE.Detail.text = "달성 " + Achievements.ClaimedCount(save) + "/" + Achievements.All.Length;
            SetBadge(_achE, Achievements.ClaimableCount(save));
            _storyE.Detail.text = "등장인물과 사건의 사정을 다시 읽습니다 · " + StoryBriefings.Readable(save).Count + "편";
            SetBadge(_storyE, 0);
            int unseen = Memories.UnseenCount(save);
            _memoryE.Detail.text = "본 추억 " + Memories.SeenCount(save) + " / " + Memories.All.Length + (unseen > 0 ? " · 새 추억 " + unseen + "개" : "");
            SetBadge(_memoryE, unseen);
            var notes = new List<string>();
            foreach (var fa in Factions.All) if (Factions.Tier(save, fa.Id) > 0) notes.Add(fa.Name + " " + Factions.TierNames[Factions.Tier(save, fa.Id)]);
            _factionE.Detail.text = notes.Count > 0 ? "평판: " + string.Join(" · ", notes) : "전투를 이기면 세력 평판이 쌓이고 객잔 선반이 열립니다";
            SetBadge(_factionE, 0);
            int ritualDone = Ritual.DoneCount(save), ritualTotal = Ritual.Conditions(save).Count;
            _ritualE.Detail.text = save.RitualDone ? "완료 — 진비뢰도를 얻었습니다" : "초월 등급 진비뢰도의 시련 · 조건 " + ritualDone + "/" + ritualTotal;
            SetBadge(_ritualE, Ritual.CanComplete(save) ? 1 : 0);
        }
    }
}
