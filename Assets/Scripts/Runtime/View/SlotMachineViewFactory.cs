using System.Collections.Generic;
using System.Linq;
using SlotGame.Data;
using SlotGame.Reels;
using SlotGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.View
{
    /// <summary>
    /// Builds the whole machine and UI from the config asset.
    ///
    /// Why build from code? The project opens and plays with zero manual wiring in
    /// the Inspector, and every position comes from the art in one place. The same
    /// factory is used by the editor menu (Tools > Slot Game > Generate Machine Prefab)
    /// to save the result as Assets/Prefabs/SlotMachine.prefab, so designers can also
    /// tweak it visually like any normal prefab.
    ///
    /// All machine coordinates below are in the pixel space of the 816 x 624
    /// machine sprites (origin at the centre), measured from the provided art.
    /// </summary>
    public static class SlotMachineViewFactory
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        // Machine art measurements (see class summary).
        private static readonly Vector2 MachineSize = new Vector2(816f, 624f);
        private const float MachineScale = 1.3f;
        private static readonly Vector2 MachinePosition = new Vector2(-250f, -20f);
        private static readonly float[] ArtReelCentersX = { -125f, 5f, 135f };
        private const float ReelWindowWidth = 108f;
        private const float ReelWindowHeight = 210f;
        private const float ReelWindowCenterY = -39f;
        private const float PartialRowFraction = 0.8f;
        private static readonly Vector2 LeverHitSize = new Vector2(92f, 330f);
        private static readonly Vector2 LeverHitPosition = new Vector2(310f, -146f);
        private static readonly Vector2 MachineDisplayPosition = new Vector2(8f, -288f);

        // Side panel.
        private const float PanelColumnX = 560f;

        private static readonly Color Gold = new Color(1f, 0.84f, 0.25f, 1f);
        private static readonly Color SoftWhite = new Color(1f, 1f, 1f, 0.8f);

        /// <summary>
        /// Builds the machine. The returned root is INACTIVE so the caller can
        /// initialise everything before any Awake/Start runs; activate it afterwards.
        /// </summary>
        public static SlotMachineView Build(SlotMachineConfig config)
        {
            MachineArt art = config.Art;

            var root = new GameObject("SlotMachine", typeof(RectTransform));
            root.SetActive(false);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();
            var view = root.AddComponent<SlotMachineView>();

            // Background fills the screen.
            RectTransform background = UiBuilder.CreateStretched("Background", root.transform);
            var backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.sprite = art.background;
            backgroundImage.raycastTarget = false;

            TextMeshProUGUI title = UiBuilder.CreateText("Title", root.transform, "LUCKY SLOTS", 78f, Gold,
                new Vector2(900f, 110f), new Vector2(MachinePosition.x, 470f));
            AddLoopingAnimation(title.gameObject, art.titleIdleAnimation);

            // Machine, drawn back to front: body, reel backgrounds, reels, payline, frame, lever.
            RectTransform machine = UiBuilder.CreateRect("Machine", root.transform, MachineSize, MachinePosition);
            machine.localScale = Vector3.one * MachineScale;

            UiBuilder.CreateImage("Body", machine, art.machineBody, MachineSize, Vector2.zero);
            UiBuilder.CreateImage("ReelBackground", machine, art.reelBackground, MachineSize, Vector2.zero);

            List<ReelView> reels = BuildReels(config, machine);

            Image payline = UiBuilder.CreateImage("PaylineMarker", machine, null,
                new Vector2(ReelWindowWidth * config.ReelCount + 40f, 4f), new Vector2(5f, ReelWindowCenterY));

            UiBuilder.CreateImage("Frame", machine, art.machineFrame, MachineSize, Vector2.zero);
            LeverView lever = BuildLever(art, machine);

            TextMeshProUGUI machineMessage = UiBuilder.CreateText("MachineDisplay", machine, string.Empty, 26f, Gold,
                new Vector2(400f, 44f), MachineDisplayPosition, TextAlignmentOptions.Center, false);
            machineMessage.enableAutoSizing = true;
            machineMessage.fontSizeMin = 14f;
            machineMessage.fontSizeMax = 26f;

            HudView hud = BuildHud(art, root.transform, machineMessage);

            UiBuilder.CreateText("Hint", root.transform, "SPACE = spin / stop     M = mute", 26f, SoftWhite,
                new Vector2(900f, 50f), new Vector2(MachinePosition.x, -505f), TextAlignmentOptions.Center, false);

            RectTransform popups = UiBuilder.CreateStretched("Popups", root.transform);
            PaytablePopupView paytable = BuildPaytable(config, popups);
            ConfirmDialogView confirmDialog = BuildConfirmDialog(art, popups);
            WinPopupView winPopup = BuildWinPopup(art, popups);

            view.Construct(reels, hud, lever, winPopup, paytable, confirmDialog, payline);
            view.SetPaylineHighlighted(false);
            return view;
        }

        private static List<ReelView> BuildReels(SlotMachineConfig config, RectTransform machine)
        {
            var reels = new List<ReelView>();
            int rows = config.VisibleRows;

            // Like the reference design, the payline symbol is fully visible and the rows
            // above and below are cut by the window edge. For 3 rows that gives ~95 px cells
            // and ~92 px symbols, which fills the 108 px wide window nicely.
            float cellHeight = ReelWindowHeight / (rows - PartialRowFraction);
            float symbolSize = Mathf.Min(cellHeight, ReelWindowWidth - 8f) - 3f;

            for (int reelIndex = 0; reelIndex < config.ReelCount; reelIndex++)
            {
                RectTransform window = UiBuilder.CreateRect($"Reel_{reelIndex + 1}", machine,
                    new Vector2(ReelWindowWidth, ReelWindowHeight),
                    new Vector2(GetReelCenterX(reelIndex, config.ReelCount), ReelWindowCenterY));

                // RectMask2D clips the cells to the reel window without needing a stencil.
                window.gameObject.AddComponent<RectMask2D>();

                Image anticipation = UiBuilder.CreateImage("AnticipationGlow", window, null,
                    new Vector2(ReelWindowWidth, ReelWindowHeight), Vector2.zero);
                anticipation.color = new Color(1f, 0.85f, 0.2f, 0.3f);
                anticipation.enabled = false;

                // Visible rows plus one hidden cell above and one below (see ReelView).
                var cells = new List<SymbolView>();
                for (int cellIndex = 0; cellIndex < rows + 2; cellIndex++)
                {
                    RectTransform cellRect = UiBuilder.CreateRect($"Cell_{cellIndex}", window,
                        new Vector2(ReelWindowWidth, cellHeight), Vector2.zero);

                    Image glow = UiBuilder.CreateImage("Glow", cellRect, config.Art.glow,
                        new Vector2(symbolSize * 1.3f, symbolSize * 1.3f), Vector2.zero);
                    // Placeholder sprite so the generated prefab looks right in the editor;
                    // the reel replaces it with the real strip symbol on Initialize.
                    Sprite previewSprite = PreviewSprite(config, reelIndex + cellIndex);
                    Image symbol = UiBuilder.CreateImage("Symbol", cellRect, previewSprite,
                        new Vector2(symbolSize, symbolSize), Vector2.zero, false, true);

                    var cell = cellRect.gameObject.AddComponent<SymbolView>();
                    cell.Construct(symbol, glow);
                    cells.Add(cell);
                }

                var reel = window.gameObject.AddComponent<ReelView>();
                reel.Construct(reelIndex, cellHeight, rows, cells, anticipation);
                reels.Add(reel);
            }

            return reels;
        }

        /// <summary>Plays a legacy clip on loop (simple idle animations that need no Animator Controller).</summary>
        private static void AddLoopingAnimation(GameObject target, AnimationClip clip)
        {
            if (clip == null) return;

            var animation = target.AddComponent<Animation>();
            animation.AddClip(clip, clip.name);
            animation.clip = clip;
            animation.wrapMode = WrapMode.Loop;
            animation.playAutomatically = true;
        }

        private static Sprite PreviewSprite(SlotMachineConfig config, int index)
        {
            int count = config.Symbols.Count;
            if (count == 0) return null;

            SymbolDefinition definition = config.Symbols[index % count];
            return definition != null ? definition.Sprite : null;
        }

        /// <summary>Uses the real window positions from the art for 3 reels, spreads evenly otherwise.</summary>
        private static float GetReelCenterX(int reelIndex, int reelCount)
        {
            if (reelCount == ArtReelCentersX.Length)
            {
                return ArtReelCentersX[reelIndex];
            }

            float left = ArtReelCentersX[0];
            float right = ArtReelCentersX[ArtReelCentersX.Length - 1];
            return reelCount == 1 ? (left + right) * 0.5f : Mathf.Lerp(left, right, reelIndex / (float)(reelCount - 1));
        }

        private static LeverView BuildLever(MachineArt art, RectTransform machine)
        {
            // The lever sprites are full machine-sized layers, so the image covers the machine
            // and a separate invisible hit area makes only the lever itself clickable.
            Image leverImage = UiBuilder.CreateImage("Lever", machine, art.leverUp, MachineSize, Vector2.zero);

            Image hitImage = UiBuilder.CreateImage("LeverHitArea", leverImage.transform, null, LeverHitSize,
                LeverHitPosition, true);
            hitImage.color = new Color(1f, 1f, 1f, 0f);
            var hitButton = hitImage.gameObject.AddComponent<Button>();
            hitButton.transition = Selectable.Transition.None;
            hitButton.navigation = new Navigation { mode = Navigation.Mode.None };

            var lever = leverImage.gameObject.AddComponent<LeverView>();
            lever.Construct(leverImage, art.leverUp, art.leverDown, hitButton);
            return lever;
        }

        private static HudView BuildHud(MachineArt art, Transform parent, TMP_Text machineMessage)
        {
            RectTransform hudRoot = UiBuilder.CreateStretched("Hud", parent);
            Vector2 panelSize = new Vector2(400f, 148f);

            TextMeshProUGUI balance = BuildValuePanel(art, hudRoot, "BalancePanel", "BALANCE", new Vector2(PanelColumnX, 300f), panelSize);
            TextMeshProUGUI bet = BuildValuePanel(art, hudRoot, "BetPanel", "BET", new Vector2(PanelColumnX, 130f), panelSize);
            TextMeshProUGUI lastWin = BuildValuePanel(art, hudRoot, "WinPanel", "LAST WIN", new Vector2(PanelColumnX, -40f), panelSize);

            Button betDown = UiBuilder.CreateButton("BetDownButton", hudRoot, art.arrowLeftButton, new Vector2(90f, 90f),
                new Vector2(PanelColumnX - 250f, 130f));
            Button betUp = UiBuilder.CreateButton("BetUpButton", hudRoot, art.arrowRightButton, new Vector2(90f, 90f),
                new Vector2(PanelColumnX + 250f, 130f));

            Button spin = UiBuilder.CreateLabelButton("SpinButton", hudRoot, art.blankButton, new Vector2(377f, 148f),
                new Vector2(PanelColumnX, -220f), "SPIN", 64f, out TextMeshProUGUI spinLabel);

            Button paytable = UiBuilder.CreateLabelButton("PaytableButton", hudRoot, art.blankButton,
                new Vector2(230f, 90f), new Vector2(PanelColumnX - 122f, -370f), "PAYTABLE", 28f, out _);
            Button autoSpin = UiBuilder.CreateLabelButton("AutoSpinButton", hudRoot, art.blankButton,
                new Vector2(230f, 90f), new Vector2(PanelColumnX + 122f, -370f), "AUTO: OFF", 28f,
                out TextMeshProUGUI autoLabel);

            var hud = hudRoot.gameObject.AddComponent<HudView>();
            hud.Construct(balance, bet, lastWin, machineMessage, spin, spinLabel, betDown, betUp, paytable, autoSpin,
                autoLabel);
            return hud;
        }

        /// <summary>Panel sprite with a small caption on top and a big number in the middle.</summary>
        private static TextMeshProUGUI BuildValuePanel(MachineArt art, Transform parent, string name, string caption,
            Vector2 position, Vector2 size)
        {
            Image panel = UiBuilder.CreateImage(name, parent, art.panel, size, position);
            UiBuilder.CreateText("Caption", panel.transform, caption, 28f, Gold, new Vector2(size.x, 36f),
                new Vector2(0f, size.y * 0.5f - 34f));
            return UiBuilder.CreateText("Value", panel.transform, "0", 54f, Color.white, new Vector2(size.x - 40f, 70f),
                new Vector2(0f, -14f));
        }

        private static WinPopupView BuildWinPopup(MachineArt art, Transform parent)
        {
            RectTransform root = UiBuilder.CreateStretched("WinPopup", parent);
            var group = root.gameObject.AddComponent<CanvasGroup>();

            Image dimmer = UiBuilder.CreateDimmer(root, 0.65f);
            var clickToClose = dimmer.gameObject.AddComponent<Button>();
            clickToClose.transition = Selectable.Transition.None;
            clickToClose.navigation = new Navigation { mode = Navigation.Mode.None };

            Image panel = UiBuilder.CreateImage("Panel", root, art.popup, new Vector2(900f, 426f), Vector2.zero);
            TextMeshProUGUI title = UiBuilder.CreateText("Title", panel.transform, "BIG WIN!", 84f, Gold,
                new Vector2(820f, 110f), new Vector2(0f, 105f));
            TextMeshProUGUI amount = UiBuilder.CreateText("Amount", panel.transform, "0", 118f, Color.white,
                new Vector2(820f, 140f), new Vector2(0f, -10f));
            TextMeshProUGUI subtitle = UiBuilder.CreateText("Subtitle", panel.transform, string.Empty, 36f, SoftWhite,
                new Vector2(820f, 60f), new Vector2(0f, -125f));

            var popup = root.gameObject.AddComponent<WinPopupView>();
            popup.Construct(group, panel.rectTransform, title, amount, subtitle, clickToClose);
            return popup;
        }

        private static PaytablePopupView BuildPaytable(SlotMachineConfig config, Transform parent)
        {
            MachineArt art = config.Art;
            RectTransform root = UiBuilder.CreateStretched("PaytablePopup", parent);
            UiBuilder.CreateDimmer(root, 0.7f);

            Image panel = UiBuilder.CreateImage("Panel", root, art.popup, new Vector2(1180f, 560f), Vector2.zero, true);
            UiBuilder.CreateText("Title", panel.transform, "PAYTABLE", 60f, Gold, new Vector2(800f, 80f),
                new Vector2(0f, 205f));

            // One row per symbol, best paying first. Built from the same data the maths uses.
            List<SymbolDefinition> ordered = config.Symbols.Where(s => s != null)
                .OrderByDescending(s => s.PayoutMultiplier).ToList();
            float rowY = 125f;
            const float rowStep = 66f;
            foreach (SymbolDefinition symbol in ordered)
            {
                for (int i = 0; i < config.ReelCount; i++)
                {
                    UiBuilder.CreateImage($"{symbol.DisplayName}_{i}", panel.transform, symbol.Sprite,
                        new Vector2(54f, 54f), new Vector2(-470f + i * 60f, rowY), false, true);
                }

                UiBuilder.CreateText($"{symbol.DisplayName}_Pays", panel.transform, $"x{symbol.PayoutMultiplier}",
                    40f, Color.white, new Vector2(160f, 54f), new Vector2(-230f, rowY), TextAlignmentOptions.Left);
                rowY -= rowStep;
            }

            string rules =
                "- Win when all reels show the same symbol on the middle line.\n" +
                $"- Payout = bet x multiplier.\n" +
                $"- WILD stands in for any symbol. Wins that use a WILD pay x{config.WildAssistMultiplier}.\n" +
                $"- {config.BuildBonusRules().ScattersNeededForFreeSpins} or more WILDs anywhere on screen give " +
                $"{config.BuildBonusRules().FreeSpinsAwarded} FREE SPINS with all wins x{config.BuildBonusRules().FreeSpinWinMultiplier}.";
            UiBuilder.CreateText("Rules", panel.transform, rules, 28f, Color.white, new Vector2(500f, 330f),
                new Vector2(250f, -10f), TextAlignmentOptions.TopLeft);

            Button close = UiBuilder.CreateButton("CloseButton", panel.transform, art.closeButton, new Vector2(90f, 90f),
                new Vector2(540f, 230f));

            var popup = root.gameObject.AddComponent<PaytablePopupView>();
            popup.Construct(close);
            return popup;
        }

        private static ConfirmDialogView BuildConfirmDialog(MachineArt art, Transform parent)
        {
            RectTransform root = UiBuilder.CreateStretched("ConfirmDialog", parent);
            UiBuilder.CreateDimmer(root, 0.7f);

            Image panel = UiBuilder.CreateImage("Panel", root, art.popup, new Vector2(780f, 380f), Vector2.zero, true);
            TextMeshProUGUI message = UiBuilder.CreateText("Message", panel.transform, string.Empty, 42f, Color.white,
                new Vector2(660f, 160f), new Vector2(0f, 60f));

            Button yes = UiBuilder.CreateButton("YesButton", panel.transform, art.yesButton, new Vector2(230f, 90f),
                new Vector2(-140f, -100f));
            Button no = UiBuilder.CreateButton("NoButton", panel.transform, art.noButton, new Vector2(230f, 90f),
                new Vector2(140f, -100f));

            var dialog = root.gameObject.AddComponent<ConfirmDialogView>();
            dialog.Construct(message, yes, no);
            return dialog;
        }
    }
}
