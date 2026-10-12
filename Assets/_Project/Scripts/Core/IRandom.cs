using System;

namespace BiRyeoDo.Core
{
    /// <summary>Injectable RNG so battles are reproducible in tests and replays.</summary>
    public interface IRandom
    {
        /// <summary>Returns a value in [0, 1).</summary>
        double NextDouble();
    }

    public sealed class SeededRandom : IRandom
    {
        private readonly Random _random;

        public SeededRandom(int seed) { _random = new Random(seed); }

        public double NextDouble() { return _random.NextDouble(); }
    }
}
