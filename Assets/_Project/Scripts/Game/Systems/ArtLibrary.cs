using System.Collections.Generic;
using UnityEngine;

namespace BiRyeoDo.Game.Systems
{
    /// <summary>
    /// Painted UI icons come from ReadableIconLibrary; other artwork and legacy fallbacks are dropped into <c>Assets/_Project/Resources/Art</c>. Files are matched by name, so nothing has to be wired up:
    /// <c>Backgrounds/battle_forest.png</c>, <c>Characters/tiger.png</c> (enemy id), <c>Characters/player.png</c>, <c>Items/laundry_bat.png</c> (item id). Player, companions, enemies and story-only NPCs all live in <c>Characters</c>, found by id.
    /// Any missing file returns null and the UI falls back to its flat-colour placeholder.
    /// Sprites are cached at runtime. Readable sheets preserve their original dimensions and disable mipmaps.
    /// </summary>
    public static class ArtLibrary
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Background(int stageNumber) { return Environment(SceneryArt.StageKey(stageNumber)); }
        public static Sprite Environment(string key) { return Load("Backgrounds/" + key); }
        private static readonly HashSet<Sprite> CutoutIcons = new HashSet<Sprite>();
        private static Sprite Cutout(Sprite sprite) { if (sprite != null) CutoutIcons.Add(sprite); return sprite; }
        public static bool IsCutoutIcon(Sprite sprite) { return sprite != null && CutoutIcons.Contains(sprite); }
        public static Sprite Enemy(string enemyId) { return Load("Characters/" + enemyId); }
        /// <summary>Painted menu/command icon first; legacy fallback: <c>Icons/&lt;key&gt;.png</c> (daily, quest, fortune, star, tower, stage, promotion, party).</summary>
        public static Sprite Icon(string key) { return Cutout(ReadableIconLibrary.Find("Icon", key) ?? Load("Icons/" + key)); }
        /// <summary>Individually mapped item picture first; legacy fallback: <c>Items/&lt;item id&gt;.png</c>, else the kind's default <c>Items/_weapon|_armor|_pants|_boots|_accessory|_consumable|_material|_key.png</c>.</summary>
        public static Sprite Item(BiRyeoDo.Core.ItemData item)
        {
            if (item == null) return null;
            return Cutout(ReadableIconLibrary.Find("Item", item.Id) ?? Load("Items/" + item.Id)
                ?? ReadableIconLibrary.Find("DefaultItem", item.Kind.ToString()) ?? Load("Items/_" + item.Kind.ToString().ToLowerInvariant()));
        }
        /// <summary>Individually mapped martial-art picture first; legacy fallback: <c>Arts/&lt;art id&gt;.png</c> (also the skill id in battle); null when missing.</summary>
        public static Sprite Art(string artId) { return string.IsNullOrEmpty(artId) ? null : ReadableIconLibrary.Find("Art", artId) ?? Load("Arts/" + artId); }
        /// <summary>Individual passive-method icon, shared with the martial-arts workspace.</summary>
        public static Sprite InnerArt(string id) { return ReadableIconLibrary.Find("Inner", id); }
        /// <summary>SD figure source: <c>Battle/&lt;id&gt;.png</c>, used on the field, combat summaries and compact character pickers.</summary>
        public static Sprite Chibi(string id) { return string.IsNullOrEmpty(id) ? null : Load("Battle/" + id); }
        /// <summary>Stand-in SD figure when a dedicated <c>Battle/</c> picture is missing: <c>Battle/default_assassin.png</c> for bandits and assassins, <c>default_woman.png</c> for the women listed in <c>Genders</c>, else <c>default_man.png</c> (all under <c>Battle/</c>). Null when that file is missing too.</summary>
        public static Sprite DefaultChibi(string artKey) { return Load("Battle/" + BiRyeoDo.Core.Genders.DefaultFigure(artKey)); }
        public static Sprite Player() { return Load("Characters/player"); }
        /// <summary>Party member portrait: <c>Characters/&lt;id&gt;.png</c> (e.g. hyo_ryong.png).</summary>
        public static Sprite Companion(string companionId) { return Load("Characters/" + companionId); }

        /// <summary>Stable filename for a character name or ID; enemy art keys may differ from their battle IDs.</summary>
        private static string CharacterKey(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (name == BiRyeoDo.Core.StoryLine.Protagonist) return "player";
            foreach (var companion in BiRyeoDo.Core.EarlyGameContent.Companions)
                if (companion.Name == name || companion.Id == name) return companion.Id;
            var cast = BiRyeoDo.Core.StoryCast.Find(name);
            if (cast != null) return cast.Id;
            foreach (var enemy in BiRyeoDo.Core.EarlyGameContent.Enemies)
                if (enemy.Name == name || enemy.Id == name) return enemy.ArtKey;
            // Dialogue often uses a short title (e.g. 염도) rather than the enemy's complete name.
            foreach (var enemy in BiRyeoDo.Core.EarlyGameContent.Enemies)
                if (name.Length >= 2 && enemy.Name.Contains(name)) return enemy.ArtKey;
            return name;
        }

        /// <summary>All compact/combat character contexts stay in Battle/, including the fallback.</summary>
        public static Sprite SdFigure(string nameOrId)
        {
            if (string.IsNullOrEmpty(nameOrId)) return null;
            var exact = Chibi(nameOrId);
            if (exact != null) return exact;
            string key = CharacterKey(nameOrId);
            return Chibi(key) ?? DefaultChibi(key);
        }

        /// <summary>Original non-combat art. Any character can be converted by adding Standing/&lt;id&gt;.png.</summary>
        public static Sprite StandingPortrait(string name)
        {
            string key = CharacterKey(name);
            return key == null ? null : Load("Standing/" + key);
        }

        /// <summary>Default presentation rule outside SD contexts: Standing first, then Characters.</summary>
        public static Sprite CharacterPortrait(string name)
        {
            string key = CharacterKey(name);
            return key == null ? null : StandingPortrait(name) ?? Portrait(name) ?? Load("Characters/" + key);
        }

        public static Sprite LobbyPortrait(string name) { return CharacterPortrait(name); }

        /// <summary>The lobby and dialogue share standing originals; unconverted portraits retain soft edges.</summary>
        public static Sprite DialoguePortrait(string name)
        {
            return StandingPortrait(name) ?? Feathered(CharacterPortrait(name));
        }

        /// <summary>Six painted lobby tiles, sliced at runtime without importing a sprite sheet.</summary>
        public static Sprite LobbyCard(string key)
        {
            string[] keys = { "companion", "equipment", "arts", "gacha", "bag", "record" };
            int index = System.Array.IndexOf(keys, key);
            if (index < 0) return Icon(key);
            string cacheKey = "LobbyCard/" + key;
            Sprite sprite;
            if (Cache.TryGetValue(cacheKey, out sprite)) return sprite;
            var atlas = Resources.Load<Texture2D>("Art/Lobby/menu_cards");
            if (atlas == null) return Icon(key);
            float width = atlas.width / 3f, height = atlas.height / 2f;
            // A small inset keeps neighbouring tile pixels outside bilinear sampling.
            sprite = Sprite.Create(atlas, new Rect(index % 3 * width + 3f, (1 - index / 3) * height + 3f,
                width - 6f, height - 6f), new Vector2(.5f, .5f), 100f);
            Cache[cacheKey] = sprite;
            return sprite;
        }

        /// <summary>Portrait by character name: the protagonist, a companion, or an enemy/NPC whose name matches (e.g. 나예린 → the 빙백봉 art).</summary>
        public static Sprite Portrait(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
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
        /// A picture that is already cut out (its border is mostly transparent) is returned unchanged. Works on non-readable textures by going through a RenderTexture, reads only the sprite's own rect,
        /// and is cached (the first call per picture costs one GPU read-back and one pass over at most 1024 px wide).
        /// </summary>
        public static Sprite Feathered(Sprite sprite, float fade = 0.07f)
        {
            if (sprite == null) return null;
            Sprite cached;
            if (FeatherCache.TryGetValue(sprite, out cached)) return cached;
            Sprite result = sprite;
            Texture2D tex = null;
            RenderTexture rt = null;
            var prev = RenderTexture.active;
            try
            {
                var src = sprite.texture;
                var r = sprite.rect;
                int w = Mathf.Max(1, Mathf.Min(Mathf.RoundToInt(r.width), 1024)), h = Mathf.Max(1, Mathf.RoundToInt(w * r.height / r.width));
                rt = Downscale(src, r, w, h);
                RenderTexture.active = rt;
                tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                RenderTexture.active = prev;

                var px = tex.GetPixels32();
                if (IsCutOut(px, w, h)) { Object.Destroy(tex); tex = null; }
                else
                {
                    // separable fade: one weight per column and per row, multiplied per pixel
                    var wx = new float[w];
                    for (int x = 0; x < w; x++) wx[x] = Smooth(Mathf.Min(x, w - 1 - x) / (fade * w));
                    for (int y = 0; y < h; y++)
                    {
                        float fy = Smooth(Mathf.Min(y, h - 1 - y) / (fade * h));
                        int row = y * w;
                        for (int x = 0; x < w; x++)
                        {
                            float k = fy * wx[x];
                            if (k >= 1f) continue;
                            px[row + x].a = (byte)(px[row + x].a * k + 0.5f);
                        }
                    }
                    tex.SetPixels32(px);
                    tex.Apply(false, true);
                    result = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
                    tex = null;   // owned by the sprite now
                }
            }
            catch (System.Exception) { result = sprite; }
            finally
            {
                RenderTexture.active = prev;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                if (tex != null) Object.Destroy(tex);
            }
            FeatherCache[sprite] = result;
            return result;
        }

        /// <summary>The sprite's rect of <paramref name="src"/> at w x h, halving in steps so a big picture does not alias (the source has no mipmaps).</summary>
        private static RenderTexture Downscale(Texture src, Rect r, int w, int h)
        {
            var scale = new Vector2(r.width / src.width, r.height / src.height);
            var offset = new Vector2(r.x / src.width, r.y / src.height);
            int cw = Mathf.RoundToInt(r.width), ch = Mathf.RoundToInt(r.height);
            while (cw / 2 >= w * 2) { cw /= 2; ch /= 2; }
            var cur = RenderTexture.GetTemporary(Mathf.Max(w, cw), Mathf.Max(h, ch), 0, RenderTextureFormat.ARGB32);
            cur.filterMode = FilterMode.Bilinear;
            Graphics.Blit(src, cur, scale, offset);
            while (cur.width > w)
            {
                int nw = Mathf.Max(w, cur.width / 2), nh = cur.width / 2 <= w ? h : Mathf.Max(h, cur.height / 2);
                var next = RenderTexture.GetTemporary(nw, nh, 0, RenderTextureFormat.ARGB32);
                next.filterMode = FilterMode.Bilinear;
                Graphics.Blit(cur, next);
                RenderTexture.ReleaseTemporary(cur);
                cur = next;
            }
            return cur;
        }

        /// <summary>True when most of the border is transparent (a cut-out figure; its feet may touch the edge). Rounded corners alone do not count.</summary>
        private static bool IsCutOut(Color32[] px, int w, int h)
        {
            const int perEdge = 16;
            int clear = 0, total = 0;
            for (int i = 0; i < perEdge; i++)
            {
                int x = (int)((i + 0.5f) * w / perEdge), y = (int)((i + 0.5f) * h / perEdge);
                if (px[x].a < 40) clear++;
                if (px[(h - 1) * w + x].a < 40) clear++;
                if (px[y * w].a < 40) clear++;
                if (px[y * w + w - 1].a < 40) clear++;
                total += 4;
            }
            return clear >= total * 0.6f;
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
