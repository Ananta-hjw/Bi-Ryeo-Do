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
    /// 나예린 호감도 이벤트: scenes between 비류연 and 나예린 taken from the novel (volumes 5-16, see Story/analysis), unlocked as the
    /// stages that cover them are cleared. Watching a scene for the first time pays 두 and, once she has joined, +2 인연 with 나예린; collecting scenes opens
    /// milestone rewards. Narration paraphrases the analysis notes and every spoken line is quoted from them.
    /// </summary>
    public static class Memories
    {
        public const string HeroineId = "na_yerin";
        public const int BondPerMemory = 2;

        public static readonly MemoryData[] All =
        {
            new MemoryData { Id = "m_invite", Title = "우뢰매의 초대", Volume = 5, UnlockStage = 12, RewardDoo = 1560, Lines = new[]
            {
                "삼성무제가 한창일 때, 비류연은 해동청 우뢰를 날려 나예린을 관전에 초대한다.",
                "그녀는 처음으로 그 초대를 받아들인다. 약속이 있었고, 백향관 침입자로 의심되는 사람의 실력을 직접 확인하고 싶었기 때문이다.",
                "사저 독고령이 동행한다.",
                "관중석의 나예린을 향해 비류연이 손을 흔들자 비무장이 질투의 야유로 가득 찬다.",
            } },
            new MemoryData { Id = "m_cave", Title = "무당산의 동굴", Volume = 6, UnlockStage = 14, RewardDoo = 1870, Lines = new[]
            {
                "합숙소에도 암습이 들어와 비류연이 봉황섬뢰로 정리한다. 혼자 산책하던 나예린은 암습자 일곱 명에게 포위되었다가 비류연에게 구해지고, 폭우를 피해 동굴에서 함께 머문다.",
                "나예린은 자신의 어린 시절을 이야기한다. 납치 미수만 오천육백칠십이 번이었다. 그래서 남자를 믿지 않는다고 한다.",
                "비류연|믿지 말아요. 하지만 피하기만 해서는 해결이 안 돼요.",
                "사부의 말이었다. 나예린은 그의 앞에서 두 번째로 웃었고, 비류연은 그녀가 귀엽다고 말한다.",
                "그 사이 뇌령사 결계에 걸린 침입자는 비류연이 몰래 처리한다.",
            } },
            new MemoryData { Id = "m_cheonsal", Title = "천살 앞에서", Volume = 7, UnlockStage = 14, RewardDoo = 2180, Lines = new[]
            {
                "염도가 명부오귀장을 전멸시킨 뒤, 나예린은 한상옥령신검으로 천살과 맞선다. 그러나 곧 위기에 빠지고 비류연이 그녀를 막아선다.",
                "천살의 귀요안은 눈빛으로 마음을 장악하는 초혼섭령술의 비기였지만 나예린에게는 통하지 않았다.",
                "비류연은 영사심결 절대정신방어 허무도로 귀요안을 되받아쳐 천살의 눈을 멀게 한다.",
                "묵룡환을 풀고 손가락질 하나로 초혼령을 부수고, 천살의 오른팔을 자른다.",
            } },
            new MemoryData { Id = "m_geum", Title = "묵금 연주의 밤", Volume = 7, UnlockStage = 15, RewardDoo = 2500, Lines = new[]
            {
                "무당산 합숙지가 고립된 밤. 비류연은 사면초가에 노래가 없다며 홀로 묵금을 연주한다. 나예린이 그를 찾아온다.",
                "나예린|당신은 언제나 혼자군요.",
                "비류연|나 소저도 마찬가지 아닌가요.",
                "나예린은 묵금 소리가 듣기 좋은 데 놀란다.",
            } },
            new MemoryData { Id = "m_flee", Title = "도망 작전", Volume = 7, UnlockStage = 15, RewardDoo = 2810, Lines = new[]
            {
                "무당산의 두 번째 공세를 앞두고 비류연은 염도에게 연애 상담을 한다. 동정심을 얻으려면 약해져야 한다는 조언이었다.",
                "그래서 \"도망치기\" 작전을 시도했지만, 결국 나예린의 뒤에 숨은 꼴만 되고 만다.",
            } },
            new MemoryData { Id = "m_visit", Title = "병문안", Volume = 10, UnlockStage = 19, RewardDoo = 4060, Lines = new[]
            {
                "나예린은 망설이다가 의약전으로 병문안을 온다. 그리고 비류연이 적어 준 목록대로 음식 바구니를 든 염도와 마주친다.",
                "비류연은 나예린 앞에서 병약한 척하며 음식을 순식간에 먹어 치운다.",
                "의약전 앞에서는 염도와 빙검이 서로를 외면하며 신경전을 벌이지만, 나예린은 사정을 모른다.",
            } },
            new MemoryData { Id = "m_funeral", Title = "장례식장의 두 사람", Volume = 10, UnlockStage = 20, RewardDoo = 3740, Lines = new[]
            {
                "매몰 44일째, 학관은 빈 관 둘을 놓고 장례식을 치르고 있었다. 맹주 나백천이 통곡하며 천겁우를 몰살하겠다고 맹세한 직후였다.",
                "그때 비류연이 나예린과 함께 장례식장에 나타난다.",
                "비류연|조의금은 많이 걷혔어요?",
                "나예린의 추종자들은 낙담하고, 염도와 빙검은 생환을 기뻐한다.",
                "염도|사부, 무사히 돌아오셔서 기쁩니다.",
                "마진가가 환마동에서 본 것을 묻자 비류연은 \"뛰어넘어야 할 존재(사부)와 내 자신, 그리고 차가운 선녀\"라고 답한다.",
                "마진가와 나백천은 각각 \"정말 아무 일도 없었나\" 하고 의심하고, 나예린은 얼굴을 붉히며 부인한다.",
            } },
            new MemoryData { Id = "m_father", Title = "아버지의 면담", Volume = 11, UnlockStage = 21, RewardDoo = 3120, Lines = new[]
            {
                "나백천이 비류연을 개인적으로 불러 44일 동안의 일을 캐묻는다.",
                "비류연은 \"있었을 수도 없었을 수도\"라며 도발하고, 그녀를 \"예린\"이라 부른다.",
                "그때 나예린이 들어온다.",
                "나예린|류연은 제 생명의 은인이에요.",
                "나백천은 딸이 처음으로 남자를 이름으로 부르는 것을 듣고 충격을 받는다. 이 면담 뒤 나예린은 한동안 비류연을 피한다.",
            } },
            new MemoryData { Id = "m_talk", Title = "은설란과의 연애 토크", Volume = 11, UnlockStage = 22, RewardDoo = 4370, Lines = new[]
            {
                "호북 양양에서 곡성으로 가는 길, 은설란이 나예린에게 비류연과의 사이를 캐묻는다.",
                "나예린은 부인하지만, 비류연의 이름을 부르게 된 변화만은 인정한다.",
                "나예린|내가 내가 아닌 것 같아요.",
                "은설란은 첫사랑이 \"아주 먼 곳으로 떠났다\"고 말한다.",
            } },
            new MemoryData { Id = "m_rescue", Title = "구출대", Volume = 13, UnlockStage = 25, RewardDoo = 4990, Lines = new[]
            {
                "은설란이 납치되자 비류연은 외부 접촉 금지 규칙을 깨고 구출대를 꾸린다. 나예린도 함께한다.",
                "나예린|친구의 위험을 그냥 지나칠 수 없죠.",
                "독고령이 놀란다. 풍매객잔에서는 나예린이 용안으로 표사의 거짓을 가려낸다.",
            } },
            new MemoryData { Id = "m_blood", Title = "류연! 피가", Volume = 14, UnlockStage = 27, RewardDoo = 5620, Lines = new[]
            {
                "홍매곡 입구에서 대공자와 비류연이 서로 간격을 읽으며 대치한다. 나예린은 대공자의 기운에 오한과 구역질을 느낀다.",
                "비류연이 다치자 나예린이 놀라 소리친다.",
                "나예린|류연! 피가!",
            } },
            new MemoryData { Id = "m_sword", Title = "검후의 시험", Volume = 16, UnlockStage = 29, RewardDoo = 6860, Lines = new[]
            {
                "검후 이옥상은 나예린과 비류연의 관계를 알고 \"검을 뽑아라\" 하며 내기를 건다. 비류연이 이기면 두 사람의 관계를 공인하겠다는 것이다.",
                "비류연|내가 당신을 사랑하게 된 것을 사과할 이유가 없습니다.",
                "나예린이 홍당무가 된다.",
                "비류연은 우중거 불점의로 검후의 해상비조천참절을 피해 낸다. 검후는 일단은 인정한다.",
                "검후는 나예린에게 말한다. \"많이 변했다. 마음에 솔직해져라.\"",
            } },
            new MemoryData { Id = "m_phoenix", Title = "불사조의 다짐", Volume = 16, UnlockStage = 30, RewardDoo = 9360, Lines = new[]
            {
                "대공자 비가 화룡멸겁대진으로 홍매곡을 불태우자 비류연은 풍신으로 불을 끄기로 한다.",
                "그는 나예린에게 \"성공을 위한 부적, 여신의 입맞춤\"을 요구하고, 모두가 보는 앞에서 입을 맞춘다.",
                "묵룡환을 모두 풀고 열 자루 비뢰도와 뇌령사가 나선으로 회전하며 용권풍이 불꽃을 위로 밀어 올려 모두 꺼뜨린다.",
                "비류연은 기절해 나예린의 무릎에서 쉰다.",
                "폐허가 된 홍매곡에서 두 사람은 다짐한다. \"불사조는 재 속에서 되살아난다. 빚은 만 배로 갚는다.\"",
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
            notes.Add("두 +" + m.RewardDoo.ToString("N0"));
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
                string text = "추억 " + ms.Count + "개 보상: 두 +" + ms.Doo.ToString("N0");
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
