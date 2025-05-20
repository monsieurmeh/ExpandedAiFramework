using MelonLoader.TinyJSON;
using UnityEngine;
using System.Text;
using MelonLoader.Utils;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using ComplexLogger;
using UnityEngine.AI;
using ModData;
using Il2Cpp;
using static Il2Cpp.PlayerVoice;
using ExpandedAiFramework.Enums;


namespace ExpandedAiFramework
{
    public class EAFManager
    {
        #region Lazy Singleton

        private class Nested
        {
            static Nested()
            {
            }

            internal static readonly EAFManager instance = new EAFManager();
        }

        private EAFManager() { }
        public static EAFManager Instance { get { return Nested.instance; } }

        #endregion


        #region Internal stuff

        private ExpandedAiFrameworkSettings mSettings;
        private Dictionary<int, ICustomAi> mCustomAis = new Dictionary<int, ICustomAi>();
        private WeightedTypePicker<BaseAi> mTypePicker = new WeightedTypePicker<BaseAi>();
        private Dictionary<Type, ISpawnTypePickerCandidate> mModSettingsDict = new Dictionary<Type, ISpawnTypePickerCandidate>();
        private Dictionary<Type, ISubManager> mSubManagers = new Dictionary<Type, ISubManager>();
        private ISubManager[] mSubManagerUpdateLoopArray = new ISubManager[0];
        private float mLastPlayerStruggleTime = 0.0f;
        private float mCheckForMissingScriptsTime = 0.0f;
        private bool mSceneInitialized = false;
        private bool mMapDataRefreshed = false;
        private bool mNeedToCheckForMissingScripts = false;


#if DEV_BUILD
        protected ModDataManager mModData = new ModDataManager(ModName, true);
#else
        protected ModDataManager mModData = new ModDataManager(ModName, false);
#endif

        public ModDataManager ModData { get { return mModData; } }
        public ExpandedAiFrameworkSettings Settings { get { return mSettings; } }
        public Dictionary<int, ICustomAi> CustomAis { get { return mCustomAis; } }
        public WeightedTypePicker<BaseAi> TypePicker { get { return mTypePicker; } }
        public Dictionary<Type, ISpawnTypePickerCandidate> ModSettingsDict { get { return mModSettingsDict; } }
        public Dictionary<Type, ISubManager> SubManagers { get { return mSubManagers; } }
        public float LastPlayerStruggleTime { get { return mLastPlayerStruggleTime; } set { mLastPlayerStruggleTime = value; } }



        public void Initialize(ExpandedAiFrameworkSettings settings)
        {
            mSettings = settings;
            InitializeLogger();
            LogError("Test error log!");
            RegisterSpawnableAi(typeof(BaseWolf), BaseWolf.Settings, ModName);
            RegisterSpawnableAi(typeof(BaseTimberwolf), BaseTimberwolf.Settings, ModName);
            RegisterSpawnableAi(typeof(BaseBear), BaseBear.Settings, ModName);
            RegisterSpawnableAi(typeof(BaseCougar), BaseCougar.Settings, ModName);
            RegisterSpawnableAi(typeof(BaseMoose), BaseMoose.Settings, ModName);
            RegisterSpawnableAi(typeof(BaseRabbit), BaseRabbit.Settings, ModName);
            RegisterSpawnableAi(typeof(BasePtarmigan), BasePtarmigan.Settings, ModName);
            LoadMapData();
        }


        public void Shutdown()
        {
            foreach (ISubManager subManager in mSubManagers.Values)
            {
                subManager.Shutdown();
            }
            SaveMapData();
            ClearCustomAis();
            ClearMapData();
        }

        #endregion


        #region API


        [HideFromIl2Cpp]
        public bool RegisterSpawnableAi(Type type, ISpawnTypePickerCandidate modSettings, string settingsPageName)
        {
            if (mModSettingsDict.TryGetValue(type, out _))
            {
                LogError($"Can't register {type} as it is already registered!", FlaggedLoggingLevel.Critical);
                return false;
            }
            LogAlways($"Registering type {type}");
            modSettings.Settings.AddToModSettings(settingsPageName);
            modSettings.Settings.RefreshGUI();

            mModSettingsDict.Add(type, modSettings);
            mTypePicker.AddWeight(type, modSettings.SpawnWeight, modSettings.CanSpawn);
            return true;
        }


        public void RegisterSubmanager(Type type, ISubManager subManager)
        {
            if (mSubManagers.TryGetValue(type, out ISubManager _))
            {
                LogError($"Type {type} already registered in submanager dictionary!");
                return;
            }
            LogAlways($"Registering SubManager for type {type}");
            mSubManagers.Add(type, subManager);
            Array.Resize(ref mSubManagerUpdateLoopArray, mSubManagerUpdateLoopArray.Length + 1);
            mSubManagerUpdateLoopArray[^1] = subManager;
        }


        public void Update()
        {
            for (int i = 0, iMax = mSubManagerUpdateLoopArray.Length; i < iMax; i++)
            {
                mSubManagerUpdateLoopArray[i].Update();
            }
            if (mNeedToCheckForMissingScripts && Time.time - mCheckForMissingScriptsTime >= 1.0f)
            {
                mNeedToCheckForMissingScripts = false;
                foreach (BaseAi baseAi in GameObject.FindObjectsOfType<BaseAi>())
                {
                    if (baseAi != null/* && baseAi.m_CurrentMode == AiMode.Dead*/ && baseAi.gameObject != null && !baseAi.gameObject.TryGetComponent(out CustomAiBase customAi))
                    {
                        TryInjectCustomBaseAi(baseAi);
                    }
                }
            }
        }


        public void OnStartNewGame()
        {
            for (int i = 0, iMax = mSubManagerUpdateLoopArray.Length; i < iMax; i++)
            {
                mSubManagerUpdateLoopArray[i].OnStartNewGame();
            }
        }



        public void OnLoadGame()
        {
            for (int i = 0, iMax = mSubManagerUpdateLoopArray.Length; i < iMax; i++)
            {
                mSubManagerUpdateLoopArray[i].OnLoadGame();
            }
        }


        public void OnSaveGame()
        {
            for (int i = 0, iMax = mSubManagerUpdateLoopArray.Length; i < iMax; i++)
            {
                mSubManagerUpdateLoopArray[i].OnSaveGame();
            }
        }



        public void OnLoadScene()
        {
            mSceneInitialized = false;
            mMapDataRefreshed = false;
            Manager.ClearCustomAis();
            for (int i = 0, iMax = mSubManagerUpdateLoopArray.Length; i < iMax; i++)
            {
                mSubManagerUpdateLoopArray[i].OnLoadScene();
            }
        }


        public void OnInitializedScene()
        {
            if (!mSceneInitialized)
            {
                mSceneInitialized = true;
                for (int i = 0, iMax = mSubManagerUpdateLoopArray.Length; i < iMax; i++)
                {
                    mSubManagerUpdateLoopArray[i].OnInitializedScene();
                }
            }
            if (!mMapDataRefreshed)
            {
                RefreshAvailableMapData(SceneUtilities.GetActiveSceneName());
            }
            mCheckForMissingScriptsTime = Time.time;
            mNeedToCheckForMissingScripts = true;
        }


        public void ClearCustomAis()
        {
            foreach (ICustomAi customAi in mCustomAis.Values)
            {
                TryRemoveCustomAi(customAi.BaseAi);
            }
            mCustomAis.Clear();
        }


        public bool TryInjectRandomCustomAi(BaseAi baseAi, SpawnRegion region)
        {
            if (baseAi == null)
            {
                LogDebug("Null base ai, can't augment.");
                return false;
            }
            if (mCustomAis.ContainsKey(baseAi.GetHashCode()))
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

            Il2CppSystem.Type spawnType = Il2CppType.From(typeof(void));
            for (int i = 0, iMax = mSubManagerUpdateLoopArray.Length; i < iMax; i++)
            {
                if (mSubManagerUpdateLoopArray[i].ShouldInterceptSpawn(baseAi, region))
                {
                    spawnType = Il2CppType.From(mSubManagerUpdateLoopArray[i].SpawnType);
                    break;
                }
            }
            if (spawnType == Il2CppType.From(typeof(void)))
            {
                spawnType = Il2CppType.From(mTypePicker.PickType(baseAi));
            }
            if (spawnType == Il2CppType.From(typeof(void)))
            {
                LogDebug($"No spawn type available from type picker or manager overrides for base ai {baseAi.gameObject.name}, defaulting to fallback...");
                return TryInjectCustomBaseAi(baseAi);
            }
            return TryInjectCustomAi(baseAi, spawnType, region);
        }


        public bool TryInjectCustomBaseAi(BaseAi baseAi)
        {
            switch (baseAi.m_AiSubType)
            {
                case AiSubType.Wolf: return TryInjectCustomAi(baseAi, baseAi.Timberwolf == null ? Il2CppType.From(typeof(BaseWolf)) : Il2CppType.From(typeof(BaseTimberwolf)), baseAi.m_SpawnRegionParent);
                case AiSubType.Rabbit: return TryInjectCustomAi(baseAi, baseAi.Ptarmigan == null ? Il2CppType.From(typeof(BaseRabbit)) : Il2CppType.From(typeof(BasePtarmigan)), baseAi.m_SpawnRegionParent);
                case AiSubType.Bear: return TryInjectCustomAi(baseAi, Il2CppType.From(typeof(BaseBear)), baseAi.m_SpawnRegionParent);
                case AiSubType.Moose: return TryInjectCustomAi(baseAi, Il2CppType.From(typeof(BaseMoose)), baseAi.m_SpawnRegionParent);
                case AiSubType.Stag: return TryInjectCustomAi(baseAi, Il2CppType.From(typeof(BaseDeer)), baseAi.m_SpawnRegionParent);
                case AiSubType.Cougar: return TryInjectCustomAi(baseAi, Il2CppType.From(typeof(BaseCougar)), baseAi.m_SpawnRegionParent);
                default:
                    LogError($"Can't find fallback custom base ai for baseAi {baseAi.gameObject.name}!");
                    return false;
            }
        }


        public bool TryInjectCustomAi(BaseAi baseAi, Il2CppSystem.Type spawnType, SpawnRegion region)
        {

            InjectCustomAi(baseAi, spawnType, region);
            return true;
        }


        public bool TryRemoveCustomAi(BaseAi baseAI)
        {
            if (baseAI == null)
            {
                return false;
            }
            if (!mCustomAis.ContainsKey(baseAI.GetHashCode()))
            {
                return false;
            }
            RemoveCustomAi(baseAI.GetHashCode());
            return true;
        }


        public bool TryStart(BaseAi baseAi)
        {
            if (!CustomAis.TryGetValue(baseAi.GetHashCode(), out ICustomAi customAi))
            {
                return false;
            }
            customAi.OverrideStart();
            return true;
        }


        public bool TrySetAiMode(BaseAi baseAi, AiMode aiMode)
        {
            if (!CustomAis.TryGetValue(baseAi.GetHashCode(), out ICustomAi customAi))
            {
                return false;
            }
            customAi.SetAiMode(aiMode);
            return true;
        }


        public bool TryApplyDamage(BaseAi baseAi, float damage, float bleedOutTime, DamageSource damageSource)
        {
            if (!CustomAis.TryGetValue(baseAi.GetHashCode(), out ICustomAi customAi))
            {
                return false;
            }
            customAi.ApplyDamage(damage, bleedOutTime, damageSource);
            return true;
        }


        #endregion


        #region Event Registers

        #region OnUpdateWounds

        private static event Func<BaseAi, float, bool> OnUpdateWounds;
        

        public bool InvokeUpdateWounds(BaseAi baseAi, float timePassed)
        {
            if (OnUpdateWounds == null)
                return true; 

            foreach (Func<BaseAi, float, bool> handler in OnUpdateWounds.GetInvocationList())
            {
                if (!handler(baseAi, timePassed))
                    return false;
            }

            return true;
        }

        
        public static void RegisterUpdateWounds(Func<BaseAi, float, bool> handler)
        {
            OnUpdateWounds += handler;
        }

        #endregion


        #region OnUpdateBleeding

        private static event Func<BaseAi, float, bool> OnUpdateBleeding;


        public bool InvokeUpdateBleeding(BaseAi baseAi, float timePassed)
        {
            if (OnUpdateBleeding == null)
                return true;

            foreach (Func<BaseAi, float, bool> handler in OnUpdateBleeding.GetInvocationList())
            {
                if (!handler(baseAi, timePassed))
                    return false;
            }

            return true;
        }


        public static void RegisterUpdateBleeding(Func<BaseAi, float, bool> handler)
        {
            OnUpdateBleeding += handler;
        }

        #endregion

        #endregion


        #region Internal Methods

        private void InjectCustomAi(BaseAi baseAi, Il2CppSystem.Type spawnType, SpawnRegion spawnRegion)
        {
            LogDebug($"Spawning {spawnType.Name} at {baseAi.gameObject.transform.position}");
            try
            {
                mCustomAis.Add(baseAi.GetHashCode(), (ICustomAi)baseAi.gameObject.AddComponent(spawnType));
                if (!mCustomAis.TryGetValue(baseAi.GetHashCode(), out ICustomAi customAi))
                {
                    LogError($"Critical error at ExpandedAiFramework.AugmentAi: newly created {spawnType} cannot be found in augment dictionary! Did its hash code change?", FlaggedLoggingLevel.Critical);
                    return;
                }
                customAi.Initialize(baseAi, GameManager.m_TimeOfDay, spawnRegion);//, this);
#if DEV_BUILD_SPAWNONE
                mSpawnedOne = true;
#endif
            }
            catch (Exception e)
            {
                LogError($"Error during EAFManager.InjectCustomAi: {e}");
            }
        }


        private void RemoveCustomAi(int hashCode)
        {
            if (mCustomAis.TryGetValue(hashCode, out ICustomAi customAi))
            {
                customAi.Despawn(GetCurrentTimelinePoint());
                UnityEngine.Object.Destroy(customAi.Self.gameObject); //if I'm converting back from the interface to destroy it, is there really any point to the interface? We should be demanding people use CustomBaseAi instead...
                mCustomAis.Remove(hashCode);
            }
        }


        public void Teleport(Vector3 pos, Quaternion rot)
        {
            PlayerManager playerManager = GameManager.m_PlayerManager;
            playerManager.TeleportPlayer(pos, rot);
            playerManager.StickPlayerToGround();
        }


        #endregion


        #region Path & Location Management
        //todo: break into subregions? this is getting crowded

        private Dictionary<string, List<HidingSpot>> mHidingSpots = new Dictionary<string, List<HidingSpot>>();
        private Dictionary<string, List<WanderPath>> mWanderPaths = new Dictionary<string, List<WanderPath>>();
        private Dictionary<string, List<SpawnRegionModDataProxy>> mSpawnRegionModDataProxies = new Dictionary<string, List<SpawnRegionModDataProxy>>();

        private List<HidingSpot> mAvailableHidingSpots = new List<HidingSpot>();
        private List<WanderPath> mAvailableWanderPaths = new List<WanderPath>();

        public Dictionary<string, List<HidingSpot>> HidingSpots { get { return mHidingSpots; } }
        public Dictionary<string, List<WanderPath>> WanderPaths { get { return mWanderPaths; } }
        public Dictionary<string, List<SpawnRegionModDataProxy>> SpawnRegionModDataProxies { get { return mSpawnRegionModDataProxies; } }


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
            List<SpawnRegionModDataProxy> allSpawnRegionModDataProxies = new List<SpawnRegionModDataProxy>();
            foreach (string key in mSpawnRegionModDataProxies.Keys)
            {
                allSpawnRegionModDataProxies.AddRange(mSpawnRegionModDataProxies[key]);
            }
            string json = JSON.Dump(allPaths, EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints);
            ModData.Save(json, "SpawnRegionModDataProxies");
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
                        if (sceneSpots[i] == newSpot)
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
                        if (scenePaths[i] == newPath)
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

            mSpawnRegionModDataProxies.Clear();
            string proxiesString = ModData.Load("SpawnRegionModDataProxies");
            if (proxiesString != null)
            {
                Variant proxiesVariant = JSON.Load(proxiesString);
                foreach (var pathJSON in proxiesVariant as ProxyArray)
                {
                    canAdd = true;
                    SpawnRegionModDataProxy newProxy = new SpawnRegionModDataProxy();
                    JSON.Populate(pathJSON, newProxy);
                    if (!mSpawnRegionModDataProxies.TryGetValue(newProxy.Scene, out List<SpawnRegionModDataProxy> proxies))
                    {
                        proxies = new List<SpawnRegionModDataProxy>();
                        mSpawnRegionModDataProxies.Add(newProxy.Scene, proxies);
                    }
                    for (int i = 0, iMax = proxies.Count; i < iMax; i++)
                    {
                        if (proxies[i] == newProxy)
                        {
                            LogWarning($"Can't add new proxy {newProxy} because it already exists!");
                            canAdd = false;
                        }
                    }
                    if (canAdd)
                    {
                        proxies.Add(newProxy);
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
            if (sceneName == null || !SceneUtilities.IsScenePlayable(sceneName))
            {
                return;
            }
            mMapDataRefreshed = true;
            LogDebug($"Loading EAF map data for scene {sceneName}");
            if (sceneName.Contains("_SANDBOX"))
            {
                sceneName = sceneName.Substring(0, sceneName.IndexOf("_SANDBOX"));
                LogDebug($"Modifying scene name to {sceneName}");
            }
            if (sceneName.Contains("_DLC"))
            {
                sceneName = sceneName.Substring(0, sceneName.IndexOf("_DLC"));
                LogDebug($"Modifying scene name to {sceneName}");
            }
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


        public HidingSpot GetNearestHidingSpot(ICustomAi ai, int extraNearestCandidatesToMaybePickFrom = 0, bool requireAbleToPathfind = false)
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
                    if (!requireAbleToPathfind || ai.BaseAi.CanPathfindToPosition(mAvailableHidingSpots[i].Position))
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
                    LogDebug($"{ai} picked {toReturn}.");
                    mAvailableHidingSpots.Remove(toReturn);
#if DEV_BUILD_LOCATIONMARKERS
                    mDebugShownHidingSpots.Add(CreateMarker(toReturn.Position, Color.yellow, $"Hiding spot for ai at {ai.BaseAi.transform.position}", 100));
#endif
                }
            }
            if (toReturn == null)
            {
                LogWarning($"Could not resolve a valid hiding spot for ai at {ai.BaseAi.transform.position}, expect auto generated spot..");
                while (toReturn == null)
                {
                    if (AiUtils.GetRandomPointOnNavmesh(out Vector3 validPos, ai.BaseAi.transform.position, 250.0f, 5.0f, NavMesh.AllAreas, false, 0.2f) && ai.BaseAi.CanPathfindToPosition(validPos, MoveAgent.PathRequirement.FullPath))
                    {
                        toReturn = new HidingSpot($"AutoGenerated for ai at {validPos}", validPos, Quaternion.LookRotation(new Vector3(UnityEngine.Random.Range(0f, 360f), 0f, 0f)), GameManager.m_ActiveScene);
#if DEV_BUILD_LOCATIONMARKERS
                        mDebugShownHidingSpots.Add(CreateMarker(validPos, Color.yellow, toReturn.Name, 100.0f));
#endif
                    }
                }
            }
            return toReturn;
        }

        public WanderPath GetNearestWanderPath(ICustomAi ai, int extraNearestCandidatesToMaybePickFrom = 0, bool requireAbleToPathfind = false)
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
                    if (!requireAbleToPathfind || ai.BaseAi.CanPathfindToPosition(mAvailableWanderPaths[i].PathPoints[0]))
                    {
                        if (i == iMax || pickIndex <= 0)
                        {
                            toReturn = mAvailableWanderPaths[i];
                            break;
                        }
                        pickIndex--;
                    }
                }
                if (toReturn != null)
                {
                    mAvailableWanderPaths.Remove(toReturn);
                    LogDebug($"{ai} picked {toReturn}.");
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

        //Stay outa here if you value your sanity, ugly debug-only code ahead. Aside from the logger, it is unlikely to stick around beyond

        private ComplexLogger<Main> mLogger;
        private bool mRecordingWanderPath = false;
        private string mCurrentWanderPathName = string.Empty;
        private List<Vector3> mCurrentWanderPathPoints = new List<Vector3>();
        private List<GameObject> mCurrentWanderPathPointMarkers = new List<GameObject>();
        private List<GameObject> mDebugShownHidingSpots = new List<GameObject>();
        private List<GameObject> mDebugShownWanderPaths = new List<GameObject>();
        private List<GameObject> mDebugShownSpawnRegions = new List<GameObject>();
#if DEV_BUILD_SPAWNONE
        private bool mSpawnedOne = false;
#endif

        #region Logging

        public void Log(string message, FlaggedLoggingLevel logLevel, bool toUConsole)
        {
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

        public void Console_OnCommand()
        {
            string command = uConsole.GetString();
            if (command == null || command.Length == 0)
            {
                LogAlways($"Supported commands: {CommandString_OnCommandSupportedTypes}");
                return;
            }
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
                case CommandString_List: Console_List(); return;
                case "unlock":
                    GameManager.GetFeatMasterHunter().Unlock();
                    return;
            }
        }



        #endregion


        #region Help

        private void Console_Help()
        {
            string command = uConsole.GetString();
            if (command == null || command.Length == 0)
            {
                LogAlways($"Supported commands: {CommandString_HelpSupportedCommands}");
                return;
            }
            switch (command.ToLower())
            {
                case CommandString_Create:
                    LogAlways($"Attempts to create an object. Syntax: '{CommandString} {CommandString_Create} <type> <name>'. Supported types: {CommandString_CreateSupportedTypes}");
                    return;
                case CommandString_Delete:
                    LogAlways($"Attempts to delete an object. Syntax: '{CommandString} {CommandString_Delete} <type> <name>'. Supported types: {CommandString_DeleteSupportedTypes}");
                    return;
                case CommandString_Save:
                    LogAlways($"Attempts to save an object. Syntax: '{CommandString} {CommandString_Save} <type>'. Supported types: {CommandString_SaveSupportedTypes}");
                    return;
                case CommandString_Load:
                    LogAlways($"Attempts to load an object. Syntax: '{CommandString} {CommandString_Load} <type>'. Supported types: {CommandString_LoadSupportedTypes}");
                    return;
                case CommandString_AddTo:
                    LogAlways($"Attempts to add something to an object. Syntax: '{CommandString} {CommandString_AddTo} <type>'. Supported types: {CommandString_AddToSupportedTypes}");
                    return;
                case CommandString_GoTo:
                    LogAlways($"Attempts to teleport to an object. Syntax: '{CommandString} {CommandString_GoTo} <type> <name>'. Supporteed Types: {CommandString_GoToSupportedTypes}");
                    return;
                case CommandString_Finish:
                    LogAlways($"Attempts to finish creation of an object during a multi-step process. Syntax: '{CommandString} {CommandString_Finish} <type>'. Supported Types: {CommandString_FinishSupportedTypes}");
                    return;
                case CommandString_Show:
                    LogAlways($"Attempts to show an object. Syntax: '{CommandString} {CommandString_Show} <type> <optional name>'. Supported Types: {CommandString_ShowSupportedTypes}");
                    return;
                case CommandString_Hide:
                    LogAlways($"Attempts to hide an object. Syntax: '{CommandString} {CommandString_Hide} <type> <optional name>'. Supported Types: {CommandString_HideSupportedTypes}");
                    return;
                case CommandString_List:
                    LogAlways($"Attempts to list available objects. Syntax: '{CommandString} {CommandString_List} <type>'. Supported Types: {CommandString_ListSupportedTypes}");
                    return;
            }
        }

        #endregion


        #region Create

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
            Vector3 pos = GameManager.m_vpFPSCamera.transform.position;
            Quaternion rot = GameManager.m_vpFPSCamera.transform.rotation;

            AiUtils.GetClosestNavmeshPos(out Vector3 actualPos, pos, pos);
            HidingSpots[scene].Add(new HidingSpot(name, actualPos, rot, GameManager.m_ActiveScene));
#if DEV_BUILD_LOCATIONMARKERS
            mDebugShownHidingSpots.Add(CreateMarker(actualPos, Color.yellow, $"Hiding spot: {name}", 100.0f));
#endif
            LogAlways($"Generated hiding spot {name} at {actualPos} with rotation {rot} in scene {scene}!");
            SaveMapData();
        }


        #endregion


        #region Delete

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
            Vector3 pos = GameManager.m_vpFPSCamera.transform.position;
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
                case CommandString_SpawnRegion: Console_ShowNavMesh(); return;
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


        private void Console_ShowSpawnRegion()
        {
            Console_ShowAllSpawnRegions();
        }


        private void Console_ShowAllSpawnRegions()
        {
            foreach (SpawnRegion spawnRegion in GameManager.m_SpawnRegionManager.m_SpawnRegions)
            {
                if (spawnRegion == null)
                {
                    continue;
                }
                mDebugShownSpawnRegions.Add(CreateMarker(spawnRegion.transform.position, GetSpawnRegionColor(spawnRegion), $"{spawnRegion.m_AiSubTypeSpawned} SpawnRegion Marker at {spawnRegion.transform.position}", 1000, 10));
            }
        }


        private Color GetSpawnRegionColor(SpawnRegion spawnRegion)
        {
            switch (spawnRegion.m_AiSubTypeSpawned)
            {
                case AiSubType.Wolf: return Color.grey;
                case AiSubType.Bear: return Color.red;
                case AiSubType.Cougar: return Color.cyan;
                case AiSubType.Rabbit: return Color.blue;
                case AiSubType.Stag: return Color.yellow;
                case AiSubType.Moose: return Color.green;
                default: return Color.clear;
            }
        }

        #endregion


        #region Hide

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
                case CommandString_SpawnRegion: Console_HideSpawnRegion(); return;
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
                    if (obj != null && obj.name != null && obj.name.Contains(name))
                    {
                        mDebugShownHidingSpots.Remove(obj);
                        GameObject.Destroy(obj);
                    }
                }
            }
        }


        private void Console_HideAllHidingSpots()
        {
            foreach (GameObject obj in mDebugShownHidingSpots)
            {
                UnityEngine.Object.Destroy(obj);
            }
            mDebugShownHidingSpots.Clear();
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
                    if (obj != null && obj.name != null && obj.name.Contains(name))
                    {
                        mDebugShownWanderPaths.Remove(obj);
                        GameObject.Destroy(obj);
                    }
                }
            }
        }


        private void Console_HideAllWanderPaths()
        {
            foreach (GameObject obj in mDebugShownWanderPaths)
            {
                UnityEngine.Object.Destroy(obj);
            }
            mDebugShownWanderPaths.Clear();
        }


        private void Console_HideSpawnRegion()
        {
            Console_HideAllSpawnRegions();
        }


        private void Console_HideAllSpawnRegions()
        {
            foreach (GameObject obj in mDebugShownSpawnRegions)
            {
                UnityEngine.Object.Destroy(obj);
            }
            mDebugShownSpawnRegions.Clear();
        }

        #endregion


        #region List

        public void Console_List()
        {
            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_ListSupportedTypes))
            {
                return;
            }
            switch (type)
            {
                case CommandString_WanderPath: Console_ListWanderPaths(); return;
                case CommandString_HidingSpot: Console_ListHidingSpots(); return;
                default: LogAlways($"{type} is supported per debug constants, but not routed! Report this please."); return;
            }
        }


        private void Console_ListHidingSpots()
        {
            if (!HidingSpots.TryGetValue(GameManager.m_ActiveScene, out List<HidingSpot> spots))
            {
                LogAlways("No hiding spots found in active scene.");
                return;
            }
            foreach (HidingSpot spot in HidingSpots[GameManager.m_ActiveScene])
            {
                if (spot.Scene == GameManager.m_ActiveScene)
                {
                    LogAlways($"Found {spot}. Occupied: {!mAvailableHidingSpots.Contains(spot)}");
                }
            }
        }


        private void Console_ListWanderPaths()
        {
            if (!WanderPaths.TryGetValue(GameManager.m_ActiveScene, out List<WanderPath> paths))
            {
                LogAlways("No wander paths found in active scene.");
                return;
            }
            foreach (WanderPath path in WanderPaths[GameManager.m_ActiveScene])
            {
                if (path.Scene == GameManager.m_ActiveScene)
                {
                    LogAlways($"Found {path}. Occupied: {!mAvailableWanderPaths.Contains(path)}");
                }
            }
        }

        #endregion


        #region NavMesh Management

        private GameObject mNavmeshObj = null;

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
            if (mNavmeshObj == null)
            {
                try
                {
                    var triangulation = NavMesh.CalculateTriangulation();

                    if (triangulation.vertices.Length == 0 || triangulation.indices.Length == 0)
                    {
                        LogWarning("NavMesh triangulation is empty.");
                        return;
                    }

                    Vector3[] vertices = triangulation.vertices;
                    int[] rawIndices = triangulation.indices;
                    int[] areas = triangulation.areas;
                    int[] areaColorCounts = new int[31];

                    Color[] areaColors = new Color[vertices.Length]; // Vertex colors
                    List<int> validIndices = new List<int>(rawIndices.Length);

                    for (int i = 0; i < rawIndices.Length; i += 3)
                    {
                        int idx0 = rawIndices[i];
                        int idx1 = rawIndices[i + 1];
                        int idx2 = rawIndices[i + 2];

                        if (idx0 < vertices.Length && idx1 < vertices.Length && idx2 < vertices.Length)
                        {
                            validIndices.Add(idx0);
                            validIndices.Add(idx1);
                            validIndices.Add(idx2);

                            int triangleIndex = i / 3;
                            int areaType = areas[triangleIndex];

                            Color color = GetNavMeshColor(areaType); // define your color mapping
                            areaColorCounts[areaType]++;

                            // Assign the same color to all vertices of the triangle
                            areaColors[idx0] = color;
                            areaColors[idx1] = color;
                            areaColors[idx2] = color;
                        }
                        else
                        {
                            LogVerbose($"Skipping invalid triangle at index {i} (out of bounds)");
                        }
                    }

                    for (int i = 0, iMax = 31; i < iMax; i++)
                    {
                        LogAlways($"Vertices with area index {i}: {areaColorCounts[i]}");
                    }

                    Mesh mesh = new Mesh();
                    mesh.name = "LargeNavMesh";
                    mesh.name = "LargeNavMesh";
                    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Enables >65k vertices

                    mesh.vertices = vertices;
                    mesh.triangles = validIndices.ToArray();
                    mesh.colors = areaColors;
                    mesh.RecalculateNormals();

                    mNavmeshObj = new GameObject();

                    // Assign to MeshFilter
                    MeshFilter meshFilter = mNavmeshObj.AddComponent<MeshFilter>();
                    meshFilter.sharedMesh = mesh;

                    // Apply material
                    MeshRenderer renderer = mNavmeshObj.AddComponent<MeshRenderer>();

                    Material vertexColorMat = new Material(Shader.Find("Legacy Shaders/Diffuse")); // or a custom shader
                    renderer.material = vertexColorMat;
                    mNavmeshObj.name = "eafNavMeshObj";
                }
                catch (Exception e)
                {
                    LogError(e.ToString());
                    return;
                }
            }
            mNavmeshObj.SetActive(true);
        }


        private void Console_HideNavMesh()
        {
            if (mNavmeshObj != null)
            {
                mNavmeshObj.SetActive(false);
            }
        }



        #endregion

        #endregion

        #endregion
    }
}