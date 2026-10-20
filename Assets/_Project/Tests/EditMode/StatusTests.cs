using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StatusTests
    {
        private static CombatStats Stats(int hp, int atk, int speed) { return new CombatStats { MaxHp = hp, MaxQi = 100, Attack = atk, Defense = 0, Speed = speed, CritRate = 0f, CritDamage = 1.5f }; }

        private static Combatant Enemy(string id, CombatStats s, params EnemyMove[] moves)
        {
            var c = new Combatant(id, id, Team.Enemy, s, null);
            c.Moves = new List<EnemyMove>(moves.Length > 0 ? moves : new[] { EnemyMove.Plain });
            return c;
        }

        [Test]
        public void EnemyMovesTakeTheirStatusFromTheName()
        {
            Assert.AreEqual(StatusKind.Poison, EnemyMove.Attack("독침", 1f).Inflict);
            Assert.AreEqual(StatusKind.Chill, EnemyMove.Attack("백빙신침", 1f).Inflict);
            Assert.AreEqual(StatusKind.Burn, EnemyMove.Attack("화령염천도세", 1f).Inflict);
            Assert.AreEqual(StatusKind.None, EnemyMove.Attack("화살", 1f).Inflict, "화살 is not fire");
            Assert.IsTrue(EarlyGameContent.Enemies.Any(e => e.Moves.Any(m => m.Inflict == StatusKind.Bleed)));
        }

        [Test]
        public void PoisonTicksAtTheUnitsTurnAndWearsOff()
        {
            var poison = new SkillSpec { Id = "p", Name = "p", Power = 0.01f, Inflict = StatusKind.Poison, InflictChance = 1f };
            var p = new Combatant("p", "비류연", Team.Player, Stats(1000, 1, 100), new[] { poison });
            var e = Enemy("e", Stats(10000, 1, 100), EnemyMove.Attack("x", 0.01f));
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1));
            b.Begin();
            var ev = b.PlayerAct(PlayerCommand.UseSkill(poison, e));
            Assert.IsTrue(ev.Any(x => x.Kind == EventKind.Status && x.Status == StatusKind.Poison && x.Damage == 0), "applied");
            var tick = ev.First(x => x.Kind == EventKind.Status && x.Damage > 0);
            Assert.AreEqual(Statuses.DotDamage(e, StatusKind.Poison), tick.Damage);
            Assert.AreEqual(500, tick.Damage, "5% of max HP");
            for (int i = 0; i < 4; i++) b.PlayerAct(PlayerCommand.Guard());
            Assert.IsFalse(e.Has(StatusKind.Poison), "lasts three of its own turns");
        }

        [Test]
        public void BossesTakeLessFromDamageOverTime()
        {
            var e = Enemy("e", Stats(10000, 1, 100));
            var boss = Enemy("b", Stats(10000, 1, 100)); boss.IsBoss = true; boss.Rank = EnemyRank.Boss;
            Assert.Less(Statuses.DotDamage(boss, StatusKind.Burn), Statuses.DotDamage(e, StatusKind.Burn));
        }

        [Test]
        public void BleedUsesFractionalPercentInDamageAndHelp()
        {
            var e = Enemy("e", Stats(100000, 1, 100));
            Assert.AreEqual(2500, Statuses.DotDamage(e, StatusKind.Bleed));
            e.Rank = EnemyRank.Elite;
            Assert.AreEqual(1500, Statuses.DotDamage(e, StatusKind.Bleed));
            e.Rank = EnemyRank.Boss;
            e.IsBoss = true;
            Assert.AreEqual(875, Statuses.DotDamage(e, StatusKind.Bleed));
            StringAssert.Contains("2.5%", Statuses.Describe(StatusKind.Bleed), "help must not round the percentage to an integer");
        }

        [Test]
        public void ChargedBlowsLeaveInnerWoundUnlessGuardedAndItStopsQi()
        {
            var p = new Combatant("p", "비류연", Team.Player, Stats(100000, 1, 100), null);
            var e = Enemy("e", Stats(100000, 1, 100), EnemyMove.Wind("w"), EnemyMove.Attack("h", 0.5f));
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1));
            b.Begin();
            b.PlayerAct(PlayerCommand.Guard());   // the wind-up comes, then the guarded blow
            for (int i = 0; i < 3 && !p.Has(StatusKind.InnerWound); i++) b.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e));
            Assert.IsTrue(p.Has(StatusKind.InnerWound), "an unguarded charged blow leaves 내상");
            p.Qi = 0;
            b.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), e));
            Assert.AreEqual(0, p.Qi, "no 내공 comes back while wounded");
        }

        [Test]
        public void GuardBlocksTheStatusOfAMove()
        {
            var p = new Combatant("p", "비류연", Team.Player, Stats(100000, 1, 100), null);
            var e = Enemy("e", Stats(100000, 1, 100), EnemyMove.Attack("독침", 0.1f));
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1));
            b.Begin();
            for (int i = 0; i < 20; i++) b.PlayerAct(PlayerCommand.Guard());
            Assert.IsFalse(p.Has(StatusKind.Poison));
        }

        [Test]
        public void MeditateCuresOneAndTheAntidoteCuresAll()
        {
            var p = new Combatant("p", "비류연", Team.Player, Stats(100000, 1, 100), null);
            var e = Enemy("e", Stats(100000, 1, 1));
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1));
            b.Begin();
            p.AddStatus(StatusKind.Burn, 9);
            b.PlayerAct(PlayerCommand.Meditate());
            Assert.AreEqual(0, p.Statuses.Count, "운기 cures the status");
            p.AddStatus(StatusKind.Poison, 9);
            b.PlayerAct(PlayerCommand.UseItem(EarlyGameContent.FindItem("antidote")));
            Assert.AreEqual(0, p.Statuses.Count);
        }

        [Test]
        public void YunJunhosCleanseClearsTheChosenAlly()
        {
            var yun = EarlyGameContent.FindCompanion("yun_junho").CreateCombatant(15);
            var cleanse = yun.Skills.First(s => s.Effect == SkillEffect.Cleanse);
            var p = new Combatant("p", "비류연", Team.Player, Stats(100000, 1, 1), null);
            var e = Enemy("e", Stats(100000, 1, 1));
            var b = new InteractiveBattle(new[] { p, yun }, new[] { e }, new SeededRandom(1));
            b.Begin();
            while (b.Current != yun) b.PlayerAct(PlayerCommand.Guard());
            p.AddStatus(StatusKind.Bleed, 3);
            b.PlayerAct(PlayerCommand.UseSkill(cleanse, null).WithAlly(p));
            Assert.AreEqual(0, p.Statuses.Count);
        }

        [Test]
        public void YunJunhoLearnsTheCleanseAtLevelFifteen()
        {
            var yun = EarlyGameContent.FindCompanion("yun_junho");
            Assert.IsFalse(yun.CreateCombatant(14).Skills.Any(s => s.Effect == SkillEffect.Cleanse), "not yet at level 14");
            Assert.IsTrue(yun.CreateCombatant(15).Skills.Any(s => s.Effect == SkillEffect.Cleanse), "learned at level 15");
            Assert.IsTrue(yun.CreateCombatant(14).Skills.Any(s => s.Effect == SkillEffect.HealAlly), "the other arts are known from joining");
            CollectionAssert.AreEqual(new[] { "윤준호  자하청심결 습득!" }, CompanionGrowth.NewArtLines(yun, 14, 15));
            CollectionAssert.AreEqual(new[] { "윤준호  자하청심결 습득!" }, CompanionGrowth.NewArtLines(yun, 10, 20), "a jump over the level still learns it");
            Assert.IsEmpty(CompanionGrowth.NewArtLines(yun, 15, 20));
            CollectionAssert.AreEqual(new[] { "자하청심결 (Lv.15)" }, CompanionGrowth.PendingArtLines(yun, 10));
            Assert.IsEmpty(CompanionGrowth.PendingArtLines(yun, 15));
        }

        [Test]
        public void ABenchedCompanionWhoLevelsPastFifteenLearnsItToo()
        {
            var save = new SaveData { Level = 20, ClearedStage = 30 };
            CompanionGrowth.EnsureRecords(save);
            var p = save.CompanionLevels.First(x => x.Id == "yun_junho");
            p.Level = 14; p.Exp = 0;
            var lines = CompanionGrowth.ShareWithBench(save, new List<string>(), Progression.ExpToNext(14) * 4);
            Assert.IsTrue(lines.Any(l => l.Contains("자하청심결 습득!")), string.Join("|", lines));
        }

        [Test]
        public void ChillSlowsTheActionGauge()
        {
            var p = new Combatant("p", "비류연", Team.Player, Stats(100000, 1, 100), null);
            var e = Enemy("e", Stats(100000, 1, 100));
            Assert.AreEqual(1f, Statuses.SpeedFactor(e));
            e.AddStatus(StatusKind.Chill, 2);
            Assert.Less(Statuses.SpeedFactor(e), 1f);
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1));
            b.Begin();
            Assert.AreSame(p, b.Current);
            Assert.AreEqual(700, e.Gauge, "while chilled it gains 70 per tick, versus the player's 100");
        }

        [Test]
        public void OnlyOneStatusAtATimeAndItIsNotLengthened()
        {
            var p = new Combatant("p", "비류연", Team.Player, Stats(1000, 1, 100), null);
            Assert.IsTrue(p.AddStatus(StatusKind.Burn, 2));
            Assert.IsFalse(p.AddStatus(StatusKind.Poison, 3), "nothing else while burning");
            Assert.IsFalse(p.AddStatus(StatusKind.Burn, 5), "not lengthened either");
            Assert.AreEqual(1, p.Statuses.Count);
            Assert.AreEqual(2, p.Statuses[0].Turns);
            p.RemoveStatus(StatusKind.Burn);
            Assert.IsTrue(p.AddStatus(StatusKind.Poison, 3), "free again once it is gone");
        }

        [TestCase(false, false, 1)]
        [TestCase(false, false, 2)]
        [TestCase(false, true, 2)]
        [TestCase(true, false, 2)]
        public void PreviewOrderMatchesActualTurnsAfterChillExpires(bool chillPlayer, bool boss, int turns)
        {
            var p = new Combatant("p", "비류연", Team.Player, Stats(100000, 1, 100), null);
            var e = Enemy("e", Stats(100000, 1, 95));
            e.IsBoss = boss;
            var chilled = chillPlayer ? p : e;
            chilled.AddStatus(StatusKind.Chill, turns);
            var b = new InteractiveBattle(p, new[] { e }, new SeededRandom(1));
            b.Begin();
            int remaining = chilled.Statuses[0].Turns;
            int playerGauge = p.Gauge, enemyGauge = e.Gauge, turn = b.Turn;
            var predicted = b.PreviewOrder(12).Select(u => u.Id).ToList();
            CollectionAssert.AreEqual(predicted, b.PreviewOrder(12).Select(u => u.Id));
            Assert.AreEqual(remaining, chilled.Statuses[0].Turns, "preview must not tick live statuses");
            Assert.AreEqual(playerGauge, p.Gauge);
            Assert.AreEqual(enemyGauge, e.Gauge);
            Assert.AreEqual(turn, b.Turn);

            var actual = new List<string>();
            while (actual.Count < predicted.Count)
            {
                actual.Add(b.Current.Id);
                var events = b.PlayerAct(PlayerCommand.Guard());
                actual.AddRange(events.Where(x => x.ActorId == e.Id && x.Kind == EventKind.Attack).Select(x => x.ActorId));
            }
            Assert.IsFalse(chilled.Has(StatusKind.Chill), "comparison extends past expiry");
            CollectionAssert.AreEqual(predicted, actual.Take(predicted.Count));
        }

        [Test]
        public void StatusesEndWithTheFight()
        {
            var p = new Combatant("p", "비류연", Team.Player, Stats(1000, 1, 100), null);
            p.AddStatus(StatusKind.Poison, 3);
            p.RestBetweenFights();
            Assert.AreEqual(0, p.Statuses.Count);
        }
    }
}
