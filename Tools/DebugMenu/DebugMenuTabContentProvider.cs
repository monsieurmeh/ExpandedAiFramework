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
        protected T mSelectedItem = default(T);
        protected int mSelectedIndex = -1;

        // State
        protected bool mIsVisible = false;
        protected bool mIsLoading = false;
        protected string mSceneFilter = "";
        protected string mNameFilter = "";

        // SubDataManager for CRUD operations
        protected ISubDataManager mSubDataManager;

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

            CreateUnifiedButtonBar();
            CreateListPanel();
            CreateStatusText();

            mRootPanel.SetActive(false);
        }

        protected virtual void CreateUnifiedButtonBar()
        {
            // Create unified button bar that combines filter, action, and tab-specific buttons
            mFilterPanel = new GameObject("UnifiedButtonBar");
            mFilterPanel.transform.SetParent(mRootPanel.transform, false);
            
            var buttonBarRect = mFilterPanel.DefinitelyGetComponent<RectTransform>();
            buttonBarRect.anchorMin = new Vector2(0, 0.9f);
            buttonBarRect.anchorMax = new Vector2(1, 1);
            buttonBarRect.offsetMin = new Vector2(10, -10);
            buttonBarRect.offsetMax = new Vector2(-10, -10);
            
            // Add background
            var buttonBarBg = mFilterPanel.AddComponent<Image>();
            buttonBarBg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            
            var buttonBarLayout = mFilterPanel.AddComponent<HorizontalLayoutGroup>();
            buttonBarLayout.spacing = 5;
            buttonBarLayout.padding = new RectOffset(5, 5, 2, 2);
            buttonBarLayout.childControlWidth = false;
            buttonBarLayout.childControlHeight = true;
            buttonBarLayout.childForceExpandWidth = false;
            buttonBarLayout.childForceExpandHeight = false;
            buttonBarLayout.childAlignment = TextAnchor.MiddleLeft; // Left-align buttons

            // Create filter controls first (always present)
            CreateFilterControls();
            
            // Create global action buttons (always present)
            CreateGlobalActionButtons();
            
            // Create tab-specific buttons (will be added by derived classes)
            CreateTabSpecificButtons();
        }

        protected virtual void CreateFilterControls()
        {
            // Scene filter group
            var sceneGroup = CreateButtonGroup("Scene Filter", 180);
            CreateLabel("Scene:", sceneGroup.transform, 50);
            mSceneFilterInput = CreateInputField("", sceneGroup.transform, 120, OnSceneFilterChanged);
            
            // Name filter group  
            var nameGroup = CreateButtonGroup("Name Filter", 180);
            CreateLabel("Name:", nameGroup.transform, 50);
            mNameFilterInput = CreateInputField("", nameGroup.transform, 120, OnNameFilterChanged);
            
            // Filter action buttons
            var filterButtonGroup = CreateButtonGroup("Filter Actions", 220);
            mApplyFilterButton = CreateButton("Apply", filterButtonGroup.transform, OnApplyFilterClicked);
            mClearFilterButton = CreateButton("Clear", filterButtonGroup.transform, OnClearFilterClicked);
        }

        protected virtual void CreateGlobalActionButtons()
        {
            // Global action buttons (Save, Load, Refresh)
            var globalGroup = CreateButtonGroup("Global Actions", 220);
            var saveButton = CreateButton("Save", globalGroup.transform, OnSaveClicked);
            var loadButton = CreateButton("Load", globalGroup.transform, OnLoadClicked);
            mRefreshButton = CreateButton("Refresh", globalGroup.transform, OnRefreshClicked);
        }

        protected virtual void CreateTabSpecificButtons()
        {
            // Override in derived classes to add tab-specific buttons
        }

        protected virtual GameObject CreateButtonGroup(string groupName, float width)
        {
            var group = new GameObject(groupName);
            group.transform.SetParent(mFilterPanel.transform, false);
            
            var groupRect = group.DefinitelyGetComponent<RectTransform>();
            groupRect.sizeDelta = new Vector2(width, 35);
            
            // Add LayoutElement to maintain consistent sizing
            var layoutElement = group.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = 35;
            layoutElement.flexibleWidth = 0;
            layoutElement.flexibleHeight = 0;
            
            var groupLayout = group.AddComponent<HorizontalLayoutGroup>();
            groupLayout.spacing = 5;
            groupLayout.padding = new RectOffset(5, 5, 2, 2);
            groupLayout.childControlWidth = false;
            groupLayout.childControlHeight = false;
            groupLayout.childForceExpandWidth = false;
            groupLayout.childForceExpandHeight = false;
            groupLayout.childAlignment = TextAnchor.MiddleLeft; // Left-align within group
            
            return group;
        }

        // Legacy method for backward compatibility
        protected virtual GameObject CreateFilterGroup(string groupName)
        {
            if (groupName == "Actions")
                return CreateButtonGroup(groupName, 250);
            else if (groupName == "Wildlife Mode")
                return CreateButtonGroup(groupName, 160);
            else
                return CreateButtonGroup(groupName, 180);
        }


        protected virtual void CreateListPanel()
        {
            mListPanel = new GameObject("ListPanel");
            mListPanel.transform.SetParent(mRootPanel.transform, false);
            
            var listRect = mListPanel.DefinitelyGetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0.05f);
            listRect.anchorMax = new Vector2(1, 0.9f); // Adjust to account for unified button bar
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
            
            // Main content area
            var contentArea = new GameObject("ContentArea");
            contentArea.transform.SetParent(itemObj.transform, false);
            
            var contentRect = contentArea.DefinitelyGetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(0.75f, 1); // Leave space for buttons
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
            
            // Item content
            PopulateListItem(contentArea, item, index);
            
            // Click handler
            contentButton.onClick.AddListener(new Action(() => OnItemClicked(item)));
            
            // Entity action buttons area
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
            
            // Create entity action buttons
            CreateEntityActionButtons(buttonArea, item, index);
            
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
            
            // Update selection
            mSelectedItem = item;
            mSelectedIndex = mFilteredData.IndexOf(item);
            UpdateListItemSelection();
            
            if (DebugMenuManager.Instance == null)
            {
                LogError("DebugMenuManager.Instance is null!");
                return;
            }
            
            LogDebug("Calling ShowItemDetails...");
            DebugMenuManager.Instance.ShowItemDetails(item);
        }

        protected virtual void UpdateListItemSelection()
        {
            for (int i = 0; i < mListItems.Count; i++)
            {
                if (mListItems[i] != null)
                {
                    var image = mListItems[i].GetComponent<Image>();
                    if (image != null)
                    {
                        if (i == mSelectedIndex)
                        {
                            // Highlight selected item
                            image.color = new Color(0.4f, 0.6f, 0.8f, 1f);
                        }
                        else
                        {
                            // Normal alternating colors
                            image.color = i % 2 == 0 ? 
                                new Color(0.22f, 0.22f, 0.22f, 1f) : 
                                new Color(0.18f, 0.18f, 0.18f, 1f);
                        }
                    }
                }
            }
        }

        // Action event handlers
        protected virtual void OnSaveClicked()
        {
            if (mSubDataManager != null)
            {
                mSubDataManager.ScheduleSave();
                LogDebug($"Saved data for {GetTabDisplayName()}");
            }
            else
            {
                LogError($"No SubDataManager found for {GetTabDisplayName()}");
            }
        }

        protected virtual void OnLoadClicked()
        {
            if (mSubDataManager != null)
            {
                mSubDataManager.ScheduleLoad();
                LogDebug($"Loaded data for {GetTabDisplayName()}");
                Refresh(); // Refresh the list after loading
            }
            else
            {
                LogError($"No SubDataManager found for {GetTabDisplayName()}");
            }
        }

        protected virtual void CreateEntityActionButtons(GameObject buttonArea, T item, int index)
        {
            // GoTo button
            var gotoButton = CreateButton("GoTo", buttonArea.transform, () => OnGoToClicked(item));
            
            // Delete button
            var deleteButton = CreateButton("Delete", buttonArea.transform, () => OnDeleteClicked(item));
        }

        protected virtual void OnGoToClicked(T item)
        {
            LogDebug($"GoTo clicked for {GetItemName(item)} - override in derived class for specific behavior");
        }

        protected virtual void OnDeleteClicked(T item)
        {
            LogDebug($"Delete clicked for {GetItemName(item)} - override in derived class for specific behavior");
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

        // SubDataManager integration - to be set by derived classes
        protected abstract ISubDataManager GetSubDataManager();
    }
}
