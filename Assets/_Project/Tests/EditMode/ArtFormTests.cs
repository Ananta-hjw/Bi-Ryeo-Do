using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    /// <summary>무공 변화: an art of a line turns into the next technique once its 숙련, the form's stage and 비류연's level allow it.</summary>
    public class ArtFormTests
    {
        private static void Clear(SaveData save, int stage, int diff)
        {
            for (int f = 0; f < StageStars.FightCount(stage); f++) StageStars.Record(save, stage, f, diff, 1);
        }

        private static void Uses(SaveData save, string id, int uses)
        {
            var e = Mastery.Find(save, id);
            if (e == null) save.ArtMastery.Add(new ArtMasteryEntry { Id = id, Uses = uses });
            else e.Uses = uses;
        }

        [Test]
        public void PungsinNeedsMasteryStageAndLevelForEachForm()
        {
            var pungsin = EarlyGameContent.FindArt("pungsin");
            var save = new SaveData { Level = 80 };
            save.LearnedArts.Add("pungsin");
            Uses(save, "pungsin", 5);   // 대성 for a 비기
            Assert.AreEqual("pungsin", Mastery.CurrentForm(save, pungsin).Id, "어려움 36 not cleared yet");
            Clear(save, 36, 1);
            Assert.AreEqual("pyeonsu_pungsin", Mastery.CurrentForm(save, pungsin).Id);
            save.Level = Mastery.TierGate(pungsin, 2).Level - 1;
            Assert.AreEqual("pungsin", Mastery.CurrentForm(save, pungsin).Id, "below the step's level");
            save.Level = 80;
            Clear(save, 20, 2);
            Assert.AreEqual("pyeonsu_pungsin", Mastery.CurrentForm(save, pungsin).Id, "극의 not reached");
            Uses(save, "pungsin", 20);
            var spec = Mastery.Apply(save, pungsin);
            Assert.AreEqual("pungsin", spec.Id, "the line keeps its id");
            Assert.AreEqual("풍신 화룡난류", spec.Name);
            Assert.AreEqual(StatusKind.Burn, spec.Inflict);
        }

        [Test]
        public void FormsAreNotLearnableArtsAndEveryFormHasALine()
        {
            foreach (var f in EarlyGameContent.ArtForms)
            {
                Assert.IsNull(EarlyGameContent.FindArt(f.Id), f.Id);
                var line = EarlyGameContent.FindArt(f.FormOf);
                Assert.IsNotNull(line, f.Id);
                Assert.IsNull(line.OwnerId);
                Assert.AreEqual(line.SpiritCost > 0, f.SpiritCost > 0, f.Id);
                Assert.AreEqual(line.Effect, f.Effect, f.Id);
                Assert.That(f.FormTier, Is.InRange(1, Mastery.MasterLevel), f.Id);
            }
            // within a line the steps rise
            foreach (var g in EarlyGameContent.ArtForms.GroupBy(f => f.FormOf))
            {
                var forms = EarlyGameContent.FormsOf(g.Key);
                for (int i = 1; i < forms.Count; i++)
                {
                    Assert.Greater(forms[i].FormTier, forms[i - 1].FormTier, g.Key);
                    Assert.GreaterOrEqual(forms[i].UnlockDiff * 36 + forms[i].UnlockStage, forms[i - 1].UnlockDiff * 36 + forms[i - 1].UnlockStage, g.Key);
                }
            }
            Assert.AreEqual(28, EarlyGameContent.Arts.Count(a => a.OwnerId == null), "48 arts - 11 folded into lines - 9 removed (비도 던지기 included)");
        }

        [Test]
        public void ANewFormIsAnnouncedOnce()
        {
            var save = new SaveData { Level = 80 };
            save.LearnedArts.Add("hoejeop");
            Clear(save, 8, 2);
            Clear(save, 14, 2);
            Assert.IsEmpty(Mastery.PollNewForms(save));
            Uses(save, "hoejeop", 25);
            CollectionAssert.AreEqual(new[] { "회접 → 격공장" }, Mastery.PollNewForms(save));
            Assert.IsEmpty(Mastery.PollNewForms(save));
        }

        [Test]
        public void OldSavesFoldFormsIntoTheirLineAndForgetRemovedArts()
        {
            var save = new SaveData();
            save.LearnedArts.AddRange(new[] { "samcheopyeong", "nakroe_yeong", "yeobanjang", "pyeonsu_pungsin" });
            save.EquippedArts.AddRange(new[] { "yeobanjang", "nakroe_yeong", "pyeonsu_pungsin" });
            Uses(save, "samcheopyeong", 12);
            Uses(save, "nakroe_yeong", 40);
            Uses(save, "yeobanjang", 30);
            Mastery.MigrateForms(save);
            CollectionAssert.AreEquivalent(new[] { "samcheopyeong", "pungsin" }, save.LearnedArts);
            CollectionAssert.AreEqual(new[] { "samcheopyeong", "pungsin" }, save.EquippedArts);
            Assert.AreEqual(40, Mastery.Uses(save, "samcheopyeong"), "the higher 숙련 wins");
            Assert.IsNull(Mastery.Find(save, "yeobanjang"));
            Assert.IsNull(Mastery.Find(save, "nakroe_yeong"));
        }
    
        [Test]
        public void MasteryStepsWaitForTheirStageAndLevel()
        {
            var art = EarlyGameContent.FindArt("bunroesu");   // 일반 2
            var save = new SaveData { Level = 1 };
            save.LearnedArts.Add("bunroesu");
            Uses(save, "bunroesu", 80);
            Assert.AreEqual(0, Mastery.Level(save, "bunroesu"), "uses alone are not enough");
            Assert.AreEqual(4, Mastery.UsesLevel(save, "bunroesu"));
            var g = Mastery.TierGate(art, 1);
            Assert.AreEqual(0, g.Difficulty);
            Assert.AreEqual(5, g.Stage);   // 일반 2 + 3
            save.ClearedStage = g.Stage - 1;   // reached
            save.Level = g.Level;
            Assert.AreEqual(1, Mastery.Level(save, "bunroesu"));
            Assert.AreEqual(0, Mastery.UsesToNext(save, "bunroesu"), "the next step waits for its gate, not for uses");
        }

        [Test]
        public void EveryArtHasRisingGatesAndFormStepsUseTheFormsGate()
        {
            foreach (var art in EarlyGameContent.Arts.Where(a => a.OwnerId == null))
            {
                for (int t = 2; t <= Mastery.MasterLevel; t++)
                {
                    var a = Mastery.TierGate(art, t - 1);
                    var b = Mastery.TierGate(art, t);
                    Assert.GreaterOrEqual(b.Difficulty * 36 + b.Stage, a.Difficulty * 36 + a.Stage, art.Id);
                    Assert.GreaterOrEqual(b.Level, a.Level, art.Id);
                }
                foreach (var f in EarlyGameContent.FormsOf(art.Id))
                {
                    var g = Mastery.TierGate(art, f.FormTier);
                    Assert.AreEqual(f.UnlockStage, g.Stage, f.Id);
                    Assert.AreEqual(f.UnlockDiff, g.Difficulty, f.Id);
                }
            }
        }
    }
}
