using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>기록 tab: the non-combat records and menus — 도감, 업적, 이야기, 나예린 추억, 세력 평판. Each tile opens a pop-up panel inside this tab.</summary>
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
        private Entry _codexE, _achE, _storyE, _memoryE, _factionE, _ritualE;
        private RitualPanel _ritual;
        private ReadingList _menuLayout;
        private bool _built;

        public void Build(RectTransform root, Action<string, string[], Action> onStory, Action openStatus = null)
        {
            if (_built) return;
            _built = true;
            _openStatus = openStatus;

            UiFactory.TabHeader(root, "record", "기록", .97f);

            var workspace = UiFactory.Rect("Workspace", root, new Vector2(.27f, .025f), new Vector2(.98f, .855f));
            UiFactory.Panel("Background", workspace, Vector2.zero, Vector2.one, UiFactory.Ink);
            UiFactory.Frame(workspace, UiFactory.GoldDim, 2f);
            UiFactory.ScrollBody(UiFactory.Label("Hint", workspace, new Vector2(.08f, .30f), new Vector2(.92f, .70f), "왼쪽에서 기록을 선택해 주세요.\n상태는 협객, 장비 관리는 장비 탭에서 이어집니다.", 32, UiFactory.Dim), 32);
            var menu = UiFactory.Rect("MenuViewport", root, new Vector2(.02f, .025f), new Vector2(.25f, .855f));
            var content = CodexPanel.Scroller(menu, 0f, 1f);
            _codexE = Tile(content, 0, "도감", "codex", () => { CloseOverlays(); _codex.OpenCodex(); });
            _achE = Tile(content, 1, "업적", "achievement", () => { CloseOverlays(); _codex.OpenAchievements(); });
            _storyE = Tile(content, 2, "이야기 줄거리", "story", () => { CloseOverlays(); _storyLog.Open(); });
            _memoryE = Tile(content, 3, "나예린 추억", "memories", () => { CloseOverlays(); _memories.Open(); });
            _factionE = Tile(content, 4, "세력 평판", "faction", () => { CloseOverlays(); _factions.Open(); });
            _ritualE = Tile(content, 5, "전승의식", "ritual", () => { CloseOverlays(); _ritual.Open(); });
            _menuLayout = content.GetComponent<ReadingList>();
            _codex = new CodexPanel(UiFactory.Rect("CodexOverlay", workspace, Vector2.zero, Vector2.one), UiFactory.Rect("AchievementOverlay", workspace, Vector2.zero, Vector2.one), Refresh);
            _storyLog = new StoryLogPanel(UiFactory.Rect("StoryLogOverlay", workspace, Vector2.zero, Vector2.one), Refresh, onStory);
            _memories = new MemoryPanel(UiFactory.Rect("MemoryOverlay", workspace, Vector2.zero, Vector2.one), Refresh, onStory);
            _factions = new FactionPanel(UiFactory.Rect("FactionOverlay", workspace, Vector2.zero, Vector2.one), Refresh);
            _ritual = new RitualPanel(UiFactory.Rect("RitualOverlay", workspace, Vector2.zero, Vector2.one), Refresh, onStory);
            Refresh();
        }

        private static Entry Tile(RectTransform root, int index, string title, string icon, UnityEngine.Events.UnityAction onClick)
        {
            const float height = 180f, gap = 16f;
            var rt = UiFactory.Panel("Menu_" + icon, root, new Vector2(.02f, 1f), new Vector2(.96f, 1f), UiFactory.Ink).rectTransform;
            rt.pivot = new Vector2(.5f, 1f);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = new Vector2(0f, -gap - index * (height + gap));
            ReadingList.Register(rt, height, gap);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = rt.GetComponent<Image>();
            button.targetGraphic.raycastTarget = true;
            button.onClick.AddListener(onClick);
            UiFactory.Frame(rt, UiFactory.GoldDim, 2f);
            var glyph = UiFactory.Glyph(icon, rt, new Vector2(.04f, .76f), new Vector2(.04f, .76f), UiFactory.Gold);
            glyph.rectTransform.pivot = new Vector2(0f, .5f);
            glyph.rectTransform.sizeDelta = new Vector2(48f, 48f);
            UiFactory.Label("Title", rt, new Vector2(.23f, .57f), new Vector2(.96f, .94f), title, 32, UiFactory.PaperText, TextAnchor.MiddleLeft);
            var e = new Entry();
            e.Detail = UiFactory.Label("Detail", rt, new Vector2(.06f, .08f), new Vector2(.94f, .53f), "", 26, UiFactory.Dim, TextAnchor.UpperLeft, FontStyle.Normal);
            e.Detail.resizeTextForBestFit = false;
            e.Detail.lineSpacing = 1.2f;
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
            _codexE.Detail.text = "적 " + save.SeenEnemies.Count + "/" + Codex.EnemyTotal + " · 물품 " + save.SeenItems.Count + "/" + Codex.ItemTotal;
            SetBadge(_codexE, RecordUpdates.Unread(save, RecordUpdates.CodexPage));
            _achE.Detail.text = "달성 " + Achievements.ClaimedCount(save) + "/" + Achievements.All.Length;
            SetBadge(_achE, Achievements.ClaimableCount(save));
            _storyE.Detail.text = "등장인물과 사건의 사정을 다시 읽습니다 · " + StoryBriefings.Readable(save).Count + "편";
            SetBadge(_storyE, RecordUpdates.Unread(save, RecordUpdates.StoryPage));
            int unseen = Memories.UnseenCount(save);
            _memoryE.Detail.text = "본 추억 " + Memories.SeenCount(save) + " / " + Memories.All.Length + (unseen > 0 ? " · 새 추억 " + unseen + "개" : "");
            SetBadge(_memoryE, unseen);
            var notes = new List<string>();
            foreach (var fa in Factions.All) if (Factions.Tier(save, fa.Id) > 0) notes.Add(fa.Name + " " + Factions.TierNames[Factions.Tier(save, fa.Id)]);
            _factionE.Detail.text = notes.Count > 0 ? "평판: " + string.Join(" · ", notes) : "전투를 이기면 세력 평판이 쌓이고, 강호 탭 객잔의 선반이 열립니다";
            SetBadge(_factionE, RecordUpdates.Unread(save, RecordUpdates.FactionPage));
            int ritualDone = Ritual.DoneCount(save), ritualTotal = Ritual.Conditions(save).Count;
            _ritualE.Detail.text = save.RitualDone ? "완료 — 진비뢰도를 얻었습니다" : "초월 등급 진비뢰도의 시련 · 조건 " + ritualDone + "/" + ritualTotal;
            SetBadge(_ritualE, Ritual.CanComplete(save) ? 1 : 0);
            _menuLayout.Reflow(true);
        }
    }
}
