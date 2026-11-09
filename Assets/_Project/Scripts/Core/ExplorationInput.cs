using System;

namespace BiRyeoDo.Core
{
    public static class ExplorationInput
    {
        public const float DeadZone = .15f;

        /// <summary>Radial dead zone with a continuous remaining speed range.</summary>
        public static void Stick(float x, float y, out float horizontal, out float vertical)
        {
            horizontal = vertical = 0f;
            if (float.IsNaN(x) || float.IsNaN(y) || float.IsInfinity(x) || float.IsInfinity(y)) return;
            double magnitude = Math.Sqrt((double)x * x + (double)y * y);
            if (magnitude <= DeadZone) return;
            double speed = (Math.Min(1d, magnitude) - DeadZone) / (1d - DeadZone);
            horizontal = (float)(x / magnitude * speed);
            vertical = (float)(y / magnitude * speed);
        }

        /// <summary>World offset for a centered canvas, clamped at map edges.</summary>
        public static float CameraOffset(float position, float viewportSize, float mapSize)
        {
            float half = Math.Min(Math.Max(0f, viewportSize) * .5f, mapSize * .5f);
            float center = Math.Max(half, Math.Min(mapSize - half, position));
            return mapSize * .5f - center;
        }
    }
}
