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
        public int Shown = -1;   // the 숙련 step last announced (-1: not recorded yet; additive field)
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
                foreach (var f in EarlyGameContent.FormsOf(art.Id))
                    if (f.FormTier == i) parts.Add("「" + f.Name + "」로 변화");
                lines.Add(LevelNames[i] + " (" + ThresholdsFor(art)[i] + "회" + (i > 0 ? ", " + GateText(art, i) : "") + ")  " + (parts.Count > 0 ? string.Join(" · ", parts) : "기본"));
            }
            return lines;
        }

        /// <summary>
        /// 무공 변화: the technique the art currently is. A form of the art's line (<see cref="EarlyGameContent.ArtForms"/>) counts once the art's 숙련 reached
        /// the form's step, the form's stage is reached on its difficulty and 비류연 is at least the form's level; the latest such form wins.
        /// </summary>
        public static ArtData CurrentForm(SaveData save, ArtData art)
        {
            if (art == null || art.OwnerId != null) return art;
            int level = Level(save, art.Id);
            var shown = art;
            foreach (var f in EarlyGameContent.FormsOf(art.Id))
                if (FormReached(save, f, level)) shown = f;
            return shown;
        }

        /// <summary>The art's (gated) 숙련 step has reached the form's step; the gate of that step already holds the form's stage.</summary>
        public static bool FormReached(SaveData save, ArtData form, int masteryLevel) { return masteryLevel >= form.FormTier; }

        /// <summary>"대성 · 어려움 스테이지 36 돌파 · Lv.36" — what a form needs.</summary>
        public static string FormCondition(ArtData form)
        {
            return LevelNames[form.FormTier] + " · " + GateText(EarlyGameContent.FindArt(form.FormOf), form.FormTier);
        }

        /// <summary>
        /// Forms reached since the last call, as "풍신 → 편수 풍신" (one per form, announced once, <see cref="SaveData.SeenForms"/>).
        /// </summary>
        public static List<string> PollNewForms(SaveData save)
        {
            var lines = new List<string>();
            foreach (var id in save.LearnedArts)
            {
                var art = EarlyGameContent.FindArt(id);
                if (art == null || art.OwnerId != null) continue;
                var form = CurrentForm(save, art);
                if (form == art || save.SeenForms.Contains(form.Id)) continue;
                save.SeenForms.Add(form.Id);
                lines.Add(art.Name + " → " + form.Name);
            }
            return lines;
        }

        /// <summary>Old saves: arts that became forms fold into their line's art (the higher 숙련 wins), and the removed arts are forgotten.</summary>
        public static void MigrateForms(SaveData save)
        {
            foreach (var f in EarlyGameContent.ArtForms) Fold(save, f.Id, f.FormOf);
            foreach (var id in RemovedArts) Fold(save, id, null);
        }

        /// <summary>Arts taken out of the game (2026-10 말): plain strikes and repeats of other arts.</summary>
        public static readonly string[] RemovedArts =
            { "yeobanjang", "cheonji_ibun", "jeokjunghwa", "palhwa_nanmu", "seomjeon_ilsi", "palseom_noegwang", "hyeoncheon_secheom", "hyeoncheon_hoecheon", "sanmak" };

        private static void Fold(SaveData save, string oldId, string intoId)
        {
            bool learned = save.LearnedArts.Remove(oldId);
            int slot = save.EquippedArts.IndexOf(oldId);
            if (slot >= 0) save.EquippedArts.RemoveAt(slot);
            save.SeenArts.Remove(oldId);
            var old = Find(save, oldId);
            if (old != null) save.ArtMastery.Remove(old);
            if (intoId == null || !learned) return;
            if (!save.LearnedArts.Contains(intoId)) save.LearnedArts.Add(intoId);
            if (slot >= 0 && !save.EquippedArts.Contains(intoId)) save.EquippedArts.Insert(Math.Min(slot, save.EquippedArts.Count), intoId);
            if (old != null)
            {
                var into = Find(save, intoId);
                if (into == null) save.ArtMastery.Add(new ArtMasteryEntry { Id = intoId, Uses = old.Uses });
                else into.Uses = Math.Max(into.Uses, old.Uses);
            }
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
            return lv >= steps.Length - 1 ? -1 : Math.Max(0, steps[lv + 1] - Uses(save, artId));   // 0: the uses are there, the step waits for its stage or level
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
        /// <summary>The 숙련 step the art has reached: its uses, held back while the step's stage and level gate (<see cref="TierGate"/>) is not met.</summary>
        public static int Level(SaveData save, string artId)
        {
            var art = EarlyGameContent.FindArt(artId);
            int level = LevelOf(art, Uses(save, artId));
            if (art == null || art.OwnerId != null) return level;
            while (level > 0 && !TierOpen(save, art, level)) level--;
            return level;
        }

        /// <summary>The step the uses alone would give (shown next to a step that still waits for its stage or level).</summary>
        public static int UsesLevel(SaveData save, string artId) { return LevelOf(EarlyGameContent.FindArt(artId), Uses(save, artId)); }

        /// <summary>
        /// 숙련 단계의 관문: each step after 입문 also needs a stage and 비류연's level. The step's stage lies further along the learning ladder than the
        /// art's own (소성 +3, 대성 +8, 극성 +15, 극의 +24 stages, 일반 1-36 → 어려움 → 극악, capped at 극악 36); a step where the art turns into a form
        /// (<see cref="EarlyGameContent.ArtForms"/>) uses the form's stage. The level is the player's expected level there (<see cref="ExpectedLevel"/>)
        /// minus <see cref="TierLevelSlack"/>. Later steps never ask for less than earlier ones.
        /// </summary>
        public static readonly int[] TierStageOffset = { 0, 3, 8, 15, 24 };
        public const int TierLevelSlack = 2;
        private const int LadderStages = 36;

        public struct Gate { public int Stage, Difficulty, Level; }

        public static Gate TierGate(ArtData art, int tier)
        {
            int start = Math.Max(1, art.UnlockDiff * LadderStages + art.UnlockStage);
            var forms = EarlyGameContent.FormsOf(art.Id);
            int pos = start, level = 1;
            for (int t = 1; t <= tier; t++)
            {
                ArtData form = null;
                foreach (var f in forms) if (f.FormTier == t) form = f;
                int p = form != null ? form.UnlockDiff * LadderStages + Math.Max(1, form.UnlockStage) : Math.Min(3 * LadderStages, start + TierStageOffset[t]);
                int d = (p - 1) / LadderStages;
                int lv = Math.Max(1, ExpectedLevel(p - d * LadderStages, d) - TierLevelSlack);
                pos = Math.Max(pos, p);
                level = Math.Max(level, lv);
            }
            int diff = (pos - 1) / LadderStages;
            return new Gate { Stage = pos - diff * LadderStages, Difficulty = diff, Level = tier == 0 ? 1 : level };
        }

        /// <summary>
        /// The level a player usually has on reaching a stage: 일반 = the stage's recommended level (28 at 36); 어려움 and 극악 add about ten levels each
        /// across their 36 stages (어려움 36 ≈ 38, 극악 36 ≈ 48), the pace their experience rewards give.
        /// </summary>
        public static int ExpectedLevel(int stage, int difficulty)
        {
            stage = Math.Max(1, Math.Min(LadderStages, stage));
            if (difficulty <= 0) { var s = EarlyGameContent.FindStage(stage); return s != null ? s.RecommendedPlayerLevel : stage; }
            return 18 + 10 * difficulty + (int)Math.Round(10.0 * stage / LadderStages);
        }

        public static bool TierOpen(SaveData save, ArtData art, int tier)
        {
            if (tier <= 0) return true;
            var g = TierGate(art, tier);
            return StageStars.Reached(save, g.Stage, g.Difficulty) && save.Level >= g.Level;
        }

        /// <summary>"어려움 스테이지 5 돌파 · Lv.30"</summary>
        public static string GateText(ArtData art, int tier)
        {
            var g = TierGate(art, tier);
            return StageStars.ReachText(g.Stage, g.Difficulty) + " · Lv." + g.Level;
        }
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
        public static int RecordUse(SaveData save, string artId) { string waiting; return RecordUse(save, artId, out waiting); }

        /// <summary>
        /// Counts one use. Returns the 숙련 step reached (gated: the step's stage and level must be met too), or -1. When the uses complete a step
        /// whose gate is still closed, <paramref name="waiting"/> says what is missing ("대성 25회 달성 — 어려움 스테이지 5 돌파 · Lv.27이 필요합니다").
        /// </summary>
        public static int RecordUse(SaveData save, string artId, out string waiting)
        {
            waiting = null;
            var e = Find(save, artId);
            if (e == null) { e = new ArtMasteryEntry { Id = artId }; save.ArtMastery.Add(e); }
            var art = EarlyGameContent.FindArt(artId);
            if (e.Shown < 0) e.Shown = Level(save, artId);
            int usesBefore = LevelOf(art, e.Uses);
            e.Uses++;
            int usesAfter = LevelOf(art, e.Uses), gated = Level(save, artId);
            if (gated > e.Shown) { e.Shown = gated; return gated; }
            if (art != null && art.OwnerId == null && usesAfter > usesBefore && usesAfter > gated)
                waiting = LevelNames[gated + 1] + " " + ThresholdsFor(art)[gated + 1] + "회 달성 — " + GateText(art, gated + 1) + "이 필요합니다";
            return -1;
        }

        /// <summary>"다음 대성: 25회 · 일반 스테이지 10에 도달 · Lv.8" for the step after <paramref name="level"/>, or "" at 극의.</summary>
        public static string NextStepText(ArtData art, int level)
        {
            if (art == null || level >= MasterLevel) return "";
            return "다음 " + LevelNames[level + 1] + ": " + ThresholdsFor(art)[level + 1] + "회 · " + GateText(art, level + 1);
        }

        /// <summary>
        /// Steps reached outside a fight's use count (a stage cleared or a level gained opened a gate the uses had already passed), announced once
        /// as "분뢰수 숙련 대성!". The first call for an art only records its step.
        /// </summary>
        public static List<string> PollGateRises(SaveData save)
        {
            var lines = new List<string>();
            foreach (var id in save.LearnedArts)
            {
                var art = EarlyGameContent.FindArt(id);
                var e = Find(save, id);
                if (art == null || art.OwnerId != null || e == null) continue;
                int level = Level(save, id);
                if (e.Shown < 0) { e.Shown = level; continue; }
                if (level <= e.Shown) continue;
                e.Shown = level;
                lines.Add(art.Name + " 숙련 " + LevelNames[level] + "!" + (level == VariantLevel ? " 변초 해금" : ""));
            }
            return lines;
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
            if (art.OwnerId != null) return art.ToSpec();
            var spec = CurrentForm(save, art).ToSpec();
            spec.Id = art.Id;   // the line keeps its id: 숙련, cooldowns, icon and slot stay with the art
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
