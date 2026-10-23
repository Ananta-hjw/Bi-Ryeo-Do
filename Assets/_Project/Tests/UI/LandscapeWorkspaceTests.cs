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
            _canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1920f, 960f);
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

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void StatusShowsRealmPowerRemainingAndProgressForTheSelectedPerson(float width)
        {
            _canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 960f);
            _save.Level = 40;
            var companion = EarlyGameContent.Companions.First();
            foreach (var owner in new[] { Inventory.Hero, companion.Id })
            {
                Click("People/Viewport/Content/Person_" + owner);
                double power = owner == Inventory.Hero
                    ? Realm.Power(InnerArts.HeroStatsBeforeRealm(_save))
                    : CompanionGrowth.Create(_save, companion).RealmPowerBeforeBonus;
                var realm = (RectTransform)_canvas.transform.Find("Page/Realm");
                Assert.IsTrue(realm.gameObject.activeInHierarchy);
                Assert.AreEqual("무공 경지 · " + Realm.Label(Realm.StepOf(power)), realm.Find("Name").GetComponent<Text>().text);
                Assert.AreEqual("종합 수치 " + System.Math.Round(power).ToString("N0"), realm.Find("Power").GetComponent<Text>().text);
                Assert.AreEqual(Realm.RemainingText(power), realm.Find("Remaining").GetComponent<Text>().text);
                var fill = (RectTransform)realm.Find("Progress/Fill");
                Assert.That(fill.anchorMax.x, Is.EqualTo((float)Realm.ProgressToNext(power)).Within(.0001f));
                var stat = (RectTransform)_canvas.transform.Find("Page/Stat0");
                Assert.Less(stat.anchorMax.y, realm.anchorMin.y, "Realm summary must not overlap the stat cards");
                var last = (RectTransform)_canvas.transform.Find(owner == Inventory.Hero ? "Page/Stat6" : "Page/Stat4");
                Assert.Greater(last.anchorMin.y, 0f, "All stats must remain inside the page");
                if (owner != Inventory.Hero)
                {
                    var critical = (RectTransform)_canvas.transform.Find("Page/CriticalViewport");
                    Assert.Less(critical.anchorMax.y, last.anchorMin.y);
                }
            }
            Click("Pages/Page1");
            Assert.IsNull(_canvas.transform.Find("Page/Realm"));
            Click("Pages/Page0");
            Assert.IsNotNull(_canvas.transform.Find("Page/Realm/Progress/Fill"));
        }

        [Test]
        public void EquipmentShortcutOpensTheDedicatedRouteForTheSelectedCompanion()
        {
            string selected = null;
            typeof(CompanionScreen).GetField("_openEquipment", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(_screen, (System.Action<string>)(owner => selected = owner));
            var companion = EarlyGameContent.Companions.First();
            Click("People/Viewport/Content/Person_" + companion.Id);
            Click("Identity/Equipment");
            Assert.AreEqual(companion.Id, selected);
            Assert.IsNull(_canvas.transform.Find("EquipmentManager"));
        }

        [TestCase(1440f)]
        [TestCase(2400f)]
        public void ArtSummaryContainsOnlySkillsUsedInBattleForTheSelectedPerson(float width)
        {
            _canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 960f);
            var unused = EarlyGameContent.Arts.First(art => art.OwnerId == null && !_save.EquippedArts.Contains(art.Id));
            _save.LearnedArts.Add(unused.Id);
            Click("Pages/Page1");
            foreach (var owner in new[] { Inventory.Hero, EarlyGameContent.Companions.First().Id })
            {
                Click("People/Viewport/Content/Person_" + owner);
                var unit = owner == Inventory.Hero ? Formation.BuildHero(_save) : CompanionGrowth.Create(_save, EarlyGameContent.FindCompanion(owner));
                var expected = EarlyGameContent.Arts.Where(art => (owner == Inventory.Hero ? art.OwnerId == null : art.OwnerId == owner)
                    && unit.Skills.Any(skill => skill.Id == art.Id)).ToList();
                var content = _canvas.transform.Find("Page/List/Viewport/Content");
                var names = Enumerable.Range(0, content.childCount).Select(i => content.GetChild(i).Find("Name").GetComponent<Text>().text).ToArray();
                CollectionAssert.AreEquivalent(expected.Select(art => art.Name).ToArray(), names);
                Assert.IsFalse(names.Contains(unused.Name));
                for (int i = 0; i < expected.Count; i++)
                {
                    var row = content.GetChild(i);
                    Assert.IsNotNull(row.Find("Resources")); Assert.IsNotNull(row.Find("Effect"));
                    Assert.IsNull(row.Find("Action")); Assert.IsNull(row.Find("Variant"));
                }
            }
        }

        [Test]
        public void CharacterTabsOmitTrainingAndCompanionTrainingLivesUnderBonds()
        {
            var tabs = _canvas.transform.Find("Pages").GetComponentsInChildren<Button>();
            CollectionAssert.AreEqual(new[] { "상태", "무공", "인연" }, tabs.Select(t => t.GetComponentInChildren<Text>().text).ToArray());
            Click("Pages/Page1");
            Assert.IsNull(_canvas.transform.Find("Page/HeroGrowthViewport"));
            Assert.IsNotNull(_canvas.transform.Find("Page/ArtsMenu"));
            var companion = EarlyGameContent.Companions.First();
            Click("People/Viewport/Content/Person_" + companion.Id);
            Click("Pages/Page2");
            const string trainingPath = "Page/List/Viewport/Content/Row0/Train";
            Assert.IsTrue(_canvas.transform.Find(trainingPath).GetComponent<Button>().interactable);
            _save.Doo = 0; _screen.Refresh();
            Assert.IsFalse(_canvas.transform.Find(trainingPath).GetComponent<Button>().interactable);
            _save.Doo = 100000;
            _save.CompanionTraining.Add(new TrainingEntry { Id = companion.Id, Level = CompanionTraining.Cap(_save, companion.Id) });
            _screen.Refresh();
            Assert.IsFalse(_canvas.transform.Find(trainingPath).GetComponent<Button>().interactable);
            StringAssert.Contains("인연", _canvas.transform.Find(trainingPath).GetComponentInChildren<Text>().text);
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
