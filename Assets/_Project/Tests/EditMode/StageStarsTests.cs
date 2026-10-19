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
        public void HarderDifficultiesOpenStageByStage()
        {
            var save = new SaveData { Level = 60 };
            int Last(int stage) { return StageStars.FightCount(stage) - 1; }
            Assert.IsTrue(StageStars.Unlocked(save, 5, 0));
            Assert.IsFalse(StageStars.Unlocked(save, 1, 1));
            StageStars.Record(save, 1, 0, 0, 1);                 // only the first fight of stage 1 so far
            Assert.IsFalse(StageStars.Unlocked(save, 1, 1));
            StageStars.Record(save, 1, Last(1), 0, 1);
            Assert.IsTrue(StageStars.Unlocked(save, 1, 1));      // 어려움 1 needs only 일반 1

            StageStars.Record(save, 2, Last(2), 0, 1);
            Assert.IsFalse(StageStars.Unlocked(save, 2, 1));     // 일반 2 is not enough: 어려움 1 must be cleared too
            StageStars.Record(save, 1, Last(1), 1, 1);
            Assert.IsTrue(StageStars.Unlocked(save, 2, 1));
            Assert.IsFalse(StageStars.Unlocked(save, 3, 1));     // per stage: 일반 3 not cleared
        }

        [Test]
        public void ExtremeNeedsTheSameStageOnHardAndThePreviousStageOnExtreme()
        {
            var save = new SaveData { Level = 60 };
            int Last(int stage) { return StageStars.FightCount(stage) - 1; }
            foreach (int st in new[] { 1, 2 }) { StageStars.Record(save, st, Last(st), 0, 1); StageStars.Record(save, st, Last(st), 1, 1); }
            Assert.IsTrue(StageStars.Unlocked(save, 1, 2));
            Assert.IsFalse(StageStars.Unlocked(save, 2, 2));     // 극악 1 not cleared yet
            StageStars.Record(save, 1, Last(1), 2, 1);
            Assert.IsTrue(StageStars.Unlocked(save, 2, 2));
            Assert.IsFalse(StageStars.Unlocked(save, 3, 2));
        }

        [Test]
        public void SyncCreditsEveryFightOfClearedStagesOnNormal()
        {
            var save = new SaveData { ClearedStage = 3, Level = 60 };
            StageStars.Sync(save);
            int fights = Enumerable.Range(1, 3).Sum(n => StageStars.FightCount(n));
            Assert.AreEqual(fights, StageStars.Total(save));
            Assert.IsTrue(StageStars.Unlocked(save, 1, 1));      // 어려움 1 opens with 일반 1
            Assert.IsFalse(StageStars.Unlocked(save, 2, 1));     // 어려움 2 also needs 어려움 1
            Assert.IsFalse(StageStars.Unlocked(save, 4, 1));
            StageStars.Sync(save);
            Assert.AreEqual(fights, StageStars.Total(save));
        }

        [Test]
        public void HarderDifficultiesScaleEnemiesMoreInLateStages()
        {
            var e = EarlyGameContent.FindEnemy("tiger");
            Assert.AreEqual(e.HpMul * StageStars.NormalHpBoost(1), StageStars.ScaleEnemy(e, 1, 0).HpMul, 0.001f, "일반 gets the HP boost of its stage");
            Assert.Greater(StageStars.NormalHpBoost(30), StageStars.NormalHpBoost(10), "the boost grows with the stage");
            Assert.Greater(StageStars.NormalHpBoost(10), StageStars.NormalHpBoost(1));
            var survival = EarlyGameContent.FindEnemy("geomhu_ioksang");
            Assert.AreSame(survival, StageStars.ScaleEnemy(survival, 29, 0), "survival-only enemies keep their stats");
            var hard = StageStars.ScaleEnemy(e, 20, 1);
            var hell = StageStars.ScaleEnemy(e, 20, 2);
            Assert.Greater(hard.HpMul, e.HpMul);
            Assert.Greater(hell.HpMul, hard.HpMul);
            Assert.Greater(hell.AtkMul, hard.AtkMul);
            Assert.AreEqual(e.Id, hard.Id);
            Assert.Greater(StageStars.HpFor(25, 1), StageStars.HpFor(8, 1));
            Assert.Less(StageStars.HpFor(5, 2), StageStars.HpFor(8, 2));   // the solo stages stay gentle
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
            Assert.AreEqual(3000, save.Doo);
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
            Assert.AreEqual(3000, save.Doo);
        }

        [Test]
        public void HarderDifficultiesStartAtTheirBaseLevelAndRiseToTheLastStage()
        {
            var tiger = EarlyGameContent.FindEnemy("tiger");
            Assert.AreEqual(1, StageStars.ScaleEnemy(tiger, 1, 0).Level);                 // 일반 is untouched
            Assert.AreEqual(33, StageStars.ScaleEnemy(tiger, 1, 1).Level);                // 어려움 stage 1 is built for level 33
            Assert.AreEqual(63, StageStars.ScaleEnemy(tiger, 1, 2).Level);                // 극악 stage 1 for level 63
            int last = EarlyGameContent.Stages.Length;
            Assert.AreEqual(0, StageStars.LevelAdd(last, 1));                              // the last stage already has the top level
            Assert.AreEqual(0, StageStars.LevelAdd(last, 2));
            int prevHard = int.MaxValue, prevExtreme = int.MaxValue;
            for (int n = 1; n <= last; n++)
            {
                var st = EarlyGameContent.FindStage(n);
                int hard = st.RecommendedLevel + StageStars.LevelAdd(n, 1), extreme = st.RecommendedLevel + StageStars.LevelAdd(n, 2);
                Assert.GreaterOrEqual(hard, st.RecommendedLevel);
                Assert.GreaterOrEqual(extreme, hard, "stage " + n);
                if (n > 1) { Assert.GreaterOrEqual(hard, 33); Assert.GreaterOrEqual(extreme, 63); }
            }
        }

        [Test]
        public void HardNeedsLevelTenAndExtremeLevelTwenty()
        {
            var save = new SaveData { Level = 9 };
            int last = StageStars.FightCount(1) - 1;
            StageStars.Record(save, 1, last, 0, 1);
            Assert.IsFalse(StageStars.Unlocked(save, 1, 1));
            StringAssert.Contains("Lv.10", StageStars.LockLabel(save, 1, 1));
            save.Level = 10;
            Assert.IsTrue(StageStars.Unlocked(save, 1, 1));
            StageStars.Record(save, 1, last, 1, 1);
            Assert.IsFalse(StageStars.Unlocked(save, 1, 2));    // cleared on 어려움 but level 10 < 20
            StringAssert.Contains("Lv.20", StageStars.LockLabel(save, 1, 2));
            save.Level = 20;
            Assert.IsTrue(StageStars.Unlocked(save, 1, 2));
            Assert.AreEqual("", StageStars.LockLabel(save, 1, 0));
        }
    }
}
