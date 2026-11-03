using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>The first thirty-six chapters share one detailed timeline between battle and the record.</summary>
    public static class EarlyStory
    {
        public static bool Supports(StageData stage)
        {
            return stage != null && !stage.IsQuest && !stage.IsTower && stage.Number >= 1 && stage.Number <= 36;
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

        /// <summary>Place background passages after the event they describe, sharing this order with battle and replay.</summary>
        public static StoryScene[] ArrangeBeforeFight(int stageNumber, int encounter, StoryScene[] authored)
        {
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
