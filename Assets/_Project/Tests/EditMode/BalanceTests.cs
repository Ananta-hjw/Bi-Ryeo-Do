using System;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class BalanceTests
    {
        [Test]
        public void PreparedFixtureUsesOnlyTheActualStartKitAndEntryMastery()
        {
            var save = BalanceSimulation.Starter(5, 6);
            CollectionAssert.AreEquivalent(EarlyGameContent.StartKit.Select(x => x.Id), save.Items.Select(x => x.Id));
            Assert.IsEmpty(save.GearLevels);
            Assert.IsEmpty(save.ArtMastery);
            var hero = Formation.BuildHero(save);
            Assert.AreEqual(0.5f, hero.Skills.First(x => x.Id == "samcheopyeong").EvadeChance);
        }

        [Test]
        public void FirstSixStagesAreAffordableWithoutRandomGearOrForcedReplays()
        {
            var reached = new int[6];
            var wins = new int[6];
            for (int seed = 301; seed <= 500; seed++)
            {
                var save = new SaveData();
                Inventory.EnsureStartKit(save);
                foreach (var stage in EarlyGameContent.Stages.Take(6))
                {
                    int n = stage.Number - 1;
                    reached[n]++;
                    BalanceSimulation.BuyLoadout(save, stage.Number);
                    if (!BalanceSimulation.Play(stage, save, seed, true).Won) break;
                    wins[n]++;
                    save.ClearedStage = stage.Number;
                    Assert.GreaterOrEqual(save.Doo, 0);
                }
            }
            for (int n = 0; n < 6; n++) TestContext.Out.WriteLine("campaign " + (n + 1) + ": " + wins[n] + "/" + reached[n]);
            Assert.That(wins[4], Is.InRange(130, 190), "the first bottleneck must be neither a wall nor an automatic win");
            Assert.GreaterOrEqual(wins[5], (int)(wins[4] * 0.9), "stage 6 should remain accessible after the stage-5 level-up");
            for (int n = 0; n < 4; n++) Assert.GreaterOrEqual(wins[n], (int)(reached[n] * 0.9));
        }

        [TestCase(24, 137, 215)]
        [TestCase(25, 123, 193)]
        [TestCase(26, 130, 246)]   // shorter since 일반 8+ enemy attack/defense x0.8
        [TestCase(27, 85, 131)]
        [TestCase(28, 124, 194)]
        [TestCase(29, 160, 256)]
        [TestCase(30, 165, 259)]
        [TestCase(31, 96, 150)]
        [TestCase(32, 63, 99)]
        [TestCase(33, 136, 214)]
        [TestCase(34, 110, 173)]
        [TestCase(35, 127, 198)]
        [TestCase(36, 70, 110)]
        public void LateNormalStagesKeepPressureWithoutLongHpSlogs(int number, int minActions, int maxActions)   // ranges re-measured after the 일반 상향 (HP and attack both raised: longer fights are fine, slogs are not)
        {
            var stage = EarlyGameContent.FindStage(number);
            var runs = Enumerable.Range(301, 100).Select(seed => BalanceSimulation.Play(stage,
                BalanceSimulation.Starter(number, BalanceSimulation.BotLevel(stage)), seed)).ToList();
            var wins = runs.Where(x => x.Won).ToList();
            Assert.GreaterOrEqual(wins.Count, 85, "prepared players should not need repeated grinding");
            double actions = wins.Average(x => x.Actions);
            TestContext.Out.WriteLine(number + ": " + wins.Count + "/100, actions " + actions.ToString("F1"));
            Assert.That(actions, Is.InRange((double)minActions, (double)maxActions));
        }

        [TestCase(5)]
        [TestCase(15)]
        [TestCase(25)]
        [TestCase(30)]
        public void TowerBossSpikesAreChallengingButNotWalls(int floor)
        {
            int story = Math.Max(8, Math.Min(30, floor + 6));
            int level = Tower.RecommendedPlayerLevel(floor);
            int wins = 0, grownWins = 0;
            for (int seed = 301; seed <= 400; seed++)
            {
                var save = BalanceSimulation.Starter(story, level);
                var stage = Tower.ToStage(save, floor);
                if (BalanceSimulation.Play(stage, save, seed).Won) wins++;
                var grown = BalanceSimulation.Starter(story, level + 1);
                if (BalanceSimulation.Play(stage, grown, seed).Won) grownWins++;
            }
            TestContext.Out.WriteLine("tower " + floor + ": " + wins + "/100; +1 level " + grownWins + "/100");
            Assert.GreaterOrEqual(wins, 85, "prepared, telegraph-aware players should have a reliable route; careless play is checked separately");
            Assert.GreaterOrEqual(grownWins, wins, "one level of investment must not reduce the measured chance");
        }

        [Test]
        public void RequiredPromotionKeepsASafeRouteAndARiskyHigherPayout()
        {
            int NotLost(int tier)
            {
                int count = 0;
                for (int seed = 301; seed <= 400; seed++)
                {
                    var save = BalanceSimulation.Starter(11, 12);
                    save.SwordRank = 2;
                    var offer = Promotion.BuildOffer(save, 3, tier);
                    save.Doo = offer.Fee;
                    Assert.IsTrue(Promotion.Pay(save, offer));
                    var hero = Formation.BuildHero(save);
                    bool lost = false;
                    for (int k = 0; k < offer.Opponents.Length; k++)
                    {
                        var battle = new InteractiveBattle(hero, new[] { offer.Opponents[k].CreateCombatant("duelist") }, new SeededRandom(seed * 977 + 39 + k));
                        battle.Begin();
                        for (int a = 0; !battle.IsOver && a < Promotion.TurnLimitFor(3); a++) battle.PlayerAct(SimpleBot.Choose(battle));
                        if (battle.IsOver && !battle.PlayerWon) { lost = true; break; }
                        hero.RestBetweenFights();
                    }
                    if (!lost) count++;
                }
                return count;
            }
            Assert.GreaterOrEqual(NotLost(0), 90);
            Assert.That(NotLost(2), Is.InRange(20, 80));
        }

        [Test]
        public void SideContentUsesNormalScalingOnceAndHardDifficultiesKeepTheirOwnRules()
        {
            var stage = EarlyGameContent.FindStage(26);
            var id = stage.Encounters[0][0];
            var raw = EarlyGameContent.FindEnemy(id);
            Assert.AreEqual(raw.HpMul * StageStars.NormalHpBoost(26) * 0.85f, stage.EnemyFor(id).HpMul, 0.001f);   // 일반 only: boosted, then the late-stage shortening
            var survivalEnemy = EarlyGameContent.FindEnemy("geomhu_ioksang");
            Assert.AreSame(survivalEnemy, EarlyGameContent.FindStage(29).EnemyFor(survivalEnemy.Id));
            var hard = stage.WithDifficulty(1).EnemyFor(id);
            Assert.AreEqual(raw.HpMul * StageStars.HpFor(26, 1) / (float)Math.Pow(StageStars.NormalEase(26), 0.7) * StageStars.NormalHpBoost(26) * StageStars.HardExtra(26, 1), hard.HpMul, 0.001f);   // 어려움 carries the stage boost and its own extra, but not the late-stage x0.85
            var quest = Quests.ToStage(Quests.Find("q_salgwi"));
            id = quest.Encounters[0][0];
            var q = Quests.Find(quest.QuestId);
            var questBase = StageStars.ScaleEnemy(EarlyGameContent.FindEnemy(id), q.RefStage, 0);
            Assert.AreEqual(questBase.HpMul * (float)Math.Pow(q.Edge, .7), quest.EnemyFor(id).HpMul, .001f);
            var tower = Tower.ToStage(BalanceSimulation.Starter(30, 26), 24);
            id = tower.Encounters[0][0];
            Assert.AreSame(EarlyGameContent.FindEnemy(id), tower.EnemyFor(id));
            var towerEnemy = tower.EnemyFor(id);
            int anchor = Tower.ReferenceStage(24);
            float late = anchor >= 24 ? .85f : 1f;
            Assert.AreEqual(Tower.NormalHpMul * Tower.FloorHpTune(24) * StageStars.NormalHpBoost(anchor) * late, towerEnemy.HpMul, .001f);
            Assert.AreEqual(Tower.AtkMulFor(24, false) * StageStars.NormalAtkBoost(anchor) * StageStars.NormalStatEase * 1.25f, towerEnemy.AtkMul, .001f);
            Assert.AreEqual(StageStars.NormalStatEase, towerEnemy.DefMul, .001f);
            Assert.Greater(Tower.BuildEnemies(35)[0].BuildStats().Attack, Tower.BuildEnemies(30)[0].BuildStats().Attack);
        }
    }
}
