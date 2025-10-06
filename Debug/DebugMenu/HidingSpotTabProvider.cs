using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Il2Cpp;
using ExpandedAiFramework.UI;

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
        
        protected override void CreateTabSpecificButtons(GameObject buttonBar)
        {
            // Add Paint button for hiding spots
            var paintGroup = CreateButtonGroup("Paint Actions", 80, buttonBar);
            var paintButton = CreateButton("Paint", paintGroup.transform, OnPaintClicked);
            
            // Call base to add settings button
            base.CreateTabSpecificButtons(buttonBar);
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
                LogDebug($"Started paint mode for {GetTabDisplayName()}", LogCategoryFlags.DebugMenu);
                
                // Hide the debug menu
                DebugMenu.DebugMenuManager.Instance?.HideMenu();
            }
            else
            {
                LogError($"No paint manager found for {GetTabDisplayName()}");
            }
        }
        
        // IDebugMenuEntityModalProvider implementation
        protected override void PopulateEntityModalForType(HidingSpot entity, GameObject modalContent, System.Action<string, object> onValueChanged)
        {
            // Create basic fields first
            base.PopulateEntityModalForType(entity, modalContent, onValueChanged);
            
            // Create HidingSpot-specific fields with custom styling
            
            // Name field - important, make it wide and prominent
            var nameOptions = TextFormFieldOptions.Wide("Name");
            nameOptions.LabelOptions.textOptions.fontSize = 14;
            nameOptions.LabelOptions.textOptions.color = new Color(0.9f, 0.9f, 1f, 1f);
            nameOptions.ContainerOptions.ImageOptions = new ImageOptions { Color = new Color(0.1f, 0.15f, 0.2f, 0.4f) };
            var nameField = FormFieldFactory.CreateTextField("Name", entity.Name, modalContent.transform, onValueChanged, nameOptions);
            
            // Position field - standard Vector3 with spatial color coding
            var positionOptions = Vector3FormFieldOptions.Default("Position");
            positionOptions.LabelOptions.textOptions.fontSize = 13;
            positionOptions.LabelOptions.textOptions.color = new Color(0.8f, 0.9f, 0.8f, 1f); // Light green for position
            positionOptions.ContainerOptions.ImageOptions = new ImageOptions { Color = new Color(0.05f, 0.12f, 0.05f, 0.3f) };
            var positionField = FormFieldFactory.CreateVector3Field("Position", entity.Position, modalContent.transform, onValueChanged, positionOptions);
            
            // Rotation field - compact since it's less commonly edited
            var rotationOptions = Vector3FormFieldOptions.Compact("Rotation (Euler)");
            rotationOptions.LabelOptions.textOptions.fontSize = 12;
            rotationOptions.LabelOptions.textOptions.color = new Color(0.9f, 0.8f, 0.7f, 1f); // Orange-ish for rotation
            rotationOptions.ContainerOptions.ImageOptions = new ImageOptions { Color = new Color(0.12f, 0.08f, 0.05f, 0.3f) };
            var rotationField = FormFieldFactory.CreateVector3Field("Rotation (Euler)", entity.Rotation.eulerAngles, modalContent.transform, onValueChanged, rotationOptions);
            
            // Register the fields with the modal
            var entityModal = modalContent.GetComponentInParent<DebugMenuEntityModal>();
            if (entityModal != null)
            {
                entityModal.RegisterFormField("Name", nameField);
                entityModal.RegisterFormField("Position", positionField);
                entityModal.RegisterFormField("Rotation", rotationField);
            }
        }

        protected override bool ApplyEntityChangesForType(HidingSpot entity, Dictionary<string, object> fieldValues)
        {
            try
            {
                
                // Log requested changes for debugging
                foreach (var field in fieldValues)
                {
                    LogDebug($"  {field.Key}: {field.Value} ({field.Value?.GetType().Name})", LogCategoryFlags.DebugMenu);
                }
                
                // Use the thread-safe request system for CRUD operations
                var updateRequest = new UpdateHidingSpotRequest(entity.Guid.ToString(), fieldValues, (updatedEntity, result) =>
                {
                    if (result == RequestResult.Succeeded)
                    {
                        LogDebug($"Successfully updated HidingSpot: {updatedEntity.Name}", LogCategoryFlags.DebugMenu);
                        // Refresh the tab to show updated data
                        Refresh();
                    }
                    else
                    {
                        LogError($"Failed to update HidingSpot: {entity.Name}");
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
                    LogError("Failed to get SubDataManager for HidingSpot update");
                    return false;
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to apply changes to HidingSpot: {e.Message}");
                return false;
            }
        }

        protected override string GetEntityModalTitleForType(HidingSpot entity)
        {
            return $"Hiding Spot Details - {entity.Name}";
        }
    }
}
