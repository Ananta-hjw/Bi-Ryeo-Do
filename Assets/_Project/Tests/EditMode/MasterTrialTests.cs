using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class MasterTrialTests
    {
        private static SaveData Save(int cleared, int level = 40)
        {
            var save = new SaveData { ClearedStage = cleared, Level = level };
            Inventory.EnsureStartKit(save);
            foreach (var skill in SimpleBot.BestLoadout(cleared)) { save.LearnedArts.Add(skill.Id); save.EquippedArts.Add(skill.Id); }
            return save;
        }

        private static CombatStats Stats(int hp, int atk, int speed) { return new CombatStats { MaxHp = hp, MaxQi = 100, Attack = atk, Defense = 0, Speed = speed, CritRate = 0f, CritDamage = 1.5f }; }

        [Test]
        public void OpensAtStage31AndTiersOpenOneByOne()
        {
            Assert.IsFalse(MasterTrial.Unlocked(Save(MasterTrial.UnlockStage - 1)));
            var save = Save(MasterTrial.UnlockStage);
            Assert.IsFalse(MasterTrial.Start(save, 2, 1), "only tier 1 is open");
            Assert.IsTrue(MasterTrial.Start(save, 1, 1));
            Assert.IsFalse(MasterTrial.Start(save, 1, 2), "one run at a time");
            save.MasterTrialRun.Floor = MasterTrial.Floors;
            save.MasterTrialRun.Node = MasterTrial.Options(save)[0].Code;
            var notes = MasterTrial.Finish(save);
            Assert.AreEqual(1, save.MasterTrialBest);
            Assert.AreEqual(2, MasterTrial.OpenTier(save));
            Assert.IsTrue(notes[0].Contains("첫 돌파"));
            MasterTrial.Close(save);
            Assert.IsTrue(MasterTrial.Start(save, 2, 3));
        }

        [Test]
        public void ThePathStartsWithFightsAndEndsWithABoss()
        {
            var save = Save(40);
            MasterTrial.Start(save, 1, 77);
            var first = MasterTrial.Options(save);
            Assert.AreEqual(2, first.Count);
            Assert.IsTrue(first.All(n => n.Kind == TrialNodeKind.Fight));
            CollectionAssert.AreEqual(first.Select(n => n.Code).ToList(), MasterTrial.Options(save).Select(n => n.Code).ToList(), "the same run shows the same paths");
            for (int floor = 2; floor < MasterTrial.Floors; floor++)
            {
                save.MasterTrialRun.Floor = floor;
                var opts = MasterTrial.Options(save);
                Assert.That(opts.Count, Is.InRange(2, 3));
                Assert.IsTrue(opts.Any(n => n.Kind == TrialNodeKind.Fight), "a plain fight is always offered");
                Assert.AreEqual(opts.Count, opts.Select(n => n.Code).Distinct().Count() + opts.Count(n => n.Kind == TrialNodeKind.Fight) - opts.Where(n => n.Kind == TrialNodeKind.Fight).Select(n => n.Code).Distinct().Count());
            }
            save.MasterTrialRun.Floor = MasterTrial.Floors;
            var last = MasterTrial.Options(save);
            Assert.AreEqual(1, last.Count);
            Assert.AreEqual(TrialNodeKind.Boss, last[0].Kind);
            Assert.IsTrue(EarlyGameContent.FindStage(last[0].Stage).Encounters[last[0].Fight].Any(id => EarlyGameContent.FindEnemy(id).IsBoss));
        }

        [Test]
        public void EveryReferenceStageBuildsPlayableFightsAndBosses()
        {
            for (int cleared = MasterTrial.UnlockStage; cleared <= EarlyGameContent.Stages.Length; cleared++)
            {
                var save = Save(cleared);
                foreach (bool boss in new[] { false, true })
                {
                    var pool = MasterTrial.Candidates(save, boss);
                    Assert.Greater(pool.Count, 0, "stage " + cleared + (boss ? " boss" : ""));
                    foreach (var c in pool)
                    {
                        MasterTrial.Start(save, 10, 1);
                        save.MasterTrialRun.Floor = 7;
                        var node = new TrialNode { Kind = boss ? TrialNodeKind.Boss : TrialNodeKind.Elite, Stage = c[0], Fight = c[1] };
                        var st = MasterTrial.ToStage(save, node);
                        Assert.IsTrue(st.IsQuest && st.MasterTrialFloor == 7);
                        foreach (var id in st.Encounters[0])
                        {
                            var e = st.EnemyFor(id);
                            Assert.IsNotNull(e, id);
                            Assert.Greater(e.CreateCombatant(id).Stats.MaxHp, 0, id);
                        }
                        save.MasterTrialRun = new MasterTrialRun();
                    }
                }
            }
        }

        [Test]
        public void HigherTiersAndElitesMakeTougherEnemies()
        {
            var save = Save(45);
            var node = new TrialNode { Kind = TrialNodeKind.Fight, Stage = MasterTrial.Candidates(save, false)[0][0], Fight = MasterTrial.Candidates(save, false)[0][1] };
            MasterTrial.Start(save, 1, 1);
            var id = EarlyGameContent.FindStage(node.Stage).Encounters[node.Fight][0];
            int low = MasterTrial.ToStage(save, node).EnemyFor(id).CreateCombatant(id).Stats.MaxHp;
            node.Kind = TrialNodeKind.Elite;
            int elite = MasterTrial.ToStage(save, node).EnemyFor(id).CreateCombatant(id).Stats.MaxHp;
            save.MasterTrialRun.Tier = 10;
            node.Kind = TrialNodeKind.Fight;
            int high = MasterTrial.ToStage(save, node).EnemyFor(id).CreateCombatant(id).Stats.MaxHp;
            Assert.Greater(elite, low);
            Assert.Greater(high, low);
        }

        [Test]
        public void BlessingsChangeTheFreshPartyAndWoundsCarryOver()
        {
            var save = Save(40);
            MasterTrial.Start(save, 1, 5);
            var run = save.MasterTrialRun;
            var art = save.EquippedArts.First(a => EarlyGameContent.FindArt(a).Effect == SkillEffect.None);
            MasterTrial.AddBlessing(run, "atk_up");
            MasterTrial.AddBlessing(run, "atk_up");
            MasterTrial.AddBlessing(run, "art_hits|" + art);
            MasterTrial.AddBlessing(run, "start_spirit");
            Assert.AreEqual(2, MasterTrial.Stacks(run, "atk_up"));
            var plain = new List<Combatant> { Formation.BuildHero(save) };
            var hero = Formation.BuildHero(save);
            var party = new List<Combatant> { hero };
            MasterTrial.ApplyToParty(save, party);
            Assert.AreEqual((int)System.Math.Round(plain[0].Stats.Attack * 1.2f), hero.Stats.Attack);
            Assert.AreEqual(plain[0].Skills.First(s => s.Id == art).Hits + 1, hero.Skills.First(s => s.Id == art).Hits);
            Assert.AreEqual(plain[0].Skills.First(s => s.Id == art).Hits, Formation.BuildHero(save).Skills.First(s => s.Id == art).Hits, "the art itself is untouched");
            Assert.GreaterOrEqual(hero.StartSpirit, 25);

            hero.Hp = hero.Stats.MaxHp / 4;
            hero.Spirit = 60;
            MasterTrial.CompleteFight(save, party, new SeededRandom(1));
            Assert.IsTrue(run.Pending.StartsWith("pick"));
            Assert.AreEqual(3, run.Offer.Count);
            var next = new List<Combatant> { Formation.BuildHero(save) };
            MasterTrial.ApplyToParty(save, next);
            Assert.AreEqual(.25, next[0].Hp / (double)next[0].Stats.MaxHp, .01);
            Assert.AreEqual(60, next[0].Spirit);

            hero.Hp = 0;
            MasterTrial.CompleteFight(save, party, new SeededRandom(2));
            var revived = new List<Combatant> { Formation.BuildHero(save) };
            MasterTrial.ApplyToParty(save, revived);
            Assert.AreEqual(MasterTrial.FallenRevive, revived[0].Hp / (double)revived[0].Stats.MaxHp, .01, "a fallen member stands up at 10%");
        }

        [Test]
        public void PicksRestsAndEventsMoveTheRunOn()
        {
            var save = Save(40);
            save.MasterTrialBest = 2;
            Assert.IsTrue(MasterTrial.Start(save, 3, 9));
            var run = save.MasterTrialRun;
            MasterTrial.OfferPick(save, 1, new SeededRandom(4));
            string taken = run.Offer[0];
            Assert.IsTrue(MasterTrial.Pick(save, 0));
            Assert.AreEqual(2, run.Floor);
            Assert.AreEqual(1, run.Blessings.Count);
            Assert.AreEqual("", run.Pending);

            run.Party.Add(Inventory.Hero + ":300:200:10");
            MasterTrial.Choose(save, new TrialNode { Kind = TrialNodeKind.Rest, Code = "rest" });
            Assert.AreEqual("rest", run.Pending);
            MasterTrial.RestHealAll(save);
            Assert.AreEqual(3, run.Floor);
            StringAssert.StartsWith(Inventory.Hero + ":" + (300 + (int)System.Math.Round(MasterTrial.RestHeal(3) * 1000)), run.Party[0]);

            MasterTrial.Choose(save, new TrialNode { Kind = TrialNodeKind.Rest, Code = "rest" });
            string key = taken;
            Assert.IsTrue(MasterTrial.RestTrain(save, key));
            Assert.AreEqual(2, run.Blessings.Count == 1 ? int.Parse(run.Blessings[0].Substring(run.Blessings[0].LastIndexOf(':') + 1)) : -1);

            MasterTrial.Choose(save, new TrialNode { Kind = TrialNodeKind.Event, EventId = "wanderer", Code = "event:wanderer" });
            save.Doo = 0;
            Assert.IsNull(MasterTrial.ChooseEvent(save, 0, new SeededRandom(1)), "not enough 은자");
            save.Doo = MasterTrial.WandererPrice;
            Assert.IsNotNull(MasterTrial.ChooseEvent(save, 0, new SeededRandom(1)));
            Assert.AreEqual(0, save.Doo);
            Assert.IsTrue(run.Pending.StartsWith("pick"));
            MasterTrial.Pick(save, -1);

            MasterTrial.Choose(save, new TrialNode { Kind = TrialNodeKind.Event, EventId = "herbs", Code = "event:herbs" });
            MasterTrial.ChooseEvent(save, 1, new SeededRandom(1));
            Assert.AreEqual(2, Inventory.Count(save, Beasts.FoodId));
        }

        [Test]
        public void ARepeatedTierPaysHalfAndAFallEndsTheRun()
        {
            var save = Save(40);
            save.MasterTrialBest = 3;
            Assert.IsTrue(MasterTrial.Start(save, 3, 1));
            long before = save.Doo;
            MasterTrial.Finish(save);
            Assert.AreEqual(Income.Boost(MasterTrial.ClearDoo(3) / 2), save.Doo - before);
            Assert.AreEqual(3, save.MasterTrialBest);
            MasterTrial.Close(save);
            MasterTrial.Start(save, 2, 1);
            StringAssert.Contains("쓰러졌습니다", MasterTrial.Fail(save));
            Assert.IsFalse(save.MasterTrialRun.Active);
            Assert.AreEqual(2, save.MasterTrialRuns);
        }

        [Test]
        public void RunPerksWorkInBattle()
        {
            var hero = new Combatant(Inventory.Hero, "비류연", Team.Player, Stats(100000, 300, 100), null);
            var e = new Combatant("e", "e", Team.Enemy, Stats(100000, 100, 10), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("주먹", 1f) } };
            var b = new InteractiveBattle(hero, new[] { e }, new SeededRandom(1));
            b.Begin();
            int hp = e.Hp;
            b.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e));
            int plain = hp - e.Hp;

            var hero2 = new Combatant(Inventory.Hero, "비류연", Team.Player, Stats(100000, 300, 100), null) { DamageDealtMul = 1.5f, GuardStrikeBonus = 1f, MeditateSpiritBonus = 20, JointCostCut = 15 };
            var e2 = new Combatant("e", "e", Team.Enemy, Stats(100000, 100, 10), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("주먹", 1f) } };
            var b2 = new InteractiveBattle(hero2, new[] { e2 }, new SeededRandom(1));
            b2.Begin();
            hp = e2.Hp;
            b2.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e2));
            Assert.AreEqual((int)System.Math.Round(plain * 1.5), hp - e2.Hp, 1);
            b2.PlayerAct(PlayerCommand.Guard());
            Assert.IsTrue(hero2.GuardStrikeReady);
            hp = e2.Hp;
            b2.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e2));
            Assert.AreEqual((int)System.Math.Round(plain * 3.0), hp - e2.Hp, 2);
            Assert.IsFalse(hero2.GuardStrikeReady);
            int spirit = hero2.Spirit;
            b2.PlayerAct(PlayerCommand.Meditate());
            Assert.GreaterOrEqual(hero2.Spirit, System.Math.Min(100, spirit + 20));
            Assert.AreEqual(35, InteractiveBattle.JointCostFor(hero2));
        }

        [Test]
        public void EveryBlessingDescribesAndApplies()
        {
            var save = Save(50);
            save.ClearedStage = 50;
            Beasts.Sync(save);
            MasterTrial.Start(save, 1, 1);
            var art = save.EquippedArts.First(a => EarlyGameContent.FindArt(a).Effect == SkillEffect.None);
            foreach (var b in MasterTrial.Blessings)
            {
                string key = b.NeedsArt ? b.Id + "|" + art : b.Id;
                MasterTrial.AddBlessing(save.MasterTrialRun, key);
                Assert.IsFalse(string.IsNullOrEmpty(MasterTrial.Describe(key)), key);
            }
            var party = new List<Combatant> { Formation.BuildHero(save) };
            foreach (var c in EarlyGameContent.SelectCompanionsFor(50, save)) party.Add(CompanionGrowth.Create(save, c));
            MasterTrial.ApplyToParty(save, party);
            foreach (var m in party) Assert.Greater(m.Stats.MaxHp, 0);
            var beast = Beasts.LoadoutFor(save);
            int cd = beast.Cooldown;
            MasterTrial.ApplyToBeast(save, beast);
            Assert.AreEqual(System.Math.Max(1, cd - 1), beast.Cooldown);
            Assert.IsNotNull(Tutorial.All.FirstOrDefault(t => t.Id == "master_trial"));
        }

        [Test]
        public void TheFirstBossWinPaysFullExpAndAnAbandonedFightEndsTheRun()
        {
            var save = Save(40);
            MasterTrial.Start(save, 1, 3);
            var run = save.MasterTrialRun;
            run.Floor = MasterTrial.Floors;
            var boss = MasterTrial.Options(save)[0];
            var st = MasterTrial.Choose(save, boss);
            Assert.IsNotNull(st);
            MasterTrial.BeginFight(save);
            Assert.AreEqual("fight", run.Pending);
            Assert.IsNull(MasterTrial.Choose(save, boss), "no new path while the fight is on");
            long full = (long)System.Math.Round(Progression.ExpToNext(save.Level) * .20);
            MasterTrial.CompleteFight(save, new List<Combatant> { Formation.BuildHero(save) }, new SeededRandom(1));
            Assert.AreEqual(1, save.MasterTrialBest);
            Assert.AreEqual(full, MasterTrial.NodeExp(save, st), "the boss win raised the best tier but still pays a first clear");

            MasterTrial.Close(save);
            MasterTrial.Start(save, 1, 4);
            Assert.IsTrue(save.MasterTrialRun.Repeat);
            MasterTrial.Choose(save, MasterTrial.Options(save)[0]);
            MasterTrial.BeginFight(save);
            StringAssert.Contains("떠났습니다", MasterTrial.FailAbandoned(save));
            Assert.IsFalse(save.MasterTrialRun.Active);
        }

        [Test]
        public void BenchedMembersKeepTheirWoundsAndCostsNeverFellAnyone()
        {
            var save = Save(40);
            MasterTrial.Start(save, 1, 3);
            var run = save.MasterTrialRun;
            run.Party.Add("hyo_ryong:100:500:20");
            var hero = Formation.BuildHero(save);
            hero.Hp = hero.Stats.MaxHp / 2;
            MasterTrial.CompleteFight(save, new List<Combatant> { hero }, new SeededRandom(1));
            Assert.IsTrue(MasterTrial.PartyState(run).Any(m => m.Id == "hyo_ryong" && m.HpPermil == 100), "the benched companion keeps 10%");
            MasterTrial.Pick(save, -1);
            run.Party.Add("jang_hong:50:500:0");
            run.Pending = "event:test";
            MasterTrial.ChooseEvent(save, 0, new SeededRandom(1));
            foreach (var m in MasterTrial.PartyState(run)) Assert.GreaterOrEqual(m.HpPermil, 1, m.Id);
        }

        [Test]
        public void TheMasterBlowOffersRareOrBetterAndPathsDoNotRepeatAFight()
        {
            var save = Save(45);
            for (int seed = 1; seed <= 30; seed++)
            {
                MasterTrial.Start(save, 1, seed);
                var run = save.MasterTrialRun;
                run.Party.Add(Inventory.Hero + ":800:800:0");
                MasterTrial.Choose(save, new TrialNode { Kind = TrialNodeKind.Event, EventId = "test", Code = "event:test" });
                MasterTrial.ChooseEvent(save, 0, new SeededRandom(seed));
                Assert.IsTrue(run.Offer.All(o => MasterTrial.RarityOf(o) >= 1), string.Join(",", run.Offer));
                for (int floor = 1; floor <= MasterTrial.Floors; floor++)
                {
                    run.Floor = floor;
                    var fights = MasterTrial.Options(save).Where(n => n.Kind != TrialNodeKind.Rest && n.Kind != TrialNodeKind.Event).Select(n => n.Stage + ":" + n.Fight).ToList();
                    Assert.AreEqual(fights.Count, fights.Distinct().Count(), "seed " + seed + " floor " + floor);
                }
                MasterTrial.Fail(save); MasterTrial.Close(save);
            }
            Assert.AreEqual(0, MasterTrial.ClearStones(5, false), "a repeat of 5단계 gives no 심득석");
            Assert.AreEqual(2, MasterTrial.ClearStones(5, true));
        }

        [Test]
        public void GuardStrikeWaitsForAnAttackAndTakenDamageScalesDamageOverTime()
        {
            var hero = new Combatant(Inventory.Hero, "비류연", Team.Player, Stats(100000, 300, 100), null) { GuardStrikeBonus = 1f, CounterStance = true, GuardStrikeReady = true };
            var e = new Combatant("e", "e", Team.Enemy, Stats(100000, 100, 500), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("주먹", 1f) } };
            var b = new InteractiveBattle(hero, new[] { e }, new SeededRandom(1));
            b.Begin();
            Assert.IsTrue(hero.GuardStrikeReady, "the counter strikes did not spend it");

            var plain = new Combatant(Inventory.Hero, "a", Team.Player, Stats(100000, 300, 100), null);
            var tough = new Combatant(Inventory.Hero, "b", Team.Player, Stats(100000, 300, 100), null) { DamageTakenMul = .5f };
            foreach (var u in new[] { plain, tough })
            {
                u.AddStatus(StatusKind.Burn, 3, "e");
                var foe = new Combatant("x", "x", Team.Enemy, Stats(100000, 1, 1), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("주먹", .01f) } };
                var bb = new InteractiveBattle(u, new[] { foe }, new SeededRandom(1));
                bb.Begin();
            }
            Assert.Less(100000 - tough.Hp, 100000 - plain.Hp, "화상 is cut by 호신강기 too");
        }

        /// <summary>Informational only (not tuning): how often the balance bot clears a run at the stage's bot level, picking the first 가르침 and always resting.</summary>
        [Test, Explicit]
        public void BotRunReport()
        {
            foreach (int stage in new[] { 31, 40, 50, 66 })
                foreach (int tier in new[] { 1, 5, 10 })
                {
                    int cleared = 0, floors = 0, runs = 6;
                    for (int seed = 1; seed <= runs; seed++)
                    {
                        var ref0 = EarlyGameContent.FindStage(stage);
                        var save = BalanceSimulation.Starter(stage + 1, BalanceSimulation.BotLevel(ref0));
                        save.ClearedStage = stage;
                        save.MasterTrialBest = tier - 1;
                        MasterTrial.Start(save, tier, seed * 97);
                        var run = save.MasterTrialRun;
                        bool alive = true;
                        while (alive && run.Active)
                        {
                            var opts = MasterTrial.Options(save);
                            var node = opts.FirstOrDefault(n => n.Kind == TrialNodeKind.Rest) ?? opts[0];
                            var st = MasterTrial.Choose(save, node);
                            if (st == null) { if (run.Pending == "rest") MasterTrial.RestHealAll(save); else { MasterTrial.ChooseEvent(save, 1, new SeededRandom(seed)); if (run.Pending.StartsWith("pick")) MasterTrial.Pick(save, 0); } continue; }
                            var party = BalanceSimulation.Party(save, st);
                            MasterTrial.ApplyToParty(save, party);
                            var enemies = st.Encounters[0].Select((id, i) => st.EnemyFor(id).CreateCombatant(id + i)).ToList();
                            var battle = new InteractiveBattle(party, enemies, new SeededRandom(seed * 1000 + run.Floor));
                            battle.Begin();
                            for (int guard = 0; !battle.IsOver && guard < 3000; guard++) battle.PlayerAct(SimpleBot.Choose(battle));
                            if (!battle.PlayerWon) { alive = false; break; }
                            floors++;
                            MasterTrial.CompleteFight(save, party, new SeededRandom(seed));
                            if (run.Pending.StartsWith("pick")) MasterTrial.Pick(save, 0);
                        }
                        if (alive && run.Pending == "done") cleared++;
                    }
                    TestContext.Out.WriteLine("stage " + stage + " tier " + tier + ": cleared " + cleared + "/" + runs + ", fights won " + floors);
                }
        }
    }
}
