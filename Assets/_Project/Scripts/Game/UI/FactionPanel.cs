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
            if (RecordUpdates.Acknowledge(save, RecordUpdates.FactionPage) && Application.isPlaying) GameManager.Instance.SaveNow();
            Begin();
            if (!Tutorial.IsDone(save, "faction") && HelpPanel.Instance != null) HelpPanel.Instance.Show("faction", true);

            UiFactory.Label("Title", Root, new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.99f), (_shop ? "객잔 · " + save.Doo.ToString("N0") + "은자" : "세력 평판"), 40, UiFactory.Gold);
            UiFactory.ScrollBody(UiFactory.Label("Toast", Root, new Vector2(0.05f, .80f), new Vector2(0.95f, .89f), toast ?? (_shop ? "세력 평판이 오르면 그 세력의 선반이 열립니다. 평판은 기록 탭 「세력 평판」에서 봅니다." : "그 세력의 스테이지에서 전투를 이기면 평판이 쌓입니다 (일반 +1, 어려움 +2, 극악 +3)"), 30, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 30);

            var content = CodexPanel.Scroller(Root, .02f, .79f);
            _scroll = content.parent.GetComponent<ScrollRect>();
            if (_shop) DrawShop(content, save);
            else DrawReputation(content, save);
            AddClose();
        }

        private static int DrawReputation(RectTransform content, SaveData save)
        {
            int n = 0;
            var grid = new ReadingGrid(content, 2, 300f);
            foreach (var f in Factions.All)
            {
                var band = grid.Add("Entry" + n++);
                int tier = Factions.Tier(save, f.Id), points = Factions.Points(save, f.Id), next = Factions.ToNext(save, f.Id);
                UiFactory.Label("Name", band, new Vector2(0.04f, 0.58f), new Vector2(0.96f, 0.97f), f.Name + "  ·  " + Factions.TierNames[tier], 32, tier >= 4 ? UiFactory.Gold : UiFactory.PaperText, TextAnchor.MiddleLeft);
                string perk = f.PerkText + " " + (tier >= 2 ? "+" + Math.Round(Factions.PerkValue(save, f) * 100f) + (f.Perk == PerkKind.RestHeal ? "%p" : "%") : "(우호부터)");
                UiFactory.Label("Info", band, new Vector2(0.04f, 0.32f), new Vector2(0.96f, 0.58f), "평판 " + points + (next >= 0 ? " (다음 단계까지 " + next + ")" : " (최고)") + "  ·  " + perk, 30, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                UiFactory.Label("Where", band, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.32f), "얻는 곳: 스테이지 " + string.Join(", ", f.Stages), 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            }
            return n;
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
                GoodIcon(icon, glyph, g, open && !sold);
                if (g.FactionId != null)
                    UiFactory.Label("FactionMark", card.transform, new Vector2(.87f, .79f), new Vector2(.99f, .96f),
                        FactionMark(g.FactionId), 22, open && !sold ? UiFactory.GoldDim : UiFactory.Dim);
                UiFactory.Label("Name", card.transform, new Vector2(.04f, .17f), new Vector2(.96f, .40f), open ? Shop.NameOf(g) : "???", 26, open ? UiFactory.PaperText : UiFactory.InkSoft);
                UiFactory.Label("Price", card.transform, new Vector2(.04f, .02f), new Vector2(.96f, .17f), sold ? "매진" : open ? g.Price.ToString("N0") + "은자" : "잠김",
                    26, sold || !open ? UiFactory.Dim : save.Doo >= g.Price ? UiFactory.Gold : new Color(1f, .55f, .45f, 1f));
            }
            content.sizeDelta = new Vector2(0f, gap + ((goods.Count + ShopColumns - 1) / ShopColumns) * (rowHeight + gap));
            return goods.Count;
        }

        private static string FactionMark(string id)
        {
            switch (id)
            {
                case "academy": return "학";
                case "biaoguk": return "표";
                case "wudang": return "무";
                case "greenwood": return "림";
                case "huashan": return "화";
                default: return "";
            }
        }

        /// <summary>Item art or a distinct service illustration; locked and sold goods share one ink tone.</summary>
        private static void GoodIcon(Image icon, Text glyph, ShopGood g, bool available)
        {
            var item = g.Kind == ShopKind.Item ? EarlyGameContent.FindItem(g.ItemId) : null;
            if (item != null) ItemArt.Apply(icon, glyph, item);
            else if (ArtLibrary.ShopGood(g) != null)
            {
                icon.sprite = ArtLibrary.ShopGood(g);
                icon.color = Color.white;
                icon.material = null;
                glyph.text = "";
            }
            else
            {
                icon.sprite = null;
                icon.color = new Color(0f, 0f, 0f, 0f);
                glyph.text = "";
                UiFactory.Glyph(g.Kind == ShopKind.Training ? "arts" : "record", icon.transform, Vector2.zero, Vector2.one, UiFactory.Gold).raycastTarget = false;
            }
            icon.preserveAspect = true;
            if (!available) IconCutout.ApplyLocked(icon.transform);
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
            GoodIcon(icon, glyph, g, open && !sold);
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
