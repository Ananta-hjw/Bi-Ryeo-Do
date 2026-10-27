using System;
using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StoryExpansionTests
    {
        [TestCase(21, 2, 2, -1)]
        [TestCase(22, 2, 5, 0)]
        [TestCase(23, 2, 3, 2)]
        [TestCase(24, 2, 3, 2)]
        [TestCase(25, 2, 3, 2)]
        public void RevisionSixPreservesClearsWithoutGrantingInsertedFightRewards(int number, int oldFinal, int final, int unplayed)
        {
            var save = new SaveData { StoryContentRevision = 5, ClearedStage = 25, ProgressStage = number, ProgressNext = 2, Level = 60, Doo = 12345 };
            for (int diff = 0; diff < 3; diff++)
                for (int fight = 0; fight <= oldFinal; fight++)
                    save.StageStars.Add(new StarEntry { Stage = number, Fight = fight, Diff = diff, Mask = 7 });
            save.DoneQuests.Add("q_hyeolma"); save.DoneFortunes.Add("f_mirror_return"); save.SeenMemories.Add("m_talk");
            int total = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(total, StageStars.Total(save));
            Assert.AreEqual(0, save.ProgressStage);
            Assert.AreEqual(25, save.ClearedStage); Assert.AreEqual(60, save.Level); Assert.AreEqual(12345, save.Doo);
            for (int diff = 0; diff < 3; diff++)
            {
                var stage = EarlyGameContent.FindStage(number).WithDifficulty(diff);
                Assert.AreEqual(7, StageStars.MaskOf(save, number, final, diff));
                Assert.IsTrue(StageStars.StageCleared(save, number, diff));
                Assert.IsTrue(Recommend.IsReplay(save, stage));
                for (int fight = 0; fight < stage.Encounters.Length; fight++)
                {
                    var actual = GearDrops.RollFight(save, stage, fight, new SeededRandom(42));
                    var repeat = GearDrops.Roll(number, diff, false, new SeededRandom(42));
                    CollectionAssert.AreEqual(repeat.Select(x => x.Id + ":" + x.Count), actual.Select(x => x.Id + ":" + x.Count));
                }
                if (unplayed >= 0) Assert.AreEqual(0, StageStars.MaskOf(save, number, unplayed, diff));
            }
            Assert.AreEqual(1, save.DoneQuests.Count); Assert.AreEqual(1, save.DoneFortunes.Count); Assert.AreEqual(1, save.SeenMemories.Count);
            StoryContentMigration.Apply(save); Assert.AreEqual(total, StageStars.Total(save));
            var untouched = new SaveData { StoryContentRevision = 5, ProgressStage = 20, ProgressNext = 1 };
            StoryContentMigration.Apply(untouched); Assert.AreEqual(20, untouched.ProgressStage);
        }

        [Test]
        public void VolumesElevenToThirteenKeepEscortTrialsAndLivingOpponentsDistinct()
        {
            var bandits = EarlyGameContent.FindStage(21);
            CollectionAssert.AreEqual(new[] { "im_gae" }, bandits.Encounters[1]);
            CollectionAssert.AreEqual(new[] { "lee_songhak" }, bandits.Encounters[2]);
            var valley = EarlyGameContent.FindStage(22);
            Assert.AreEqual(6, valley.Encounters.Length);
            Assert.AreEqual(TrialKind.Survive, valley.TrialFor(0).Kind);
            Assert.AreEqual(5, valley.Encounters[1].Length + valley.Encounters[2].Length);
            CollectionAssert.AreEqual(valley.Encounters[3], Quests.ToStage(Quests.Find("q_hyeolma")).Encounters[0]);
            var masked = EarlyGameContent.FindStage(24);
            Assert.IsTrue(GearDrops.IsBossFight(masked, 2));
            Assert.IsFalse(GearDrops.IsBossFight(masked, 3));
            Assert.AreEqual(TrialKind.Survive, masked.TrialFor(3).Kind);
            var rescue = EarlyGameContent.FindStage(25);
            Assert.AreEqual(TrialKind.Survive, rescue.TrialFor(3).Kind);
            CollectionAssert.Contains(rescue.Encounters[1], "cheonsal_hwapyeong");
            CollectionAssert.Contains(rescue.Encounters[1], "jisal_hwapyeong");
            foreach (string id in new[] { "im_deokseong_story", "im_gae", "lee_songhak", "cheonghong_ssanggaksa", "mo_saryeong", "gong_pae", "maehwa_mask_trial", "cheonsal_hwapyeong", "jisal_hwapyeong", "baek_son_gosu" })
                Assert.Greater(EarlyGameContent.FindEnemy(id).RetreatHpPct, 0f, id);
        }

        [TestCase(15, 4, 3, 4, 3)]
        [TestCase(18, 3, 2, 3, 2)]
        [TestCase(19, 5, 4, 6, 3)]
        public void RevisionFiveKeepsFinalCreditAndLeavesInsertedFightsUnclaimed(int number, int oldCount, int oldLast, int newLast, int added)
        {
            var save = new SaveData { StoryContentRevision = 4, ClearedStage = 20, ProgressStage = number, ProgressNext = oldLast, Doo = 12345, Level = 24 };
            for (int diff = 0; diff < 3; diff++)
                for (int fight = 0; fight < oldCount; fight++)
                    save.StageStars.Add(new StarEntry { Stage = number, Fight = fight, Diff = diff, Mask = 7 });
            save.DoneQuests.Add("q_baegin"); save.SeenMemories.Add("m_visit");
            int stars = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            for (int diff = 0; diff < 3; diff++)
            {
                Assert.AreEqual(7, StageStars.MaskOf(save, number, newLast, diff));
                Assert.AreEqual(0, StageStars.MaskOf(save, number, added, diff));
                Assert.IsTrue(StageStars.StageCleared(save, number, diff));
            }
            if (number == 19) Assert.AreEqual(0, StageStars.MaskOf(save, 19, 4, 0));
            Assert.AreEqual(stars, StageStars.Total(save));
            Assert.AreEqual(0, save.ProgressStage);
            Assert.AreEqual(20, save.ClearedStage); Assert.AreEqual(12345, save.Doo); Assert.AreEqual(24, save.Level);
            CollectionAssert.AreEqual(new[] { "q_baegin" }, save.DoneQuests);
            CollectionAssert.AreEqual(new[] { "m_visit" }, save.SeenMemories);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(stars, StageStars.Total(save));
        }

        [Test]
        public void VolumesEightToTenKeepNonlethalAndPrivateGoalsDistinct()
        {
            var cavalry = EarlyGameContent.FindStage(15);
            Assert.AreEqual(5, cavalry.Encounters.Length);
            CollectionAssert.AreEqual(new[] { "wi_musang" }, cavalry.Encounters[3]);
            CollectionAssert.AreEqual(new[] { "gu_cheonhak" }, cavalry.Encounters[4]);
            Assert.AreEqual(TrialKind.Survive, EarlyGameContent.FindStage(17).TrialFor(1).Kind);
            var ice = EarlyGameContent.FindStage(18);
            Assert.AreEqual(4, ice.Encounters.Length);
            Assert.AreEqual(TrialKind.Survive, ice.TrialFor(0).Kind);
            Assert.AreEqual(TrialKind.Survive, ice.TrialFor(2).Kind);
            Assert.IsFalse(GearDrops.IsBossFight(ice, 2));
            Assert.IsTrue(GearDrops.IsBossFight(ice, 3));
            var trial = EarlyGameContent.FindStage(19);
            Assert.AreEqual(7, trial.Encounters.Length);
            foreach (int i in new[] { 1, 3, 4, 5 }) Assert.AreEqual(TrialKind.Survive, trial.TrialFor(i).Kind);
            foreach (string id in new[] { "gu_cheonhak", "wi_musang", "baek_tanjeo", "pae_hyeokseong", "hyo_ryong_rope" })
                Assert.Greater(EarlyGameContent.FindEnemy(id).RetreatHpPct, 0f, id);
            Assert.AreEqual(27, EarlyGameContent.FindArt("saengsa_seomroe").UnlockStage);
            Assert.IsFalse(EarlyGameContent.StageRewards[20].Any(item => item.Id == "mukrin_talisman"));
            string story = string.Join(" ", StoryEpisodes.Replay(EarlyGameContent.FindStage(20)));
            Assert.Less(story.IndexOf("이십일 일째", StringComparison.Ordinal), story.IndexOf("이십육 일째", StringComparison.Ordinal));
            Assert.Less(story.IndexOf("나예린이 먼저 입을 맞췄다", StringComparison.Ordinal), story.IndexOf("벽이 뚫리고", StringComparison.Ordinal));
            StringAssert.Contains("최종 집계", story);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void RevisedChaptersPayOnTheirOwnDifficultyAndReplayRate(int difficulty)
        {
            foreach (int number in new[] { 15, 18, 19, 20, 21, 22, 23, 24, 25 })
            {
                var stage = EarlyGameContent.FindStage(number).WithDifficulty(difficulty);
                Recommend.StageReward(new SaveData(), stage, out long exp, out long doo);
                var completed = new SaveData { ClearedStage = number };
                StageStars.Record(completed, number, stage.Encounters.Length - 1, difficulty, 1);
                Recommend.StageReward(completed, stage, out long repeatExp, out long repeatDoo);
                Assert.Greater(exp, repeatExp); Assert.Greater(doo, repeatDoo);
                for (int i = 0; i < stage.Encounters.Length; i++)
                {
                    Recommend.FightReward(new SaveData(), stage, i, false, out long fightExp, out long fightDoo);
                    Assert.Greater(fightExp, 0); Assert.Greater(fightDoo, 0);
                }
                TestContext.Out.WriteLine(number + "/" + difficulty + ": EXP " + exp + "; Doo " + doo);
            }
        }

        [Test]
        public void VolumesFourToSevenSeparateEscapeRetreatAndMentalRestoration()
        {
            var infiltration = EarlyGameContent.FindStage(10);
            Assert.IsTrue(infiltration.Trials.All(t => t.Kind == TrialKind.Survive));
            var wudang = EarlyGameContent.FindStage(14);
            Assert.AreEqual(11, wudang.Encounters.Length);
            CollectionAssert.AreEqual(new[] { "chohon_gwigeom", "chohon_gwigeom" }, wudang.Encounters[7]);
            CollectionAssert.AreEqual(new[] { "myeongbu_jang", "myeongbu_jang" }, wudang.Encounters[8]);
            Assert.AreEqual(TrialKind.Survive, wudang.TrialFor(6).Kind);
            Assert.AreEqual(TrialKind.Survive, wudang.TrialFor(9).Kind);
            CollectionAssert.AreEqual(new[] { "cheonsal", "jisal" }, wudang.Encounters.Last());
            foreach (string id in new[] { "chohon_gwigeom", "myeongbu_jang", "cheonsal", "jisal" })
                Assert.Greater(EarlyGameContent.FindEnemy(id).RetreatHpPct, 0f, id);
            string text = string.Join(" ", StoryEpisodes.Replay(wudang));
            Assert.Less(text.IndexOf("윤준호는 아홉 시진", StringComparison.Ordinal), text.IndexOf("열두 시진", StringComparison.Ordinal));
            StringAssert.Contains("갈효봉|효룡", text);
            StringAssert.Contains("마진가는 학생들의 증언을 믿고 거절했다", text);
        }

        [Test]
        public void RevisionFourPreservesEveryDifficultyAndDoesNotAwardNewFightOrMemoryCredit()
        {
            var save = new SaveData { StoryContentRevision = 3, ClearedStage = 20, ProgressStage = 14, ProgressNext = 6, Doo = 12345, Level = 20 };
            for (int diff = 0; diff < 3; diff++)
                for (int fight = 0; fight < 8; fight++)
                    save.StageStars.Add(new StarEntry { Stage = 14, Fight = fight, Diff = diff, Mask = 7 });
            save.SeenMemories.Add("m_cave");
            save.DoneFortunes.Add("f_cave");
            int total = StageStars.Total(save);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(total, StageStars.Total(save));
            foreach (int diff in new[] { 0, 1, 2 })
            {
                Assert.IsTrue(StageStars.StageCleared(save, 14, diff));
                foreach (int added in new[] { 2, 5, 8 }) Assert.AreEqual(0, StageStars.MaskOf(save, 14, added, diff));
                Assert.AreEqual(7, StageStars.MaskOf(save, 14, 10, diff));
            }
            Assert.AreEqual(0, save.ProgressStage);
            Assert.AreEqual(20, save.ClearedStage); Assert.AreEqual(12345, save.Doo); Assert.AreEqual(20, save.Level);
            CollectionAssert.AreEqual(new[] { "m_cave" }, save.SeenMemories);
            CollectionAssert.AreEqual(new[] { "f_cave" }, save.DoneFortunes);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(total, StageStars.Total(save));
        }

        [TestCase(10, 1)]
        [TestCase(10, 2)]
        [TestCase(14, 1)]
        [TestCase(14, 2)]
        [TestCase(15, 1)]
        [TestCase(15, 2)]
        [TestCase(16, 1)]
        [TestCase(16, 2)]
        [TestCase(17, 1)]
        [TestCase(17, 2)]
        [TestCase(18, 1)]
        [TestCase(18, 2)]
        [TestCase(19, 1)]
        [TestCase(19, 2)]
        [TestCase(20, 1)]
        [TestCase(20, 2)]
        [TestCase(21, 1)]
        [TestCase(21, 2)]
        [TestCase(22, 1)]
        [TestCase(22, 2)]
        [TestCase(23, 1)]
        [TestCase(23, 2)]
        [TestCase(24, 1)]
        [TestCase(24, 2)]
        [TestCase(25, 1)]
        [TestCase(25, 2)]
        public void RevisedChapterDifficultyStillSupportsItsPreparedBuild(int number, int difficulty)
        {
            var stage = EarlyGameContent.FindStage(number).WithDifficulty(difficulty);
            int wins = Enumerable.Range(801, 10).Count(seed => BalanceSimulation.Play(stage,
                BalanceSimulation.Starter(number, (int)Math.Round(StageStars.TargetLevel(number, difficulty))), seed).Won);
            TestContext.Out.WriteLine(number + " difficulty " + difficulty + ": " + wins + "/10");
            Assert.GreaterOrEqual(wins, 8);
        }

        [TestCase(10)]
        [TestCase(14)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(17)]
        [TestCase(18)]
        [TestCase(19)]
        [TestCase(20)]
        [TestCase(21)]
        [TestCase(22)]
        [TestCase(23)]
        [TestCase(24)]
        [TestCase(25)]
        public void RevisedChaptersRemainClearableWithPreparedStartKitBuilds(int number)
        {
            var stage = EarlyGameContent.FindStage(number);
            int wins = Enumerable.Range(701, 20).Count(seed => BalanceSimulation.Play(stage,
                BalanceSimulation.Starter(number, BalanceSimulation.BotLevel(stage)), seed).Won);
            TestContext.Out.WriteLine(number + ": revised chapter " + wins + "/20");
            Assert.GreaterOrEqual(wins, 17);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void AddedWudangFightsPayAtEachDifficultyWithoutDilutingOldShares(int difficulty)
        {
            var stage = EarlyGameContent.FindStage(14).WithDifficulty(difficulty);
            var save = new SaveData();
            foreach (int fight in new[] { 2, 5, 8 })
            {
                Recommend.FightReward(save, stage, fight, false, out long exp, out long doo);
                Assert.Greater(exp, 0); Assert.Greater(doo, 0);
                Recommend.FightReward(save, stage, fight, true, out long replayExp, out long replayDoo);
                Assert.Less(replayExp, exp); Assert.Less(replayDoo, doo);
            }
            Recommend.StageReward(save, stage, out long totalExp, out long totalDoo);
            TestContext.Out.WriteLine("Wudang difficulty " + difficulty + ": EXP " + totalExp + ", Doo " + totalDoo);
        }

        [Test]
        public void ExpandedStoryUsesActualOpponentsWithoutRepeatedOrCombinedTournamentDuels()
        {
            var escort = EarlyGameContent.FindStage(2);
            Assert.AreEqual(5, escort.Encounters.Length);
            CollectionAssert.AreEqual(new[] { "deung_yeoun", "deung_yeoho" }, escort.Encounters[3]);
            CollectionAssert.AreEqual(new[] { "jang_uyang" }, escort.Encounters[4]);
            var tournament = EarlyGameContent.FindStage(12);
            CollectionAssert.AreEqual(new[] { "dan_pyeong", "jo_yeonil", "jin_pae", "ilyeon", "eonga_yeong", "jeon_okgi" }, tournament.Encounters.SelectMany(x => x));
            Assert.IsTrue(tournament.Encounters.All(x => x.Length == 1));
            CollectionAssert.AreEqual(new[] { "dang_munceon" }, EarlyGameContent.FindStage(13).Encounters[0]);
            Assert.AreEqual(36, EarlyGameContent.Stages.Length, "Persisted stage and unlock IDs remain stable");
        }

        [Test]
        public void SaveMigrationPreservesEarnedStarsBossWinsAndProgressionAndIsIdempotent()
        {
            var save = new SaveData { StoryContentRevision = 0, ClearedStage = 13, Level = 18, Doo = 12345, ProgressStage = 12, ProgressNext = 1 };
            save.StageStars.AddRange(new[] {
                new StarEntry { Stage = 2, Fight = 2, Diff = 1, Mask = 7 },
                new StarEntry { Stage = 11, Fight = 0, Mask = 7 },
                new StarEntry { Stage = 11, Fight = 1, Mask = 3 },
                new StarEntry { Stage = 11, Fight = 2, Mask = 7 },
                new StarEntry { Stage = 12, Fight = 2, Diff = 2, Mask = 7 } });
            int stars = StageStars.Total(save);
            save.ClaimedStarChests.Add(15);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(stars, StageStars.Total(save));
            Assert.AreEqual(7, StageStars.MaskOf(save, 2, 4, 1));
            Assert.AreEqual(7, StageStars.MaskOf(save, 12, 5, 2));
            Assert.IsTrue(StageStars.StageCleared(save, 11, 0));
            Assert.AreEqual(0, save.ProgressStage);
            Assert.AreEqual(13, save.ClearedStage);
            Assert.AreEqual(18, save.Level);
            Assert.AreEqual(12345, save.Doo);
            CollectionAssert.Contains(save.ClaimedStarChests, 15);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(stars, StageStars.Total(save));
            Assert.AreEqual(5, save.RetiredStoryStars);
            var fresh = new SaveData { StoryContentRevision = 0 }; StoryContentMigration.Apply(fresh);
            Assert.AreEqual(0, StageStars.Total(fresh));
        }

        [Test]
        public void FormerSideStoriesAreRequiredMainStageFightsWithNormalRewardsAndResume()
        {
            var admission = EarlyGameContent.FindStage(7);
            Assert.AreEqual(8, admission.Encounters.Length);
            CollectionAssert.AreEqual(new[] { "admit_jeomchang", "admit_hwasan", "admit_geumbok", "admit_eonga" }, admission.Encounters.Take(4).SelectMany(x => x));
            CollectionAssert.AreEqual(new[] { "no_jusaeng", "chae_hagun" }, EarlyGameContent.FindStage(11).Encounters[0]);
            var final = EarlyGameContent.FindStage(13);
            Assert.AreEqual(4, final.Encounters.Length);
            Assert.AreEqual(TrialKind.Survive, final.TrialFor(1).Kind);
            Assert.AreEqual(TrialKind.Survive, final.TrialFor(2).Kind);
            var save = BalanceSimulation.Starter(13, 20);
            StageProgress.Store(save, 13, 2, BalanceSimulation.Party(save, final));
            Assert.IsTrue(StageProgress.Has(save, 13));
            foreach (int number in new[] { 7, 11, 13 })
            {
                var stage = EarlyGameContent.FindStage(number);
                for (int fight = 0; fight < stage.Encounters.Length; fight++)
                {
                    Recommend.FightReward(save, stage, fight, false, out long exp, out long doo);
                    Assert.Greater(exp, 0); Assert.Greater(doo, 0);
                    Assert.IsFalse(stage.IsQuest || stage.IsTower);
                }
            }
        }

        [TestCase(2, 9.0 / 7.0)]
        [TestCase(7, 12.0 / 8.0)]
        [TestCase(11, 7.0 / 3.0)]
        [TestCase(12, 8.0 / 6.0)]
        [TestCase(13, 6.0 / 4.0)]
        public void AddedFightsIncreaseTheStageRewardRatherThanDilutingExistingPayouts(int number, double factor)
        {
            var stage = EarlyGameContent.FindStage(number);
            var baseline = stage.WithDifficulty(0); baseline.RewardWeightBudget = 0;
            var ids = stage.Encounters.SelectMany(x => x).ToList();
            double originalDoo = ids.Sum(id => EarlyGameContent.DooReward(baseline, id));
            double originalExp = ids.Sum(id => EarlyGameContent.ExpReward(baseline, id));
            Assert.That(ids.Sum(id => EarlyGameContent.DooReward(stage, id)), Is.EqualTo(originalDoo * factor).Within(ids.Count * factor));
            Assert.That(ids.Sum(id => EarlyGameContent.ExpReward(stage, id)), Is.EqualTo(originalExp * factor).Within(ids.Count * factor));
            Assert.Greater(factor, 1);
        }

        [Test]
        public void RevisionOneSaveMovesExistingBossWinsAndDoesNotDeleteNewRewardOpportunities()
        {
            var save = new SaveData { StoryContentRevision = 1, ClearedStage = 13, ProgressStage = 13, ProgressNext = 1 };
            save.StageStars.AddRange(new[] {
                new StarEntry { Stage = 7, Fight = 3, Mask = 7 },
                new StarEntry { Stage = 11, Fight = 0, Mask = 7 },
                new StarEntry { Stage = 13, Fight = 1, Diff = 1, Mask = 7 } });
            StoryContentMigration.Apply(save);
            Assert.AreEqual(7, StageStars.MaskOf(save, 7, 7, 0));
            Assert.AreEqual(7, StageStars.MaskOf(save, 11, 3, 0));
            Assert.AreEqual(7, StageStars.MaskOf(save, 13, 3, 1));
            Assert.AreEqual(0, StageStars.MaskOf(save, 7, 0, 0));
            Assert.AreEqual(9, StageStars.Total(save));
            Assert.AreEqual(0, save.ProgressStage);
            StoryContentMigration.Apply(save);
            Assert.AreEqual(9, StageStars.Total(save));
        }

        [TestCase(2)]
        [TestCase(7)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        public void ExpandedMainStoryRemainsClearableAtThePreparedLevel(int number)
        {
            var stage = EarlyGameContent.FindStage(number);
            var runs = Enumerable.Range(301, 40).Select(seed => BalanceSimulation.Play(stage,
                BalanceSimulation.Starter(number, BalanceSimulation.BotLevel(stage)), seed)).ToList();
            int wins = runs.Count(x => x.Won);
            TestContext.Out.WriteLine(number + ": " + wins + "/40, actions " + runs.Where(x => x.Won).Select(x => x.Actions).DefaultIfEmpty().Average() + ", HP " + runs.Where(x => x.Won).Select(x => x.HpLeft).DefaultIfEmpty().Average());
            Assert.GreaterOrEqual(wins, 34);
        }

    }
}
