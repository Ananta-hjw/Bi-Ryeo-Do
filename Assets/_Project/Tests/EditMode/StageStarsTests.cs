using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StageStarsTests
    {
        [Test]
        public void EveryFightHasAFastLimit()
        {
            foreach (var st in EarlyGameContent.Stages)
                for (int f = 0; f < st.Encounters.Length; f++) Assert.Greater(StageStars.FastLimit(st.Number, f), 1, st.Name + " " + f);
            Assert.AreEqual(st5().Encounters.Length * 3, StageStars.MaxStarsOfStage(5));
        }

        private static StageData st5() { return EarlyGameContent.FindStage(5); }

        [Test]
        public void EvaluateAwardsEachStarSeparately()
        {
            int limit = StageStars.FastLimit(3, 1);
            Assert.AreEqual(7, StageStars.Evaluate(3, 1, limit, false));
            Assert.AreEqual(1 | 4, StageStars.Evaluate(3, 1, limit, true));
            Assert.AreEqual(1 | 2, StageStars.Evaluate(3, 1, limit + 1, false));
            Assert.AreEqual(1, StageStars.Evaluate(3, 1, limit + 5, true));
            Assert.AreEqual(3, StageStars.Count(7));
            Assert.AreEqual(2, StageStars.Count(5));
        }

        [Test]
        public void EachFightKeepsItsOwnBestPerDifficulty()
        {
            var save = new SaveData();
            Assert.AreEqual(2, StageStars.Record(save, 4, 0, 0, 1 | 2));
            Assert.AreEqual(1, StageStars.Record(save, 4, 0, 0, 1 | 4));   // adds only the fast star
            Assert.AreEqual(0, StageStars.Record(save, 4, 0, 0, 1));
            Assert.AreEqual(3, StageStars.StarsOf(save, 4, 0, 0));
            Assert.AreEqual(0, StageStars.StarsOf(save, 4, 1, 0));          // another fight of the same stage
            Assert.AreEqual(0, StageStars.StarsOf(save, 4, 0, 1));          // another difficulty
            StageStars.Record(save, 4, 1, 0, 7);
            StageStars.Record(save, 4, 0, 1, 7);
            Assert.AreEqual(9, StageStars.Total(save));
            Assert.AreEqual(6, StageStars.StarsOfStage(save, 4, 0));
            Assert.AreEqual(3, StageStars.StarsOfStage(save, 4, 1));
        }

        [Test]
        public void HarderDifficultiesOpenAfterTheEntryStageThenStageByStage()
        {
            var save = new SaveData();
            int Last(int stage, int diff) { return StageStars.FightCount(stage, diff) - 1; }
            void Clear(int stage, int diff) { StageStars.Record(save, stage, Last(stage, diff), diff, 1); }
            for (int n = 1; n < StageStars.OpenStage(1); n++) Clear(n, 0);
            Assert.IsFalse(StageStars.Unlocked(save, 1, 1));     // 일반 1-29 is not enough
            StringAssert.Contains("일반 30", StageStars.LockLabel(save, 1, 1));
            StageStars.Record(save, StageStars.OpenStage(1), 0, 0, 1);   // only the first fight of 일반 30
            Assert.IsFalse(StageStars.Unlocked(save, 1, 1));
            Clear(StageStars.OpenStage(1), 0);
            Assert.IsTrue(StageStars.Unlocked(save, 1, 1));      // 일반 30 cleared: 어려움 opens
            Assert.IsFalse(StageStars.Unlocked(save, 2, 1));     // 어려움 1 must be cleared first
            Clear(1, 1);
            Assert.IsTrue(StageStars.Unlocked(save, 2, 1));
            for (int n = 2; n <= StageStars.OpenStage(1); n++) Clear(n, 1);
            Assert.IsFalse(StageStars.Unlocked(save, 31, 1));    // 어려움 31 still needs 일반 31
            Clear(31, 0);
            Assert.IsTrue(StageStars.Unlocked(save, 31, 1));
        }

        [Test]
        public void ExtremeOpensAfterHardFortyThenNeedsTheSameStageOnHard()
        {
            var save = new SaveData();
            int Last(int stage, int diff) { return StageStars.FightCount(stage, diff) - 1; }
            void Clear(int stage, int diff) { StageStars.Record(save, stage, Last(stage, diff), diff, 1); }
            for (int n = 1; n < StageStars.OpenStage(2); n++) { Clear(n, 0); Clear(n, 1); }
            Clear(StageStars.OpenStage(2), 0);
            Assert.IsFalse(StageStars.Unlocked(save, 1, 2));     // 어려움 40 not cleared yet
            StringAssert.Contains("어려움 40", StageStars.LockLabel(save, 1, 2));
            Clear(StageStars.OpenStage(2), 1);
            Assert.IsTrue(StageStars.Unlocked(save, 1, 2));
            Assert.IsFalse(StageStars.Unlocked(save, 2, 2));     // 극악 1 not cleared yet
            Clear(1, 2);
            Assert.IsTrue(StageStars.Unlocked(save, 2, 2));
        }

        [Test]
        public void SyncCreditsEveryFightOfClearedStagesOnNormal()
        {
            var save = new SaveData { ClearedStage = 20, Level = 60 };
            StageStars.Sync(save);
            int fights = Enumerable.Range(1, 20).Sum(n => StageStars.FightCount(n));
            Assert.AreEqual(fights + 1, StageStars.Total(save)); // the retired stage-19 filler keeps its legacy normal star
            Assert.IsFalse(StageStars.Unlocked(save, 1, 1));     // 어려움 1 opens with 일반 30, not 20
            StageStars.Sync(save);
            Assert.AreEqual(fights + 1, StageStars.Total(save)); // the retired stage-19 filler keeps its legacy normal star
        }

        [Test]
        public void HarderDifficultiesScaleEnemiesMoreInLateStages()
        {
            var e = EarlyGameContent.FindEnemy("tiger");
            Assert.AreEqual(e.HpMul * StageStars.NormalHpBoost(1) * StageStars.NormalHpTrim(1), StageStars.ScaleEnemy(e, 1, 0).HpMul, 0.001f, "일반 gets the HP boost and trim of its stage");
            Assert.AreEqual(e.HpMul * StageStars.NormalHpBoost(1), StageStars.ScaleEnemy(e, 1, 0, survive: true).HpMul, 0.001f, "a 버티기 fight keeps full HP");
            Assert.Greater(StageStars.NormalHpBoost(30), StageStars.NormalHpBoost(10), "the boost grows with the stage");
            Assert.Greater(StageStars.NormalHpBoost(10), StageStars.NormalHpBoost(1));
            var survival = EarlyGameContent.FindEnemy("geomhu_ioksang");
            Assert.AreSame(survival, StageStars.ScaleEnemy(survival, 29, 0), "survival-only enemies keep their stats");
            var normal20 = StageStars.ScaleEnemy(e, 20, 0);
            var hard = StageStars.ScaleEnemy(e, 20, 1);
            var hell = StageStars.ScaleEnemy(e, 20, 2);
            Assert.Greater(hard.BuildStats().MaxHp, normal20.BuildStats().MaxHp);
            Assert.Greater(hell.BuildStats().MaxHp, hard.BuildStats().MaxHp);
            Assert.Greater(hell.BuildStats().EffAttack, hard.BuildStats().EffAttack);
            Assert.AreEqual(e.Id, hard.Id);
            Assert.AreEqual(normal20.HpMul * StageStars.HardHpFix(20, 1), hard.HpMul, 0.001f, "어려움 = the 일반 enemy, raised, with the length fix");
            Assert.AreEqual(normal20.AtkMul * StageStars.HardAtkFix(20, 1), hard.AtkMul, 0.001f);
            Assert.AreEqual(1, StageStars.DropRolls(0));
            Assert.AreEqual(3, StageStars.DropRolls(2));
        }

        [Test]
        public void StageDifficultyCopyKeepsTheStage()
        {
            var st = EarlyGameContent.FindStage(5);
            var hard = st.WithDifficulty(1);
            Assert.AreEqual(1, hard.Difficulty);
            Assert.AreEqual(0, st.Difficulty);
            Assert.AreEqual(st.Name, hard.Name);
            Assert.AreEqual(StageStars.DifficultyCount - 1, st.WithDifficulty(9).Difficulty);
        }

        [Test]
        public void ChestsOpenByTotalStarsAndOnlyOnce()
        {
            var save = new SaveData();
            Assert.AreEqual(0, StageStars.ClaimableCount(save));
            for (int s = 1; s <= 5; s++) StageStars.Record(save, s, 0, 0, 7);   // 15 stars
            Assert.AreEqual(1, StageStars.ClaimableCount(save));
            var note = StageStars.ClaimAll(save);
            Assert.IsNotNull(note);
            Assert.AreEqual(4680, save.Doo);
            Assert.IsNull(StageStars.ClaimAll(save));
            for (int s = 6; s <= 13; s++) StageStars.Record(save, s, 0, 0, 7);  // 39 stars (one short of the next chest)
            StageStars.Record(save, 14, 0, 0, 1);                                  // 40
            StageStars.ClaimAll(save);
            Assert.AreEqual(1, Inventory.Count(save, "ginseng"));
            Assert.AreEqual(2, save.ClaimedStarChests.Count);
            foreach (var c in StageStars.Chests) if (c.ItemId != null) Assert.IsNotNull(EarlyGameContent.FindItem(c.ItemId));
            CollectionAssert.AreEqual(StageStars.Chests.OrderBy(c => c.Stars).Select(c => c.Stars), StageStars.Chests.Select(c => c.Stars));
        }

        [Test]
        public void ResumePointKeepsTheDifficulty()
        {
            var save = new SaveData();
            var party = new[] { new Combatant("player", "비류연", Team.Player, new CombatStats { MaxHp = 10, MaxQi = 1, Attack = 1, Defense = 1, Speed = 1 }, null) };
            StageProgress.Store(save, 5, 1, party, 2);
            Assert.AreEqual(2, save.ProgressDifficulty);
            StageProgress.Clear(save);
            Assert.AreEqual(0, save.ProgressDifficulty);
        }

        [Test]
        public void TheFastLimitGrowsWithDifficulty()
        {
            int normal = StageStars.FastLimit(20, 1, 0), hard = StageStars.FastLimit(20, 1, 1), hell = StageStars.FastLimit(20, 1, 2);
            Assert.AreEqual(StageStars.FastLimit(20, 1), normal);
            Assert.Greater(hard, normal);
            Assert.Greater(hell, hard);
            Assert.AreEqual(7, StageStars.Evaluate(20, 1, hell, false, 2));
            Assert.AreEqual(3, StageStars.Evaluate(20, 1, hell, false, 0));   // too slow for 일반
        }

        [Test]
        public void ASingleChestCanBeClaimedOnItsOwn()
        {
            var save = new SaveData();
            for (int s = 1; s <= 14; s++) StageStars.Record(save, s, 0, 0, 7);   // 42 stars: the first two chests
            Assert.AreEqual(2, StageStars.ClaimableCount(save));
            Assert.IsNotNull(StageStars.Claim(save, StageStars.Chests[0]));
            Assert.AreEqual(1, StageStars.ClaimableCount(save));
            Assert.IsNull(StageStars.Claim(save, StageStars.Chests[0]));
            Assert.AreEqual(4680, save.Doo);
        }

        [Test]
        public void HarderDifficultiesStartAtTheEntryStageAndClimbAtThreeQuartersOfTheNormalSlope()
        {
            int entry = StageStars.EntryStage;
            Assert.AreEqual(StageStars.NormalBotLevel(entry), StageStars.TargetLevel(1, 1), 1e-9);   // 어려움 1 ≈ 일반 40
            Assert.AreEqual(StageStars.TargetLevel(entry, 1), StageStars.TargetLevel(1, 2), 1e-9);  // 극악 1 ≈ 어려움 40
            Assert.AreEqual(0, StageStars.LevelAdd(5, 0));                                          // 일반 is untouched
            var tiger = EarlyGameContent.FindEnemy("tiger");
            Assert.AreEqual(tiger.Level + StageStars.LevelAdd(1, 1), StageStars.ScaleEnemy(tiger, 1, 1).Level);
            int last = EarlyGameContent.Stages.Length;
            for (int n = 2; n <= last; n++)
            {
                double normalRise = StageStars.NormalBotLevel(n) - StageStars.NormalBotLevel(1);
                Assert.AreEqual(StageStars.TargetLevel(1, 1) + StageStars.HardSlope * normalRise, StageStars.TargetLevel(n, 1), 1e-9, "stage " + n);
                Assert.Greater(StageStars.TargetLevel(n, 2), StageStars.TargetLevel(n, 1), "stage " + n);
                Assert.Greater(StageStars.TargetLevel(n, 1), StageStars.TargetLevel(n, 0), "stage " + n);
                Assert.GreaterOrEqual(StageStars.TargetLevel(n, 1), StageStars.TargetLevel(n - 1, 1), "stage " + n);
                Assert.Greater(StageStars.LevelAdd(n, 2), StageStars.LevelAdd(n, 1), "stage " + n);
            }
            Assert.AreEqual(EarlyGameContent.FindStage(7).RecommendedPlayerLevel, StageStars.RecommendedPlayerLevel(7, 0));
            Assert.Greater(StageStars.RecommendedPlayerLevel(1, 1), EarlyGameContent.FindStage(10).RecommendedPlayerLevel);   // 어려움 1 asks for a 일반 40 player
            Assert.Greater(StageStars.RecommendedPlayerLevel(1, 2), StageStars.RecommendedPlayerLevel(1, 1));
        }

        [Test]
        public void LevelAloneNoLongerOpensAHarderDifficulty()
        {
            var save = new SaveData { Level = 99 };
            StageStars.Record(save, 1, StageStars.FightCount(1) - 1, 0, 1);
            Assert.IsFalse(StageStars.Unlocked(save, 1, 1));    // 일반 1 and a high level are not enough: 일반 20 must be cleared
            Assert.AreEqual("", StageStars.LockLabel(save, 1, 0));
        }
    }
}
