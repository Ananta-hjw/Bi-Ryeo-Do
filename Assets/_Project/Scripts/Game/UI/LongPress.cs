using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Fires <see cref="OnLong"/> when a pointer stays down on this object for <see cref="Seconds"/> (real time, so it also works at game speed x3).</summary>
    public sealed class LongPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public const float Seconds = 0.45f;
        private static float _lastFired = -10f;

        /// <summary>True for a moment after a long press fired, so the click that follows the release can be ignored.</summary>
        public static bool JustFired { get { return Time.unscaledTime - _lastFired < 0.35f; } }

        public Action OnLong;
        private bool _down;
        private float _since;

        public void OnPointerDown(PointerEventData eventData) { _down = true; _since = Time.unscaledTime; }
        public void OnPointerUp(PointerEventData eventData) { _down = false; }
        public void OnPointerExit(PointerEventData eventData) { _down = false; }

        private void OnDisable() { _down = false; }

        private void Update()
        {
            if (!_down || Time.unscaledTime - _since < Seconds) return;
            _down = false;
            _lastFired = Time.unscaledTime;
            if (OnLong != null) OnLong();
        }
    }
}
