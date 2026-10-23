using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class RecordUpdatesTests
    {
        [Test]
        public void AcknowledgingOnePageDoesNotClearAnotherAndNewDiscoveriesReappear()
        {
            var save = new SaveData();
            Codex.SeeItem(save, "cloth_robe");
            Codex.SeeEnemy(save, EarlyGameContent.Enemies[0].Id);
            Assert.AreEqual(2, RecordUpdates.Unread(save, RecordUpdates.CodexPage));
            int stories = RecordUpdates.Unread(save, RecordUpdates.StoryPage);
            Assert.Greater(stories, 0);
            Assert.IsTrue(RecordUpdates.Acknowledge(save, RecordUpdates.CodexPage));
            Assert.AreEqual(0, RecordUpdates.Unread(save, RecordUpdates.CodexPage));
            Assert.AreEqual(stories, RecordUpdates.Unread(save, RecordUpdates.StoryPage));
            Assert.IsFalse(RecordUpdates.Acknowledge(save, RecordUpdates.CodexPage));
            Codex.SeeItem(save, "straw_sandals");
            Assert.AreEqual(1, RecordUpdates.Unread(save, RecordUpdates.CodexPage));
            Assert.AreEqual(2, save.SeenItems.Count);
        }

        [Test]
        public void ProgressUnlocksNewStoriesAndFactionTiersAfterAcknowledgement()
        {
            var save = new SaveData();
            RecordUpdates.Acknowledge(save, RecordUpdates.StoryPage);
            save.ClearedStage = 5;
            Assert.Greater(RecordUpdates.Unread(save, RecordUpdates.StoryPage), 0);
            save.Reputation.Add(new ReputationEntry { Id = Factions.All[0].Id, Points = Factions.Thresholds[1] });
            Assert.AreEqual(1, RecordUpdates.Unread(save, RecordUpdates.FactionPage));
            RecordUpdates.Acknowledge(save, RecordUpdates.FactionPage);
            Assert.AreEqual(0, RecordUpdates.Unread(save, RecordUpdates.FactionPage));
            save.Reputation[0].Points = Factions.Thresholds[2];
            Assert.AreEqual(1, RecordUpdates.Unread(save, RecordUpdates.FactionPage));
        }

        [Test]
        public void LegacyNullReadStateAndViewingPagesDoNotClaimRewards()
        {
            var save = new SaveData { ClearedStage = 5, ReadRecordUpdates = null };
            int rewards = Achievements.ClaimableCount(save) + Memories.UnseenCount(save) + (Ritual.CanComplete(save) ? 1 : 0);
            Assert.Greater(rewards, 0);
            foreach (var page in new[] { RecordUpdates.CodexPage, RecordUpdates.StoryPage, RecordUpdates.FactionPage })
                RecordUpdates.Acknowledge(save, page);
            var persisted = save.ReadRecordUpdates.ToList();
            save.ReadRecordUpdates = persisted;
            Assert.AreEqual(rewards, RecordUpdates.Count(save));
            Assert.IsEmpty(save.SeenMemories);
            Assert.AreEqual(0, save.Doo);
        }
    }
}
