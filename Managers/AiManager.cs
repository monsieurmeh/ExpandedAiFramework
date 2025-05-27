using ComplexLogger;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using MelonLoader.TinyJSON;
using ModData;
using UnityEngine;

namespace ExpandedAiFramework
{
    //This will act similar to in-game BaseAiManager, but for the ICustomAi wrappers. Things like tracking spawn variant persistency, etc
    //at some point I'll probably make a SpawnRegionManager too, which will do something similar but wrap up the spawn regions instead.

    public sealed class AiManager : BaseSubManager
    {
        //lots of cross-manager contamination happening as part of your 'get this shit working' push. Need to clean it up eventually

        private Dictionary<int, ICustomAi> mCustomAis = new Dictionary<int, ICustomAi>(); 
        private WeightedTypePicker<BaseAi> mTypePicker = new WeightedTypePicker<BaseAi>();
        private Dictionary<Type, ISpawnTypePickerCandidate> mSpawnSettingsDict = new Dictionary<Type, ISpawnTypePickerCandidate>();
        private DataManager mDataManager;
        private bool mInitializedScene = false;
        private string mLastSceneName;

        public Dictionary<int, ICustomAi> CustomAis { get { return mCustomAis; } }
        public WeightedTypePicker<BaseAi> TypePicker { get { return mTypePicker; } }
        public Dictionary<Type, ISpawnTypePickerCandidate> SpawnSettingsDict { get { return mSpawnSettingsDict; } }


        public AiManager(EAFManager manager, ISubManager[] subManagers) : base(manager, subManagers) { }


        public override void Initialize(EAFManager manager, ISubManager[] subManagers)
        {
            base.Initialize(manager, subManagers);
            mDataManager = mManager.DataManager; //should be ok since data manager always loads first
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
            RegisterBaseSpawnableAi(typeof(BaseDeer), BaseDeer.BaseDeerSettings); //sorry, deer
        }


        private void RegisterBaseSpawnableAi<T>(Type type, T settings) where T : JsonModSettings, ISpawnTypePickerCandidate
        {
            RegisterSpawnableAi(type, settings);
            settings.AddToModSettings(ModName);
        }


        public override void Shutdown()
        {
            ClearCustomAis();
            //SaveSpawnModDataProxies(); No! bad shutdown! do NOT serialize without actual save request!
            base.Shutdown();
        }


        public override void OnLoadScene()
        {
            base.OnLoadScene();
            mInitializedScene = false;
            CacheSpawnModDataProxies();
            ClearCustomAis();
        }


        private void CacheSpawnModDataProxies()
        {
            LogDebug($"Caching spawn mod data proxies to scene {mLastSceneName}!");
            List<SpawnModDataProxy> cachedProxies = mDataManager.GetCachedSpawnModDataProxies(mLastSceneName);
            cachedProxies.Clear();
            foreach (ICustomAi customAi in mCustomAis.Values)
            {
                LogDebug($"Caching spawn region mod data proxies with guid {customAi.ModDataProxy.Guid}");
                cachedProxies.Add(customAi.ModDataProxy);
            }
        }


        public override void OnInitializedScene(string sceneName)
        {
            base.OnInitializedScene(sceneName);
            mLastSceneName = mManager.CurrentScene;
            if (!mInitializedScene)
            {
                mInitializedScene = true;
                mDataManager.UncacheSpawnModDataProxies(mLastSceneName);
            }
        }


        public Type GetRandomSpawnType(BaseAi baseAi)
        {
            Type spawnType = mTypePicker.PickType(baseAi);
            if (spawnType == typeof(void))
            {
                spawnType = GetFallbackBaseSpawnableType(baseAi);
            }
            if (spawnType == typeof(void))
            {
                LogError("Could not find valid spawn type for base ai while pre-queuing spawns! what the heck bruh");
            }
            return spawnType;
        }


        [HideFromIl2Cpp]
        public bool RegisterSpawnableAi(Type type, ISpawnTypePickerCandidate spawnSettings)
        {
            if (mSpawnSettingsDict.TryGetValue(type, out _))
            {
                LogError($"Can't register {type} as it is already registered!", FlaggedLoggingLevel.Critical);
                return false;
            }
            LogAlways($"Registering type {type}");

            mSpawnSettingsDict.Add(type, spawnSettings);
            mTypePicker.AddWeight(type, spawnSettings.SpawnWeight, spawnSettings.CanSpawn);
            return true;
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

            Type spawnType = typeof(void);
            for (int i = 0, iMax = mSubManagers.Length; i < iMax; i++)
            {
                LogDebug($"Allowing submanager {mSubManagers[i]} to intercept spawn...");
                if (mSubManagers[i].ShouldInterceptSpawn(baseAi, region))
                {
                    LogDebug($"Spawn intercept from submanager {mSubManagers[i]}! new type: {mSubManagers[i].SpawnType}");
                    spawnType =mSubManagers[i].SpawnType;
                    break;
                }
            }
            if (spawnType == typeof(void))
            {
                LogDebug($"No submanager interceptions, attempting to randomly pick a valid spawn type...");
                spawnType = mTypePicker.PickType(baseAi);
            }
            if (spawnType == typeof(void))
            {
                LogDebug($"No spawn type available from type picker or manager overrides for base ai {baseAi.gameObject.name}, defaulting to fallback...");
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


        public SpawnModDataProxy GenerateNewSpawnModDataProxy(string scene, SpawnRegion spawnRegion, Type variantSpawnType)
        {
            if (spawnRegion == null)
            {
                LogDebug($"Cant generate a new spawn mod data proxy without parent region!");
                return null;
            }
            if (variantSpawnType == null)
            {
                LogDebug($"Can't generate new spawn mod data proxy with null variant spawn type!");
                return null;
            }
            //need to find a smarter way to bridge this working data gap...
            if (!mManager.SpawnRegionManager.CustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out ICustomSpawnRegion customSpawnRegion))
            {
                LogDebug($"Can't fetch wrapper for spawn region with hash code {spawnRegion.GetHashCode()}!");
                return null;
            }
            if (customSpawnRegion.ModDataProxy == null || customSpawnRegion.ModDataProxy.Guid == Guid.Empty)
            {
                LogDebug($"Couldnt fetch guid from spawn region with hash code {spawnRegion.GetHashCode()}!");
                return null;
            }
            SpawnModDataProxy newProxy = new SpawnModDataProxy(Guid.NewGuid(), scene, spawnRegion, variantSpawnType);
            newProxy.ParentGuid = customSpawnRegion.ModDataProxy.Guid;
            if (!mDataManager.TryRegisterActiveSpawnModDataProxy(newProxy))
            {
                LogDebug($"Couldnt register new spawn mod data proxy with guid {newProxy.Guid} due to guid collision!");
                return null;
            }
            /*
            if (queueForSpawn)
            {
                if (!mQueuedSpawnModDataProxiesByParentGuid.TryGetValue(newProxy.ParentGuid, out List<Guid> proxyList))
                {
                    proxyList = new List<Guid>();
                    mQueuedSpawnModDataProxiesByParentGuid.Add(newProxy.ParentGuid, proxyList);
                }
                mQueuedSpawnModDataProxiesByParentGuid[newProxy.ParentGuid].Add(newProxy.Guid);
            }
            */
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
                        proxy = GenerateNewSpawnModDataProxy(mLastSceneName, spawnRegion, spawnType);
                    }
                    if (proxy.ParentGuid == Guid.Empty)
                    {
                        //should probably do a "try connect to parent" method in datamanager for this instead.
                        LogDebug($"Triggered section that is going to disappear, figure out why!");
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
                        if (!mManager.SpawnRegionManager.CustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out ICustomSpawnRegion customSpawnRegion))
                        {
                            LogError($"Could not fetch custom spawn region wrapper to correlate parentless-spawnmoddataproxy! Aborting..");
                            return;
                        }
                        proxy.ParentGuid = customSpawnRegion.ModDataProxy.Guid;
                    }
                    mDataManager.TryRegisterActiveSpawnModDataProxy(proxy);
                }
                mCustomAis.Add(baseAi.GetHashCode(), (ICustomAi)baseAi.gameObject.AddComponent(Il2CppType.From(spawnType)));
                if (!mCustomAis.TryGetValue(baseAi.GetHashCode(), out ICustomAi customAi))
                {
                    LogError($"Critical error at ExpandedAiFramework.AugmentAi: newly created {spawnType} cannot be found in augment dictionary! Did its hash code change?", FlaggedLoggingLevel.Critical);
                    return;
                }
                customAi.Initialize(baseAi, GameManager.m_TimeOfDay, spawnRegion, proxy);
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
                UnityEngine.Object.Destroy(customAi.Self); //this *should* still leave the spawn mod data proxy in the guid dictionary though!!!
                mCustomAis.Remove(hashCode);
            }
        }
    }
}
