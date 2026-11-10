namespace BiRyeoDo.Core
{
    /// <summary>An unnamed game guide, not an added canonical character or story event.</summary>
    public static class AcademyGuide
    {
        public const string Name = "학관 안내원";
        public const float X = 800f, Y = 1000f, Radius = 24f, TalkRange = 150f;

        public static bool Nearby(float x, float y)
        {
            float dx = x - X, dy = y - Y;
            return dx * dx + dy * dy <= TalkRange * TalkRange;
        }

        public static string Greeting(SaveData save)
        {
            if (!Academy.Unlocked(save)) return "학관 생활이 시작되면 수강과 수업을 안내해 드리겠습니다.";
            if (save.AcademyCourses.Count == 0)
                return Academy.SemesterOpen(save)
                    ? "이번 학기에 들을 과목 " + Academy.CoursesPerSemester + "개를 골라 주세요. 수강신청과 수업은 뒤쪽 건물에서 확인하실 수 있습니다."
                    : Academy.SemesterLockText(save);
            if (Academy.ExamOpen(save)) return "이번 학기의 과목을 모두 이수하셨습니다. 수업 메뉴에서 연말 평가를 확인해 주세요.";
            return "수강한 과목의 수업과 이번 학기 성적을 확인하실 수 있습니다. 이미 이수한 과목은 다시 치러 성적을 높이실 수 있습니다.";
        }
    }
}
