using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class PromotionTests
    {
        private static SaveData Save(int rank, long doo = 100000)
        {
            return new SaveData { SwordRank = rank, Doo = doo };
        }

        [Test]
        public void OldSavesCountAsRankOne()
        {
            Assert.AreEqual(1, Promotion.EffectiveRank(new SaveData()));
            Assert.AreEqual("일검룡", Promotion.RankName(1));
            Assert.AreEqual("십검룡", Promotion.RankName(10));
        }

        [Test]
        public void TargetsAreOneOrTwoRanksUpAndCapped()
        {
            CollectionAssert.AreEqual(new[] { 3, 4 }, Promotion.TargetRanks(Save(2)));
            CollectionAssert.AreEqual(new[] { 10 }, Promotion.TargetRanks(Save(9)));
            Assert.AreEqual(0, Promotion.TargetRanks(Save(10)).Count);
            Assert.Throws<System.ArgumentException>(() => Promotion.BuildOffer(Save(2), 2, 1));
            Assert.Throws<System.ArgumentException>(() => Promotion.BuildOffer(Save(2), 5, 1));
        }

        [Test]
        public void SkipGradePaysMoreAndHigherTiersPayMore()
        {
            var save = Save(2);
            var normal = Promotion.BuildOffer(save, 3, 1);
            Assert.IsFalse(normal.Skip);
            Assert.IsTrue(Promotion.BuildOffer(save, 4, 1).Skip);
            Assert.Greater(Promotion.BuildOffer(save, 4, 1).DooReward, Promotion.BuildOffer(save, 3, 1).DooReward);
            Assert.Greater(Promotion.BuildOffer(save, 3, 2).DooReward, normal.DooReward);
            Assert.Less(Promotion.BuildOffer(save, 3, 0).DooReward, normal.DooReward);
            Assert.Greater(normal.Fee, 0);
            Assert.AreEqual(2, normal.Opponents.Length);
        }

        [Test]
        public void FeeIsChargedAndCannotBeSkippedWhenPoor()
        {
            var save = Save(2, 10);
            var offer = Promotion.BuildOffer(save, 3, 1);
            Assert.IsFalse(Promotion.Pay(save, offer));
            Assert.AreEqual(10, save.Doo);
            save.Doo = offer.Fee + 5;
            Assert.IsTrue(Promotion.Pay(save, offer));
            Assert.AreEqual(5, save.Doo);
        }

        [Test]
        public void CompletePromotesAndDrawHalvesPay()
        {
            var a = Save(2, 0);
            var offer = Promotion.BuildOffer(a, 3, 1);
            long full = Promotion.Complete(a, offer, false);
            Assert.AreEqual(3, a.SwordRank);
            Assert.AreEqual(offer.DooReward, full);

            var b = Save(2, 0);
            long half = Promotion.Complete(b, offer, true);
            Assert.AreEqual(3, b.SwordRank);
            Assert.AreEqual(full / 2.0, half, 1.0);
        }

        [Test]
        public void GateStagesNeedFifthRank()
        {
            Assert.IsTrue(Promotion.StageAllowed(Save(1), 11));
            Assert.IsFalse(Promotion.StageAllowed(Save(4), 12));
            Assert.IsFalse(Promotion.StageAllowed(Save(4), 13));
            Assert.IsTrue(Promotion.StageAllowed(Save(5), 12));
        }

        private static double NotLostRate(int rank, int tier, int runs)
        {
            int ok = 0;
            for (int r = 0; r < runs; r++)
            {
                var player = new Combatant("p", "비류연", Team.Player, Progression.StatsFor(Promotion.OpponentLevel(rank)), SimpleBot.BestLoadout(13));
                bool lost = false;
                for (int k = 0; k < 2 && !lost; k++)
                {
                    var enemy = Promotion.MakeOpponent(rank, tier, k).CreateCombatant("e" + k);
                    var battle = new InteractiveBattle(new[] { player }, new[] { enemy }, new SeededRandom(r * 977 + rank * 13 + k));
                    battle.Begin();
                    for (int a = 0; !battle.IsOver && a < Promotion.TurnLimit; a++) battle.PlayerAct(SimpleBot.Choose(battle));
                    if (battle.IsOver && !battle.PlayerWon) lost = true;
                    player.RestBetweenFights(0.5f);
                }
                if (!lost) ok++;
            }
            return (double)ok / runs;
        }

        [Test]
        public void BotPassesNormalAndWeakDuelsAtEveryRank()
        {
            for (int rank = 2; rank <= Promotion.MaxRank; rank++)
            {
                Assert.GreaterOrEqual(NotLostRate(rank, 1, 20), 0.8, "normal rank " + rank);
                Assert.GreaterOrEqual(NotLostRate(rank, 0, 20), 0.8, "weak rank " + rank);
            }
        }
    }
}
