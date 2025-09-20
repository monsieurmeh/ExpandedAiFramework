using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Il2Cpp;

namespace ExpandedAiFramework.DebugMenu
{
    public class HidingSpotTabProvider : DebugMenuTabContentProvider<HidingSpot>
    {
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
    }
}
