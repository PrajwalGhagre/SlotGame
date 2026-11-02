using System;

namespace SlotGame.Core.Random
{
    /// <summary>
    /// Default RNG, a thin wrapper around System.Random.
    /// Pass a seed to get a repeatable sequence (useful for tests and for
    /// reproducing a bug), or use the empty constructor for a fresh random seed.
    /// </summary>
    public sealed class SystemRandomProvider : IRandomProvider
    {
        private readonly System.Random _random;

        /// <summary>The seed this provider was created with, handy for logging.</summary>
        public int Seed { get; }

        /// <summary>
        /// Uses a GUID hash as the seed. Unlike Environment.TickCount, two providers
        /// created in the same millisecond still get different sequences.
        /// </summary>
        public SystemRandomProvider() : this(Guid.NewGuid().GetHashCode())
        {
        }

        public SystemRandomProvider(int seed)
        {
            Seed = seed;
            _random = new System.Random(seed);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive),
                    "maxExclusive must be greater than minInclusive.");
            }

            return _random.Next(minInclusive, maxExclusive);
        }

        public double NextDouble()
        {
            return _random.NextDouble();
        }
    }
}
