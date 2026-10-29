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
        private RectTransform _detail;
        public bool Embedded;
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
                r.gameObject.AddComponent<Image>().color = UiFactory.ModalInk;
                r.gameObject.SetActive(false);
            }
        }

        internal static void Clear(RectTransform rt)
        {
            LandscapeMenu.Clear(rt);
            UiFactory.MenuBackdrop(rt, BiRyeoDo.Game.Systems.SceneryArt.PanelKey(rt.name));
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
            if (!_codexRoot.gameObject.activeSelf)
            {
                // What the red badge counted: remembered for this visit so every tab can mark it, then acknowledged.
                _fresh = new HashSet<string>(RecordUpdates.UnreadKeys(save, RecordUpdates.CodexPage));
                if (FreshCount("enemy:") == 0 && FreshCount("item:") > 0) _tab = 1;
                else if (FreshCount("enemy:") > 0) _tab = 0;
            }
            if (RecordUpdates.Acknowledge(save, RecordUpdates.CodexPage) && Application.isPlaying) GameManager.Instance.SaveNow();
            Clear(_codexRoot);
            _codexRoot.gameObject.SetActive(true);
            if (!Tutorial.IsDone(save, "codex") && HelpPanel.Instance != null) HelpPanel.Instance.Show("codex", true);
            UiFactory.Label("Title", _codexRoot, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "도감", 44, UiFactory.Gold);
            for (int t = 0; t < TabNames.Length; t++)
            {
                int tab = t;
                float x0 = 0.05f + t * 0.31f;
                int fresh = t == 0 ? FreshCount("enemy:") : t == 1 ? FreshCount("item:") : 0;
                var btn = UiFactory.Button("Tab" + t, _codexRoot, new Vector2(x0, .79f), new Vector2(x0 + 0.29f, 0.91f), TabNames[t] + (fresh > 0 ? "  <color=#FF8A7A>새 " + fresh + "</color>" : ""), t == _tab ? UiFactory.Red : UiFactory.InkSoft, () => { _tab = tab; OpenCodex(); }, 30);
            }

            string summary;
            var content = Scroller(_codexRoot, .02f, .69f);
            var grid = new ReadingGrid(content, 3, 290f);
            int n = 0;
            if (_tab == 0)
            {
                summary = "만난 적 " + save.SeenEnemies.Count + " / " + Codex.EnemyTotal + FreshSummary("enemy:", id => EarlyGameContent.FindEnemy(id)?.Name);
                var enemies = new List<EnemyData>();   // new ones first, the rest in content order
                foreach (var e in EarlyGameContent.Enemies) if (IsFresh("enemy:" + e.Id)) enemies.Add(e);
                foreach (var e in EarlyGameContent.Enemies) if (!IsFresh("enemy:" + e.Id)) enemies.Add(e);
                foreach (var e in enemies)
                {
                    bool seen = Codex.HasEnemy(save, e.Id);
                    var card = grid.Add("Enemy" + n++);
                    if (IsFresh("enemy:" + e.Id)) NewTag(card);
                    var icon = UiFactory.Panel("Icon", card, new Vector2(.04f, .57f), new Vector2(.25f, .94f), UiFactory.InkSoft);
                    var glyph = UiFactory.Label("Glyph", icon.transform, Vector2.zero, Vector2.one, seen ? e.Name.Substring(0, 1) : "?", 44, UiFactory.PaperText);
                    var portrait = seen ? ArtLibrary.CharacterPortrait(e.ArtKey) : null;
                    if (portrait != null) { icon.sprite = portrait; icon.color = Color.white; icon.preserveAspect = true; glyph.text = ""; }
                    UiFactory.Label("Name", card, new Vector2(.29f, .57f), new Vector2(.96f, .95f), seen ? e.Name : "???", 30, seen ? UiFactory.PaperText : UiFactory.Dim, TextAnchor.MiddleLeft);
                    UiFactory.Label("Info", card, new Vector2(.04f, .10f), new Vector2(.96f, .53f), seen ? "Lv." + e.DisplayLevel + (e.IsBoss ? " · 우두머리" : "") + (Realm.HasRealm(e) ? "\n경지 " + Realm.ForEnemy(e) : "") : "아직 쓰러뜨리지 못했습니다", 28, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                }
            }
            else if (_tab == 1)
            {
                summary = "모은 물품 " + save.SeenItems.Count + " / " + Codex.ItemTotal + FreshSummary("item:", id => EarlyGameContent.FindItem(id)?.Name);
                var items = new List<ItemData>(EarlyGameContent.Items);
                items.Sort((a, b) =>
                {
                    int fa = IsFresh("item:" + a.Id) ? 0 : 1, fb = IsFresh("item:" + b.Id) ? 0 : 1;
                    if (fa != fb) return fa.CompareTo(fb);
                    return b.Grade != a.Grade ? ((int)b.Grade).CompareTo((int)a.Grade) : string.CompareOrdinal(a.Id, b.Id);
                });
                foreach (var it in items)
                {
                    bool seen = Codex.HasItem(save, it.Id);
                    var card = grid.Add("Item" + n++);
                    if (IsFresh("item:" + it.Id)) NewTag(card);
                    var icon = UiFactory.Panel("Icon", card, new Vector2(.04f, .57f), new Vector2(.25f, .94f), Color.white);
                    var letter = UiFactory.Label("Glyph", icon.transform, Vector2.zero, Vector2.one, "?", 44, UiFactory.PaperText);
                    if (seen) ItemArt.Apply(icon, letter, it);
                    else icon.color = UiFactory.InkSoft;
                    UiFactory.Label("Name", card, new Vector2(.29f, .57f), new Vector2(.96f, .95f), seen ? UiFactory.GradedName(it) : "[" + Grades.NameOf(it.Grade) + "] ???", 28, UiFactory.PaperText, TextAnchor.MiddleLeft);
                    string effect = seen ? (it.IsGear ? StatusSheet.EffectText(it) + "\n세트: " + (Gear.SetOf(it.Id)?.Name ?? "없음") : it.Description) : "아직 손에 넣지 못했습니다";
                    UiFactory.Label("Info", card, new Vector2(.04f, .19f), new Vector2(.96f, .54f), effect, 26, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                    if (seen)
                    {
                        UiFactory.Label("DetailHint", card, new Vector2(.04f, .035f), new Vector2(.96f, .16f), "눌러서 상세 보기", 24, UiFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Normal);
                        var item = it;
                        var button = card.gameObject.AddComponent<Button>();
                        button.targetGraphic = card.GetComponent<Image>();
                        button.onClick.AddListener(() => ShowDetail(UiFactory.GradedName(item), item.Description + (item.IsGear ? "\n\n" + StatusSheet.EffectText(item) + "\n세트: " + (Gear.SetOf(item.Id)?.Name ?? "없음") : "")));
                    }
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
                    var card = grid.Add("Art" + n++);
                    var icon = UiFactory.Panel("Icon", card, new Vector2(.04f, .57f), new Vector2(.25f, .94f), UiFactory.InkSoft);
                    if (known) { icon.sprite = ArtLibrary.Art(a.Id); icon.color = Color.white; icon.preserveAspect = true; }
                    else UiFactory.Label("Glyph", icon.transform, Vector2.zero, Vector2.one, "?", 44, UiFactory.Dim);
                    UiFactory.Label("Name", card, new Vector2(.29f, .57f), new Vector2(.96f, .95f), known ? a.Name : "???", 30, UiFactory.PaperText, TextAnchor.MiddleLeft);
                    UiFactory.Label("Info", card, new Vector2(.04f, .19f), new Vector2(.96f, .54f), known ? (a.AllEnemies ? "전체 대상" : a.SpiritCost > 0 ? "필살기" : "무공") + " · 내공 " + a.QiCost + (a.SpiritCost > 0 ? " · 기세 " + a.SpiritCost : "") + "\n재사용 " + a.Cooldown + "턴" : a.UnlockText + " 후 습득 가능", 26, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                    UiFactory.Label("State", card, new Vector2(.04f, .035f), new Vector2(.96f, .16f), known ? "습득 · 눌러서 상세 보기" : "미습득", 24, known ? UiFactory.Gold : UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                    if (known)
                    {
                        var art = a;
                        var button = card.gameObject.AddComponent<Button>();
                        button.targetGraphic = card.GetComponent<Image>();
                        button.onClick.AddListener(() => ShowDetail(art.Name, art.Description));
                    }
                }
            }
            UiFactory.ScrollBody(UiFactory.Label("Summary", _codexRoot, new Vector2(0.05f, .70f), new Vector2(0.95f, .78f), summary, 30, UiFactory.Gold), 30);
            if (!Embedded) UiFactory.BackCorner(_codexRoot, () => { _codexRoot.gameObject.SetActive(false); _onChange(); });
        }

        private HashSet<string> _fresh = new HashSet<string>();

        private bool IsFresh(string key) { return _fresh.Contains(key); }

        private int FreshCount(string prefix)
        {
            int n = 0;
            foreach (var k in _fresh) if (k.StartsWith(prefix, StringComparison.Ordinal)) n++;
            return n;
        }

        /// <summary>"  ·  새로 발견 3: 염도, 철검, …" — the names behind the red badge (up to four, then a count).</summary>
        private string FreshSummary(string prefix, Func<string, string> nameOf)
        {
            var names = new List<string>();
            foreach (var k in _fresh)
                if (k.StartsWith(prefix, StringComparison.Ordinal)) { var name = nameOf(k.Substring(prefix.Length)); if (!string.IsNullOrEmpty(name)) names.Add(name); }
            if (names.Count == 0) return "";
            string shown = string.Join(", ", names.GetRange(0, Math.Min(4, names.Count))) + (names.Count > 4 ? " 외 " + (names.Count - 4) + "개" : "");
            return "\n<color=#FF8A7A>새로 발견 " + names.Count + "</color>: " + shown;
        }

        /// <summary>A red 「새로 발견」 ribbon on a card found since the 도감 was last opened (it stays for this visit).</summary>
        private static void NewTag(RectTransform card)
        {
            var tag = UiFactory.Panel("New", card, new Vector2(.62f, .86f), new Vector2(.99f, .99f), new Color(.70f, .16f, .12f, .95f));
            tag.raycastTarget = false;
            UiFactory.Label("Text", tag.transform, Vector2.zero, Vector2.one, "새로 발견", 22, UiFactory.PaperText);
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
            var all = UiFactory.Button("ClaimAll", _achRoot, new Vector2(0.64f, .80f), new Vector2(0.95f, .92f), "모두 수령", ready > 0 ? UiFactory.Red : UiFactory.InkSoft, () => ClaimAll(), 28);
            all.interactable = ready > 0;

            var list = new List<AchievementData>(Achievements.All);
            // Ready first, then in progress, then taken.
            list.Sort((a, b) => Rank(save, a).CompareTo(Rank(save, b)));
            var content = Scroller(_achRoot, .02f, .78f);
            var grid = new ReadingGrid(content, 2, 310f);
            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                var band = grid.Add("Achievement" + i);
                bool claimed = Achievements.IsClaimed(save, a), can = Achievements.CanClaim(save, a);
                int target = Achievements.TargetOf(a), prog = Math.Min(Achievements.Progress(save, a), target);
                UiFactory.Label("Name", band, new Vector2(.04f, .75f), new Vector2(.96f, .96f), a.Name, 32, claimed ? UiFactory.Dim : UiFactory.PaperText, TextAnchor.MiddleLeft);
                UiFactory.Label("Desc", band, new Vector2(.04f, .47f), new Vector2(.96f, .73f), a.Description, 28, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                UiFactory.Label("Progress", band, new Vector2(.04f, .28f), new Vector2(.96f, .45f), prog + " / " + target + " · 보상 " + a.RewardDoo.ToString("N0") + "은자" + (string.IsNullOrEmpty(a.RewardItemId) ? "" : " + " + EarlyGameContent.FindItem(a.RewardItemId)?.Name), 27, UiFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Normal);
                var id = a.Id;
                var btn = UiFactory.Button("Claim", band, new Vector2(.04f, .04f), new Vector2(.96f, .25f), claimed ? "수령 완료" : can ? "수령" : "진행 중", can ? UiFactory.Red : UiFactory.InkSoft, () => Claim(id), 28);
                btn.interactable = can;
            }
            if (!Embedded) UiFactory.BackCorner(_achRoot, () => { _achRoot.gameObject.SetActive(false); _onChange(); });
        }

        private void ShowDetail(string title, string body)
        {
            SoundManager.Sfx("ui_click");
            if (_detail != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_detail.gameObject);
                else UnityEngine.Object.DestroyImmediate(_detail.gameObject);
            }
            _detail = UiFactory.InfoWindow(_codexRoot, title, body);
            UiFactory.ShowInfo(_detail);
        }

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
