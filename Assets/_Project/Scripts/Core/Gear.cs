using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>Persisted enhancement level of one piece of gear.</summary>
    [Serializable]
    public sealed class GearLevel
    {
        public string Id;
        public int Level;
    }

    /// <summary>A stat bonus that applies once enough pieces of a set are worn.</summary>
    public sealed class SetTier
    {
        public int Pieces;
        public float AtkPct, DefPct, HpPct, QiPct, CritAdd;
        public int SpeedAdd;
    }

    public sealed class GearSet
    {
        public string Id, Name;
        public string[] Items;
        public SetTier[] Tiers;
    }

    /// <summary>
    /// 장비 강화 and 세트 효과. Enhancing costs 두 and raises a piece's stats by 10% of its base per level (max +10).
    /// Wearing several pieces of one themed set adds a bonus (set bonuses are not scaled by enhancement).
    /// </summary>
    public static class Gear
    {
        public const int MaxLevel = 10;
        public const float PerLevel = 0.10f;
        private static readonly long[] CostBase = { 75, 150, 300, 600, 1200, 2400 };   // by grade; the n-th level costs base x n

        public static int LevelOf(IList<GearLevel> levels, string id)
        {
            if (levels == null) return 0;
            foreach (var l in levels) if (l.Id == id) return l.Level;
            return 0;
        }

        public static int Level(SaveData save, string id) { return LevelOf(save.GearLevels, id); }

        public static float Multiplier(int level) { return 1f + PerLevel * Math.Max(0, Math.Min(MaxLevel, level)); }

        /// <summary>두 for the step to <paramref name="toLevel"/>, or -1 when out of range.</summary>
        public static long CostFor(ItemData item, int toLevel)
        {
            if (item == null || !item.IsGear || toLevel < 1 || toLevel > MaxLevel) return -1;
            return CostBase[(int)item.Grade] * toLevel;
        }

        /// <summary>두 for the next step of this piece; -1 at max level or when not enhanceable.</summary>
        public static long NextCost(SaveData save, string id)
        {
            return CostFor(EarlyGameContent.FindItem(id), Level(save, id) + 1);
        }

        public static bool Enhance(SaveData save, string id, out string error)
        {
            error = null;
            var item = EarlyGameContent.FindItem(id);
            if (item == null || !item.IsGear || Inventory.Count(save, id) <= 0) { error = "강화할 수 없는 물건이다."; return false; }
            int cur = Level(save, id);
            if (cur >= MaxLevel) { error = "이미 최대 강화다."; return false; }
            long cost = CostFor(item, cur + 1);
            if (save.Doo < cost) { error = "두가 모자란다. (" + cost.ToString("N0") + "두 필요)"; return false; }
            save.Doo -= cost;
            GearLevel entry = null;
            foreach (var l in save.GearLevels) if (l.Id == id) entry = l;
            if (entry == null) { entry = new GearLevel { Id = id }; save.GearLevels.Add(entry); }
            entry.Level = cur + 1;
            return true;
        }

        // ------------------------------------------------------------------ sets

        private static SetTier T(int pieces, float atk = 0f, float def = 0f, float hp = 0f, float qi = 0f, float crit = 0f, int speed = 0)
        {
            return new SetTier { Pieces = pieces, AtkPct = atk, DefPct = def, HpPct = hp, QiPct = qi, CritAdd = crit, SpeedAdd = speed };
        }

        public static readonly GearSet[] Sets =
        {
            new GearSet { Id = "training", Name = "수련 시절", Items = new[] { "laundry_bat", "cloth_robe", "mukryonghwan", "claw_necklace" },
                Tiers = new[] { T(2, atk: 0.03f), T(3, hp: 0.05f), T(4, atk: 0.04f, def: 0.04f) } },
            new GearSet { Id = "ironwall", Name = "철벽", Items = new[] { "dumok_spear", "mountain_armor", "iron_bracer", "bronze_gauntlet" },
                Tiers = new[] { T(2, def: 0.04f), T(3, hp: 0.06f), T(4, def: 0.06f, atk: 0.03f) } },
            new GearSet { Id = "galewind", Name = "질풍", Items = new[] { "windcut_sword", "silk_battle_robe", "wind_boots", "straw_sandals" },
                Tiers = new[] { T(2, speed: 2), T(3, crit: 0.03f), T(4, atk: 0.04f, speed: 3) } },
            new GearSet { Id = "frost", Name = "한빙", Items = new[] { "frost_sword", "blackturtle_cloak", "snowlotus_charm", "moonlight_pendant" },
                Tiers = new[] { T(2, def: 0.04f), T(3, crit: 0.03f), T(4, atk: 0.06f, qi: 0.10f) } },
            new GearSet { Id = "flame", Name = "적염", Items = new[] { "crimson_saber", "crimson_longcoat", "phoenix_feather", "bloodjade_bracelet" },
                Tiers = new[] { T(2, atk: 0.04f), T(3, hp: 0.06f), T(4, atk: 0.08f, crit: 0.04f) } },
            new GearSet { Id = "thunder", Name = "뇌전", Items = new[] { "thunder_dagger", "thunder_beads", "thunder_crown" },
                Tiers = new[] { T(2, crit: 0.03f), T(3, atk: 0.08f, speed: 2) } },
            new GearSet { Id = "dragon", Name = "용", Items = new[] { "dragonfang_sword", "dragonscale_armor", "dragon_pearl" },
                Tiers = new[] { T(2, hp: 0.05f), T(3, atk: 0.08f, def: 0.08f) } },
            new GearSet { Id = "biroe", Name = "비뢰", Items = new[] { "jin_biroedo", "roegeum", "mukrin_talisman" },
                Tiers = new[] { T(2, atk: 0.04f, crit: 0.02f), T(3, atk: 0.10f, speed: 3) } },
            new GearSet { Id = "emperor", Name = "제왕", Items = new[] { "heaven_cutter", "taegeuk_jade", "emperor_seal" },
                Tiers = new[] { T(2, atk: 0.05f, hp: 0.05f), T(3, atk: 0.10f, def: 0.08f, hp: 0.08f) } },
        };

        public static GearSet SetOf(string itemId)
        {
            foreach (var s in Sets) if (Array.IndexOf(s.Items, itemId) >= 0) return s;
            return null;
        }

        public static int WornPieces(GearSet set, IEnumerable<string> equipped)
        {
            int n = 0;
            foreach (var id in equipped) if (Array.IndexOf(set.Items, id) >= 0) n++;
            return n;
        }

        /// <summary>The summed bonus of every tier reached by the worn pieces of one set.</summary>
        public static SetTier ActiveBonus(GearSet set, int worn)
        {
            var sum = new SetTier { Pieces = worn };
            foreach (var t in set.Tiers)
            {
                if (worn < t.Pieces) continue;
                sum.AtkPct += t.AtkPct; sum.DefPct += t.DefPct; sum.HpPct += t.HpPct; sum.QiPct += t.QiPct; sum.CritAdd += t.CritAdd; sum.SpeedAdd += t.SpeedAdd;
            }
            return sum;
        }

        /// <summary>Text of a tier's bonus, e.g. "공격 +4%  치명타 +2%".</summary>
        public static string TierText(SetTier t)
        {
            var parts = new List<string>();
            if (t.HpPct != 0f) parts.Add("체력 +" + Math.Round(t.HpPct * 100f) + "%");
            if (t.QiPct != 0f) parts.Add("내공 +" + Math.Round(t.QiPct * 100f) + "%");
            if (t.AtkPct != 0f) parts.Add("공격 +" + Math.Round(t.AtkPct * 100f) + "%");
            if (t.DefPct != 0f) parts.Add("방어 +" + Math.Round(t.DefPct * 100f) + "%");
            if (t.SpeedAdd != 0) parts.Add("속도 +" + t.SpeedAdd);
            if (t.CritAdd != 0f) parts.Add("치명타 +" + Math.Round(t.CritAdd * 100f) + "%");
            return string.Join("  ", parts);
        }

        /// <summary>One line per set that has at least one worn piece, with the worn count and the bonus reached so far.</summary>
        public static List<string> SetLines(IEnumerable<string> equipped)
        {
            var lines = new List<string>();
            foreach (var s in Sets)
            {
                int worn = WornPieces(s, equipped);
                if (worn == 0) continue;
                var tier = ActiveBonus(s, worn);
                string text = TierText(tier);
                lines.Add(s.Name + " 세트 " + worn + "/" + s.Items.Length + (text.Length > 0 ? "  —  " + text : "  (2개부터 효과)"));
            }
            return lines;
        }
    }
}
