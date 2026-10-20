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
                    new FortuneChoice { Label = "그냥 푹 잔다", Result = "개운하게 잤다. 일어나 보니 머리맡에 약간의 두가 놓여 있다.", Doo = 470 },
                } },
            new FortuneEvent { Id = "f_peddler", Title = "수상한 행상", UnlockStage = 4,
                Text = new[] { "길가에서 행상이 보따리를 펼친다. \"귀한 영약이오. 지금 아니면 못 구하오.\"", "믿을 만한 물건일까?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "영약을 산다 (-800두)", Result = "한 병을 샀다. 진짜 영약이었다!", Doo = -800, ItemId = "ginseng" },
                    new FortuneChoice { Label = "흥정해서 약초만 산다 (-200두)", Result = "약초 한 뿌리를 싸게 샀다.", Doo = -200, ItemId = "bear_gall" },
                    new FortuneChoice { Label = "지나친다", Result = "행상은 아쉬운 얼굴로 보따리를 쌌다." },
                } },
            new FortuneEvent { Id = "f_campfire", Title = "모닥불 곁의 밤", UnlockStage = 7,
                Text = new[] { "동료들과 모닥불을 둘러싸고 앉았다. 오늘은 별이 유난히 많다.", "남은 밤을 어떻게 보낼까?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "다 같이 술과 이야기를 나눈다", Result = "웃음이 오가며 서로를 한결 편하게 대하게 되었다.", BondPoints = 2 },
                    new FortuneChoice { Label = "차분히 합동 수련을 한다", Result = "함께 호흡을 맞춰 보았다. 경험을 얻었고 서로를 알게 되었다. 경험 +", ExpLevels = 0.35, BondPoints = 1 },
                } },
            new FortuneEvent { Id = "f_cave", Title = "이름 모를 동굴", UnlockStage = 12,
                Text = new[] { "산길 옆에 오래된 동굴이 입을 벌리고 있다. 안쪽에서 희미한 기운이 느껴진다.", "들어가 볼까?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "깊이 들어가 조사한다", Result = "한참 헤맸지만 선인의 흔적에서 깨달음을 얻었다. 경험 +", ExpLevels = 0.9 },
                    new FortuneChoice { Label = "입구 근처만 살핀다", Result = "버려진 짐 속에서 쓸 만한 약병을 찾았다.", ItemId = "okruju" },
                    new FortuneChoice { Label = "위험하니 돌아선다", Result = "무사히 돌아왔다. 길에서 두 몇 푼을 주웠다.", Doo = 780 },
                } },
            new FortuneEvent { Id = "f_master_scroll", Title = "낡은 비급 조각", UnlockStage = 17,
                Text = new[] { "헌책방 구석에서 낡은 비급 조각을 발견했다. 내용은 알아볼 수 없다.", "어떻게 할까?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "풀이를 사서 깊이 연구한다 (-3000두)", Result = "며칠 밤을 새워 연구했다. 모든 장착 무공이 크게 숙련되었다.", Doo = -3000, MasteryUses = 25 },
                    new FortuneChoice { Label = "되팔아 돈으로 바꾼다", Result = "수집가가 후하게 값을 쳐주었다.", Doo = 3900 },
                } },
            new FortuneEvent { Id = "f_reunion", Title = "오랜 동료와의 술자리", UnlockStage = 22,
                Text = new[] { "싸움이 잠시 잦아든 밤, 일행이 한자리에 모였다.", "오늘만큼은 마음을 터놓을 수 있을 것 같다." },
                Choices = new[]
                {
                    new FortuneChoice { Label = "속마음을 이야기한다", Result = "서로의 이야기를 들으며 신뢰가 한 단계 깊어졌다.", BondPoints = 4 },
                    new FortuneChoice { Label = "함께 합공 수련을 한다", Result = "호흡을 맞추는 연습에 땀을 흘렸다. 경험과 인연을 얻었다. 경험 +", ExpLevels = 0.5, BondPoints = 2 },
                } },
            new FortuneEvent { Id = "f_river", Title = "강가의 낚시꾼", UnlockStage = 2,
                Text = new[] { "강가에서 노인이 낚싯대를 드리우고 있다. 말없이 곁자리를 내주었다.", "이 한가한 시간을 어떻게 보낼까?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "낚시를 거들어 준다", Result = "잡은 물고기를 시장에 넘겨 노인이 몫을 나눠 주었다.", Doo = 620 },
                    new FortuneChoice { Label = "노인의 호흡을 따라 해 본다", Result = "느린 호흡 속에서 장착한 무공의 감이 한결 잡혔다.", MasteryUses = 8 },
                    new FortuneChoice { Label = "구운 생선을 먹으며 쉰다", Result = "배불리 먹고 한숨 자고 나니 몸이 가벼워졌다. 경험 +", ExpLevels = 0.4 },
                } },
            new FortuneEvent { Id = "f_inn", Title = "객잔의 소문", UnlockStage = 9,
                Text = new[] { "저잣거리 객잔에 들어서니 온갖 소문이 오간다.", "어떤 이야기에 귀를 기울일까?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "정보꾼에게 소문을 산다 (-600두)", Result = "값진 이야기였다. 큰 깨달음을 얻었다. 경험 +", Doo = -600, ExpLevels = 0.9 },
                    new FortuneChoice { Label = "일행과 술잔을 기울인다 (-100두)", Result = "시끌벅적한 술자리에서 서로가 한결 가까워졌다.", Doo = -100, BondPoints = 2 },
                    new FortuneChoice { Label = "남의 대화에는 신경 쓰지 않는다", Result = "자리에 떨어진 두 몇 푼을 주웠다.", Doo = 550 },
                } },
            new FortuneEvent { Id = "f_blacksmith", Title = "늙은 대장장이", UnlockStage = 14,
                Text = new[] { "산 아래 대장간에서 늙은 대장장이가 쇠를 두드리고 있다.", "무슨 일로 들렀는가?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "일손을 거든다", Result = "풀무질을 도왔더니 품삯을 두둑이 받았다.", Doo = 1870 },
                    new FortuneChoice { Label = "망치질을 지켜보며 운기한다", Result = "쇠를 두드리는 박자에 호흡을 맞추었다. 경험 +", ExpLevels = 0.6 },
                    new FortuneChoice { Label = "비상약을 맡겨 둔 것을 찾는다 (-900두)", Result = "대장장이가 몰래 달여 둔 귀한 약을 건네주었다.", Doo = -900, ItemId = "ginseng" },
                } },
            new FortuneEvent { Id = "f_monk", Title = "산사의 노승", UnlockStage = 19,
                Text = new[] { "고요한 산사. 노승이 마당을 쓸다 말고 이쪽을 본다.", "\"검을 쥔 손에 물어볼 것이 많아 보이는구려.\"" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "선문답을 나눈다", Result = "알 듯 말 듯한 말 속에서 마음이 맑아졌다. 경험 +", ExpLevels = 1.0 },
                    new FortuneChoice { Label = "시주하고 수련을 청한다 (-1500두)", Result = "노승이 직접 초식을 바로잡아 주었다. 일행도 곁에서 함께 배웠다.", Doo = -1500, MasteryUses = 15, BondPoints = 1 },
                    new FortuneChoice { Label = "공양만 하고 떠난다", Result = "노승이 공양 답례로 약병을 쥐여 주었다.", ItemId = "okruju" },
                } },
            new FortuneEvent { Id = "f_snownight", Title = "눈 내리는 밤", UnlockStage = 24,
                Text = new[] { "눈이 쌓인 산길. 야영지의 모닥불이 잦아든다.", "차가운 밤을 어떻게 넘길까?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "서로 곁에 앉아 체온을 나눈다", Result = "말은 적었지만 서로를 믿는 마음이 한층 깊어졌다.", BondPoints = 3 },
                    new FortuneChoice { Label = "눈 속에서 호흡을 가다듬는다", Result = "한기를 이겨내며 기운이 단단해졌다. 경험과 인연을 얻었다. 경험 +", ExpLevels = 0.7, BondPoints = 1 },
                    new FortuneChoice { Label = "홀로 칼날을 닦으며 초식을 되새긴다", Result = "밤새 초식을 복기했다. 장착한 무공이 크게 숙련되었다.", MasteryUses = 12 },
                } },
            new FortuneEvent { Id = "f_eve", Title = "결전 전야", UnlockStage = 28,
                Text = new[] { "큰 싸움을 앞둔 밤, 아무도 쉽게 잠들지 못한다.", "마지막 준비를 어떻게 할까?" },
                Choices = new[]
                {
                    new FortuneChoice { Label = "일행과 함께 살아 돌아오자고 맹세한다", Result = "서로의 손을 맞잡았다. 이보다 든든한 밤은 없었다.", BondPoints = 5 },
                    new FortuneChoice { Label = "밤새 운기조식에 몰두한다", Result = "기운이 한계까지 차올랐다. 경험 +", ExpLevels = 1.3 },
                    new FortuneChoice { Label = "비상약을 챙겨 둔다", Result = "행낭 깊숙이 비장의 영단을 챙겼다.", ItemId = "ilgeup_yeongdan" },
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
            return text;
        }
    }
}
