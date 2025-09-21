using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using Il2Cpp;
using static ExpandedAiFramework.DebugMenu.Extensions;
using ExpandedAiFramework.StolenCode;
using ExpandedAiFramework.UI;

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
        private GameObject mUnifiedButtonBar;
        private GameObject mContentArea;
        private DebugMenuEntityModal mEntityModal;
        private DebugMenuSettingsModal mSettingsModal;

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
        
        // Public access to unified button bar for tab providers
        public GameObject UnifiedButtonBar => mUnifiedButtonBar;

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
                // Create Canvas using factory
                mCanvasObject = CreateCanvas();
                CreateMainPanel();
                CreateEntityModal();
                CreateSettingsModal();
                
                // Initially hide the menu
                mCanvasObject.SetActive(false);
                
                LogDebug("Debug menu UI created successfully");
            }
            catch (Exception e)
            {
                LogError($"Error creating debug menu UI: {e}");
            }
        }

        GameObject CreateCanvas()
        {
            var canvasObj = new GameObject("EAFDebugMenuCanvas");
            canvasObj.transform.SetParent(transform);
            
            mCanvas = canvasObj.AddComponent<Canvas>();
            mCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mCanvas.sortingOrder = 1000;
            
            var canvasScaler = canvasObj.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            return canvasObj;
        }

        void CreateMainPanel()
        {
            var mainPanelOptions = PanelOptions.RootContainerPanel(PanelLayoutType.Vertical);
            mainPanelOptions.Name = "MainPanel";
            mainPanelOptions.ImageOptions = new ImageOptions { Color = new Color(0.1f, 0.1f, 0.1f, 0.95f) };
            mainPanelOptions.LayoutGroupOptions = LayoutGroupOptions.Vertical(0, new RectOffset(0, 0, 0, 0));
            
            mMainPanel = PanelFactory.CreatePanel(mCanvasObject.transform, mainPanelOptions);
            
            CreateTabButtonsPanel();
            CreateUnifiedButtonBar();
            CreateContentArea();
        }

        void CreateTabButtonsPanel()
        {
            var tabButtonsPanelOptions = PanelOptions.Default(PanelLayoutType.Horizontal);
            tabButtonsPanelOptions.Name = "TabButtonsPanel";
            tabButtonsPanelOptions.LayoutElementOptions = LayoutElementOptions.PreferredSize(-1, 50, 1, 0);
            tabButtonsPanelOptions.LayoutGroupOptions = LayoutGroupOptions.Horizontal(5, new RectOffset(10, 10, 5, 5));
            tabButtonsPanelOptions.LayoutGroupOptions.childForceExpandWidth = false;
            tabButtonsPanelOptions.LayoutGroupOptions.childControlWidth = true;
            tabButtonsPanelOptions.ImageOptions = new ImageOptions { Color = new Color(0.12f, 0.12f, 0.12f, 0.9f) };
            
            mTabButtonsPanel = PanelFactory.CreatePanel(mMainPanel.transform, tabButtonsPanelOptions);
        }

        void CreateUnifiedButtonBar()
        {
            var buttonBarOptions = PanelOptions.Default(PanelLayoutType.Horizontal);
            buttonBarOptions.Name = "UnifiedButtonBar";
            buttonBarOptions.LayoutElementOptions = LayoutElementOptions.PreferredSize(-1, 50, 1, 0);
            buttonBarOptions.LayoutGroupOptions = LayoutGroupOptions.Horizontal(3, new RectOffset(3, 3, 1, 1));
            buttonBarOptions.LayoutGroupOptions.childControlWidth = true;
            buttonBarOptions.LayoutGroupOptions.childControlHeight = true;
            buttonBarOptions.LayoutGroupOptions.childForceExpandWidth = false;
            buttonBarOptions.LayoutGroupOptions.childForceExpandHeight = false;
            buttonBarOptions.ImageOptions = new ImageOptions { Color = new Color(0.15f, 0.15f, 0.15f, 0.9f) };
            
            mUnifiedButtonBar = PanelFactory.CreatePanel(mMainPanel.transform, buttonBarOptions);
            
            // Add a Mask component to ensure children don't extend beyond bounds
            var mask = mUnifiedButtonBar.AddComponent<Mask>();
            mask.showMaskGraphic = false; // Don't show the mask graphic itself
        }

        void CreateContentArea()
        {
            var contentAreaOptions = PanelOptions.Default(PanelLayoutType.None);
            contentAreaOptions.Name = "ContentArea";
            contentAreaOptions.LayoutElementOptions = LayoutElementOptions.Flexible(1, 1);
            contentAreaOptions.ImageOptions = new ImageOptions { Color = new Color(0.08f, 0.08f, 0.08f, 0.3f) };
            
            mContentArea = PanelFactory.CreatePanel(mMainPanel.transform, contentAreaOptions);
        }

        void CreateEntityModal()
        {
            var modalOptions = PanelOptions.RootContainerPanel(PanelLayoutType.None);
            modalOptions.Name = "DebugMenuEntityModal";
            modalOptions.HasBackground = false;
            
            var modalObj = PanelFactory.CreatePanel(mCanvasObject.transform, modalOptions);
            modalObj.transform.SetAsLastSibling();
            mEntityModal = modalObj.AddComponent<DebugMenuEntityModal>();
        }

        void CreateSettingsModal()
        {
            var settingsModalOptions = PanelOptions.RootContainerPanel(PanelLayoutType.None);
            settingsModalOptions.Name = "DebugMenuSettingsModal";
            settingsModalOptions.HasBackground = false;
            
            var settingsModalObj = PanelFactory.CreatePanel(mCanvasObject.transform, settingsModalOptions);
            settingsModalObj.transform.SetAsLastSibling();
            mSettingsModal = settingsModalObj.AddComponent<DebugMenuSettingsModal>();
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
            // Create tab button using new factory system
            var buttonOptions = ButtonOptions.TextButton(tabName, 120, 40);
            buttonOptions.textOptions = TextOptions.Default(tabName, 12);
            buttonOptions.layoutElement = LayoutElementOptions.Fixed(120, 40);
            
            var button = ButtonFactory.CreateButton(mTabButtonsPanel.transform, buttonOptions, () => SwitchTab(tabName));
            button.name = $"TabButton_{tabName}";
            
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
            
            if (mEntityModal == null)
            {
                LogError("mEntityModal is null!");
                return;
            }
            
            if (mCurrentTabProvider == null)
            {
                LogError("mCurrentTabProvider is null!");
                return;
            }
            
            LogDebug("Calling mEntityModal.ShowEntityDetails...");
            mEntityModal.ShowEntityDetails(item, mCurrentTabProvider);
        }

        public void ShowTabSettings(string tabName, Dictionary<string, string> settings, Dictionary<string, System.Action<string>> callbacks)
        {
            LogDebug($"ShowTabSettings called for tab: {tabName}");
            
            if (mSettingsModal == null)
            {
                LogError("mSettingsModal is null!");
                return;
            }
            
            LogDebug("Calling mSettingsModal.ShowSettings...");
            mSettingsModal.ShowSettings(tabName, settings, callbacks);
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
            InputBLocker.LockPosition(true);
        }

        public void HideMenu()
        {
            mMenuVisible = false;
            if (mCanvasObject != null)
            {
                mCanvasObject.SetActive(false);
            }
            InputBLocker.LockPosition(false);
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
