using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class GameSettingsTests
    {
        [Test]
        public void VolumeStepsStayInRange()
        {
            Assert.AreEqual(0.8f, GameSettings.StepVolume(0.7f, 1), 0.001f);
            Assert.AreEqual(1f, GameSettings.StepVolume(1f, 1), 0.001f);
            Assert.AreEqual(0f, GameSettings.StepVolume(0f, -1), 0.001f);
            Assert.AreEqual(0.3f, GameSettings.StepVolume(0.4f, -1), 0.001f);
        }

        [Test]
        public void SpeedCyclesOneToThree()
        {
            var s = new GameSettings();
            Assert.AreEqual(2, s.NextSpeed());
            s.BattleSpeed = 3;
            Assert.AreEqual(1, s.NextSpeed());
        }

        [Test]
        public void NewSaveHasDefaultSettings()
        {
            var save = new SaveData();
            Assert.IsNotNull(save.Settings);
            Assert.IsTrue(save.Settings.Vibration);
        }
    }
}
