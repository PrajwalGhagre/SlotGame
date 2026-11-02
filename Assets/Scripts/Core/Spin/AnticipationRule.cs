using System;
using SlotGame.Core.Payouts;
using SlotGame.Core.Symbols;

namespace SlotGame.Core.Spin
{
    /// <summary>
    /// Decides if the last reel should spin a bit longer for suspense
    /// ("anticipation" in slot game terms). That happens when every reel except
    /// the last one could still be part of a win, or when one more scatter would
    /// trigger free spins.
    /// It only changes the animation, never the result.
    /// </summary>
    public static class AnticipationRule
    {
        public static bool ShouldAnticipateLastReel(SpinResult spin, BonusRules bonusRules)
        {
            if (spin == null) throw new ArgumentNullException(nameof(spin));
            if (bonusRules == null) throw new ArgumentNullException(nameof(bonusRules));
            if (spin.ReelCount < 2) return false;

            return PaylineStillOpen(spin, bonusRules.WildSymbol) || OneScatterAway(spin, bonusRules);
        }

        /// <summary>True when the payline symbols on all reels but the last can still form a line.</summary>
        private static bool PaylineStillOpen(SpinResult spin, SymbolType wild)
        {
            bool hasTarget = false;
            SymbolType target = wild;

            for (int reel = 0; reel < spin.ReelCount - 1; reel++)
            {
                SymbolType symbol = spin.GetSymbol(reel, spin.PaylineRow);
                if (symbol == wild) continue;

                if (!hasTarget)
                {
                    target = symbol;
                    hasTarget = true;
                }
                else if (symbol != target)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool OneScatterAway(SpinResult spin, BonusRules rules)
        {
            if (rules.FreeSpinsAwarded <= 0) return false;

            int scattersBeforeLastReel = 0;
            for (int reel = 0; reel < spin.ReelCount - 1; reel++)
            {
                for (int row = 0; row < spin.RowCount; row++)
                {
                    if (spin.GetSymbol(reel, row) == rules.ScatterSymbol)
                    {
                        scattersBeforeLastReel++;
                    }
                }
            }

            return scattersBeforeLastReel == rules.ScattersNeededForFreeSpins - 1;
        }
    }
}
