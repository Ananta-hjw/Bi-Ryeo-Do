#if UNITY_EDITOR
using BiRyeoDo.Game.UI;
using NUnit.Framework;
using UnityEngine;

namespace BiRyeoDo.Tests.UI
{
    public class ReadingViewportTests
    {
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
