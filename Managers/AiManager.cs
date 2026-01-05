
using ExpandedAiFramework.Enums;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using TLD.TinyJSON;
using ModData;
using System.Security.AccessControl;
using UnityEngine;

namespace ExpandedAiFramework
{
    //Eventually, this needs to be a behavioral manager and not a spawnign manager for AIs.
    public sealed class AiManager : BaseSubManager
    {

        private Dictionary<int, CustomBaseAi> mCustomAis = new Dictionary<int, CustomBaseAi>();
        private Dictionary<Guid, CustomBaseAi> mCustomAisByGuid = new Dictionary<Guid, CustomBaseAi>();
        private WeightedTypePicker<BaseAi> mTypePicker;
        private Dictionary<Type, ISpawnTypePickerCandidate> mSpawnSettingsDict = new Dictionary<Type, ISpawnTypePickerCandidate>();
        private DataManager mDataManager;

        public Dictionary<int, CustomBaseAi> CustomAis { get { return mCustomAis; } }
        public Dictionary<Guid, CustomBaseAi> CustomAisByGuid { get { return mCustomAisByGuid; } }
        public WeightedTypePicker<BaseAi> TypePicker { get { return mTypePicker; } }
        public Dictionary<Type, ISpawnTypePickerCandidate> SpawnSettingsDict { get { return mSpawnSettingsDict; } }


        public AiManager(EAFManager manager) : base(manager) { }


        public override void Initialize(EAFManager manager)
        {
            base.Initialize(manager);
            mDataManager = mManager.DataManager;
            mTypePicker = new WeightedTypePicker<BaseAi>(GetFallbackBaseSpawnableType, OnSpawnTypePicked);
            mTypePicker.StartWorker();
            RegisterBaseSpawnableAiSettings();
            RegisterBaseSpawnableAis();
        }


        private void RegisterBaseSpawnableAiSettings()
        {
            BaseWolf.BaseWolfSettings = new BaseWolfSettings(Path.Combine(DataFolderPath, $"{nameof(BaseWolf)}"));
            BaseTimberwolf.BaseTimberwolfSettings = new BaseTimberwolfSettings(Path.Combine(DataFolderPath, $"{nameof(BaseTimberwolf)}"));
            BaseBear.BaseBearSettings = new BaseBearSettings(Path.Combine(DataFolderPath, $"{nameof(BaseBear)}"));
            BaseCougar.BaseCougarSettings = new BaseCougarSettings(Path.Combine(DataFolderPath, $"{nameof(BaseCougar)}"));
            BaseMoose.BaseMooseSettings = new BaseMooseSettings(Path.Combine(DataFolderPath, $" {nameof(BaseMoose)}"));
            BaseRabbit.BaseRabbitSettings = new BaseRabbitSettings(Path.Combine(DataFolderPath, $"{nameof(BaseRabbit)}"));
            BasePtarmigan.BasePtarmiganSettings = new BasePtarmiganSettings(Path.Combine(DataFolderPath, $"{nameof(BasePtarmigan)}"));
            BaseDeer.BaseDeerSettings = new BaseDeerSettings(Path.Combine(DataFolderPath, $"{nameof(BaseDeer)}"));
        }


        private void RegisterBaseSpawnableAis()
        {
            RegisterBaseSpawnableAi(typeof(BaseWolf), BaseWolf.BaseWolfSettings);
            RegisterBaseSpawnableAi(typeof(BaseTimberwolf), BaseTimberwolf.BaseTimberwolfSettings);
            RegisterBaseSpawnableAi(typeof(BaseBear), BaseBear.BaseBearSettings);
            RegisterBaseSpawnableAi(typeof(BaseCougar), BaseCougar.BaseCougarSettings);
            RegisterBaseSpawnableAi(typeof(BaseMoose), BaseMoose.BaseMooseSettings);
            RegisterBaseSpawnableAi(typeof(BaseRabbit), BaseRabbit.BaseRabbitSettings);
            RegisterBaseSpawnableAi(typeof(BasePtarmigan), BasePtarmigan.BasePtarmiganSettings);
            RegisterBaseSpawnableAi(typeof(BaseDeer), BaseDeer.BaseDeerSettings);
        }


        private void RegisterBaseSpawnableAi<T>(Type type, T settings) where T : JsonModSettings, ISpawnTypePickerCandidate
        {
            RegisterSpawnableAi(type, settings);
            settings.AddToModSettings(ModName);
        }


        public override void OnSaveGame()
        {
            base.OnSaveGame();
            foreach (CustomBaseAi baseAi in mCustomAis.Values)
            {
                baseAi.Save();
            }
        }


        public override void Shutdown()
        {
            ClearCustomAis(false);
            base.Shutdown();
        }


        public override void OnLoadScene(string sceneName)
        {
            base.OnLoadScene(sceneName);
            ClearCustomAis(true);
        }


        public override void OnQuitToMainMenu()
        {
            ClearCustomAis(false);
            mTypePicker.Clear();
            base.OnQuitToMainMenu();
        }


        public Type GetRandomSpawnType(BaseAi baseAi)
        {
            Type spawnType = mTypePicker.PickType(baseAi);
            if (spawnType == typeof(void))
            {
                Error($"Could not find valid spawn type for base ai while pre-queuing spawns!");
            }
            return spawnType;
        }


        public bool RegisterSpawnableAi(Type type, ISpawnTypePickerCandidate spawnSettings)
        {
            if (mSpawnSettingsDict.TryGetValue(type, out _))
            {
                Error($"Can't register {type} as it is already registered!");
                return false;
            }
            Log($"Registering type {type}", LogCategoryFlags.AiManager);

            mSpawnSettingsDict.Add(type, spawnSettings);
            mTypePicker.AddWeight(type, spawnSettings.SpawnWeight, spawnSettings.CanSpawn);
            return true;
        }


        public void ClearCustomAis(bool despawn)
        {
            if (despawn)
            {
                foreach (CustomBaseAi customAi in mCustomAis.Values)
                {
                    TryRemoveCustomAi(customAi.BaseAi);
                }
            }
            mCustomAis.Clear();
            mCustomAisByGuid.Clear();
        }


        public bool TryInjectCustomBaseAi(BaseAi baseAi, out CustomBaseAi newCustomBaseAi)
        {
            return TryInjectCustomBaseAi(baseAi, baseAi.m_SpawnRegionParent, out newCustomBaseAi);
        }


        private Type GetFallbackBaseSpawnableType(BaseAi baseAi)
        {
            switch (baseAi.m_AiSubType)
            {
                case AiSubType.Wolf: return baseAi.Timberwolf == null ? typeof(BaseWolf) : typeof(BaseTimberwolf);
                case AiSubType.Rabbit: return baseAi.Ptarmigan == null ? typeof(BaseRabbit) : typeof(BasePtarmigan);
                case AiSubType.Bear: return typeof(BaseBear);
                case AiSubType.Moose: return typeof(BaseMoose);
                case AiSubType.Stag: return typeof(BaseDeer);
                case AiSubType.Cougar: return typeof(BaseCougar);
                default:
                    Error($"Can't find fallback custom base ai for baseAi {baseAi.gameObject.name}!");
                    return typeof(void);
            }
        }


        public bool TryInjectCustomBaseAi(BaseAi baseAi, SpawnRegion spawnRegion, out CustomBaseAi newCustomBaseAi, SpawnModDataProxy proxy = null)
        {
            return TryInjectCustomAi(baseAi, GetFallbackBaseSpawnableType(baseAi), spawnRegion, out newCustomBaseAi, proxy);
        }


        public bool TryInjectCustomAi(BaseAi baseAi, Type spawnType, SpawnRegion region, out CustomBaseAi newCustomBaseAi, SpawnModDataProxy proxy = null)
        {
            InjectCustomAi(baseAi, spawnType, region, out newCustomBaseAi, proxy);
            return true;
        }


        public bool TryInterceptCarcassSpawn(BaseAi baseAi)
        {
            if (baseAi == null)
            {
                Error("TryInterceptCarcassSpawn given null base ai, aborting!");
                return false;
            }
            if (mCustomAis.ContainsKey(baseAi.GetHashCode()))
            {
                Log("Already wrapped this ai, no need for a second on transition to carcass state.");
                return true;
            }
            InjectCustomAi(baseAi, GetFallbackBaseSpawnableType(baseAi), null, out _, null, true);
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
            if (!CustomAis.TryGetValue(baseAi.GetHashCode(), out CustomBaseAi customAi))
            {
                return false;
            }
            customAi.OverrideStart();
            return true;
        }


        public bool TrySetAiMode(BaseAi baseAi, AiMode aiMode)
        {
            if (!CustomAis.TryGetValue(baseAi.GetHashCode(), out CustomBaseAi customAi))
            {
                return false;
            }
            customAi.SetAiMode(aiMode);
            return true;
        }


        public bool TryApplyDamage(BaseAi baseAi, float damage, float bleedOutTime, DamageSource damageSource)
        {
            if (!CustomAis.TryGetValue(baseAi.GetHashCode(), out CustomBaseAi customAi))
            {
                return false;
            }
            customAi.ApplyDamage(damage, bleedOutTime, damageSource);
            return true;
        }


        private void InjectCustomAi(BaseAi baseAi, Type spawnType, SpawnRegion spawnRegion, out CustomBaseAi newCustomBaseAi, SpawnModDataProxy proxy, bool bypassProxy = false)
        {
            newCustomBaseAi = null;
            try
            {
                if (!bypassProxy)
                {
                    if (proxy == null)
                    {
                        Error($"No longer valid route to generate proxy! Send one down the chain.");
                        return;
                    }
                    if (proxy.ParentGuid == Guid.Empty)
                    {

                        Error($"No longer valid route to generate parent proxy guid! Ensure it's set before sending down the chain.");
                        return;
                    }
                }
                newCustomBaseAi = (CustomBaseAi)baseAi.gameObject.AddComponent(Il2CppType.From(spawnType));
                mCustomAis.Add(baseAi.GetHashCode(), newCustomBaseAi);
                if (newCustomBaseAi.ModDataProxy != null)
                {
                    mCustomAisByGuid.Add(newCustomBaseAi.ModDataProxy.Guid, newCustomBaseAi);
                }
                newCustomBaseAi.Initialize(baseAi, GameManager.m_TimeOfDay, spawnRegion, proxy);
            }
            catch (Exception e)
            {
                Error($"Error during injection: {e}");
            }
        }


        private void RemoveCustomAi(int hashCode)
        {
            if (mCustomAis.TryGetValue(hashCode, out CustomBaseAi customAi))
            {
                customAi.Save();
                //UnityEngine.Object.Destroy(customAi.Self);
                mCustomAis.Remove(hashCode);
                if (customAi.ModDataProxy != null)
                {
                    mCustomAisByGuid.Remove(customAi.ModDataProxy.Guid);
                }
            }
        }


        private void OnSpawnTypePicked(BaseAi baseAi, Type type)
        {
            if (!mSpawnSettingsDict.TryGetValue(type, out var spawnSettings))
            {
                Error($"Couldn't fetch spawn settings for type {type}!");
                return;
            }
            spawnSettings.OnPick();
        }
    }
}
