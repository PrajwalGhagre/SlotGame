using System.Collections.Generic;
using SlotGame.Core.Symbols;

namespace SlotGame.Core.Payouts
{
    /// <summary>What the payline paid for a single spin.</summary>
    public sealed class WinResult
    {
        private static readonly SlotPosition[] NoPositions = new SlotPosition[0];

        public bool IsWin { get; }

        /// <summary>The symbol the line counts as (Wild only when every reel showed a Wild).</summary>
        public SymbolType WinningSymbol { get; }

        /// <summary>Paytable multiplier for the symbol, before the wild bonus.</summary>
        public int SymbolMultiplier { get; }

        /// <summary>True when a Wild had to stand in for the winning symbol.</summary>
        public bool UsedWild { get; }

        /// <summary>Final coins won for this line at the given bet (before any free spin multiplier).</summary>
        public int Payout { get; }

        public IReadOnlyList<SlotPosition> WinningPositions { get; }

        private WinResult(bool isWin, SymbolType winningSymbol, int symbolMultiplier, bool usedWild, int payout,
            IReadOnlyList<SlotPosition> positions)
        {
            IsWin = isWin;
            WinningSymbol = winningSymbol;
            SymbolMultiplier = symbolMultiplier;
            UsedWild = usedWild;
            Payout = payout;
            WinningPositions = positions ?? NoPositions;
        }

        public static WinResult Loss()
        {
            return new WinResult(false, default, 0, false, 0, NoPositions);
        }

        public static WinResult Win(SymbolType symbol, int symbolMultiplier, bool usedWild, int payout,
            IReadOnlyList<SlotPosition> positions)
        {
            return new WinResult(true, symbol, symbolMultiplier, usedWild, payout, positions);
        }

        public override string ToString()
        {
            return IsWin ? $"WIN {WinningSymbol} x{SymbolMultiplier}{(UsedWild ? " (wild)" : "")} = {Payout}" : "No win";
        }
    }
}
