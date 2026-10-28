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
        public int Stars;
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

    /// <summary>Equipment slot enhancement, item breakthroughs and unscaled set bonuses.</summary>
    public static partial class Gear
    {
        public static int LevelOf(IList<GearLevel> levels, string id)
        {
            if (levels == null) return 0;
            foreach (var l in levels) if (l.Id == id) return l.Level;
            return 0;
        }

        // ------------------------------------------------------------------ sets

        private static SetTier T(int pieces, float atk = 0f, float def = 0f, float hp = 0f, float qi = 0f, float crit = 0f, int speed = 0)
        {
            return new SetTier { Pieces = pieces, AtkPct = atk, DefPct = def, HpPct = hp, QiPct = qi, CritAdd = crit, SpeedAdd = speed };
        }

        // A set can list up to six pieces (weapon, 상의, 하의, 신발, two accessories); a Transcendent weapon may stand beside the weapon it replaces.
        public static readonly GearSet[] Sets =
        {
            new GearSet { Id = "training", Name = "수련 시절", Items = new[] { "laundry_bat", "cloth_robe", "train_pants", "straw_sandals", "mukryonghwan", "claw_necklace" },
                Tiers = new[] { T(2, atk: 0.02f), T(3, hp: 0.04f), T(4, def: 0.03f, hp: 0.03f), T(5, atk: 0.03f, speed: 1), T(6, atk: 0.04f, def: 0.04f, hp: 0.04f) } },
            new GearSet { Id = "ironwall", Name = "철벽", Items = new[] { "dumok_spear", "mountain_armor", "ironwall_greaves", "ironwall_boots", "iron_bracer", "bronze_gauntlet" },
                Tiers = new[] { T(2, def: 0.04f), T(3, hp: 0.05f), T(4, def: 0.05f, hp: 0.04f), T(5, atk: 0.03f, def: 0.05f), T(6, atk: 0.04f, def: 0.08f, hp: 0.08f) } },
            new GearSet { Id = "galewind", Name = "질풍", Items = new[] { "windcut_sword", "silk_battle_robe", "gale_pants", "wind_boots", "gale_feather" },
                Tiers = new[] { T(2, speed: 2), T(3, crit: 0.03f), T(4, atk: 0.04f, speed: 3), T(5, atk: 0.05f, crit: 0.03f, speed: 3) } },
            new GearSet { Id = "frost", Name = "한빙", Items = new[] { "frost_sword", "blackturtle_cloak", "frost_pants", "frost_boots", "snowlotus_charm", "moonlight_pendant" },
                Tiers = new[] { T(2, def: 0.04f), T(3, crit: 0.03f), T(4, atk: 0.05f, qi: 0.015f), T(5, def: 0.06f, crit: 0.03f), T(6, atk: 0.06f, def: 0.06f, qi: 0.018f) } },
            new GearSet { Id = "flame", Name = "적염", Items = new[] { "crimson_saber", "crimson_longcoat", "flame_pants", "flame_boots", "phoenix_feather", "bloodjade_bracelet" },
                Tiers = new[] { T(2, atk: 0.04f), T(3, hp: 0.05f), T(4, atk: 0.06f, crit: 0.03f), T(5, hp: 0.06f, speed: 2), T(6, atk: 0.10f, crit: 0.05f, hp: 0.06f) } },
            new GearSet { Id = "thunder", Name = "뇌전", Items = new[] { "thunder_dagger", "thunder_robe", "thunder_pants", "thunder_boots", "thunder_beads", "thunder_crown" },
                Tiers = new[] { T(2, crit: 0.03f), T(3, atk: 0.06f, speed: 2), T(4, atk: 0.06f, crit: 0.03f), T(5, qi: 0.015f, speed: 3), T(6, atk: 0.10f, crit: 0.06f, speed: 3) } },
            new GearSet { Id = "dragon", Name = "용", Items = new[] { "dragonfang_sword", "dragonscale_armor", "dragon_greaves", "dragon_boots", "dragon_pearl" },
                Tiers = new[] { T(2, hp: 0.05f), T(3, atk: 0.06f, def: 0.06f), T(4, atk: 0.06f, def: 0.06f, hp: 0.06f), T(5, atk: 0.10f, def: 0.10f, hp: 0.08f, qi: 0.015f) } },
            // The strongest set in the game: six pieces, every tier above the other sets' (the Transcendent 진비뢰도 counts as the weapon piece too).
            new GearSet { Id = "biroe", Name = "비뢰", Items = new[] { "biroe_blade", "jin_biroedo", "biroe_robe", "biroe_pants", "biroe_boots", "roegeum", "mukrin_talisman" },
                Tiers = new[] { T(2, atk: 0.05f, crit: 0.03f), T(3, atk: 0.08f, speed: 3), T(4, atk: 0.08f, def: 0.08f, hp: 0.08f, crit: 0.03f), T(5, atk: 0.10f, crit: 0.04f, speed: 4, qi: 0.015f), T(6, atk: 0.15f, def: 0.10f, hp: 0.10f, crit: 0.05f, speed: 5, qi: 0.0225f) } },
            new GearSet { Id = "emperor", Name = "제왕", Items = new[] { "heaven_cutter", "emperor_robe", "emperor_pants", "emperor_boots", "taegeuk_jade", "emperor_seal" },
                Tiers = new[] { T(2, atk: 0.05f, hp: 0.05f), T(3, atk: 0.06f, def: 0.06f), T(4, hp: 0.08f, def: 0.08f), T(5, atk: 0.08f, crit: 0.03f, speed: 3), T(6, atk: 0.12f, def: 0.10f, hp: 0.10f) } },
        };

        /// <summary>The 비뢰 set (including 진비뢰도) is exclusive to the protagonist: companions cannot wear it.</summary>
        public const string HeroOnlySetId = "biroe";

        public static bool IsHeroOnly(string itemId)
        {
            var set = SetOf(itemId);
            return set != null && set.Id == HeroOnlySetId;
        }

        public const string HeroOnlyLabel = "비류연 전용";

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
            if (t.QiPct != 0f) parts.Add("내공 +" + (t.QiPct * 100f).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%");
            if (t.AtkPct != 0f) parts.Add("공격 +" + Math.Round(t.AtkPct * 100f) + "%");
            if (t.DefPct != 0f) parts.Add("방어 +" + Math.Round(t.DefPct * 100f) + "%");
            if (t.SpeedAdd != 0) parts.Add("속도 +" + t.SpeedAdd);
            if (t.CritAdd != 0f) parts.Add("치명타 +" + Math.Round(t.CritAdd * 100f) + "%");
            return string.Join("  ", parts);
        }

        /// <summary>Multi-line description of a set for the item window: each piece with its state, then every tier bonus.</summary>
        public static string SetDetail(GearSet set, IEnumerable<string> equipped, SaveData save)
        {
            var worn = new List<string>(equipped);
            var sb = new System.Text.StringBuilder();
            sb.Append("[").Append(set.Name).Append(" 세트]  착용 ").Append(WornPieces(set, worn)).Append("/").Append(set.Items.Length).Append("\n");
            var names = new List<string>();
            foreach (var id in set.Items)
            {
                var it = EarlyGameContent.FindItem(id);
                string mark = worn.Contains(id) ? "●" : Inventory.Count(save, id) > 0 ? "○" : "·";
                names.Add(mark + " " + (it != null ? it.Name : id));
            }
            sb.Append(string.Join("   ", names)).Append("\n");
            foreach (var t in set.Tiers) sb.Append(t.Pieces).Append("개: ").Append(TierText(t)).Append("\n");
            return sb.ToString().TrimEnd('\n');
        }

        /// <summary>One entry per set with at least one worn piece: title, the bonuses reached so far, and what the next piece would add.</summary>
        public static List<string[]> SetStatus(IEnumerable<string> equipped)
        {
            var worn = new List<string>(equipped);
            var list = new List<string[]>();
            foreach (var s in Sets)
            {
                int n = WornPieces(s, worn);
                if (n == 0) continue;
                string title = s.Name + " 세트  " + n + " / " + s.Items.Length;
                var active = new List<string>();
                string next = "";
                foreach (var t in s.Tiers)
                {
                    if (n >= t.Pieces) active.Add(t.Pieces + "개: " + TierText(t));
                    else if (next.Length == 0) next = "다음 " + t.Pieces + "개: " + TierText(t);
                }
                list.Add(new[] { title, active.Count > 0 ? string.Join("\n", active) : "아직 효과 없음 (2개부터)", next });
            }
            return list;
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
