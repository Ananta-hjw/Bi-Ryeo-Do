using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class BeastTests
    {
        private static CombatStats Stats(int hp, int atk, int speed) { return new CombatStats { MaxHp = hp, MaxQi = 100, Attack = atk, Defense = 0, Speed = speed, CritRate = 0f, CritDamage = 1.5f }; }

        private static Combatant Hero(int speed = 100) { return new Combatant(Inventory.Hero, "비류연", Team.Player, Stats(100000, 300, speed), null); }

        private static Combatant Enemy(string id, int speed = 10, bool boss = false, params EnemyMove[] moves)
        {
            var c = new Combatant(id, id, Team.Enemy, Stats(100000, 100, speed), null) { IsBoss = boss };
            c.Moves = new List<EnemyMove>(moves.Length > 0 ? moves : new[] { EnemyMove.Attack("주먹", 1f) });
            return c;
        }

        private static InteractiveBattle Battle(string beastId, int bond, params Combatant[] enemies)
        {
            var b = new InteractiveBattle(Hero(), enemies, new SeededRandom(5)) { Beast = Beasts.Loadout(Beasts.Find(beastId), bond) };
            b.Begin();
            return b;
        }

        private static BeastSkill SkillOf(string id) { return Beasts.FindSkill(id); }

        [Test]
        public void BeastsArriveWithTheirStagesAndTheFirstOneIsTakenAlong()
        {
            var save = new SaveData { ClearedStage = Beasts.Find("mukmuk").JoinStage - 1 };
            Assert.AreEqual(0, Beasts.Sync(save).Count);
            Assert.AreEqual("", save.ActiveBeast);
            save.ClearedStage = Beasts.Find("urwe").JoinStage;
            var got = Beasts.Sync(save);
            CollectionAssert.AreEqual(new[] { "mukmuk", "urwe" }, got.Select(b => b.Id).ToArray());
            Assert.AreEqual("mukmuk", save.ActiveBeast);
            Assert.AreEqual(0, Beasts.Sync(save).Count, "once only");
            save.ClearedStage = 66;
            Beasts.Sync(save);
            Assert.AreEqual(Beasts.All.Length, save.Beasts.Count);
            Assert.IsTrue(Beasts.All.Count(b => !b.Original) == 3, "우뢰 · 청홍쌍각사 · 백무후 come from the novel");
        }

        [Test]
        public void FeedingRaisesBondOpensTheSecondSkillAndShortensTheRest()
        {
            var save = new SaveData { ClearedStage = 30 };
            Beasts.Sync(save);
            Inventory.Add(save, Beasts.FoodId, 100);
            Assert.AreEqual(1, Beasts.LoadoutFor(save).Skills.Count);
            Assert.AreEqual(0, Beasts.Feed(save, "baekmuhu", Beasts.FoodId, 1), "not owned yet");
            Assert.AreEqual(Beasts.BondThresholds[2], Beasts.Feed(save, "mukmuk", Beasts.FoodId, Beasts.BondThresholds[2]));
            Assert.AreEqual(3, Beasts.Bond(save, "mukmuk"));
            var l = Beasts.LoadoutFor(save);
            Assert.AreEqual(2, l.Skills.Count);
            Assert.AreEqual(1.3f, l.PowerFactor, 1e-5);
            Assert.AreEqual(Beasts.Find("mukmuk").Cooldown, l.Cooldown);
            Inventory.Add(save, Beasts.FeastId, 10);
            Beasts.Feed(save, "mukmuk", Beasts.FeastId, 10);
            Assert.AreEqual(Beasts.MaxBond, Beasts.Bond(save, "mukmuk"));
            Assert.Less(Inventory.Count(save, Beasts.FeastId), 10);
            Assert.Greater(Inventory.Count(save, Beasts.FeastId), 0, "feeding stops at the top 친밀도");
            Assert.AreEqual(Beasts.Find("mukmuk").Cooldown - 1, Beasts.LoadoutFor(save).Cooldown);
        }

        [Test]
        public void CallingIsAFreeActionWithARest()
        {
            var e = Enemy("e");
            var b = Battle("urwe", 3, e);
            var hero = b.Current;
            Assert.AreSame(b.Player, hero);
            Assert.IsTrue(b.CanCallBeast);
            int hp = e.Hp, actions = b.PartyActions;
            var evs = b.CallBeast(PlayerCommand.CallBeast(SkillOf("urwe_dive"), e));
            Assert.Less(e.Hp, hp);
            Assert.IsTrue(evs.Where(x => x.Kind == EventKind.Attack).All(x => x.Beast && x.IsCrit), "급강하 always crits");
            Assert.AreEqual(2, evs.Count(x => x.Kind == EventKind.Attack));
            Assert.AreSame(hero, b.Current, "the turn stays 비류연's");
            Assert.AreEqual(actions, b.PartyActions);
            Assert.IsFalse(b.CanCallBeast);
            Assert.Throws<System.InvalidOperationException>(() => b.CallBeast(PlayerCommand.CallBeast(SkillOf("urwe_scout"), e)));
            int cd = b.BeastCooldownLeft;
            for (int i = 0; i < cd; i++)
            {
                Assert.IsFalse(b.CanCallBeast, "resting " + i);
                b.PlayerAct(PlayerCommand.Guard());
            }
            Assert.IsTrue(b.CanCallBeast, "ready after " + cd + " of his turns");
        }

        [Test]
        public void ASecondSkillNeedsTheBondAndNoBeastMeansNoCall()
        {
            var e = Enemy("e");
            var low = Battle("urwe", 0, e);
            Assert.IsFalse(low.CanUseBeastSkill(SkillOf("urwe_dive")));
            Assert.IsTrue(low.CanUseBeastSkill(SkillOf("urwe_scout")));
            var none = new InteractiveBattle(Hero(), new[] { Enemy("x") }, new SeededRandom(1));
            none.Begin();
            Assert.IsFalse(none.CanCallBeast);
        }

        [Test]
        public void ScoutTellsTheNextMove()
        {
            var e = Enemy("e", 10, false, EnemyMove.Attack("독장", 1f), EnemyMove.Attack("주먹", 1f));
            var b = Battle("urwe", 0, e);
            var evs = b.CallBeast(PlayerCommand.CallBeast(SkillOf("urwe_scout"), e));
            var report = evs.First(x => x.Tag == "정찰");
            StringAssert.Contains(e.PeekMove().Name, report.Notice);
        }

        [Test]
        public void SnakesPoisonForSureAndParalyzeOnlyCutsABossCharge()
        {
            var e = Enemy("e");
            var b = Battle("ssanggaksa", 3, e);
            b.CallBeast(PlayerCommand.CallBeast(SkillOf("ssang_fang"), e));
            Assert.IsTrue(e.Has(StatusKind.Poison));

            var minion = Enemy("m");
            var b2 = Battle("ssanggaksa", 3, minion);
            b2.CallBeast(PlayerCommand.CallBeast(SkillOf("ssang_glare"), minion));
            Assert.IsTrue(minion.Stunned);

            var boss = Enemy("boss", 10, true);
            var b3 = Battle("ssanggaksa", 3, boss);
            boss.Charging = true;
            b3.CallBeast(PlayerCommand.CallBeast(SkillOf("ssang_glare"), boss));
            Assert.IsFalse(boss.Stunned, "a boss is never stunned");
            Assert.IsFalse(boss.Charging, "but its wind-up is cut");
        }

        [Test]
        public void ShellCutsTheNextHitOnceAndHealScalesWithBond()
        {
            var e = Enemy("e", 200);
            var b = Battle("turtle", 0, e);
            var hero = b.Player;
            b.CallBeast(PlayerCommand.CallBeast(SkillOf("turtle_shell"), null).WithAlly(hero));
            Assert.AreEqual(.30f, hero.ShellFactor, 1e-5);
            var evs = b.PlayerAct(PlayerCommand.Guard());
            var hit = evs.First(x => x.Kind == EventKind.Attack && x.TargetId == hero.Id && !x.Dodged);
            Assert.AreEqual("등껍질", hit.Tag);
            Assert.AreEqual(1f, hero.ShellFactor);

            var monkey = Battle("monkey", 5, Enemy("x"));
            monkey.Player.Hp = 1000;
            var heal = monkey.CallBeast(PlayerCommand.CallBeast(SkillOf("monkey_herb"), null)).First(x => x.Kind == EventKind.Heal);
            Assert.AreEqual((int)System.Math.Round(100000 * .12f * 1.5f), heal.Heal);
        }

        [Test]
        public void PlayerActRoutesABeastCommandAsAFreeAction()
        {
            var e = Enemy("e");
            var b = Battle("baekmuhu", 3, e);
            var hero = b.Current;
            var evs = b.PlayerAct(PlayerCommand.CallBeast(SkillOf("baek_claw"), null));
            Assert.IsTrue(evs.Any(x => x.Beast && x.Kind == EventKind.Attack));
            Assert.AreSame(hero, b.Current);
            Assert.IsFalse(evs.Any(x => x.ActorId == e.Id), "no enemy acted");
        }

        [Test]
        public void ScriptedShowdownsKeepTheBeastAway()
        {
            var b = new InteractiveBattle(Hero(), new[] { Enemy("e") }, new SeededRandom(1))
            {
                Beast = Beasts.Loadout(Beasts.Find("urwe"), 0),
                Trial = new TrialRule { Kind = TrialKind.Finale, Style = FinaleStyle.Duel, Limit = 5, Script = new ShowdownScript() }
            };
            b.Begin();
            Assert.IsFalse(b.CanCallBeast);
        }

        [Test]
        public void FoodDropsFromWinsAndTheInnSellsIt()
        {
            var save = new SaveData();
            var got = Beasts.RollFood(save, new SeededRandom(1), true, 0);
            Assert.GreaterOrEqual(Inventory.Count(save, Beasts.FoodId), 1, "a boss fight always feeds");
            Assert.AreEqual(got.Where(s => s.Id == Beasts.FoodId).Sum(s => s.Count), Inventory.Count(save, Beasts.FoodId));
            int total = 0;
            var rng = new SeededRandom(9);
            for (int i = 0; i < 1000; i++) total += Beasts.RollFood(new SaveData(), rng, false, 0).Sum(s => s.Count);
            Assert.That(total, Is.InRange(280, 420), "about 35% of ordinary wins");
            Assert.IsTrue(Shop.Goods.Any(g => g.ItemId == Beasts.FoodId && g.FactionId == null && g.MinTier == 0));
            Assert.IsTrue(Shop.Goods.Any(g => g.ItemId == Beasts.FeastId));
            foreach (var id in new[] { Beasts.FoodId, Beasts.FeastId })
            {
                var item = EarlyGameContent.FindItem(id);
                Assert.IsNotNull(item);
                Assert.AreEqual(0, item.SellValue, "food cannot be sold");
                Assert.IsFalse(item.InPool, "never from the 뽑기");
            }
        }

        [Test]
        public void EveryBeastHasTwoUsableSkillsAndATutorial()
        {
            Assert.IsNotNull(Tutorial.All.FirstOrDefault(t => t.Id == "beast"));
            foreach (var beast in Beasts.All)
            {
                Assert.AreEqual(2, beast.Skills.Length, beast.Id);
                Assert.Greater(beast.Cooldown, 1);
                foreach (var sk in beast.Skills)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(Beasts.EffectText(sk, 0)), sk.Id);
                    var fresh = Battle(beast.Id, Beasts.MaxBond, Enemy("a"), Enemy("b"));
                    Assert.IsTrue(fresh.CanUseBeastSkill(sk), sk.Id);
                    var evs = fresh.CallBeast(PlayerCommand.CallBeast(sk, sk.NeedsEnemy ? fresh.Enemies[0] : null));
                    Assert.Greater(evs.Count, 0, sk.Id);
                }
            }
        }
    }
}
