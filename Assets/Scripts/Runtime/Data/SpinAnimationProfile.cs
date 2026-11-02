using UnityEngine;

namespace SlotGame.Data
{
    /// <summary>
    /// All the numbers that control how the reels move. Kept in an asset
    /// (Assets/Animations/ReelSpinProfile.asset) so the game feel can be tuned in
    /// the Inspector while the game is running, without touching code.
    /// Distances are measured in symbols, speeds in symbols per second.
    /// </summary>
    [CreateAssetMenu(fileName = "ReelSpinProfile", menuName = "Slot Game/Spin Animation Profile", order = 2)]
    public sealed class SpinAnimationProfile : ScriptableObject
    {
        [Header("Start")]
        [Tooltip("Small pull back before the reel starts, like a real reel being kicked.")]
        [SerializeField, Range(0f, 1f)] private float windUpDistance = 0.3f;
        [SerializeField, Min(0f)] private float windUpDuration = 0.12f;
        [Tooltip("Time to reach full speed.")]
        [SerializeField, Min(0.01f)] private float accelerationDuration = 0.35f;
        [Tooltip("Speed over the acceleration time (0..1 of max speed).")]
        [SerializeField] private AnimationCurve accelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Delay between each reel starting, left to right.")]
        [SerializeField, Min(0f)] private float startStagger = 0.08f;

        [Header("Spin")]
        [SerializeField, Min(1f)] private float maxSpeed = 18f;
        [Tooltip("How long all reels spin before the first one is told to stop.")]
        [SerializeField, Min(0f)] private float minimumSpinDuration = 0.9f;
        [Tooltip("Gap between one reel being told to stop and the next one.")]
        [SerializeField, Min(0f)] private float stopInterval = 0.32f;
        [Tooltip("Extra wait before the last reel stops when a win or bonus is still possible.")]
        [SerializeField, Min(0f)] private float anticipationExtraTime = 1.1f;

        [Header("Stop")]
        [Tooltip("Extra symbols that scroll past while slowing down. More = longer, softer stop.")]
        [SerializeField, Range(0, 8)] private int extraStopSymbols = 2;
        [Tooltip("How far (in symbols) the reel overshoots before bouncing back.")]
        [SerializeField, Range(0f, 0.5f)] private float overshoot = 0.22f;
        [SerializeField, Min(0f)] private float settleDuration = 0.18f;
        [SerializeField, Min(0.05f)] private float minStopDuration = 0.35f;
        [SerializeField, Min(0.05f)] private float maxStopDuration = 1.4f;

        [Header("Polish")]
        [Tooltip("Vertical stretch of symbols at full speed (fake motion blur).")]
        [SerializeField, Range(0f, 0.5f)] private float motionStretch = 0.12f;
        [SerializeField, Range(1f, 1.5f)] private float winPulseScale = 1.15f;
        [SerializeField, Min(0.05f)] private float winPulseDuration = 0.45f;

        public float WindUpDistance => windUpDistance;
        public float WindUpDuration => windUpDuration;
        public float AccelerationDuration => accelerationDuration;
        public AnimationCurve AccelerationCurve => accelerationCurve;
        public float StartStagger => startStagger;
        public float MaxSpeed => maxSpeed;
        public float MinimumSpinDuration => minimumSpinDuration;
        public float StopInterval => stopInterval;
        public float AnticipationExtraTime => anticipationExtraTime;
        public int ExtraStopSymbols => extraStopSymbols;
        public float Overshoot => overshoot;
        public float SettleDuration => settleDuration;
        public float MinStopDuration => minStopDuration;
        public float MaxStopDuration => Mathf.Max(maxStopDuration, minStopDuration);
        public float MotionStretch => motionStretch;
        public float WinPulseScale => winPulseScale;
        public float WinPulseDuration => winPulseDuration;

        /// <summary>Evaluates the acceleration curve safely, even if it was left empty in the Inspector.</summary>
        public float EvaluateAcceleration(float normalizedTime)
        {
            if (accelerationCurve == null || accelerationCurve.length == 0)
            {
                return Mathf.Clamp01(normalizedTime);
            }

            return Mathf.Clamp01(accelerationCurve.Evaluate(Mathf.Clamp01(normalizedTime)));
        }
    }
}
