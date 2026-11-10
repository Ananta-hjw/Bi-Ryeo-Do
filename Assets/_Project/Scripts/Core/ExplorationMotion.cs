using System;

namespace BiRyeoDo.Core
{
    // Order matches the top-to-bottom rows in the exploration atlas.
    public enum ExplorationFacing { Down, Left, Right, Up }

    /// <summary>Animation state driven by actual displacement, independent of the engine.</summary>
    public sealed class ExplorationMotion
    {
        public const float FramesPerSecond = 8f;
        public ExplorationFacing Facing { get; private set; }
        public int Frame { get; private set; }
        private float _elapsed;
        private bool _moving;
        /// <summary>The other axis must be this much larger before the facing switches axis while walking.</summary>
        public const float AxisSwitchRatio = 1.25f;
        private static readonly int[] WalkCycle = { 1, 2, 3, 2 };

        public void Advance(float dx, float dy, float seconds)
        {
            if (float.IsNaN(dx) || float.IsNaN(dy) || float.IsNaN(seconds)
                || float.IsInfinity(dx) || float.IsInfinity(dy) || float.IsInfinity(seconds)
                || seconds < 0f) { Stop(); return; }
            if (Math.Abs(dx) + Math.Abs(dy) < .001f) { Stop(); return; }
            float ax = Math.Abs(dx), ay = Math.Abs(dy);
            bool horizontal = ax > ay;
            // Near a diagonal keep the axis the figure already faces, so the row does not flicker and restart the walk cycle.
            if (_moving)
            {
                bool wasHorizontal = Facing == ExplorationFacing.Left || Facing == ExplorationFacing.Right;
                if (wasHorizontal && ax * AxisSwitchRatio >= ay) horizontal = true;
                else if (!wasHorizontal && ay * AxisSwitchRatio >= ax) horizontal = false;
            }
            var next = horizontal
                ? (dx < 0f ? ExplorationFacing.Left : ExplorationFacing.Right)
                : (dy < 0f ? ExplorationFacing.Down : ExplorationFacing.Up);
            _moving = true;
            if (next != Facing) _elapsed = 0f;
            Facing = next;
            Frame = WalkCycle[(int)(_elapsed * FramesPerSecond) % WalkCycle.Length];
            _elapsed = (_elapsed + Math.Min(seconds, .1f)) % (WalkCycle.Length / FramesPerSecond);
        }

        public void Stop() { Frame = 0; _elapsed = 0f; _moving = false; }
        public int AtlasIndex { get { return (int)Facing * 4 + Frame; } }
    }
}
