using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Il2Cpp;

namespace ExpandedAiFramework
{
    // Base request for debug menu queries - extends DataRequest<T> for proper thread safety
    public abstract class DebugMenuRequest<T> : DataRequest<T> where T : ISerializedData, new()
    {
        protected DebugMenuRequest(Action<T, RequestResult> callback, bool threadSafe = true, bool threadSafeCallback = false) 
            : base(callback, threadSafe, threadSafeCallback) { }
    }

    // List-based request for debug menu queries that return multiple items
    public abstract class DebugMenuListRequest<T> : Request where T : ISerializedData, new()
    {
        protected Action<List<T>, RequestResult> mCallback;
        protected SerializedDataContainer<T> mDataContainer;
        protected List<T> mResults;

        public override string InstanceInfo { get { return string.Empty; } }
        public override string TypeInfo { get { return $"DebugMenuListRequest<{typeof(T)}>"; } }

        protected DebugMenuListRequest(Action<List<T>, RequestResult> callback, bool threadSafe = true, bool threadSafeCallback = false) 
            : base(threadSafe, threadSafeCallback) 
        {
            mCallback = callback;
            mResults = new List<T>();
        }

        public override void Callback() => mCallback?.Invoke(mResults, mResult);

        public override void Preprocess(ISubDataManager manager)
        {
            if (manager is ISerializedDataProvider<T> dataProvider)
            {
                mDataContainer = dataProvider.GetDataContainer();
            }
        }

        protected override bool Validate()
        {
            if (mDataContainer == null)
            {
                // Validation failed
                return false;
            }
            return ValidateInternal();
        }

        protected virtual bool ValidateInternal() => true;
    }

    // Request to get all SpawnModDataProxies with filtering
    public class GetSpawnModDataProxiesRequest : DebugMenuListRequest<SpawnModDataProxy>
    {
        private WildlifeMode mWildlifeMode;
        private string mSceneFilter;
        private AiSubType? mAiSubTypeFilter;

        public override string InstanceInfo { get { return $"Wildlife:{mWildlifeMode} Scene:{mSceneFilter ?? "All"} AiSubType:{mAiSubTypeFilter?.ToString() ?? "All"}"; } }

        public GetSpawnModDataProxiesRequest(
            WildlifeMode wildlifeMode, 
            Action<List<SpawnModDataProxy>, RequestResult> callback,
            string sceneFilter = null,
            AiSubType? aiSubTypeFilter = null,
            bool callbackIsThreadSafe = false) : base(callback, true, callbackIsThreadSafe)
        {
            mWildlifeMode = wildlifeMode;
            mSceneFilter = sceneFilter;
            mAiSubTypeFilter = aiSubTypeFilter;
        }

        protected override RequestResult PerformRequestInternal()
        {
            var allData = mDataContainer.EnumerateContents();
            mResults.Clear();

            foreach (var proxy in allData)
            {
                if (proxy.WildlifeMode != mWildlifeMode)
                    continue;

                if (!string.IsNullOrEmpty(mSceneFilter) && !proxy.Scene.Contains(mSceneFilter))
                    continue;

                if (mAiSubTypeFilter.HasValue && proxy.AiSubType != mAiSubTypeFilter.Value)
                    continue;

                mResults.Add(proxy);
            }

            return RequestResult.Succeeded;
        }
    }

    // Request to get all SpawnRegionModDataProxies with filtering
    public class GetSpawnRegionModDataProxiesRequest : DebugMenuListRequest<SpawnRegionModDataProxy>
    {
        private string mSceneFilter;
        private AiType? mAiTypeFilter;
        private AiSubType? mAiSubTypeFilter;

        public override string InstanceInfo { get { return $"Scene:{mSceneFilter ?? "All"} AiType:{mAiTypeFilter?.ToString() ?? "All"} AiSubType:{mAiSubTypeFilter?.ToString() ?? "All"}"; } }

        public GetSpawnRegionModDataProxiesRequest(
            Action<List<SpawnRegionModDataProxy>, RequestResult> callback,
            string sceneFilter = null,
            AiType? aiTypeFilter = null,
            AiSubType? aiSubTypeFilter = null,
            bool callbackIsThreadSafe = false) : base(callback, true, callbackIsThreadSafe)
        {
            mSceneFilter = sceneFilter;
            mAiTypeFilter = aiTypeFilter;
            mAiSubTypeFilter = aiSubTypeFilter;
        }

        protected override RequestResult PerformRequestInternal()
        {
            var allData = mDataContainer.EnumerateContents();
            mResults.Clear();

            foreach (var proxy in allData)
            {
                if (!string.IsNullOrEmpty(mSceneFilter) && !proxy.Scene.Contains(mSceneFilter))
                    continue;

                if (mAiTypeFilter.HasValue && proxy.AiType != mAiTypeFilter.Value)
                    continue;

                if (mAiSubTypeFilter.HasValue && proxy.AiSubType != mAiSubTypeFilter.Value)
                    continue;

                mResults.Add(proxy);
            }

            return RequestResult.Succeeded;
        }
    }

    // Request to get HidingSpots with filtering
    public class GetHidingSpotsRequest : DebugMenuListRequest<HidingSpot>
    {
        private string mSceneFilter;
        private string mNameFilter;

        public override string InstanceInfo { get { return $"Scene:{mSceneFilter ?? "All"} Name:{mNameFilter ?? "All"}"; } }

        public GetHidingSpotsRequest(
            Action<List<HidingSpot>, RequestResult> callback,
            string sceneFilter = null,
            string nameFilter = null,
            bool callbackIsThreadSafe = false) : base(callback, true, callbackIsThreadSafe)
        {
            mSceneFilter = sceneFilter;
            mNameFilter = nameFilter;
        }

        protected override RequestResult PerformRequestInternal()
        {
            var allData = mDataContainer.EnumerateContents();
            mResults.Clear();

            foreach (var spot in allData)
            {
                if (!string.IsNullOrEmpty(mSceneFilter) && !spot.Scene.Contains(mSceneFilter))
                    continue;

                if (!string.IsNullOrEmpty(mNameFilter) && !spot.Name.Contains(mNameFilter))
                    continue;

                mResults.Add(spot);
            }

            return RequestResult.Succeeded;
        }
    }

    // Request to get WanderPaths with filtering
    public class GetWanderPathsRequest : DebugMenuListRequest<WanderPath>
    {
        private string mSceneFilter;
        private string mNameFilter;
        private WanderPathTypes? mTypeFilter;

        public override string InstanceInfo { get { return $"Scene:{mSceneFilter ?? "All"} Name:{mNameFilter ?? "All"} Type:{mTypeFilter?.ToString() ?? "All"}"; } }

        public GetWanderPathsRequest(
            Action<List<WanderPath>, RequestResult> callback,
            string sceneFilter = null,
            string nameFilter = null,
            WanderPathTypes? typeFilter = null,
            bool callbackIsThreadSafe = false) : base(callback, true, callbackIsThreadSafe)
        {
            mSceneFilter = sceneFilter;
            mNameFilter = nameFilter;
            mTypeFilter = typeFilter;
        }

        protected override RequestResult PerformRequestInternal()
        {
            var allData = mDataContainer.EnumerateContents();
            mResults.Clear();

            foreach (var path in allData)
            {
                if (!string.IsNullOrEmpty(mSceneFilter) && !path.Scene.Contains(mSceneFilter))
                    continue;

                if (!string.IsNullOrEmpty(mNameFilter) && !path.Name.Contains(mNameFilter))
                    continue;

                if (mTypeFilter.HasValue && path.WanderPathType != mTypeFilter.Value)
                    continue;

                mResults.Add(path);
            }

            return RequestResult.Succeeded;
        }
    }

    // Request to update SpawnModDataProxy with field-based changes
    public class UpdateSpawnModDataProxyRequest : DebugMenuRequest<SpawnModDataProxy>
    {
        private string mProxyGuid;
        private Dictionary<string, object> mFieldValues;

        public override string InstanceInfo { get { return $"Proxy:{mProxyGuid}"; } }

        public UpdateSpawnModDataProxyRequest(string proxyGuid, Dictionary<string, object> fieldValues, Action<SpawnModDataProxy, RequestResult> callback, bool callbackIsThreadSafe = false) 
            : base(callback, true, callbackIsThreadSafe)
        {
            mProxyGuid = proxyGuid;
            mFieldValues = fieldValues;
        }

        protected override bool Validate()
        {
            if (mDataContainer == null)
            {
                return false;
            }
            if (string.IsNullOrEmpty(mProxyGuid))
            {
                return false;
            }
            if (mFieldValues == null || mFieldValues.Count == 0)
            {
                return false;
            }
            return true;
        }

        protected override RequestResult PerformRequestInternal()
        {
            // Find the proxy in the container by GUID
            SpawnModDataProxy proxy = null;
            if (Guid.TryParse(mProxyGuid, out Guid guid))
            {
                foreach (var item in mDataContainer.EnumerateContents())
                {
                    if (item.Guid == guid)
                    {
                        proxy = item;
                        break;
                    }
                }
            }
            
            if (proxy == null)
            {
                // Entity not found
                return RequestResult.Failed;
            }

            try
            {
                // Apply field changes to the proxy in worker thread
                if (mFieldValues.TryGetValue("Position", out var positionValue) && positionValue is Vector3 position)
                {
                    proxy.CurrentPosition = position;
                }

                if (mFieldValues.TryGetValue("Rotation (Euler)", out var rotationValue) && rotationValue is Vector3 rotation)
                {
                    proxy.CurrentRotation = Quaternion.Euler(rotation);
                }

                if (mFieldValues.TryGetValue("AiSubType", out var aiSubTypeValue) && aiSubTypeValue is AiSubType aiSubType)
                {
                    proxy.AiSubType = aiSubType;
                }

                if (mFieldValues.TryGetValue("AiMode", out var aiModeValue) && aiModeValue is AiMode aiMode)
                {
                    proxy.AiMode = aiMode;
                }

                if (mFieldValues.TryGetValue("WildlifeMode", out var wildlifeModeValue) && wildlifeModeValue is WildlifeMode wildlifeMode)
                {
                    proxy.WildlifeMode = wildlifeMode;
                }

                if (mFieldValues.TryGetValue("ForceSpawn", out var forceSpawnValue) && forceSpawnValue is bool forceSpawn)
                {
                    proxy.ForceSpawn = forceSpawn;
                }

                if (mFieldValues.TryGetValue("Available", out var availableValue) && availableValue is bool available)
                {
                    proxy.Available = available;
                }

                if (mFieldValues.TryGetValue("Spawned", out var spawnedValue) && spawnedValue is bool spawned)
                {
                    proxy.Spawned = spawned;
                }

                if (mFieldValues.TryGetValue("Disconnected", out var disconnectedValue) && disconnectedValue is bool disconnected)
                {
                    proxy.Disconnected = disconnected;
                }

                if (mFieldValues.TryGetValue("Parent GUID", out var parentGuidValue) && parentGuidValue is string parentGuidStr)
                {
                    if (Guid.TryParse(parentGuidStr, out Guid parentGuid))
                    {
                        proxy.ParentGuid = parentGuid;
                    }
                }

                if (mFieldValues.TryGetValue("Last Despawn Time", out var despawnTimeValue) && despawnTimeValue is string despawnTime)
                {
                    if (float.TryParse(despawnTime, out float despawnFloat))
                    {
                        proxy.LastDespawnTime = despawnFloat;
                    }
                }

                // Handle generic ISerializedData fields (from ApplyGenericEntityChanges)
                if (mFieldValues.TryGetValue("Data Location", out var dataLocationValue) && dataLocationValue is string dataLocation)
                {
                    proxy.DataLocation = dataLocation;
                }

                // Note: Scene is typically read-only in most implementations
                if (mFieldValues.TryGetValue("Scene", out var sceneValue) && sceneValue is string sceneString)
                {
                    // Scene changes are typically not supported
                }

                mPayload = proxy;
                return RequestResult.Succeeded;
            }
            catch (Exception)
            {
                // Update failed
                return RequestResult.Failed;
            }
        }
    }

    // Request to update SpawnRegionModDataProxy with field-based changes
    public class UpdateSpawnRegionModDataProxyRequest : DebugMenuRequest<SpawnRegionModDataProxy>
    {
        private string mProxyGuid;
        private Dictionary<string, object> mFieldValues;

        public override string InstanceInfo { get { return $"Proxy:{mProxyGuid}"; } }

        public UpdateSpawnRegionModDataProxyRequest(string proxyGuid, Dictionary<string, object> fieldValues, Action<SpawnRegionModDataProxy, RequestResult> callback, bool callbackIsThreadSafe = false) 
            : base(callback, true, callbackIsThreadSafe)
        {
            mProxyGuid = proxyGuid;
            mFieldValues = fieldValues;
        }

        protected override bool Validate()
        {
            if (mDataContainer == null)
            {
                // Validation failed
                return false;
            }
            if (string.IsNullOrEmpty(mProxyGuid))
            {
                // Validation failed
                return false;
            }
            if (mFieldValues == null || mFieldValues.Count == 0)
            {
                // Validation failed
                return false;
            }
            return true;
        }

        protected override RequestResult PerformRequestInternal()
        {
            // Find the proxy in the container by GUID
            SpawnRegionModDataProxy proxy = null;
            if (Guid.TryParse(mProxyGuid, out Guid guid))
            {
                foreach (var item in mDataContainer.EnumerateContents())
                {
                    if (item.Guid == guid)
                    {
                        proxy = item;
                        break;
                    }
                }
            }
            
            if (proxy == null)
            {
                // Entity not found
                return RequestResult.Failed;
            }

            try
            {
                // Apply field changes in worker thread
                if (mFieldValues.TryGetValue("Position", out var positionValue) && positionValue is Vector3 position)
                {
                    proxy.CurrentPosition = position;
                }

                if (mFieldValues.TryGetValue("AiType", out var aiTypeValue) && aiTypeValue is AiType aiType)
                {
                    proxy.AiType = aiType;
                }

                if (mFieldValues.TryGetValue("AiSubType", out var aiSubTypeValue) && aiSubTypeValue is AiSubType aiSubType)
                {
                    proxy.AiSubType = aiSubType;
                }

                if (mFieldValues.TryGetValue("WildlifeMode", out var wildlifeModeValue) && wildlifeModeValue is WildlifeMode wildlifeMode)
                {
                    proxy.WildlifeMode = wildlifeMode;
                }

                if (mFieldValues.TryGetValue("Is Active", out var isActiveValue) && isActiveValue is bool isActive)
                {
                    proxy.IsActive = isActive;
                }

                if (mFieldValues.TryGetValue("Connected", out var connectedValue) && connectedValue is bool connected)
                {
                    proxy.Connected = connected;
                }

                if (mFieldValues.TryGetValue("Pending Force Spawns", out var pendingValue) && pendingValue is bool pending)
                {
                    proxy.PendingForceSpawns = pending;
                }

                if (mFieldValues.TryGetValue("HasBeenDisabledByAurora", out var disabledValue) && disabledValue is bool disabled)
                {
                    proxy.HasBeenDisabledByAurora = disabled;
                }

                if (mFieldValues.TryGetValue("WasActiveBeforeAurora", out var wasActiveValue) && wasActiveValue is bool wasActive)
                {
                    proxy.WasActiveBeforeAurora = wasActive;
                }

                // Handle numeric fields with string parsing
                if (mFieldValues.TryGetValue("Hours Played", out var hoursValue) && hoursValue is string hoursStr && float.TryParse(hoursStr, out float hours))
                {
                    proxy.HoursPlayed = hours;
                }

                if (mFieldValues.TryGetValue("Last Despawn Time", out var despawnValue) && despawnValue is string despawnStr && float.TryParse(despawnStr, out float despawn))
                {
                    proxy.LastDespawnTime = despawn;
                }

                if (mFieldValues.TryGetValue("Cooldown Timer Hours", out var cooldownValue) && cooldownValue is string cooldownStr && float.TryParse(cooldownStr, out float cooldown))
                {
                    proxy.CooldownTimerHours = cooldown;
                }

                // Note: MaxSpawns and NumSpawns properties don't exist on SpawnRegionModDataProxy
                // These would need to be added to the data structure if needed

                // Handle generic ISerializedData fields (from ApplyGenericEntityChanges)
                if (mFieldValues.TryGetValue("Data Location", out var dataLocationValue) && dataLocationValue is string dataLocation)
                {
                    proxy.DataLocation = dataLocation;
                }

                // Note: Scene is typically read-only in most implementations
                if (mFieldValues.TryGetValue("Scene", out var sceneValue) && sceneValue is string sceneString)
                {
                    // Scene changes are typically not supported
                }

                mPayload = proxy;
                return RequestResult.Succeeded;
            }
            catch (Exception)
            {
                // Update failed
                return RequestResult.Failed;
            }
        }
    }

    // Request to update HidingSpot with field-based changes
    public class UpdateHidingSpotRequest : DebugMenuRequest<HidingSpot>
    {
        private string mHidingSpotGuid;
        private Dictionary<string, object> mFieldValues;

        public override string InstanceInfo { get { return $"HidingSpot:{mHidingSpotGuid}"; } }

        public UpdateHidingSpotRequest(string hidingSpotGuid, Dictionary<string, object> fieldValues, Action<HidingSpot, RequestResult> callback, bool callbackIsThreadSafe = false) 
            : base(callback, true, callbackIsThreadSafe)
        {
            mHidingSpotGuid = hidingSpotGuid;
            mFieldValues = fieldValues;
        }

        protected override bool Validate()
        {
            if (mDataContainer == null)
            {
                // Validation failed
                return false;
            }
            if (string.IsNullOrEmpty(mHidingSpotGuid))
            {
                // Validation failed
                return false;
            }
            if (mFieldValues == null || mFieldValues.Count == 0)
            {
                // Validation failed
                return false;
            }
            return true;
        }

        protected override RequestResult PerformRequestInternal()
        {
            // Find the hiding spot in the container by GUID
            HidingSpot hidingSpot = null;
            if (Guid.TryParse(mHidingSpotGuid, out Guid guid))
            {
                foreach (var item in mDataContainer.EnumerateContents())
                {
                    if (item.Guid == guid)
                    {
                        hidingSpot = item;
                        break;
                    }
                }
            }
            
            if (hidingSpot == null)
            {
                // Entity not found
                return RequestResult.Failed;
            }

            try
            {
                // Apply field changes in worker thread
                // Note: Name, Position, and Rotation are read-only properties on HidingSpot
                // These would require direct field access or making the properties settable
                if (mFieldValues.TryGetValue("Name", out var nameValue) && nameValue is string name)
                {
                    // hidingSpot.Name = name; // Read-only property
                    // Name property is read-only
                }

                if (mFieldValues.TryGetValue("Position", out var positionValue) && positionValue is Vector3 position)
                {
                    // hidingSpot.Position = position; // Read-only property
                    // Position property is read-only
                }

                if (mFieldValues.TryGetValue("Rotation (Euler)", out var rotationValue) && rotationValue is Vector3 rotation)
                {
                    // hidingSpot.Rotation = Quaternion.Euler(rotation); // Read-only property
                    // Rotation property is read-only
                }

                // Handle generic ISerializedData fields (from ApplyGenericEntityChanges)
                if (mFieldValues.TryGetValue("Data Location", out var dataLocationValue) && dataLocationValue is string dataLocation)
                {
                    hidingSpot.DataLocation = dataLocation;
                }

                // Note: Scene is typically read-only in most implementations
                if (mFieldValues.TryGetValue("Scene", out var sceneValue) && sceneValue is string sceneString)
                {
                    // Scene changes are typically not supported
                }

                mPayload = hidingSpot;
                return RequestResult.Succeeded;
            }
            catch (Exception)
            {
                // Update failed
                return RequestResult.Failed;
            }
        }
    }

    // Request to update WanderPath with field-based changes
    public class UpdateWanderPathRequest : DebugMenuRequest<WanderPath>
    {
        private string mWanderPathGuid;
        private Dictionary<string, object> mFieldValues;

        public override string InstanceInfo { get { return $"WanderPath:{mWanderPathGuid}"; } }

        public UpdateWanderPathRequest(string wanderPathGuid, Dictionary<string, object> fieldValues, Action<WanderPath, RequestResult> callback, bool callbackIsThreadSafe = false) 
            : base(callback, true, callbackIsThreadSafe)
        {
            mWanderPathGuid = wanderPathGuid;
            mFieldValues = fieldValues;
        }

        protected override bool Validate()
        {
            if (mDataContainer == null)
            {
                // Validation failed
                return false;
            }
            if (string.IsNullOrEmpty(mWanderPathGuid))
            {
                // Validation failed
                return false;
            }
            if (mFieldValues == null || mFieldValues.Count == 0)
            {
                // Validation failed
                return false;
            }
            return true;
        }

        protected override RequestResult PerformRequestInternal()
        {
            // Find the wander path in the container by GUID
            WanderPath wanderPath = null;
            if (Guid.TryParse(mWanderPathGuid, out Guid guid))
            {
                foreach (var item in mDataContainer.EnumerateContents())
                {
                    if (item.Guid == guid)
                    {
                        wanderPath = item;
                        break;
                    }
                }
            }
            
            if (wanderPath == null)
            {
                // Entity not found
                return RequestResult.Failed;
            }

            try
            {
                // Apply field changes in worker thread
                // Note: Name and WanderPathType are read-only properties on WanderPath
                if (mFieldValues.TryGetValue("Name", out var nameValue) && nameValue is string name)
                {
                    // wanderPath.Name = name; // Read-only property
                    // Name property is read-only
                }

                if (mFieldValues.TryGetValue("Wander Path Type", out var typeValue) && typeValue is WanderPathTypes pathType)
                {
                    // wanderPath.WanderPathType = pathType; // Read-only property
                    // WanderPathType property is read-only
                }

                // Handle generic ISerializedData fields (from ApplyGenericEntityChanges)
                if (mFieldValues.TryGetValue("Data Location", out var dataLocationValue) && dataLocationValue is string dataLocation)
                {
                    wanderPath.DataLocation = dataLocation;
                }

                // Note: Scene is typically read-only in most implementations
                if (mFieldValues.TryGetValue("Scene", out var sceneValue) && sceneValue is string sceneString)
                {
                    // Scene changes are typically not supported
                }

                // Update path points (first 5 editable)
                // Note: PathPoints array is read-only, but individual elements can be modified
                for (int i = 0; i < 5 && i < wanderPath.PathPoints.Length; i++)
                {
                    if (mFieldValues.TryGetValue($"Path Point {i}", out var pointValue) && pointValue is Vector3 point)
                    {
                        wanderPath.PathPoints[i] = point;
                    }
                }

                mPayload = wanderPath;
                return RequestResult.Succeeded;
            }
            catch (Exception)
            {
                // Update failed
                return RequestResult.Failed;
            }
        }
    }
}
