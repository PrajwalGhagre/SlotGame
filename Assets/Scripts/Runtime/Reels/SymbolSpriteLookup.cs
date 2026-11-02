using System.Collections.Generic;
using SlotGame.Core.Symbols;
using SlotGame.Data;
using UnityEngine;

namespace SlotGame.Reels
{
    /// <summary>Fast SymbolType to Sprite/colour lookup built once from the config.</summary>
    public sealed class SymbolSpriteLookup
    {
        private readonly Dictionary<SymbolType, SymbolDefinition> _definitions = new Dictionary<SymbolType, SymbolDefinition>();

        public SymbolSpriteLookup(IEnumerable<SymbolDefinition> definitions)
        {
            foreach (SymbolDefinition definition in definitions)
            {
                if (definition != null)
                {
                    _definitions[definition.SymbolType] = definition;
                }
            }
        }

        public Sprite GetSprite(SymbolType type)
        {
            return _definitions.TryGetValue(type, out SymbolDefinition definition) ? definition.Sprite : null;
        }

        public Color GetGlowColor(SymbolType type)
        {
            return _definitions.TryGetValue(type, out SymbolDefinition definition) ? definition.WinGlowColor : Color.yellow;
        }
    }
}
