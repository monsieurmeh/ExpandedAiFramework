using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Il2Cpp;
using static ExpandedAiFramework.DebugMenu.Extensions;

namespace ExpandedAiFramework.DebugMenu
{
    public abstract class DebugMenuTabContentProvider<T> : IDebugMenuTabContentProvider where T : ISerializedData
    {
        // UI Components
        protected GameObject mRootPanel;
        protected GameObject mFilterPanel;
        protected GameObject mListPanel;
        protected ScrollRect mScrollRect;
        protected GameObject mScrollContent;
        protected Text mStatusText;

        // Filter components
        protected InputField mSceneFilterInput;
        protected InputField mNameFilterInput;
        protected Button mApplyFilterButton;
        protected Button mClearFilterButton;
        protected Button mRefreshButton;

        // Data
        protected List<T> mData = new List<T>();
        protected List<T> mFilteredData = new List<T>();
        protected List<GameObject> mListItems = new List<GameObject>();

        // State
        protected bool mIsVisible = false;
        protected bool mIsLoading = false;
        protected string mSceneFilter = "";
        protected string mNameFilter = "";

        public virtual void Initialize(GameObject parentContentArea)
        {
            CreateUI(parentContentArea);
        }

        protected virtual void CreateUI(GameObject parentContentArea)
        {
            // Root panel
            mRootPanel = new GameObject($"{GetType().Name}_RootPanel");
            mRootPanel.transform.SetParent(parentContentArea.transform, false);
            
            var rootRect = mRootPanel.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            CreateFilterPanel();
            CreateListPanel();
            CreateStatusText();

            mRootPanel.SetActive(false);
        }

        protected virtual void CreateFilterPanel()
        {
            mFilterPanel = new GameObject("FilterPanel");
            mFilterPanel.transform.SetParent(mRootPanel.transform, false);
            
            var filterRect = mFilterPanel.DefinitelyGetComponent<RectTransform>();
            filterRect.anchorMin = new Vector2(0, 0.95f);
            filterRect.anchorMax = new Vector2(1, 1);
            filterRect.offsetMin = new Vector2(10, -10);
            filterRect.offsetMax = new Vector2(-10, -10);
            
            // Add background
            var filterBg = mFilterPanel.AddComponent<Image>();
            filterBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            
            var filterLayout = mFilterPanel.AddComponent<HorizontalLayoutGroup>();
            filterLayout.spacing = 5;
            filterLayout.padding = new RectOffset(5, 5, 2, 2);
            filterLayout.childControlWidth = false;
            filterLayout.childControlHeight = true;
            filterLayout.childForceExpandHeight = false;

            // Scene filter group
            var sceneGroup = CreateFilterGroup("Scene Filter");
            CreateLabel("Scene:", sceneGroup.transform, 50);
            mSceneFilterInput = CreateInputField("", sceneGroup.transform, 120, OnSceneFilterChanged);
            
            // Name filter group  
            var nameGroup = CreateFilterGroup("Name Filter");
            CreateLabel("Name:", nameGroup.transform, 50);
            mNameFilterInput = CreateInputField("", nameGroup.transform, 120, OnNameFilterChanged);
            
            // Button group
            var buttonGroup = CreateFilterGroup("Actions");
            mApplyFilterButton = CreateButton("Apply", buttonGroup.transform, OnApplyFilterClicked);
            mClearFilterButton = CreateButton("Clear", buttonGroup.transform, OnClearFilterClicked);
            mRefreshButton = CreateButton("Refresh", buttonGroup.transform, OnRefreshClicked);
        }

        protected virtual GameObject CreateFilterGroup(string groupName)
        {
            var group = new GameObject(groupName);
            group.transform.SetParent(mFilterPanel.transform, false);
            
            var groupRect = group.DefinitelyGetComponent<RectTransform>();
            if (groupName == "Actions")
                groupRect.sizeDelta = new Vector2(250, 35); // Much wider for 3 buttons
            else if (groupName == "Wildlife Mode")
                groupRect.sizeDelta = new Vector2(160, 35); // Wide enough for 2 mode buttons
            else
                groupRect.sizeDelta = new Vector2(180, 35); // Wide enough for label + input
            
            var groupLayout = group.AddComponent<HorizontalLayoutGroup>();
            groupLayout.spacing = 5;
            groupLayout.padding = new RectOffset(5, 5, 2, 2);
            groupLayout.childControlWidth = false;
            groupLayout.childControlHeight = false;
            groupLayout.childForceExpandWidth = false;
            groupLayout.childForceExpandHeight = false;
            
            return group;
        }

        protected virtual void CreateListPanel()
        {
            mListPanel = new GameObject("ListPanel");
            mListPanel.transform.SetParent(mRootPanel.transform, false);
            
            var listRect = mListPanel.DefinitelyGetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0.05f);
            listRect.anchorMax = new Vector2(1, 0.95f);
            listRect.offsetMin = new Vector2(10, 10);
            listRect.offsetMax = new Vector2(-10, -10);
            
            // Scroll rect
            mScrollRect = mListPanel.AddComponent<ScrollRect>();
            mScrollRect.horizontal = false;
            mScrollRect.vertical = true;
            mScrollRect.scrollSensitivity = 20f; // Increase scroll wheel sensitivity
            mScrollRect.movementType = ScrollRect.MovementType.Clamped;
            
            var scrollImage = mListPanel.AddComponent<Image>();
            scrollImage.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);
            
            // Add mask to prevent content bleeding
            var mask = mListPanel.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            
            // Scroll content
            mScrollContent = new GameObject("ScrollContent");
            mScrollContent.transform.SetParent(mListPanel.transform, false);
            
            var contentRect = mScrollContent.DefinitelyGetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);
            
            var contentLayout = mScrollContent.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 1;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.padding = new RectOffset(8, 8, 8, 8);
            contentLayout.childForceExpandWidth = true;
            
            var contentSizeFitter = mScrollContent.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            mScrollRect.content = contentRect;
        }

        protected virtual void CreateStatusText()
        {
            var statusObj = new GameObject("StatusText");
            statusObj.transform.SetParent(mRootPanel.transform, false);
            
            var statusRect = statusObj.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0);
            statusRect.anchorMax = new Vector2(1, 0.05f);
            statusRect.offsetMin = new Vector2(5, 0);
            statusRect.offsetMax = new Vector2(-5, 0);
            
            mStatusText = statusObj.AddComponent<Text>();
            mStatusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            mStatusText.fontSize = 12;
            mStatusText.color = Color.white;
            mStatusText.alignment = TextAnchor.MiddleLeft;
            mStatusText.text = "Loading...";
        }

        protected virtual Text CreateLabel(string text, Transform parent, float width)
        {
            var labelObj = new GameObject($"Label_{text}");
            labelObj.transform.SetParent(parent, false);
            
            var labelRect = labelObj.DefinitelyGetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(width, 22);
            
            // Add LayoutElement to maintain size in layout groups
            var layoutElement = labelObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = 22;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;
            
            var label = labelObj.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 11;
            label.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            label.alignment = TextAnchor.MiddleLeft;
            label.fontStyle = FontStyle.Bold;
            
            return label;
        }

        protected virtual InputField CreateInputField(string placeholder, Transform parent, float width, Action<string> onValueChanged)
        {
            return CreateInputFieldInternal(placeholder, parent, width, (UnityEngine.Events.UnityAction<string>)onValueChanged);
        }

        private InputField CreateInputField(string placeholder, Transform parent, float width, UnityEngine.Events.UnityAction<string> onValueChanged)
        {
            return CreateInputFieldInternal(placeholder, parent, width, onValueChanged);
        }

        protected virtual InputField CreateInputFieldInternal(string placeholder, Transform parent, float width, UnityEngine.Events.UnityAction<string> onValueChanged)
        {
            var inputObj = new GameObject($"InputField_{placeholder}");
            inputObj.transform.SetParent(parent, false);
            var inputRect = inputObj.DefinitelyGetComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(width, 50);
            
            // Add LayoutElement to maintain size in layout groups
            var layoutElement = inputObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = 50;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;
            
            var inputImage = inputObj.AddComponent<Image>();
            inputImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            var inputField = inputObj.AddComponent<InputField>();
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            var inputText = textObj.AddComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            inputText.fontSize = 10;            
            inputText.color = Color.white;
            inputText.supportRichText = false;
            var textRect = textObj.DefinitelyGetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6, 2);
            textRect.offsetMax = new Vector2(-6, -2);
            inputField.textComponent = inputText;
            inputField.text = "";
            if (onValueChanged != null)
            {
                inputField.onValueChanged.AddListener(onValueChanged);
            }
            return inputField;
        }

        protected virtual Button CreateButton(string text, Transform parent, UnityEngine.Events.UnityAction onClick)
        {
            return CreateButtonInternal(text, parent, onClick);
        }

        protected virtual Button CreateButton(string text, Transform parent, Action onClick)
        {
            return CreateButtonInternal(text, parent, (UnityEngine.Events.UnityAction)onClick);
        }

        private Button CreateButtonInternal(string text, Transform parent, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObj = new GameObject($"Button_{text}");
            buttonObj.transform.SetParent(parent, false);
            
            var buttonRect = buttonObj.DefinitelyGetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(70, 28);
            
            // Add LayoutElement to maintain size in layout groups
            var layoutElement = buttonObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 70;
            layoutElement.preferredHeight = 28;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;
            
            var button = buttonObj.AddComponent<Button>();
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            
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
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.fontStyle = FontStyle.Bold;
            
            // Button hover colors
            var colors = button.colors;
            colors.normalColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            colors.highlightedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            colors.pressedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            button.colors = colors;
            
            button.onClick.AddListener(onClick);
            
            return button;
        }

        protected virtual GameObject CreateListItem(T item, int index)
        {
            var itemObj = new GameObject($"ListItem_{index}");
            itemObj.transform.SetParent(mScrollContent.transform, false);
            
            var itemRect = itemObj.DefinitelyGetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, GetItemHeight());
            
            var button = itemObj.AddComponent<Button>();
            var buttonImage = itemObj.AddComponent<Image>();
            buttonImage.color = index % 2 == 0 ? 
                new Color(0.22f, 0.22f, 0.22f, 1f) : 
                new Color(0.18f, 0.18f, 0.18f, 1f);
            
            // Button hover colors
            var colors = button.colors;
            colors.normalColor = buttonImage.color;
            colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            colors.pressedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            button.colors = colors;
            
            // Item content
            PopulateListItem(itemObj, item, index);
            
            // Click handler
            button.onClick.AddListener(new Action(() => OnItemClicked(item)));
            
            return itemObj;
        }

        // Event handlers
        protected virtual void OnSceneFilterChanged(string value)
        {
            mSceneFilter = value;
        }

        protected virtual void OnNameFilterChanged(string value)
        {
            mNameFilter = value;
        }

        protected virtual void OnApplyFilterClicked()
        {
            ApplyFilters();
            UpdateDisplay();
        }

        protected virtual void OnClearFilterClicked()
        {
            mSceneFilter = "";
            mNameFilter = "";
            mSceneFilterInput.text = "";
            mNameFilterInput.text = "";
            ApplyFilters();
            UpdateDisplay();
        }

        protected virtual void OnRefreshClicked()
        {
            Refresh();
        }

        protected virtual void OnItemClicked(T item)
        {
            LogDebug($"Item clicked: {item?.DisplayName ?? "null"}");
            
            if (DebugMenuManager.Instance == null)
            {
                LogError("DebugMenuManager.Instance is null!");
                return;
            }
            
            LogDebug("Calling ShowItemDetails...");
            DebugMenuManager.Instance.ShowItemDetails(item);
        }

        // Public interface
        public virtual void Show()
        {
            mIsVisible = true;
            if (mRootPanel != null)
            {
                mRootPanel.SetActive(true);
            }
        }

        public virtual void Hide()
        {
            mIsVisible = false;
            if (mRootPanel != null)
            {
                mRootPanel.SetActive(false);
            }
        }

        public virtual void Refresh()
        {
            if (!mIsVisible) return;
            
            mIsLoading = true;
            UpdateStatusText();
            LoadData();
        }

        public virtual void Cleanup()
        {
            if (mRootPanel != null)
            {
                UnityEngine.Object.Destroy(mRootPanel);
            }
        }

        protected virtual void ApplyFilters()
        {
            mFilteredData.Clear();
            
            foreach (var item in mData)
            {
                bool passesFilter = true;
                
                if (!string.IsNullOrEmpty(mSceneFilter) && !item.Scene.ToLower().Contains(mSceneFilter.ToLower()))
                {
                    passesFilter = false;
                }
                
                if (!string.IsNullOrEmpty(mNameFilter) && !GetItemName(item).ToLower().Contains(mNameFilter.ToLower()))
                {
                    passesFilter = false;
                }
                
                if (passesFilter && PassesCustomFilter(item))
                {
                    mFilteredData.Add(item);
                }
            }
        }

        protected virtual void UpdateDisplay()
        {
            // Clear existing items
            foreach (var item in mListItems)
            {
                if (item != null)
                {
                    UnityEngine.Object.Destroy(item);
                }
            }
            mListItems.Clear();

            // Create new items
            for (int i = 0; i < mFilteredData.Count; i++)
            {
                var listItem = CreateListItem(mFilteredData[i], i);
                mListItems.Add(listItem);
            }

            UpdateStatusText();
        }

        protected virtual void UpdateStatusText()
        {
            if (mStatusText == null) return;
            
            string statusText = GetTabDisplayName();
            
            if (mIsLoading)
            {
                statusText += " - Loading...";
            }
            else
            {
                statusText += $" - {mFilteredData.Count}/{mData.Count} items";
                
                if (!string.IsNullOrEmpty(mSceneFilter))
                {
                    statusText += $" | Scene: '{mSceneFilter}'";
                }
                
                if (!string.IsNullOrEmpty(mNameFilter))
                {
                    statusText += $" | Name: '{mNameFilter}'";
                }
            }
            
            mStatusText.text = statusText;
        }

        protected virtual void OnDataLoaded(List<T> data, RequestResult result)
        {
            mIsLoading = false;
            
            if (result == RequestResult.Succeeded)
            {
                mData = data ?? new List<T>();
                ApplyFilters();
                UpdateDisplay();
                LogDebug($"Loaded {mData.Count} items for {GetTabDisplayName()}");
            }
            else
            {
                LogError($"Failed to load data for {GetTabDisplayName()}: {result}");
                mData.Clear();
                mFilteredData.Clear();
                UpdateDisplay();
            }
        }

        // Abstract methods to be implemented by derived classes
        protected abstract void LoadData();
        protected abstract void PopulateListItem(GameObject itemObj, T item, int index);
        protected abstract string GetItemName(T item);
        protected abstract string GetTabDisplayName();
        protected abstract float GetItemHeight();
        protected virtual bool PassesCustomFilter(T item) => true;
    }
}
