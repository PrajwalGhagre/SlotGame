using System.Collections.Generic;
using SlotGame.Core.Payouts;
using SlotGame.Core.Spin;

namespace SlotGame.Core.Session
{
    /// <summary>
    /// Everything that happened in one round, handed to the presentation layer
    /// so it can animate the reels and then show the win.
    /// </summary>
    public sealed class SpinRoundResult
    {
        public SpinResult Spin { get; }
        public WinResult Win { get; }

        /// <summary>The stake for this round (not charged when it was a free spin).</summary>
        public int Bet { get; }

        public bool WasFreeSpin { get; }

        /// <summary>Free spin multiplier applied to this round's win (1 in the base game).</summary>
        public int AppliedMultiplier { get; }

        /// <summary>Coins actually added to the balance this round.</summary>
        public int TotalPayout { get; }

        /// <summary>Free spins awarded by this round (0 if none).</summary>
        public int FreeSpinsAwarded { get; }

        public int FreeSpinsRemaining { get; }

        public IReadOnlyList<SlotPosition> ScatterPositions { get; }

        public int BalanceAfter { get; }

        public bool IsWin => TotalPayout > 0;

        public SpinRoundResult(SpinResult spin, WinResult win, int bet, bool wasFreeSpin, int appliedMultiplier,
            int totalPayout, int freeSpinsAwarded, int freeSpinsRemaining, IReadOnlyList<SlotPosition> scatterPositions,
            int balanceAfter)
        {
            Spin = spin;
            Win = win;
            Bet = bet;
            WasFreeSpin = wasFreeSpin;
            AppliedMultiplier = appliedMultiplier;
            TotalPayout = totalPayout;
            FreeSpinsAwarded = freeSpinsAwarded;
            FreeSpinsRemaining = freeSpinsRemaining;
            ScatterPositions = scatterPositions;
            BalanceAfter = balanceAfter;
        }
    }
}
