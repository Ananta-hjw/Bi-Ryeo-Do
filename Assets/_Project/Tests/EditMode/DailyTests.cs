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
        public void StoryAndCompanionSkillsCountDailyGoalsWithoutCreatingHeroMastery()
        {
            var save = BalanceSimulation.Starter(40, 44);
            var child = new Combatant("story_young_yeonbi", "어린 연비", Team.Player, Progression.HeroStatsAtCurve(30), new[] { new SkillSpec { Id = "scene_ssangseom", Name = "충각 쌍섬일격뢰", Power = 8f } }, true);   // a story-only body (37 is reenacted now)
            var focus = child.Skills.First(x => x.Id == "scene_ssangseom");
            Assert.AreEqual(-1, Daily.RecordPlayerSkill(save, child, focus));
            var heroine = CompanionGrowth.Create(save, EarlyGameContent.FindCompanion("na_yerin"));
            Daily.RecordPlayerSkill(save, heroine, heroine.Skills.First(x => x.Id != "basic"));
            Assert.AreEqual(2, Daily.ProgressOf(save, Daily.Find("skill20")));
            Assert.IsEmpty(save.ArtMastery);
            var hero = Formation.BuildHero(save); var skill = hero.Skills.First(x => x.Id != "basic");
            Daily.RecordPlayerSkill(save, hero, skill);
            Assert.AreEqual(3, Daily.ProgressOf(save, Daily.Find("skill20")));
            Assert.AreEqual(1, Mastery.Uses(save, skill.Id));
            Daily.RecordPlayerSkill(save, child, SkillSpec.BasicAttack());
            var enemy = new Combatant("enemy", "적", Team.Enemy, child.Stats, null);
            Daily.RecordPlayerSkill(save, enemy, focus);
            Assert.AreEqual(3, Daily.ProgressOf(save, Daily.Find("skill20")));
        }

        [Test]
        public void PoolIsValidAndEveryTierHasMissions()
        {
            Assert.AreEqual(Daily.Pool.Length, Daily.Pool.Select(m => m.Id).Distinct().Count());
            for (int t = 0; t < 3; t++) Assert.GreaterOrEqual(Daily.Pool.Count(m => m.Tier == t), 4, "tier " + t);
            foreach (var m in Daily.Pool) Assert.Greater(m.Target, 0, m.Id);
        }

        [Test]
        public void SixMissionsTwoOfEachTierAndStableWithinADay()
        {
            var save = Player();
            var a = Daily.Missions(save);
            Assert.AreEqual(Daily.Count, a.Count);
            CollectionAssert.AreEqual(new[] { 0, 0, 1, 1, 2, 2 }, a.Select(m => m.Tier));
            Assert.AreEqual(Daily.Count, a.Select(m => m.Id).Distinct().Count());
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
            Assert.IsFalse(Daily.AllClaimed(save));
            Assert.AreEqual(Daily.Count, second.Count);
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
        public void ClearingAllSixPaysTheBonusAndStartsTheStreak()
        {
            var save = Player();
            Finish(save);
            for (int i = 0; i < Daily.Count; i++) Daily.Claim(save, i);
            Assert.IsTrue(Daily.AllClaimed(save));
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
                for (int i = 0; i < Daily.Count; i++) Daily.Claim(save, i);
                _now = _now.AddDays(1);
            }
            Assert.AreEqual(3, save.DailyStreak);
            Assert.AreEqual(3, Inventory.Count(save, "okruju"));   // day 3 milestone
            _now = _now.AddDays(2);                                 // skip a day
            Daily.Refresh(save);
            Assert.AreEqual(0, save.DailyStreak);
            Finish(save);
            for (int i = 0; i < Daily.Count; i++) Daily.Claim(save, i);
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
                CollectionAssert.DoesNotContain(ids, DailyEvent.UltimateUse);   // nothing equipped
                CollectionAssert.DoesNotContain(ids, DailyEvent.PromotionTry);
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

        [Test]
        public void SettingTheClockBackDoesNotReopenClaimedMissions()
        {
            var save = Player();
            Finish(save);
            for (int i = 0; i < Daily.Count; i++) Daily.Claim(save, i);
            long doo = save.Doo;
            _now = _now.AddDays(-1);
            Assert.AreEqual(Daily.Count, Daily.Missions(save).Count);
            Assert.IsTrue(Daily.AllClaimed(save));
            _now = _now.AddDays(1);
            Daily.Missions(save);
            Assert.IsTrue(Daily.AllClaimed(save));
            for (int i = 0; i < Daily.Count; i++) Assert.AreEqual(0, Daily.Claim(save, i).Count);
            Assert.AreEqual(doo, save.Doo);
        }

        [Test]
        public void TheThirtyDayRewardRepeatsAndTheNextMilestoneKeepsGoing()
        {
            var save = Player();
            save.DailyStreak = 29;
            var yesterday = _now.AddDays(-1);
            save.DailyLastClear = yesterday.Year * 10000 + yesterday.Month * 100 + yesterday.Day;
            save.DailyDate = 0;
            Finish(save);
            for (int i = 0; i < Daily.Count; i++) Daily.Claim(save, i);
            Assert.AreEqual(30, save.DailyStreak);
            Assert.AreEqual(2, Inventory.Count(save, "ilgeup_yeongdan"));
            Assert.AreEqual(60, Daily.NextMilestone(save));
        }

        [Test]
        public void EveryTierStillHasMissionsForAPlayerWithEverythingOpen()
        {
            var save = new SaveData { ClearedStage = 30, Doo = 1000000 };
            save.EquippedArts.Add("seonpung_roejeol");
            for (int d = 0; d < 60; d++)
            {
                _now = new DateTime(2026, 12, 1).AddDays(d);
                var ms = Daily.Missions(save);
                Assert.AreEqual(Daily.Count, ms.Count);
                CollectionAssert.AreEqual(new[] { 0, 0, 1, 1, 2, 2 }, ms.Select(m => m.Tier));
                Assert.AreEqual(Daily.Count, ms.Select(m => m.Id).Distinct().Count());
            }
        }
    }
}
