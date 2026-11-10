using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// 천무학관 overlay (강호 ▸ 도전 ▸ 천무학관): pages 수강신청 (sign up for three courses, take their lessons and the 연말 평가),
    /// 동호회 (join one) and 단 대항전 (best of three against 청룡단·백호단·현무단). Rules live in Core <see cref="Academy"/>.
    /// </summary>
    public sealed class AcademyPanel : OverlayPanel
    {
        private enum Page { Courses, Clubs, League }

        private readonly Action<StageData> _start;
        private readonly AcademyWalk _walk = new AcademyWalk();
        private Page _page = Page.Courses;
        private readonly List<string> _picked = new List<string>();

        public AcademyPanel(RectTransform root, Action onChange, Action<StageData> start) : base(root, onChange) { _start = start; }

        private static Text T(string name, Transform parent, float x0, float y0, float x1, float y1, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var t = UiFactory.Body(UiFactory.Label(name, parent, new Vector2(x0, y0), new Vector2(x1, y1), text, size, color, anchor, FontStyle.Normal), 1.15f);
            t.supportRichText = true;
            return t;
        }

        public void Open(string toast = null)
        {
            if (!Academy.Unlocked(GameManager.Instance.Save)) { OpenMenu(toast); return; }
            Begin();
            UiFactory.Label("Title", Root, new Vector2(.15f, .90f), new Vector2(.95f, .99f), "천무학관 · 마당", 40, UiFactory.Gold);
            var host = UiFactory.Rect("Campus", Root, Vector2.zero, Vector2.one);
            host.gameObject.AddComponent<AcademyCampus>().Build(host, _walk, page => { _page = (Page)page; OpenMenu(); });
            UiFactory.HelpButton(Root, () => { if (HelpPanel.Instance != null) HelpPanel.Instance.Show("academy"); });
            AddClose();
            // The first page of the topic explains walking, so it opens on the first visit to the courtyard.
            if (!Tutorial.IsDone(GameManager.Instance.Save, "academy") && HelpPanel.Instance != null) HelpPanel.Instance.Show("academy", true);
        }

        /// <summary>After an academy fight: back to the menu page the fight was started from (its back arrow leads to the courtyard).</summary>
        public void OpenAfterBattle()
        {
            OpenMenu();
        }

        private void OpenMenu(string toast = null)
        {
            var save = GameManager.Instance.Save;
            Begin();
            UiFactory.Label("Title", Root, new Vector2(.05f, .92f), new Vector2(.95f, .99f), "천무학관", 44, UiFactory.Gold);
            if (!Academy.Unlocked(save))
            {
                T("Locked", Root, .05f, .45f, .95f, .75f, "스테이지 " + Academy.UnlockStage + "를 돌파하면 학관 생활이 시작됩니다.", 34, UiFactory.PaperText, TextAnchor.MiddleCenter);
                AddClose();
                return;
            }
            var club = Academy.FindClub(save.AcademyClub);
            T("Summary", Root, .05f, .865f, .95f, .92f,
                save.AcademySemester + "학기  ·  학점 " + save.AcademyCredits + "  ·  동호회 " + (club != null ? club.Name + " " + Academy.ClubRank(save) + "단계" : "없음"),
                28, UiFactory.PaperText, TextAnchor.MiddleCenter);

            string[] names = { "수강신청", "동호회", "단 대항전" };
            for (int i = 0; i < names.Length; i++)
            {
                var page = (Page)i;
                UiFactory.Button("Page" + i, Root, new Vector2(.05f + i * .305f, .79f), new Vector2(.34f + i * .305f, .855f), names[i],
                    page == _page ? UiFactory.Red : UiFactory.InkSoft, () => { _page = page; OpenMenu(); }, 30);
            }
            T("Toast", Root, .04f, .015f, .96f, .075f, toast ?? Hint(save), 26, toast != null ? UiFactory.Gold : UiFactory.Dim, TextAnchor.MiddleCenter);

            var content = CodexPanel.Scroller(Root, .08f, .78f);
            switch (_page)
            {
                case Page.Courses: DrawCourses(save, content); break;
                case Page.Clubs: DrawClubs(save, content); break;
                default: DrawLeague(save, content); break;
            }
            UiFactory.HelpButton(Root, () => { if (HelpPanel.Instance != null) HelpPanel.Instance.Show("academy"); });
            UiFactory.BackCorner(Root, () => Open());
            if (!Tutorial.IsDone(save, "academy") && HelpPanel.Instance != null) HelpPanel.Instance.Show("academy", true);
        }

        private string Hint(SaveData save)
        {
            switch (_page)
            {
                case Page.Courses:
                    if (save.AcademyCourses.Count == 0 && !Academy.SemesterOpen(save)) return Academy.SemesterLockText(save);
                    return save.AcademyCourses.Count == 0 ? "이번 학기에 들을 과목 " + Academy.CoursesPerSemester + "개를 골라 주세요."
                        : Academy.ExamOpen(save) ? "세 과목을 모두 이수했습니다. 연말 평가를 치르면 다음 학기로 넘어갑니다."
                        : "과목마다 한 번 이수하면 보상을 받습니다. 다시 치르면 성적만 올릴 수 있습니다.";
                case Page.Clubs:
                    return "동호회는 한 곳만 가입할 수 있고, 한 학기에 한 번 바꿀 수 있습니다.";
                default:
                    if (!Academy.SemesterOpen(save)) return Academy.SemesterLockText(save) + " 그때까지 대항전은 보상이 없습니다.";
                    return "학기마다 세 단을 처음 이길 때 보상을 받습니다. 세 판 가운데 두 판을 이기면 승리합니다.";
            }
        }

        // ------------------------------------------------------------------ 수강신청

        private void DrawCourses(SaveData save, RectTransform content)
        {
            var grid = new ReadingGrid(content, 3, 330f);
            bool signedUp = save.AcademyCourses.Count > 0;
            if (!signedUp && !Academy.SemesterOpen(save))
            {
                grid.Heading(Academy.SemesterLockText(save));
                foreach (var c in Academy.Courses) CourseCard(save, grid, c, false);
            }
            else if (!signedUp)
            {
                grid.Heading("수강신청 (" + _picked.Count + " / " + Academy.CoursesPerSemester + ")");
                foreach (var c in Academy.Courses) CourseCard(save, grid, c, false);
                var row = grid.Add("SignUp");
                bool ready = _picked.Count == Academy.CoursesPerSemester;
                var btn = UiFactory.Button("SignUpButton", row, new Vector2(.08f, .3f), new Vector2(.92f, .7f), ready ? "수강신청 완료" : "과목을 " + Academy.CoursesPerSemester + "개 골라 주세요",
                    ready ? UiFactory.Red : UiFactory.InkSoft, SignUp, 30);
                btn.interactable = ready;
            }
            else
            {
                grid.Heading("이번 학기 수업");
                foreach (var id in save.AcademyCourses) CourseCard(save, grid, Academy.FindCourse(id), true);
                grid.Heading("연말 평가");
                var exam = grid.Add("Exam");
                bool open = Academy.ExamOpen(save);
                T("Name", exam, .05f, .72f, .95f, .95f, "연말 평가", 32, UiFactory.Gold);
                T("Info", exam, .05f, .32f, .95f, .72f, open ? "세 과목의 시험을 이어서 치릅니다.\n통과하면 학점 · 평판 · 심득석을 얻고 다음 학기가 시작됩니다." : "이번 학기의 세 과목을 모두 이수하면 열립니다.", 24, UiFactory.PaperText);
                var go = UiFactory.Button("Go", exam, new Vector2(.08f, .05f), new Vector2(.92f, .27f), open ? "응시" : "잠김", open ? UiFactory.Red : UiFactory.InkSoft, () => StartFight(Academy.ExamStage(save)), 28);
                go.interactable = open;
            }

            grid.Heading("성적표 (최고 성적 · 영구 보너스)");
            foreach (var c in Academy.Courses)
            {
                var card = grid.Add("Record_" + c.Id);
                int best = Academy.BestGrade(save, c.Id);
                T("Name", card, .05f, .60f, .95f, .95f, c.Name + "  " + (best > 0 ? "<color=#E8C46A>" + Academy.GradeNames[best] + "</color>" : "미이수"), 30, UiFactory.PaperText);
                T("Bonus", card, .05f, .10f, .95f, .58f, "현재 " + Academy.BonusText(c, best) + "\n병 · 을 · 갑: " + Academy.BonusText(c, 1) + " · " + Academy.BonusText(c, 2) + " · " + Academy.BonusText(c, 3), 24, UiFactory.Dim);
            }
        }

        private void CourseCard(SaveData save, ReadingGrid grid, CourseData c, bool signedUp)
        {
            if (c == null) return;
            var card = grid.Add("Course_" + c.Id);
            int now = Academy.SemesterGrade(save, c.Id), best = Academy.BestGrade(save, c.Id);
            bool picked = _picked.Contains(c.Id);
            if (!signedUp && picked) UiFactory.Frame(card, UiFactory.Gold, 4f);
            T("Name", card, .05f, .80f, .95f, .97f, c.Name + "  <size=22>· " + c.Teacher + "</size>", 30, picked || now > 0 ? UiFactory.Gold : UiFactory.PaperText);
            T("Brief", card, .05f, .44f, .95f, .80f, c.Brief, 23, UiFactory.PaperText);
            T("Goal", card, .05f, .30f, .95f, .44f, "갑 조건: 승리 · 전원 생존 · " + c.GoalText, 21, UiFactory.Dim);
            T("Grade", card, .05f, .18f, .95f, .30f,
                (signedUp ? "이번 학기 " + (now > 0 ? Academy.GradeNames[now] : "미이수") + "  ·  " : "") + "최고 " + (best > 0 ? Academy.GradeNames[best] : "—") + "  ·  " + Academy.BonusText(c, Math.Max(1, best)), 22, UiFactory.Dim);
            string label;
            UnityEngine.Events.UnityAction action;
            if (!signedUp)
            {
                label = picked ? "선택 해제" : "선택";
                action = () => { if (!_picked.Remove(c.Id) && _picked.Count < Academy.CoursesPerSemester) _picked.Add(c.Id); OpenMenu(); };
            }
            else
            {
                label = now > 0 ? "다시 치르기 (보상 없음)" : "수업 듣기";
                action = () => StartFight(Academy.LessonStage(save, c.Id));
            }
            UiFactory.Button("Act", card, new Vector2(.06f, .02f), new Vector2(.94f, .16f), label, signedUp && now == 0 || picked ? UiFactory.Red : UiFactory.InkSoft, action, 26);
        }

        private void SignUp()
        {
            var save = GameManager.Instance.Save;
            if (!Academy.SignUp(save, _picked)) { OpenMenu("수강신청을 할 수 없습니다."); return; }
            _picked.Clear();
            GameManager.Instance.SaveNow();
            SoundManager.Sfx("levelup");
            OpenMenu("수강신청을 마쳤습니다. 과목을 골라 수업을 들어 주세요.");
        }

        // ------------------------------------------------------------------ 동호회

        private void DrawClubs(SaveData save, RectTransform content)
        {
            var grid = new ReadingGrid(content, 3, 360f);
            int rank = Academy.ClubRank(save);
            grid.Heading("동호회 단계는 학점으로 오릅니다 (2단계 " + Academy.ClubRankCredits[1] + " · 3단계 " + Academy.ClubRankCredits[2] + "학점)");
            foreach (var c in Academy.Clubs)
            {
                var card = grid.Add("Club_" + c.Id);
                bool joined = save.AcademyClub == c.Id;
                if (joined) UiFactory.Frame(card, UiFactory.Gold, 4f);
                T("Name", card, .05f, .80f, .95f, .97f, c.Name + (joined ? "  <size=22>· 가입 중</size>" : ""), 32, joined ? UiFactory.Gold : UiFactory.PaperText);
                T("Brief", card, .05f, .50f, .95f, .80f, c.Brief, 24, UiFactory.PaperText);
                T("Perk", card, .05f, .22f, .95f, .50f, "효과 (" + rank + "단계): " + Academy.PerkText(c, rank) + "\n1 · 2 · 3단계: " + Academy.PerkText(c, 1) + " · " + Academy.PerkText(c, 2) + " · " + Academy.PerkText(c, 3), 23, UiFactory.Dim);
                bool can = !joined && Academy.CanChangeClub(save);
                var btn = UiFactory.Button("Join", card, new Vector2(.06f, .03f), new Vector2(.94f, .19f), joined ? "가입 중" : can ? "가입" : "이번 학기에는 바꿀 수 없음",
                    can ? UiFactory.Red : UiFactory.InkSoft, () => Join(c.Id), 26);
                btn.interactable = can;
            }
        }

        private void Join(string clubId)
        {
            var save = GameManager.Instance.Save;
            if (!Academy.JoinClub(save, clubId)) { OpenMenu("지금은 동호회를 바꿀 수 없습니다."); return; }
            GameManager.Instance.SaveNow();
            SoundManager.Sfx("levelup");
            OpenMenu(Academy.FindClub(clubId).Name + "에 가입했습니다.");
        }

        // ------------------------------------------------------------------ 단 대항전

        private void DrawLeague(SaveData save, RectTransform content)
        {
            var grid = new ReadingGrid(content, 3, 330f);
            grid.Heading("주작단의 대항전 (이번 학기 " + save.AcademyLeague.Count + " / " + Academy.Houses.Length + "승 · 전승 시 심득석)");
            foreach (var h in Academy.Houses)
            {
                var card = grid.Add("House_" + h.Id);
                bool won = save.AcademyLeague.Contains(h.Id) || !Academy.SemesterOpen(save);
                T("Name", card, .05f, .80f, .95f, .97f, h.Name + "  <size=22>· " + h.Style + "</size>", 32, won ? UiFactory.Gold : UiFactory.PaperText);
                T("Brief", card, .05f, .48f, .95f, .80f, h.Brief + "\n선봉 · 중견 · 대장 세 판", 24, UiFactory.PaperText);
                T("State", card, .05f, .26f, .95f, .48f, !Academy.SemesterOpen(save) ? "학기가 열리면 보상을 받습니다 (지금은 연습)" : won ? "이번 학기 승리 (다시 겨루면 보상 없음)" : "이번 학기 첫 승리 보상: 경험 · 은자 · 천무학관 평판 +" + Academy.LeagueReputation, 22, won ? UiFactory.Gold : UiFactory.Dim);
                UiFactory.Button("Go", card, new Vector2(.06f, .03f), new Vector2(.94f, .21f), won ? "다시 겨루기" : "도전", won ? UiFactory.InkSoft : UiFactory.Red,
                    () => StartFight(Academy.LeagueStage(save, h.Id)), 28);
            }
        }

        private void StartFight(StageData st)
        {
            if (st == null) return;
            _start(st);
        }
    }
}
