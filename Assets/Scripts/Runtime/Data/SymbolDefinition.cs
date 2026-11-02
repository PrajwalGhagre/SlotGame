using SlotGame.Core.Symbols;
using UnityEngine;

namespace SlotGame.Data
{
    /// <summary>
    /// Designer-facing data for one symbol: its art and its tuning numbers.
    /// Create more with Assets > Create > Slot Game > Symbol Definition.
    /// </summary>
    [CreateAssetMenu(fileName = "Symbol_", menuName = "Slot Game/Symbol Definition", order = 0)]
    public sealed class SymbolDefinition : ScriptableObject
    {
        [SerializeField] private SymbolType symbolType;
        [SerializeField] private string displayName = "Symbol";
        [SerializeField] private Sprite sprite;

        [Header("Maths")]
        [Tooltip("How many times this symbol is printed on every reel strip.")]
        [SerializeField, Min(1)] private int copiesOnStrip = 2;

        [Tooltip("Weight of each stop showing this symbol. Higher = lands more often.")]
        [SerializeField, Min(1)] private int stopWeight = 5;

        [Tooltip("Bet multiplier paid for this symbol on every reel of the payline.")]
        [SerializeField, Min(0)] private int payoutMultiplier = 10;

        [Header("Presentation")]
        [Tooltip("Glow colour used when this symbol is part of a win.")]
        [SerializeField] private Color winGlowColor = new Color(1f, 0.9f, 0.3f, 1f);

        public SymbolType SymbolType => symbolType;
        public string DisplayName => displayName;
        public Sprite Sprite => sprite;
        public int CopiesOnStrip => copiesOnStrip;
        public int StopWeight => stopWeight;
        public int PayoutMultiplier => payoutMultiplier;
        public Color WinGlowColor => winGlowColor;

        /// <summary>Converts the asset into the plain struct the game logic uses.</summary>
        public SymbolRule ToRule()
        {
            return new SymbolRule(symbolType, copiesOnStrip, stopWeight, payoutMultiplier);
        }
    }
}
