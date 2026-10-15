using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>One line the lobby character can say. Lines unlock with progress; the text is game flavour, not canon.</summary>
    public sealed class LobbyLine
    {
        public string Speaker;
        public string Text;
        public int MinCleared;   // stages cleared before the line can appear
        public int MinBond;      // bond level with that companion (0 = any)
    }

    /// <summary>The 강호 home screen shows one character at a time (the protagonist, joined companions, the heroine) saying a short line.</summary>
    public static class Lobby
    {
        public const string Heroine = "나예린";
        public const int HeroineUnlockStage = 10;

        private static LobbyLine L(string speaker, string text, int cleared = 0, int bond = 0)
        {
            return new LobbyLine { Speaker = speaker, Text = text, MinCleared = cleared, MinBond = bond };
        }

        public static readonly LobbyLine[] Lines =
        {
            L("비류연", "오늘은 어디서 돈이 되는 일이 없을까."),
            L("비류연", "수련? 낮잠도 수련이지. 반쯤은."),
            L("비류연", "내공은 아껴 쓰는 게 남는 거야."),
            L("비류연", "다음 스테이지 준비는 됐어? 무공부터 한 번 점검해 둬."),
            L("비류연", "의뢰 게시판에 쓸 만한 일감이 올라왔나 볼까?", 3),
            L("비류연", "숙련이 오르면 변초를 고를 수 있어. 무공 탭을 확인해 봐.", 5),
            L("비류연", "백향관 내기는 내가 이긴 거다. 아무튼.", 10),
            L("비류연", "동료가 많으니 합공 한번 써 볼까.", 8),
            L("비류연", "나 소저가 기다리고 있겠지. 서둘러야겠어.", 20),

            L("효룡", "대사형, 오늘은 어디로 가죠?"),
            L("효룡", "검 수련을 같이 하실래요?", 0, 1),
            L("효룡", "대사형이 계시면 든든해요.", 0, 2),
            L("효룡", "무당산 이후로 마음가짐이 달라졌어요.", 14),

            L("장홍", "비류연 소협, 준비는 되셨습니까?"),
            L("장홍", "점혈 수련은 하루도 거를 수 없습니다."),
            L("장홍", "적의 강타는 제가 끊겠습니다.", 0, 1),

            L("윤준호", "오늘은 매화 향기가 안 나서 다행이에요."),
            L("윤준호", "형들 덕분에 힘이 나요!", 0, 1),
            L("윤준호", "칠매검, 조금씩 익숙해지고 있어요.", 22),

            L("나예린", "수련은 거르지 마세요."),
            L("나예린", "무리하지 말아요. 다친 곳은 없나요?", 15),
            L("나예린", "무사해서 다행이에요, 류연.", 20),
            L("나예린", "당신 곁에서라면 검이 흔들리지 않아요.", 16, 2),
        };

        /// <summary>Who can appear now: the protagonist, companions that have joined, and the heroine once she is part of the story.</summary>
        public static List<string> Speakers(SaveData save)
        {
            var list = new List<string> { StoryLine.Protagonist };
            foreach (var c in EarlyGameContent.CompanionsFor(Math.Max(1, save.HighestUnlockedStage))) list.Add(c.Name);
            if (save.ClearedStage >= HeroineUnlockStage && !list.Contains(Heroine)) list.Add(Heroine);
            return list;
        }

        /// <summary>Lines the given speaker may say right now.</summary>
        public static List<LobbyLine> Available(SaveData save, string speaker)
        {
            var list = new List<LobbyLine>();
            string companionId = null;
            foreach (var c in EarlyGameContent.Companions) if (c.Name == speaker) companionId = c.Id;
            int bond = companionId != null ? Bonds.Level(save, companionId) : 0;
            foreach (var line in Lines)
            {
                if (line.Speaker != speaker || save.ClearedStage < line.MinCleared) continue;
                if (line.MinBond > 0 && bond < line.MinBond) continue;
                list.Add(line);
            }
            return list;
        }

        /// <summary>Deterministic pick (step = a counter that grows with every change) so the rotation is easy to test.</summary>
        public static LobbyLine Pick(SaveData save, int step)
        {
            var speakers = Speakers(save);
            string speaker = speakers[step % speakers.Count];
            var lines = Available(save, speaker);
            if (lines.Count == 0) return new LobbyLine { Speaker = StoryLine.Protagonist, Text = "…" };
            return lines[(step / speakers.Count) % lines.Count];
        }
    }
}
