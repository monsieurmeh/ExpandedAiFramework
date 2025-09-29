using UnityEngine;
using UnityEngine.UI;
using ExpandedAiFramework.UI;

namespace ExpandedAiFramework.DebugMenu
{
    public class SpawnModDataProxyTabProvider : DebugMenuTabContentProvider<SpawnModDataProxy>
    {
        // Mode selection
        private Button mNormalButton;
        private Button mAuroraButton;
        private WildlifeMode mCurrentMode = WildlifeMode.Normal;
        
        public override void Initialize(GameObject parentContentArea)
        {
            mSubDataManager = GetSubDataManager();
            base.Initialize(parentContentArea);
        }

        protected override void CreateTabSpecificButtons(GameObject buttonBar)
        {
            // Add Wildlife Mode selection buttons
            var modeGroup = CreateButtonGroup("Wildlife Mode", 140, buttonBar);
            mNormalButton = CreateButton("Normal", modeGroup.transform, new Action(() => SwitchMode(WildlifeMode.Normal)));
            mAuroraButton = CreateButton("Aurora", modeGroup.transform, new Action(() => SwitchMode(WildlifeMode.Aurora)));
            
            UpdateSubTabButtons();
            
            // Call base to add settings button
            base.CreateTabSpecificButtons(buttonBar);
        }

        protected override Dictionary<string, string> GetTabSettings()
        {
            var settings = new Dictionary<string, string>();
            
            // SpawnModDataProxy doesn't have a paint manager, but has wildlife mode
            settings.Add("Wildlife Mode", mCurrentMode.ToString());
            settings.Add("Scene Filter", mSceneFilter ?? "");
            settings.Add("Name Filter", mNameFilter ?? "");
            
            return settings;
        }

        protected override Dictionary<string, System.Action<string>> GetTabSettingsCallbacks()
        {
            var callbacks = new Dictionary<string, System.Action<string>>();
            
            callbacks.Add("Wildlife Mode", (value) => {
                if (System.Enum.TryParse<WildlifeMode>(value, out var mode))
                {
                    SwitchMode(mode);
                }
            });
            
            callbacks.Add("Scene Filter", (value) => {
                mSceneFilter = value;
                mSceneFilterInput.text = value;
                OnSceneFilterChanged(value);
            });
            
            callbacks.Add("Name Filter", (value) => {
                mNameFilter = value;
                mNameFilterInput.text = value;
                OnNameFilterChanged(value);
            });
            
            return callbacks;
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

        protected override ISubDataManager GetSubDataManager()
        {
            return Manager.DataManager.SpawnModDataProxyManagers[(int)mCurrentMode];
        }
        
        
        // IDebugMenuEntityModalProvider implementation
        protected override void PopulateEntityModalForType(SpawnModDataProxy entity, GameObject modalContent, System.Action<string, object> onValueChanged)
        {
            // Create basic fields first
            base.PopulateEntityModalForType(entity, modalContent, onValueChanged);
            
            // Create SpawnModDataProxy-specific fields with organized styling
            
            // === SPATIAL DATA (Most Important) ===
            
            // Position field - prominent spatial data
            var positionOptions = Vector3FormFieldOptions.Default("Position");
            positionOptions.LabelOptions.textOptions.fontSize = 13;
            positionOptions.LabelOptions.textOptions.color = new Color(0.8f, 0.9f, 0.8f, 1f);
            positionOptions.ContainerOptions.ImageOptions = new ImageOptions { Color = new Color(0.05f, 0.15f, 0.05f, 0.4f) };
            var positionField = FormFieldFactory.CreateVector3Field("Position", entity.CurrentPosition, modalContent.transform, onValueChanged, positionOptions);
            
            // Rotation field - compact since less commonly edited
            var rotationOptions = Vector3FormFieldOptions.Compact("Rotation (Euler)");
            rotationOptions.LabelOptions.textOptions.fontSize = 12;
            rotationOptions.LabelOptions.textOptions.color = new Color(0.9f, 0.8f, 0.7f, 1f);
            rotationOptions.ContainerOptions.ImageOptions = new ImageOptions { Color = new Color(0.12f, 0.08f, 0.05f, 0.3f) };
            var rotationField = FormFieldFactory.CreateVector3Field("Rotation (Euler)", entity.CurrentRotation.eulerAngles, modalContent.transform, onValueChanged, rotationOptions);

            // === AI CONFIGURATION (Important) ===
            
            // AI Type dropdowns - standard size with distinct colors
            var aiSubTypeOptions = DropdownFormFieldOptions.Default("AI SubType");
            aiSubTypeOptions.LabelOptions.textOptions.fontSize = 12;
            aiSubTypeOptions.LabelOptions.textOptions.color = new Color(0.9f, 0.8f, 0.9f, 1f);
            var aiSubTypeField = FormFieldFactory.CreateDropdownField("AI SubType", entity.AiSubType, modalContent.transform, onValueChanged, aiSubTypeOptions);
            
            var aiModeOptions = DropdownFormFieldOptions.Default("AI Mode");
            aiModeOptions.LabelOptions.textOptions.fontSize = 12;
            aiModeOptions.LabelOptions.textOptions.color = new Color(0.8f, 0.9f, 0.9f, 1f);
            var aiModeField = FormFieldFactory.CreateDropdownField("AI Mode", entity.AiMode, modalContent.transform, onValueChanged, aiModeOptions);
            
            var wildlifeModeOptions = DropdownFormFieldOptions.Default("Wildlife Mode");
            wildlifeModeOptions.LabelOptions.textOptions.fontSize = 12;
            var wildlifeModeField = FormFieldFactory.CreateDropdownField("Wildlife Mode", entity.WildlifeMode, modalContent.transform, onValueChanged, wildlifeModeOptions);
            
            // === STATUS TOGGLES (Important) ===
            
            // Force Spawn - prominent since it's a key action
            var forceSpawnOptions = ToggleFormFieldOptions.Default("Force Spawn");
            forceSpawnOptions.LabelOptions.textOptions.color = new Color(1f, 0.9f, 0.7f, 1f); // Orange for action
            forceSpawnOptions.LabelOptions.textOptions.fontSize = 12;
            var forceSpawnField = FormFieldFactory.CreateToggleField("Force Spawn", entity.ForceSpawn, modalContent.transform, onValueChanged, forceSpawnOptions);
            
            // Status toggles - standard size with semantic colors
            var availableOptions = ToggleFormFieldOptions.Default("Available");
            availableOptions.LabelOptions.textOptions.color = new Color(0.8f, 1f, 0.8f, 1f); // Green for available
            var availableField = FormFieldFactory.CreateToggleField("Available", entity.Available, modalContent.transform, onValueChanged, availableOptions);
            
            var spawnedOptions = ToggleFormFieldOptions.Default("Spawned");
            spawnedOptions.LabelOptions.textOptions.color = new Color(0.8f, 0.9f, 1f, 1f); // Blue for spawned
            var spawnedField = FormFieldFactory.CreateToggleField("Spawned", entity.Spawned, modalContent.transform, onValueChanged, spawnedOptions);
            
            var disconnectedOptions = ToggleFormFieldOptions.Compact("Disconnected");
            disconnectedOptions.LabelOptions.textOptions.color = new Color(1f, 0.8f, 0.8f, 1f); // Red for disconnected
            var disconnectedField = FormFieldFactory.CreateToggleField("Disconnected", entity.Disconnected, modalContent.transform, onValueChanged, disconnectedOptions);
            
            // === REFERENCE DATA (Secondary) ===
            
            // Parent GUID - compact since it's mostly reference
            var parentGuidOptions = TextFormFieldOptions.Compact("Parent GUID", 45f);
            parentGuidOptions.LabelOptions.textOptions.color = new Color(0.8f, 0.8f, 0.9f, 1f);
            var parentGuidField = FormFieldFactory.CreateTextField("Parent GUID", entity.ParentGuid.ToString(), modalContent.transform, onValueChanged, parentGuidOptions);
            
            // Variant Spawn Type - read-only, very compact
            var variantOptions = TextFormFieldOptions.Compact("Variant Spawn Type", 40f);
            variantOptions.InputOptions.backgroundImageOptions = new ImageOptions { Color = new Color(0.15f, 0.15f, 0.2f, 1f) }; // Darker for read-only
            variantOptions.LabelOptions.textOptions.color = new Color(0.7f, 0.7f, 0.8f, 1f);
            variantOptions.LabelOptions.textOptions.fontSize = 10;
            var variantSpawnTypeField = FormFieldFactory.CreateTextField("Variant Spawn Type", entity.VariantSpawnTypeString, modalContent.transform, null, variantOptions, true);
            
            // Last Despawn Time - compact timing data
            var despawnOptions = TextFormFieldOptions.Compact("Last Despawn Time", 45f);
            despawnOptions.LabelOptions.textOptions.color = new Color(0.9f, 0.9f, 0.7f, 1f); // Yellowish for time
            var lastDespawnTimeField = FormFieldFactory.CreateTextField("Last Despawn Time", entity.LastDespawnTime.ToString("F2"), modalContent.transform, onValueChanged, despawnOptions);
            
            // Register the fields with the modal
            var entityModal = modalContent.GetComponentInParent<DebugMenuEntityModal>();
            if (entityModal != null)
            {
                entityModal.RegisterFormField("Position", positionField);
                entityModal.RegisterFormField("Rotation", rotationField);
                entityModal.RegisterFormField("AI SubType", aiSubTypeField);
                entityModal.RegisterFormField("AI Mode", aiModeField);
                entityModal.RegisterFormField("Wildlife Mode", wildlifeModeField);
                entityModal.RegisterFormField("Force Spawn", forceSpawnField);
                entityModal.RegisterFormField("Available", availableField);
                entityModal.RegisterFormField("Spawned", spawnedField);
                entityModal.RegisterFormField("Disconnected", disconnectedField);
                entityModal.RegisterFormField("Parent GUID", parentGuidField);
                entityModal.RegisterFormField("Variant Spawn Type", variantSpawnTypeField);
                entityModal.RegisterFormField("Last Despawn Time", lastDespawnTimeField);
            }
        }

        protected override bool ApplyEntityChangesForType(SpawnModDataProxy entity, Dictionary<string, object> fieldValues)
        {
            try
            {
                // Log requested changes for debugging
                foreach (var field in fieldValues)
                {
                    LogDebug($"  {field.Key}: {field.Value} ({field.Value?.GetType().Name})");
                }
                
                // Use the thread-safe request system for CRUD operations
                var updateRequest = new UpdateSpawnModDataProxyRequest(entity.Guid.ToString(), fieldValues, (updatedEntity, result) =>
                {
                    if (result == RequestResult.Succeeded)
                    {
                        LogDebug($"Successfully updated SpawnModDataProxy: {updatedEntity.DisplayName}");
                        // Refresh the tab to show updated data
                        Refresh();
                    }
                    else
                    {
                        LogError($"Failed to update SpawnModDataProxy: {entity.DisplayName}");
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
                    LogError("Failed to get SubDataManager for SpawnModDataProxy update");
                    return false;
                }
            }
            catch (Exception e)
            {
                LogError($"Failed to apply changes to SpawnModDataProxy: {e.Message}");
                return false;
            }
        }

        protected override string GetEntityModalTitleForType(SpawnModDataProxy entity)
        {
            return $"Spawn Data Details - {entity.DisplayName}";
        }
    }
}
