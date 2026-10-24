using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Keeps the figure inside its cell's art region after a safe-area or viewport resize.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class BattleFigureFit : MonoBehaviour
    {
        private Vector2 _lastSize = new Vector2(-1f, -1f);
        private Sprite _lastSprite;

        private void OnEnable() { _lastSize = new Vector2(-1f, -1f); Fit(); }
        private void LateUpdate() { Fit(); }

        public void Fit()
        {
            var image = GetComponent<Image>();
            var rect = (RectTransform)transform;
            var cell = rect.parent as RectTransform;
            if (cell == null || image.sprite == null) return;
            var size = cell.rect.size;
            if (size.x <= 0f || size.y <= 0f || (size == _lastSize && image.sprite == _lastSprite)) return;
            _lastSize = size; _lastSprite = image.sprite;
            float aspect = image.sprite.rect.width / Mathf.Max(1f, image.sprite.rect.height);
            float height = Mathf.Min(size.y * .62f, size.x * .96f / aspect);   // feet at .38 of the cell, head up to its top
            rect.sizeDelta = new Vector2(height * aspect, height);
        }
    }
}
