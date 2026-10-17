using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// Item pictures: <c>Resources/Art/Items/&lt;item id&gt;.png</c>, falling back to <c>Items/_&lt;kind&gt;.png</c> (weapon, armor, accessory, consumable, material, key)
    /// and finally to a kind-coloured square with the first letter of the name.
    /// </summary>
    internal static class ItemArt
    {
        public static Color KindColor(ItemData item)
        {
            switch (item.Kind)
            {
                case ItemKind.Weapon: return new Color(0.55f, 0.18f, 0.18f, 1f);
                case ItemKind.Armor: return new Color(0.18f, 0.34f, 0.58f, 1f);
                case ItemKind.Accessory: return new Color(0.38f, 0.22f, 0.55f, 1f);
                case ItemKind.Consumable: return new Color(0.20f, 0.46f, 0.30f, 1f);
                case ItemKind.Key: return new Color(0.62f, 0.44f, 0.12f, 1f);
                default: return new Color(0.40f, 0.30f, 0.20f, 1f);
            }
        }

        /// <summary>Shows the item's picture in <paramref name="icon"/> (or the coloured placeholder with a letter in <paramref name="glyph"/>).</summary>
        public static void Apply(Image icon, Text glyph, ItemData item)
        {
            var sprite = ArtLibrary.Item(item);
            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.color = Color.white;
                icon.preserveAspect = true;
                if (glyph != null) glyph.text = "";
            }
            else
            {
                icon.sprite = null;
                icon.color = KindColor(item);
                if (glyph != null) glyph.text = item.Name.Substring(0, 1);
            }
        }
    }
}
