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
            UiFactory.Glyph("record", root, new Vector2(.065f, .936f), new Vector2(.13f, .977f), UiFactory.Gold);
            UiFactory.Label("Header", root, new Vector2(0.16f, 0.925f), new Vector2(0.94f, 0.985f), "기록", 42, UiFactory.Gold);

            _statusE = Tile(root, .04f, .96f, .71f, .90f, "상태창", "party", () => _status.Open());
            _codexE = Tile(root, .04f, .49f, .48f, .695f, "도감", "record", () => _codex.OpenCodex());
            _achE = Tile(root, .51f, .96f, .48f, .695f, "업적", "star", () => _codex.OpenAchievements());
            _storyE = Tile(root, .04f, .96f, .34f, .465f, "이야기 줄거리", "quest", () => _storyLog.Open());
            _memoryE = Tile(root, .04f, .49f, .175f, .325f, "나예린 추억", "fortune", () => _memories.Open());
            _factionE = Tile(root, .51f, .96f, .175f, .325f, "세력과 객잔", "tower", () => _factions.Open());
            _ritualE = Tile(root, .04f, .96f, .025f, .16f, "전승의식", "arts", () => _ritual.Open());

            var panelH = new Vector2(1f, 1f);
            _status = new StatusPanel(UiFactory.Rect("StatusOverlay", root, Vector2.zero, panelH), Refresh);
            _codex = new CodexPanel(UiFactory.Rect("CodexOverlay", root, Vector2.zero, panelH), UiFactory.Rect("AchievementOverlay", root, Vector2.zero, panelH), Refresh);
            _storyLog = new StoryLogPanel(UiFactory.Rect("StoryLogOverlay", root, Vector2.zero, panelH), Refresh, onStory);
            _memories = new MemoryPanel(UiFactory.Rect("MemoryOverlay", root, Vector2.zero, panelH), Refresh, onStory);
            _factions = new FactionPanel(UiFactory.Rect("FactionOverlay", root, Vector2.zero, panelH), Refresh);
            _ritual = new RitualPanel(UiFactory.Rect("RitualOverlay", root, Vector2.zero, panelH), Refresh, onStory);
            Refresh();
        }

        private static Entry Tile(RectTransform root, float x0, float x1, float y0, float y1, string title, string icon, UnityEngine.Events.UnityAction onClick)
        {
            var rt = UiFactory.Rect("Tile", root, new Vector2(x0, y0), new Vector2(x1, y1));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UiFactory.Gradient;
            img.color = UiFactory.Ink;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            UiFactory.Frame(rt, UiFactory.GoldDim, 2f);
            bool wide = x1 - x0 > .6f;
            UiFactory.Glyph(icon, rt, wide ? new Vector2(.025f, .17f) : new Vector2(.06f, .65f),
                wide ? new Vector2(.22f, .83f) : new Vector2(.28f, .95f), UiFactory.Gold);
            UiFactory.Heading(UiFactory.Label("Title", rt, wide ? new Vector2(.26f, .57f) : new Vector2(.33f, .64f),
                new Vector2(.94f, .94f), title, wide ? 38 : 30, UiFactory.PaperText, TextAnchor.MiddleLeft));
            var e = new Entry();
            e.Detail = UiFactory.Body(UiFactory.Label("Detail", rt, wide ? new Vector2(.26f, .08f) : new Vector2(.08f, .07f),
                wide ? new Vector2(.94f, .56f) : new Vector2(.92f, .61f), "", 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal), 1.15f);
            UiFactory.Label("Arrow", rt, new Vector2(.90f, .15f), new Vector2(.98f, .50f), "›", 36, UiFactory.Gold);
            var dot = UiFactory.Panel("Dot", rt, new Vector2(.84f, .78f), new Vector2(.97f, .96f), UiFactory.Red);
            e.Badge = UiFactory.Label("Count", dot.transform, Vector2.zero, Vector2.one, "", 30, UiFactory.PaperText);
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
