using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>One 천무학관 course: a lesson fight with a rule, graded 갑·을·병, whose best grade leaves a lasting bonus.</summary>
    public sealed class CourseData
    {
        public string Id, Name, Teacher, Brief, BonusName;
        public TrialKind Rule;
        public float[] Bonus;          // by grade 병·을·갑 (index 0..2)
        public bool PercentBonus = true;
        public string GoalText;        // what the third grade point asks for, shown before the lesson
    }

    /// <summary>A 동호회: one joined at a time; its perk grows with the 학점 earned.</summary>
    public sealed class ClubData
    {
        public string Id, Name, Brief, PerkName;
        public float[] Perk;           // by club rank 1..3 (index 0..2)
        public bool PercentPerk;
    }

    /// <summary>A rival 단 for the 단 대항전.</summary>
    public sealed class HouseData
    {
        public string Id, Name, Style, Brief;
        public EnemyMove[] MemberMoves, LeaderMoves;
    }

    /// <summary>
    /// 천무학관 생활: 수강신청 (three courses a 학기, each a graded lesson fight), the 연말 평가 that closes the 학기, 동호회 perks, and the
    /// 단 대항전 against 청룡단·백호단·현무단 (best of three fights). The school's frame — 학점제 과목, 수강신청, 동호회 의무 가입, the four 단,
    /// the 연말 평가 — is from 1-3권 (Story/analysis/vol01-03.md); every course, rule, perk and number here is game design, not canon.
    /// Fights borrow the enemies of the latest cleared stage (<see cref="ReferenceStage"/>) and run as 의뢰-type stages (IsQuest + AcademyId).
    /// </summary>
    public static class Academy
    {
        public const int UnlockStage = 8;          // after the protagonist enters the 학관 (stage 8 also opens the tower)
        public const int CoursesPerSemester = 3;
        public static readonly string[] GradeNames = { "", "병", "을", "갑" };
        public static readonly int[] ClubRankCredits = { 0, 12, 30 };   // 학점 for club rank 1·2·3

        public const int LessonReputation = 1, LeagueReputation = 2, ExamReputation = 5;

        public static readonly CourseData[] Courses =
        {
            new CourseData { Id = "sword", Name = "검법 실기", Teacher = "검법 교관", Rule = TrialKind.Deadline, BonusName = "공격", Bonus = new[] { .02f, .04f, .06f },
                Brief = "정해진 행동 안에 상대를 모두 쓰러뜨리는 실기 시험입니다.", GoalText = "신속: 기준 행동 이내" },
            new CourseData { Id = "guard", Name = "호신술", Teacher = "호신술 교관", Rule = TrialKind.Lifeline, BonusName = "방어", Bonus = new[] { .03f, .06f, .09f },
                Brief = "큰 공격을 맞을 때마다 생명줄이 하나씩 끊어집니다. 두 번 맞으면 낙제입니다.", GoalText = "무결: 생명줄을 하나도 잃지 않기" },
            new CourseData { Id = "harmony", Name = "합격술", Teacher = "합격술 교관", Rule = TrialKind.Harmony, BonusName = "치명타 피해", Bonus = new[] { .05f, .10f, .15f },
                Brief = "모두가 끊김 없이 공격하면 다음 일격이 크게 강해집니다. 정해진 행동 안에 이겨야 합니다.", GoalText = "신속: 기준 행동 이내" },
            new CourseData { Id = "qinggong", Name = "경공", Teacher = "경공 교관", Rule = TrialKind.Survive, BonusName = "속도", Bonus = new[] { 2f, 4f, 6f }, PercentBonus = false,
                Brief = "적의 공세를 정해진 횟수만큼 버티면 통과합니다.", GoalText = "여유: 파티 체력 50% 이상 남기기" },
            new CourseData { Id = "hidden", Name = "암기술", Teacher = "당평", Rule = TrialKind.Deadline, BonusName = "치명타", Bonus = new[] { .01f, .02f, .03f },
                Brief = "당평 교관의 암기 수업입니다. 짧은 행동 안에 끝내야 합니다.", GoalText = "신속: 기준 행동보다 2행동 빨리" },
            new CourseData { Id = "theory", Name = "무도총론", Teacher = "무도총론 교관", Rule = TrialKind.Deadline, BonusName = "체력", Bonus = new[] { .03f, .06f, .09f },
                Brief = "배운 것을 차분히 실전에 옮기는 시험입니다. 행동 제한이 넉넉합니다.", GoalText = "여유: 파티 체력 60% 이상 남기기" },
        };

        public static readonly ClubData[] Clubs =
        {
            new ClubData { Id = "geomhon", Name = "검혼회", PerkName = "치명타", Perk = new[] { .02f, .03f, .04f }, PercentPerk = true,
                Brief = "검을 숭상하는 학관 최대의 동호회입니다." },
            new ClubData { Id = "dobaek", Name = "도백회", PerkName = "시작 기세", Perk = new[] { 10f, 15f, 20f },
                Brief = "검혼회를 견제하는 또 하나의 큰 동호회입니다." },
            new ClubData { Id = "aesojeo", Name = "애소저회", PerkName = "맞을 때 기세", Perk = new[] { 3f, 4f, 5f },
                Brief = "아름다운 소저를 아끼는 사람들의 모임입니다." },
        };

        public static readonly HouseData[] Houses =
        {
            new HouseData { Id = "cheongryong", Name = "청룡단", Style = "검", Brief = "3학년 최고 인재들이 모인 단입니다.",
                MemberMoves = new[] { EnemyMove.Attack("청룡검", 0.7f, 2), EnemyMove.Attack("검격", 1.2f) },
                LeaderMoves = new[] { EnemyMove.Attack("청룡연검", 0.7f, 3), EnemyMove.Attack("검격", 1.2f), EnemyMove.Wind("검기를 모은다"), EnemyMove.Attack("청룡승천검", 2.6f) } },
            new HouseData { Id = "baekho", Name = "백호단", Style = "도", Brief = "힘으로 밀어붙이는 단입니다.",
                MemberMoves = new[] { EnemyMove.Attack("호조도", 1.1f), EnemyMove.Attack("연환도", 0.65f, 2) },
                LeaderMoves = new[] { EnemyMove.Attack("호조도", 1.1f), EnemyMove.Attack("연환도", 0.65f, 2), EnemyMove.Wind("도를 치켜든다"), EnemyMove.Attack("백호참", 2.7f) } },
            new HouseData { Id = "hyeonmu", Name = "현무단", Style = "권", Brief = "단단하게 버티는 단입니다.",
                MemberMoves = new[] { EnemyMove.Attack("현무권", 1.0f), EnemyMove.Attack("연환권", 0.6f, 2) },
                LeaderMoves = new[] { EnemyMove.Attack("현무권", 1.0f), EnemyMove.Attack("연환권", 0.6f, 2), EnemyMove.Wind("기를 끌어모은다"), EnemyMove.Attack("현무붕산권", 2.5f) } },
        };

        public static CourseData FindCourse(string id) { foreach (var c in Courses) if (c.Id == id) return c; return null; }
        public static ClubData FindClub(string id) { foreach (var c in Clubs) if (c.Id == id) return c; return null; }
        public static HouseData FindHouse(string id) { foreach (var h in Houses) if (h.Id == id) return h; return null; }

        public static bool Unlocked(SaveData save) { return save != null && save.ClearedStage >= UnlockStage; }

        /// <summary>The stage whose enemies the 학관 fights borrow: the latest cleared one (lessons grow with the story).</summary>
        public static int ReferenceStage(SaveData save)
        {
            return Math.Max(UnlockStage, Math.Min(EarlyGameContent.Stages.Length, save.ClearedStage));
        }

        // ------------------------------------------------------------------ grades and records

        private static int GradeIn(List<string> list, string id)
        {
            foreach (var e in list)
            {
                int colon = e.IndexOf(':');
                if (colon > 0 && e.Substring(0, colon) == id) { int g; if (int.TryParse(e.Substring(colon + 1), out g)) return g; }
            }
            return 0;
        }

        private static void SetGrade(List<string> list, string id, int grade)
        {
            list.RemoveAll(e => e.StartsWith(id + ":"));
            list.Add(id + ":" + grade);
        }

        /// <summary>The grade passed this 학기 (0 = not yet).</summary>
        public static int SemesterGrade(SaveData save, string courseId) { return GradeIn(save.AcademyGrades, courseId); }

        /// <summary>The best grade ever (0 = never passed): it sets the lasting bonus.</summary>
        public static int BestGrade(SaveData save, string courseId) { return GradeIn(save.AcademyBest, courseId); }

        public static int ClubRank(SaveData save)
        {
            int rank = 1;
            for (int i = 1; i < ClubRankCredits.Length; i++) if (save.AcademyCredits >= ClubRankCredits[i]) rank = i + 1;
            return rank;
        }

        public static bool ExamOpen(SaveData save)
        {
            if (save.AcademyCourses.Count < CoursesPerSemester) return false;
            foreach (var c in save.AcademyCourses) if (SemesterGrade(save, c) == 0) return false;
            return true;
        }

        // ------------------------------------------------------------------ 수강신청 and 동호회

        /// <summary>Signs up for this 학기's courses (once a 학기, exactly three). Returns false when not allowed.</summary>
        public static bool SignUp(SaveData save, IList<string> courseIds)
        {
            if (!Unlocked(save) || save.AcademyCourses.Count > 0 || courseIds == null || courseIds.Count != CoursesPerSemester) return false;
            var picked = new List<string>();
            foreach (var id in courseIds) if (FindCourse(id) != null && !picked.Contains(id)) picked.Add(id);
            if (picked.Count != CoursesPerSemester) return false;
            save.AcademyCourses.AddRange(picked);
            return true;
        }

        public static bool CanChangeClub(SaveData save) { return string.IsNullOrEmpty(save.AcademyClub) || save.AcademyClubSemester < save.AcademySemester; }

        /// <summary>Joins a 동호회 (the first time any time, afterwards once per 학기).</summary>
        public static bool JoinClub(SaveData save, string clubId)
        {
            if (!Unlocked(save) || FindClub(clubId) == null || save.AcademyClub == clubId || !CanChangeClub(save)) return false;
            save.AcademyClub = clubId;
            save.AcademyClubSemester = save.AcademySemester;
            return true;
        }

        // ------------------------------------------------------------------ bonuses

        /// <summary>The protagonist's lasting 학관 bonuses: the best grade of every course and the club perk (stats part).</summary>
        public static CombatStats Apply(CombatStats stats, SaveData save)
        {
            if (save == null) return stats;
            foreach (var c in Courses)
            {
                int g = BestGrade(save, c.Id);
                if (g <= 0) continue;
                float b = c.Bonus[g - 1];
                switch (c.Id)
                {
                    case "sword": stats.Attack = (int)Math.Round(stats.Attack * (1f + b)); break;
                    case "guard": stats.Defense = (int)Math.Round(stats.Defense * (1f + b)); break;
                    case "harmony": stats.CritDamage += b; break;
                    case "qinggong": stats.Speed += (int)b; break;
                    case "hidden": stats.CritRate += b; break;
                    case "theory": stats.MaxHp = (int)Math.Round(stats.MaxHp * (1f + b)); break;
                }
            }
            var club = FindClub(save.AcademyClub);
            if (club != null && club.Id == "geomhon") stats.CritRate += club.Perk[ClubRank(save) - 1];
            return stats;
        }

        /// <summary>The in-fight part of the club perk (기세) on the protagonist's combatant.</summary>
        public static void ApplyPerks(Combatant hero, SaveData save)
        {
            if (hero == null || save == null) return;
            var club = FindClub(save.AcademyClub);
            if (club == null) return;
            int v = (int)club.Perk[ClubRank(save) - 1];
            if (club.Id == "dobaek") hero.StartSpirit += v;
            else if (club.Id == "aesojeo") hero.SpiritTakenBonus += v;
        }

        /// <summary>"공격 +4%" / "속도 +4" for a course at a grade.</summary>
        public static string BonusText(CourseData c, int grade)
        {
            if (grade <= 0) return c.BonusName + " —";
            float b = c.Bonus[grade - 1];
            return c.BonusName + " +" + (c.PercentBonus ? Math.Round(b * 100f) + "%" : ((int)b).ToString());
        }

        public static string PerkText(ClubData c, int rank)
        {
            float v = c.Perk[Math.Max(1, Math.Min(3, rank)) - 1];
            return c.PerkName + " +" + (c.PercentPerk ? Math.Round(v * 100f) + "%" : ((int)v).ToString());
        }

        // ------------------------------------------------------------------ fights

        /// <summary>Enemies of a 학관 fight: the reference stage's ordinary scaling on 일반, gear included.</summary>
        public static EnemyData ScaleEnemy(EnemyData enemy, int stage)
        {
            if (enemy.Id.StartsWith("academy_")) return enemy;   // a 단 대항전 body is registered already scaled
            return StageStars.StoryEnemy(enemy, stage, 0, null);
        }

        /// <summary>Ordinary fights of a stage a lesson can borrow: no event, rule, showdown or boss, no 시험 wall.</summary>
        private static List<int> OrdinaryFights(StageData st, bool needCharge)
        {
            var list = new List<int>();
            if (st == null) return list;
            for (int f = 0; f < st.Encounters.Length; f++)
            {
                if (st.EventFor(f) != null || st.TrialFor(f) != null || Showdowns.Is(st, f) || st.Encounters[f].Length == 0) continue;
                bool ok = true, charge = false;
                foreach (var id in st.Encounters[f])
                {
                    var e = EarlyGameContent.FindEnemy(id);
                    if (e == null || e.IsBoss || e.HpMul > 10f || e.Moves == null) { ok = false; break; }
                    foreach (var m in e.Moves) if (m.Charge) charge = true;
                }
                if (ok && (!needCharge || charge)) list.Add(f);
            }
            return list;
        }

        /// <summary>The (stage, fight) a course borrows this 학기: the newest stage at or below the reference that has a fitting fight.</summary>
        public static void LessonFight(SaveData save, CourseData course, out int stage, out int fight)
        {
            bool needCharge = course.Rule == TrialKind.Lifeline;
            int r = ReferenceStage(save);
            for (int s = r; s >= 1; s--)
            {
                var st = EarlyGameContent.FindStage(s);
                var fights = OrdinaryFights(st, needCharge);
                if (fights.Count == 0) continue;
                int seed = StableHash(course.Id) + save.AcademySemester * 7;
                stage = s; fight = fights[seed % fights.Count];
                return;
            }
            stage = r; fight = 0;
        }

        /// <summary>The rule of a lesson (its limit comes from the borrowed fight's 신속 limit).</summary>
        public static TrialRule LessonRule(CourseData course, int stage, int fight)
        {
            int fast = StageStars.FastLimit(stage, fight);
            switch (course.Id)
            {
                case "sword": return TrialRule.Deadline(course.Name, fast + 3 + "행동 안에 모두 쓰러뜨리세요.", fast + 3);
                case "hidden": return TrialRule.Deadline(course.Name, fast + 1 + "행동 안에 모두 쓰러뜨리세요.", fast + 1);
                case "theory": return TrialRule.Deadline(course.Name, fast + 6 + "행동 안에 모두 쓰러뜨리세요.", fast + 6);
                case "harmony": return TrialRule.Harmony(course.Name, "끊김 없이 모두가 공격하면 다음 일격이 강해집니다. " + (fast + 3) + "행동 안에 이기세요.", fast + 3);
                case "guard": return TrialRule.Lifeline(course.Name, "큰 공격을 두 번 맞으면 낙제입니다.", 2);
                default: return TrialRule.Survive(course.Name, "적의 행동 " + SurviveActions + "번을 버티세요.", SurviveActions);
            }
        }

        public const int SurviveActions = 8;

        private static int StableHash(string s) { int h = 0; foreach (char ch in s) h = (h * 31 + ch) & 0xffff; return h; }

        /// <summary>What a fight scored: 1 point for passing, +1 if nobody fell, +1 for the course's own goal.</summary>
        public static int Grade(CourseData course, int stage, int fight, int actions, bool fell, int ropesCut, double partyHpPct)
        {
            int fast = StageStars.FastLimit(stage, fight);
            bool goal;
            switch (course.Id)
            {
                case "sword": case "harmony": goal = actions <= fast; break;
                case "hidden": goal = actions <= Math.Max(1, fast - 2); break;
                case "guard": goal = ropesCut == 0; break;
                case "theory": goal = partyHpPct >= 0.6; break;
                default: goal = partyHpPct >= 0.5; break;
            }
            return 1 + (fell ? 0 : 1) + (goal ? 1 : 0);
        }

        private static StageData BaseStage(int stage, string name, string academyId)
        {
            var reference = EarlyGameContent.FindStage(stage);
            return new StageData
            {
                Number = stage, Name = name, IsQuest = true, QuestId = academyId, AcademyId = academyId,
                RecommendedLevel = reference != null ? reference.RecommendedLevel : 1,
                Outro = new[] { "천무학관으로 돌아왔다." },
            };
        }

        /// <summary>The one-fight stage of a lesson. A course already passed this 학기 can be retaken for a better grade, without rewards.</summary>
        public static StageData LessonStage(SaveData save, string courseId)
        {
            var course = FindCourse(courseId);
            if (course == null) return null;
            int stage, fight;
            LessonFight(save, course, out stage, out fight);
            var reference = EarlyGameContent.FindStage(stage);
            var st = BaseStage(stage, course.Name + " 수업", "lesson:" + course.Id);
            var rule = LessonRule(course, stage, fight);
            st.Intro = new[] { course.Teacher + "의 " + course.Name + " 수업입니다.", course.Brief, rule.Brief, "갑 조건: 승리 · 전원 생존 · " + course.GoalText };
            st.Encounters = new[] { reference.Encounters[fight] };
            st.Trials = new[] { rule };
            st.DooTotal = Math.Max(500, reference.DooTotal / Math.Max(1, reference.Encounters.Length));
            st.NoReward = SemesterGrade(save, course.Id) > 0;
            return st;
        }

        /// <summary>The 연말 평가: the three courses of the 학기 in a row (rules and fights as in their lessons).</summary>
        public static StageData ExamStage(SaveData save)
        {
            if (!ExamOpen(save)) return null;
            int r = ReferenceStage(save);
            var st = BaseStage(r, "연말 평가", "exam");
            var encounters = new List<string[]>();
            var rules = new List<TrialRule>();
            foreach (var id in save.AcademyCourses)
            {
                var course = FindCourse(id);
                int stage, fight;
                LessonFight(save, course, out stage, out fight);
                encounters.Add(EarlyGameContent.FindStage(stage).Encounters[fight]);
                rules.Add(LessonRule(course, stage, fight));
            }
            st.Intro = new[] { "한 학기를 마무리하는 연말 평가입니다.", "이번 학기에 들은 세 과목의 시험을 이어서 치릅니다." };
            st.Encounters = encounters.ToArray();
            st.Trials = rules.ToArray();
            var reference = EarlyGameContent.FindStage(r);
            st.DooTotal = Math.Max(1500, reference != null ? reference.DooTotal : 1500);
            return st;
        }

        /// <summary>A 단 대항전 match: 선봉·중견·대장, best of three. A house beaten this 학기 can be fought again without rewards.</summary>
        public static StageData LeagueStage(SaveData save, string houseId)
        {
            var house = FindHouse(houseId);
            if (house == null) return null;
            int r = ReferenceStage(save);
            string member = RegisterHouseEnemy(house, false, r), leader = RegisterHouseEnemy(house, true, r);
            var st = BaseStage(r, "단 대항전 · " + house.Name, "league:" + house.Id);
            st.Intro = new[] { "주작단과 " + house.Name + "의 대항전입니다.", house.Brief, "선봉 · 중견 · 대장 세 판 가운데 두 판을 이기면 승리합니다." };
            st.Encounters = new[] { new[] { member, member }, new[] { member, leader }, new[] { leader, member } };
            st.Trials = new TrialRule[] { null, null, null };
            st.BestOfThree = true;
            var reference = EarlyGameContent.FindStage(r);
            st.DooTotal = Math.Max(1000, reference != null ? reference.DooTotal * 2 / 3 : 1000);
            st.NoReward = save.AcademyLeague.Contains(house.Id);
            return st;
        }

        /// <summary>
        /// The house's 관도 (or its 대표) at the reference stage: a copy of that stage's middle ordinary enemy as it fights there (scaling and gear
        /// included), with the house's name and moves, its HP shared out so a bout of <see cref="BoutSize"/> holds what an ordinary fight holds.
        /// </summary>
        private static string RegisterHouseEnemy(HouseData house, bool leader, int stage)
        {
            var pool = new List<KeyValuePair<int, EnemyData>>();
            int fights = 0, bodies = 0;
            for (int s = stage; s >= 1 && pool.Count == 0; s--)
            {
                var st = EarlyGameContent.FindStage(s);
                if (st == null) continue;
                foreach (var f in OrdinaryFights(st, false))
                {
                    fights++; bodies += st.Encounters[f].Length;
                    foreach (var id in st.Encounters[f])
                    {
                        var e = st.EnemyFor(id, null);
                        if (e != null) pool.Add(new KeyValuePair<int, EnemyData>(e.CreateCombatant(id).Stats.MaxHp, e));
                    }
                }
            }
            string houseId = "academy_" + house.Id + (leader ? "_leader" : "_member");
            EnemyData data;
            if (pool.Count == 0)
                data = new EnemyData { Level = 20, HpMul = 1.5f, AtkMul = 3f };
            else
            {
                pool.Sort((a, b) => a.Key.CompareTo(b.Key));
                float share = Math.Max(.5f, Math.Min(2f, bodies / (float)fights / BoutSize));
                data = pool[pool.Count / 2].Value.Scaled(share * (leader ? 1.6f : 1f), (float)Math.Sqrt(share) * (leader ? 1.1f : 1f));
            }
            data.Id = houseId;
            data.Name = house.Name + (leader ? " 대표" : " 관도");
            data.ArtId = null;
            data.Moves = leader ? house.LeaderMoves : house.MemberMoves;
            data.IsBoss = false; data.IsMinion = false; data.StoryOnly = true;
            data.RetreatHpPct = 0f; data.HoldsAtOneHp = false; data.RetreatLines = null; data.FixedHp = 0; data.RoleOf = null;
            EarlyGameContent.RegisterDynamicEnemy(data);
            return data.Id;
        }

        public const int BoutSize = 2;   // bodies in each 대항전 bout


        // ------------------------------------------------------------------ results

        /// <summary>Records a passed lesson (keeps the best grade of the 학기 and ever, adds the 학점 gained). Returns the note lines.</summary>
        public static List<string> CompleteLesson(SaveData save, string courseId, int grade)
        {
            var notes = new List<string>();
            var course = FindCourse(courseId);
            if (course == null || grade <= 0) return notes;
            int before = SemesterGrade(save, courseId);
            if (grade > before)
            {
                SetGrade(save.AcademyGrades, courseId, grade);
                save.AcademyCredits += grade - before;
                notes.Add("학점 +" + (grade - before) + " (총 " + save.AcademyCredits + ")");
                if (before == 0)
                {
                    int rose = Factions.Gain(save, "academy", LessonReputation);
                    notes.Add("천무학관 평판 +" + LessonReputation + (rose >= 0 ? " (" + Factions.TierNames[rose] + "!)" : ""));
                }
            }
            int best = BestGrade(save, courseId);
            if (grade > best)
            {
                SetGrade(save.AcademyBest, courseId, grade);
                notes.Add("영구 보너스: " + BonusText(course, grade));
            }
            return notes;
        }

        /// <summary>Records a won 단 대항전 match (the first win of the 학기 gives 평판).</summary>
        public static List<string> CompleteLeague(SaveData save, string houseId)
        {
            var notes = new List<string>();
            if (FindHouse(houseId) == null || save.AcademyLeague.Contains(houseId)) return notes;
            save.AcademyLeague.Add(houseId);
            int rose = Factions.Gain(save, "academy", LeagueReputation);
            notes.Add("천무학관 평판 +" + LeagueReputation + (rose >= 0 ? " (" + Factions.TierNames[rose] + "!)" : ""));
            if (save.AcademyLeague.Count == Houses.Length)
            {
                Inventory.Add(save, InnerArts.StoneId, 1);
                notes.Add("대항전 전승! 심득석 1개");
            }
            return notes;
        }

        /// <summary>Closes the 학기 after a passed 연말 평가: 학점 by the exam grade, rewards, then a fresh 학기 (new sign-up, league reset).</summary>
        public static List<string> CompleteExam(SaveData save, int grade)
        {
            var notes = new List<string>();
            if (!ExamOpen(save)) return notes;
            grade = Math.Max(1, Math.Min(3, grade));
            save.AcademyCredits += grade;
            notes.Add("연말 평가 " + GradeNames[grade] + " · 학점 +" + grade + " (총 " + save.AcademyCredits + ")");
            int rose = Factions.Gain(save, "academy", ExamReputation);
            notes.Add("천무학관 평판 +" + ExamReputation + (rose >= 0 ? " (" + Factions.TierNames[rose] + "!)" : ""));
            Inventory.Add(save, InnerArts.StoneId, 1);
            notes.Add("심득석 1개");
            save.AcademySemester++;
            save.AcademyCourses.Clear();
            save.AcademyGrades.Clear();
            save.AcademyLeague.Clear();
            notes.Add(save.AcademySemester + "학기가 시작되었습니다. 새로 수강신청을 해 주세요.");
            return notes;
        }

        /// <summary>The course a lesson / exam fight belongs to (null for the league).</summary>
        public static CourseData CourseOf(SaveData save, StageData st, int fight)
        {
            if (st == null || st.AcademyId == null) return null;
            if (st.AcademyId.StartsWith("lesson:")) return FindCourse(st.AcademyId.Substring(7));
            if (st.AcademyId == "exam" && fight >= 0 && fight < save.AcademyCourses.Count) return FindCourse(save.AcademyCourses[fight]);
            return null;
        }

        /// <summary>The borrowed (stage, fight) behind a lesson / exam fight, for its grade (its 신속 limit).</summary>
        public static void SourceOf(SaveData save, CourseData course, out int stage, out int fight) { LessonFight(save, course, out stage, out fight); }
    }
}
