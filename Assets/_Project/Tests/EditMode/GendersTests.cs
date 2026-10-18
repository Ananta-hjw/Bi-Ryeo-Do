using System.Linq;
using NUnit.Framework;
using BiRyeoDo.Core;

namespace BiRyeoDo.Tests
{
    public class GendersTests
    {
        [Test]
        public void WomenAreKnownAndUnclearDefaultsToMan()
        {
            Assert.IsTrue(Genders.IsFemale("na_yerin"));
            Assert.IsTrue(Genders.IsFemale("geomhu_ioksang"));
            Assert.IsFalse(Genders.IsFemale("player"));
            Assert.IsFalse(Genders.IsFemale("tiger"));
            Assert.IsFalse(Genders.IsFemale(null));
        }

        [Test]
        public void EveryListedWomanIsARealArtKey()
        {
            foreach (var key in new[] { "na_yerin", "geomhu_ioksang", "sunyeo", "ma_haryeong", "mask_gongpo", "mask_jeolmang", "mask_bitan" })
                Assert.IsTrue(EarlyGameContent.Enemies.Any(e => e.ArtKey == key) || EarlyGameContent.Companions.Any(c => c.Id == key), key);
        }
    }
}
