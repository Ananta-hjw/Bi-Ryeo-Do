using System;
using UnityEngine;

namespace BiRyeoDo.Game.Systems
{
    /// <summary>Independent exploration scenery. Atlas rectangles use unscaled source pixels.</summary>
    public static class AcademyScenery
    {
        [Serializable]
        private sealed class Atlas { public int width, height; public Region[] frames; }
        [Serializable]
        private sealed class Region { public int x, y, width, height; }
        private static Sprite[] _facilities;

        public static Sprite Ground { get { return ArtLibrary.Exploration("Academy/courtyard"); } }

        public static Sprite Facility(int index)
        {
            if (index < 0 || index >= 3) return null;
            if (_facilities == null)
            {
                var texture = Resources.Load<Texture2D>("Art/Exploration/Academy/facilities");
                var metadata = Resources.Load<TextAsset>("Art/Exploration/Academy/facilities_atlas");
                if (texture == null || metadata == null) return null;
                Atlas atlas;
                try { atlas = JsonUtility.FromJson<Atlas>(metadata.text); }
                catch (Exception) { return null; }
                if (atlas == null || atlas.width != texture.width || atlas.height != texture.height
                    || atlas.frames == null || atlas.frames.Length != 3) return null;
                foreach (var f in atlas.frames)
                    if (f == null || f.x < 0 || f.y < 0 || f.width <= 0 || f.height <= 0
                        || f.x + f.width > texture.width || f.y + f.height > texture.height) return null;
                _facilities = new Sprite[3];
                for (int i = 0; i < 3; i++)
                {
                    var f = atlas.frames[i];
                    _facilities[i] = Sprite.Create(texture, new Rect(f.x, f.y, f.width, f.height), new Vector2(.5f, 0f), 100f);
                    _facilities[i].name = "AcademyFacility_" + i;
                }
            }
            return _facilities[index];
        }
    }
}
