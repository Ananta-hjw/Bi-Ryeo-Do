using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>The first eighteen chapters share one detailed timeline between battle and the record.</summary>
    public static class EarlyStory
    {
        public static bool Supports(StageData stage)
        {
            return stage != null && !stage.IsQuest && !stage.IsTower && stage.Number >= 1 && stage.Number <= 18;
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
            return null;
        }

        /// <summary>Place background passages after the event they describe, sharing this order with battle and replay.</summary>
        public static StoryScene[] ArrangeBeforeFight(int stageNumber, int encounter, StoryScene[] authored)
        {
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
