using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class AcademyTests
    {
        private static SaveData Save(int cleared) { return new SaveData { ClearedStage = cleared, Level = 20 }; }

        [Test]
        public void OpensAfterStageEightAndSignUpTakesThreeDistinctCourses()
        {
            Assert.IsFalse(Academy.Unlocked(Save(Academy.UnlockStage - 1)));
            var save = Save(Academy.UnlockStage);
            Assert.IsTrue(Academy.Unlocked(save));
            Assert.IsFalse(Academy.SignUp(save, new[] { "sword", "sword", "guard" }));
            Assert.IsFalse(Academy.SignUp(save, new[] { "sword", "guard" }));
            Assert.IsTrue(Academy.SignUp(save, new[] { "sword", "guard", "theory" }));
            Assert.IsFalse(Academy.SignUp(save, new[] { "harmony", "hidden", "qinggong" }), "once a 학기");
        }

        [Test]
        public void EveryLessonExamAndMatchBuildsAPlayableStageAtEveryReference()
        {
            for (int cleared = Academy.UnlockStage; cleared <= EarlyGameContent.Stages.Length; cleared++)
            {
                var save = Save(cleared);
                foreach (var c in Academy.Courses)
                {
                    var st = Academy.LessonStage(save, c.Id);
                    Assert.AreEqual(1, st.Encounters.Length, c.Id + " @" + cleared);
                    Assert.AreEqual(c.Rule, st.Trials[0].Kind);
                    Assert.IsTrue(st.IsQuest && st.AcademyId == "lesson:" + c.Id);
                    foreach (var id in st.Encounters[0])
                    {
                        var e = st.EnemyFor(id, st.Trials[0]);
                        Assert.IsNotNull(e, id);
                        Assert.Greater(e.CreateCombatant(id).Stats.MaxHp, 0);
                        Assert.IsFalse(e.IsBoss, "lessons skip bosses");
                    }
                    if (c.Rule == TrialKind.Lifeline)
                        Assert.IsTrue(st.Encounters[0].Any(id => EarlyGameContent.FindEnemy(id).Moves.Any(m => m.Charge)), "호신술 needs a charged blow to guard: " + cleared);
                }
                foreach (var h in Academy.Houses)
                {
                    var st = Academy.LeagueStage(save, h.Id);
                    Assert.IsTrue(st.BestOfThree);
                    Assert.AreEqual(3, st.Encounters.Length);
                    foreach (var fight in st.Encounters)
                        foreach (var id in fight) Assert.Greater(st.EnemyFor(id).CreateCombatant(id).Stats.MaxHp, 0, id);
                }
            }
        }

        [Test]
        public void GradesKeepTheBestAndGiveLastingBonuses()
        {
            var save = Save(20);
            Academy.SignUp(save, new[] { "sword", "guard", "theory" });
            int atk = InnerArts.HeroStats(save).Attack;
            var notes = Academy.CompleteLesson(save, "sword", 2);
            Assert.AreEqual(2, save.AcademyCredits);
            Assert.AreEqual(2, Academy.BestGrade(save, "sword"));
            Assert.IsTrue(notes.Any(n => n.Contains("영구 보너스")));
            Assert.Greater(InnerArts.HeroStats(save).Attack, atk, "검법 실기 raises attack");
            Academy.CompleteLesson(save, "sword", 1);
            Assert.AreEqual(2, save.AcademyCredits, "a worse retake changes nothing");
            Academy.CompleteLesson(save, "sword", 3);
            Assert.AreEqual(3, save.AcademyCredits, "only the improvement counts");
            Assert.IsTrue(Academy.LessonStage(save, "sword").NoReward, "a passed course is retaken without rewards");
            Assert.IsFalse(Academy.LessonStage(save, "guard").NoReward);
        }

        [Test]
        public void TheExamClosesTheSemester()
        {
            var save = Save(20);
            Academy.SignUp(save, new[] { "sword", "guard", "theory" });
            Assert.IsNull(Academy.ExamStage(save));
            foreach (var c in save.AcademyCourses.ToList()) Academy.CompleteLesson(save, c, 3);
            var exam = Academy.ExamStage(save);
            Assert.AreEqual(3, exam.Encounters.Length);
            Assert.AreEqual(TrialKind.Lifeline, exam.Trials[1].Kind);
            Assert.AreEqual("guard", Academy.CourseOf(save, exam, 1).Id);
            save.AcademyLeague.Add("baekho");
            int stones = Inventory.Count(save, InnerArts.StoneId);
            Academy.CompleteExam(save, 3);
            Assert.AreEqual(2, save.AcademySemester);
            Assert.AreEqual(0, save.AcademyCourses.Count);
            Assert.AreEqual(0, save.AcademyLeague.Count);
            Assert.AreEqual(12, save.AcademyCredits);
            Assert.AreEqual(3, Academy.BestGrade(save, "guard"), "best grades outlive the 학기");
            Assert.AreEqual(stones + 1, Inventory.Count(save, InnerArts.StoneId));
        }

        [Test]
        public void ClubsChangeOncePerSemesterAndApplyTheirPerk()
        {
            var save = Save(20);
            Assert.IsTrue(Academy.JoinClub(save, "dobaek"));
            Assert.IsFalse(Academy.JoinClub(save, "geomhon"), "once a 학기");
            int baseSpirit = Formation.BuildHero(Save(20)).StartSpirit;
            Assert.AreEqual((int)Academy.FindClub("dobaek").Perk[0], Formation.BuildHero(save).StartSpirit - baseSpirit);
            save.AcademySemester++;
            Assert.IsTrue(Academy.JoinClub(save, "geomhon"));
            float crit = InnerArts.HeroStats(Save(20)).CritRate;
            Assert.AreEqual(crit + Academy.FindClub("geomhon").Perk[0], InnerArts.HeroStats(save).CritRate, 1e-4);
            save.AcademyCredits = Academy.ClubRankCredits[2];
            Assert.AreEqual(3, Academy.ClubRank(save));
        }

        [Test]
        public void LeagueWinsCountOncePerSemesterAndASweepGivesAStone()
        {
            var save = Save(20);
            int stones = Inventory.Count(save, InnerArts.StoneId);
            foreach (var h in Academy.Houses) Assert.Greater(Academy.CompleteLeague(save, h.Id).Count, 0);
            Assert.AreEqual(0, Academy.CompleteLeague(save, "cheongryong").Count);
            Assert.AreEqual(stones + 1, Inventory.Count(save, InnerArts.StoneId));
            Assert.IsTrue(Academy.LeagueStage(save, "cheongryong").NoReward);
        }

        [Test]
        public void GradeCountsPassingSurvivalAndTheGoal()
        {
            var sword = Academy.FindCourse("sword");
            int fast = StageStars.FastLimit(20, 0);
            Assert.AreEqual(3, Academy.Grade(sword, 20, 0, fast, false, 0, 1));
            Assert.AreEqual(2, Academy.Grade(sword, 20, 0, fast + 1, false, 0, 1));
            Assert.AreEqual(1, Academy.Grade(sword, 20, 0, fast + 1, true, 0, 1));
            Assert.AreEqual(2, Academy.Grade(Academy.FindCourse("guard"), 20, 0, 99, false, 1, 1));
            Assert.AreEqual(3, Academy.Grade(Academy.FindCourse("qinggong"), 20, 0, 99, false, 0, .5));
        }
    }
}
