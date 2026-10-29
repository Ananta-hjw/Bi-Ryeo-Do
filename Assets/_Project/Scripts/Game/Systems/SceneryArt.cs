using BiRyeoDo.Core;
using UnityEngine;

namespace BiRyeoDo.Game.Systems
{
    /// <summary>Atmospheric environment themes mapped to existing story settings, without changing stage rules.</summary>
    public static class SceneryArt
    {
        private static readonly string[] Stages =
        {
            "forest", "manor", "road", "road", "manor", "manor", "cave", "manor", "pavilion", "pavilion",
            "academy", "academy", "academy", "forest", "road", "manor", "academy", "forest", "cave", "cave",
            "road", "valley", "valley", "valley", "manor", "road", "valley", "manor", "cave", "valley",
            "manor", "academy", "academy", "manor", "manor", "academy",
            "forest", "academy", "manor", "road", "pavilion",
            "road", "manor", "academy", "academy", "academy", "cave", "academy",
            "cave", "academy", "manor", "cave", "cave", "academy", "manor", "road", "road",
            "road", "manor", "manor", "academy", "forest", "cave", "forest", "valley", "valley"
        };

        public static string StageKey(int stage)
        {
            return "battle_" + (stage >= 1 && stage <= Stages.Length ? Stages[stage - 1] : "road");
        }

        public static Sprite Battle(StageData stage)
        {
            return ArtLibrary.Environment(stage.IsTower ? "battle_academy" : stage.IsQuest ? "battle_road" : StageKey(stage.Number));
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

        /// <summary>Six decorative journey regions, one for each six-stage map page.</summary>
        public static string JourneyKey(int page)
        {
            return "world_journey_" + (Mathf.Clamp(page, 0, 5) + 1).ToString("00");
        }

        public static Sprite Journey(int page) { return ArtLibrary.Environment(JourneyKey(page)); }

        public static Sprite Menu(string key) { return ArtLibrary.Environment(MenuKey(key)); }
    }
}
