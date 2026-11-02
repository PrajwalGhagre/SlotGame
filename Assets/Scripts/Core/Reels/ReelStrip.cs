using System;
using System.Collections.Generic;
using SlotGame.Core.Random;
using SlotGame.Core.Symbols;

namespace SlotGame.Core.Reels
{
    /// <summary>
    /// One reel's strip: the looping list of symbols printed on the reel,
    /// plus a weight for every stop on it.
    ///
    /// Indexing rule used everywhere in this project:
    /// when the reel rests on stop <c>s</c>, the payline (middle row) shows
    /// <c>strip[s]</c>, the row above shows <c>strip[s + 1]</c> and the row below
    /// shows <c>strip[s - 1]</c>. Indices wrap around because a reel is a loop.
    /// </summary>
    public sealed class ReelStrip
    {
        private readonly SymbolType[] _symbols;
        private readonly WeightedRandomPicker<int> _stopPicker;

        public int Length => _symbols.Length;

        public IReadOnlyList<SymbolType> Symbols => _symbols;

        public ReelStrip(IReadOnlyList<SymbolType> symbols, IReadOnlyList<int> stopWeights)
        {
            if (symbols == null) throw new ArgumentNullException(nameof(symbols));
            if (stopWeights == null) throw new ArgumentNullException(nameof(stopWeights));
            if (symbols.Count == 0) throw new ArgumentException("A reel strip needs at least one symbol.");
            if (symbols.Count != stopWeights.Count)
            {
                throw new ArgumentException("Every stop needs exactly one weight.");
            }

            _symbols = new SymbolType[symbols.Count];
            var stopIndices = new int[symbols.Count];
            for (int i = 0; i < symbols.Count; i++)
            {
                _symbols[i] = symbols[i];
                stopIndices[i] = i;
            }

            _stopPicker = new WeightedRandomPicker<int>(stopIndices, stopWeights);
        }

        /// <summary>Returns the symbol at any index. Negative and large indices wrap around.</summary>
        public SymbolType GetSymbol(int index)
        {
            return _symbols[WrapIndex(index)];
        }

        /// <summary>Wraps any integer into the range [0, Length).</summary>
        public int WrapIndex(int index)
        {
            int wrapped = index % _symbols.Length;
            return wrapped < 0 ? wrapped + _symbols.Length : wrapped;
        }

        /// <summary>Uses the RNG to choose where this reel stops, respecting the stop weights.</summary>
        public int PickStopIndex(IRandomProvider random)
        {
            return _stopPicker.Pick(random);
        }

        /// <summary>Probability (0..1) that this reel stops on the given index.</summary>
        public double GetStopProbability(int stopIndex)
        {
            return _stopPicker.GetProbability(WrapIndex(stopIndex));
        }
    }
}
