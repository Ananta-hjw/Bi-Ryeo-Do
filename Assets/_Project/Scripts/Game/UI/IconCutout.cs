using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>One shared transparent-canvas material for painted item/menu icons; martial art remains untouched.</summary>
    public static class IconCutout
    {
        private static Material _material;
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
    }
}
