using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class FormationPlanningTests
    {
        private static Combatant Unit(string id, Team team, int hp, int attack, int defense = 50)
        {
            return new Combatant(id, id, team, new CombatStats
            {
                MaxHp = hp, MaxQi = 100, Attack = attack, Defense = defense,
                Speed = 100, CritDamage = 1.5f
            }, null);
        }

        private static IDictionary Cache(System.Type type, string name)
        {
            return (IDictionary)type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        }

        private static string Snapshot(params Combatant[] units)
        {
            return (string)typeof(FormationPlanner).GetMethod("Fingerprint", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { units, true });
        }

        [SetUp]
        [TearDown]
        public void ClearCaches()
        {
            Cache(typeof(Formation), "PlanCache").Clear();
            Cache(typeof(FormationPlanner), "EnemyCache").Clear();
        }

        [Test]
        public void ForecastsPreserveLiveResourcesStatusesCooldownsPhasesAndPositions()
        {
            var hero = Unit("player", Team.Player, 2400, 180);
            var guard = Unit("guard", Team.Player, 4000, 80, 250);
            hero.Col = 1; hero.Row = 0; hero.Hp = 1200; hero.Qi = 27; hero.Spirit = 46; hero.Gauge = 117;
            hero.AddStatus(StatusKind.Burn, 3);
            hero.Skills.Add(new SkillSpec { Id = "strong", Power = 3, Cooldown = 3, QiCost = 20 });
            var cooldowns = (Dictionary<string, int>)typeof(Combatant).GetField("Cooldowns", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hero);
            cooldowns.Add("strong", 2);
            var foe = Unit("boss", Team.Enemy, 5000, 190);
            foe.IsBoss = true;
            foe.Phases = new List<BossPhase> { new BossPhase { HpPct = .5f, AtkMul = 1.5f, DefMul = 1.2f } };
            foe.ScaleAttackDefense(1.2f, 1.1f);
            foe.Charging = true; foe.ChargeTarget = hero;
            foe.Moves = new List<EnemyMove> { EnemyMove.Attack("strike", 3) };
            string before = Snapshot(hero, guard, foe);
            var party = new[] { hero, guard };
            var fights = new List<IList<Combatant>> { new[] { foe } };
            var first = FormationPlanner.PlanPartyLines(party, fights, null);
            var second = FormationPlanner.PlanPartyLines(party, fights, null);
            CollectionAssert.AreEquivalent(first, second);
            Assert.AreEqual(before, Snapshot(hero, guard, foe));
            Assert.AreSame(hero, foe.ChargeTarget);
            Assert.AreEqual(3, hero.Statuses[0].Turns);
            Assert.AreEqual(2, hero.CooldownLeft(hero.Skills[1]));
        }

        [Test]
        public void MidRangeCannotReachTheProtectedEnemyInTheThirdLine()
        {
            var hero = Unit("player", Team.Player, 20000, 300);
            hero.Col = 2; hero.Row = 1;
            hero.Skills.Add(new SkillSpec { Id = "middle", Power = 2, Range = 2 });
            var wall = Unit("wall", Team.Enemy, 12000, 10, 500);
            var reserve = Unit("reserve", Team.Enemy, 10000, 10, 450);
            var striker = Unit("striker", Team.Enemy, 1000, 1000, 10);
            Formation.PlaceEnemies(new[] { wall, reserve, striker }, new[] { hero });
            Assert.AreEqual(5, striker.Col);
            var battle = new InteractiveBattle(new[] { hero }, new[] { wall, reserve, striker }, new SeededRandom(51));
            CollectionAssert.DoesNotContain(battle.EnemiesInRange(2), striker);
            CollectionAssert.Contains(battle.EnemiesInRange(3), striker);
        }

        [TestCase("qi")]
        [TestCase("critical")]
        [TestCase("perks")]
        [TestCase("skill")]
        [TestCase("status")]
        [TestCase("enemy")]
        [TestCase("phase")]
        public void EnemyPlansRefreshWhenCombatInputsChange(string input)
        {
            var hero = Unit("player", Team.Player, 2500, 250);
            hero.Col = 2; hero.Row = 1;
            var wall = Unit("wall", Team.Enemy, 5000, 80);
            var striker = Unit("striker", Team.Enemy, 900, 350);
            striker.Moves = new List<EnemyMove> { EnemyMove.Attack("strike", 1) };
            FormationPlanner.PlanEnemyCols(new[] { wall, striker }, new[] { hero });
            Assert.AreEqual(1, Cache(typeof(FormationPlanner), "EnemyCache").Count);
            switch (input)
            {
                case "qi": hero.Qi = 0; break;
                case "critical":
                    var stats = hero.Stats; stats.CritRate = .5f; stats.CritDamage = 2;
                    hero = new Combatant(hero.Id, hero.Name, hero.Team, stats, null) { Col = 2, Row = 1 };
                    break;
                case "perks": hero.Lifesteal = .2f; hero.SpiritDealtBonus = 4; break;
                case "skill": hero.Skills[0].QiCost = 20; hero.Skills[0].EvadeChance = .5f; break;
                case "status": hero.AddStatus(StatusKind.Poison, 2); break;
                case "enemy": striker.Moves[0].Sniping(); break;
                case "phase": striker.Phases = new List<BossPhase> { new BossPhase { HpPct = .5f, AtkMul = 2, DefMul = 1 } }; break;
            }
            FormationPlanner.PlanEnemyCols(new[] { wall, striker }, new[] { hero });
            Assert.AreEqual(2, Cache(typeof(FormationPlanner), "EnemyCache").Count);
        }

        [TestCase(24)]
        [TestCase(30)]
        [TestCase(31)]
        public void LateStagePlansRemainViableOutsideForecastSamples(int number)
        {
            var stage = EarlyGameContent.FindStage(number);
            int wins = 0;
            for (int seed = 901; seed < 941; seed++)
                if (BalanceSimulation.Play(stage, BalanceSimulation.Starter(number, BalanceSimulation.BotLevel(stage)), seed).Won) wins++;
            TestContext.Out.WriteLine(number + ": " + wins + "/40 holdout wins");
            Assert.GreaterOrEqual(wins, 34, "formation changes must not strand the prepared baseline party");
        }

        [Test]
        public void ReturnedEnemyPlansCannotCorruptTheCache()
        {
            var hero = Unit("player", Team.Player, 2500, 250);
            hero.Col = 2; hero.Row = 1;
            var enemies = new[] { Unit("wall", Team.Enemy, 5000, 80), Unit("striker", Team.Enemy, 900, 350) };
            var first = FormationPlanner.PlanEnemyCols(enemies, new[] { hero });
            int expected = first[0];
            first[0] = -1;
            Assert.AreEqual(expected, FormationPlanner.PlanEnemyCols(enemies, new[] { hero })[0]);
        }

        [Test]
        public void PartyCacheIncludesEnemyDefinitionsAndTrialRules()
        {
            const string id = "formation_cache_test_guard";
            EarlyGameContent.RegisterDynamicEnemy(new EnemyData { Id = id, Name = "Test", Level = 5 });
            var stage = new StageData { Number = 1, Encounters = new[] { new[] { id } } };
            var save = new SaveData();
            Formation.AutoFor(save, new[] { "player" }, stage);
            EarlyGameContent.RegisterDynamicEnemy(new EnemyData { Id = id, Name = "Test", Level = 5, AtkMul = 2 });
            Formation.AutoFor(save, new[] { "player" }, stage);
            Assert.AreEqual(2, Cache(typeof(Formation), "PlanCache").Count);
            stage.Trials = new[] { TrialRule.Survive("Test", "Test", 4) };
            Formation.AutoFor(save, new[] { "player" }, stage);
            Assert.AreEqual(3, Cache(typeof(Formation), "PlanCache").Count);
        }

        [Test]
        public void ResumedPlansIgnoreClearedFightsAndRefreshStoredResources()
        {
            var stage = new StageData { Number = 1, Encounters = new[] { new[] { "bandit" }, new[] { "bandit" } } };
            // Use a real enemy id, without depending on the starter encounter composition.
            stage.Encounters[0][0] = stage.Encounters[1][0] = EarlyGameContent.Stages[0].Encounters[0][0];
            var save = new SaveData();
            var hero = Formation.BuildHero(save);
            hero.Hp /= 2; hero.Qi = 0;
            StageProgress.Store(save, 1, 1, new[] { hero });
            Formation.AutoFor(save, new[] { "player" }, stage);
            stage.Encounters[0] = new[] { EarlyGameContent.Stages[4].Encounters.Last()[0] };
            Formation.AutoFor(save, new[] { "player" }, stage);
            Assert.AreEqual(1, Cache(typeof(Formation), "PlanCache").Count, "already cleared encounters must not affect the plan");
            save.ProgressHp[0].Qi = 10;
            Formation.AutoFor(save, new[] { "player" }, stage);
            Assert.AreEqual(2, Cache(typeof(Formation), "PlanCache").Count);
            Assert.AreEqual(hero.Hp, save.ProgressHp[0].Hp);
        }
    }
}
