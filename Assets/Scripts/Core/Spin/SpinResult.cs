using System;
using System.Collections.Generic;
using SlotGame.Core.Symbols;

namespace SlotGame.Core.Spin
{
    /// <summary>
    /// The outcome of one spin, decided by the RNG before any reel starts moving
    /// (that is how real machines work too, the animation only shows a result
    /// that already exists).
    ///
    /// Rows are numbered from the top: row 0 is the top row. With three visible
    /// rows the payline is row 1, the middle one.
    /// </summary>
    public sealed class SpinResult
    {
        private readonly SymbolType[,] _window;
        private readonly int[] _stopIndices;

        public int ReelCount { get; }
        public int RowCount { get; }

        /// <summary>Row index of the payline (the middle row).</summary>
        public int PaylineRow { get; }

        public IReadOnlyList<int> StopIndices => _stopIndices;

        /// <param name="stopIndices">Strip index each reel stops on (shown on the payline).</param>
        /// <param name="window">Visible symbols, indexed [reel, row] with row 0 at the top.</param>
        public SpinResult(int[] stopIndices, SymbolType[,] window)
        {
            if (stopIndices == null) throw new ArgumentNullException(nameof(stopIndices));
            if (window == null) throw new ArgumentNullException(nameof(window));
            if (window.GetLength(0) != stopIndices.Length)
            {
                throw new ArgumentException("The window needs one column per reel.");
            }

            _stopIndices = (int[])stopIndices.Clone();
            _window = (SymbolType[,])window.Clone();
            ReelCount = window.GetLength(0);
            RowCount = window.GetLength(1);
            PaylineRow = RowCount / 2;
        }

        public SymbolType GetSymbol(int reel, int row)
        {
            return _window[reel, row];
        }

        /// <summary>The symbol each reel shows on the payline, left to right.</summary>
        public SymbolType[] GetPaylineSymbols()
        {
            var line = new SymbolType[ReelCount];
            for (int reel = 0; reel < ReelCount; reel++)
            {
                line[reel] = _window[reel, PaylineRow];
            }

            return line;
        }

        public override string ToString()
        {
            return "Payline: " + string.Join(" | ", GetPaylineSymbols());
        }
    }
}
