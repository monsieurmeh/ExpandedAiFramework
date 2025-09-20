using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Il2Cpp;
using static ExpandedAiFramework.DebugMenu.Extensions;

namespace ExpandedAiFramework.DebugMenu
{
    [RegisterTypeInIl2Cpp]
    public class DebugMenuManager : MonoBehaviour
    {
        // UI Components
        private GameObject mCanvasObject;
        private Canvas mCanvas;
        private GameObject mMainPanel;
        private GameObject mTabButtonsPanel;
        private GameObject mContentArea;
        private DebugMenuModal mModal;

        // Tab management
        private Dictionary<string, IDebugMenuTabContentProvider> mTabProviders;
        private Dictionary<string, Button> mTabButtons;
        private IDebugMenuTabContentProvider mCurrentTabProvider;
        private string mCurrentTabName;

        // State
        private bool mMenuVisible = false;

        // Singleton
        private static DebugMenuManager sInstance;
        public static DebugMenuManager Instance => sInstance;

        public DebugMenuManager(IntPtr ptr) : base(ptr) { }

        void Awake()
        {
            if (sInstance != null && sInstance != this)
            {
                Destroy(gameObject);
                return;
            }
            sInstance = this;
            DontDestroyOnLoad(gameObject);

            mTabProviders = new Dictionary<string, IDebugMenuTabContentProvider>();
            mTabButtons = new Dictionary<string, Button>();

            CreateUI();
            RegisterTabProviders();
        }

        void Update()
        {
            // F2 key binding to toggle debug menu
            if (Input.GetKeyDown(KeyCode.F2))
            {
                ToggleMenu();
            }
        }

        void OnDestroy()
        {
            if (sInstance == this)
            {
                sInstance = null;
            }
        }

        void CreateUI()
        {
            try
            {
                // Create Canvas
                mCanvasObject = new GameObject("EAFDebugMenuCanvas");
                mCanvasObject.transform.SetParent(transform);
                
                mCanvas = mCanvasObject.AddComponent<Canvas>();
                mCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                mCanvas.sortingOrder = 1000;
                
                var canvasScaler = mCanvasObject.AddComponent<CanvasScaler>();
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1920, 1080);
                
                mCanvasObject.AddComponent<GraphicRaycaster>();

                CreateMainPanel();
                CreateTabButtonsPanel();
                CreateContentArea();
                CreateModal();
                
                // Initially hide the menu
                mCanvasObject.SetActive(false);
                
                LogDebug("Debug menu UI created successfully");
            }
            catch (Exception e)
            {
                LogError($"Error creating debug menu UI: {e}");
            }
        }

        void CreateMainPanel()
        {
            mMainPanel = new GameObject("MainPanel");
            mMainPanel.transform.SetParent(mCanvasObject.transform, false);
            
            // Add RectTransform first
            var panelRect = mMainPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.05f, 0.05f); // Leave 25px margin (roughly 5% on 1920x1080)
            panelRect.anchorMax = new Vector2(0.95f, 0.95f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            var panelImage = mMainPanel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        }

        void CreateTabButtonsPanel()
        {
            mTabButtonsPanel = new GameObject("TabButtonsPanel");
            mTabButtonsPanel.transform.SetParent(mMainPanel.transform, false);
            
            var tabPanelRect = mTabButtonsPanel.AddComponent<RectTransform>();
            tabPanelRect.anchorMin = new Vector2(0, 0.9f);
            tabPanelRect.anchorMax = new Vector2(1, 1);
            tabPanelRect.offsetMin = new Vector2(10, -10);
            tabPanelRect.offsetMax = new Vector2(-10, -10);
            
            var tabLayout = mTabButtonsPanel.AddComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 5;
            tabLayout.childControlWidth = true;
            tabLayout.childControlHeight = true;
        }

        void CreateContentArea()
        {
            mContentArea = new GameObject("ContentArea");
            mContentArea.transform.SetParent(mMainPanel.transform, false);
            
            // Add RectTransform first
            var contentRect = mContentArea.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 0.9f);
            contentRect.offsetMin = new Vector2(10, 10);
            contentRect.offsetMax = new Vector2(-10, -10);
        }

        void CreateModal()
        {
            var modalObj = new GameObject("DebugMenuModal");
            modalObj.transform.SetParent(mCanvasObject.transform, false);
            
            // Ensure modal is rendered on top
            var modalRect = modalObj.DefinitelyGetComponent<RectTransform>();
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.offsetMin = Vector2.zero;
            modalRect.offsetMax = Vector2.zero;
            modalRect.SetAsLastSibling(); // Render on top
            
            mModal = modalObj.AddComponent<DebugMenuModal>();
        }

        void RegisterTabProviders()
        {
            // Register all tab providers
            RegisterTabProvider("Spawn Mod Data", new SpawnModDataProxyTabProvider());
            RegisterTabProvider("Spawn Regions", new SpawnRegionModDataProxyTabProvider());
            RegisterTabProvider("Hiding Spots", new HidingSpotTabProvider());
            RegisterTabProvider("Wander Paths", new WanderPathTabProvider());

            // Switch to first tab if any exist
            if (mTabProviders.Count > 0)
            {
                var firstTab = new List<string>(mTabProviders.Keys)[0];
                SwitchTab(firstTab);
            }
        }

        public void RegisterTabProvider(string tabName, IDebugMenuTabContentProvider provider)
        {
            if (mTabProviders.ContainsKey(tabName))
            {
                LogWarning($"Tab provider for '{tabName}' already registered, replacing...");
                mTabProviders[tabName].Cleanup();
            }

            mTabProviders[tabName] = provider;
            provider.Initialize(mContentArea);

            // Create tab button
            CreateTabButton(tabName);
        }

        void CreateTabButton(string tabName)
        {
            var buttonObj = new GameObject($"TabButton_{tabName}");
            buttonObj.transform.SetParent(mTabButtonsPanel.transform, false);
            
            // Add RectTransform first
            var buttonRect = buttonObj.DefinitelyGetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(120, 15);
            
            var button = buttonObj.AddComponent<Button>();
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            
            // Button text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            // Add RectTransform for text first
            var textRect = textObj.DefinitelyGetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var buttonText = textObj.AddComponent<Text>();
            buttonText.text = tabName;
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.fontSize = 12;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            
            button.onClick.AddListener(new Action(() => SwitchTab(tabName)));
            mTabButtons[tabName] = button;
        }

        void SwitchTab(string tabName)
        {
            if (!mTabProviders.TryGetValue(tabName, out var provider))
            {
                LogError($"Tab provider for '{tabName}' not found");
                return;
            }

            // Hide current tab
            if (mCurrentTabProvider != null)
            {
                mCurrentTabProvider.Hide();
            }

            // Show new tab
            mCurrentTabProvider = provider;
            mCurrentTabName = tabName;
            provider.Show();
            provider.Refresh();

            // Update button colors
            foreach (var kvp in mTabButtons)
            {
                var buttonImage = kvp.Value.GetComponent<Image>();
                buttonImage.color = (kvp.Key == tabName) ? 
                    new Color(0.5f, 0.5f, 0.8f, 1f) : 
                    new Color(0.3f, 0.3f, 0.3f, 1f);
            }
        }

        public void ShowItemDetails<T>(T item) where T : ISerializedData
        {
            LogDebug($"ShowItemDetails called for: {item?.DisplayName ?? "null"}");
            
            if (mModal == null)
            {
                LogError("mModal is null!");
                return;
            }
            
            LogDebug("Calling mModal.ShowItemDetails...");
            mModal.ShowItemDetails(item);
        }

        public void ToggleMenu()
        {
            if (mMenuVisible)
            {
                HideMenu();
            }
            else
            {
                ShowMenu();
            }
        }

        public void ShowMenu()
        {
            mMenuVisible = true;
            if (mCanvasObject != null)
            {
                mCanvasObject.SetActive(true);
            }
            
            // Refresh current tab
            if (mCurrentTabProvider != null)
            {
                mCurrentTabProvider.Refresh();
            }
        }

        public void HideMenu()
        {
            mMenuVisible = false;
            if (mCanvasObject != null)
            {
                mCanvasObject.SetActive(false);
            }
        }

        public void ProcessCommand(string[] args)
        {
            if (args.Length == 0)
            {
                ToggleMenu();
                return;
            }

            // For now, just toggle the menu since we have GUI controls
            ToggleMenu();
        }
        // STOP ADDING LOGGING YOU TURD. ITS GLOBALLY AVAILABLE.
    }

    public static class Extensions
    {
        public static T DefinitelyGetComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() ?? gameObject.AddComponent<T>();
        }
    }
}
