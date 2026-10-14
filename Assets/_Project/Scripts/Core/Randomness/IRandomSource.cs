using System;

namespace CubeTower.Core.Randomness
{
    /// <summary>
    /// Injectable randomness, makes gameplay deterministic in tests.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>Returns a random value in [min, max].</summary>
        float Range(float min, float max);
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SystemRandomSource() : this(Environment.TickCount)
        {
        }

        public SystemRandomSource(int seed)
        {
            _random = new Random(seed);
        }

        public float Range(float min, float max)
        {
            if (max < min)
                (min, max) = (max, min);

            return min + (float)_random.NextDouble() * (max - min);
        }
    }
}
