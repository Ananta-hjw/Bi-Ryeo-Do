#if UNITY_EDITOR
using BiRyeoDo.Game.UI;
using NUnit.Framework;
using UnityEngine;

namespace BiRyeoDo.Tests.UI
{
    public class ReadingViewportTests
    {
        [TestCase(480f)]
        [TestCase(800f)]
        public void ReflowDoesNotGrowRowsToFitFixedHeightTagsAndButtons(float width)
        {
            var root = new GameObject("ReadingListTest", typeof(RectTransform), typeof(Canvas));
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                var parent = root.GetComponent<RectTransform>();
                parent.sizeDelta = new Vector2(width, 800f);
                var content = UiFactory.Rect("Content", parent, new Vector2(0f, 1f), Vector2.one);
                content.pivot = new Vector2(.5f, 1f);
                var row = UiFactory.Rect("Row", content, new Vector2(0f, 1f), Vector2.one);
                row.pivot = new Vector2(.5f, 1f);
                row.sizeDelta = new Vector2(0f, 180f);
                var icon = UiFactory.Rect("Icon", row, new Vector2(0f, .5f), new Vector2(.2f, .5f));
                icon.sizeDelta = new Vector2(0f, 120f);
                var tag = UiFactory.Label("Tag", icon, Vector2.zero, new Vector2(1f, .26f), "장착 중", 22, Color.white);
                var button = UiFactory.Button("Mastery", row, new Vector2(.75f, .5f), new Vector2(1f, .5f), "★☆☆☆☆ 입문\n0/10회", Color.white, () => { }, 24);
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 128f);
                var body = UiFactory.Label("Body", row, new Vector2(.22f, .1f), new Vector2(.72f, .9f), new string('가', 80), 30, Color.white);
                // A fixed inset must remain fixed when the row expands for the paragraph.
                body.rectTransform.offsetMin = new Vector2(0f, 8f);
                body.rectTransform.offsetMax = new Vector2(0f, -8f);
                ReadingList.Register(row, 180f);
                var layout = content.GetComponent<ReadingList>();
                Canvas.ForceUpdateCanvases();
                layout.Reflow();
                float height = row.rect.height, contentHeight = content.sizeDelta.y;
                var position = row.anchoredPosition;
                Assert.Greater(height, 180f, "Wrapped body text must still expand its row");
                for (int pass = 0; pass < 30; pass++)
                {
                    layout.SendMessage("Invalidate");
                    Canvas.ForceUpdateCanvases();
                    layout.Reflow();
                    Assert.That(row.rect.height, Is.EqualTo(height).Within(.5f));
                    Assert.That(content.sizeDelta.y, Is.EqualTo(contentHeight).Within(.5f));
                    Assert.AreEqual(position, row.anchoredPosition);
                }
                Assert.IsTrue(tag.resizeTextForBestFit, "Fixed controls keep their own fitting policy");
                body.text = "짧은 글";
                layout.SendMessage("Invalidate");
                layout.Reflow();
                Assert.That(row.rect.height, Is.EqualTo(180f).Within(.5f));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(720f)]
        [TestCase(1080f)]
        public void RemeasureKeepsParagraphInsideMask(float width)
        {
            var root = new GameObject("ReadingTest", typeof(RectTransform), typeof(Canvas));
            try
            {
                root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                var parent = root.GetComponent<RectTransform>();
                parent.sizeDelta = new Vector2(width, 1200f);
                var text = UiFactory.ScrollBody(UiFactory.Label("Body", parent,
                    Vector2.zero, Vector2.one, "안내 문장의 왼쪽과 오른쪽을 모두 표시합니다.",
                    36, Color.white));
                var reading = text.GetComponent<ReadingText>();
                foreach (var paragraph in new[] { text.text, new string('가', 1500), "짧은 안내" })
                {
                    text.text = paragraph;
                    Canvas.ForceUpdateCanvases();
                    reading.SendMessage("LateUpdate");
                    var corners = new Vector3[4];
                    text.rectTransform.GetWorldCorners(corners);
                    var left = reading.Viewport.InverseTransformPoint(corners[0]).x;
                    var right = reading.Viewport.InverseTransformPoint(corners[2]).x;
                    Assert.That(left - reading.Viewport.rect.xMin, Is.EqualTo(16f).Within(.1f));
                    Assert.That(reading.Viewport.rect.xMax - right, Is.EqualTo(42f).Within(.1f));
                    Assert.That(text.rectTransform.rect.width,
                        Is.EqualTo(reading.Viewport.rect.width - 58f).Within(.1f));
                    if (text.rectTransform.rect.height > reading.Viewport.rect.height + .1f)
                        Assert.That(reading.Scroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(.01f));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
