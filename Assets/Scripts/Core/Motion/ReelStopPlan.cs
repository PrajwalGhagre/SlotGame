namespace SlotGame.Core.Motion
{
    /// <summary>
    /// The numbers a reel needs to land exactly on its result. See ReelStopPlanner.
    /// </summary>
    public readonly struct ReelStopPlan
    {
        /// <summary>Reel position (in symbol steps) where the payline shows the target symbol.</summary>
        public double LandingPosition { get; }

        /// <summary>
        /// First virtual slot that uses the new symbol mapping. Slots below this keep
        /// the old mapping because the player can already see them.
        /// </summary>
        public long SpliceFrom { get; }

        /// <summary>Offset added to a virtual slot to get its strip index, for slots at or above SpliceFrom.</summary>
        public long NewIndexOffset { get; }

        /// <summary>Distance left to travel from the current position to the landing position.</summary>
        public double Distance { get; }

        /// <summary>How long the slow down should take so the speed does not jump.</summary>
        public float Duration { get; }

        public ReelStopPlan(double landingPosition, long spliceFrom, long newIndexOffset, double distance, float duration)
        {
            LandingPosition = landingPosition;
            SpliceFrom = spliceFrom;
            NewIndexOffset = newIndexOffset;
            Distance = distance;
            Duration = duration;
        }
    }
}
