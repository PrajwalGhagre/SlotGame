using System;
using SlotGame.Core.Symbols;

namespace SlotGame.Core.Payouts
{
    /// <summary>
    /// Settings for the bonus features.
    /// Wild: substitutes for any symbol on the payline.
    /// Free spins: if enough scatter symbols are visible anywhere in the window
    /// (not only on the payline) the player gets free spins with a multiplier.
    /// </summary>
    public sealed class BonusRules
    {
        public SymbolType WildSymbol { get; }
        public SymbolType ScatterSymbol { get; }
        public int ScattersNeededForFreeSpins { get; }
        public int FreeSpinsAwarded { get; }
        public int FreeSpinWinMultiplier { get; }

        public BonusRules(SymbolType wildSymbol, SymbolType scatterSymbol, int scattersNeededForFreeSpins,
            int freeSpinsAwarded, int freeSpinWinMultiplier)
        {
            if (scattersNeededForFreeSpins < 1) throw new ArgumentOutOfRangeException(nameof(scattersNeededForFreeSpins));
            if (freeSpinsAwarded < 0) throw new ArgumentOutOfRangeException(nameof(freeSpinsAwarded));
            if (freeSpinWinMultiplier < 1) throw new ArgumentOutOfRangeException(nameof(freeSpinWinMultiplier));

            WildSymbol = wildSymbol;
            ScatterSymbol = scatterSymbol;
            ScattersNeededForFreeSpins = scattersNeededForFreeSpins;
            FreeSpinsAwarded = freeSpinsAwarded;
            FreeSpinWinMultiplier = freeSpinWinMultiplier;
        }

        /// <summary>Rules used by the game unless the config says otherwise.</summary>
        public static BonusRules Default => new BonusRules(SymbolType.Wild, SymbolType.Wild, 3, 5, 2);
    }
}
