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
    /// 무공 숙련: the protagonist's arts grow with use (입문 → 소성 10 → 대성 25 → 극성 50 → 극의 80 uses; power +3/7/15/30%). From 극성 on the player
    /// may pick a 변초 that reshapes the art: 강 (+50% power, costlier), 쾌 (cheaper, shorter cooldown) or 연 (one more, weaker hit).
    /// 극의 also recharges one turn faster and costs 20% less qi.
    /// Pure upside for the balance bot, which ignores mastery on purpose.
    /// </summary>
    public static class Mastery
    {
        public static readonly int[] Thresholds = { 0, 10, 25, 50, 80 };
        /// <summary>비기 (필살기) are used at most about once a fight, so their steps come far sooner: 소성 2, 대성 5, 극성 10, 극의 20 uses.</summary>
        public static readonly int[] UltimateThresholds = { 0, 2, 5, 10, 20 };

        public static int[] ThresholdsFor(ArtData art) { return art != null && art.SpiritCost > 0 ? UltimateThresholds : Thresholds; }
        public static readonly string[] LevelNames = { "입문", "소성", "대성", "극성", "극의" };
        public static readonly float[] PowerBonus = { 0f, 0.03f, 0.07f, 0.15f, 0.30f };
        public static readonly float[] UltimatePowerBonus = { 0f, 0.05f, 0.10f, 0.20f, 0.40f };   // 비기 have no 변초, so they grow more
        public static readonly float[] EvadeChanceByLevel = { 0.50f, 0.65f, 0.80f, 0.90f, 1.00f };   // 삼첩영: share of hits dodged
        public const int VariantLevel = 3;      // 극성: the 변초 opens
        public const int MasterLevel = 4;       // 극의: one turn faster recharge (never below 0) and 20% less qi
        public const float MasterQiFactor = 0.8f;
        public const float PowerVariantFactor = 1.5f, FlurryPowerFactor = 0.65f;

        /// <summary>One line per mastery level for the detail window: what the art gains at that step.</summary>
        public static List<string> LevelLines(ArtData art)
        {
            var lines = new List<string>();
            if (art.OwnerId != null) return lines;
            for (int i = 0; i < Thresholds.Length; i++)
            {
                var parts = new List<string>();
                if (art.Effect == SkillEffect.Evade) parts.Add("회피 " + Math.Round(EvadeChanceByLevel[i] * 100f) + "%");
                else if (i > 0) parts.Add("위력 +" + Math.Round(PowerBonusOf(art, i) * 100f) + "%");
                if (i == VariantLevel && AvailableVariants(art).Count > 0) parts.Add("변초 해금");
                if (i == MasterLevel)
                {
                    if (art.Cooldown > 0) parts.Add("재사용 -1턴");
                    if (art.QiCost > 0) parts.Add("내공 -" + Math.Round((1f - MasterQiFactor) * 100f) + "%");
                }
                lines.Add(LevelNames[i] + " (" + ThresholdsFor(art)[i] + "회)  " + (parts.Count > 0 ? string.Join(" · ", parts) : "기본"));
            }
            return lines;
        }

        public static float PowerBonusOf(ArtData art, int level)
        {
            return art.SpiritCost > 0 ? UltimatePowerBonus[level] : PowerBonus[level];
        }

        public static int LevelOf(int uses) { return LevelOf(Thresholds, uses); }

        public static int LevelOf(ArtData art, int uses) { return LevelOf(ThresholdsFor(art), uses); }

        private static int LevelOf(int[] steps, int uses)
        {
            int level = 0;
            for (int i = 0; i < steps.Length; i++) if (uses >= steps[i]) level = i;
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
            var steps = ThresholdsFor(EarlyGameContent.FindArt(artId));
            return lv >= steps.Length - 1 ? -1 : steps[lv + 1] - Uses(save, artId);
        }

        /// <summary>One line listing every step for the 무공 tab: "소성 10회(위력 +3%) · 대성 25회(+7%) · 극성 50회(+15%, 변초) · 극의 80회(+30%, 재사용 -1턴, 내공 -20%)".</summary>
        /// <summary>The 숙련 explanation behind the ? button of the 무공 tab (player-facing, one rule per line).</summary>
        public static string HelpText()
        {
            return "<color=#D4B87A>숙련 (사용 횟수)</color>\n"
                + "· " + LevelNames[1] + " " + Thresholds[1] + "회　 위력 +3%\n"
                + "· " + LevelNames[2] + " " + Thresholds[2] + "회　 위력 +7%\n"
                + "· " + LevelNames[3] + " " + Thresholds[3] + "회　 위력 +15%, 변초\n"
                + "· " + LevelNames[4] + " " + Thresholds[4] + "회　 위력 +30%, 재사용 -1턴, 내공 -20%\n\n"
                + "비기(필살기): " + string.Join(" / ", new[] { UltimateThresholds[1], UltimateThresholds[2], UltimateThresholds[3], UltimateThresholds[4] }) + "회마다 단계, 변초 없음, 위력 +5 / 10 / 20 / 40%\n"
                + "변초는 무공을 눌러 상세 창에서 바꿉니다.";
        }

        public static string ConditionText()
        {
            return LevelNames[1] + " " + Thresholds[1] + "회(위력 +3%) · " + LevelNames[2] + " " + Thresholds[2] + "회(+7%) · "
                + LevelNames[3] + " " + Thresholds[3] + "회(+15%, 변초) · " + LevelNames[4] + " " + Thresholds[4] + "회(+30%, 재사용 -1턴, 내공 -20%)";
        }

        public static int Uses(SaveData save, string artId) { var e = Find(save, artId); return e != null ? e.Uses : 0; }
        public static int Level(SaveData save, string artId) { return LevelOf(EarlyGameContent.FindArt(artId), Uses(save, artId)); }
        public static ArtVariant VariantOf(SaveData save, string artId) { var e = Find(save, artId); return e != null ? (ArtVariant)e.Variant : ArtVariant.None; }

        // 무공 수련서: found after winning a fight (each rolled on its own), also sold by the 천무학관 shelf.
        public static readonly string[] ScrollIds = { "art_scroll_5", "art_scroll_10", "art_scroll_20" };
        public static readonly float[] ScrollChance = { 0.10f, 0.05f, 0.02f };
        /// <summary>Uses a 수련서 adds to a 비기 (소 1, 중 2, 대 4) instead of its <see cref="ItemData.MasteryUses"/>, since 비기 steps are much shorter.</summary>
        public static readonly int[] UltimateScrollUses = { 1, 2, 4 };

        /// <summary>Uses one 수련서 adds to this art.</summary>
        public static int ScrollUses(ItemData scroll, ArtData art)
        {
            if (scroll == null) return 0;
            if (art == null || art.SpiritCost <= 0) return scroll.MasteryUses;
            int i = Array.IndexOf(ScrollIds, scroll.Id);
            return i >= 0 ? UltimateScrollUses[i] : Math.Max(1, (int)Math.Round(scroll.MasteryUses / 5.0));
        }

        /// <summary>First stage of the given difficulty (0 normal, 1 어려움, 2 극악) where each 수련서 starts to drop: 소 from the start, 중 from normal 20, 대 from 어려움 10.</summary>
        public static readonly int[] ScrollDropDifficulty = { 0, 0, 1 }, ScrollDropStage = { 1, 20, 10 };

        public static bool ScrollCanDrop(int scroll, int stage, int difficulty)
        {
            if (difficulty != ScrollDropDifficulty[scroll]) return difficulty > ScrollDropDifficulty[scroll];
            return stage >= ScrollDropStage[scroll];
        }

        /// <summary>Rolls the 수련서 for one won fight and puts what was found in the bag. Only the scrolls that can drop at that stage and difficulty are rolled.</summary>
        public static List<ItemStack> RollScrolls(SaveData save, IRandom rng, int stage = 1, int difficulty = 0)
        {
            var found = new List<ItemStack>();
            for (int i = 0; i < ScrollIds.Length; i++)
            {
                if (!ScrollCanDrop(i, stage, difficulty)) continue;
                if (rng.NextDouble() >= ScrollChance[i]) continue;
                Inventory.Add(save, ScrollIds[i], 1);
                found.Add(new ItemStack(ScrollIds[i], 1));
            }
            return found;
        }

        /// <summary>The protagonist's learned arts a 수련서 can be read into (companion arts have no mastery).</summary>
        public static List<ArtData> ScrollTargets(SaveData save)
        {
            var list = new List<ArtData>();
            foreach (var artId in save.LearnedArts)
            {
                var art = EarlyGameContent.FindArt(artId);
                if (art != null && art.OwnerId == null) list.Add(art);
            }
            return list;
        }

        /// <summary>Reads one 수련서 into one learned art. Returns the result line; <paramref name="used"/> tells whether it was consumed.</summary>
        public static string UseScroll(SaveData save, string itemId, string artId, out bool used)
        {
            used = false;
            var item = EarlyGameContent.FindItem(itemId);
            if (item == null || item.MasteryUses <= 0) return "수련서가 아닙니다.";
            if (Inventory.Count(save, itemId) <= 0) return "수련서가 없습니다.";
            var art = EarlyGameContent.FindArt(artId);
            if (art == null || art.OwnerId != null || !save.LearnedArts.Contains(artId)) return "배운 무공을 골라 주세요.";
            Inventory.Remove(save, itemId, 1);
            int before = Level(save, artId), gain = ScrollUses(item, art);
            for (int i = 0; i < gain; i++) RecordUse(save, artId);
            used = true;
            int after = Level(save, artId);
            return art.Name + "의 숙련 +" + gain + "회 (" + Uses(save, artId) + "회)" + (after > before ? "  " + LevelNames[after] + "에 올랐습니다!" : "");
        }

        /// <summary>Free practice: counts <paramref name="uses"/> uses for every equipped art of the protagonist (a 비기 gets a fifth, at least one, like the 수련서).</summary>
        public static void Train(SaveData save, int uses)
        {
            foreach (var artId in save.EquippedArts)
            {
                var art = EarlyGameContent.FindArt(artId);
                if (art == null || art.OwnerId != null) continue;
                int n = art.SpiritCost > 0 ? Math.Max(1, (int)Math.Round(uses / 5.0)) : uses;
                for (int i = 0; i < n; i++) RecordUse(save, artId);
            }
        }

        /// <summary>Counts one use; returns the new mastery level when it just rose, otherwise -1.</summary>
        public static int RecordUse(SaveData save, string artId)
        {
            var e = Find(save, artId);
            if (e == null) { e = new ArtMasteryEntry { Id = artId }; save.ArtMastery.Add(e); }
            var art = EarlyGameContent.FindArt(artId);
            int before = LevelOf(art, e.Uses);
            e.Uses++;
            int after = LevelOf(art, e.Uses);
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
                case ArtVariant.Power: return "강(剛): 위력 +50%, 내공 소모 +20%";
                case ArtVariant.Swift: return "쾌(快): 내공 소모 -20%, 재사용 -1턴";
                case ArtVariant.Flurry: return "연(連): 타수 +1, 타당 위력 ×0.65";
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
            int level = Level(save, art.Id);
            spec.Power *= 1f + PowerBonusOf(art, level);
            if (art.Effect == SkillEffect.Evade) spec.EvadeChance = EvadeChanceByLevel[level];
            if (level >= MasterLevel) spec.Cooldown = Math.Max(0, spec.Cooldown - 1);
            var v = level >= VariantLevel && AvailableVariants(art).Contains(VariantOf(save, art.Id)) ? VariantOf(save, art.Id) : ArtVariant.None;
            switch (v)
            {
                case ArtVariant.Power:
                    spec.Power *= PowerVariantFactor;
                    spec.QiCost = (spec.QiCost * 12 + 9) / 10;   // x1.2 rounded up, in integers
                    break;
                case ArtVariant.Swift:
                    spec.QiCost = spec.QiCost * 8 / 10;          // x0.8 rounded down, in integers
                    spec.Cooldown = Math.Max(0, spec.Cooldown - 1);
                    break;
                case ArtVariant.Flurry:
                    spec.Hits = Math.Max(1, spec.Hits) + 1;
                    spec.Power *= FlurryPowerFactor;
                    break;
            }
            if (level >= MasterLevel && spec.QiCost > 0) spec.QiCost = Math.Max(1, (int)Math.Floor(spec.QiCost * MasterQiFactor + 0.0001f));   // after the variant
            if (v != ArtVariant.None) spec.Name += "(" + VariantName(v) + ")";
            return spec;
        }
    }
}
