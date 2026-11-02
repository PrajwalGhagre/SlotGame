using System;
using System.Collections.Generic;

namespace SlotGame.Core.Economy
{
    /// <summary>
    /// Holds the player's coins and the current bet.
    /// It does not know anything about reels or UI; it only guards the rules
    /// "you can not bet more than you have" and "bets come from a fixed list".
    /// </summary>
    public sealed class Wallet
    {
        private readonly int[] _betLevels;
        private int _betIndex;

        /// <summary>Raised with the new balance whenever coins are added or removed.</summary>
        public event Action<int> BalanceChanged;

        /// <summary>Raised with the new bet whenever the bet level changes.</summary>
        public event Action<int> BetChanged;

        public int Balance { get; private set; }

        public int CurrentBet => _betLevels[_betIndex];

        public IReadOnlyList<int> BetLevels => _betLevels;

        public bool CanAffordBet => Balance >= CurrentBet;

        public bool IsAtMinBet => _betIndex == 0;

        public bool IsAtMaxBet => _betIndex == _betLevels.Length - 1;

        public Wallet(int startingBalance, IReadOnlyList<int> betLevels, int startingBetIndex = 0)
        {
            if (startingBalance < 0) throw new ArgumentOutOfRangeException(nameof(startingBalance));
            if (betLevels == null || betLevels.Count == 0)
            {
                throw new ArgumentException("At least one bet level is required.", nameof(betLevels));
            }

            _betLevels = new int[betLevels.Count];
            for (int i = 0; i < betLevels.Count; i++)
            {
                if (betLevels[i] <= 0) throw new ArgumentOutOfRangeException(nameof(betLevels), "Bets must be positive.");
                if (i > 0 && betLevels[i] <= betLevels[i - 1])
                {
                    throw new ArgumentException("Bet levels must be in increasing order.", nameof(betLevels));
                }

                _betLevels[i] = betLevels[i];
            }

            Balance = startingBalance;
            _betIndex = Math.Max(0, Math.Min(startingBetIndex, _betLevels.Length - 1));
        }

        public void IncreaseBet()
        {
            SetBetIndex(_betIndex + 1);
        }

        public void DecreaseBet()
        {
            SetBetIndex(_betIndex - 1);
        }

        /// <summary>Takes the current bet from the balance. Returns false if the player can not afford it.</summary>
        public bool TryPlaceBet()
        {
            if (!CanAffordBet)
            {
                return false;
            }

            Balance -= CurrentBet;
            BalanceChanged?.Invoke(Balance);
            return true;
        }

        public void AddWinnings(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount == 0) return;

            Balance += amount;
            BalanceChanged?.Invoke(Balance);
        }

        /// <summary>Sets the balance back to a fixed amount (used by the "refill coins" button).</summary>
        public void ResetBalance(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));

            Balance = amount;
            BalanceChanged?.Invoke(Balance);
        }

        private void SetBetIndex(int index)
        {
            int clamped = Math.Max(0, Math.Min(index, _betLevels.Length - 1));
            if (clamped == _betIndex)
            {
                return;
            }

            _betIndex = clamped;
            BetChanged?.Invoke(CurrentBet);
        }
    }
}
