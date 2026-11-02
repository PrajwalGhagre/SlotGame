using System.Collections.Generic;
using SlotGame.Reels;
using SlotGame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.View
{
    /// <summary>
    /// Holds references to every visual part of the machine. It is the root of
    /// the SlotMachine prefab (or of the hierarchy built at runtime) and is what
    /// the controller talks to.
    /// </summary>
    public sealed class SlotMachineView : MonoBehaviour
    {
        [SerializeField] private List<ReelView> reels = new List<ReelView>();
        [SerializeField] private HudView hud;
        [SerializeField] private LeverView lever;
        [SerializeField] private WinPopupView winPopup;
        [SerializeField] private PaytablePopupView paytablePopup;
        [SerializeField] private ConfirmDialogView confirmDialog;
        [SerializeField] private Image paylineMarker;

        public IReadOnlyList<ReelView> Reels => reels;
        public HudView Hud => hud;
        public LeverView Lever => lever;
        public WinPopupView WinPopup => winPopup;
        public PaytablePopupView PaytablePopup => paytablePopup;
        public ConfirmDialogView ConfirmDialog => confirmDialog;
        public Image PaylineMarker => paylineMarker;

        public bool AnyPopupOpen =>
            (winPopup != null && winPopup.IsOpen) ||
            (paytablePopup != null && paytablePopup.IsOpen) ||
            (confirmDialog != null && confirmDialog.IsOpen);

        public void Construct(List<ReelView> reelViews, HudView hudView, LeverView leverView, WinPopupView popup,
            PaytablePopupView paytable, ConfirmDialogView dialog, Image payline)
        {
            reels = reelViews;
            hud = hudView;
            lever = leverView;
            winPopup = popup;
            paytablePopup = paytable;
            confirmDialog = dialog;
            paylineMarker = payline;
        }

        /// <summary>Makes the payline marker brighter while a win is shown.</summary>
        public void SetPaylineHighlighted(bool highlighted)
        {
            if (paylineMarker != null)
            {
                paylineMarker.color = highlighted ? new Color(1f, 0.85f, 0.2f, 0.95f) : new Color(1f, 0.2f, 0.2f, 0.45f);
            }
        }
    }
}
