using UnityEngine;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Stretches a backdrop past its safe-area parent to the whole screen, so artwork also fills the notch/cutout strips.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class FullBleed : MonoBehaviour
    {
        private readonly Vector3[] _corners = new Vector3[4];
        private Vector2 _lastMin = new Vector2(float.NaN, 0f), _lastMax;

        private void LateUpdate()
        {
            var rect = (RectTransform)transform;
            var parent = rect.parent as RectTransform;
            var canvas = GetComponentInParent<Canvas>();
            if (parent == null || canvas == null) return;
            ((RectTransform)canvas.rootCanvas.transform).GetWorldCorners(_corners);
            Vector2 min = (Vector2)parent.InverseTransformPoint(_corners[0]) - parent.rect.min;
            Vector2 max = (Vector2)parent.InverseTransformPoint(_corners[2]) - parent.rect.max;
            if (min == _lastMin && max == _lastMax) return;
            _lastMin = min; _lastMax = max;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = min; rect.offsetMax = max;
        }
    }
}
