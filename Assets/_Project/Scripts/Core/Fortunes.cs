using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>One option of a 기연. Costs and gains apply together; a choice with Cost > 두 owned is unavailable.</summary>
    public sealed class FortuneChoice
    {
        public string Label, Result;
        public long Doo;            // gained (negative = price)
        public double ExpLevels;    // exp as a fraction of the current level-up cost
        public string ItemId;
        public int BondPoints;      // to every companion that has joined
        public int MasteryUses;     // free practice for each equipped art
    }

    /// <summary>A short choice scene between stages. Flavour is game-layer (not canon); each event happens once.</summary>
    public sealed class FortuneEvent
    {
        public string Id, Title;
        public int UnlockStage;     // available after clearing this stage
        public string[] Text;
        public FortuneChoice[] Choices;
    }

    public static class Fortunes
    {
        public static readonly FortuneEvent[] All =
        {
            new FortuneEvent { Id = "f_waterfall", Title = "폭포 아래의 밤", UnlockStage = 1,
                Text = new[] { "달빛이 쏟아지는 폭포 아래. 물소리 때문에 잠이 오지 않는다.", "이 시간을 어떻게 쓸까?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "폭포를 맞으며 수련한다", Result = "물살을 버텨내며 호흡이 한결 안정되었다. 경험 +", ExpLevels = 0.5 },
                    new FortuneChoice { Label = "무공 초식을 반복해 익힌다", Result = "장착한 무공을 밤새 반복했다. 숙련이 올랐다.", MasteryUses = 6 },
                    new FortuneChoice { Label = "그냥 푹 잔다", Result = "개운하게 잤다. 일어나 보니 머리맡에 약간의 은자가 놓여 있다.", Doo = 470 },
                } },
            new FortuneEvent { Id = "f_river", Title = "강가의 낚시꾼", UnlockStage = 2,
                Text = new[] { "강가에서 노인이 낚싯대를 드리우고 있다. 말없이 곁자리를 내주었다.", "이 한가한 시간을 어떻게 보낼까?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "낚시를 거들어 준다", Result = "잡은 물고기를 시장에 넘겨 노인이 몫을 나눠 주었다.", Doo = 620 },
                    new FortuneChoice { Label = "노인의 호흡을 따라 해 본다", Result = "느린 호흡 속에서 장착한 무공의 감이 한결 잡혔다.", MasteryUses = 8 },
                    new FortuneChoice { Label = "구운 생선을 먹으며 쉰다", Result = "배불리 먹고 한숨 자고 나니 몸이 가벼워졌다. 경험 +", ExpLevels = 0.4 },
                } },
            new FortuneEvent { Id = "f_peddler", Title = "수상한 행상", UnlockStage = 4,
                Text = new[] { "길가에서 행상이 보따리를 펼친다. \"귀한 영약이오. 지금 아니면 못 구하오.\"", "믿을 만한 물건일까?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "영약을 산다 (-800은자)", Result = "한 병을 샀다. 진짜 영약이었다!", Doo = -800, ItemId = "ginseng" },
                    new FortuneChoice { Label = "흥정해서 약초만 산다 (-200은자)", Result = "약초 한 뿌리를 싸게 샀다.", Doo = -200, ItemId = "bear_gall" },
                    new FortuneChoice { Label = "지나친다", Result = "행상은 아쉬운 얼굴로 보따리를 쌌다." },
                } },
            new FortuneEvent { Id = "f_campfire", Title = "모닥불 곁의 밤", UnlockStage = 7,
                Text = new[] { "동료들과 모닥불을 둘러싸고 앉았다. 오늘은 별이 유난히 많다.", "남은 밤을 어떻게 보낼까?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "다 같이 술과 이야기를 나눈다", Result = "웃음이 오가며 서로를 한결 편하게 대하게 되었다.", BondPoints = 2 },
                    new FortuneChoice { Label = "차분히 합동 수련을 한다", Result = "함께 호흡을 맞춰 보았다. 경험을 얻었고 서로를 알게 되었다. 경험 +", ExpLevels = 0.35, BondPoints = 1 },
                } },
            new FortuneEvent { Id = "f_quiet_route", Title = "생문을 기다리는 준비", UnlockStage = 9,
                Text = new[] { "백향관 잠입 전 한 달의 준비를 되짚습니다. 도면과 기관, 진법의 생문이 열리는 때를 확인하는 회상입니다.", "준비는 신중했지만 학관이 허가한 공식 시험은 아니었다." }, Choices = new[] {
                    new FortuneChoice { Label = "도면과 경계 순서를 대조한다", Result = "무작정 돌파하는 대신 물러날 길까지 살폈습니다.", ExpLevels = .15 },
                    new FortuneChoice { Label = "봉황무의 발디딤을 복기한다", Result = "이미 익힌 움직임을 조용히 되짚었습니다.", MasteryUses = 3 },
                } },
            new FortuneEvent { Id = "f_inn", Title = "객잔의 소문", UnlockStage = 9,
                Text = new[] { "저잣거리 객잔에 들어서니 온갖 소문이 오간다.", "어떤 이야기에 귀를 기울일까?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "정보꾼에게 소문을 산다 (-600은자)", Result = "값진 이야기였다. 큰 깨달음을 얻었다. 경험 +", Doo = -600, ExpLevels = 0.9 },
                    new FortuneChoice { Label = "일행과 술잔을 기울인다 (-100은자)", Result = "시끌벅적한 술자리에서 서로가 한결 가까워졌다.", Doo = -100, BondPoints = 2 },
                    new FortuneChoice { Label = "남의 대화에는 신경 쓰지 않는다", Result = "자리에 떨어진 두 몇 푼을 주웠다.", Doo = 550 },
                } },
            new FortuneEvent { Id = "f_three_coins", Title = "당철영의 동전 세 개", UnlockStage = 12,
                Text = new[] { "당문천에게 비류연을 조심하라던 당철영은 삼환비선의 동전 세 개를 받아냈다.", "일상처럼 반복한 구슬꿰기가 손끝의 감각을 키웠다. 그 성과를 돌아보며 지금의 수련을 가다듬어 본다." }, Choices = new[] {
                    new FortuneChoice { Label = "꾸준한 구슬 수련을 되새긴다", Result = "화려한 비급 대신 반복한 손놀림에서 성취를 찾았습니다.", MasteryUses = 4 },
                    new FortuneChoice { Label = "성과와 방심을 함께 기록한다", Result = "성취가 모든 실전의 승리를 보장하지 않는다는 것을 기억합니다.", ExpLevels = .15 },
                } },
            new FortuneEvent { Id = "f_cave", Title = "이름 모를 동굴", UnlockStage = 12,
                Text = new[] { "산길 옆에 오래된 동굴이 입을 벌리고 있다. 안쪽에서 희미한 기운이 느껴진다.", "들어가 볼까?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "깊이 들어가 조사한다", Result = "한참 헤맸지만 선인의 흔적에서 깨달음을 얻었다. 경험 +", ExpLevels = 0.9 },
                    new FortuneChoice { Label = "입구 근처만 살핀다", Result = "버려진 짐 속에서 쓸 만한 약병을 찾았다.", ItemId = "okruju" },
                    new FortuneChoice { Label = "위험하니 돌아선다", Result = "무사히 돌아왔다. 길에서 두 몇 푼을 주웠다.", Doo = 780 },
                } },
            new FortuneEvent { Id = "f_teacher_net", Title = "그물의 한 올", UnlockStage = 14,
                Text = new[] { "현검자는 천뢰무망 쇄천을 보고 자신의 천망과 평생의 강박을 되돌아봤다.", "비류연이 새 비급을 받은 사건이 아닙니다. 이미 보았던 공방을 차분히 복기해 본다." }, Choices = new[] {
                    new FortuneChoice { Label = "힘과 속도의 차이를 복기한다", Result = "성긴 그물의 한 올에 담긴 힘을 되짚었습니다.", MasteryUses = 4 },
                    new FortuneChoice { Label = "스스로 세운 한계를 돌아본다", Result = "자유로워야 한다는 강박도 속박일 수 있었습니다.", ExpLevels = .15 },
                } },
            new FortuneEvent { Id = "f_junho_advice", Title = "비급이 아닌 기연", UnlockStage = 14,
                Text = new[] { "준호는 현검자의 조언과 공감이 항아리 수련을 버티는 힘이 되었음을 깨달았다.", "그의 항아리는 비었지만 검향 과민 반응이 완치되거나 새로운 비급을 받은 것은 아니었다." }, Choices = new[] {
                    new FortuneChoice { Label = "동료의 수련을 격려한다", Result = "염도의 말처럼 이로운 사람과의 만남도 기연입니다.", BondPoints = 1 },
                    new FortuneChoice { Label = "간결한 운용을 함께 복습한다", Result = "물속의 한 수와 항아리의 집중을 차분히 되짚었습니다.", MasteryUses = 3 },
                } },
            new FortuneEvent { Id = "f_blacksmith", Title = "늙은 대장장이", UnlockStage = 14,
                Text = new[] { "산 아래 대장간에서 늙은 대장장이가 쇠를 두드리고 있다.", "무슨 일로 들렀는가?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "일손을 거든다", Result = "풀무질을 도왔더니 품삯을 두둑이 받았다.", Doo = 1870 },
                    new FortuneChoice { Label = "망치질을 지켜보며 운기한다", Result = "쇠를 두드리는 박자에 호흡을 맞추었다. 경험 +", ExpLevels = 0.6 },
                    new FortuneChoice { Label = "비상약을 맡겨 둔 것을 찾는다 (-900은자)", Result = "대장장이가 몰래 달여 둔 귀한 약을 건네주었다.", Doo = -900, ItemId = "ginseng" },
                } },
            new FortuneEvent { Id = "f_guard_distinction", Title = "호위가 읽어야 할 것", UnlockStage = 16, Text = new[] { "쟁반을 피해 민간인을 다치게 하는 대신 나예린은 베어 막았고 모용휘는 다른 비도를 받았다.", "행인 모두가 적은 아니었다. 지난 공방의 판단을 되새겨 본다." }, Choices = new[] {
                new FortuneChoice { Label = "위험과 행인을 구분해 복기한다", Result = "공격을 더하는 것보다 무엇을 막을지 먼저 읽었습니다.", MasteryUses = 4 },
                new FortuneChoice { Label = "동료와 맡은 방향을 돌아본다", Result = "함께 막아낸 순간을 기억하며 호흡을 맞췄습니다.", BondPoints = 1 },
            } },
            new FortuneEvent { Id = "f_master_scroll", Title = "낡은 비급 조각", UnlockStage = 17,
                Text = new[] { "헌책방 구석에서 낡은 비급 조각을 발견했다. 내용은 알아볼 수 없다.", "어떻게 할까?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "풀이를 사서 깊이 연구한다 (-3000은자)", Result = "며칠 밤을 새워 연구했다. 모든 장착 무공이 크게 숙련되었다.", Doo = -3000, MasteryUses = 25 },
                    new FortuneChoice { Label = "되팔아 돈으로 바꾼다", Result = "수집가가 후하게 값을 쳐주었다.", Doo = 3900 },
                } },
            new FortuneEvent { Id = "f_rope_restraint", Title = "힘을 아끼는 발판", UnlockStage = 18, Text = new[] { "줄 위에서는 강한 일격만으로 승부가 나지 않았다. 끊거나 떨어지는 일까지 살펴야 했다.", "효룡이 항복한 뒤 발판을 읽는 감각을 되짚는다." }, Choices = new[] {
                new FortuneChoice { Label = "장력과 발디딤을 복기한다", Result = "기술과 발판을 한 번에 읽는 감각을 되살렸습니다.", MasteryUses = 4 },
                new FortuneChoice { Label = "힘을 과하게 쓰지 않는 호흡을 찾는다", Result = "안정된 호흡으로 집중을 이어 갔습니다.", ExpLevels = .15 },
            } },
            new FortuneEvent { Id = "f_inquiry_limits", Title = "밝혀진 것과 남은 것", UnlockStage = 19, Text = new[] { "팔비는 비영각 신분을 밝혔지만 의뢰인은 말하지 않았다. 학관의 암류와 위지천의 힘도 아직 같은 선으로 증명되지 않았다.", "누가 무엇을 직접 보았는지 기록해 본다." }, Choices = new[] {
                new FortuneChoice { Label = "확인한 증언부터 분리한다", Result = "추측을 범인의 이름으로 바꾸지 않고 남은 물음을 적었습니다.", ExpLevels = .15 },
                new FortuneChoice { Label = "친구가 말할 때까지 기다린다", Result = "말하지 못하는 사정을 억지 고백으로 바꾸지 않았습니다.", BondPoints = 1 },
            } },
            new FortuneEvent { Id = "f_monk", Title = "산사의 노승", UnlockStage = 19,
                Text = new[] { "고요한 산사. 노승이 마당을 쓸다 말고 이쪽을 본다.", "\"검을 쥔 손에 물어볼 것이 많아 보이는구려.\"" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "선문답을 나눈다", Result = "알 듯 말 듯한 말 속에서 마음이 맑아졌다. 경험 +", ExpLevels = 1.0 },
                    new FortuneChoice { Label = "시주하고 수련을 청한다 (-1500은자)", Result = "노승이 직접 초식을 바로잡아 주었다. 일행도 곁에서 함께 배웠다.", Doo = -1500, MasteryUses = 15, BondPoints = 1 },
                    new FortuneChoice { Label = "공양만 하고 떠난다", Result = "노승이 공양 답례로 약병을 쥐여 주었다.", ItemId = "okruju" },
                } },
            new FortuneEvent { Id = "f_cave_supplies", Title = "사십사 일의 준비를 돌아보다", UnlockStage = 20, Text = new[] { "금창약·붕대, 빛을 내는 검, 식량과 물, 침낭포와 벽곡단이 각기 다른 역할을 했다.", "두 사람이 함께 살아 나온 뒤 그 판단을 되짚는다." }, Choices = new[] {
                new FortuneChoice { Label = "빛과 공기, 배급의 순서를 적는다", Result = "불을 피우기 전에 공기부터 살피던 준비를 기억했습니다.", ExpLevels = .2 },
                new FortuneChoice { Label = "서로의 판단을 믿은 순간을 나눈다", Result = "나예린의 지식과 비류연의 준비가 함께 필요했던 여정이었습니다.", BondPoints = 1 },
            } },
            new FortuneEvent { Id = "f_mirror_return", Title = "거울 너머 돌아올 길", UnlockStage = 21, Text = new[] { "주작단은 먼 산의 빛을 찾는 동안 돌아올 힘도 남겨야 했다. 성공과 실패를 지나 얻은 감각을 되새긴다.", "추가 동행자가 주작단 새 단원이 된 것은 아니었다." }, Choices = new[] {
                new FortuneChoice { Label = "호흡과 귀환 경로를 복기한다", Result = "먼 곳에 닿은 뒤의 여력까지 계산했습니다.", ExpLevels = .15 },
                new FortuneChoice { Label = "신호를 읽던 집중을 되살린다", Result = "이미 익힌 무공의 집중을 다듬었습니다.", MasteryUses = 4 },
            } },
            new FortuneEvent { Id = "f_witness_notes", Title = "죽은 증인에게 묻지 않기", UnlockStage = 22, Text = new[] { "흑랑채의 흔적과 부자의 신뢰는 조사를 이어 갈 길을 열었다. 죽은 증인과 추격자에게 자백을 받을 수는 없었다.", "이송학은 안전한 곳에 맡겼다. 지금 함께 걷는 동료의 말부터 들어 본다." }, Choices = new[] {
                new FortuneChoice { Label = "확인한 사실을 기록한다", Result = "추측과 증언을 나눠 적었습니다.", ExpLevels = .15 },
                new FortuneChoice { Label = "첫 살인의 충격을 겪은 친구 곁에 앉는다", Result = "자랑이나 고백을 재촉하지 않고 함께 쉬었습니다.", BondPoints = 1 },
            } },
            new FortuneEvent { Id = "f_reunion", Title = "오랜 동료와의 술자리", UnlockStage = 22,
                Text = new[] { "싸움이 잠시 잦아든 밤, 일행이 한자리에 모였다.", "오늘만큼은 마음을 터놓을 수 있을 것 같다." },
                Choices = new[]
                {
                    new FortuneChoice { Label = "속마음을 이야기한다", Result = "서로의 이야기를 들으며 신뢰가 한 단계 깊어졌다.", BondPoints = 4 },
                    new FortuneChoice { Label = "함께 합공 수련을 한다", Result = "호흡을 맞추는 연습에 땀을 흘렸다. 경험과 인연을 얻었다. 경험 +", ExpLevels = 0.5, BondPoints = 2 },
                } },
            new FortuneEvent { Id = "f_healing_before_flute", Title = "피리보다 먼저 손볼 것", UnlockStage = 24, Text = new[] { "낙뢰곡 뒤 돌밭에서 야영하며 열 명 넘는 부상자를 돌봤다. 당삼은 사충적 연구보다 해독을 먼저 했다.", "서른 금과 열 금의 외상은 곧바로 받은 보상이 아니었다." }, Choices = new[] {
                new FortuneChoice { Label = "치료와 보급 순서를 되짚는다", Result = "급한 일을 앞에 두고 다음 걸음을 준비했습니다.", ExpLevels = .15 },
                new FortuneChoice { Label = "부상한 동료의 회복을 돕는다", Result = "함께 버틴 시간을 돌아보며 서로를 살폈습니다.", BondPoints = 1 },
            } },
            new FortuneEvent { Id = "f_snownight", Title = "눈 내리는 밤", UnlockStage = 24,
                Text = new[] { "눈이 쌓인 산길. 야영지의 모닥불이 잦아든다.", "차가운 밤을 어떻게 넘길까?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "서로 곁에 앉아 체온을 나눈다", Result = "말은 적었지만 서로를 믿는 마음이 한층 깊어졌다.", BondPoints = 3 },
                    new FortuneChoice { Label = "눈 속에서 호흡을 가다듬는다", Result = "한기를 이겨내며 기운이 단단해졌다. 경험과 인연을 얻었다. 경험 +", ExpLevels = 0.7, BondPoints = 1 },
                    new FortuneChoice { Label = "홀로 칼날을 닦으며 초식을 되새긴다", Result = "밤새 초식을 복기했다. 장착한 무공이 크게 숙련되었다.", MasteryUses = 12 },
                } },
            new FortuneEvent { Id = "f_cart_records", Title = "기록이 이어 준 길", UnlockStage = 25, Text = new[] { "마차 스물다섯에서 열다섯, 여덟 대와 여섯 행선지로 좁혀 갔다. 마지막 단서는 새로 경비가 붙은 장원이었다.", "화산에 모인 세 마차를 모두 범행 장소로 몰아붙이지 않았다." }, Choices = new[] {
                new FortuneChoice { Label = "기록을 대조한 순서를 복기한다", Result = "확인할 수 있는 변화부터 좁히는 판단을 익혔습니다.", ExpLevels = .2 },
                new FortuneChoice { Label = "잠입의 조용한 손놀림을 되짚는다", Result = "힘보다 정확함이 필요했던 순간을 기억했습니다.", MasteryUses = 4 },
            } },
            new FortuneEvent { Id = "f_watch_agreement", Title = "잠을 위한 작은 합의", UnlockStage = 27, Text = new[] { "태양관의 이틀 밤 뒤 불침번을 정했다. 서로 잠들 수 있다는 첫 합의였지만 뒤의 분계선 다툼까지 지우지는 못했다.", "그날의 작은 타협을 돌아본다." }, Choices = new[] {
                new FortuneChoice { Label = "교대하며 살핀 사람을 떠올린다", Result = "함께 쉬기 위해 맡은 몫을 생각했습니다.", BondPoints = 1 },
                new FortuneChoice { Label = "경계와 휴식의 순서를 복기한다", Result = "다음 걸음을 위한 여력을 계산했습니다.", ExpLevels = .15 },
            } },
            new FortuneEvent { Id = "f_muyong_question", Title = "아직 답하지 못한 물음", UnlockStage = 28, Text = new[] { "혁 노야가 음양이기와 물극필반을 물었을 때 모용휘는 자기 한계를 떠올렸다.", "답을 대신 정해 주지 않고 이미 익힌 길부터 돌아본다." }, Choices = new[] {
                new FortuneChoice { Label = "장착한 초식의 변화를 복기한다", Result = "다른 모양 속에서도 이어지는 움직임을 살폈습니다.", MasteryUses = 4 },
                new FortuneChoice { Label = "서둘러 한계를 정하지 않고 호흡한다", Result = "지금의 질문을 남겨 두고 다음 수련을 준비했습니다.", ExpLevels = .15 },
            } },
            new FortuneEvent { Id = "f_baton_trust", Title = "바통을 놓지 않은 손", UnlockStage = 28, Text = new[] { "준호는 다친 뒤에도 바통을 가져왔다. 비류연은 늦지 않았다며 친구라 답했다.", "공동 일위라는 판정 뒤에도 그 신뢰는 남았다." }, Choices = new[] {
                new FortuneChoice { Label = "친구가 견딘 길을 함께 돌아본다", Result = "순위보다 먼저 지킨 약속을 기억했습니다.", BondPoints = 1 },
                new FortuneChoice { Label = "돌아올 힘을 남기는 달리기를 복기한다", Result = "기와 호흡을 아끼던 조언을 되짚었습니다.", ExpLevels = .2 },
            } },
            new FortuneEvent { Id = "f_eve", Title = "결전 전야", UnlockStage = 28,
                Text = new[] { "큰 싸움을 앞둔 밤, 아무도 쉽게 잠들지 못한다.", "마지막 준비를 어떻게 할까?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "일행과 함께 살아 돌아오자고 맹세한다", Result = "서로의 손을 맞잡았다. 이보다 든든한 밤은 없었다.", BondPoints = 5 },
                    new FortuneChoice { Label = "밤새 운기조식에 몰두한다", Result = "기운이 한계까지 차올랐다. 경험 +", ExpLevels = 1.3 },
                    new FortuneChoice { Label = "비상약을 챙겨 둔다", Result = "행낭 깊숙이 비장의 영단을 챙겼다.", ItemId = "ilgeup_yeongdan" },
                } },
            new FortuneEvent { Id = "f_camp_work", Title = "동굴에서 나눈 일", UnlockStage = 29, Text = new[] { "나예린이 찾은 동굴을 위장하고 물·식량·마른 장작을 마련했다. 우뢰매의 사냥과 길잡이도 도움이 됐다.", "생존은 혼자 잘 싸우는 일만으로 끝나지 않았다." }, Choices = new[] {
                new FortuneChoice { Label = "거점 준비의 순서를 적는다", Result = "그동안 익힌 생활 기술을 다시 살폈습니다.", ExpLevels = .2 },
                new FortuneChoice { Label = "힘을 아끼는 손놀림을 복기한다", Result = "장착 무공의 기본 움직임을 차분히 다듬었습니다.", MasteryUses = 4 },
            } },
            new FortuneEvent { Id = "f_shared_responsibility", Title = "나누어 질 몫", UnlockStage = 30, Text = new[] { "빙검의 자책 앞에서 염도는 자신에게도 책임질 몫이 있다 말했다.", "살아남은 이들의 회복과 전승은 그 뒤부터 시작이었다." }, Choices = new[] {
                new FortuneChoice { Label = "곁의 동료를 살핀다", Result = "혼자 삼키지 않아도 될 마음을 나눴습니다.", BondPoints = 1 },
                new FortuneChoice { Label = "다음에 지켜야 할 것을 기록한다", Result = "승패 뒤에 남은 책임을 돌아봤습니다.", ExpLevels = .2 },
            } },
            new FortuneEvent { Id = "f_white_tiger", Title = "술동이 앞의 백호", UnlockStage = 31,
                Text = new[] { "산길 모퉁이에서 커다란 하얀 호랑이가 빈 술동이 앞에 앉아 이쪽을 본다.", "황금빛 눈이 무언가를 기다리는 것 같다." },
                Choices = new[]
                {
                    new FortuneChoice { Label = "주막에서 술 한 동이를 사 온다 (-1200은자)", Result = "호랑이가 단숨에 비우고 꼬리를 흔들며 사라졌다. 그 자리에 산삼 한 뿌리가 놓여 있다.", Doo = -1200, ItemId = "ginseng" },
                    new FortuneChoice { Label = "눈을 마주친 채 천천히 물러난다", Result = "등골이 서늘한 긴장 속에서 기감이 한층 날카로워졌다. 경험 +", ExpLevels = 0.8 },
                    new FortuneChoice { Label = "동료들과 한달음에 달아난다", Result = "한참을 달린 끝에 서로의 꼴을 보고 다 같이 웃음을 터뜨렸다.", BondPoints = 3 },
                } },
            new FortuneEvent { Id = "f_armband", Title = "완장을 노리는 도전장", UnlockStage = 32,
                Text = new[] { "객잔 문 앞에 입관 희망자가 도전장을 들고 서 있다. 시선은 오른팔의 황금 완장에 박혀 있다.", "\"한 수 가르침을 청합니다!\"" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "정식으로 비무를 받아 준다", Result = "제법 매서운 검이었다. 받아 내는 사이 손에 감각이 붙었다.", MasteryUses = 12 },
                    new FortuneChoice { Label = "한 수 가르쳐 주고 수강료를 받는다", Result = "도전자는 고개를 숙이고 두둑한 수강료를 내밀었다.", Doo = 3360 },
                    new FortuneChoice { Label = "동료에게 상대를 맡긴다", Result = "동료가 멋지게 이겨 냈다. 다 함께 축배를 들었다.", BondPoints = 2, ExpLevels = 0.3 },
                } },
            new FortuneEvent { Id = "f_pillow", Title = "굴러 나온 목침", UnlockStage = 35,
                Text = new[] { "표국 별채 앞에 박달나무 목침 하나가 굴러 나와 있다. 안에서는 우렁찬 코골이가 들린다.", "어떻게 할까?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "조심스레 문 앞에 돌려놓는다", Result = "코골이가 잦아들고, 문틈으로 술값 정도의 은자가 굴러 나왔다.", Doo = 1870 },
                    new FortuneChoice { Label = "문을 두드려 깨운다", Result = "날아온 목침에 이마를 맞고 기절했다. 깨어나니 이상하게 머리가 맑다. 경험 +", ExpLevels = 1.0 },
                    new FortuneChoice { Label = "동료들과 살금살금 물러난다", Result = "숨죽여 물러나다 서로 눈이 마주쳐 웃음을 참느라 혼났다.", BondPoints = 3 },
                } },
            new FortuneEvent { Id = "f_wager", Title = "대연무장의 내기판", UnlockStage = 36,
                Text = new[] { "대연무장 구석에 투명 삼인방이 차린 내기판이 성황이다. 판돈이 쌓여 간다.", "\"자, 마감 전입니다! 어느 쪽에 거시겠소?\"" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "남궁상에게 건다", Result = "상식이 깨졌다. 배당을 두둑이 받았다.", Doo = 3900 },
                    new FortuneChoice { Label = "판을 돕고 수수료를 받는다", Result = "장부를 맞춰 주었더니 삼인방이 수고비를 쥐여 주었다. 동료들과 나눠 먹었다.", Doo = 1560, BondPoints = 1 },
                    new FortuneChoice { Label = "비무만 집중해서 본다", Result = "이기어검의 분영을 눈에 새겼다. 장착 무공의 감이 잡혔다.", MasteryUses = 15 },
                } },
            new FortuneEvent { Id = "f_unanswered_commission", Title = "답을 정하지 않은 의뢰", UnlockStage = 31, Text = new[] { "혁중은 모용휘에게 우상이 된 조부의 세계를 넘어서라는 의뢰를 남겼다.", "목표의 말만으로 실행 결과와 대가까지 정해진 것은 아니었다." }, Choices = new[] {
                new FortuneChoice { Label = "자신의 검이 향할 곳을 돌아본다", Result = "남의 명성보다 자기 선택을 먼저 보았습니다.", ExpLevels = .2 },
                new FortuneChoice { Label = "함께 짊어질 책임을 묻는다", Result = "아직 답하지 않은 물음을 동료와 나눴습니다.", BondPoints = 2 },
            } },
            new FortuneEvent { Id = "f_sealed_legacy", Title = "열지 않은 유산", UnlockStage = 31, Text = new[] { "유재룡은 돈주머니와 봉한 짐, 옥패를 아이들에게 맡겼다. 황룡시 은행 금고 열쇠는 또 다른 단서였다.", "봉한 짐의 내용이나 금고 속 재산을 짐작만으로 자기 몫이라 할 수 없었다." }, Choices = new[] {
                new FortuneChoice { Label = "맡겨진 것과 알지 못하는 것을 구분한다", Result = "기록을 정돈하며 판단을 가다듬었습니다.", ExpLevels = .2 },
                new FortuneChoice { Label = "남겨진 아이들의 안전을 먼저 생각한다", Result = "사람을 지켜야 유산도 의미를 이었습니다.", BondPoints = 2 },
            } },
            new FortuneEvent { Id = "f_two_cases", Title = "닮은 상처, 다른 사건", UnlockStage = 33, Text = new[] { "민간인의 죽음과 황금 완장을 노린 비살상 습격은 피해자도 결과도 달랐다.", "비류연의 수법과 닮은 흔적만으로 독점적인 증거나 범인을 단정할 수 없었다." }, Choices = new[] {
                new FortuneChoice { Label = "피해와 시간 기록을 나란히 놓는다", Result = "서로 다른 사건을 하나로 뭉뚱그리지 않았습니다.", ExpLevels = .25 },
                new FortuneChoice { Label = "확인하지 못한 증언은 남겨 둔다", Result = "모르는 것을 아는 듯 말하지 않기로 했습니다.", MasteryUses = 12 },
            } },
            new FortuneEvent { Id = "f_rebuild_credit", Title = "태운 증서, 다시 쌓을 신용", UnlockStage = 35, Text = new[] { "사부는 예금증서를 태웠고, 은장은 무너졌다. 약속한 재건은 아직 이루어지지 않았다.", "봉한 짐은 유경경의 손에서 마 노인에게 전해졌다. 누구도 그 앞에서 내용물을 열지 않았다." }, Choices = new[] {
                new FortuneChoice { Label = "물건보다 살아남은 사람을 기억한다", Result = "다시 일어설 날을 함께 기다리기로 했습니다.", BondPoints = 2 },
                new FortuneChoice { Label = "당장 할 수 있는 수련부터 이어 간다", Result = "미완의 약속을 가볍게 여기지 않았습니다.", ExpLevels = .25 },
            } },
            new FortuneEvent { Id = "f_own_choice", Title = "스스로 정할 한 걸음", UnlockStage = 36, Text = new[] { "나예린은 도움을 청하고 직접 표국에 갔다. 실패했다고 여긴 선택에도 스스로 움직인 용기가 남았다.", "다른 곳의 영령에게는 은명이 관계와 기억을 통제하려 했다. 누군가 대신 정한 마음은 자신의 선택이 아니었다." }, Choices = new[] {
                new FortuneChoice { Label = "동료가 직접 말할 때까지 기다린다", Result = "상대가 정할 마음의 자리를 남겨 두었습니다.", BondPoints = 2 },
                new FortuneChoice { Label = "알 수 없는 이야기를 단정하지 않는다", Result = "확인한 사실과 전해 들은 말을 갈라 적었습니다.", ExpLevels = .25 },
            } },
            new FortuneEvent { Id = "f_red_signal", Title = "붉은 수건의 뜻", UnlockStage = 37, Text = new[] { "붉은 수건은 다시 도움을 청할 수 있다는 약속이었다.", "계약에 걸린 세 가지 부탁은 아직 쓰이지 않았다." }, Choices = new[] {
                new FortuneChoice { Label = "도움을 청한 용기를 기억한다", Result = "혼자가 아니어도 될 길을 기억했습니다.", BondPoints = 2 },
                new FortuneChoice { Label = "약속을 지킬 준비를 한다", Result = "불러 줄 때 닿을 수 있도록 수련을 이어 갔습니다.", ExpLevels = .2 },
            } },
            new FortuneEvent { Id = "f_guest_map", Title = "지도와 목적지", UnlockStage = 38, Text = new[] { "마천각의 지도에는 자유 구역과 안내가 필요한 곳, 비공개 구역이 나뉘어 있었다.", "지도와 지남철만 있어도 목적지가 없다면 길은 정해지지 않았다." }, Choices = new[] {
                new FortuneChoice { Label = "갈 길과 필요한 도움을 정리한다", Result = "아는 길과 물어야 할 길을 구별했습니다.", ExpLevels = .2 },
                new FortuneChoice { Label = "동료와 지도를 함께 살핀다", Result = "서로 놓친 방향을 짚어 주었습니다.", BondPoints = 2 },
            } },
            new FortuneEvent { Id = "f_samjae_question", Title = "가장 기본인 세 가지", UnlockStage = 39, Text = new[] { "혁중은 모용휘에게 천지인 삼재의 이치를 화두로 남겼다.", "익숙한 말에 답이 있다고 해서 건곤조화경을 이미 얻은 것은 아니었다." }, Choices = new[] {
                new FortuneChoice { Label = "지금 익힌 무공의 기본을 돌아본다", Result = "한 번의 대답 대신 반복할 수련을 남겼습니다.", MasteryUses = 14 },
                new FortuneChoice { Label = "함께 수련할 사람을 찾는다", Result = "배운 것을 나누며 생각을 정리했습니다.", BondPoints = 2 },
            } },
            new FortuneEvent { Id = "f_staked_name", Title = "스스로 밝힌 이름", UnlockStage = 39, Text = new[] { "나예린은 자기 얼굴과 이름을 밝혀 돈왕의 협상 조건을 바꿨다.", "오십만 냥은 아직 받지 못한 상금 약속이었다. 누군가는 그 이름을 다른 계산에 쓰려 했다." }, Choices = new[] {
                new FortuneChoice { Label = "상금과 참가 조건을 구별해 적는다", Result = "아직 얻지 않은 보상을 가진 듯 여기지 않았습니다.", ExpLevels = .25 },
                new FortuneChoice { Label = "협력하기로 한 마음을 기억한다", Result = "대신 정하지 않고 서로의 선택을 믿었습니다.", BondPoints = 2 },
            } },
            new FortuneEvent { Id = "f_unopened_door", Title = "도망치지 않을 준비", UnlockStage = 41, Text = new[] { "연비가 품은 달의 이슬은 사부의 방문을 열기 위한 준비였다.", "문을 열었다고 생명의 빚이나 다른 계약의 답이 끝난 것은 아니었다." }, Choices = new[] {
                new FortuneChoice { Label = "마주한 뒤 할 말을 가다듬는다", Result = "미룬 대답 앞에서 호흡을 고르게 했습니다.", ExpLevels = .25 },
                new FortuneChoice { Label = "곁에 남은 사람을 떠올린다", Result = "혼자 감당하려던 짐에도 함께 걸은 길이 남았습니다.", BondPoints = 2 },
            } },
            new FortuneEvent { Id = "f_unfamiliar_staff", Title = "익숙한 무기 너머", UnlockStage = 42, Text = new[] { "진비뢰도를 되찾지 못한 비류연은 정신봉으로 배 값을 받아 냈다.", "익숙한 도구를 쥐지 못해도 반복한 수련까지 사라진 것은 아니었다." }, Choices = new[] {
                new FortuneChoice { Label = "사용자가 익힌 기본을 되짚는다", Result = "손에 든 것보다 오래 익힌 것을 돌아봤습니다.", MasteryUses = 16 },
                new FortuneChoice { Label = "다른 공방의 길을 정리한다", Result = "한 가지 무기에만 매이지 않을 호흡을 골랐습니다.", ExpLevels = .2 },
            } },
            new FortuneEvent { Id = "f_leave_nest", Title = "둥지를 떠날 뜻", UnlockStage = 43, Text = new[] { "나예린은 부모에게 큰절하며 자신이 정한 길을 말했다.", "허락을 구한 일은 소중한 관계를 끊거나 남에게 마음을 대신 정해 달라는 말이 아니었다." }, Choices = new[] {
                new FortuneChoice { Label = "스스로 정한 길을 지켜본다", Result = "존중할 마음과 기다릴 자리를 남겼습니다.", BondPoints = 2 },
                new FortuneChoice { Label = "길을 나설 준비를 돌아본다", Result = "선택 뒤에 필요한 수련을 이어 갔습니다.", ExpLevels = .2 },
            } },
            new FortuneEvent { Id = "f_free_help", Title = "대가 없이 내민 손", UnlockStage = 44, Text = new[] { "류은경은 대가 없는 도움을 믿기 어려웠다. 용천명은 사연을 듣고 돕기로 했다.", "인정받을 길을 찾는 마음은 혼인 소동의 웃음만으로 끝나지 않았다." }, Choices = new[] {
                new FortuneChoice { Label = "무시당한 말보다 자기 실력을 돌아본다", Result = "타인의 평가 밖에 남길 한 걸음을 찾았습니다.", ExpLevels = .25 },
                new FortuneChoice { Label = "부탁과 협력을 나눈다", Result = "혼자 감당하지 않아도 될 몫을 나눴습니다.", BondPoints = 2 },
            } },
            new FortuneEvent { Id = "f_unfinished_memory", Title = "몸에 남은 시간", UnlockStage = 45, Text = new[] { "소안검과 한빙섬옥 앞에 영령의 몸이 반응했다.", "돌아온 짧은 호칭이 완전한 기억 회복을 보증하지는 않았다." }, Choices = new[] {
                new FortuneChoice { Label = "돌아온 작은 반응을 기억한다", Result = "성급한 결론보다 남은 사람의 시간을 기다렸습니다.", BondPoints = 2 },
                new FortuneChoice { Label = "함께 익힌 기본을 수련한다", Result = "오래된 호흡을 가볍게 여기지 않았습니다.", MasteryUses = 16 },
            } },
            new FortuneEvent { Id = "f_spared_witness", Title = "환호보다 남긴 목숨", UnlockStage = 46, Text = new[] { "연비는 관중이 원하는 처형을 거절했다. 이벽한은 살아서 사부를 다시 만났다.", "독무가 증언을 끊었어도 살아남은 사람과 우승의 성취까지 지워지지는 않았다." }, Choices = new[] {
                new FortuneChoice { Label = "이긴 뒤 남길 책임을 정리한다", Result = "승부의 다음 걸음을 생각했습니다.", ExpLevels = .25 },
                new FortuneChoice { Label = "아직 말하지 못한 사람을 기다린다", Result = "모르는 진실을 임의로 채우지 않았습니다.", BondPoints = 2 },
            } },
            new FortuneEvent { Id = "f_asked_help", Title = "무릎으로 청한 도움", UnlockStage = 47, Text = new[] { "비류연은 사부 앞에서 반드시 구할 사람이 있다 청했다.", "돌려줄 약속 아래 빌린 칼은 절반이었다. 사람을 찾을 몫은 동료들과 나눴다." }, Choices = new[] {
                new FortuneChoice { Label = "맡겨야 할 일을 함께 정한다", Result = "자기 힘만으로 풀 수 없는 길을 나눴습니다.", BondPoints = 2 },
                new FortuneChoice { Label = "돌아와 지킬 약속을 적는다", Result = "다급한 마음 뒤의 책임도 남겨 두었습니다.", ExpLevels = .25 },
            } },
            new FortuneEvent { Id = "f_hidden_hand", Title = "마지막에 숨긴 한 수", UnlockStage = 48, Text = new[] { "장홍은 흑도의 고수에게 숨긴 흑수가 있다고 했다.", "효룡의 강타와 비류연의 느린 접촉은 서로 다른 반응을 보였다. 갑옷의 모든 비밀이 풀린 것은 아니었다." }, Choices = new[] {
                new FortuneChoice { Label = "실제로 본 조건부터 정리한다", Result = "아직 받지 않은 다섯 공격의 결말을 단정하지 않았습니다.", ExpLevels = .25 },
                new FortuneChoice { Label = "상대의 전조를 읽는 수련을 한다", Result = "강함만으로 놓칠 수 있는 한 수를 돌아봤습니다.", MasteryUses = 16 },
            } },
        };

        public static FortuneEvent Find(string id)
        {
            foreach (var e in All) if (e.Id == id) return e;
            return null;
        }

        public static List<FortuneEvent> Open(SaveData save)
        {
            var list = new List<FortuneEvent>();
            foreach (var e in All) if (save.ClearedStage >= e.UnlockStage && !save.DoneFortunes.Contains(e.Id)) list.Add(e);
            return list;
        }

        public static bool CanChoose(SaveData save, FortuneChoice c) { return c.Doo >= 0 || save.Doo >= -c.Doo; }

        /// <summary>Applies a choice, marks the event done, returns the result text.</summary>
        public static string Resolve(SaveData save, FortuneEvent ev, FortuneChoice c, IEnumerable<string> companionIds)
        {
            if (save.DoneFortunes.Contains(ev.Id) || !CanChoose(save, c)) return "";
            save.DoneFortunes.Add(ev.Id);
            save.Doo += c.Doo;
            string text = c.Result;
            if (c.ExpLevels > 0)
            {
                long exp = (long)Math.Round(Progression.ExpToNext(save.Level) * c.ExpLevels);
                int levels = Progression.AddExp(save, exp);
                text += exp + (levels > 0 ? " (Lv." + save.Level + " 상승!)" : "");
            }
            if (!string.IsNullOrEmpty(c.ItemId))
            {
                var item = EarlyGameContent.FindItem(c.ItemId);
                if (item != null) { Inventory.Add(save, c.ItemId, 1); text += "\n획득: " + item.Name; }
            }
            if (c.BondPoints > 0 && companionIds != null)
                foreach (var id in companionIds) Bonds.Add(save, id, c.BondPoints);
            if (c.MasteryUses > 0)
                Mastery.Train(save, c.MasteryUses);
            if (c.Doo > 0) text += "\n은자 +" + c.Doo.ToString("N0");   // the payout is shown, like every other reward
            return text;
        }
    }
}
