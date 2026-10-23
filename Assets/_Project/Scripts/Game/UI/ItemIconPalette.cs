using BiRyeoDo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Shared grade backgrounds that preserve the original metal and luminous effects.</summary>
    public static class ItemIconPalette
    {
        private static readonly Material[] Materials = new Material[7];

        public static Color Background(ItemGrade grade)
        {
            switch (grade)
            {
                case ItemGrade.Fine: return new Color(.075f, .160f, .115f, 1f);
                case ItemGrade.Rare: return new Color(.065f, .125f, .205f, 1f);
                case ItemGrade.Epic: return new Color(.140f, .095f, .205f, 1f);
                case ItemGrade.Legendary: return new Color(.220f, .140f, .065f, 1f);
                case ItemGrade.Mythic: return new Color(.215f, .080f, .100f, 1f);
                case ItemGrade.Transcendent: return new Color(.245f, .205f, .090f, 1f);
                default: return Color.clear;
            }
        }

        public static void Apply(Image image, ItemGrade grade)
        {
            int index = (int)grade;
            if (index <= 0 || index >= Materials.Length)
            {
                image.material = null;
                return;
            }
            if (Materials[index] == null)
            {
                var shader = Resources.Load<Shader>("Shaders/GradeIcon");
                if (shader == null) { image.material = null; return; }
                var material = new Material(shader) { name = "ItemBackground_" + grade, hideFlags = HideFlags.HideAndDontSave };
                material.SetColor("_Background", Background(grade));
                Materials[index] = material;
            }
            image.material = Materials[index];
        }
    }
}
