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
            "manor", "academy", "academy"
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

        public static Sprite Menu(string key) { return ArtLibrary.Environment(MenuKey(key)); }
    }
}
