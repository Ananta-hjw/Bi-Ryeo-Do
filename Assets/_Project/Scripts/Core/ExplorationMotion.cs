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
        private static readonly int[] WalkCycle = { 1, 2, 3, 2 };

        public void Advance(float dx, float dy, float seconds)
        {
            if (float.IsNaN(dx) || float.IsNaN(dy) || float.IsNaN(seconds)
                || float.IsInfinity(dx) || float.IsInfinity(dy) || float.IsInfinity(seconds)
                || seconds < 0f) { Stop(); return; }
            if (Math.Abs(dx) + Math.Abs(dy) < .001f) { Stop(); return; }
            var next = Math.Abs(dx) > Math.Abs(dy)
                ? (dx < 0f ? ExplorationFacing.Left : ExplorationFacing.Right)
                : (dy < 0f ? ExplorationFacing.Down : ExplorationFacing.Up);
            if (next != Facing) _elapsed = 0f;
            Facing = next;
            Frame = WalkCycle[(int)(_elapsed * FramesPerSecond) % WalkCycle.Length];
            _elapsed = (_elapsed + Math.Min(seconds, .1f)) % (WalkCycle.Length / FramesPerSecond);
        }

        public void Stop() { Frame = 0; _elapsed = 0f; }
        public int AtlasIndex { get { return (int)Facing * 4 + Frame; } }
    }
}
