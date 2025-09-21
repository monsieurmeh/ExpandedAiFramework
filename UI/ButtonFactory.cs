using System;
using UnityEngine;
using UnityEngine.UI;

namespace ExpandedAiFramework.UI
{
    [System.Serializable]
    public struct ButtonOptions
    {
        public TextOptions? textOptions;
        public ImageOptions? iconOptions;
        public ImageOptions backgroundOptions;
        public LayoutElementOptions layoutElement;
        public ColorBlock? colorBlock;

        public static ButtonOptions TextButton(string text, float width = 100f, float height = 30f)
        {
            return new ButtonOptions
            {
                textOptions = TextOptions.Default(text, 12),
                iconOptions = null,
                backgroundOptions = new ImageOptions { Color = new Color(0.3f, 0.3f, 0.3f, 1f) },
                layoutElement = LayoutElementOptions.Fixed(width, height),
                colorBlock = CreateDefaultColorBlock()
            };
        }

        public static ButtonOptions IconButton(string iconResourcePath, float size = 30f)
        {
            return new ButtonOptions
            {
                textOptions = null,
                iconOptions = new ImageOptions { ResourcePath = iconResourcePath, Color = Color.white },
                backgroundOptions = new ImageOptions { Color = new Color(0.3f, 0.3f, 0.3f, 1f) },
                layoutElement = LayoutElementOptions.Fixed(size, size),
                colorBlock = CreateDefaultColorBlock()
            };
        }

        public static ButtonOptions TextWithIcon(string text, string iconResourcePath, float width = 120f, float height = 30f)
        {
            return new ButtonOptions
            {
                textOptions = TextOptions.Default(text, 12),
                iconOptions = new ImageOptions { ResourcePath = iconResourcePath, Color = Color.white },
                backgroundOptions = new ImageOptions { Color = new Color(0.3f, 0.3f, 0.3f, 1f) },
                layoutElement = LayoutElementOptions.Fixed(width, height),
                colorBlock = CreateDefaultColorBlock()
            };
        }

        public static ButtonOptions FlexibleTextButton(string text, float minWidth = 80f, float height = 30f)
        {
            return new ButtonOptions
            {
                textOptions = TextOptions.Default(text, 12),
                iconOptions = null,
                backgroundOptions = new ImageOptions { Color = new Color(0.3f, 0.3f, 0.3f, 1f) },
                layoutElement = LayoutElementOptions.MinSize(minWidth, height, 1, 0),
                colorBlock = CreateDefaultColorBlock()
            };
        }

        private static ColorBlock CreateDefaultColorBlock()
        {
            return new ColorBlock
            {
                normalColor = new Color(0.3f, 0.3f, 0.3f, 1f),
                highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f),
                pressedColor = new Color(0.2f, 0.2f, 0.2f, 1f),
                disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };
        }
    }

    public static class ButtonFactory
    {            
        public static Button CreateButton(Transform parent, ButtonOptions options, Action onClick = null)
        {
            var buttonObj = new GameObject("Button");
            buttonObj.transform.SetParent(parent, false);
            
            // Add layout element for sizing
            LayoutFactory.CreateLayoutElement(buttonObj.transform, options.layoutElement);
            
            // Add background image
            Image backgroundImage = ImageFactory.CreateImage(buttonObj.transform, options.backgroundOptions);
            backgroundImage.type = Image.Type.Sliced;
            
            // Add button component
            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = backgroundImage;
            
            // Set button colors
            if (options.colorBlock.HasValue)
            {
                button.colors = options.colorBlock.Value;
            }
            
            // Create content based on what's provided
            bool hasText = options.textOptions.HasValue && !string.IsNullOrEmpty(options.textOptions.Value.text);
            bool hasIcon = options.iconOptions.HasValue;
            
            if (hasText && hasIcon)
            {
                CreateTextWithIconContent(buttonObj.transform, options.textOptions.Value, options.iconOptions.Value);
            }
            else if (hasText)
            {
                CreateTextContent(buttonObj.transform, options.textOptions.Value);
            }
            else if (hasIcon)
            {
                CreateIconContent(buttonObj.transform, options.iconOptions.Value);
            }
            
            // Add click listener
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }
            
            return button;
        }

        private static void CreateTextContent(Transform parent, TextOptions textOptions)
        {
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(parent, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 2);
            textRect.offsetMax = new Vector2(-5, -2);
            
            var textOptionsWithAlignment = textOptions;
            textOptionsWithAlignment.alignment = TextAnchor.MiddleCenter;
            TextFactory.CreateText(textObj.transform, textOptionsWithAlignment);
        }

        private static void CreateIconContent(Transform parent, ImageOptions iconOptions)
        {
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(parent, false);
            
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(5, 5);
            iconRect.offsetMax = new Vector2(-5, -5);
            
            ImageFactory.CreateImage(iconObj.transform, iconOptions);
        }

        private static void CreateTextWithIconContent(Transform parent, TextOptions textOptions, ImageOptions iconOptions)
        {
            // Create icon on the left
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(parent, false);
            
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0);
            iconRect.anchorMax = new Vector2(0, 1);
            iconRect.offsetMin = new Vector2(5, 5);
            iconRect.offsetMax = new Vector2(25, -5); // 20px wide icon + 5px padding
            
            ImageFactory.CreateImage(iconObj.transform, iconOptions);
            
            // Create text on the right
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(parent, false);
            
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(30, 2); // Start after icon + spacing
            textRect.offsetMax = new Vector2(-5, -2);
            
            var textOptionsWithAlignment = textOptions;
            textOptionsWithAlignment.alignment = TextAnchor.MiddleLeft;
            TextFactory.CreateText(textObj.transform, textOptionsWithAlignment);
        }

        // Convenience methods
        public static Button CreateTextButton(Transform parent, string text, Action onClick = null)
        {
            return CreateButton(parent, ButtonOptions.TextButton(text), onClick);
        }

        public static Button CreateIconButton(Transform parent, string iconResourcePath, Action onClick = null)
        {
            return CreateButton(parent, ButtonOptions.IconButton(iconResourcePath), onClick);
        }

        public static Button CreateTextWithIconButton(Transform parent, string text, string iconResourcePath, Action onClick = null)
        {
            return CreateButton(parent, ButtonOptions.TextWithIcon(text, iconResourcePath), onClick);
        }

        public static Button CreateFlexibleTextButton(Transform parent, string text, Action onClick = null)
        {
            return CreateButton(parent, ButtonOptions.FlexibleTextButton(text), onClick);
        }
    }
}