using System;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class AcademyWalkTests
    {
        [Test]
        public void DiagonalMovementHasTheSameSpeedAsStraightMovement()
        {
            var straight = new AcademyWalk(); var diagonal = new AcademyWalk();
            straight.Move(1f, 0f, .1f); diagonal.Move(1f, -1f, .1f);
            double dx = diagonal.X - 1200f, dy = diagonal.Y - 400f;
            Assert.AreEqual(straight.X - 1200f, Math.Sqrt(dx * dx + dy * dy), .001);
        }

        [Test]
        public void GardenBlocksMovementAndAllowsSlidingAroundItsEdge()
        {
            var walk = new AcademyWalk();
            for (int i = 0; i < 30; i++) walk.Move(0f, 1f, .1f);
            Assert.Less(walk.Y, 650f - AcademyWalk.Radius);
            for (int i = 0; i < 30; i++) walk.Move(-1f, 1f, .1f);
            Assert.Less(walk.X, 880f - AcademyWalk.Radius);
            Assert.Greater(walk.Y, 800f + AcademyWalk.Radius);
            Assert.IsTrue(AcademyWalk.Walkable(walk.X, walk.Y));
        }

        [Test]
        public void PlayerCannotCrossMapBoundaryOrEnterBuildings()
        {
            var walk = new AcademyWalk();
            for (int i = 0; i < 200; i++) walk.Move(-1f, 0f, .1f);
            Assert.GreaterOrEqual(walk.X, AcademyWalk.Radius);
            for (int i = 0; i < 200; i++) walk.Move(0f, -1f, .1f);
            Assert.GreaterOrEqual(walk.Y, AcademyWalk.Radius);
            Assert.IsFalse(AcademyWalk.Walkable(500f, 1250f));
            Assert.IsTrue(AcademyWalk.Walkable(500f, 1100f));
        }

        [Test]
        public void EntranceRequiresProximityAndCanBeReachedAroundGarden()
        {
            var walk = new AcademyWalk();
            Assert.AreEqual(-1, walk.NearbyFacility());
            for (int i = 0; i < 23; i++) walk.Move(-1f, 0f, .1f);
            for (int i = 0; i < 23; i++) walk.Move(0f, 1f, .1f);
            Assert.AreEqual(0, walk.NearbyFacility());
            for (int i = 0; i < 20; i++) walk.Move(0f, 1f, .1f);
            Assert.Less(walk.Y, 1150f - AcademyWalk.Radius);
        }

        [Test]
        public void MapEdgesKeepTheFigureAndNamePlateInsideTheGround()
        {
            var walk = new AcademyWalk();
            for (int i = 0; i < 200; i++) walk.Move(-1f, 0f, .1f);
            for (int i = 0; i < 200; i++) walk.Move(0f, 1f, .1f);
            Assert.GreaterOrEqual(walk.X - AcademyWalk.SideClearance, 0f);
            Assert.LessOrEqual(walk.Y + AcademyWalk.TopClearance, AcademyWalk.Height);
            Assert.IsFalse(AcademyWalk.Walkable(1200f, 1570f));
            for (int i = 0; i < 200; i++) walk.Move(1f, 0f, .1f);
            Assert.LessOrEqual(walk.X + AcademyWalk.SideClearance, AcademyWalk.Width);
        }

        [Test]
        public void AllThreeEntrancesRemainReachableWithVisualClearance()
        {
            var walk = new AcademyWalk();
            for (int i = 0; i < 23; i++) walk.Move(-1f, 0f, .1f);
            for (int i = 0; i < 23; i++) walk.Move(0f, 1f, .1f);
            Assert.AreEqual(0, walk.NearbyFacility());
            for (int i = 0; i < 23; i++) walk.Move(1f, 0f, .1f);
            Assert.AreEqual(1, walk.NearbyFacility());
            for (int i = 0; i < 23; i++) walk.Move(1f, 0f, .1f);
            Assert.AreEqual(2, walk.NearbyFacility());
        }

        [Test]
        public void LongFramesAndInvalidInputCannotTeleportThePlayer()
        {
            var walk = new AcademyWalk();
            walk.Move(float.NaN, 0f, .1f); walk.Move(1f, 0f, float.PositiveInfinity);
            walk.Move(1f, 0f, -1f);
            Assert.AreEqual(1200f, walk.X);
            walk.Move(1f, 0f, 100f);
            Assert.AreEqual(1230f, walk.X);
        }
    }
}
