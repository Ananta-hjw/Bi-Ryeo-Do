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
        public void ReadableCoversClearedStagesAndTheNext()
        {
            var save = new SaveData { ClearedStage = 3 };
            var list = StoryBriefings.Readable(save);
            Assert.AreEqual(4, list.Count);
            Assert.AreEqual(4, list[list.Count - 1]);
        }
    }
}
