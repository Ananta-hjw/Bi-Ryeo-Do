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
        public void WudangJourneyAndTrainingLeadToTheControlledBrotherAndHisLastWords()
        {
            string text = Replay(14);
            Ordered(text, "한계를 두지 않는다는 것", "연말의 청룡단전", "아미산 대신 무당산", "장강 표선의 세 차례 암습", "적웅채의 통행세",
                "태극절애의 황금 그물", "폭우를 피한 동굴", "돌아온 혈류도", "고립된 밤의 묵금",
                "이레째의 항아리", "갈효봉의 두 번째 폭주", "천지쌍살의 마지막 공세", "단심무형의 장 심뢰", "겁, 하늘의 겁", "효룡은 검기상인으로 형을 보냈다");
            StringAssert.Contains("염도의 염격은 지살의 오른팔", text);
            StringAssert.Contains("노인의 정체는 확정되지 않았다", text);
            StringAssert.Contains("모용휘·청흔·효룡·남궁상·현운·장홍·나예린", text);
        }

        [Test]
        public void CavalryRetreatPrecedesTheMemorialAndInspectorAndDoesNotBecomeAnExtermination()
        {
            string cavalry = Replay(15), inspector = Replay(16);
            Assert.IsFalse(cavalry.Contains("진혼제"));
            Ordered(cavalry, "철비십이격", "구천학의 일기승부", "구천학|졌다.", "철각비마대를 물렸다");
            Ordered(inspector, "돌아온 뒤의 진혼제", "흑천맹의 사신과 합의문", "웃음을 잃지 않는 조사관", "백인회주를 막다", "세 번째 호위");
            StringAssert.Contains("행인 모두가 적인 것은 아니었다", inspector);
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
            Ordered(duel, "백룡검의 한 합", "대치의 끝과 중간 평가", "가는 줄 위의 수업", "운향정의 눈물과 오해", "빙검과 세 가지 부탁");
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
            Ordered(text, "여덟 갈래 문", "매화의 두려움", "검성의 나뭇가지와 은설란의 환영", "형의 환영", "한 사람의 오래된 악몽", "비류연 앞에 선 사부", "직접 시험해 보시죠", "비황신침은 돌풍에 빗나갔다", "연쇄 붕괴");
            StringAssert.Contains("실제 은설란은 밖에서 기다렸다", text);
            StringAssert.Contains("실제 인물이 아니라 환영", text);
            StringAssert.Contains("학관은 그가 염마뢰를 던졌다는 사실을 알지 못했다", text);
            Assert.IsFalse(text.Contains("나예린|류연"));
        }

        [Test]
        public void NaturalCaveSurvivalLeadsToLakeRescueAndTheEmptyCoffinFuneral()
        {
            string text = Replay(20);
            Ordered(text, "나예린|류연.", "열흘째, 구조대 밖으로", "별의 강 사이로 흐르는 음률", "이십육 일째의 묵린혈망",
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
            Assert.AreEqual(number == 14 ? 8 : number == 15 ? 4 : number == 19 ? 5 : 3, stage.Encounters.Length);
        }
    }
}
