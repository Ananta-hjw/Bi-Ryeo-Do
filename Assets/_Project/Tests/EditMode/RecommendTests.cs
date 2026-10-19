using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class RecommendTests
    {
        [Test]
        public void StageRewardMatchesTheSumOfItsFightsAndHalvesOnReplay()
        {
            var save = new SaveData();
            var st = EarlyGameContent.FindStage(5);
            Recommend.StageReward(save, st, out long exp, out long doo);
            long e = 0, d = 0;
            for (int i = 0; i < st.Encounters.Length; i++) { Recommend.FightReward(save, st, i, false, out long fe, out long fd); e += fe; d += fd; }
            Assert.AreEqual(e, exp);
            Assert.AreEqual(d, doo);
            Assert.Greater(exp, 0);
            save.ClearedStage = 5;
            Recommend.StageReward(save, st, out long exp2, out long doo2);
            Assert.Less(exp2, exp);
            Assert.Less(doo2, doo);
        }

        [Test]
        public void FightsAlreadyWonOnAnUnclearedStagePayTheReplayRate()
        {
            var save = new SaveData();
            var st = EarlyGameContent.FindStage(5);
            Assert.Greater(st.Encounters.Length, 1);
            Recommend.StageReward(save, st, out long full, out long fullDoo);
            Recommend.FightReward(save, st, 0, false, out long first, out long firstDoo);
            Recommend.FightReward(save, st, 0, true, out long half, out long halfDoo);
            StageStars.Record(save, st.Number, 0, 0, 1);   // won fight 1, then lost a later fight
            Assert.IsTrue(Recommend.WonBefore(save, st, 0));
            Assert.IsFalse(Recommend.WonBefore(save, st, 1));
            Assert.IsFalse(Recommend.WonBefore(save, st.WithDifficulty(1), 0), "each difficulty keeps its own record");
            Recommend.StageReward(save, st, out long exp, out long doo);
            Assert.AreEqual(full - first + half, exp);
            Assert.AreEqual(fullDoo - firstDoo + halfDoo, doo);
        }

        [Test]
        public void HarderDifficultiesAskForMoreAndTheTowerClimbs()
        {
            var st = EarlyGameContent.FindStage(20);
            double n = Recommend.ForStage(st), h = Recommend.ForStage(st.WithDifficulty(1)), x = Recommend.ForStage(st.WithDifficulty(2));
            Assert.Greater(h, n);
            Assert.Greater(x, h);
            Assert.Greater(Recommend.ForTowerFloor(30), Recommend.ForTowerFloor(5));
            Assert.IsNotEmpty(Recommend.Line(new SaveData(), n));
        }

        [Test]
        public void PromotionRecommendationRisesWithTheRank()
        {
            var save = new SaveData { Level = 30, SwordRank = 1 };
            double low = Recommend.ForPromotion(Promotion.BuildOffer(save, 2, 0));
            save.SwordRank = 7;
            double high = Recommend.ForPromotion(Promotion.BuildOffer(save, 8, 2));
            Assert.Greater(high, low);
        }
    }
}
