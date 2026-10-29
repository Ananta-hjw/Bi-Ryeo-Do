using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>One of two overlays: 세력 평판 (기록 tab) or the 객잔 whose shelves open with it (강호 tab side button).</summary>
    public sealed class FactionPanel : OverlayPanel
    {
        private ScrollRect _scroll;
        private readonly bool _shop;   // true = 객잔, false = 세력 평판

        public FactionPanel(RectTransform root, Action onChange, bool shop = false) : base(root, onChange)
        {
            _shop = shop;
        }

        public void Open(string toast = null)
        {
            var save = GameManager.Instance.Save;
            if (!_shop)
            {
                // Remember this visit's 단계 rises before acknowledging, so the page can say what changed.
                _rises.Clear();
                foreach (var rise in RecordUpdates.FactionRises(save)) _rises[rise.Faction.Id] = rise;
                if (RecordUpdates.Acknowledge(save, RecordUpdates.FactionPage) && Application.isPlaying) GameManager.Instance.SaveNow();
            }
            Begin();
            if (!Tutorial.IsDone(save, "faction") && HelpPanel.Instance != null) HelpPanel.Instance.Show("faction", true);

            UiFactory.Label("Title", Root, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.99f), (_shop ? "객잔 · " + save.Doo.ToString("N0") + "은자" : "세력 평판"), 40, UiFactory.Gold);
            UiFactory.ScrollBody(UiFactory.Label("Toast", Root, new Vector2(0.05f, .80f), new Vector2(0.95f, .89f), toast ?? (_shop ? "세력 평판이 오르면 그 세력의 선반이 열립니다. 평판은 기록 탭 「세력 평판」에서 봅니다." : RiseSummary() ?? "세력을 누르면 평판을 얻는 스테이지와 적용 중인 효과를 볼 수 있습니다."), 30, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 30);

            var content = CodexPanel.Scroller(Root, .02f, .79f);
            _scroll = content.parent.GetComponent<ScrollRect>();
            if (_shop) DrawShop(content, save);
            else DrawReputation(content, save);
            _detail = null;
            AddClose();
        }

        /// <summary>Colour of each 평판 단계 name (무명 · 안면 · 우호 · 신뢰 · 존경).</summary>
        private static readonly string[] TierColors = { "#8C8C8C", "#D8CFB8", "#9ADB9F", "#7FB8F0", "#E8C860" };

        private static string TierText(int tier) { return "<color=" + TierColors[tier] + ">" + Factions.TierNames[tier] + "</color>"; }

        private static string PerkNow(SaveData save, FactionData f) { return PerkAt(f, Factions.Tier(save, f.Id)); }

        private static string PerkAt(FactionData f, int tier)
        {
            return tier >= 2 ? f.PerkText + " +" + Math.Round(f.PerkSteps[tier - 2] * 100f) + (f.Perk == PerkKind.RestHeal ? "%p" : "%") : "없음 (우호부터)";
        }

        /// <summary>단계 rises since the page was last opened, kept for this visit (the badge is acknowledged on open).</summary>
        private readonly Dictionary<string, RecordUpdates.FactionRise> _rises = new Dictionary<string, RecordUpdates.FactionRise>();

        /// <summary>"단계 상승: 무당파 안면 → 우호, …" for the top line, or null when nothing rose.</summary>
        private string RiseSummary()
        {
            if (_rises.Count == 0) return null;
            var parts = new List<string>();
            foreach (var r in _rises.Values) parts.Add(r.Faction.Name + " " + TierText(r.From) + " → " + TierText(r.To));
            return "<color=#FF8A7A>단계 상승</color>: " + string.Join(", ", parts) + "\n<size=26>세력을 누르면 무엇이 바뀌었는지 볼 수 있습니다.</size>";
        }

        /// <summary>What one rise changed: the 단계, the effect and the 객잔 goods it opened.</summary>
        private static string RiseDetail(RecordUpdates.FactionRise r)
        {
            var lines = new List<string> { "단계 " + TierText(r.From) + " → " + TierText(r.To) };
            string before = PerkAt(r.Faction, r.From), after = PerkAt(r.Faction, r.To);
            if (before != after) lines.Add("효과 " + before + " → <color=#9ADB9F>" + after + "</color>");
            var opened = new List<string>();
            foreach (var g in Shop.Goods)
                if (g.FactionId == r.Faction.Id && g.MinTier > r.From && g.MinTier <= r.To) opened.Add(Shop.NameOf(g));
            if (opened.Count > 0) lines.Add("객잔 선반에 새로 열림: " + string.Join(", ", opened));
            return string.Join("\n", lines);
        }

        /// <summary>One card per faction: name and its 단계 (only the 단계 is coloured), points and the effect in force; tapping opens the details.</summary>
        private int DrawReputation(RectTransform content, SaveData save)
        {
            int n = 0;
            var grid = new ReadingGrid(content, 2, 240f);
            foreach (var f in Factions.All)
            {
                var band = grid.Add("Entry" + n++);
                int tier = Factions.Tier(save, f.Id), points = Factions.Points(save, f.Id), next = Factions.ToNext(save, f.Id);
                UiFactory.Label("Name", band, new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.96f), f.Name + "  ·  " + TierText(tier), 32, UiFactory.PaperText, TextAnchor.MiddleLeft);
                UiFactory.Label("Info", band, new Vector2(0.04f, 0.30f), new Vector2(0.96f, 0.62f), "평판 " + points + (next >= 0 ? " (다음 단계까지 " + next + ")" : " (최고)") + "  ·  효과: " + PerkNow(save, f), 28, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                UiFactory.Label("Hint", band, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.28f), "눌러서 상세 보기", 24, UiFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Normal);
                if (_rises.ContainsKey(f.Id)) RiseTag(band);
                var faction = f;
                var button = band.gameObject.AddComponent<Button>();
                button.targetGraphic = band.GetComponent<Image>();
                button.onClick.AddListener(() => ShowFaction(faction));
            }
            return n;
        }

        private RectTransform _detail;

        /// <summary>A red 「단계 상승」 ribbon on a faction whose 단계 rose since the page was last opened.</summary>
        private static void RiseTag(RectTransform card)
        {
            var tag = UiFactory.Panel("Rise", card, new Vector2(.70f, .74f), new Vector2(.99f, .97f), new Color(.70f, .16f, .12f, .95f));
            tag.raycastTarget = false;
            UiFactory.Label("Text", tag.transform, Vector2.zero, Vector2.one, "단계 상승", 22, UiFactory.PaperText);
        }

        /// <summary>Details of one faction: its 단계 ladder, the effect in force and at each 단계, where reputation comes from and the 객잔 shelf it opens.</summary>
        private void ShowFaction(FactionData f)
        {
            SoundManager.Sfx("ui_click");
            var save = GameManager.Instance.Save;
            int tier = Factions.Tier(save, f.Id), points = Factions.Points(save, f.Id), next = Factions.ToNext(save, f.Id);
            var lines = new List<string>();
            RecordUpdates.FactionRise rise;
            if (_rises.TryGetValue(f.Id, out rise)) lines.Add("<color=#FF8A7A>새로 바뀐 점</color>\n" + RiseDetail(rise));
            if (!string.IsNullOrEmpty(f.Description)) lines.Add(f.Description);
            lines.Add("<color=#E8C860>현재 단계</color>\n" + TierText(tier) + "  ·  평판 " + points + (next >= 0 ? " (다음 「" + Factions.TierNames[tier + 1] + "」까지 " + next + ")" : " (최고 단계)"));
            var ladder = new List<string>();
            for (int t = 1; t <= Factions.MaxTier; t++) ladder.Add(TierText(t) + " " + Factions.Thresholds[t]);
            lines.Add("<color=#E8C860>단계 문턱</color>\n" + string.Join("  ·  ", ladder));
            lines.Add("<color=#E8C860>적용 중인 효과</color>\n" + PerkNow(save, f) + "\n<size=26>" + f.PerkText + ": " + Factions.PerkLine(f) + "</size>");
            var stages = new List<string>();
            foreach (int st in f.Stages)
            {
                var data = EarlyGameContent.FindStage(st);
                stages.Add("스테이지 " + st + (data != null ? " 「" + data.Name + "」" : ""));
            }
            lines.Add("<color=#E8C860>평판 얻는 곳</color>\n" + string.Join("\n", stages)
                + "\n<size=26>전투마다 처음 이길 때 일반 +1 · 어려움 +2 · 극악 +3, 이 스테이지의 의뢰 완수 +" + Quests.QuestReputation + "</size>");
            var shelf = new List<string>();
            foreach (var g in Shop.Goods)
                if (g.FactionId == f.Id) shelf.Add((Shop.IsOpen(save, g) ? "" : "<color=#8C8C8C>") + TierText(g.MinTier) + " " + Shop.NameOf(g) + (Shop.IsOpen(save, g) ? "" : " (잠김)</color>"));
            if (shelf.Count > 0) lines.Add("<color=#E8C860>객잔 선반</color>\n" + string.Join("\n", shelf));
            if (_detail != null) UnityEngine.Object.Destroy(_detail.gameObject);
            _detail = UiFactory.InfoWindow(Root, f.Name + " 평판", string.Join("\n\n", lines));
            UiFactory.ShowInfo(_detail);
        }

        private const int ShopColumns = 5;

        /// <summary>객잔 shelf: five compact cards per row (big icon, name, price); tapping one opens its details and the buy button.</summary>
        private int DrawShop(RectTransform content, SaveData save)
        {
            var goods = new List<ShopGood>(Shop.Goods);
            // Open goods first, then by faction tier.
            goods.Sort((a, b) =>
            {
                int oa = Shop.IsOpen(save, a) ? 0 : 1, ob = Shop.IsOpen(save, b) ? 0 : 1;
                if (oa != ob) return oa.CompareTo(ob);
                return a.MinTier != b.MinTier ? a.MinTier.CompareTo(b.MinTier) : a.Price.CompareTo(b.Price);
            });
            const float rowHeight = 300f, gap = 16f, colGap = .012f;
            float cardWidth = (1f - colGap * (ShopColumns - 1)) / ShopColumns;
            RectTransform row = null;
            for (int i = 0; i < goods.Count; i++)
            {
                if (i % ShopColumns == 0)
                {
                    int r = i / ShopColumns;
                    row = UiFactory.Rect("Row" + r, content, new Vector2(.025f, 1f), new Vector2(.975f, 1f));
                    row.pivot = new Vector2(.5f, 1f);
                    row.sizeDelta = new Vector2(0f, rowHeight);
                    row.anchoredPosition = new Vector2(0f, -gap - r * (rowHeight + gap));
                    ReadingList.Register(row, rowHeight, gap);
                }
                var g = goods[i];
                bool open = Shop.IsOpen(save, g), sold = Shop.SoldOut(save, g);
                float x0 = (i % ShopColumns) * (cardWidth + colGap);
                var card = UiFactory.Button("Good" + i, row, new Vector2(x0, 0f), new Vector2(x0 + cardWidth, 1f), "", UiFactory.Ink, () => OpenGood(g), 26);
                UiFactory.Frame(card.transform, open && !sold ? UiFactory.GoldDim : new Color(.25f, .27f, .28f, 1f), 2f);
                var icon = UiFactory.Panel("Icon", card.transform, new Vector2(.12f, .40f), new Vector2(.88f, .96f), Color.white);
                icon.raycastTarget = false;
                var glyph = UiFactory.Label("Glyph", icon.transform, Vector2.zero, Vector2.one, "", 60, UiFactory.PaperText);
                GoodIcon(icon, glyph, g, open);
                if (!open || sold)
                {
                    // Some painted icons stay bright under a grey tint, so a dark veil over the picture makes a shut shelf read as shut.
                    var veil = UiFactory.Panel("Veil", card.transform, new Vector2(.02f, .02f), new Vector2(.98f, .98f), new Color(0f, 0f, 0f, .62f));
                    veil.raycastTarget = false;
                }
                string need = g.FactionId == null ? "잠김" : Factions.Find(g.FactionId).Name + " " + Factions.TierNames[g.MinTier];
                UiFactory.Label("Name", card.transform, new Vector2(.04f, .17f), new Vector2(.96f, .40f), open ? Shop.NameOf(g) : "???", 26, open && !sold ? UiFactory.PaperText : UiFactory.Dim);
                UiFactory.Label("Price", card.transform, new Vector2(.04f, .02f), new Vector2(.96f, .17f), sold ? "매진" : open ? g.Price.ToString("N0") + "은자" : "잠김 · " + need,
                    26, sold || !open ? UiFactory.Dim : save.Doo >= g.Price ? UiFactory.Gold : new Color(1f, .55f, .45f, 1f));
            }
            content.sizeDelta = new Vector2(0f, gap + ((goods.Count + ShopColumns - 1) / ShopColumns) * (rowHeight + gap));
            return goods.Count;
        }

        /// <summary>The good's picture: the item's own art, or a scroll glyph for 숙련 수련 and 경험치 두루마리; grey while its shelf is shut.</summary>
        private static void GoodIcon(Image icon, Text glyph, ShopGood g, bool open)
        {
            var item = g.Kind == ShopKind.Item ? EarlyGameContent.FindItem(g.ItemId) : null;
            if (item != null) ItemArt.Apply(icon, glyph, item);
            else
            {
                icon.sprite = null;
                icon.color = new Color(0f, 0f, 0f, 0f);
                glyph.text = "";
                UiFactory.Glyph(g.Kind == ShopKind.Training ? "arts" : "record", icon.transform, Vector2.zero, Vector2.one, UiFactory.Gold).raycastTarget = false;
            }
            icon.preserveAspect = true;
            if (!open) icon.color = new Color(.25f, .25f, .27f, icon.color.a);
        }

        /// <summary>Details of one good: picture, shelf, description, limit and the buy button.</summary>
        private void OpenGood(ShopGood g, string feedback = null)
        {
            var save = GameManager.Instance.Save;
            var old = Root.Find("GoodDetail");
            if (old != null) UnityEngine.Object.Destroy(old.gameObject);
            var layer = UiFactory.Rect("GoodDetail", Root, Vector2.zero, Vector2.one);
            layer.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, .78f);   // blocks the shelf underneath
            var card = UiFactory.Panel("Card", layer, new Vector2(.25f, .08f), new Vector2(.75f, .92f), UiFactory.ModalInk);
            UiFactory.Frame(card.transform, UiFactory.Gold, 3f);
            bool open = Shop.IsOpen(save, g), sold = Shop.SoldOut(save, g), afford = save.Doo >= g.Price;
            var icon = UiFactory.Panel("Icon", card.transform, new Vector2(.06f, .64f), new Vector2(.36f, .96f), Color.white);
            var glyph = UiFactory.Label("Glyph", icon.transform, Vector2.zero, Vector2.one, "", 60, UiFactory.PaperText);
            GoodIcon(icon, glyph, g, open);
            string owner = g.FactionId == null ? "객잔" : Factions.Find(g.FactionId).Name;
            UiFactory.Label("Title", card.transform, new Vector2(.40f, .84f), new Vector2(.95f, .96f), open ? Shop.NameOf(g) : "???", 38, UiFactory.Gold, TextAnchor.MiddleLeft);
            UiFactory.Label("Shelf", card.transform, new Vector2(.40f, .66f), new Vector2(.95f, .84f),
                owner + " 선반" + (g.Limit > 0 ? "\n구매 " + Shop.Bought(save, g.Id) + " / " + g.Limit : ""), 28, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            string body = open ? Shop.DescriptionOf(g) : owner + " 평판이 「" + Factions.TierNames[g.MinTier] + "」 이상이면 열립니다.";
            if (!string.IsNullOrEmpty(feedback)) body = feedback + "\n\n" + body;
            UiFactory.Body(UiFactory.ScrollBody(UiFactory.Label("Body", card.transform, new Vector2(.06f, .30f), new Vector2(.94f, .62f), body, 30, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal), 30));
            bool can = open && !sold && afford;
            string label = sold ? "매진" : !open ? "잠김" : g.Price.ToString("N0") + "은자에 사기" + (afford ? "" : " (은자 부족)");
            UiFactory.Button("Buy", card.transform, new Vector2(.06f, .15f), new Vector2(.94f, .27f), label, can ? UiFactory.Red : UiFactory.InkSoft, () => Buy(g), 32).interactable = can;
            UiFactory.BackButton(card.transform, new Vector2(.30f, .02f), new Vector2(.70f, .12f), () => UnityEngine.Object.Destroy(layer.gameObject));
        }

        private void Buy(ShopGood g)
        {
            var save = GameManager.Instance.Save;
            string error;
            float position = _scroll != null ? _scroll.verticalNormalizedPosition : 1f;
            var note = Shop.Buy(save, g.Id, out error);
            if (note != null) { GameManager.Instance.SaveNow(); SoundManager.Sfx("win"); }
            Open(note ?? error);
            Canvas.ForceUpdateCanvases();
            _scroll.content.GetComponent<ReadingList>()?.Reflow();
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = position;
            OpenGood(g, note ?? error);
        }
    }
}
