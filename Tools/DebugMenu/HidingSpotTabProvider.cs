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
        
        protected override void CreateGlobalActionGroup(GameObject parent)
        {
            base.CreateGlobalActionGroup(parent);
            
            var globalGroup = parent.transform.GetChild(0).gameObject; // Get the group we just created
            
            // Add Paint button
            var paintButton = CreateButton("Paint", globalGroup.transform, OnPaintClicked);
        }
        
        protected virtual void OnPaintClicked()
        {
            var paintManager = Manager.PaintManagers.TryGetValue("hidingspot", out var pm) ? pm : null;
            if (paintManager != null)
            {
                string[] args = { "NewHidingSpot" };
                paintManager.StartPaint(args);
                LogDebug($"Started paint mode for {GetTabDisplayName()}");
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
