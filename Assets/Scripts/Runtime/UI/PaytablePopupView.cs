using System;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>
    /// Shows what every symbol pays and the bonus rules.
    /// The rows themselves are created by the view factory from the config, so
    /// the paytable on screen can never disagree with the real numbers.
    /// </summary>
    public sealed class PaytablePopupView : MonoBehaviour
    {
        [SerializeField] private Button closeButton;

        public event Action Closed;

        public bool IsOpen => gameObject.activeSelf;

        public void Construct(Button close)
        {
            closeButton = close;
            gameObject.SetActive(false);
        }

        private void Start()
        {
            closeButton.onClick.AddListener(Hide);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            Closed?.Invoke();
        }
    }
}
