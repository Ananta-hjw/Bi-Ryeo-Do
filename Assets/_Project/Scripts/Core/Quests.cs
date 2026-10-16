using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// A one-time 의뢰 from the board: one fight borrowed from an earlier stage with a twist rule (a 시간 제한 or 버티기).
    /// Unlocks once its reference stage is cleared. Flavour text is game-layer, not canon.
    /// </summary>
    public sealed class QuestData
    {
        public string Id, Name, Brief;
        public int RefStage, Encounter;
        public TrialRule Trial;
        public long RewardDoo;
        public string ItemId;      // optional reward item (one)
        public int BondPoints = 1; // given to every companion who fought
    }

    public static class Quests
    {
        public static readonly QuestData[] All =
        {
            new QuestData { Id = "q_hogol", Name = "호골채 잔당 소탕", Brief = "남은 산적들이 길목을 노린다. 날이 저물기 전에 정리해 달라는 의뢰.", RefStage = 3, Encounter = 1,
                Trial = TrialRule.Deadline("신속 소탕", "9행동 안에 모두 쓰러뜨려라.", 9), RewardDoo = 1800, ItemId = "bear_gall" },
            new QuestData { Id = "q_yonggol", Name = "용골채 산길 정리", Brief = "산길을 막은 용골채 잔당을 서둘러 몰아내 달라는 의뢰.", RefStage = 4, Encounter = 1,
                Trial = TrialRule.Deadline("신속 소탕", "9행동 안에 모두 쓰러뜨려라.", 9), RewardDoo = 2700, ItemId = "okruju" },
            new QuestData { Id = "q_hoajang", Name = "호아장 수련생 단속", Brief = "소란을 피우는 수련생들을 짧은 시간에 제압해 달라는 의뢰.", RefStage = 6, Encounter = 1,
                Trial = TrialRule.Deadline("신속 제압", "7행동 안에 모두 쓰러뜨려라.", 7), RewardDoo = 3900 },
            new QuestData { Id = "q_sadae", Name = "철각비마대 척후 격퇴", Brief = "정찰병이 길을 열기 전에 막아야 한다.", RefStage = 15, Encounter = 0,
                Trial = TrialRule.Deadline("척후 격퇴", "16행동 안에 모두 쓰러뜨려라.", 16), RewardDoo = 8400, ItemId = "ginseng" },
            new QuestData { Id = "q_baegin", Name = "저잣거리 자객 소탕", Brief = "백인 자객들이 사람 많은 거리에 숨어 있다. 소란이 커지기 전에 끝내라.", RefStage = 16, Encounter = 0,
                Trial = TrialRule.Deadline("속전속결", "24행동 안에 모두 쓰러뜨려라.", 24), RewardDoo = 9000 },
            new QuestData { Id = "q_viper", Name = "동굴 독사 퇴치", Brief = "독사 떼가 몰려온다. 쓰러지지 않고 버텨낼 수 있겠는가.", RefStage = 20, Encounter = 0,
                Trial = TrialRule.Survive("독사굴 버티기", "적의 공격을 14번 견뎌라.", 14), RewardDoo = 11400, ItemId = "ginseng" },
            new QuestData { Id = "q_hyeolma", Name = "혈마대 잔당 추격", Brief = "달아나는 혈마대 잔당이 합류하기 전에 따라잡아야 한다.", RefStage = 22, Encounter = 0,
                Trial = TrialRule.Deadline("추격전", "34행동 안에 모두 쓰러뜨려라.", 34), RewardDoo = 12600 },
            new QuestData { Id = "q_salgwi", Name = "살귀대 정찰조 제압", Brief = "정찰조가 신호를 보내기 전에 소리 없이 제압하라.", RefStage = 26, Encounter = 0,
                Trial = TrialRule.Deadline("소리 없는 제압", "40행동 안에 모두 쓰러뜨려라.", 40), RewardDoo = 16200, ItemId = "ilgeup_yeongdan" },
        };

        public static QuestData Find(string id)
        {
            foreach (var q in All) if (q.Id == id) return q;
            return null;
        }

        public const int QuestReputation = 3;   // reputation with each faction tied to the quest's stage

        public static bool IsDone(SaveData save, string id) { return save.DoneQuests.Contains(id); }
        public static bool IsUnlocked(SaveData save, QuestData q) { return save.ClearedStage >= q.RefStage; }

        public static List<QuestData> Open(SaveData save)
        {
            var list = new List<QuestData>();
            foreach (var q in All) if (IsUnlocked(save, q) && !IsDone(save, q.Id)) list.Add(q);
            return list;
        }

        /// <summary>The one-fight stage that plays this quest (reuses the reference stage's encounter and background).</summary>
        public static StageData ToStage(QuestData q)
        {
            var reference = EarlyGameContent.FindStage(q.RefStage);
            return new StageData
            {
                Number = q.RefStage, Name = q.Name, IsQuest = true, QuestId = q.Id,
                Intro = new[] { q.Brief, q.Trial != null ? q.Trial.Brief : "" },
                Outro = new[] { "의뢰를 마쳤다." },
                Encounters = new[] { reference.Encounters[q.Encounter] },
                Trials = new[] { q.Trial },
                RecommendedLevel = reference.RecommendedLevel,
                DooTotal = q.RewardDoo
            };
        }

        /// <summary>Exp for a quest fight: a fraction of the current level-up cost.</summary>
        public static long ExpReward(SaveData save) { return (long)Math.Round(Progression.ExpToNext(save.Level) * 0.30); }

        /// <summary>Marks the quest done and hands out the item reward; returns a one-line note.</summary>
        public static string Complete(SaveData save, QuestData q)
        {
            if (!save.DoneQuests.Contains(q.Id)) save.DoneQuests.Add(q.Id);
            Daily.Record(save, DailyEvent.QuestDone);
            string note = "";
            foreach (var f in Factions.ForStage(q.RefStage))
            {
                int rose = Factions.Gain(save, f.Id, QuestReputation);
                note += (note.Length > 0 ? "\n" : "") + f.Name + " 평판 +" + QuestReputation + (rose >= 0 ? " (" + Factions.TierNames[rose] + "!)" : "");
            }
            if (!string.IsNullOrEmpty(q.ItemId))
            {
                var item = EarlyGameContent.FindItem(q.ItemId);
                if (item != null) { Inventory.Add(save, q.ItemId, 1); note += (note.Length > 0 ? "\n" : "") + "의뢰 보상: " + item.Name; }
            }
            return note;
        }
    }
}
