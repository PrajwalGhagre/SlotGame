using System;
using UnityEngine;

namespace SlotGame.Data
{
    /// <summary>
    /// References to every sprite the machine and UI use, grouped so the view
    /// factory can build the whole screen from one config asset.
    /// </summary>
    [Serializable]
    public sealed class MachineArt
    {
        [Header("Machine")]
        public Sprite background;
        public Sprite machineBody;
        public Sprite reelBackground;
        public Sprite machineFrame;
        public Sprite leverUp;
        public Sprite leverDown;

        [Header("UI")]
        public Sprite panel;
        public Sprite popup;
        [Tooltip("Soft round glow drawn behind winning symbols.")]
        public Sprite glow;
        public ButtonSprites blankButton;
        public ButtonSprites arrowLeftButton;
        public ButtonSprites arrowRightButton;
        public ButtonSprites closeButton;
        public ButtonSprites yesButton;
        public ButtonSprites noButton;

        [Header("Animations")]
        [Tooltip("Legacy looping clip played on the title text (Assets/Animations/TitlePulse.anim).")]
        public AnimationClip titleIdleAnimation;
    }

    /// <summary>The sprite for each state of a button (used with Unity's Sprite Swap transition).</summary>
    [Serializable]
    public sealed class ButtonSprites
    {
        public Sprite normal;
        public Sprite highlighted;
        public Sprite pressed;
        public Sprite disabled;
    }
}
