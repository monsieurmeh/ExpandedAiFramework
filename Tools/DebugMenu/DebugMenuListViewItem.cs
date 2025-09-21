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
        /// Creates a simple horizontal list item: Icon + Display Name + Show/Hide toggle
        /// </summary>
        public static GameObject CreateListItem<T>(T item, int index, Transform parent, float itemHeight, 
            Action<GameObject, T, int> populateCallback, Action<T> onItemClicked, Action<GameObject, T, int> createButtonsCallback) 
            where T : ISerializedData
        {
            var itemObj = new GameObject($"ListItem_{index}");
            itemObj.transform.SetParent(parent, false);
            
            var itemRect = itemObj.DefinitelyGetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, itemHeight);
            
            // Background
            var backgroundImage = itemObj.AddComponent<Image>();
            backgroundImage.color = index % 2 == 0 ? 
                new Color(0.22f, 0.22f, 0.22f, 1f) : 
                new Color(0.18f, 0.18f, 0.18f, 1f);
            
            // Horizontal layout for the entire item
            var layout = itemObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.padding = new RectOffset(5, 5, 2, 2);
            layout.childControlWidth = true;  // Control child widths
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            
            // 1. Show/Hide toggle (tiny, first)
            CreateShowHideToggle(itemObj.transform, item, index);
            
            // 2. Icon (small square)
            CreateIcon(itemObj.transform, itemHeight);
            
            // 3. Name and Position (takes up most space)
            CreateDisplayName(itemObj.transform, GetItemDisplayText(item), onItemClicked != null ? () => onItemClicked(item) : null);
            
            return itemObj;
        }
        
        /// <summary>
        /// Gets the display text for an item (Name and Position if available)
        /// </summary>
        private static string GetItemDisplayText<T>(T item) where T : ISerializedData
        {
            var name = "";
            var position = "";
            
            // Try to get name from various properties
            var type = item.GetType();
            var nameProperty = type.GetProperty("Name");
            if (nameProperty != null)
            {
                name = nameProperty.GetValue(item)?.ToString() ?? "";
            }
            else
            {
                name = item.DisplayName; // Fallback to DisplayName
            }
            
            // Try to get position
            var positionProperty = type.GetProperty("Position");
            if (positionProperty != null && positionProperty.PropertyType == typeof(UnityEngine.Vector3))
            {
                var pos = (UnityEngine.Vector3)positionProperty.GetValue(item);
                position = $" ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})";
            }
            else
            {
                var currentPositionProperty = type.GetProperty("CurrentPosition");
                if (currentPositionProperty != null && currentPositionProperty.PropertyType == typeof(UnityEngine.Vector3))
                {
                    var pos = (UnityEngine.Vector3)currentPositionProperty.GetValue(item);
                    position = $" ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})";
                }
            }
            
            return name + position;
        }
        
        /// <summary>
        /// Creates a small square icon with white background
        /// </summary>
        private static void CreateIcon(Transform parent, float itemHeight)
        {
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(parent, false);
            
            var iconSize = 35; // Fixed small square icon
            
            var layoutElement = iconObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = iconSize;
            layoutElement.preferredHeight = iconSize;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;
            
            var iconImage = iconObj.AddComponent<Image>();
            iconImage.color = Color.white;
        }
        
        /// <summary>
        /// Creates the display name text with optional click handler - takes up most of the space
        /// </summary>
        private static void CreateDisplayName(Transform parent, string displayName, System.Action onClicked)
        {
            var nameObj = new GameObject("DisplayName");
            nameObj.transform.SetParent(parent, false);
            
            var layoutElement = nameObj.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1; // Take all remaining space
            layoutElement.preferredHeight = -1;
            
            var nameText = nameObj.AddComponent<Text>();
            nameText.text = displayName;
            nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.fontSize = 32; // Much bigger font
            nameText.color = Color.white;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.fontStyle = FontStyle.Normal;
            
            // Add click handler if provided
            if (onClicked != null)
            {
                var button = nameObj.AddComponent<Button>();
                button.targetGraphic = null; // No visual feedback, just clickable
                button.onClick.AddListener(new Action(() => onClicked()));
            }
        }
        
        /// <summary>
        /// Creates a tiny show/hide toggle
        /// </summary>
        private static void CreateShowHideToggle<T>(Transform parent, T item, int index) where T : ISerializedData
        {
            var toggleObj = new GameObject("ShowHideToggle");
            toggleObj.transform.SetParent(parent, false);
            
            var layoutElement = toggleObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 16; // Even tinier
            layoutElement.preferredHeight = 16;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;
            
            var toggle = toggleObj.AddComponent<Toggle>();
            
            // Background
            var backgroundObj = new GameObject("Background");
            backgroundObj.transform.SetParent(toggleObj.transform, false);
            
            var bgRect = backgroundObj.DefinitelyGetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            var bgImage = backgroundObj.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            // Checkmark
            var checkmarkObj = new GameObject("Checkmark");
            checkmarkObj.transform.SetParent(backgroundObj.transform, false);
            
            var checkRect = checkmarkObj.DefinitelyGetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkRect.anchorMax = new Vector2(0.9f, 0.9f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;
            
            var checkImage = checkmarkObj.AddComponent<Image>();
            checkImage.color = Color.green;
            
            // Setup toggle
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = true; // Default to visible/shown
            
            // Add toggle functionality (this would need to be connected to actual show/hide logic)
            toggle.onValueChanged.AddListener(new Action<bool>((value) => {
                // TODO: Connect to actual show/hide functionality
                // For now, just change the checkmark color
                checkImage.color = value ? Color.green : Color.red;
            }));
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
            var image = listItem.GetComponent<Image>();
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
