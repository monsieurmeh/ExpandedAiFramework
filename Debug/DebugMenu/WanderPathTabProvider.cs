using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Il2Cpp;
using ExpandedAiFramework.UI;

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
        
        protected override void CreateTabSpecificButtons(GameObject buttonBar)
        {
            // Add Paint button for wander paths
            var paintGroup = CreateButtonGroup("Paint Actions", 80, buttonBar);
            var paintButton = CreateButton("Paint", paintGroup.transform, OnPaintClicked);
            
            // Call base to add settings button
            base.CreateTabSpecificButtons(buttonBar);
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
        
        // IDebugMenuEntityModalProvider implementation
        protected override void PopulateEntityModalForType(WanderPath entity, GameObject modalContent, System.Action<string, object> onValueChanged)
        {
            // Create basic fields first
            base.PopulateEntityModalForType(entity, modalContent, onValueChanged);
            
            // Create WanderPath-specific fields with custom styling
            
            // Name field - important, make it wide
            var nameOptions = TextFormFieldOptions.Wide("Name", 80f);
            nameOptions.LabelOptions.textOptions.fontSize = 14;
            nameOptions.LabelOptions.textOptions.color = new Color(0.9f, 0.9f, 1f, 1f);
            var nameField = FormFieldFactory.CreateTextField("Name", entity.Name, modalContent.transform, onValueChanged, nameOptions);
            
            // Wander Path Type - standard dropdown
            var wanderPathTypeOptions = DropdownFormFieldOptions.Default("Wander Path Type");
            wanderPathTypeOptions.LabelOptions.textOptions.fontSize = 12;
            var wanderPathTypeField = FormFieldFactory.CreateDropdownField("Wander Path Type", entity.WanderPathType, modalContent.transform, onValueChanged, wanderPathTypeOptions);
            
            // Path points count (read-only) - compact since it's just info
            var countOptions = TextFormFieldOptions.Compact("Path Points Count", 45f);
            countOptions.InputOptions.backgroundImageOptions = new ImageOptions { Color = new Color(0.15f, 0.15f, 0.2f, 1f) }; // Darker for read-only
            countOptions.LabelOptions.textOptions.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            var pathPointsCountField = FormFieldFactory.CreateTextField("Path Points Count", entity.PathPoints?.Length.ToString() ?? "0", modalContent.transform, null, countOptions, true);
            
            // Individual path points (for editing, show first few points)
            if (entity.PathPoints != null && entity.PathPoints.Length > 0)
            {
                // Show first 5 path points for editing (to avoid overwhelming the UI)
                int maxPointsToShow = Mathf.Min(5, entity.PathPoints.Length);
                for (int i = 0; i < maxPointsToShow; i++)
                {
                    // Compact Vector3 fields for path points
                    var pointOptions = Vector3FormFieldOptions.Compact($"Path Point {i}");
                    pointOptions.LabelOptions.textOptions.fontSize = 11;
                    pointOptions.LabelOptions.textOptions.color = new Color(0.7f, 0.9f, 0.7f, 1f); // Greenish for path points
                    var pointField = FormFieldFactory.CreateVector3Field($"Path Point {i}", entity.PathPoints[i], modalContent.transform, onValueChanged, pointOptions);
                    
                    // Register each path point field
                    var entityModal = modalContent.GetComponentInParent<DebugMenuEntityModal>();
                    if (entityModal != null)
                    {
                        entityModal.RegisterFormField($"Path Point {i}", pointField);
                    }
                }
                
                if (entity.PathPoints.Length > maxPointsToShow)
                {
                    // Info field for additional points - very compact
                    var moreOptions = TextFormFieldOptions.Compact("Additional Points", 35f);
                    moreOptions.InputOptions.backgroundImageOptions = new ImageOptions { Color = new Color(0.12f, 0.12f, 0.15f, 1f) };
                    moreOptions.LabelOptions.textOptions.color = new Color(0.6f, 0.6f, 0.6f, 1f);
                    moreOptions.LabelOptions.textOptions.fontSize = 10;
                    var morePointsField = FormFieldFactory.CreateTextField("Additional Points", $"... and {entity.PathPoints.Length - maxPointsToShow} more points", modalContent.transform, null, moreOptions, true);
                    
                    var entityModal2 = modalContent.GetComponentInParent<DebugMenuEntityModal>();
                    if (entityModal2 != null)
                    {
                        entityModal2.RegisterFormField("Additional Points", morePointsField);
                    }
                }
            }
            
            // Register the main fields with the modal
            var entityModal3 = modalContent.GetComponentInParent<DebugMenuEntityModal>();
            if (entityModal3 != null)
            {
                entityModal3.RegisterFormField("Name", nameField);
                entityModal3.RegisterFormField("Wander Path Type", wanderPathTypeField);
                entityModal3.RegisterFormField("Path Points Count", pathPointsCountField);
            }
        }

        protected override bool ApplyEntityChangesForType(WanderPath entity, Dictionary<string, object> fieldValues)
        {
            try
            {                
                // Log requested changes for debugging
                foreach (var field in fieldValues)
                {
                    LogDebug($"  {field.Key}: {field.Value} ({field.Value?.GetType().Name})");
                }
                
                // Use the thread-safe request system for CRUD operations
                var updateRequest = new UpdateWanderPathRequest(entity.Guid.ToString(), fieldValues, (updatedEntity, result) =>
                {
                    if (result == RequestResult.Succeeded)
                    {
                        LogDebug($"Successfully updated WanderPath: {updatedEntity.Name}");
                        // Refresh the tab to show updated data
                        Refresh();
                    }
                    else
                    {
                        LogError($"Failed to update WanderPath: {entity.Name}");
                    }
                }, false); // Callback is not thread-safe (runs on main thread)
                
                // Schedule the request through the data manager
                var dataManager = GetSubDataManager();
                if (dataManager != null)
                {
                    dataManager.ScheduleRequest(updateRequest);
                    return true;
                }
                else
                {
                    LogError("Failed to get SubDataManager for WanderPath update");
                    return false;
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to apply changes to WanderPath: {e.Message}");
                return false;
            }
        }

        protected override string GetEntityModalTitleForType(WanderPath entity)
        {
            return $"Wander Path Details - {entity.Name}";
        }
    }
}
