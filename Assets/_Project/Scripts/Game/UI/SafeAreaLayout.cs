using UnityEngine;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Updates the usable canvas bounds after a resize, device flip, or notch change.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaLayout : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastArea;
        private int _width, _height;

        public static Rect Normalize(Rect area, int width, int height)
        {
            if (width <= 0 || height <= 0 || area.width <= 0f || area.height <= 0f)
                return new Rect(0f, 0f, 1f, 1f);
            float left = Mathf.Clamp01(area.xMin / width), right = Mathf.Clamp01(area.xMax / width);
            float bottom = Mathf.Clamp01(area.yMin / height), top = Mathf.Clamp01(area.yMax / height);
            return right > left && top > bottom ? Rect.MinMaxRect(left, bottom, right, top) : new Rect(0f, 0f, 1f, 1f);
        }

        private void OnEnable() { Apply(true); }
        private void LateUpdate() { Apply(false); }

        private void Apply(bool force)
        {
            if (!force && _width == Screen.width && _height == Screen.height && _lastArea == Screen.safeArea) return;
            if (_rect == null) _rect = (RectTransform)transform;
            _width = Screen.width; _height = Screen.height; _lastArea = Screen.safeArea;
            var area = Normalize(_lastArea, _width, _height);
            _rect.anchorMin = area.min; _rect.anchorMax = area.max;
            _rect.offsetMin = _rect.offsetMax = Vector2.zero;
        }
    }
}
