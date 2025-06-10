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
            DataManager = 0,
            AiManager,
            SpawnRegionManager,
            ConsoleCommandManager,
            DispatchManager,
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



        private void RegisterBaseSubManagers()
        {
            mBaseSubManagers = new BaseSubManager[(int)BaseSubManagers.COUNT];

            mDataManager = new DataManager(this, mSubManagers);
            mSpawnRegionManager = new SpawnRegionManager(this, mSubManagers);
            mAiManager = new AiManager(this, mSubManagers);
            mConsoleCommandManager = new ConsoleCommandManager(this, mSubManagers);
            mDispatchManager = new DispatchManager(this, mSubManagers);

            mBaseSubManagers[(int)BaseSubManagers.DataManager] = mDataManager;
            mBaseSubManagers[(int)BaseSubManagers.SpawnRegionManager] = mSpawnRegionManager;
            mBaseSubManagers[(int)BaseSubManagers.AiManager] = mAiManager;
            mBaseSubManagers[(int)BaseSubManagers.ConsoleCommandManager] = mConsoleCommandManager;
            mBaseSubManagers[(int)BaseSubManagers.DispatchManager] = mDispatchManager;
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
            for (int i = 0, iMax = mBaseSubManagers.Length; i < iMax; i++)
            {
                mBaseSubManagers[i].OnLoadGame();
            }
            for (int i = 0, iMax = mSubManagers.Length; i < iMax; i++)
            {
                mSubManagers[i].OnLoadGame();
            }
        }


        public void OnSaveGame()
        {
            for (int i = 0, iMax = mBaseSubManagers.Length; i < iMax; i++)
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
            mCurrentScene = string.Empty;
            if (sceneName.Contains("MainMenu"))
            {
                OnQuitToMainMenu();
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
            if (IsValidGameplayScene(sceneName, out string newCurrentScene))
            { 
                if (mCurrentScene == string.Empty)
                {
                    mCurrentScene = newCurrentScene;
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
        }


        public void OnQuitToMainMenu()
        {
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
            mSubManagerDict.Add(type, subManager);
            Array.Resize(ref mSubManagers, mSubManagers.Length + 1);
            mSubManagers[^1] = subManager;
        }

        public void SaveData(string data, string suffix) => mDataManager.ModData.Save(data, suffix);
        public string LoadData(string suffix) => mDataManager.ModData.Load(suffix);
        public bool RegisterSpawnableAi(Type type, ISpawnTypePickerCandidate spawnSettings) => mAiManager.RegisterSpawnableAi(type, spawnSettings);
        public void ClearCustomAis() => mAiManager.ClearCustomAis();
        public bool TryInterceptSpawn(BaseAi baseAi, SpawnRegion spawnRegion) => mSpawnRegionManager.TryInterceptSpawn(baseAi, spawnRegion);
        public bool TryInjectRandomCustomAi(BaseAi baseAi, SpawnRegion region) => mAiManager.TryInjectRandomCustomAi(baseAi, region);
        public bool TryInjectCustomBaseAi(BaseAi baseAi, SpawnRegion spawnRegion) => mAiManager.TryInjectCustomBaseAi(baseAi, spawnRegion);
        public bool TryInjectCustomAi(BaseAi baseAi, Type spawnType, SpawnRegion region) => mAiManager.TryInjectCustomAi(baseAi, spawnType, region);
        public bool TryRemoveCustomAi(BaseAi baseAi) => mAiManager.TryRemoveCustomAi(baseAi);
        public bool TryStart(BaseAi baseAi) => mAiManager.TryStart(baseAi);
        public bool TryStart(SpawnRegion spawnRegion) => mSpawnRegionManager.TryStart(spawnRegion);
        public bool TrySetAiMode(BaseAi baseAi, AiMode aiMode) => mAiManager.TrySetAiMode(baseAi, aiMode);
        public bool TryApplyDamage(BaseAi baseAi, float damage, float bleedOutTime, DamageSource damageSource) => mAiManager.TryApplyDamage(baseAi, damage, bleedOutTime, damageSource);
        /*
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
        private void InitializeLogger() => mLogger = new ComplexLogger<Main>();


        public void Log(
            string message, 
            FlaggedLoggingLevel logLevel, 
            bool toUConsole,
            string callerType,
            [CallerMemberName] string callerName = "")
        {
            mLogger.Log($"[{callerType}.{callerName}] {message}", logLevel, toUConsole ? LoggingSubType.uConsole : LoggingSubType.Normal);
        }

#endregion

    }
}
