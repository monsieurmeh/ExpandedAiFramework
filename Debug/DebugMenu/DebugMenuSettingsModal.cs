using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Il2Cpp;
using static ExpandedAiFramework.DebugMenu.Extensions;

namespace ExpandedAiFramework.DebugMenu
{
    [RegisterTypeInIl2Cpp]
    public class DebugMenuSettingsModal : MonoBehaviour
    {
        // UI Components
        private GameObject mModalPanel;
        private GameObject mContentPanel;
        private ScrollRect mScrollRect;
        private GameObject mScrollContent;
        private Button mCloseButton;
        private Button mApplyButton;

        // Settings fields
        private Dictionary<string, InputField> mSettingsFields = new Dictionary<string, InputField>();
        private Dictionary<string, System.Action<string>> mSettingsCallbacks = new Dictionary<string, System.Action<string>>();

        // State
        private bool mIsVisible = false;
        private string mCurrentTabName = "";

        public DebugMenuSettingsModal(IntPtr ptr) : base(ptr) { }

        void Awake()
        {
            CreateUI();
        }

        void CreateUI()
        {
            // Create a separate Canvas for the modal with higher sorting order
            var modalCanvas = new GameObject("SettingsModalCanvas");
            modalCanvas.transform.SetParent(transform, false);
            
            // Ensure the modal canvas RectTransform fills the screen
            var modalCanvasRect = modalCanvas.DefinitelyGetComponent<RectTransform>();
            modalCanvasRect.anchorMin = Vector2.zero;
            modalCanvasRect.anchorMax = Vector2.one;
            modalCanvasRect.offsetMin = Vector2.zero;
            modalCanvasRect.offsetMax = Vector2.zero;
            
            var canvas = modalCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1002; // Higher than main modal (1001)
            
            modalCanvas.AddComponent<GraphicRaycaster>();
            
            var canvasScaler = modalCanvas.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            // Modal background (covers entire screen)
            mModalPanel = new GameObject("SettingsModalPanel");
            mModalPanel.transform.SetParent(modalCanvas.transform, false);
            
            var modalRect = mModalPanel.DefinitelyGetComponent<RectTransform>();
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.offsetMin = Vector2.zero;
            modalRect.offsetMax = Vector2.zero;
            
            var modalImage = mModalPanel.AddComponent<Image>();
            modalImage.color = new Color(0, 0, 0, 0.8f); // Semi-transparent background
            
            // Make background clickable to close
            var modalButton = mModalPanel.AddComponent<Button>();
            modalButton.onClick.AddListener((UnityEngine.Events.UnityAction)Hide);

            // Content panel (bigger modal)
            mContentPanel = new GameObject("SettingsContentPanel");
            mContentPanel.transform.SetParent(mModalPanel.transform, false);
            
            var contentRect = mContentPanel.DefinitelyGetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f); // Center anchor
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(700, 500); // Made bigger: was 600x400, now 700x500
            contentRect.anchoredPosition = Vector2.zero;
            
            var contentImage = mContentPanel.AddComponent<Image>();
            contentImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            
            // Add border outline
            var outline = mContentPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            outline.effectDistance = new Vector2(2, 2);

            // Prevent content panel clicks from closing modal
            var contentButton = mContentPanel.AddComponent<Button>();

            CreateTitleBar();
            CreateScrollArea();
            CreateButtonArea();

            mModalPanel.SetActive(false);
        }

        void CreateTitleBar()
        {
            var titleBar = new GameObject("SettingsTitleBar");
            titleBar.transform.SetParent(mContentPanel.transform, false);
            
            var titleRect = titleBar.DefinitelyGetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.85f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            
            var titleBg = titleBar.AddComponent<Image>();
            titleBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            // Title text
            var titleTextObj = new GameObject("SettingsTitleText");
            titleTextObj.transform.SetParent(titleBar.transform, false);
            
            var titleTextRect = titleTextObj.DefinitelyGetComponent<RectTransform>();
            titleTextRect.anchorMin = new Vector2(0, 0);
            titleTextRect.anchorMax = new Vector2(0.8f, 1);
            titleTextRect.offsetMin = new Vector2(15, 0);
            titleTextRect.offsetMax = new Vector2(0, 0);
            
            var titleText = titleTextObj.AddComponent<Text>();
            titleText.text = "Tab Settings";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 14;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.fontStyle = FontStyle.Bold;
            
            CreateCloseButton(titleBar);
        }

        void CreateScrollArea()
        {
            // Scroll rect for settings
            var scrollObj = new GameObject("SettingsScrollRect");
            scrollObj.transform.SetParent(mContentPanel.transform, false);
            
            var scrollRect = scrollObj.DefinitelyGetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0.19f); // Updated to match new button area height
            scrollRect.anchorMax = new Vector2(1, 0.85f);
            scrollRect.offsetMin = new Vector2(15, 10);
            scrollRect.offsetMax = new Vector2(-15, -10);
            
            mScrollRect = scrollObj.AddComponent<ScrollRect>();
            mScrollRect.horizontal = false;
            mScrollRect.vertical = true;
            mScrollRect.scrollSensitivity = 20f;
            mScrollRect.movementType = ScrollRect.MovementType.Clamped;
            
            // Add background to scroll area
            var scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            
            // Add mask to prevent content bleeding
            var mask = scrollObj.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            
            // Scroll content
            mScrollContent = new GameObject("SettingsScrollContent");
            mScrollContent.transform.SetParent(scrollObj.transform, false);
            
            var contentRect = mScrollContent.DefinitelyGetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);
            
            var contentLayout = mScrollContent.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 10;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.padding = new RectOffset(10, 10, 10, 10);
            contentLayout.childForceExpandWidth = true;
            
            var contentSizeFitter = mScrollContent.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            mScrollRect.content = contentRect;
        }

        void CreateButtonArea()
        {
            var buttonArea = new GameObject("SettingsButtonArea");
            buttonArea.transform.SetParent(mContentPanel.transform, false);
            
            var buttonRect = buttonArea.DefinitelyGetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0, 0);
            buttonRect.anchorMax = new Vector2(1, 0.19f); // Increased from 0.15f to 0.19f (25% taller)
            buttonRect.offsetMin = new Vector2(15, 15);
            buttonRect.offsetMax = new Vector2(-15, -5);
            
            var buttonBg = buttonArea.AddComponent<Image>();
            buttonBg.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            
            var buttonLayout = buttonArea.AddComponent<HorizontalLayoutGroup>();
            buttonLayout.spacing = 10;
            buttonLayout.padding = new RectOffset(10, 10, 10, 10);
            buttonLayout.childControlWidth = true;
            buttonLayout.childControlHeight = true;
            buttonLayout.childForceExpandWidth = true;
            buttonLayout.childForceExpandHeight = true;
            
            // Apply button
            mApplyButton = CreateButton("Apply", buttonArea.transform, new Action(() => OnApplyClicked()));
            
            // Close button
            var closeButton = CreateButton("Close", buttonArea.transform, new Action(() => Hide()));
        }

        void CreateCloseButton(GameObject titleBar)
        {
            var buttonObj = new GameObject("SettingsCloseButton");
            buttonObj.transform.SetParent(titleBar.transform, false);
            
            var buttonRect = buttonObj.DefinitelyGetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.92f, 0.1f);
            buttonRect.anchorMax = new Vector2(0.98f, 0.9f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            
            mCloseButton = buttonObj.AddComponent<Button>();
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.7f, 0.2f, 0.2f, 1f);
            
            // Button hover colors
            var colors = mCloseButton.colors;
            colors.normalColor = new Color(0.7f, 0.2f, 0.2f, 1f);
            colors.highlightedColor = new Color(0.9f, 0.3f, 0.3f, 1f);
            colors.pressedColor = new Color(0.5f, 0.1f, 0.1f, 1f);
            mCloseButton.colors = colors;
            
            // Button text (X)
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            var textRect = textObj.DefinitelyGetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var buttonText = textObj.AddComponent<Text>();
            buttonText.text = "✕";
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.fontSize = 16;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.fontStyle = FontStyle.Bold;
            
            mCloseButton.onClick.AddListener((UnityEngine.Events.UnityAction)Hide);
        }

        Button CreateButton(string text, Transform parent, UnityEngine.Events.UnityAction onClick)
        {
            var buttonObj = new GameObject($"SettingsButton_{text}");
            buttonObj.transform.SetParent(parent, false);
            
            var button = buttonObj.AddComponent<Button>();
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.4f, 0.4f, 0.4f, 1f);
            
            // Button text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            var textRect = textObj.DefinitelyGetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 5);
            textRect.offsetMax = new Vector2(-5, -5);
            
            var buttonText = textObj.AddComponent<Text>();
            buttonText.text = text;
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.fontSize = 12;
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

        InputField CreateSettingField(string labelText, string currentValue, System.Action<string> onValueChanged)
        {
            var fieldContainer = new GameObject($"SettingField_{labelText}");
            fieldContainer.transform.SetParent(mScrollContent.transform, false);
            
            var containerRect = fieldContainer.DefinitelyGetComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(0, 60);
            
            var containerLayout = fieldContainer.AddComponent<HorizontalLayoutGroup>();
            containerLayout.spacing = 10;
            containerLayout.padding = new RectOffset(5, 5, 5, 5);
            containerLayout.childControlWidth = false;
            containerLayout.childControlHeight = true;
            containerLayout.childForceExpandWidth = false;
            containerLayout.childForceExpandHeight = false;
            
            // Label
            var labelObj = new GameObject($"Label_{labelText}");
            labelObj.transform.SetParent(fieldContainer.transform, false);
            
            var labelRect = labelObj.DefinitelyGetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(150, 50);
            
            var layoutElement = labelObj.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 150;
            layoutElement.preferredHeight = 50;
            layoutElement.flexibleWidth = 0;
            
            var label = labelObj.AddComponent<Text>();
            label.text = labelText + ":";
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 11;
            label.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            label.alignment = TextAnchor.MiddleLeft;
            label.fontStyle = FontStyle.Bold;
            
            // Input field
            var inputObj = new GameObject($"Input_{labelText}");
            inputObj.transform.SetParent(fieldContainer.transform, false);
            
            var inputRect = inputObj.DefinitelyGetComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(350, 50);
            
            var inputLayoutElement = inputObj.AddComponent<LayoutElement>();
            inputLayoutElement.preferredWidth = 350;
            inputLayoutElement.preferredHeight = 50;
            inputLayoutElement.flexibleWidth = 1;
            
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
            textRect.offsetMin = new Vector2(8, 2);
            textRect.offsetMax = new Vector2(-8, -2);
            
            inputField.textComponent = inputText;
            inputField.text = currentValue;
            
            if (onValueChanged != null)
            {
                inputField.onValueChanged.AddListener((UnityEngine.Events.UnityAction<string>)onValueChanged);
            }
            
            return inputField;
        }

        public void ShowSettings(string tabName, Dictionary<string, string> settings, Dictionary<string, System.Action<string>> callbacks)
        {
            mCurrentTabName = tabName;
            mSettingsFields.Clear();
            mSettingsCallbacks.Clear();
            
            // Clear existing settings
            foreach (Transform child in mScrollContent.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Update title
            var titleText = mContentPanel.transform.Find("SettingsTitleBar/SettingsTitleText")?.GetComponent<Text>();
            if (titleText != null)
            {
                titleText.text = $"{tabName} Settings";
            }
            
            // Create setting fields
            foreach (var setting in settings)
            {
                var callback = callbacks.ContainsKey(setting.Key) ? callbacks[setting.Key] : null;
                var inputField = CreateSettingField(setting.Key, setting.Value, callback);
                mSettingsFields[setting.Key] = inputField;
                if (callback != null)
                {
                    mSettingsCallbacks[setting.Key] = callback;
                }
            }
            
            Show();
        }

        void OnApplyClicked()
        {
            foreach (var field in mSettingsFields)
            {
                if (mSettingsCallbacks.ContainsKey(field.Key))
                {
                    mSettingsCallbacks[field.Key]?.Invoke(field.Value.text);
                }
            }
            
            LogDebug($"Applied settings for {mCurrentTabName}", LogCategoryFlags.DebugMenu);
            Hide();
        }

        public void Show()
        {
            mIsVisible = true;
            
            if (mModalPanel == null)
            {
                LogWarning("mModalPanel is null!");
                return;
            }
            
            // Ensure modal is on top when shown
            transform.SetAsLastSibling();
            
            // Also ensure the modal canvas is on top
            var modalCanvas = mModalPanel.transform.parent;
            if (modalCanvas != null)
            {
                modalCanvas.SetAsLastSibling();
            }
            
            mModalPanel.SetActive(true);
        }

        public void Hide()
        {
            mIsVisible = false;
            if (mModalPanel != null)
            {
                mModalPanel.SetActive(false);
            }
        }

        void Update()
        {
            // Close modal with Escape key
            if (mIsVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                Hide();
            }
        }
    }
}
