using System;
using System.Collections.Generic;
using UnityEngine;

namespace BiRyeoDo.Game.Systems
{
    /// <summary>Individually mapped painted icons, shared by lists, details and battle commands.</summary>
    public static class ReadableIconLibrary
    {
        [Serializable]
        private sealed class Catalog { public Entry[] entries = Array.Empty<Entry>(); }
        [Serializable]
        private sealed class Entry
        {
            public string category = "", id = "", sheet = "";
            public int cell = 0, columns = 8, rows = 8;
            public float x = 0f, y = 0f, width = 0f, height = 0f;
            public bool transparent = false;
        }

        private static Dictionary<string, Entry> _catalog;
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Texture2D> Sheets = new Dictionary<string, Texture2D>();

        public static bool HasTransparentCanvas(string category, string id)
        {
            EnsureCatalog();
            Entry entry;
            return id != null && _catalog.TryGetValue(category + "/" + id, out entry) && entry.transparent;
        }

        public static Sprite Find(string category, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            EnsureCatalog();
            string key = category + "/" + id;
            Sprite sprite;
            if (Sprites.TryGetValue(key, out sprite)) return sprite;
            Entry entry;
            if (!_catalog.TryGetValue(key, out entry) || entry.columns < 1 || entry.rows < 1 || entry.cell < 0 || entry.cell >= entry.columns * entry.rows) return null;
            Texture2D texture;
            if (!Sheets.TryGetValue(entry.sheet, out texture))
            {
                texture = Resources.Load<Texture2D>("Art/Readable/" + entry.sheet);
                Sheets[entry.sheet] = texture;
            }
            if (texture == null) return null;
            float width = texture.width / (float)entry.columns, height = texture.height / (float)entry.rows;
            // Explicit regions follow measured dividers; equal-grid slicing is only a legacy fallback.
            // Omit seams and keep bilinear filtering inside the illustration.
            const float inset = 3f;
            var rect = entry.width > 0f && entry.height > 0f
                ? new Rect(entry.x * texture.width + inset,
                    (1f - entry.y - entry.height) * texture.height + inset,
                    entry.width * texture.width - inset * 2f, entry.height * texture.height - inset * 2f)
                : new Rect(entry.cell % entry.columns * width + inset,
                    texture.height - (entry.cell / entry.columns + 1) * height + inset,
                    width - inset * 2f, height - inset * 2f);
            if (rect.xMin < 0f || rect.yMin < 0f || rect.xMax > texture.width || rect.yMax > texture.height) return null;
            sprite = Sprite.Create(texture, rect, new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = key;
            Sprites[key] = sprite;
            return sprite;
        }

        private static void EnsureCatalog()
        {
            if (_catalog != null) return;
            _catalog = new Dictionary<string, Entry>();
            var file = Resources.Load<TextAsset>("Art/Readable/catalog");
            if (file == null) return;
            var catalog = JsonUtility.FromJson<Catalog>(file.text);
            if (catalog == null || catalog.entries == null) return;
            foreach (var entry in catalog.entries)
                if (entry != null && !string.IsNullOrEmpty(entry.id) && !string.IsNullOrEmpty(entry.sheet))
                    _catalog[entry.category + "/" + entry.id] = entry;
        }
    }
}
