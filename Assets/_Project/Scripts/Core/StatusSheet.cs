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
            var f = InnerArts.HeroStats(save);   // gear, 검룡위, 심법 and the 경지 bonus, like in battle
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
        public static List<StatLine> Ally(CompanionData c, int level, int bondLevel = 0, int trainLevel = 0)
        {
            var s = c.CreateCombatant(level, bondLevel, trainLevel).Stats;
            return new List<StatLine>
            {
                new StatLine { Label = "체력", Value = s.MaxHp.ToString("N0"), Bonus = "" },
                new StatLine { Label = "내공", Value = s.MaxQi.ToString("N0"), Bonus = "" },
                new StatLine { Label = "공격력", Value = s.Attack.ToString("N0"), Bonus = "" },
                new StatLine { Label = "방어력", Value = s.Defense.ToString("N0"), Bonus = "" },
                new StatLine { Label = "속도", Value = s.Speed.ToString("N0"), Bonus = "" },
            };
        }

        /// <summary>Same as <see cref="Ally(CompanionData,int,int,int)"/> but with the companion's own level and gear from the save.</summary>
        public static List<StatLine> Ally(SaveData save, CompanionData c)
        {
            var s = CompanionGrowth.Create(save, c).Stats;
            return new List<StatLine>
            {
                new StatLine { Label = "체력", Value = s.MaxHp.ToString("N0"), Bonus = "" },
                new StatLine { Label = "내공", Value = s.MaxQi.ToString("N0"), Bonus = "" },
                new StatLine { Label = "공격력", Value = s.Attack.ToString("N0"), Bonus = "" },
                new StatLine { Label = "방어력", Value = s.Defense.ToString("N0"), Bonus = "" },
                new StatLine { Label = "속도", Value = s.Speed.ToString("N0"), Bonus = "" },
            };
        }

        /// <summary>"용아검  적운 장포  ...": the names of the gear a companion wears, or a hint when nothing is worn.</summary>
        public static string GearNames(SaveData save, string owner)
        {
            var names = new List<string>();
            foreach (var id in Inventory.GearOf(save, owner))
            {
                var it = EarlyGameContent.FindItem(id);
                int lv = Gear.LevelFor(save, owner, id);
                if (it != null) names.Add(it.Name + (lv > 0 ? " +" + lv : ""));
            }
            return names.Count == 0 ? "착용한 장비가 없습니다 (상태창에서 부위를 눌러 장착합니다)" : string.Join("  ", names);
        }

        public static string AllyRealm(SaveData save, CompanionData c)
        {
            double power = Realm.Power(CompanionGrowth.Create(save, c).Stats);
            return Realm.Label(Realm.StepOf(power)) + " (종합 " + Math.Round(power).ToString("N0") + ")";
        }

        /// <summary>"삼급 (종합 57)": where a companion's composite stat power sits on the 무공 경지 ladder (the same stats the fight uses, no realm bonus).</summary>
        public static string AllyRealm(CompanionData c, int level, int bondLevel = 0, int trainLevel = 0)
        {
            double power = Realm.Power(c.CreateCombatant(level, bondLevel, trainLevel).Stats);
            return Realm.Label(Realm.StepOf(power)) + " (종합 " + Math.Round(power).ToString("N0") + ")";
        }

        /// <summary>"공격 +10%  방어 +10%  속도 -6" for a piece of gear.</summary>
        public static string EffectText(ItemData item, int enhance = 0, int stars = 0)
        {
            var effects = Gear.EffectsFor(item, enhance, stars);
            var parts = new List<string>();
            if (effects.HpPct != 0f) parts.Add("체력 " + Pct(effects.HpPct));
            if (effects.QiPct != 0f) parts.Add("내공 " + Pct(effects.QiPct));
            if (effects.AtkPct != 0f) parts.Add("공격 " + Pct(effects.AtkPct));
            if (effects.DefPct != 0f) parts.Add("방어 " + Pct(effects.DefPct));
            if (effects.SpeedAdd != 0) { int sp = (int)Math.Round(effects.SpeedAdd); parts.Add("속도 " + (sp > 0 ? "+" : "") + sp); }
            if (effects.CritAdd != 0f) parts.Add("치명타 " + Pct(effects.CritAdd));
            if (effects.CritDmgAdd != 0f) parts.Add("치명타 피해 " + Pct(effects.CritDmgAdd));
            if (effects.StartSpirit != 0) parts.Add("시작 기세 +" + effects.StartSpirit);
            return parts.Count == 0 ? "특별한 효과 없음" : string.Join("  ", parts);
        }

        /// <summary>The flavor part of a description. Gear descriptions end with their own effect sentence ("공격 +8%."), which the UI already shows separately.</summary>
        public static string Flavor(ItemData item)
        {
            string d = (item.Description ?? "").Trim();
            if (!item.IsGear) return d;
            string body = d.TrimEnd('.');
            int cut = body.LastIndexOf(". ", StringComparison.Ordinal);
            string last = cut >= 0 ? body.Substring(cut + 2) : body;
            if (last.IndexOf('%') < 0 && last.IndexOf('+') < 0 && last.IndexOf('-') < 0) return d;
            return cut >= 0 ? body.Substring(0, cut) + "." : "";
        }

        private static string Pct(float v) { return (v > 0 ? "+" : "") + Math.Round(v * 100f, 1) + "%"; }

        /// <summary>The equipped weapon (null when none).</summary>
        public static ItemData Weapon(SaveData save)
        {
            foreach (var id in save.EquippedGear) { var it = EarlyGameContent.FindItem(id); if (it != null && it.Kind == ItemKind.Weapon) return it; }
            return null;
        }

        /// <summary>The equipped 상의 (null when none).</summary>
        public static ItemData Armor(SaveData save)
        {
            foreach (var id in save.EquippedGear) { var it = EarlyGameContent.FindItem(id); if (it != null && it.Kind == ItemKind.Armor) return it; }
            return null;
        }

        public static ItemData Pants(SaveData save) { return OfKind(save, ItemKind.Pants); }

        public static ItemData Boots(SaveData save) { return OfKind(save, ItemKind.Boots); }

        private static ItemData OfKind(SaveData save, ItemKind kind)
        {
            foreach (var id in save.EquippedGear) { var it = EarlyGameContent.FindItem(id); if (it != null && it.Kind == kind) return it; }
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
        /// <summary>"심법: 강건심법, 맹호심법" or "" when none is equipped.</summary>
        public static string InnerLine(SaveData save)
        {
            var names = new List<string>();
            foreach (var id in save.EquippedInner) { var a = InnerArts.Find(id); if (a != null) names.Add(a.Name); }
            return names.Count == 0 ? "" : "심법: " + string.Join(", ", names);
        }

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
