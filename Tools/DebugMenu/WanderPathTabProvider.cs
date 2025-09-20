using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Il2Cpp;

namespace ExpandedAiFramework.DebugMenu
{
    public class WanderPathTabProvider : DebugMenuTabContentProvider<WanderPath>
    {
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
    }
}
