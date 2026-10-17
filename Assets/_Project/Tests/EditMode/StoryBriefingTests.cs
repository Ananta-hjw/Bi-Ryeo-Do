using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StoryBriefingTests
    {
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
        public void StoriesAreMostlyDialogueAndInterludesAreWellFormed()
        {
            int interludes = 0;
            foreach (var s in EarlyGameContent.Stages)
            {
                foreach (var lines in new[] { StoryBriefings.Briefing(s.Number), StoryBriefings.Aftermath(s.Number) })
                {
                    int talk = 0;
                    foreach (var l in lines) if (l.IndexOf('|') > 0) talk++;
                    Assert.GreaterOrEqual(talk * 100, lines.Length * 70, "dialogue share in stage " + s.Number);
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
        public void ReadableCoversClearedStagesAndTheNext()
        {
            var save = new SaveData { ClearedStage = 3 };
            var list = StoryBriefings.Readable(save);
            Assert.AreEqual(4, list.Count);
            Assert.AreEqual(4, list[list.Count - 1]);
        }
    }
}
