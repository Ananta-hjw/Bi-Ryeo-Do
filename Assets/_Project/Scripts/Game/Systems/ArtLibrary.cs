using System.Collections.Generic;
using UnityEngine;

namespace BiRyeoDo.Game.Systems
{
    /// <summary>
    /// Optional artwork dropped into <c>Assets/_Project/Resources/Art</c>. Files are matched by name, so nothing has to be wired up:
    /// <c>Backgrounds/stage_01.png</c>, <c>Enemies/tiger.png</c> (enemy id), <c>Characters/player.png</c>. A file is found in either <c>Enemies</c> or <c>Characters</c> (by id), so a misplaced upload still works.
    /// Any missing file returns null and the UI falls back to its flat-colour placeholder.
    /// Textures are turned into sprites at runtime, so the PNG import settings do not matter.
    /// </summary>
    public static class ArtLibrary
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Background(int stageNumber) { return Load("Backgrounds/stage_" + stageNumber.ToString("00")); }
        public static Sprite Enemy(string enemyId) { return Load("Enemies/" + enemyId) ?? Load("Characters/" + enemyId); }
        public static Sprite Player() { return Load("Characters/player"); }
        /// <summary>Party member portrait: <c>Characters/&lt;id&gt;.png</c> (e.g. hyo_ryong.png).</summary>
        public static Sprite Companion(string companionId) { return Load("Characters/" + companionId) ?? Load("Enemies/" + companionId); }

        /// <summary>Portrait by character name: the protagonist, a companion, or an enemy/NPC whose name matches (e.g. 나예린 → the 빙백봉 art).</summary>
        public static Sprite Portrait(string name)
        {
            if (name == BiRyeoDo.Core.StoryLine.Protagonist) return Player();
            foreach (var c in BiRyeoDo.Core.EarlyGameContent.Companions) if (c.Name == name) return Companion(c.Id);
            foreach (var e in BiRyeoDo.Core.EarlyGameContent.Enemies)
                if (e.Name == name || (name.Length >= 2 && e.Name.Contains(name))) { var sprite = Enemy(e.ArtKey); if (sprite != null) return sprite; }
            return null;
        }

        /// <summary>Stable muted colour for a character without artwork.</summary>
        public static Color TintFor(string name)
        {
            int h = 17;
            foreach (char ch in name) h = h * 31 + ch;
            float hue = Mathf.Abs(h % 360) / 360f;
            return Color.HSVToRGB(hue, 0.35f, 0.45f);
        }

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
