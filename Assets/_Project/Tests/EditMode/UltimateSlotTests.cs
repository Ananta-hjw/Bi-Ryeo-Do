using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class UltimateSlotTests
    {
        private static ArtData[] Ultimates() { return EarlyGameContent.Arts.Where(a => a.OwnerId == null && Progression.IsUltimate(a)).ToArray(); }
        private static ArtData[] Normals() { return EarlyGameContent.Arts.Where(a => a.OwnerId == null && !Progression.IsUltimate(a)).ToArray(); }

        [Test]
        public void ContentHasSeveralUltimatesAndOrdinaryArts()
        {
            Assert.GreaterOrEqual(Ultimates().Length, 2);
            Assert.GreaterOrEqual(Normals().Length, 5);
        }

        [Test]
        public void UltimateHasItsOwnSlotAndDoesNotUseAnOrdinaryOne()
        {
            var save = new SaveData();
            string replaced;
            foreach (var a in Normals().Take(Progression.MaxEquippedArts)) Assert.IsTrue(Progression.EquipArt(save, a, out replaced));
            Assert.IsFalse(Progression.EquipArt(save, Normals()[Progression.MaxEquippedArts], out replaced));   // ordinary slots full
            Assert.IsTrue(Progression.EquipArt(save, Ultimates()[0], out replaced));                           // 필살기 still fits
            Assert.AreEqual(Progression.MaxEquippedArts, Progression.EquippedCount(save, false));
            Assert.AreEqual(1, Progression.EquippedCount(save, true));
        }

        [Test]
        public void EquippingASecondUltimateReplacesTheFirst()
        {
            var save = new SaveData();
            string replaced;
            Progression.EquipArt(save, Ultimates()[0], out replaced);
            Assert.IsNull(replaced);
            Assert.IsTrue(Progression.EquipArt(save, Ultimates()[1], out replaced));
            Assert.AreEqual(Ultimates()[0].Id, replaced);
            Assert.AreEqual(1, Progression.EquippedCount(save, true));
            CollectionAssert.DoesNotContain(save.EquippedArts, Ultimates()[0].Id);
            CollectionAssert.Contains(save.EquippedArts, Ultimates()[1].Id);
        }

        [Test]
        public void CompanionArtsAndDuplicatesAreRejected()
        {
            var save = new SaveData();
            string replaced;
            var companionArt = EarlyGameContent.Arts.First(a => a.OwnerId != null);
            Assert.IsFalse(Progression.EquipArt(save, companionArt, out replaced));
            Assert.IsTrue(Progression.EquipArt(save, Normals()[0], out replaced));
            Assert.IsFalse(Progression.EquipArt(save, Normals()[0], out replaced));
            Assert.AreEqual(1, save.EquippedArts.Count);
        }
    }
}
