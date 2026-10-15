using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>도감: which enemies, items and martial arts the player has met. Entries are recorded in <see cref="SaveData"/>.</summary>
    public static class Codex
    {
        public static void SeeItem(SaveData save, string id)
        {
            if (!string.IsNullOrEmpty(id) && !save.SeenItems.Contains(id)) save.SeenItems.Add(id);
        }

        /// <summary>Records a beaten enemy; tower and duel copies (not in the story list) are ignored.</summary>
        public static void SeeEnemy(SaveData save, string id)
        {
            if (string.IsNullOrEmpty(id) || save.SeenEnemies.Contains(id)) return;
            foreach (var e in EarlyGameContent.Enemies)
                if (e.Id == id) { save.SeenEnemies.Add(id); return; }
        }

        /// <summary>Fills the codex from state that already exists (older saves): the bag, worn gear, and enemies of cleared stages.</summary>
        public static void Sync(SaveData save)
        {
            foreach (var s in save.Items) SeeItem(save, s.Id);
            foreach (var id in save.EquippedGear) SeeItem(save, id);
            foreach (var st in EarlyGameContent.Stages)
            {
                if (st.Number > save.ClearedStage) break;
                foreach (var enc in st.Encounters) foreach (var id in enc) SeeEnemy(save, id);
            }
        }

        public static bool HasEnemy(SaveData save, string id) { return save.SeenEnemies.Contains(id); }
        public static bool HasItem(SaveData save, string id) { return save.SeenItems.Contains(id); }

        public static int EnemyTotal { get { return EarlyGameContent.Enemies.Length; } }
        public static int ItemTotal { get { return EarlyGameContent.Items.Length; } }

        /// <summary>Martial arts the protagonist can learn (companion arts are not part of the collection).</summary>
        public static List<ArtData> HeroArts()
        {
            var list = new List<ArtData>();
            foreach (var a in EarlyGameContent.Arts) if (a.OwnerId == null) list.Add(a);
            return list;
        }

        public static int ItemsSeenOfGrade(SaveData save, ItemGrade minGrade)
        {
            int n = 0;
            foreach (var id in save.SeenItems)
            {
                var it = EarlyGameContent.FindItem(id);
                if (it != null && it.Grade >= minGrade) n++;
            }
            return n;
        }
    }

    public enum AchMetric { StageCleared, TowerFloor, Level, SwordRank, EnemiesSeen, ItemsSeen, ArtsLearned, GachaPulls, BattleWins, QuestsDone, FortunesDone, TrustedBonds, LegendarySeen }

    public sealed class AchievementData
    {
        public string Id, Name, Description;
        public AchMetric Metric;
        public int Target;
        public long RewardDoo;
        public string RewardItemId;   // optional extra reward
    }

    /// <summary>업적: milestones measured from the save; each pays 두 (and sometimes an item) once, when the player claims it.</summary>
    public static class Achievements
    {
        private static AchievementData A(string id, string name, string desc, AchMetric m, int target, long doo, string item = null)
        {
            return new AchievementData { Id = id, Name = name, Description = desc, Metric = m, Target = target, RewardDoo = doo, RewardItemId = item };
        }

        public static readonly AchievementData[] All =
        {
            A("stage_5", "강호 첫걸음", "스테이지 5 돌파", AchMetric.StageCleared, 5, 800),
            A("stage_10", "이름을 알리다", "스테이지 10 돌파", AchMetric.StageCleared, 10, 1500),
            A("stage_20", "강호의 별", "스테이지 20 돌파", AchMetric.StageCleared, 20, 3000, "ginseng"),
            A("stage_30", "한 시대의 끝", "스테이지 30 돌파", AchMetric.StageCleared, 30, 6000, "ilgeup_yeongdan"),
            A("level_10", "수련의 결실", "레벨 10 달성", AchMetric.Level, 10, 600),
            A("level_30", "일가를 이루다", "레벨 30 달성", AchMetric.Level, 30, 2000),
            A("level_50", "절정의 문턱", "레벨 50 달성", AchMetric.Level, 50, 4000),
            A("level_65", "초절정", "레벨 65 달성", AchMetric.Level, 65, 8000),
            A("wins_10", "첫 승리들", "전투 10회 승리", AchMetric.BattleWins, 10, 500),
            A("wins_50", "싸움꾼", "전투 50회 승리", AchMetric.BattleWins, 50, 1500),
            A("wins_200", "백전노장", "전투 200회 승리", AchMetric.BattleWins, 200, 5000),
            A("tower_5", "비무대에 서다", "무한 비무대 5층 돌파", AchMetric.TowerFloor, 5, 1000),
            A("tower_10", "탑의 중턱", "무한 비무대 10층 돌파", AchMetric.TowerFloor, 10, 2500, "ginseng"),
            A("tower_20", "하늘이 가깝다", "무한 비무대 20층 돌파", AchMetric.TowerFloor, 20, 6000),
            A("tower_30", "탑의 주인", "무한 비무대 30층 돌파", AchMetric.TowerFloor, 30, 12000, "ilgeup_yeongdan"),
            A("rank_3", "삼검룡", "검룡위 3 달성", AchMetric.SwordRank, 3, 800),
            A("rank_5", "오검룡", "검룡위 5 달성", AchMetric.SwordRank, 5, 2000),
            A("rank_8", "팔검룡", "검룡위 8 달성", AchMetric.SwordRank, 8, 5000),
            A("rank_10", "십검룡", "검룡위 10 달성", AchMetric.SwordRank, 10, 10000),
            A("enemy_10", "상대를 알다", "도감에 적 10종 등록", AchMetric.EnemiesSeen, 10, 700),
            A("enemy_30", "강호 견문", "도감에 적 30종 등록", AchMetric.EnemiesSeen, 30, 2500),
            A("enemy_all", "모든 적을 만나다", "도감의 적을 모두 등록", AchMetric.EnemiesSeen, -1, 8000),
            A("item_10", "수집의 시작", "도감에 물품 10종 등록", AchMetric.ItemsSeen, 10, 700),
            A("item_30", "보물 창고", "도감에 물품 30종 등록", AchMetric.ItemsSeen, 30, 2500),
            A("item_all", "만물상", "도감의 물품을 모두 등록", AchMetric.ItemsSeen, -1, 15000),
            A("legend_1", "전설을 손에 쥐다", "전설 이상 물품 1종 등록", AchMetric.LegendarySeen, 1, 3000),
            A("legend_5", "전설의 수집가", "전설 이상 물품 5종 등록", AchMetric.LegendarySeen, 5, 8000),
            A("gacha_10", "운을 시험하다", "뽑기 10회", AchMetric.GachaPulls, 10, 500),
            A("gacha_100", "뽑기 중독", "뽑기 100회", AchMetric.GachaPulls, 100, 3000),
            A("arts_5", "다양한 무공", "무공 5개 습득", AchMetric.ArtsLearned, 5, 1000),
            A("arts_all", "무공을 두루 익히다", "습득 가능한 무공을 모두 습득", AchMetric.ArtsLearned, -1, 6000),
            A("quest_3", "심부름꾼", "의뢰 3건 완료", AchMetric.QuestsDone, 3, 800),
            A("quest_all", "의뢰 해결사", "의뢰를 모두 완료", AchMetric.QuestsDone, -1, 5000),
            A("fortune_3", "행운아", "기연 3건 겪기", AchMetric.FortunesDone, 3, 800),
            A("fortune_all", "기연의 주인", "기연을 모두 겪기", AchMetric.FortunesDone, -1, 5000),
            A("bond_1", "믿음직한 벗", "신뢰 이상의 동료 1명", AchMetric.TrustedBonds, 1, 1500),
            A("bond_4", "의기투합", "신뢰 이상의 동료 4명", AchMetric.TrustedBonds, 4, 6000),
        };

        public static AchievementData Find(string id)
        {
            foreach (var a in All) if (a.Id == id) return a;
            return null;
        }

        /// <summary>The number to reach; -1 targets resolve to "everything" for their metric.</summary>
        public static int TargetOf(AchievementData a)
        {
            if (a.Target > 0) return a.Target;
            switch (a.Metric)
            {
                case AchMetric.EnemiesSeen: return Codex.EnemyTotal;
                case AchMetric.ItemsSeen: return Codex.ItemTotal;
                case AchMetric.ArtsLearned: return Codex.HeroArts().Count;
                case AchMetric.QuestsDone: return Quests.All.Length;
                case AchMetric.FortunesDone: return Fortunes.All.Length;
            }
            return 1;
        }

        public static int Progress(SaveData save, AchievementData a)
        {
            switch (a.Metric)
            {
                case AchMetric.StageCleared: return save.ClearedStage;
                case AchMetric.TowerFloor: return save.TowerBest;
                case AchMetric.Level: return save.Level;
                case AchMetric.SwordRank: return Promotion.EffectiveRank(save);
                case AchMetric.EnemiesSeen: return save.SeenEnemies.Count;
                case AchMetric.ItemsSeen: return save.SeenItems.Count;
                case AchMetric.GachaPulls: return save.GachaPulls;
                case AchMetric.BattleWins: return save.BattleWins;
                case AchMetric.QuestsDone: return save.DoneQuests.Count;
                case AchMetric.FortunesDone: return save.DoneFortunes.Count;
                case AchMetric.LegendarySeen: return Codex.ItemsSeenOfGrade(save, ItemGrade.Legendary);
                case AchMetric.ArtsLearned:
                    {
                        int n = 0;
                        foreach (var art in Codex.HeroArts()) if (save.LearnedArts.Contains(art.Id)) n++;
                        return n;
                    }
                case AchMetric.TrustedBonds:
                    {
                        int n = 0;
                        foreach (var c in EarlyGameContent.Companions) if (Bonds.Level(save, c.Id) >= 2) n++;
                        return n;
                    }
            }
            return 0;
        }

        public static bool IsDone(SaveData save, AchievementData a) { return Progress(save, a) >= TargetOf(a); }
        public static bool IsClaimed(SaveData save, AchievementData a) { return save.ClaimedAchievements.Contains(a.Id); }
        public static bool CanClaim(SaveData save, AchievementData a) { return IsDone(save, a) && !IsClaimed(save, a); }

        public static int ClaimableCount(SaveData save)
        {
            int n = 0;
            foreach (var a in All) if (CanClaim(save, a)) n++;
            return n;
        }

        public static int ClaimedCount(SaveData save) { return save.ClaimedAchievements.Count; }

        /// <summary>Pays the reward; returns a one-line note, or null when the achievement is not ready or was already taken.</summary>
        public static string Claim(SaveData save, string id)
        {
            var a = Find(id);
            if (a == null || !CanClaim(save, a)) return null;
            save.ClaimedAchievements.Add(a.Id);
            save.Doo += a.RewardDoo;
            string note = "두 +" + a.RewardDoo.ToString("N0");
            if (!string.IsNullOrEmpty(a.RewardItemId))
            {
                var item = EarlyGameContent.FindItem(a.RewardItemId);
                if (item != null) { Inventory.Add(save, item.Id, 1); note += ", " + item.Name; }
            }
            return note;
        }

        public static string ClaimAll(SaveData save)
        {
            long doo = 0; int n = 0;
            var extra = new List<string>();
            foreach (var a in All)
            {
                if (!CanClaim(save, a)) continue;
                Claim(save, a.Id);
                doo += a.RewardDoo; n++;
                if (!string.IsNullOrEmpty(a.RewardItemId)) { var it = EarlyGameContent.FindItem(a.RewardItemId); if (it != null) extra.Add(it.Name); }
            }
            if (n == 0) return null;
            return n + "건 수령: 두 +" + doo.ToString("N0") + (extra.Count > 0 ? ", " + string.Join(", ", extra) : "");
        }
    }
}
