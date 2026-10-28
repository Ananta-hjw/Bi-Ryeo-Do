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
        public void GateStagesNeedThirdRank()
        {
            Assert.IsTrue(Promotion.StageAllowed(Save(1), 11));
            Assert.IsFalse(Promotion.StageAllowed(Save(2), 12));
            Assert.IsFalse(Promotion.StageAllowed(Save(2), 13));
            Assert.IsTrue(Promotion.StageAllowed(Save(3), 12));
            Assert.IsTrue(Promotion.StageAllowed(Save(3), 13));
        }

        internal static double NotLostRate(int rank, int tier, int runs, float power = 1f)
        {
            int ok = 0;
            for (int r = 0; r < runs; r++)
            {
                var ps = Progression.HeroStatsAtCurve(Promotion.OpponentLevel(rank)); ps.MaxHp = (int)(ps.MaxHp * power); ps.Attack = (int)(ps.Attack * power); ps.Defense = (int)(ps.Defense * power);
                var player = new Combatant("p", "비류연", Team.Player, ps, SimpleBot.BestLoadout(13));
                bool lost = false;
                for (int k = 0; k < 2 && !lost; k++)
                {
                    var enemy = Promotion.MakeOpponent(rank, tier, k).CreateCombatant("e" + k);
                    var battle = new InteractiveBattle(new[] { player }, new[] { enemy }, new SeededRandom(r * 977 + rank * 13 + k));
                    battle.Begin();
                    for (int a = 0; !battle.IsOver && a < Promotion.TurnLimit; a++) battle.PlayerAct(SimpleBot.Choose(battle));
                    if (battle.IsOver && !battle.PlayerWon) lost = true;
                    player.RestBetweenFights();
                }
                if (!lost) ok++;
            }
            return (double)ok / runs;
        }

        [Test]
        public void DuelsAreHardAndNeedABuiltCharacter()
        {
            // The bare bot (no gear, 심법 or rank bonus) cannot even win the first promotion; a starter build (1.5x) can, but not rank 5...
            Assert.Less(NotLostRate(2, 1, 20), 0.5, "bare bot, normal rank 2");
            Assert.GreaterOrEqual(NotLostRate(2, 1, 20, 1.5f), 0.8, "starter build, normal rank 2");
            Assert.Less(NotLostRate(5, 1, 20, 1.5f), 0.5, "starter build, normal rank 5");
            // ...a character with about 3x the bot's power (decent gear, 심법, rank bonus; duelists wear gear too) handles the middle ranks...
            Assert.GreaterOrEqual(NotLostRate(6, 1, 20, 3f), 0.8, "built, normal rank 6");
            // ...and the top still needs a fully built one.
            Assert.Less(NotLostRate(10, 1, 20, 3f), 0.5, "built, normal rank 10");
            Assert.Less(NotLostRate(10, 2, 20, 3f), 0.5, "built, strong rank 10");
        }

        [Test]
        public void OpponentsGetHarderWithRankAndFollowThePlayersLevel()
        {
            for (int r = 3; r <= Promotion.MaxRank; r++) Assert.Greater(Promotion.HardnessFor(r), Promotion.HardnessFor(r - 1));
            Assert.Greater(Promotion.HardnessFor(2), 1.3f);
            Assert.Greater(Promotion.HardnessFor(10), 2.5f);
            Assert.AreEqual(Promotion.OpponentLevel(3), Promotion.OpponentLevelFor(3, 1));
            int curve40 = (int)System.Math.Round(Progression.CurveLevel(40));   // the opponent follows the player's stat-curve level
            Assert.AreEqual(curve40, Promotion.OpponentLevelFor(3, 40));
            Assert.AreEqual(curve40, Promotion.BuildOffer(new SaveData { SwordRank = 2, Level = 40, Doo = 1 }, 3, 1).Opponents[0].Level);
            Assert.Greater(Promotion.TurnLimitFor(10), Promotion.TurnLimitFor(2));
        }

        [Test]
        public void EverySwordRankAddsThreePercentToAllStats()
        {
            var b = new CombatStats { MaxHp = 1000, MaxQi = 200, Attack = 100, Defense = 100, Speed = 100, CritRate = 0.05f, CritDamage = 1.5f };
            var r1 = Promotion.ApplyRankBonus(b, 1);
            Assert.AreEqual(1030, r1.MaxHp);
            Assert.AreEqual(103, r1.Attack);
            var r10 = Promotion.ApplyRankBonus(b, 10);
            Assert.AreEqual(1300, r10.MaxHp);
            Assert.AreEqual(260, r10.MaxQi);
            Assert.AreEqual(130, r10.Attack);
            Assert.AreEqual(130, r10.Defense);
            Assert.AreEqual(130, r10.Speed);
            Assert.AreEqual(0.05f, r10.CritRate, 0.0001f);
            // The bonus reaches what battles and the 상태창 use.
            var low = new SaveData { Level = 20, SwordRank = 1 };
            var high = new SaveData { Level = 20, SwordRank = 10 };
            Assert.Greater(InnerArts.HeroStats(high).Attack, InnerArts.HeroStats(low).Attack);
            Assert.AreEqual(InnerArts.HeroStats(high).Attack, int.Parse(StatusSheet.Hero(high)[2].Value.Replace(",", "")));
        }
    }
}
