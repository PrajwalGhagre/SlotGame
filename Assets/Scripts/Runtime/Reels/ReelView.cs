using System;
using System.Collections.Generic;
using SlotGame.Core.Motion;
using SlotGame.Core.Reels;
using SlotGame.Data;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.Reels
{
    /// <summary>
    /// One spinning reel.
    ///
    /// The reel never moves real objects in a long strip. It keeps a small pool of
    /// cells (visible rows + one hidden cell above + one below) and a single number,
    /// <see cref="_position"/>, measured in symbols. Every frame the cells are placed
    /// from that number and given the right sprite, so the reel looks endless while
    /// only a handful of Images exist.
    ///
    /// Life of a spin (a small state machine driven from Update):
    ///   Waiting      - start delay so the reels kick off one after another
    ///   WindUp       - tiny pull back, like a real reel being kicked
    ///   Accelerating - speed follows the acceleration curve up to max speed
    ///   Spinning     - constant speed until a stop is requested
    ///   Stopping     - ease-out to just past the result (speed matched, no jerk)
    ///   Settling     - bounce back onto the exact result
    ///   Idle         - resting on the result
    /// The maths for landing exactly on the RNG result lives in ReelStopPlanner (Core).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ReelView : MonoBehaviour
    {
        private enum ReelState
        {
            Idle,
            Waiting,
            WindUp,
            Accelerating,
            Spinning,
            Stopping,
            Settling
        }

        [SerializeField] private int reelIndex;
        [SerializeField] private float cellHeight = 70f;
        [SerializeField] private int visibleRows = 3;

        [Tooltip("Cells ordered top to bottom: hidden buffer, visible rows, hidden buffer.")]
        [SerializeField] private List<SymbolView> cells = new List<SymbolView>();

        [Tooltip("Optional highlight behind the symbols, pulsed during anticipation.")]
        [SerializeField] private Image anticipationGlow;

        private ReelStrip _strip;
        private SymbolSpriteLookup _sprites;
        private SpinAnimationProfile _profile;

        private ReelState _state = ReelState.Idle;
        private double _position;
        private float _speed;
        private float _stateTime;
        private float _startDelay;
        private double _phaseStartPosition;

        // Symbol mapping (see ReelStopPlanner): slots >= _spliceFrom use _offsetAfter.
        private long _spliceFrom = long.MinValue;
        private long _offsetBefore;
        private long _offsetAfter;

        private bool _hasPendingStop;
        private int _pendingStopIndex;
        private bool _pendingQuickStop;
        private ReelStopPlan _stopPlan;
        private bool _anticipating;

        /// <summary>Raised once the reel has fully settled on its result.</summary>
        public event Action<ReelView> Stopped;

        public int ReelIndex => reelIndex;
        public bool IsSpinning => _state != ReelState.Idle;
        public int VisibleRows => visibleRows;
        public float CellHeight => cellHeight;
        public bool CanAcceptStop => _state != ReelState.Idle && _state != ReelState.Stopping && _state != ReelState.Settling;

        private int MiddleRow => visibleRows / 2;

        /// <summary>Called by the view factory right after the reel is created.</summary>
        public void Construct(int index, float heightOfCell, int rows, List<SymbolView> orderedCells, Image glow)
        {
            reelIndex = index;
            cellHeight = heightOfCell;
            visibleRows = rows;
            cells = orderedCells;
            anticipationGlow = glow;
        }

        /// <summary>Gives the reel its strip and art. Must be called before spinning.</summary>
        public void Initialize(ReelStrip strip, SymbolSpriteLookup sprites, SpinAnimationProfile profile, int startStopIndex)
        {
            _strip = strip ?? throw new ArgumentNullException(nameof(strip));
            _sprites = sprites ?? throw new ArgumentNullException(nameof(sprites));
            _profile = profile != null ? profile : throw new ArgumentNullException(nameof(profile));

            if (cells.Count != visibleRows + 2)
            {
                Debug.LogError($"Reel {reelIndex} needs {visibleRows + 2} cells but has {cells.Count}.", this);
            }

            RestOn(startStopIndex);
            SetAnticipation(false);
        }

        /// <summary>Starts spinning after the given delay (used for the left to right stagger).</summary>
        public void StartSpin(float delay)
        {
            if (_strip == null)
            {
                Debug.LogError("ReelView.StartSpin called before Initialize.", this);
                return;
            }

            ClearEffects();
            _hasPendingStop = false;
            _speed = 0f;
            _startDelay = Mathf.Max(0f, delay);
            EnterState(ReelState.Waiting);
        }

        /// <summary>
        /// Asks the reel to stop on the given strip index. If the reel is still
        /// speeding up, the request waits until it reaches full speed.
        /// </summary>
        /// <param name="quickStop">Skip the extra symbols for a faster stop (player pressed STOP).</param>
        public void RequestStop(int stripIndex, bool quickStop = false)
        {
            if (!CanAcceptStop)
            {
                return;
            }

            _hasPendingStop = true;
            _pendingStopIndex = stripIndex;
            _pendingQuickStop = quickStop;
        }

        /// <summary>The cell currently showing the given visible row (0 = top). Only valid when idle.</summary>
        public SymbolView GetCellAtRow(int row)
        {
            // cells[0] is the hidden buffer above the window, so visible row r is cells[r + 1].
            int index = row + 1;
            return index >= 0 && index < cells.Count ? cells[index] : null;
        }

        public void SetAnticipation(bool active)
        {
            _anticipating = active;
            if (anticipationGlow != null)
            {
                anticipationGlow.enabled = active;
            }
        }

        public void ClearEffects()
        {
            foreach (SymbolView cell in cells)
            {
                if (cell != null)
                {
                    cell.StopEffects();
                }
            }
        }

        private void Update()
        {
            if (_strip == null || _state == ReelState.Idle)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            _stateTime += deltaTime;

            switch (_state)
            {
                case ReelState.Waiting:
                    if (_stateTime >= _startDelay)
                    {
                        _phaseStartPosition = _position;
                        EnterState(_profile.WindUpDuration > 0f ? ReelState.WindUp : ReelState.Accelerating);
                    }

                    break;

                case ReelState.WindUp:
                    UpdateWindUp();
                    break;

                case ReelState.Accelerating:
                    UpdateAcceleration(deltaTime);
                    break;

                case ReelState.Spinning:
                    _position += _speed * deltaTime;
                    if (_hasPendingStop)
                    {
                        BeginStop();
                    }

                    break;

                case ReelState.Stopping:
                    UpdateStopping();
                    break;

                case ReelState.Settling:
                    UpdateSettling();
                    break;
            }

            if (_anticipating && anticipationGlow != null)
            {
                Color color = anticipationGlow.color;
                color.a = 0.25f + 0.25f * Mathf.Sin(Time.time * 12f);
                anticipationGlow.color = color;
            }

            RefreshCells();
        }

        private void UpdateWindUp()
        {
            // Pull the reel back a little (position goes down) with an ease-out.
            float t = Mathf.Clamp01(_stateTime / _profile.WindUpDuration);
            _position = _phaseStartPosition - _profile.WindUpDistance * Easing.OutQuad(t);

            if (t >= 1f)
            {
                EnterState(ReelState.Accelerating);
            }
        }

        private void UpdateAcceleration(float deltaTime)
        {
            float t = Mathf.Clamp01(_stateTime / _profile.AccelerationDuration);
            _speed = _profile.MaxSpeed * _profile.EvaluateAcceleration(t);
            _position += _speed * deltaTime;

            if (t >= 1f)
            {
                _speed = _profile.MaxSpeed;
                EnterState(ReelState.Spinning);
                if (_hasPendingStop)
                {
                    BeginStop();
                }
            }
        }

        private void BeginStop()
        {
            _hasPendingStop = false;
            int extraSymbols = _pendingQuickStop ? 0 : _profile.ExtraStopSymbols;

            _stopPlan = ReelStopPlanner.Plan(_position, _speed, _pendingStopIndex, _strip.Length, visibleRows,
                extraSymbols, _profile.Overshoot, _profile.MinStopDuration, _profile.MaxStopDuration);

            // Splice the strip above the window so the landing slot shows the result.
            // Cells already on screen keep the old mapping, so nothing visibly jumps.
            _offsetBefore = CurrentOffsetFor(_stopPlan.SpliceFrom - 1);
            _offsetAfter = _stopPlan.NewIndexOffset;
            _spliceFrom = _stopPlan.SpliceFrom;

            _phaseStartPosition = _position;
            EnterState(ReelState.Stopping);
        }

        private void UpdateStopping()
        {
            // Ease-out-cubic from where we are to slightly past the landing slot.
            // The duration was chosen by the planner so the starting speed matches the spin speed.
            float t = Mathf.Clamp01(_stateTime / _stopPlan.Duration);
            double travel = _stopPlan.Distance + _profile.Overshoot;
            _position = _phaseStartPosition + travel * Easing.OutCubic(t);
            _speed = _profile.MaxSpeed * (1f - t);

            if (t >= 1f)
            {
                _phaseStartPosition = _position;
                EnterState(_profile.SettleDuration > 0f && _profile.Overshoot > 0f ? ReelState.Settling : ReelState.Idle);
                if (_state == ReelState.Idle)
                {
                    FinishStop();
                }
            }
        }

        private void UpdateSettling()
        {
            // Bounce back from the overshoot onto the exact result.
            float t = Mathf.Clamp01(_stateTime / _profile.SettleDuration);
            _position = Lerp(_phaseStartPosition, _stopPlan.LandingPosition, Easing.SmoothStep(t));
            _speed = 0f;

            if (t >= 1f)
            {
                FinishStop();
            }
        }

        private void FinishStop()
        {
            int landedIndex = ReelStopPlanner.SlotToStripIndex((long)Math.Round(_stopPlan.LandingPosition),
                _offsetAfter, _strip.Length);
            RestOn(landedIndex);
            SetAnticipation(false);
            Stopped?.Invoke(this);
        }

        /// <summary>Snaps the reel to rest on a strip index and resets the mapping, keeping numbers small.</summary>
        private void RestOn(int stripIndex)
        {
            _state = ReelState.Idle;
            _speed = 0f;
            _position = _strip.WrapIndex(stripIndex);
            _spliceFrom = long.MinValue;
            _offsetBefore = 0;
            _offsetAfter = 0;
            RefreshCells();
        }

        private void EnterState(ReelState state)
        {
            _state = state;
            _stateTime = 0f;
        }

        private long CurrentOffsetFor(long slot)
        {
            return slot >= _spliceFrom ? _offsetAfter : _offsetBefore;
        }

        /// <summary>
        /// Places every cell and gives it the right sprite for the current position.
        /// Cell k (k = -1 is the hidden one above the window) shows virtual slot
        /// floor(position) + middleRow - k and sits (middleRow - k - fraction) cells
        /// above the centre. As the position grows the fraction grows, so the
        /// symbols slide down, and when it wraps the next symbol takes over.
        /// </summary>
        private void RefreshCells()
        {
            if (_strip == null || _sprites == null)
            {
                return;
            }

            long baseSlot = (long)Math.Floor(_position);
            float fraction = (float)(_position - baseSlot);
            float stretch = _profile != null ? _profile.MotionStretch * Mathf.Clamp01(_speed / _profile.MaxSpeed) : 0f;

            for (int i = 0; i < cells.Count; i++)
            {
                int k = i - 1;
                long slot = baseSlot + MiddleRow - k;
                int stripIndex = ReelStopPlanner.SlotToStripIndex(slot, CurrentOffsetFor(slot), _strip.Length);

                SymbolView cell = cells[i];
                var symbol = _strip.GetSymbol(stripIndex);
                cell.SetSymbol(symbol, _sprites.GetSprite(symbol));
                cell.RectTransform.anchoredPosition = new Vector2(0f, (MiddleRow - k - fraction) * cellHeight);
                if (_state != ReelState.Idle)
                {
                    cell.SetMotionStretch(stretch);
                }
            }
        }

        private static double Lerp(double from, double to, float t)
        {
            return from + (to - from) * t;
        }
    }
}
