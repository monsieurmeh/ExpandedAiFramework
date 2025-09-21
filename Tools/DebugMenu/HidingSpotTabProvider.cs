using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Il2Cpp;

namespace ExpandedAiFramework.DebugMenu
{
    public class HidingSpotTabProvider : DebugMenuTabContentProvider<HidingSpot>
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
            
            var request = new GetHidingSpotsRequest(OnDataLoaded, sceneFilter, nameFilter);
            Manager.DataManager.ScheduleMapDataRequest<HidingSpot>(request);
        }

        protected override void PopulateListItem(GameObject itemObj, HidingSpot item, int index)
        {
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(itemObj.transform, false);
            
            var text = textObj.AddComponent<Text>();
            text.text = $"[{index}] {item.Name}\n" +
                       $"Scene: {item.Scene}\n" +
                       $"Position: ({item.Position.x:F1}, {item.Position.y:F1}, {item.Position.z:F1})";
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

        protected override string GetItemName(HidingSpot item)
        {
            return item.Name;
        }

        protected override string GetTabDisplayName()
        {
            return "Hiding Spots";
        }

        protected override float GetItemHeight()
        {
            return 50f;
        }

        protected override ISubDataManager GetSubDataManager()
        {
            return Manager.DataManager.MapDataManagers.TryGetValue(typeof(HidingSpot), out var manager) ? manager as ISubDataManager : null;
        }
        
        protected override void CreateTabSpecificButtons()
        {
            // Add Paint button for hiding spots
            var paintGroup = CreateButtonGroup("Paint Actions", 80);
            var paintButton = CreateButton("Paint", paintGroup.transform, OnPaintClicked);
            
            // Call base to add settings button
            base.CreateTabSpecificButtons();
        }

        protected override Dictionary<string, string> GetTabSettings()
        {
            var settings = new Dictionary<string, string>();
            
            var paintManager = Manager.PaintManagers.TryGetValue("hidingspot", out var pm) ? pm as HidingSpotPaintManager : null;
            if (paintManager != null)
            {
                settings.Add("Data Path", paintManager.CurrentDataPath ?? paintManager.DefaultDataPath);
                settings.Add("Current Data Name", paintManager.CurrentDataName ?? "");
                settings.Add("Current Data Name Base", paintManager.CurrentDataNameBase ?? "HidingSpot");
            }
            
            return settings;
        }

        protected override Dictionary<string, System.Action<string>> GetTabSettingsCallbacks()
        {
            var callbacks = new Dictionary<string, System.Action<string>>();
            
            var paintManager = Manager.PaintManagers.TryGetValue("hidingspot", out var pm) ? pm as HidingSpotPaintManager : null;
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
            var paintManager = Manager.PaintManagers.TryGetValue("hidingspot", out var pm) ? pm : null;
            if (paintManager != null)
            {
                string[] args = { "NewHidingSpot" };
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
        
        protected override void OnGoToClicked(HidingSpot item)
        {
            var paintManager = Manager.PaintManagers.TryGetValue("hidingspot", out var pm) ? pm : null;
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
        
        protected override void OnDeleteClicked(HidingSpot item)
        {
            var paintManager = Manager.PaintManagers.TryGetValue("hidingspot", out var pm) ? pm : null;
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
