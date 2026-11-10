using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class AcademyResidentsTests
    {
        [TestCase(0, 10)] [TestCase(1, 33)] [TestCase(2, 55)]
        public void AllResidentsAreReachableAndRouteToDistinctFacilities(int facility, int eastSteps)
        {
            var walk = new AcademyWalk();
            Assert.IsNull(AcademyResidents.Nearby(walk.X, walk.Y));
            for (int i = 0; i < 23; i++) walk.Move(-1f, 0f, .1f);
            for (int i = 0; i < 23; i++) walk.Move(0f, 1f, .1f);
            for (int i = 0; i < eastSteps; i++) walk.Move(1f, 0f, .1f);
            var resident = AcademyResidents.Nearby(walk.X, walk.Y);
            Assert.IsNotNull(resident); Assert.AreEqual(facility, resident.Facility);
        }

        [Test]
        public void EachResidentBlocksItsOwnFeetAndHasAnAccessibleApproach()
        {
            foreach (var resident in AcademyResidents.All)
            {
                Assert.IsFalse(AcademyWalk.Walkable(resident.X, resident.Y));
                Assert.IsTrue(AcademyWalk.Walkable(resident.X, resident.Y - 90f));
                Assert.AreSame(resident, AcademyResidents.Nearby(resident.X, resident.Y - 90f));
            }
            Assert.IsNull(AcademyResidents.Nearby(float.NaN, float.PositiveInfinity));
        }

        [Test]
        public void ClubGreetingReflectsMembershipAndChangeAvailability()
        {
            var save = new SaveData { ClearedStage = 30 };
            var npc = AcademyResidents.All[1];
            StringAssert.Contains("원하는 동호회", AcademyResidents.Greeting(npc, save));
            Assert.IsTrue(Academy.JoinClub(save, Academy.Clubs[0].Id));
            StringAssert.Contains(Academy.Clubs[0].Name, AcademyResidents.Greeting(npc, save));
            StringAssert.Contains("다음 학기", AcademyResidents.Greeting(npc, save));
            save.AcademySemester++;
            StringAssert.Contains("변경하실 수", AcademyResidents.Greeting(npc, save));
            Assert.AreEqual(Academy.Clubs[0].Id, save.AcademyClub);
        }

        [Test]
        public void LeagueGreetingReflectsWinsAndRewardlessPractice()
        {
            var save = new SaveData { ClearedStage = 30 };
            var npc = AcademyResidents.All[2];
            StringAssert.Contains("0 / " + Academy.Houses.Length, AcademyResidents.Greeting(npc, save));
            foreach (var house in Academy.Houses) save.AcademyLeague.Add(house.Id);
            StringAssert.Contains("추가 보상은 없습니다", AcademyResidents.Greeting(npc, save));
            save.ClearedStage = Academy.UnlockStage; save.AcademySemester = 2;
            StringAssert.Contains("보상 없는 연습", AcademyResidents.Greeting(npc, save));
            Assert.AreEqual(Academy.Houses.Length, save.AcademyLeague.Count);
            Assert.AreEqual(0, save.AcademyCredits);
        }
    }
}
