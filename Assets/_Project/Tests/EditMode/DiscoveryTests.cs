using System.Linq;
using NUnit.Framework;
using BiRyeoDo.Core;

namespace BiRyeoDo.Tests
{
    public class DiscoveryTests
    {
        [Test]
        public void NewGameStartsWearingTheStartKitAndTheFreeBreathMethod()
        {
            var save = new SaveData();
            Inventory.EnsureStartKit(save);
            foreach (var stack in EarlyGameContent.StartKit)
            {
                var item = EarlyGameContent.FindItem(stack.Id);
                if (item.IsGear) Assert.IsTrue(save.EquippedGear.Contains(stack.Id), stack.Id);
            }
            CollectionAssert.Contains(save.LearnedInner, "breath");
            CollectionAssert.Contains(save.EquippedInner, "breath");
            Inventory.EnsureStartKit(save);   // only once
            Assert.AreEqual(1, save.EquippedInner.Count);
        }

        [Test]
        public void UnlockedButUnlearnedArtsShowADotUntilTheirPageIsSeen()
        {
            var save = new SaveData { ClearedStage = 0 };
            Discovery.Sync(save);
            Assert.AreEqual(0, Discovery.Count(save));
            save.ClearedStage = 6;   // unlocks arts and 심법 on the way
            int before = Discovery.NewArts(save).Count + Discovery.NewInner(save).Count;
            Assert.Greater(before, 0);
            Assert.IsTrue(Discovery.NewArts(save).All(a => !save.LearnedArts.Contains(a.Id)));

            Discovery.MarkArtsSeen(save);
            Assert.AreEqual(0, Discovery.NewArts(save).Count);
            Assert.AreEqual(Discovery.NewInner(save).Count, Discovery.Count(save));   // the 심법 page still counts
            Discovery.MarkInnerSeen(save);
            Assert.AreEqual(0, Discovery.Count(save));

            save.ClearedStage = 30;   // later unlocks light the dot again
            Assert.Greater(Discovery.Count(save), 0);
        }

        [Test]
        public void LearnedArtsDoNotShowADot()
        {
            var save = new SaveData { ClearedStage = 6 };
            var art = Discovery.NewArts(save).First();
            save.LearnedArts.Add(art.Id);
            Assert.IsFalse(Discovery.NewArts(save).Contains(art));
        }

        [Test]
        public void OldSavesAreNotFloodedWithDots()
        {
            var save = new SaveData { ClearedStage = 12 };
            Assert.Greater(Discovery.Count(save), 0);
            Discovery.Sync(save);
            Assert.AreEqual(0, Discovery.Count(save));
            Assert.IsTrue(save.DiscoveryInit);
        }
    }
}
