using SlotGame.Core.Session;
using SlotGame.Data;
using SlotGame.Gameplay;
using UnityEngine;

namespace SlotGame.Audio
{
    /// <summary>
    /// Plays the sound effects. It only listens to GameEvents, so the game code
    /// never calls audio directly. Press M in game to mute.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private SoundLibrary _sounds;
        private GameEvents _events;
        private AudioSource _effectsSource;
        private AudioSource _loopSource;
        private bool _muted;

        public bool IsMuted => _muted;

        public void Initialize(SoundLibrary sounds, GameEvents events)
        {
            _sounds = sounds ?? new SoundLibrary();
            Unsubscribe();
            _events = events;

            _effectsSource = CreateSource("Effects");
            _loopSource = CreateSource("SpinLoop");
            _loopSource.loop = true;
            _loopSource.clip = _sounds.reelSpinLoop;
            _loopSource.volume = _sounds.spinLoopVolume * _sounds.masterVolume;

            Subscribe();
        }

        public void ToggleMute()
        {
            _muted = !_muted;
            AudioListener.volume = _muted ? 0f : 1f;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                ToggleMute();
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_events == null) return;

            _events.SpinStarted += HandleSpinStarted;
            _events.ReelStopped += HandleReelStopped;
            _events.SpinCompleted += HandleSpinCompleted;
            _events.WinShown += HandleWinShown;
            _events.BigWinShown += HandleBigWinShown;
            _events.FreeSpinsAwarded += HandleFreeSpinsAwarded;
            _events.NotEnoughCoins += HandleNotEnoughCoins;
            _events.ButtonClicked += HandleButtonClicked;
            _events.LeverPulled += HandleLeverPulled;
        }

        private void Unsubscribe()
        {
            if (_events == null) return;

            _events.SpinStarted -= HandleSpinStarted;
            _events.ReelStopped -= HandleReelStopped;
            _events.SpinCompleted -= HandleSpinCompleted;
            _events.WinShown -= HandleWinShown;
            _events.BigWinShown -= HandleBigWinShown;
            _events.FreeSpinsAwarded -= HandleFreeSpinsAwarded;
            _events.NotEnoughCoins -= HandleNotEnoughCoins;
            _events.ButtonClicked -= HandleButtonClicked;
            _events.LeverPulled -= HandleLeverPulled;
        }

        private void HandleSpinStarted(SpinRoundResult round)
        {
            if (_loopSource.clip != null && !_loopSource.isPlaying)
            {
                _loopSource.Play();
            }
        }

        private void HandleReelStopped(int reelIndex)
        {
            Play(_sounds.reelStop);
        }

        private void HandleSpinCompleted(SpinRoundResult round)
        {
            _loopSource.Stop();
        }

        private void HandleWinShown(int amount) => Play(_sounds.winSmall);
        private void HandleBigWinShown(int amount) => Play(_sounds.winBig);
        private void HandleFreeSpinsAwarded(int spins) => Play(_sounds.freeSpins);
        private void HandleNotEnoughCoins() => Play(_sounds.error);
        private void HandleButtonClicked() => Play(_sounds.buttonClick);
        private void HandleLeverPulled() => Play(_sounds.leverPull);

        private void Play(AudioClip clip)
        {
            if (clip != null && _effectsSource != null)
            {
                _effectsSource.PlayOneShot(clip, _sounds.masterVolume);
            }
        }

        private AudioSource CreateSource(string sourceName)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
