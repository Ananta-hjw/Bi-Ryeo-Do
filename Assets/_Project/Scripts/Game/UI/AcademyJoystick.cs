using BiRyeoDo.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BiRyeoDo.Game.UI
{
    /// <summary>A single touch owns the stick until release; disabled controls never retain movement.</summary>
    public sealed class AcademyJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Vector2 Direction { get; private set; }
        public RectTransform Knob;
        private int? _pointer;
        public void OnPointerDown(PointerEventData e)
        {
            if (_pointer.HasValue) return;
            _pointer = e.pointerId;
            OnDrag(e);
        }
        public void OnDrag(PointerEventData e)
        {
            if (_pointer != e.pointerId) return;
            var rect = (RectTransform)transform;
            Vector2 point;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out point)) return;
            float radius = Mathf.Max(1f, Mathf.Min(rect.rect.width, rect.rect.height) * .35f);
            var raw = (point - rect.rect.center) / radius;
            float horizontal, vertical;
            ExplorationInput.Stick(raw.x, raw.y, out horizontal, out vertical);
            Direction = new Vector2(horizontal, vertical);
            if (Knob != null) Knob.anchoredPosition = Direction * radius;
        }
        public void OnPointerUp(PointerEventData e) { if (_pointer == e.pointerId) ResetInput(); }
        private void OnDisable() { ResetInput(); }
        private void OnApplicationPause(bool paused) { if (paused) ResetInput(); }
        private void OnApplicationFocus(bool focused) { if (!focused) ResetInput(); }
        public void ResetInput()
        {
            _pointer = null; Direction = Vector2.zero;
            if (Knob != null) Knob.anchoredPosition = Vector2.zero;
        }
    }
}
