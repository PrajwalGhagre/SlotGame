using System;
using System.Collections.Generic;
using SlotGame.Core.Random;
using SlotGame.Core.Reels;
using SlotGame.Core.Symbols;

namespace SlotGame.Core.Spin
{
    /// <summary>
    /// Turns random numbers into a SpinResult.
    /// Each reel independently picks a stop index from its own strip (weighted),
    /// then we read the visible window around that stop.
    /// </summary>
    public sealed class SpinOutcomeGenerator
    {
        private readonly ReelStrip[] _strips;
        private readonly IRandomProvider _random;

        public int ReelCount => _strips.Length;
        public int VisibleRows { get; }

        public IReadOnlyList<ReelStrip> Strips => _strips;

        public SpinOutcomeGenerator(IReadOnlyList<ReelStrip> strips, int visibleRows, IRandomProvider random)
        {
            if (strips == null) throw new ArgumentNullException(nameof(strips));
            if (strips.Count == 0) throw new ArgumentException("At least one reel is required.", nameof(strips));
            if (visibleRows < 1 || visibleRows % 2 == 0)
            {
                // An odd row count keeps the payline exactly in the middle.
                throw new ArgumentOutOfRangeException(nameof(visibleRows), "Use an odd number of rows (1, 3, 5).");
            }

            _strips = new ReelStrip[strips.Count];
            for (int i = 0; i < strips.Count; i++)
            {
                _strips[i] = strips[i] ?? throw new ArgumentException("Reel strips can not be null.");
            }

            VisibleRows = visibleRows;
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>Rolls the RNG once per reel and builds the result.</summary>
        public SpinResult Spin()
        {
            var stops = new int[_strips.Length];
            for (int reel = 0; reel < _strips.Length; reel++)
            {
                stops[reel] = _strips[reel].PickStopIndex(_random);
            }

            return CreateResult(stops);
        }

        /// <summary>
        /// Builds a result for known stop positions. Used by Spin() and by tests
        /// that want to check one exact outcome.
        /// </summary>
        public SpinResult CreateResult(int[] stopIndices)
        {
            if (stopIndices == null || stopIndices.Length != _strips.Length)
            {
                throw new ArgumentException("Need exactly one stop index per reel.", nameof(stopIndices));
            }

            int paylineRow = VisibleRows / 2;
            var window = new SymbolType[_strips.Length, VisibleRows];

            for (int reel = 0; reel < _strips.Length; reel++)
            {
                for (int row = 0; row < VisibleRows; row++)
                {
                    // Rows above the payline show higher strip indices (see ReelStrip docs).
                    int stripIndex = stopIndices[reel] + (paylineRow - row);
                    window[reel, row] = _strips[reel].GetSymbol(stripIndex);
                }
            }

            return new SpinResult(stopIndices, window);
        }
    }
}
