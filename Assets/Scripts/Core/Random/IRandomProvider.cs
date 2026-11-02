namespace SlotGame.Core.Random
{
    /// <summary>
    /// Source of random numbers for the slot machine.
    /// Everything that needs randomness asks this interface instead of calling
    /// System.Random or UnityEngine.Random directly. That gives two things:
    /// 1. Tests can plug in a seeded or scripted provider and get repeatable results.
    /// 2. The RNG can be swapped (for example a crypto RNG for a real money game)
    ///    without touching any game code.
    /// </summary>
    public interface IRandomProvider
    {
        /// <summary>Returns an integer in the range [minInclusive, maxExclusive).</summary>
        int NextInt(int minInclusive, int maxExclusive);

        /// <summary>Returns a double in the range [0, 1).</summary>
        double NextDouble();
    }
}
