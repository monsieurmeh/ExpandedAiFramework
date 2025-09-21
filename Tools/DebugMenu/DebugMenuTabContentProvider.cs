using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Il2Cpp;
using static ExpandedAiFramework.DebugMenu.Extensions;
using ExpandedAiFramework.UI;

namespace ExpandedAiFramework.DebugMenu
{
    public abstract class DebugMenuTabContentProvider<T> : IDebugMenuTabContentProvider where T : ISerializedData
    {
        // UI Components
        protected GameObject mRootPanel;
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
            // Create root panel using new factory system with vertical layout
            var rootPanelOptions = PanelOptions.FlexiblePanel(1, 1, PanelLayoutType.Vertical);
            rootPanelOptions.Name = $"{GetType().Name}_RootPanel";
            rootPanelOptions.LayoutGroupOptions = LayoutGroupOptions.Vertical(0, new RectOffset(0, 0, 0, 0));
            rootPanelOptions.HasBackground = false;
            
            mRootPanel = PanelFactory.CreatePanel(parentContentArea.transform, rootPanelOptions);

            CreateListPanel();
            CreateStatusText();

            mRootPanel.SetActive(false);
        }

        protected virtual void PopulateUnifiedButtonBar()
        {
            // Get reference to the shared unified button bar
            var buttonBar = DebugMenuManager.Instance?.UnifiedButtonBar;
            if (buttonBar == null)
            {
                LogError("Unified button bar not found in DebugMenuManager");
                return;
            }

            // Clear existing children
            ClearButtonBar(buttonBar);

            // Create filter controls first (always present)
            CreateFilterControls(buttonBar);
            
            // Create global action buttons (always present)
            CreateGlobalActionButtons(buttonBar);
            
            // Create tab-specific buttons (will be added by derived classes)
            CreateTabSpecificButtons(buttonBar);
        }

        protected virtual void ClearButtonBar(GameObject buttonBar)
        {
            // Clear all children from the button bar
            for (int i = buttonBar.transform.childCount - 1; i >= 0; i--)
            {
                var child = buttonBar.transform.GetChild(i);
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        protected virtual void CreateFilterControls(GameObject buttonBar)
        {
            // Scene filter group
            var sceneGroup = CreateButtonGroup("Scene Filter", 180, buttonBar);
            CreateLabel("Scene:", sceneGroup.transform, 50);
            mSceneFilterInput = CreateInputField("", sceneGroup.transform, 120, OnSceneFilterChanged);
            
            // Name filter group  
            var nameGroup = CreateButtonGroup("Name Filter", 180, buttonBar);
            CreateLabel("Name:", nameGroup.transform, 50);
            mNameFilterInput = CreateInputField("", nameGroup.transform, 120, OnNameFilterChanged);
            
            // Filter action buttons
            var filterButtonGroup = CreateButtonGroup("Filter Actions", 140, buttonBar);
            mApplyFilterButton = CreateButton("Apply", filterButtonGroup.transform, OnApplyFilterClicked);
            mClearFilterButton = CreateButton("Clear", filterButtonGroup.transform, OnClearFilterClicked);
        }

        protected virtual void CreateGlobalActionButtons(GameObject buttonBar)
        {
            // Global action buttons (Save, Load, Refresh)
            var globalGroup = CreateButtonGroup("Global Actions", 200, buttonBar);
            var saveButton = CreateButton("Save", globalGroup.transform, OnSaveClicked);
            var loadButton = CreateButton("Load", globalGroup.transform, OnLoadClicked);
            mRefreshButton = CreateButton("Refresh", globalGroup.transform, OnRefreshClicked);
        }

        protected virtual void CreateTabSpecificButtons(GameObject buttonBar)
        {
            // Always add settings button at the end
            CreateSettingsButton(buttonBar);
        }

        protected virtual void CreateSettingsButton(GameObject buttonBar)
        {
            var settingsGroup = CreateButtonGroup("Settings", 60, buttonBar);
            var settingsButton = CreateButton("+", settingsGroup.transform, OnSettingsClicked);
        }

        protected virtual void OnSettingsClicked()
        {
            var settings = GetTabSettings();
            var callbacks = GetTabSettingsCallbacks();
            
            if (DebugMenuManager.Instance != null)
            {
                DebugMenuManager.Instance.ShowTabSettings(GetTabDisplayName(), settings, callbacks);
            }
        }

        protected abstract Dictionary<string, string> GetTabSettings();
        protected abstract Dictionary<string, System.Action<string>> GetTabSettingsCallbacks();

        protected virtual GameObject CreateButtonGroup(string groupName, float width, GameObject buttonBar)
        {
            var groupOptions = PanelOptions.Default(PanelLayoutType.Horizontal);
            groupOptions.Name = groupName;
            groupOptions.LayoutElementOptions = LayoutElementOptions.Fixed(width, 40);
            groupOptions.LayoutGroupOptions = LayoutGroupOptions.Horizontal(3, new RectOffset(3, 3, 5, 5));
            groupOptions.LayoutGroupOptions.childControlWidth = true;
            groupOptions.LayoutGroupOptions.childControlHeight = true;
            groupOptions.LayoutGroupOptions.childForceExpandWidth = false;
            groupOptions.LayoutGroupOptions.childForceExpandHeight = false;
            groupOptions.LayoutGroupOptions.childAlignment = TextAnchor.MiddleCenter;
            groupOptions.HasBackground = false;
            
            var buttonGroup = PanelFactory.CreatePanel(buttonBar.transform, groupOptions);
            
            // Add a Mask component to ensure children don't extend beyond bounds
            var mask = buttonGroup.AddComponent<Mask>();
            mask.showMaskGraphic = false; // Don't show the mask graphic itself
            
            return buttonGroup;
        }

        // Legacy method for backward compatibility
        protected virtual GameObject CreateFilterGroup(string groupName)
        {
            var buttonBar = DebugMenuManager.Instance?.UnifiedButtonBar;
            if (buttonBar == null) return null;
            
            if (groupName == "Actions")
                return CreateButtonGroup(groupName, 180, buttonBar);
            else if (groupName == "Wildlife Mode")
                return CreateButtonGroup(groupName, 140, buttonBar);
            else
                return CreateButtonGroup(groupName, 120, buttonBar);
        }


        protected virtual void CreateListPanel()
        {
            // Create list panel using new factory system with scroll view
            var scrollOptions = ScrollViewOptions.Vertical();
            scrollOptions.name = "ListPanel";
            scrollOptions.layoutElement = LayoutElementOptions.Flexible(1, 1); // Fill remaining space between button bar and status
            scrollOptions.backgroundImage = new ImageOptions { Color = new Color(0.08f, 0.08f, 0.08f, 0.95f) };
            scrollOptions.scrollSensitivity = 20f;
            scrollOptions.contentLayout = LayoutGroupOptions.Vertical(1, new RectOffset(8, 8, 8, 8));
            // These guys are special, because they are NOT using layout elements.
            scrollOptions.contentLayout.childControlWidth = true;
            scrollOptions.contentLayout.childControlHeight = false;
            scrollOptions.contentLayout.childForceExpandWidth = true;
            scrollOptions.contentLayout.childForceExpandHeight = false;
            
            mScrollRect = ScrollViewFactory.CreateScrollView(mRootPanel.transform, scrollOptions);
            
            // Get references for compatibility
            mListPanel = mScrollRect.gameObject;
            mScrollContent = mScrollRect.content.gameObject;
        }

        protected virtual void CreateStatusText()
        {
            // Create status text using new factory system
            var textOptions = TextOptions.Default("Loading...", 12);
            textOptions.color = Color.white;
            textOptions.alignment = TextAnchor.MiddleLeft;
            
            var textFieldOptions = TextFieldOptions.Default(textOptions);
            textFieldOptions.layoutElement = LayoutElementOptions.Fixed(0, 30); // Fixed height for status bar
            
            mStatusText = TextFactory.CreateTextField(mRootPanel.transform, textFieldOptions);
            mStatusText.name = "StatusText";
        }

        protected virtual Text CreateLabel(string text, Transform parent, float width)
        {
            var textOptions = TextOptions.Default(text, 11);
            textOptions.color = new Color(0.7f, 0.7f, 0.7f, 1f); // TextSecondary equivalent
            textOptions.alignment = TextAnchor.MiddleLeft;
            textOptions.fontStyle = FontStyle.Bold;
            
            var textFieldOptions = TextFieldOptions.Label(textOptions, width);
            textFieldOptions.layoutElement = LayoutElementOptions.Fixed(width, 22);
            
            var label = TextFactory.CreateTextField(parent, textFieldOptions);
            label.name = $"Label_{text}";
            return label;
        }

        protected virtual InputField CreateInputField(string placeholder, Transform parent, float width, Action<string> onValueChanged)
        {
            return CreateInputFieldInternal(placeholder, parent, width, (UnityEngine.Events.UnityAction<string>)onValueChanged);
        }


        protected virtual InputField CreateInputFieldInternal(string placeholder, Transform parent, float width, UnityEngine.Events.UnityAction<string> onValueChanged)
        {
            var inputOptions = InputFieldOptions.Default();
            
            // Configure text field options
            var textOptions = TextOptions.Default(placeholder, 10);
            textOptions.color = Color.white;
            inputOptions.textFieldOptions = TextFieldOptions.Default(textOptions);
            inputOptions.textFieldOptions.layoutElement = LayoutElementOptions.Fixed(width, 30);
            
            // Configure background
            inputOptions.backgroundImageOptions = new ImageOptions { Color = new Color(0.3f, 0.3f, 0.3f, 1f) }; // MediumBackground equivalent
            
            var inputField = InputFieldFactory.CreateInputField(parent, inputOptions, onValueChanged != null ? (value) => onValueChanged.Invoke(value) : null);
            inputField.name = $"InputField_{placeholder}";
            
            // Set placeholder text if provided
            if (!string.IsNullOrEmpty(placeholder))
            {
                inputField.placeholder = inputField.textComponent;
                inputField.textComponent.text = "";
                var placeholderText = inputField.placeholder as Text;
                if (placeholderText != null)
                {
                    placeholderText.text = placeholder;
                    placeholderText.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
                }
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
            var buttonOptions = ButtonOptions.TextButton(text, 55, 30); // Slightly smaller width, taller for better fit
            buttonOptions.textOptions = TextOptions.Default(text, 9);
            buttonOptions.layoutElement = LayoutElementOptions.Fixed(55, 30);
            
            var button = ButtonFactory.CreateButton(parent, buttonOptions, onClick != null ? () => onClick.Invoke() : null);
            button.name = $"Button_{text}";
            return button;
        }

        protected virtual GameObject CreateListItem(T item, int index)
        {
            // Create simple horizontal list item: Icon + Display Name + Show/Hide toggle
            return DebugMenuListViewItem.CreateListItem(item, index, mScrollContent.transform, GetItemHeight(),
                null, OnItemClicked, null); // Using new simplified format
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
                    DebugMenuListViewItem.SetItemSelected(mListItems[i], i == mSelectedIndex, i);
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

        // Removed CreateEntityActionButtons, OnGoToClicked, and OnDeleteClicked methods
        // Per-entity buttons have been removed as requested - actions now handled through modals

        // Public interface
        public virtual void Show()
        {
            mIsVisible = true;
            if (mRootPanel != null)
            {
                mRootPanel.SetActive(true);
            }
            
            // Populate the shared unified button bar for this tab
            PopulateUnifiedButtonBar();
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
        protected abstract string GetItemName(T item);
        protected abstract string GetTabDisplayName();
        protected abstract float GetItemHeight();
        protected virtual bool PassesCustomFilter(T item) => true;

        // SubDataManager integration - to be set by derived classes
        protected abstract ISubDataManager GetSubDataManager();

        // IDebugMenuEntityModalProvider implementation
        public virtual void PopulateEntityModal<TEntity>(TEntity entity, GameObject modalContent, System.Action<string, object> onValueChanged) where TEntity : ISerializedData
        {
            if (entity is T typedEntity)
            {
                PopulateEntityModalForType(typedEntity, modalContent, onValueChanged);
            }
            else
            {
                PopulateGenericEntityModal(entity, modalContent, onValueChanged);
            }
        }

        protected virtual void PopulateEntityModalForType(T entity, GameObject modalContent, System.Action<string, object> onValueChanged)
        {
            // Default implementation - create basic fields for common ISerializedData properties
            PopulateGenericEntityModal(entity, modalContent, onValueChanged);
        }

        protected virtual void PopulateGenericEntityModal<TEntity>(TEntity entity, GameObject modalContent, System.Action<string, object> onValueChanged) where TEntity : ISerializedData
        {
            // Create fields for basic ISerializedData properties with automatic read-only detection
            var entityType = entity.GetType();
            
            var guidField = FormFieldFactory.CreateTextField("GUID", entity.Guid.ToString(), modalContent.transform, null, true);
            var sceneField = FormFieldFactory.CreateTextField("Scene", entity.Scene, modalContent.transform, onValueChanged, false);
            var dataLocationField = FormFieldFactory.CreateTextField("Data Location", entity.DataLocation, modalContent.transform, onValueChanged, false);
            var displayNameField = FormFieldFactory.CreateTextField("Display Name", entity.DisplayName, modalContent.transform, null, false);

            // Register the fields with the modal
            var entityModal = modalContent.GetComponentInParent<DebugMenuEntityModal>();
            if (entityModal != null)
            {
                entityModal.RegisterFormField("GUID", guidField);
                entityModal.RegisterFormField("Scene", sceneField);
                entityModal.RegisterFormField("Data Location", dataLocationField);
                entityModal.RegisterFormField("Display Name", displayNameField);
            }
        }

        public virtual bool ApplyEntityChanges<TEntity>(TEntity entity, Dictionary<string, object> fieldValues) where TEntity : ISerializedData
        {
            if (entity is T typedEntity)
            {
                return ApplyEntityChangesForType(typedEntity, fieldValues);
            }
            else
            {
                return ApplyGenericEntityChanges(entity, fieldValues);
            }
        }

        protected virtual bool ApplyEntityChangesForType(T entity, Dictionary<string, object> fieldValues)
        {
            // Default implementation - apply basic fields
            return ApplyGenericEntityChanges(entity, fieldValues);
        }

        protected virtual bool ApplyGenericEntityChanges<TEntity>(TEntity entity, Dictionary<string, object> fieldValues) where TEntity : ISerializedData
        {
            try
            {
                // Apply basic ISerializedData changes
                if (fieldValues.TryGetValue("Scene", out var sceneValue) && sceneValue is string sceneString)
                {
                    // Scene is typically read-only in most implementations
                    LogDebug("Scene changes are typically not supported");
                }

                if (fieldValues.TryGetValue("Data Location", out var dataLocationValue) && dataLocationValue is string dataLocationString)
                {
                    entity.DataLocation = dataLocationString;
                }

                LogDebug($"Applied basic entity changes to {entity.DisplayName}");
                return true;
            }
            catch (Exception e)
            {
                LogError($"Failed to apply entity changes: {e.Message}");
                return false;
            }
        }

        public virtual string GetEntityModalTitle<TEntity>(TEntity entity) where TEntity : ISerializedData
        {
            if (entity is T typedEntity)
            {
                return GetEntityModalTitleForType(typedEntity);
            }
            else
            {
                return $"{entity.GetType().Name} Details - {entity.DisplayName}";
            }
        }

        protected virtual string GetEntityModalTitleForType(T entity)
        {
            return $"{GetTabDisplayName()} - {GetItemName(entity)}";
        }
    }
}
