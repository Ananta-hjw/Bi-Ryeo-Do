using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    [Serializable]
    public sealed class TrainingEntry
    {
        public string Id;
        public int Level;
    }

    /// <summary>
    /// 동료 수련: spend 두 to train a companion (levels 0-10). Each level adds +4% 체력·공격·방어; level 3 strengthens every art,
    /// level 6 makes the main art cheaper and quicker, level 10 awakens a signature change of that companion's arts.
    /// How far training can go is capped by the bond with the companion. Companion arts and awakenings are game design.
    /// </summary>
    public static class CompanionTraining
    {
        public const int MaxLevel = 10;
        public const float StatPerLevel = 0.04f;
        public const int ArtBoostLevel = 3, MainArtLevel = 6, AwakenLevel = 10;
        public const float ArtBoost = 1.10f;

        /// <summary>Training levels allowed per bond level (서먹, 친분, 신뢰, 의기투합).</summary>
        public static readonly int[] CapByBond = { 3, 5, 8, 10 };

        public static int LevelOf(SaveData save, string id)
        {
            foreach (var e in save.CompanionTraining) if (e.Id == id) return e.Level;
            return 0;
        }

        public static int Cap(SaveData save, string id) { return CapByBond[Math.Max(0, Math.Min(CapByBond.Length - 1, Bonds.Level(save, id)))]; }

        public static float StatMultiplier(int level) { return 1f + StatPerLevel * Math.Max(0, Math.Min(MaxLevel, level)); }

        /// <summary>두 for the step to <paramref name="toLevel"/> (400 for the first, 12,650 for the tenth).</summary>
        public static long CostFor(int toLevel)
        {
            if (toLevel < 1 || toLevel > MaxLevel) return -1;
            return (long)Math.Round(400.0 * Math.Pow(toLevel, 1.5) / 10.0) * 10;
        }

        public static long NextCost(SaveData save, string id) { return CostFor(LevelOf(save, id) + 1); }

        public static bool Train(SaveData save, string id, out string error)
        {
            error = null;
            var companion = EarlyGameContent.FindCompanion(id);
            if (companion == null || companion.JoinStage > save.HighestUnlockedStage) { error = "아직 함께하지 않는 동료입니다."; return false; }
            int level = LevelOf(save, id);
            if (level >= MaxLevel) { error = "이미 최고 수련 단계입니다."; return false; }
            if (level >= Cap(save, id)) { error = "인연이 더 깊어져야 이 이상 수련할 수 있습니다. (" + Bonds.LevelNames[Math.Min(3, Bonds.Level(save, id) + 1)] + " 필요)"; return false; }
            long cost = CostFor(level + 1);
            if (save.Doo < cost) { error = "은자가 모자랍니다. (" + cost.ToString("N0") + "은자)"; return false; }
            save.Doo -= cost;
            TrainingEntry entry = null;
            foreach (var e in save.CompanionTraining) if (e.Id == id) entry = e;
            if (entry == null) { entry = new TrainingEntry { Id = id }; save.CompanionTraining.Add(entry); }
            entry.Level = level + 1;
            return true;
        }

        public static int TotalLevels(SaveData save)
        {
            int n = 0;
            foreach (var e in save.CompanionTraining) n += e.Level;
            return n;
        }

        // ------------------------------------------------------------------ skill changes

        private sealed class Awakening
        {
            public string ArtId;
            public float PowerMul = 1f;
            public int HitsAdd, CooldownAdd;
            public float HealAdd;
        }

        private static readonly Dictionary<string, Awakening[]> Awakenings = new Dictionary<string, Awakening[]>
        {
            { "hyo_ryong", new[] { new Awakening { ArtId = "xr_twin", HitsAdd = 1 }, new Awakening { ArtId = "xr_barrier", CooldownAdd = -1 } } },
            { "jang_hong", new[] { new Awakening { ArtId = "jh_stun", PowerMul = 1.5f, CooldownAdd = -1 } } },
            { "yun_junho", new[] { new Awakening { ArtId = "yj_heal", HealAdd = 0.15f, CooldownAdd = -1 } } },
            { "na_yerin", new[] { new Awakening { ArtId = "ny_simahn", PowerMul = 1.3f }, new Awakening { ArtId = "ny_hansang", HitsAdd = 1 } } },
            { "im_seongjin", new[] { new Awakening { ArtId = "isj_iljeom", PowerMul = 1.3f }, new Awakening { ArtId = "isj_seonggwang", HitsAdd = 1 } } },
            { "namgung_sang", new[] { new Awakening { ArtId = "ns_noejeon", PowerMul = 1.3f }, new Awakening { ArtId = "ns_baekroe", HitsAdd = 1 } } },
            { "mo_yonghwi", new[] { new Awakening { ArtId = "mh_ilseom", PowerMul = 1.3f }, new Awakening { ArtId = "mh_mancheon", HitsAdd = 1 } } },
        };

        public static string AwakeningText(string companionId)
        {
            switch (companionId)
            {
                case "hyo_ryong": return "쌍검 연격 +1타, 태을천강벽 재사용 -1턴";
                case "jang_hong": return "점혈 위력 x1.5, 재사용 -1턴";
                case "yun_junho": return "자하신공 치유량 +15%p, 재사용 -1턴";
                case "na_yerin": return "심안검 위력 x1.3, 한상옥령신검 +1타";
                case "im_seongjin": return "일점필중 위력 x1.3, 성광일시 +1타";
                case "namgung_sang": return "뇌전검법 위력 x1.3, 백뢰비성시 +1타";
                case "mo_yonghwi": return "은하일섬 위력 x1.3, 은하유성만천 +1타";
            }
            return "";
        }

        /// <summary>Applies the level 3 / 6 / 10 changes to a companion's skills (their ids are the art ids).</summary>
        /// <summary>True when <paramref name="artId"/> took over <paramref name="olderId"/> through a chain of <see cref="ArtData.Replaces"/>.</summary>
        public static bool Succeeds(string artId, string olderId)
        {
            for (int guard = 0; guard < 8; guard++)
            {
                var art = EarlyGameContent.FindArt(artId);
                if (art == null || string.IsNullOrEmpty(art.Replaces)) return false;
                if (art.Replaces == olderId) return true;
                artId = art.Replaces;
            }
            return false;
        }

        public static void ApplyToSkills(string companionId, List<SkillSpec> skills, int level)
        {
            if (level >= ArtBoostLevel)
                foreach (var s in skills)
                {
                    s.Power *= ArtBoost;
                    s.HealPct *= ArtBoost;
                }
            if (level >= MainArtLevel)
            {
                SkillSpec main = null;
                float best = 0f;
                foreach (var s in skills)
                {
                    if (s.Effect != SkillEffect.None || s.Id == "basic" || s.SpiritCost > 0) continue;
                    float value = s.Power * Math.Max(1, s.Hits);
                    if (value > best) { best = value; main = s; }
                }
                if (main != null)
                {
                    main.QiCost = main.QiCost * 7 / 10;
                    main.Cooldown = Math.Max(0, main.Cooldown - 1);
                }
            }
            Awakening[] list;
            if (level >= AwakenLevel && Awakenings.TryGetValue(companionId, out list))
                foreach (var a in list)
                    foreach (var s in skills)
                    {
                        if (s.Id != a.ArtId && !Succeeds(s.Id, a.ArtId)) continue;   // an art that replaced the awakened one keeps its 각성
                        s.Power *= a.PowerMul;
                        s.Hits = Math.Max(1, s.Hits) + a.HitsAdd;
                        s.Cooldown = Math.Max(0, s.Cooldown + a.CooldownAdd);
                        s.HealPct += a.HealAdd;
                    }
        }

        /// <summary>The next unreached milestone as one line, or "" when everything is unlocked.</summary>
        public static string NextMilestone(string companionId, int level)
        {
            if (level < ArtBoostLevel) return "Lv." + ArtBoostLevel + ": 모든 무공 위력 +10%";
            if (level < MainArtLevel) return "Lv." + MainArtLevel + ": 주력 무공 내공 -30%, 재사용 -1턴";
            if (level < AwakenLevel) return "Lv." + AwakenLevel + " 각성: " + AwakeningText(companionId);
            return "";
        }

        /// <summary>One line per milestone for the UI, marking reached ones.</summary>
        public static string MilestoneText(string companionId, int level)
        {
            return (level >= ArtBoostLevel ? "● " : "○ ") + "Lv." + ArtBoostLevel + " 모든 무공 위력 +10%\n"
                + (level >= MainArtLevel ? "● " : "○ ") + "Lv." + MainArtLevel + " 주력 무공 내공 -30%, 재사용 -1턴\n"
                + (level >= AwakenLevel ? "● " : "○ ") + "Lv." + AwakenLevel + " 각성: " + AwakeningText(companionId);
        }
    }
}
