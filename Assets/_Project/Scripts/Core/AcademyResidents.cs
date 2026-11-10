using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    public sealed class AcademyResident
    {
        public readonly string Id, Name, Art, ActionLabel;
        public readonly float X, Y;
        public readonly int Facility;
        internal AcademyResident(string id, string name, string art, float x, float y, int facility, string actionLabel)
        { Id = id; Name = name; Art = art; X = x; Y = y; Facility = facility; ActionLabel = actionLabel; }
    }

    /// <summary>Unnamed interface roles; dialogue reads existing rules and never changes the save.</summary>
    public static class AcademyResidents
    {
        public static readonly IReadOnlyList<AcademyResident> All = Array.AsReadOnly(new[]
        {
            new AcademyResident("courses", AcademyGuide.Name, "academy_guide", AcademyGuide.X, AcademyGuide.Y, 0, "수강 · 수업 보기"),
            new AcademyResident("clubs", "동호회 안내원", "academy_club_guide", 1500f, 1000f, 1, "동호회 보기"),
            new AcademyResident("league", "대항전 교관", "academy_league_instructor", 2170f, 1000f, 2, "단 대항전 보기")
        });

        public static AcademyResident Nearby(float x, float y)
        {
            AcademyResident nearest = null;
            float best = AcademyGuide.TalkRange * AcademyGuide.TalkRange;
            foreach (var resident in All)
            {
                float dx = x - resident.X, dy = y - resident.Y, distance = dx * dx + dy * dy;
                if (distance <= best) { nearest = resident; best = distance; }
            }
            return nearest;
        }

        public static bool Blocks(float x, float y)
        {
            float radius = AcademyWalk.Radius + AcademyGuide.Radius;
            foreach (var resident in All)
            {
                float dx = x - resident.X, dy = y - resident.Y;
                if (dx * dx + dy * dy < radius * radius) return true;
            }
            return false;
        }

        public static string Greeting(AcademyResident resident, SaveData save)
        {
            if (resident == null) return "가까운 안내원에게 말을 걸어 주세요.";
            if (resident.Facility == 0) return AcademyGuide.Greeting(save);
            if (!Academy.Unlocked(save)) return "학관 생활이 시작되면 안내해 드리겠습니다.";
            if (resident.Facility == 1)
            {
                var club = Academy.FindClub(save.AcademyClub);
                if (club == null) return "동호회는 한 곳에 가입하실 수 있습니다. 활동과 효과를 살펴보고 원하는 동호회를 선택해 주세요.";
                return "현재 " + club.Name + "에 가입되어 있습니다. "
                    + (Academy.CanChangeClub(save) ? "이번 학기에는 동호회를 변경하실 수 있습니다." : "이번 학기에는 이미 가입하거나 변경하셨습니다. 다음 학기에 다시 변경하실 수 있습니다.");
            }
            if (!Academy.SemesterOpen(save)) return Academy.SemesterLockText(save) + " 지금은 보상 없는 연습으로 겨루실 수 있습니다.";
            int wins = 0;
            foreach (var house in Academy.Houses) if (save.AcademyLeague.Contains(house.Id)) wins++;
            return wins == Academy.Houses.Length
                ? "이번 학기의 모든 단을 이기셨습니다. 다시 겨루기는 가능하지만 추가 보상은 없습니다."
                : "이번 학기에 " + wins + " / " + Academy.Houses.Length + "단을 이기셨습니다. 세 판 가운데 두 판을 이기면 승리합니다. 상대 단과 보상을 확인해 주세요.";
        }
    }
}
