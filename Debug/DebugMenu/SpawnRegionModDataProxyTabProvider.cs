using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Il2Cpp;
using ExpandedAiFramework.UI;

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
        
        protected override void CreateTabSpecificButtons(GameObject buttonBar)
        {
            // Add Paint button for spawn regions
            var paintGroup = CreateButtonGroup("Paint Actions", 80, buttonBar);
            var paintButton = CreateButton("Paint", paintGroup.transform, OnPaintClicked);
            
            // Call base to add settings button
            base.CreateTabSpecificButtons(buttonBar);
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
        protected override void PopulateEntityModalForType(SpawnRegionModDataProxy entity, GameObject modalContent, System.Action<string, object> onValueChanged)
        {
            // Create basic fields first
            base.PopulateEntityModalForType(entity, modalContent, onValueChanged);
            
            // Create SpawnRegionModDataProxy-specific fields with organized styling
            
            // === CORE SETTINGS (Most Important) ===
            
            // Position field - prominent spatial data
            var positionOptions = Vector3FormFieldOptions.Default("Position");
            positionOptions.LabelOptions.textOptions.fontSize = 13;
            positionOptions.LabelOptions.textOptions.color = new Color(0.8f, 0.9f, 0.8f, 1f);
            positionOptions.ContainerOptions.ImageOptions = new ImageOptions { Color = new Color(0.05f, 0.15f, 0.05f, 0.4f) };
            var positionField = FormFieldFactory.CreateVector3Field("Position", entity.CurrentPosition, modalContent.transform, onValueChanged, positionOptions);
            
            // AI Type and SubType - important dropdowns, standard size
            var aiTypeOptions = DropdownFormFieldOptions.Default("AI Type");
            aiTypeOptions.LabelOptions.textOptions.fontSize = 12;
            aiTypeOptions.LabelOptions.textOptions.color = new Color(0.9f, 0.8f, 0.9f, 1f);
            var aiTypeField = FormFieldFactory.CreateDropdownField("AI Type", entity.AiType, modalContent.transform, onValueChanged, aiTypeOptions);
            
            var aiSubTypeOptions = DropdownFormFieldOptions.Default("AI SubType");
            aiSubTypeOptions.LabelOptions.textOptions.fontSize = 12;
            aiSubTypeOptions.LabelOptions.textOptions.color = new Color(0.9f, 0.8f, 0.9f, 1f);
            var aiSubTypeField = FormFieldFactory.CreateDropdownField("AI SubType", entity.AiSubType, modalContent.transform, onValueChanged, aiSubTypeOptions);
            
            var wildlifeModeOptions = DropdownFormFieldOptions.Default("Wildlife Mode");
            wildlifeModeOptions.LabelOptions.textOptions.fontSize = 12;
            var wildlifeModeField = FormFieldFactory.CreateDropdownField("Wildlife Mode", entity.WildlifeMode, modalContent.transform, onValueChanged, wildlifeModeOptions);
            
            // === STATUS TOGGLES (Important) ===
            
            // Primary status toggles - standard size
            var isActiveOptions = ToggleFormFieldOptions.Default("Is Active");
            isActiveOptions.LabelOptions.textOptions.color = new Color(0.8f, 1f, 0.8f, 1f); // Green for active status
            var isActiveField = FormFieldFactory.CreateToggleField("Is Active", entity.IsActive, modalContent.transform, onValueChanged, isActiveOptions);
            
            var connectedOptions = ToggleFormFieldOptions.Default("Connected");
            connectedOptions.LabelOptions.textOptions.color = new Color(0.8f, 0.9f, 1f, 1f); // Blue for connection
            var connectedField = FormFieldFactory.CreateToggleField("Connected", entity.Connected, modalContent.transform, onValueChanged, connectedOptions);
            
            // Secondary status toggles - compact
            var pendingOptions = ToggleFormFieldOptions.Compact("Pending Force Spawns");
            var pendingForceSpawnsField = FormFieldFactory.CreateToggleField("Pending Force Spawns", entity.PendingForceSpawns, modalContent.transform, onValueChanged, pendingOptions);
            
            var auroraDisabledOptions = ToggleFormFieldOptions.Compact("Disabled By Aurora");
            auroraDisabledOptions.LabelOptions.textOptions.color = new Color(1f, 0.8f, 0.8f, 1f); // Reddish for disabled
            var hasBeenDisabledByAuroraField = FormFieldFactory.CreateToggleField("Disabled By Aurora", entity.HasBeenDisabledByAurora, modalContent.transform, onValueChanged, auroraDisabledOptions);
            
            var wasActiveOptions = ToggleFormFieldOptions.Compact("Was Active Before Aurora");
            var wasActiveBeforeAuroraField = FormFieldFactory.CreateToggleField("Was Active Before Aurora", entity.WasActiveBeforeAurora, modalContent.transform, onValueChanged, wasActiveOptions);
            
            // === TIMING DATA (Secondary) ===
            
            // Time-related fields - compact since they're numerous
            var hoursOptions = TextFormFieldOptions.Compact("Hours Played", 45f);
            hoursOptions.LabelOptions.textOptions.color = new Color(0.9f, 0.9f, 0.7f, 1f); // Yellowish for time data
            var hoursPlayedField = FormFieldFactory.CreateTextField("Hours Played", entity.HoursPlayed.ToString("F2"), modalContent.transform, onValueChanged, hoursOptions);
            
            var despawnOptions = TextFormFieldOptions.Compact("Last Despawn Time", 45f);
            despawnOptions.LabelOptions.textOptions.color = new Color(0.9f, 0.9f, 0.7f, 1f);
            var lastDespawnTimeField = FormFieldFactory.CreateTextField("Last Despawn Time", entity.LastDespawnTime.ToString("F2"), modalContent.transform, onValueChanged, despawnOptions);
            
            var rerollOptions = TextFormFieldOptions.Compact("Elapsed Hours At Last ReRoll", 45f);
            rerollOptions.LabelOptions.textOptions.color = new Color(0.9f, 0.9f, 0.7f, 1f);
            var elapsedHoursAtLastReRollField = FormFieldFactory.CreateTextField("Elapsed Hours At Last ReRoll", entity.ElapsedHoursAtLastActiveReRoll.ToString("F2"), modalContent.transform, onValueChanged, rerollOptions);
            
            var respawnOptions = TextFormFieldOptions.Compact("Hours Until Next Respawn", 45f);
            respawnOptions.LabelOptions.textOptions.color = new Color(0.9f, 0.9f, 0.7f, 1f);
            var elapasedHoursNextRespawnField = FormFieldFactory.CreateTextField("Hours Until Next Respawn", entity.ElapasedHoursNextRespawnAllowed.ToString("F2"), modalContent.transform, onValueChanged, respawnOptions);
            
            var cooldownOptions = TextFormFieldOptions.Compact("Cooldown Timer Hours", 45f);
            cooldownOptions.LabelOptions.textOptions.color = new Color(0.9f, 0.9f, 0.7f, 1f);
            var cooldownTimerHoursField = FormFieldFactory.CreateTextField("Cooldown Timer Hours", entity.CooldownTimerHours.ToString("F2"), modalContent.transform, onValueChanged, cooldownOptions);
            
            // === COUNT DATA (Tertiary) ===
            
            // Count fields - very compact
            var pendingCountOptions = TextFormFieldOptions.Compact("Respawns Pending", 40f);
            pendingCountOptions.LabelOptions.textOptions.color = new Color(0.8f, 0.8f, 0.9f, 1f); // Light blue for counts
            var numRespawnsPendingField = FormFieldFactory.CreateTextField("Respawns Pending", entity.NumRespawnsPending.ToString(), modalContent.transform, onValueChanged, pendingCountOptions);
            
            var trappedOptions = TextFormFieldOptions.Compact("Num Trapped", 40f);
            trappedOptions.LabelOptions.textOptions.color = new Color(0.8f, 0.8f, 0.9f, 1f);
            var numTrappedField = FormFieldFactory.CreateTextField("Num Trapped", entity.NumTrapped.ToString(), modalContent.transform, onValueChanged, trappedOptions);
            
            var trapResetOptions = TextFormFieldOptions.Compact("Hours Next Trap Reset", 40f);
            trapResetOptions.LabelOptions.textOptions.color = new Color(0.8f, 0.8f, 0.9f, 1f);
            var hoursNextTrapResetField = FormFieldFactory.CreateTextField("Hours Next Trap Reset", entity.HoursNextTrapReset.ToString("F2"), modalContent.transform, onValueChanged, trapResetOptions);
            
            var waypointOptions = TextFormFieldOptions.Compact("Current Waypoint Path Index", 40f);
            waypointOptions.LabelOptions.textOptions.color = new Color(0.8f, 0.8f, 0.9f, 1f);
            var currentWaypointPathIndexField = FormFieldFactory.CreateTextField("Current Waypoint Path Index", entity.CurrentWaypointPathIndex.ToString(), modalContent.transform, onValueChanged, waypointOptions);
            
            // Register the fields with the modal
            var entityModal = modalContent.GetComponentInParent<DebugMenuEntityModal>();
            if (entityModal != null)
            {
                entityModal.RegisterFormField("Position", positionField);
                entityModal.RegisterFormField("AI Type", aiTypeField);
                entityModal.RegisterFormField("AI SubType", aiSubTypeField);
                entityModal.RegisterFormField("Wildlife Mode", wildlifeModeField);
                entityModal.RegisterFormField("Is Active", isActiveField);
                entityModal.RegisterFormField("Connected", connectedField);
                entityModal.RegisterFormField("Pending Force Spawns", pendingForceSpawnsField);
                entityModal.RegisterFormField("Disabled By Aurora", hasBeenDisabledByAuroraField);
                entityModal.RegisterFormField("Was Active Before Aurora", wasActiveBeforeAuroraField);
                entityModal.RegisterFormField("Hours Played", hoursPlayedField);
                entityModal.RegisterFormField("Last Despawn Time", lastDespawnTimeField);
                entityModal.RegisterFormField("Elapsed Hours At Last ReRoll", elapsedHoursAtLastReRollField);
                entityModal.RegisterFormField("Respawns Pending", numRespawnsPendingField);
                entityModal.RegisterFormField("Hours Until Next Respawn", elapasedHoursNextRespawnField);
                entityModal.RegisterFormField("Num Trapped", numTrappedField);
                entityModal.RegisterFormField("Hours Next Trap Reset", hoursNextTrapResetField);
                entityModal.RegisterFormField("Current Waypoint Path Index", currentWaypointPathIndexField);
                entityModal.RegisterFormField("Cooldown Timer Hours", cooldownTimerHoursField);
            }
        }

        protected override bool ApplyEntityChangesForType(SpawnRegionModDataProxy entity, Dictionary<string, object> fieldValues)
        {
            try
            {
                // Log requested changes for debugging
                foreach (var field in fieldValues)
                {
                    LogDebug($"  {field.Key}: {field.Value} ({field.Value?.GetType().Name})", LogCategoryFlags.DebugMenu);
                }
                
                // Use the thread-safe request system for CRUD operations
                var updateRequest = new UpdateSpawnRegionModDataProxyRequest(entity.Guid.ToString(), fieldValues, (updatedEntity, result) =>
                {
                    if (result == RequestResult.Succeeded)
                    {
                        LogDebug($"Successfully updated SpawnRegionModDataProxy: {updatedEntity.DisplayName}", LogCategoryFlags.DebugMenu);
                        // Refresh the tab to show updated data
                        Refresh();
                    }
                    else
                    {
                        LogError($"Failed to update SpawnRegionModDataProxy: {entity.DisplayName}");
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
                    LogError("Failed to get SubDataManager for SpawnRegionModDataProxy update");
                    return false;
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to apply changes to SpawnRegionModDataProxy: {e.Message}");
                return false;
            }
        }

        protected override string GetEntityModalTitleForType(SpawnRegionModDataProxy entity)
        {
            return $"Spawn Region Details - {entity.Guid}";
        }
    }
}
