using System.Collections.Generic;
using UnityEngine;

namespace BiRyeoDo.Game.Systems
{
    /// <summary>
    /// Optional artwork dropped into <c>Assets/_Project/Resources/Art</c>. Files are matched by name, so nothing has to be wired up:
    /// <c>Backgrounds/stage_01.png</c>, <c>Enemies/tiger.png</c> (enemy id), <c>Characters/player.png</c>.
    /// Any missing file returns null and the UI falls back to its flat-colour placeholder.
    /// Textures are turned into sprites at runtime, so the PNG import settings do not matter.
    /// </summary>
    public static class ArtLibrary
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Background(int stageNumber) { return Load("Backgrounds/stage_" + stageNumber.ToString("00")); }
        public static Sprite Enemy(string enemyId) { return Load("Enemies/" + enemyId); }
        public static Sprite Player() { return Load("Characters/player"); }

        private static Sprite Load(string path)
        {
            Sprite sprite;
            if (Cache.TryGetValue(path, out sprite)) return sprite;

            var tex = Resources.Load<Texture2D>("Art/" + path);
            sprite = tex == null ? null : Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            Cache[path] = sprite;
            return sprite;
        }
    }
}
