using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>상태창 overlay: the protagonist's stats (gear included), equipment, equipped arts and the fighting party.</summary>
    public sealed class StatusPanel : OverlayPanel
    {
        public StatusPanel(RectTransform root, Action onChange) : base(root, onChange) { }

        public void Open()
        {
            var save = GameManager.Instance.Save;
            Begin();
            UiFactory.Label("Title", Root, new Vector2(0.05f, 0.935f), new Vector2(0.95f, 0.995f), "상태창", 44, UiFactory.Gold);

            // Header: portrait block + name, level, rank, exp
            var head = UiFactory.Panel("Head", Root, new Vector2(0.04f, 0.835f), new Vector2(0.96f, 0.93f), UiFactory.Ink);
            UiFactory.Frame(head.transform, UiFactory.GoldDim, 2f);
            var pic = UiFactory.Panel("Pic", head.transform, new Vector2(0.02f, 0.08f), new Vector2(0.17f, 0.92f), ArtLibrary.TintFor("비류연"));
            var sprite = ArtLibrary.Player();
            if (sprite != null) { pic.sprite = sprite; pic.color = Color.white; pic.preserveAspect = true; }
            else UiFactory.Label("Glyph", pic.transform, Vector2.zero, Vector2.one, "비", 50, UiFactory.PaperText);
            long need = Progression.ExpToNext(save.Level);
            UiFactory.Label("Name", head.transform, new Vector2(0.20f, 0.52f), new Vector2(0.96f, 0.96f), "비류연  Lv." + save.Level + "  ·  " + Promotion.RankName(Promotion.EffectiveRank(save)), 34, UiFactory.Gold, TextAnchor.MiddleLeft);
            UiFactory.Label("Exp", head.transform, new Vector2(0.20f, 0.06f), new Vector2(0.96f, 0.50f), "경험 " + save.Exp.ToString("N0") + " / " + need.ToString("N0") + "   ·   두 " + save.Doo.ToString("N0"), 24, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);

            // Stats: two columns
            var stats = StatusSheet.Hero(save);
            float top = 0.815f, rowH = 0.048f;
            for (int i = 0; i < stats.Count; i++)
            {
                int col = i % 2, row = i / 2;
                float x0 = col == 0 ? 0.04f : 0.52f;
                float y1 = top - row * (rowH + 0.004f);
                StatRow(Root, stats[i], x0, x0 + 0.44f, y1 - rowH, y1);
            }

            // Gear
            float gy = top - 4 * (rowH + 0.004f) - 0.01f;
            UiFactory.Label("GearTitle", Root, new Vector2(0.05f, gy - 0.045f), new Vector2(0.95f, gy), GearTitle(save), 30, UiFactory.Gold, TextAnchor.MiddleLeft);
            gy -= 0.047f;
            GearRow("무기", StatusSheet.Weapon(save), gy);
            gy -= 0.054f;
            GearRow("방어구", StatusSheet.Armor(save), gy);
            gy -= 0.054f;
            var accs = StatusSheet.Accessories(save);
            for (int i = 0; i < Inventory.AccessorySlots; i++)
            {
                GearRow("장신구 " + (i + 1), i < accs.Count ? accs[i] : null, gy);
                gy -= 0.054f;
            }

            // Arts
            UiFactory.Label("ArtsTitle", Root, new Vector2(0.05f, gy - 0.036f), new Vector2(0.95f, gy), "장착 무공", 32, UiFactory.Gold, TextAnchor.MiddleLeft);
            gy -= 0.036f;
            var artLines = StatusSheet.ArtLines(save);
            if (StatusSheet.InnerLine(save).Length > 0) artLines.Add(StatusSheet.InnerLine(save));
            UiFactory.Label("Arts", Root, new Vector2(0.06f, gy - 0.07f), new Vector2(0.94f, gy), artLines.Count == 0 ? "장착한 무공이 없다" : string.Join("\n", artLines), 22, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal);
            gy -= 0.075f;

            // Party
            var party = EarlyGameContent.SelectCompanionsFor(Math.Max(1, save.HighestUnlockedStage), save);
            if (party.Count > 0)
            {
                UiFactory.Label("PartyTitle", Root, new Vector2(0.05f, gy - 0.036f), new Vector2(0.95f, gy), "출전 동료", 32, UiFactory.Gold, TextAnchor.MiddleLeft);
                gy -= 0.038f;
                foreach (var c in party)
                {
                    var l = StatusSheet.Ally(c, save.Level, Bonds.Level(save, c.Id), CompanionTraining.LevelOf(save, c.Id));
                    UiFactory.Label("Ally_" + c.Id, Root, new Vector2(0.06f, gy - 0.033f), new Vector2(0.94f, gy),
                        c.Name + "   체력 " + l[0].Value + "  내공 " + l[1].Value + "  공격 " + l[2].Value + "  방어 " + l[3].Value, 22, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                    gy -= 0.033f;
                }
            }

            UiFactory.Button("Close", Root, new Vector2(0.3f, 0.008f), new Vector2(0.7f, 0.056f), "닫기", UiFactory.InkSoft, () => { Root.gameObject.SetActive(false); OnChange(); }, 32);
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

        private static void StatRow(RectTransform parent, StatLine line, float x0, float x1, float y0, float y1)
        {
            var band = UiFactory.Panel("Stat", parent, new Vector2(x0, y0), new Vector2(x1, y1), UiFactory.Ink);
            UiFactory.Frame(band.transform, UiFactory.GoldDim, 2f);
            UiFactory.Label("Label", band.transform, new Vector2(0.05f, 0f), new Vector2(0.42f, 1f), line.Label, 26, UiFactory.Dim, TextAnchor.MiddleLeft);
            UiFactory.Label("Value", band.transform, new Vector2(0.42f, 0f), new Vector2(0.78f, 1f), line.Value, 30, UiFactory.PaperText, TextAnchor.MiddleRight);
            if (!string.IsNullOrEmpty(line.Bonus))
                UiFactory.Label("Bonus", band.transform, new Vector2(0.78f, 0f), new Vector2(0.98f, 1f), "(" + line.Bonus + ")", 20, new Color(0.45f, 0.85f, 0.5f, 1f), TextAnchor.MiddleLeft, FontStyle.Normal);
        }

        private void GearRow(string slot, ItemData item, float y1)
        {
            var band = UiFactory.Panel("Gear", Root, new Vector2(0.04f, y1 - 0.05f), new Vector2(0.96f, y1), UiFactory.Ink);
            UiFactory.Frame(band.transform, item != null ? UiFactory.GoldDim : new Color(0.25f, 0.25f, 0.28f, 1f), 2f);
            UiFactory.Label("Slot", band.transform, new Vector2(0.03f, 0f), new Vector2(0.20f, 1f), slot, 22, UiFactory.Dim, TextAnchor.MiddleLeft);
            UiFactory.Label("Name", band.transform, new Vector2(0.20f, 0.45f), new Vector2(0.97f, 1f), item != null ? UiFactory.GradedName(item) + (Gear.Level(GameManager.Instance.Save, item.Id) > 0 ? " +" + Gear.Level(GameManager.Instance.Save, item.Id) : "") : "비어 있음", 26, item != null ? UiFactory.PaperText : UiFactory.InkSoft, TextAnchor.MiddleLeft);
            if (item != null)
                UiFactory.Label("Effect", band.transform, new Vector2(0.20f, 0f), new Vector2(0.97f, 0.48f), StatusSheet.EffectText(item, Gear.Level(GameManager.Instance.Save, item.Id)), 20, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
        }
    }
}
