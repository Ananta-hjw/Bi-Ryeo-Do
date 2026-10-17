using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// 상태창 overlay: the protagonist's stats (gear included), equipment, equipped arts and the fighting party.
    /// Laid out in a scrolling list with fixed pixel row heights and fixed font sizes, so every row lines up and stays readable.
    /// </summary>
    public sealed class StatusPanel : OverlayPanel
    {
        private static readonly Color Green = new Color(0.45f, 0.85f, 0.5f, 1f);
        private const float Gap = 14f, SectionH = 64f;

        private RectTransform _content;
        private float _y;

        public StatusPanel(RectTransform root, Action onChange) : base(root, onChange) { }

        public void Open()
        {
            var save = GameManager.Instance.Save;
            Begin();
            UiFactory.Label("Title", Root, new Vector2(0.05f, 0.935f), new Vector2(0.95f, 0.995f), "상태창", 44, UiFactory.Gold);
            _content = CodexPanel.Scroller(Root, 0.075f, 0.93f);
            _y = Gap;

            // Header: portrait + name, level, rank, exp
            var head = Block(150f);
            var pic = UiFactory.Panel("Pic", head, new Vector2(0.02f, 0.06f), new Vector2(0.20f, 0.94f), ArtLibrary.TintFor("비류연"));
            var sprite = ArtLibrary.Player();
            if (sprite != null) { pic.sprite = sprite; pic.color = Color.white; pic.preserveAspect = true; }
            else UiFactory.Body(UiFactory.Label("Glyph", pic.transform, Vector2.zero, Vector2.one, "비", 56, UiFactory.PaperText));
            long need = Progression.ExpToNext(save.Level);
            Text(head, 0.23f, 0.97f, 0.50f, 0.96f, "비류연  Lv." + save.Level + "  ·  " + Promotion.RankName(Promotion.EffectiveRank(save)), 38, UiFactory.Gold);
            Text(head, 0.23f, 0.97f, 0.06f, 0.50f, "경험 " + save.Exp.ToString("N0") + " / " + need.ToString("N0") + "\n두 " + save.Doo.ToString("N0"), 28, UiFactory.PaperText, TextAnchor.UpperLeft);

            // 무공 경지: composite stat power placed on the realm ladder (체력·공격·방어·속도, gear included)
            Section("무공 경지");
            double power = Realm.Power(InnerArts.HeroStatsBeforeRealm(save));
            int realmStep = Realm.StepOf(power);
            var realmCard = Block(150f);
            Text(realmCard, 0.04f, 0.96f, 0.52f, 0.98f, Realm.Label(realmStep), 40, UiFactory.Gold, TextAnchor.MiddleLeft);
            string nextText = Realm.NextText(power);
            Text(realmCard, 0.04f, 0.96f, 0.04f, 0.52f, "종합 수치 " + Math.Round(power).ToString("N0") + (nextText.Length > 0 ? "\n" + nextText : "\n더 오를 경지가 없습니다"), 26, UiFactory.PaperText, TextAnchor.UpperLeft);
            var realmNote = Block(72f);
            Text(realmNote, 0.04f, 0.97f, 0f, 1f, "체력·공격력·방어력·속도를 합친 수치(장비·심법 반영)로 정한 경지입니다. 경지가 한 칸 오를 때마다 모든 능력치가 " + Mathf.RoundToInt(Realm.BonusPerStep * 100f) + "%씩 오릅니다(지금 +" + Mathf.RoundToInt(Realm.Bonus(realmStep) * 100f) + "%).", 22, UiFactory.Dim, TextAnchor.MiddleLeft);

            // Stats: two columns of equal rows
            Section("능력치");
            var stats = StatusSheet.Hero(save);
            for (int i = 0; i < stats.Count; i += 2)
            {
                var row = Place(84f, false);
                StatCell(row, stats[i], 0f, 0.495f);
                if (i + 1 < stats.Count) StatCell(row, stats[i + 1], 0.505f, 1f);
                _y += 84f + Gap;
            }

            // Gear
            Section(GearTitle(save));
            GearCard("무기", StatusSheet.Weapon(save));
            GearCard("방어구", StatusSheet.Armor(save));
            var accs = StatusSheet.Accessories(save);
            for (int i = 0; i < Inventory.AccessorySlots; i++) GearCard("장신구 " + (i + 1), i < accs.Count ? accs[i] : null);

            // Arts
            Section("장착 무공");
            var artLines = StatusSheet.ArtLines(save);
            if (StatusSheet.InnerLine(save).Length > 0) artLines.Add(StatusSheet.InnerLine(save));
            if (artLines.Count == 0) artLines.Add("장착한 무공이 없습니다");
            foreach (var line in artLines)
            {
                var block = Block(72f);
                Text(block, 0.04f, 0.97f, 0f, 1f, line, 28, UiFactory.PaperText, TextAnchor.MiddleLeft);
            }

            // Party
            var party = EarlyGameContent.SelectCompanionsFor(Math.Max(1, save.HighestUnlockedStage), save);
            if (party.Count > 0)
            {
                Section("출전 동료");
                foreach (var c in party)
                {
                    var l = StatusSheet.Ally(c, save.Level, Bonds.Level(save, c.Id), CompanionTraining.LevelOf(save, c.Id));
                    var block = Block(130f);
                    Text(block, 0.04f, 0.97f, 0.52f, 0.97f, c.Name, 32, UiFactory.Gold, TextAnchor.MiddleLeft);
                    Text(block, 0.04f, 0.97f, 0.05f, 0.52f, "체력 " + l[0].Value + "   내공 " + l[1].Value + "   공격 " + l[2].Value + "   방어 " + l[3].Value, 28, UiFactory.PaperText, TextAnchor.MiddleLeft);
                }
            }

            _content.sizeDelta = new Vector2(0f, _y + Gap);
            UiFactory.Button("Close", Root, new Vector2(0.3f, 0.01f), new Vector2(0.7f, 0.065f), "닫기", UiFactory.InkSoft, () => { Root.gameObject.SetActive(false); OnChange(); }, 34);
        }

        // ------------------------------------------------------------------ layout helpers (pixel rows inside the scroll content)

        /// <summary>A framed row of the given height at the current offset; advances the offset.</summary>
        private RectTransform Block(float h)
        {
            var rt = Place(h, true);
            _y += h + Gap;
            return rt;
        }

        private RectTransform Place(float h, bool framed)
        {
            var rt = UiFactory.Rect("Row", _content, new Vector2(0.04f, 1f), new Vector2(0.96f, 1f));
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -_y);
            rt.sizeDelta = new Vector2(0f, h);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = framed ? UiFactory.Ink : new Color(0f, 0f, 0f, 0f);
            bg.raycastTarget = true;   // lets a drag that starts on a row still scroll
            if (framed) UiFactory.Frame(rt, UiFactory.GoldDim, 2f);
            return rt;
        }

        private void Section(string title)
        {
            var rt = Place(SectionH, false);
            Text(rt, 0.02f, 1f, 0f, 1f, title, 34, UiFactory.Gold, TextAnchor.MiddleLeft);
            _y += SectionH + Gap * 0.5f;
        }

        /// <summary>A fixed-size, wrapping text (no shrink-to-fit) inside a row.</summary>
        private static Text Text(RectTransform parent, float x0, float x1, float y0, float y1, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            return UiFactory.Body(UiFactory.Label("Text", parent, new Vector2(x0, y0), new Vector2(x1, y1), text, size, color, anchor, FontStyle.Normal), 1.2f);
        }

        private static void StatCell(RectTransform row, StatLine line, float x0, float x1)
        {
            var cell = UiFactory.Panel("Stat", row, new Vector2(x0, 0f), new Vector2(x1, 1f), UiFactory.Ink);
            UiFactory.Frame(cell.transform, UiFactory.GoldDim, 2f);
            Text(cell.rectTransform, 0.05f, 0.40f, 0f, 1f, line.Label, 28, UiFactory.Dim);
            Text(cell.rectTransform, 0.40f, 0.95f, 0.45f, 1f, line.Value, 34, UiFactory.PaperText, TextAnchor.MiddleRight);
            if (!string.IsNullOrEmpty(line.Bonus))
                Text(cell.rectTransform, 0.40f, 0.95f, 0f, 0.45f, "장비 " + line.Bonus, 22, Green, TextAnchor.MiddleRight);
        }

        private void GearCard(string slot, ItemData item)
        {
            var save = GameManager.Instance.Save;
            bool has = item != null;
            var card = Block(has ? 150f : 84f);
            UiFactory.Frame(card, has ? UiFactory.GoldDim : new Color(0.25f, 0.25f, 0.28f, 1f), 2f);
            string name = has ? UiFactory.GradedName(item) + (Gear.Level(save, item.Id) > 0 ? " +" + Gear.Level(save, item.Id) : "") : "비어 있음";
            Text(card, 0.04f, 0.26f, has ? 0.58f : 0f, 1f, slot, 26, UiFactory.Dim);
            Text(card, 0.27f, has ? 0.83f : 0.97f, has ? 0.58f : 0f, 1f, name, 32, has ? UiFactory.PaperText : UiFactory.InkSoft);
            if (has)
            {
                Text(card, 0.04f, 0.83f, 0.04f, 0.58f, StatusSheet.EffectText(item, Gear.Level(save, item.Id)), 24, UiFactory.PaperText, TextAnchor.UpperLeft);
                var pic = UiFactory.Panel("Picture", card, new Vector2(0.85f, 0.08f), new Vector2(0.98f, 0.92f), Color.white);
                var glyph = UiFactory.Label("Glyph", pic.transform, Vector2.zero, Vector2.one, "", 40, UiFactory.PaperText);
                ItemArt.Apply(pic, glyph, item);
            }
        }

        private static string GearTitle(SaveData save)
        {
            var parts = new List<string>();
            foreach (var set in Gear.Sets)
            {
                int n = Gear.WornPieces(set, save.EquippedGear);
                if (n >= 2) parts.Add(set.Name + " 세트 " + n + "/" + set.Items.Length);
            }
            return parts.Count == 0 ? "장비" : "장비   <color=#9ADB9F>" + string.Join(" · ", parts) + "</color>";
        }
    }
}
