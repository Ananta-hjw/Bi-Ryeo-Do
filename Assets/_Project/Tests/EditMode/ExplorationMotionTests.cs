using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class ExplorationMotionTests
    {
        [TestCase(0f, -1f, ExplorationFacing.Down)]
        [TestCase(-1f, 0f, ExplorationFacing.Left)]
        [TestCase(1f, 0f, ExplorationFacing.Right)]
        [TestCase(0f, 1f, ExplorationFacing.Up)]
        public void DirectionUsesMatchingAtlasRow(float x, float y, ExplorationFacing facing)
        {
            var motion = new ExplorationMotion(); motion.Advance(x, y, .05f);
            Assert.AreEqual(facing, motion.Facing);
            Assert.AreEqual((int)facing * 4 + 1, motion.AtlasIndex);
        }

        [Test]
        public void StoppingKeepsDirectionAndRestartsWalkAtFirstStep()
        {
            var motion = new ExplorationMotion();
            for (int i = 0; i < 4; i++) motion.Advance(-1f, 0f, .1f);
            motion.Advance(0f, 0f, .1f);
            Assert.AreEqual(ExplorationFacing.Left, motion.Facing);
            Assert.AreEqual(4, motion.AtlasIndex);
            motion.Advance(-1f, 0f, .1f);
            Assert.AreEqual(5, motion.AtlasIndex);
        }

        [Test]
        public void WalkAdvancesAndUsesNoIdleFrameUntilStopped()
        {
            var motion = new ExplorationMotion(); var seen = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < 40; i++) { motion.Advance(1f, 0f, .05f); seen.Add(motion.Frame); }
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, seen);
            motion.Stop(); Assert.AreEqual(0, motion.Frame);
        }

        [Test]
        public void ChangingDirectionRestartsTheCycleAndInvalidInputStopsIt()
        {
            var motion = new ExplorationMotion();
            for (int i = 0; i < 4; i++) motion.Advance(1f, 0f, .1f);
            motion.Advance(0f, 1f, .1f); Assert.AreEqual(13, motion.AtlasIndex);
            motion.Advance(float.NaN, 0f, .1f); Assert.AreEqual(12, motion.AtlasIndex);
        }
    }
}
