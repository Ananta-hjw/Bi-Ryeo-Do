using System;

namespace BiRyeoDo.Core
{
    /// <summary>Player options saved with the game. Public fields for JsonUtility.</summary>
    [Serializable]
    public sealed class GameSettings
    {
        public float BgmVolume = 0.7f;
        public float SfxVolume = 1f;
        public bool Vibration = true;
        public int BattleSpeed = 1;   // 1-3, the speed the battle screen starts at

        public const float VolumeStep = 0.1f;

        public static float StepVolume(float current, int direction)
        {
            float v = (float)Math.Round(current + VolumeStep * direction, 1);
            return v < 0f ? 0f : v > 1f ? 1f : v;
        }

        public int NextSpeed() { return BattleSpeed >= 3 ? 1 : BattleSpeed + 1; }
    }
}
