using System.Collections;
using System.Collections.Generic;
using SlotGame.Core.Payouts;
using SlotGame.Core.Random;
using SlotGame.Core.Reels;
using SlotGame.Core.Session;
using SlotGame.Core.Spin;
using SlotGame.Core.Symbols;
using SlotGame.Data;
using SlotGame.Reels;
using SlotGame.View;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SlotGame.Gameplay
{
    /// <summary>
    /// Runs one round from button press to win celebration.
    ///
    /// The important design choice: the result is decided FIRST by the plain C#
    /// SlotGameSession (RNG, payout, wallet, free spins). This class only plays
    /// that result back: spin the reels, stop them one by one on the chosen
    /// symbols, then show the win. The animation can never change the outcome.
    /// </summary>
    public sealed class SlotMachineController : MonoBehaviour
    {
        private const float DelayBeforeFreeSpin = 0.8f;
        private const float DelayBeforeAutoSpin = 0.5f;
        private const float SmallWinPause = 0.9f;
        private const float PopupSeconds = 2.5f;

        private SlotMachineConfig _config;
        private SlotMachineView _view;
        private SlotGameSession _session;
        private SpinAnimationProfile _profile;
        private BonusRules _bonusRules;
        private SymbolSpriteLookup _sprites;
        private GameEvents _events;

        private bool _isRoundRunning;
        private bool _reelsMoving;
        private bool _stopEarlyRequested;
        private bool _autoSpin;
        private int _reelsStillSpinning;

        public GameEvents Events => _events;
        public bool IsRoundRunning => _isRoundRunning;

        /// <summary>Wires the controller to its view and game session. Called once by the bootstrap.</summary>
        public void Initialize(SlotMachineConfig config, SlotMachineView view, SlotGameSession session,
            IReadOnlyList<ReelStrip> strips, GameEvents events, IRandomProvider cosmeticRandom)
        {
            _config = config;
            _view = view;
            _session = session;
            _events = events;
            _profile = config.AnimationProfile;
            _bonusRules = config.BuildBonusRules();
            _sprites = new SymbolSpriteLookup(config.Symbols);

            for (int i = 0; i < _view.Reels.Count; i++)
            {
                ReelView reel = _view.Reels[i];

                // Start every reel on a random stop so the first screen does not look staged.
                int startStop = cosmeticRandom.NextInt(0, strips[i].Length);
                reel.Initialize(strips[i], _sprites, _profile, startStop);
                reel.Stopped += HandleReelStopped;
            }

            _view.Hud.SpinClicked += HandleSpinPressed;
            _view.Hud.BetUpClicked += HandleBetUp;
            _view.Hud.BetDownClicked += HandleBetDown;
            _view.Hud.PaytableClicked += HandlePaytable;
            _view.Hud.AutoSpinClicked += HandleAutoSpinToggle;
            _view.Lever.Clicked += HandleSpinPressed;

            _view.Hud.SetBalance(_session.Wallet.Balance, false);
            _view.Hud.SetBet(_session.Wallet.CurrentBet);
            _view.Hud.SetLastWin(0, false);
            _view.Hud.SetMessage("PRESS SPIN TO PLAY");
            _view.Hud.SetSpinButtonMode(false);
            _view.Hud.SetAutoSpin(false);
            _view.SetPaylineHighlighted(false);
            RefreshControls();
        }

        private void OnDestroy()
        {
            if (_view == null) return;

            foreach (ReelView reel in _view.Reels)
            {
                if (reel != null) reel.Stopped -= HandleReelStopped;
            }
        }

        private void Update()
        {
            if (_session == null) return;

            // Keep UI buttons from staying "selected". Otherwise pressing Space would
            // also trigger the last clicked button through Unity's Submit input,
            // and one key press could spin twice.
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                HandleSpinPressed();
            }
        }

        // ------------------------------------------------------------------
        // Input
        // ------------------------------------------------------------------

        private void HandleSpinPressed()
        {
            if (_isRoundRunning)
            {
                // Second press while the reels move = stop them early (classic slot behaviour).
                if (_reelsMoving)
                {
                    _stopEarlyRequested = true;
                }

                return;
            }

            if (_view.AnyPopupOpen)
            {
                return;
            }

            _events.RaiseButtonClicked();
            TryStartRound();
        }

        private void HandleBetUp()
        {
            if (!CanChangeSettings()) return;

            _events.RaiseButtonClicked();
            _session.Wallet.IncreaseBet();
            _view.Hud.SetBet(_session.Wallet.CurrentBet);
            RefreshControls();
        }

        private void HandleBetDown()
        {
            if (!CanChangeSettings()) return;

            _events.RaiseButtonClicked();
            _session.Wallet.DecreaseBet();
            _view.Hud.SetBet(_session.Wallet.CurrentBet);
            RefreshControls();
        }

        private void HandlePaytable()
        {
            if (!CanChangeSettings() || _view.AnyPopupOpen) return;

            _events.RaiseButtonClicked();
            _view.PaytablePopup.Show();
        }

        private void HandleAutoSpinToggle()
        {
            _events.RaiseButtonClicked();
            _autoSpin = !_autoSpin;
            _view.Hud.SetAutoSpin(_autoSpin);

            if (_autoSpin && !_isRoundRunning && !_view.AnyPopupOpen)
            {
                TryStartRound();
            }
        }

        private bool CanChangeSettings()
        {
            return !_isRoundRunning && !_session.FreeSpins.IsActive;
        }

        // ------------------------------------------------------------------
        // Round flow
        // ------------------------------------------------------------------

        private void TryStartRound()
        {
            if (!_session.CanPlayRound)
            {
                SetAutoSpin(false);
                _events.RaiseNotEnoughCoins();
                _view.Hud.SetMessage("NOT ENOUGH COINS");

                if (_session.Wallet.Balance < _session.Wallet.BetLevels[0])
                {
                    _view.ConfirmDialog.Show($"Out of coins!\nRefill to {_config.StartingBalance:N0} coins?", RefillCoins, null);
                }

                return;
            }

            // The outcome is decided here, before any animation starts.
            SpinRoundResult round = _session.PlayRound();
            StartCoroutine(PlayRoundRoutine(round));
        }

        private IEnumerator PlayRoundRoutine(SpinRoundResult round)
        {
            _isRoundRunning = true;
            _reelsMoving = true;
            _stopEarlyRequested = false;

            ClearWinPresentation();
            RefreshControls();
            _view.Hud.SetSpinButtonMode(true);
            _view.Hud.SetLastWin(0, false);

            // Show the balance after the bet; the win is added once the reels stop.
            _view.Hud.SetBalance(round.BalanceAfter - round.TotalPayout, true);
            _view.Hud.SetMessage(round.WasFreeSpin
                ? $"FREE SPIN  |  {round.FreeSpinsRemaining} LEFT  |  WINS x{round.AppliedMultiplier}"
                : "GOOD LUCK!");

            _view.Lever.PlayPull();
            _events.RaiseLeverPulled();
            _events.RaiseSpinStarted(round);

            yield return SpinAndStopReels(round);

            _reelsMoving = false;
            _view.Hud.SetSpinButtonMode(false);
            _events.RaiseSpinCompleted(round);

            yield return PresentResult(round);

            _isRoundRunning = false;
            RefreshControls();

            yield return ContinueAutomatically();
        }

        /// <summary>
        /// Starts the reels left to right, lets them spin, then stops them one by one
        /// (staggered) on the stop indices the RNG picked. The last reel may wait
        /// longer for suspense when a win is still possible.
        /// </summary>
        private IEnumerator SpinAndStopReels(SpinRoundResult round)
        {
            IReadOnlyList<ReelView> reels = _view.Reels;
            _reelsStillSpinning = reels.Count;

            for (int i = 0; i < reels.Count; i++)
            {
                reels[i].StartSpin(i * _profile.StartStagger);
            }

            yield return WaitUnlessStoppedEarly(_profile.MinimumSpinDuration);

            bool anticipate = AnticipationRule.ShouldAnticipateLastReel(round.Spin, _bonusRules);

            for (int i = 0; i < reels.Count; i++)
            {
                bool isLastReel = i == reels.Count - 1;

                if (isLastReel && anticipate && !_stopEarlyRequested)
                {
                    reels[i].SetAnticipation(true);
                    _view.Hud.SetMessage("...");
                    yield return WaitUnlessStoppedEarly(_profile.AnticipationExtraTime);
                }

                reels[i].RequestStop(round.Spin.StopIndices[i], _stopEarlyRequested);

                if (!isLastReel)
                {
                    yield return WaitUnlessStoppedEarly(_profile.StopInterval);
                }
            }

            // If STOP was pressed after some reels were already told to stop normally,
            // ask again with quick stop so the remaining ones hurry up too.
            if (_stopEarlyRequested)
            {
                for (int i = 0; i < reels.Count; i++)
                {
                    reels[i].RequestStop(round.Spin.StopIndices[i], true);
                }
            }

            while (_reelsStillSpinning > 0)
            {
                yield return null;
            }
        }

        private IEnumerator WaitUnlessStoppedEarly(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds && !_stopEarlyRequested)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private void HandleReelStopped(ReelView reel)
        {
            _reelsStillSpinning = Mathf.Max(0, _reelsStillSpinning - 1);
            _events.RaiseReelStopped(reel.ReelIndex);
        }

        private IEnumerator PresentResult(SpinRoundResult round)
        {
            _view.Hud.SetBalance(round.BalanceAfter, true);

            if (round.IsWin)
            {
                HighlightPositions(round.Win.WinningPositions, round.Win.WinningSymbol);
                _view.SetPaylineHighlighted(true);
                _view.Hud.SetLastWin(round.TotalPayout, true);

                string label = round.Win.UsedWild ? "WILD WIN" : "WIN";
                string multiplierNote = round.AppliedMultiplier > 1 ? $" (x{round.AppliedMultiplier})" : string.Empty;
                _view.Hud.SetMessage($"{label} {round.TotalPayout:N0}!{multiplierNote}");

                bool isBigWin = round.TotalPayout >= round.Bet * _config.BigWinBetMultiple;
                if (isBigWin)
                {
                    _events.RaiseBigWinShown(round.TotalPayout);
                    string symbolName = GetDisplayName(round);
                    yield return ShowPopupAndWait("BIG WIN!", round.TotalPayout, $"3 x {symbolName}");
                }
                else
                {
                    _events.RaiseWinShown(round.TotalPayout);
                    yield return new WaitForSeconds(SmallWinPause);
                }
            }
            else
            {
                _view.Hud.SetMessage(round.WasFreeSpin ? "NO WIN THIS FREE SPIN" : "NO WIN, TRY AGAIN");
            }

            if (round.FreeSpinsAwarded > 0)
            {
                HighlightPositions(round.ScatterPositions, _bonusRules.ScatterSymbol);
                _events.RaiseFreeSpinsAwarded(round.FreeSpinsAwarded);
                _view.Hud.SetMessage($"{round.FreeSpinsAwarded} FREE SPINS!");
                yield return ShowPopupAndWait("FREE SPINS!", round.FreeSpinsAwarded,
                    $"All wins x{_session.FreeSpins.ActiveMultiplier}");
            }
            else if (round.WasFreeSpin && round.FreeSpinsRemaining == 0)
            {
                int total = _session.FreeSpins.TotalWonThisFeature;
                _events.RaiseFreeSpinsFinished(total);
                yield return ShowPopupAndWait("FREE SPINS OVER", total, "Total won");
            }
        }

        private IEnumerator ShowPopupAndWait(string title, int amount, string subtitle)
        {
            _view.WinPopup.Show(title, amount, subtitle, PopupSeconds);
            while (_view.WinPopup.IsOpen)
            {
                yield return null;
            }
        }

        /// <summary>Plays free spins on their own, and keeps auto spin going while it is on.</summary>
        private IEnumerator ContinueAutomatically()
        {
            if (_session.FreeSpins.IsActive)
            {
                yield return new WaitForSeconds(DelayBeforeFreeSpin);
                if (!_isRoundRunning && !_view.AnyPopupOpen)
                {
                    TryStartRound();
                }

                yield break;
            }

            if (_autoSpin)
            {
                yield return new WaitForSeconds(DelayBeforeAutoSpin);
                if (_autoSpin && !_isRoundRunning && !_view.AnyPopupOpen)
                {
                    TryStartRound();
                }
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private void HighlightPositions(IReadOnlyList<SlotPosition> positions, SymbolType symbol)
        {
            var winning = new HashSet<(int reel, int row)>();
            foreach (SlotPosition position in positions)
            {
                winning.Add((position.Reel, position.Row));
            }

            Color glow = _sprites.GetGlowColor(symbol);
            for (int reelIndex = 0; reelIndex < _view.Reels.Count; reelIndex++)
            {
                ReelView reel = _view.Reels[reelIndex];
                for (int row = 0; row < reel.VisibleRows; row++)
                {
                    SymbolView cell = reel.GetCellAtRow(row);
                    if (cell == null) continue;

                    if (winning.Contains((reelIndex, row)))
                    {
                        cell.PlayWinEffect(glow, _profile.WinPulseScale, _profile.WinPulseDuration);
                    }
                    else
                    {
                        cell.SetDimmed(true);
                    }
                }
            }
        }

        private void ClearWinPresentation()
        {
            foreach (ReelView reel in _view.Reels)
            {
                reel.ClearEffects();
            }

            _view.SetPaylineHighlighted(false);
        }

        private void RefreshControls()
        {
            bool idle = !_isRoundRunning && !_session.FreeSpins.IsActive;
            _view.Hud.SetBetControls(idle, _session.Wallet.IsAtMinBet, _session.Wallet.IsAtMaxBet);
            _view.Hud.SetSpinInteractable(!_session.FreeSpins.IsActive || _reelsMoving);
            _view.Lever.SetInteractable(idle || _reelsMoving);
        }

        private void RefillCoins()
        {
            _session.Wallet.ResetBalance(_config.StartingBalance);
            _view.Hud.SetBalance(_session.Wallet.Balance, true);
            _view.Hud.SetMessage("COINS REFILLED, GOOD LUCK!");
            RefreshControls();
        }

        private void SetAutoSpin(bool enabled)
        {
            _autoSpin = enabled;
            _view.Hud.SetAutoSpin(enabled);
        }

        private string GetDisplayName(SpinRoundResult round)
        {
            SymbolDefinition definition = _config.GetDefinition(round.Win.WinningSymbol);
            return definition != null ? definition.DisplayName.ToUpperInvariant() : round.Win.WinningSymbol.ToString();
        }
    }
}
