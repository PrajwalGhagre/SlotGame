using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>
    /// The machine's lever. Clicking it spins the reels, and every spin shows a
    /// short pull animation by swapping between the "up" and "down" sprites
    /// that came with the art.
    /// </summary>
    public sealed class LeverView : MonoBehaviour
    {
        [SerializeField] private Image leverImage;
        [SerializeField] private Sprite upSprite;
        [SerializeField] private Sprite downSprite;
        [SerializeField] private Button hitArea;
        [SerializeField] private float holdDownSeconds = 0.25f;

        private Coroutine _pullRoutine;

        public event Action Clicked;

        public void Construct(Image image, Sprite up, Sprite down, Button clickArea)
        {
            leverImage = image;
            upSprite = up;
            downSprite = down;
            hitArea = clickArea;
            leverImage.sprite = upSprite;
        }

        private void Start()
        {
            hitArea.onClick.AddListener(() => Clicked?.Invoke());
        }

        public void SetInteractable(bool interactable)
        {
            hitArea.interactable = interactable;
        }

        public void PlayPull()
        {
            if (_pullRoutine != null)
            {
                StopCoroutine(_pullRoutine);
            }

            _pullRoutine = StartCoroutine(PullRoutine());
        }

        private IEnumerator PullRoutine()
        {
            leverImage.sprite = downSprite != null ? downSprite : upSprite;
            yield return new WaitForSeconds(holdDownSeconds);
            leverImage.sprite = upSprite;
            _pullRoutine = null;
        }
    }
}
