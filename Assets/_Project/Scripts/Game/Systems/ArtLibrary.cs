using System.Collections.Generic;
using UnityEngine;

namespace BiRyeoDo.Game.Systems
{
    /// <summary>
    /// Optional artwork dropped into <c>Assets/_Project/Resources/Art</c>. Files are matched by name, so nothing has to be wired up:
    /// <c>Backgrounds/stage_01.png</c>, <c>Characters/tiger.png</c> (enemy id), <c>Characters/player.png</c>, <c>Items/laundry_bat.png</c> (item id). Player, companions, enemies and story-only NPCs all live in <c>Characters</c>, found by id.
    /// Any missing file returns null and the UI falls back to its flat-colour placeholder.
    /// Textures are turned into sprites at runtime, so the PNG import settings do not matter.
    /// </summary>
    public static class ArtLibrary
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Background(int stageNumber) { return Load("Backgrounds/stage_" + stageNumber.ToString("00")); }
        public static Sprite Enemy(string enemyId) { return Load("Characters/" + enemyId); }
        /// <summary>Home-screen button icon: <c>Icons/&lt;key&gt;.png</c> (daily, quest, fortune, star, tower, stage, promotion, party).</summary>
        public static Sprite Icon(string key) { return Load("Icons/" + key); }
        /// <summary>Item picture: <c>Items/&lt;item id&gt;.png</c>, else the kind's default <c>Items/_weapon|_armor|_pants|_boots|_accessory|_consumable|_material|_key.png</c>.</summary>
        public static Sprite Item(BiRyeoDo.Core.ItemData item)
        {
            if (item == null) return null;
            return Load("Items/" + item.Id) ?? Load("Items/_" + item.Kind.ToString().ToLowerInvariant());
        }
        /// <summary>Martial art picture: <c>Arts/&lt;art id&gt;.png</c> (also the skill id in battle); null when missing.</summary>
        public static Sprite Art(string artId) { return string.IsNullOrEmpty(artId) ? null : Load("Arts/" + artId); }
        /// <summary>Small chibi figure used only on the battle field: <c>Battle/&lt;id&gt;.png</c> (player, companion id or enemy ArtKey), drawn facing right. Null when missing, and the battle falls back to the large portrait.</summary>
        public static Sprite Chibi(string id) { return string.IsNullOrEmpty(id) ? null : Load("Battle/" + id); }
        /// <summary>Stand-in battle figure for a character with neither a <c>Battle/</c> nor a <c>Characters/</c> picture: <c>Battle/default_assassin.png</c> for bandits and assassins, <c>default_woman.png</c> for the women listed in <c>Genders</c>, else <c>default_man.png</c> (all under <c>Battle/</c>). Null when that file is missing too.</summary>
        public static Sprite DefaultChibi(string artKey) { return Load("Battle/" + BiRyeoDo.Core.Genders.DefaultFigure(artKey)); }
        public static Sprite Player() { return Load("Characters/player"); }
        /// <summary>Party member portrait: <c>Characters/&lt;id&gt;.png</c> (e.g. hyo_ryong.png).</summary>
        public static Sprite Companion(string companionId) { return Load("Characters/" + companionId); }

        /// <summary>Portrait by character name: the protagonist, a companion, or an enemy/NPC whose name matches (e.g. 나예린 → the 빙백봉 art).</summary>
        public static Sprite Portrait(string name)
        {
            if (name == BiRyeoDo.Core.StoryLine.Protagonist) return Player();
            foreach (var c in BiRyeoDo.Core.EarlyGameContent.Companions) if (c.Name == name) return Companion(c.Id);
            var cast = BiRyeoDo.Core.StoryCast.Find(name);
            if (cast != null) { var sprite = Companion(cast.Id); if (sprite != null) return sprite; }
            foreach (var e in BiRyeoDo.Core.EarlyGameContent.Enemies)
                if (e.Name == name || (name.Length >= 2 && e.Name.Contains(name))) { var sprite = Enemy(e.ArtKey); if (sprite != null) return sprite; }
            return null;
        }

        private static readonly Dictionary<Sprite, Sprite> FeatherCache = new Dictionary<Sprite, Sprite>();

        /// <summary>
        /// For full-bleed pictures (an opaque rectangle with a painted background): a copy whose four edges fade out, so the picture melts into whatever is behind it instead of showing a hard box.
        /// A picture that is already cut out (transparent corners) is returned unchanged. Works on non-readable textures by going through a RenderTexture and is cached.
        /// </summary>
        public static Sprite Feathered(Sprite sprite, float fade = 0.16f)
        {
            if (sprite == null) return null;
            Sprite cached;
            if (FeatherCache.TryGetValue(sprite, out cached)) return cached;
            Sprite result = sprite;
            try
            {
                var src = sprite.texture;
                int w = Mathf.Min(src.width, 1024), h = Mathf.Max(1, Mathf.RoundToInt(w * (float)src.height / src.width));
                var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(src, rt);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);

                var px = tex.GetPixels32();
                bool cutOut = px[0].a < 40 && px[w - 1].a < 40 && px[(h - 1) * w].a < 40 && px[h * w - 1].a < 40;
                if (cutOut) { Object.Destroy(tex); }
                else
                {
                    for (int y = 0; y < h; y++)
                    {
                        float fy = Smooth(Mathf.Min(y, h - 1 - y) / (fade * h));
                        for (int x = 0; x < w; x++)
                        {
                            float k = fy * Smooth(Mathf.Min(x, w - 1 - x) / (fade * w));
                            var c = px[y * w + x];
                            c.a = (byte)Mathf.RoundToInt(c.a * k);
                            px[y * w + x] = c;
                        }
                    }
                    tex.SetPixels32(px);
                    tex.Apply(false, true);
                    result = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
                }
            }
            catch (System.Exception) { result = sprite; }
            FeatherCache[sprite] = result;
            return result;
        }

        private static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

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
