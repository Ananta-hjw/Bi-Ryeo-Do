#if UNITY_EDITOR
using System.Reflection;
using BiRyeoDo.Game.UI;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BiRyeoDo.Tests.UI
{
    public class TabRouterTests
    {
        private GameObject _host;
        private TabRouter _router;
        private readonly Vector2 _origin = new Vector2(8f, 12f);

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("Router", typeof(RectTransform));
            _router = _host.AddComponent<TabRouter>();
            _router.panels = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                var panel = new GameObject("Panel" + i, typeof(RectTransform));
                panel.transform.SetParent(_host.transform, false);
                ((RectTransform)panel.transform).anchoredPosition = _origin;
                _router.panels[i] = panel;
            }
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(_host); }

        private void Advance(float seconds)
        {
            typeof(TabRouter).GetMethod("AdvanceTransition", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_router, new object[] { seconds });
        }

        private Vector2 Position(int index) { return ((RectTransform)_router.panels[index].transform).anchoredPosition; }

        [Test]
        public void FirstRouteIsImmediateAndNextRouteSettlesWithinAShortTransition()
        {
            _router.Show(0);
            Assert.AreEqual(_origin, Position(0));
            _router.Show(1);
            var group = _router.panels[1].GetComponent<CanvasGroup>();
            Assert.IsFalse(_router.panels[0].activeSelf);
            Assert.IsTrue(_router.panels[1].activeSelf);
            Assert.Greater(Position(1).x, _origin.x);
            Assert.Less(group.alpha, 1f);
            Assert.IsTrue(group.interactable);
            Assert.IsTrue(group.blocksRaycasts);
            float initialAlpha = group.alpha;
            Advance(.07f);
            Assert.Greater(group.alpha, initialAlpha);
            Assert.Less(group.alpha, 1f);
            Advance(.08f);
            Assert.AreEqual(1f, group.alpha);
            Assert.AreEqual(_origin, Position(1));
        }

        [Test]
        public void RapidSelectionsRestorePreviousPanelsAndLatestSelectionWins()
        {
            _router.Show(0);
            _router.Show(1);
            Advance(.03f);
            _router.Show(2);
            Assert.AreEqual(_origin, Position(1));
            Assert.AreEqual(1f, _router.panels[1].GetComponent<CanvasGroup>().alpha);
            Assert.IsFalse(_router.panels[1].activeSelf);
            Assert.IsTrue(_router.panels[2].activeSelf);
            Advance(.15f);
            Assert.AreEqual(_origin, Position(2));
            _router.Show(1);
            Advance(.15f);
            Assert.AreEqual(_origin, Position(1));
        }

        [Test]
        public void RepeatedSelectionDoesNotRestartAndDisableRestoresLayout()
        {
            _router.Show(0);
            _router.Show(1);
            Advance(.07f);
            float alpha = _router.panels[1].GetComponent<CanvasGroup>().alpha;
            _router.Show(1);
            Assert.AreEqual(alpha, _router.panels[1].GetComponent<CanvasGroup>().alpha);
            _router.enabled = false;
            Assert.AreEqual(1f, _router.panels[1].GetComponent<CanvasGroup>().alpha);
            Assert.AreEqual(_origin, Position(1));
            _router.Show(2);
            Assert.AreEqual(_origin, Position(2));
        }

        [Test]
        public void InvalidSelectionKeepsTheVisibleRouteAndStartDoesNotOverrideSelection()
        {
            _router.Show(1);
            _router.Show(-1);
            _router.Show(3);
            typeof(TabRouter).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_router, null);
            Assert.IsTrue(_router.panels[1].activeSelf);
            Assert.IsFalse(_router.panels[0].activeSelf);
        }
    }
}
#endif
