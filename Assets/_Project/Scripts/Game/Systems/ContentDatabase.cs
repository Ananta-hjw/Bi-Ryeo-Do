using System.Collections.Generic;
using BiRyeoDo.Game.Data;
using UnityEngine;

namespace BiRyeoDo.Game.Systems
{
    /// <summary>Loads all content ScriptableObjects from Resources/Data once and indexes them.</summary>
    public static class ContentDatabase
    {
        public const string ResourcesPath = "Data";
        public const string StarterArtId = "sword_basic";

        private static bool _loaded;
        private static readonly List<StageDef> _stages = new List<StageDef>();
        private static readonly Dictionary<string, MartialArtDef> _arts = new Dictionary<string, MartialArtDef>();

        public static IReadOnlyList<StageDef> Stages { get { EnsureLoaded(); return _stages; } }

        public static StageDef GetStage(int number)
        {
            EnsureLoaded();
            foreach (var s in _stages) if (s.number == number) return s;
            return null;
        }

        /// <summary>First stage whose number is >= the requested one (so gaps in numbering don't break progression).</summary>
        public static StageDef GetStageAtOrAfter(int number)
        {
            EnsureLoaded();
            foreach (var s in _stages) if (s.number >= number) return s;
            return _stages.Count > 0 ? _stages[_stages.Count - 1] : null;
        }

        public static MartialArtDef GetArt(string id)
        {
            EnsureLoaded();
            MartialArtDef a;
            _arts.TryGetValue(id ?? string.Empty, out a);
            return a;
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _stages.AddRange(Resources.LoadAll<StageDef>(ResourcesPath));
            _stages.Sort((a, b) => a.number.CompareTo(b.number));
            foreach (var a in Resources.LoadAll<MartialArtDef>(ResourcesPath))
                if (!string.IsNullOrEmpty(a.id)) _arts[a.id] = a;
        }
    }
}
