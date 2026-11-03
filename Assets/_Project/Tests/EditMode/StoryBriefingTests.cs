using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StoryBriefingTests
    {
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(4, 4)]
        [TestCase(5, 5)]
        [TestCase(6, 6)]
        [TestCase(7, 7)]
        [TestCase(8, 8)]
        [TestCase(9, 9)]
        [TestCase(10, 10)]
        [TestCase(11, 11)]
        [TestCase(12, 12)]
        [TestCase(13, 13)]
        [TestCase(14, 14)]
        [TestCase(15, 15)]
        [TestCase(16, 16)]
        [TestCase(17, 17)]
        [TestCase(18, 18)]
        [TestCase(19, 19)]
        [TestCase(20, 20)]
        [TestCase(21, 21)]
        [TestCase(22, 22)]
        [TestCase(23, 23)]
        [TestCase(24, 24)]
        [TestCase(25, 25)]
        [TestCase(26, 26)]
        [TestCase(27, 27)]
        [TestCase(28, 28)]
        [TestCase(29, 29)]
        [TestCase(30, 30)]
        [TestCase(31, 31)]
        [TestCase(32, 32)]
        [TestCase(33, 33)]
        [TestCase(34, 34)]
        [TestCase(35, 35)]
        public void EarlyStoriesCannotBePreviewedBeforeClearing(int cleared, int readableCount)
        {
            var save = new SaveData { ClearedStage = cleared };
            var readable = StoryBriefings.Readable(save);
            Assert.AreEqual(readableCount, readable.Count);
            for (int chapter = 1; chapter <= 36; chapter++)
                Assert.AreEqual(chapter <= cleared, StoryBriefings.CanRead(save, chapter));
            Assert.IsFalse(StoryBriefings.CanRead(save, 0));
            Assert.IsFalse(StoryBriefings.CanRead(save, 999));
            Assert.IsFalse(StoryBriefings.CanRead(null, 1));
        }

        [Test]
        public void EveryStageHasBriefingAndAftermath()
        {
            foreach (var s in EarlyGameContent.Stages)
            {
                var before = StoryBriefings.Briefing(s.Number);
                var after = StoryBriefings.Aftermath(s.Number);
                Assert.IsNotNull(before, "briefing " + s.Number);
                Assert.IsNotNull(after, "aftermath " + s.Number);
                Assert.GreaterOrEqual(before.Length, 8, "briefing length " + s.Number);
                Assert.GreaterOrEqual(after.Length, 3, "aftermath length " + s.Number);
            }
        }

        [Test]
        public void LinesAreNarrationOrWellFormedDialogue()
        {
            foreach (var s in EarlyGameContent.Stages)
            {
                var all = new System.Collections.Generic.List<string>(StoryBriefings.Briefing(s.Number));
                all.AddRange(StoryBriefings.Aftermath(s.Number));
                foreach (var line in all)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(line), "blank line in stage " + s.Number);
                    Assert.LessOrEqual(line.Length, 160, "too long in stage " + s.Number + ": " + line);
                    int bar = line.IndexOf('|');
                    if (bar >= 0) Assert.That(bar, Is.InRange(1, 8), "speaker name in stage " + s.Number + ": " + line);
                }
            }
        }

        [Test]
        public void LegacyStoriesKeepDialogueAndInterludesAreWellFormed()
        {
            int interludes = 0;
            foreach (var s in EarlyGameContent.Stages)
            {
                foreach (var lines in new[] { StoryBriefings.Briefing(s.Number), StoryBriefings.Aftermath(s.Number) })
                {
                    int talk = 0;
                    foreach (var l in lines) if (l.IndexOf('|') > 0) talk++;
                    // New reanalysis summaries use narration to avoid inventing canonical quotations.
                    if (s.Number <= 36) Assert.GreaterOrEqual(talk * 100, lines.Length * 70, "dialogue share in stage " + s.Number);
                }
                var between = StoryBriefings.Interlude(s.Number);
                if (between == null) continue;
                interludes++;
                Assert.IsFalse(string.IsNullOrEmpty(between.Title));
                Assert.GreaterOrEqual(between.Lines.Length, 6, "interlude length " + s.Number);
                foreach (var l in between.Lines)
                {
                    Assert.LessOrEqual(l.Length, 160, "interlude line too long in stage " + s.Number);
                    int bar = l.IndexOf('|');
                    if (bar >= 0) Assert.That(bar, Is.InRange(1, 8), "speaker in interlude " + s.Number + ": " + l);
                }
            }
            Assert.GreaterOrEqual(interludes, 15, "comic interludes");
        }

        [Test]
        public void LaterStoriesKeepTheirExistingNextChapterPreview()
        {
            var save = new SaveData { ClearedStage = 36 };
            var list = StoryBriefings.Readable(save);
            Assert.AreEqual(37, list.Count);
            Assert.AreEqual(37, list[list.Count - 1]);
        }
    }
}
