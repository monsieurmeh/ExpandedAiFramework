using UnityEngine;
using Il2CppInterop.Runtime;
using MelonLoader.TinyJSON;
using Il2CppTLD.AddressableAssets;
using static ExpandedAiFramework.Utility;
using Il2Cpp;



namespace ExpandedAiFramework.CompanionWolfMod
{
    public class CompanionWolfManager : ISubManager
    {
        public const string WolfPrefabString = "WILDLIFE_Wolf";

        protected EAFManager mManager;
        protected GameObject mWolfPrefab;
        protected Transform mTamedSpawnTransform;
        protected CompanionWolf mInstance;
        protected CompanionWolfData mData;
        protected bool mInitialized = false;
        protected bool mShouldCheckForSpawnTamedCompanion = false;
        protected float mLastTriggeredCheckForSpawnTamedCompanionTime = 0.0f;
        protected bool mShouldShowInfoScreen = false;
        protected bool mSpawnOneFlag = false;

        public CompanionWolfData Data { get { return mData; } set { mData = value; } }
        public CompanionWolf Instance { get { return mInstance; } set { mInstance = value; } }
        public Type SpawnType { get { return typeof(CompanionWolf); } }
        public GameObject WolfPrefab { get { return mWolfPrefab; } set { mWolfPrefab = value; } }
        public bool ShouldShowInfoScreen { get { return mShouldShowInfoScreen; } }
        public bool SpawnOneFlag { get { return mSpawnOneFlag; } set { mSpawnOneFlag = value; } }


        public void Initialize(EAFManager manager)
        {
            mManager = manager;
            mInitialized = true;
            LogVerbose("CompanionWolfManager initialized!");
        }


        public void PostProcessNewSpawnModDataProxy(SpawnModDataProxy proxy) { proxy.ForceSpawn = true; }


        public bool ShouldInterceptSpawn(CustomSpawnRegion region) => false;


        public void Shutdown() { }


        public void OnStartNewGame() { }


        public void OnLoadGame() { }


        public void OnLoadScene(string sceneName)
        {
            SpawnOneFlag = false;
            if (mInstance != null)
            {
                GameObject.Destroy(mInstance.gameObject.transform.parent.gameObject); //destroy the whole thing
            }
        }


        public void TryLoadCompanionData()
        {
            if (mData == null)
            {
                mData = new CompanionWolfData();

                string cWolfDataJson = mManager.LoadData("CompanionWolfMod");

                if (cWolfDataJson == null)
                {
                    LogVerbose("No companionwolf data found. explore and find one! :) ");
                    return;
                }

                Variant cWolfDataVariant = JSON.Load(cWolfDataJson);

                if (cWolfDataVariant == null)
                {
                    LogWarning($"Found serialized companionwolf data, but could not load to populatable variant!");
                    return;
                }

                JSON.Populate(cWolfDataVariant, mData);
                LogTrace($"Companion data reloaded. Connected: {mData.Connected} | Tamed: {mData.Tamed} | Calories: {mData.CurrentCalories} | Affection: {mData.CurrentAffection} | Outdoors: {GameManager.m_ActiveSceneSet.m_IsOutdoors}");
            }
        }



        public void OnInitializedScene(string sceneName)
        {
            if (GameManager.m_ActiveSceneSet != null && GameManager.m_ActiveSceneSet.m_IsOutdoors && mData != null && mData.Tamed && mInstance == null)
            {
                mShouldCheckForSpawnTamedCompanion = true;
                mLastTriggeredCheckForSpawnTamedCompanionTime = Time.time;
            }
        }


        public void OnSaveGame()
        {   
            if (mData == null)
            {
                //Can happen on new game loads I guess.
                return;
            }
            mData.LastDespawnTime = GetCurrentTimelinePoint();
            string json = JSON.Dump(mData);
            if (json != null)
            {
                mManager.SaveData(json, "CompanionWolfMod");
            }
        }


        public void OnQuitToMainMenu()
        {
            mData = null;
        }


        public void Update()
        {
            if (mShouldCheckForSpawnTamedCompanion && Time.time - mLastTriggeredCheckForSpawnTamedCompanionTime > 2.0f)
            {
                mShouldCheckForSpawnTamedCompanion = false;
                SpawnCompanion();
            }
            if (mInstance != null)
            {
                mInstance.UpdateStatusText();
            }
        }


        public void SpawnCompanion()
        {
            if (Data == null)
            {
                LogTrace("No data found, cannot spawn companion!");
                return;
            }
            if (!Data.Tamed)
            {
                LogTrace("Companion is not tamed, go find and tame one!");
                return;
            }
            if (mInstance != null)
            {
                LogVerbose("Companion is already here!");
                return;
            }
            GameObject wolfContainer = new GameObject("CompanionWolfContainer");
            Vector3 playerPos = GameManager.m_PlayerManager.m_LastPlayerPosition;
            AiUtils.GetClosestNavmeshPos(out Vector3 validPos, playerPos, playerPos);
            GameObject newWolf = AssetHelper.SafeInstantiateAssetAsync(WolfPrefabString).WaitForCompletion();
            newWolf.transform.position = validPos;
            LogVerbose("Successfully instantiated: " + newWolf.name);
            if (newWolf == null)
            {
                LogWarning("Couldn't instantiate new wolf prefab!");
                return;
            }
            newWolf.transform.position = validPos;
            BaseAi baseAi = newWolf.GetComponentInChildren<BaseAi>();
            if (baseAi == null)
            {
                LogError("Coult not find BaseAi script attached to wolf prefab!");
                return;
            }
            LogVerbose($"Creating move agent...");
            baseAi.CreateMoveAgent(wolfContainer.transform);
            LogVerbose($"Reparenting...");
            baseAi.ReparentBaseAi(wolfContainer.transform);
            LogVerbose($"Wrapping...");
            if (!mManager.AiManager.TryInjectCustomAi(baseAi, typeof(CompanionWolf), null, out CustomBaseAi wrapper))
            {
                LogError("Could not re-inject CompanionWolf!");
                return;
            }
            LogVerbose($"Grabbing Instance..");
            mInstance = wrapper as CompanionWolf;
            if (mInstance == null)
            {
                LogError("Instantiated companion wolf but script is not correct!");
                return;
            }
            wrapper.BaseAi.m_MoveAgent.transform.position = validPos;
            wrapper.BaseAi.m_MoveAgent.Warp(validPos, 5.0f, true, -1);
            mShouldCheckForSpawnTamedCompanion = false;
            BaseAiManager.Remove(wrapper.BaseAi); // justin case, this should no longer even be present in there
            LogTrace($"Companion wolf loaded!");
        }



        #region console commands

        public const string CWolfCommandString = "cwolf";
        public const string CWolfCommandString_Tamed = "tamed";
        public const string CWolfCommandString_Untamed = "untamed";

        public const string CWoldCommandString_OnCommandSupportedTypes =
                                                $"{CommandString_Help}" +
                                                $"{CommandString_Create} " +
                                                $"{CommandString_Delete} " +
                                                $"{CommandString_GoTo} " +
                                                $"{CommandString_Spawn}" + 
                                                $"{CommandString_Info} ";


        public const string CWoldCommandString_HelpSupportedTypes =
                                         $"{CommandString_Create} " +
                                         $"{CommandString_Delete} " +
                                         $"{CommandString_GoTo} " +
                                         $"{CommandString_Spawn}" +
                                         $"{CommandString_Info} ";


        public const string CWoldCommandString_CreateTypes =
                         $"{CWolfCommandString_Tamed} " +
                         $"{CWolfCommandString_Untamed} ";





        internal static void Console_OnCommand()
        {
            if (!Manager.SubManagers.TryGetValue(typeof(CompanionWolf), out ISubManager subManager))
            {
                LogError("Could not fetch CompanionWolfManager instance!");
                return;
            }

            if (subManager is not CompanionWolfManager instance)
            {
                LogError("Could not fetch CompanionWolfManager instance!");
                return;
            }

            string command = uConsole.GetString().ToLowerInvariant();
            if (command == null)
            {
                LogAlways($"Available commands: {CWoldCommandString_OnCommandSupportedTypes}");
            }
            switch (command)
            {
                case CommandString_Help: instance.Console_Help(); break;
                case CommandString_Create: instance.Console_Create(); break;
                case CommandString_Delete: instance.Console_Delete(); break;
                case CommandString_GoTo: instance.Console_GoTo(); break;
                case CommandString_Spawn: instance.Console_Spawn(); break;
                case CommandString_Info: instance.Console_Info(); break;
                default: LogWarning($"Unknown command: {command}"); break;
            }
        }


        private void Console_Help()
        {
            string command = uConsole.GetString();
            if (command == null || command.Length == 0)
            {
                LogAlways($"Supported commands: {CWoldCommandString_HelpSupportedTypes}");
                return;
            }
            switch (command.ToLower())
            {
                case CommandString_Create:
                    LogAlways($"Attempts to create an tamed or untambed companion. Syntax: '{CWolfCommandString} {CommandString_Create} <type>'. Supported types: {CWoldCommandString_CreateTypes}");
                    return;
                case CommandString_Delete:
                    LogAlways($"Attempts to disconnect current tamed or untamed companion. Syntax: '{CWolfCommandString} {CommandString_Delete}'");
                    return;
                case CommandString_GoTo:
                    LogAlways($"Attempts to teleport current tamed or untamed companion. Syntax: '{CommandString} {CommandString_GoTo}'");
                    return;
                case CommandString_Spawn:
                    LogAlways($"Attempts to spawn current tamed or untamed companion. Syntax: '{CommandString} {CommandString_Spawn}'");
                    return;
                case CommandString_Info:
                    LogAlways($"Attempts to readout info on current tamed or untamed companion. Syntax: '{CommandString} {CommandString_Info}'");
                    return;
                default:
                    LogAlways($"Unknown comand '{command.ToLower()}'!");
                    return;
            }
        }



        public void Console_Create()
        {
            if (mData == null)
            {
                LogAlways($"No data to {CommandString_Create}!");
                return;
            }
            if (mData.Connected)
            {
                LogAlways($"Companion wolf already created! To force switch state, delete current and re-create in preferred state.");
                return;
            }

            string type = uConsole.GetString();
            if (!IsTypeSupported(type, CommandString_CreateSupportedTypes)) return;

            switch (type)
            {
                case CWolfCommandString_Untamed:
                    ForceCreateUntamedCompanionWolf();
                    return;
                case CWolfCommandString_Tamed:
                    ForceCreateTamedCompanionWolf();
                    return;
                default:
                    LogAlways($"Unknown type '{type}'!");
                    return;
            }
        }


        public void Console_Delete()
        {
            if (mData == null)
            {
                LogAlways($"No data to {CommandString_Delete}!");
                return;
            }
            if (!mData.Connected)
            {
                LogAlways($"No connected companion wolf to {CommandString_Delete}!");
                return;
            }
            if (mInstance != null)
            {
                GameObject.Destroy(mInstance);
            }
            mData.Disconnect();
            LogAlways($"{CommandString_Delete} companion wolf successful!");
        }


        public void Console_GoTo()
        {
            if (mData == null)
            {
                LogAlways($"No data to {CommandString_GoTo}!");
                return;
            }
            if (!mData.Connected)
            {
                LogAlways($"No connected companion wolf to {CommandString_GoTo}!");
                return;
            }
            if (mInstance == null)
            {
                LogAlways($"No spawned companion wolf to {CommandString_GoTo}!");
                return;
            }
            Teleport(mInstance.transform.position, mInstance.transform.rotation);
            LogAlways($"{CommandString_GoTo} companion wolf successful!");
        }


        public void Console_Spawn()
        {
            if (mData == null)
            {
                LogAlways($"No data to {CommandString_Spawn}!");
                return;
            }
            if (!mData.Connected)
            {
                LogAlways($"No connected companion wolf to {CommandString_Spawn}!");
                return;
            }
            if (mInstance != null)
            {
                LogAlways($"Companion wolf is already in scene, cannot {CommandString_Spawn}!");
                return;
            }
            SpawnCompanion();
            LogAlways($"{CommandString_Spawn} companion wolf successful!");
        }



        public void Console_Info()
        {
            if (mData == null)
            {
                LogAlways($"No data to {CommandString_Info}!");
                return;
            }
            if (!mData.Connected)
            {
                LogAlways($"No connected companion wolf to {CommandString_Info}!");
                return;
            }
            mShouldShowInfoScreen = !mShouldShowInfoScreen;
            LogAlways($"{CommandString_Info} companion wolf successful!");
        }


        private void ForceCreateUntamedCompanionWolf()
        {
            LogAlways($"Havent created this yet, set the spawn weight high and fly around. Eventually when I support custom spawn region creation this command will create one next to the player to respawn it until it disappears after settings timer.");
            return;
        }


        private void ForceCreateTamedCompanionWolf()
        {
            mData.Connect();
            mData.Tamed = true;
            mData.CurrentAffection = CompanionWolf.CompanionWolfSettings.AffectionRequirement;
            mData.CurrentCalories = CompanionWolf.CompanionWolfSettings.MaximumCalorieIntake * 0.5f;
            SpawnCompanion();
        }

        #endregion
    }
}


