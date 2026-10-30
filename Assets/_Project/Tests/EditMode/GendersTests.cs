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

        [TestCase("jo_yeonil", "default_man")]
        [TestCase("maeng_yeonho", "default_man")]
        [TestCase("salhon_sipgwi", "default_assassin")]
        [TestCase("amryong_salsu", "default_assassin")]
        [TestCase("hogol_archer", "default_assassin")]
        [TestCase("gwan_seolji_story", "default_woman")]
        [TestCase("merchant_yeo", "default_woman")]
        [TestCase("silent_duet", "default_woman")]
        [TestCase("mothers_fire_ice", "default_woman")]
        [TestCase("changranga", "default_man")]
        public void UnillustratedOpponentsUseTheirOwnFallbackClassification(string id, string expected)
        {
            var enemy = EarlyGameContent.FindEnemy(id);
            Assert.AreEqual(expected, Genders.DefaultFigure(enemy.ArtKey));
        }

        [TestCase("jo_yeonil")]
        [TestCase("jin_pae")]
        [TestCase("ilyeon")]
        [TestCase("maeng_yeonho")]
        [TestCase("salhon_sipgwi")]
        [TestCase("admit_eonga")]
        [TestCase("changranga")]
        public void MissingArtworkNeverBorrowsAnUnrelatedCharactersKey(string id)
        {
            Assert.AreEqual(id, EarlyGameContent.FindEnemy(id).ArtKey);
        }

        [TestCase("hyo_ryong_rope", "hyo_ryong")]
        [TestCase("hwan_geomseong", "geom_seong")]
        [TestCase("hwan_family_duty", "im_deokseong")]
        [TestCase("im_deokseong_story", "im_deokseong")]
        [TestCase("yong_cheongmyeong_vote", "yong_cheonmyeong")]
        [TestCase("yeongryeong_ice", "dokgo_ryeong")]
        public void SameCharacterPortrayalsKeepTheirCanonicalArtwork(string id, string expected)
        {
            Assert.AreEqual(expected, EarlyGameContent.FindEnemy(id).ArtKey);
        }

        [Test]
        public void EveryListedWomanIsARealArtKey()
        {
            foreach (var key in new[] { "na_yerin", "geomhu_ioksang", "sunyeo", "ma_haryeong", "mask_gongpo", "mask_jeolmang", "mask_bitan", "chu_gayeon", "eonga_yeong", "baek_sayeong" })
                Assert.IsTrue(EarlyGameContent.Enemies.Any(e => e.ArtKey == key) || EarlyGameContent.Companions.Any(c => c.Id == key), key);
        }
    }
}
