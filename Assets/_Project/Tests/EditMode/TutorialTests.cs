using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class TutorialTests
    {
        [Test]
        public void EveryTopicHasUniqueIdTitleAndPages()
        {
            Assert.AreEqual(Tutorial.All.Length, Tutorial.All.Select(t => t.Id).Distinct().Count());
            foreach (var t in Tutorial.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(t.Title), t.Id);
                Assert.Greater(t.Pages.Length, 0, t.Id);
                foreach (var p in t.Pages) Assert.IsFalse(string.IsNullOrWhiteSpace(p), t.Id);
                if (t.Details == null) continue;
                Assert.Greater(t.Details.Length, 0, t.Id);
                foreach (var p in t.Details) Assert.IsFalse(string.IsNullOrWhiteSpace(p), t.Id);
            }
        }

        [Test]
        public void TopicsUsedByTheGameExist()
        {
            foreach (var id in new[] { "intro", "battle", "counter", "joint", "mastery", "board", "promotion", "status" }) Assert.IsNotNull(Tutorial.Find(id), id);
            foreach (TrialKind kind in System.Enum.GetValues(typeof(TrialKind))) if (kind != TrialKind.None && kind != TrialKind.Finale) Assert.IsNotNull(Tutorial.Find(Tutorial.TrialTopic(kind)), kind.ToString());   // the scripted 화룡멸겁대진 (Finale) has no rule page
        }

        [Test]
        public void MarkDoneIsRememberedOnce()
        {
            var save = new SaveData();
            Assert.IsFalse(Tutorial.IsDone(save, "battle"));
            Tutorial.MarkDone(save, "battle");
            Tutorial.MarkDone(save, "battle");
            Assert.IsTrue(Tutorial.IsDone(save, "battle"));
            Assert.IsTrue(Tutorial.IsDone(save, "counter"));
            Assert.AreEqual(2, save.TutorialDone.Count);
        }
        [Test]
        public void LegacyBattleGuideCompletionDoesNotHideTheNewCounterGuide()
        {
            var save = new SaveData();
            save.TutorialDone.Add("battle");
            Assert.IsTrue(Tutorial.IsDone(save, "battle"));
            Assert.IsFalse(Tutorial.IsDone(save, "counter"));
            Tutorial.MarkDone(save, "counter");
            Assert.IsTrue(Tutorial.IsDone(save, "counter"));
            Assert.AreEqual(2, save.TutorialDone.Count);
        }

        [Test]
        public void FirstBattleAndStandaloneCounterGuideShareTheSameInstructionPages()
        {
            var battle = Tutorial.Find("battle").PagesFor(false);
            var counter = Tutorial.Find("counter").PagesFor(false);
            Assert.AreEqual(counter[0], battle[battle.Length - 2]);
            Assert.AreEqual(counter[1], battle[battle.Length - 1]);
            StringAssert.Contains("25%", counter[1]);
            StringAssert.Contains("다른 적", counter[1]);
        }
    }
}
