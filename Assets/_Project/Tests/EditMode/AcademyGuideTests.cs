using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class AcademyGuideTests
    {
        [Test]
        public void GuideRequiresProximityAndCannotBeWalkedThrough()
        {
            var walk = new AcademyWalk();
            Assert.IsFalse(AcademyGuide.Nearby(walk.X, walk.Y));
            for (int i = 0; i < 13; i++) walk.Move(-1f, 0f, .1f);
            for (int i = 0; i < 40; i++) walk.Move(0f, 1f, .1f);
            Assert.IsTrue(AcademyGuide.Nearby(walk.X, walk.Y));
            float dx = walk.X - AcademyGuide.X, dy = walk.Y - AcademyGuide.Y;
            Assert.GreaterOrEqual(dx * dx + dy * dy, 48f * 48f);
            Assert.IsFalse(AcademyWalk.Walkable(AcademyGuide.X, AcademyGuide.Y));
            Assert.IsFalse(AcademyGuide.Nearby(float.NaN, 0f));
        }

        [Test]
        public void GreetingReflectsRegistrationAndCompletedLessonsWithoutChangingSave()
        {
            var save = new SaveData { ClearedStage = Academy.UnlockStage };
            StringAssert.Contains("과목 " + Academy.CoursesPerSemester, AcademyGuide.Greeting(save));
            foreach (var course in Academy.Courses)
            {
                if (save.AcademyCourses.Count >= Academy.CoursesPerSemester) break;
                save.AcademyCourses.Add(course.Id);
            }
            StringAssert.Contains("수강한 과목", AcademyGuide.Greeting(save));
            foreach (var id in save.AcademyCourses) save.AcademyGrades.Add(id + ":1");
            StringAssert.Contains("연말 평가", AcademyGuide.Greeting(save));
            Assert.AreEqual(1, save.AcademySemester);
            Assert.AreEqual(0, save.AcademyCredits);
        }
    }
}
