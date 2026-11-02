using System.Collections;
using SlotGame.Core.Motion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>
    /// Big centred popup for BIG WIN, FREE SPINS and the free spins summary.
    /// It pops in with an overshoot, counts the amount up, and closes by itself
    /// after a few seconds or when the player clicks it.
    /// </summary>
    public sealed class WinPopupView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private Button closeArea;

        private Coroutine _routine;
        private bool _closeRequested;

        public bool IsOpen { get; private set; }

        public void Construct(CanvasGroup group, RectTransform panelTransform, TMP_Text title, TMP_Text amount,
            TMP_Text subtitle, Button clickToClose)
        {
            canvasGroup = group;
            panel = panelTransform;
            titleText = title;
            amountText = amount;
            subtitleText = subtitle;
            closeArea = clickToClose;
            HideImmediate();
        }

        private void Start()
        {
            closeArea.onClick.AddListener(() => _closeRequested = true);
        }

        /// <summary>Shows the popup. amount below zero hides the number line.</summary>
        public void Show(string title, int amount, string subtitle, float autoCloseSeconds)
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            gameObject.SetActive(true);
            _routine = StartCoroutine(ShowRoutine(title, amount, subtitle, autoCloseSeconds));
        }

        public void HideImmediate()
        {
            IsOpen = false;
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            gameObject.SetActive(false);
        }

        private IEnumerator ShowRoutine(string title, int amount, string subtitle, float autoCloseSeconds)
        {
            IsOpen = true;
            _closeRequested = false;

            titleText.text = title;
            subtitleText.text = subtitle;
            amountText.gameObject.SetActive(amount >= 0);
            amountText.text = "0";

            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            // Pop in: fade + scale with a small overshoot (OutBack).
            StartCoroutine(UiAnimations.Fade(canvasGroup, 0f, 1f, 0.2f));
            yield return UiAnimations.ScaleTo(panel, Vector3.one * 0.6f, Vector3.one, 0.35f, t => Easing.OutBack(t));

            if (amount >= 0)
            {
                yield return UiAnimations.CountTo(amountText, 0, amount, 1.0f, "{0:N0}");
            }

            float timer = 0f;
            while (timer < autoCloseSeconds && !_closeRequested)
            {
                timer += Time.unscaledDeltaTime;
                yield return null;
            }

            yield return UiAnimations.Fade(canvasGroup, 1f, 0f, 0.2f);
            HideImmediate();
            _routine = null;
        }
    }
}
