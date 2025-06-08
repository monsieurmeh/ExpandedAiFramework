using ComplexLogger;
using ExpandedAiFramework.Enums;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using MelonLoader.TinyJSON;
using ModData;
using System.Security.AccessControl;
using UnityEngine;

namespace ExpandedAiFramework
{
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


        public AiManager(EAFManager manager, ISubManager[] subManagers) : base(manager, subManagers) { }


        public override void Initialize(EAFManager manager, ISubManager[] subManagers)
        {
            base.Initialize(manager, subManagers);
            mDataManager = mManager.DataManager;
            mTypePicker = new WeightedTypePicker<BaseAi>(GetFallbackBaseSpawnableType);
            RegisterBaseSpawnableAis();
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


        public override void Shutdown()
        {
            ClearCustomAis();
            base.Shutdown();
        }


        public override void OnLoadScene(string sceneName)
        {
            base.OnLoadScene(sceneName);
            ClearCustomAis();
        }


        public Type GetRandomSpawnType(BaseAi baseAi)
        {
            Type spawnType = mTypePicker.PickType(baseAi);
            if (spawnType == typeof(void))
            {
                LogError($"[AiManager.GetRandomSpawnType] Could not find valid spawn type for base ai while pre-queuing spawns!");
            }
            return spawnType;
        }


        [HideFromIl2Cpp]
        public bool RegisterSpawnableAi(Type type, ISpawnTypePickerCandidate spawnSettings)
        {
            if (mSpawnSettingsDict.TryGetValue(type, out _))
            {
                LogError($"[AiManager.RegisterSpawnableAi] Can't register {type} as it is already registered!", FlaggedLoggingLevel.Critical);
                return false;
            }
            LogAlways($"[EAFManager.RegisterSpawnableAi] Registering type {type}");

            mSpawnSettingsDict.Add(type, spawnSettings);
            mTypePicker.AddWeight(type, spawnSettings.SpawnWeight, spawnSettings.CanSpawn);
            return true;
        }


        public void ClearCustomAis()
        {
            foreach (CustomBaseAi customAi in mCustomAis.Values)
            {
                TryRemoveCustomAi(customAi.BaseAi);
            }
            mCustomAis.Clear();
            mCustomAisByGuid.Clear();
        }


        public bool TryInjectRandomCustomAi(BaseAi baseAi, SpawnRegion region)
        {
            if (baseAi == null)
            {
                LogTrace($"[AiManager.TryInjectRandomCustomAi] Null base ai, can't augment.");
                return false;
            }
            if (mCustomAis.ContainsKey(baseAi.GetHashCode()))
            {
                LogTrace($"[AiManager.TryInjectRandomCustomAi] BaseAi in dictionary, can't augment.");
                return false;
            }
            Type spawnType = typeof(void);
            for (int i = 0, iMax = mSubManagers.Length; i < iMax; i++)
            {
                LogTrace($"[AiManager.TryInjectRandomCustomAi] Allowing submanager {mSubManagers[i]} to intercept spawn...");
                if (mSubManagers[i].ShouldInterceptSpawn(baseAi, region))
                {
                    LogTrace($"[AiManager.TryInjectRandomCustomAi] Spawn intercept from submanager {mSubManagers[i]}! new type: {mSubManagers[i].SpawnType}");
                    spawnType =mSubManagers[i].SpawnType;
                    break;
                }
            }
            if (spawnType == typeof(void))
            {
                LogTrace($"[AiManager.TryInjectRandomCustomAi] No submanager interceptions, attempting to randomly pick a valid spawn type...");
                spawnType = mTypePicker.PickType(baseAi);
            }
            if (spawnType == typeof(void))
            {
                LogTrace($"[AiManager.TryInjectRandomCustomAi] No spawn type available from type picker or manager overrides for base ai {baseAi.gameObject.name}, defaulting to fallback...");
                return TryInjectCustomBaseAi(baseAi, region);
            }
            return TryInjectCustomAi(baseAi, spawnType, region);
        }


        public bool TryInjectCustomBaseAi(BaseAi baseAi)
        {
            return TryInjectCustomBaseAi(baseAi, baseAi.m_SpawnRegionParent);
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
                    LogError($"Can't find fallback custom base ai for baseAi {baseAi.gameObject.name}!");
                    return typeof(void);
            }
        }


        public bool TryInjectCustomBaseAi(BaseAi baseAi, SpawnRegion spawnRegion, SpawnModDataProxy proxy = null)
        {
            return TryInjectCustomAi(baseAi, GetFallbackBaseSpawnableType(baseAi), spawnRegion, proxy);
        }


        public bool TryInjectCustomAi(BaseAi baseAi, Type spawnType, SpawnRegion region, SpawnModDataProxy proxy = null)
        {
            InjectCustomAi(baseAi, spawnType, region, proxy);
            return true;
        }


        public bool TryInterceptCarcassSpawn(BaseAi baseAi)
        {
            if (baseAi == null)
            {
                LogError("TryInterceptCarcassSpawn given null base ai, aborting!");
                return false;
            }
            if (mCustomAis.ContainsKey(baseAi.GetHashCode()))
            {
                LogTrace("Already wrapped this ai, no need for a second on transition to carcass state.");
                return false;
            }
            InjectCustomAi(baseAi, GetFallbackBaseSpawnableType(baseAi), null, null, true);
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


        public SpawnModDataProxy GenerateNewSpawnModDataProxy(string scene, SpawnRegion spawnRegion, Type variantSpawnType)
        {
            if (spawnRegion == null)
            {
                LogTrace($"[AiManager.GenerateNewSpawnModDataProxy] Cant generate a new spawn mod data proxy without parent region!");
                return null;
            }
            //need to find a smarter way to bridge this working data gap...
            if (!mManager.SpawnRegionManager.CustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out CustomBaseSpawnRegion customSpawnRegion))
            {
                LogTrace($"[AiManager.GenerateNewSpawnModDataProxy] Can't fetch wrapper for spawn region with hash code {spawnRegion.GetHashCode()}!");
                return null;
            }
            if (customSpawnRegion.ModDataProxy == null || customSpawnRegion.ModDataProxy.Guid == Guid.Empty)
            {
                LogTrace($"[AiManager.GenerateNewSpawnModDataProxy] Couldnt fetch guid from spawn region with hash code {spawnRegion.GetHashCode()}!");
                return null;
            }
            return GenerateNewSpawnModDataProxy(scene, customSpawnRegion.ModDataProxy.Guid, spawnRegion.AiSubTypeSpawned, variantSpawnType, spawnRegion.transform.position, spawnRegion.transform.rotation);
        }


        public SpawnModDataProxy GenerateNewSpawnModDataProxy(string scene, Guid parentGuid, AiSubType spawnType, Type variantSpawnType, Vector3 position, Quaternion rotation)
        {
            if (variantSpawnType == null)
            {
                LogTrace($"[AiManager.GenerateNewSpawnModDataProxy] Can't generate new spawn mod data proxy with null variant spawn type!");
                return null;
            }
            SpawnModDataProxy newProxy = new SpawnModDataProxy(Guid.NewGuid(), scene, position, rotation, spawnType, variantSpawnType);
            newProxy.ParentGuid = parentGuid;
            if (!mDataManager.TryRegisterActiveSpawnModDataProxy(newProxy))
            {
                LogTrace($"[AiManager.GenerateNewSpawnModDataProxy] Couldnt register new spawn mod data proxy with guid {newProxy.Guid} due to guid collision!");
                return null;
            }
            foreach (ISubManager subManager in mManager.SubManagerArray)
            {
                if (variantSpawnType == subManager.SpawnType)
                {
                    subManager.PostProcessNewSpawnModDataProxy(newProxy);
                    break;
                }
            }
            return newProxy;
        }


        private void InjectCustomAi(BaseAi baseAi, Type spawnType, SpawnRegion spawnRegion, SpawnModDataProxy proxy, bool bypassProxy = false)
        {
            try
            {
                if (!bypassProxy)
                {
                    if (proxy == null)
                    {
                        proxy = GenerateNewSpawnModDataProxy(mManager.CurrentScene, spawnRegion, spawnType);
                    }
                    if (proxy.ParentGuid == Guid.Empty)
                    {
                        //should probably do a "try connect to parent" method in datamanager for this instead.
                        LogTrace($"Triggered section that is going to disappear, figure out why!");
                        bool fixedParentGuid = true;
                        if (mManager == null)
                        {
                            LogError("Null manager!");
                            fixedParentGuid = false;
                        }
                        if (fixedParentGuid && mManager.SpawnRegionManager == null)
                        {
                            LogError("Null mManager.SpawnRegionManager!");
                            fixedParentGuid = false;
                        }
                        if (fixedParentGuid && mManager.SpawnRegionManager.CustomSpawnRegions == null)
                        {
                            LogError("Null mManager.SpawnRegionManager.CustomSpawnRegions!");
                            fixedParentGuid = false;
                        }
                        if (fixedParentGuid && spawnRegion == null)
                        {
                            LogError("Null spawnregion! how the heck did we even get here like this???");
                            fixedParentGuid = false;
                        }
                        if (!fixedParentGuid)
                        {
                            return;
                        }
                        if (!mManager.SpawnRegionManager.CustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out CustomBaseSpawnRegion customSpawnRegion))
                        {
                            LogError($"Could not fetch custom spawn region wrapper to correlate parentless-spawnmoddataproxy! Aborting..");
                            return;
                        }
                        proxy.ParentGuid = customSpawnRegion.ModDataProxy.Guid;
                    }
                    mDataManager.TryRegisterActiveSpawnModDataProxy(proxy);
                }
                CustomBaseAi newCustomBaseAi = (CustomBaseAi)baseAi.gameObject.AddComponent(Il2CppType.From(spawnType));
                mCustomAis.Add(baseAi.GetHashCode(), newCustomBaseAi);
                if (newCustomBaseAi.ModDataProxy != null)
                {
                    mCustomAisByGuid.Add(newCustomBaseAi.ModDataProxy.Guid, newCustomBaseAi);
                }
                newCustomBaseAi.Initialize(baseAi, GameManager.m_TimeOfDay, spawnRegion, proxy);
            }
            catch (Exception e)
            {
                LogError($"[AiManager.InjectCustomAi] Error during injection: {e}");
            }
        }


        private void RemoveCustomAi(int hashCode)
        {
            if (mCustomAis.TryGetValue(hashCode, out CustomBaseAi customAi))
            {
                customAi.Despawn(GetCurrentTimelinePoint());
                UnityEngine.Object.Destroy(customAi.Self);
                mCustomAis.Remove(hashCode);
                if (customAi.ModDataProxy != null)
                {
                    mCustomAisByGuid.Remove(customAi.ModDataProxy.Guid);
                }
            }
        }
    }
}
