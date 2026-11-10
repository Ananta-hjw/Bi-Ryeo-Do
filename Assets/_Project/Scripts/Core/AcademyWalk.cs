using System;

namespace BiRyeoDo.Core
{
    /// <summary>Prototype campus layout; coordinates are game staging, not a canonical floor plan.</summary>
    public sealed class AcademyWalk
    {
        public const float Width = 2400f, Height = 1600f, Speed = 300f, Radius = 24f;
        // Reserve the full 140px figure and its 1.6-width name plate / 1.3-height top edge.
        public const float FigureWidth = 140f, FigureHeight = 160f;
        public const float SideClearance = FigureWidth * .8f, TopClearance = FigureHeight * 1.3f;
        public float X { get; private set; } = 1200f;
        public float Y { get; private set; } = 400f;
        public static readonly float[] FacilityX = { 500f, 1200f, 1900f };
        public static readonly float[] FacilityY = { 1100f, 1100f, 1100f };

        public static bool Walkable(float x, float y)
        {
            if (x < SideClearance || y < Radius || x > Width - SideClearance || y > Height - TopClearance) return false;
            for (int i = 0; i < 3; i++)
                if (x > FacilityX[i] - 200f - Radius && x < FacilityX[i] + 200f + Radius
                    && y > 1150f - Radius && y < 1450f + Radius) return false;
            if (AcademyResidents.Blocks(x, y)) return false;
            // A small garden separates the two paths through the courtyard.
            return !(x > 880f - Radius && x < 1520f + Radius && y > 650f - Radius && y < 800f + Radius);
        }

        public void Move(float x, float y, float seconds)
        {
            if (float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(seconds)
                || float.IsInfinity(x) || float.IsInfinity(y) || float.IsInfinity(seconds) || seconds <= 0f) return;
            double length = Math.Sqrt((double)x * x + (double)y * y);
            if (length > 1d) { x = (float)(x / length); y = (float)(y / length); }
            float distance = Speed * Math.Min(seconds, .1f);
            int steps = Math.Max(1, (int)Math.Ceiling(distance / 8f));
            for (int i = 0; i < steps; i++)
            {
                float dx = x * distance / steps, dy = y * distance / steps;
                if (Walkable(X + dx, Y)) X += dx;
                if (Walkable(X, Y + dy)) Y += dy;
            }
        }

        public int NearbyFacility()
        {
            for (int i = 0; i < 3; i++)
            {
                float dx = X - FacilityX[i], dy = Y - FacilityY[i];
                if (dx * dx + dy * dy <= 180f * 180f) return i;
            }
            return -1;
        }
    }
}
