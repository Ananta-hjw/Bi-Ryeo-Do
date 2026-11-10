#if UNITY_EDITOR
using System.Reflection;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BiRyeoDo.Tests.UI
{
    public class AcademyCampusTests
    {
        [TestCase(1920f, 1080f)]
        [TestCase(2400f, 1080f)]
        [TestCase(1440f, 1080f)]
        public void FigureAndNameStayVisibleAtMapEdges(float width, float height)
        {
            var host = new GameObject("CampusTest", typeof(RectTransform), typeof(Canvas));
            try
            {
                host.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                var root = host.GetComponent<RectTransform>(); root.sizeDelta = new Vector2(width, height);
                var walk = new AcademyWalk(); var campus = host.AddComponent<AcademyCampus>();
                campus.Build(root, walk, page => { });
                var viewport = (RectTransform)root.Find("CampusViewport");
                var figure = (RectTransform)viewport.Find("CampusWorld/Player");
                var name = (RectTransform)figure.Find("PlayerName");
                var refresh = typeof(AcademyCampus).GetMethod("RefreshView", BindingFlags.Instance | BindingFlags.NonPublic);
                var directions = new[] { Vector2.left, Vector2.up, Vector2.down, Vector2.right, Vector2.up };
                foreach (var direction in directions)
                {
                    // Reach both upper corners through the clear side passages.
                    for (int i = 0; i < 200; i++) walk.Move(direction.x, direction.y, .1f);
                    Canvas.ForceUpdateCanvases(); refresh.Invoke(campus, null); Canvas.ForceUpdateCanvases();
                    AssertInside(viewport, name);
                    AssertInside(viewport, (RectTransform)figure.Find("SD"));
                }
                AssertInside(root, (RectTransform)root.Find("MoveStick"));
                AssertInside(root, (RectTransform)root.Find("Interact"));
            }
            finally { Object.DestroyImmediate(host); }
        }

        [Test]
        public void VisibleJoystickReceivesRaycastPointerInputAndRelease()
        {
            var previous = EventSystem.current;
            var events = new GameObject("CampusInputEvents", typeof(EventSystem));
            var host = new GameObject("CampusInputTest", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            try
            {
                var system = events.GetComponent<EventSystem>();
                EventSystem.current = system;
                host.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var root = host.GetComponent<RectTransform>();
                host.AddComponent<AcademyCampus>().Build(root, new AcademyWalk(), page => { });
                Canvas.ForceUpdateCanvases();
                var stick = root.Find("MoveStick").GetComponent<AcademyJoystick>();
                var surface = stick.GetComponent<Image>();
                Assert.IsTrue(surface.raycastTarget, "The visible control must opt into UiFactory.Panel's disabled raycast default.");
                Assert.IsFalse(stick.Knob.GetComponent<Image>().raycastTarget, "The knob must pass touches to the surface.");
                var rect = (RectTransform)stick.transform;
                var center = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                var pointer = new PointerEventData(system) { pointerId = 31, position = center };
                var hits = new List<RaycastResult>();
                host.GetComponent<GraphicRaycaster>().Raycast(pointer, hits);
                Assert.IsNotEmpty(hits, "The raycaster must see the joystick at its screen position.");
                Assert.AreEqual(stick.gameObject, hits[0].gameObject);
                pointer.pointerPressRaycast = pointer.pointerCurrentRaycast = hits[0];
                var owner = ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerDownHandler);
                Assert.AreEqual(stick.gameObject, owner);
                pointer.position = center + Vector2.right * 80f;
                ExecuteEvents.Execute(owner, pointer, ExecuteEvents.dragHandler);
                Assert.Greater(stick.Direction.x, .9f);
                Assert.AreEqual(0f, stick.Direction.y, .001f);
                ExecuteEvents.Execute(owner, pointer, ExecuteEvents.pointerUpHandler);
                Assert.AreEqual(Vector2.zero, stick.Direction);
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(events);
                EventSystem.current = previous;
            }
        }

        private static void AssertInside(RectTransform viewport, RectTransform child)
        {
            var corners = new Vector3[4]; child.GetWorldCorners(corners);
            foreach (var point in corners)
            {
                var local = viewport.InverseTransformPoint(point);
                Assert.GreaterOrEqual(local.x, viewport.rect.xMin - .5f, child.name);
                Assert.LessOrEqual(local.x, viewport.rect.xMax + .5f, child.name);
                Assert.GreaterOrEqual(local.y, viewport.rect.yMin - .5f, child.name);
                Assert.LessOrEqual(local.y, viewport.rect.yMax + .5f, child.name);
            }
        }
    }
}
#endif
