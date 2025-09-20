using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppInterop.Runtime.Runtime;

namespace ExpandedAiFramework.DebugMenu
{
    public class SpawnModDataProxyTabProvider : DebugMenuTabContentProvider<SpawnModDataProxy>
    {
        // Sub-tab management
        private GameObject mSubTabPanel;
        private Button mNormalButton;
        private Button mAuroraButton;
        private WildlifeMode mCurrentMode = WildlifeMode.Normal;

        protected override void CreateUI(GameObject parentContentArea)
        {
            base.CreateUI(parentContentArea);
            CreateSubTabPanel();
        }

        void CreateSubTabPanel()
        {
            mSubTabPanel = new GameObject("SubTabPanel");
            mSubTabPanel.transform.SetParent(mRootPanel.transform, false);
            
            var subTabRect = mSubTabPanel.AddComponent<RectTransform>();
            subTabRect.anchorMin = new Vector2(0, 0.75f);
            subTabRect.anchorMax = new Vector2(0.5f, 0.85f);
            subTabRect.offsetMin = new Vector2(5, -5);
            subTabRect.offsetMax = new Vector2(-5, -5);
            
            var subTabLayout = mSubTabPanel.AddComponent<HorizontalLayoutGroup>();
            subTabLayout.spacing = 5;
            subTabLayout.childControlWidth = true;
            subTabLayout.childControlHeight = true;

            mNormalButton = CreateButton("Normal", mSubTabPanel.transform, new Action(() => SwitchMode(WildlifeMode.Normal)));
            mAuroraButton = CreateButton("Aurora", mSubTabPanel.transform, new Action(() => SwitchMode(WildlifeMode.Aurora)));
            
            UpdateSubTabButtons();

            // Adjust filter panel position
            var filterRect = mFilterPanel.GetComponent<RectTransform>();
            filterRect.anchorMin = new Vector2(0, 0.65f);
            filterRect.anchorMax = new Vector2(1, 0.75f);

            // Adjust list panel position
            var listRect = mListPanel.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0, 0.05f);
            listRect.anchorMax = new Vector2(1, 0.65f);
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
    }
}
