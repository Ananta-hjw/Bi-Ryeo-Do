#if UNITY_EDITOR
using BiRyeoDo.Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace BiRyeoDo.Tests.UI
{
    public class AcademyJoystickTests
    {
        private GameObject _root, _events;
        private AcademyJoystick _stick;
        private EventSystem _system;

        [SetUp]
        public void SetUp()
        {
            _events = new GameObject("Events", typeof(EventSystem));
            _system = _events.GetComponent<EventSystem>();
            _root = new GameObject("Stick", typeof(RectTransform));
            ((RectTransform)_root.transform).sizeDelta = new Vector2(210f, 210f);
            _stick = _root.AddComponent<AcademyJoystick>();
            var knob = new GameObject("Knob", typeof(RectTransform));
            knob.transform.SetParent(_root.transform, false);
            _stick.Knob = (RectTransform)knob.transform;
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(_root); Object.DestroyImmediate(_events); }

        private PointerEventData Touch(int id, Vector2 position)
        {
            return new PointerEventData(_system) { pointerId = id, position = position };
        }

        [Test]
        public void LowerLeftPivotStillHasANeutralCenter()
        {
            var rect = (RectTransform)_root.transform;
            rect.pivot = Vector2.zero;
            var center = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            _stick.OnPointerDown(Touch(1, center));
            Assert.AreEqual(0f, _stick.Direction.magnitude, .001f);
        }

        [Test]
        public void SmallTouchNoiseStaysNeutralAndLosingFocusReleasesTheStick()
        {
            var rect = (RectTransform)_root.transform;
            var center = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            _stick.OnPointerDown(Touch(1, center + new Vector2(4f, 3f)));
            Assert.AreEqual(Vector2.zero, _stick.Direction);
            _stick.OnDrag(Touch(1, center + new Vector2(1000f, 0f)));
            Assert.Greater(_stick.Direction.x, 0f);
            _stick.SendMessage("OnApplicationFocus", false);
            Assert.AreEqual(Vector2.zero, _stick.Direction);
            Assert.AreEqual(Vector2.zero, _stick.Knob.anchoredPosition);
        }

        [Test]
        public void DragIsClampedAndReleaseStopsMovement()
        {
            var touch = Touch(1, new Vector2(1000f, 0f));
            _stick.OnPointerDown(touch);
            Assert.AreEqual(1f, _stick.Direction.magnitude, .001f);
            _stick.OnPointerUp(touch);
            Assert.AreEqual(Vector2.zero, _stick.Direction);
            Assert.AreEqual(Vector2.zero, _stick.Knob.anchoredPosition);
        }

        [Test]
        public void SecondTouchCannotStealOrReleaseTheStick()
        {
            var owner = Touch(1, new Vector2(1000f, 0f));
            var other = Touch(2, new Vector2(-1000f, 0f));
            _stick.OnPointerDown(owner);
            var direction = _stick.Direction;
            _stick.OnPointerDown(other); _stick.OnDrag(other); _stick.OnPointerUp(other);
            Assert.AreEqual(direction, _stick.Direction);
        }

        [Test]
        public void ClosingAndReopeningDoesNotRetainMovement()
        {
            _stick.OnPointerDown(Touch(1, new Vector2(1000f, 0f)));
            _root.SetActive(false); _root.SetActive(true);
            Assert.AreEqual(Vector2.zero, _stick.Direction);
            _stick.OnPointerDown(Touch(2, new Vector2(-1000f, 0f)));
            Assert.Less(_stick.Direction.x, 0f);
        }
    }
}
#endif
