using BiRyeoDo.Core;
using UnityEngine;

namespace BiRyeoDo.Game.Systems
{
    /// <summary>Atmospheric environment themes mapped to existing story settings, without changing stage rules.</summary>
    public static class SceneryArt
    {
        public const int StagesPerJourney = 6;
        public static int JourneyPageCount { get { return (EarlyGameContent.Stages.Length + StagesPerJourney - 1) / StagesPerJourney; } }

        private static readonly string[] OpeningStages =
        {
            "forest", "manor", "road", "road", "manor", "manor"
        };

        public static string StageKey(int stage)
        {
            if (stage >= 7 && stage <= 66) return "stage_" + stage.ToString("00");
            return "battle_" + (stage >= 1 && stage <= OpeningStages.Length ? OpeningStages[stage - 1] : "road");
        }

        public static Sprite Battle(StageData stage)
        {
            return ArtLibrary.Environment(stage.IsTower ? "battle_academy" : StageKey(stage.Number));
        }

        public static string MenuKey(string key)
        {
            switch (key)
            {
                case "companions": return "menu_inn";
                case "equipment": return "menu_forge";
                case "arts": return "battle_academy";
                case "gacha": return "battle_pavilion";
                case "bag": return "menu_inn";
                case "record": return "menu_archive";
                default: return "battle_forest";
            }
        }

        /// <summary>Decorative settings for secondary menus; no story or reward rules depend on this mapping.</summary>
        public static string PanelKey(string name)
        {
            if (name.Contains("Shop") || name.Contains("Faction") || name.Contains("Companion")) return "companions";
            if (name.Contains("Codex") || name.Contains("Ach") || name.Contains("Story") || name.Contains("Memory") || name.Contains("Ritual")) return "record";
            if (name.Contains("Inner") || name.Contains("Art") || name.Contains("Tower") || name.Contains("Daily") || name.Contains("Promotion")) return "arts";
            if (name.Contains("Gear") || name.Contains("Equipment")) return "equipment";
            if (name.Contains("Gacha")) return "gacha";
            return "world";
        }

        /// <summary>One decorative landscape for every six-stage page, including the final story chapters.</summary>
        public static string JourneyKey(int page)
        {
            return "world_journey_" + (Mathf.Clamp(page, 0, JourneyPageCount - 1) + 1).ToString("00");
        }

        public static Sprite Journey(int page) { return ArtLibrary.Environment(JourneyKey(page)); }

        public static Sprite Menu(string key) { return ArtLibrary.Environment(MenuKey(key)); }
    }
}
