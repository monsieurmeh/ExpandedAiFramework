using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ExpandedAiFramework.UI
{    
    [System.Serializable]
    public struct DropdownOptions
    {
        public TextFieldOptions captionTextOptions;
        public TextOptions arrowTextOptions;
        public ImageOptions backgroundImageOptions;
        public ImageOptions templateBackgroundOptions;
        public ImageOptions itemBackgroundOptions;
        public TextOptions itemTextOptions;
        public LayoutElementOptions layoutElement;
        public float templateHeight;

        public static DropdownOptions Default()
        {
            return new DropdownOptions
            {
                captionTextOptions = TextFieldOptions.Default(TextOptions.Default("", 14)),
                arrowTextOptions = TextOptions.Default("▼", 14),
                backgroundImageOptions = new ImageOptions { Color = Color.white },
                templateBackgroundOptions = new ImageOptions { Color = new Color(0.95f, 0.95f, 0.95f, 1f) },
                itemBackgroundOptions = new ImageOptions { Color = new Color(0.98f, 0.98f, 0.98f, 1f) },
                itemTextOptions = new TextOptions { text = "", fontSize = 12, color = Color.black, alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Normal },
                layoutElement = LayoutElementOptions.MinSize(100, 25, 1, 0),
                templateHeight = 150f
            };
        }
    }

    public static class DropdownFactory
    {            
        public static Dropdown CreateDropdown(Transform parent, List<string> options, int selectedIndex = 0, Action<int> onValueChanged = null, DropdownOptions? dropdownOptions = null)
        {
            var opts = dropdownOptions ?? DropdownOptions.Default();
            
            // Create main dropdown container
            var dropdownObj = new GameObject("Dropdown");
            dropdownObj.transform.SetParent(parent, false);
            
            // Add layout element for sizing - this ensures the dropdown respects layout groups
            LayoutFactory.CreateLayoutElement(dropdownObj.transform, opts.layoutElement);
            
            // Add background image using factory
            Image backgroundImage = ImageFactory.CreateImage(dropdownObj.transform, opts.backgroundImageOptions);
            backgroundImage.type = Image.Type.Sliced;
            
            // Add dropdown component
            var dropdown = dropdownObj.AddComponent<Dropdown>();
            dropdown.targetGraphic = backgroundImage;
            
            // Create the caption and arrow using Unity's expected structure
            CreateCaptionAndArrow(dropdownObj, dropdown, opts);
            
            // Create dropdown template following Unity's standard expectations
            CreateUnityStandardTemplate(dropdownObj, dropdown, opts);
            
            // Populate options
            dropdown.options.Clear();
            foreach (var option in options)
            {
                dropdown.options.Add(new Dropdown.OptionData(option));
            }
            
            // Set selected value
            dropdown.value = selectedIndex >= 0 && selectedIndex < options.Count ? selectedIndex : 0;
            dropdown.RefreshShownValue();
            
            // Add event listener
            if (onValueChanged != null)
            {
                dropdown.onValueChanged.AddListener(onValueChanged);
            }
            
            return dropdown;
        }

        private static void CreateCaptionAndArrow(GameObject dropdownObj, Dropdown dropdown, DropdownOptions opts)
        {
            // Create Label (Caption Text) - Unity expects this exact structure
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(dropdownObj.transform, false);
            
            var labelRect = GetOrCreateRectTransform(labelObj);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 6);
            labelRect.offsetMax = new Vector2(-25, -7);
            
            var captionText = TextFactory.CreateText(labelObj.transform, opts.captionTextOptions.textOptions);
            dropdown.captionText = captionText;
            
            // Create Arrow - Unity expects this exact structure
            var arrowObj = new GameObject("Arrow");
            arrowObj.transform.SetParent(dropdownObj.transform, false);
            
            var arrowRect = arrowObj.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1, 0.5f);
            arrowRect.anchorMax = new Vector2(1, 0.5f);
            arrowRect.sizeDelta = new Vector2(20, 20);
            arrowRect.anchoredPosition = new Vector2(-15, 0);
            
            TextFactory.CreateText(arrowObj.transform, opts.arrowTextOptions);
        }

        private static void CreateUnityStandardTemplate(GameObject dropdownObj, Dropdown dropdown, DropdownOptions opts)
        {
            // Create template following Unity's standard structure
            var template = new GameObject("Template");
            template.transform.SetParent(dropdownObj.transform, false);
            template.SetActive(false);
            
            var templateRect = GetOrCreateRectTransform(template);
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.anchoredPosition = new Vector2(0, 2);
            templateRect.sizeDelta = new Vector2(0, opts.templateHeight);
            
            // Template background using factory
            var templateBg = ImageFactory.CreateImage(template.transform, opts.templateBackgroundOptions);
            templateBg.type = Image.Type.Sliced;
            
            // Create Viewport for scrolling
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(template.transform, false);
            
            var viewportRect = GetOrCreateRectTransform(viewport);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            
            // Add mask for clipping
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            
            // Viewport needs an image for the mask to work
            var viewportImage = ImageFactory.CreateImage(viewport.transform, new ImageOptions { Color = Color.white });
            viewportImage.raycastTarget = false;
            
            // Create Content container
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            
            var contentRect = GetOrCreateRectTransform(content);
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, opts.itemTextOptions.fontSize + 8);
            
            // Simple vertical layout for items
            var verticalLayout = content.AddComponent<VerticalLayoutGroup>();
            verticalLayout.childControlWidth = true;
            verticalLayout.childControlHeight = true;
            verticalLayout.childForceExpandWidth = true;
            verticalLayout.childForceExpandHeight = false;
            verticalLayout.spacing = 0;
            verticalLayout.padding = new RectOffset(0, 0, 0, 0);
            
            // ContentSizeFitter to resize content based on children
            var sizeFitter = content.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // ScrollRect for scrolling behavior
            var scrollRect = template.AddComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalScrollbar = null;
            scrollRect.horizontalScrollbar = null;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            
            // Create the item template - this is critical
            var item = CreateSimpleDropdownItem(content.transform, opts);
            
            // Set dropdown references - Unity uses these to clone items
            dropdown.template = templateRect;
            dropdown.itemText = item.GetComponentInChildren<Text>();
        }

        private static Toggle CreateSimpleDropdownItem(Transform parent, DropdownOptions opts)
        {
            // Create the most basic dropdown item structure possible
            var item = new GameObject("Item");
            item.transform.SetParent(parent, false);
            
            var itemRect = GetOrCreateRectTransform(item);
            itemRect.anchorMin = Vector2.zero;
            itemRect.anchorMax = Vector2.one;
            itemRect.sizeDelta = new Vector2(0, opts.itemTextOptions.fontSize + 8);
            
            // Layout element for vertical layout group
            var layoutElement = item.AddComponent<LayoutElement>();
            layoutElement.minHeight = opts.itemTextOptions.fontSize + 8;
            layoutElement.preferredHeight = opts.itemTextOptions.fontSize + 8;
            layoutElement.flexibleHeight = 0;
            
            // Simple background
            var itemBg = ImageFactory.CreateImage(item.transform, opts.itemBackgroundOptions);
            
            // Toggle component
            var toggle = item.AddComponent<Toggle>();
            toggle.targetGraphic = itemBg;
            toggle.isOn = false;
            
            // Text component directly on the item
            var textObj = new GameObject("Item Label");
            textObj.transform.SetParent(item.transform, false);
            
            var textRect = GetOrCreateRectTransform(textObj);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);
            
            // Create visible text
            var textOptions = new TextOptions
            {
                text = "Option",
                fontSize = opts.itemTextOptions.fontSize,
                color = Color.black,
                alignment = TextAnchor.MiddleLeft,
                fontStyle = FontStyle.Normal
            };
            
            var text = TextFactory.CreateText(textObj.transform, textOptions);
            
            // Simple checkmark (invisible)
            var checkmark = new GameObject("Item Checkmark");
            checkmark.transform.SetParent(item.transform, false);
            
            var checkRect = GetOrCreateRectTransform(checkmark);
            checkRect.anchorMin = new Vector2(1, 0.5f);
            checkRect.anchorMax = new Vector2(1, 0.5f);
            checkRect.sizeDelta = new Vector2(16, 16);
            checkRect.anchoredPosition = new Vector2(-12, 0);
            
            var checkImage = ImageFactory.CreateImage(checkmark.transform, new ImageOptions { Color = Color.clear });
            toggle.graphic = checkImage;
            
            return toggle;
        }

        // Convenience method
        public static Dropdown CreateDropdown(Transform parent, List<string> options, Action<int> onValueChanged = null)
        {
            return CreateDropdown(parent, options, 0, onValueChanged, null);
        }

        private static RectTransform GetOrCreateRectTransform(GameObject obj)
        {
            return obj.GetComponent<RectTransform>() ?? obj.AddComponent<RectTransform>();
        }
    }
}