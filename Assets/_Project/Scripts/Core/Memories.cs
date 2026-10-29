using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>One 나예린 호감도 이벤트 (a remembered scene). Lines use the story format: "화자|대사" is dialogue, anything else is narration.</summary>
    public sealed class MemoryData
    {
        public string Id, Title;
        public int Volume;          // source volume (Story/analysis/volXX.md)
        public int UnlockStage;     // open once this stage is cleared
        public long RewardDoo;
        public string[] Lines;
    }

    public sealed class MemoryMilestone
    {
        public int Count;
        public long Doo;
        public string ItemId;
        public int ItemCount = 1;
    }

    /// <summary>
    /// 나예린 호감도 이벤트: scenes between 비류연 and 나예린 taken from the novel (volumes 4-16, see Story/analysis), unlocked as the
    /// stages that cover them are cleared. Watching a scene for the first time pays 두 and, once she has joined, +2 인연 with 나예린; collecting scenes opens
    /// milestone rewards. Narration paraphrases the analysis notes and dialogue is adapted from the analysed scenes.
    /// </summary>
    public static class Memories
    {
        public const string HeroineId = "na_yerin";
        public const int BondPerMemory = 2;

        public static readonly MemoryData[] All =
        {
            new MemoryData { Id = "m_other_eyes", Title = "의심과 다른 눈", Volume = 4, UnlockStage = 11, RewardDoo = 1250, Lines = new[] {
                "새벽 검기 수련과 이진설의 스물네 검기를 피한 반격. 나예린은 보호받기만 하는 인물이 아니라 스스로 무공과 사람을 판단하는 검사였다.",
                "애소저회에서 나예린은 검후의 무공을 훔칠 필요가 없다는 비류연의 말을 대회 우승으로 검증하겠다고 했다.",
                "그녀에게 다른 눈은 의심을 거두는 뜻이었다. 비류연이 품은 관계 변화의 기대와는 달랐다. 우승하면 연인이 된다는 합의는 없었다.",
            } },
            new MemoryData { Id = "m_invite", Title = "우뢰매의 초대", Volume = 5, UnlockStage = 12, RewardDoo = 1560, Lines = new[]
            {
                "삼성무제가 한창일 때, 비류연은 해동청 우뢰를 날려 나예린을 관전에 초대한다.",
                "그녀는 처음으로 그 초대를 받아들인다. 약속이 있었고, 백향관 침입자로 의심되는 사람의 실력을 직접 확인하고 싶었기 때문이다.",
                "사저 독고령이 동행한다.",
                "관중석의 나예린을 향해 비류연이 손을 흔들자 비무장이 질투의 야유로 가득 찬다.",
            } },
            new MemoryData { Id = "m_apology", Title = "우승 뒤에도 남은 경계", Volume = 5, UnlockStage = 13, RewardDoo = 1560, Lines = new[] {
                "세 부문이 무승부가 되어 비류연은 종합 우승자가 되었다. 나예린은 검후처 침입을 의심한 일을 사과했다.",
                "백향관 301호의 실제 침입까지 없었던 일이 되거나 물증으로 무죄가 입증된 것은 아니었다.",
                "사과는 접촉의 허락이 아니었다. 두 번째 무단 입맞춤 뒤 나예린은 검을 뽑았고, 용안으로도 그의 마음을 읽지 못했다.",
                "그 경계는 무당산 동굴에서 나누는 대화로 이어진다.",
            } },
            new MemoryData { Id = "m_cave", Title = "무당산의 동굴", Volume = 6, UnlockStage = 14, RewardDoo = 1870, Lines = new[]
            {
                "합숙소에도 암습이 들어와 비류연이 봉황섬뢰로 정리한다. 혼자 산책하던 나예린은 암습자 일곱 명의 연환 공격을 읽으며 맞섰다. 비류연이 합류하고, 폭우를 피해 동굴에서 함께 머문다.",
                "나예린은 두 번 허락 없이 입맞춘 그를 믿을 수 없다고 말한다. 어린 시절부터 겪은 납치 위협·시비·원치 않는 접근을 합친 수가 오천육백칠십이였다. 전부 납치 미수는 아니었다.",
                "비류연|믿지 말아요. 하지만 피하기만 해서는 해결이 안 돼요.",
                "귀엽다는 말 뒤 과거 이야기를 들은 비류연은 불신할 만하다고 인정한다. 사부 일화를 들으며 나예린은 웃는다. 경청이 작은 변화를 만들었지만 전적인 신뢰나 관계의 합의로 바뀐 것은 아니다.",
                "그 사이 뇌령사 결계에 걸린 침입자는 비류연이 몰래 처리한다.",
            } },
            new MemoryData { Id = "m_geum", Title = "묵금 연주의 밤", Volume = 7, UnlockStage = 14, RewardDoo = 2500, Lines = new[]
            {
                "무당산 합숙지가 고립된 밤. 비류연은 사면초가에 노래가 없다며 홀로 묵금을 연주한다. 나예린이 그를 찾아온다.",
                "나예린|당신은 언제나 혼자군요.",
                "비류연|나 소저도 마찬가지 아닌가요.",
                "나예린은 묵금 소리가 듣기 좋은 데 놀란다.",
            } },
            new MemoryData { Id = "m_flee", Title = "도망 작전", Volume = 7, UnlockStage = 14, RewardDoo = 2810, Lines = new[]
            {
                "무당산의 두 번째 공세를 앞두고 비류연은 염도에게 연애 상담을 한다. 동정심을 얻으려면 약해져야 한다는 조언이었다.",
                "그래서 \"도망치기\" 작전을 시도했지만, 결국 나예린의 뒤에 숨은 꼴만 되고 만다.",
            } },
            new MemoryData { Id = "m_cheonsal", Title = "천살 앞에서", Volume = 7, UnlockStage = 14, RewardDoo = 2180, Lines = new[]
            {
                "염도가 명부오귀장을 전멸시킨 뒤, 나예린은 약자를 보호하는 것이 강자의 의무라며 천살과 맞선다. 비애무한으로 검을 붙들어 기회를 만들지만 검풍의 압력을 견디기 어려워지자 비류연이 그녀를 막아선다.",
                "천살의 귀요안은 눈빛으로 마음을 장악하는 초혼섭령술의 비기였지만 나예린에게는 통하지 않았다.",
                "비류연은 영사심결 절대정신방어 허무도로 귀요안을 되받아쳐 천살의 눈을 멀게 한다.",
                "묵룡환을 풀고 손가락질 하나로 초혼령을 부수고, 천살의 오른팔을 자른다.",
            } },
            new MemoryData { Id = "m_requiem", Title = "스스로 택한 검무", Volume = 8, UnlockStage = 16, RewardDoo = 2400, Lines = new[] {
                "추혼대 스무 명의 관 가운데 수습한 시신은 한 구뿐이었다. 학생들의 생환과 첩보대의 희생은 서로 다른 사실이었다.",
                "나예린은 스스로 진혼검무를 맡았다. 오색 비단과 은방울, 한기가 죽은 이를 기리고 남은 사람들을 이었다.",
                "그 선택은 무력한 보호 대상의 모습과 달랐다. 부활의 주문이나 애도를 거래한 연애 약속은 아니었다.",
            } },
            new MemoryData { Id = "m_visit", Title = "병문안", Volume = 10, UnlockStage = 19, RewardDoo = 4060, Lines = new[]
            {
                "나예린은 망설이다가 의약전으로 병문안을 온다. 그리고 비류연이 적어 준 목록대로 음식 바구니를 든 염도와 마주친다.",
                "비류연은 나예린 앞에서 병약한 척하며 음식을 순식간에 먹어 치운다.",
                "의약전에는 실제 중환자도 있었다. 두 노사의 신경전과 달리 나예린은 승패·제자 계약·꾀병의 진상을 모른 채 안부를 물었다. 그녀의 안도는 공개 연인 선언이 아니었다.",
            } },
            new MemoryData { Id = "m_cave_stars", Title = "검을 넣자 나타난 별", Volume = 10, UnlockStage = 20, RewardDoo = 3600, Lines = new[] {
                "십삼 일째, 밝은 검을 넣자 푸른 광물이 별의 강처럼 나타났다. 빛이 있을 때는 보지 못하던 풍경이었다.",
                "가지고 들어온 묵뢰로 비류연은 한 사람을 위해 진지하게 연주했다. 나예린은 그의 어깨에 기대 잠들었다.",
                "전투의 음공이나 잠을 강요하는 술법이 아니었다. 잠시 쉴 수 있다는 신뢰가 그 곁에 남았다.",
            } },
            new MemoryData { Id = "m_water_promise", Title = "벽을 부수기 전의 약속", Volume = 10, UnlockStage = 20, RewardDoo = 4000, Lines = new[] {
                "사십사 일째 수맥을 찾은 비류연은 물길의 위험을 설명하고 다시 목숨을 맡겨 달라고 했다. 나예린은 듣고 동의했다.",
                "벽을 부수기 전 축복을 청하자 망설이던 그녀가 동의해 먼저 입을 맞췄다. 앞선 무단 접촉과는 순서도 뜻도 달랐다.",
                "물길에서 수공이 약한 나예린이 의식을 잃자 비류연은 건져 내어 응급조치했다. 구조를 위한 접촉과 앞의 의식 있는 선택은 구별됐다.",
            } },
            new MemoryData { Id = "m_funeral", Title = "장례식장의 두 사람", Volume = 10, UnlockStage = 20, RewardDoo = 3740, Lines = new[]
            {
                "매몰 44일째, 학관은 빈 관 둘을 놓고 장례식을 치르고 있었다. 맹주 나백천이 통곡하며 천겁우를 몰살하겠다고 맹세한 직후였다.",
                "그때 비류연이 나예린과 함께 장례식장에 나타난다.",
                "비류연|조의금은 많이 걷혔어요?",
                "나예린은 아버지와 재회했고 구조를 기다리던 이들도 놀라움과 기쁨을 드러냈다. 염도와 빙검에게는 안도와 복잡한 부담이 섞였다.",
                "염도는 기쁨과 제자 신분의 부담을 함께 삼켰다. 공개된 장례 자리에서 사제의 비밀을 밝히지는 않았다.",
                "마진가가 환마동에서 본 것을 묻자 비류연은 \"뛰어넘어야 할 존재(사부)와 내 자신, 그리고 차가운 선녀\"라고 답한다.",
                "마진가와 나백천은 각각 \"정말 아무 일도 없었나\" 하고 의심하고, 나예린은 얼굴을 붉히며 부인한다.",
            } },
            new MemoryData { Id = "m_father", Title = "아버지의 면담", Volume = 11, UnlockStage = 21, RewardDoo = 3120, Lines = new[]
            {
                "나백천이 비류연을 개인적으로 불러 44일 동안의 일을 캐묻는다.",
                "비류연은 \"있었을 수도 없었을 수도\"라며 도발하고, 그녀를 \"예린\"이라 부른다.",
                "그때 나예린이 들어온다.",
                "나예린|류연은 제 생명의 은인이에요.",
                "비류연은 나예린의 뜻이 먼저라고 답했다. 나백천은 딸이 처음으로 남자를 이름으로 부르는 것을 듣고 충격을 받는다. 이 면담 뒤 나예린은 한동안 비류연을 피한다.",
            } },
            new MemoryData { Id = "m_street", Title = "낯설어진 자신의 마음", Volume = 11, UnlockStage = 22, RewardDoo = 3900, Lines = new[] { "거리에서 비류연과 순풍루의 일을 이야기하던 나예린이 달라진 자신을 돌아봤다.", "나예린|내가 내가 아닌 것 같아요.", "이름을 부르고 다가가는 마음에도 두려움이 다시 일 수 있었다. 비류연과의 대화가 오래된 상처를 없애는 치료였던 것은 아니었다." } },
            new MemoryData { Id = "m_talk", Title = "은설란과의 연애 토크", Volume = 11, UnlockStage = 22, RewardDoo = 4370, Lines = new[] { "함께 걷는 길에 은설란은 나예린에게 비류연과의 사이를 물었다. 나예린은 관계를 단정하지 못했다.", "은설란은 첫사랑이 아주 먼 곳으로 떠났다고 했다. 그 사연과 정체가 나예린에게 모두 설명된 것은 아니었다.", "내가 내가 아닌 것 같다는 말은 앞서 나예린이 비류연에게 한 말이었다. 은설란에게 털어놓은 대사와 혼동하지 않는다." } },
            new MemoryData { Id = "m_arrow_thread", Title = "화살 끝에서 지킨 사람", Volume = 12, UnlockStage = 23, RewardDoo = 4200, Lines = new[] { "나예린의 독아 부적은 보통 뱀만 피하게 했다. 청홍쌍각사를 얹은 화살이 날아들자 비류연의 호통도 잠깐의 주저만 만들었다.", "뇌령사로 화살의 궤도를 바꾸고도 뱀은 다시 달려들었다. 그는 양손으로 붙잡았고 뒤에 통증을 느꼈다.", "길어진 듯한 순간은 완성된 시간 정지가 아니었다. 함께 막아 준 청흔과 모용휘의 검도 그곳에 있었다." } },
            new MemoryData { Id = "m_wall", Title = "흔들림을 읽은 사람", Volume = 13, UnlockStage = 25, RewardDoo = 4800, Lines = new[] { "겁흔벽 앞에서 비류연이 평소와 다르게 동요했다. 나예린은 용안으로 그 흔들림을 읽었다.", "왜 동요했는지, 사부가 누구인지까지 알게 된 것은 아니었다. 가까운 사이에도 아직 말하지 않은 과거가 있었다.", "그녀는 알 수 없는 사연을 단정하지 않았다. 은설란을 찾아 나선 뒤에도 동료들의 판단과 기록을 함께 믿었다." } },
            new MemoryData { Id = "m_rescue", Title = "구출대", Volume = 13, UnlockStage = 25, RewardDoo = 4990, Lines = new[] { "은설란을 구하러 가는 것은 외부 접촉 금지 규칙과 부딪쳤다. 나예린은 친구의 긴급한 위험을 외면하지 않겠다고 선택했다.", "나예린|친구의 위험을 보고 그냥 지나칠 수는 없죠.", "풍매객잔의 표사는 실제로 행방을 몰랐다. 용안으로 알아낸 가족의 거짓말도 행선지의 답은 아니었다.", "개방이 마차 기록을 좁혀 새 경비가 붙은 장원을 찾아냈다. 한 사람의 능력으로 모든 수수께끼가 풀린 구조가 아니었다." } },
            new MemoryData { Id = "m_blood", Title = "류연! 피가", Volume = 14, UnlockStage = 26, RewardDoo = 5620, Lines = new[] {
                "비류연이 영뢰 천잔에 가슴을 베인 뒤 개방 분타에 돌아왔다. 기다리던 나예린이 다가가 살피다 피를 보았다.",
                "나예린|류연! 피가……",
                "그는 지혈했지만 상처가 모두 나은 것은 아니었다. 뒤에 홍매곡에서 재대치할 때 생긴 새 부상과는 다른 순간이었다.",
            } },
            new MemoryData { Id = "m_sisters", Title = "서로를 지켜 온 자매", Volume = 15, UnlockStage = 28, RewardDoo = 6200, Lines = new[] {
                "검각의 어린 나예린을 먼저 안고 영혼의 자매가 되자 한 사람은 독고령이었다.",
                "이제 나예린은 사저의 악몽과 아픔을 걱정했다. 진설 덕에 조금 열린 마음에도 흔들리는 날은 남아 있었다.",
                "과거의 은명은 그녀의 거부를 무시하고 왼눈을 빼앗았다. 나예린은 만나러 나가지 말라 부탁했지만 막지 못했다.",
                "사연을 아는 일이 상처를 낫게 하지는 않았다. 비류연도 사적인 아픔을 구경거리로 만들지 않겠다고 마음을 고쳤다.",
            } },
            new MemoryData { Id = "m_baton", Title = "함께 달린 길의 끝", Volume = 15, UnlockStage = 28, RewardDoo = 6400, Lines = new[] {
                "심판의 판정은 공동 일위였다. 비류연은 자신이 먼저 도착했다고 확신하며 낙담했다.",
                "나예린은 그 마음을 살폈다. 준호가 다친 몸으로 넘겨 준 바통과 비류연이 끝까지 달린 길은 판정만으로 사라지지 않았다.",
                "경쟁의 결과를 바꿔 약속한 것이 아니라 함께 버틴 마음을 알아준 위로였다.",
            } },
            new MemoryData { Id = "m_sword", Title = "검후의 시험", Volume = 16, UnlockStage = 29, RewardDoo = 6860, Lines = new[] {
                "검후의 열 초식을 받아내면 내기의 승리로 본다는 조건 아래 관계 공인의 약속이 오갔다. 나예린은 자기 의견 없이 진행되는 일에 당황했다.",
                "비류연|제가 당신의 제자를 사랑하게 된 것을 사과할 이유가 없습니다.",
                "그녀의 침묵이 혼인 승낙은 아니었다. 아직 자기 마음도 확신하지 못했다.",
                "검성·도성의 공증 아래 비류연은 양쪽 발목 환을 풀고 우중거 불점의로 흐름을 따랐다. 검후는 제한된 내기의 승리를 인정했다.",
                "나예린은 용안으로도 읽지 못한 순간을 보았다. 공적인 보증과 당사자가 정할 마음은 서로 다른 문제로 남았다.",
            } },
            new MemoryData { Id = "m_waiting", Title = "먼저 기다린 사람", Volume = 16, UnlockStage = 29, RewardDoo = 7000, Lines = new[] {
                "안명후의 손에서 비류연 이름의 피 묻은 천율패가 나왔다. 군중은 출처와 알리바이보다 편견을 먼저 보았다.",
                "비류연은 증인 보호에 실패한 자기 방심도 인정했다. 살기로 길을 열었지만 앞을 막은 사람을 죽이지 않았다.",
                "먼저 나온 그를 기다린 사람은 나예린이었다. 장홍·준호·효룡·진설과 다른 조 모용휘도 함께했다.",
                "그를 믿는 선택이 이미 모든 증거를 해명했다는 뜻은 아니었다. 함께 확인하러 갈 길이 남아 있었다.",
            } },
            new MemoryData { Id = "m_phoenix", Title = "불사조의 다짐", Volume = 16, UnlockStage = 30, RewardDoo = 9360, Lines = new[] {
                "입구와 대피로가 무너진 뒤 비류연은 혼자서는 완벽히 해낼 자신이 없다며 네 고수의 힘을 구했다.",
                "성공을 위한 부적은 눈빛과 마음으로 오갔다. 사람들 앞에서 망설인 나예린은 믿고 살아가기로 마음을 정해 다가갔고, 비류연이 허리를 감아 입맞췄다.",
                "나예린에게 우승을 약속받은 것이 아니었다. 비류연이 묻고 혁중이 성공의 명예와 자격을 약속했다. 현금 배당의 정산은 따로 나오지 않았다.",
                "열 자루 비뢰도와 뇌령사가 나선으로 기류를 올리는 동안 마지막 폭발에도 네 고수의 지원이 필요했다.",
                "불이 꺼지자 나예린이 먼저 안았다. 탈진한 비류연은 품에 쓰러져 무릎에서 쉬었다.",
                "사망 셋과 다친 이들이 남았다. 불사조의 재생은 파괴의 변명이 아니라 남은 사람들의 회복과 책임을 다짐하는 말이 됐다.",
            } },
            new MemoryData { Id = "m_who", Title = "너는 누구냐", Volume = 18, UnlockStage = 32, RewardDoo = 9980, Lines = new[]
            {
                "월향정. 나예린은 행방이 끊긴 독고령을 걱정한다. 제어할 수 없던 용안 때문에 무너질 뻔했던 어린 날, 검각에서 그녀를 지켜 준 사람이 독고령이었다.",
                "정원에 숨은 애소저회의 남정네들을 심중지검으로 쫓아낸 검후 이옥상이 묻는다. \"너는 누구냐?\"",
                "나예린은 자기를 규정하는 것은 자신뿐이라던 비류연의 말을 떠올린다. \"한 사람의 강호인으로서 검의 길을 추구하는 검객이며, 한 사람의 여자입니다.\"",
                "짧은 비무 끝에 검후는 흩날리는 눈 같은 보법 비설보를 전한다. \"령이가 위기에 빠졌을 때 구하려면 쌓아 둔 적공이 필요하다.\"",
                "검후가 비류연의 사문과 신분을 묻자 나예린이 답한다. \"류연은 남의 지위에 연연하지 않는 사람이에요.\"",
            } },
            new MemoryData { Id = "m_trust", Title = "나를 믿어 달라", Volume = 19, UnlockStage = 33, RewardDoo = 10600, Lines = new[]
            {
                "노을 아래 비류연과 나예린이 말없이 마음을 나눈다. 나예린이 마음을 전하려던 참에 무원대가 다가온다.",
                "청흔의 무극검을 적중화로 꺾은 뒤, 백 명의 검진 앞에서 나예린이 검을 뽑으려 한다. 비류연이 그 손을 말린다.",
                "비류연|예린답지 않다. 나를 믿어 달라.",
                "나예린은 \"믿겠다\"고 답하고, 비류연은 수갑을 찬 채 순순히 끌려간다.",
                "홀로 남은 나예린은 독고령과 비류연이 자신과 세상을 잇는 접점이었음을 깨닫고 스스로에게 맹세한다. \"류연도, 령 언니도 내 손으로 되찾겠다.\"",
            } },
            new MemoryData { Id = "m_lunch", Title = "옥중의 도시락", Volume = 20, UnlockStage = 35, RewardDoo = 11230, Lines = new[]
            {
                "나예린이 처음으로 스스로 나서 중원표국을 베어 들어갔지만 이시건을 놓쳤다. 감옥의 비류연에게 실패했다고 털어놓는다.",
                "비류연은 그녀가 스스로 움직였으니 성공이라고 말하고, 손수 만든 음식을 넣어 달라고 부탁한다.",
                "며칠 뒤 감옥에 도시락이 들어온다. 써는 솜씨는 완벽했고, 불 조절은 실패했다.",
                "면회 온 장홍은 그것이 나예린이 직접 만든 음식이라는 데 경악한다.",
            } },
            new MemoryData { Id = "m_hairpin", Title = "칠채 보요", Volume = 20, UnlockStage = 36, RewardDoo = 11850, Lines = new[]
            {
                "마천각으로 떠나기 전날, 나예린은 짐을 꾸리다 빛바랜 붉은 봉황 상자를 연다. 은빛 단검과 칠채 보요가 들어 있다.",
                "비류연이 사절단에서 빠져 서운하냐는 이진설의 물음에 무심하게 답하지만, 검을 놓고 갈 뻔하고는 얼굴이 붉어진다.",
                "출발하는 날, 흑마를 탄 현의여인이 그녀 곁에 다가와 같은 보요를 들어 보인다. 나예린은 그 마음을 읽지 못한다.",
                "현의여인|오랜만이에요, 린.",
                "나예린|연비!",
                "십 년 동안 묻어 두었던 생명의 은인의 이름이었다.",
            } },
            new MemoryData { Id = "m_after_fire", Title = "재생 뒤의 계절", Volume = 17, UnlockStage = 31, RewardDoo = 9700, Lines = new[] { "화재 뒤 계절이 바뀌어도 죽은 세 사람과 부상자들의 몫은 사라지지 않았다.", "비류연과 나예린은 다시 남창으로 향했다. 불사조라는 말은 상처를 모두 잊었다는 선언이 아니었다.", "나예린은 자기 마음을 아직 스스로 정할 수 있었다. 검후와의 내기에 걸린 공인의 약속이 그녀의 혼인 승낙을 대신하지 않았다." } },
            new MemoryData { Id = "m_asked_for_help", Title = "혼자가 아니었던 첫걸음", Volume = 20, UnlockStage = 34, RewardDoo = 10900, Lines = new[] { "나예린은 비류연을 되찾겠다던 맹세 뒤 도움을 청했다. 함께 표국으로 간 사람은 이진설과 효룡이었다.", "빙령백봉의 검이 길을 열고 두 형제를 쓰러뜨렸지만, 윤이정과 이시건은 섬광을 남기고 도주했다.", "놓친 상대가 있어도 스스로 선택하고 동료에게 손을 내민 한 걸음은 남았다. 실패의 결론을 혼자 짊어질 필요는 없었다." } },
            new MemoryData { Id = "m_red_contract", Title = "다시 부를 수 있는 약속", Volume = 21, UnlockStage = 37, RewardDoo = 12300, Lines = new[] { "십 년 전, 후원의 연비에게 린은 자기 이름과 두려움을 말했다. 상처를 한마디로 지워 준 만남은 아니었다.", "린이 먼저 계약을 청했다. 창문에 붉은 수건을 걸면 이틀 안에 오겠다는 약속은 다시 도움을 청할 길이 됐다.", "대가인 부탁 세 가지의 내용은 아직 남아 있었다. 현재의 나예린은 은인의 또 다른 이름을 알지 못했다." } },
            new MemoryData { Id = "m_returned_hawk", Title = "그 둥지의 흰 매", Volume = 21, UnlockStage = 38, RewardDoo = 12600, Lines = new[] { "독고령을 아직 찾지 못했다는 소식과 함께 백응이 돌아왔다. 연비는 그 새를 알아봤다.", "백교 앞에서 부모를 잃고 살아남은 새끼 한 마리를 린이 맡았다. 십 년의 시간이 지나 그 새는 그녀의 소식을 전하는 매가 됐다.", "은인을 기억하는 마음이 사저의 실종을 해결하지는 못했다. 기다리고 찾을 길은 여전히 남았다." } },
            new MemoryData { Id = "m_market_stroll", Title = "십 년을 기다린 노점", Volume = 22, UnlockStage = 39, RewardDoo = 13000, Lines = new[] { "나예린과 연비는 강호란도의 노점에서 양고기 꼬치와 고리 던지기를 즐겼다.", "십 년 전부터 꼭 해 보고 싶었던 일이 눈앞의 작은 하루가 됐다. 목돈을 구해야 한다는 마음 사이에도 함께 보낸 시간이 남았다.", "그 뒤 나예린은 돈왕 앞에서 스스로 얼굴과 이름을 밝혔다. 도움을 주고받는 관계에서 이번에는 그녀도 가진 것을 걸었다." } },
            new MemoryData { Id = "m_silver_step", Title = "진법 한가운데의 한 걸음", Volume = 22, UnlockStage = 40, RewardDoo = 13400, Lines = new[] { "검후에게 배운 비설보의 흐름이 처음 펼치는 새 검기로 이어졌다. 나예린은 흑삭구두룡살진의 중심에 서서 흑룡삭을 끊었다.", "연비가 대신 모든 것을 끝내 준 것이 아니었다. 배후를 확인한 두 사람의 공방 뒤 마지막 길은 그녀의 검이 열었다.", "이름도 완성도 아직 남은 검기였다. 자기가 내디딘 걸음만은 이미 그곳에 있었다." } },
            new MemoryData { Id = "m_facing_fear", Title = "지상 최대의 적 앞에서", Volume = 22, UnlockStage = 41, RewardDoo = 13800, Lines = new[] { "정원에서 연비는 이제 도망치지 않고 지상 최대의 적과 싸우겠다고 했다.", "나예린은 비류연의 빈자리를 연비가 채우고 있다고 느꼈다. 은인의 또 다른 이름은 그녀에게 아직 닿지 않았다.", "달의 이슬을 안은 연비가 마침내 사부의 방문을 열었다. 두려움을 마주한 선택과 그 뒤의 대답은 서로 다른 걸음이었다." } },
            new MemoryData { Id = "m_refused_name", Title = "돌아오지 않은 언니의 이름", Volume = 23, UnlockStage = 42, RewardDoo = 14300, Lines = new[] { "나예린이 령 언니라 불렀을 때 영령은 누구냐고 물었다. 몽영령이라는 이름과 검각을 향한 적의가 돌아왔다.", "몸과 목소리가 가리키는 사람을 나예린은 외면하지 않았다. 연비도 그녀의 판단을 믿었다.", "나예린은 울지 않고 언니를 되찾겠다고 정했다. 그 마음이 조종의 방법이나 대결의 답까지 알게 한 것은 아니었다." } },
            new MemoryData { Id = "m_wings_granted", Title = "날아가거라, 딸아", Volume = 23, UnlockStage = 43, RewardDoo = 14800, Lines = new[] { "연비의 위험한 기류가 비껴간 뒤 나예린은 부모 앞에 큰절을 올렸다. 소중한 친구와 함께 정한 길을 가고 싶다고 했다.", "관계를 끊는 말이 아니라 자기 날개로 날 뜻이었다. 예청은 딸을 보내기로 했다.", "허락을 낳은 마지막 행동은 부모를 굴복시킨 힘이 아니라 나예린 자신의 말과 선택이었다." } },
            new MemoryData { Id = "m_shared_ice", Title = "함께 익힌 통곡오의", Volume = 24, UnlockStage = 45, RewardDoo = 15200, Lines = new[] { "두 사람이 함께 수련해야 익힐 수 있었던 한빙섬옥이 다시 펼쳐졌다. 빠름과 정교함으로 서로의 균형을 찾아 주던 검이었다.", "영령의 몸에 한빙기가 반응했다. 말로 부정한 시간도 몸에는 남아 있었다.", "그 뒤 울린 혈흔적은 바깥에서 건 조종이었다. 나예린은 상처 입고도 마지막 검을 자기 손으로 펼쳤다." } },
            new MemoryData { Id = "m_thanks_ryuyeon", Title = "품에서 남긴 한마디", Volume = 24, UnlockStage = 45, RewardDoo = 15400, Lines = new[] { "해상비조천참절 뒤 영령은 사부님이라 중얼거렸다. 나예린은 연비의 품에서 기절하며 고마워요, 류연이라 했다.", "그 말 하나로 모든 정체가 풀리거나 언니가 돌아온 것은 아니었다. 영령은 아직 상대 진영에 남아 있었다.", "도움이 닿은 자리와 끝나지 않은 기다림이 함께 그 품에 남았다." } },
            new MemoryData { Id = "m_black_horn_trace", Title = "남겨 둔 검은 뿔", Volume = 25, UnlockStage = 47, RewardDoo = 16000, Lines = new[] { "수면향과 부상 속에서도 나예린은 저항했다. 검은 뿔 장식 한쪽을 남겨 자신이 지나간 길을 표시했다.", "비류연은 밀실에서 쌍으로 만든 그 장식을 알아봤다. 단순한 물건보다 먼저 떠오른 것은 남긴 사람의 뜻이었다.", "아직 구출되지는 못했다. 그러나 찾는 사람과 기다리는 사람 사이에 이어 갈 흔적 하나가 남았다." } },
            new MemoryData { Id = "m_opened_eyes", Title = "안쪽에서 열린 눈", Volume = 26, UnlockStage = 52, RewardDoo = 16600, Lines = new[] { "물방울 반향으로 경비와 벽 너머를 읽은 뒤, 나예린은 자기 몸에 박힌 서른여섯 금침을 발견했다.", "포기하기 전 모든 방법을 써 보라는 말을 떠올렸다. 그날의 성취는 금제의 원인과 해제 방법을 스스로 찾은 일이었다." } },
            new MemoryData { Id = "m_self_return", Title = "스스로 돌아온 날개", Volume = 27, UnlockStage = 55, RewardDoo = 17200, Lines = new[] { "나예린은 마지막 두 침을 밀어내고 간수의 동선을 읽어 문을 열게 했다. 족쇄를 끊고 빙루를 되찾아 창고 밖으로 나왔다.", "무명 앞에서 기력이 다해 쓰러진 일은 그 탈출을 실패로 바꾸지 않았다." } },
            new MemoryData { Id = "m_sister_hand", Title = "이름보다 먼저 잡은 손", Volume = 27, UnlockStage = 55, RewardDoo = 17500, Lines = new[] { "영령은 조작되었을지 모를 기억을 두려워했다. 나예린은 망가진 것이 아니라 잊은 것이라 했다.", "예린아라는 부름이 먼저 흘러나오고 두 사람이 손을 잡았다. 모든 기억이 복원된 것은 아니었지만 지금의 감정은 남았다." } },
            new MemoryData { Id = "m_trembling_hand", Title = "돌아왔다는 인사", Volume = 27, UnlockStage = 55, RewardDoo = 17800, Lines = new[] { "서로 말없이 바라보던 시간이 지나 비류연은 스스로 돌아온 그녀를 자랑스러워했다.", "비류연|고생했어요, 잘 돌아왔어요", "나예린|다녀왔어요, 류연", "그의 손이 떨렸다. 그녀를 잃을 뻔했다는 두려움이 뒤늦게 닿았다." } },
            new MemoryData { Id = "m_stood_with_you", Title = "도망치지 않는다는 선택", Volume = 27, UnlockStage = 56, RewardDoo = 18200, Lines = new[] { "서천의 사기 앞에 몸이 굳을 때 비류연의 손이 어깨에 닿았다. 나예린은 물러서지 않겠다며 검을 들었다.", "진기가 바닥인 그녀 대신 그가 앞을 막았다. 함께 서겠다는 의지를 없는 일로 만들지 않는 보호였다." } },
            new MemoryData { Id = "m_umbrella_question", Title = "끝내지 못한 우산의 물음", Volume = 27, UnlockStage = 57, RewardDoo = 18600, Lines = new[] { "현천은린을 타고 돌아온 사람에게서 나예린은 연비의 우산을 알아보았다. 물음은 배 위의 비명에 끊겼다.", "장송곡 뒤 기력을 다 쓴 비류연이 그녀의 무릎을 베고 잠들었다. 돌아온 기쁨 곁에 한 사람의 빈자리가 함께 남았다." } },
            new MemoryData { Id = "m_face_in_quiet", Title = "침묵 속의 앞머리", Volume = 28, UnlockStage = 58, RewardDoo = 19200, Lines = new[] { "나예린의 무릎에서 비류연이 잠들어 있었다. 그녀는 앞머리를 치워 얼굴을 보고 싶다는 장난기를 억눌렀다.", "예전과 달라진 마음 곁에 당문혜를 잃은 배 안의 침묵이 함께 있었다." } },
            new MemoryData { Id = "m_chosen_seat", Title = "스스로 오른 자리", Volume = 28, UnlockStage = 58, RewardDoo = 19600, Lines = new[] { "예청은 비류연에게 남편 구출을 부탁하며 딸을 데려가지 말라 했다. 비류연은 예린 스스로 정할 문제라 답했다.", "출항선에 나예린이 직접 뛰어올랐다. 그의 옆에 앉는 일은 구출의 대가로 받은 허락보다 먼저 그녀가 고른 자리였다." } },
            new MemoryData { Id = "m_sister_responsibility", Title = "곁을 맡겠다는 말", Volume = 28, UnlockStage = 58, RewardDoo = 19900, Lines = new[] { "빙검은 기억이 온전하지 않은 영령이 행적을 노출할 위험을 걱정했다. 나예린은 자신이 책임지고 옆에 있겠다 청했다.", "영령은 령 언니라는 호칭과 사자의 자리 사이에서 아직 어떻게 반응할지 몰랐다. 함께 있는 마음이 모든 기억의 정답을 요구하지는 않았다." } },
            new MemoryData { Id = "m_inn_certainty", Title = "감이 아닌 확신", Volume = 28, UnlockStage = 59, RewardDoo = 20300, Lines = new[] { "나예린은 영령과 진령, 총관 담환까지 깨웠다. 소란 없이 무장한 사람들을 식당으로 불렀다.", "이번에는 감보다 확신이 먼저였다. 용안이 모든 미래를 보여 준 것은 아니어도 지금의 위험 앞에 행동할 수 있었다." } },
            new MemoryData { Id = "m_storm_wings", Title = "폭풍 속에서 배운 날개", Volume = 29, UnlockStage = 64, RewardDoo = 21000, Lines = new[] { "효효의 창을 읽던 비설보에도 한계가 왔다. 나예린은 남해의 폭풍에서 사익비홍을 시험하던 날갯짓을 떠올렸다.", "폭풍비상 비홍관운으로 뚫은 길은 모든 위협을 없애지 못했다. 효민의 음공 앞에서 검을 놓으라는 명령을 견디는 의지가 다시 필요했다." } },
            new MemoryData { Id = "m_returned_samae", Title = "돌아온 사자", Volume = 29, UnlockStage = 64, RewardDoo = 21400, Lines = new[] { "나예린이 없다는 것을 알아챈 영령은 사매라 부른 자신에게 놀랐다. 진령의 부탁에 꿈자리 때문이라며 돌아갔다.", "움직일 수 없던 나예린에게 영령의 연막과 손이 돌아왔다. 온전한 기억보다 먼저 서로를 두고 가지 않은 선택이 남았다." } },
        };

        public static readonly MemoryMilestone[] Milestones =
        {
            new MemoryMilestone { Count = 4, Doo = 4680, ItemId = "ginseng" },
            new MemoryMilestone { Count = 8, Doo = 12480, ItemId = "ilgeup_yeongdan" },
            new MemoryMilestone { Count = 13, Doo = 31200, ItemId = "ilgeup_yeongdan", ItemCount = 2 },
        };

        public static MemoryData Find(string id)
        {
            foreach (var m in All) if (m.Id == id) return m;
            return null;
        }

        public static bool IsUnlocked(SaveData save, MemoryData m) { return save.ClearedStage >= m.UnlockStage; }
        public static bool IsSeen(SaveData save, string id) { return save.SeenMemories.Contains(id); }
        public static int SeenCount(SaveData save) { return save.SeenMemories.Count; }

        public static int UnseenCount(SaveData save)
        {
            int n = 0;
            foreach (var m in All) if (IsUnlocked(save, m) && !IsSeen(save, m.Id)) n++;
            return n;
        }

        /// <summary>Marks a scene as watched. The first time it pays 두, +2 인연 with 나예린 and any milestone it completes; returns the notes (empty when already seen).</summary>
        public static List<string> Complete(SaveData save, string id)
        {
            var notes = new List<string>();
            var m = Find(id);
            if (m == null || !IsUnlocked(save, m) || IsSeen(save, id)) return notes;
            save.SeenMemories.Add(id);
            save.Doo += m.RewardDoo;
            notes.Add("은자 +" + m.RewardDoo.ToString("N0"));
            // The bond is part of her companion progress, so it only grows once she has joined the party (stage 16).
            var heroine = EarlyGameContent.FindCompanion(HeroineId);
            if (heroine != null && heroine.JoinStage <= save.HighestUnlockedStage)
            {
                int rose = Bonds.Add(save, HeroineId, BondPerMemory);
                notes.Add("나예린과의 인연 +" + BondPerMemory + (rose >= 0 ? " (" + Bonds.LevelNames[rose] + "!)" : ""));
            }
            foreach (var ms in Milestones)
            {
                if (SeenCount(save) < ms.Count || save.ClaimedMemoryMilestones.Contains(ms.Count)) continue;
                save.ClaimedMemoryMilestones.Add(ms.Count);
                save.Doo += ms.Doo;
                string text = "추억 " + ms.Count + "개 보상: 은자 +" + ms.Doo.ToString("N0");
                var item = string.IsNullOrEmpty(ms.ItemId) ? null : EarlyGameContent.FindItem(ms.ItemId);
                if (item != null) { Inventory.Add(save, item.Id, ms.ItemCount); text += ", " + item.Name + (ms.ItemCount > 1 ? " x" + ms.ItemCount : ""); }
                notes.Add(text);
            }
            return notes;
        }

        public static MemoryMilestone NextMilestone(SaveData save)
        {
            foreach (var ms in Milestones) if (!save.ClaimedMemoryMilestones.Contains(ms.Count)) return ms;
            return null;
        }
    }
}
