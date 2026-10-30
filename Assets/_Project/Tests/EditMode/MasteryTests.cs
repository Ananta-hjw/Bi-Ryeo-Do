using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class MasteryTests
    {
        /// <summary>A save past every 숙련 stage and level gate, so these tests see the use-count steps alone (the gates have their own tests).</summary>
        internal static SaveData Opened()
        {
            var save = new SaveData { Level = 99, ClearedStage = 36 };
            for (int d = 1; d <= 2; d++)
                for (int s = 1; s <= 36; s++)
                    for (int f = 0; f < StageStars.FightCount(s, d); f++) StageStars.Record(save, s, f, d, 1);
            return save;
        }

        private static ArtData Art(string id) { return EarlyGameContent.FindArt(id); }

        [Test]
        public void UsesRaiseLevelAtThresholds()
        {
            var save = Opened();
            int rose = -1;
            for (int i = 0; i < 10; i++) rose = Mastery.RecordUse(save, "samcheopyeong");
            Assert.AreEqual(1, rose);
            Assert.AreEqual(1, Mastery.Level(save, "samcheopyeong"));
            for (int i = 0; i < 20; i++) Mastery.RecordUse(save, "samcheopyeong");
            Assert.AreEqual(2, Mastery.Level(save, "samcheopyeong"));
        }

        [Test]
        public void PowerGrowsWithLevelAndVariantNeedsDaeseong()
        {
            var save = Opened();
            var art = Mastery_AttackArtWithCost();
            float basePower = art.Power;
            for (int i = 0; i < 10; i++) Mastery.RecordUse(save, art.Id);
            Assert.AreEqual(basePower * 1.03f, Mastery.Apply(save, art).Power, 0.001f);
            for (int i = 0; i < 15; i++) Mastery.RecordUse(save, art.Id);
            Assert.AreEqual(basePower * 1.07f, Mastery.Apply(save, art).Power, 0.001f);   // 대성
            Assert.AreEqual(ArtVariant.None, Mastery.CycleVariant(save, art));   // not 극성 yet
            for (int i = 0; i < 25; i++) Mastery.RecordUse(save, art.Id);
            Assert.AreEqual(basePower * 1.15f, Mastery.Apply(save, art).Power, 0.001f);   // 극성
            Assert.AreNotEqual(ArtVariant.None, Mastery.CycleVariant(save, art));
        }

        [Test]
        public void VariantsReshapeTheSkill()
        {
            var art = Mastery_AttackArtWithCost();
            var save = Opened();
            for (int i = 0; i < 50; i++) Mastery.RecordUse(save, art.Id);
            var plain = Mastery.Apply(save, art);
            Mastery.Find(save, art.Id).Variant = (int)ArtVariant.Flurry;
            var flurry = Mastery.Apply(save, art);
            Assert.AreEqual(plain.Hits + 1, flurry.Hits);
            Assert.AreEqual(plain.Power * 0.65f, flurry.Power, 0.001f);
            Mastery.Find(save, art.Id).Variant = (int)ArtVariant.Swift;
            var swift = Mastery.Apply(save, art);
            Assert.Less(swift.QiCost, plain.QiCost);
            Mastery.Find(save, art.Id).Variant = (int)ArtVariant.Power;
            var power = Mastery.Apply(save, art);
            Assert.AreEqual(plain.Power * 1.5f, power.Power, 0.001f);
            Assert.GreaterOrEqual(power.QiCost, plain.QiCost);
        }

        [Test]
        public void CompanionArtsAndUltimatesHaveNoVariants()
        {
            foreach (var a in EarlyGameContent.Arts.Where(x => x.OwnerId != null || x.SpiritCost > 0))
                Assert.AreEqual(0, Mastery.AvailableVariants(a).Count, a.Id);
        }

        private static ArtData Mastery_AttackArtWithCost()
        {
            return EarlyGameContent.Arts.First(a => a.OwnerId == null && a.SpiritCost == 0 && a.Effect == SkillEffect.None && a.QiCost > 0 && EarlyGameContent.FormsOf(a.Id).Count == 0);   // an art without 무공 변화, so only 숙련 changes it
        }

        [Test]
        public void VariantCostsUseExactIntegerRounding()
        {
            var save = Opened();
            var art = new ArtData { Id = "t", Name = "t", Power = 2f, QiCost = 20, Cooldown = 2 };
            for (int i = 0; i < 50; i++) Mastery.RecordUse(save, art.Id);
            Mastery.Find(save, art.Id).Variant = (int)ArtVariant.Power;
            Assert.AreEqual(24, Mastery.Apply(save, art).QiCost);   // 20 x 1.2, never 25
            Mastery.Find(save, art.Id).Variant = (int)ArtVariant.Swift;
            Assert.AreEqual(16, Mastery.Apply(save, art).QiCost);   // 20 x 0.8
            Mastery.Find(save, art.Id).Variant = (int)ArtVariant.Flurry;
            Assert.AreEqual(2, Mastery.Apply(save, art).Hits);      // unset Hits counts as one
        }

        [Test]
        public void ThresholdsAreTenTwentyFiveFiftyEighty()
        {
            CollectionAssert.AreEqual(new[] { 0, 10, 25, 50, 80 }, Mastery.Thresholds);
            CollectionAssert.AreEqual(new[] { 0f, 0.03f, 0.07f, 0.15f, 0.30f }, Mastery.PowerBonus);
            Assert.AreEqual(3, Mastery.VariantLevel);
            Assert.AreEqual(4, Mastery.MasterLevel);
            var save = Opened();
            for (int i = 0; i < 24; i++) Mastery.RecordUse(save, "samcheopyeong");
            Assert.AreEqual(1, Mastery.Level(save, "samcheopyeong"));
            Assert.AreEqual(1, Mastery.UsesToNext(save, "samcheopyeong"));
            Mastery.RecordUse(save, "samcheopyeong");
            Assert.AreEqual(2, Mastery.Level(save, "samcheopyeong"));
            for (int i = 0; i < 25; i++) Mastery.RecordUse(save, "samcheopyeong");
            Assert.AreEqual(3, Mastery.Level(save, "samcheopyeong"));
            for (int i = 0; i < 30; i++) Mastery.RecordUse(save, "samcheopyeong");
            Assert.AreEqual(4, Mastery.Level(save, "samcheopyeong"));
            Assert.AreEqual(-1, Mastery.UsesToNext(save, "samcheopyeong"));
            StringAssert.Contains("80회", Mastery.ConditionText());
        }

        [Test]
        public void MasterLevelCutsQiByTwentyPercent()
        {
            var art = new ArtData { Id = "t2", Name = "t2", Power = 2f, QiCost = 20, Cooldown = 2 };
            var save = Opened();
            for (int i = 0; i < 79; i++) Mastery.RecordUse(save, art.Id);
            Assert.AreEqual(20, Mastery.Apply(save, art).QiCost);
            Mastery.RecordUse(save, art.Id);
            Assert.AreEqual(16, Mastery.Apply(save, art).QiCost);
            Mastery.Find(save, art.Id).Variant = (int)ArtVariant.Power;
            Assert.AreEqual(19, Mastery.Apply(save, art).QiCost);   // 20 x 1.2 = 24, then x 0.8
            Assert.AreEqual(0, Mastery.Apply(save, new ArtData { Id = "free", Name = "f", Power = 1f }).QiCost);
        }

        [Test]
        public void MasterLevelShortensTheCooldownByOne()
        {
            var art = EarlyGameContent.Arts.First(a => a.OwnerId == null && a.Cooldown >= 2 && a.SpiritCost == 0);
            var save = Opened();
            for (int i = 0; i < 79; i++) Mastery.RecordUse(save, art.Id);
            Assert.AreEqual(art.Cooldown, Mastery.Apply(save, art).Cooldown);
            Mastery.RecordUse(save, art.Id);
            Assert.AreEqual(art.Cooldown - 1, Mastery.Apply(save, art).Cooldown);
            var instant = EarlyGameContent.Arts.First(a => a.OwnerId == null && a.Cooldown == 0);
            for (int i = 0; i < 80; i++) Mastery.RecordUse(save, instant.Id);
            Assert.AreEqual(0, Mastery.Apply(save, instant).Cooldown);
        }

        [Test]
        public void ScrollsAreRolledSeparatelyAtTenFiveTwoPercent()
        {
            CollectionAssert.AreEqual(new[] { 0.10f, 0.05f, 0.02f }, Mastery.ScrollChance);
            var save = Opened();
            var rng = new SeededRandom(11);
            int[] found = new int[3];
            const int rolls = 20000;
            for (int i = 0; i < rolls; i++)
                foreach (var s in Mastery.RollScrolls(save, rng, 30, 1)) found[System.Array.IndexOf(Mastery.ScrollIds, s.Id)]++;
            Assert.AreEqual(0.10, found[0] / (double)rolls, 0.01);
            Assert.AreEqual(0.05, found[1] / (double)rolls, 0.007);
            Assert.AreEqual(0.02, found[2] / (double)rolls, 0.005);
            Assert.AreEqual(found[0], Inventory.Count(save, "art_scroll_5"));
        }

        [Test]
        public void ScrollsStartDroppingAtTheirOwnStage()
        {
            Assert.IsTrue(Mastery.ScrollCanDrop(0, 1, 0));
            Assert.IsFalse(Mastery.ScrollCanDrop(1, 19, 0));
            Assert.IsTrue(Mastery.ScrollCanDrop(1, 20, 0));
            Assert.IsTrue(Mastery.ScrollCanDrop(1, 1, 1));
            Assert.IsFalse(Mastery.ScrollCanDrop(2, 30, 0));
            Assert.IsFalse(Mastery.ScrollCanDrop(2, 9, 1));
            Assert.IsTrue(Mastery.ScrollCanDrop(2, 10, 1));
            Assert.IsTrue(Mastery.ScrollCanDrop(2, 1, 2));
            var save = Opened();
            var rng = new SeededRandom(3);
            for (int i = 0; i < 5000; i++) foreach (var s in Mastery.RollScrolls(save, rng, 5, 0)) Assert.AreEqual("art_scroll_5", s.Id);
        }

        [Test]
        public void ReadingAScrollRaisesOnlyTheChosenLearnedArt()
        {
            var save = Opened();
            bool used;
            Inventory.Add(save, "art_scroll_10", 2);
            save.LearnedArts.Add("bunroesu");
            save.LearnedArts.Add("samcheopyeong");
            StringAssert.Contains("배운 무공", Mastery.UseScroll(save, "art_scroll_10", "roegwang_ryuhagok", out used));   // not learned
            Assert.IsFalse(used);
            Assert.AreEqual(2, Inventory.Count(save, "art_scroll_10"));
            CollectionAssert.AreEquivalent(new[] { "bunroesu", "samcheopyeong" }, Mastery.ScrollTargets(save).Select(a => a.Id));

            string line = Mastery.UseScroll(save, "art_scroll_10", "bunroesu", out used);
            Assert.IsTrue(used);
            Assert.AreEqual(10, Mastery.Uses(save, "bunroesu"));
            Assert.AreEqual(0, Mastery.Uses(save, "samcheopyeong"));
            Assert.AreEqual(1, Inventory.Count(save, "art_scroll_10"));
            StringAssert.Contains("소성", line);   // 10 uses is 소성
            Mastery.UseScroll(save, "art_scroll_10", "bunroesu", out used);
            Mastery.UseScroll(save, "art_scroll_10", "bunroesu", out used);
            Assert.IsFalse(used);   // none left
        }

        [Test]
        public void AcademyShelfSellsScrollsFromYuho()
        {
            long[] prices = { 4500, 8200, 13000 };
            for (int i = 0; i < 3; i++)
            {
                var good = Shop.Goods.Single(g => g.ItemId == Mastery.ScrollIds[i]);
                Assert.AreEqual("academy", good.FactionId);
                Assert.AreEqual(2, good.MinTier);
                Assert.AreEqual(prices[i], good.Price);
                Assert.IsFalse(EarlyGameContent.FindItem(good.ItemId).InGacha);
            }
        }

        [Test]
        public void UltimatesGainFiveTenTwentyFortyPercentInsteadOfVariants()
        {
            var ult = EarlyGameContent.Arts.First(a => a.OwnerId == null && a.SpiritCost > 0 && a.Power > 0);
            var save = Opened();
            float[] expect = { 1f, 1.05f, 1.10f, 1.20f, 1.40f };
            int[] uses = { 0, 2, 5, 10, 20 };
            for (int lv = 0; lv < 5; lv++)
            {
                for (int i = Mastery.Uses(save, ult.Id); i < uses[lv]; i++) Mastery.RecordUse(save, ult.Id);
                Assert.AreEqual(lv, Mastery.Level(save, ult.Id), "uses " + uses[lv]);
                Assert.AreEqual(ult.Power * expect[lv], Mastery.Apply(save, ult).Power, 0.001f, "level " + lv);
            }
            Assert.AreEqual(0, Mastery.AvailableVariants(ult).Count);
        }

        [Test]
        public void UltimatesStepUpAtTwoFiveTenTwentyUsesAndScrollsGiveOneTwoFour()
        {
            var ult = EarlyGameContent.Arts.First(a => a.OwnerId == null && a.SpiritCost > 0 && a.Power > 0);
            var normal = EarlyGameContent.Arts.First(a => a.OwnerId == null && a.SpiritCost == 0 && a.Power > 0);
            CollectionAssert.AreEqual(new[] { 0, 2, 5, 10, 20 }, Mastery.ThresholdsFor(ult));
            CollectionAssert.AreEqual(new[] { 0, 10, 25, 50, 80 }, Mastery.ThresholdsFor(normal));
            var save = Opened();
            Assert.AreEqual(-1, Mastery.RecordUse(save, ult.Id));
            Assert.AreEqual(1, Mastery.RecordUse(save, ult.Id));     // the 2nd use reaches 소성
            Assert.AreEqual(3, Mastery.UsesToNext(save, ult.Id));    // 대성 at 5
            int[] gains = { 1, 2, 4 };
            for (int i = 0; i < Mastery.ScrollIds.Length; i++)
            {
                var scroll = EarlyGameContent.FindItem(Mastery.ScrollIds[i]);
                Assert.AreEqual(gains[i], Mastery.ScrollUses(scroll, ult));
                Assert.AreEqual(scroll.MasteryUses, Mastery.ScrollUses(scroll, normal));
            }
            save.LearnedArts.Add(ult.Id);
            Inventory.Add(save, "art_scroll_20", 1);
            bool used;
            Mastery.UseScroll(save, "art_scroll_20", ult.Id, out used);
            Assert.IsTrue(used);
            Assert.AreEqual(6, Mastery.Uses(save, ult.Id));          // 2 + 4
            Assert.AreEqual(2, Mastery.Level(save, ult.Id));
            save.EquippedArts.Add(ult.Id);
            Mastery.Train(save, 12);                                   // free practice: a fifth for a 비기
            Assert.AreEqual(8, Mastery.Uses(save, ult.Id));
        }

        [Test]
        public void SamcheopyeongDodgeChanceFollowsMastery()
        {
            var art = EarlyGameContent.FindArt("samcheopyeong");
            var save = Opened();
            float[] expect = { 0.50f, 0.65f, 0.80f, 0.90f, 1.00f };
            int[] uses = { 0, 10, 25, 50, 80 };
            for (int lv = 0; lv < 5; lv++)
            {
                for (int i = Mastery.Uses(save, art.Id); i < uses[lv]; i++) Mastery.RecordUse(save, art.Id);
                Assert.AreEqual(expect[lv], Mastery.Apply(save, art).EvadeChance, 0.0001f, "level " + lv);
            }
            Assert.AreEqual(1f, art.ToSpec().EvadeChance);   // the balance bot uses the plain art
        }

        [Test]
        public void PartialDodgeChanceMissesOnlyAShareOfHits()
        {
            int dodged = 0, hit = 0;
            for (int seed = 0; seed < 300; seed++)
            {
                var st = EarlyGameContent.FindStage(1);
                var party = SimpleBot.BuildParty(1, st.RecommendedPlayerLevel);
                var enemies = st.Encounters[0].Select((id, i) => EarlyGameContent.FindEnemy(id).CreateCombatant(id + i)).ToList();
                var battle = new InteractiveBattle(party, enemies, new SeededRandom(seed));
                battle.Begin();
                var hero = party[0];
                var evade = new SkillSpec { Id = "ev", Name = "ev", Effect = SkillEffect.Evade, EvadeChance = 0.5f };
                foreach (var e in battle.PlayerAct(new PlayerCommand { Kind = CommandKind.Skill, Skill = evade }))
                    if (e.Kind == EventKind.Attack && e.TargetId == hero.Id) { if (e.Dodged) dodged++; else hit++; }
            }
            Assert.Greater(dodged, 0);
            Assert.Greater(hit, 0);
            Assert.AreEqual(0.5, dodged / (double)(dodged + hit), 0.1);
        }

        [Test]
        public void TheHelpTextListsEveryMasteryStep()
        {
            var text = Mastery.HelpText();
            for (int i = 1; i < Mastery.Thresholds.Length; i++)
                StringAssert.Contains(Mastery.LevelNames[i] + " " + Mastery.Thresholds[i] + "회", text);
            StringAssert.Contains("+3%", text);
            StringAssert.Contains("+30%", text);
            StringAssert.Contains("변초", text);
        }
    }
}
