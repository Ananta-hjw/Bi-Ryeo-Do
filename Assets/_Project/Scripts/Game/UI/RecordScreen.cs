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

        private StatusPanel _status;
        private CodexPanel _codex;
        private MemoryPanel _memories;
        private StoryLogPanel _storyLog;
        private FactionPanel _factions;
        private Entry _statusE, _codexE, _achE, _storyE, _memoryE, _factionE, _ritualE;
        private RitualPanel _ritual;
        private bool _built;

        public void Build(RectTransform root, Action<string, string[], Action> onStory)
        {
            if (_built) return;
            _built = true;

            UiFactory.Frame(UiFactory.Panel("HeaderBand", root, new Vector2(0.04f, 0.925f), new Vector2(0.96f, 0.985f), UiFactory.Ink).transform, UiFactory.GoldDim, 2f);
            UiFactory.Label("Header", root, new Vector2(0.06f, 0.925f), new Vector2(0.94f, 0.985f), "기록", 42, UiFactory.Gold);

            // Five tiles stacked in the space below the header
            const float top = 0.90f, h = 0.11f, gap = 0.01f;
            float y = top;
            _statusE = Tile(root, ref y, h, gap, "상태창", () => _status.Open());
            _codexE = Tile(root, ref y, h, gap, "도감", () => _codex.OpenCodex());
            _achE = Tile(root, ref y, h, gap, "업적", () => _codex.OpenAchievements());
            _storyE = Tile(root, ref y, h, gap, "이야기 줄거리", () => _storyLog.Open());
            _memoryE = Tile(root, ref y, h, gap, "나예린 추억", () => _memories.Open());
            _factionE = Tile(root, ref y, h, gap, "세력과 객잔", () => _factions.Open());
            _ritualE = Tile(root, ref y, h, gap, "전승의식", () => _ritual.Open());

            var panelH = new Vector2(1f, 1f);
            _status = new StatusPanel(UiFactory.Rect("StatusOverlay", root, Vector2.zero, panelH), Refresh);
            _codex = new CodexPanel(UiFactory.Rect("CodexOverlay", root, Vector2.zero, panelH), UiFactory.Rect("AchievementOverlay", root, Vector2.zero, panelH), Refresh);
            _storyLog = new StoryLogPanel(UiFactory.Rect("StoryLogOverlay", root, Vector2.zero, panelH), Refresh, onStory);
            _memories = new MemoryPanel(UiFactory.Rect("MemoryOverlay", root, Vector2.zero, panelH), Refresh, onStory);
            _factions = new FactionPanel(UiFactory.Rect("FactionOverlay", root, Vector2.zero, panelH), Refresh);
            _ritual = new RitualPanel(UiFactory.Rect("RitualOverlay", root, Vector2.zero, panelH), Refresh, onStory);
            Refresh();
        }

        private static Entry Tile(RectTransform root, ref float y, float h, float gap, string title, UnityEngine.Events.UnityAction onClick)
        {
            var rt = UiFactory.Rect("Tile", root, new Vector2(0.04f, y - h), new Vector2(0.96f, y));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UiFactory.Gradient;
            img.color = new Color(0.16f, 0.19f, 0.27f, 1f);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            UiFactory.Frame(rt, UiFactory.GoldDim, 2f);
            UiFactory.Label("Title", rt, new Vector2(0.06f, 0.58f), new Vector2(0.80f, 0.94f), title, 40, UiFactory.Gold, TextAnchor.MiddleLeft);
            var e = new Entry();
            e.Detail = UiFactory.Label("Detail", rt, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.58f), "", 24, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal);
            var dot = UiFactory.Panel("Dot", rt, new Vector2(0.84f, 0.62f), new Vector2(0.95f, 0.92f), UiFactory.Red);
            e.Badge = UiFactory.Label("Count", dot.transform, Vector2.zero, Vector2.one, "", 28, UiFactory.PaperText);
            y -= h + gap;
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
        public void OpenStatus() { if (_built) _status.Open(); }

        public void CloseOverlays()
        {
            _status.Hide();
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
            _statusE.Detail.text = "능력치와 장비를 확인합니다 · 경지 " + Realm.Steps[Realm.StepOf(save)].Name + " · " + hero[0].Label + " " + hero[0].Value + " · " + hero[2].Label + " " + hero[2].Value;
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
