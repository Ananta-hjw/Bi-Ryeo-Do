using System.Collections.Generic;
using UnityEngine;

namespace BiRyeoDo.Game.Systems
{
    /// <summary>
    /// Painted UI icons come from ReadableIconLibrary; other artwork and legacy fallbacks are dropped into <c>Assets/_Project/Resources/Art</c>. Files are matched by name, so nothing has to be wired up:
    /// <c>Backgrounds/battle_forest.png</c>, <c>Standing/tiger.png</c> (enemy art key), <c>Standing/player.png</c>, <c>Items/laundry_bat.png</c> (item id). Player, companions, enemies and story-only NPCs all live in <c>Standing</c> (non-combat art) and <c>Battle</c> (SD figures), found by id.
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

        /// <summary>Non-combat art of a character: <c>Standing/&lt;id&gt;.png</c>; null when missing.</summary>
        public static Sprite StandingPortrait(string name)
        {
            string key = CharacterKey(name);
            return key == null ? null : Load("Standing/" + key);
        }

        /// <summary>Picture used outside SD contexts (profiles, codex, formation): the standing art.</summary>
        public static Sprite CharacterPortrait(string name) { return StandingPortrait(name); }

        public static Sprite LobbyPortrait(string name) { return CharacterPortrait(name); }

        /// <summary>The lobby and dialogue share the standing art.</summary>
        public static Sprite DialoguePortrait(string name) { return StandingPortrait(name); }

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
