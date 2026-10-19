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
            }
        }

        [Test]
        public void TopicsUsedByTheGameExist()
        {
            foreach (var id in new[] { "intro", "battle", "joint", "mastery", "board", "promotion" }) Assert.IsNotNull(Tutorial.Find(id), id);
            foreach (TrialKind kind in System.Enum.GetValues(typeof(TrialKind))) if (kind != TrialKind.None) Assert.IsNotNull(Tutorial.Find(Tutorial.TrialTopic(kind)), kind.ToString());
        }

        [Test]
        public void MarkDoneIsRememberedOnce()
        {
            var save = new SaveData();
            Assert.IsFalse(Tutorial.IsDone(save, "battle"));
            Tutorial.MarkDone(save, "battle");
            Tutorial.MarkDone(save, "battle");
            Assert.IsTrue(Tutorial.IsDone(save, "battle"));
            Assert.AreEqual(1, save.TutorialDone.Count);
        }
    }
}
