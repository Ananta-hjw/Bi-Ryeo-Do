using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// A drag bar that picks a whole number between <see cref="Min"/> and <see cref="Max"/>: press or drag along the track, or use the − / + buttons.
    /// Built by <see cref="Create"/>; <see cref="OnChanged"/> fires with the new value.
    /// </summary>
    public sealed class CountBar : MonoBehaviour, IPointerDownHandler, IDragHandler, IInitializePotentialDragHandler
    {
        public int Min = 1, Max = 1;
        public Action<int> OnChanged;
        private int _value = 1;
        private RectTransform _track, _fill, _knob;

        public int Value { get { return _value; } }

        public static CountBar Create(Transform parent, Vector2 min, Vector2 max, int lo, int hi, int value, Action<int> onChanged)
        {
            var root = UiFactory.Rect("CountBar", parent, min, max);
            UiFactory.Button("Minus", root, new Vector2(0f, .1f), new Vector2(.16f, .9f), "−", UiFactory.InkSoft, null, 34);
            UiFactory.Button("Plus", root, new Vector2(.84f, .1f), new Vector2(1f, .9f), "+", UiFactory.InkSoft, null, 34);
            var track = UiFactory.Panel("Track", root, new Vector2(.2f, .38f), new Vector2(.8f, .62f), new Color(0f, 0f, 0f, .55f));
            UiFactory.Frame(track.transform, UiFactory.GoldDim, 1.5f);
            var fill = UiFactory.Panel("Fill", track.transform, Vector2.zero, new Vector2(0f, 1f), UiFactory.Gold);
            fill.raycastTarget = false;
            var knob = UiFactory.Panel("Knob", track.transform, new Vector2(0f, -.6f), new Vector2(0f, 1.6f), UiFactory.PaperText);
            knob.rectTransform.sizeDelta = new Vector2(22f, 0f);
            knob.raycastTarget = false;
            // A wider invisible hit area over the track, so the bar is easy to grab with a finger.
            var hit = UiFactory.Rect("Hit", root, new Vector2(.18f, 0f), new Vector2(.82f, 1f));
            hit.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var bar = hit.gameObject.AddComponent<CountBar>();
            bar._track = track.rectTransform;
            bar._fill = fill.rectTransform;
            bar._knob = knob.rectTransform;
            bar.Min = Math.Min(lo, hi);
            bar.Max = Math.Max(lo, hi);
            bar.OnChanged = onChanged;
            root.Find("Minus").GetComponent<Button>().onClick.AddListener(() => bar.Set(bar._value - 1, true));
            root.Find("Plus").GetComponent<Button>().onClick.AddListener(() => bar.Set(bar._value + 1, true));
            bar.Set(value, false);
            return bar;
        }

        /// <summary>Moves the bar to <paramref name="value"/> (clamped); fires <see cref="OnChanged"/> when asked and the value changed.</summary>
        public void Set(int value, bool notify)
        {
            int v = Math.Max(Min, Math.Min(Max, value));
            bool changed = v != _value;
            _value = v;
            float t = Max > Min ? (float)(v - Min) / (Max - Min) : 1f;
            _fill.anchorMax = new Vector2(t, 1f);
            _knob.anchorMin = new Vector2(t, -.6f);
            _knob.anchorMax = new Vector2(t, 1.6f);
            if (notify && changed && OnChanged != null) OnChanged(v);
        }

        public void OnInitializePotentialDrag(PointerEventData eventData) { }   // keeps an enclosing ScrollRect from taking the drag
        public void OnPointerDown(PointerEventData eventData) { Pick(eventData); }
        public void OnDrag(PointerEventData eventData) { Pick(eventData); }

        private void Pick(PointerEventData eventData)
        {
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_track, eventData.position, eventData.pressEventCamera, out local)) return;
            var r = _track.rect;
            float t = r.width > 0f ? Mathf.Clamp01((local.x - r.xMin) / r.width) : 0f;
            Set(Min + Mathf.RoundToInt(t * (Max - Min)), true);
        }
    }
}
