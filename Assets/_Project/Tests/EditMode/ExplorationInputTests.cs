using System;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class ExplorationInputTests
    {
        [TestCase(0f, 0f)] [TestCase(.1f, .1f)] [TestCase(-.14f, 0f)]
        public void SmallStickNoiseDoesNotMoveThePlayer(float x, float y)
        {
            float horizontal, vertical; ExplorationInput.Stick(x, y, out horizontal, out vertical);
            Assert.AreEqual(0f, horizontal); Assert.AreEqual(0f, vertical);
        }

        [Test]
        public void StickPreservesDirectionAndHasAContinuousSpeedRange()
        {
            float x, y; ExplorationInput.Stick(.575f, 0f, out x, out y);
            Assert.AreEqual(.5f, x, .0001f);
            ExplorationInput.Stick(-2f, 2f, out x, out y);
            Assert.AreEqual(-x, y, .0001f);
            Assert.AreEqual(1f, Math.Sqrt(x * x + y * y), .0001);
            ExplorationInput.Stick(float.NaN, 1f, out x, out y);
            Assert.AreEqual(0f, x); Assert.AreEqual(0f, y);
        }

        [TestCase(1920f, 874.8f)] [TestCase(2400f, 874.8f)] [TestCase(1440f, 874.8f)]
        public void CameraKeepsAllMapEdgesCoveredAtSupportedAspectRatios(float width, float height)
        {
            foreach (float x in new[] { 112f, 1200f, 2288f })
            {
                float offset = ExplorationInput.CameraOffset(x, width, AcademyWalk.Width);
                Assert.LessOrEqual(offset - AcademyWalk.Width / 2f, -width / 2f);
                Assert.GreaterOrEqual(offset + AcademyWalk.Width / 2f, width / 2f);
                Assert.LessOrEqual(Math.Abs(offset + x - AcademyWalk.Width / 2f), width / 2f);
            }
            foreach (float y in new[] { 24f, 800f, 1392f })
            {
                float offset = ExplorationInput.CameraOffset(y, height, AcademyWalk.Height);
                Assert.LessOrEqual(offset - AcademyWalk.Height / 2f, -height / 2f);
                Assert.GreaterOrEqual(offset + AcademyWalk.Height / 2f, height / 2f);
            }
        }

        [Test]
        public void OversizedViewportKeepsTheMapCentered()
        {
            Assert.AreEqual(0f, ExplorationInput.CameraOffset(112f, 3000f, AcademyWalk.Width));
        }
    }
}
