using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>도감 and 업적 overlays: scrolling lists drawn into two full-screen panels owned by the 강호 tab.</summary>
    public sealed class CodexPanel
    {
        private readonly RectTransform _codexRoot, _achRoot;
        private readonly Action _onChange;
        private int _tab;                       // 0 enemies, 1 items, 2 arts
        private static readonly string[] TabNames = { "적", "물품", "무공" };
        internal const float RowH = 220f, RowGap = 16f;   // tall enough for two stacked, easy-to-hit buttons

        public CodexPanel(RectTransform codexRoot, RectTransform achRoot, Action onChange)
        {
            _codexRoot = codexRoot;
            _achRoot = achRoot;
            _onChange = onChange;
            foreach (var r in new[] { codexRoot, achRoot })
            {
                r.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.06f, 0.09f, 0.98f);
                r.gameObject.SetActive(false);
            }
        }

        internal static void Clear(RectTransform rt)
        {
            for (int i = rt.childCount - 1; i >= 0; i--) { rt.GetChild(i).gameObject.SetActive(false); UnityEngine.Object.Destroy(rt.GetChild(i).gameObject); }
            UiFactory.Frame(rt, UiFactory.GoldDim, 3f);
        }

        /// <summary>A scrolling area between the given anchors; returns its content rectangle (height set later).</summary>
        internal static RectTransform Scroller(RectTransform parent, float yMin, float yMax)
        {
            var viewport = UiFactory.Rect("Viewport", parent, new Vector2(0f, yMin), new Vector2(1f, yMax));
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
            UiFactory.ScrollIndicator(scroll, viewport);
            return content;
        }

        internal static RectTransform Band(RectTransform content, int index)
        {
            var rt = UiFactory.Rect("Row" + index, content, new Vector2(0.04f, 1f), new Vector2(0.96f, 1f));
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -(RowGap + index * (RowH + RowGap)));
            rt.sizeDelta = new Vector2(0f, RowH);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = UiFactory.Ink;
            bg.raycastTarget = true;   // lets a drag that starts on a row still scroll
            UiFactory.Frame(rt, UiFactory.GoldDim, 2f);
            ReadingList.Register(rt, RowH, RowGap);
            return rt;
        }

        // ------------------------------------------------------------------ 도감

        public void HideAll()
        {
            _codexRoot.gameObject.SetActive(false);
            _achRoot.gameObject.SetActive(false);
        }

        public void OpenCodex()
        {
            var save = GameManager.Instance.Save;
            Codex.Sync(save);
            Clear(_codexRoot);
            _codexRoot.gameObject.SetActive(true);
            if (!Tutorial.IsDone(save, "codex") && HelpPanel.Instance != null) HelpPanel.Instance.Show("codex", true);
            UiFactory.Label("Title", _codexRoot, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "도감", 44, UiFactory.Gold);
            for (int t = 0; t < TabNames.Length; t++)
            {
                int tab = t;
                float x0 = 0.05f + t * 0.31f;
                var btn = UiFactory.Button("Tab" + t, _codexRoot, new Vector2(x0, 0.855f), new Vector2(x0 + 0.29f, 0.91f), TabNames[t], t == _tab ? UiFactory.Red : UiFactory.InkSoft, () => { _tab = tab; OpenCodex(); }, 30);
            }

            string summary;
            var content = Scroller(_codexRoot, 0.09f, 0.795f);
            int n = 0;
            if (_tab == 0)
            {
                summary = "만난 적 " + save.SeenEnemies.Count + " / " + Codex.EnemyTotal;
                foreach (var e in EarlyGameContent.Enemies)
                {
                    bool seen = Codex.HasEnemy(save, e.Id);
                    var band = Band(content, n++);
                    var glyph = UiFactory.Panel("Icon", band, new Vector2(0.025f, 0.14f), new Vector2(0.125f, 0.86f), seen ? new Color(0.30f, 0.20f, 0.20f, 1f) : new Color(0.15f, 0.15f, 0.17f, 1f));
                    UiFactory.Label("Glyph", glyph.transform, Vector2.zero, Vector2.one, seen ? e.Name.Substring(0, 1) : "?", 44, UiFactory.PaperText);
                    UiFactory.Label("Name", band, new Vector2(0.15f, 0.52f), new Vector2(0.97f, 0.96f), seen ? e.Name + (e.IsBoss ? "  (우두머리)" : "") : "???", 32, seen ? UiFactory.PaperText : UiFactory.InkSoft, TextAnchor.MiddleLeft);
                    UiFactory.Label("Info", band, new Vector2(0.15f, 0.06f), new Vector2(0.97f, 0.52f), seen ? "Lv." + e.DisplayLevel + (Realm.HasRealm(e) ? "  ·  경지 " + Realm.ForEnemy(e) : "") : "아직 쓰러뜨리지 못했습니다", 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                }
            }
            else if (_tab == 1)
            {
                summary = "모은 물품 " + save.SeenItems.Count + " / " + Codex.ItemTotal;
                var items = new List<ItemData>(EarlyGameContent.Items);
                items.Sort((a, b) => b.Grade != a.Grade ? ((int)b.Grade).CompareTo((int)a.Grade) : string.CompareOrdinal(a.Id, b.Id));
                foreach (var it in items)
                {
                    bool seen = Codex.HasItem(save, it.Id);
                    var band = Band(content, n++);
                    var glyph = UiFactory.Panel("Icon", band, new Vector2(0.025f, 0.14f), new Vector2(0.125f, 0.86f), seen ? Dark(UiFactory.GradeColor(it.Grade)) : new Color(0.15f, 0.15f, 0.17f, 1f));
                    UiFactory.Label("Glyph", glyph.transform, Vector2.zero, Vector2.one, seen ? it.Name.Substring(0, 1) : "?", 44, UiFactory.PaperText);
                    UiFactory.Label("Name", band, new Vector2(0.15f, 0.52f), new Vector2(0.97f, 0.96f), seen ? UiFactory.GradedName(it) : "<color=#555B66>[" + Grades.NameOf(it.Grade) + "]</color> ???", 30, UiFactory.PaperText, TextAnchor.MiddleLeft);
                    UiFactory.Label("Info", band, new Vector2(0.15f, 0.04f), new Vector2(0.97f, 0.52f), seen ? it.Description : "아직 손에 넣지 못했습니다", 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                }
            }
            else
            {
                var arts = Codex.HeroArts();
                int have = 0;
                foreach (var a in arts) if (save.LearnedArts.Contains(a.Id)) have++;
                summary = "익힌 무공 " + have + " / " + arts.Count;
                foreach (var a in arts)
                {
                    bool known = save.LearnedArts.Contains(a.Id);
                    var band = Band(content, n++);
                    UiFactory.Label("Name", band, new Vector2(0.04f, 0.52f), new Vector2(0.70f, 0.96f), known ? a.Name : "???", 32, known ? UiFactory.PaperText : UiFactory.InkSoft, TextAnchor.MiddleLeft);
                    UiFactory.Label("Info", band, new Vector2(0.04f, 0.04f), new Vector2(0.97f, 0.52f), known ? a.Description : a.UnlockText + "하면 배울 수 있습니다", 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                    UiFactory.Label("State", band, new Vector2(0.70f, 0.52f), new Vector2(0.97f, 0.96f), known ? "습득" : "미습득", 30, known ? UiFactory.Gold : UiFactory.Dim, TextAnchor.MiddleRight);
                }
            }
            content.sizeDelta = new Vector2(0f, RowGap + n * (RowH + RowGap));
            UiFactory.Label("Summary", _codexRoot, new Vector2(0.05f, 0.795f), new Vector2(0.95f, 0.85f), summary, 30, UiFactory.Gold);
            UiFactory.Button("Close", _codexRoot, new Vector2(0.25f, 0.01f), new Vector2(0.75f, 0.09f), "닫기", UiFactory.InkSoft, () => { _codexRoot.gameObject.SetActive(false); _onChange(); }, 36);
        }

        // ------------------------------------------------------------------ 업적

        public void OpenAchievements(string toast = null)
        {
            var save = GameManager.Instance.Save;
            Codex.Sync(save);
            Clear(_achRoot);
            _achRoot.gameObject.SetActive(true);
            int ready = Achievements.ClaimableCount(save);
            UiFactory.Label("Title", _achRoot, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "업적  " + Achievements.ClaimedCount(save) + " / " + Achievements.All.Length, 44, UiFactory.Gold);
            UiFactory.Label("Toast", _achRoot, new Vector2(0.05f, 0.865f), new Vector2(0.62f, 0.92f), toast ?? (ready > 0 ? "받을 수 있는 보상 " + ready + "건" : "조건을 채우면 보상을 받습니다"), 30, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
            var all = UiFactory.Button("ClaimAll", _achRoot, new Vector2(0.64f, 0.865f), new Vector2(0.95f, 0.92f), "모두 수령", ready > 0 ? UiFactory.Red : UiFactory.InkSoft, () => ClaimAll(), 28);
            all.interactable = ready > 0;

            var list = new List<AchievementData>(Achievements.All);
            // Ready first, then in progress, then taken.
            list.Sort((a, b) => Rank(save, a).CompareTo(Rank(save, b)));
            var content = Scroller(_achRoot, 0.09f, 0.855f);
            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                var band = Band(content, i);
                bool claimed = Achievements.IsClaimed(save, a), can = Achievements.CanClaim(save, a);
                int target = Achievements.TargetOf(a), prog = Math.Min(Achievements.Progress(save, a), target);
                UiFactory.Label("Name", band, new Vector2(0.04f, 0.54f), new Vector2(0.68f, 0.97f), a.Name, 32, claimed ? UiFactory.InkSoft : UiFactory.PaperText, TextAnchor.MiddleLeft);
                UiFactory.Label("Desc", band, new Vector2(0.04f, 0.04f), new Vector2(0.68f, 0.54f),
                    a.Description + "  (" + prog + "/" + target + ")  · 보상 " + a.RewardDoo.ToString("N0") + "두" + (string.IsNullOrEmpty(a.RewardItemId) ? "" : " + 아이템"), 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                var id = a.Id;
                var btn = UiFactory.Button("Claim", band, new Vector2(0.71f, 0.14f), new Vector2(0.97f, 0.86f), claimed ? "수령 완료" : can ? "수령" : "진행 중", can ? UiFactory.Red : UiFactory.InkSoft, () => Claim(id), 26);
                btn.interactable = can;
            }
            content.sizeDelta = new Vector2(0f, RowGap + list.Count * (RowH + RowGap));
            UiFactory.Button("Close", _achRoot, new Vector2(0.25f, 0.01f), new Vector2(0.75f, 0.09f), "닫기", UiFactory.InkSoft, () => { _achRoot.gameObject.SetActive(false); _onChange(); }, 36);
        }

        private static Color Dark(Color c) { return new Color(c.r * 0.6f, c.g * 0.6f, c.b * 0.6f, 1f); }

        private static int Rank(SaveData save, AchievementData a)
        {
            if (Achievements.IsClaimed(save, a)) return 2;
            return Achievements.CanClaim(save, a) ? 0 : 1;
        }

        private void Claim(string id)
        {
            var note = Achievements.Claim(GameManager.Instance.Save, id);
            if (note == null) return;
            GameManager.Instance.SaveNow();
            SoundManager.Sfx("levelup");
            OpenAchievements("수령: " + note);
        }

        private void ClaimAll()
        {
            var note = Achievements.ClaimAll(GameManager.Instance.Save);
            if (note == null) return;
            GameManager.Instance.SaveNow();
            SoundManager.Sfx("levelup");
            OpenAchievements(note);
        }
    }
}
