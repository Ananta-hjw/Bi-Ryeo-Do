namespace BiRyeoDo.Core
{
    public sealed class StoryEventChoice
    {
        public string Label;
        public string[] Outcome;
    }

    public sealed class StoryChoiceEvent
    {
        public string Title;
        public string[] Opening;
        public string Prompt;
        public StoryEventChoice[] Choices;
        public string[] Ending;
    }

    /// <summary>Noncombat story beats. Choices change the focus of the scene, preserving the novel's outcome.</summary>
    public static class StoryChoiceEvents
    {
        public static readonly StoryChoiceEvent RuinedContacts = new StoryChoiceEvent
        {
            Title = "무너진 연락 거점",
            Opening = new[] {
                "장홍·비류연·모용휘·남궁상은 밤의 비밀 거점을 돌았다. 나머지 일행은 신용산객잔에 머물렀다.",
                "태원잡화점에 이어 다관과 포목점, 푸줏간까지. 연락이 끊긴 거점은 무너지거나 비어 있었다. 남궁상은 뒤처리를 맡았다.",
                "우연히 빈 가게가 이어진 모양은 아니었다. 장홍은 남은 흔적으로 연락망에 무슨 일이 생겼는지 짚었다.",
            },
            Prompt = "네 거점의 연락이 끊겼습니다. 조사의 초점을 선택해 주세요.",
            Choices = new[] {
                new StoryEventChoice {
                    Label = "거점의 파괴 흔적을 읽는다",
                    Outcome = new[] {
                        "거점마다 이어지는 파괴와 침묵은 연락망을 뿌리째 지우는 작업을 가리켰다. 업계에서 말하는 식근이었다.",
                        "빈 가게만 확인하고 떠날 수는 없었다. 네 사람은 마지막으로 살피던 푸줏간의 흔적을 더 조사했다.",
                    },
                },
                new StoryEventChoice {
                    Label = "끊긴 연락망을 대조한다",
                    Outcome = new[] {
                        "태원잡화점·다관·포목점·푸줏간. 장홍이 아는 다섯 거점 중 네 곳의 연락이 끊겼고, 일행이 머문 신용산객잔만 남아 있었다.",
                        "네 곳에서 이어진 침묵을 우연으로 넘길 수 없었다. 객잔의 안전도 단정하지 않은 채, 푸줏간에 남은 단서를 마저 살폈다.",
                    },
                },
            },
            Ending = new[] {
                "푸줏간을 살피던 장홍이 돼지 몸통 속에 숨겨진 부하 상현을 찾아냈다. 무영 백이십칠호에게 아직 전할 보고가 남아 있었다.",
            },
        };

        public static readonly StoryChoiceEvent LastReport = new StoryChoiceEvent
        {
            Title = "상현의 최후 보고",
            Opening = new[] {
                "푸줏간의 돼지 몸통 속에서 찾아낸 부하 상현. 무영 백이십칠호가 장홍에게 마지막 힘으로 보고를 전했다.",
                "나백천 맹주는 무창을 탈출해 특일급 은밀 은신 장소 대난원으로 향했다. 천라지망이 발동 중이며, 부상 확률은 팔 할이었다.",
                "칠성좌가 움직일 듯하니 조심하라는 경고를 끝으로, 상현은 숨을 거뒀다.",
            },
            Prompt = "보고를 들었습니다. 먼저 짚어 볼 단서를 선택해 주세요.",
            Choices = new[] {
                new StoryEventChoice {
                    Label = "대난원과 추격망을 짚는다",
                    Outcome = new[] {
                        "나백천이 향한 곳은 대난원. 그러나 이미 천라지망이 발동했고, 부상했을 가능성도 높았다. 칠성좌의 움직임까지 경계해야 했다.",
                        "맹주의 행적을 좇기에 앞서, 연락 거점 네 곳이 무너졌는데도 일행이 머문 신용산객잔만 남아 있다는 사실을 놓칠 수 없었다.",
                    },
                },
                new StoryEventChoice {
                    Label = "남겨진 다섯째 거점을 짚는다",
                    Outcome = new[] {
                        "태원잡화점과 다관, 포목점, 푸줏간. 네 곳의 연락이 끊긴 뒤에도 다섯째 거점인 신용산객잔만 남아 있었다. 일부러 남겨 둔 덫일 가능성이 높았다.",
                        "상현이 남긴 대난원과 천라지망, 맹주의 부상 가능성과 칠성좌의 경고도 함께 새겼다. 지금은 객잔에 남은 일행에게 돌아가야 했다.",
                    },
                },
            },
            Ending = new[] {
                "네 사람은 상현의 마지막 보고를 안고 신용산객잔으로 서둘러 돌아갔다.",
            },
        };

        public static readonly StoryChoiceEvent InnEncirclement = new StoryChoiceEvent
        {
            Title = "신용산객잔 포위 · 나예린의 확신",
            Opening = new[] {
                "객잔의 나예린은 낯선 기류를 느꼈다. 모든 미래를 안 것은 아니어도 지금의 위험은 외면할 수 없었다.",
                "나예린은 영령을 깨우고 진령에게 몇 마디를 속삭였다. 대원들은 조용히 무장하고 식당으로 내려왔다. 총관 담환도 깨어 있었다.",
                "염도와 빙검은 술을 마시던 자리에서 경고를 들었다. 이번 것은 감이 아니라 확신이었다. 그러나 객잔의 포위는 이미 시작된 뒤였다.",
            },
            Prompt = "일행이 모였습니다. 대피 준비에서 짚어 볼 사항을 선택해 주세요.",
            Choices = new[] {
                new StoryEventChoice {
                    Label = "일행에게 전할 경고를 정리한다",
                    Outcome = new[] {
                        "나예린의 용안은 모든 미래의 정답을 보여 주는 힘이 아니었다. 빙검은 아직 만개한 것은 아니라고 보았다. 그럼에도 지금 움직여야 한다는 확신은 분명했다.",
                        "이미 시작된 포위를 없던 일로 만들 수는 없었다. 조용히 무장한 일행에게는 맞서 싸우기보다 빠져나갈 길과 시간이 필요했다.",
                    },
                },
                new StoryEventChoice {
                    Label = "비밀 통로로 빠질 준비를 점검한다",
                    Outcome = new[] {
                        "총관 담환이 깨어 있었고, 일행은 조용히 무장한 채 모였다. 성 밖으로 이어지는 비밀 통로로 움직이는 동안 누군가는 추격을 막아야 했다.",
                        "염도와 빙검이 시간을 벌고 나머지가 빠져나가기로 했다. 나예린의 확신이 마련한 것은 포위가 사라진 미래가 아니라 지금 행동할 준비였다.",
                    },
                },
            },
            Ending = new[] {
                "염도와 빙검은 객잔 앞에 남아 시간을 벌기로 했다. 나머지 일행은 담환의 비밀 통로로 성 밖으로 빠질 준비를 했다.",
                "염도는 영령에게 나예린을 지키라 일렀다. 진령에게는 대사형이 죽게 하지는 않을 것이라 전했다. 두 노사의 엄호전은 이제부터였다.",
            },
        };

        public static string[] Outcome(StoryChoiceEvent ev, int choice)
        {
            if (ev == null || choice < 0 || choice >= ev.Choices.Length)
                throw new System.ArgumentOutOfRangeException(nameof(choice));
            return ev.Choices[choice].Outcome;
        }
    }
}
