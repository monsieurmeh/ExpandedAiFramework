using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppInterop.Runtime.Runtime;

namespace ExpandedAiFramework.DebugMenu
{
    public class SpawnModDataProxyTabProvider : DebugMenuTabContentProvider<SpawnModDataProxy>
    {
        // Mode selection
        private Button mNormalButton;
        private Button mAuroraButton;
        private WildlifeMode mCurrentMode = WildlifeMode.Normal;
        
        public override void Initialize(GameObject parentContentArea)
        {
            mSubDataManager = GetSubDataManager();
            base.Initialize(parentContentArea);
        }

        protected override void CreateFilterPanel()
        {
            base.CreateFilterPanel();
            AddModeButtonsToFilter();
        }

        void AddModeButtonsToFilter()
        {
            // Add mode selection group to filter panel
            var modeGroup = CreateFilterGroup("Wildlife Mode");
            mNormalButton = CreateButton("Normal", modeGroup.transform, new Action(() => SwitchMode(WildlifeMode.Normal)));
            mAuroraButton = CreateButton("Aurora", modeGroup.transform, new Action(() => SwitchMode(WildlifeMode.Aurora)));
            
            UpdateSubTabButtons();
        }

        void SwitchMode(WildlifeMode mode)
        {
            if (mCurrentMode != mode)
            {
                mCurrentMode = mode;
                UpdateSubTabButtons();
                Refresh();
            }
        }

        void UpdateSubTabButtons()
        {
            if (mNormalButton != null)
            {
                var normalImage = mNormalButton.GetComponent<Image>();
                normalImage.color = (mCurrentMode == WildlifeMode.Normal) ? 
                    new Color(0.5f, 0.8f, 0.5f, 1f) : 
                    new Color(0.3f, 0.3f, 0.3f, 1f);
            }

            if (mAuroraButton != null)
            {
                var auroraImage = mAuroraButton.GetComponent<Image>();
                auroraImage.color = (mCurrentMode == WildlifeMode.Aurora) ? 
                    new Color(0.5f, 0.8f, 0.5f, 1f) : 
                    new Color(0.3f, 0.3f, 0.3f, 1f);
            }
        }

        protected override void LoadData()
        {
            string sceneFilter = string.IsNullOrEmpty(mSceneFilter) ? null : mSceneFilter;
            var request = new GetSpawnModDataProxiesRequest(mCurrentMode, OnDataLoaded, sceneFilter, null);
            Manager.DataManager.ScheduleSpawnModDataProxyRequest(request, mCurrentMode);
        }

        protected override void PopulateListItem(GameObject itemObj, SpawnModDataProxy item, int index)
        {
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(itemObj.transform, false);
            
            var text = textObj.AddComponent<Text>();
            text.text = $"[{index}] {item.DisplayName}\n" +
                       $"Scene: {item.Scene} | SubType: {item.AiSubType}\n" +
                       $"Available: {item.Available} | Spawned: {item.Spawned}";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 10;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);
        }

        protected override string GetItemName(SpawnModDataProxy item)
        {
            return item.DisplayName;
        }

        protected override string GetTabDisplayName()
        {
            return $"Spawn Mod Data ({mCurrentMode})";
        }

        protected override float GetItemHeight()
        {
            return 50f;
        }

        protected override bool PassesCustomFilter(SpawnModDataProxy item)
        {
            // Additional filtering can be added here
            return item.WildlifeMode == mCurrentMode;
        }

        protected override ISubDataManager GetSubDataManager()
        {
            return Manager.DataManager.SpawnModDataProxyManagers[(int)mCurrentMode];
        }
        
        protected override void OnGoToClicked(SpawnModDataProxy item)
        {
            // SpawnModDataProxy doesn't have direct goto functionality
            LogDebug($"GoTo not available for SpawnModDataProxy items");
        }
        
        protected override void OnDeleteClicked(SpawnModDataProxy item)
        {
            // SpawnModDataProxy deletion would need special handling
            LogDebug($"Delete not available for SpawnModDataProxy items - use spawn region management instead");
        }
    }
}
