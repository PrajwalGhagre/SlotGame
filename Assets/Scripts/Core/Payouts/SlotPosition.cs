namespace SlotGame.Core.Payouts
{
    /// <summary>A cell in the visible window, used to highlight winning symbols.</summary>
    public readonly struct SlotPosition
    {
        public int Reel { get; }
        public int Row { get; }

        public SlotPosition(int reel, int row)
        {
            Reel = reel;
            Row = row;
        }

        public override string ToString()
        {
            return $"(reel {Reel}, row {Row})";
        }
    }
}
