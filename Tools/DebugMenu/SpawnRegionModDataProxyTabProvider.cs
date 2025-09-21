using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Il2Cpp;

namespace ExpandedAiFramework.DebugMenu
{
    public class SpawnRegionModDataProxyTabProvider : DebugMenuTabContentProvider<SpawnRegionModDataProxy>
    {
        public override void Initialize(GameObject parentContentArea)
        {
            mSubDataManager = GetSubDataManager();
            base.Initialize(parentContentArea);
        }
        protected override void LoadData()
        {
            string sceneFilter = string.IsNullOrEmpty(mSceneFilter) ? null : mSceneFilter;
            var request = new GetSpawnRegionModDataProxiesRequest(OnDataLoaded, sceneFilter, null, null);
            Manager.DataManager.ScheduleSpawnRegionModDataProxyRequest(request);
        }

        protected override void PopulateListItem(GameObject itemObj, SpawnRegionModDataProxy item, int index)
        {
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(itemObj.transform, false);
            
            var text = textObj.AddComponent<Text>();
            text.text = $"[{index}] {item.Guid}\n" +
                       $"Scene: {item.Scene} | Type: {item.AiType}/{item.AiSubType}\n" +
                       $"Active: {item.IsActive} | Connected: {item.Connected}";
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

        protected override string GetItemName(SpawnRegionModDataProxy item)
        {
            return item.Guid.ToString();
        }

        protected override string GetTabDisplayName()
        {
            return "Spawn Region Mod Data";
        }

        protected override float GetItemHeight()
        {
            return 50f;
        }

        protected override ISubDataManager GetSubDataManager()
        {
            return Manager.DataManager.SpawnRegionModDataProxyManager;
        }
        
        protected override void CreateTabSpecificButtons()
        {
            // Add Paint button for spawn regions
            var paintGroup = CreateButtonGroup("Paint Actions", 80);
            var paintButton = CreateButton("Paint", paintGroup.transform, OnPaintClicked);
            
            // Call base to add settings button
            base.CreateTabSpecificButtons();
        }

        protected override Dictionary<string, string> GetTabSettings() => new Dictionary<string, string>(); //not a lot here yet.
            
        protected override Dictionary<string, System.Action<string>> GetTabSettingsCallbacks() => new Dictionary<string, System.Action<string>>(); //not a lot here yet.
        
        protected virtual void OnPaintClicked()
        {
            var paintManager = Manager.PaintManagers.TryGetValue("spawnregion", out var pm) ? pm : null;
            if (paintManager != null)
            {
                string[] args = { "NewSpawnRegion" };
                Manager.ConsoleCommandManager.SetActivePaintManager(paintManager);
                paintManager.StartPaint(args);
                LogDebug($"Started paint mode for {GetTabDisplayName()}");
                
                // Hide the debug menu
                DebugMenu.DebugMenuManager.Instance?.HideMenu();
            }
            else
            {
                LogError($"No paint manager found for {GetTabDisplayName()}");
            }
        }
        
        protected override void OnGoToClicked(SpawnRegionModDataProxy item)
        {
            var paintManager = Manager.PaintManagers.TryGetValue("spawnregion", out var pm) ? pm : null;
            if (paintManager != null)
            {
                string[] args = { GetItemName(item) };
                paintManager.ProcessCommand("goto", args);
                LogDebug($"Going to {GetItemName(item)}");
            }
            else
            {
                LogError($"No paint manager found for {GetTabDisplayName()}");
            }
        }
        
        protected override void OnDeleteClicked(SpawnRegionModDataProxy item)
        {
            // Delete operation disabled for spawn region proxies - should handle entire spawn region instead
            LogDebug($"Delete not available for SpawnRegionModDataProxy items - use spawn region management to handle entire spawn region");
        }
    }
}
