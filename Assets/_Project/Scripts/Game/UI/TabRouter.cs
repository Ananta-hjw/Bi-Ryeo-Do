using UnityEngine;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Shows one route at a time. All seven routes are available from the bottom navigation.</summary>
    public class TabRouter : MonoBehaviour
    {
        // Append new routes so existing inspector values keep their original meaning.
        public enum Tab { World, MartialArts, Bag, Gacha, Record, Companions, Equipment }

        [Tooltip("Panels indexed by the Tab enum order.")]
        public GameObject[] panels;
        public Tab startTab = Tab.World;

        private const float TransitionDuration = .14f;
        private const float SlideDistance = 14f;
        private int _current = -1;
        private CanvasGroup _incoming;
        private RectTransform _moving;
        private Vector2 _restPosition;
        private float _elapsed;

        private void Start()
        {
            // MainScreen may already have selected a route during its own Start.
            if (_current < 0) Show((int)startTab);
        }

        public void Show(int index)
        {
            if (panels == null || index < 0 || index >= panels.Length || panels[index] == null) return;
            if (_current == index && panels[index].activeSelf) return;

            bool animate = _current >= 0 && isActiveAndEnabled;
            FinishTransition();
            _current = index;
            // Keep only the destination active so old popups and pending actions close immediately.
            for (int i = 0; i < panels.Length; i++)
                if (panels[i] != null) panels[i].SetActive(i == index);
            if (!animate) return;

            var panel = panels[index];
            _incoming = panel.GetComponent<CanvasGroup>();
            if (_incoming == null) _incoming = panel.AddComponent<CanvasGroup>();
            _moving = panel.transform as RectTransform;
            if (_moving != null) _restPosition = _moving.anchoredPosition;
            _elapsed = 0f;
            ApplyTransition(0f);
        }

        private void Update() { AdvanceTransition(Time.unscaledDeltaTime); }

        private void AdvanceTransition(float deltaTime)
        {
            if (_incoming == null) return;
            _elapsed += Mathf.Max(0f, deltaTime);
            float progress = Mathf.Clamp01(_elapsed / TransitionDuration);
            if (progress >= 1f) FinishTransition();
            else ApplyTransition(progress);
        }

        private void ApplyTransition(float progress)
        {
            // Cubic ease-out responds immediately and settles gently without delaying input.
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            _incoming.alpha = Mathf.Lerp(.45f, 1f, eased);
            if (_moving != null)
                _moving.anchoredPosition = _restPosition + Vector2.right * (SlideDistance * (1f - eased));
        }

        private void FinishTransition()
        {
            if (_incoming != null) _incoming.alpha = 1f;
            if (_moving != null) _moving.anchoredPosition = _restPosition;
            _incoming = null;
            _moving = null;
        }

        private void OnDisable() { FinishTransition(); }
    }
}
