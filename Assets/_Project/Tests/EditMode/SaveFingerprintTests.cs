using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class SaveFingerprintTests
    {
        [Test]
        public void ChangesWithWhatTheLobbyShowsAndNotOtherwise()
        {
            var save = new SaveData { ClearedStage = 10, Level = 8 };
            long a = SaveFingerprint.Of(save);
            Assert.AreEqual(a, SaveFingerprint.Of(save), "stable while nothing changes");
            save.Doo += 1; long b = SaveFingerprint.Of(save); Assert.AreNotEqual(a, b);
            save.DoneQuests.Add("q"); long c = SaveFingerprint.Of(save); Assert.AreNotEqual(b, c);
            save.ClaimedAchievements.Add("x"); long d = SaveFingerprint.Of(save); Assert.AreNotEqual(c, d);
            save.ReadRecordUpdates.Add("codex:a"); long e = SaveFingerprint.Of(save); Assert.AreNotEqual(d, e);
            save.DailyProgress.Add(0); long f = SaveFingerprint.Of(save);
            save.DailyProgress[0] = 3; Assert.AreNotEqual(f, SaveFingerprint.Of(save), "daily mission progress moves the badge");
            Assert.AreEqual(0, SaveFingerprint.Of(null));
        }
    }
}
