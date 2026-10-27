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
        public float Edge = 1f;    // HP x Edge^0.7 / attack x Edge^0.3; later quests start from the current normal-stage scaling.
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
            // Optional rehearsals: these do not invent a second canonical incident or re-stage a death.
            new QuestData { Id = "q_baekhyang_exit", Name = "백향관 경계 복기", Brief = "지난 잠입의 경계 돌파를 안전한 모의 수련으로 되짚습니다. 원작의 독한녀와 실제로 싸운 사건이 아닙니다.", RefStage = 10, Encounter = 1,
                Trial = TrialRule.Survive("탈출 동선", "적 8행동을 견디며 퇴로를 확보하세요.", 8), RewardDoo = 7200, ItemId = "bear_gall" },
            new QuestData { Id = "q_promotion_guard", Name = "승급 비무 방어 수련", Brief = "단대풍전의 예고 공격을 복기합니다. 새 승급이나 검룡위를 지급하는 시험이 아닙니다.", RefStage = 11, Encounter = 3,
                Trial = TrialRule.Survive("검세 읽기", "적 10행동을 견디세요.", 10), RewardDoo = 8100, ItemId = "okruju" },
            new QuestData { Id = "q_river_cover", Name = "장강 표선 엄호 복기", Brief = "물속에서 동료를 보호했던 움직임을 모의 수련으로 재현합니다. 추가 암살 사건이 아닙니다.", RefStage = 14, Encounter = 1,
                Trial = TrialRule.Survive("동료 엄호", "적 12행동을 견디세요.", 12), RewardDoo = 10800, ItemId = "ginseng" },
            new QuestData { Id = "q_sadae", Edge = 1.10f, Name = "철각비마대 척후 격퇴", Brief = "정찰병이 길을 열기 전에 막아야 한다.", RefStage = 15, Encounter = 1,
                Trial = TrialRule.Deadline("척후 격퇴", "27행동 안에 모두 쓰러뜨리세요.", 27), RewardDoo = 12600, ItemId = "ginseng" },
            new QuestData { Id = "q_market_screen", Name = "저잣거리 엄호 복기", Brief = "민간인과 자객을 가려내던 호위의 공방을 모의 수련으로 복기합니다. 새 청부 사건은 아닙니다.", RefStage = 16, Encounter = 0,
                Trial = TrialRule.Survive("호위의 시야", "적 12행동을 견디며 동료를 엄호하세요.", 12), RewardDoo = 13500, ItemId = "okruju" },
            new QuestData { Id = "q_baegin", Edge = 1.10f, Name = "저잣거리 자객 소탕", Brief = "백인 자객들이 사람 많은 거리에 숨어 있다. 소란이 커지기 전에 끝내라.", RefStage = 16, Encounter = 0,
                Trial = TrialRule.Deadline("속전속결", "37행동 안에 모두 쓰러뜨리세요.", 37), RewardDoo = 13500 },
            new QuestData { Id = "q_rope_rehearsal", Name = "절제된 쌍검 수련", Brief = "효룡과의 수련에서 힘을 아끼던 공방을 복기합니다. 실제 줄의 절단·낙하를 판정하는 새 시험은 아닙니다.", RefStage = 18, Encounter = 1,
                Trial = TrialRule.Survive("힘의 절제", "적 8행동을 견디세요.", 8), RewardDoo = 15000, ItemId = "bear_gall" },
            new QuestData { Id = "q_illusion_breath", Name = "환상 뒤의 집중 수련", Brief = "환마동에서 현실의 감각을 되찾던 집중을 안전한 모의 수련으로 복기합니다. 다른 이의 상처를 공개하는 과제가 아닙니다.", RefStage = 19, Encounter = 4,
                Trial = TrialRule.Survive("호흡과 감각", "적 10행동을 견디며 호흡을 유지하세요.", 10), RewardDoo = 16200, ItemId = "ginseng" },
            new QuestData { Id = "q_viper", Edge = 1.10f, Name = "동굴 독사 퇴치", Brief = "독사 떼가 몰려온다. 쓰러지지 않고 버텨낼 수 있겠는가.", RefStage = 20, Encounter = 0,
                Trial = TrialRule.Survive("독사굴 버티기", "적의 공격을 18번 견디세요.", 18), RewardDoo = 17100, ItemId = "ginseng" },
            new QuestData { Id = "q_witness_cover", Name = "생존자 호위 복기", Brief = "이송학을 지키던 윤준호의 공방을 모의 수련으로 복기합니다. 숲의 사건을 다시 벌이거나 새 자백을 얻는 의뢰가 아닙니다.", RefStage = 22, Encounter = 1,
                Trial = TrialRule.Survive("생존자 엄호", "적 12행동을 견디며 동료를 보호하세요.", 12), RewardDoo = 18900, ItemId = "bear_gall" },
            new QuestData { Id = "q_hyeolma", Edge = 1.10f, Name = "혈마대 잔당 추격", Brief = "달아나는 혈마대 잔당이 합류하기 전에 따라잡아야 한다.", RefStage = 22, Encounter = 3,
                Trial = TrialRule.Deadline("추격전", "68행동 안에 모두 쓰러뜨리세요.", 68), RewardDoo = 18900 },
            new QuestData { Id = "q_flute_cover", Name = "지사진 대응 수련", Brief = "부적만으로 막을 수 없던 지사진을 모의 수련으로 복기합니다. 이미 익힌 무공과 방어로 대응할 수 있습니다.", RefStage = 23, Encounter = 3,
                Trial = TrialRule.Survive("진의 틈 읽기", "적 10행동을 견디세요.", 10), RewardDoo = 19800, ItemId = "ginseng" },
            new QuestData { Id = "q_hwapyeong_exit", Name = "화평장 퇴로 복기", Brief = "구출한 동료의 퇴로를 지키던 공방을 모의 수련으로 재현합니다. 은설란이 다시 납치되는 사건이 아닙니다.", RefStage = 25, Encounter = 2,
                Trial = TrialRule.Survive("퇴로의 엄호", "적 12행동을 견디세요.", 12), RewardDoo = 22500, ItemId = "okruju" },
            new QuestData { Id = "q_salgwi", Edge = 1.10f, Name = "살귀대 정찰조 제압", Brief = "정찰조가 신호를 보내기 전에 소리 없이 제압하라.", RefStage = 26, Encounter = 0,
                Trial = TrialRule.Deadline("소리 없는 제압", "68행동 안에 모두 쓰러뜨리세요.", 68), RewardDoo = 24300, ItemId = "ilgeup_yeongdan" },
            new QuestData { Id = "q_salryong", Edge = 1.10f, Name = "살룡대 잔당 추적", Brief = "불탄 은장 근처에 복면 무리가 아직 남아 있다는 소문. 흩어지기 전에 따라잡아 달라는 의뢰.", RefStage = 31, Encounter = 0,
                Trial = TrialRule.Deadline("잔당 추적", "65행동 안에 모두 쓰러뜨리세요.", 65), RewardDoo = 28800, ItemId = "ginseng" },
            new QuestData { Id = "q_dojeon", Edge = 1.10f, Name = "도전자 정리", Brief = "황금 완장을 노리는 입관 희망자들이 밤길에 몰려다닌다. 소란이 커지기 전에 정리해 달라는 의뢰.", RefStage = 33, Encounter = 1,
                Trial = TrialRule.Deadline("야간 정리", "76행동 안에 모두 쓰러뜨리세요.", 76), RewardDoo = 30600 },
            new QuestData { Id = "q_jungwon", Edge = 1.10f, Name = "중원표국 뒷골목 정리", Brief = "대문이 베인 뒤로 중원표국 표사들이 뒷골목을 막고 서 있다. 길을 터 달라는 의뢰.", RefStage = 34, Encounter = 0,
                Trial = TrialRule.Deadline("길 트기", "76행동 안에 모두 쓰러뜨리세요.", 76), RewardDoo = 32400, ItemId = "okruju" },
            new QuestData { Id = "q_hyeol", Edge = 1.10f, Name = "십삼혈 잔당 수색", Brief = "전설의 새벽에 달아난 붉은 옷의 잔당이 남창 어딘가에 숨었다. 다시 피를 보기 전에 찾아내라.", RefStage = 35, Encounter = 1,
                Trial = TrialRule.Deadline("잔당 수색", "54행동 안에 모두 쓰러뜨리세요.", 54), RewardDoo = 34200, ItemId = "ginseng" },
        };

        public static QuestData Find(string id)
        {
            foreach (var q in All) if (q.Id == id) return q;
            return null;
        }

        /// <summary>Preserves solo-era tuning; later quests follow current normal-stage stats before their own edge.</summary>
        public static EnemyData Scale(string questId, EnemyData enemy)
        {
            var q = Find(questId);
            if (q == null) return enemy;
            if (q.RefStage >= 14)
            {
                bool survive = q.Trial != null && q.Trial.Kind == TrialKind.Survive;
                enemy = StageStars.ScaleEnemy(enemy, q.RefStage, 0, survive);
                if (survive) enemy = enemy.Scaled(1.25f, 1f);
            }
            if (q.Edge <= 1f) return enemy;
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
            if (save.DoneQuests.Contains(q.Id)) return "";
            save.DoneQuests.Add(q.Id);
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
