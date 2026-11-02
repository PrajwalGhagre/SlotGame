using System.Collections;
using SlotGame.Core.Symbols;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.Reels
{
    /// <summary>
    /// One cell on a reel. It shows a symbol sprite and can play the win effect
    /// (a pulse plus a soft coloured glow behind the symbol).
    /// The reel moves the cell around and swaps its sprite; the cell itself only
    /// knows how to draw.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SymbolView : MonoBehaviour
    {
        [SerializeField] private Image symbolImage;
        [SerializeField] private Image glowImage;

        private RectTransform _rectTransform;
        private Coroutine _effectRoutine;
        private bool _hasSymbol;

        public SymbolType CurrentSymbol { get; private set; }

        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = (RectTransform)transform;
                }

                return _rectTransform;
            }
        }

        /// <summary>Called by the view factory when the cell is created from code.</summary>
        public void Construct(Image symbol, Image glow)
        {
            symbolImage = symbol;
            glowImage = glow;
            SetGlowVisible(false);
        }

        /// <summary>Swaps the sprite only when the symbol actually changes, to keep spinning cheap.</summary>
        public void SetSymbol(SymbolType type, Sprite sprite)
        {
            if (_hasSymbol && CurrentSymbol == type)
            {
                return;
            }

            _hasSymbol = true;
            CurrentSymbol = type;
            symbolImage.sprite = sprite;
        }

        /// <summary>Stretches the symbol vertically while the reel is fast, a cheap motion blur.</summary>
        public void SetMotionStretch(float stretch)
        {
            symbolImage.rectTransform.localScale = new Vector3(1f, 1f + stretch, 1f);
        }

        public void PlayWinEffect(Color glowColor, float pulseScale, float pulseDuration)
        {
            StopEffects();
            _effectRoutine = StartCoroutine(WinPulseRoutine(glowColor, pulseScale, pulseDuration));
        }

        /// <summary>Resets the cell to its plain look.</summary>
        public void StopEffects()
        {
            if (_effectRoutine != null)
            {
                StopCoroutine(_effectRoutine);
                _effectRoutine = null;
            }

            symbolImage.rectTransform.localScale = Vector3.one;
            symbolImage.color = Color.white;
            SetGlowVisible(false);
        }

        /// <summary>Greys the symbol out (used to make the winning ones stand out).</summary>
        public void SetDimmed(bool dimmed)
        {
            symbolImage.color = dimmed ? new Color(0.55f, 0.55f, 0.6f, 1f) : Color.white;
        }

        private IEnumerator WinPulseRoutine(Color glowColor, float pulseScale, float pulseDuration)
        {
            SetGlowVisible(true);
            symbolImage.color = Color.white;
            float time = 0f;

            // Loops until StopEffects() is called (at the start of the next spin).
            while (true)
            {
                time += Time.deltaTime;

                // 0 -> 1 -> 0 every pulseDuration seconds, smooth at both ends.
                float wave = 0.5f - 0.5f * Mathf.Cos(time / pulseDuration * Mathf.PI * 2f);
                float scale = Mathf.Lerp(1f, pulseScale, wave);
                symbolImage.rectTransform.localScale = new Vector3(scale, scale, 1f);

                Color glow = glowColor;
                glow.a = Mathf.Lerp(0.35f, 0.9f, wave);
                glowImage.color = glow;
                glowImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.1f, 1.3f, wave);
                yield return null;
            }
        }

        private void SetGlowVisible(bool visible)
        {
            if (glowImage != null)
            {
                glowImage.enabled = visible;
            }
        }
    }
}
