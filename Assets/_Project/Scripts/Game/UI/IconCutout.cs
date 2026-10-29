using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>One shared transparent-canvas material for painted item/menu icons; martial art remains untouched.</summary>
    public static class IconCutout
    {
        private static Material _material;
        private static Material _lockedCanvas, _lockedTransparent;
        private static readonly Color LockedInk = new Color(.48f, .50f, .49f, 1f);
        public static Material Material
        {
            get
            {
                if (_material == null)
                {
                    var shader = Resources.Load<Shader>("Shaders/IconCutout");
                    if (shader == null) return null;
                    _material = new Material(shader) { name = "TransparentIconCanvas", hideFlags = HideFlags.HideAndDontSave };
                }
                return _material;
            }
        }

        public static void Apply(Image image)
        {
            image.material = ArtLibrary.IsCutoutIcon(image.sprite) ? Material : null;
        }

        private static Material LockedMaterial(bool removeCanvas)
        {
            var material = removeCanvas ? _lockedCanvas : _lockedTransparent;
            if (material != null) return material;
            var shader = Resources.Load<Shader>("Shaders/IconCutout");
            if (shader == null) return null;
            material = new Material(shader) { name = removeCanvas ? "LockedPaintedIcon" : "LockedTransparentIcon", hideFlags = HideFlags.HideAndDontSave };
            material.SetFloat("_UniformSilhouette", 1f);
            material.SetFloat("_RemoveCanvas", removeCanvas ? 1f : 0f);
            material.SetColor("_Color", LockedInk);
            if (removeCanvas) _lockedCanvas = material;
            else _lockedTransparent = material;
            return material;
        }

        /// <summary>One muted ink tone for the sprite and every child glyph, preserving transparency.</summary>
        public static void ApplyLocked(Transform root)
        {
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                var image = graphic as Image;
                var glyph = graphic as WuxiaGlyph;
                graphic.material = LockedMaterial(image != null ? ArtLibrary.IsCutoutIcon(image.sprite) : glyph != null && glyph.UsesCutoutIcon);
                // Painted glyphs intentionally use white vertex RGB; tint them through the shared material too.
                graphic.color = new Color(1f, 1f, 1f, graphic.color.a);
            }
        }
    }
}
