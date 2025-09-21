using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Il2Cpp;

namespace ExpandedAiFramework.DebugMenu
{
    public class WanderPathTabProvider : DebugMenuTabContentProvider<WanderPath>
    {
        public override void Initialize(GameObject parentContentArea)
        {
            mSubDataManager = GetSubDataManager();
            base.Initialize(parentContentArea);
        }
        protected override void LoadData()
        {
            string sceneFilter = string.IsNullOrEmpty(mSceneFilter) ? null : mSceneFilter;
            string nameFilter = string.IsNullOrEmpty(mNameFilter) ? null : mNameFilter;
            
            var request = new GetWanderPathsRequest(OnDataLoaded, sceneFilter, nameFilter, null);
            Manager.DataManager.ScheduleMapDataRequest<WanderPath>(request);
        }

        protected override void PopulateListItem(GameObject itemObj, WanderPath item, int index)
        {
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(itemObj.transform, false);
            
            var text = textObj.AddComponent<Text>();
            text.text = $"[{index}] {item.Name}\n" +
                       $"Scene: {item.Scene} | Type: {item.WanderPathType}\n" +
                       $"Points: {item.PathPoints?.Length ?? 0}";
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

        protected override string GetItemName(WanderPath item)
        {
            return item.Name;
        }

        protected override string GetTabDisplayName()
        {
            return "Wander Paths";
        }

        protected override float GetItemHeight()
        {
            return 50f;
        }

        protected override ISubDataManager GetSubDataManager()
        {
            return Manager.DataManager.MapDataManagers.TryGetValue(typeof(WanderPath), out var manager) ? manager as ISubDataManager : null;
        }
        
        protected override void CreateTabSpecificButtons()
        {
            // Add Paint button for wander paths
            var paintGroup = CreateButtonGroup("Paint Actions", 80);
            var paintButton = CreateButton("Paint", paintGroup.transform, OnPaintClicked);
            
            // Call base to add settings button
            base.CreateTabSpecificButtons();
        }

        protected override Dictionary<string, string> GetTabSettings()
        {
            var settings = new Dictionary<string, string>();
            
            var paintManager = Manager.PaintManagers.TryGetValue("wanderpath", out var pm) ? pm as WanderPathPaintManager : null;
            if (paintManager != null)
            {
                settings.Add("Data Path", paintManager.CurrentDataPath ?? paintManager.DefaultDataPath);
                settings.Add("Current Data Name", paintManager.CurrentDataName ?? "");
                settings.Add("Current Data Name Base", paintManager.CurrentDataNameBase ?? "WanderPath");
            }
            
            return settings;
        }

        protected override Dictionary<string, System.Action<string>> GetTabSettingsCallbacks()
        {
            var callbacks = new Dictionary<string, System.Action<string>>();
            
            var paintManager = Manager.PaintManagers.TryGetValue("wanderpath", out var pm) ? pm as WanderPathPaintManager : null;
            if (paintManager != null)
            {
                callbacks.Add("Data Path", (value) => {
                    if (!string.IsNullOrEmpty(value))
                    {
                        paintManager.SetDataPath(value);
                    }
                });
                
                // Note: Current Data Name and Base Name are read-only for display purposes
            }
            
            return callbacks;
        }
        
        protected virtual void OnPaintClicked()
        {
            var paintManager = Manager.PaintManagers.TryGetValue("wanderpath", out var pm) ? pm : null;
            if (paintManager != null)
            {
                string[] args = { "NewWanderPath" };
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
        
        protected override void OnGoToClicked(WanderPath item)
        {
            var paintManager = Manager.PaintManagers.TryGetValue("wanderpath", out var pm) ? pm : null;
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
        
        protected override void OnDeleteClicked(WanderPath item)
        {
            var paintManager = Manager.PaintManagers.TryGetValue("wanderpath", out var pm) ? pm : null;
            if (paintManager != null)
            {
                string[] args = { GetItemName(item) };
                paintManager.ProcessCommand("delete", args);
                LogDebug($"Deleted {GetItemName(item)}");
                Refresh(); // Refresh the list after deletion
            }
            else
            {
                LogError($"No paint manager found for {GetTabDisplayName()}");
            }
        }
    }
}
