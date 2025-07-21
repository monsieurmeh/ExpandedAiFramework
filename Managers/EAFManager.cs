#define RecordLogCallers

using ComplexLogger;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
            DataManager,
            AiManager,
            SpawnRegionManager,
            ConsoleCommandManager,
            COUNT
        }


        private ExpandedAiFrameworkSettings mSettings;
        private DataManager mDataManager;
        private SpawnRegionManager mSpawnRegionManager;
        private AiManager mAiManager;
        private ConsoleCommandManager mConsoleCommandManager;
        private DispatchManager mDispatchManager;
        private BaseSubManager[] mBaseSubManagers = new BaseSubManager[(int)BaseSubManagers.COUNT];
        private Dictionary<Type, ISubManager> mSubManagerDict = new Dictionary<Type, ISubManager>();
        private ISubManager[] mSubManagers = new ISubManager[0];
        private float mLastPlayerStruggleTime = 0.0f;
        private string mCurrentScene = string.Empty;
        private bool mGameLoaded = false;
        private bool mPendingSceneLoadRequest = false;



        private void RegisterBaseSubManagers()
        {
            mBaseSubManagers = new BaseSubManager[(int)BaseSubManagers.COUNT];

            mDispatchManager = new DispatchManager(this, mSubManagers);
            mBaseSubManagers[(int)BaseSubManagers.DispatchManager] = mDispatchManager;

            mDataManager = new DataManager(this, mSubManagers);
            mBaseSubManagers[(int)BaseSubManagers.DataManager] = mDataManager;

            mSpawnRegionManager = new SpawnRegionManager(this, mSubManagers);
            mBaseSubManagers[(int)BaseSubManagers.SpawnRegionManager] = mSpawnRegionManager;

            mAiManager = new AiManager(this, mSubManagers);
            mBaseSubManagers[(int)BaseSubManagers.AiManager] = mAiManager;

            mConsoleCommandManager = new ConsoleCommandManager(this, mSubManagers);
            mBaseSubManagers[(int)BaseSubManagers.ConsoleCommandManager] = mConsoleCommandManager;
        }


        public void Initialize(ExpandedAiFrameworkSettings settings)
        {
            mSettings = settings;
            InitializeLogger();
            RegisterBaseSubManagers();
        }


        #region API

        public ExpandedAiFrameworkSettings Settings => mSettings;
        public Dictionary<Type, ISubManager> SubManagers => mSubManagerDict;
        public ISubManager[] SubManagerArray => mSubManagers;
        public float LastPlayerStruggleTime { get { return mLastPlayerStruggleTime; } set { mLastPlayerStruggleTime = value; } } //should be encapsulated elsewhere, datamanager maybe? or maybe some sort of timeline manager.
        public string CurrentScene => mCurrentScene;
        public bool GameLoaded { get { return mGameLoaded; } set { mGameLoaded = value; } }
        public DataManager DataManager => mDataManager;
        public SpawnRegionManager SpawnRegionManager => mSpawnRegionManager;
        public AiManager AiManager => mAiManager;
        public DispatchManager DispatchManager => mDispatchManager;
        public Dictionary<int, CustomBaseAi> CustomAis => mAiManager.CustomAis;
        public WeightedTypePicker<BaseAi> TypePicker => mAiManager.TypePicker;
        public Dictionary<Type, ISpawnTypePickerCandidate> SpawnSettingsDict => mAiManager.SpawnSettingsDict;


        public void Shutdown()
        {
            for (int i = 0, iMax = mBaseSubManagers.Length; i < iMax; i++)
            {
                mBaseSubManagers[i].Shutdown();
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
                mBaseSubManagers[i].Update();
            }
            for (int i = 0, iMax = mSubManagers.Length; i < iMax; i++)
            {
                mSubManagers[i].Update();
            }
        }


        public void OnStartNewGame()
        {
            for (int i = 0, iMax = mBaseSubManagers.Length; i < iMax; i++)
            {
                mBaseSubManagers[i].OnStartNewGame();
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
            for (int i = 0, iMax = mBaseSubManagers.Length; i < iMax; i++)
            {
                mBaseSubManagers[i].OnInitializedScene(sceneName);
            }
            for (int i = 0, iMax = mSubManagers.Length; i < iMax; i++)
            {
                mSubManagers[i].OnInitializedScene(sceneName);
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
            for (int i = 0, iMax = mSubManagers.Length; i < iMax; i++)
            {
                mSubManagers[i].OnQuitToMainMenu();
            }
        }


        public void RegisterSubmanager(Type type, ISubManager subManager)
        { 
            if (mSubManagerDict.TryGetValue(type, out ISubManager _))
            {
                LogError($"Type {type} already registered in submanager dictionary!");
                return;
            }
            LogAlways($"Registering SubManager for type {type}");
            subManager.Initialize(this);
            mSubManagerDict.Add(type, subManager);
            Array.Resize(ref mSubManagers, mSubManagers.Length + 1);
            mSubManagers[^1] = subManager;
        }


        public void SaveData(string data, string suffix) => mDataManager.ModData.Save(data, suffix);
        public string LoadData(string suffix) => mDataManager.ModData.Load(suffix);
        public bool RegisterSpawnableAi(Type type, ISpawnTypePickerCandidate spawnSettings) => mAiManager.RegisterSpawnableAi(type, spawnSettings);
        public bool TrySetAiMode(BaseAi baseAi, AiMode aiMode) => mAiManager.TrySetAiMode(baseAi, aiMode);
        public bool TryApplyDamage(BaseAi baseAi, float damage, float bleedOutTime, DamageSource damageSource) => mAiManager.TryApplyDamage(baseAi, damage, bleedOutTime, damageSource);
       
        
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
        private void InitializeLogger() => mLogger = new ComplexLogger<Main>();

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
            string callerInstanceInfo = "",
            [CallerMemberName] string callerName = "")
        {
            callerInstanceInfo = !string.IsNullOrEmpty(callerInstanceInfo) ? $":{callerInstanceInfo}" : string.Empty;
            mLogger.Log($"[{callerType}.{callerName}{callerInstanceInfo}] {message}", logLevel, LoggingSubType.Normal);
        }
#endregion

    }
}
