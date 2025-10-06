#define RecordLogCallers

using ComplexLogger;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ExpandedAiFramework.Enums;
using UnityEngine;

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


        private enum BaseSubManagers : int
        {
            DispatchManager = 0,
            LogDispatcher,
            DataManager,
            AiManager,
            SpawnRegionManager,
            ConsoleCommandManager,
            COUNT
        }

        public enum HotSwappableSubManagers : int
        {
            CougarManager = 0,
            PackManager,
            COUNT
        }

        private Dictionary<HotSwappableSubManagers, Type> mHotSwappableInterfaceMap = new Dictionary<HotSwappableSubManagers, Type>()
        {
            { HotSwappableSubManagers.CougarManager, typeof(ICougarManager) },
             { HotSwappableSubManagers.PackManager, typeof(IPackManager) },
        };

        private ExpandedAiFrameworkSettings mSettings;
        private DataManager mDataManager;
        private SpawnRegionManager mSpawnRegionManager;
        private AiManager mAiManager;
        private ConsoleCommandManager mConsoleCommandManager;
        private DispatchManager mDispatchManager;
        private DispatchManager mLogDispatcher;
        private BaseSubManager[] mBaseSubManagers = new BaseSubManager[(int)BaseSubManagers.COUNT];
        private ISubManager[] mHotSwappableSubManagers = new ISubManager[(int)HotSwappableSubManagers.COUNT];
        private Dictionary<Type, ISpawnManager> mSubManagerDict = new Dictionary<Type, ISpawnManager>();
        private ISubManager[] mSubManagers = new ISubManager[0];
        private Dictionary<string, BasePaintManager> mPaintManagerDict = new Dictionary<string, BasePaintManager>();
        private float mLastPlayerStruggleTime = 0.0f;
        private string mCurrentScene = string.Empty;
        private bool mGameLoaded = false;
        private bool mPendingSceneLoadRequest = false;
        private object mLoggerLock = new object();
        private uint mHotSwapLockMask = 0U;
        private LogCategoryFlags mLogCategoryFlags = LogCategoryFlags.General;



        private void RegisterBaseSubManagers()
        {
            mBaseSubManagers = new BaseSubManager[(int)BaseSubManagers.COUNT];

            mDispatchManager = new DispatchManager(this);
            mBaseSubManagers[(int)BaseSubManagers.DispatchManager] = mDispatchManager;

            mLogDispatcher = new DispatchManager(this);
            mBaseSubManagers[(int)BaseSubManagers.LogDispatcher] = mLogDispatcher;

            mDataManager = new DataManager(this);
            mBaseSubManagers[(int)BaseSubManagers.DataManager] = mDataManager;

            mSpawnRegionManager = new SpawnRegionManager(this);
            mBaseSubManagers[(int)BaseSubManagers.SpawnRegionManager] = mSpawnRegionManager;

            mAiManager = new AiManager(this);
            mBaseSubManagers[(int)BaseSubManagers.AiManager] = mAiManager;

            mConsoleCommandManager = new ConsoleCommandManager(this);
            mBaseSubManagers[(int)BaseSubManagers.ConsoleCommandManager] = mConsoleCommandManager;
        }

        private void RegisterDefaultHotSwappableSubManagers()
        {
            RegisterDefaultCougarManager();
            RegisterDefaultPackManager();
        }

        private void RegisterDefaultCougarManager()
        {
            CougarManager cougarManager = new CougarManager(this);
            mHotSwappableSubManagers[(int)HotSwappableSubManagers.CougarManager] = cougarManager;
            mSubManagerDict.Add(typeof(BaseCougar), cougarManager);
        }


        private void RegisterDefaultPackManager()
        {
            PackManager packManager = new PackManager(this);
            mHotSwappableSubManagers[(int)HotSwappableSubManagers.PackManager] = packManager;
            // PackManager is NOT an ISpawnManager - don't try to add it to the spawnmanager dict
        }


        public void Initialize(ExpandedAiFrameworkSettings settings)
        {
            mSettings = settings;
            InitializeLogger();
            RegisterBaseSubManagers();
            RegisterDefaultHotSwappableSubManagers();
            InitializeDebugMenu();
        }


        #region API

        public ExpandedAiFrameworkSettings Settings => mSettings;
        public float LastPlayerStruggleTime { get { return mLastPlayerStruggleTime; } set { mLastPlayerStruggleTime = value; } } //should be encapsulated elsewhere, datamanager maybe? or maybe some sort of timeline manager.
        public string CurrentScene => mCurrentScene;
        public bool GameLoaded { get { return mGameLoaded; } set { mGameLoaded = value; } }
        public DataManager DataManager => mDataManager;
        public SpawnRegionManager SpawnRegionManager => mSpawnRegionManager;
        public AiManager AiManager => mAiManager;
        public ConsoleCommandManager ConsoleCommandManager => mConsoleCommandManager;
        public DispatchManager DispatchManager => mDispatchManager;
        public Dictionary<int, CustomBaseAi> CustomAis => mAiManager.CustomAis;
        public WeightedTypePicker<BaseAi> TypePicker => mAiManager.TypePicker;
        public Dictionary<Type, ISpawnTypePickerCandidate> SpawnSettingsDict => mAiManager.SpawnSettingsDict;
        public Dictionary<string, BasePaintManager> PaintManagers => mPaintManagerDict;
        public ICougarManager CougarManager => mHotSwappableSubManagers[(int)HotSwappableSubManagers.CougarManager] as ICougarManager;
        public IPackManager PackManager => mHotSwappableSubManagers[(int)HotSwappableSubManagers.PackManager] as IPackManager;
        public LogCategoryFlags LogCategoryFlags { get { return mLogCategoryFlags; } set { mLogCategoryFlags = value; } }

        public void Shutdown()
        {
            foreach (var paintManager in mPaintManagerDict.Values)
            {
                paintManager.Shutdown();
            }
            for (int i = 0, iMax = mBaseSubManagers.Length; i < iMax; i++)
            {
                mBaseSubManagers[i].Shutdown();
            }
            for (int i = 0, iMax = mHotSwappableSubManagers.Length; i < iMax; i++)
            {
                mHotSwappableSubManagers[i].Shutdown();
            }
            for (int i = 0, iMax = mSubManagers.Length; i < iMax; i++)
            {
                mSubManagers[i].Shutdown();
            }
        }


        public void Update()
        {
            for (int i = 0, iMax = mBaseSubManagers.Length; i < iMax; i++)
            {
                mBaseSubManagers[i].UpdateFromManager();
            }
            for (int i = 0, iMax = mHotSwappableSubManagers.Length; i < iMax; i++)
            {
                mHotSwappableSubManagers[i].UpdateFromManager();
            }
            for (int i = 0, iMax = mSubManagers.Length; i < iMax; i++)
            {
                mSubManagers[i].UpdateFromManager();
            }
        }


        public void OnStartNewGame()
        {
            for (int i = 0, iMax = mBaseSubManagers.Length; i < iMax; i++)
            {
                mBaseSubManagers[i].OnStartNewGame();
            }
            for (int i = 0, iMax = mHotSwappableSubManagers.Length; i < iMax; i++)
            {
                mHotSwappableSubManagers[i].OnStartNewGame();
            }
            for (int i = 0, iMax = mSubManagers.Length; i < iMax; i++)
            {
                mSubManagers[i].OnStartNewGame();
            }
        }


        public void OnLoadGame()
        {
            mGameLoaded = true;
            for (int i = 0, iMax = mBaseSubManagers.Length; i < iMax; i++)
            {
                mBaseSubManagers[i].OnLoadGame();
            }
            for (int i = 0, iMax = mHotSwappableSubManagers.Length; i < iMax; i++)
            {
                mHotSwappableSubManagers[i].OnLoadGame();
            }
            for (int i = 0, iMax = mSubManagers.Length; i < iMax; i++)
            {
                mSubManagers[i].OnLoadGame();
            }
            if (mPendingSceneLoadRequest)
            {
                mPendingSceneLoadRequest = false;
                OnLoadScene(mCurrentScene);
            }
        }


        public void OnSaveGame()
        {
            for (int i = mBaseSubManagers.Length - 1, iMin = 0; i >= iMin; i--)
            {
                mBaseSubManagers[i].OnSaveGame();
            }
            for (int i = 0, iMax = mHotSwappableSubManagers.Length; i < iMax; i++)
            {
                mHotSwappableSubManagers[i].OnSaveGame();
            }
            for (int i = 0, iMax = mSubManagers.Length; i < iMax; i++)
            {
                mSubManagers[i].OnSaveGame();
            }
        }


        public void OnLoadScene(string sceneName)
        {
            if (sceneName.Contains("MainMenu"))
            {
                mCurrentScene = string.Empty;
                LogTrace($"OnQuitToMainMenu");
                OnQuitToMainMenu();
                return;
            }
            if (!IsValidGameplayScene(sceneName, out string parsedSceneName))
            {
                LogTrace($"{sceneName} invalid, ignoreing");
                return;
            }
            if (mCurrentScene == parsedSceneName)
            {
                LogDebug($"{mCurrentScene} already loaded, aborting!");
                return;
            }
            LogTrace($"OnLoadScene {parsedSceneName} valid!");
            mCurrentScene = parsedSceneName;
            if (!mGameLoaded)
            {
                LogTrace($"Scene loaded without game loaded, queueing load scene request for after game loaded...");
                mPendingSceneLoadRequest = true;
                return;
            }
            for (int i = 0, iMax = mBaseSubManagers.Length; i < iMax; i++)
            {
                mBaseSubManagers[i].OnLoadScene(sceneName);
            }
            for (int i = 0, iMax = mHotSwappableSubManagers.Length; i < iMax; i++)
            {
                mHotSwappableSubManagers[i].OnLoadScene(sceneName);
            }
            for (int i = 0, iMax = mSubManagers.Length; i < iMax; i++)
            {
                mSubManagers[i].OnLoadScene(sceneName);
            }
        }


        public void OnInitializedScene(string sceneName)
        {
            if (sceneName == null)
            {
                return;
            }
            if (!sceneName.Contains(mCurrentScene))
            {
                LogDebug($"{sceneName} is not {mCurrentScene}, aborting");
                return;
            }
            for (int i = 0, iMax = mBaseSubManagers.Length; i < iMax; i++)
            {
                mBaseSubManagers[i].OnInitializedScene(sceneName);
            }
            for (int i = 0, iMax = mHotSwappableSubManagers.Length; i < iMax; i++)
            {
                mHotSwappableSubManagers[i].OnInitializedScene(sceneName);
            }
            for (int i = 0, iMax = mSubManagers.Length; i < iMax; i++)
            {
                mSubManagers[i].OnInitializedScene(sceneName);
            }
            if (IsValidGameplayScene(sceneName, out _))
            {
                CougarManager.OverrideStart();
                PackManager.OverrideStart();
            }
        }


        public void OnQuitToMainMenu()
        {
            mGameLoaded = false;
            mPendingSceneLoadRequest = false;
            for (int i = 0, iMax = mBaseSubManagers.Length; i < iMax; i++)
            {
                mBaseSubManagers[i].OnQuitToMainMenu();
            }
            for (int i = 0, iMax = mHotSwappableSubManagers.Length; i < iMax; i++)
            {
                mHotSwappableSubManagers[i].OnQuitToMainMenu();
            }
            for (int i = 0, iMax = mSubManagers.Length; i < iMax; i++)
            {
                mSubManagers[i].OnQuitToMainMenu();
            }
        }


        public void RegisterSpawnManager(ISpawnManager subManager)
        { 
            if (mSubManagerDict.TryGetValue(subManager.SpawnType, out ISpawnManager _))
            {
                LogError($"Type {subManager.SpawnType} already registered in submanager dictionary!");
                return;
            }
            LogTrace($"Registering SubManager for type {subManager.SpawnType}");
            subManager.Initialize(this);
            mSubManagerDict.Add(subManager.SpawnType, subManager);
            Array.Resize(ref mSubManagers, mSubManagers.Length + 1);
            mSubManagers[^1] = subManager;
        }

        public void HotSwapSubManager(HotSwappableSubManagers hotSwapType, ISubManager subManager)
        {
            if (((1U << (int)hotSwapType) & mHotSwapLockMask) != 0U)
            {
                LogError($"{hotSwapType} already claimed!");
                return;
            }
            if (!mHotSwappableInterfaceMap.TryGetValue(hotSwapType, out Type interfaceType))
            {
                LogError($"No interface type found for {hotSwapType}!");
                return;
            }
            if (!interfaceType.IsAssignableFrom(subManager.GetType()))
            {
                LogError($"{subManager.GetType()} is not assignable from {interfaceType}!");
                return;
            }
            mHotSwapLockMask |= 1U << (int)hotSwapType;
            mHotSwappableSubManagers[(int)hotSwapType] = subManager;
            subManager.Initialize(this);
            PostProcessHotswappedSubmanager(hotSwapType, subManager);
            LogAlways($"{hotSwapType} locked!");
        }


        private void PostProcessHotswappedSubmanager(HotSwappableSubManagers hotSwapType, ISubManager subManager)
        {
            switch (hotSwapType)
            {
                case HotSwappableSubManagers.CougarManager:
                    ICougarManager cougarManager = subManager as ICougarManager;
                    if (cougarManager == null)
                    {
                        LogError($"{subManager.GetType()} is not a ICougarManager!");
                        return;
                    }
                    mSubManagerDict.Remove(typeof(BaseCougar));
                    mSubManagerDict.Add(cougarManager.SpawnType, cougarManager);
                    return;
            }
        }

        public IEnumerable<ISubManager> EnumerateSubManagers() 
        {
            for (int i = 0, iMax = mSubManagers.Length; i < iMax; i++)
            {
                yield return mSubManagers[i];
            }
            for (int i = 0, iMax = mHotSwappableSubManagers.Length; i < iMax; i++)
            {
                yield return mHotSwappableSubManagers[i];
            }
        }

        public IEnumerable<ISpawnManager> EnumerateSpawnManagers()
        {
            for (int i = 0, iMax = mSubManagers.Length; i < iMax; i++)
            {
                if (mSubManagers[i] is ISpawnManager spawnManager)
                {
                    yield return spawnManager;
                }
            }
            for (int i = 0, iMax = mHotSwappableSubManagers.Length; i < iMax; i++)
            {
                if (mHotSwappableSubManagers[i] is ISpawnManager spawnManager)
                {
                    yield return spawnManager;
                }
            }
        }

        
        public bool TryGetSpawnManager(Type type, out ISpawnManager subManager) => mSubManagerDict.TryGetValue(type, out subManager);
        public void PostProcessNewSpawnModDataProxy(SpawnModDataProxy proxy) { if (mSubManagerDict.TryGetValue(proxy.VariantSpawnType, out ISpawnManager subManager)) subManager.PostProcessNewSpawnModDataProxy(proxy);}
        public void SaveData(string data, string suffix) => mDataManager.ModData.Save(data, suffix);
        public string LoadData(string suffix) => mDataManager.ModData.Load(suffix);
        public bool RegisterSpawnableAi(Type type, ISpawnTypePickerCandidate spawnSettings) => mAiManager.RegisterSpawnableAi(type, spawnSettings);
        public bool TrySetAiMode(BaseAi baseAi, AiMode aiMode) => mAiManager.TrySetAiMode(baseAi, aiMode);
        public bool TryApplyDamage(BaseAi baseAi, float damage, float bleedOutTime, DamageSource damageSource) => mAiManager.TryApplyDamage(baseAi, damage, bleedOutTime, damageSource);


        public void RegisterPaintManager(BasePaintManager paintManager)
        {
            if (mPaintManagerDict.TryGetValue(paintManager.TypeName.ToLower(), out BasePaintManager existing))
            {
                LogError($"Paint manager for type {paintManager.TypeName} already registered!");
                return;
            }
            LogTrace($"Registering paint manager for type {paintManager.TypeName}");
            paintManager.Initialize();
            mPaintManagerDict.Add(paintManager.TypeName.ToLower(), paintManager);
        }

        public bool TryGetPaintManager(string typeName, out BasePaintManager paintManager)
        {
            return mPaintManagerDict.TryGetValue(typeName.ToLower(), out paintManager);
        }

        /* This section needs a rework, possibly its own interop platform specifically for prefix bool patches
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
        */

        #endregion


        #region Debug

        public void Console_OnCommand() => mConsoleCommandManager.Console_OnCommand();

        private ComplexLogger<Main> mLogger;
        public ComplexLogger<Main> Logger => mLogger;
        
        private void InitializeLogger()
        {
            LogSettings logSettings = LogSettings.CreateDynamic(Path.Combine(DataFolderPath, "LogSettings"));
            logSettings.AddToModSettings(ModName);
            mLogCategoryFlags = logSettings.GetFlags();
            mLogger = new ComplexLogger<Main>();
        }

        private void InitializeDebugMenu()
        {
            // Create the debug menu GameObject and manager during initialization
            // This ensures F2 key binding always works, but menu starts hidden
            var debugMenuObj = new GameObject("EAFDebugMenu");
            UnityEngine.Object.DontDestroyOnLoad(debugMenuObj);
            debugMenuObj.AddComponent<DebugMenu.DebugMenuManager>();
            LogStatic("Debug menu initialized (hidden)", FlaggedLoggingLevel.Debug, nameof(EAFManager), LogCategoryFlags.DebugMenu);
        }

        public FlaggedLoggingLevel CurrentLogLevel => ComplexLogger.Main.CurrentLevel;

       
        public static void LogWithStackTrace(string message, int offsetStart = 1, int offsetEnd = 0)
        {
            StackTrace stackTrace = new StackTrace();
            for (int i = offsetStart, iMax = stackTrace.FrameCount - offsetEnd; i < iMax; i++)
            {
                var method = stackTrace.GetFrame(i).GetMethod();
                if (method != null)
                {
                    message = $"[{method.DeclaringType}.{method.Name}]\n" + message;
                }
                else
                {
                    message = $"[NULL]\n" + message;
                }
            }
            Manager.Logger.Log(message, FlaggedLoggingLevel.Always);
        }

        public void Log(
            string message, 
            FlaggedLoggingLevel logLevel,
            string callerType,
            LogCategoryFlags logCategoryFlags = LogCategoryFlags.General,
            string callerInstanceInfo = "",
            [CallerMemberName] string callerName = "",
            bool toUConsole = false)
        {
            if (!logCategoryFlags.AnyOf(mLogCategoryFlags))
            {
                return;
            }
            callerInstanceInfo = !string.IsNullOrEmpty(callerInstanceInfo) ? $":{callerInstanceInfo}" : string.Empty;
            lock (mLoggerLock)
            {
                DateTime now = DateTime.Now;
                mLogDispatcher.Dispatch(() => LogInternal(message, logLevel, callerType, callerInstanceInfo, callerName, toUConsole, now));
            }
        }

        
        private void LogInternal(
            string message,
            FlaggedLoggingLevel logLevel,
            string callerType,
            string callerInstanceInfo,
            string callerName,
            bool toUConsole,
            DateTime now)
        { 
            if (toUConsole)
            {
                mLogger.Log(message, logLevel, LoggingSubType.uConsole, $"[{callerType}.{callerName}{callerInstanceInfo} at {now}]");
            }
            mLogger.Log(message, logLevel, LoggingSubType.Normal, $"[{callerType}.{callerName}{callerInstanceInfo} at {now}]");
        }


        public static void LogStatic(
            string message, 
            FlaggedLoggingLevel logLevel,
            string callerType,
            LogCategoryFlags logCategoryFlags = LogCategoryFlags.General,
            string callerInstanceInfo = "",
            [CallerMemberName] string callerName = "",
            bool toUConsole = false)
        {
            Manager.Log(message, logLevel, callerType, logCategoryFlags, callerInstanceInfo, callerName, toUConsole);
        }
#endregion

    }
}
