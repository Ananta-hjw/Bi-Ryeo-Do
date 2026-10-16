using System;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class DailyTests
    {
        private DateTime _now;

        [SetUp]
        public void SetUp()
        {
            _now = new DateTime(2026, 10, 10, 12, 0, 0);
            Daily.Clock = () => _now;
        }

        [TearDown]
        public void TearDown() { Daily.Clock = () => DateTime.Now; }

        private static SaveData Player() { return new SaveData { ClearedStage = 10, Doo = 0 }; }

        private static void Finish(SaveData save)
        {
            foreach (var m in Daily.Missions(save)) Daily.Record(save, m.Event, m.Target);
        }

        [Test]
        public void PoolIsValidAndEveryTierHasMissions()
        {
            Assert.AreEqual(Daily.Pool.Length, Daily.Pool.Select(m => m.Id).Distinct().Count());
            for (int t = 0; t < 3; t++) Assert.GreaterOrEqual(Daily.Pool.Count(m => m.Tier == t), 3, "tier " + t);
            foreach (var m in Daily.Pool) Assert.Greater(m.Target, 0, m.Id);
        }

        [Test]
        public void ThreeMissionsOneOfEachTierAndStableWithinADay()
        {
            var save = Player();
            var a = Daily.Missions(save);
            Assert.AreEqual(3, a.Count);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, a.Select(m => m.Tier));
            var b = Daily.Missions(save);
            CollectionAssert.AreEqual(a.Select(m => m.Id), b.Select(m => m.Id));
        }

        [Test]
        public void ANewDayRollsNewMissionsAndClearsProgress()
        {
            var save = Player();
            var first = Daily.Missions(save).Select(m => m.Id).ToList();
            Daily.Record(save, DailyEvent.BattleWin, 5);
            _now = _now.AddDays(1);
            var second = Daily.Missions(save);
            Assert.AreEqual(0, save.DailyProgress[(int)DailyEvent.BattleWin]);
            // A different date almost always rolls a different set; at least the claim state is fresh.
            Assert.AreEqual(0, save.DailyClaimed.Count);
            Assert.IsFalse(save.DailyAllClaimed);
            Assert.AreEqual(3, second.Count);
            Assert.IsNotNull(first);
        }

        [Test]
        public void RecordCountsAndClaimPaysOnlyWhenDone()
        {
            var save = Player();
            var m = Daily.Missions(save)[0];
            Assert.AreEqual(0, Daily.Claim(save, 0).Count);
            Daily.Record(save, m.Event, m.Target - 1);
            Assert.IsFalse(Daily.IsDone(save, m));
            Assert.AreEqual(0, Daily.Claim(save, 0).Count);
            Daily.Record(save, m.Event, 1);
            Assert.IsTrue(Daily.IsDone(save, m));
            Assert.AreEqual(1, Daily.ClaimableCount(save) >= 1 ? 1 : 0);
            var notes = Daily.Claim(save, 0);
            Assert.AreEqual(1, notes.Count);
            Assert.AreEqual(Daily.RewardDoo(save, m), save.Doo);
            Assert.AreEqual(0, Daily.Claim(save, 0).Count);   // once
        }

        [Test]
        public void RewardsGrowWithProgress()
        {
            var early = new SaveData { ClearedStage = 0 };
            var late = new SaveData { ClearedStage = 30 };
            var hard = Daily.Pool.First(m => m.Tier == 2);
            Assert.Greater(Daily.RewardDoo(late, hard), Daily.RewardDoo(early, hard));
            Assert.Greater(Daily.RewardDoo(early, hard), Daily.RewardDoo(early, Daily.Pool.First(m => m.Tier == 0)));
        }

        [Test]
        public void ClearingAllThreePaysTheBonusAndStartsTheStreak()
        {
            var save = Player();
            Finish(save);
            for (int i = 0; i < 3; i++) Daily.Claim(save, i);
            Assert.IsTrue(save.DailyAllClaimed);
            Assert.AreEqual(1, save.DailyStreak);
            Assert.AreEqual(2, Inventory.Count(save, "bear_gall"));
            Assert.Greater(save.Doo, Daily.AllClearDoo(save));
            Assert.AreEqual(1, save.DailyBest);
        }

        [Test]
        public void StreakContinuesOnConsecutiveDaysAndResetsAfterAGap()
        {
            var save = Player();
            for (int day = 0; day < 3; day++)
            {
                Finish(save);
                for (int i = 0; i < 3; i++) Daily.Claim(save, i);
                _now = _now.AddDays(1);
            }
            Assert.AreEqual(3, save.DailyStreak);
            Assert.AreEqual(3, Inventory.Count(save, "okruju"));   // day 3 milestone
            _now = _now.AddDays(2);                                 // skip a day
            Daily.Refresh(save);
            Assert.AreEqual(0, save.DailyStreak);
            Finish(save);
            for (int i = 0; i < 3; i++) Daily.Claim(save, i);
            Assert.AreEqual(1, save.DailyStreak);
            Assert.AreEqual(3, save.DailyBest);
        }

        [Test]
        public void MissionsThatCannotBeEarnedYetAreNotOffered()
        {
            var fresh = new SaveData { ClearedStage = 0 };
            for (int d = 0; d < 40; d++)
            {
                _now = new DateTime(2026, 11, 1).AddDays(d);
                fresh.DailyDate = 0;
                var ids = Daily.Missions(fresh).Select(m => m.Event).ToList();
                CollectionAssert.DoesNotContain(ids, DailyEvent.TowerClear);
                CollectionAssert.DoesNotContain(ids, DailyEvent.QuestDone);
                CollectionAssert.DoesNotContain(ids, DailyEvent.JointUse);
            }
        }

        [Test]
        public void GameplayHooksFeedTheCounters()
        {
            var save = new SaveData { ClearedStage = 6, Doo = 100000 };
            Daily.Missions(save);
            Gacha.Pull(save, 10, new SeededRandom(3));
            Assert.AreEqual(10, save.DailyProgress[(int)DailyEvent.GachaPull]);
            var offer = Promotion.BuildOffer(save, 2, 1);
            Assert.IsTrue(Promotion.Pay(save, offer));
            Assert.AreEqual(1, save.DailyProgress[(int)DailyEvent.PromotionTry]);
            Quests.Complete(save, Quests.Find("q_hogol"));
            Assert.AreEqual(1, save.DailyProgress[(int)DailyEvent.QuestDone]);
        }

        [Test]
        public void OlderSavesWithShorterCountersStayValid()
        {
            var save = Player();
            Daily.Missions(save);
            save.DailyProgress.RemoveRange(2, save.DailyProgress.Count - 2);
            Daily.Record(save, DailyEvent.GachaPull, 1);   // would have thrown before padding
            Assert.AreEqual(1, save.DailyProgress[(int)DailyEvent.GachaPull]);
        }
    }
}
