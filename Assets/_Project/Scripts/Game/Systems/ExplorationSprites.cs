using System;
using UnityEngine;

namespace BiRyeoDo.Game.Systems
{
    /// <summary>Loads measured atlas rectangles without requiring CPU-readable textures.</summary>
    public static class ExplorationSprites
    {
        [Serializable]
        private sealed class Atlas { public int width, height; public Frame[] frames; }
        [Serializable]
        private sealed class Frame { public int x, y, width, height; }
        private static Sprite[] _player;

        public static Sprite[] Player()
        {
            if (_player != null) return _player;
            var texture = Resources.Load<Texture2D>("Art/Exploration/player_walk");
            var metadata = Resources.Load<TextAsset>("Art/Exploration/player_walk_atlas");
            if (texture == null || metadata == null) return null;
            Atlas atlas;
            try { atlas = JsonUtility.FromJson<Atlas>(metadata.text); }
            catch (Exception) { return null; }
            // Rectangles use source pixels; an import resize would invalidate them.
            if (atlas == null || texture.width != atlas.width || texture.height != atlas.height
                || atlas.frames == null || atlas.frames.Length != 16) return null;
            foreach (var f in atlas.frames)
                if (f == null || f.x < 0 || f.y < 0 || f.width <= 0 || f.height <= 0
                    || f.x + f.width > texture.width || f.y + f.height > texture.height) return null;
            var sprites = new Sprite[16];
            for (int i = 0; i < sprites.Length; i++)
            {
                var f = atlas.frames[i];
                sprites[i] = Sprite.Create(texture, new Rect(f.x, f.y, f.width, f.height), new Vector2(.5f, 0f), 100f);
                sprites[i].name = "ExplorationPlayer_" + i;
            }
            _player = sprites;
            return _player;
        }
    }
}
