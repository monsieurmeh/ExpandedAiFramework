using UnityEngine;
using UnityEngine.UI;
using System;
using Il2Cpp;
using static ExpandedAiFramework.DebugMenu.Extensions;

namespace ExpandedAiFramework.DebugMenu
{
    [RegisterTypeInIl2Cpp]
    public class DebugMenuModal : MonoBehaviour
    {
        // UI Components
        private GameObject mModalPanel;
        private GameObject mContentPanel;
        private ScrollRect mScrollRect;
        private GameObject mScrollContent;
        private Text mDetailText;
        private Button mCloseButton;

        // State
        private bool mIsVisible = false;

        public DebugMenuModal(IntPtr ptr) : base(ptr) { }

        void Awake()
        {
            CreateUI();
        }

        void CreateUI()
        {
            // Create a separate Canvas for the modal with higher sorting order
            var modalCanvas = new GameObject("ModalCanvas");
            modalCanvas.transform.SetParent(transform, false);
            
            // Ensure the modal canvas RectTransform fills the screen
            var modalCanvasRect = modalCanvas.DefinitelyGetComponent<RectTransform>();
            modalCanvasRect.anchorMin = Vector2.zero;
            modalCanvasRect.anchorMax = Vector2.one;
            modalCanvasRect.offsetMin = Vector2.zero;
            modalCanvasRect.offsetMax = Vector2.zero;
            
            var canvas = modalCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1001; // Higher than main menu (1000)
            
            modalCanvas.AddComponent<GraphicRaycaster>();
            
            var canvasScaler = modalCanvas.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            // Modal background (covers entire screen)
            mModalPanel = new GameObject("ModalPanel");
            mModalPanel.transform.SetParent(modalCanvas.transform, false);
            
            var modalRect = mModalPanel.DefinitelyGetComponent<RectTransform>();
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.offsetMin = Vector2.zero;
            modalRect.offsetMax = Vector2.zero;
            
            var modalImage = mModalPanel.AddComponent<Image>();
            modalImage.color = new Color(0, 0, 0, 0.8f); // More opaque background
            
            // Make background clickable to close
            var modalButton = mModalPanel.AddComponent<Button>();
            modalButton.onClick.AddListener((UnityEngine.Events.UnityAction)Hide);

            // Content panel (much wider, centered)
            mContentPanel = new GameObject("ContentPanel");
            mContentPanel.transform.SetParent(mModalPanel.transform, false);
            
            var contentRect = mContentPanel.DefinitelyGetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f); // Center anchor
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(1200, 800); // Initial size
            contentRect.anchoredPosition = Vector2.zero;
            
            var contentImage = mContentPanel.AddComponent<Image>();
            contentImage.color = new Color(0.15f, 0.15f, 0.15f, 1f); // Fully opaque background
            
            // Add border outline
            var outline = mContentPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            outline.effectDistance = new Vector2(2, 2);

            // Prevent content panel clicks from closing modal
            var contentButton = mContentPanel.AddComponent<Button>();

            CreateTitleBar();
            CreateScrollArea();

            mModalPanel.SetActive(false);
        }

        void CreateTitleBar()
        {
            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(mContentPanel.transform, false);
            
            var titleRect = titleBar.DefinitelyGetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.9f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            
            var titleBg = titleBar.AddComponent<Image>();
            titleBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            // Title text
            var titleTextObj = new GameObject("TitleText");
            titleTextObj.transform.SetParent(titleBar.transform, false);
            
            var titleTextRect = titleTextObj.DefinitelyGetComponent<RectTransform>();
            titleTextRect.anchorMin = new Vector2(0, 0);
            titleTextRect.anchorMax = new Vector2(0.8f, 1);
            titleTextRect.offsetMin = new Vector2(15, 0);
            titleTextRect.offsetMax = new Vector2(0, 0);
            
            var titleText = titleTextObj.AddComponent<Text>();
            titleText.text = "Item Details - Drag to Move";
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 14;
            titleText.color = Color.white;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.fontStyle = FontStyle.Bold;
            
            CreateCloseButton(titleBar);
        }

        void CreateScrollArea()
        {
            // Scroll rect for content
            var scrollObj = new GameObject("ScrollRect");
            scrollObj.transform.SetParent(mContentPanel.transform, false);
            
            var scrollRect = scrollObj.DefinitelyGetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 0.9f);
            scrollRect.offsetMin = new Vector2(15, 15);
            scrollRect.offsetMax = new Vector2(-15, -10);
            
            mScrollRect = scrollObj.AddComponent<ScrollRect>();
            mScrollRect.horizontal = false;
            mScrollRect.vertical = true;
            mScrollRect.scrollSensitivity = 20f; // Increase scroll wheel sensitivity
            mScrollRect.movementType = ScrollRect.MovementType.Clamped;
            
            // Add background to scroll area
            var scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            
            // Add mask to prevent content bleeding
            var mask = scrollObj.AddComponent<Mask>();
            mask.showMaskGraphic = true;
            
            // Scroll content
            mScrollContent = new GameObject("ScrollContent");
            mScrollContent.transform.SetParent(scrollObj.transform, false);
            
            var contentRect = mScrollContent.DefinitelyGetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0, 1);
            
            var contentSizeFitter = mScrollContent.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            mScrollRect.content = contentRect;
            
            // Detail text
            var textObj = new GameObject("DetailText");
            textObj.transform.SetParent(mScrollContent.transform, false);
            
            var textRect = textObj.DefinitelyGetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);
            
            mDetailText = textObj.AddComponent<Text>();
            mDetailText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            mDetailText.fontSize = 11;
            mDetailText.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            mDetailText.alignment = TextAnchor.UpperLeft;
            mDetailText.supportRichText = true;
            
            var textSizeFitter = textObj.AddComponent<ContentSizeFitter>();
            textSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        void CreateCloseButton(GameObject titleBar)
        {
            var buttonObj = new GameObject("CloseButton");
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

        public void ShowItemDetails<T>(T item) where T : ISerializedData
        {
            LogWarning($"Modal ShowItemDetails called for: {item?.DisplayName ?? "null"}");
            
            if (item == null)
            {
                LogWarning("Cannot show details for null item");
                return;
            }

            string detailText = GenerateDetailText(item);
            LogWarning($"Generated detail text length: {detailText?.Length ?? 0}");
            
            if (mDetailText == null)
            {
                LogWarning("mDetailText is null!");
                return;
            }
            
            mDetailText.text = detailText;
            LogWarning("About to call Show()...");
            Show();
        }

        string GenerateDetailText<T>(T item) where T : ISerializedData
        {
            string detailText = $"<b>{item.GetType().Name} Details</b>\n\n";
            detailText += $"<b>GUID:</b> {item.Guid}\n";
            detailText += $"<b>Display Name:</b> {item.DisplayName}\n";
            detailText += $"<b>Scene:</b> {item.Scene}\n";
            detailText += $"<b>Data Location:</b> {item.DataLocation}\n\n";

            // Type-specific details
            if (item is SpawnModDataProxy proxy)
            {
                detailText += GenerateSpawnModDataProxyDetails(proxy);
            }
            else if (item is SpawnRegionModDataProxy regionProxy)
            {
                detailText += GenerateSpawnRegionModDataProxyDetails(regionProxy);
            }
            else if (item is HidingSpot spot)
            {
                detailText += GenerateHidingSpotDetails(spot);
            }
            else if (item is WanderPath path)
            {
                detailText += GenerateWanderPathDetails(path);
            }

            return detailText;
        }

        string GenerateSpawnModDataProxyDetails(SpawnModDataProxy proxy)
        {
            return $"<b>Position:</b> ({proxy.CurrentPosition.x:F2}, {proxy.CurrentPosition.y:F2}, {proxy.CurrentPosition.z:F2})\n" +
                   $"<b>Rotation:</b> ({proxy.CurrentRotation.x:F2}, {proxy.CurrentRotation.y:F2}, {proxy.CurrentRotation.z:F2}, {proxy.CurrentRotation.w:F2})\n" +
                   $"<b>AI SubType:</b> {proxy.AiSubType}\n" +
                   $"<b>AI Mode:</b> {proxy.AiMode}\n" +
                   $"<b>Wildlife Mode:</b> {proxy.WildlifeMode}\n" +
                   $"<b>Force Spawn:</b> {proxy.ForceSpawn}\n" +
                   $"<b>Available:</b> {proxy.Available}\n" +
                   $"<b>Spawned:</b> {proxy.Spawned}\n" +
                   $"<b>Disconnected:</b> {proxy.Disconnected}\n" +
                   $"<b>Last Despawn Time:</b> {proxy.LastDespawnTime}\n" +
                   $"<b>Variant Spawn Type:</b> {proxy.VariantSpawnTypeString}\n" +
                   $"<b>Parent GUID:</b> {proxy.ParentGuid}";
        }

        string GenerateSpawnRegionModDataProxyDetails(SpawnRegionModDataProxy proxy)
        {
            return $"<b>Position:</b> ({proxy.CurrentPosition.x:F2}, {proxy.CurrentPosition.y:F2}, {proxy.CurrentPosition.z:F2})\n" +
                   $"<b>AI Type:</b> {proxy.AiType}\n" +
                   $"<b>AI SubType:</b> {proxy.AiSubType}\n" +
                   $"<b>Is Active:</b> {proxy.IsActive}\n" +
                   $"<b>Connected:</b> {proxy.Connected}\n" +
                   $"<b>Pending Force Spawns:</b> {proxy.PendingForceSpawns}\n" +
                   $"<b>Last Despawn Time:</b> {proxy.LastDespawnTime}\n" +
                   $"<b>Hours Played:</b> {proxy.HoursPlayed}\n" +
                   $"<b>Cooldown Timer Hours:</b> {proxy.CooldownTimerHours}\n" +
                   $"<b>Num Respawns Pending:</b> {proxy.NumRespawnsPending}\n" +
                   $"<b>Num Trapped:</b> {proxy.NumTrapped}\n" +
                   $"<b>Wildlife Mode:</b> {proxy.WildlifeMode}";
        }

        string GenerateHidingSpotDetails(HidingSpot spot)
        {
            return $"<b>Name:</b> {spot.Name}\n" +
                   $"<b>Position:</b> ({spot.Position.x:F2}, {spot.Position.y:F2}, {spot.Position.z:F2})\n" +
                   $"<b>Rotation:</b> ({spot.Rotation.x:F2}, {spot.Rotation.y:F2}, {spot.Rotation.z:F2}, {spot.Rotation.w:F2})";
        }

        string GenerateWanderPathDetails(WanderPath path)
        {
            string details = $"<b>Name:</b> {path.Name}\n" +
                           $"<b>Type:</b> {path.WanderPathType}\n" +
                           $"<b>Path Points Count:</b> {path.PathPoints?.Length ?? 0}\n\n";
                           
            if (path.PathPoints != null)
            {
                details += "<b>Path Points:</b>\n";
                for (int i = 0; i < path.PathPoints.Length; i++)
                {
                    var point = path.PathPoints[i];
                    details += $"  Point {i}: ({point.x:F2}, {point.y:F2}, {point.z:F2})\n";
                }
            }

            return details;
        }

        public void Show()
        {
            LogWarning("Modal Show() called");
            mIsVisible = true;
            
            if (mModalPanel == null)
            {
                LogWarning("mModalPanel is null!");
                return;
            }
            
            // Ensure modal is on top when shown
            transform.SetAsLastSibling();
            
            LogWarning("Setting modal panel active...");
            mModalPanel.SetActive(true);
            LogWarning("Modal should now be visible");
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
