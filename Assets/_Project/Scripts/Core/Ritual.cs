using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    public sealed class RitualCondition
    {
        public string Text, Progress;
        public bool Done;
    }

    /// <summary>
    /// 전승의식: the trial that unlocks 진비뢰도 (six conditions, the last one a +10 비뢰도 낙뢰), the only 초월 (above 신화) piece of gear and the goal of the whole game.
    /// In the novel the 계승의식 is finished but the 전승의식 is not (Story/analysis/vol02); the conditions and the reward flow are game design.
    /// </summary>
    public static class Ritual
    {
        public const string RewardId = "jin_biroedo";
        public const int RequiredRank = 10, RequiredStage = 30, RequiredTowerFloor = 50;
        public const string BladeId = "biroe_blade";   // 비뢰도 낙뢰, which must be enhanced to +10 (and still be owned)

        public static List<RitualCondition> Conditions(SaveData save)
        {
            int rank = Promotion.EffectiveRank(save);
            var supreme = InnerArts.Find("supreme");
            int supremeLevel = InnerArts.Level(save, "supreme");
            var list = new List<RitualCondition>
            {
                new RitualCondition { Text = "검룡위 " + Promotion.RankName(RequiredRank), Progress = Promotion.RankName(rank), Done = rank >= RequiredRank },
                new RitualCondition { Text = "비기 풍신(風神) 습득", Progress = save.LearnedArts.Contains("pungsin") ? "습득" : "미습득", Done = save.LearnedArts.Contains("pungsin") },
                new RitualCondition { Text = "심법 " + (supreme != null ? supreme.Name : "무극대성") + " +" + InnerArts.MaxLevel + " 강화", Progress = InnerArts.IsLearned(save, "supreme") ? "+" + supremeLevel : "미습득", Done = supremeLevel >= InnerArts.MaxLevel },
                new RitualCondition { Text = "어려움 스테이지 " + RequiredStage + " 돌파", Progress = StageStars.StageCleared(save, RequiredStage, 1) ? "돌파" : "미돌파", Done = StageStars.StageCleared(save, RequiredStage, 1) },
                new RitualCondition { Text = "무한 비무대 " + RequiredTowerFloor + "층 돌파", Progress = save.TowerBest + "층", Done = save.TowerBest >= RequiredTowerFloor },
                new RitualCondition { Text = "비뢰도 낙뢰 +" + Gear.MaxLevel + " 보유", Progress = BladeProgress(save), Done = Inventory.Count(save, BladeId) > 0 && Gear.Level(save, BladeId) >= Gear.MaxLevel },
            };
            return list;
        }

        private static string BladeProgress(SaveData save)
        {
            if (Inventory.Count(save, BladeId) <= 0) return "미보유";
            return "+" + Gear.Level(save, BladeId);
        }

        public static int DoneCount(SaveData save)
        {
            int n = 0;
            foreach (var c in Conditions(save)) if (c.Done) n++;
            return n;
        }

        public static bool CanComplete(SaveData save) { return !save.RitualDone && DoneCount(save) == Conditions(save).Count; }

        /// <summary>Finishes the ritual and hands over 진비뢰도; returns a one-line note, or null when the conditions are not met / it was done already.</summary>
        public static string Complete(SaveData save)
        {
            if (!CanComplete(save)) return null;
            save.RitualDone = true;
            Inventory.Add(save, RewardId, 1);
            return "전승의식 완성! 진비뢰도를 얻었습니다.";
        }
    }
}
