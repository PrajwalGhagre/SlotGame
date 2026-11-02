using System;
using System.Collections.Generic;

namespace SlotGame.Core.Random
{
    /// <summary>
    /// Picks items at random where each item has its own weight.
    /// An item with weight 4 is picked twice as often as an item with weight 2.
    ///
    /// How it works: we build a running total ("cumulative weights") once in the
    /// constructor. For weights [5, 3, 2] the totals are [5, 8, 10]. To pick, we
    /// roll a number in [0, 10) and find the first total that is bigger than the
    /// roll. Rolls 0..4 give item 0, 5..7 give item 1, 8..9 give item 2.
    /// The search is a binary search, so picking stays fast for long lists.
    /// </summary>
    public sealed class WeightedRandomPicker<T>
    {
        private readonly T[] _items;
        private readonly int[] _cumulativeWeights;

        /// <summary>Sum of all weights.</summary>
        public int TotalWeight { get; }

        public int Count => _items.Length;

        public WeightedRandomPicker(IReadOnlyList<T> items, IReadOnlyList<int> weights)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (weights == null) throw new ArgumentNullException(nameof(weights));
            if (items.Count == 0) throw new ArgumentException("At least one item is required.", nameof(items));
            if (items.Count != weights.Count)
            {
                throw new ArgumentException("items and weights must have the same length.");
            }

            _items = new T[items.Count];
            _cumulativeWeights = new int[items.Count];

            int runningTotal = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (weights[i] < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(weights), "Weights can not be negative.");
                }

                runningTotal += weights[i];
                _items[i] = items[i];
                _cumulativeWeights[i] = runningTotal;
            }

            if (runningTotal <= 0)
            {
                throw new ArgumentException("At least one weight must be greater than zero.", nameof(weights));
            }

            TotalWeight = runningTotal;
        }

        /// <summary>Picks an item and returns it.</summary>
        public T Pick(IRandomProvider random)
        {
            return _items[PickIndex(random)];
        }

        /// <summary>Picks an item and returns its index in the original list.</summary>
        public int PickIndex(IRandomProvider random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));

            int roll = random.NextInt(0, TotalWeight);

            // Binary search for the first cumulative weight that is greater than the roll.
            int low = 0;
            int high = _cumulativeWeights.Length - 1;
            while (low < high)
            {
                int mid = (low + high) / 2;
                if (roll < _cumulativeWeights[mid])
                {
                    high = mid;
                }
                else
                {
                    low = mid + 1;
                }
            }

            return low;
        }

        /// <summary>Chance (0..1) that the item at the given index gets picked.</summary>
        public double GetProbability(int index)
        {
            int previous = index == 0 ? 0 : _cumulativeWeights[index - 1];
            return (double)(_cumulativeWeights[index] - previous) / TotalWeight;
        }
    }
}
