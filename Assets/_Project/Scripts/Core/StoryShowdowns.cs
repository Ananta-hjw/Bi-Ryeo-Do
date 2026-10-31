namespace BiRyeoDo.Core
{
    /// <summary>
    /// Scripted showdowns (<see cref="TrialKind.Finale"/>) other than 일반 30 (<see cref="FireDragonFinale"/>).
    /// 43 「쌍월야와 십이야」 (23권): 예청's 십이야 presses 연비's left-hand guard; his right hand takes the umbrella and 현천포월 breaks out,
    /// 나예린 throws herself in front of her mother, and he strikes his own right shoulder to turn the lightning into the ground.
    /// Story/analysis/vol23.md 「장모님 습격 사건」.
    /// 46 「천외일도」 (24권): the 심도 lands however far 연비 dodges; he unbinds the wrist 봉황환 against his master's warning, wakes from the dark,
    /// draws the 무명검 and ends it with 묵뢰살, sparing 칠상흔. Story/analysis/vol24.md 「결승 — 칠상흔 대 연비」. Lines marked 연출 below are game wording.
    /// </summary>
    public static class StoryShowdowns
    {
        public static readonly ShowdownScript Cheonoe = new ShowdownScript
        {
            Title = "천외일도",
            AfterBlow = new[]
            {
                "천외일도는 아무리 피해도 거리가 줄지 않는 심도였다.",
                "관중석의 혁중은 열세 번째 도를 완성했느냐는 물음에 한마디만 남겼다.",
                "혁중|비밀일세.",
                "결승에 나서기 전 혁중이 건넨 말이 떠올랐다.",
                "혁중|반드시 이기게, 연비 '군'!",
                "그리고 사부의 경고도.",
                "사부|남은 묵룡환은 절대 풀지 마라. 죽는다.",
                "연비|……안 풀면 지금 죽을 것 같은데요.",   // 연출
                "흙먼지로 몸을 가린 연비가 손목에 남은 봉황환을 풀었다. 폭주하는 흐름을 거스르지 않고 길을 내 주자, 흐름이 머리끝까지 차올라 눈앞이 캄캄해졌다.",
                "깨어나 보니 시간은 거의 흐르지 않았다.",
                "현천은린 우산대에서 은사 문양의 흑검이 나왔다. 사부가 나뭇가지로 보여 주던 형태 없는 검, 무명검이었다.",
            },
            IconArtId = "scene_mukroesal", IconName = "무명검 · 묵뢰살", Hint = "묵뢰살 아이콘을 눌러 펼치세요",
            SkillId = "scene_mukroesal_finale", SkillName = "무명검 오의 · 묵뢰살",
            BlowName = "천외일도",
            FirstBlowQuip = "연비: 「……피했는데 왜 맞는 거야?」",   // 연출
            BlowQuip = "거리가 줄지 않는다!",
            NoHarmQuip = "심도에 닿지 않는다.",
            AfterStrike = new[]
            {
                "검극과 도극이 한 점에서 부딪쳤다. 확신이 깨진 칠상흔을 흑과 은의 뇌광이 삼켰다.",
                "관중|죽여라! 죽여라!",
                "연비|내가 왜 너희 재미를 위해 살인자가 돼야 하지?",
                "관중석의 노사부가 중얼거렸다.",
                "사부|저, 바보 자식.",
                "연비|……칭찬이죠?",   // 연출
            },
            EnemyLeaves = true,
        };

        public static readonly TrialRule CheonoeTrial = Cheonoe46();

        /// <summary>
        /// 20 「매몰 44일」 (10권): the first 비뢰도 bounces off the old scales, and 섬뢰창 goes into the opened mouth.
        /// Story/analysis/vol10.md 「묵린혈망전과 다시 막힌 길」. The 25% force, the three seconds and the guidance lines are game terms.
        /// </summary>
        public static readonly ShowdownScript Hyeolmang = new ShowdownScript
        {
            Title = "묵린혈망",
            ArmorLines = new[] { "나예린이 영물도감의 지식으로 설명했다. 팔백 년 가까이 산 영물의 오래된 비늘은 단단하고 탄력이 있어, 웬만한 힘은 그대로 튕겨 냈다." },
            FirstNoHarmQuip = "비늘에 튕긴다!",
            NoHarmQuip = "검은 비늘에 막힌다.",
            IconArtId = "saengsa_seomroe", IconName = "생사결 섬뢰창", Hint = "입이 열렸다! 지금!",
            SkillId = "scene_seomroechang_finale", SkillName = "비뢰도 오의 검기 · 생사결 섬뢰창",
            WindowSeconds = 3f,
        };

        public static readonly TrialRule HyeolmangTrial = TrialRule.Finale("묵린혈망", "검은 비늘은 공격을 튕겨 냅니다. 혈망이 침을 모아 입을 여는 순간을 노리세요.", Hyeolmang, FinaleStyle.Window);

        /// <summary>
        /// 24 「떨어지는 동전」 (12권): 동방학's 쾌 against 비류연's, the coin 염도 throws is the signal; 섬뢰 through the brow, the shirt cut in a cross.
        /// Lines are the original's (Story/raw/vol12, 3,080~3,200행, shortened); 「아직!」 and 「늦었다!」 are game wording.
        /// </summary>
        public static readonly ShowdownScript Coin = new ShowdownScript
        {
            Title = "떨어지는 동전",
            AfterBlow = new[]
            {
                "비류연|할아버지께서는 뢰(雷)의 어느 얼굴을 추구하고 계시죠?",
                "동방학|나는 빛을 뒤쫓는 쾌(快)를 추구한다!",
                "비류연|저의 쾌로 노인장의 쾌를 맞상대해 드리죠.",
                "동방학|건방지구나!",
                "비류연|보통이죠.",
                "찰칵! 오른팔과 오른다리에서 두 개의 묵룡환이 풀려 땅바닥에 떨어졌다. 쿵!",
                "비류연|이 동전에 떨어지는 걸로 신호를 삼죠!",
                "비류연이 주머니에서 동전 하나를 꺼내 염도에게 건넸다.",
                "동방학|그냥 자결하는 게 시간도 절약하고 더 좋지 않겠느냐?",
                "비류연|그럼 이제 던지죠!",
            },
            IconArtId = "seomroe", IconName = "섬뢰(閃雷)", Hint = "동전이 땅에 닿는 순간 누르세요",
            EarlyText = "아직!", LateText = "늦었다!",
            LateQuip = "붉은 검이 먼저 닿았다!", LastQuip = "붉은 검이 먼저 닿았다……",
            BlowName = "붉은 검",
            SkillId = "scene_seomroe_finale", SkillName = "비뢰도 오의 · 섬뢰", QuietStrike = true,
            AfterStrike = new[]
            {
                "빛과 생사가 한 순간 한 자리에서 교차했다. 아무도 두 사람의 초식 교환을 보지 못했다.",
                "염도|봤나?",
                "빙검|못 봤네!",
                "동방학의 광소가 터져 나왔다가 거짓말처럼 뚝 멎었다. 두 사람은 어느새 서로의 자리에 서 있었다.",
                "동방학|……시험 결과는 어땠나?",
                "비류연|그 정도 빠르기면 쓸만하군요.",
                "투두둑! 비류연의 앞섶이 열십자 모양으로 잘려 나갔다.",
                "비류연|두 번의 변화가 아니라 그냥 한 번이면 좋았을 것을, 아쉽군요.",
                "동방학|합격인가?",
                "비류연|당신의 명호에 들어간 뇌의 이름을 인정하죠.",
                "동방학|방금 그 초식의 이름이 무엇인가?",
                "비류연|섬뢰(閃雷)!",
                "동방학|나의 이름으로 날 깨트렸다는 이야긴가? 더할 나위 없는 완벽한 패배로군.",
                "미간에서 붉은 선이 흘러내렸다. 동방학의 몸이 그대로 앞으로 고꾸라졌다.",
            },
        };

        public static readonly TrialRule CoinTrial = TrialRule.Duel("떨어지는 동전", "비류연 혼자 겨룹니다. 동전이 땅에 닿는 순간 섬뢰를 누르세요. 늦으면 체력을 잃고, 다섯 번 늦으면 패배합니다.", Coin);

        public static readonly ShowdownScript Moon = new ShowdownScript
        {
            Title = "현천포월",
            AfterBlow = new[]
            {
                "왼손의 수비가 한계에 닿았다. 연비의 오른손이 마침내 현천은린을 쥐었다.",
                "눈이 황금빛으로 타올랐다.",
            },
            IconArtId = "hyeoncheon_powol", IconName = "현천포월", Hint = "현천포월 아이콘을 눌러 펼치세요",
            CutInName = "현천은린 오의 · 현천포월",
            InterruptLines = new[]
            {
                "수백 줄기의 검은 번개가 터져 나왔다. 옆에서 찔러 든 나백천의 검기마저 깨졌다.",
                "나예린이 어머니 앞에 뛰어들었다!",
            },
            SecondIconArtId = "scene_redirect", SecondIconName = "오른쪽 어깨 혈도", SecondHint = "아이콘을 눌러 번개의 방향을 바꾸세요",
            SkillId = "scene_redirect_finale", SkillName = "오른쪽 어깨 혈도 · 기류를 땅으로",
            BlowName = "십이야",
            FirstBlowQuip = "연비: 「……왼손만으로는 좀 버거운데요.」",   // 연출
            BlowQuip = "십이야가 왼손의 수비를 파고든다!",
            NoHarmQuip = "상대는 장모님이다.",   // 연출
            StrikeHarmless = true, SelfCostPct = 0.3f, StrikeQuip = "검은 번개가 땅으로 꺾였다!",
            AfterStrike = new[]
            {
                "연비가 왼손으로 오른쪽 어깨의 혈도를 쳤다. 검은 번개가 땅으로 꺾여 꽂혔다.",
                "나백천이 날아가는 예청을 받아 내며 벽에 등을 박았다.",
                "연비의 오른팔은 근맥이 끊어질 뻔했다. 당분간 우산은 왼손으로 들어야 했다.",
                "예청은 이십 년 만에 죽음의 공포를 느꼈다. 그리고 패배를 인정했다.",
            },
            EnemyLeaves = true,
        };

        public static readonly TrialRule MoonTrial = Moon43();

        private static TrialRule Moon43()
        {
            var t = TrialRule.Finale("쌍월야와 십이야", "왼손만으로 막아야 합니다. 버티며 기회를 기다리세요.", Moon, FinaleStyle.Unavoidable);
            t.FinaleTurns = 3;
            t.BlowPct = 0.15f;
            t.TriggerHpPct = 0.3f;
            return t;
        }

        private static TrialRule Cheonoe46()
        {
            var t = TrialRule.Finale("천외일도", "피해도 거리가 줄지 않는 심도입니다. 버티며 기회를 기다리세요.", Cheonoe, FinaleStyle.Unavoidable);
            t.FinaleTurns = 3;
            t.BlowPct = 0.2f;
            t.TriggerHpPct = 0.3f;
            return t;
        }
    }
}
