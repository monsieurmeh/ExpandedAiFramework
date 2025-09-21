using UnityEngine;
using UnityEngine.UI;
using System;
using static ExpandedAiFramework.DebugMenu.Extensions;
using ExpandedAiFramework.UI;

namespace ExpandedAiFramework.DebugMenu
{
    /// <summary>
    /// Efficient list view item for high-frequency rendering
    /// Uses anchors for performance instead of layout groups
    /// </summary>
    public static class DebugMenuListViewItem
    {
        /// <summary>
        /// Creates an efficient list item with manual anchor positioning
        /// </summary>
        public static GameObject CreateListItem<T>(T item, int index, Transform parent, float itemHeight, 
            Action<GameObject, T, int> populateCallback, Action<T> onItemClicked, Action<GameObject, T, int> createButtonsCallback) 
            where T : ISerializedData
        {
            var itemObj = new GameObject($"ListItem_{index}");
            itemObj.transform.SetParent(parent, false);
            
            var itemRect = itemObj.DefinitelyGetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, itemHeight);
            
            // Main content area (clickable)
            var contentArea = new GameObject("ContentArea");
            contentArea.transform.SetParent(itemObj.transform, false);
            
            var contentRect = contentArea.DefinitelyGetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            // Use full width if no buttons, otherwise leave space for buttons
            contentRect.anchorMax = createButtonsCallback == null ? new Vector2(1, 1) : new Vector2(0.75f, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            
            var contentButton = contentArea.AddComponent<Button>();
            var contentImage = contentArea.AddComponent<Image>();
            contentImage.color = index % 2 == 0 ? 
                new Color(0.22f, 0.22f, 0.22f, 1f) : 
                new Color(0.18f, 0.18f, 0.18f, 1f);
            
            // Button hover colors
            var colors = contentButton.colors;
            colors.normalColor = contentImage.color;
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            contentButton.colors = colors;
            
            // Populate item content using callback
            populateCallback?.Invoke(contentArea, item, index);
            
            // Click handler for main content
            contentButton.onClick.AddListener(new Action(() => onItemClicked?.Invoke(item)));
            
            // Entity action buttons area (only create if callback provided)
            if (createButtonsCallback != null)
            {
                var buttonArea = new GameObject("ButtonArea");
                buttonArea.transform.SetParent(itemObj.transform, false);
                
                var buttonRect = buttonArea.DefinitelyGetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0.75f, 0);
                buttonRect.anchorMax = new Vector2(1, 1);
                buttonRect.offsetMin = Vector2.zero;
                buttonRect.offsetMax = Vector2.zero;
                
                var buttonBg = buttonArea.AddComponent<Image>();
                buttonBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
                
                var buttonLayout = buttonArea.AddComponent<HorizontalLayoutGroup>();
                buttonLayout.spacing = 2;
                buttonLayout.padding = new RectOffset(5, 5, 5, 5);
                buttonLayout.childControlWidth = true;
                buttonLayout.childControlHeight = false;
                buttonLayout.childForceExpandWidth = true;
                buttonLayout.childForceExpandHeight = false;
                
                // Create action buttons using callback
                createButtonsCallback.Invoke(buttonArea, item, index);
            }
            
            return itemObj;
        }
        
        /// <summary>
        /// Creates a simple text label for list items (efficient)
        /// </summary>
        public static Text CreateItemLabel(string text, Transform parent, Vector2 position, Vector2 size, 
            int fontSize = 11, Color? textColor = null, TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle fontStyle = FontStyle.Normal)
        {
            var labelObj = new GameObject($"Label_{text}");
            labelObj.transform.SetParent(parent, false);
            
            var labelRect = labelObj.DefinitelyGetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.zero;
            labelRect.anchoredPosition = position;
            labelRect.sizeDelta = size;
            
            var label = labelObj.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = fontSize;
            label.color = textColor ?? Color.white;
            label.alignment = alignment;
            label.fontStyle = fontStyle;
            
            return label;
        }
        
        /// <summary>
        /// Creates an efficient button for list items
        /// </summary>
        public static Button CreateItemButton(string text, Transform parent, Action onClick, 
            Color? backgroundColor = null, Color? textColor = null)
        {
            var buttonObj = new GameObject($"Button_{text}");
            buttonObj.transform.SetParent(parent, false);
            
            var button = buttonObj.AddComponent<Button>();
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = backgroundColor ?? new Color(0.3f, 0.3f, 0.3f, 1f); // ButtonNormal equivalent
            
            // Button text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            var textRect = textObj.DefinitelyGetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(2, 2);
            textRect.offsetMax = new Vector2(-2, -2);
            
            var buttonText = textObj.AddComponent<Text>();
            buttonText.text = text;
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.fontSize = 9;
            buttonText.color = textColor ?? Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.fontStyle = FontStyle.Bold;
            
            // Button hover colors
            var colors = button.colors;
            colors.normalColor = backgroundColor ?? new Color(0.3f, 0.3f, 0.3f, 1f); // ButtonNormal equivalent
            colors.highlightedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            colors.pressedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            button.colors = colors;
            
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }
            
            return button;
        }
        
        /// <summary>
        /// Updates selection state for a list item
        /// </summary>
        public static void SetItemSelected(GameObject listItem, bool isSelected, int originalIndex)
        {
            var contentArea = listItem.transform.Find("ContentArea");
            if (contentArea != null)
            {
                var image = contentArea.GetComponent<Image>();
                if (image != null)
                {
                    if (isSelected)
                    {
                        // Highlight selected item
                        image.color = new Color(0.4f, 0.6f, 0.8f, 1f);
                    }
                    else
                    {
                        // Normal alternating colors
                        image.color = originalIndex % 2 == 0 ? 
                            new Color(0.22f, 0.22f, 0.22f, 1f) : 
                            new Color(0.18f, 0.18f, 0.18f, 1f);
                    }
                }
            }
        }
    }
}
