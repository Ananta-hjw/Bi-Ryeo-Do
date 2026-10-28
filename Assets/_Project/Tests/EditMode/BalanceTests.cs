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

        [TestCase(24, 82, 130)]   // re-measured with the bot and the story enemies both in gear (GearLoadout): 0.8 - 1.25 x the measured length
        [TestCase(25, 85, 134)]
        [TestCase(26, 83, 130)]
        [TestCase(27, 89, 140)]
        [TestCase(28, 78, 124)]
        [TestCase(29, 124, 195)]
        [TestCase(30, 102, 160)]
        [TestCase(31, 60, 96)]
        [TestCase(32, 57, 91)]
        [TestCase(33, 108, 171)]
        [TestCase(34, 81, 128)]
        [TestCase(35, 108, 171)]
        [TestCase(36, 61, 96)]
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

        [TestCase(1, 1)]
        [TestCase(10, 1)]
        [TestCase(20, 1)]
        [TestCase(36, 1)]
        [TestCase(1, 2)]
        [TestCase(20, 2)]
        [TestCase(36, 2)]
        public void HarderStagesPlayLikeTheSameNormalStageAtTheirTargetLevel(int number, int difficulty)   // 어려움·극악 재조정: same win rate, about the same fight length
        {
            var normal = EarlyGameContent.FindStage(number);
            var harder = normal.WithDifficulty(difficulty);
            int level = (int)Math.Round(StageStars.TargetLevel(number, difficulty));
            var baseRuns = Enumerable.Range(901, 20).Select(seed => BalanceSimulation.Play(normal, BalanceSimulation.Starter(number, StageStars.NormalBotLevel(number)), seed)).ToList();
            var runs = Enumerable.Range(901, 20).Select(seed => BalanceSimulation.Play(harder, BalanceSimulation.Starter(number, level), seed)).ToList();
            Assert.GreaterOrEqual(runs.Count(r => r.Won), 17, "the bot at the target level should win about as often as on 일반");
            double ratio = runs.Where(r => r.Won).Average(r => r.Actions) / baseRuns.Where(r => r.Won).Average(r => r.Actions);
            TestContext.Out.WriteLine(number + "/" + difficulty + ": lv" + level + ", length x" + ratio.ToString("F2"));
            // Both sides wear gear now (GearLoadout); the harder difficulties' gear differs more from 일반 at the two ends, so the band is wider.
            Assert.That(ratio, Is.InRange(0.5, 2.0), "fights must not become slogs");
        }

        [Test]
        public void SideContentUsesNormalScalingOnceAndHardDifficultiesKeepTheirOwnRules()
        {
            var stage = EarlyGameContent.FindStage(26);
            var id = stage.Encounters[0][0];
            var raw = EarlyGameContent.FindEnemy(id);
            var normal = StageStars.ScaleEnemy(raw, 26, 0);
            Assert.AreEqual(raw.HpMul * StageStars.NormalHpBoost(26) * 0.85f * StageStars.NormalHpTrim(26), normal.HpMul, 0.001f);   // 일반 only: boosted, then the late-stage shortening and the HP trim
            var story = stage.EnemyFor(id);
            Assert.AreEqual(normal.HpMul, story.HpMul, 0.001f);   // story stages: the same multipliers, plus the stage's enemy gear
            Assert.AreSame(GearLoadout.ForEnemies(26, 0), story.Gear);
            Assert.AreEqual(Inventory.ApplyGear(normal.BuildStatsBeforeRealm(), GearLoadout.Ids(story.Gear), story.Gear).MaxHp, story.BuildStatsBeforeRealm().MaxHp);
            var survivalEnemy = EarlyGameContent.FindEnemy("geomhu_ioksang");
            Assert.AreSame(survivalEnemy, StageStars.ScaleEnemy(survivalEnemy, 29, 0));
            Assert.AreEqual(survivalEnemy.HpMul, EarlyGameContent.FindStage(29).EnemyFor(survivalEnemy.Id).HpMul, 0.001f);   // a 버티기-only enemy keeps its multipliers
            var hard = stage.WithDifficulty(1).EnemyFor(id);
            Assert.AreEqual(normal.HpMul * StageStars.HardHpFix(26, 1), hard.HpMul, 0.001f);   // 어려움 = the 일반 enemy of the stage, raised by level, with the fight-length fix and its own gear
            Assert.AreSame(GearLoadout.ForEnemies(26, 1), hard.Gear);
            Assert.AreEqual(normal.Level + StageStars.LevelAdd(26, 1), hard.Level);
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
            Assert.AreEqual(Tower.NormalHpMul * Tower.FloorHpTune(24) * StageStars.NormalHpBoost(anchor) * late * StageStars.NormalHpTrim(anchor), towerEnemy.HpMul, .001f);
            Assert.AreEqual(Tower.AtkMulFor(24, false) * StageStars.NormalAtkBoost(anchor) * StageStars.NormalStatEase * 1.25f, towerEnemy.AtkMul, .001f);
            Assert.AreEqual(StageStars.NormalStatEase, towerEnemy.DefMul, .001f);
            Assert.Greater(Tower.BuildEnemies(35)[0].BuildStats().Attack, Tower.BuildEnemies(30)[0].BuildStats().Attack);
        }
    }
}
