using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>세력 평판 and 객잔 overlay: two tabs, reputation per faction and the shop whose shelves open with it.</summary>
    public sealed class FactionPanel : OverlayPanel
    {
        private int _tab;   // 0 평판, 1 객잔
        private static readonly string[] TabNames = { "평판", "객잔" };

        public FactionPanel(RectTransform root, Action onChange) : base(root, onChange)
        {
        }

        public void Open(string toast = null)
        {
            var save = GameManager.Instance.Save;
            if (RecordUpdates.Acknowledge(save, RecordUpdates.FactionPage) && Application.isPlaying) GameManager.Instance.SaveNow();
            Begin();
            if (!Tutorial.IsDone(save, "faction") && HelpPanel.Instance != null) HelpPanel.Instance.Show("faction", true);

            UiFactory.Label("Title", Root, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "세력과 객잔 · " + save.Doo.ToString("N0") + "두", 40, UiFactory.Gold);
            for (int t = 0; t < TabNames.Length; t++)
            {
                int tab = t;
                float x0 = 0.05f + t * 0.47f;
                UiFactory.Button("Tab" + t, Root, new Vector2(x0, .79f), new Vector2(x0 + 0.43f, 0.91f), TabNames[t], t == _tab ? UiFactory.Red : UiFactory.InkSoft, () => { _tab = tab; Open(); }, 30);
            }
            UiFactory.ScrollBody(UiFactory.Label("Toast", Root, new Vector2(0.05f, .70f), new Vector2(0.95f, .78f), toast ?? (_tab == 0 ? "그 세력의 스테이지에서 전투를 이기면 평판이 쌓입니다 (일반 +1, 어려움 +2, 극악 +3)" : "세력 평판이 오르면 그 세력의 선반이 열립니다"), 30, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 30);

            var content = CodexPanel.Scroller(Root, .16f, .69f);
            if (_tab == 0) DrawReputation(content, save);
            else DrawShop(content, save);
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

        private int DrawShop(RectTransform content, SaveData save)
        {
            int n = 0;
            var grid = new ReadingGrid(content, 2, 300f);
            var goods = new List<ShopGood>(Shop.Goods);
            // Open goods first, then by faction tier.
            goods.Sort((a, b) =>
            {
                int oa = Shop.IsOpen(save, a) ? 0 : 1, ob = Shop.IsOpen(save, b) ? 0 : 1;
                if (oa != ob) return oa.CompareTo(ob);
                return a.MinTier != b.MinTier ? a.MinTier.CompareTo(b.MinTier) : a.Price.CompareTo(b.Price);
            });
            foreach (var g in goods)
            {
                var band = grid.Add("Entry" + n++);
                bool open = Shop.IsOpen(save, g), sold = Shop.SoldOut(save, g), afford = save.Doo >= g.Price;
                string owner = g.FactionId == null ? "객잔" : Factions.Find(g.FactionId).Name;
                string limit = g.Limit > 0 ? "  (" + Shop.Bought(save, g.Id) + "/" + g.Limit + ")" : "";
                UiFactory.Label("Name", band, new Vector2(0.04f, 0.73f), new Vector2(0.96f, 0.96f), (open ? Shop.NameOf(g) : "???") + limit, 30, open ? UiFactory.PaperText : UiFactory.InkSoft, TextAnchor.MiddleLeft);
                UiFactory.Label("Info", band, new Vector2(0.04f, 0.30f), new Vector2(0.96f, 0.70f),
                    open ? "[" + owner + "] " + Shop.DescriptionOf(g) : owner + " 평판 " + Factions.TierNames[g.MinTier] + " 필요", 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                var good = g;
                bool can = open && !sold && afford;
                var btn = UiFactory.Button("Buy", band, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.26f), sold ? "매진" : open ? g.Price.ToString("N0") + "두" : "잠김", can ? UiFactory.Red : UiFactory.InkSoft, () => Buy(good), 26);
                btn.interactable = can;
            }
            return n;
        }

        private void Buy(ShopGood g)
        {
            var save = GameManager.Instance.Save;
            string error;
            var note = Shop.Buy(save, g.Id, out error);
            if (note != null) { GameManager.Instance.SaveNow(); SoundManager.Sfx("win"); }
            Open(note ?? error);
        }
    }
}
