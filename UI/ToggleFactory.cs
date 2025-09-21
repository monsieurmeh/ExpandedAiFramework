using System;
using UnityEngine;
using UnityEngine.UI;

namespace ExpandedAiFramework.UI
{
    [System.Serializable]
    public struct ToggleOptions
    {
        public bool isOn;
        public TextOptions? textOptions;
        public ImageOptions backgroundOptions;
        public ImageOptions checkmarkOptions;
        public LayoutElementOptions layoutElement;
        public ColorBlock? colorBlock;
        public float checkmarkPadding;

        public static ToggleOptions Default(string label, bool isOn = false, float width = 150f, float height = 25f)
        {
            return new ToggleOptions
            {
                isOn = isOn,
                textOptions = TextOptions.Default(label, 12),
                backgroundOptions = new ImageOptions { Color = new Color(0.2f, 0.2f, 0.2f, 1f) },
                checkmarkOptions = new ImageOptions { Color = Color.white },
                layoutElement = LayoutElementOptions.Fixed(width, height),
                colorBlock = CreateDefaultColorBlock(),
                checkmarkPadding = 2f
            };
        }

        public static ToggleOptions CheckboxOnly(bool isOn = false, float size = 20f)
        {
            return new ToggleOptions
            {
                isOn = isOn,
                textOptions = null,
                backgroundOptions = new ImageOptions { Color = new Color(0.2f, 0.2f, 0.2f, 1f) },
                checkmarkOptions = new ImageOptions { Color = Color.white },
                layoutElement = LayoutElementOptions.Fixed(size, size),
                colorBlock = CreateDefaultColorBlock(),
                checkmarkPadding = 2f
            };
        }

        public static ToggleOptions WithLabel(string label, bool isOn = false, float width = 150f, float height = 25f)
        {
            return new ToggleOptions
            {
                isOn = isOn,
                textOptions = TextOptions.Default(label, 12),
                backgroundOptions = new ImageOptions { Color = new Color(0.2f, 0.2f, 0.2f, 1f) },
                checkmarkOptions = new ImageOptions { Color = Color.white },
                layoutElement = LayoutElementOptions.Fixed(width, height),
                colorBlock = CreateDefaultColorBlock(),
                checkmarkPadding = 2f
            };
        }

        private static ColorBlock CreateDefaultColorBlock()
        {
            return new ColorBlock
            {
                normalColor = new Color(0.2f, 0.2f, 0.2f, 1f),
                highlightedColor = new Color(0.3f, 0.3f, 0.3f, 1f),
                pressedColor = new Color(0.15f, 0.15f, 0.15f, 1f),
                disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };
        }
    }

    public static class ToggleFactory
    {            
        public static Toggle CreateToggle(Transform parent, ToggleOptions options, Action<bool> onValueChanged = null)
        {
            var toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(parent, false);
            
            // Add layout element for sizing
            LayoutFactory.CreateLayoutElement(toggleObj.transform, options.layoutElement);
            
            // Create content based on whether we have text
            bool hasText = options.textOptions.HasValue && !string.IsNullOrEmpty(options.textOptions.Value.text);
            
            if (hasText)
            {
                return CreateToggleWithLabel(toggleObj.transform, options, onValueChanged);
            }
            else
            {
                return CreateCheckboxOnly(toggleObj.transform, options, onValueChanged);
            }
        }

        private static Toggle CreateToggleWithLabel(Transform parent, ToggleOptions options, Action<bool> onValueChanged)
        {
            // Create horizontal layout for checkbox + label
            var containerObj = PanelFactory.CreateHorizontalPanel(parent, "ToggleContainer");
            var containerLayout = containerObj.GetComponent<HorizontalLayoutGroup>();
            containerLayout.spacing = 5f;
            containerLayout.childControlWidth = false;
            containerLayout.childControlHeight = true;
            containerLayout.childForceExpandWidth = false;
            containerLayout.childForceExpandHeight = false;

            // Create checkbox part
            var checkboxObj = new GameObject("Checkbox");
            checkboxObj.transform.SetParent(containerObj.transform, false);
            
            // Fixed size for checkbox
            var checkboxSize = Mathf.Min(options.layoutElement.preferredHeight, 20f);
            LayoutFactory.CreateLayoutElement(checkboxObj.transform, LayoutElementOptions.Fixed(checkboxSize, checkboxSize));
            
            var toggle = CreateCheckboxToggle(checkboxObj.transform, options, onValueChanged);
            
            // Create label
            if (options.textOptions.HasValue)
            {
                var textOptions = options.textOptions.Value;
                textOptions.alignment = TextAnchor.MiddleLeft;
                TextFactory.CreateTextField(containerObj.transform, TextFieldOptions.Default(textOptions));
            }
            
            return toggle;
        }

        private static Toggle CreateCheckboxOnly(Transform parent, ToggleOptions options, Action<bool> onValueChanged)
        {
            return CreateCheckboxToggle(parent, options, onValueChanged);
        }

        private static Toggle CreateCheckboxToggle(Transform parent, ToggleOptions options, Action<bool> onValueChanged)
        {
            // Add background image
            Image backgroundImage = ImageFactory.CreateImage(parent, options.backgroundOptions);
            backgroundImage.type = Image.Type.Sliced;
            
            // Add toggle component
            var toggle = parent.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = backgroundImage;
            toggle.isOn = options.isOn;
            
            // Set toggle colors
            if (options.colorBlock.HasValue)
            {
                toggle.colors = options.colorBlock.Value;
            }
            
            // Create checkmark as child
            var checkmarkObj = new GameObject("Checkmark");
            checkmarkObj.transform.SetParent(parent, false);
            
            var checkmarkRect = checkmarkObj.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = Vector2.zero;
            checkmarkRect.anchorMax = Vector2.one;
            checkmarkRect.offsetMin = new Vector2(options.checkmarkPadding, options.checkmarkPadding);
            checkmarkRect.offsetMax = new Vector2(-options.checkmarkPadding, -options.checkmarkPadding);
            
            // Create checkmark image
            var checkmarkOptions = options.checkmarkOptions;
            var checkmarkImage = ImageFactory.CreateImage(checkmarkObj.transform, checkmarkOptions);
            checkmarkImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Checkmark.psd");
            
            toggle.graphic = checkmarkImage;
            
            // Add click listener
            if (onValueChanged != null)
            {
                toggle.onValueChanged.AddListener(onValueChanged);
            }
            
            return toggle;
        }

        // Convenience methods
        public static Toggle CreateCheckbox(Transform parent, bool isOn = false, Action<bool> onValueChanged = null)
        {
            return CreateToggle(parent, ToggleOptions.CheckboxOnly(isOn), onValueChanged);
        }

        public static Toggle CreateLabeledToggle(Transform parent, string label, bool isOn = false, Action<bool> onValueChanged = null)
        {
            return CreateToggle(parent, ToggleOptions.WithLabel(label, isOn), onValueChanged);
        }
    }

}