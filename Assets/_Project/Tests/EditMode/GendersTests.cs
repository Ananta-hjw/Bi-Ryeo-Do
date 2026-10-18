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
            Assert.IsTrue(Genders.IsFemale("chu_gayeon"));
            Assert.IsFalse(Genders.IsFemale("player"));
            Assert.IsFalse(Genders.IsFemale("yeomdo"));
            Assert.IsFalse(Genders.IsFemale("tiger"));
            Assert.IsFalse(Genders.IsFemale(null));
        }

        [Test]
        public void AssassinTypesGetTheAssassinFigureBeforeGender()
        {
            Assert.AreEqual("default_assassin", Genders.DefaultFigure("hogol_bandit"));
            Assert.AreEqual("default_assassin", Genders.DefaultFigure("mu_jeong"));
            Assert.AreEqual("default_woman", Genders.DefaultFigure("na_yerin"));
            Assert.AreEqual("default_man", Genders.DefaultFigure("tiger"));
            foreach (var key in new[] { "hogol_bandit", "yonggol_bandit", "heukrang_bandit", "baegin_jagaek", "ssanggwi", "chohon_gwigeom", "cheonsal", "jisal", "ssangsal_hapgyeok", "salgwi_daewon", "mu_jeong", "suda_bokmyeon", "baek_son_gosu" })
                Assert.IsTrue(EarlyGameContent.Enemies.Any(e => e.ArtKey == key), key);
        }

        [Test]
        public void EveryListedWomanIsARealArtKey()
        {
            foreach (var key in new[] { "na_yerin", "geomhu_ioksang", "sunyeo", "ma_haryeong", "mask_gongpo", "mask_jeolmang", "mask_bitan", "chu_gayeon", "eonga_yeong", "baek_sayeong" })
                Assert.IsTrue(EarlyGameContent.Enemies.Any(e => e.ArtKey == key) || EarlyGameContent.Companions.Any(c => c.Id == key), key);
        }
    }
}
