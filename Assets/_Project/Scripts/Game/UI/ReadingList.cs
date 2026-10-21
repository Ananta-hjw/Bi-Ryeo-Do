using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Expands registered rows for wrapped text, then places them without overlap in a scroll list.</summary>
    public sealed class ReadingList : MonoBehaviour
    {
        private sealed class Row { public RectTransform Rect; public float Minimum; public bool Active; }
        private readonly List<Row> _rows = new List<Row>();
        private readonly HashSet<Text> _labels = new HashSet<Text>();
        private float _width = -1f, _gap = 14f;
        private bool _dirty = true;

        public static void Register(RectTransform row, float minimum, float gap = 14f)
        {
            var owner = row.parent.GetComponent<ReadingList>();
            if (owner == null) owner = row.parent.gameObject.AddComponent<ReadingList>();
            owner._rows.Add(new Row { Rect = row, Minimum = minimum });
            owner._gap = gap; owner._dirty = true;
        }

        private void OnEnable() { _dirty = true; }
        private void Invalidate() { _dirty = true; }
        private void OnDestroy()
        {
            foreach (var label in _labels) if (label != null) label.UnregisterDirtyVerticesCallback(Invalidate);
        }

        private void LateUpdate() { Reflow(); }

        /// <summary>Measures new rows immediately before a caller restores its saved scroll position.</summary>
        public void Reflow()
        {
            var content = (RectTransform)transform;
            if (content.rect.width < 1f) return;
            // Reused bag/result rows can become visible before their text callbacks were registered.
            foreach (var row in _rows)
            {
                bool active = row.Rect != null && row.Rect.gameObject.activeSelf;
                if (row.Active != active) { row.Active = active; _dirty = true; }
            }
            if (!_dirty && Mathf.Abs(_width - content.rect.width) < .5f) return;
            _dirty = false; _width = content.rect.width;
            float offset = _gap;
            foreach (var row in _rows)
            {
                if (row.Rect == null || !row.Rect.gameObject.activeSelf) continue;
                float oldHeight = row.Rect.rect.height;
                if (oldHeight < 1f) continue;
                float height = row.Minimum;
                foreach (var label in row.Rect.GetComponentsInChildren<Text>(false))
                {
                    // Large fallback glyphs are decoration, not paragraphs.
                    if (string.IsNullOrEmpty(label.text) || label.fontSize >= 64 || label.rectTransform.rect.width < 35f) continue;
                    if (_labels.Add(label)) label.RegisterDirtyVerticesCallback(Invalidate);
                    if (label.resizeTextForBestFit) label.resizeTextForBestFit = false;
                    if (label.fontStyle == FontStyle.Normal && label.lineSpacing < 1.15f) label.lineSpacing = 1.15f;
                    float share = label.rectTransform.rect.height / oldHeight;
                    if (share < .025f) continue;
                    float needed = UiFactory.MeasureHeight(label, label.text, label.rectTransform.rect.width);
                    height = Mathf.Max(height, (needed + 12f) / share);
                }
                var position = new Vector2(row.Rect.anchoredPosition.x, -offset);
                if ((row.Rect.anchoredPosition - position).sqrMagnitude > .25f) row.Rect.anchoredPosition = position;
                if (Mathf.Abs(row.Rect.sizeDelta.y - height) > .5f) row.Rect.sizeDelta = new Vector2(row.Rect.sizeDelta.x, height);
                offset += height + _gap;
            }
            if (Mathf.Abs(content.sizeDelta.y - offset) > .5f) content.sizeDelta = new Vector2(content.sizeDelta.x, offset);
        }
    }
}
