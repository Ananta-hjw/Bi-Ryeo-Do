#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BiRyeoDo.Core;
using BiRyeoDo.Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BiRyeoDo.Tests.UI
{
    public class LandscapeBattleTests
    {
        private GameObject _canvas;
        private BattleScreen _screen;

        private void Build(float width)
        {
            _canvas = new GameObject("LandscapeBattle", typeof(RectTransform), typeof(Canvas));
            _canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var root = _canvas.GetComponent<RectTransform>();
            root.sizeDelta = new Vector2(width, 1080f);
            _screen = _canvas.AddComponent<BattleScreen>();
            _screen.Build(root);
            Canvas.ForceUpdateCanvases();
        }

        [TearDown]
        public void TearDown() { if (_canvas != null) Object.DestroyImmediate(_canvas); }

        private object Invoke(string method, params object[] args)
        {
            return typeof(BattleScreen).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_screen, args);
        }

        private static bool Intersects(Rect a, Rect b)
        {
            return Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin) > .001f
                && Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin) > .001f;
        }

        private static Rect Anchors(RectTransform rect) { return Rect.MinMaxRect(rect.anchorMin.x, rect.anchorMin.y, rect.anchorMax.x, rect.anchorMax.y); }

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void AllCommandsStayReachableAndClearOfEveryBoardTileInBothModes(float width)
        {
            Build(width);
            var tileMethod = typeof(BattleScreen).GetMethod("TileCell", BindingFlags.Static | BindingFlags.NonPublic);
            foreach (bool picker in new[] { false, true })
            {
                Invoke("LayoutCommands", picker);
                var buttons = new List<Rect>();
                for (int i = 0; i < 10; i++)
                {
                    var rect = (RectTransform)_canvas.transform.Find("Command" + i);
                    var area = Anchors(rect);
                    Assert.That(area.xMin, Is.GreaterThanOrEqualTo(0f));
                    Assert.That(area.xMax, Is.LessThanOrEqualTo(1f));
                    Assert.That(area.yMin, Is.GreaterThanOrEqualTo(0f));
                    Assert.That(area.yMax, Is.LessThanOrEqualTo(.27f));
                    Assert.That(rect.rect.width, Is.GreaterThan(160f));
                    Assert.That(rect.rect.height, Is.GreaterThan(120f));
                    foreach (var previous in buttons) Assert.IsFalse(Intersects(previous, area), "Commands overlap");
                    buttons.Add(area);
                    for (int col = 0; col < Board.Cols; col++)
                        for (int row = 0; row < Board.Rows; row++)
                            Assert.IsFalse(Intersects(area, (Rect)tileMethod.Invoke(null, new object[] { col, row })), "Command covers a target");
                }
            }
        }

        [TestCase(1440f)]
        [TestCase(1920f)]
        [TestCase(2400f)]
        public void BattleUsesWideFieldAndSeparatedReadableHudAndCommandRows(float width)
        {
            Build(width);
            var header = (RectTransform)_canvas.transform.Find("BattleHeader");
            var ribbon = (RectTransform)_canvas.transform.Find("TurnRibbon");
            var focus = (RectTransform)_canvas.transform.Find("EnemyFocus");
            var actor = (RectTransform)_canvas.transform.Find("ActingMember");
            var log = (RectTransform)_canvas.transform.Find("BattleLog");
            var hint = (RectTransform)_canvas.transform.Find("CommandHint");
            var field = (RectTransform)_canvas.transform.Find("FieldWash");
            Assert.Greater(field.rect.width, width * .95f);
            Assert.IsFalse(Intersects(Anchors(ribbon), Anchors(focus)));
            Assert.IsFalse(Intersects(Anchors(header), Anchors(ribbon)));
            Assert.IsFalse(Intersects(Anchors(focus), Anchors(field)));
            Assert.IsFalse(Intersects(Anchors(actor), Anchors(log)));
            Assert.IsFalse(Intersects(Anchors(actor), Anchors(field)));
            Assert.IsFalse(Intersects(Anchors(hint), Anchors(field)));
            Assert.GreaterOrEqual(((RectTransform)focus.Find("Intent")).rect.height, 56f);
            Assert.Greater(((RectTransform)actor.Find("Actor")).offsetMin.x, 68f);
            Assert.AreEqual(5, ribbon.GetComponentsInChildren<Button>(true).Length);
            Assert.IsNotNull(focus.GetComponent<Button>());
            Assert.IsTrue(focus.GetComponent<Image>().raycastTarget);
            Assert.DoesNotThrow(() => focus.GetComponent<Button>().onClick.Invoke());
            Assert.DoesNotThrow(() => ribbon.Find("Turn0").GetComponent<Button>().onClick.Invoke());
            log.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(_canvas.transform.Find("Tip").gameObject.activeSelf);
            _canvas.transform.Find("Tip").gameObject.SetActive(false);
            int[] order = { 0, 1, 2, 3, 4, 9, 8, 5, 6, 7 };
            for (int position = 0; position < order.Length; position++)
            {
                var button = (RectTransform)_canvas.transform.Find("Command" + order[position]);
                Assert.That(button.anchorMax.y, Is.EqualTo(position < 5 ? .26f : .13f).Within(.001f));
                Assert.IsFalse(Intersects(Anchors(button), Anchors(actor)));
                Assert.IsFalse(Intersects(Anchors(button), Anchors(log)));
                var label = button.Find("Label") as RectTransform;
                var detail = button.Find("Detail") as RectTransform;
                Assert.IsFalse(Intersects(Anchors(label), Anchors(detail)));
                Assert.LessOrEqual(((RectTransform)button.Find("Icon")).anchorMax.x, label.anchorMin.x);
            }
        }

        [Test]
        public void ResultAndStoryLayersBlockTheEntireBattleAndKeepSkipSeparate()
        {
            Build(1920f);
            foreach (string name in new[] { "Panel", "Story", "Pick", "Tip" })
            {
                var root = (RectTransform)_canvas.transform.Find(name);
                Assert.AreEqual(new Rect(0f, 0f, 1f, 1f), Anchors(root));
                Assert.IsTrue(root.GetComponent<Image>().raycastTarget);
            }
            var story = _canvas.transform.Find("Story");
            var skip = story.Find("Skip").GetComponent<Button>();
            Assert.IsNotNull(skip);
            Assert.IsFalse(Intersects(Anchors((RectTransform)skip.transform), Anchors((RectTransform)story.Find("Text"))));
        }

        [TestCase(1440f)]
        [TestCase(2400f)]
        public void FightRewardsSeparateStarsAndCurrencyFromTheScrollableLoot(float width)
        {
            Build(width);
            var stack = new Stack<IEnumerator>();
            stack.Push((IEnumerator)Invoke("ShowFightRewards", EarlyGameContent.Stages[0], 0, 7, 3, 12345L, 67890L,
                new List<ItemStack>(), null, 0));
            try
            {
                // Enter the nested confirmation coroutine, then inspect the real reward UI before clicking it.
                while (stack.Count > 0)
                {
                    var current = stack.Peek();
                    if (!current.MoveNext()) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (current.Current is IEnumerator nested) { stack.Push(nested); continue; }
                    break;
                }
                var card = _canvas.transform.Find("FightRewards/RewardCard");
                var list = (RectTransform)card.Find("Viewport");
                Assert.That(list.rect.width, Is.GreaterThan(650f));
                Assert.That(list.rect.height, Is.GreaterThan(500f));
                Assert.IsFalse(Intersects(Anchors(list), Anchors((RectTransform)card.Find("Stars"))));
                foreach (Transform child in card)
                    if (child.name == "RewardValue") Assert.IsFalse(Intersects(Anchors(list), Anchors((RectTransform)child)));
                Assert.IsTrue(_canvas.transform.Find("FightRewards/Dim").GetComponent<Image>().raycastTarget);
                Assert.IsNotNull(card.Find("Continue"));
            }
            finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); }
        }

        [Test]
        public void FigureRefitsAfterResizeAndKeepsEnemyFacing()
        {
            var root = new GameObject("Cell", typeof(RectTransform));
            var texture = new Texture2D(100, 200);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 100f, 200f), new Vector2(.5f, 0f));
            try
            {
                var cell = root.GetComponent<RectTransform>();
                var image = UiFactory.Panel("Art", cell, new Vector2(.5f, .38f), new Vector2(.5f, .38f), Color.white);
                image.sprite = sprite;
                image.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                var fit = image.gameObject.AddComponent<BattleFigureFit>();
                foreach (var size in new[] { new Vector2(240f, 220f), new Vector2(80f, 400f) })
                {
                    cell.sizeDelta = size; fit.Fit();
                    Assert.LessOrEqual(image.rectTransform.rect.width, size.x * .96f + .01f);
                    Assert.LessOrEqual(image.rectTransform.rect.height, size.y * .52f + .01f);
                    Assert.That(image.rectTransform.rect.width / image.rectTransform.rect.height, Is.EqualTo(.5f).Within(.001f));
                    Assert.AreEqual(-1f, image.rectTransform.localScale.x);
                }
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }
    }
}
#endif
