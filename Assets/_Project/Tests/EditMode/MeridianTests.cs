using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class MeridianTests
    {
        private static SaveData Rich(int cleared = 66)
        {
            var save = new SaveData { ClearedStage = cleared, Doo = 10000000, Level = 30 };
            Inventory.Add(save, InnerArts.StoneId, 100);
            return save;
        }

        private static void Complete(SaveData save, string id)
        {
            string error;
            for (int i = 0; i < Meridians.Points; i++) Assert.IsTrue(Meridians.Open(save, id, out error), id + ": " + error);
        }

        [Test]
        public void EightMeridiansOpenEveryEightStages()
        {
            Assert.AreEqual(8, Meridians.All.Length);
            for (int i = 0; i < Meridians.All.Length; i++) Assert.AreEqual(8 * (i + 1), Meridians.All[i].UnlockStage);
            var save = Rich(15);
            Assert.IsTrue(Meridians.IsOpen(save, Meridians.Find("im")));
            Assert.IsFalse(Meridians.IsOpen(save, Meridians.Find("dok")));
            string error;
            Assert.IsFalse(Meridians.Open(save, "dok", out error));
            StringAssert.Contains("스테이지 16", error);
        }

        [Test]
        public void PointsCostSilverAndTheLastAlsoInsightStones()
        {
            var im = Meridians.Find("im");
            var eumgyo = Meridians.Find("eumgyo");
            Assert.AreEqual(1500, Meridians.DooCost(im, 1));
            Assert.AreEqual(23400, Meridians.DooCost(eumgyo, 5));
            for (int p = 1; p < Meridians.Points; p++) Assert.AreEqual(0, Meridians.StoneCost(im, p));
            Assert.AreEqual(28, Meridians.All.Sum(m => Meridians.StoneCost(m, Meridians.Points)));

            var save = Rich();
            long before = save.Doo;
            int stones = InnerArts.Stones(save);
            Complete(save, "im");
            long spent = 0;
            for (int p = 1; p <= Meridians.Points; p++) spent += Meridians.DooCost(im, p);
            Assert.AreEqual(before - spent, save.Doo);
            Assert.AreEqual(stones - Meridians.StoneCost(im, Meridians.Points), InnerArts.Stones(save));
            Assert.IsTrue(Meridians.IsComplete(save, "im"));
            string error;
            Assert.IsFalse(Meridians.Open(save, "im", out error));
        }

        [Test]
        public void NothingIsSpentWhenShort()
        {
            var save = new SaveData { ClearedStage = 66, Doo = 100 };
            string error;
            Assert.IsFalse(Meridians.Open(save, "im", out error));
            Assert.AreEqual(100, save.Doo);
            Assert.AreEqual(0, Meridians.Level(save, "im"));

            save = Rich();
            for (int i = 0; i < 4; i++) Assert.IsTrue(Meridians.Open(save, "im", out error));
            save.Items.RemoveAll(s => s.Id == InnerArts.StoneId);
            long doo = save.Doo;
            Assert.IsFalse(Meridians.Open(save, "im", out error));
            StringAssert.Contains("심득석", error);
            Assert.AreEqual(doo, save.Doo);
            Assert.AreEqual(4, Meridians.Level(save, "im"));
        }

        [Test]
        public void StatsGrowPerPointAndWithTheSmallCircuit()
        {
            var save = Rich();
            var plain = InnerArts.HeroStatsBeforeRealm(save);
            string error;
            for (int i = 0; i < 4; i++) { Meridians.Open(save, "dok", out error); Meridians.Open(save, "chung", out error); }
            var grown = InnerArts.HeroStatsBeforeRealm(save);
            Assert.AreEqual(plain.MaxHp * (1 + 4 * Meridians.HpPerPoint), grown.MaxHp, plain.MaxHp * 0.01);
            Assert.AreEqual(plain.Attack * (1 + 4 * Meridians.AtkPerPoint), grown.Attack, plain.Attack * 0.01);

            Complete(save, "im");
            Meridians.Open(save, "dok", out error);
            Assert.IsTrue(Meridians.SmallCircuit(save));
            var circuit = InnerArts.HeroStatsBeforeRealm(save);
            Assert.AreEqual(plain.MaxHp * (1 + 4 * Meridians.HpPerPoint + Meridians.SmallCircuitBonus), circuit.MaxHp, plain.MaxHp * 0.01);
            Assert.AreEqual(plain.MaxQi * (1 + 4 * Meridians.QiPerPoint + Meridians.SmallCircuitBonus), circuit.MaxQi, plain.MaxQi * 0.01);
        }

        [Test]
        public void CompletionEffectsReachTheHeroCombatant()
        {
            var save = Rich();
            var plain = Formation.BuildHero(save);
            foreach (var m in Meridians.All) Complete(save, m.Id);
            Assert.IsTrue(Meridians.GrandCircuit(save));
            var hero = Formation.BuildHero(save);
            Assert.AreEqual(plain.QiRegenBonus + Meridians.QiRegenFinish, hero.QiRegenBonus);
            Assert.AreEqual(Meridians.MeditateHealFinish, hero.MeditateHealBonus, 1e-6);
            Assert.AreEqual(Meridians.GuardCutFinish, hero.GuardCut, 1e-6);
            Assert.AreEqual(Meridians.StartGaugeFinish, hero.StartGauge);
            Assert.AreEqual(Meridians.StatusTurnCutFinish, hero.StatusTurnCut);
            Assert.AreEqual(plain.StartSpirit + Meridians.StartSpiritFinish, hero.StartSpirit);
            Assert.AreEqual(plain.HealTakenBonus + Meridians.HealTakenFinish, hero.HealTakenBonus, 1e-6);
            Assert.AreEqual(Meridians.GrandCircuitUltimate, hero.UltimateDamageBonus, 1e-6);
            Assert.AreEqual(4 * Meridians.ResistPerPoint, hero.StatusResist, 1e-6);
            Assert.AreEqual(plain.SpiritDealtBonus + 4 * Meridians.SpiritPerPoint, hero.SpiritDealtBonus);
            Assert.AreEqual(plain.Stats.CritDamage + Meridians.CritDamageFinish, hero.Stats.CritDamage, 1e-4);
            Assert.AreEqual(3, Achievements.All.Count(a => (a.Metric == AchMetric.MeridiansComplete || a.Metric == AchMetric.SmallCircuit) && Achievements.IsDone(save, a)));
        }

        [Test]
        public void AnUntouchedSaveIsUnchanged()
        {
            var save = Rich();
            var hero = Formation.BuildHero(save);
            Assert.AreEqual(0f, hero.GuardCut);
            Assert.AreEqual(0, hero.StartGauge);
            Assert.AreEqual(0f, hero.StatusResist);
            Assert.AreEqual(0f, hero.UltimateDamageBonus);
            Assert.AreEqual(0, Meridians.SummaryLines(save).Count);
        }

        private static CombatStats S(int hp, int atk, int spd) { return new CombatStats { MaxHp = hp, MaxQi = 100, Attack = atk, Speed = spd, CritDamage = 1.5f }; }

        [Test]
        public void TheGirdleMeridianCutsTheGuardFactor()
        {
            var hero = new Combatant(Inventory.Hero, "h", Team.Player, S(1000, 10, 100), null) { Guarding = true };
            var e = new Combatant("e", "e", Team.Enemy, S(1000, 10, 100), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("x", 1f) } };
            var b = new InteractiveBattle(hero, new[] { e }, new SeededRandom(1));
            Assert.AreEqual(InteractiveBattle.GuardDamageFactor, b.GuardFactorFor(hero), 1e-6);
            hero.GuardCut = Meridians.GuardCutFinish;
            Assert.AreEqual(0.35f, b.GuardFactorFor(hero), 1e-6);
        }

        [Test]
        public void AStartGaugeLetsTheHeroActFirst()
        {
            var hero = new Combatant(Inventory.Hero, "h", Team.Player, S(100000, 10, 100), null);
            var e = new Combatant("e", "e", Team.Enemy, S(100000, 10, 110), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("x", 1f) } };
            var events = new InteractiveBattle(hero, new[] { e }, new SeededRandom(1)).Begin();
            Assert.IsTrue(events.Any(x => x.ActorId == "e"), "the faster enemy moves first without the bonus");

            hero = new Combatant(Inventory.Hero, "h", Team.Player, S(100000, 10, 100), null) { StartGauge = Meridians.StartGaugeFinish };
            e = new Combatant("e", "e", Team.Enemy, S(100000, 10, 110), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("x", 1f) } };
            events = new InteractiveBattle(hero, new[] { e }, new SeededRandom(1)).Begin();
            Assert.IsFalse(events.Any(x => x.ActorId == "e"), "양유맥: the hero is first");
        }

        /// <summary>Meditates until the enemy's sealing blow lands; returns its turns, or -1 when it never took hold.</summary>
        private static int SealTurnsLanded(float resist, int cut)
        {
            var hero = new Combatant(Inventory.Hero, "h", Team.Player, S(100000000, 10, 100), null) { StatusResist = resist, StatusTurnCut = cut };
            var move = EnemyMove.Attack("x", .1f);
            move.Inflict = StatusKind.Seal;
            var e = new Combatant("e", "e", Team.Enemy, S(100000000, 10, 100), null) { Moves = new List<EnemyMove> { move } };
            var b = new InteractiveBattle(hero, new[] { e }, new SeededRandom(3));
            b.Begin();
            for (int i = 0; i < 60; i++)
            {
                b.PlayerAct(PlayerCommand.Meditate());
                if (hero.Statuses.Count > 0) return hero.Statuses[0].Turns;
            }
            return -1;
        }

        [Test]
        public void TheYinLinkingMeridianResistsAndShortensStatuses()
        {
            int full = SealTurnsLanded(0f, 0);
            Assert.AreEqual(Statuses.DefaultTurns(StatusKind.Seal), full);
            Assert.AreEqual(full - 1, SealTurnsLanded(0f, 1));
            Assert.AreEqual(-1, SealTurnsLanded(1f, 0), "full resistance: nothing takes hold");
        }

        [Test]
        public void NewlyOpenedMeridiansShowARedDotUntilSeen()
        {
            var save = new SaveData { ClearedStage = 17 };
            Assert.AreEqual(2, Meridians.NewlyOpened(save).Count);
            int before = Discovery.Count(save);
            Meridians.MarkSeen(save);
            Assert.AreEqual(0, Meridians.NewlyOpened(save).Count);
            Assert.AreEqual(before - 2, Discovery.Count(save));
            Assert.IsNotNull(Tutorial.Find("meridian"));
        }
    }
}
