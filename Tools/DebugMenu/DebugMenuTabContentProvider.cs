using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Il2Cpp;

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
            
            var filterRect = mFilterPanel.AddComponent<RectTransform>();
            filterRect.anchorMin = new Vector2(0, 0.85f);
            filterRect.anchorMax = new Vector2(1, 1);
            filterRect.offsetMin = new Vector2(5, -5);
            filterRect.offsetMax = new Vector2(-5, -5);
            
            var filterLayout = mFilterPanel.AddComponent<HorizontalLayoutGroup>();
            filterLayout.spacing = 10;
            filterLayout.childControlWidth = false;
            filterLayout.childControlHeight = true;

            // Scene filter
            CreateLabel("Scene:", mFilterPanel.transform, 60);
            mSceneFilterInput = CreateInputField("", mFilterPanel.transform, 150, OnSceneFilterChanged);
            
            // Name filter
            CreateLabel("Name:", mFilterPanel.transform, 60);
            mNameFilterInput = CreateInputField("", mFilterPanel.transform, 150, OnNameFilterChanged);
            
            // Filter buttons
            mApplyFilterButton = CreateButton("Apply", mFilterPanel.transform, OnApplyFilterClicked);
            mClearFilterButton = CreateButton("Clear", mFilterPanel.transform, OnClearFilterClicked);
            mRefreshButton = CreateButton("Refresh", mFilterPanel.transform, OnRefreshClicked);
        }

        protected virtual void CreateListPanel()
        {
            mListPanel = new GameObject("ListPanel");
            mListPanel.transform.SetParent(mRootPanel.transform, false);
            
            var listRect = mListPanel.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0.05f);
            listRect.anchorMax = new Vector2(1, 0.85f);
            listRect.offsetMin = new Vector2(10, 10);
            listRect.offsetMax = new Vector2(-10, -10);
            
            // Scroll rect
            mScrollRect = mListPanel.AddComponent<ScrollRect>();
            mScrollRect.horizontal = false;
            mScrollRect.vertical = true;
            
            var scrollImage = mListPanel.AddComponent<Image>();
            scrollImage.color = new Color(0.05f, 0.05f, 0.05f, 0.8f);
            
            // Scroll content
            mScrollContent = new GameObject("ScrollContent");
            mScrollContent.transform.SetParent(mListPanel.transform, false);
            
            var contentRect = mScrollContent.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);
            
            var contentLayout = mScrollContent.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 2;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            
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
            
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(width, 25);
            
            var label = labelObj.AddComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 12;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            
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
            var inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(width, 25);
            var inputImage = inputObj.AddComponent<Image>();
            inputImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var inputField = inputObj.AddComponent<InputField>();
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            var inputText = textObj.AddComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            inputText.fontSize = 12;            
            inputText.color = Color.white;
            inputText.supportRichText = false;
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 2);
            textRect.offsetMax = new Vector2(-5, -2);
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
            
            var buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(80, 25);
            
            var button = buttonObj.AddComponent<Button>();
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            // Button text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            var buttonText = textObj.AddComponent<Text>();
            buttonText.text = text;
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.fontSize = 10;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            button.onClick.AddListener(onClick);
            
            return button;
        }

        protected virtual GameObject CreateListItem(T item, int index)
        {
            var itemObj = new GameObject($"ListItem_{index}");
            itemObj.transform.SetParent(mScrollContent.transform, false);
            
            var itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, GetItemHeight());
            
            var button = itemObj.AddComponent<Button>();
            var buttonImage = itemObj.AddComponent<Image>();
            buttonImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            
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
            if (DebugMenuManager.Instance != null)
            {
                DebugMenuManager.Instance.ShowItemDetails(item);
            }
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
