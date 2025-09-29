using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
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
            
            // 2. Icon (small square with wildlife icon)
            CreateIcon(itemObj.transform, itemHeight, item);
            
            // 3. Name and Position (takes up most space)
            CreateDisplayName(itemObj.transform, GetItemDisplayText(item), onItemClicked != null ? () => onItemClicked(item) : null);
            
            return itemObj;
        }
        

        /// <summary>
        /// Gets the display text for an item: Name (SceneName) @ Position
        /// </summary>
        private static string GetItemDisplayText<T>(T item) where T : ISerializedData
        {            
            switch (item)
            {
                case HidingSpot hidingSpot:
                    return hidingSpot.Name;
                case WanderPath wanderPath:
                    return wanderPath.Name;
                case SpawnModDataProxy spawnModDataProxy:
                    return spawnModDataProxy.VariantSpawnType.Name;
                case SpawnRegionModDataProxy spawnRegionModDataProxy:
                    return spawnRegionModDataProxy.Name;
                default:
                    return item.DisplayName;
            }
        }

        private struct SpriteInfo 
        {
            public string SpriteName;
            public string AtlasName;
            public bool UseAtlas;
        }
        
        /// <summary>
        /// Gets the wildlife icon name based on the data item
        /// </summary>
        private static SpriteInfo GetIconInfo<T>(T item) where T : ISerializedData
        {
            var type = item.GetType();
            switch (type.Name)
            {
                case "SpawnModDataProxy":
                    if (item is not SpawnModDataProxy spawnModDataProxy)
                    {
                        return new SpriteInfo { SpriteName = "Default", AtlasName = "Default", UseAtlas = false };
                    }
                     return new SpriteInfo { SpriteName = MapAiSubTypeToIcon(GetAiTypeString(spawnModDataProxy), spawnModDataProxy.WildlifeMode.ToString()), AtlasName = "", UseAtlas = false };
                case "SpawnRegionModDataProxy":
                    if (item is not SpawnRegionModDataProxy spawnRegionModDataProxy)
                    {   
                        return new SpriteInfo { SpriteName = "Default", AtlasName = "Default", UseAtlas = false };
                    }
                     return new SpriteInfo { SpriteName = MapAiSubTypeToIcon(GetAiTypeString(spawnRegionModDataProxy), spawnRegionModDataProxy.WildlifeMode.ToString()), AtlasName = "", UseAtlas = false };
                default:
                    return new SpriteInfo { SpriteName = "Default", AtlasName = "Default", UseAtlas = false };
            }
        }

        private static string GetAiTypeString(SpawnModDataProxy spawnModDataProxy)
        {
            if (spawnModDataProxy.AiSubType == AiSubType.Wolf)
            {
                return spawnModDataProxy.WolfType == WolfType.Timberwolf ? "timberwolf" : "wolf";
            }
            else if (spawnModDataProxy.AiSubType == AiSubType.Rabbit)
            {
                return spawnModDataProxy.WolfType == WolfType.Timberwolf ? "ptarmigan" : "rabbit";
            }
            return spawnModDataProxy.AiSubType.ToString();
        }

        private static string GetAiTypeString(SpawnRegionModDataProxy spawnRegionModDataProxy)
        {
            if (spawnRegionModDataProxy.AiSubType == AiSubType.Wolf)
            {
                return spawnRegionModDataProxy.WolfType == WolfType.Timberwolf ? "timberwolf" : "wolf";
            }
            else if (spawnRegionModDataProxy.AiSubType == AiSubType.Rabbit)
            {
                return spawnRegionModDataProxy.WolfType == WolfType.Timberwolf ? "ptarmigan" : "rabbit";
            }
            return spawnRegionModDataProxy.AiSubType.ToString();
        }
        
        /// <summary>
        /// Maps AiSubType and WildlifeMode to icon sprite names
        /// </summary>
        private static string MapAiSubTypeToIcon(string aiSubType, string wildlifeMode)
        {
            if (string.IsNullOrEmpty(aiSubType)) return null;
            
            // Handle aurora mode variants
            bool isAurora = wildlifeMode?.Contains("Aurora") == true;
            
            switch (aiSubType.ToLower())
            {
                case "bear":
                    return isAurora ? "Ico_DeadlyBear" : "Ico_Bear";
                    
                case "cougar":
                    return isAurora ? "Ico_FuckingCougar" : "Ico_Cougar";
                    
                case "timberwolf":
                    return isAurora ? "Ico_LaggyTimberwolf" : "Ico_Timberwolf";
                    
                case "wolf":
                    return isAurora ? "Ico_WolfAurora" : "Ico_Wolf";
                    
                case "rabbit":
                    return isAurora ? "Ico_HIM" : "Ico_RabbitHop";
                    
                case "ptarmigan":
                    return isAurora ? "Ico_DeadlyBirb" : "Ico_Ptarmigan";
                    
                case "deer":
                    return isAurora ? "Ico_HIM" : "Ico_RabbitHop";
                    
                case "moose":
                    return isAurora ? "Ico_HIM" : "Ico_RabbitHop";
                    
                default:
                    return isAurora ? "Ico_DeadlyBirb" : "Ico_Ptarmigan";
            }
        }
        
        /// <summary>
        /// Creates a small square icon with wildlife sprite
        /// </summary>
        private static void CreateIcon<T>(Transform parent, float itemHeight, T item) where T : ISerializedData
        {
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(parent, false);
            
            var iconSize = 35; // Fixed small square icon
            
            var layoutElement = iconObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = iconSize;
            layoutElement.preferredHeight = iconSize;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;
            
            // Add background and mask to contain the icon properly
            var backgroundImage = iconObj.AddComponent<Image>();
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.3f); // Subtle background
            
            // Add mask to clip content to bounds
            var mask = iconObj.AddComponent<Mask>();
            mask.showMaskGraphic = true; // Show the background
            

            var iconInfo = GetIconInfo(item);
            
            // Create child object for the actual sprite (so it gets masked)
            var spriteObj = new GameObject("Sprite");
            spriteObj.transform.SetParent(iconObj.transform, false);
            
            var spriteRect = spriteObj.DefinitelyGetComponent<RectTransform>();
            spriteRect.anchorMin = Vector2.zero;
            spriteRect.anchorMax = Vector2.one;
            spriteRect.offsetMin = new Vector2(2, 2); // Small padding
            spriteRect.offsetMax = new Vector2(-2, -2);
            
            try
            {
                // Try to load sprite from AssetBundle (individual sprites, no atlas)
                string assetBundlePath = Path.Combine(MelonLoader.Utils.MelonEnvironment.ModsDirectory, "EAF", "assets");
                Sprite sprite = null;
                
                if (iconInfo.UseAtlas && !string.IsNullOrEmpty(iconInfo.AtlasName))
                {
                    sprite = ImageFactory.LoadSpriteFromAtlas(assetBundlePath, iconInfo.AtlasName, iconInfo.SpriteName);
                }
                else
                {
                    sprite = ImageFactory.LoadSpriteFromAssetBundle(assetBundlePath, iconInfo.SpriteName);
                }
                
                // Create the sprite image
                var spriteImage = spriteObj.AddComponent<Image>();
                spriteImage.sprite = sprite;
                spriteImage.type = Image.Type.Simple;
                spriteImage.preserveAspect = true;
                spriteImage.raycastTarget = false;
                
                // Set color - white for sprites, fallback color if no sprite
                if (sprite != null)
                {
                    spriteImage.color = Color.white;
                }
            }
            catch (System.Exception ex)
            {
                // Fallback to colored square if AssetBundle loading fails
                MelonLoader.MelonLogger.Warning($"Failed to load wildlife icon '{iconInfo.SpriteName}': {ex.Message}");
                var spriteImage = spriteObj.AddComponent<Image>();
                spriteImage.color = Color.white;
                spriteImage.type = Image.Type.Simple;
                spriteImage.preserveAspect = true;
                spriteImage.raycastTarget = false;
            }
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
            
            // Check if current scene matches item's scene
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            var itemScene = item.Scene;
            var isCurrentScene = string.Equals(currentScene, itemScene, System.StringComparison.OrdinalIgnoreCase);
            
            // Setup toggle
            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = true; // Default to visible/shown
            toggle.interactable = isCurrentScene; // Disable if scene mismatch
            
            // Visual feedback for disabled state
            if (!isCurrentScene)
            {
                bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Darker, more transparent
                checkImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Grayed out
            }
            
            // Add toggle functionality (this would need to be connected to actual show/hide logic)
            toggle.onValueChanged.AddListener(new Action<bool>((value) => {
                // TODO: Connect to actual show/hide functionality
                // For now, just change the checkmark color (only if enabled)
                if (toggle.interactable)
                {
                    checkImage.color = value ? Color.green : Color.red;
                }
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
