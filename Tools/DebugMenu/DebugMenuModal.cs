using UnityEngine;
using UnityEngine.UI;
using System;
using Il2Cpp;

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
            // Modal background (covers entire screen)
            mModalPanel = new GameObject("ModalPanel");
            mModalPanel.transform.SetParent(transform, false);
            
            var modalRect = mModalPanel.DefinitelyGetComponent<RectTransform>();
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.offsetMin = Vector2.zero;
            modalRect.offsetMax = Vector2.zero;
            
            var modalImage = mModalPanel.AddComponent<Image>();
            modalImage.color = new Color(0, 0, 0, 0.7f); // Semi-transparent background
            
            // Make background clickable to close
            var modalButton = mModalPanel.AddComponent<Button>();
            modalButton.onClick.AddListener((UnityEngine.Events.UnityAction)Hide);

            // Content panel (centered)
            mContentPanel = new GameObject("ContentPanel");
            mContentPanel.transform.SetParent(mModalPanel.transform, false);
            
            var contentRect = mContentPanel.DefinitelyGetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.2f, 0.2f);
            contentRect.anchorMax = new Vector2(0.8f, 0.8f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            
            var contentImage = mContentPanel.AddComponent<Image>();
            contentImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            // Prevent content panel clicks from closing modal
            var contentButton = mContentPanel.AddComponent<Button>();

            CreateScrollArea();
            CreateCloseButton();

            mModalPanel.SetActive(false);
        }

        void CreateScrollArea()
        {
            // Scroll rect for content
            var scrollObj = new GameObject("ScrollRect");
            scrollObj.transform.SetParent(mContentPanel.transform, false);
            
            var scrollRect = scrollObj.DefinitelyGetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0.1f);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(20, 10);
            scrollRect.offsetMax = new Vector2(-20, -10);
            
            mScrollRect = scrollObj.AddComponent<ScrollRect>();
            mScrollRect.horizontal = false;
            mScrollRect.vertical = true;
            
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
            
            mDetailText = textObj.AddComponent<Text>();
            mDetailText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            mDetailText.fontSize = 12;
            mDetailText.color = Color.white;
            mDetailText.alignment = TextAnchor.UpperLeft;
            
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var textSizeFitter = textObj.AddComponent<ContentSizeFitter>();
            textSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        void CreateCloseButton()
        {
            var buttonObj = new GameObject("CloseButton");
            buttonObj.transform.SetParent(mContentPanel.transform, false);
            
            var buttonRect = buttonObj.DefinitelyGetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.8f, 0);
            buttonRect.anchorMax = new Vector2(1, 0.1f);
            buttonRect.offsetMin = new Vector2(-10, 10);
            buttonRect.offsetMax = new Vector2(-10, -10);
            
            mCloseButton = buttonObj.AddComponent<Button>();
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.6f, 0.3f, 0.3f, 1f);
            
            // Button text
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            
            var buttonText = textObj.AddComponent<Text>();
            buttonText.text = "Close";
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.fontSize = 12;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
            
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            mCloseButton.onClick.AddListener((UnityEngine.Events.UnityAction)Hide);
        }

        public void ShowItemDetails<T>(T item) where T : ISerializedData
        {
            if (item == null)
            {
                LogWarning("Cannot show details for null item");
                return;
            }

            string detailText = GenerateDetailText(item);
            mDetailText.text = detailText;
            
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
            mIsVisible = true;
            if (mModalPanel != null)
            {
                mModalPanel.SetActive(true);
            }
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
