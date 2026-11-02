using System;

namespace SlotGame.Core.Bonus
{
    /// <summary>
    /// Counts free spins left and the multiplier that applies while they run.
    /// Free spins can be re-triggered: new spins are added to the ones left.
    /// </summary>
    public sealed class FreeSpinsTracker
    {
        public int RemainingSpins { get; private set; }

        /// <summary>Win multiplier while free spins are active, 1 otherwise.</summary>
        public int ActiveMultiplier { get; private set; } = 1;

        /// <summary>Coins won since the current free spins round started.</summary>
        public int TotalWonThisFeature { get; private set; }

        public bool IsActive => RemainingSpins > 0;

        /// <summary>Raised with (spins left, multiplier) when the count changes.</summary>
        public event Action<int, int> Changed;

        public void Award(int spins, int multiplier)
        {
            if (spins <= 0) throw new ArgumentOutOfRangeException(nameof(spins));
            if (multiplier < 1) throw new ArgumentOutOfRangeException(nameof(multiplier));

            if (!IsActive)
            {
                TotalWonThisFeature = 0;
            }

            RemainingSpins += spins;
            ActiveMultiplier = Math.Max(ActiveMultiplier, multiplier);
            Changed?.Invoke(RemainingSpins, ActiveMultiplier);
        }

        /// <summary>Uses up one free spin. Returns false if there were none left.</summary>
        public bool TryConsume()
        {
            if (!IsActive)
            {
                return false;
            }

            RemainingSpins--;
            Changed?.Invoke(RemainingSpins, ActiveMultiplier);
            return true;
        }

        public void RecordWin(int amount)
        {
            if (amount > 0)
            {
                TotalWonThisFeature += amount;
            }
        }

        /// <summary>Call after the last free spin has fully finished.</summary>
        public void EndFeatureIfFinished()
        {
            if (!IsActive && ActiveMultiplier != 1)
            {
                ActiveMultiplier = 1;
                Changed?.Invoke(RemainingSpins, ActiveMultiplier);
            }
        }
    }
}
