using System;

namespace SlotGame.Core.Symbols
{
    /// <summary>
    /// Plain data describing how one symbol behaves in the maths of the game.
    /// Built from a SymbolDefinition asset at runtime, or by hand in tests.
    /// </summary>
    public readonly struct SymbolRule
    {
        /// <summary>Which symbol this rule is for.</summary>
        public SymbolType Type { get; }

        /// <summary>How many times the symbol is printed on each reel strip (what the player sees).</summary>
        public int CopiesOnStrip { get; }

        /// <summary>
        /// Weight of every stop that shows this symbol. A higher weight means the
        /// RNG lands on it more often. This is the "virtual reel" idea used by real
        /// slot machines: the visible strip stays short and tidy while the odds are
        /// tuned separately.
        /// </summary>
        public int StopWeight { get; }

        /// <summary>Bet multiplier paid when all reels show this symbol on the payline.</summary>
        public int PayoutMultiplier { get; }

        public SymbolRule(SymbolType type, int copiesOnStrip, int stopWeight, int payoutMultiplier)
        {
            if (copiesOnStrip < 1) throw new ArgumentOutOfRangeException(nameof(copiesOnStrip));
            if (stopWeight < 1) throw new ArgumentOutOfRangeException(nameof(stopWeight));
            if (payoutMultiplier < 0) throw new ArgumentOutOfRangeException(nameof(payoutMultiplier));

            Type = type;
            CopiesOnStrip = copiesOnStrip;
            StopWeight = stopWeight;
            PayoutMultiplier = payoutMultiplier;
        }

        public override string ToString()
        {
            return $"{Type} (copies {CopiesOnStrip}, weight {StopWeight}, pays x{PayoutMultiplier})";
        }
    }
}
