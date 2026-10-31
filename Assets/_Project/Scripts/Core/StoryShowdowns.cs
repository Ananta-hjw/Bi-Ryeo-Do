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
