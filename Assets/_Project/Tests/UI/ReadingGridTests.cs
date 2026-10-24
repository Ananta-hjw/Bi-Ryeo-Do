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
    public class ReadingGridTests
    {
        private GameObject _canvas, _manager;
        private GameManager _previous;
        private SaveData _save;
        private static readonly FieldInfo Instance = typeof(GameManager).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        private RectTransform Root { get { return _canvas.GetComponent<RectTransform>(); } }

        [SetUp]
        public void SetUp()
        {
            _previous = GameManager.Instance;
            _manager = new GameObject("GridTestManager");
            _manager.SetActive(false);
            var manager = _manager.AddComponent<GameManager>();
            _save = new SaveData { Level = 35, ClearedStage = 35, Doo = 123456 };
            Inventory.EnsureStartKit(_save);
            typeof(GameManager).GetField("<Save>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, _save);
            Instance.SetValue(null, manager);
            _canvas = new GameObject("GridTest", typeof(RectTransform), typeof(Canvas));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            Root.sizeDelta = new Vector2(1920f, 960f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvas);
            Object.DestroyImmediate(_manager);
            Instance.SetValue(null, _previous);
        }

        private RectTransform Overlay(string name) { return UiFactory.Rect(name, Root, Vector2.zero, Vector2.one); }
        private static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [TestCase(1440f, 2)]
        [TestCase(1440f, 3)]
        [TestCase(1920f, 2)]
        [TestCase(1920f, 3)]
        [TestCase(2400f, 2)]
        [TestCase(2400f, 3)]
        public void LongCardsGrowTheirWholeRowAndLeaveTheNextRowAndButtonsSeparate(float width, int columns)
        {
            Root.sizeDelta = new Vector2(width, 960f);
            var content = UiFactory.Rect("Content", Root, new Vector2(0f, 1f), Vector2.one);
            content.pivot = new Vector2(.5f, 1f);
            var grid = new ReadingGrid(content, columns, 280f);
            for (int i = 0; i < columns + 1; i++)
            {
                var card = grid.Add("Card" + i);
                UiFactory.Label("Body", card, new Vector2(.04f, .35f), new Vector2(.96f, .95f), i == 0 ? new string('가', 150) : "짧은 안내", 30, UiFactory.PaperText);
                UiFactory.Button("Action", card, new Vector2(.04f, .04f), new Vector2(.96f, .27f), "받기", UiFactory.Red, null, 28);
            }
            Canvas.ForceUpdateCanvases();
            var reading = content.GetComponent<ReadingList>();
            reading.Reflow(true);
            Canvas.ForceUpdateCanvases();
            var first = (RectTransform)content.Find("Row0");
            var next = (RectTransform)content.Find("Row1");
            Assert.AreEqual(columns, first.childCount);
            Assert.GreaterOrEqual(Bounds(first).yMin - Bounds(next).yMax, 15f);
            for (int i = 0; i < columns; i++)
            {
                var card = (RectTransform)first.GetChild(i);
                Assert.AreEqual(Bounds(first).height, Bounds(card).height, .1f);
                Assert.GreaterOrEqual(Bounds((RectTransform)card.Find("Action")).height, 58f);
                if (i > 0) Assert.GreaterOrEqual(Bounds(card).xMin - Bounds((RectTransform)first.GetChild(i - 1)).xMax, 15f);
            }
            float height = first.rect.height;
            for (int i = 0; i < 3; i++) { reading.Reflow(true); Canvas.ForceUpdateCanvases(); }
            Assert.AreEqual(height, first.rect.height, .1f, "Reflow must converge instead of pushing the list down every frame");
        }

        [TestCase("Daily", 3)]
        [TestCase("Stars", 3)]
        [TestCase("Memories", 3)]
        [TestCase("Stories", 2)]
        [TestCase("Factions", 2)]
        [TestCase("Shop", 5)]
        [TestCase("Ritual", 2)]
        [TestCase("Codex", 3)]
        [TestCase("Achievements", 2)]
        public void ContentPanelsUseMeasuredCardsAndOpeningDoesNotClaimRewards(string page, int columns)
        {
            var root = Overlay("Panel");
            switch (page)
            {
                case "Daily": new DailyPanel(root, () => { }).Open(); break;
                case "Stars": new StarPanel(root, () => { }).Open(); break;
                case "Memories": new MemoryPanel(root, () => { }, null).Open(); break;
                case "Stories": new StoryLogPanel(root, () => { }, null).Open(); break;
                case "Factions": new FactionPanel(root, () => { }).Open(); break;
                case "Shop": new FactionPanel(root, () => { }, true).Open(); break;
                case "Ritual": new RitualPanel(root, () => { }, null).Open(); break;
                default:
                    var other = Overlay("Other");
                    var codex = new CodexPanel(page == "Codex" ? root : other, page == "Achievements" ? root : other, () => { });
                    if (page == "Codex") codex.OpenCodex(); else codex.OpenAchievements();
                    break;
            }
            Canvas.ForceUpdateCanvases();
            var content = root.Find("Viewport/Content");
            content.GetComponent<ReadingList>().Reflow(true);
            Canvas.ForceUpdateCanvases();
            Assert.AreEqual(columns, content.Find("Row0").childCount);
            Assert.AreEqual(123456, _save.Doo);
            Assert.IsEmpty(_save.ClaimedStarChests);
            Assert.IsEmpty(_save.ClaimedAchievements);
            Assert.IsEmpty(_save.DailyClaimed);
        }
        [Test]
        public void CompactCodexCardKeepsTheFullDescriptionInItsDetailWindow()
        {
            var item = EarlyGameContent.FindItem("frost_sword");
            Codex.SeeItem(_save, item.Id);
            var root = Overlay("Codex");
            new CodexPanel(root, Overlay("Achievements"), () => { }).OpenCodex();
            root.Find("Tab1").GetComponent<Button>().onClick.Invoke();
            var button = root.GetComponentsInChildren<Button>().First(b =>
            {
                var title = b.transform.Find("Name");
                return title != null && title.GetComponent<Text>() != null && title.GetComponent<Text>().text.Contains(item.Name);
            });
            button.onClick.Invoke();
            var detail = root.Find("InfoWindow");
            Assert.IsTrue(detail.gameObject.activeSelf);
            Assert.IsTrue(detail.GetComponentsInChildren<Text>().Any(label => label.text.Contains(item.Description)));
            Assert.AreEqual(123456, _save.Doo);
        }
    }
}
#endif
