using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Il2Cpp;
using static ExpandedAiFramework.DebugMenu.Extensions;
using ExpandedAiFramework.UI;

namespace ExpandedAiFramework.DebugMenu
{
    [RegisterTypeInIl2Cpp]
    public class DebugMenuEntityModal : MonoBehaviour
    {
        // UI Components
        private GameObject mModalPanel;
        private GameObject mContentPanel;
        private ScrollRect mScrollRect;
        private GameObject mScrollContent;
        private Text mTitleText;
        private Button mCloseButton;
        private Button mApplyButton;
        private Button mResetButton;

        // Form management
        private Dictionary<string, IFormField> mFormFields = new Dictionary<string, IFormField>();
        private Dictionary<string, object> mOriginalValues = new Dictionary<string, object>();
        private IDebugMenuEntityModalProvider mCurrentProvider;
        private ISerializedData mCurrentEntity;

        // State
        private bool mIsVisible = false;
        private bool mHasChanges = false;

        public DebugMenuEntityModal(IntPtr ptr) : base(ptr) { }

        void Awake()
        {
            CreateUI();
        }

        void CreateUI()
        {
            var modalCanvas = CreateModalCanvas();
            
            var modalPanelOptions = PanelOptions.RootContainerPanel(PanelLayoutType.None);
            modalPanelOptions.Name = "EntityModalPanel";
            modalPanelOptions.ImageOptions = new ImageOptions { Color = new Color(0, 0, 0, 0.85f) };
            modalPanelOptions.HasBackground = true;
            
            mModalPanel = PanelFactory.CreatePanel(modalCanvas.transform, modalPanelOptions);
            
            var modalButton = mModalPanel.AddComponent<Button>();
            modalButton.onClick.AddListener(new Action(() => OnCloseRequested()));

            CreateContentPanel();
            
            mModalPanel.SetActive(false);
        }

        GameObject CreateModalCanvas()
        {
            var modalCanvas = new GameObject("EntityModalCanvas");
            modalCanvas.transform.SetParent(transform, false);
            
            var canvas = modalCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1001; // Higher than main menu (1000)
            
            modalCanvas.AddComponent<GraphicRaycaster>();
            
            var canvasScaler = modalCanvas.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            return modalCanvas;
        }

        void CreateContentPanel()
        {
            var contentPanelOptions = PanelOptions.Default(PanelLayoutType.Vertical);
            contentPanelOptions.Name = "EntityContentPanel";
            contentPanelOptions.LayoutElementOptions = LayoutElementOptions.Fixed(950, 750);
            contentPanelOptions.LayoutGroupOptions = LayoutGroupOptions.Vertical(0, new RectOffset(0, 0, 0, 0));
            contentPanelOptions.ImageOptions = new ImageOptions { Color = new Color(0.12f, 0.12f, 0.15f, 1f) };
            contentPanelOptions.HasOutline = true;
            contentPanelOptions.OutlineColor = new Color(0.3f, 0.45f, 0.6f, 1f);
            contentPanelOptions.OutlineSize = new Vector2(3, 3);
            
            mContentPanel = PanelFactory.CreatePanel(mModalPanel.transform, contentPanelOptions);
            
            var contentRect = mContentPanel.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;

            var contentButton = mContentPanel.AddComponent<Button>();

            CreateTitleBar();
            CreateScrollArea();
            CreateButtonArea();
        }

        void CreateTitleBar()
        {
            var titleBarOptions = PanelOptions.Default(PanelLayoutType.Horizontal);
            titleBarOptions.Name = "EntityTitleBar";
            titleBarOptions.LayoutElementOptions = LayoutElementOptions.PreferredSize(-1, 60, 1, 0);
            titleBarOptions.LayoutGroupOptions = LayoutGroupOptions.Horizontal(0, new RectOffset(20, 10, 10, 10));
            titleBarOptions.LayoutGroupOptions.childControlWidth = true; 
            titleBarOptions.LayoutGroupOptions.childControlHeight = true; 
            titleBarOptions.LayoutGroupOptions.childForceExpandWidth = false;
            titleBarOptions.LayoutGroupOptions.childForceExpandHeight = false;
            titleBarOptions.LayoutGroupOptions.childAlignment = TextAnchor.MiddleLeft;
            titleBarOptions.ImageOptions = new ImageOptions { Color = new Color(0.15f, 0.2f, 0.28f, 1f) };
            
            var titleBar = PanelFactory.CreatePanel(mContentPanel.transform, titleBarOptions);
            
            var titleTextOptions = TextOptions.Default("Entity Details", 16);
            titleTextOptions.color = new Color(0.9f, 0.95f, 1f, 1f);
            titleTextOptions.alignment = TextAnchor.MiddleLeft;
            titleTextOptions.fontStyle = FontStyle.Bold;
            
            var titleFieldOptions = TextFieldOptions.Default(titleTextOptions);
            titleFieldOptions.layoutElement = LayoutElementOptions.Flexible(1, 1);
            
            mTitleText = TextFactory.CreateTextField(titleBar.transform, titleFieldOptions);
            mTitleText.name = "EntityTitleText";
            
            CreateCloseButton(titleBar);
        }

        void CreateScrollArea()
        {
            var scrollOptions = ScrollViewOptions.Vertical();
            scrollOptions.name = "EntityScrollRect";
            scrollOptions.layoutElement = LayoutElementOptions.Flexible(1, 1);
            scrollOptions.backgroundImage = new ImageOptions { Color = new Color(0.1f, 0.1f, 0.13f, 1f) };
            scrollOptions.scrollSensitivity = 20f;
            scrollOptions.scrollbarWidth = 2f;
            scrollOptions.scrollbarBackgroundColor = new Color(0.08f, 0.1f, 0.12f, 0.8f);
            scrollOptions.scrollbarHandleColor = new Color(0.3f, 0.45f, 0.6f, 0.8f);
            scrollOptions.contentLayout = LayoutGroupOptions.Vertical(15, new RectOffset(25, 25, 20, 20));
            scrollOptions.contentLayout.childControlWidth = true;
            scrollOptions.contentLayout.childControlHeight = false;
            scrollOptions.contentLayout.childForceExpandWidth = true;
            scrollOptions.contentLayout.childForceExpandHeight = false;
            
            mScrollRect = ScrollViewFactory.CreateScrollView(mContentPanel.transform, scrollOptions);
            
            mScrollContent = mScrollRect.content.gameObject;
        }

        void CreateButtonArea()
        {
            var buttonAreaOptions = PanelOptions.Default(PanelLayoutType.Horizontal);
            buttonAreaOptions.Name = "EntityButtonArea";
            buttonAreaOptions.LayoutElementOptions = LayoutElementOptions.Fixed(0, 70);
            buttonAreaOptions.LayoutGroupOptions = LayoutGroupOptions.Horizontal(15, new RectOffset(25, 25, 15, 15));
            buttonAreaOptions.LayoutGroupOptions.childControlWidth = true;
            buttonAreaOptions.LayoutGroupOptions.childControlHeight = true;
            buttonAreaOptions.LayoutGroupOptions.childForceExpandWidth = true;
            buttonAreaOptions.LayoutGroupOptions.childForceExpandHeight = true;
            buttonAreaOptions.ImageOptions = new ImageOptions { Color = new Color(0.08f, 0.1f, 0.12f, 1f) };
            
            var buttonArea = PanelFactory.CreatePanel(mContentPanel.transform, buttonAreaOptions);
            
            var applyButtonOptions = ButtonOptions.TextButton("Apply Changes", 140, 45);
            var applyTextOptions = TextOptions.Default("Apply Changes", 13);
            applyTextOptions.color = Color.white;
            applyTextOptions.fontStyle = FontStyle.Bold;
            applyButtonOptions.textOptions = applyTextOptions;
            applyButtonOptions.backgroundOptions = new ImageOptions { Color = new Color(0.2f, 0.6f, 0.3f, 1f) };
            var applyColorBlock = applyButtonOptions.colorBlock ?? new ColorBlock();
            applyColorBlock.normalColor = new Color(0.2f, 0.6f, 0.3f, 1f);
            applyColorBlock.highlightedColor = new Color(0.25f, 0.7f, 0.35f, 1f);
            applyColorBlock.pressedColor = new Color(0.15f, 0.5f, 0.25f, 1f);
            applyButtonOptions.colorBlock = applyColorBlock;
            
            mApplyButton = ButtonFactory.CreateButton(buttonArea.transform, applyButtonOptions, () => OnApplyClicked());
            
            var resetButtonOptions = ButtonOptions.TextButton("Reset", 110, 45);
            var resetTextOptions = TextOptions.Default("Reset", 13);
            resetTextOptions.color = Color.white;
            resetTextOptions.fontStyle = FontStyle.Bold;
            resetButtonOptions.textOptions = resetTextOptions;
            resetButtonOptions.backgroundOptions = new ImageOptions { Color = new Color(0.9f, 0.6f, 0.1f, 1f) };
            var resetColorBlock = resetButtonOptions.colorBlock ?? new ColorBlock();
            resetColorBlock.normalColor = new Color(0.9f, 0.6f, 0.1f, 1f);
            resetColorBlock.highlightedColor = new Color(1f, 0.7f, 0.2f, 1f);
            resetColorBlock.pressedColor = new Color(0.8f, 0.5f, 0.05f, 1f);
            resetButtonOptions.colorBlock = resetColorBlock;
            
            mResetButton = ButtonFactory.CreateButton(buttonArea.transform, resetButtonOptions, () => OnResetClicked());
            
            var closeButtonOptions = ButtonOptions.TextButton("Close", 100, 45);
            var closeTextOptions = TextOptions.Default("Close", 13);
            closeTextOptions.color = Color.white;
            closeTextOptions.fontStyle = FontStyle.Bold;
            closeButtonOptions.textOptions = closeTextOptions;
            closeButtonOptions.backgroundOptions = new ImageOptions { Color = new Color(0.7f, 0.25f, 0.25f, 1f) };
            var closeColorBlock = closeButtonOptions.colorBlock ?? new ColorBlock();
            closeColorBlock.normalColor = new Color(0.7f, 0.25f, 0.25f, 1f);
            closeColorBlock.highlightedColor = new Color(0.8f, 0.3f, 0.3f, 1f);
            closeColorBlock.pressedColor = new Color(0.6f, 0.2f, 0.2f, 1f);
            closeButtonOptions.colorBlock = closeColorBlock;
            
            var closeButton = ButtonFactory.CreateButton(buttonArea.transform, closeButtonOptions, () => OnCloseRequested());
        }

        void CreateCloseButton(GameObject titleBar)
        {
            var closeButtonOptions = ButtonOptions.TextButton("✕", 45, 45);
            var titleCloseTextOptions = TextOptions.Default("✕", 18);
            titleCloseTextOptions.color = Color.white;
            titleCloseTextOptions.fontStyle = FontStyle.Bold;
            closeButtonOptions.textOptions = titleCloseTextOptions;
            closeButtonOptions.backgroundOptions = new ImageOptions { Color = new Color(0.7f, 0.25f, 0.25f, 1f) };
            closeButtonOptions.layoutElement = LayoutElementOptions.Fixed(45, 45);
            var closeColorBlock = closeButtonOptions.colorBlock ?? new ColorBlock();
            closeColorBlock.normalColor = new Color(0.7f, 0.25f, 0.25f, 1f);
            closeColorBlock.highlightedColor = new Color(0.8f, 0.3f, 0.3f, 1f);
            closeColorBlock.pressedColor = new Color(0.6f, 0.2f, 0.2f, 1f);
            closeButtonOptions.colorBlock = closeColorBlock;
            
            mCloseButton = ButtonFactory.CreateButton(titleBar.transform, closeButtonOptions, () => OnCloseRequested());
            mCloseButton.name = "EntityCloseButton";
        }


        public void ShowEntityDetails<T>(T entity, IDebugMenuEntityModalProvider provider) where T : ISerializedData
        {
            if (entity == null || provider == null)
            {
                LogError("Cannot show entity details - entity or provider is null");
                return;
            }

            mCurrentEntity = entity;
            mCurrentProvider = provider;
            mHasChanges = false;

            // Update title
            mTitleText.text = provider.GetEntityModalTitle(entity);

            // Clear existing form fields
            ClearFormFields();

            // Let the provider populate the modal with custom form fields
            provider.PopulateEntityModal(entity, mScrollContent, OnFieldValueChanged);

            // Store original values for reset functionality
            StoreOriginalValues();

            Show();
        }

        void ClearFormFields()
        {
            // Clear existing form fields - IL2CPP compatible way
            var transform = mScrollContent.transform;
            int childCount = transform.childCount;
            
            // Destroy children in reverse order to avoid index issues
            for (int i = childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child != null && child.gameObject != null)
                {
                    Destroy(child.gameObject);
                }
            }
            
            mFormFields.Clear();
            mOriginalValues.Clear();
        }

        void StoreOriginalValues()
        {
            mOriginalValues.Clear();
            foreach (var field in mFormFields)
            {
                mOriginalValues[field.Key] = field.Value.GetValue();
            }
        }

        void OnFieldValueChanged(string fieldName, object newValue)
        {
            mHasChanges = true;
            UpdateButtonStates();
        }

        void UpdateButtonStates()
        {
            if (mApplyButton != null)
            {
                var applyImage = mApplyButton.GetComponent<Image>();
                if (mHasChanges)
                {
                    applyImage.color = new Color(0.2f, 0.7f, 0.2f, 1f); // Green when changes exist
                }
                else
                {
                    applyImage.color = new Color(0.4f, 0.4f, 0.4f, 1f); // Gray when no changes
                }
            }
        }

        void OnApplyClicked()
        {
            if (!mHasChanges || mCurrentEntity == null || mCurrentProvider == null)
            {
                LogDebug("No changes to apply or missing entity/provider");
                return;
            }

            // Collect current field values
            var fieldValues = new Dictionary<string, object>();
            foreach (var field in mFormFields)
            {
                fieldValues[field.Key] = field.Value.GetValue();
            }

            // Apply changes through the provider
            bool success = mCurrentProvider.ApplyEntityChanges(mCurrentEntity, fieldValues);
            
            if (success)
            {
                mHasChanges = false;
                UpdateButtonStates();
                StoreOriginalValues(); // Update original values after successful apply
                LogDebug($"Successfully applied changes to {mCurrentEntity.DisplayName}", LogCategoryFlags.DebugMenu);
            }
            else
            {
                LogError($"Failed to apply changes to {mCurrentEntity.DisplayName}");
            }
        }

        void OnResetClicked()
        {
            if (mOriginalValues.Count == 0)
            {
                LogDebug("No original values to reset to");
                return;
            }

            // Reset all fields to their original values
            foreach (var originalValue in mOriginalValues)
            {
                if (mFormFields.TryGetValue(originalValue.Key, out var field))
                {
                    field.SetValue(originalValue.Value);
                }
            }

            mHasChanges = false;
            UpdateButtonStates();
            LogDebug("Reset all fields to original values");
        }

        void OnCloseRequested()
        {
            if (mHasChanges)
            {
                // TODO: Show confirmation dialog for unsaved changes
                LogDebug("Closing modal with unsaved changes");
            }
            Hide();
        }

        public void RegisterFormField(string fieldName, IFormField formField)
        {
            mFormFields[fieldName] = formField;
        }

        public void Show()
        {
            mIsVisible = true;
            
            if (mModalPanel == null)
            {
                LogError("mModalPanel is null!");
                return;
            }
            
            // Ensure modal is on top when shown
            transform.SetAsLastSibling();
            
            mModalPanel.SetActive(true);
            UpdateButtonStates();
        }

        public void Hide()
        {
            mIsVisible = false;
            if (mModalPanel != null)
            {
                mModalPanel.SetActive(false);
            }
            
            // Clear references when hiding
            mCurrentEntity = null;
            mCurrentProvider = null;
            mHasChanges = false;
        }

        void Update()
        {
            // Close modal with Escape key
            if (mIsVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                OnCloseRequested();
            }
        }
    }

    /// <summary>
    /// Interface for form fields in the entity modal
    /// </summary>
    public interface IFormField
    {
        object GetValue();
        void SetValue(object value);
        string FieldName { get; }
        bool IsReadOnly { get; set; }
        void SetReadOnlyState(bool readOnly);
    }
}
