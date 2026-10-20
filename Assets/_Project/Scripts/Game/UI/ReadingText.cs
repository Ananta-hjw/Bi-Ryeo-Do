using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Keeps a fixed-size paragraph inside a scroll viewport; text and width changes remeasure it.</summary>
    public sealed class ReadingText : MonoBehaviour
    {
        public RectTransform Viewport;
        public ScrollRect Scroll;
        private Text _text;
        private string _lastText;
        private float _width = -1f, _height = -1f;
        private bool _dirty = true;

        private void Awake() { _text = GetComponent<Text>(); _text.RegisterDirtyVerticesCallback(Invalidate); }
        private void OnEnable() { _lastText = null; _dirty = true; }
        private void OnDestroy() { if (_text != null) _text.UnregisterDirtyVerticesCallback(Invalidate); }
        private void Invalidate() { _dirty = true; }

        private void LateUpdate()
        {
            if (Viewport == null || Scroll == null || _text == null) return;
            var size = Viewport.rect.size;
            if (size.x < 1f || size.y < 1f) return;
            bool changed = _lastText != _text.text;
            if (!_dirty && !changed && Mathf.Abs(_width - size.x) < .5f && Mathf.Abs(_height - size.y) < .5f) return;
            _dirty = false;
            _width = size.x; _height = size.y;
            float height = Mathf.Max(size.y, UiFactory.MeasureHeight(_text, _text.text, size.x - 58f) + 20f);
            var rect = _text.rectTransform;
            if (Mathf.Abs(rect.sizeDelta.y - height) > .5f) rect.sizeDelta = new Vector2(-58f, height);
            if (changed) { Scroll.StopMovement(); Scroll.verticalNormalizedPosition = 1f; }
            _lastText = _text.text;
        }
    }
}
