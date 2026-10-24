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
        public float Edge = 1f;    // how much tougher than the borrowed fight the enemies are (HP x Edge^0.7, attack x Edge^0.3), tuned with the balance bot
    }

    public static class Quests
    {
        public static readonly QuestData[] All =
        {
            new QuestData { Id = "q_hogol", Edge = 1.346f, Name = "호골채 잔당 소탕", Brief = "남은 산적들이 길목을 노린다. 날이 저물기 전에 정리해 달라는 의뢰.", RefStage = 3, Encounter = 1,
                Trial = TrialRule.Deadline("신속 소탕", "11행동 안에 모두 쓰러뜨리세요.", 11), RewardDoo = 2700, ItemId = "bear_gall" },
            new QuestData { Id = "q_yonggol", Edge = 1.545f, Name = "용골채 산길 정리", Brief = "산길을 막은 용골채 잔당을 서둘러 몰아내 달라는 의뢰.", RefStage = 4, Encounter = 1,
                Trial = TrialRule.Deadline("신속 소탕", "8행동 안에 모두 쓰러뜨리세요.", 8), RewardDoo = 4050, ItemId = "okruju" },
            new QuestData { Id = "q_hoajang", Edge = 1.084f, Name = "호아장 수련생 단속", Brief = "소란을 피우는 수련생들을 짧은 시간에 제압해 달라는 의뢰.", RefStage = 6, Encounter = 1,
                Trial = TrialRule.Deadline("신속 제압", "7행동 안에 모두 쓰러뜨리세요.", 7), RewardDoo = 5850 },
            new QuestData { Id = "q_sadae", Edge = 1.502f, Name = "철각비마대 척후 격퇴", Brief = "정찰병이 길을 열기 전에 막아야 한다.", RefStage = 15, Encounter = 1,
                Trial = TrialRule.Deadline("척후 격퇴", "23행동 안에 모두 쓰러뜨리세요.", 23), RewardDoo = 12600, ItemId = "ginseng" },
            new QuestData { Id = "q_baegin", Edge = 1.027f, Name = "저잣거리 자객 소탕", Brief = "백인 자객들이 사람 많은 거리에 숨어 있다. 소란이 커지기 전에 끝내라.", RefStage = 16, Encounter = 0,
                Trial = TrialRule.Deadline("속전속결", "22행동 안에 모두 쓰러뜨리세요.", 22), RewardDoo = 13500 },
            new QuestData { Id = "q_viper", Edge = 1.084f, Name = "동굴 독사 퇴치", Brief = "독사 떼가 몰려온다. 쓰러지지 않고 버텨낼 수 있겠는가.", RefStage = 20, Encounter = 0,
                Trial = TrialRule.Survive("독사굴 버티기", "적의 공격을 18번 견디세요.", 18), RewardDoo = 17100, ItemId = "ginseng" },
            new QuestData { Id = "q_hyeolma", Edge = 1.0f, Name = "혈마대 잔당 추격", Brief = "달아나는 혈마대 잔당이 합류하기 전에 따라잡아야 한다.", RefStage = 22, Encounter = 0,
                Trial = TrialRule.Deadline("추격전", "33행동 안에 모두 쓰러뜨리세요.", 33), RewardDoo = 18900 },
            new QuestData { Id = "q_salgwi", Edge = 1.027f, Name = "살귀대 정찰조 제압", Brief = "정찰조가 신호를 보내기 전에 소리 없이 제압하라.", RefStage = 26, Encounter = 0,
                Trial = TrialRule.Deadline("소리 없는 제압", "46행동 안에 모두 쓰러뜨리세요.", 46), RewardDoo = 24300, ItemId = "ilgeup_yeongdan" },
            new QuestData { Id = "q_salryong", Edge = 2.2f, Name = "살룡대 잔당 추적", Brief = "불탄 은장 근처에 복면 무리가 아직 남아 있다는 소문. 흩어지기 전에 따라잡아 달라는 의뢰.", RefStage = 31, Encounter = 0,
                Trial = TrialRule.Deadline("잔당 추적", "44행동 안에 모두 쓰러뜨리세요.", 44), RewardDoo = 28800, ItemId = "ginseng" },
            new QuestData { Id = "q_dojeon", Edge = 2.2f, Name = "도전자 정리", Brief = "황금 완장을 노리는 입관 희망자들이 밤길에 몰려다닌다. 소란이 커지기 전에 정리해 달라는 의뢰.", RefStage = 33, Encounter = 1,
                Trial = TrialRule.Deadline("야간 정리", "42행동 안에 모두 쓰러뜨리세요.", 42), RewardDoo = 30600 },
            new QuestData { Id = "q_jungwon", Edge = 2.2f, Name = "중원표국 뒷골목 정리", Brief = "대문이 베인 뒤로 중원표국 표사들이 뒷골목을 막고 서 있다. 길을 터 달라는 의뢰.", RefStage = 34, Encounter = 0,
                Trial = TrialRule.Deadline("길 트기", "44행동 안에 모두 쓰러뜨리세요.", 44), RewardDoo = 32400, ItemId = "okruju" },
            new QuestData { Id = "q_hyeol", Edge = 2.2f, Name = "십삼혈 잔당 수색", Brief = "전설의 새벽에 달아난 붉은 옷의 잔당이 남창 어딘가에 숨었다. 다시 피를 보기 전에 찾아내라.", RefStage = 35, Encounter = 1,
                Trial = TrialRule.Deadline("잔당 수색", "44행동 안에 모두 쓰러뜨리세요.", 44), RewardDoo = 34200, ItemId = "ginseng" },
        };

        public static QuestData Find(string id)
        {
            foreach (var q in All) if (q.Id == id) return q;
            return null;
        }

        /// <summary>The quest's enemies are tougher than the stage fight they are borrowed from.</summary>
        public static EnemyData Scale(string questId, EnemyData enemy)
        {
            var q = Find(questId);
            if (q == null || q.Edge <= 1f) return enemy;
            return enemy.Scaled((float)Math.Pow(q.Edge, 0.7), (float)Math.Pow(q.Edge, 0.3));
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
