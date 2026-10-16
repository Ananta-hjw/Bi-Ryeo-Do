using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>Use counter and chosen 변초 (variant) for one martial art. Public fields for JsonUtility.</summary>
    [Serializable]
    public sealed class ArtMasteryEntry
    {
        public string Id;
        public int Uses;
        public int Variant;   // 0 = none, otherwise a <see cref="ArtVariant"/> value
    }

    public enum ArtVariant { None = 0, Power = 1, Swift = 2, Flurry = 3 }

    /// <summary>
    /// 무공 숙련: the protagonist's arts grow with use (입문 → 소성 → 대성 → 극의, +5% power each step). From 대성 on the player
    /// may pick a 변초 that reshapes the art: 강 (stronger, costlier), 쾌 (cheaper, shorter cooldown) or 연 (one more, weaker hit).
    /// Pure upside for the balance bot, which ignores mastery on purpose.
    /// </summary>
    public static class Mastery
    {
        public static readonly int[] Thresholds = { 0, 10, 20, 50 };
        public static readonly string[] LevelNames = { "입문", "소성", "대성", "극의" };
        public const int VariantLevel = 2;
        public const float PowerPerLevel = 0.05f;
        public const int MasterLevel = 3;   // 극의: every art of this level recharges one turn faster (never below 0)

        public static int LevelOf(int uses)
        {
            int level = 0;
            for (int i = 0; i < Thresholds.Length; i++) if (uses >= Thresholds[i]) level = i;
            return level;
        }

        public static ArtMasteryEntry Find(SaveData save, string artId)
        {
            foreach (var e in save.ArtMastery) if (e.Id == artId) return e;
            return null;
        }

        /// <summary>Uses still needed for the next step, or -1 at 극의.</summary>
        public static int UsesToNext(SaveData save, string artId)
        {
            int lv = Level(save, artId);
            return lv >= Thresholds.Length - 1 ? -1 : Thresholds[lv + 1] - Uses(save, artId);
        }

        /// <summary>One line listing every step for the 무공 tab: "소성 10회(위력 +5%) · 대성 20회(+10%, 변초) · 극의 50회(+15%, 재사용 -1턴)".</summary>
        public static string ConditionText()
        {
            return LevelNames[1] + " " + Thresholds[1] + "회(위력 +5%) · " + LevelNames[2] + " " + Thresholds[2] + "회(+10%, 변초) · " + LevelNames[3] + " " + Thresholds[3] + "회(+15%, 재사용 -1턴)";
        }

        public static int Uses(SaveData save, string artId) { var e = Find(save, artId); return e != null ? e.Uses : 0; }
        public static int Level(SaveData save, string artId) { return LevelOf(Uses(save, artId)); }
        public static ArtVariant VariantOf(SaveData save, string artId) { var e = Find(save, artId); return e != null ? (ArtVariant)e.Variant : ArtVariant.None; }

        /// <summary>Free practice: counts <paramref name="uses"/> uses for every equipped art of the protagonist.</summary>
        public static void Train(SaveData save, int uses)
        {
            foreach (var artId in save.EquippedArts)
            {
                var art = EarlyGameContent.FindArt(artId);
                if (art != null && art.OwnerId == null) for (int i = 0; i < uses; i++) RecordUse(save, artId);
            }
        }

        /// <summary>Counts one use; returns the new mastery level when it just rose, otherwise -1.</summary>
        public static int RecordUse(SaveData save, string artId)
        {
            var e = Find(save, artId);
            if (e == null) { e = new ArtMasteryEntry { Id = artId }; save.ArtMastery.Add(e); }
            int before = LevelOf(e.Uses);
            e.Uses++;
            int after = LevelOf(e.Uses);
            return after > before ? after : -1;
        }

        /// <summary>Variants that make sense for this art (supports and 비기 are left alone).</summary>
        public static List<ArtVariant> AvailableVariants(ArtData art)
        {
            var list = new List<ArtVariant>();
            if (art.OwnerId != null || art.SpiritCost > 0) return list;
            if (art.Effect != SkillEffect.None && art.Effect != SkillEffect.Stun) return list;
            list.Add(ArtVariant.Power);
            if (art.QiCost > 0 || art.Cooldown > 0) list.Add(ArtVariant.Swift);
            list.Add(ArtVariant.Flurry);
            return list;
        }

        public static string VariantName(ArtVariant v)
        {
            switch (v)
            {
                case ArtVariant.Power: return "강";
                case ArtVariant.Swift: return "쾌";
                case ArtVariant.Flurry: return "연";
                default: return "";
            }
        }

        public static string VariantHint(ArtVariant v)
        {
            switch (v)
            {
                case ArtVariant.Power: return "강(剛): 위력 +25%, 내공 소모 +20%";
                case ArtVariant.Swift: return "쾌(快): 내공 소모 -30%, 재사용 -1턴";
                case ArtVariant.Flurry: return "연(連): 타수 +1, 타당 위력 ×0.7";
                default: return "변초 없음";
            }
        }

        /// <summary>Cycles none → each available variant → none (free to change).</summary>
        public static ArtVariant CycleVariant(SaveData save, ArtData art)
        {
            var options = AvailableVariants(art);
            if (options.Count == 0 || Level(save, art.Id) < VariantLevel) return ArtVariant.None;
            var e = Find(save, art.Id);
            if (e == null) { e = new ArtMasteryEntry { Id = art.Id }; save.ArtMastery.Add(e); }
            int idx = options.IndexOf((ArtVariant)e.Variant);   // -1 when none
            e.Variant = idx + 1 < options.Count ? (int)options[idx + 1] : 0;
            return (ArtVariant)e.Variant;
        }

        /// <summary>The battle skill for an art with the save's mastery and variant applied.</summary>
        public static SkillSpec Apply(SaveData save, ArtData art)
        {
            var spec = art.ToSpec();
            if (art.OwnerId != null) return spec;
            spec.Power *= 1f + PowerPerLevel * Level(save, art.Id);
            if (Level(save, art.Id) >= MasterLevel) spec.Cooldown = Math.Max(0, spec.Cooldown - 1);
            var v = Level(save, art.Id) >= VariantLevel && AvailableVariants(art).Contains(VariantOf(save, art.Id)) ? VariantOf(save, art.Id) : ArtVariant.None;
            switch (v)
            {
                case ArtVariant.Power:
                    spec.Power *= 1.25f;
                    spec.QiCost = (spec.QiCost * 12 + 9) / 10;   // x1.2 rounded up, in integers
                    break;
                case ArtVariant.Swift:
                    spec.QiCost = spec.QiCost * 7 / 10;          // x0.7 rounded down, in integers
                    spec.Cooldown = Math.Max(0, spec.Cooldown - 1);
                    break;
                case ArtVariant.Flurry:
                    spec.Hits = Math.Max(1, spec.Hits) + 1;
                    spec.Power *= 0.7f;
                    break;
            }
            if (v != ArtVariant.None) spec.Name += "(" + VariantName(v) + ")";
            return spec;
        }
    }
}
