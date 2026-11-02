using System;
using System.Collections;
using SlotGame.Core.Motion;
using TMPro;
using UnityEngine;

namespace SlotGame.UI
{
    /// <summary>
    /// Tiny coroutine tweens for the UI. Writing these by hand keeps the project
    /// free of third party tween libraries; each one is only a few lines.
    /// All of them use unscaled time so they still work if the game is paused.
    /// </summary>
    public static class UiAnimations
    {
        public static IEnumerator ScaleTo(Transform target, Vector3 from, Vector3 to, float duration,
            Func<float, float> ease)
        {
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = ease(Mathf.Clamp01(time / duration));
                target.localScale = Vector3.LerpUnclamped(from, to, t);
                yield return null;
            }

            target.localScale = to;
        }

        public static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Easing.SmoothStep(time / duration));
                yield return null;
            }

            group.alpha = to;
        }

        /// <summary>Counts a number up (or down) in a text field, like coins rolling in.</summary>
        public static IEnumerator CountTo(TMP_Text label, int from, int to, float duration, string format)
        {
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Easing.OutQuad(time / duration);
                int value = Mathf.RoundToInt(Mathf.Lerp(from, to, t));
                label.text = string.Format(format, value);
                yield return null;
            }

            label.text = string.Format(format, to);
        }

        /// <summary>Quick "pop" used when a value changes.</summary>
        public static IEnumerator Punch(Transform target, float amount, float duration)
        {
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                float scale = 1f + amount * Mathf.Sin(t * Mathf.PI);
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            target.localScale = Vector3.one;
        }
    }
}
