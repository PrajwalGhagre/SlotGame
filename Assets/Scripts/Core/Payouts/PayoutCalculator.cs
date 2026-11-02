using System;
using System.Collections.Generic;
using SlotGame.Core.Spin;
using SlotGame.Core.Symbols;

namespace SlotGame.Core.Payouts
{
    /// <summary>
    /// Decides if a spin won and how much.
    ///
    /// The main rule from the brief: the player wins when every reel shows the
    /// same symbol on the payline. On top of that:
    /// - A Wild counts as any symbol. "Bell, Wild, Bell" is a Bell win.
    ///   Wins that needed a Wild are multiplied by WildAssistMultiplier.
    /// - Three Wilds is its own top prize.
    /// - Scatter symbols are counted over the whole visible window for free spins.
    /// </summary>
    public sealed class PayoutCalculator
    {
        private readonly Paytable _paytable;
        private readonly BonusRules _bonusRules;

        public PayoutCalculator(Paytable paytable, BonusRules bonusRules)
        {
            _paytable = paytable ?? throw new ArgumentNullException(nameof(paytable));
            _bonusRules = bonusRules ?? throw new ArgumentNullException(nameof(bonusRules));
        }

        public WinResult Evaluate(SpinResult spin, int bet)
        {
            if (spin == null) throw new ArgumentNullException(nameof(spin));
            if (bet < 0) throw new ArgumentOutOfRangeException(nameof(bet));

            SymbolType[] line = spin.GetPaylineSymbols();
            SymbolType wild = _bonusRules.WildSymbol;

            // Find the first symbol on the line that is not a Wild. Every other
            // non-wild symbol has to match it for the line to win.
            bool foundTarget = false;
            SymbolType target = wild;
            bool usedWild = false;

            foreach (SymbolType symbol in line)
            {
                if (symbol == wild)
                {
                    usedWild = true;
                    continue;
                }

                if (!foundTarget)
                {
                    target = symbol;
                    foundTarget = true;
                }
                else if (symbol != target)
                {
                    return WinResult.Loss();
                }
            }

            // No non-wild symbol at all means the whole line is Wilds: pay the Wild prize
            // and do not apply the "wild assist" bonus on top of it.
            if (!foundTarget)
            {
                usedWild = false;
            }

            int symbolMultiplier = _paytable.GetMultiplier(target);
            if (symbolMultiplier <= 0)
            {
                return WinResult.Loss();
            }

            int payout = bet * symbolMultiplier;
            if (usedWild)
            {
                payout *= _paytable.WildAssistMultiplier;
            }

            var positions = new List<SlotPosition>(line.Length);
            for (int reel = 0; reel < line.Length; reel++)
            {
                positions.Add(new SlotPosition(reel, spin.PaylineRow));
            }

            return WinResult.Win(target, symbolMultiplier, usedWild, payout, positions);
        }

        /// <summary>Every visible scatter symbol, anywhere in the window.</summary>
        public IReadOnlyList<SlotPosition> FindScatters(SpinResult spin)
        {
            if (spin == null) throw new ArgumentNullException(nameof(spin));

            var positions = new List<SlotPosition>();
            for (int reel = 0; reel < spin.ReelCount; reel++)
            {
                for (int row = 0; row < spin.RowCount; row++)
                {
                    if (spin.GetSymbol(reel, row) == _bonusRules.ScatterSymbol)
                    {
                        positions.Add(new SlotPosition(reel, row));
                    }
                }
            }

            return positions;
        }

        /// <summary>True if this spin shows enough scatters to award free spins.</summary>
        public bool TriggersFreeSpins(SpinResult spin)
        {
            return _bonusRules.FreeSpinsAwarded > 0
                   && FindScatters(spin).Count >= _bonusRules.ScattersNeededForFreeSpins;
        }
    }
}
