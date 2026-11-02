using System;
using System.Collections.Generic;
using SlotGame.Core.Symbols;

namespace SlotGame.Core.Payouts
{
    /// <summary>
    /// How much each three-of-a-kind pays, as a multiple of the bet,
    /// plus the extra multiplier for wins that needed a Wild.
    /// </summary>
    public sealed class Paytable
    {
        private readonly Dictionary<SymbolType, int> _multipliers = new Dictionary<SymbolType, int>();

        /// <summary>A win that used at least one Wild as a stand-in is multiplied by this.</summary>
        public int WildAssistMultiplier { get; }

        public Paytable(IEnumerable<SymbolRule> rules, int wildAssistMultiplier)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (wildAssistMultiplier < 1) throw new ArgumentOutOfRangeException(nameof(wildAssistMultiplier));

            foreach (SymbolRule rule in rules)
            {
                _multipliers[rule.Type] = rule.PayoutMultiplier;
            }

            WildAssistMultiplier = wildAssistMultiplier;
        }

        /// <summary>Bet multiplier for a line of this symbol, 0 if the symbol is unknown.</summary>
        public int GetMultiplier(SymbolType symbol)
        {
            return _multipliers.TryGetValue(symbol, out int multiplier) ? multiplier : 0;
        }

        public IReadOnlyDictionary<SymbolType, int> Multipliers => _multipliers;
    }
}
