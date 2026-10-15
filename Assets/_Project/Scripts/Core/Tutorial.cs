using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>One help topic: a few short pages shown once automatically at the right moment and always available from the help list.</summary>
    public sealed class TutorialTopic
    {
        public string Id, Title;
        public string[] Pages;
    }

    public static class Tutorial
    {
        public static readonly TutorialTopic[] All =
        {
            new TutorialTopic { Id = "intro", Title = "강호 화면", Pages = new[]
            {
                "맨 위 큰 카드는 다음에 도전할 스테이지다. 「출전」을 누르면 이야기가 나오고 전투가 이어진다.",
                "카드 아래 메뉴: 승급 시험장(검룡위 올리기), 파티 편성(함께 싸울 동료 고르기), 의뢰 게시판(조건이 붙은 전투), 기연(선택지 이벤트). 붉은 숫자는 새로 할 수 있는 일이 있다는 뜻이다.",
                "아래 탭: 강호(스테이지), 무공(배우고 장착하기), 행낭(장비 착용과 재료 판매). 전투로 경험과 두를 얻고, 두로 무공을 익힌다."
            } },
            new TutorialTopic { Id = "battle", Title = "전투 조작", Pages = new[]
            {
                "내 차례가 오면 명령을 고른다. 「공격」은 내공이 들지 않는다. 무공은 내공을 쓰고 한 번 쓰면 몇 턴 기다려야 한다.",
                "적 그림을 누르면 공격 대상이 바뀐다(금색 테두리). 적 아래 글은 다음 행동 예고다. 「위험!」이 뜨면 강한 공격이 곧 온다. 방어, 회피, 점혈로 대비하거나 끊어라.",
                "막대는 초록 체력, 파랑 내공, 금색 기세다. 기세가 가득 차면 비기를 쓸 수 있다. 「운기」는 내공과 체력을 조금 회복한다.",
                "위쪽 순서 줄은 누가 다음에 움직이는지 알려 준다. 이기면 전투 사이에 정비 화면이 나오니, 준비가 되면 다음 전투에 도전하자."
            } },
            new TutorialTopic { Id = "joint", Title = "합공과 인연", Pages = new[]
            {
                "동료와 같은 스테이지를 깰수록 인연이 쌓인다. 인연이 친분 이상이 되면 비류연의 차례에 「합공」 명령이 나타난다.",
                "합공은 기세 50을 쓰고, 비류연과 동료가 연달아 때린다. 대신 동료는 다음 행동을 쓴다. 효과는 동료마다 다르다: 방어, 점혈, 치유, 관통."
            } },
            new TutorialTopic { Id = "mastery", Title = "무공 숙련과 변초", Pages = new[]
            {
                "비류연의 무공은 쓸수록 숙련된다. 입문, 소성, 대성, 극의 순서로 오르고 단계마다 위력이 5%씩 늘어난다.",
                "대성부터 무공 탭에서 변초를 고를 수 있다. 강은 더 세게, 쾌는 내공을 아끼고 빨리, 연은 한 번 더 때린다. 언제든 바꿀 수 있다."
            } },
            new TutorialTopic { Id = "board", Title = "의뢰와 기연", Pages = new[]
            {
                "의뢰 게시판은 지나온 싸움에 시간 제한이나 버티기 조건을 붙여 다시 도전한다. 한 번씩만 받을 수 있고 두와 보상을 얻는다.",
                "기연은 스테이지를 깨면 열리는 선택지 이벤트다. 두, 경험, 아이템, 인연, 숙련 중 무엇을 얻을지 직접 고른다."
            } },
            new TutorialTopic { Id = "gacha", Title = "뽑기", Pages = new[]
            {
                "처음에 받은 장비 말고는, 새 장비를 뽑기로 얻는다. 뽑기 탭에서 두를 내고 장비와 영약을 뽑는다. 나오는 등급은 일반 60%, 고급 20%, 희귀 10%, 영웅 6%, 전설 3%, 신화 1%다.",
                "장비는 무기, 방어구, 장신구 두 개까지 착용한다. 이미 가진 장비가 또 나오면 분해되어 등급에 따른 두가 돌아온다. 10회 뽑기는 10% 싸다. 뽑은 장비는 행낭 탭에서 착용한다."
            } },
            new TutorialTopic { Id = "promotion", Title = "승급 시험장", Pages = new[]
            {
                "승급 시험장에서 비무첩을 사서 상대 둘을 연달아 꺾으면 검룡위가 오른다. 입장료가 필요하고 지면 돌려받지 못한다.",
                "한 판은 12턴 안에 끝내야 하며 시간이 넘으면 무승부다. 오검룡 이상이어야 삼성무제(스테이지 12, 13)에 나갈 수 있다."
            } },
        };

        public static TutorialTopic Find(string id)
        {
            foreach (var t in All) if (t.Id == id) return t;
            return null;
        }

        public static bool IsDone(SaveData save, string id) { return save.TutorialDone.Contains(id); }

        public static void MarkDone(SaveData save, string id)
        {
            if (!save.TutorialDone.Contains(id)) save.TutorialDone.Add(id);
        }
    }
}
