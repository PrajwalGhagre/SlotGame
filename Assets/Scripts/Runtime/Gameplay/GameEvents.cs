using System;
using SlotGame.Core.Session;

namespace SlotGame.Gameplay
{
    /// <summary>
    /// Small event hub owned by the SlotMachineController.
    /// Systems that only need to react (audio, effects, analytics later on)
    /// subscribe here instead of being called directly, so the controller does
    /// not need to know they exist.
    /// </summary>
    public sealed class GameEvents
    {
        public event Action<SpinRoundResult> SpinStarted;
        public event Action<int> ReelStopped;
        public event Action<SpinRoundResult> SpinCompleted;
        public event Action<int> WinShown;
        public event Action<int> BigWinShown;
        public event Action<int> FreeSpinsAwarded;
        public event Action<int> FreeSpinsFinished;
        public event Action NotEnoughCoins;
        public event Action ButtonClicked;
        public event Action LeverPulled;

        public void RaiseSpinStarted(SpinRoundResult round) => SpinStarted?.Invoke(round);
        public void RaiseReelStopped(int reelIndex) => ReelStopped?.Invoke(reelIndex);
        public void RaiseSpinCompleted(SpinRoundResult round) => SpinCompleted?.Invoke(round);
        public void RaiseWinShown(int amount) => WinShown?.Invoke(amount);
        public void RaiseBigWinShown(int amount) => BigWinShown?.Invoke(amount);
        public void RaiseFreeSpinsAwarded(int spins) => FreeSpinsAwarded?.Invoke(spins);
        public void RaiseFreeSpinsFinished(int totalWon) => FreeSpinsFinished?.Invoke(totalWon);
        public void RaiseNotEnoughCoins() => NotEnoughCoins?.Invoke();
        public void RaiseButtonClicked() => ButtonClicked?.Invoke();
        public void RaiseLeverPulled() => LeverPulled?.Invoke();
    }
}
