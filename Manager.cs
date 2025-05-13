using MelonLoader.TinyJSON;
using UnityEngine;
using System.Text;
using MelonLoader.Utils;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using ComplexLogger;
using Il2Cpp;
using Il2CppRewired.Config;
using static Il2CppParadoxNotion.Services.Logger;


namespace ExpandedAiFramework
{
    public class Manager
    {
        public const string ModName = "Expanded Ai Framework";

        #region Lazy Singleton

        private class Nested
        {
            static Nested()
            {
            }

            internal static readonly Manager instance = new Manager();
        }

        private Manager() { }
        public static Manager Instance { get { return Nested.instance; } }

        #endregion


        #region Internal stuff

        private FlaggedLoggingLevel mLoggerLevel = 0U; 
        private ComplexLogger<Main> mLogger;
        private ExpandedAiFrameworkSettings mSettings;
        private Dictionary<int, ICustomAi> mAiAugments = new Dictionary<int, ICustomAi>();
        private WeightedTypePicker<BaseAi> mTypePicker = new WeightedTypePicker<BaseAi>();
        private Dictionary<Type, TypeSpecificSettings> mModSettingsDict = new Dictionary<Type, TypeSpecificSettings>();

        public ExpandedAiFrameworkSettings Settings { get { return mSettings; } }
        public Dictionary<int, ICustomAi> AiAugments { get { return mAiAugments; } }
        public WeightedTypePicker<BaseAi> TypePicker { get { return mTypePicker; } }
        public Dictionary<Type, TypeSpecificSettings> ModSettingsDict { get { return mModSettingsDict; } }
        public FlaggedLoggingLevel LoggerLevel { get { return mLoggerLevel; } set { mLoggerLevel = value; } }

        public void Initialize(ExpandedAiFrameworkSettings settings)
        {
            mSettings = settings;
            InitializeLogger();
            RegisterSpawnableAi(typeof(BaseWolf), BaseWolf.Settings);
            LoadMapData();
        }


        public void Shutdown()
        {
            SaveMapData();
            ClearAugments();
            ClearMapData();
        }

        #endregion


        #region API

        [HideFromIl2Cpp]
        public bool RegisterSpawnableAi(Type type, TypeSpecificSettings modSettings)
        {
            if (mModSettingsDict.TryGetValue(type, out _))
            {
                LogError($"Can't register {type} as it is already registered!", FlaggedLoggingLevel.Critical);
                return false;
            }
            Log($"Registering type {type}:", FlaggedLoggingLevel.Always, true);
            modSettings.AddToModSettings(ModName);
            modSettings.ShowSettingsIfEnabled();
            modSettings.RefreshGUI();
            mModSettingsDict.Add(type, modSettings);
            mTypePicker.AddWeight(type, modSettings.GetSpawnWeight, modSettings.CanSpawn);
            return true;
        }


        public void ClearAugments()
        {
            foreach (ICustomAi customAi in mAiAugments.Values)
            {
                TryUnaugment(customAi.BaseAi);
            }
            mAiAugments.Clear();
        }



        public bool TryAugment(BaseAi baseAi)
        {
            if (baseAi == null)
            {
                LogDebug("Null base ai, can't augment.");
                return false;
            }
            if (baseAi.m_AiSubType != AiSubType.Wolf)
            {
                LogDebug("BaseAi is not wolf, cannot augment.");//todo: add non wolf scripts... one day...
                return false;
            }
            if (mAiAugments.ContainsKey(baseAi.GetHashCode()))
            {
                LogDebug("BaseAi in dictionary, can't augment.");
                return false;
            }


#if DEV_BUILD_SPAWNONE
            if (mSpawnedOne)
            {
                return false;
            }
#endif
            Il2CppSystem.Type spawnType = Il2CppType.From(mTypePicker.PickType(baseAi));
            if (spawnType == Il2CppType.From(typeof(void)))
            {
                LogError($"Unable to resolve a custom spawn type from weighted type picker!", FlaggedLoggingLevel.Critical);                return false;
            }
            AugmentAi(baseAi, spawnType);
            return true;
        }


        public bool TryUnaugment(BaseAi baseAI)
        {
            if (baseAI == null)
            {
                return false;
            }
            if (!mAiAugments.ContainsKey(baseAI.GetHashCode()))
            {
                return false;
            }
            UnaugmentAi(baseAI.GetHashCode());
            return true;
        }

        /*
        public bool TryUpdate(BaseAi baseAi)
        {
            if (!AiAugments.TryGetValue(baseAi.GetHashCode(), out ICustomAi ai))
            {
                return false;
            }
            ai.Update();
            return true;
        }
        */

        public bool TrySetAiMode(BaseAi baseAi, AiMode aiMode)
        {
            if (!AiAugments.TryGetValue(baseAi.GetHashCode(), out ICustomAi customAi))
            {
                return false;
            }
            customAi.SetAiMode(aiMode);
            return true;
        }


        public bool TryApplyDamage(BaseAi baseAi, float damage, float bleedOutTime, DamageSource damageSource)
        {
            if (!AiAugments.TryGetValue(baseAi.GetHashCode(), out ICustomAi customAi))
            {
                return false;
            }
            customAi.ApplyDamage(damage, bleedOutTime, damageSource);
            return true;
        }


        #endregion


        #region Internal Methods

        private void AugmentAi(BaseAi baseAi, Il2CppSystem.Type spawnType)
        {
            LogDebug($"Spawning {spawnType.Name} at {baseAi.gameObject.transform.position}");
            mAiAugments.Add(baseAi.GetHashCode(), (ICustomAi)baseAi.gameObject.AddComponent(spawnType));
            if (!mAiAugments.TryGetValue(baseAi.GetHashCode(), out ICustomAi customAi))
            {
                LogError($"Critical error at ExpandedAiFramework.AugmentAi: newly created {spawnType} cannot be found in augment dictionary! Did its hash code change?", FlaggedLoggingLevel.Critical);
                return;
            }
            customAi.Initialize(baseAi, GameManager.m_TimeOfDay);
            customAi.Augment();
#if DEV_BUILD_SPAWNONE
                mSpawnedOne = true;
#endif
        }


        private void UnaugmentAi(int hashCode)
        {
            if (mAiAugments.TryGetValue(hashCode, out ICustomAi customAi))
            {
                customAi.UnAugment();
                UnityEngine.Object.Destroy(customAi.Self.gameObject); //if I'm converting back from the interface to destroy it, is there really any point to the interface? We should be demanding people use CustomBaseAi instead...
                mAiAugments.Remove(hashCode);
            }
        }

        #endregion


        #region Path & Location Management

        private Dictionary<string, List<HidingSpot>> mHidingSpots = new Dictionary<string, List<HidingSpot>>();
        private Dictionary<string, List<WanderPath>> mWanderPaths = new Dictionary<string, List<WanderPath>>(); 
        private List<HidingSpot> mAvailableHidingSpots = new List<HidingSpot>();
        private List<WanderPath> mAvailableWanderPaths = new List<WanderPath>();

        public Dictionary<string, List<HidingSpot>> HidingSpots { get { return mHidingSpots; } }
        public Dictionary<string, List<WanderPath>> WanderPaths { get { return mWanderPaths; } }


        public void SaveMapData()
        {
            List<HidingSpot> allSpots = new List<HidingSpot>();
            foreach (string key in mHidingSpots.Keys)
            {
                allSpots.AddRange(mHidingSpots[key]);
            }
            File.WriteAllText(Path.Combine(MelonEnvironment.ModsDirectory, "ExpandedAiFramework.HidingSpots.json"), JSON.Dump(allSpots, EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints), Encoding.UTF8);
            List<WanderPath> allPaths = new List<WanderPath>();
            foreach (string key in mWanderPaths.Keys)
            {
                allPaths.AddRange(mWanderPaths[key]);
            }
            File.WriteAllText(Path.Combine(MelonEnvironment.ModsDirectory, "ExpandedAiFramework.WanderPaths.json"), JSON.Dump(allPaths, EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints), Encoding.UTF8);
        }


        public void LoadMapData()
        {
            mHidingSpots.Clear();
            bool canAdd;
            string hidingSpots = File.ReadAllText(Path.Combine(MelonEnvironment.ModsDirectory, "ExpandedAiFramework.HidingSpots.json"), Encoding.UTF8);
            if (hidingSpots != null)
            {
                Variant hidingSpotsVariant = JSON.Load(hidingSpots);
                foreach (var spotJSON in hidingSpotsVariant as ProxyArray)
                {
                    canAdd = true;
                    HidingSpot newSpot = new HidingSpot();
                    JSON.Populate(spotJSON, newSpot);
                    if (!mHidingSpots.TryGetValue(newSpot.Scene, out List<HidingSpot> sceneSpots))
                    {
                        sceneSpots = new List<HidingSpot>();
                        mHidingSpots.Add(newSpot.Scene, sceneSpots);
                    }
                    for (int i = 0, iMax = sceneSpots.Count; i < iMax; i++)
                    {
                        if (sceneSpots[i].Name == newSpot.Name)
                        {
                            LogWarning($"Can't add hiding spot {newSpot.Name} at {newSpot.Position} because another hiding spot with the same name is already defined!");
                            canAdd = false;
                        }
                    }
                    if (canAdd)
                    {
                        sceneSpots.Add(newSpot);
                    }
                }
            }

            mWanderPaths.Clear();
            string wanderPaths = File.ReadAllText(Path.Combine(MelonEnvironment.ModsDirectory, "ExpandedAiFramework.WanderPaths.json"), Encoding.UTF8);
            if (wanderPaths != null)
            {
                Variant wanderPathsVariant = JSON.Load(wanderPaths);
                foreach (var pathJSON in wanderPathsVariant as ProxyArray)
                {
                    canAdd = true;
                    WanderPath newPath = new WanderPath();
                    JSON.Populate(pathJSON, newPath);
                    if (!mWanderPaths.TryGetValue(newPath.Scene, out List<WanderPath> scenePaths))
                    {
                        scenePaths = new List<WanderPath>();
                        mWanderPaths.Add(newPath.Scene, scenePaths);
                    }
                    for (int i = 0, iMax = scenePaths.Count; i < iMax; i++)
                    {
                        if (scenePaths[i].Name == newPath.Name)
                        {
                            LogWarning($"Can't add hiding spot {newPath.Name} starting at {newPath.PathPoints[0]} because another hiding spot with the same name is already defined!");
                            canAdd = false;
                        }
                    }
                    if (canAdd)
                    {
                        scenePaths.Add(newPath);
                    }
                }
            }
        }


        public void ClearMapData()
        {
            mHidingSpots.Clear();
            mWanderPaths.Clear();
        }


        public void RefreshAvailableMapData(string sceneName)
        {
            mAvailableHidingSpots.Clear();
            mAvailableWanderPaths.Clear();
            if (HidingSpots.TryGetValue(sceneName, out List<HidingSpot> hidingSpots))
            {
                mAvailableHidingSpots.AddRange(hidingSpots);
            }
            for (int i = 0, iMax = mAvailableHidingSpots.Count; i < iMax; i++)
            {
                LogDebug($"Available Hiding spot {i}: {mAvailableHidingSpots[i].Name} at {mAvailableHidingSpots[i].Position}");
            }
            if (WanderPaths.TryGetValue(sceneName, out List<WanderPath> wanderPaths))
            {
                mAvailableWanderPaths.AddRange(wanderPaths);
            }
            for (int i = 0, iMax = mAvailableWanderPaths.Count; i < iMax; i++)
            {
                LogDebug($"Available Wander Path {i}: {mAvailableWanderPaths[i].Name} starting at {mAvailableWanderPaths[i].PathPoints[0]}");
            }
            mRecordingWanderPath = false;
            mCurrentWanderPathName = string.Empty;
            mCurrentWanderPathPoints.Clear();
            for (int i = 0, iMax = mCurrentWanderPathPointMarkers.Count; i < iMax; i++)
            {
                UnityEngine.Object.Destroy(mCurrentWanderPathPointMarkers[i]);
            }
            mCurrentWanderPathPointMarkers.Clear();
        }


        public HidingSpot GetNearestHidingSpot(ICustomAi ai, int extraNearestCandidatesToMaybePickFrom = 0)
        {
            Vector3 spawnPosition = ai.BaseAi.transform.position;
            int pickIndex = -1;
            if (mAvailableHidingSpots.Count > 1)
            {
                mAvailableHidingSpots.Sort((a, b) => Vector3.Distance(spawnPosition, a.Position).CompareTo(Vector3.Distance(spawnPosition, b.Position)));
                pickIndex = UnityEngine.Random.Range(0, Math.Min(mAvailableHidingSpots.Count - 1, extraNearestCandidatesToMaybePickFrom));
            }
            else if (mAvailableHidingSpots.Count == 1)
            {
                pickIndex = 0;
            }

            HidingSpot toReturn = null;
            if (pickIndex >= 0)
            {
                for (int i = 0, iMax = mAvailableHidingSpots.Count; i < iMax; i++)
                {
                    if (ai.BaseAi.CanPathfindToPosition(mAvailableHidingSpots[i].Position) && (i == iMax || pickIndex == 0))
                    {
                        toReturn = mAvailableHidingSpots[i];
                        break;
                    }
                    pickIndex--;
                }
                if (toReturn != null)
                {
                    mAvailableHidingSpots.Remove(toReturn);
#if DEV_BUILD_LOCATIONMARKERS
                    mDebugShownHidingSpots.Add(CreateMarker(toReturn.Position, Color.yellow, $"Hiding spot for ai at {ai.BaseAi.transform.position}", 100));
#endif
                }
            }
            else
            {
                LogWarning($"Could not resolve a valid hiding spot for ai at {ai.BaseAi.transform.position}, expect auto generated spot..");
                while (toReturn == null)
                {
                    if (AiUtils.GetRandomPointOnNavmesh(out Vector3 validPos, ai.BaseAi.transform.position, 250.0f, 25.0f, -1, false, 0.2f) && ai.BaseAi.CanPathfindToPosition(validPos, MoveAgent.PathRequirement.FullPath))
                    {
                        toReturn = new HidingSpot($"AutoGenerated HidingSpot for ai at {validPos}", validPos, new Vector3(UnityEngine.Random.Range(0f, 360f), 0f, 0f), GameManager.m_ActiveScene);
#if DEV_BUILD_LOCATIONMARKERS
                        mDebugShownHidingSpots.Add(CreateMarker(validPos, Color.yellow, toReturn.Name, 100.0f));
#endif
                    }
                }
            }
            return toReturn;
        }

        public WanderPath GetNearestWanderPath(ICustomAi ai, int extraNearestCandidatesToMaybePickFrom = 0)
        {
            Vector3 spawnPosition = ai.BaseAi.transform.position;
            int pickIndex = -1;
            if (mAvailableWanderPaths.Count > 1)
            {
                mAvailableWanderPaths.Sort((a, b) => Vector3.Distance(spawnPosition, a.PathPoints[0]).CompareTo(Vector3.Distance(spawnPosition, b.PathPoints[0])));
                pickIndex = UnityEngine.Random.Range(0, Math.Min(mAvailableWanderPaths.Count - 1, extraNearestCandidatesToMaybePickFrom));
            }
            else if (mAvailableWanderPaths.Count == 1)
            {
                pickIndex = 0;
            }
            WanderPath toReturn = null;
            if (pickIndex >= 0)
            {
                for (int i = 0, iMax = mAvailableHidingSpots.Count; i < iMax; i++)
                {
                    if (ai.BaseAi.CanPathfindToPosition(mAvailableWanderPaths[i].PathPoints[0]) && (i == iMax || pickIndex == 0))
                    {
                        toReturn = mAvailableWanderPaths[i];
                        break;
                    }
                    LogDebug($"{ai} at {ai.BaseAi.transform.position} can't pathfind to first point of {mAvailableWanderPaths[i].Name} at {mAvailableWanderPaths[i].PathPoints[0]}!");
                    pickIndex--;
                }
                if (toReturn != null)
                {
                    mAvailableWanderPaths.Remove(toReturn);
                    LogDebug($"{ai} at {ai.BaseAi.transform.position} picked path {toReturn.Name} with first point of {toReturn.PathPoints[0]}!");
#if DEV_BUILD_LOCATIONMARKERS
                    for (int i = 0, iMax = toReturn.PathPoints.Length; i < iMax; i++)
                    {
                        mDebugShownWanderPaths.Add(CreateMarker(toReturn.PathPoints[i], Color.blue, $"WanderPath.PathPoint[{i}] for ai at {ai.BaseAi.transform.position}", 100));
                        if (i > 0)
                        {
                            mDebugShownWanderPaths.Add(ConnectMarkers(toReturn.PathPoints[i], toReturn.PathPoints[i - 1], Color.blue, $"WanderPath.PathPointConnector[{i - 1} -> {i}] for ai at {ai.BaseAi.transform.position}", 100));
                        }
                    }
#endif
                }
            }
            else
            {
                LogWarning($"Could not resolve a valid wander path for ai at {ai.BaseAi.transform.position}, expect auto-generated path...");
                int newNumWaypoints = UnityEngine.Random.Range(4, 8);
                Vector3[] pathPoints = new Vector3[newNumWaypoints];
                int failCount = 0;
                for (int i = 0, iMax = newNumWaypoints; i < iMax;)
                {
                    if (AiUtils.GetRandomPointOnNavmesh(out Vector3 validPos, ai.BaseAi.transform.position, Mathf.Max(50.0f - failCount, 10.0f), Mathf.Max(500.0f - failCount, 10.0f), -1, false, 0.2f) && ai.BaseAi.CanPathfindToPosition(validPos, MoveAgent.PathRequirement.FullPath))
                    {
                        pathPoints[i] = validPos;
#if DEV_BUILD_LOCATIONMARKERS
                        mDebugShownWanderPaths.Add(CreateMarker(validPos, Color.blue, $"AutoGenerated WanderPath Marker {i} for qi at {ai.BaseAi.transform.position}", 100));
                        if (i > 0)
                        {
                            mDebugShownWanderPaths.Add(ConnectMarkers(validPos, pathPoints[i - 1], Color.blue, $"AutoGenerated WanderPath Connector {i - 1} -> {i} for ai at {ai.BaseAi.transform.position}", 100));
                        }
#endif
                        i++;
                    }
                    else
                    {
                        failCount++;
                    }
                }
                toReturn = new WanderPath($"AutoGenerated WanderPath for ai at {ai.BaseAi.transform.position} with starting point of {pathPoints[0]}", pathPoints, GameManager.m_ActiveScene);
            }
            return toReturn;
        }

        #endregion


        #region Debug

        //Stay outa here if you value your sanity, ugly debug-only code ahead

        private bool mRecordingWanderPath = false;
        private string mCurrentWanderPathName = string.Empty;
        private List<Vector3> mCurrentWanderPathPoints = new List<Vector3>();
        private List<GameObject> mCurrentWanderPathPointMarkers = new List<GameObject>();
        private List<GameObject> mDebugShownHidingSpots = new List<GameObject>();
        private List<GameObject> mDebugShownWanderPaths = new List<GameObject>();



        public void Log(string message, FlaggedLoggingLevel logLevel, bool toUConsole)
        {
            if (!mLoggerLevel.AnyOf(logLevel))
            {
                return;
            }
            mLogger.Log(message, logLevel);
            if (toUConsole)
            {
                uConsole.Log($"[{logLevel}] {message}");
            }
        }

        public void Log(string message, FlaggedLoggingLevel logLevel) { Log(message, logLevel, false); }
        public void LogTrace(string message) { Log(message, FlaggedLoggingLevel.Trace, false); }
        public void LogDebug(string message) { Log(message, FlaggedLoggingLevel.Debug, false); }
        public void LogVerbose(string message) { Log(message, FlaggedLoggingLevel.Verbose, false); }
        public void LogWarning(string message, bool toUConsole = true) { Log(message, FlaggedLoggingLevel.Warning, toUConsole); }
        public void LogError(string message, FlaggedLoggingLevel additionalFlags = 0U) { Log(message, FlaggedLoggingLevel.Error | additionalFlags); }



        private void InitializeLogger()
        {
            mLogger = new ComplexLogger<Main>();
#if DEV_BUILD_TRACE
            mLoggerLevel |= FlaggedLoggingLevel.Trace;
#endif
#if DEV_BUILD_DEBUG
            mLoggerLevel |= FlaggedLoggingLevel.Debug;
#endif
#if DEV_BUILD_VERBOSE
            mLoggerLevel |= FlaggedLoggingLevel.Verbose;
#endif
            mLoggerLevel |= FlaggedLoggingLevel.Warning;
            mLoggerLevel |= FlaggedLoggingLevel.Error;
            mLoggerLevel |= FlaggedLoggingLevel.Critical;
            mLoggerLevel |= FlaggedLoggingLevel.Exception;

        }


#if DEV_BUILD

#if DEV_BUILD_SPAWNONE
        private bool mSpawnedOne = false;
#endif





        public GameObject CreateMarker(Vector3 position, Color color, string name, float height)
        {
            GameObject waypointMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(waypointMarker.GetComponent<Collider>());
            waypointMarker.transform.localScale = new Vector3(.5f, height, .5f);
            waypointMarker.transform.position = position;
            waypointMarker.GetComponent<Renderer>().material.color = color;
            waypointMarker.name = name;
            return waypointMarker;
        }


        public GameObject ConnectMarkers(Vector3 pos1, Vector3 pos2, Color color, string name, float height)
        {
            GameObject waypointConnector = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(waypointConnector.GetComponent<Collider>());
            Vector3 direction = pos1 - pos2;
            float distance = direction.magnitude;
            waypointConnector.transform.position = (pos1 + pos2) / 2.0f + new Vector3(0f, height, 0f);
            waypointConnector.transform.localScale = new Vector3(0.5f, distance / 2.0f, 0.5f);
            waypointConnector.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            waypointConnector.GetComponent<Renderer>().material.color = color;
            waypointConnector.name = name;
            return waypointConnector;
        }


        public void ShowWanderPaths()
        {
            if (!WanderPaths.TryGetValue(GameManager.m_ActiveScene, out List<WanderPath> paths))
            {
                return;
            }
            foreach (WanderPath path in paths)
            {
                for (int i = 0, iMax = path.PathPoints.Length; i < iMax; i++)
                {
                    mDebugShownWanderPaths.Add(CreateMarker(path.PathPoints[i], Color.blue, path.Name, 100));
                    if (i > 0)
                    {
                        mDebugShownWanderPaths.Add(ConnectMarkers(path.PathPoints[i], path.PathPoints[i - 1], Color.blue, path.Name, 100));
                    }
                }
            }
        }


        public void HideWanderPaths()
        {
            for (int i = 0, iMax = mDebugShownWanderPaths.Count; i < iMax; i++)
            {
                UnityEngine.Object.Destroy(mDebugShownWanderPaths[i]);
            }
        }


        public void ShowHidingSpots()
        {
            if (!HidingSpots.TryGetValue(GameManager.m_ActiveScene, out List<HidingSpot> spots))
            {
                return;
            }
            foreach (HidingSpot spot in spots)
            {
                mDebugShownHidingSpots.Add(CreateMarker(spot.Position, Color.yellow, spot.Name, 100));
            }
        }


        public void HideHidingSpots()
        {
            for (int i = 0, iMax = mDebugShownHidingSpots.Count; i < iMax; i++)
            {
                UnityEngine.Object.Destroy(mDebugShownHidingSpots[i]);
            }
        }


        public void CreateHidingSpot()
        {
            string[] consoleParams = uConsole.GetString().Split(' ');
            if (consoleParams.Length == 0)
            {
                LogWarning($"Provide a name! example: 'createHidingSpot debug0'");
                return;
            }
            string name = consoleParams[0];
            string scene = GameManager.m_ActiveScene;
            if (!HidingSpots.TryGetValue(scene, out List<HidingSpot> spots))
            {
                spots = new List<HidingSpot>();
                HidingSpots.Add(scene, spots);
            }
            for (int i = 0, iMax = spots.Count; i < iMax; i++)
            {
                if (spots[i].Name == name)
                {
                    LogWarning($"Can't generate hiding spot {name} another spot with that name exists in this scene!");
                    return;
                }
            }
            Vector3 pos = GameManager.m_PlayerManager.m_LastPlayerPosition;
            AiUtils.GetClosestNavmeshPos(out Vector3 actualPos, pos, pos);
            HidingSpots[scene].Add(new HidingSpot(consoleParams[0], actualPos, GameManager.m_PlayerManager.m_LastPlayerAngle, GameManager.m_ActiveScene));
#if DEV_BUILD_LOCATIONMARKERS
            mDebugShownHidingSpots.Add(CreateMarker(actualPos, Color.yellow, $"Hiding spot: {name}", 100.0f));
#endif
            Log($"Generated hiding spot {name} at {actualPos} in scene {scene} with rotation {GameManager.m_PlayerManager.m_LastPlayerAngle}!", FlaggedLoggingLevel.Always, true);
        }


        public void StartWanderPath()
        {
            if (mRecordingWanderPath)
            {
                LogWarning($"Can't start recording path because path {mCurrentWanderPathName} is still active!");
                return;
            }
            string[] consoleParams = uConsole.GetString().Split(' ');
            if (consoleParams.Length == 0)
            {
                LogWarning($"Provide a name! example: 'createWanderPath debug0'");
                return;
            }
            string name = consoleParams[0];
            string scene = GameManager.m_ActiveScene;
            if (!WanderPaths.TryGetValue(scene, out List<WanderPath> paths))
            {
                paths = new List<WanderPath>();
                WanderPaths.Add(scene, paths);
            }
            for (int i = 0, iMax = paths.Count; i < iMax; i++)
            {
                if (paths[i].Name == name)
                {
                    LogWarning($"Can't start recording path because a path with this name exists in this scene!");
                    return;
                }
            }
            mRecordingWanderPath = true;
            mCurrentWanderPathName = name;
            AddWanderPathPos();
            Log($"Started wander path with name {name} at {mCurrentWanderPathPoints[0]}. Use command '{Patches.ConsoleManagerPatches_Initialize.CommandString_AddToWanderPath}' to add more points, and '{Patches.ConsoleManagerPatches_Initialize.CommandString_FinishCurrentWanderPath} to finish the path.", FlaggedLoggingLevel.Always, true);
        }


        public void DeleteWanderingPath()
        {
            string[] consoleParams = uConsole.GetString().Split(' ');
            if (consoleParams.Length == 0)
            {
                LogWarning($"Provide a name, or a scene followed by a name! example: 'deleteWanderPath crashmountainregion debug0' or 'deleteWanderPath debug0'!");
                return;
            }
            string name = consoleParams[0];
            string scene = consoleParams.Length >= 1 ? consoleParams[1] : GameManager.m_ActiveScene;
            if (!WanderPaths.TryGetValue(scene, out List<WanderPath> paths))
            {
                LogWarning($"No paths found in scene!");
                return;
            }
            for (int i = 0, iMax = paths.Count; i < iMax; i++)
            {
                if (paths[i].Name == name)
                {
                    Log($"Deleting wander path {name} in scene {scene}.", FlaggedLoggingLevel.Always, true);
                    WanderPaths[scene].RemoveAt(i);
                    return;
                }
            }
            LogWarning($"No path matching name {name} found in scene {scene}!");
        }


        public void CompleteWanderingPath()
        {
            if (!mRecordingWanderPath)
            {
                LogWarning($"Start recording a path to cancel!");
                return;
            }
            mRecordingWanderPath = false;
            WanderPaths[GameManager.m_ActiveScene].Add(new WanderPath(mCurrentWanderPathName, mCurrentWanderPathPoints.ToArray(), GameManager.m_ActiveScene));
            Log($"Generated wander path {mCurrentWanderPathName} starting at {mCurrentWanderPathPoints[0]}.", FlaggedLoggingLevel.Always, true);
            mCurrentWanderPathPoints.Clear();
            mCurrentWanderPathName = string.Empty;
#if DEV_BUILD_LOCATIONMARKERS
            for (int i = 0, iMax = mCurrentWanderPathPointMarkers.Count; i < iMax; i++)
            {
                UnityEngine.Object.Destroy(mCurrentWanderPathPointMarkers[i]);
            }
#endif
            mCurrentWanderPathPointMarkers.Clear();
        }


        public void AddWanderPathPos()
        {
            Vector3 pos = GameManager.m_PlayerManager.m_LastPlayerPosition;
            if (!mRecordingWanderPath)
            {
                LogWarning($"Start a path first!");
                return;
            }
            AiUtils.GetClosestNavmeshPos(out Vector3 actualPos, pos, pos);
            mCurrentWanderPathPoints.Add(actualPos);
#if DEV_BUILD_LOCATIONMARKERS
            mCurrentWanderPathPointMarkers.Add(CreateMarker(actualPos, Color.blue, $"{mCurrentWanderPathName}.Position {mCurrentWanderPathPoints.Count} Marker", 100));
            if (mCurrentWanderPathPoints.Count > 1)
            {
                mCurrentWanderPathPointMarkers.Add(ConnectMarkers(actualPos, mCurrentWanderPathPoints[mCurrentWanderPathPoints.Count - 2], Color.blue, $"{mCurrentWanderPathName}.Connector {mCurrentWanderPathPoints.Count - 2} -> {mCurrentWanderPathPoints.Count - 1}", 100));
            }
#endif
            Log($"Added wanderpath point at {actualPos} to wanderpath {mCurrentWanderPathName}", FlaggedLoggingLevel.Always, true);
        }
#endif


        #endregion
    }
}