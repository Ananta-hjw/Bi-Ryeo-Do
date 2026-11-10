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
        public void NearDiagonalInputKeepsTheCurrentRowAndWalkCycle()
        {
            var motion = new ExplorationMotion();
            motion.Advance(1f, 0f, .05f);
            for (int i = 0; i < 20; i++)
            {
                // jitter around 45 degrees: neither axis is 1.25 times the other
                motion.Advance(1f, i % 2 == 0 ? 1.05f : .95f, .05f);
                Assert.AreEqual(ExplorationFacing.Right, motion.Facing);
            }
            motion.Advance(.5f, 1f, .05f);
            Assert.AreEqual(ExplorationFacing.Up, motion.Facing);
            motion.Stop(); motion.Advance(1f, 1.05f, .05f);
            Assert.AreEqual(ExplorationFacing.Up, motion.Facing, "from a standstill the larger axis decides");
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
