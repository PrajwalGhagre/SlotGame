using System;

namespace SlotGame.Core.Motion
{
    /// <summary>
    /// Easing functions used by the reel and UI animations.
    /// Every function takes t in [0, 1] and returns the eased progress.
    /// They live in Core (no UnityEngine) so they can be unit tested.
    /// </summary>
    public static class Easing
    {
        public static float Clamp01(float t)
        {
            return t < 0f ? 0f : (t > 1f ? 1f : t);
        }

        public static float Linear(float t)
        {
            return Clamp01(t);
        }

        public static float InQuad(float t)
        {
            t = Clamp01(t);
            return t * t;
        }

        public static float OutQuad(float t)
        {
            t = Clamp01(t);
            return 1f - (1f - t) * (1f - t);
        }

        public static float InOutQuad(float t)
        {
            t = Clamp01(t);
            return t < 0.5f ? 2f * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 2) / 2f;
        }

        /// <summary>
        /// Fast start, slow finish. Its slope at t = 0 is exactly 3, which the reel
        /// uses to make the stop start at the same speed the reel was spinning
        /// (see ReelStopPlanner).
        /// </summary>
        public static float OutCubic(float t)
        {
            t = Clamp01(t);
            float inverse = 1f - t;
            return 1f - inverse * inverse * inverse;
        }

        /// <summary>Goes a little past 1 and comes back, nice for buttons and popups.</summary>
        public static float OutBack(float t, float overshoot = 1.70158f)
        {
            t = Clamp01(t);
            float c3 = overshoot + 1f;
            float shifted = t - 1f;
            return 1f + c3 * shifted * shifted * shifted + overshoot * shifted * shifted;
        }

        /// <summary>Hermite smoothstep, gentle at both ends.</summary>
        public static float SmoothStep(float t)
        {
            t = Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
