namespace SlotGame.Core.Symbols
{
    /// <summary>
    /// Every symbol that can appear on a reel.
    /// The game logic only ever works with this enum. Sprites, sounds and
    /// tuning numbers live in the SymbolDefinition ScriptableObjects, so the
    /// rules can be tested without Unity.
    /// </summary>
    public enum SymbolType
    {
        Cherry = 0,
        Bell = 1,
        Bar = 2,
        Seven = 3,

        /// <summary>Substitutes for any other symbol and also works as the free spins scatter.</summary>
        Wild = 4
    }
}
