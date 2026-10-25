using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    public enum GearTraitKind { Hp, Qi, Attack, Defense, Speed, Crit, CritDamage, StartSpirit }

    public struct GearTrait
    {
        public int Stars;
        public GearTraitKind Kind;
        public float Value;
    }

    public struct GearEffects
    {
        public float HpPct, QiPct, AtkPct, DefPct, SpeedAdd, CritAdd, CritDmgAdd;
        public int StartSpirit;
    }

    public static partial class Gear
    {
        // Explicit identities for upper-grade gear; other gear follows its slot and existing effects.
        private static readonly Dictionary<string, GearTraitKind[]> TraitPlans = new Dictionary<string, GearTraitKind[]>
        {
            { "crimson_saber", new[] { GearTraitKind.CritDamage, GearTraitKind.Qi, GearTraitKind.Defense } },
            { "dragonfang_sword", new[] { GearTraitKind.Qi, GearTraitKind.Speed, GearTraitKind.CritDamage } },
            { "dragonscale_armor", new[] { GearTraitKind.Qi, GearTraitKind.Speed, GearTraitKind.Attack } },
            { "phoenix_robe", new[] { GearTraitKind.Qi, GearTraitKind.Attack, GearTraitKind.CritDamage } },
            { "dragon_greaves", new[] { GearTraitKind.Qi, GearTraitKind.Speed, GearTraitKind.Attack } },
            { "dragon_boots", new[] { GearTraitKind.Qi, GearTraitKind.Attack, GearTraitKind.CritDamage } },
            { "dragon_pearl", new[] { GearTraitKind.Speed, GearTraitKind.Crit, GearTraitKind.CritDamage } },
            { "thunder_crown", new[] { GearTraitKind.CritDamage, GearTraitKind.Speed, GearTraitKind.Hp } },
            { "snowlotus_charm", new[] { GearTraitKind.Speed, GearTraitKind.Attack, GearTraitKind.CritDamage } },
            { "phoenix_feather", new[] { GearTraitKind.CritDamage, GearTraitKind.Qi, GearTraitKind.Hp } },
            { "gen_wea_leg01", new[] { GearTraitKind.Crit, GearTraitKind.Speed, GearTraitKind.CritDamage } },
            { "gen_wea_leg02", new[] { GearTraitKind.CritDamage, GearTraitKind.Hp, GearTraitKind.Speed } },
            { "gen_wea_leg03", new[] { GearTraitKind.Crit, GearTraitKind.Qi, GearTraitKind.CritDamage } },
            { "gen_arm_leg01", new[] { GearTraitKind.Hp, GearTraitKind.Qi, GearTraitKind.Speed } },
            { "gen_arm_leg02", new[] { GearTraitKind.Defense, GearTraitKind.Qi, GearTraitKind.Speed } },
            { "gen_arm_leg03", new[] { GearTraitKind.Qi, GearTraitKind.Attack, GearTraitKind.Speed } },
            { "gen_pan_leg01", new[] { GearTraitKind.Hp, GearTraitKind.Speed, GearTraitKind.Qi } },
            { "gen_pan_leg02", new[] { GearTraitKind.Defense, GearTraitKind.Qi, GearTraitKind.Speed } },
            { "gen_boo_leg01", new[] { GearTraitKind.Hp, GearTraitKind.Qi, GearTraitKind.Crit } },
            { "gen_acc_leg01", new[] { GearTraitKind.Speed, GearTraitKind.CritDamage, GearTraitKind.Qi } },
            { "gen_acc_leg02", new[] { GearTraitKind.Defense, GearTraitKind.Speed, GearTraitKind.Attack } },
            { "starfall_sword", new[] { GearTraitKind.CritDamage, GearTraitKind.Qi, GearTraitKind.Defense } },
            { "heaven_cutter", new[] { GearTraitKind.CritDamage, GearTraitKind.Qi, GearTraitKind.Speed } },
            { "heavensilk_robe", new[] { GearTraitKind.Qi, GearTraitKind.Speed, GearTraitKind.Attack } },
            { "vajra_armor", new[] { GearTraitKind.Qi, GearTraitKind.Attack, GearTraitKind.Speed } },
            { "roegeum", new[] { GearTraitKind.CritDamage, GearTraitKind.Defense, GearTraitKind.StartSpirit } },
            { "heavenly_demon_core", new[] { GearTraitKind.CritDamage, GearTraitKind.Speed, GearTraitKind.Hp } },
            { "taegeuk_jade", new[] { GearTraitKind.Speed, GearTraitKind.Crit, GearTraitKind.CritDamage } },
            { "emperor_seal", new[] { GearTraitKind.Qi, GearTraitKind.Speed, GearTraitKind.CritDamage } },
            { "emperor_robe", new[] { GearTraitKind.Qi, GearTraitKind.Speed, GearTraitKind.CritDamage } },
            { "emperor_pants", new[] { GearTraitKind.Qi, GearTraitKind.Attack, GearTraitKind.CritDamage } },
            { "emperor_boots", new[] { GearTraitKind.Qi, GearTraitKind.Attack, GearTraitKind.Crit } },
            { "biroe_blade", new[] { GearTraitKind.CritDamage, GearTraitKind.Qi, GearTraitKind.Defense } },
            { "biroe_robe", new[] { GearTraitKind.Qi, GearTraitKind.Crit, GearTraitKind.CritDamage } },
            { "biroe_pants", new[] { GearTraitKind.Qi, GearTraitKind.Attack, GearTraitKind.Crit } },
            { "biroe_boots", new[] { GearTraitKind.Qi, GearTraitKind.Attack, GearTraitKind.CritDamage } },
            { "gen_wea_myt01", new[] { GearTraitKind.Crit, GearTraitKind.Speed, GearTraitKind.CritDamage } },
            { "gen_acc_myt01", new[] { GearTraitKind.Speed, GearTraitKind.CritDamage, GearTraitKind.Hp } },
            { "gen_acc_myt02", new[] { GearTraitKind.Defense, GearTraitKind.Speed, GearTraitKind.Attack } },
        };
        private static readonly float[] TraitPercent = { 0f, 0f, 0f, .05f, .07f, .10f, .14f };
        private static readonly float[] TraitCrit = { 0f, 0f, 0f, .0125f, .015f, .02f, .025f };
        private static readonly float[] TraitCritDamage = { 0f, 0f, 0f, .10f, .14f, .20f, .28f };
        private static readonly int[] TraitSpeed = { 0, 0, 0, 2, 3, 4, 5 };
        private static readonly int[] TraitSpirit = { 0, 0, 0, 4, 6, 8, 10 };
        private static readonly int[] TraitStars = { 1, 3, 5 };
        private static readonly float[] TraitStrength = { 1f, 2f, 3f };
        private static readonly float[] EpicTraitBoost = { 1.20f, 1.40f, 1.70f };
        private static readonly float[] LegendaryTraitBoost = { 1.40f, 1.60f, 2.00f };
        private static readonly float[] MythicTraitBoost = { 1.60f, 1.80f, 2.30f };

        private static GearTraitKind[] DefaultTraitPlan(ItemData item)
        {
            switch (item.Kind)
            {
                case ItemKind.Weapon: return new[] { GearTraitKind.Crit, GearTraitKind.Speed, GearTraitKind.CritDamage, GearTraitKind.Qi, GearTraitKind.Hp };
                case ItemKind.Armor: return new[] { GearTraitKind.Hp, GearTraitKind.Defense, GearTraitKind.Qi, GearTraitKind.Speed, GearTraitKind.Attack };
                case ItemKind.Pants: return new[] { GearTraitKind.Defense, GearTraitKind.Hp, GearTraitKind.Speed, GearTraitKind.Qi, GearTraitKind.Attack };
                case ItemKind.Boots: return new[] { GearTraitKind.Speed, GearTraitKind.Qi, GearTraitKind.Hp, GearTraitKind.Attack, GearTraitKind.Crit };
                default:
                    return item.QiPct > 0f ? new[] { GearTraitKind.Speed, GearTraitKind.Defense, GearTraitKind.CritDamage, GearTraitKind.Hp, GearTraitKind.Attack }
                        : new[] { GearTraitKind.Qi, GearTraitKind.CritDamage, GearTraitKind.Speed, GearTraitKind.Hp, GearTraitKind.Defense };
            }
        }

        private static bool HasBaseEffect(ItemData item, GearTraitKind kind)
        {
            switch (kind)
            {
                case GearTraitKind.Hp: return item.HpPct != 0f;
                case GearTraitKind.Qi: return item.QiPct != 0f;
                case GearTraitKind.Attack: return item.AtkPct != 0f;
                case GearTraitKind.Defense: return item.DefPct != 0f;
                case GearTraitKind.Speed: return item.SpeedAdd != 0;
                case GearTraitKind.Crit: return item.CritAdd != 0f;
                case GearTraitKind.CritDamage: return item.CritDmgAdd != 0f;
                default: return false;
            }
        }

        /// <summary>내공 breakthrough traits are worth half the other percent traits.</summary>
        public const float QiTraitScale = 0.5f;

        public static GearTrait[] BreakthroughTraits(ItemData item)
        {
            if (item == null || !item.IsGear || item.Grade < ItemGrade.Epic || item.Id == Ritual.RewardId) return new GearTrait[0];
            GearTraitKind[] plan;
            if (!TraitPlans.TryGetValue(item.Id, out plan)) plan = DefaultTraitPlan(item);
            var selected = new List<GearTraitKind>();
            foreach (var kind in plan) if (!HasBaseEffect(item, kind) && !selected.Contains(kind)) selected.Add(kind);
            foreach (GearTraitKind kind in Enum.GetValues(typeof(GearTraitKind)))
                if (!HasBaseEffect(item, kind) && !selected.Contains(kind)) selected.Add(kind);
            int grade = Math.Max(0, Math.Min(6, (int)item.Grade));
            var boosts = item.Grade == ItemGrade.Epic ? EpicTraitBoost
                : item.Grade == ItemGrade.Legendary ? LegendaryTraitBoost
                : item.Grade == ItemGrade.Mythic ? MythicTraitBoost : null;
            var result = new GearTrait[Math.Min(3, selected.Count)];
            for (int i = 0; i < result.Length; i++)
            {
                var kind = selected[i];
                float value = kind == GearTraitKind.Crit ? TraitCrit[grade] : kind == GearTraitKind.CritDamage ? TraitCritDamage[grade]
                    : kind == GearTraitKind.Speed ? TraitSpeed[grade] : kind == GearTraitKind.StartSpirit ? TraitSpirit[grade] : TraitPercent[grade];
                value *= TraitStrength[i];
                // Apply the new grade/milestone boost to existing traits, except critical chance.
                if (kind != GearTraitKind.Crit && boosts != null) value *= boosts[i];
                if (kind == GearTraitKind.Qi) value *= QiTraitScale;   // 내공 from gear is halved everywhere (base effects, these traits and set bonuses)
                if (kind == GearTraitKind.Speed || kind == GearTraitKind.StartSpirit) value = (float)Math.Round(value, MidpointRounding.AwayFromZero);
                result[i] = new GearTrait { Stars = TraitStars[i], Kind = kind, Value = value };
            }
            return result;
        }

        public static GearEffects EffectsFor(ItemData item, int enhance = 0, int stars = 0)
        {
            if (item == null) return new GearEffects();
            stars = Math.Max(0, Math.Min(MaxStars, stars));
            // Transcendent gear is always five-star, including previews without an explicit star count.
            if (item.Grade == ItemGrade.Transcendent) stars = MaxStars;
            float m = EffectMultiplier(item, enhance, stars);
            // Critical chance gets 10% of enhancement gains and 50% of breakthrough bonuses.
            float critMultiplier = 1f + (Multiplier(enhance) - 1f) * .10f + StarBonusFor(item, stars) * .50f;
            // Innate five-star definitions retain their original reference denominator.
            if (item.Grade == ItemGrade.Transcendent) critMultiplier /= 1.3f;
            var effects = new GearEffects
            {
                HpPct = item.HpPct * m, QiPct = item.QiPct * m, AtkPct = item.AtkPct * m, DefPct = item.DefPct * m,
                SpeedAdd = item.SpeedAdd * m, CritAdd = item.CritAdd * critMultiplier, CritDmgAdd = item.CritDmgAdd * m
            };
            if (stars == 0) return effects;
            foreach (var trait in BreakthroughTraits(item))
            {
                if (stars < trait.Stars) continue;
                switch (trait.Kind)
                {
                    case GearTraitKind.Hp: effects.HpPct += trait.Value; break;
                    case GearTraitKind.Qi: effects.QiPct += trait.Value; break;
                    case GearTraitKind.Attack: effects.AtkPct += trait.Value; break;
                    case GearTraitKind.Defense: effects.DefPct += trait.Value; break;
                    case GearTraitKind.Speed: effects.SpeedAdd += trait.Value; break;
                    case GearTraitKind.Crit: effects.CritAdd += trait.Value; break;
                    case GearTraitKind.CritDamage: effects.CritDmgAdd += trait.Value; break;
                    case GearTraitKind.StartSpirit: effects.StartSpirit += (int)trait.Value; break;
                }
            }
            return effects;
        }

        public static string TraitText(GearTrait trait)
        {
            string[] labels = { "체력", "내공", "공격", "방어", "속도", "치명타", "치명타 피해", "시작 기세" };
            bool flat = trait.Kind == GearTraitKind.Speed || trait.Kind == GearTraitKind.StartSpirit;
            return labels[(int)trait.Kind] + " +" + Math.Round(flat ? trait.Value : trait.Value * 100f, 2)
                + (flat ? "" : trait.Kind == GearTraitKind.Crit || trait.Kind == GearTraitKind.CritDamage ? "%p" : "%");
        }

        public static string TraitsText(ItemData item, int stars, string unlockedColor = null, string lockedColor = null)
        {
            if (item != null && item.Grade == ItemGrade.Transcendent) stars = MaxStars;
            var lines = new List<string>();
            foreach (var trait in BreakthroughTraits(item))
            {
                bool active = stars >= trait.Stars;
                string line = trait.Stars + "성 " + (active ? "[적용] " : "[잠김] ") + TraitText(trait);
                string color = active ? unlockedColor : lockedColor;
                lines.Add(string.IsNullOrEmpty(color) ? line : "<color=" + color + ">" + line + "</color>");
            }
            return string.Join("\n", lines);
        }
    }
}
