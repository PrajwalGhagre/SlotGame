using System;

namespace SlotGame.Core.Motion
{
    /// <summary>
    /// Works out how a spinning reel can stop exactly on the symbol the RNG chose,
    /// without any visible jump. This is the trickiest part of the reel code, so it
    /// is kept here as pure maths with unit tests.
    ///
    /// The model:
    /// - A reel has a continuous "position" measured in symbols. Spinning just adds
    ///   speed * deltaTime to it. Symbols move down the screen as it grows.
    /// - Every cell on screen belongs to an integer "virtual slot" q. The payline
    ///   shows slot floor(position), the row above shows floor(position) + 1, etc.
    /// - A slot is turned into a strip index with: index = q + offset (wrapped).
    ///
    /// When a stop is requested the result symbol usually is not the next one on
    /// the strip. Instead of teleporting the reel, we "splice" the strip: slots the
    /// player can already see keep the old offset, and every slot that has not
    /// appeared yet uses a new offset chosen so that the landing slot shows the
    /// target. The seam sits above the top edge of the window, so nobody sees it,
    /// and the symbols around the result are the real strip neighbours.
    ///
    /// Then the reel slows down with an ease-out-cubic curve. That curve starts with
    /// slope 3, so a duration of 3 * distance / speed keeps the starting speed equal
    /// to the spinning speed (no sudden brake).
    /// </summary>
    public static class ReelStopPlanner
    {
        public static ReelStopPlan Plan(double currentPosition, float currentSpeed, int targetStripIndex, int stripLength,
            int visibleRows, int extraSymbols, float overshoot, float minDuration, float maxDuration)
        {
            if (stripLength <= 0) throw new ArgumentOutOfRangeException(nameof(stripLength));
            if (visibleRows <= 0) throw new ArgumentOutOfRangeException(nameof(visibleRows));
            if (extraSymbols < 0) throw new ArgumentOutOfRangeException(nameof(extraSymbols));

            int middleRow = visibleRows / 2;
            long currentSlot = (long)Math.Floor(currentPosition);

            // Highest slot drawn right now is the hidden buffer cell above the top row:
            // currentSlot + middleRow + 1. Everything above that is still unseen.
            long spliceFrom = currentSlot + middleRow + 2;

            // When the reel lands on slot L, its lowest visible row is L - (rows - 1 - middleRow).
            // That row must already use the new mapping, so L has to be at least
            // spliceFrom + (rows - 1 - middleRow). Extra symbols make the stop longer.
            long landingSlot = spliceFrom + (visibleRows - 1 - middleRow) + extraSymbols;

            // Choose the offset so that landingSlot maps to the target strip index.
            long newOffset = Mod(targetStripIndex - landingSlot, stripLength);

            double distance = landingSlot - currentPosition;
            float duration = currentSpeed > 0.01f
                ? (float)(3.0 * (distance + overshoot) / currentSpeed)
                : maxDuration;
            duration = Math.Max(minDuration, Math.Min(maxDuration, duration));

            return new ReelStopPlan(landingSlot, spliceFrom, newOffset, distance, duration);
        }

        /// <summary>Strip index for a virtual slot under a given offset.</summary>
        public static int SlotToStripIndex(long slot, long offset, int stripLength)
        {
            return (int)Mod(slot + offset, stripLength);
        }

        /// <summary>Always positive modulo, so -1 mod 5 is 4.</summary>
        public static long Mod(long value, long modulus)
        {
            long result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }
}
