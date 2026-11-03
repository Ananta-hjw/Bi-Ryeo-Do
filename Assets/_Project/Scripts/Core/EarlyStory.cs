using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>The first sixty-six chapters share one detailed timeline between battle and the record.</summary>
    public static class EarlyStory
    {
        public static bool Supports(StageData stage)
        {
            return stage != null && !stage.IsQuest && !stage.IsTower && stage.Number >= 1 && stage.Number <= 66;
        }

        public static StoryScene Background(StageData stage)
        {
            if (!Supports(stage)) return null;
            return new StoryScene
            {
                Title = stage.Number == 1 ? "현재 — 열아홉 살의 일상" : "이야기 배경",
                Lines = StoryBriefings.Briefing(stage.Number),
                HeroArtKey = stage.Number == 3 ? null : stage.IntroHeroArtKey
            };
        }

        public static StoryScene[] Opening(StageData stage)
        {
            if (!Supports(stage)) return null;
            var scenes = new List<StoryScene>();
            if (stage.Number == 61)
            {
                var background = StoryBriefings.Briefing(61);
                scenes.Add(stage.Prologue[0]);
                scenes.Add(Segment("살아 있는 포로 — 다시 구할 가능성을 남기다", background, 0, 3));
                scenes.Add(Segment("남문 앞 — 흑견의 소리와 포승줄", stage.Prologue[1].Lines, 0, 5));
                scenes.Add(Segment("성문 밖으로 나갈 길", background, 3, 2));
                scenes.Add(stage.Prologue[2]);
                scenes.Add(Segment("다른 길의 동료들과 수색조의 목적", background, 6, 2));
                scenes.Add(new StoryScene { Title = "남문 돌파 — 나백천을 찾기 위한 후퇴", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 62)
            {
                var background = StoryBriefings.Briefing(62);
                scenes.Add(new StoryScene { Title = "별도 시점 — 검은 불꽃을 바라본 갈중하", Lines = stage.Prologue[2].Lines });
                scenes.Add(Segment("구출대가 모르는 잠적자의 결심", background, 6, 2));
                scenes.Add(new StoryScene { Title = "남문을 빠져나온 밤 — 알림 방울과 웃음", Lines = stage.Prologue[0].Lines });
                scenes.Add(Segment("수색조의 길 — 냄새를 바꾼 웃음", background, 0, 3));
                scenes.Add(new StoryScene { Title = "멈칫한 추격대의 틈", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 63)
            {
                var background = StoryBriefings.Briefing(63);
                scenes.Add(new StoryScene { Title = "교차 시점 — 같은 밤 신용산객잔의 비밀 통로", Lines = stage.Prologue[0].Lines });
                scenes.Add(Segment("가짜 문을 지나 — 출구의 무음탄주", background, 0, 3));
                scenes.Add(stage.Prologue[1]);
                scenes.Add(Segment("지휘 위임 — 아직 완성되지 않은 인정", background, 3, 3));
                scenes.Add(Segment("앞을 여는 사람과 뒤에 남을 사람", stage.Prologue[2].Lines, 1, 1));
                scenes.Add(Segment("미리 나눈 후위의 약속", background, 7, 1));
                scenes.Add(new StoryScene { Title = "네 역할을 엮는 마하령의 지휘", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 64)
            {
                scenes.Add(stage.Prologue[0]);
                scenes.Add(Segment("홀로 남은 나예린 — 보이는 창과 남은 한계", StoryBriefings.Briefing(64), 0, 3));
                return scenes.ToArray();
            }
            if (stage.Number == 65)
            {
                scenes.Add(new StoryScene { Title = "이튿날 새벽 — 수색조 앞에 펼쳐진 삼천의 포위", Lines = stage.Prologue[0].Lines });
                scenes.Add(Segment("세 문파 사이 — 정면 승부 대신 불신의 틈", StoryBriefings.Briefing(65), 0, 3));
                return scenes.ToArray();
            }
            if (stage.Number == 66)
            {
                var wound = stage.Prologue[0];
                var history = stage.Prologue[1];
                var background = StoryBriefings.Briefing(66);
                scenes.Add(Segment("대난원 — 안도 뒤에도 멎지 않는 피", wound.Lines, 0, 6));
                scenes.Add(Segment("상처에서 떠오른 기억", background, 0, 2));
                scenes.Add(Segment("회상 — 백이 년 전 천무봉에 남은 나백천", wound.Lines, 7, 2));
                scenes.Add(Segment("회상 — 인의 열쇠를 맡긴 형님들", wound.Lines, 6, 1));
                scenes.Add(Segment("회상 — 지켜야 할 열쇠", background, 2, 1));
                scenes.Add(Segment("회상 — 방패를 지난 일격과 보지 못한 싸움", history.Lines, 0, 1));
                scenes.Add(Segment("회상 — 환살 너머는 알 수 없었다", background, 3, 1));
                scenes.Add(Segment("회상 — 싸움 뒤, 지켜 낸 것의 의미", wound.Lines, 9, 2));
                scenes.Add(Segment("현재로 — 천붕금시를 지키는 맹주의 임무", history.Lines, 1, 1));
                scenes.Add(Segment(null, background, 4, 1));
                scenes.Add(Segment("대난원 — 천하군림궤의 내용과 개봉의 위험", StoryEscape.Episodes[6600][0].Lines, 0, 1));
                scenes.Add(Segment("군림삼보와 빈 신무림기", history.Lines, 2, 7));
                scenes.Add(Segment("권위의 증표와 청룡은장의 희생", background, 5, 2));
                scenes.Add(Segment("설명을 마친 뒤 — 나백천을 데리고 밖으로", history.Lines, 9, 1));
                scenes.Add(Segment(null, background, 7, 1));
                scenes.Add(Segment("교차 시점 — 구출대가 모르는 첫 자물쇠", stage.Prologue[2].Lines, 0, 2));
                scenes.Add(Segment("별도 장소 — 두 가면과 아직 남은 잠금", background, 8, 2));
                scenes.Add(Segment("대난원 밖으로 — 준비된 환영식", stage.Prologue[2].Lines, 2, 1));
                scenes.Add(Segment(null, background, 10, 1));
                scenes.Add(new StoryScene { Title = "현재의 구출대 — 다친 사람을 지킬 공방", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 56)
            {
                var departure = stage.Prologue[0];
                var background = StoryBriefings.Briefing(56);
                scenes.Add(Segment("제십삼 기숙사 — 구출 전서와 배의 준비", departure.Lines, 0, 8));
                scenes.Add(Segment("숙소에 남은 사람들의 책임", background, 0, 6));
                scenes.Add(Segment("안락휴 — 포위망을 재울 준비", departure.Lines, 8, 2));
                scenes.Add(Segment("당가 남매와 백무영의 몫", background, 6, 2));
                scenes.Add(Segment("포위 돌파를 앞두고", stage.Intro, 0, 2));
                return scenes.ToArray();
            }
            if (stage.Number == 57)
            {
                scenes.Add(Segment("출항 뒤 — 현천은린을 펼친 물길", stage.Prologue[0].Lines, 0, 3));
                scenes.Add(Segment("먼저 떠난 배를 따라", StoryBriefings.Briefing(57), 0, 5));
                scenes.Add(Segment("돌아오는 우산 위", stage.Intro, 0, 1));
                scenes.Add(Segment("배 위의 재회 — 끝나지 못한 우산의 물음", stage.Prologue[0].Lines, 3, 5));
                scenes.Add(Segment("돌아온 사람과 돌아오지 못할 사람", StoryBriefings.Briefing(57), 5, 3));
                return scenes.ToArray();
            }
            if (stage.Number == 58)
            {
                var background = StoryBriefings.Briefing(58);
                scenes.Add(new StoryScene { Title = "교차 시점 — 구출대가 모르는 폭우 속 나백천", Lines = stage.Prologue[0].Lines });
                scenes.Add(Segment("시점을 돌려 — 섬으로 돌아가는 배", background, 0, 1));
                scenes.Add(Segment("강호란도 — 상실을 안고 돌아온 일행", stage.Prologue[1].Lines, 0, 5));
                scenes.Add(Segment("자책하는 남궁상과 모녀의 재회", background, 1, 1));
                scenes.Add(Segment("예청의 부탁 — 예린의 아버지를 구하는 일", stage.Prologue[1].Lines, 5, 6));
                scenes.Add(Segment("값을 매기지 않은 이유", background, 2, 1));
                scenes.Add(Segment("구출의 조건과 예린 자신의 선택", stage.Prologue[2].Lines, 0, 4));
                scenes.Add(Segment("아직 남아 있는 승낙의 조건", background, 3, 1));
                scenes.Add(Segment("같은 무렵 — 효룡과 조부의 쌍도", stage.Prologue[2].Lines, 4, 10));
                scenes.Add(Segment("계승한 쌍붕도 중천", background, 4, 1));
                scenes.Add(Segment("출발 전 — 노학에게 남은 한 손", stage.Prologue[2].Lines, 14, 7));
                scenes.Add(Segment("역린검을 향한 결심", background, 5, 1));
                scenes.Add(Segment("아직 완성이 아닌 두 사람의 수련", background, 7, 1));
                scenes.Add(stage.Prologue[3]);
                scenes.Add(Segment("선발 뒤 — 돌아갈 사람과 남을 사람", background, 6, 1));
                scenes.Add(new StoryScene { Title = "강호란도 출항 — 절벽 밑을 떠나는 배", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 59)
            {
                scenes.Add(Segment("무한의 밤 — 객잔과 떨어진 조사조", StoryBriefings.Briefing(59), 0, 1));
                scenes.Add(stage.Prologue[0]);
                scenes.Add(stage.Prologue[1]);
                return scenes.ToArray();
            }
            if (stage.Number == 60)
            {
                scenes.Add(stage.Prologue[0]);
                scenes.Add(Segment("신용산객잔 앞 — 두 노사가 맡은 시간", StoryBriefings.Briefing(60), 0, 3));
                return scenes.ToArray();
            }
            if (stage.Number == 51)
            {
                var market = stage.Prologue[0];
                scenes.Add(Segment("남해도 — 문지기와 시장의 규칙", market.Lines, 0, 7));
                var background = StoryBriefings.Briefing(51);
                scenes.Add(Segment("통행료 뒤 — 상혼대의 시장", background, 0, 4));
                scenes.Add(Segment("강매에서 위협으로", market.Lines, 7, market.Lines.Length - 7));
                scenes.Add(Segment("삼대상 앞 — 목관을 찾는 두 사람", background, 4, background.Length - 4));
                scenes.Add(new StoryScene { Title = "첫 공방 — 한 사람의 틈을 열다", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 52)
            {
                var north = stage.Prologue[0];
                scenes.Add(Segment("북해도 길목 — 동행의 이유", north.Lines, 0, 3));
                return scenes.ToArray();
            }
            if (stage.Number == 53)
            {
                scenes.Add(stage.Prologue[0]);
                scenes.Add(Segment("빈 관 — 깊이를 본 효룡", StoryBriefings.Briefing(53), 0, 4));
                scenes.Add(Segment("암습 앞에 선 효룡", stage.Intro, 0, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 54)
            {
                var coffins = stage.Prologue[0];
                scenes.Add(Segment("남해도 — 목관 폭발과 산산의 중상", coffins.Lines, 0, 9));
                scenes.Add(Segment("같은 무렵 동해도 — 다른 관의 암기", coffins.Lines, 9, coffins.Lines.Length - 9));
                var background = StoryBriefings.Briefing(54);
                scenes.Add(Segment("남해도에서 — 부상자를 업고 의원을 향해", background, 0, 4));
                scenes.Add(stage.Prologue[1]);
                scenes.Add(Segment("합류한 동료들과 막힌 이송로", background, 4, background.Length - 4));
                scenes.Add(new StoryScene { Title = "같은 전선 — 부상자를 옮길 길", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 55)
            {
                scenes.Add(stage.Prologue[0]);
                scenes.Add(Segment("붉은 방 — 자신을 다시 세우고 남은 두 침", StoryBriefings.Briefing(55), 0, 4));
                scenes.Add(Segment("스스로 선택한 탈출", StoryBriefings.Briefing(55), 7, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 46)
            {
                scenes.Add(stage.Prologue[0]);
                scenes.Add(Segment("결승 전 — 죽이지 않겠다는 약속", StoryBriefings.Briefing(46), 0, 2));
                scenes.Add(Segment("결승의 목표 — 처형이 아닌 승부", StoryBriefings.Briefing(46), 7, 1));
                scenes.Add(stage.Prologue[1]);
                scenes.Add(new StoryScene { Title = "결승 — 진비뢰도 없는 연비", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 47)
            {
                scenes.Add(stage.Prologue[0]);
                var background = StoryBriefings.Briefing(47);
                scenes.Add(Segment("현재 — 암습 뒤 다시 모인 동료들", background, 0, 3));
                var abduction = stage.Prologue[1];
                scenes.Add(Segment("회상 — 결승 직전 대기실의 약속", abduction.Lines, 0, 4));
                scenes.Add(Segment("같은 회상 — 납치 당시의 저항과 흔적", abduction.Lines, 4, abduction.Lines.Length - 4));
                scenes.Add(new StoryScene { Title = "현재로 — 빈 집무실과 환기 통로", Lines = stage.Prologue[2].Lines });
                scenes.Add(Segment("현재 — 흔적분으로 좁히는 경로", background, 3, 2));
                scenes.Add(new StoryScene { Title = "수색 — 남은 흔적과 퇴로", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 48)
            {
                scenes.Add(stage.Prologue[0]);
                scenes.Add(Segment("출항 뒤 — 빌린 칼과 남은 사람들", StoryBriefings.Briefing(48), 0, 3));
                scenes.Add(Segment("마천각으로 — 대장들의 눈을 끌 준비", StoryBriefings.Briefing(48), 3, 1));
                scenes.Add(new StoryScene { Title = "고정 구출대 — 정문을 향해", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 49)
            {
                var memory = stage.Prologue[0];
                scenes.Add(Segment(memory.Title, memory.Lines, 0, 8));
                scenes.Add(Segment("구출대와 다른 장소의 현재 — 적포인의 독백", memory.Lines, 8, memory.Lines.Length - 8));
                scenes.Add(stage.Prologue[1]);
                var background = StoryBriefings.Briefing(49);
                scenes.Add(Segment("현재 서해도 — 다섯 주먹과 불완전한 몸", background, 0, 3));
                scenes.Add(Segment("규칙 속에서도 잊지 않을 목적", background, 4, 2));
                scenes.Add(Segment("첫 판 — 패배와 포기를 묻다", stage.Intro, 0, 2));
                return scenes.ToArray();
            }
            if (stage.Number == 50)
            {
                scenes.Add(stage.Prologue[0]);
                scenes.Add(Segment("같은 밤 동해도 — 동료와 미의 대전", StoryBriefings.Briefing(50), 0, 6));
                scenes.Add(Segment("현재 — 한낮의 유성을 보이다", stage.Intro, 0, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 41)
            {
                scenes.Add(stage.Prologue[0]);
                var background = StoryBriefings.Briefing(41);
                scenes.Add(Segment("밤 정원 — 피하지 않기로 한 만남", background, 0, 2));
                scenes.Add(stage.Prologue[1]);
                scenes.Add(Segment("현재 — 술창고의 백무후", background, 2, 2));
                scenes.Add(new StoryScene { Title = "백무후의 회상 — 술친구가 된 노인", Lines = stage.Prologue[2].Lines });
                scenes.Add(Segment("회상 — 기와 경공을 배운 세월", background, 4, 2));
                scenes.Add(new StoryScene { Title = "현재로 — 달의 이슬을 위한 공방", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 42)
            {
                scenes.Add(stage.Prologue[0]);
                scenes.Add(stage.Prologue[1]);
                scenes.Add(Segment("일주일 전 — 협상 뒤의 계승 과제", StoryBriefings.Briefing(42), 0, 3));
                scenes.Add(new StoryScene { Title = "협상 다음 날 — 경매와 특급 서찰", Lines = stage.Prologue[2].Lines });
                scenes.Add(new StoryScene { Title = "그 뒤 마천각 — 등록소에서 만난 영령", Lines = stage.Prologue[3].Lines });
                scenes.Add(Segment("등록소의 재회 뒤 — 돌아오지 않은 언니", StoryBriefings.Briefing(42), 3, 1));
                scenes.Add(new StoryScene { Title = "등록 마감을 앞두고 — 세 사람의 조", Lines = stage.Prologue[4].Lines });
                scenes.Add(Segment("같은 무렵 — 서로 다른 참가 동기", StoryBriefings.Briefing(42), 4, 1));
                scenes.Add(new StoryScene { Title = "그날 밤 — 선착장 공방 안내", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 43)
            {
                scenes.Add(stage.Prologue[0]);
                scenes.Add(Segment("상으로 풀린 환 — 안전과 다른 힘", StoryBriefings.Briefing(43), 0, 3));
                scenes.Add(stage.Prologue[1]);
                scenes.Add(stage.Prologue[2]);
                scenes.Add(Segment("객잔의 현재 — 왼손으로 마주한 예청", StoryBriefings.Briefing(43), 3, 2));
                scenes.Add(new StoryScene { Title = "왼손 수비와 방향 전환의 공방", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 44)
            {
                scenes.Add(stage.Prologue[0]);
                scenes.Add(Segment("본선 개막 — 예선을 통과한 열여섯 조", StoryBriefings.Briefing(44), 0, 1));
                var previous = EarlyGameContent.FindStage(43);
                scenes.Add(Segment("본선 개막 무렵 — 외팔이와 감시 보고", previous.Outro, 12, previous.Outro.Length - 12));
                scenes.Add(Segment("관중에게 전해지지 않은 명령", StoryBriefings.Aftermath(43), 3, 1));
                scenes.Add(stage.Prologue[1]);
                scenes.Add(new StoryScene { Title = "본선 — 서로 다른 검과 동기", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 45)
            {
                scenes.Add(stage.Prologue[0]);
                scenes.Add(Segment("문 너머에서 들은 살해 명령", StoryBriefings.Briefing(45), 2, 1));
                scenes.Add(stage.Prologue[1]);
                scenes.Add(Segment("기억을 부정하는 말과 몸에 남은 검", StoryBriefings.Briefing(45), 0, 2));
                scenes.Add(new StoryScene { Title = "준결승 — 언니를 되찾으려는 검", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 37)
            {
                var current = stage.Prologue[0];
                scenes.Add(Segment(current.Title, current.Lines, 0, 1));
                scenes.Add(Segment("플레이어에게 드러난 이름 — 예린은 아직 모르는 사실", current.Lines, 3, 1));
                var background = StoryBriefings.Briefing(37);
                scenes.Add(Segment("연비라는 이름과 서로 다른 지식", background, 0, 2));
                scenes.Add(stage.Prologue[1]);
                scenes.Add(stage.Prologue[2]);
                scenes.Add(Segment("회상 — 사천의 배신과 후원의 만남", background, 2, 3));
                var promise = stage.Prologue[3];
                scenes.Add(Segment(promise.Title, promise.Lines, 0, 4));
                scenes.Add(Segment("회상 — 도움을 청할 수 있는 약속", background, 5, 1));
                scenes.Add(Segment("회상 — 린과 함께 아미산으로", promise.Lines, 4, 3));
                scenes.Add(Segment("회상 속 회상 — 사부와 처음 만난 날", promise.Lines, 7, promise.Lines.Length - 7));
                scenes.Add(Segment("십 년 전 아미산으로 — 둥지를 노리는 백교", background, 6, background.Length - 6));
                scenes.Add(new StoryScene { Title = "회상 — 아미산의 둥지 앞", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 38)
            {
                scenes.Add(stage.Prologue[0]);
                scenes.Add(Segment("그믐밤 — 고향으로 돌아온 효룡", StoryBriefings.Briefing(38), 0, 2));
                var rooms = stage.Prologue[1];
                scenes.Add(Segment("도착한 밤 — 기숙사 방 배정", rooms.Lines, 0, 2));
                scenes.Add(Segment("도착 다음 날 — 설명회와 임시 십삼대", rooms.Lines, 2, rooms.Lines.Length - 2));
                scenes.Add(new StoryScene { Title = "설명회 뒤 — 새 책임을 앞두고", Lines = stage.Intro });
                var reunion = stage.Prologue[2];
                scenes.Add(Segment("현재 — 준호의 질문", reunion.Lines, 0, 3));
                scenes.Add(Segment("회상 — 전날 복도에서 부딪힌 두 사람", reunion.Lines, 3, reunion.Lines.Length - 3));
                return scenes.ToArray();
            }
            if (stage.Number == 39)
            {
                scenes.Add(stage.Prologue[0]);
                var background = StoryBriefings.Briefing(39);
                scenes.Add(Segment("환영회 초대에 담긴 목전", background, 0, 2));
                scenes.Add(stage.Prologue[1]);
                scenes.Add(Segment("같은 무렵 다른 물길 — 접수된 흑룡선", background, 2, 2));
                scenes.Add(stage.Prologue[2]);
                scenes.Add(Segment("포효를 들은 뒤 — 협력을 택한 예린", background, 4, 2));
                scenes.Add(stage.Prologue[3]);
                scenes.Add(Segment("칠상흔 관전 뒤 — 참가 조건을 향해", background, 6, background.Length - 6));
                scenes.Add(new StoryScene { Title = "현재 — 돈왕을 찾아가는 계단", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 40)
            {
                var road = stage.Prologue[0];
                scenes.Add(Segment("돈왕의 집무실을 나서며", road.Lines, 0, 2));
                scenes.Add(Segment("현재 — 혼자 감당하려는 만남", road.Lines, 2, 3));
                scenes.Add(Segment("다리 위 — 기다리던 매복", road.Lines, 5, road.Lines.Length - 5));
                scenes.Add(Segment("십면매복 — 위아래의 공격 방향", StoryBriefings.Briefing(40), 0, 2));
                scenes.Add(new StoryScene { Title = "매복을 읽는 두 사람", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 31)
            {
                scenes.Add(new StoryScene { Title = "겨울을 지나 봄 — 귀환과 증언", Lines = stage.Prologue[0].Lines });
                var background = StoryBriefings.Briefing(31);
                scenes.Add(Segment("학관 — 두 노사의 전승을 재다", background, 0, 4));
                var trial = stage.Prologue[1];
                scenes.Add(Segment(trial.Title, trial.Lines, 0, 4));
                scenes.Add(Segment("시험 뒤 — 의약전의 두 노사", background, 4, background.Length - 4));
                scenes.Add(Segment("의약전 — 합격의 이름 다툼", trial.Lines, 4, trial.Lines.Length - 4));
                var task = stage.Prologue[2];
                scenes.Add(Segment(task.Title, task.Lines, 0, 12));
                scenes.Add(StoryBriefings.Interlude(31));
                scenes.Add(Segment("답장을 품고 떠난 비류연", task.Lines, 12, task.Lines.Length - 12));
                scenes.Add(stage.Prologue[3]);
                var dream = stage.Prologue[4];
                scenes.Add(Segment("꿈 — 사라진 이름", dream.Lines, 0, 5));
                scenes.Add(Segment("다른 곳의 현재 — 몽환산장에서 깨어나다", dream.Lines, 5, dream.Lines.Length - 5));
                scenes.Add(new StoryScene { Title = "회상 — 학관에 봄이 오기 전, 사천의 밤", Lines = stage.Prologue[5].Lines });
                scenes.Add(new StoryScene { Title = "회상 — 남매를 보낼 시간", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 32)
            {
                scenes.Add(new StoryScene { Title = "회상 — 홍매겁화 직후의 실패 보고", Lines = stage.Prologue[0].Lines });
                scenes.Add(new StoryScene { Title = "현재 — 누구인가에 답하는 검객", Lines = stage.Prologue[1].Lines });
                scenes.Add(stage.Prologue[2]);
                scenes.Add(Segment("시험관 발탁 뒤 — 보수와 숨긴 규칙", StoryBriefings.Briefing(32), 0, 6));
                scenes.Add(stage.Prologue[3]);
                scenes.Add(new StoryScene { Title = "현재 — 남창 골목의 도전자", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 33)
            {
                var family = stage.Prologue[0];
                scenes.Add(Segment("회상 — 공손세가에서의 출정", family.Lines, 0, 4));
                scenes.Add(Segment("현재 — 사흘의 미행과 기초 수련", family.Lines, 4, family.Lines.Length - 4));
                scenes.Add(stage.Prologue[1]);
                scenes.Add(new StoryScene { Title = "금란주루 — 거절을 무시한 사신", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 34)
            {
                var funeral = stage.Prologue[0];
                scenes.Add(Segment(funeral.Title, funeral.Lines, 0, 4));
                scenes.Add(Segment("같은 날, 감옥 — 죽음 위장의 속사정", funeral.Lines, 4, 6));
                scenes.Add(Segment("장례식으로 — 진령의 눈물", funeral.Lines, 10, funeral.Lines.Length - 10));
                scenes.Add(stage.Prologue[1]);
                scenes.Add(Background(stage));
                scenes.Add(new StoryScene { Title = "그날 밤 — 중원표국의 대문", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 35)
            {
                var bait = stage.Prologue[0];
                scenes.Add(Segment("감옥 — 손수 만든 도시락", bait.Lines, 0, 4));
                scenes.Add(StoryBriefings.Interlude(34));
                scenes.Add(Segment(bait.Title, bait.Lines, 4, bait.Lines.Length - 4));
                var background = StoryBriefings.Briefing(35);
                scenes.Add(Segment("밤길 — 그물 역할을 맡은 친구들", background, 0, 4));
                scenes.Add(Segment("기다리는 미끼조", background, 5, background.Length - 5));
                scenes.Add(new StoryScene { Title = "황금 완장의 미끼", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 36)
            {
                scenes.Add(new StoryScene { Title = "습격 다음 날 — 봉한 금낭의 전달", Lines = stage.Prologue[0].Lines });
                scenes.Add(stage.Prologue[1]);
                scenes.Add(Segment("관주실 — 살아 있는 손자를 기다리며", StoryBriefings.Briefing(36), 0, 5));
                scenes.Add(new StoryScene { Title = "검존의 오해와 공세", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 25)
            {
                var gate = stage.Prologue[0];
                scenes.Add(Segment("종쾌의 증언 — 백 년 전의 패배", gate.Lines, 0, 4));
                scenes.Add(Background(stage));
                scenes.Add(Segment("현재 — 대표 한 사람의 도약", gate.Lines, 4, gate.Lines.Length - 4));
                for (int i = 1; i < stage.Prologue.Length; i++)
                {
                    if (i == 5) scenes.Add(StoryBriefings.Interlude(25));
                    if (i == 4)
                    {
                        var missing = stage.Prologue[i];
                        scenes.Add(Segment(missing.Title, missing.Lines, 0, 1));
                        scenes.Add(Segment("회상 — 외출을 권하던 한노", missing.Lines, 1, 1));
                        scenes.Add(Segment("현재 — 부상 입은 한노의 소식", missing.Lines, 2, missing.Lines.Length - 2));
                    }
                    else scenes.Add(stage.Prologue[i]);
                }
                scenes.Add(new StoryScene { Title = "그날 밤 — 화평장 담장 아래", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 26)
            {
                scenes.Add(stage.Prologue[0]);
                scenes.Add(Segment("화평장 — 남은 친구와 떠나는 친구", StoryBriefings.Briefing(26), 0, 5));
                scenes.Add(Segment("회상 — 하산 전에 빙검에게 맡긴 부탁", stage.Outro, 9, 1));
                scenes.Add(stage.Prologue[1]);
                var background = StoryBriefings.Briefing(26);
                scenes.Add(Segment("같은 밤 — 은설란의 퇴로", background, 5, background.Length - 5));
                scenes.Add(new StoryScene { Title = "십이수라쇄성진 앞에서", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 27)
            {
                scenes.Add(stage.Prologue[0]);
                scenes.Add(stage.Prologue[1]);
                scenes.Add(stage.Prologue[2]);
                scenes.Add(Background(stage));
                scenes.Add(new StoryScene { Title = "홍매곡 입구 — 칠걸의 연환공격", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 28)
            {
                var legacy = stage.Prologue[0].Lines;
                scenes.Add(Segment("회상 — 백 년 전, 풀려난 두 사람", legacy, 0, 4));
                scenes.Add(Segment("회상 — 그로부터 이 년 뒤, 비전을 바꾸다", legacy, 4, 11));
                scenes.Add(Segment("현재 — 달을 보는 혁 노야", legacy, 15, legacy.Length - 15));
                var sisters = stage.Prologue[1].Lines;
                scenes.Add(Segment("현재 — 독고령의 지도와 남은 상처", sisters, 0, 8));
                scenes.Add(Segment("꿈속의 회상 — 영혼의 자매", sisters, 8, sisters.Length - 8));
                var memory = stage.Prologue[2].Lines;
                scenes.Add(Segment("회상 — 보타암에서 만난 은명", memory, 0, 6));
                scenes.Add(Segment("회상 — 일 년 뒤, 동굴의 상처", memory, 6, 7));
                scenes.Add(Segment("현재 — 천무봉의 달빛 아래", memory, 13, memory.Length - 13));
                scenes.Add(stage.Prologue[3]);
                scenes.Add(stage.Prologue[4]);
                scenes.Add(Background(stage));
                scenes.Add(new StoryScene { Title = "현재 — 바통을 쥔 마지막 주자", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 29)
            {
                var cliff = stage.Prologue[0].Lines;
                var previous = EarlyGameContent.FindStage(28).Outro;
                scenes.Add(Segment("회상 — 금요관 전날 밤의 잠행", cliff, 0, 7));
                scenes.Add(Segment("다음 날 — 금요관 출발", previous, 10, previous.Length - 11));
                scenes.Add(Segment("현재 — 출발선의 협박", cliff, 7, 4));
                scenes.Add(Background(stage));
                scenes.Add(Segment("현재 — 잘린 줄과 절벽의 추격", cliff, 11, cliff.Length - 11));
                scenes.Add(Segment("점수보다 먼저 구할 사람", previous, previous.Length - 1, 1));
                scenes.Add(new StoryScene { Title = "절벽 아래로", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 30)
            {
                scenes.Add(stage.Prologue[0]);
                scenes.Add(stage.Prologue[1]);
                scenes.Add(new StoryScene { Title = "중토관 종료 사흘 뒤 — 최우수 조 발표", Lines = stage.Prologue[2].Lines });
                scenes.Add(Segment("발표식 — 이의를 들을 자리", StoryBriefings.Briefing(30), 0, 5));
                scenes.Add(new StoryScene { Title = "옳은 말과 옳은 행동", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 19)
            {
                scenes.Add(stage.Prologue[0]);
                scenes.Add(StoryBriefings.Interlude(19));
                scenes.Add(stage.Prologue[1]);
                scenes.Add(stage.Prologue[2]);
                var briefing = Background(stage);
                briefing.Title = "사흘 뒤 — 환마동 시험 당일";
                scenes.Add(briefing);
                scenes.Add(stage.Prologue[3]);
                scenes.Add(new StoryScene { Title = "환마동 안 — 저마다의 두려움", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 20)
            {
                var underground = stage.Prologue[0];
                scenes.Add(Segment(underground.Title, underground.Lines, 0, 8));
                scenes.AddRange(StoryMidgame.Episodes[2000]);
                var rescue = Background(stage);
                rescue.Title = "지상 — 구조대를 맞다";
                scenes.Add(rescue);
                scenes.Add(StoryBriefings.Interlude(20));
                scenes.Add(Segment("지하 — 칠 일째의 약속", underground.Lines, 8, underground.Lines.Length - 8));
                scenes.Add(stage.Prologue[1]);
                return scenes.ToArray();
            }
            if (stage.Number == 21)
            {
                scenes.Add(stage.Prologue[0]);
                scenes.Add(stage.Prologue[1]);
                var background = StoryBriefings.Briefing(21);
                scenes.Add(Segment("화산으로 — 돌아올 힘을 남기는 수련", background, 0, 5));
                scenes.Add(StoryBriefings.Interlude(21));
                scenes.Add(stage.Prologue[2]);
                scenes.Add(Segment("구궁산 — 청혼을 거절하다", background, 5, background.Length - 5));
                scenes.Add(new StoryScene { Title = "노사들이 지켜보는 출전", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 22)
            {
                scenes.Add(stage.Prologue[0]);
                scenes.Add(stage.Prologue[1]);
                scenes.Add(stage.Prologue[2]);
                scenes.Add(StoryBriefings.Interlude(22));
                scenes.Add(stage.Prologue[3]);
                scenes.Add(new StoryScene { Title = "대홍산 — 아버지 앞에 나선 아들", Lines = stage.Intro });
                var background = Background(stage);
                background.Title = "아들이 나선 뒤 — 혈채의 추궁";
                scenes.Add(background);
                return scenes.ToArray();
            }
            if (stage.Number == 23)
            {
                if (stage.Prologue != null) scenes.AddRange(stage.Prologue);
                scenes.Add(Background(stage));
                scenes.Add(new StoryScene { Title = "협곡 아래 — 청홍쌍각사를 경계하다", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 24)
            {
                var memory = stage.Prologue[0];
                scenes.Add(Segment("회상 — 오 년 전 비뢰라는 이름", memory.Lines, 0, 6));
                scenes.Add(Segment("현재 — 피 흘리는 효룡", memory.Lines, 6, memory.Lines.Length - 6));
                var background = StoryBriefings.Briefing(24);
                scenes.Add(Segment("다친 학생들을 뒤로", background, 0, 4));
                scenes.Add(stage.Prologue[1]);
                scenes.Add(Segment("결계 안 — 일부에게만 닿은 말", background, 4, background.Length - 4));
                scenes.Add(new StoryScene { Title = "비뢰쌍마와의 대치", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 13)
            {
                if (stage.Prologue != null) scenes.AddRange(stage.Prologue);
                scenes.Add(StoryBriefings.Interlude(13));
                scenes.Add(new StoryScene { Title = "당철영이 전한 경고", Lines = stage.Intro });
                var preparation = Background(stage);
                preparation.Title = "경고 뒤 — 준결승 대기실";
                scenes.Add(preparation);
                return scenes.ToArray();
            }
            if (stage.Number == 14)
            {
                if (stage.Prologue != null)
                    for (int i = 0; i < stage.Prologue.Length; i++)
                    {
                        var scene = stage.Prologue[i];
                        scenes.Add(new StoryScene { Title = (i == 0 ? "회상 — " : "현재 — ") + scene.Title, Lines = scene.Lines, HeroArtKey = scene.HeroArtKey });
                    }
                scenes.Add(new StoryScene { Title = "연말 단체전 — 마지막 공방", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 15)
            {
                if (stage.Prologue != null) scenes.AddRange(stage.Prologue);
                var memory = Background(stage);
                memory.Title = "청흔의 회상 — 무당산의 길목으로";
                scenes.Add(memory);
                scenes.Add(StoryBriefings.Interlude(15));
                scenes.Add(new StoryScene { Title = "회상 — 철각비마대와의 대치", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 16)
            {
                if (stage.Prologue != null) scenes.AddRange(stage.Prologue);
                var arrival = Background(stage);
                arrival.Title = "조사관을 맞은 뒤 — 호위의 시작";
                scenes.Add(arrival);
                scenes.Add(StoryBriefings.Interlude(16));
                scenes.Add(new StoryScene { Title = "호위를 이어 가던 날 — 저잣거리", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 17)
            {
                if (stage.Prologue != null)
                    foreach (var scene in stage.Prologue)
                        scenes.Add(new StoryScene
                        {
                            Title = scene.Title == "지키기로 한 맹세" ? "회상 — " + scene.Title
                                : scene.Title == "전달되지 못한 경고" ? "현재 — " + scene.Title : scene.Title,
                            Lines = scene.Lines,
                            HeroArtKey = scene.HeroArtKey
                        });
                scenes.Add(Background(stage));
                scenes.Add(StoryBriefings.Interlude(17));
                scenes.Add(new StoryScene { Title = "현재 — 길모퉁이의 재회", Lines = stage.Intro });
                return scenes.ToArray();
            }
            if (stage.Number == 18)
            {
                if (stage.Prologue != null) scenes.AddRange(stage.Prologue);
                var background = StoryBriefings.Briefing(18);
                scenes.Add(Segment("아직 끝나지 않은 대치", background, 0, 5));
                scenes.Add(new StoryScene { Title = "현재 — 백룡검을 뽑다", Lines = stage.Intro });
                scenes.Add(Segment("백룡검과 아직 뽑히지 않은 검", background, 5, background.Length - 5));
                scenes.Add(StoryBriefings.Interlude(18));
                return scenes.ToArray();
            }
            if (stage.Number == 9 || stage.Number == 10 || stage.Number == 11)
            {
                scenes.Add(Background(stage));
                scenes.Add(StoryBriefings.Interlude(stage.Number));
                if (stage.Prologue != null)
                    for (int i = 0; i < stage.Prologue.Length; i++)
                    {
                        scenes.Add(stage.Prologue[i]);
                        if (stage.Number == 11 && i == 0)
                        {
                            var aftermath = StoryBriefings.Aftermath(10);
                            scenes.Add(Segment("나예린과의 약속 뒤 — 내기의 정산", aftermath, 2, aftermath.Length - 2));
                        }
                    }
                scenes.Add(new StoryScene { Lines = stage.Intro, HeroArtKey = stage.IntroHeroArtKey });
                return scenes.ToArray();
            }
            if (stage.Number == 12)
            {
                var previous = EarlyGameContent.FindStage(11).Outro;
                scenes.Add(Segment("개막일 — 묵금을 멘 비류연", previous, 5, previous.Length - 5));
                if (stage.Prologue != null) scenes.AddRange(stage.Prologue);
                scenes.Add(Segment("개막 뒤 — 우뢰매의 초대", stage.Intro, 0, 3));
                scenes.Add(Background(stage));
                scenes.Add(Segment("첫 공식전을 앞둔 관중석", stage.Intro, 3, stage.Intro.Length - 3));
                return scenes.ToArray();
            }
            if (stage.Prologue != null) scenes.AddRange(stage.Prologue);
            // The farewell precedes the account of the journey without the master.
            if (stage.Number == 3) scenes.Add(new StoryScene { Title = "이별의 아침", Lines = stage.Intro, HeroArtKey = stage.IntroHeroArtKey });
            var interlude = StoryBriefings.Interlude(stage.Number);
            if (stage.Number == 6 && interlude != null) scenes.Add(interlude);
            // Chapter 5's inn conversation follows the road; chapter 6's lord appears after his disciples.
            scenes.Add(stage.Number == 5 ? Segment("현재 — 맨얼굴의 표사", StoryBriefings.Briefing(5), 0, 5) : Background(stage));
            if (interlude != null && stage.Number != 4 && stage.Number != 5 && stage.Number != 6) scenes.Add(interlude);
            if (stage.Number == 6) scenes.Add(Segment("호아장 정문", stage.Intro, 0, 3));
            else if (stage.Number != 3) scenes.Add(new StoryScene { Title = stage.Name, Lines = stage.Intro, HeroArtKey = stage.IntroHeroArtKey });
            return scenes.ToArray();
        }

        /// <summary>Detailed record passages placed at the encounter where they occur.</summary>
        public static StoryScene[] BeforeFight(int stageNumber, int encounter)
        {
            if (stageNumber == 4 && encounter == 1)
                return new[] { StoryBriefings.Interlude(4) };
            if (stageNumber == 5 && encounter == 1)
            {
                var background = StoryBriefings.Briefing(5);
                return new[]
                {
                    Segment("현재 — 윤하루에서 들은 이름", background, 5, background.Length - 5),
                    StoryBriefings.Interlude(5)
                };
            }
            if (stageNumber == 6 && encounter == 2)
            {
                var intro = EarlyGameContent.FindStage(6).Intro;
                return new[] { Segment("문하의 패배 뒤 — 호아장주의 대치", intro, 3, intro.Length - 3) };
            }
            if (stageNumber == 12 && encounter == 1)
                return new[] { StoryBriefings.Interlude(12) };
            if (stageNumber == 23 && encounter == 1)
                return new[] { StoryBriefings.Interlude(23) };
            return null;
        }

        /// <summary>Explanations between noncombat choices, shared by play and the completed record.</summary>
        public static StoryScene[] BeforeEvent(StageData stage, int encounter)
        {
            if (!Supports(stage) || stage.EventFor(encounter) == null) return null;
            if (stage.Number == 58 && encounter == 1)
                return new[] { Segment("출항선에 오른 뒤 — 스스로 택한 자리", StoryBriefings.Briefing(58), 8, 1) };
            if (stage.Number == 59 && encounter == 1)
                return new[]
                {
                    Segment("푸줏간에서 상현을 찾은 뒤 — 끊어진 네 연락망", StoryBriefings.Briefing(59), 1, 2),
                    new StoryScene { Title = "최후 보고를 앞두고", Lines = stage.Intro }
                };
            if (stage.Number == 59 && encounter == 2)
                return new[]
                {
                    Segment("보고를 들은 조사조 — 남겨 둔 다섯째 거점", StoryBriefings.Briefing(59), 3, 2),
                    new StoryScene { Title = "교차 시점 — 같은 시각 객잔에서", Lines = stage.Prologue[2].Lines },
                    Segment("객잔 안과 밖 — 서로 모르는 두 판단", StoryBriefings.Briefing(59), 5, 3)
                };
            return null;
        }

        /// <summary>Place background passages after the event they describe, sharing this order with battle and replay.</summary>
        public static StoryScene[] ArrangeBeforeFight(int stageNumber, int encounter, StoryScene[] authored)
        {
            if (stageNumber == 61 && encounter == 3 && authored != null)
            {
                var stage = EarlyGameContent.FindStage(61);
                return new[]
                {
                    Segment("흑견을 넘긴 뒤 — 문을 열 세 검", stage.Prologue[1].Lines, 5, 1),
                    Segment("수색조 동료들의 차례", StoryBriefings.Briefing(61), 5, 1),
                    Segment(authored[0].Title, authored[0].Lines, 0, 4)
                };
            }
            if (stageNumber == 62 && encounter == 0 && authored != null)
                return new[] { authored[0] };
            if (stageNumber == 62 && encounter == 1 && authored != null)
            {
                var stage = EarlyGameContent.FindStage(62);
                return new[]
                {
                    StoryEscape.Episodes[6200][1],
                    Segment("제삼대 제압 뒤 — 물품과 복장", stage.Prologue[1].Lines, 0, 1),
                    Segment("추격을 뒤집을 단서", StoryBriefings.Briefing(62), 3, 1),
                    Segment("다음 추격조 — 풀린 점혈과 휘파람", stage.Prologue[1].Lines, 1, 1),
                    Segment("소리가 닿을 곳에 필요한 준비", StoryBriefings.Briefing(62), 4, 1),
                    Segment(authored[0].Title, authored[0].Lines, 0, 3)
                };
            }
            if (stageNumber == 62 && encounter == 2 && authored != null)
            {
                var stage = EarlyGameContent.FindStage(62);
                var whistle = StoryEscape.Episodes[6201][0];
                var result = new List<StoryScene>
                {
                    Segment("휘파람을 막은 뒤 — 차음막과 거짓 연기", whistle.Lines, 3, whistle.Lines.Length - 3),
                    Segment("수색을 이어 갈 복장과 정보", stage.Prologue[1].Lines, 2, 1),
                    Segment("추격의 방향을 거꾸로 읽다", StoryBriefings.Briefing(62), 5, 1)
                };
                result.AddRange(authored);
                return result.ToArray();
            }
            if (stageNumber == 63 && encounter == 2 && authored != null)
                return new[]
                {
                    Segment("첫 중단 뒤 — 합주 최고조를 기다리는 이유", EarlyGameContent.FindStage(63).Prologue[2].Lines, 0, 1),
                    Segment("모두 이탈할 시간을 위한 공진 저지", StoryBriefings.Briefing(63), 6, 1),
                    Segment(authored[0].Title, authored[0].Lines, 0, 1)
                };
            if (stageNumber == 63 && encounter == 3 && authored != null)
            {
                var peak = StoryEscape.Episodes[6302][0];
                var result = new List<StoryScene>
                {
                    Segment("사자후 뒤 — 기다림의 끝", peak.Lines, 1, peak.Lines.Length - 1)
                };
                result.AddRange(authored);
                return result.ToArray();
            }
            if (stageNumber == 64 && encounter == 1 && authored != null)
                return new[]
                {
                    Segment(authored[0].Title, authored[0].Lines, 0, 4),
                    Segment("회상 — 남해의 폭풍과 사익비홍 시련", authored[0].Lines, 4, 1)
                };
            if (stageNumber == 64 && encounter == 2 && authored != null)
            {
                var stage = EarlyGameContent.FindStage(64);
                var result = new List<StoryScene>
                {
                    Segment("현재로 — 폭풍을 뚫은 뒤 날아온 나뭇잎", StoryEscape.Episodes[6401][0].Lines, 5, 1),
                    new StoryScene { Title = "교차 시점 — 먼저 떠난 일행과 돌아가는 영령", Lines = stage.Prologue[1].Lines },
                    Segment("기억의 답보다 먼저 택한 귀환", StoryBriefings.Briefing(64), 3, 3),
                    Segment("아직 만나기 전 — 후위와 돌아오는 사람", stage.Prologue[2].Lines, 1, 1),
                    Segment("두 사람 사이에 남은 거리", StoryBriefings.Briefing(64), 7, 1),
                    new StoryScene { Title = "후위의 현재 — 창끝 다음의 피리", Lines = stage.Intro }
                };
                result.AddRange(authored);
                return result.ToArray();
            }
            if (stageNumber == 64 && encounter == 3 && authored != null)
                return new[]
                {
                    Segment("무혼초령음 뒤 — 풀숲을 향한 시선", EarlyGameContent.FindStage(64).Prologue[2].Lines, 0, 1),
                    Segment("피리와 다가오는 존재", StoryBriefings.Briefing(64), 6, 1),
                    Segment(authored[0].Title, authored[0].Lines, 0, 3)
                };
            if (stageNumber == 65 && encounter == 1 && authored != null)
            {
                var stage = EarlyGameContent.FindStage(65);
                return new[]
                {
                    authored[0],
                    Segment("통과 뒤 — 지나치게 쉬운 길에 남은 의문", authored[1].Lines, 1, 1),
                    stage.Prologue[1],
                    Segment("흑견대 물품에서 — 암시 봉인과 붉은 꽃", StoryBriefings.Briefing(65), 3, 3)
                };
            }
            if (stageNumber == 66 && encounter == 0 && authored != null)
                return new[] { Segment(authored[0].Title, authored[0].Lines, 1, authored[0].Lines.Length - 1) };
            if (stageNumber == 56 && encounter == 0)
                return new[] { Segment("안락휴 — 포위망을 향한 부채", EarlyGameContent.FindStage(56).Intro, 2, 2) };
            if (stageNumber == 56 && encounter == 1 && authored != null)
            {
                var result = new List<StoryScene>(StoryRescue.Episodes[5600]);
                result.AddRange(authored);
                return result.ToArray();
            }
            if (stageNumber == 57 && encounter == 0 && authored != null)
            {
                var result = new List<StoryScene>
                {
                    new StoryScene { Title = "회고 — 봉익일우와 끝나지 못한 물음", Lines = authored[0].Lines, HeroArtKey = authored[0].HeroArtKey }
                };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                result.Add(Segment("장송 뒤 — 살아 있는 이들의 귀환을 위해", EarlyGameContent.FindStage(57).Intro, 1, 1));
                return result.ToArray();
            }
            if (stageNumber == 60 && encounter == 1 && authored != null)
            {
                var stage = EarlyGameContent.FindStage(60);
                var identity = stage.Prologue[1];
                var background = StoryBriefings.Briefing(60);
                var result = new List<StoryScene>
                {
                    Segment("순찰단을 물린 뒤 — 드러난 위장", identity.Lines, 0, 8),
                    Segment("표행 때부터 이어진 감시", background, 3, 2),
                    Segment("옷으로 감출 수 없는 걸음과 기세", identity.Lines, 10, 1),
                    Segment("이화합격진을 준비하는 쌍둥이", identity.Lines, 11, 1),
                    Segment("쌍둥이가 준비한 합격진", background, 5, 1),
                    stage.Prologue[2],
                    Segment("합공을 거부하던 두 노사", background, 6, 2),
                    new StoryScene { Title = "일행을 보낼 시간 — 쌍둥이와의 공방", Lines = stage.Intro }
                };
                result.AddRange(authored);
                return result.ToArray();
            }
            if (stageNumber == 60 && encounter == 2 && authored != null)
            {
                var result = new List<StoryScene>
                {
                    Segment("쌍둥이의 공방 뒤 — 직접 나서는 무화", EarlyGameContent.FindStage(60).Prologue[1].Lines, 8, 2)
                };
                result.AddRange(authored);
                return result.ToArray();
            }
            if (stageNumber == 60 && encounter == 3 && authored != null)
            {
                var rescue = authored[0];
                var result = new List<StoryScene> { Segment(rescue.Title, rescue.Lines, 0, 2) };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                return result.ToArray();
            }
            if (stageNumber == 51 && encounter == 3 && authored != null)
            {
                var wind = authored[0];
                var result = new List<StoryScene>
                {
                    Segment(wind.Title, wind.Lines, 0, 2),
                    Segment("회상 — 삼 년 전 무당산의 회전", wind.Lines, 2, 3),
                    Segment("현재로 — 현운이 가진 전부", wind.Lines, 5, wind.Lines.Length - 5)
                };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                return result.ToArray();
            }
            if (stageNumber == 52 && encounter == 0)
                return new[] { Segment("북해도 길목 — 납치를 막는 동행", StoryBriefings.Briefing(52), 0, 2) };
            if (stageNumber == 52 && encounter == 1 && authored != null)
            {
                var stage = EarlyGameContent.FindStage(52);
                var route = StoryRescue.Episodes[5200][0];
                var room = authored[0];
                var result = new List<StoryScene>
                {
                    Segment("길목을 연 뒤 — 조용해지는 북해도", route.Lines, 0, 1),
                    Segment("치료실 — 영령과 꿈의 잔상", stage.Prologue[0].Lines, 3, stage.Prologue[0].Lines.Length - 3),
                    Segment("기억이 돌아오지 않은 영령", StoryBriefings.Briefing(52), 2, 4),
                    Segment("함께 찾겠다는 선택", stage.Intro, 0, 3),
                    Segment(route.Title, route.Lines, 1, route.Lines.Length - 1),
                    Segment("북해도 목관 — 나예린 대신 남은 시체", room.Lines, 0, 6),
                    Segment("외팔이와 서천 — 수색대의 대화", stage.Outro, 0, 1),
                    Segment("북해도 수색 뒤 — 남은 의문과 동행", StoryBriefings.Aftermath(52), 0, 2),
                    Segment("교차 시점 — 숙소와 붉은 방", stage.Intro, 3, 1),
                    Segment("밖의 수색과 안쪽의 관찰", StoryBriefings.Briefing(52), 6, 2),
                    Segment(room.Title, room.Lines, 6, room.Lines.Length - 6)
                };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                return result.ToArray();
            }
            if (stageNumber == 53 && encounter == 1 && authored != null)
            {
                var stage = EarlyGameContent.FindStage(53);
                var currents = authored[0];
                var result = new List<StoryScene>
                {
                    Segment("암습 뒤 — 끊긴 생포의 단서", currents.Lines, 0, 4),
                    Segment("영역의 책임 — 소생단을 청하다", stage.Intro, 1, stage.Intro.Length - 1),
                    Segment("완치와 다른 불안정한 기운", StoryBriefings.Briefing(53), 4, 2),
                    Segment(currents.Title, currents.Lines, 4, 7),
                    Segment("효룡이 지킬 시간", StoryBriefings.Briefing(53), 6, 2),
                    Segment("회상 — 먼저 비우라는 사부", currents.Lines, 11, 2),
                    Segment("현재로 — 세 기운을 담을 자리", currents.Lines, 13, currents.Lines.Length - 13)
                };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                return result.ToArray();
            }
            if (stageNumber == 55 && encounter == 1 && authored != null)
            {
                var stage = EarlyGameContent.FindStage(55);
                var result = new List<StoryScene>
                {
                    Segment("두 침을 뽑은 뒤 — 돌아오지 않은 내공", stage.Intro, 0, 1),
                    Segment("고갈된 몸과 문밖의 관찰", StoryBriefings.Briefing(55), 4, 3),
                    Segment("회상 — 웃음이 만드는 빈틈", stage.Intro, 1, 2),
                    Segment("현재로 — 철문을 열 방법", stage.Intro, 3, stage.Intro.Length - 3)
                };
                result.AddRange(authored);
                return result.ToArray();
            }
            if (stageNumber == 55 && encounter == 3 && authored != null)
            {
                var clinic = authored[0];
                var result = new List<StoryScene>
                {
                    Segment("창고 앞 — 파훼된 해상과 기절", clinic.Lines, 0, 4),
                    Segment("한편 사번대 — 먼저 도착한 일행의 치료", clinic.Lines, 4, 8),
                    Segment("사번대 — 나예린을 안고 온 무명", clinic.Lines, 12, clinic.Lines.Length - 12)
                };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                return result.ToArray();
            }
            if (stageNumber == 46 && encounter >= 1 && encounter <= 3 && authored != null)
            {
                var background = StoryBriefings.Briefing(46);
                var result = new List<StoryScene>();
                if (encounter == 3) result.Add(Segment("쌍도가 드러난 뒤 — 다가오는 심도", background, 4, 1));
                result.AddRange(authored);
                if (encounter == 1) result.Add(Segment("혈강 뒤 — 붉은 안개", background, 2, 1));
                if (encounter == 2) result.Add(Segment("관에서 꺼낸 아홉 해의 과거", background, 3, 1));
                return result.ToArray();
            }
            if (stageNumber == 47 && encounter == 0 && authored != null)
            {
                var result = new List<StoryScene>(authored);
                result.Add(Segment("뿔 장식을 찾은 뒤 — 밀실의 함정", StoryBriefings.Briefing(47), 5, 2));
                return result.ToArray();
            }
            if (stageNumber == 48 && encounter == 1 && authored != null)
            {
                var result = new List<StoryScene>(authored);
                result.Add(Segment("네 섬의 역할 — 서해도 철갑사패 앞", StoryBriefings.Briefing(48), 4, 2));
                return result.ToArray();
            }
            if (stageNumber == 49 && encounter == 1 && authored != null)
            {
                var intro = EarlyGameContent.FindStage(49).Intro;
                var result = new List<StoryScene>
                {
                    Segment("다섯 주먹 뒤 — 주망포박과 격공장", intro, 2, intro.Length - 2),
                    Segment("갑옷을 직접 치지 않는 길", StoryBriefings.Briefing(49), 3, 1)
                };
                result.AddRange(authored);
                return result.ToArray();
            }
            if (stageNumber == 49 && encounter == 5 && authored != null)
            {
                var mind = authored[0];
                var result = new List<StoryScene>
                {
                    Segment(mind.Title, mind.Lines, 0, 3),
                    Segment("갑옷 아래의 반탄강 — 죽음 대신 찾을 길", StoryBriefings.Briefing(49), 6, 2),
                    Segment("판관을 싫어한 까닭과 마음의 틈", mind.Lines, 3, mind.Lines.Length - 3)
                };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                return result.ToArray();
            }
            if (stageNumber == 50 && encounter == 1 && authored != null)
            {
                var intro = EarlyGameContent.FindStage(50).Intro;
                var result = new List<StoryScene> { Segment("홍선을 가른 뒤 — 진짜 무기", intro, 1, intro.Length - 1) };
                result.AddRange(authored);
                return result.ToArray();
            }
            if (stageNumber == 50 && encounter == 3 && authored != null)
            {
                var method = authored[0];
                var result = new List<StoryScene>
                {
                    Segment(method.Title, method.Lines, 0, 2),
                    Segment("회상 — 자신에게 맞는 방법을 찾으라", method.Lines, 2, 2),
                    Segment("모용휘가 이어 온 서로 다른 배움", StoryBriefings.Briefing(50), 6, 2),
                    Segment("현재로 — 오른손 검과 왼손 소도", method.Lines, 4, method.Lines.Length - 4)
                };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                return result.ToArray();
            }
            if (stageNumber == 42 && encounter == 0 && authored != null)
            {
                var result = new List<StoryScene>(authored);
                var background = StoryBriefings.Briefing(42);
                result.Add(Segment("일대일 약속 뒤 — 비도 대신 정신봉", background, 5, background.Length - 5));
                return result.ToArray();
            }
            if (stageNumber == 43 && encounter == 1 && authored != null)
            {
                var background = StoryBriefings.Briefing(43);
                var result = new List<StoryScene>(authored)
                {
                    Segment("두 언월의 압박 — 방향을 돌려야 할 힘", background, 5, 1),
                    Segment("마지막 공방의 선택", background, 7, 1)
                };
                return result.ToArray();
            }
            if (stageNumber == 44 && encounter >= 0 && encounter <= 4 && authored != null)
            {
                var background = StoryBriefings.Briefing(44);
                var result = new List<StoryScene>();
                if (encounter == 4) result.Add(Segment("곁에 서기 위해 배운 비상련화", background, 6, 1));
                result.AddRange(authored);
                if (encounter <= 2) result.Add(Segment("본선 — 각자의 무공", background, encounter + 1, 1));
                if (encounter == 3)
                {
                    result.Add(Segment("힘을 아끼려는 연비의 기권", background, 5, 1));
                    result.Add(Segment("팀의 다음 검 — 나예린", background, 4, 1));
                }
                return result.ToArray();
            }
            if (stageNumber == 45 && encounter == 0 && authored != null)
            {
                var guard = authored[0];
                var result = new List<StoryScene> { Segment(guard.Title, guard.Lines, 0, 4) };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                return result.ToArray();
            }
            if (stageNumber == 45 && encounter == 1 && authored != null)
            {
                var guard = StoryTournament.Episodes[4500][0];
                var result = new List<StoryScene>
                {
                    Segment("감시자를 막아 낸 뒤 — 린의 차례", guard.Lines, 4, guard.Lines.Length - 4),
                    Segment("열린 검의 무대", StoryBriefings.Briefing(45), 3, 1)
                };
                result.AddRange(authored);
                result.Add(Segment("소안검에 남은 반응", StoryBriefings.Briefing(45), 4, 1));
                return result.ToArray();
            }
            if (stageNumber == 45 && encounter == 2 && authored != null)
            {
                var ice = authored[0];
                var result = new List<StoryScene>
                {
                    Segment("회상 — 혼자서는 익힐 수 없는 검", ice.Lines, 0, 2),
                    Segment("함께 배웠던 한빙섬옥", StoryBriefings.Briefing(45), 5, 1),
                    Segment("현재로 — " + ice.Title, ice.Lines, 2, ice.Lines.Length - 2)
                };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                return result.ToArray();
            }
            if (stageNumber == 45 && encounter == 3 && authored != null)
            {
                var flute = authored[0];
                var result = new List<StoryScene>
                {
                    Segment("붉은 피리 — 폭주와 옆구리의 상처", flute.Lines, 0, 2),
                    Segment("외부에서 울린 심령금제", StoryBriefings.Briefing(45), 6, 1),
                    Segment("회접 — 피리를 부수다", flute.Lines, 2, 2),
                    Segment("남은 마지막 검의 주체", StoryBriefings.Briefing(45), 7, 1),
                    Segment(flute.Title, flute.Lines, 4, flute.Lines.Length - 4)
                };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                return result.ToArray();
            }
            if (stageNumber == 38 && encounter == 0 && authored != null)
            {
                var result = new List<StoryScene>();
                foreach (var scene in authored)
                    result.Add(new StoryScene { Title = "회상 — 전날 복도의 혈풍란", Lines = scene.Lines, HeroArtKey = scene.HeroArtKey });
                return result.ToArray();
            }
            if (stageNumber == 38 && encounter == 1 && authored != null)
            {
                var background = StoryBriefings.Briefing(38);
                var result = new List<StoryScene>
                {
                    Segment("현재로 — 지도·통행 띠와 임시 부대", background, 2, 3),
                    Segment("현재 — 대장 선출의 승부와 기권", background, 5, 2)
                };
                result.AddRange(authored);
                return result.ToArray();
            }
            if (stageNumber == 40 && encounter == 1 && authored != null)
            {
                var result = new List<StoryScene>(authored);
                result.Add(Segment("다리 붕괴 뒤 — 물 위의 현천은린", StoryBriefings.Briefing(40), 2, 2));
                return result.ToArray();
            }
            if (stageNumber == 40 && encounter == 2 && authored != null)
            {
                var capture = authored[0];
                var result = new List<StoryScene>
                {
                    Segment(capture.Title, capture.Lines, 0, 8),
                    Segment("배후를 확인한 뒤 — 일부러 밀린 공방", StoryBriefings.Briefing(40), 4, 2)
                };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                return result.ToArray();
            }
            if (stageNumber == 40 && encounter == 3 && authored != null)
            {
                var wait = StoryMidgame.Episodes[4002][0];
                var background = StoryBriefings.Briefing(40);
                var result = new List<StoryScene>
                {
                    Segment("버텨 낸 뒤 — 열 명의 진법을 기다리다", wait.Lines, 8, wait.Lines.Length - 8),
                    Segment("예린에게 맡긴 진법의 중심", background, 6, background.Length - 6)
                };
                result.AddRange(authored);
                return result.ToArray();
            }
            if (stageNumber == 32 && encounter == 0 && authored != null)
            {
                var result = new List<StoryScene>
                {
                    new StoryScene { Title = "회상 — 비도를 읽던 일 촌의 수련", Lines = authored[0].Lines, HeroArtKey = authored[0].HeroArtKey }
                };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                var background = StoryBriefings.Briefing(32);
                result.Add(Segment("현재 — 두 도전자를 마주한 남궁상", background, 6, background.Length - 6));
                return result.ToArray();
            }
            if (stageNumber == 33 && encounter == 0 && authored != null)
            {
                var result = new List<StoryScene>(authored);
                result.Add(Background(EarlyGameContent.FindStage(33)));
                return result.ToArray();
            }
            if (stageNumber == 33 && encounter == 2 && authored != null)
            {
                var result = new List<StoryScene> { StoryBriefings.Interlude(33) };
                result.AddRange(authored);
                return result.ToArray();
            }
            if (stageNumber == 35 && encounter == 0 && authored != null)
            {
                var result = new List<StoryScene>(authored);
                result.Add(Segment("상징을 노렸다는 이시건", StoryBriefings.Briefing(35), 4, 1));
                return result.ToArray();
            }
            if (stageNumber == 35 && encounter == 2 && authored != null)
            {
                var hostage = authored[0];
                var result = new List<StoryScene>
                {
                    Segment("인질 때문에 놓친 이시건", hostage.Lines, 0, 4),
                    Segment("이틀 혼수 뒤 — 열쇠를 노린 계획", hostage.Lines, 4, 5),
                    StoryBriefings.Interlude(35),
                    Segment("다음 날 밤 — 관제묘에서 확인한 함정", EarlyGameContent.FindStage(35).Outro, 0, 2),
                    Segment("두 고수가 비운 표국으로", hostage.Lines, 9, hostage.Lines.Length - 9)
                };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                return result.ToArray();
            }
            if (stageNumber == 36 && encounter == 1 && authored != null)
            {
                var duel = authored[0];
                var background = StoryBriefings.Briefing(36);
                var result = new List<StoryScene>
                {
                    Segment("검존의 오해 뒤 — 사절단 인솔", duel.Lines, 0, 6),
                    Segment("그 다음 날 — 비상련화 비무", background, 5, background.Length - 5),
                    Segment(duel.Title, duel.Lines, 6, duel.Lines.Length - 6)
                };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                return result.ToArray();
            }
            if (stageNumber == 25 && encounter == 3 && authored != null)
            {
                var result = new List<StoryScene>
                {
                    Segment("은설란 앞을 지킨 모용휘", StoryBriefings.Aftermath(25), 0, 2)
                };
                result.AddRange(authored);
                return result.ToArray();
            }
            if (stageNumber == 28 && encounter == 1 && authored != null)
            {
                var race = authored[0];
                var result = new List<StoryScene>
                {
                    Segment("목요관 결승선 — 공동 일위", race.Lines, 0, 6),
                    StoryBriefings.Interlude(28),
                    Segment("한편 — 감찰조 말살 명령과 삼 일의 도주", race.Lines, 6, race.Lines.Length - 6)
                };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                return result.ToArray();
            }
            if (stageNumber == 29 && encounter == 1 && authored != null)
            {
                var result = new List<StoryScene> { StoryBriefings.Interlude(29) };
                var duel = authored[0];
                result.Add(Segment("구조 뒤 — 걱정과 책임 추궁", duel.Lines, 0, 10));
                result.Add(Segment("그날 — 검후의 십초지적", duel.Lines, 10, duel.Lines.Length - 10));
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                return result.ToArray();
            }
            if (stageNumber == 30 && encounter == 0 && authored != null)
            {
                var result = new List<StoryScene>(authored);
                var background = StoryBriefings.Briefing(30);
                result.Add(Segment("대진 발동 뒤 — 불길 앞의 대치", background, 5, background.Length - 5));
                return result.ToArray();
            }
            if (stageNumber == 20 && encounter == 0)
                return new[] { new StoryScene { Title = "열흘째 이후 — 자연동굴", Lines = EarlyGameContent.FindStage(20).Intro } };
            if (stageNumber == 22 && encounter == 1 && authored != null)
            {
                var witness = authored[0];
                var farewell = EarlyGameContent.FindStage(22).Outro;
                var result = new List<StoryScene>
                {
                    Segment("대홍산 — 무승부와 아버지의 인정", witness.Lines, 0, 2),
                    Segment("대홍산을 떠나며", farewell, 2, 3),
                    Segment(witness.Title, witness.Lines, 2, witness.Lines.Length - 2)
                };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                return result.ToArray();
            }
            if (stageNumber == 24 && encounter == 3 && authored != null)
            {
                var promise = authored[0];
                var result = new List<StoryScene>
                {
                    Segment(promise.Title, promise.Lines, 0, 15),
                    Segment("내기에 진 모사령 — 약속을 지키다", StoryBriefings.Aftermath(23), 0, 2),
                    Segment("세 가지 질문", promise.Lines, 15, promise.Lines.Length - 16),
                    StoryBriefings.Interlude(24),
                    Segment("야영 뒤 — 화산으로", promise.Lines, promise.Lines.Length - 1, 1)
                };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                return result.ToArray();
            }
            if (stageNumber == 14 && encounter == 1 && authored != null)
            {
                var destination = authored[0];
                var result = new List<StoryScene>
                {
                    Segment("단체전 뒤 — 패배를 인정한 맹연호", destination.Lines, 0, 2),
                    StoryBriefings.Interlude(14),
                    Segment(destination.Title, destination.Lines, 2, destination.Lines.Length - 2),
                    Segment("무당산으로 — 새해의 합숙", StoryBriefings.Briefing(14), 0, 4)
                };
                for (int i = 1; i < authored.Length; i++) result.Add(authored[i]);
                return result.ToArray();
            }
            var extra = BeforeFight(stageNumber, encounter);
            if (stageNumber == 14 && encounter == 2)
            {
                var background = StoryBriefings.Briefing(14);
                extra = new[] { Segment("첫 습격 뒤 — 수상한 표적", background, 4, background.Length - 4) };
            }
            if (extra == null) return authored;
            var combined = new List<StoryScene>(extra);
            if (authored != null) combined.AddRange(authored);
            return combined.ToArray();
        }

        /// <summary>Keep the night aftermath before the next chapter's morning, and the ceremony after promotion.</summary>
        public static StoryScene[] Ending(StageData stage)
        {
            if (!Supports(stage)) return null;
            var scenes = new List<StoryScene>();
            if (stage.Number == 61)
            {
                scenes.Add(Segment("삼인연환 뒤 — 열린 남문", StoryEscape.Episodes[6103][0].Lines, 4, 1));
                scenes.Add(Segment("성문 밖으로 — 계속되는 수색", stage.Outro, 0, 4));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(61), 0, 1));
                scenes.Add(Segment("뒤에 남은 남문 — 흑견의 연기와 진짜 내상", stage.Outro, 4, 4));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(61), 1, 1));
                scenes.Add(Segment("교차 시점 — 흑천맹의 임시 맹주와 출병 명령", stage.Outro, 8, 2));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(61), 2, 2));
                return scenes.ToArray();
            }
            if (stage.Number == 62)
            {
                scenes.Add(Segment("흑견 생포 뒤 — 얼굴을 본뜨고 임시 보관하다", stage.Outro, 0, 6));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(62), 0, 1));
                scenes.Add(Segment("알려진 연기 뒤에도 남은 더 큰 감시", stage.Outro, 6, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(62), 1, 1));
                scenes.Add(Segment("갈라진 두 길 — 계속 수색하는 네 사람", stage.Outro, 7, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(62), 2, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 63)
            {
                scenes.Add(Segment("대지의 파도 뒤 — 사상자 없는 이탈", stage.Outro, 0, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(63), 0, 1));
                scenes.Add(Segment("네 역할로 연 길과 아직 남은 인정", stage.Outro, 1, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(63), 1, 1));
                scenes.Add(Segment("후위의 약속 — 홀로 남은 나예린", stage.Outro, 2, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(63), 2, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 64)
            {
                scenes.Add(Segment("연막으로 빠져나온 뒤 — 돌아온 이유", StoryEscape.Episodes[6403][0].Lines, 3, 2));
                scenes.Add(Segment("자매들 쪽에서 — 의도된 인질과 이어지는 계획", stage.Outro, 0, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(64), 0, 1));
                scenes.Add(Segment("임시 동행과 아직 돌아오지 않은 기억", stage.Outro, 1, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(64), 1, 1));
                scenes.Add(Segment("교차 시점 — 굉천의 무당 진군과 포로들의 합의", stage.Outro, 2, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(64), 2, 1));
                scenes.Add(Segment("효혜의 판단 — 잠적한 둘째를 부를 때", stage.Outro, 3, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(64), 3, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 65)
            {
                var memory = StoryEscape.Episodes[6501][1];
                scenes.Add(Segment("암시 봉인 해제 뒤 — 되찾은 남천의 기억", memory.Lines, 0, 1));
                scenes.Add(Segment("붉은 꽃을 찾다", memory.Lines, 2, 1));
                scenes.Add(new StoryScene { Title = "삼천의 틈에서 물 아래로", Lines = stage.Intro });
                scenes.Add(stage.Prologue[2]);
                scenes.Add(Segment("먼저 들어갈 사람과 따라갈 사람", StoryBriefings.Briefing(65), 6, 2));
                scenes.Add(Segment("급류 앞 — 먼저 뛴 장홍에게", stage.Outro, 1, 3));
                scenes.Add(Segment("뇌령사로 확인한 생존 — 대난원 입수", stage.Outro, 0, 1));
                scenes.Add(Segment("대난원 — 발견과 안도의 시간", stage.Outro, 4, 2));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(65), 0, 2));
                scenes.Add(Segment("멎지 않는 피 — 떠오르는 천무봉의 기억", stage.Outro, 6, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(65), 2, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 66)
            {
                scenes.Add(Segment("쌍룡귀천 뒤 — 장홍이 맡은 나백천의 이송", stage.Outro, 0, 2));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(66), 0, 1));
                scenes.Add(Segment("신마가 쪽 — 무당산을 가리킨 서찰", stage.Outro, 2, 3));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(66), 1, 1));
                scenes.Add(Segment("구출대 쪽 — 실제로 택한 남쪽 무한", stage.Outro, 5, 5));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(66), 2, 1));
                scenes.Add(Segment("그날 저녁에서 밤으로 — 흑견의 시찰과 신 녹호객잔", stage.Outro, 10, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(66), 3, 1));
                scenes.Add(Segment("객잔의 술자리 — 합석을 청한 쌍도의 사내", stage.Outro, 12, 3));
                scenes.Add(Segment("삼층의 호위와 아래층의 새로운 만남", stage.Outro, 11, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(66), 4, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 56)
            {
                scenes.Add(Segment("출항을 확보한 뒤 — 서천에게 남긴 상흔", stage.Outro, 0, 2));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(56), 0, 2));
                scenes.Add(Segment("먼저 떠난 배 — 돌아가야 할 약속", stage.Outro, 2, 2));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(56), 3, 1));
                scenes.Add(Segment("해명되지 않은 과거", stage.Outro, 4, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(56), 2, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 57)
            {
                var after = StoryBriefings.Aftermath(57);
                scenes.Add(Segment("호수의 물기둥 — 당문혜를 위한 장송", stage.Outro, 0, 2));
                scenes.Add(Segment(null, after, 0, 2));
                scenes.Add(Segment("풍신을 알아본 동료들", stage.Outro, 2, 1));
                scenes.Add(Segment(null, after, 2, 1));
                scenes.Add(Segment("물길을 따라온 무명과 장소옥", stage.Outro, 3, 4));
                scenes.Add(Segment(null, after, 4, 3));
                scenes.Add(Segment("배 위 — 소진된 기력과 잠시의 휴식", stage.Outro, 7, 3));
                scenes.Add(Segment(null, after, 3, 1));
                scenes.Add(Segment(null, after, 7, 1));
                scenes.Add(Segment("교차 시점 — 그날 밤 흑상루의 협박", stage.Outro, 10, 3));
                scenes.Add(Segment("백뢰 위탁과 아직 모르는 검의 행방", after, 8, 3));
                scenes.Add(Segment("다른 장소, 집무실 — 먼저 온 마천각주", stage.Outro, 13, 4));
                scenes.Add(Segment(null, after, 12, 1));
                scenes.Add(Segment("접객당 — 나백천의 기세를 확인한 검마", stage.Outro, 17, 2));
                scenes.Add(Segment(null, after, 11, 1));
                scenes.Add(Segment("구출 전서 — 안도한 나백천", stage.Outro, 19, 2));
                scenes.Add(Segment(null, after, 13, 1));
                scenes.Add(Segment("집무실 — 이미 죽어 있던 갈중천", stage.Outro, 21, 2));
                scenes.Add(Segment(null, after, 14, 1));
                scenes.Add(Segment("백뢰를 뽑는 순간 — 별개로 준비된 누명", stage.Outro, 23, 3));
                scenes.Add(Segment(null, after, 15, 4));
                return scenes.ToArray();
            }
            if (stage.Number == 58)
            {
                scenes.Add(Segment("남문 통과 뒤 — 용산객잔으로", stage.Outro, 0, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(58), 0, 1));
                scenes.Add(Segment("교차 시점, 흑천맹 대전 — 무화와 은설란", stage.Outro, 1, 8));
                scenes.Add(Segment("대전에서 — 환살과 출진의 각오", StoryBriefings.Aftermath(58), 1, 1));
                scenes.Add(Segment("출진 — 나 씨의 이름을 적은 살생부", stage.Outro, 9, 2));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(58), 2, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 59)
            {
                scenes.Add(Segment("객잔의 포위 — 두 갈래 판단의 끝", stage.Outro, 0, 2));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(59), 0, 2));
                scenes.Add(Segment("수색조가 얻은 단서 — 아직 끝나지 않은 탈출", stage.Outro, 2, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(59), 2, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 60)
            {
                var after = StoryBriefings.Aftermath(60);
                var rescue = StoryEscape.Episodes[6003][0];
                scenes.Add(Segment("화륜참 뒤 — 붙잡힌 검을 놓은 손", rescue.Lines, 2, rescue.Lines.Length - 2));
                scenes.Add(Segment("서로를 구한 뒤 — 함께 펼친 빙폭우와 화륜람", stage.Outro, 0, 1));
                scenes.Add(Segment(null, after, 0, 1));
                scenes.Add(Segment("무화의 요구 — 미완성의 건곤일월합격진", stage.Outro, 1, 1));
                scenes.Add(Segment("회상 — 사부가 남긴 유언", stage.Outro, 2, 1));
                scenes.Add(Segment("현재로 — 이십 년 동안 이루지 못한 조화", stage.Outro, 3, 4));
                scenes.Add(Segment(null, after, 1, 1));
                scenes.Add(Segment("효홍의 용근삭 — 살아 있는 포로", stage.Outro, 7, 3));
                scenes.Add(Segment(null, after, 2, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 51)
            {
                scenes.Add(Segment("휘어진 동전 — 풍우만곡과 승리", stage.Outro, 0, 3));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(51), 0, 3));
                scenes.Add(Segment("승리 뒤에도 남은 산산의 부상", stage.Outro, 3, stage.Outro.Length - 3));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(51), 3, 2));
                return scenes.ToArray();
            }
            if (stage.Number == 52)
            {
                scenes.Add(Segment("같은 무렵 강호란도 — 예청과 혁중", stage.Outro, 1, 9));
                scenes.Add(Segment("그 자리에서만 나눈 십삼대 이야기", StoryBriefings.Aftermath(52), 2, 5));
                scenes.Add(Segment("붉은 방으로 — 서른여섯 금침", stage.Outro, 10, 4));
                scenes.Add(Segment("아직 시작인 금제 해제", StoryBriefings.Aftermath(52), 7, 3));
                scenes.Add(Segment("다른 장소 — 구경을 나서는 무명", stage.Outro, 14, stage.Outro.Length - 14));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(52), 10, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 53)
            {
                scenes.Add(Segment("제어 뒤 — 남겨 둔 부대장의 목숨", stage.Outro, 0, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(53), 0, 2));
                scenes.Add(Segment("서해도를 떠나며", stage.Outro, 1, stage.Outro.Length - 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(53), 2, 2));
                return scenes.ToArray();
            }
            if (stage.Number == 54)
            {
                scenes.Add(Segment("세 사람의 승리 뒤 — 비류연의 지원", stage.Outro, 0, 4));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(54), 0, 3));
                scenes.Add(Segment("네 섬의 관을 잇는 함정", stage.Outro, 4, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(54), 3, 2));
                scenes.Add(Segment("합류한 안내자들 — 사번대로", stage.Outro, 5, stage.Outro.Length - 5));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(54), 5, 2));
                return scenes.ToArray();
            }
            if (stage.Number == 55)
            {
                scenes.Add(Segment("세 수 뒤 — 멎은 승부와 미지의 이름", stage.Outro, 0, 2));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(55), 0, 5));
                scenes.Add(Segment("수술 뒤 — 아직 회복 중인 두 사람", stage.Outro, 2, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(55), 5, 3));
                scenes.Add(Segment("영령이 믿고 싶은 지금의 감정", StoryBriefings.Aftermath(55), 8, 1));
                scenes.Add(Segment("잊은 사람 — 다시 잡은 사자매의 손", stage.Outro, 3, 4));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(55), 9, 2));
                scenes.Add(Segment("스스로 돌아온 사람 — 비류연과 나예린", stage.Outro, 7, stage.Outro.Length - 7));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(55), 11, 4));
                return scenes.ToArray();
            }
            if (stage.Number == 46)
            {
                scenes.Add(Segment("결승을 넘긴 뒤 — 위험한 환 해제와 무명검", StoryBriefings.Briefing(46), 5, 2));
                scenes.Add(Segment("미소저 전대의 우승", stage.Outro, 0, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(46), 0, 1));
                scenes.Add(Segment("우승 뒤 — 돌아온 제자 이벽한", stage.Outro, 1, 4));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(46), 1, 1));
                scenes.Add(Segment("피의 밤 증언을 끊은 독무", stage.Outro, 5, 3));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(46), 2, 1));
                scenes.Add(Segment("다른 대기실 — 암습과 납치", stage.Outro, 8, stage.Outro.Length - 8));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(46), 3, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 47)
            {
                scenes.Add(Segment("폭염진 — 예청이 연 퇴로", stage.Outro, 0, 2));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(47), 0, 1));
                scenes.Add(Segment("다른 추적 — 두노이의 마지막 말", stage.Outro, 2, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(47), 1, 1));
                scenes.Add(Segment("찾아낸 다음 목적지", StoryBriefings.Briefing(47), 7, 1));
                scenes.Add(Segment("신라각 — 반드시 구할 사람을 위한 도움", stage.Outro, 3, stage.Outro.Length - 3));
                scenes.Add(Segment("절반의 칼 — 빌림과 반환의 약속", StoryBriefings.Aftermath(47), 2, 2));
                return scenes.ToArray();
            }
            if (stage.Number == 48)
            {
                scenes.Add(Segment("서해도 첫 판 — 효룡의 패배와 작은 충격", stage.Outro, 0, 3));
                scenes.Add(Segment(null, StoryBriefings.Briefing(48), 6, 1));
                scenes.Add(Segment("운균 오 — 아직 받기 전인 다섯 공격", stage.Outro, 3, 2));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(48), 0, 1));
                scenes.Add(Segment(null, StoryBriefings.Briefing(48), 7, 1));
                scenes.Add(Segment("같은 밤 다른 섬 — 남아 있는 공방", stage.Outro, 5, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(48), 1, 1));
                scenes.Add(Segment("구출대가 듣지 못한 장막의 명령", stage.Outro, 6, stage.Outro.Length - 6));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(48), 2, 2));
                return scenes.ToArray();
            }
            if (stage.Number == 49)
            {
                scenes.Add(Segment("여덟 그림자 뒤 — 상쇄된 파문", StoryBriefings.Aftermath(49), 0, 1));
                scenes.Add(new StoryScene { Title = "죽음 대신 목관으로", Lines = stage.Outro });
                scenes.Add(Segment(null, StoryBriefings.Aftermath(49), 1, 3));
                return scenes.ToArray();
            }
            if (stage.Number == 50)
            {
                scenes.Add(Segment("동해도 — 환영을 걷어 내고 남긴 목숨", stage.Outro, 0, 4));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(50), 0, 2));
                scenes.Add(Segment("그가 모르는 관중과 아직 열지 않은 관", stage.Outro, 4, stage.Outro.Length - 4));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(50), 2, 3));
                return scenes.ToArray();
            }
            if (stage.Number == 41)
            {
                scenes.Add(Segment("현재 — 굴복한 백무후와 달의 이슬", stage.Outro, 0, 4));
                scenes.Add(Segment("백무후 등에 올라 신라각으로", StoryBriefings.Aftermath(41), 0, 2));
                scenes.Add(Segment("문 앞에서 — 다음에 남은 계약", StoryBriefings.Briefing(41), 6, 2));
                scenes.Add(Segment("방문 안 — 사부와 정신봉", stage.Outro, 4, stage.Outro.Length - 4));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(41), 2, 2));
                return scenes.ToArray();
            }
            if (stage.Number == 42)
            {
                scenes.Add(Segment("선착장 — 항복과 배 대금", stage.Outro, 0, 2));
                scenes.Add(Segment("우승 상금과 다른 정산", StoryBriefings.Aftermath(42), 0, 2));
                scenes.Add(Segment("멀리서 지켜본 사부", stage.Outro, 2, stage.Outro.Length - 2));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(42), 2, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 43)
            {
                scenes.Add(Segment("폭주 뒤 — 돌린 기류와 오른팔의 부상", stage.Outro, 0, 4));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(43), 0, 1));
                scenes.Add(Segment("부모 앞에서 밝힐 뜻", StoryBriefings.Briefing(43), 6, 1));
                scenes.Add(Segment("자기 뜻으로 날아갈 딸", stage.Outro, 4, 8));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(43), 1, 2));
                return scenes.ToArray();
            }
            if (stage.Number == 44)
            {
                scenes.Add(Segment("쓰러진 뒤 — 눈물과 화해", stage.Outro, 0, 1));
                scenes.Add(Segment("승패보다 남은 두 사람", StoryBriefings.Briefing(44), 7, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(44), 0, 1));
                scenes.Add(Segment("류은경에게 남은 길", stage.Outro, 1, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(44), 1, 1));
                scenes.Add(Segment("다음 준결승 — 몽환삼영", stage.Outro, 2, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(44), 2, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 45)
            {
                scenes.Add(Segment("마지막 검 뒤 — 쓰러진 사자매", stage.Outro, 0, 4));
                scenes.Add(Segment("아직 온전히 돌아오지 않은 사람", StoryBriefings.Aftermath(45), 0, 2));
                scenes.Add(Segment("결승에 오른 미소저 전대", stage.Outro, 4, stage.Outro.Length - 4));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(45), 2, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 37)
            {
                scenes.Add(Segment("회상 — 독니와 사부의 구조", stage.Outro, 0, 9));
                scenes.Add(Segment("회상 — 살아남은 새끼와 사흘의 해독", StoryBriefings.Aftermath(37), 0, 2));
                scenes.Add(Segment("현재로 — 오랜 꿈에서 깬 예린", stage.Outro, 9, stage.Outro.Length - 9));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(37), 2, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 38)
            {
                scenes.Add(Segment("선출을 마친 뒤 — 임시 대장의 책임", stage.Outro, 0, 3));
                scenes.Add(Segment(null, StoryBriefings.Briefing(38), 7, 1));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(38), 0, 1));
                scenes.Add(Segment("선출 다음 날 — 돌아온 백응", stage.Outro, 3, 1));
                scenes.Add(Segment("현재 — 십 년 전 구한 그 아이", EarlyGameContent.FindStage(37).Prologue[0].Lines, 1, 2));
                scenes.Add(Segment("현재 — 예린이 전한 사부 이야기", StoryBriefings.Aftermath(38), 1, 1));
                scenes.Add(Segment("같은 무렵 다른 곳 — 천무학관의 노사부", stage.Outro, 4, 8));
                scenes.Add(Segment("학관에서 표행으로 — 제자를 찾는 노인", StoryBriefings.Aftermath(38), 2, 2));
                scenes.Add(Segment("마천각으로 — 신고식과 대환영회", stage.Outro, 12, stage.Outro.Length - 12));
                return scenes.ToArray();
            }
            if (stage.Number == 39)
            {
                scenes.Add(Segment("돈왕과의 협상 — 신분과 참가 조건", stage.Outro, 0, 14));
                scenes.Add(Segment("합의 뒤 — 아직 받지 않은 상금", StoryBriefings.Aftermath(39), 0, 2));
                scenes.Add(Segment("두 사람이 듣지 못한 밀실의 보고", stage.Outro, 14, 4));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(39), 2, 1));
                scenes.Add(Segment("같은 밤 다른 일행 — 정전자의 목전", stage.Outro, 18, stage.Outro.Length - 18));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(39), 3, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 40)
            {
                scenes.Add(Segment("진법 돌파 뒤 — 아직 미완인 검기", stage.Outro, 0, 3));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(40), 0, 2));
                scenes.Add(Segment("다리를 떠나며", stage.Outro, 3, stage.Outro.Length - 3));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(40), 2, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 31)
            {
                scenes.Add(Segment("회상 — 은장이 남긴 상처와 노인의 여정", stage.Outro, 0, 10));
                scenes.Add(Segment("회상 — 철화장과 중양표국 방문", stage.Outro, 10, 4));
                scenes.Add(StoryBriefings.Interlude(32));
                scenes.Add(Segment("회상 — 남창행 표행에 합류하다", stage.Outro, 14, 15));
                scenes.Add(Segment("남매를 보호하는 표행", StoryBriefings.Aftermath(31), 0, 3));
                scenes.Add(Segment("그로부터 한 달 뒤 — 남창 도착", stage.Outro, 29, stage.Outro.Length - 29));
                scenes.Add(Segment("아직 닿지 못한 관주실", StoryBriefings.Aftermath(31), 3, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 32)
            {
                scenes.Add(Segment("첫 어검 체험 뒤 — 살아 있는 남궁상", StoryBriefings.Aftermath(32), 0, 3));
                scenes.Add(new StoryScene { Title = "보름의 수련을 앞두고", Lines = stage.Outro });
                scenes.Add(Segment(null, StoryBriefings.Aftermath(32), 3, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 34)
            {
                scenes.Add(Segment("도주 — 붙잡지 못한 증인", stage.Outro, 0, 6));
                scenes.Add(Segment("감옥에서 — 스스로 움직인 선택", StoryBriefings.Aftermath(34), 0, 3));
                scenes.Add(Segment("감옥 — 사식을 청하다", stage.Outro, 6, 7));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(34), 3, 1));
                scenes.Add(Segment("이튿날 밤 — 살아 돌아온 두 사람", stage.Outro, 13, stage.Outro.Length - 13));
                return scenes.ToArray();
            }
            if (stage.Number == 35)
            {
                scenes.Add(Segment("같은 시각 — 남매의 방과 백무후", stage.Outro, 2, 10));
                scenes.Add(new StoryScene { Title = "포효가 멎은 뒤 — 생존자의 목소리", Lines = StoryBriefings.Aftermath(35) });
                scenes.Add(Segment("돌아온 고수들 — 관주를 향한 길", stage.Outro, 12, stage.Outro.Length - 12));
                return scenes.ToArray();
            }
            if (stage.Number == 36)
            {
                scenes.Add(Segment("처음 펼친 이기어검 — 제한 비무의 인정", stage.Outro, 0, 5));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(36), 0, 2));
                scenes.Add(Segment("그 뒤 — 승천무제 본선 심사", stage.Outro, 5, 3));
                var departure = StoryBriefings.Interlude(36);
                scenes.Add(Segment("같은 무렵 — 다른 나루", departure.Lines, 5, departure.Lines.Length - 5));
                scenes.Add(Segment("동정호 자죽도 — 영령의 입관 요청", stage.Outro, 8, 1));
                scenes.Add(Segment("출발을 앞두고 — 빠진 이름과 상자", stage.Outro, 9, 3));
                scenes.Add(Segment("떠나는 효룡과 배웅하는 이들", departure.Lines, 0, 5));
                scenes.Add(Segment("사절단의 아침 — 길 위의 재회", stage.Outro, 12, stage.Outro.Length - 12));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(36), 2, 2));
                return scenes.ToArray();
            }
            if (stage.Number == 25)
            {
                scenes.Add(new StoryScene { Lines = stage.Outro });
                scenes.Add(Segment("교대를 청하다", StoryBriefings.Aftermath(25), 2, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 26)
            {
                scenes.Add(Segment("개방 분타 — 부상을 확인하다", stage.Outro, 0, 5));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(26), 0, 3));
                scenes.Add(Segment("묘시 점검을 앞두고", stage.Outro, 5, 2));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(26), 3, 1));
                scenes.Add(Segment("새벽 귀환 — 호환의 증거", stage.Outro, 7, 2));
                scenes.Add(StoryBriefings.Interlude(26));
                scenes.Add(Segment("그 뒤 — 소각된 화평장", stage.Outro, 10, stage.Outro.Length - 10));
                return scenes.ToArray();
            }
            if (stage.Number == 27)
            {
                scenes.Add(Segment("칠걸의 공세 뒤 — 재회와 이름", stage.Outro, 0, 22));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(27), 0, 3));
                scenes.Add(Segment("남들이 듣지 못한 사적 알현", stage.Outro, 22, 4));
                scenes.Add(Segment("개회식 — 우정·화합·평화", stage.Outro, 26, 5));
                scenes.Add(Segment("공동 숙소 — 첫 밤", stage.Outro, 31, 1));
                scenes.Add(StoryBriefings.Interlude(27));
                scenes.Add(Segment("일주일 뒤 — 조 추첨", stage.Outro, 32, stage.Outro.Length - 32));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(27), 3, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 28)
            {
                scenes.Add(new StoryScene { Title = "객잔 — 아직 살아 있는 증인", Lines = StoryBriefings.Aftermath(28) });
                scenes.Add(Segment("응급조치 뒤 — 환자를 옮길 사람", stage.Outro, 0, 10));
                return scenes.ToArray();
            }
            if (stage.Number == 29)
            {
                scenes.Add(Segment("동굴 — 독자에게 드러난 살해", stage.Outro, 0, 5));
                scenes.Add(Segment("일행이 돌아온 뒤 — 천율패의 누명", stage.Outro, 5, 5));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(29), 0, 1));
                scenes.Add(Segment("숲 어귀 — 동료의 선택", stage.Outro, 10, stage.Outro.Length - 10));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(29), 1, 3));
                return scenes.ToArray();
            }
            if (stage.Number == 30)
            {
                // Hard difficulties prepend their wind strike; keep that lead-in before the shared epilogue.
                int lead = stage.Outro.Length - StoryMidgame.Outros[30].Length;
                if (lead > 0) scenes.Add(Segment("풍신 — 불길을 걷어 내다", stage.Outro, 0, lead));
                scenes.Add(Segment("화재 뒤 — 희생과 인솔자의 책임", stage.Outro, lead, 11));
                scenes.Add(StoryBriefings.Interlude(30));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(30), 0, 2));
                scenes.Add(Segment("폐허에 남은 전승과 떠난 이들", stage.Outro, lead + 11, 26));
                scenes.Add(Segment(null, StoryBriefings.Aftermath(30), 2, 2));
                scenes.Add(Segment("같은 무렵 — 불타는 청룡은장", stage.Outro, lead + 37, stage.Outro.Length - lead - 37));
                return scenes.ToArray();
            }
            if (stage.Number == 23)
            {
                scenes.Add(new StoryScene { Lines = stage.Outro });
                scenes.Add(Segment(null, StoryBriefings.Aftermath(23), 2, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 22)
            {
                scenes.Add(Segment(null, stage.Outro, 0, 2));
                scenes.Add(Segment(null, stage.Outro, 5, stage.Outro.Length - 5));
                scenes.Add(new StoryScene { Lines = StoryBriefings.Aftermath(22) });
                return scenes.ToArray();
            }
            if (stage.Number == 24)
            {
                var maskAftermath = StoryBriefings.Aftermath(24);
                scenes.Add(Segment(null, stage.Outro, 0, stage.Outro.Length - 1));
                scenes.Add(Segment("매화가면을 알아본 제자", maskAftermath, 0, maskAftermath.Length - 1));
                scenes.Add(Segment("다음 날 — 회색 장삼의 노인", stage.Outro, stage.Outro.Length - 1, 1));
                scenes.Add(Segment("천무봉의 샛길로", maskAftermath, maskAftermath.Length - 1, 1));
                return scenes.ToArray();
            }
            if (stage.Number == 14)
            {
                scenes.Add(Segment(null, stage.Outro, 0, 21));
                scenes.Add(new StoryScene { Title = "합숙지 — 형을 보낸 뒤", Lines = StoryBriefings.Aftermath(14) });
                scenes.Add(Segment("그 뒤 — 학생들의 신병을 요구하다", stage.Outro, 21, stage.Outro.Length - 21));
                return scenes.ToArray();
            }
            if (stage.Number == 15)
            {
                scenes.Add(Segment(null, stage.Outro, 0, 4));
                scenes.Add(new StoryScene { Title = "회상 — 철수 뒤 열린 귀환길", Lines = StoryBriefings.Aftermath(15) });
                scenes.Add(Segment("현재 — 진혼제 뒤의 청흔", stage.Outro, 4, stage.Outro.Length - 4));
                return scenes.ToArray();
            }
            if (stage.Number == 11) scenes.Add(Segment(null, stage.Outro, 0, 5));
            else if (stage.Outro != null) scenes.Add(new StoryScene { Lines = stage.Outro, HeroArtKey = stage.OutroHeroArtKey });
            var aftermath = StoryBriefings.Aftermath(stage.Number);
            if (stage.Number == 10) scenes.Add(Segment(null, aftermath, 0, 2));
            else if (aftermath != null) scenes.Add(new StoryScene { Lines = aftermath, HeroArtKey = stage.OutroHeroArtKey });
            return scenes.ToArray();
        }

        private static StoryScene Segment(string title, string[] source, int start, int count)
        {
            var lines = new string[count];
            System.Array.Copy(source, start, lines, 0, count);
            return new StoryScene { Title = title, Lines = lines };
        }
    }
}
