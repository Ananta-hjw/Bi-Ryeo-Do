using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class FactionShopTests
    {
        [Test]
        public void FactionDataIsWellFormed()
        {
            int max = EarlyGameContent.Stages.Max(s => s.Number);
            Assert.AreEqual(Factions.All.Length, Factions.All.Select(f => f.Id).Distinct().Count());
            foreach (var f in Factions.All)
            {
                Assert.AreEqual(3, f.PerkSteps.Length, f.Id);
                Assert.IsTrue(f.Stages.All(s => s >= 1 && s <= max), f.Id);
                Assert.Less(f.PerkSteps[0], f.PerkSteps[2], f.Id);
            }
        }

        [Test]
        public void ReputationRisesByTierAndReportsTierUps()
        {
            var save = new SaveData();
            Assert.AreEqual(0, Factions.Tier(save, "academy"));
            Assert.AreEqual(-1, Factions.Gain(save, "academy", 11));
            Assert.AreEqual(1, Factions.Gain(save, "academy", 1));   // 12 = 안면
            Assert.AreEqual(2, Factions.Gain(save, "academy", 28));  // 40 = 우호
            Assert.AreEqual(-1, Factions.Gain(save, "nobody", 5));
            Assert.AreEqual(Factions.Thresholds[3] - 40, Factions.ToNext(save, "academy"));
            Factions.Gain(save, "academy", 500);
            Assert.AreEqual(Factions.MaxTier, Factions.Tier(save, "academy"));
            Assert.AreEqual(-1, Factions.ToNext(save, "academy"));
        }

        [Test]
        public void WinningFightsInAStageFeedItsFactionsScaledByDifficulty()
        {
            var save = new SaveData();
            var notes = Factions.OnFightWon(save, 2, 0);   // 천무학관 and 중양표국 share stage 2
            Assert.AreEqual(1, Factions.Points(save, "academy"));
            Assert.AreEqual(1, Factions.Points(save, "biaoguk"));
            Factions.OnFightWon(save, 2, 2);
            Assert.AreEqual(4, Factions.Points(save, "academy"));
            Assert.AreEqual(0, Factions.Points(save, "wudang"));
            Assert.AreEqual(0, Factions.OnFightWon(save, 1, 0).Count);   // no faction tied to stage 1
            Assert.IsNotNull(notes);
        }

        [Test]
        public void PerksStartAtFavorableAndGrow()
        {
            var save = new SaveData();
            Assert.AreEqual(0f, Factions.Bonus(save, PerkKind.ExpPct));
            Factions.Gain(save, "academy", Factions.Thresholds[2]);
            Assert.AreEqual(0.03f, Factions.Bonus(save, PerkKind.ExpPct), 0.0001f);
            Factions.Gain(save, "academy", Factions.Thresholds[4]);
            Assert.AreEqual(0.10f, Factions.Bonus(save, PerkKind.ExpPct), 0.0001f);
            Assert.AreEqual(0f, Factions.Bonus(save, PerkKind.DooPct));
        }

        [Test]
        public void QuestsPayReputationToTheirStagesFactions()
        {
            var save = new SaveData();
            var q = Quests.Find("q_hogol");   // reference stage 3: 중양표국
            Quests.Complete(save, q);
            Assert.AreEqual(Quests.QuestReputation, Factions.Points(save, "biaoguk"));
        }

        [Test]
        public void DropChanceMultiplierRaisesDrops()
        {
            var rng = new SeededRandom(1);
            int basic = 0, boosted = 0;
            for (int i = 0; i < 400; i++)
            {
                basic += Inventory.RollDrops("bear", new SeededRandom(i)).Count;
                boosted += Inventory.RollDrops("bear", new SeededRandom(i), 3f).Count;
            }
            Assert.Greater(boosted, basic);
            Assert.IsNotNull(rng);
        }

        [Test]
        public void ShopGoodsAreValid()
        {
            Assert.AreEqual(Shop.Goods.Length, Shop.Goods.Select(g => g.Id).Distinct().Count());
            foreach (var g in Shop.Goods)
            {
                Assert.Greater(g.Price, 0, g.Id);
                if (g.FactionId != null) Assert.IsNotNull(Factions.Find(g.FactionId), g.Id);
                if (g.Kind == ShopKind.Item) Assert.IsNotNull(EarlyGameContent.FindItem(g.ItemId), g.Id);
                else Assert.Greater(g.Amount, 0, g.Id);
                Assert.IsFalse(string.IsNullOrEmpty(Shop.NameOf(g)), g.Id);
            }
            // Shop-only goods never enter the gacha pool.
            foreach (var id in new[] { "meal_academy", "biaoguk_ration", "taegeuk_pill", "plum_elixir", "greenwood_liquor" })
                Assert.IsFalse(EarlyGameContent.FindItem(id).InPool, id);
        }

        [Test]
        public void BuyingNeedsReputationAndDooAndRespectsLimits()
        {
            var save = new SaveData { Doo = 100000, Level = 10 };
            string error;
            Assert.IsNull(Shop.Buy(save, "aca_scroll", out error));          // needs 우호
            StringAssert.Contains("평판", error);
            Factions.Gain(save, "academy", Factions.Thresholds[2]);
            long before = save.Doo;
            var note = Shop.Buy(save, "aca_scroll", out error);
            Assert.IsNotNull(note);
            Assert.AreEqual(before - 5000, save.Doo);
            Assert.AreEqual(1, Shop.Bought(save, "aca_scroll"));
            for (int i = 0; i < 4; i++) Assert.IsNotNull(Shop.Buy(save, "aca_scroll", out error));
            Assert.IsTrue(Shop.SoldOut(save, Shop.Find("aca_scroll")));
            Assert.IsNull(Shop.Buy(save, "aca_scroll", out error));
            var poor = new SaveData { Doo = 10 };
            Assert.IsNull(Shop.Buy(poor, "inn_gall", out error));
            StringAssert.Contains("두", error);
            Assert.AreEqual(10, poor.Doo);
        }

        [Test]
        public void ItemsTrainingAndScrollsDoTheirJob()
        {
            var save = new SaveData { Doo = 100000, Level = 10 };
            string error;
            Shop.Buy(save, "inn_gall", out error);
            Assert.AreEqual(1, Inventory.Count(save, "bear_gall"));
            Assert.AreEqual(100000 - 300, save.Doo);
            save.EquippedArts.Add("samboku_chobok");
            Factions.Gain(save, "wudang", Factions.Thresholds[2]);
            Shop.Buy(save, "wud_train1", out error);
            Assert.AreEqual(10, Mastery.Uses(save, "samboku_chobok"));
            long exp = save.Exp;
            Factions.Gain(save, "academy", Factions.Thresholds[2]);
            Shop.Buy(save, "aca_scroll", out error);
            Assert.Greater(save.Exp + save.Level * 1000000L, exp);
        }

        [Test]
        public void FortunesStillTrainThroughMasteryTrain()
        {
            var save = new SaveData();
            save.EquippedArts.Add("samboku_chobok");
            Mastery.Train(save, 6);
            Assert.AreEqual(6, Mastery.Uses(save, "samboku_chobok"));
        }

        [Test]
        public void ReputationCanBeRebuiltFromStarRecords()
        {
            var save = new SaveData();
            StageStars.Record(save, 7, 0, 0, 1);   // 천무학관 normal win: +1
            StageStars.Record(save, 7, 1, 2, 1);   // 극악 win: +3
            Factions.SyncFromStars(save);
            Assert.AreEqual(4, Factions.Points(save, "academy"));
            Factions.Gain(save, "academy", 5);       // later wins (and quest bonuses) stay on top
            Factions.SyncFromStars(save);
            Assert.AreEqual(9, Factions.Points(save, "academy"));
        }

        [Test]
        public void TrainingNeedsAnEquippedArt()
        {
            var save = new SaveData { Doo = 100000 };
            Factions.Gain(save, "wudang", Factions.Thresholds[2]);
            string error;
            Assert.IsNull(Shop.Buy(save, "wud_train1", out error));
            StringAssert.Contains("무공", error);
            Assert.AreEqual(100000, save.Doo);
            Assert.AreEqual(0, Shop.Bought(save, "wud_train1"));
            save.EquippedArts.Add("samboku_chobok");
            Assert.IsNotNull(Shop.Buy(save, "wud_train1", out error));
        }
    }
}
