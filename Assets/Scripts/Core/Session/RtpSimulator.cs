using System;
using SlotGame.Core.Bonus;
using SlotGame.Core.Economy;
using SlotGame.Core.Payouts;
using SlotGame.Core.Spin;

namespace SlotGame.Core.Session
{
    /// <summary>
    /// Plays a lot of rounds very fast to measure the game maths:
    /// RTP (return to player, coins paid back per coin bet) and hit rate.
    /// Used from the Tools menu and from the unit tests to sanity check the paytable.
    /// </summary>
    public static class RtpSimulator
    {
        public readonly struct Report
        {
            public long Rounds { get; }
            public long PaidRounds { get; }
            public long TotalBet { get; }
            public long TotalWon { get; }
            public long WinningRounds { get; }
            public long FreeSpinTriggers { get; }

            public double Rtp => TotalBet == 0 ? 0 : (double)TotalWon / TotalBet;
            public double HitRate => Rounds == 0 ? 0 : (double)WinningRounds / Rounds;

            public Report(long rounds, long paidRounds, long totalBet, long totalWon, long winningRounds, long freeSpinTriggers)
            {
                Rounds = rounds;
                PaidRounds = paidRounds;
                TotalBet = totalBet;
                TotalWon = totalWon;
                WinningRounds = winningRounds;
                FreeSpinTriggers = freeSpinTriggers;
            }

            public override string ToString()
            {
                return $"Rounds {Rounds:N0} (paid {PaidRounds:N0}) | RTP {Rtp:P2} | Hit rate {HitRate:P2} | " +
                       $"Free spin triggers {FreeSpinTriggers:N0}";
            }
        }

        /// <summary>Plays the given number of paid spins (free spins they trigger are played too).</summary>
        public static Report Run(SpinOutcomeGenerator generator, PayoutCalculator calculator, BonusRules bonusRules,
            int paidSpins, int bet = 1)
        {
            if (paidSpins <= 0) throw new ArgumentOutOfRangeException(nameof(paidSpins));

            // A wallet that never runs dry, so we only measure the maths.
            var wallet = new Wallet(int.MaxValue / 2, new[] { bet });
            var freeSpins = new FreeSpinsTracker();
            var session = new SlotGameSession(wallet, generator, calculator, bonusRules, freeSpins);

            long rounds = 0, paidRounds = 0, totalBet = 0, totalWon = 0, wins = 0, triggers = 0;

            while (paidRounds < paidSpins || freeSpins.IsActive)
            {
                // Keep the wallet topped up so huge simulations never run out.
                if (wallet.Balance < bet)
                {
                    wallet.ResetBalance(int.MaxValue / 2);
                }

                SpinRoundResult round = session.PlayRound();
                rounds++;
                if (!round.WasFreeSpin)
                {
                    paidRounds++;
                    totalBet += bet;
                }

                totalWon += round.TotalPayout;
                if (round.IsWin) wins++;
                if (round.FreeSpinsAwarded > 0) triggers++;

                if (wallet.Balance > int.MaxValue / 2 + 1_000_000)
                {
                    wallet.ResetBalance(int.MaxValue / 2);
                }
            }

            return new Report(rounds, paidRounds, totalBet, totalWon, wins, triggers);
        }
    }
}
