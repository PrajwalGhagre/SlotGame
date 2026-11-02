using System.Collections.Generic;
using SlotGame.Core.Payouts;
using SlotGame.Core.Random;
using SlotGame.Core.Reels;
using SlotGame.Core.Spin;
using SlotGame.Core.Symbols;

namespace SlotGame.Tests
{
    /// <summary>
    /// RNG that returns numbers from a fixed script, so a test can force an exact outcome.
    /// </summary>
    internal sealed class ScriptedRandomProvider : IRandomProvider
    {
        private readonly Queue<int> _ints;

        public ScriptedRandomProvider(params int[] ints)
        {
            _ints = new Queue<int>(ints);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            int value = _ints.Count > 0 ? _ints.Dequeue() : minInclusive;
            return System.Math.Max(minInclusive, System.Math.Min(maxExclusive - 1, value));
        }

        public double NextDouble()
        {
            return 0.0;
        }
    }

    internal static class TestData
    {
        /// <summary>The same numbers the shipped config uses.</summary>
        public static readonly SymbolRule[] DefaultRules =
        {
            new SymbolRule(SymbolType.Cherry, 3, 9, 2),
            new SymbolRule(SymbolType.Bell, 3, 7, 5),
            new SymbolRule(SymbolType.Bar, 2, 6, 15),
            new SymbolRule(SymbolType.Seven, 2, 4, 30),
            new SymbolRule(SymbolType.Wild, 1, 3, 100)
        };

        public static Paytable CreatePaytable()
        {
            return new Paytable(DefaultRules, 2);
        }

        public static PayoutCalculator CreateCalculator()
        {
            return new PayoutCalculator(CreatePaytable(), BonusRules.Default);
        }

        /// <summary>A 3x3 result built straight from symbols, top row first.</summary>
        public static SpinResult Window(SymbolType[] top, SymbolType[] middle, SymbolType[] bottom)
        {
            var window = new SymbolType[3, 3];
            for (int reel = 0; reel < 3; reel++)
            {
                window[reel, 0] = top[reel];
                window[reel, 1] = middle[reel];
                window[reel, 2] = bottom[reel];
            }

            return new SpinResult(new[] { 0, 0, 0 }, window);
        }

        public static SpinOutcomeGenerator CreateGenerator(IRandomProvider random, int layoutSeed = 1234)
        {
            var strips = new List<ReelStrip>();
            for (int reel = 0; reel < 3; reel++)
            {
                strips.Add(ReelStripBuilder.Build(DefaultRules, new SystemRandomProvider(layoutSeed + reel)));
            }

            return new SpinOutcomeGenerator(strips, 3, random);
        }
    }
}
