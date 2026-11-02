using SlotGame.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.View
{
    /// <summary>
    /// Small helpers for creating UI objects from code with sensible defaults.
    /// Used by SlotMachineViewFactory so the building code stays readable.
    /// </summary>
    public static class UiBuilder
    {
        private const string OutlineMaterialPath = "Fonts & Materials/LiberationSans SDF - Outline";

        private static Material _outlineMaterial;

        public static RectTransform CreateRect(string name, Transform parent, Vector2 size, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            return rect;
        }

        /// <summary>A rect that fills its parent completely.</summary>
        public static RectTransform CreateStretched(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            Stretch(rect);
            return rect;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static Image CreateImage(string name, Transform parent, Sprite sprite, Vector2 size, Vector2 position,
            bool raycastTarget = false, bool preserveAspect = false)
        {
            RectTransform rect = CreateRect(name, parent, size, position);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = raycastTarget;
            image.preserveAspect = preserveAspect;
            return image;
        }

        public static TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize,
            Color color, Vector2 size, Vector2 position, TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            bool outline = true)
        {
            RectTransform rect = CreateRect(name, parent, size, position);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();

            // Assign the default font explicitly so the text renders even before TMP's own Awake runs.
            if (TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }

            if (outline)
            {
                if (_outlineMaterial == null)
                {
                    _outlineMaterial = Resources.Load<Material>(OutlineMaterialPath);
                }

                if (_outlineMaterial != null)
                {
                    label.fontSharedMaterial = _outlineMaterial;
                }
            }

            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = color;
            label.alignment = alignment;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>
        /// Creates a button that swaps sprites for its states, using the art's
        /// normal / highlighted / pressed / disabled images.
        /// </summary>
        public static Button CreateButton(string name, Transform parent, ButtonSprites sprites, Vector2 size,
            Vector2 position)
        {
            Image image = CreateImage(name, parent, sprites != null ? sprites.normal : null, size, position, true, true);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            if (sprites != null && sprites.normal != null)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState
                {
                    highlightedSprite = sprites.highlighted,
                    pressedSprite = sprites.pressed,
                    selectedSprite = sprites.normal,
                    disabledSprite = sprites.disabled != null ? sprites.disabled : sprites.pressed
                };
            }

            // No keyboard/gamepad navigation between buttons; Space is handled by the controller.
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        /// <summary>Button with a text label on top.</summary>
        public static Button CreateLabelButton(string name, Transform parent, ButtonSprites sprites, Vector2 size,
            Vector2 position, string labelText, float fontSize, out TextMeshProUGUI label)
        {
            Button button = CreateButton(name, parent, sprites, size, position);
            label = CreateText("Label", button.transform, labelText, fontSize, Color.white, size, Vector2.zero);
            return button;
        }

        /// <summary>Full screen semi-transparent layer that also blocks clicks to the game behind a popup.</summary>
        public static Image CreateDimmer(Transform parent, float alpha)
        {
            RectTransform rect = CreateStretched("Dimmer", parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, alpha);
            image.raycastTarget = true;
            return image;
        }
    }
}
