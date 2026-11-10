#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using BiRyeoDo.Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BiRyeoDo.Tests.UI
{
    public class AcademyFlowTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject _host, _managerHost;
        private GameManager _previous;
        private AcademyPanel _panel;
        private AcademyWalk _walk;
        private RectTransform _root, _prep;
        private WorldScreen _world;
        private StageData _started;
        private static readonly FieldInfo Instance = typeof(GameManager).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);

        [SetUp]
        public void SetUp()
        {
            _started = null;
            _previous = GameManager.Instance;
            _managerHost = new GameObject("AcademyTestManager");
            _managerHost.SetActive(false);
            var manager = _managerHost.AddComponent<GameManager>();
            var save = new SaveData { ClearedStage = 30, Level = 30 };
            Inventory.EnsureStartKit(save);
            save.AcademyCourses.AddRange(Academy.Courses.Take(3).Select(course => course.Id));
            save.TutorialDone.Add("academy");
            typeof(GameManager).GetField("<Save>k__BackingField", Private).SetValue(manager, save);
            Instance.SetValue(null, manager);
            _host = new GameObject("AcademyFlow", typeof(RectTransform), typeof(Canvas));
            _host.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rect = _host.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(1920f, 1080f);
            // Leave WorldScreen unbuilt: its unrelated lobby refresh and save paths are excluded.
            _world = _host.AddComponent<WorldScreen>();
            _root = UiFactory.Rect("Academy", rect, Vector2.zero, Vector2.one);
            _panel = new AcademyPanel(_root, () => { }, stage => Call(_world, "StartAcademy", stage));
            _walk = (AcademyWalk)typeof(AcademyPanel).GetField("_walk", Private).GetValue(_panel);
            _prep = UiFactory.Rect("PrepOverlay", rect, Vector2.zero, Vector2.one);
            _prep.gameObject.AddComponent<Image>(); _prep.gameObject.SetActive(false);
            SetWorld("_academy", _panel); SetWorld("_prepOverlay", _prep);
            SetWorld("_onStart", new Action<StageData>(stage => _started = stage));
            _panel.Open();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host); Object.DestroyImmediate(_managerHost);
            Instance.SetValue(null, _previous);
        }

        private static object Call(object target, string name, params object[] args)
        {
            return target.GetType().GetMethod(name, Private).Invoke(target, args);
        }
        private void SetWorld(string name, object value) { typeof(WorldScreen).GetField(name, Private).SetValue(_world, value); }
        private static void Click(Transform root, string name)
        {
            var button = root.GetComponentsInChildren<Button>().First(item => item.name == name);
            Assert.IsTrue(button.interactable, name); button.onClick.Invoke();
        }
        private void Enter(int facility)
        {
            for (int i = 0; i < 23; i++) _walk.Move(-1f, 0f, .1f);
            for (int i = 0; i < 23; i++) _walk.Move(0f, 1f, .1f);
            for (int i = 0; i < 23 * facility; i++) _walk.Move(1f, 0f, .1f);
            var campus = _root.GetComponentInChildren<AcademyCampus>();
            Call(campus, "RefreshView");
            Assert.AreEqual(facility, _walk.NearbyFacility());
            Click(campus.transform, "Interact");
        }

        [TestCase(0, "Course_")] [TestCase(1, "Club_")] [TestCase(2, "House_")]
        public void EachFacilityOpensItsMenuAndBackPreservesLocation(int facility, string cardPrefix)
        {
            Enter(facility);
            float x = _walk.X, y = _walk.Y;
            Assert.IsTrue(_root.GetComponentsInChildren<RectTransform>().Any(item => item.name.StartsWith(cardPrefix)));
            Click(_root, "Back");
            Assert.IsNotNull(_root.GetComponentInChildren<AcademyCampus>());
            Assert.AreEqual(x, _walk.X); Assert.AreEqual(y, _walk.Y);
            Assert.AreEqual(Vector2.zero, _root.GetComponentInChildren<AcademyJoystick>().Direction);
        }

        [TestCase(0, "Act")] [TestCase(2, "Go")]
        public void LessonAndLeaguePreparationCanCancelAndReturnAfterBattle(int facility, string action)
        {
            Enter(facility); float x = _walk.X, y = _walk.Y;
            Click(_root, action);
            Assert.IsTrue(_prep.gameObject.activeSelf);
            Assert.IsTrue(_panel.IsOpen);
            Click(_prep, "Cancel");
            Assert.IsFalse(_prep.gameObject.activeSelf);
            Assert.IsTrue(_panel.IsOpen); Assert.IsNull(_started);
            Click(_root, action);
            Click(_prep, "Go");
            Assert.IsNotNull(_started); Assert.IsFalse(_panel.IsOpen);
            // Only simulate the battle-return callback; no combat, rewards or disk writes.
            _world.AfterBattle();
            Assert.IsTrue(_panel.IsOpen);
            // The fight returns to the menu it was started from; its back arrow leads to the same spot in the courtyard.
            Assert.IsNull(_root.GetComponentInChildren<AcademyCampus>());
            Assert.IsTrue(_root.GetComponentsInChildren<Button>().Any(item => item.name == action));
            Click(_root, "Back");
            Assert.IsNotNull(_root.GetComponentInChildren<AcademyCampus>());
            Assert.AreEqual(x, _walk.X); Assert.AreEqual(y, _walk.Y);
        }

        [Test]
        public void GuideDialogueFreezesMovementAndOpensCoursesOnlyAfterConfirmation()
        {
            for (int i = 0; i < 23; i++) _walk.Move(-1f, 0f, .1f);
            for (int i = 0; i < 23; i++) _walk.Move(0f, 1f, .1f);
            for (int i = 0; i < 10; i++) _walk.Move(1f, 0f, .1f);
            var campus = _root.GetComponentInChildren<AcademyCampus>();
            Call(campus, "RefreshView");
            Assert.IsTrue(AcademyGuide.Nearby(_walk.X, _walk.Y));
            Click(campus.transform, "Interact");
            var dialog = campus.transform.Find("GuideDialog");
            Assert.IsTrue(dialog.gameObject.activeSelf);
            Assert.IsTrue(dialog.GetComponent<Image>().raycastTarget);
            float x = _walk.X, y = _walk.Y;
            Call(campus, "AdvanceMovement", Vector2.right, .1f);
            Assert.AreEqual(x, _walk.X); Assert.AreEqual(y, _walk.Y);
            var router = _host.AddComponent<TabRouter>(); router.panels = new[] { _host };
            var main = _host.AddComponent<MainScreen>(); main.enabled = false;
            typeof(MainScreen).GetField("_router", Private).SetValue(main, router);
            Assert.IsTrue((bool)Call(main, "PressTopBack"));
            Assert.IsTrue(_panel.IsOpen, "Android back must dismiss the dialogue rather than leave the academy.");
            Assert.IsFalse(dialog.gameObject.activeSelf);
            Click(campus.transform, "Interact");
            Click(dialog, "Leave");
            Assert.IsFalse(dialog.gameObject.activeSelf);
            Assert.AreEqual(Vector2.zero, campus.GetComponentInChildren<AcademyJoystick>().Direction);
            Click(campus.transform, "Interact");
            Click(dialog, "OpenFacility");
            Assert.IsTrue(_root.GetComponentsInChildren<RectTransform>().Any(item => item.name.StartsWith("Course_")));
            Click(_root, "Back");
            Assert.IsNotNull(_root.GetComponentInChildren<AcademyCampus>());
            Assert.AreEqual(x, _walk.X); Assert.AreEqual(y, _walk.Y);
        }

        [TestCase(0, "Course_", 10)]
        [TestCase(1, "Club_", 33)]
        [TestCase(2, "House_", 55)]
        public void EachResidentOpensOnlyItsOwnFacilityAndKeepsTheLocation(int facility, string prefix, int eastSteps)
        {
            for (int i = 0; i < 23; i++) _walk.Move(-1f, 0f, .1f);
            for (int i = 0; i < 23; i++) _walk.Move(0f, 1f, .1f);
            for (int i = 0; i < eastSteps; i++) _walk.Move(1f, 0f, .1f);
            var campus = _root.GetComponentInChildren<AcademyCampus>(); Call(campus, "RefreshView");
            Assert.AreEqual(facility, AcademyResidents.Nearby(_walk.X, _walk.Y).Facility);
            float x = _walk.X, y = _walk.Y;
            Click(campus.transform, "Interact");
            var dialog = campus.transform.Find("GuideDialog");
            Assert.IsTrue(dialog.gameObject.activeSelf);
            Click(dialog, "OpenFacility");
            Assert.IsTrue(_root.GetComponentsInChildren<RectTransform>().Any(item => item.name.StartsWith(prefix)));
            Click(_root, "Back");
            Assert.AreEqual(x, _walk.X); Assert.AreEqual(y, _walk.Y);
            Assert.AreEqual(Vector2.zero, _root.GetComponentInChildren<AcademyJoystick>().Direction);
        }

        [Test]
        public void AndroidBackCancelsPreparationBeforeTheUnderlyingAcademyMenu()
        {
            Enter(0); Click(_root, "Act");
            var router = _host.AddComponent<TabRouter>(); router.panels = new[] { _host };
            var main = _host.AddComponent<MainScreen>(); main.enabled = false;
            typeof(MainScreen).GetField("_router", Private).SetValue(main, router);
            Assert.IsTrue((bool)Call(main, "PressTopBack"));
            Assert.IsFalse(_prep.gameObject.activeSelf);
            Assert.IsTrue(_panel.IsOpen);
            Assert.IsNull(_root.GetComponentInChildren<AcademyCampus>(), "The facility menu must remain below the cancelled prep.");
            Assert.IsNull(_started);
        }
    }
}
#endif
