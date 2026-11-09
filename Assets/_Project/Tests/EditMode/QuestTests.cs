using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class QuestTests
    {
        [TestCase("q_shell_rehearsal")] [TestCase("q_sister_form")] [TestCase("q_blood_cross")]
        [TestCase("q_explosion_exit")] [TestCase("q_gate_lines")]
        [TestCase("q_number_space")] [TestCase("q_bending_wind")] [TestCase("q_watch_circle")] [TestCase("q_wolf_cooperation")] [TestCase("q_departure")]
        [TestCase("q_guest_parry")]
        [TestCase("q_bridge_cover")]
        [TestCase("q_rope_pattern")]
        [TestCase("q_ichon")]
        [TestCase("q_white_phoenix")]
        [TestCase("q_bait_cover")]
        [TestCase("q_eogeom_review")]
        [TestCase("q_baekhyang_exit")]
        [TestCase("q_promotion_guard")]
        [TestCase("q_river_cover")]
        [TestCase("q_sura_escort")]
        [TestCase("q_baton_route")]
        [TestCase("q_witness_exit")]
        [TestCase("q_fire_gather")]
        [TestCase("q_witness_cover")]
        [TestCase("q_flute_cover")]
        [TestCase("q_hwapyeong_exit")]
        [TestCase("q_market_screen")]
        [TestCase("q_rope_rehearsal")]
        [TestCase("q_illusion_breath")]
        [TestCase("q_twins_cover")] [TestCase("q_shadow_deception")] [TestCase("q_chorus_break")] [TestCase("q_spear_read")] [TestCase("q_current_guard")] [TestCase("q_father_carry")]
        public void ReanalysisRehearsalRewardsArePaidOnlyOnce(string id)
        {
            var quest = Quests.Find(id);
            var save = new SaveData { ClearedStage = quest.RefStage };
            Assert.IsTrue(Quests.Open(save).Contains(quest));
            Quests.Complete(save, quest);
            int count = Inventory.Count(save, quest.ItemId);
            Assert.AreEqual(1, count);
            Assert.IsFalse(Quests.Open(save).Contains(quest));
            Assert.AreEqual("", Quests.Complete(save, quest));
            Assert.AreEqual(count, Inventory.Count(save, quest.ItemId));
            Assert.AreEqual(1, save.DoneQuests.Count(x => x == id));
        }

        [Test]
        public void EveryQuestReferencesARealEncounterAndHasATwist()
        {
            foreach (var q in Quests.All)
            {
                var st = Quests.ReferenceStage(q);   // 일반 30's quest replays one of the fights 어려움·극악 still play
                Assert.IsNotNull(st, q.Id);
                Assert.Less(q.Encounter, st.Encounters.Length, q.Id);
                Assert.IsNotNull(q.Trial, q.Id);
                if (q.ItemId != null) Assert.IsNotNull(EarlyGameContent.FindItem(q.ItemId), q.Id);
                Assert.AreEqual(1, Quests.ToStage(q).Encounters.Length);
            }
        }

        [Test]
        public void QuestsUnlockWhenTheirStageIsCleared()
        {
            var save = new SaveData { ClearedStage = 2 };
            Assert.AreEqual(0, Quests.Open(save).Count);
            save.ClearedStage = 3;
            Assert.AreEqual(1, Quests.Open(save).Count);
            Quests.Complete(save, Quests.All[0]);
            Assert.AreEqual(0, Quests.Open(save).Count);
            Assert.IsTrue(save.Items.Any(i => i.Id == "bear_gall"));
        }

        [Test]
        public void EveryQuestIsClearableByTheBotAtTheRecommendedLevel()
        {
            foreach (var q in Quests.All)
            {
                var st = Quests.ToStage(q);
                // Quests unlock after this stage; include companions/arts that have actually joined by then.
                int wins = Enumerable.Range(1, 20).Count(seed => BalanceSimulation.Play(st,
                    BalanceSimulation.Starter(q.RefStage + 1, BalanceSimulation.BotLevel(st)), seed).Won);
                TestContext.Out.WriteLine(q.Id + " " + wins + "/20");
                Assert.GreaterOrEqual(wins, 17, q.Id + " " + wins + "/20");
            }
        }
    
        [Test]
        public void FortunesAreOneTimeAndApplyTheirEffects()
        {
            var save = new SaveData { ClearedStage = 7, Doo = 100 };
            var camp = Fortunes.Find("f_campfire");
            Assert.IsTrue(Fortunes.Open(save).Contains(camp));
            Fortunes.Resolve(save, camp, camp.Choices[0], new[] { "hyo_ryong" });
            Assert.AreEqual(2, Bonds.Points(save, "hyo_ryong"));
            Assert.IsFalse(Fortunes.Open(save).Contains(camp));
            Assert.AreEqual("", Fortunes.Resolve(save, camp, camp.Choices[0], new[] { "hyo_ryong" }));   // already done
            Assert.AreEqual(2, Bonds.Points(save, "hyo_ryong"));
        }

        [Test]
        public void EveryFortuneShowsItsExpOnItsOwnLine()
        {
            foreach (var ev in Fortunes.All)
                for (int i = 0; i < ev.Choices.Length; i++)
                {
                    var c = ev.Choices[i];
                    if (c.ExpLevels <= 0) continue;
                    var save = new SaveData { ClearedStage = 66, Level = 20, Doo = 1000000 };
                    string text = Fortunes.Resolve(save, ev, c, null);
                    StringAssert.IsMatch(@"\n경험 \+[0-9,]+", text, ev.Id + " " + i);
                    StringAssert.DoesNotContain("경험 +\n", text, ev.Id + " " + i);
                    Assert.IsFalse(c.Result.EndsWith("경험 +"), ev.Id + " " + i);
                }
        }

        [Test]
        public void ShopTrainingIsRefusedWhenNoEquippedArtCanGrow()
        {
            var save = new SaveData { Doo = 1000000 };
            save.Reputation.Add(new ReputationEntry { Id = "wudang", Points = 1000 });
            save.LearnedArts.Add("bunroesu");
            save.EquippedArts.Add("bunroesu");
            for (int i = 0; i < Mastery.Thresholds[Mastery.Thresholds.Length - 1]; i++) Mastery.RecordUse(save, "bunroesu");
            string error;
            Assert.IsNull(Shop.Buy(save, "wud_train1", out error));
            StringAssert.Contains("사용 횟수", error);
            Assert.AreEqual(1000000, save.Doo);
        }

        [Test]
        public void FortunePricesNeedEnoughDoo()
        {
            var save = new SaveData { ClearedStage = 4, Doo = 100 };
            var ev = Fortunes.Find("f_peddler");
            Assert.IsFalse(Fortunes.CanChoose(save, ev.Choices[0]));
            Assert.AreEqual("", Fortunes.Resolve(save, ev, ev.Choices[0], null));
            Assert.IsFalse(save.DoneFortunes.Contains(ev.Id));
            save.Doo = 1000;
            Fortunes.Resolve(save, ev, ev.Choices[0], null);
            Assert.AreEqual(200, save.Doo);
            Assert.IsTrue(save.Items.Any(i => i.Id == "ginseng"));
        }

        [Test]
        public void EveryFortuneHasValidItemsAndChoices()
        {
            foreach (var ev in Fortunes.All)
            {
                Assert.GreaterOrEqual(ev.Choices.Length, 2, ev.Id);
                Assert.LessOrEqual(ev.UnlockStage, EarlyGameContent.Stages.Length, ev.Id);
                foreach (var c in ev.Choices) if (c.ItemId != null) Assert.IsNotNull(EarlyGameContent.FindItem(c.ItemId), ev.Id);
            }
        }

        [Test]
        public void FortunesAreWellFormed()
        {
            Assert.AreEqual(67, Fortunes.All.Length);
            Assert.AreEqual(Fortunes.All.Length, Fortunes.All.Select(f => f.Id).Distinct().Count());
            int max = EarlyGameContent.Stages.Max(st => st.Number);
            foreach (var f in Fortunes.All)
            {
                Assert.LessOrEqual(f.UnlockStage, max, f.Id);
                Assert.IsTrue(f.Choices.Length >= 2 && f.Choices.Length <= 3, f.Id);
                foreach (var c in f.Choices)
                    if (c.ItemId != null) Assert.IsNotNull(EarlyGameContent.FindItem(c.ItemId), f.Id);
            }
        }

        [Test]
        public void QuestDooIsTripledButExpStaysLow()
        {
            Assert.AreEqual(2700, Quests.Find("q_hogol").RewardDoo);
            var st = Quests.ToStage(Quests.Find("q_hogol"));
            Assert.AreEqual(System.Math.Round(Progression.ExpToNext(Progression.PlayerLevelAt(st.RecommendedLevel)) * 0.30), Quests.ExpReward(st));
        }

        /// <summary>Side content pays by its own level: a player far above it gains exactly what a player at its level gains.</summary>
        [Test]
        public void SideContentExpDoesNotGrowWithThePlayersLevel()
        {
            var low = new SaveData { Level = 10, ClearedStage = 30, Doo = 1000000, TowerBest = 0 };
            var high = new SaveData { Level = 100, ClearedStage = 30, Doo = 1000000, TowerBest = 0 };
            var quest = Quests.ToStage(Quests.Find("q_hogol"));
            long e1, d1, e2, d2;
            Recommend.FightReward(low, quest, 0, false, out e1, out d1);
            Recommend.FightReward(high, quest, 0, false, out e2, out d2);
            Assert.AreEqual(e1, e2, "quest");
            Assert.Greater(e1, 0);

            Assert.AreEqual(Tower.ExpReward(5), Progression.ContentExp(Tower.RecommendedPlayerLevel(5), 0.30));
            var tower = Tower.ToStage(low, 3);
            Recommend.FightReward(low, tower, 0, false, out e1, out d1);
            Recommend.FightReward(high, Tower.ToStage(high, 3), 0, false, out e2, out d2);
            Assert.AreEqual(e1, e2, "tower");

            Assert.AreEqual(Promotion.BuildOffer(low, 2, 0).ExpReward, Promotion.BuildOffer(new SaveData { Level = 100 }, 2, 0).ExpReward, "promotion");

            var ev = Fortunes.All.First(f => f.Choices.Any(c => c.ExpLevels > 0 && c.Doo >= 0));
            var choice = ev.Choices.First(c => c.ExpLevels > 0 && c.Doo >= 0);
            var a = new SaveData { Level = 5, ClearedStage = 66 };
            var b = new SaveData { Level = 90, ClearedStage = 66 };
            long ca = 0, cb = 0;
            for (int l = 1; l < 5; l++) ca += Progression.ExpToNext(l);
            for (int l = 1; l < 90; l++) cb += Progression.ExpToNext(l);
            Fortunes.Resolve(a, ev, choice, null);
            Fortunes.Resolve(b, ev, choice, null);
            long ta = a.Exp, tb = b.Exp;
            for (int l = 1; l < a.Level; l++) ta += Progression.ExpToNext(l);
            for (int l = 1; l < b.Level; l++) tb += Progression.ExpToNext(l);
            Assert.AreEqual(ta - ca, tb - cb, "fortune");
        }

        [Test]
        public void MasterTrialRepeatsPayNoExpOrStones()
        {
            Assert.AreEqual(0, MasterTrial.ClearStones(8, false));
            Assert.Greater(MasterTrial.ClearStones(8, true), 0);
        }

        [Test]
        public void AcademySemestersOpenWithStoryProgress()
        {
            var save = new SaveData { ClearedStage = 15 };
            Assert.IsTrue(Academy.SemesterOpen(save));
            Assert.IsTrue(Academy.SignUp(save, new[] { "sword", "guard", "theory" }));
            save.AcademyCourses.Clear();
            save.AcademySemester = 2;
            Assert.IsFalse(Academy.SemesterOpen(save));
            StringAssert.Contains("16", Academy.SemesterLockText(save));
            Assert.IsFalse(Academy.SignUp(save, new[] { "sword", "guard", "theory" }));
            Assert.IsTrue(Academy.LeagueStage(save, Academy.Houses[0].Id).NoReward, "no league rewards before the 학기 opens");
            save.ClearedStage = 16;
            Assert.IsTrue(Academy.SemesterOpen(save));
            Assert.IsTrue(Academy.SignUp(save, new[] { "sword", "guard", "theory" }));
            save.AcademySemester = 9;
            Assert.IsTrue(Academy.AllSemestersDone(save));
            Assert.AreEqual(64, Academy.SemesterGateStage(8));
        }
    }
}
