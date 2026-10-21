#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using BiRyeoDo.Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Tests.UI
{
    public class LandscapeWorkspaceTests
    {
        private GameObject _managerObject, _canvas;
        private GameManager _previous;
        private CompanionScreen _screen;
        private SaveData _save;
        private static readonly FieldInfo InstanceField = typeof(GameManager).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);

        [SetUp]
        public void SetUp()
        {
            // An inactive manager avoids loading or writing the player's actual save during UI tests.
            _previous = GameManager.Instance;
            _managerObject = new GameObject("TestManager");
            _managerObject.SetActive(false);
            var manager = _managerObject.AddComponent<GameManager>();
            _save = new SaveData { ClearedStage = 29, Doo = 100000 };
            Inventory.EnsureStartKit(_save);
            typeof(GameManager).GetField("<Save>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, _save);
            InstanceField.SetValue(null, manager);
            _canvas = new GameObject("Workspace", typeof(RectTransform), typeof(Canvas));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            _canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 984f);
            _screen = _canvas.AddComponent<CompanionScreen>();
            _screen.Build(_canvas.GetComponent<RectTransform>(), () => { }, () => { });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvas);
            Object.DestroyImmediate(_managerObject);
            InstanceField.SetValue(null, _previous);
        }

        private void Click(string path)
        {
            var button = _canvas.transform.Find(path).GetComponent<Button>();
            Assert.IsTrue(button.interactable, path);
            button.onClick.Invoke();
            Canvas.ForceUpdateCanvases();
        }

        [TestCase(1920f)]
        [TestCase(1440f)]
        [TestCase(2400f)]
        public void PersonSwitchRetainsEquipmentSlotAndDoesNotChangeAnyLoadout(float width)
        {
            _canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 984f);
            var companion = EarlyGameContent.Companions.First();
            var original = _save.EquippedGear.ToArray();
            Click("Pages/Page1");
            Click("Page/Slot5");
            Click("People/Viewport/Content/Person_" + companion.Id);
            Assert.AreEqual("현재 착용 · 장신구 2", _canvas.transform.Find("Page/Equipped/Heading").GetComponent<Text>().text);
            Assert.AreEqual("비어 있음", _canvas.transform.Find("Page/Equipped/NameViewport/Name").GetComponent<Text>().text);
            _canvas.SetActive(false); _canvas.SetActive(true);
            Assert.IsNotNull(_canvas.transform.Find("Page/Slot5"));
            Assert.AreEqual("현재 착용 · 장신구 2", _canvas.transform.Find("Page/Equipped/Heading").GetComponent<Text>().text);
            CollectionAssert.AreEqual(original, _save.EquippedGear);
        }

        [Test]
        public void CompanionCannotPreviewHeroOnlyEquipment()
        {
            Inventory.Add(_save, Ritual.RewardId, 1);
            Click("Pages/Page1");
            _screen.Refresh();
            Assert.IsTrue(_canvas.transform.Find("Page/List/Viewport/Content").GetComponentsInChildren<Text>().Any(t => t.text.Contains(EarlyGameContent.FindItem(Ritual.RewardId).Name)));
            Click("People/Viewport/Content/Person_" + EarlyGameContent.Companions.First().Id);
            Assert.IsFalse(_canvas.transform.Find("Page/List/Viewport/Content").GetComponentsInChildren<Text>().Any(t => t.text.Contains(EarlyGameContent.FindItem(Ritual.RewardId).Name)));
        }

        [Test]
        public void TrainingDistinguishesHeroGrowthBondCapAndInsufficientFunds()
        {
            Click("Pages/Page2");
            Assert.IsNotNull(_canvas.transform.Find("Page/HeroGrowthViewport"));
            Assert.IsNull(_canvas.transform.Find("Page/Train"));
            var companion = EarlyGameContent.Companions.First();
            Click("People/Viewport/Content/Person_" + companion.Id);
            Assert.IsTrue(_canvas.transform.Find("Page/Train").GetComponent<Button>().interactable);
            _save.Doo = 0; _screen.Refresh();
            Assert.IsFalse(_canvas.transform.Find("Page/Train").GetComponent<Button>().interactable);
            _save.Doo = 100000;
            _save.CompanionTraining.Add(new TrainingEntry { Id = companion.Id, Level = CompanionTraining.Cap(_save, companion.Id) });
            _screen.Refresh();
            Assert.IsFalse(_canvas.transform.Find("Page/Train").GetComponent<Button>().interactable);
            StringAssert.Contains("인연", _canvas.transform.Find("Page/Train").GetComponentInChildren<Text>().text);
        }

        [Test]
        public void UnjoinedPersonDisappearsAndSelectionFallsBackSafely()
        {
            var companion = EarlyGameContent.Companions.OrderByDescending(c => c.JoinStage).First();
            Click("People/Viewport/Content/Person_" + companion.Id);
            _save.ClearedStage = 0;
            _screen.Refresh();
            Assert.IsNull(_canvas.transform.Find("People/Viewport/Content/Person_" + companion.Id));
            Assert.AreEqual("비류연", _canvas.transform.Find("Identity/Name").GetComponent<Text>().text);
        }

        [TestCase(1920, 1080, 80f)]
        [TestCase(2400, 1080, 120f)]
        public void SafeAreaKeepsTheEntireNotchOutsideItsUsableBounds(int width, int height, float notch)
        {
            var safe = SafeAreaLayout.Normalize(new Rect(notch, 0f, width - notch * 2f, height), width, height);
            Assert.That(safe.xMin, Is.EqualTo(notch / width).Within(.0001f));
            Assert.That(safe.xMax, Is.EqualTo(1f - notch / width).Within(.0001f));
            Assert.AreEqual(0f, safe.yMin);
            Assert.AreEqual(1f, safe.yMax);
        }
    }
}
#endif
