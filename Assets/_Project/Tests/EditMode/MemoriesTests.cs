using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class MemoriesTests
    {
        [Test]
        public void MemoriesAreWellFormed()
        {
            int max = EarlyGameContent.Stages.Max(s => s.Number);
            Assert.AreEqual(Memories.All.Length, Memories.All.Select(m => m.Id).Distinct().Count());
            foreach (var m in Memories.All)
            {
                Assert.LessOrEqual(m.UnlockStage, max, m.Id);
                Assert.GreaterOrEqual(m.Volume, 1, m.Id);
                Assert.LessOrEqual(m.Volume, 16, m.Id);   // only analysed volumes (Story/INDEX.md)
                Assert.GreaterOrEqual(m.Lines.Length, 2, m.Id);
                Assert.Greater(m.RewardDoo, 0, m.Id);
                foreach (var raw in m.Lines)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(raw), m.Id);
                    var line = StoryLine.Parse(raw);
                    if (line.IsDialogue) Assert.LessOrEqual(line.Speaker.Length, 8, m.Id + ": " + raw);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(line.Text), m.Id);
                }
            }
            foreach (var ms in Memories.Milestones) if (ms.ItemId != null) Assert.IsNotNull(EarlyGameContent.FindItem(ms.ItemId));
            Assert.LessOrEqual(Memories.Milestones.Last().Count, Memories.All.Length);
        }

        [Test]
        public void MemoriesOpenWithStageProgress()
        {
            var save = new SaveData { ClearedStage = 11 };
            Assert.AreEqual(0, Memories.UnseenCount(save));
            save.ClearedStage = 14;
            Assert.AreEqual(3, Memories.UnseenCount(save));
            save.ClearedStage = 30;
            Assert.AreEqual(Memories.All.Length, Memories.UnseenCount(save));
        }

        [Test]
        public void FirstViewPaysOnceAndRaisesTheBond()
        {
            var save = new SaveData { ClearedStage = 14 };
            long before = save.Doo;
            var notes = Memories.Complete(save, "m_cave");
            Assert.Greater(notes.Count, 0);
            Assert.AreEqual(before + Memories.Find("m_cave").RewardDoo, save.Doo);
            Assert.AreEqual(Memories.BondPerMemory, Bonds.Points(save, "na_yerin"));
            Assert.AreEqual(0, Memories.Complete(save, "m_cave").Count);   // already seen
            Assert.AreEqual(Memories.BondPerMemory, Bonds.Points(save, "na_yerin"));
            Assert.AreEqual(0, Memories.Complete(save, "m_sword").Count);  // still locked
            Assert.AreEqual(0, Memories.Complete(save, "nope").Count);
        }

        [Test]
        public void MilestonesPayWhenTheCountIsReached()
        {
            var save = new SaveData { ClearedStage = 30 };
            for (int i = 0; i < 3; i++) Memories.Complete(save, Memories.All[i].Id);
            Assert.AreEqual(0, Inventory.Count(save, "ginseng"));
            var notes = Memories.Complete(save, Memories.All[3].Id);
            Assert.IsTrue(notes.Any(n => n.Contains("추억 4개")));
            Assert.AreEqual(1, Inventory.Count(save, "ginseng"));
            for (int i = 4; i < Memories.All.Length; i++) Memories.Complete(save, Memories.All[i].Id);
            Assert.AreEqual(3, save.ClaimedMemoryMilestones.Count);
            Assert.AreEqual(3, Inventory.Count(save, "ilgeup_yeongdan"));
            Assert.IsNull(Memories.NextMilestone(save));
        }
    }
}
