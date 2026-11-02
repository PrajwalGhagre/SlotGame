using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>
    /// The side panel and the message display on the machine.
    /// It only shows values and forwards button clicks as events; it never
    /// changes game state by itself.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        private const string CoinFormat = "{0:N0}";

        [Header("Texts")]
        [SerializeField] private TMP_Text balanceText;
        [SerializeField] private TMP_Text betText;
        [SerializeField] private TMP_Text lastWinText;
        [SerializeField] private TMP_Text machineMessageText;
        [SerializeField] private TMP_Text spinButtonLabel;
        [SerializeField] private TMP_Text autoButtonLabel;

        [Header("Buttons")]
        [SerializeField] private Button spinButton;
        [SerializeField] private Button betDownButton;
        [SerializeField] private Button betUpButton;
        [SerializeField] private Button paytableButton;
        [SerializeField] private Button autoSpinButton;

        private Coroutine _balanceRoutine;
        private Coroutine _winRoutine;
        private int _shownBalance;

        public event Action SpinClicked;
        public event Action BetDownClicked;
        public event Action BetUpClicked;
        public event Action PaytableClicked;
        public event Action AutoSpinClicked;

        /// <summary>Called by the view factory when the HUD is built from code.</summary>
        public void Construct(TMP_Text balance, TMP_Text bet, TMP_Text lastWin, TMP_Text machineMessage,
            Button spin, TMP_Text spinLabel, Button betDown, Button betUp, Button paytable, Button autoSpin,
            TMP_Text autoLabel)
        {
            balanceText = balance;
            betText = bet;
            lastWinText = lastWin;
            machineMessageText = machineMessage;
            spinButton = spin;
            spinButtonLabel = spinLabel;
            betDownButton = betDown;
            betUpButton = betUp;
            paytableButton = paytable;
            autoSpinButton = autoSpin;
            autoButtonLabel = autoLabel;
        }

        // Start (not Awake) so this also works when the HUD is built from code and
        // its fields are filled in by Construct() right after AddComponent.
        private void Start()
        {
            spinButton.onClick.AddListener(() => SpinClicked?.Invoke());
            betDownButton.onClick.AddListener(() => BetDownClicked?.Invoke());
            betUpButton.onClick.AddListener(() => BetUpClicked?.Invoke());
            paytableButton.onClick.AddListener(() => PaytableClicked?.Invoke());
            autoSpinButton.onClick.AddListener(() => AutoSpinClicked?.Invoke());
        }

        public void SetBalance(int balance, bool animate)
        {
            if (_balanceRoutine != null)
            {
                StopCoroutine(_balanceRoutine);
                _balanceRoutine = null;
            }

            if (animate && isActiveAndEnabled && balance != _shownBalance)
            {
                _balanceRoutine = StartCoroutine(UiAnimations.CountTo(balanceText, _shownBalance, balance, 0.6f, CoinFormat));
            }
            else
            {
                balanceText.text = string.Format(CoinFormat, balance);
            }

            _shownBalance = balance;
        }

        public void SetBet(int bet)
        {
            betText.text = string.Format(CoinFormat, bet);
            if (isActiveAndEnabled)
            {
                StartCoroutine(UiAnimations.Punch(betText.transform, 0.15f, 0.15f));
            }
        }

        public void SetLastWin(int amount, bool animate)
        {
            if (_winRoutine != null)
            {
                StopCoroutine(_winRoutine);
                _winRoutine = null;
            }

            if (animate && amount > 0 && isActiveAndEnabled)
            {
                _winRoutine = StartCoroutine(UiAnimations.CountTo(lastWinText, 0, amount, 0.8f, CoinFormat));
            }
            else
            {
                lastWinText.text = string.Format(CoinFormat, amount);
            }
        }

        public void SetMessage(string message)
        {
            machineMessageText.text = message;
        }

        /// <summary>SPIN while idle, STOP while reels are moving (to stop them early).</summary>
        public void SetSpinButtonMode(bool reelsSpinning)
        {
            spinButtonLabel.text = reelsSpinning ? "STOP" : "SPIN";
        }

        public void SetAutoSpin(bool enabled)
        {
            autoButtonLabel.text = enabled ? "AUTO: ON" : "AUTO: OFF";
        }

        public void SetSpinInteractable(bool interactable)
        {
            spinButton.interactable = interactable;
        }

        /// <summary>Enables the bet arrows, respecting the min and max bet.</summary>
        public void SetBetControls(bool allowChanges, bool atMinBet, bool atMaxBet)
        {
            betDownButton.interactable = allowChanges && !atMinBet;
            betUpButton.interactable = allowChanges && !atMaxBet;
            paytableButton.interactable = allowChanges;
        }

        public IEnumerator FlashMessage(string message, int flashes)
        {
            for (int i = 0; i < flashes; i++)
            {
                SetMessage(message);
                yield return new WaitForSeconds(0.18f);
                SetMessage(string.Empty);
                yield return new WaitForSeconds(0.12f);
            }

            SetMessage(message);
        }
    }
}
