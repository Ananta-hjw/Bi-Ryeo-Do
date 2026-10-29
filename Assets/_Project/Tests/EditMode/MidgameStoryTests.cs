using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class MidgameStoryTests
    {
        private static string Replay(int number) { return string.Join(" ", StoryEpisodes.Replay(EarlyGameContent.FindStage(number))); }
        private static void Ordered(string text, params string[] markers)
        {
            int previous = -1;
            foreach (var marker in markers)
            {
                int position = text.IndexOf(marker, System.StringComparison.Ordinal);
                Assert.GreaterOrEqual(position, 0, "Missing scene: " + marker);
                Assert.Greater(position, previous, "Out of order: " + marker);
                previous = position;
            }
        }

        [Test]
        public void RescueAndTheActualWoundPrecedeThePublicClashAndPrivateOrders()
        {
            var escape = Replay(26);
            Ordered(escape, "교대가 지키는 사람", "지킬 것이 있는 검", "휘선 뒤에서 바뀐 궤도", "걱정하는 사람이 여는 길", "현장 지휘자는 십귀", "만난 적 없는 사람들", "나예린|류연! 피가", "대호", "철침");
            var seven = Replay(27);
            Ordered(seven, "규칙이 허락한 도전", "깨어나기로 한 사람", "열 장의 경계", "돌아오는 흑응익", "여섯 창과 일곱째", "묵빛 채찍의 봉쇄", "등에 남은 상처", "앞의 겸기, 뒤의 추영", "드러난 은형", "바늘과 시선", "은설란|대공자!", "사적 알현", "대삼원칙", "추첨이 시작");
            Assert.IsFalse(seven.Contains("피가……"));
            Assert.AreEqual(26, Memories.Find("m_blood").UnlockStage);
        }

        [Test]
        public void TheBatonAndWitnessStoryKeepPartialKnowledgeAndUnfinishedRecovery()
        {
            string text = Replay(28);
            Ordered(text, "비전을 지키는 두 사람", "영혼의 자매", "은명이라는 임시 이름", "추첨 뒤의 아홉 사람", "넘어진 뒤에도 바통을", "밤길의 두 방해자", "공동 일위, 남은 증언", "거지 차림의 조사관", "저항하는 만근검익진", "명령이 버린 목숨", "한편 금요관");
            StringAssert.Contains("왼눈을 강제로", text);
            StringAssert.Contains("누구의 손이 준호를 넘어뜨렸는지", text);
            StringAssert.Contains("애써 전하려던 말은 끝내 음절로 흩어졌다", text);
        }

        [Test]
        public void RescueChoiceLeadsToCooperationThenTheWitnessMurderAndFalseAccusation()
        {
            string text = Replay(29);
            Ordered(text, "잠행과 잘린 생명줄", "절이 아닌 박의 속성", "걱정은 화가 되어", "아홉 기류의 조화", "지연된 결과와 거점", "오 일째의 비탄", "미끼 인형과 진짜 증인", "원에서 나선, 한 점으로", "교옥은 장송곡", "기다리던 나예린");
            StringAssert.Contains("살아 있었다", text);
            StringAssert.Contains("피를 토한 복면인은 도주했다", text);
            StringAssert.Contains("뇌령사의 박", text);
        }

        [Test]
        public void TheFireEndsThroughSharedRescueRatherThanKillingBiOrCollectingThePrizeEarly()
        {
            string text = Replay(30);
            Ordered(text, "실을 따라 만난 스승들", "주방으로 모인 단서", "최우수 조가 불린 뒤의 이의", "우승 발표와 염마뢰", "같지 않은 그림자", "불의 성벽", "남은 사람을 살리는 힘", "용권승룡", "피해는 사망 셋", "한때 은명이라 불린 남자", "청룡은장");
            StringAssert.Contains("혼자 완벽히 해낼 자신이 없다", text);
            StringAssert.Contains("현금 배당의 정산 장면은 없었다", text);
            StringAssert.Contains("모용휘도 혁중의 제안을 받아들였지만", text);
        }

        [Test]
        public void HwasanJourneyMovesFromMediationToWitnessEscortAndTheValley()
        {
            Ordered(Replay(21), "장례 뒤의 물음", "거울 신호와 돌아올 힘", "구궁산의 청혼", "흑랑부의 빈틈", "임개를 구한 군자소요검", "노사들이 중재");
            string valley = Replay(22);
            Ordered(valley, "흑랑채에 남은 흔적", "순풍루에서 들은 마음", "함께 가기로 한 사람들", "대홍산의 첫 추궁", "도끼와 곤", "숲에서 만난 생존자", "처음 칼을 겨눈 뒤", "볼을 스친 첫 화살", "되던진 것은 다른 화살", "쇠사슬 위의 도약");
            StringAssert.Contains("다섯을 모두 죽여 자백을 받아낸 것이 아니었다", valley);
            StringAssert.Contains("다른 화살을 되던져 혈궁을 죽였다", valley);
            StringAssert.Contains("무승부", valley);
        }

        [Test]
        public void TheSnakeBattleAndTwinDemonsPrecedeInterrogationAndTheMaskedTrial()
        {
            string snakes = Replay(23), demons = Replay(24);
            Ordered(snakes, "반 장의 안전", "화살의 궤도와 두 손", "가로막은 혈창", "지사진의 틈", "죽이지 않고 붙잡아");
            StringAssert.Contains("완성된 시간 정지가 아니었다", snakes);
            Ordered(demons, "얼마나 들렸는가", "떨어지는 동전", "부서진 도와 남은 폭탄", "비뢰쌍마전 뒤의 약속", "모사령|천겁령", "돌밭에 야영", "열 수와 옷깃", "다음 날 태극의 인재");
            StringAssert.Contains("살아 있는 공패", demons);
            StringAssert.Contains("외상", demons);
        }

        [Test]
        public void TheThreeGatesLeadToAnInvestigationAndARescueRatherThanACompletedRevenge()
        {
            string text = Replay(25);
            Ordered(text, "천겁간, 대표 한 사람", "겁흔벽에 남긴 자국", "검치의 세 치", "붉은 꽃 아래의 경계", "사라진 조사관", "기록이 가리킨 한 장원", "조용히 연 문", "깨어난 효룡", "구출한 사람을 먼저", "극안의 중심에 둔 사람", "비류연|선수교체를 해도 될까요?");
            StringAssert.Contains("백칠십팔 패", text);
            StringAssert.Contains("친구의 위험을 보고 그냥 지나칠 수는 없죠", text);
            StringAssert.Contains("지살은 왼팔을 잃고 오른팔", text);
            StringAssert.Contains("상처는 쌓였다", text);
            Assert.IsFalse(text.Contains("류연! 피가!"));
        }

        [Test]
        public void WudangJourneyAndTrainingLeadToTheControlledBrotherAndHisLastWords()
        {
            string text = Replay(14);
            Ordered(text, "한계를 두지 않는다는 것", "연말의 청룡단전", "새해의 목적지", "장강, 첫 번째 위기", "갑판에서 보내는 지원", "적웅채와 표국의 내일",
                "태극절애의 노도인", "그물을 알아본 사람", "숙소의 연환검진", "폭우 속에서 듣는 말", "네 초상화", "첫 충돌 뒤의 고립", "전원 생환을 위한 시간",
                "두 번째 변장", "함께 넘은 관문", "두 번째 공세", "보호를 택한 검", "단심무형의 장 심뢰", "갈효봉|효룡", "겁, 하늘의 겁", "마진가는 학생들의 증언을 믿고 거절했다");
            Assert.IsFalse(text.Contains("지살의 오른팔"));
            StringAssert.Contains("신원이 확인된 것은 아니다", text);
            StringAssert.Contains("모용휘·청흔·효룡·남궁상·현운·장홍·나예린", text);
        }

        [Test]
        public void CavalryRetreatPrecedesTheMemorialAndInspectorAndDoesNotBecomeAnExtermination()
        {
            string cavalry = Replay(15), inspector = Replay(16);
            StringAssert.Contains("이미 끝난 사건의 회상", cavalry);
            Ordered(cavalry, "철비십이격", "위무상의 상대", "구천학의 일기승부", "구천학|졌다.", "철각비마대를 물렸다");
            Ordered(inspector, "진혼제 뒤의 학관", "흑천맹의 사신과 합의문", "웃음을 잃지 않는 조사관", "손목을 잃은 회주", "세 번째 호위");
            StringAssert.Contains("행인 모두를 공격할 이유는 없었다", inspector);
            StringAssert.Contains("이중 정보 작전이라는 설명은 청흔을 설득하려는 거짓말", inspector);
        }

        [Test]
        public void TheTwoPangGuardsAndTheWhiteDragonClashAreNotConfusedWithTheIceSwordDuel()
        {
            string assembly = Replay(17), duel = Replay(18);
            StringAssert.Contains("단옥도 팽유경", assembly);
            StringAssert.Contains("팽혁성", assembly);
            Assert.IsFalse(EarlyGameContent.FindStage(17).Encounters.SelectMany(x => x).Contains("ma_haryeong"));
            Assert.IsFalse(assembly.Contains("가는 줄 위의 수업"));
            Assert.IsFalse(assembly.Contains("운향정의 눈물과 오해"));
            Ordered(duel, "백룡검의 한 합", "대치의 끝과 중간 평가", "눈물 이전의 말", "가는 줄 위의 수업", "줄의 탄력을 읽다", "빙검과 세 가지 부탁");
            StringAssert.Contains("녹옥여래신검은 아직 칼집 안", duel);
            StringAssert.Contains("마진가의 딸이자 도성 하후식의 외손녀", duel);
            var dragon = EarlyGameContent.FindEnemy("yong_cheonmyeong");
            Assert.IsTrue(dragon.Moves.All(move => !move.Name.Contains("여래") && !move.Name.Contains("달마")));
        }

        [Test]
        public void ThreeIceSwordRequestsAgreeWithThePhaseNoticeAndKeepTheExistingChill()
        {
            string text = Replay(18);
            StringAssert.Contains("부탁 세 가지", text);
            StringAssert.Contains("남은 두 가지", text);
            string[] first, second;
            Assert.IsTrue(BossPhaseLines.TryGet("gwan_cheolsu", out first, out second));
            StringAssert.Contains("부탁 세 가지", string.Join(" ", first));
            var move = EarlyGameContent.FindEnemy("gwan_cheolsu").Moves.Single(m => m.Name == "백빙신첨");
            Assert.AreEqual(StatusKind.Chill, move.Inflict);
            Assert.AreEqual(Statuses.ForMove("백빙신침"), move.Inflict);
            Assert.AreEqual(1.3f, move.Power);
        }

        [Test]
        public void TheTrialSeparatesPrivateIllusionsAndPreservesUnknownCulpritsAndEarlyAddress()
        {
            string text = Replay(19);
            Ordered(text, "여덟 갈래 문", "매화의 두려움", "검성의 나뭇가지와 은설란의 환영", "형의 환영", "한 사람의 오래된 악몽", "비류연 앞에 선 사부", "직접 시험해 보시죠", "비황신침도 그 바람에 빗나갔지만", "연쇄 붕괴");
            StringAssert.Contains("실제 은설란은 밖에서 기다렸다", text);
            StringAssert.Contains("실제 인물이 아니라 환영", text);
            StringAssert.Contains("학관은 그가 염마뢰를 던졌다는 사실을 알지 못했다", text);
            Assert.IsFalse(text.Contains("나예린|류연"));
        }

        [Test]
        public void NaturalCaveSurvivalLeadsToLakeRescueAndTheEmptyCoffinFuneral()
        {
            string text = Replay(20);
            Ordered(text, "나예린|류연.", "열흘째, 구조대 밖으로", "별의 강 사이로 흐르는 음률", "이십일 일째의 묵린혈망",
                "사십사 일째", "파양호", "인공호흡", "빈 관 둘", "조의금은 많이 걷혔어요", "사망자는 한 명도 없었다", "후보 오십 명");
            StringAssert.Contains("내단은 없었고", text);
            StringAssert.Contains("나백천은 딸 나예린", text);
            Assert.IsFalse(Replay(10).Contains("나백천의 손녀"));
        }

        [TestCase(14)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(17)]
        [TestCase(18)]
        [TestCase(19)]
        [TestCase(20)]
        [TestCase(21)]
        [TestCase(22)]
        [TestCase(23)]
        [TestCase(24)]
        [TestCase(25)]
        [TestCase(26)]
        [TestCase(27)]
        [TestCase(28)]
        [TestCase(29)]
        [TestCase(30)]
        [TestCase(31)] [TestCase(32)] [TestCase(33)] [TestCase(34)] [TestCase(35)] [TestCase(36)]
        public void EveryFightHasAResumeSafeSceneThatDoesNotChangeProgressOrReplayForClears(int number)
        {
            var stage = EarlyGameContent.FindStage(number);
            var save = new SaveData { ClearedStage = number - 1, ProgressStage = number, ProgressNext = stage.Encounters.Length - 1, Doo = 9876 };
            for (int i = 0; i < stage.Encounters.Length; i++)
            {
                var scenes = StoryEpisodes.BeforeFight(stage, i, save);
                Assert.IsNotNull(scenes);
                foreach (var scene in scenes) StringAssert.Contains(scene.Title, Replay(number));
                Assert.IsNull(StoryEpisodes.BeforeFight(stage, i, save, true));
            }
            Assert.AreEqual(number - 1, save.ClearedStage);
            Assert.AreEqual(stage.Encounters.Length - 1, save.ProgressNext);
            Assert.AreEqual(9876, save.Doo);
            save.ClearedStage = number;
            Assert.IsNull(StoryEpisodes.BeforeFight(stage, 0, save));
            Assert.AreEqual(number == 14 ? 11 : number == 15 ? 5 : number == 18 ? 4 : number == 19 || number == 27 || number == 29 ? 7 : number == 22 || number == 26 ? 6 : number >= 23 && number <= 25 || number == 30 ? 4 : number == 28 || number == 35 ? 5 : number >= 31 && number != 34 ? 4 : 3, stage.Encounters.Length);
        }
    }
}
