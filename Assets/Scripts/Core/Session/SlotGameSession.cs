using System;
using SlotGame.Core.Bonus;
using SlotGame.Core.Economy;
using SlotGame.Core.Payouts;
using SlotGame.Core.Spin;

namespace SlotGame.Core.Session
{
    /// <summary>
    /// The whole game round in plain C#: take the bet, roll the RNG, work out the
    /// win, pay it, handle free spins. No Unity code here, so the full game rules
    /// can be unit tested and simulated for millions of spins.
    ///
    /// The Unity side (SlotMachineController) only calls PlayRound() and then
    /// animates whatever came back.
    /// </summary>
    public sealed class SlotGameSession
    {
        private readonly SpinOutcomeGenerator _generator;
        private readonly PayoutCalculator _calculator;
        private readonly BonusRules _bonusRules;

        public Wallet Wallet { get; }
        public FreeSpinsTracker FreeSpins { get; }

        /// <summary>True if the next round can be played (free spin waiting, or enough coins).</summary>
        public bool CanPlayRound => FreeSpins.IsActive || Wallet.CanAffordBet;

        public SlotGameSession(Wallet wallet, SpinOutcomeGenerator generator, PayoutCalculator calculator,
            BonusRules bonusRules, FreeSpinsTracker freeSpins = null)
        {
            Wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
            _bonusRules = bonusRules ?? throw new ArgumentNullException(nameof(bonusRules));
            FreeSpins = freeSpins ?? new FreeSpinsTracker();
        }

        /// <summary>
        /// Plays one round. Throws if CanPlayRound is false, so always check it first.
        /// </summary>
        public SpinRoundResult PlayRound()
        {
            int bet = Wallet.CurrentBet;

            // A finished free spins round goes back to the normal x1 multiplier.
            FreeSpins.EndFeatureIfFinished();

            // 1. Pay for the spin (or use a free one).
            bool isFreeSpin = FreeSpins.TryConsume();
            if (!isFreeSpin && !Wallet.TryPlaceBet())
            {
                throw new InvalidOperationException("Not enough coins for this bet.");
            }

            // A free spin uses the multiplier that was active when it was awarded.
            int multiplier = isFreeSpin ? FreeSpins.ActiveMultiplier : 1;

            // 2. Decide the outcome. This is the only place randomness is used.
            SpinResult spin = _generator.Spin();

            // 3. Work out the line win.
            WinResult win = _calculator.Evaluate(spin, bet);
            int payout = win.Payout * multiplier;
            Wallet.AddWinnings(payout);

            if (isFreeSpin)
            {
                FreeSpins.RecordWin(payout);
            }

            // 4. Bonus check: enough scatters anywhere in the window gives free spins.
            var scatters = _calculator.FindScatters(spin);
            int awarded = 0;
            if (_calculator.TriggersFreeSpins(spin))
            {
                awarded = _bonusRules.FreeSpinsAwarded;
                FreeSpins.Award(awarded, _bonusRules.FreeSpinWinMultiplier);
            }

            return new SpinRoundResult(spin, win, bet, isFreeSpin, multiplier, payout, awarded,
                FreeSpins.RemainingSpins, scatters, Wallet.Balance);
        }
    }
}
