using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Moves only a temporary copy of the figure; the board, gauges and source transform stay fixed.</summary>
    public sealed class BasicAttackMotion : MonoBehaviour
    {
        private Graphic _source;
        private bool _sourceEnabled;
        private RectTransform _visual;
        private Vector2 _origin;
        private Quaternion _rotation;
        private Vector3 _scale;

        public bool IsActive { get { return _visual != null; } }

        public IEnumerator Approach(Graphic source, Graphic target, RectTransform layer, float speed)
        {
            Cancel();
            if (source == null || target == null || layer == null || !source.enabled || !source.gameObject.activeInHierarchy) yield break;

            var from = source.rectTransform;
            var to = target.rectTransform;
            var sourceFeet = FeetIn(layer, from);
            var targetFeet = FeetIn(layer, to);
            float direction = targetFeet.x >= sourceFeet.x ? 1f : -1f;
            float gap = Mathf.Min((WidthIn(layer, from) + WidthIn(layer, to)) * .5f + 12f,
                Mathf.Abs(targetFeet.x - sourceFeet.x) * .85f);
            _origin = layer.InverseTransformPoint(from.position);
            var destination = targetFeet - new Vector2(direction * gap, 0f) + (_origin - sourceFeet);

            var copy = Instantiate(source.gameObject, layer, false);
            copy.name = "AttackingFigure";
            foreach (var graphic in copy.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
            _visual = (RectTransform)copy.transform;
            _visual.anchorMin = _visual.anchorMax = layer.pivot;
            _visual.pivot = from.pivot;
            _visual.sizeDelta = from.rect.size;
            _visual.anchoredPosition = _origin;
            _rotation = Quaternion.Inverse(layer.rotation) * from.rotation;
            _visual.localRotation = _rotation;
            var parentScale = layer.lossyScale;
            var sourceScale = from.lossyScale;
            _scale = new Vector3(sourceScale.x / NonZero(parentScale.x), sourceScale.y / NonZero(parentScale.y), 1f);
            _visual.localScale = _scale;
            _source = source;
            _sourceEnabled = source.enabled;
            source.enabled = false;

            yield return MoveTo(destination, .17f, 18f, -6f * direction, 1f, speed);
            if (!IsActive) yield break;
            // A small wind-up, then a forward snap. The caller applies damage at this final pose.
            yield return MoveTo(destination - new Vector2(direction * 10f, 0f), .045f, 0f, 5f * direction, .98f, speed);
            if (!IsActive) yield break;
            yield return MoveTo(destination + new Vector2(direction * 16f, 0f), .06f, 0f, -12f * direction, 1.04f, speed);
        }

        public IEnumerator Return(float speed)
        {
            if (!IsActive) yield break;
            try { yield return MoveTo(_origin, .18f, 8f, 0f, 1f, speed); }
            finally { Cancel(); }
        }

        private IEnumerator MoveTo(Vector2 destination, float seconds, float arc, float tilt, float scale, float speed)
        {
            if (!IsActive) yield break;
            Vector2 start = _visual.anchoredPosition;
            Quaternion rotation = _visual.localRotation;
            Vector3 size = _visual.localScale;
            Quaternion finishRotation = _rotation * Quaternion.Euler(0f, 0f, tilt);
            Vector3 finishScale = _scale * scale;
            float duration = seconds / Mathf.Clamp(speed, 0.5f, 3f);
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                if (!IsActive) yield break;
                float t = elapsed / duration;
                float eased = Mathf.SmoothStep(0f, 1f, t);
                _visual.anchoredPosition = Vector2.Lerp(start, destination, eased) + Vector2.up * (Mathf.Sin(t * Mathf.PI) * arc);
                _visual.localRotation = Quaternion.Lerp(rotation, finishRotation, eased);
                _visual.localScale = Vector3.Lerp(size, finishScale, eased);
                yield return null;
            }
            if (!IsActive) yield break;
            _visual.anchoredPosition = destination;
            _visual.localRotation = finishRotation;
            _visual.localScale = finishScale;
        }

        public void Cancel()
        {
            if (_source != null) _source.enabled = _sourceEnabled;
            if (_visual != null)
            {
                var copy = _visual.gameObject;
                copy.SetActive(false);
                if (Application.isPlaying) Destroy(copy);
                else DestroyImmediate(copy);
            }
            _visual = null;
            _source = null;
        }

        private void OnDisable() { Cancel(); }
        private void OnDestroy() { Cancel(); }
        private static float NonZero(float value) { return Mathf.Abs(value) > .0001f ? value : 1f; }

        private static Vector2 FeetIn(RectTransform layer, RectTransform figure)
        {
            return layer.InverseTransformPoint(figure.TransformPoint(new Vector3(figure.rect.center.x, figure.rect.yMin, 0f)));
        }

        private static float WidthIn(RectTransform layer, RectTransform figure)
        {
            var left = layer.InverseTransformPoint(figure.TransformPoint(new Vector3(figure.rect.xMin, 0f, 0f)));
            var right = layer.InverseTransformPoint(figure.TransformPoint(new Vector3(figure.rect.xMax, 0f, 0f)));
            return Vector3.Distance(left, right);
        }
    }
}
