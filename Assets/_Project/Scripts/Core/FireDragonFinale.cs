namespace BiRyeoDo.Core
{
    /// <summary>
    /// 일반 30 「화룡멸겁대진」 (16권): 대공자 비 holds at 1 HP and leaves with 독고령, the 진 fells the party in one blow,
    /// the four masters lend 비류연 their 내력 and his 풍신 (풍뢰의 장 · 나선의 인 · 용권승룡) ends it.
    /// 어려움·극악 keep the earlier four fights (<see cref="Legacy"/>). Lines follow Story/analysis/vol16.md; the masters' argument
    /// and its wording are the user's direction for the game, not the original text.
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

        public static readonly TrialRule[] Trials =
        {
            null, null, TrialRule.Finale("화룡멸겁대진", "불의 성벽이 모두를 가둡니다. 먼저 움직일 수 있는 만큼 움직이세요."),
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
            stage.Outro = StoryMidgame.Outros[Stage];
        }

        /// <summary>일반 30's 비: never below 1 HP; the blow that would fell him makes him leave.</summary>
        public static EnemyData LastStand(EnemyData bi)
        {
            var c = bi.Scaled(1f, 1f);
            c.RetreatHpPct = 0f;
            c.HoldsAtOneHp = true;
            c.RetreatLines = RetreatLines;
            return c;
        }

        public static readonly string[] RetreatLines = { "대공자 비|칫... 귀찮게 하는군." };

        /// <summary>Before the finale fight: 비 sets off the 진 and leaves with 독고령.</summary>
        public static readonly StoryScene[] Departure =
        {
            new StoryScene { Title = "화룡멸겁대진", Lines = new[] {
                "대공자 비|화룡멸겁대진을 발동해라.",
                "비는 독고령을 낚아채 그대로 빠져나갔다. 뒤쫓으려는 길은 마천칠걸이 가로막았다.",
                "열두 방위에서 불기둥이 치솟으며 화룡멸겁대진이 발동했다. 불의 성벽이 사방을 두르고, 입구와 탈출구가 한꺼번에 무너져 내렸다.",
            } },
        };

        /// <summary>After the 진's blow: the panic, the request for 내력, the masters' argument, the 묵룡환 and the kiss.</summary>
        public static readonly string[] AfterBlow =
        {
            "단 한 번의 화염에 모두가 쓰러졌다. 비류연 혼자 간신히 버티고 서 있었다.",
            "나예린|류연……! 불길이 사방에서 좁혀 와요.",
            "효룡|출구가 전부 무너졌어! 이대로는 다 같이 갇혀 버린다고!",
            "윤준호|검기로 끊어 봐도 소용이 없어. 이건 보통 불이 아니야.",
            "장홍|살아남은 사람들까지 전부 여기 갇혔어. 어떻게든 해야 하는데…….",
            "비류연|....안되겠네, 저 죄송한데, 저한테 내력을 보태줄 수 있어요?",
            "검성|혼자 이 진을 감당하겠다는 겐가? 노부는 쉬이 믿기 어렵네.",
            "도성|허허, 검성. 이 아이가 지금껏 허투루 말한 적이 있던가요? 나는 믿어 보겠소.",
            "검후|저 아이의 눈을 보세요. 허세가 아닙니다. 저도 힘을 보태겠어요.",
            "혁중|내력을 남에게 넘기는 일일세. 그르치면 우리까지 함께 무너지네. 서두를 일이 아니야.",
            "검성|혁중의 말이 옳소. 바닥을 알 수 없는 수법에 우리 넷의 내력을 맡길 수는 없지.",
            "도성|그럼 여기서 다 같이 타 죽자는 말씀이시오? 그쪽이야말로 바닥이 없는 선택이지요.",
            "검후|두 분, 언성을 낮추시지요. 우리가 다투는 동안에도 불길은 기다려 주지 않습니다.",
            "혁중|……알았네, 내력을 보태지.",
            "모든 묵룡환을 푼다.",
            "비류연|아, 하나만 더요. 이거 성공하면 우승은 저희 조 거죠? 명예랑 부상이랑, 배당금까지요.",
            "혁중|……이 와중에? 성공만 하면 전부 주지.",
            "비류연|그리고 성공하려면 부적이 하나 필요한데요. 여신의 입맞춤이요.",
            "나예린|……지금, 여기서요? 다들 보고 있고, 사부님도 계신데…….",
            "나예린|……믿을게요. 그러니까 꼭 해내요.",
            "나예린이 다가오자 비류연이 그녀의 허리를 감아 입을 맞췄다.",
            "비류연|자, 이제 좀 할 만하겠네.",
        };

        /// <summary>After the 풍신: the 진 is gone and 비류연 collapses.</summary>
        public static readonly string[] AfterStrike =
        {
            "소용돌이가 불길째 하늘로 말려 올라가고, 화룡멸겁대진이 꺼졌다.",
            "비류연|하.. 죽겠네",
        };

        /// <summary>Resume point of a stage: a 일반 30 run saved before its last two fights became the finale picks up at the finale.</summary>
        public static int ResumeAt(StageData stage, int next)
        {
            int at = System.Math.Min(next, stage.Encounters.Length);
            return Is(stage) && next > FinaleFight ? FinaleFight : at;
        }

        /// <summary>일반's Outro: the 풍신, the kiss and the collapse already played in the fight, so it starts where 나예린 holds him.</summary>
        public static string[] NormalOutro()
        {
            var all = StoryMidgame.Outros[Stage];
            var rest = new string[all.Length - PlayedOutroLines];
            System.Array.Copy(all, PlayedOutroLines, rest, 0, rest.Length);
            return rest;
        }

        private const int PlayedOutroLines = 4;
    }
}
