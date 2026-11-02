using System;
using UnityEngine;

namespace SlotGame.Data
{
    /// <summary>All sound effects in one place. Any clip can be left empty.</summary>
    [Serializable]
    public sealed class SoundLibrary
    {
        public AudioClip buttonClick;
        public AudioClip leverPull;
        public AudioClip reelSpinLoop;
        public AudioClip reelStop;
        public AudioClip winSmall;
        public AudioClip winBig;
        public AudioClip freeSpins;
        public AudioClip error;

        [Range(0f, 1f)] public float masterVolume = 0.8f;
        [Range(0f, 1f)] public float spinLoopVolume = 0.35f;
    }
}
