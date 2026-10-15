using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>One row of the 상태창: a label, the value with gear applied, and the part that comes from gear ("" when none).</summary>
    public sealed class StatLine
    {
        public string Label, Value, Bonus;
    }

    /// <summary>Builds the text of the 상태창 (stat sheet) from the save, so the numbers match what the battle uses.</summary>
    public static class StatusSheet
    {
        private static StatLine Line(string label, int baseValue, int finalValue)
        {
            int diff = finalValue - baseValue;
            return new StatLine { Label = label, Value = finalValue.ToString("N0"), Bonus = diff == 0 ? "" : (diff > 0 ? "+" : "") + diff };
        }

        /// <summary>The protagonist's stats with equipped gear applied.</summary>
        public static List<StatLine> Hero(SaveData save)
        {
            var b = Progression.HeroStatsFor(save.Level);
            var f = Inventory.ApplyGear(b, save.EquippedGear);
            var list = new List<StatLine>
            {
                Line("체력", b.MaxHp, f.MaxHp),
                Line("내공", b.MaxQi, f.MaxQi),
                Line("공격력", b.Attack, f.Attack),
                Line("방어력", b.Defense, f.Defense),
                Line("속도", b.Speed, f.Speed),
                new StatLine { Label = "치명타율", Value = Math.Round(f.CritRate * 100f, 1) + "%", Bonus = f.CritRate > b.CritRate + 0.0001f ? "+" + Math.Round((f.CritRate - b.CritRate) * 100f, 1) + "%" : "" },
                new StatLine { Label = "치명타 피해", Value = "x" + f.CritDamage.ToString("0.0"), Bonus = "" },
            };
            return list;
        }

        /// <summary>A companion's stats at the current level (companions do not wear gear).</summary>
        public static List<StatLine> Ally(CompanionData c, int level)
        {
            var s = c.CreateCombatant(level).Stats;
            return new List<StatLine>
            {
                new StatLine { Label = "체력", Value = s.MaxHp.ToString("N0"), Bonus = "" },
                new StatLine { Label = "내공", Value = s.MaxQi.ToString("N0"), Bonus = "" },
                new StatLine { Label = "공격력", Value = s.Attack.ToString("N0"), Bonus = "" },
                new StatLine { Label = "방어력", Value = s.Defense.ToString("N0"), Bonus = "" },
                new StatLine { Label = "속도", Value = s.Speed.ToString("N0"), Bonus = "" },
            };
        }

        /// <summary>"공격 +10%  방어 +10%  속도 -6" for a piece of gear.</summary>
        public static string EffectText(ItemData item)
        {
            var parts = new List<string>();
            if (item.HpPct != 0f) parts.Add("체력 " + Pct(item.HpPct));
            if (item.QiPct != 0f) parts.Add("내공 " + Pct(item.QiPct));
            if (item.AtkPct != 0f) parts.Add("공격 " + Pct(item.AtkPct));
            if (item.DefPct != 0f) parts.Add("방어 " + Pct(item.DefPct));
            if (item.SpeedAdd != 0) parts.Add("속도 " + (item.SpeedAdd > 0 ? "+" : "") + item.SpeedAdd);
            if (item.CritAdd != 0f) parts.Add("치명타 " + Pct(item.CritAdd));
            return parts.Count == 0 ? "특별한 효과 없음" : string.Join("  ", parts);
        }

        private static string Pct(float v) { return (v > 0 ? "+" : "") + Math.Round(v * 100f) + "%"; }

        /// <summary>The equipped weapon (null when none).</summary>
        public static ItemData Weapon(SaveData save)
        {
            foreach (var id in save.EquippedGear) { var it = EarlyGameContent.FindItem(id); if (it != null && it.Kind == ItemKind.Weapon) return it; }
            return null;
        }

        /// <summary>Equipped accessories in equip order (at most <see cref="Inventory.AccessorySlots"/>).</summary>
        public static List<ItemData> Accessories(SaveData save)
        {
            var list = new List<ItemData>();
            foreach (var id in save.EquippedGear) { var it = EarlyGameContent.FindItem(id); if (it != null && it.Kind == ItemKind.Accessory) list.Add(it); }
            return list;
        }

        /// <summary>Equipped martial arts with their mastery, e.g. "분뢰수  ★★☆☆ 소성 (강)".</summary>
        public static List<string> ArtLines(SaveData save)
        {
            var list = new List<string>();
            foreach (var id in save.EquippedArts)
            {
                var art = EarlyGameContent.FindArt(id);
                if (art == null) continue;
                int lv = Mastery.Level(save, id);
                var sb = new System.Text.StringBuilder(art.Name + "  ");
                for (int i = 0; i < Mastery.Thresholds.Length; i++) sb.Append(i <= lv ? "★" : "☆");
                sb.Append(" ").Append(Mastery.LevelNames[lv]);
                var v = Mastery.VariantOf(save, id);
                if (v != ArtVariant.None) sb.Append(" (").Append(Mastery.VariantName(v)).Append(")");
                list.Add(sb.ToString());
            }
            return list;
        }
    }
}
