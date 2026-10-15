using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class CodexTests
    {
        [Test]
        public void ItemsAreRecordedWhenAddedAndOnlyOnce()
        {
            var save = new SaveData();
            Inventory.Add(save, "bear_gall", 2);
            Inventory.Add(save, "bear_gall", 1);
            CollectionAssert.AreEqual(new[] { "bear_gall" }, save.SeenItems);
        }

        [Test]
        public void EnemiesOutsideTheStoryListAreIgnored()
        {
            var save = new SaveData();
            Codex.SeeEnemy(save, "tower5_0");
            Codex.SeeEnemy(save, EarlyGameContent.Enemies[0].Id);
            Codex.SeeEnemy(save, EarlyGameContent.Enemies[0].Id);
            Assert.AreEqual(1, save.SeenEnemies.Count);
        }

        [Test]
        public void SyncBackfillsFromClearedStagesAndBag()
        {
            var save = new SaveData { ClearedStage = 3 };
            save.Items.Add(new ItemStack("ginseng", 1));
            Codex.Sync(save);
            Assert.IsTrue(Codex.HasItem(save, "ginseng"));
            foreach (var st in EarlyGameContent.Stages.Where(s => s.Number <= 3))
                foreach (var enc in st.Encounters) foreach (var id in enc) Assert.IsTrue(Codex.HasEnemy(save, id), id);
            Assert.IsFalse(Codex.HasEnemy(save, EarlyGameContent.Stages.Last().Encounters.Last().Last()));
        }

        [Test]
        public void GachaPullsCountAndRecordDuplicates()
        {
            var save = new SaveData { Doo = 100000 };
            Gacha.Pull(save, 10, new SeededRandom(5));
            Assert.AreEqual(10, save.GachaPulls);
            Assert.GreaterOrEqual(save.SeenItems.Count, 1);
        }

        [Test]
        public void AchievementIdsAreUniqueAndRewardsExist()
        {
            Assert.AreEqual(Achievements.All.Length, Achievements.All.Select(a => a.Id).Distinct().Count());
            foreach (var a in Achievements.All)
            {
                Assert.Greater(a.RewardDoo, 0, a.Id);
                Assert.Greater(Achievements.TargetOf(a), 0, a.Id);
                if (a.RewardItemId != null) Assert.IsNotNull(EarlyGameContent.FindItem(a.RewardItemId), a.Id);
            }
        }

        [Test]
        public void ClaimingPaysOnceAndOnlyWhenDone()
        {
            var save = new SaveData { ClearedStage = 5, Doo = 0 };
            Assert.IsNull(Achievements.Claim(save, "stage_10"));
            Assert.AreEqual(1, Achievements.ClaimableCount(save) >= 1 ? 1 : 0);
            var note = Achievements.Claim(save, "stage_5");
            Assert.IsNotNull(note);
            Assert.AreEqual(800, save.Doo);
            Assert.IsNull(Achievements.Claim(save, "stage_5"));
            Assert.AreEqual(800, save.Doo);
        }

        [Test]
        public void ClaimAllCollectsEverythingReady()
        {
            var save = new SaveData { ClearedStage = 20, Level = 30 };
            int ready = Achievements.ClaimableCount(save);
            Assert.Greater(ready, 3);
            Assert.IsNotNull(Achievements.ClaimAll(save));
            Assert.AreEqual(0, Achievements.ClaimableCount(save));
            Assert.AreEqual(ready, save.ClaimedAchievements.Count);
            Assert.IsNull(Achievements.ClaimAll(save));
            Assert.AreEqual(1, Inventory.Count(save, "ginseng"));
        }
    }
}
