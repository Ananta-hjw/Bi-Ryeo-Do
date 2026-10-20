using System;
using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using BiRyeoDo.Game.UI;
using NUnit.Framework;

namespace BiRyeoDo.Tests.UI
{
    public class ReadingPagesTests
    {
        private static float Lines(string text)
        {
            int lines = 1, column = 0;
            foreach (char c in text)
            {
                if (c == '\r') continue;
                if (c == '\n') { lines++; column = 0; continue; }
                int width = c == '\t' ? 4 : c > 127 ? 2 : 1;
                if (column + width > 36) { lines++; column = 0; }
                column += width;
            }
            return lines;
        }

        [TestCase("")]
        [TestCase(null)]
        public void EmptyStoryStillHasAnAdvanceablePage(string text)
        {
            CollectionAssert.AreEqual(new[] { "" }, ReadingPages.Split(text, Lines, 5f));
        }

        [Test]
        public void WrappedParagraphsPreserveEveryCharacterAndExplicitBreak()
        {
            string text = string.Concat(Enumerable.Repeat("첫 문장입니다.\n다음 줄입니다!  두 칸\t탭도 보존합니다.\r\n", 20));
            var pages = ReadingPages.Split(text, Lines, 5f);
            Assert.Greater(pages.Count, 1);
            Assert.AreEqual(text, string.Concat(pages));
            foreach (var page in pages) { Assert.IsNotEmpty(page); Assert.LessOrEqual(Lines(page), 5f); }
        }

        [Test]
        public void PageBoundaryNeverSeparatesASupplementaryCharacter()
        {
            string text = "가나다😀라마바😀사아자😀차카타😀파하";
            var pages = ReadingPages.Split(text, s => s.Length, 4f);
            Assert.AreEqual(text, string.Concat(pages));
            foreach (var page in pages)
            {
                Assert.IsFalse(char.IsLowSurrogate(page[0]));
                Assert.IsFalse(char.IsHighSurrogate(page[page.Length - 1]));
            }
        }

        [Test]
        public void ATooSmallViewportStillMakesProgressWithoutLosingText()
        {
            string text = "😀가나다";
            var pages = ReadingPages.Split(text, s => s.Length, 0f);
            Assert.AreEqual(text, string.Concat(pages));
            Assert.That(pages.Count, Is.InRange(1, 4));
        }

        [Test]
        public void PagePrefersASentenceBoundaryNearItsEnd()
        {
            var pages = ReadingPages.Split("가나다라마. 사아자차카타파하", s => s.Length, 10f);
            Assert.AreEqual("가나다라마. ", pages[0]);
        }

        [Test]
        public void ExistingStoryDialogueAndBossLinesRemainLossless()
        {
            var sources = new List<string>();
            foreach (var stage in EarlyGameContent.Stages)
            {
                if (stage.Intro != null) sources.AddRange(stage.Intro);
                if (stage.Outro != null) sources.AddRange(stage.Outro);
                var before = StoryBriefings.Briefing(stage.Number);
                if (before != null) sources.AddRange(before);
                var after = StoryBriefings.Aftermath(stage.Number);
                if (after != null) sources.AddRange(after);
            }
            foreach (var id in BossPhaseLines.Ids)
            {
                string[] first, second;
                BossPhaseLines.TryGet(id, out first, out second);
                sources.AddRange(first); sources.AddRange(second);
            }
            foreach (var memory in Memories.All) sources.AddRange(memory.Lines);
            Assert.Greater(sources.Count, 200);
            foreach (var raw in sources)
            {
                string text = StoryLine.Parse(raw).Text;
                var pages = ReadingPages.Split(text, Lines, 5f);
                Assert.AreEqual(text, string.Concat(pages));
                foreach (var page in pages) Assert.LessOrEqual(Lines(page), 5f, raw);
            }
        }
    }
}
