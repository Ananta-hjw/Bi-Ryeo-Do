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
            new QuestData { Id = "q_sura_escort", Name = "퇴로 호위 복기", Brief = "은설란을 데리고 검진을 돌파하던 공방을 모의 수련으로 복기합니다. 새 납치 사건은 아닙니다.", RefStage = 26, Encounter = 0,
                Trial = TrialRule.Survive("보호할 사람", "적 12행동을 견디며 동료를 엄호하세요.", 12), RewardDoo = 24300, ItemId = "ginseng" },
            new QuestData { Id = "q_salgwi", Edge = 1.10f, Name = "살귀대 정찰조 제압", Brief = "정찰조가 신호를 보내기 전에 소리 없이 제압하라.", RefStage = 26, Encounter = 3,
                Trial = TrialRule.Deadline("소리 없는 제압", "68행동 안에 모두 쓰러뜨리세요.", 68), RewardDoo = 24300, ItemId = "ilgeup_yeongdan" },
            new QuestData { Id = "q_baton_route", Name = "목요관 밤길 복기", Brief = "바통을 이어받은 뒤 길을 막은 두 사람과의 공방을 모의 수련으로 재현합니다. 순위 판정을 다시 바꾸는 경기는 아닙니다.", RefStage = 28, Encounter = 0,
                Trial = TrialRule.Deadline("밤길 돌파", "35행동 안에 길을 여세요.", 35), RewardDoo = 27000, ItemId = "okruju" },
            new QuestData { Id = "q_witness_exit", Name = "증인 보호 복기", Brief = "임성진이 증인을 업은 부친의 추격을 막던 공방을 모의 수련으로 되짚습니다. 복면인의 실명을 밝히는 새 심문은 아닙니다.", RefStage = 29, Encounter = 6,
                Trial = TrialRule.Survive("증인의 퇴로", "적 10행동을 견디세요.", 10), RewardDoo = 27900, ItemId = "ilgeup_yeongdan" },
            new QuestData { Id = "q_fire_gather", Name = "화재 속 엄호 복기", Brief = "풍신을 준비하는 동안 동료를 지키던 움직임을 모의 수련으로 복기합니다. 대진이 다시 발동한 사건은 아닙니다.", RefStage = 30, Encounter = 3,
                Trial = TrialRule.Survive("함께 버틸 시간", "적 12행동을 견디세요.", 12), RewardDoo = 28800, ItemId = "ginseng" },
            new QuestData { Id = "q_salryong", Edge = 1.10f, Name = "은장 퇴로 엄호 복기", Brief = "아이들이 빠져나갈 시간을 벌던 공방을 모의 수련으로 되짚습니다. 은장의 희생을 다시 벌이는 의뢰가 아닙니다.", RefStage = 31, Encounter = 0,
                Trial = TrialRule.Survive("퇴로 엄호", "적 12행동을 견디세요.", 12), RewardDoo = 28800, ItemId = "ginseng" },
            new QuestData { Id = "q_dojeon", Edge = 4f, Name = "객잔 시비 제압 복기", Brief = "비류연이 홀로 남아 시비를 제압했던 공방을 복기합니다. 살인 사건의 범인을 찾아내는 의뢰는 아닙니다.", RefStage = 33, Encounter = 1,
                Trial = TrialRule.Deadline("절제된 제압", "30행동 안에 제압하세요.", 30), RewardDoo = 30600 },
            new QuestData { Id = "q_jungwon", Edge = 1.10f, Name = "표국 돌파 복기", Brief = "나예린 일행의 퇴로 확보를 모의 수련으로 되짚습니다. 이미 달아난 이시건을 다시 붙잡는 사건은 아닙니다.", RefStage = 34, Encounter = 0,
                Trial = TrialRule.Survive("퇴로 확보", "적 10행동을 견디세요.", 10), RewardDoo = 32400, ItemId = "okruju" },
            new QuestData { Id = "q_hyeol", Edge = 1.10f, Name = "새벽의 호위 복기", Brief = "장우양과 장우경이 버틴 새벽의 호위를 복기합니다. 십삼혈의 생존 잔당이 남았다는 뜻은 아닙니다.", RefStage = 35, Encounter = 2,
                Trial = TrialRule.Survive("호위의 시간", "적 12행동을 견디세요.", 12), RewardDoo = 34200, ItemId = "ginseng" },
            new QuestData { Id = "q_ichon", Name = "일촌의 간격 수련", Brief = "일촌간파의 간격 읽기를 모의 수련으로 되짚습니다. 비뢰도를 받는 새 내기는 아닙니다.", RefStage = 32, Encounter = 0,
                Trial = TrialRule.Survive("공방 복기", "적 8행동을 견디세요.", 8), RewardDoo = 29700, ItemId = "ginseng" },
            new QuestData { Id = "q_white_phoenix", Name = "섬광 속 퇴로 수련", Brief = "증인을 놓친 뒤에도 동료와 살아 돌아올 길을 지키던 판단을 복기합니다. 상대의 고정 도주를 막는 시험은 아닙니다.", RefStage = 34, Encounter = 2,
                Trial = TrialRule.Survive("공방 복기", "적 10행동을 견디세요.", 10), RewardDoo = 32400, ItemId = "okruju" },
            new QuestData { Id = "q_bait_cover", Name = "미끼조 엄호 수련", Brief = "미끼로 나섰던 세 사람의 공방을 복기합니다. 학생을 새 위험에 데려가는 의뢰는 아닙니다.", RefStage = 35, Encounter = 0,
                Trial = TrialRule.Survive("공방 복기", "적 12행동을 견디세요.", 12), RewardDoo = 34200, ItemId = "ginseng" },
            new QuestData { Id = "q_eogeom_review", Name = "분영검 대응 수련", Brief = "두 그림자의 검세를 읽던 순간을 모의 수련으로 복기합니다. 무제한 비무의 승패를 다시 정하지 않습니다.", RefStage = 36, Encounter = 1,
                Trial = TrialRule.Survive("공방 복기", "적 8행동을 견디세요.", 8), RewardDoo = 36000, ItemId = "ilgeup_yeongdan" },
            new QuestData { Id = "q_guest_parry", Name = "낯선 검로 대응 수련", Brief = "혈풍란을 피하던 움직임을 안전한 모의 수련으로 되짚습니다. 장홍의 과거를 캐묻는 의뢰가 아닙니다.", RefStage = 38, Encounter = 0, Trial = TrialRule.Survive("공세 복기", "적 10행동을 견디세요.", 10), RewardDoo = 37800, ItemId = "ginseng" },
            new QuestData { Id = "q_bridge_cover", Name = "다리 위 화살 엄호 복기", Brief = "십면매복의 첫 화살비를 정규 동료들과 복기합니다. 새 장강십용사가 침입한 사건이 아닙니다.", RefStage = 40, Encounter = 0, Trial = TrialRule.Survive("공세 복기", "적 10행동을 견디세요.", 10), RewardDoo = 40500, ItemId = "okruju" },
            new QuestData { Id = "q_rope_pattern", Name = "흑룡삭 진법 대응 수련", Brief = "끊어진 진법의 공격 방향을 모의 수련으로 복기합니다. 나예린의 이름 없는 검기를 영구 전수하지 않습니다.", RefStage = 40, Encounter = 3, Trial = TrialRule.Survive("공세 복기", "적 10행동을 견디세요.", 10), RewardDoo = 40500, ItemId = "ilgeup_yeongdan" },
            new QuestData { Id = "q_shell_rehearsal", Name = "포격 전조 대응 복기", Brief = "정신봉 공방에서 읽었던 포격 전조를 정규 동료들과 복기합니다. 배 값을 다시 받는 결투가 아닙니다.", RefStage = 42, Encounter = 2, Trial = TrialRule.Survive("공방 복기", "적 10행동을 견디세요.", 10), RewardDoo = 42300, ItemId = "ginseng" },
            new QuestData { Id = "q_sister_form", Name = "검로 기억 대응 수련", Brief = "사자매의 검로에 대응하던 움직임을 모의 수련으로 되짚습니다. 영령의 기억을 회복시키는 재전투가 아닙니다.", RefStage = 45, Encounter = 1, Trial = TrialRule.Survive("공방 복기", "적 10행동을 견디세요.", 10), RewardDoo = 45000, ItemId = "okruju" },
            new QuestData { Id = "q_blood_cross", Name = "쌍도 교차 대응 복기", Brief = "혈십자의 예고를 읽던 공방을 복기합니다. 생존한 증인을 다시 처치하는 의뢰가 아닙니다.", RefStage = 46, Encounter = 2, Trial = TrialRule.Survive("공방 복기", "적 10행동을 견디세요.", 10), RewardDoo = 45900, ItemId = "ilgeup_yeongdan" },
            new QuestData { Id = "q_explosion_exit", Name = "밀실 퇴로 복기", Brief = "폭염진의 압력 속에서 퇴로를 찾던 움직임을 모의 수련으로 재현합니다. 나예린을 다시 납치하는 사건이 아닙니다.", RefStage = 47, Encounter = 1, Trial = TrialRule.Survive("공방 복기", "적 8행동을 견디세요.", 8), RewardDoo = 46800, ItemId = "ginseng" },
            new QuestData { Id = "q_gate_lines", Name = "철신관 입구 협공 복기", Brief = "철갑사패의 봉쇄에 대응하던 협공을 복기합니다. 락비오전 결말을 앞당기는 시험이 아닙니다.", RefStage = 48, Encounter = 2, Trial = TrialRule.Survive("공방 복기", "적 10행동을 견디세요.", 10), RewardDoo = 47700, ItemId = "ilgeup_yeongdan" },
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
