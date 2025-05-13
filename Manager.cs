using MelonLoader.TinyJSON;
using UnityEngine;
using System.Text;
using MelonLoader.Utils;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using ComplexLogger;
using UnityEngine.AI;
using Il2CppNodeCanvas.Tasks.Actions;


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


        public void Update()
        {
            DrawNavMeshOnUpdate();
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
            LogAlways ($"Registering type {type}:");
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


        private void Teleport(Vector3 pos)
        {
            PlayerManager playerManager = GameManager.m_PlayerManager;
            Teleport(pos, new Vector2(playerManager.m_LastPlayerAngle.x, playerManager.m_LastPlayerAngle.y));
        }


        private void Teleport(Vector3 pos, Vector2 rot)
        {
            PlayerManager playerManager = GameManager.m_PlayerManager;
            Teleport(pos, Quaternion.LookRotation(new Vector3(rot.x, rot.y, 0.0f)));
        }


        private void Teleport(Vector3 pos, Quaternion rot)
        {
            PlayerManager playerManager = GameManager.m_PlayerManager;
            playerManager.TeleportPlayer(pos, rot);
            playerManager.StickPlayerToGround();
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
                            LogWarning($"Can't add hiding spot {newPath} because another hiding spot with the same name is already defined!");
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
                LogDebug($"Available Hiding spot {i}: {mAvailableHidingSpots[i]}");
            }
            if (WanderPaths.TryGetValue(sceneName, out List<WanderPath> wanderPaths))
            {
                mAvailableWanderPaths.AddRange(wanderPaths);
            }
            for (int i = 0, iMax = mAvailableWanderPaths.Count; i < iMax; i++)
            {
                LogDebug($"Available Wander Path {i}: {mAvailableWanderPaths[i]}");
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
                mAvailableHidingSpots.Sort((a, b) => SquaredDistance(spawnPosition, a.Position).CompareTo(SquaredDistance(spawnPosition, b.Position)));
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
                    if (ai.BaseAi.CanPathfindToPosition(mAvailableHidingSpots[i].Position))
                    {
                        if (i == iMax || pickIndex <= 0)
                        {
                            toReturn = mAvailableHidingSpots[i];
                            break;
                        }
                        pickIndex--;
                    }
                }
                if (toReturn != null)
                {
                    LogDebug($"{ai} at {ai.BaseAi.transform.position} CAN pathfind to {toReturn} at {toReturn.Position}!");
                    mAvailableHidingSpots.Remove(toReturn);
#if DEV_BUILD_LOCATIONMARKERS
                    mDebugShownHidingSpots.Add(CreateMarker(toReturn.Position, Color.yellow, $"Hiding spot for ai at {ai.BaseAi.transform.position}", 100));
#endif
                }
            }
            if(toReturn == null)
            {
                LogWarning($"Could not resolve a valid hiding spot for ai at {ai.BaseAi.transform.position}, expect auto generated spot..");
                while (toReturn == null)
                {
                    if (AiUtils.GetRandomPointOnNavmesh(out Vector3 validPos, ai.BaseAi.transform.position, 250.0f, 5.0f, NavMesh.AllAreas, false, 0.2f) && ai.BaseAi.CanPathfindToPosition(validPos, MoveAgent.PathRequirement.FullPath))
                    {
                        toReturn = new HidingSpot($"AutoGenerated for ai at {validPos}", validPos, new Vector3(UnityEngine.Random.Range(0f, 360f), 0f, 0f), GameManager.m_ActiveScene);
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
                mAvailableWanderPaths.Sort((a, b) => SquaredDistance(spawnPosition, a.PathPoints[0]).CompareTo(SquaredDistance(spawnPosition, b.PathPoints[0])));
                pickIndex = UnityEngine.Random.Range(0, Math.Min(mAvailableWanderPaths.Count - 1, extraNearestCandidatesToMaybePickFrom));
            }
            else if (mAvailableWanderPaths.Count == 1)
            {
                pickIndex = 0;
            }
            WanderPath toReturn = null;
            if (pickIndex >= 0)
            {
                for (int i = 0, iMax = mAvailableWanderPaths.Count; i < iMax; i++)
                {
                    if (ai.BaseAi.CanPathfindToPosition(mAvailableWanderPaths[i].PathPoints[0]) || i == iMax || pickIndex == 0)
                    {
                        pickIndex--;
                        if (i == iMax || pickIndex == 0)
                        {
                            toReturn = mAvailableWanderPaths[i];
                            break;
                        }
                    }
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
            if (toReturn == null)
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
#if DEV_BUILD_SPAWNONE
        private bool mSpawnedOne = false;
#endif

        #region Logging

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
        public void LogAlways(string message) { Log(message, FlaggedLoggingLevel.Always, true); }


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


        #endregion


        #region Markers

        public GameObject CreateMarker(Vector3 position, Color color, string name, float height, float diameter = 5f)
        {
            GameObject waypointMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(waypointMarker.GetComponent<Collider>());
            waypointMarker.transform.localScale = new Vector3(diameter, height, diameter);
            waypointMarker.transform.position = position;
            waypointMarker.GetComponent<Renderer>().material.color = color;
            waypointMarker.name = name;
            return waypointMarker;
        }


        public GameObject ConnectMarkers(Vector3 pos1, Vector3 pos2, Color color, string name, float height, float diameter = 5f)
        {
            GameObject waypointConnector = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(waypointConnector.GetComponent<Collider>());
            Vector3 direction = pos1 - pos2;
            float distance = direction.magnitude;
            waypointConnector.transform.position = (pos1 + pos2) / 2.0f + new Vector3(0f, height, 0f);
            waypointConnector.transform.localScale = new Vector3(diameter, distance / 2.0f, diameter);
            waypointConnector.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            waypointConnector.GetComponent<Renderer>().material.color = color;
            waypointConnector.name = name;
            return waypointConnector;
        }

        #endregion


        #region Console Commands

        #region General

        //todo: load from localization? Then again these are meant to be DEBUG. Then again, framework, not just for me... 
        public const string CommandString = "eaf";
        private const string CommandString_Help = "help";
        private const string CommandString_Create = "create";
        private const string CommandString_Delete = "delete";
        private const string CommandString_Save = "save";
        private const string CommandString_Load = "load";
        private const string CommandString_AddTo = "add";
        private const string CommandString_GoTo = "goto";
        private const string CommandString_Finish = "finish";
        private const string CommandString_Show = "show";
        private const string CommandString_Hide = "hide";

        private const string CommandString_NavMesh = "navmesh";
        private const string CommandString_WanderPath = "wanderpath";
        private const string CommandString_HidingSpot = "hidingspot";
        private const string CommandString_MapData = "mapdata";


        public void Console_OnCommand()
        {
            string command = uConsole.GetString();
            switch (command.ToLower())
            {
                case CommandString_Help: Console_Help(); return;
                case CommandString_Create: Console_Create(); return;
                case CommandString_Delete: Console_Delete(); return;
                case CommandString_Save: Console_Save(); return;
                case CommandString_Load: Console_Load(); return;
                case CommandString_AddTo: Console_AddTo(); return;
                case CommandString_GoTo: Console_GoTo(); return;
                case CommandString_Finish: Console_Finish(); return;
                case CommandString_Show: Console_Show(); return;
                case CommandString_Hide: Console_Hide(); return;
            }
        }

        private bool IsTypeSupported(string type, string supportedTypeString, bool shouldWarn = true)
        {
            if (!IsTypeProvided(type, supportedTypeString, shouldWarn))
            {
                return false;
            }
            string[] supportedTypes = supportedTypeString.Split(' ');
            for (int i = 0, iMax = supportedTypes.Length; i < iMax; i++)
            {
                if (supportedTypes[i] == type)
                {
                    return true;
                }
            }
            if (shouldWarn)
            {
                LogWarning($"{type} is not supported by this command! Supported types: {supportedTypes}");
            }
            return false;
        }


        private bool IsTypeProvided(string type, string supportedTypeString, bool shouldWarn = true)
        {
            if (!IsStringProvided(type))
            {
                if (shouldWarn)
                {
                    LogWarning($"Provide a type to use this command! Supported types: {supportedTypeString}");
                }
                return false;
            }
            return true;
        }


        private bool IsNameProvided(string name, bool shouldWarn = true)
        {
            if (!IsStringProvided(name))
            {
                if (shouldWarn)
                {
                    LogWarning($"Provide a name!");
                }
                return false;
            }
            return true;
        }


        private bool IsStringProvided(string str)
        {
            if (str == null || str.Length == 0)
            {
                return false;
            }
            return true;
        }

        #endregion


        #region Help

        private const string CommandString_HelpSupportedCommands =
            $"{CommandString_Create} " +
            $"{CommandString_Delete} " +
            $"{CommandString_Save} " +
            $"{CommandString_Load} " +
            $"{CommandString_AddTo} " +
            $"{CommandString_GoTo} " +
            $"{CommandString_Finish} " +
            $"{CommandString_Show} " +
            $"{CommandString_Hide} ";


        private void Console_Help()
        {
            string command = uConsole.GetString();
            if (command == null && command.Length == 0)
            {
                LogAlways($"Supported commands: {CommandString_HelpSupportedCommands}");
                return;
            }
            switch (command.ToLower())
            {
                case CommandString_Create:
                    LogAlways($"Attempts to create an object. Syntax: '{CommandString} {CommandString_Create} <type> <name> <type-specific arguments>[]'. Supported types: {CommandString_CreateSupportedTypes}");
                    return;
                case CommandString_Delete:
                    LogAlways($"Attempts to delete an object. Syntax: '{CommandString} {CommandString_Delete} <type> <name> <type-specific arguments>[]. Supported types: {CommandString_DeleteSupportedTypes}");
                    return;
                case CommandString_Save:
                    LogAlways($"Attempts to save an object. Syntax: '{CommandString} {CommandString_Delete} <type> <name>'. ");
                    return;
                case CommandString_Load:
                    return;
                case CommandString_AddTo:
                    return;
                case CommandString_GoTo:
                    LogAlways($"Attempts to teleport to an object. Syntax: '{CommandString} {CommandString_GoTo} <type> <name> <type-specific arguments>[]'. Supporteed Types: {CommandString_GoToSupportedTypes}");
                    return;
                case CommandString_Finish:
                    return;
                case CommandString_Show:
                    return;
                case CommandString_Hide:
                    return;
            }
        }

        #endregion


        #region Create

        private const string CommandString_CreateSupportedTypes = $"{CommandString_WanderPath} {CommandString_HidingSpot}";


        private void Console_Create()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_CreateSupportedTypes))
            {
                return;
            }
            switch (type)
            {
                case CommandString_WanderPath: Console_CreateWanderPath(); return;
                case CommandString_HidingSpot: Console_CreateHidingSpot(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }



        private void Console_CreateWanderPath()
        {
            if (mRecordingWanderPath)
            {
                LogWarning($"Can't start recording path because path {mCurrentWanderPathName} is still active! enter command '{CommandString} {CommandString_Finish} {CommandString_WanderPath}' to finish current wander path.");
                return;
            }
            string name = uConsole.GetString(); 
            if (!IsNameProvided(name))
            {
                return;
            }
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
            Console_AddToCurrentWanderPath();
            LogAlways($"Started wander path with name {name} at {mCurrentWanderPathPoints[0]}. Use command '{CommandString} {CommandString_AddTo} {CommandString_WanderPath}' to add more points, and '{CommandString} {CommandString_Finish} {CommandString_WanderPath} to finish the path.");
        }



        private void Console_CreateHidingSpot()
        {
            string name = uConsole.GetString();
            if (!IsNameProvided(name))
            {
                return;
            }
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
            HidingSpots[scene].Add(new HidingSpot(name, actualPos, GameManager.m_PlayerManager.m_LastPlayerAngle, GameManager.m_ActiveScene));
#if DEV_BUILD_LOCATIONMARKERS
            mDebugShownHidingSpots.Add(CreateMarker(actualPos, Color.yellow, $"Hiding spot: {name}", 100.0f));
#endif
            LogAlways($"Generated hiding spot {name} at {actualPos} in scene {scene} with rotation {GameManager.m_PlayerManager.m_LastPlayerAngle}!");
            SaveMapData();
        }


        #endregion


        #region Delete

        private const string CommandString_DeleteSupportedTypes = $"{CommandString_WanderPath} {CommandString_HidingSpot}";


        private void Console_Delete()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_DeleteSupportedTypes))
            {
                return;
            }
            switch (type)
            {
                case CommandString_WanderPath: Console_DeleteWanderPath(); return;
                case CommandString_HidingSpot: Console_DeleteHidingSpot(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }


        private void Console_DeleteWanderPath()
        {
            string name = uConsole.GetString();
            if (!IsNameProvided(name))
            {
                return;
            }
            string scene = GameManager.m_ActiveScene;
            if (!WanderPaths.TryGetValue(scene, out List<WanderPath> paths))
            {
                LogWarning($"No paths found in scene!");
                return;
            }
            for (int i = 0, iMax = paths.Count; i < iMax; i++)
            {
                if (paths[i].Name == name)
                {
                    LogAlways($"Deleting wander path {name} in scene {scene}.");
                    WanderPaths[scene].RemoveAt(i);
                    SaveMapData();
                    return;
                }
            }
            LogWarning($"No path matching name {name} found in scene {scene}!");
        }

        private void Console_DeleteHidingSpot()
        {
            string name = uConsole.GetString();
            if (!IsNameProvided(name))
            {
                return;
            }
            string scene = GameManager.m_ActiveScene;
            if (!HidingSpots.TryGetValue(scene, out List<HidingSpot> spots))
            {
                LogWarning($"No paths found in scene!");
                return;
            }
            for (int i = 0, iMax = spots.Count; i < iMax; i++)
            {
                if (spots[i].Name == name)
                {
                    LogAlways($"Deleting hiding spot {name} in scene {scene}.");
                    HidingSpots[scene].RemoveAt(i);
                    SaveMapData();
                    return;
                }
            }
            LogWarning($"No spot matching name {name} found in scene {scene}!");
        }

        #endregion


        #region Save

        private const string CommandString_SaveSupportedTypes = $"{CommandString_MapData}";


        public void Console_Save()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_SaveSupportedTypes, false))
            {
                SaveMapData();
                return;
            }
            switch (type)
            {
                case CommandString_MapData: SaveMapData(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }

        #endregion


        #region Load

        private const string CommandString_LoadSupportedTypes = $"{CommandString_MapData}";


        public void Console_Load()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_LoadSupportedTypes, false))
            {
                LoadMapData();
                return;
            }
            switch (type)
            {
                case CommandString_MapData: LoadMapData(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }

        #endregion


        #region AddTo

        private const string CommandString_AddToSupportedTypes = $"{CommandString_WanderPath}";


        private void Console_AddTo()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_SaveSupportedTypes))
            {
                return;
            }
            switch (type)
            {
                case CommandString_WanderPath: Console_AddToWanderPath(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }


        private void Console_AddToWanderPath()
        {
            string name = uConsole.GetString();
            if (!IsNameProvided(name, false))
            {
                Console_AddToCurrentWanderPath();
            }
            else
            {
                LogWarning($"Unfortunately I have not yet added the capacity to add wander points to existing wander paths. Check in later!");
                return;
            }
        }


        private void Console_AddToCurrentWanderPath()
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
            LogAlways($"Added wanderpath point at {actualPos} to wanderpath {mCurrentWanderPathName}");
        }

        #endregion


        #region Finish

        private const string CommandString_FinishSupportedTypes = $"{CommandString_WanderPath}";


        private void Console_Finish()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_SaveSupportedTypes))
            {
                return;
            }
            switch (type)
            {
                case CommandString_WanderPath: Console_FinishWanderPath(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }


        private void Console_FinishWanderPath()
        {
            if (!mRecordingWanderPath)
            {
                LogWarning($"Start recording a path to cancel!");
                return;
            }
            mRecordingWanderPath = false;
            WanderPaths[GameManager.m_ActiveScene].Add(new WanderPath(mCurrentWanderPathName, mCurrentWanderPathPoints.ToArray(), GameManager.m_ActiveScene));
            LogAlways($"Generated wander path {mCurrentWanderPathName} starting at {mCurrentWanderPathPoints[0]}.");
            mCurrentWanderPathPoints.Clear();
            mCurrentWanderPathName = string.Empty;
            mDebugShownWanderPaths.AddRange(mCurrentWanderPathPointMarkers);
            mCurrentWanderPathPointMarkers.Clear();
            SaveMapData();
        }

        #endregion


        #region GoTo

        private const string CommandString_GoToSupportedTypes = $"{CommandString_WanderPath} {CommandString_HidingSpot}";


        public void Console_GoTo()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_GoToSupportedTypes))
            {
                return;
            }
            switch (type)
            {
                case CommandString_WanderPath: Console_GoToWanderPath(); return;
                case CommandString_HidingSpot: Console_GoToHidingSpot(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }



        private void Console_GoToHidingSpot()
        {
            if (!HidingSpots.TryGetValue(GameManager.m_ActiveScene, out List<HidingSpot> spots))
            {
                LogWarning("No hiding spots in active scene!");
                return;
            }
            int spotIndex = -1;
            string spotName = uConsole.GetString();
            for (int i = 0, iMax = spots.Count; i < iMax; i++)
            {
                if (spots[i].Name == spotName)
                {
                    spotIndex = i;
                    break;
                }
            }
            if (spotIndex == -1)
            {
                LogWarning($"Could not locate hiding spot with name {spotName}!");
                return;
            }
            Teleport(spots[spotIndex].Position, spots[spotIndex].Rotation);
            LogAlways($"Teleported to {spots[spotIndex]}! Watch out for ambush wolves...");
        }


        private void Console_GoToWanderPath()
        {
            if (!WanderPaths.TryGetValue(GameManager.m_ActiveScene, out List<WanderPath> paths))
            {
                LogWarning("No wander paths in active scene!");
                return;
            }
            int pathIndex = -1;
            string pathName = uConsole.GetString();
            for (int i = 0, iMax = paths.Count; i < iMax; i++)
            {
                if (paths[i].Name == pathName)
                {
                    pathIndex = i;
                    break;
                }
            }
            if (pathIndex == -1)
            {
                LogWarning($"Could not locate wander path with name {pathName}!");
                return;
            }
            int pathPointIndex = 0;

            try
            {
                pathPointIndex = uConsole.GetInt();
            }
            catch (Exception e)
            {
                LogError(e.ToString());
            }

            if (pathPointIndex >= paths[pathIndex].PathPoints.Length)
            {
                LogWarning($"{paths[pathIndex]} has {paths[pathIndex].PathPoints.Length} path points, please select one in that range!");
                return;
            }
            Quaternion lookDir = Quaternion.identity;
            if (pathPointIndex == paths[pathIndex].PathPoints.Length - 1)
            {
                lookDir = Quaternion.LookRotation(paths[pathIndex].PathPoints[0] - paths[pathIndex].PathPoints[pathPointIndex]);
            }
            else
            {
                lookDir = Quaternion.LookRotation(paths[pathIndex].PathPoints[pathPointIndex + 1] - paths[pathIndex].PathPoints[pathPointIndex]);
            }
            Teleport(paths[pathIndex].PathPoints[pathPointIndex], lookDir);
            LogAlways($"Teleported to WanderPath {paths[pathIndex].Name} point #{pathPointIndex} at {paths[pathIndex].PathPoints[pathPointIndex]}! Watch out for wandering wolves...");
        }


        #endregion


        #region Show

        private const string CommandString_ShowSupportedTypes = $"{CommandString_WanderPath} {CommandString_HidingSpot}";


        public void Console_Show()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_ShowSupportedTypes))
            {
                return;
            }
            switch (type)
            {
                case CommandString_WanderPath: Console_ShowWanderPath(); return;
                case CommandString_HidingSpot: Console_ShowHidingSpot(); return;
                case CommandString_NavMesh: Console_ShowNavMesh(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }


        private void Console_ShowHidingSpot()
        {
            if (!HidingSpots.TryGetValue(GameManager.m_ActiveScene, out List<HidingSpot> spots))
            {
                LogWarning("No hiding spots found in active scene!");
                return;
            }
            string name = uConsole.GetString();
            if (!IsNameProvided(name, false))
            {
                Console_ShowAllHidingSpots();
            }
            else
            {
                foreach (HidingSpot spot in spots)
                {
                    if (spot.Name == name)
                    {
                        mDebugShownHidingSpots.Add(CreateMarker(spot.Position, Color.yellow, spot.Name, 100));
                        return;
                    }
                }
            }
        }


        private void Console_ShowAllHidingSpots()
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


        private void Console_ShowWanderPath()
        {
            if (!WanderPaths.TryGetValue(GameManager.m_ActiveScene, out List<WanderPath> paths))
            {
                LogWarning("No wander paths found in active scene!");
                return;
            }
            string name = uConsole.GetString();
            if (!IsNameProvided(name, false))
            {
                Console_ShowAllWanderPaths();
            }
            else
            {
                foreach (WanderPath path in paths)
                {
                    if (path.Name == name)
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
            }
        }
        

        private void Console_ShowAllWanderPaths()
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

        #endregion


        #region Hide

        private const string CommandString_HideSupportedTypes = $"{CommandString_WanderPath} {CommandString_HidingSpot} {CommandString_NavMesh}";


        public void Console_Hide()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_HideSupportedTypes))
            {
                return;
            }
            switch (type)
            {
                case CommandString_WanderPath: Console_HideWanderPath(); return;
                case CommandString_HidingSpot: Console_HideHidingSpot(); return;
                case CommandString_NavMesh: Console_HideNavMesh(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }


        private void Console_HideHidingSpot()
        {
            if (!HidingSpots.TryGetValue(GameManager.m_ActiveScene, out List<HidingSpot> spots))
            {
                LogWarning("No hiding spots found in active scene!");
                return;
            }
            string name = uConsole.GetString();
            if (!IsNameProvided(name, false))
            {
                Console_HideAllHidingSpots();
            }
            else
            {
                foreach (GameObject obj in mDebugShownHidingSpots)
                {
                    if (obj.name.Contains(name))
                    {
                        GameObject.Destroy(obj);
                    }
                }
            }
        }


        private void Console_HideAllHidingSpots()
        {
            for (int i = 0, iMax = mDebugShownHidingSpots.Count; i < iMax; i++)
            {
                UnityEngine.Object.Destroy(mDebugShownHidingSpots[i]);
            }
        }


        private void Console_HideWanderPath()
        {
            if (!WanderPaths.TryGetValue(GameManager.m_ActiveScene, out List<WanderPath> paths))
            {
                LogWarning("No wander paths found in active scene!");
                return;
            }
            string name = uConsole.GetString();
            if (!IsNameProvided(name, false))
            {
                Console_HideAllWanderPaths();
            }
            else
            {
                foreach (GameObject obj in mDebugShownWanderPaths)
                {
                    if (obj.name.Contains(name))
                    {
                        GameObject.Destroy(obj);
                    }
                }
            }
        }


        private void Console_HideAllWanderPaths()
        {
            for (int i = 0, iMax = mDebugShownWanderPaths.Count; i < iMax; i++)
            {
                UnityEngine.Object.Destroy(mDebugShownWanderPaths[i]);
            }
        }

        #endregion


        #region NavMesh Management

        private bool mDrawNavMesh = false;
        private Material mDrawNavMeshMaterial;
        private NavMeshTriangulation mNavMeshTriangulation;

        private Color GetNavMeshColor(int layer)
        {
            switch (layer)
            {
                case 0:
                    return Color.black;
                case 1:
                    return Color.white;
                case 2:
                    return Color.red;
                case 3:
                    return Color.blue;
                case 4:
                    return Color.green;
                case 5:
                    return Color.grey;
                case 6:
                    return Color.cyan;
                case 7:
                    return Color.yellow;
                case 8:
                    return Color.magenta;
                default:
                    return new Color(100, 200, 70);
            }
        }


        private void Console_ShowNavMesh()
        {
            if (mDrawNavMeshMaterial == null || mNavMeshTriangulation == null)
            {
                try
                {
                    string toTry = "Hidden/Internal-Colored";
                    mDrawNavMeshMaterial = new Material(Shader.Find(toTry));
                    if (mDrawNavMeshMaterial == null)
                    {
                        LogError($"Could not fetch material from shader {toTry}!");
                        return;
                    }
                    if (mNavMeshTriangulation == null)
                    {
                        mNavMeshTriangulation = NavMesh.CalculateTriangulation();
                    }
                    if (mNavMeshTriangulation == null)
                    {
                        LogError($"Could not calculate nav mesh triangulation!");
                        return;
                    }
                    mDrawNavMesh = true;
                }
                catch (Exception e)
                {
                    LogError(e.ToString(), FlaggedLoggingLevel.Exception);
                    return;
                }
            }
        }


        private void Console_HideNavMesh()
        {
            mDrawNavMesh = false;
        }


        private void DrawNavMeshOnUpdate()
        {
            if (!mDrawNavMesh)
            {
                return;
            }
            if (mDrawNavMeshMaterial == null)
            {
                return;
            }
            GL.PushMatrix();
            mDrawNavMeshMaterial.SetPass(0);
            GL.Begin(GL.TRIANGLES);
            for (int i = 0; i < mNavMeshTriangulation.indices.Length; i += 3)
            {
                GL.Color(GetNavMeshColor(mNavMeshTriangulation.areas[i / 3]));
                GL.Vertex(mNavMeshTriangulation.vertices[mNavMeshTriangulation.indices[i]]);
                GL.Vertex(mNavMeshTriangulation.vertices[mNavMeshTriangulation.indices[i + 1]]);
                GL.Vertex(mNavMeshTriangulation.vertices[mNavMeshTriangulation.indices[i + 2]]);
            }
            GL.End();

            GL.PopMatrix();
        }



        #endregion


        #endregion

        #endregion
    }
}