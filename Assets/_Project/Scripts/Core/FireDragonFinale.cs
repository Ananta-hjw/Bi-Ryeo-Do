namespace BiRyeoDo.Core
{
    /// <summary>
    /// 일반 30 「화룡멸겁대진」 (16권): 대공자 비 holds at 1 HP and leaves with 독고령, the 진 fells the party in one blow,
    /// the four masters lend 비류연 their 내력 and his 풍신 (풍뢰의 장 · 나선의 인 · 용권승룡) ends it.
    /// 어려움·극악 keep the earlier four fights (<see cref="UseLegacy"/>). Dialogue is taken from Story/raw/vol16.txt;
    /// lines starting with <see cref="StoryLine.InventedMark"/> are written for the game.
    /// </summary>
    public static class FireDragonFinale
    {
        public const int Stage = 30;
        public const int FinaleFight = 2;
        public const string BiId = "bi_daegongja", JinId = "hwaryong_daejin";

        public static readonly string[][] Encounters =
        {
            new[] { "ma_chilgeol_jeoji", "ma_chilgeol_jeoji", "ma_chilgeol_jeoji" }, new[] { BiId }, new[] { JinId },
        };

        /// <summary>The finale fight pays what the two fights it replaced paid (exp, 두 and drops).</summary>
        public static readonly string[][] RewardEncounters =
        {
            new[] { "ma_chilgeol_jeoji", "ma_chilgeol_jeoji", "ma_chilgeol_jeoji" }, new[] { BiId }, new[] { "hwaryong_hwayeom", "ma_chilgeol_jeoji", "hwaryong_rescue" },
        };

        /// <summary>The fights before this change, still played on 어려움·극악 and referenced by the 화재 속 엄호 quest.</summary>
        public static readonly string[][] LegacyEncounters =
        {
            new[] { "ma_chilgeol_jeoji", "ma_chilgeol_jeoji", "ma_chilgeol_jeoji" }, new[] { BiId }, new[] { "hwaryong_hwayeom", "ma_chilgeol_jeoji" }, new[] { "hwaryong_rescue" },
        };

        public static readonly TrialRule[] LegacyTrials =
        {
            null, null, null, TrialRule.Survive("풍신을 위한 엄호", "적 6행동을 견디며 모인 사람들을 보호하세요. 이후 네 고수의 지원과 풍신으로 이어집니다.", 6),
        };

        public static bool Is(StageData stage) { return stage != null && stage.Number == Stage && !stage.IsQuest && !stage.IsTower && stage.Difficulty == 0; }
        public static bool IsFinale(StageData stage, int fight) { return Is(stage) && fight == FinaleFight; }

        /// <summary>The same stage with the earlier four fights and the full Outro (어려움·극악).</summary>
        public static void UseLegacy(StageData stage)
        {
            stage.Encounters = LegacyEncounters;
            stage.Trials = LegacyTrials;
            stage.RewardEncounters = null;
            stage.Outro = HardOutro();
        }

        /// <summary>일반 30's 비: never below 1 HP; the blow that would fell him makes him leave.</summary>
        /// <summary>어려움·극악's 비 leaves at his usual HP with the same words.</summary>
        public static EnemyData Leaving(EnemyData bi)
        {
            var c = bi.Scaled(1f, 1f);
            c.RetreatLines = RetreatLines;
            return c;
        }

        public static EnemyData LastStand(EnemyData bi)
        {
            var c = bi.Scaled(1f, 1f);
            c.RetreatHpPct = 0f;
            c.HoldsAtOneHp = true;
            c.RetreatLines = RetreatLines;
            return c;
        }

        /// <summary>비's words as he breaks off (vol16): 비류연 dodged the 영뢰 잔흔 he had seen once before.</summary>
        public static readonly string[] RetreatLines =
        {
            "대공자 비|설마, 모조리 피해 내다니? 그때보다 세 배는 더 강력한 공격이었는데!",
            "비류연|한 번 본 기술에 또 당할 거라고 생각했나? 역시 그때 어둠 저편의 존재는 당신이었던 모양이군.",
            "비류연|그럼 이제 방화, 폭파, 살인 교사에 납치까지 죄목을 추가해야겠네요.",
            "대공자 비|미안하지만 더 이상 놀아 줄 수 없겠군. 시간이 다 됐거든! 용이 날아오를 시간이다!",
        };

        /// <summary>The 진's second stage: 비 leaves with 독고령 and the valley is sealed (일반 before the finale, 어려움·극악 before fight 3).</summary>
        public static readonly string[] SealedLines =
        {
            "화룡멸겁대진 제이 단계. 용린을 타고 달리는 불꽃이 꼬리를 물며 거대한 불의 성벽이 되어 사람들을 가두었다.",
            "비는 순식간에 독고령을 제압해 들쳐 메고 찰나의 생로로 빠져나갔다. 뒤쫓으려던 비류연은 마천칠걸에게 발이 묶였다.",
            "홍매곡의 입구가 천둥 같은 소리와 함께 무너져 내렸다.",
            "+장홍|입구가……! 완전히 갇혔어!",
        };

        /// <summary>Before the finale fight: the valley is sealed.</summary>
        public static readonly StoryScene[] Departure =
        {
            new StoryScene { Title = "불의 성벽", Lines = SealedLines },
        };

        /// <summary>No way out: 비류연 asks the four masters for their 내력 and the prize (vol16).</summary>
        public static readonly string[] Plea =
        {
            "불의 장막과 항아리 같은 계곡 탓에 달아오른 공기는 빠져나가지 못했다. 단 하나뿐인 비상 탈출구마저 폭발로 막혀 있었다.",
            "누군가|이대로 다 죽어야 하나?",
            "누군가|정말 아무런 방법이 없단 말인가?",
            "비류연|모두 한곳으로 모여요! 아직 방법은 있어요!",
            "검성|소형제, 정말 방법이 있단 말인가? 우리 삼성조차도 아직 방법을 찾지 못하고 고민하고 있는데?",
            "비류연|물론 있죠. 하지만 그 방법은 막대한 내력을 소진하는 일이라 여러분의 도움이 필요해요. 아직 혼자 힘으로는 완벽하게 구현해 낼 자신이 없거든요. 좀 빌어먹을 기술이라…….",
            "혁중|정말 그것이 가능하겠는가? 정말 이 상황을 타개할 만한 기가 존재한단 말인가?",
            "비류연|물론 확실히 제 몸속에 그건 존재하고 있어요.",
            "혁중|소형제의 말이 사실이라면 우리는 우리가 할 수 있는 모든 조력을 아끼지 않겠네.",
            "비류연|근데, 그 대신이라고 하긴 좀 그렇고, 뭐 물어볼 말이 있는데요? 만일 제 모험이 성공한다면…….",
            "혁중|성공한다면?",
            "비류연|이 화산지회의 우승자는 누가 되는 거죠?",
            "혁중|……만일 성공만 한다면 두말할 것도 없이 자네가 이 화산지회의 승자가 되는 것일세. 모든 명예와 부상과 자격과 권리가 주어질 것이네. 물론 자네의 배당금과 함께 말일세.",
            "비류연|그렇다면 절대 실패할 수 없겠군요. 그럼 우선…….",
            "비류연이 나예린을 바라보았다. 말하지 않아도 그 눈빛의 뜻을 알 수 있었다. 성공을 위한 부적, 여신의 입맞춤.",
            "나예린|(지금 이런 상황에서? 이렇게 많은 사람 앞에서? 사부님까지 보고 계신데……. 진짜로 해야 돼요?)",
            "비류연은 묵묵히 고개를 끄덕였다.",
            "나예린|후우……. (이 사람을 믿자. 그리고 살자!)",
            "그녀가 다가가자 비류연이 왼손으로 허리를 감아 끌어당기고 입을 맞췄다. 누구도 입을 열지 못했다.",
            "비류연|이게 효과가 끝내주거든요. 이제 절대 실패하지 않아요.",
            "검후|어흠. 좋은 배짱이다. 안 그래도 뜨거운데 더 뜨겁게 만들어 주는구나. 정말 내 제자가 이런 애였을 줄이야…….",
            "검후|뭘 그렇게 뜸을 들이느냐? 냉큼 시작하지 않고. 뭣하면 내가 대신 또 한 번 해 주랴?",
            "도성|조심하게, 소년! 백 년 묵은 할망구의 입술에 당하면 중독사할지도 몰라.",
            "여기저기서 웃음이 터져 나왔다. 희망을 되찾은 웃음이었다.",
            "철컹, 철컹! 묵룡환이 풀려 나가고, 열 자루의 비뢰도와 뇌령사가 나선을 그리며 그의 주위를 돌기 시작했다.",
            "비류연|(먼저 원을 그리고…… 하늘과 땅 사이에 그려진 원의 내부를, 두 개의 힘이 나선을 그리며 달린다.)",
            "천무삼성과 혁중이 내력을 보탰다. 그 순간 화룡멸겁대진이 마지막 용트림을 하며 모든 힘을 폭발시켰다.",
        };

        /// <summary>After the 진's blow (일반): only 비류연 stands, then the plea, the kiss and the 풍신 begins.</summary>
        public static readonly string[] AfterBlow = Concat(new[] { "+단 한 번의 화염이 일행을 휩쓸었다. 비류연만이 간신히 버티고 서 있었다." }, Plea);

        /// <summary>After 용권승룡: the fire dies and 비류연 faints in 나예린's arms (vol16).</summary>
        public static readonly string[] AfterStrike =
        {
            "불길이 바람의 기둥을 타고 하늘로 끝없이 밀려 올라갔다. 멀리서 보면 붉은 용이 승천하는 것 같았다. 그리고 불꽃은 단번에 꺼져 버렸다.",
            "살아남은 사람들로부터 기쁨의 함성이 터져 나왔다.",
            "나예린|살았군요. 살았어요, 류연!",
            "비류연|그렇네요.",
            "그 말을 끝으로 비류연은 정신을 잃고 나예린의 품속으로 쓰러졌다.",
            "나예린|류연! 류연!",
            "나예린|정말 어쩔 수 없는 사람이라니깐…….",
            "나예린|수고하셨어요, 류연.",
        };

        /// <summary>어려움·극악's Outro opens with the strike the survive fight held the line for.</summary>
        public static readonly string[] HardOutroHead = Concat(new[] { "비뢰도 최종비전오의 — 풍뢰의 장, 나선의 인, 용권승룡." }, AfterStrike);

        private static string[] Concat(string[] a, string[] b)
        {
            var all = new string[a.Length + b.Length];
            a.CopyTo(all, 0);
            b.CopyTo(all, a.Length);
            return all;
        }

        /// <summary>The scene, the 풍신 button and its cut-in (declared after the lines it uses).</summary>
        public static readonly ShowdownScript Script = new ShowdownScript
        {
            Title = "화룡멸겁대진", AfterBlow = AfterBlow, AfterStrike = AfterStrike, HeroCollapses = true,
            IconArtId = "pungsin", IconName = "풍신(風神)", Hint = "풍신 아이콘을 눌러 펼치세요",
            SkillId = InteractiveBattle.FinaleSkillId, SkillName = InteractiveBattle.FinaleSkillName, BlowName = InteractiveBattle.FinaleBlowName,
        };

        public static readonly TrialRule[] Trials =
        {
            null, null, TrialRule.Finale("화룡멸겁대진", "불의 성벽이 모두를 가둡니다. 먼저 움직일 수 있는 만큼 움직이세요.", Script),
        };

        /// <summary>일반's Outro: the 풍신 and the collapse already played in the fight, so it is the epilogue alone.</summary>
        public static string[] NormalOutro() { return StoryMidgame.Outros[Stage]; }

        /// <summary>어려움·극악's Outro: the strike and the collapse, then the epilogue.</summary>
        public static string[] HardOutro() { return Concat(HardOutroHead, StoryMidgame.Outros[Stage]); }
    }
}
