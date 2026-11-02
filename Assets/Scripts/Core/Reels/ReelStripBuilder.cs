using System;
using System.Collections.Generic;
using SlotGame.Core.Random;
using SlotGame.Core.Symbols;

namespace SlotGame.Core.Reels
{
    /// <summary>
    /// Builds the visual reel strips from the symbol rules.
    ///
    /// Steps:
    /// 1. Put every symbol on the strip as many times as its CopiesOnStrip says.
    /// 2. Shuffle the list (Fisher-Yates) with the given RNG. A fixed seed gives the
    ///    same strip every time, so the layout does not change between sessions.
    /// 3. Try to make sure the same symbol never sits next to itself (including the
    ///    wrap from the last stop back to the first). It just looks nicer on a
    ///    spinning reel and avoids "free" near misses.
    /// </summary>
    public static class ReelStripBuilder
    {
        private const int MaxFixAttempts = 200;

        public static ReelStrip Build(IReadOnlyList<SymbolRule> rules, IRandomProvider layoutRandom)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (layoutRandom == null) throw new ArgumentNullException(nameof(layoutRandom));
            if (rules.Count == 0) throw new ArgumentException("At least one symbol rule is required.", nameof(rules));

            var symbols = new List<SymbolType>();
            var weightBySymbol = new Dictionary<SymbolType, int>();

            foreach (SymbolRule rule in rules)
            {
                if (weightBySymbol.ContainsKey(rule.Type))
                {
                    throw new ArgumentException($"Symbol {rule.Type} has more than one rule.", nameof(rules));
                }

                weightBySymbol.Add(rule.Type, rule.StopWeight);
                for (int i = 0; i < rule.CopiesOnStrip; i++)
                {
                    symbols.Add(rule.Type);
                }
            }

            Shuffle(symbols, layoutRandom);
            SpreadOutDuplicates(symbols, layoutRandom);

            var weights = new int[symbols.Count];
            for (int i = 0; i < symbols.Count; i++)
            {
                weights[i] = weightBySymbol[symbols[i]];
            }

            return new ReelStrip(symbols, weights);
        }

        /// <summary>Classic Fisher-Yates shuffle. Every order is equally likely.</summary>
        private static void Shuffle(List<SymbolType> list, IRandomProvider random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.NextInt(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// Swaps symbols around until no two neighbours are the same.
        /// If the strip is too crowded with one symbol this can not always succeed,
        /// so we stop after a fixed number of attempts and keep the best effort.
        /// </summary>
        private static void SpreadOutDuplicates(List<SymbolType> list, IRandomProvider random)
        {
            if (list.Count < 3)
            {
                return;
            }

            for (int attempt = 0; attempt < MaxFixAttempts; attempt++)
            {
                int clashIndex = FindNeighbourClash(list);
                if (clashIndex < 0)
                {
                    return;
                }

                int swapWith = random.NextInt(0, list.Count);
                (list[clashIndex], list[swapWith]) = (list[swapWith], list[clashIndex]);
            }
        }

        /// <summary>Returns the index of a symbol that equals its next neighbour, or -1.</summary>
        private static int FindNeighbourClash(List<SymbolType> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int next = (i + 1) % list.Count;
                if (list[i] == list[next])
                {
                    return next;
                }
            }

            return -1;
        }
    }
}
