using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>Simple yes / no dialog (used for "out of coins, refill?").</summary>
    public sealed class ConfirmDialogView : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        private Action _onYes;
        private Action _onNo;

        public bool IsOpen => gameObject.activeSelf;

        public void Construct(TMP_Text message, Button yes, Button no)
        {
            messageText = message;
            yesButton = yes;
            noButton = no;
            gameObject.SetActive(false);
        }

        private void Start()
        {
            yesButton.onClick.AddListener(() => Close(_onYes));
            noButton.onClick.AddListener(() => Close(_onNo));
        }

        public void Show(string message, Action onYes, Action onNo)
        {
            messageText.text = message;
            _onYes = onYes;
            _onNo = onNo;
            gameObject.SetActive(true);
        }

        private void Close(Action callback)
        {
            gameObject.SetActive(false);
            callback?.Invoke();
        }
    }
}
