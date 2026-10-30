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
            var save = new SaveData { Level = 10 };
            Assert.AreEqual(System.Math.Round(Progression.ExpToNext(10) * 0.30), Quests.ExpReward(save));
        }
    }
}
