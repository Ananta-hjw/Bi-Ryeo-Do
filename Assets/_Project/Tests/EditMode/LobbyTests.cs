using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class LobbyTests
    {
        [Test]
        public void NewGameOnlyHasTheProtagonist()
        {
            var save = new SaveData();
            var speakers = Lobby.Speakers(save);
            CollectionAssert.AreEqual(new[] { "비류연" }, speakers);
            Assert.AreEqual("비류연", Lobby.Pick(save, 0).Speaker);
            Assert.AreEqual("비류연", Lobby.Pick(save, 5).Speaker);
        }

        [Test]
        public void CompanionsAndHeroineAppearWithProgress()
        {
            var save = new SaveData { ClearedStage = 10 };
            var speakers = Lobby.Speakers(save);
            CollectionAssert.Contains(speakers, "효룡");
            CollectionAssert.Contains(speakers, "나예린");
            // Every speaker in the rotation has something to say.
            for (int i = 0; i < speakers.Count; i++) Assert.AreEqual(speakers[i], Lobby.Pick(save, i).Speaker);
        }

        [Test]
        public void LinesRespectProgressAndBond()
        {
            var save = new SaveData { ClearedStage = 7 };
            Assert.IsFalse(Lobby.Available(save, "효룡").Any(l => l.MinBond > 0));
            Bonds.Add(save, "hyo_ryong", 3);
            Assert.IsTrue(Lobby.Available(save, "효룡").Any(l => l.MinBond == 1));
            Assert.IsFalse(Lobby.Available(save, "효룡").Any(l => l.MinCleared > 7));
        }

        [Test]
        public void EverySpeakerHasAtLeastOneAlwaysAvailableLine()
        {
            var names = Lobby.Lines.Select(l => l.Speaker).Distinct();
            foreach (var n in names)
                Assert.IsTrue(Lobby.Lines.Any(l => l.Speaker == n && l.MinCleared == 0 && l.MinBond == 0) || n == "나예린", n);
            Assert.IsTrue(Lobby.Lines.Any(l => l.Speaker == "나예린" && l.MinCleared <= Lobby.HeroineUnlockStage));
        }
    }
}
