using ComplexLogger;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using MelonLoader.TinyJSON;
using UnityEngine;

namespace ExpandedAiFramework
{
    //This will act similar to in-game BaseAiManager, but for the ICustomAi wrappers. Things like tracking spawn variant persistency, etc
    //at some point I'll probably make a SpawnRegionManager too, which will do something similar but wrap up the spawn regions instead.

   

    public sealed class AiManager : BaseSubManager
    {

        private Dictionary<Guid, SpawnModDataProxy> mSpawnModDataProxies = new Dictionary<Guid, SpawnModDataProxy>();
        private Dictionary<int, ICustomAi> mCustomAis = new Dictionary<int, ICustomAi>(); 
        private WeightedTypePicker<BaseAi> mTypePicker = new WeightedTypePicker<BaseAi>();
        private Dictionary<Type, ISpawnTypePickerCandidate> mSpawnSettingsDict = new Dictionary<Type, ISpawnTypePickerCandidate>();
        //private float mCheckForMissingScriptsTime = 0.0f;
        //private bool mNeedToCheckForMissingScripts = false;
        private bool mInitializedScene = false;
        private string mLastSceneName;

        public Dictionary<int, ICustomAi> CustomAis { get { return mCustomAis; } }
        public WeightedTypePicker<BaseAi> TypePicker { get { return mTypePicker; } }
        public Dictionary<Type, ISpawnTypePickerCandidate> SpawnSettingsDict { get { return mSpawnSettingsDict; } }


        public AiManager(EAFManager manager, ISubManager[] subManagers) : base(manager, subManagers) { }


        public override void Initialize(EAFManager manager, ISubManager[] subManagers)
        {
            base.Initialize(manager, subManagers);
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


        public override void Update()
        {
            base.Update();
            /* After finding CarcassSite.Manager.TrySpawnCarcassSite, i think we're good without this now
            if (mNeedToCheckForMissingScripts && Time.time - mCheckForMissingScriptsTime >= 2.0f)
            {
                mNeedToCheckForMissingScripts = false;
                foreach (BaseAi baseAi in GameObject.FindObjectsOfType<BaseAi>())
                {
                    if (baseAi != null && baseAi.gameObject != null && !baseAi.gameObject.TryGetComponent(out CustomAiBase customAi) && baseAi.m_SpawnRegionParent != null)
                    {
                        TryInjectCustomBaseAi(baseAi, baseAi.m_SpawnRegionParent); 
                    }
                }
            }
            */
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
            ClearCustomAis();
            SaveSpawnModDataProxies();
        }

        private void SaveSpawnModDataProxies()
        {
            string json = JSON.Dump(mSpawnModDataProxies.Values.ToList(), EncodeOptions.PrettyPrint | EncodeOptions.NoTypeHints);
            mManager.SaveData(json, $"{mLastSceneName}_SpawnModDataProxies");
        }


        public override void OnInitializedScene(string sceneName)
        {
            base.OnInitializedScene(sceneName);
            mLastSceneName = mManager.CurrentScene;
            InitializeSpawnModDataProxies(!mInitializedScene);
            mInitializedScene = true;
        }


        private void InitializeSpawnModDataProxies( bool firstPass = false)
        {
            if (firstPass)
            {
                mSpawnModDataProxies.Clear();
            }

            List<SpawnModDataProxy> spawnDataProxies = new List<SpawnModDataProxy>();
            string proxiesString = mManager.LoadData($"{mLastSceneName}_SpawnModDataProxies");
            if (proxiesString != null)
            {
                Variant proxiesVariant = JSON.Load(proxiesString);
                foreach (var pathJSON in proxiesVariant as ProxyArray)
                {
                    SpawnModDataProxy newProxy = new SpawnModDataProxy();
                    JSON.Populate(pathJSON, newProxy);
                    newProxy.InitializeType();
                    spawnDataProxies.Add(newProxy);
                }
            }

            LogDebug($"Deserialized {spawnDataProxies.Count} spawn mod data proxies");

            for (int i = 0, iMax = spawnDataProxies.Count; i < iMax; i++)
            {
                if (mSpawnModDataProxies.ContainsKey(spawnDataProxies[i].Guid))
                {
                    LogWarning($"Somehow we found a repeat proxy guid {spawnDataProxies[i].Guid}?");
                    return;
                }
                if (spawnDataProxies[i].Guid == Guid.Empty)
                {
                    LogWarning($"Guidless spawn mod data proxy found! aborting...");
                    continue;
                }
                if (spawnDataProxies[i].ParentGuid == Guid.Empty)
                {
                    LogWarning("Parentless spawn mod data proxy found! cannot correlate to a spawn region...");
                    continue;
                }
                if (!mManager.SpawnRegionManager.TryGetCustomSpawnRegionByProxyGuid(spawnDataProxies[i].ParentGuid, out ICustomSpawnRegion customSpawnRegion))
                {
                    LogError("Could not find custom spawn region using parent guid! continueing...");
                    continue;
                }
                if (!customSpawnRegion.TryQueueSpawn(spawnDataProxies[i]))
                {
                    LogError("Could not queue new spawn proxy!");
                    continue;
                }
                LogDebug($"Queued spawn mod proxy data with guid {spawnDataProxies[i].Guid} on spawn region with hash code {customSpawnRegion.SpawnRegion.GetHashCode()}!");
                mSpawnModDataProxies.Add(spawnDataProxies[i].Guid, spawnDataProxies[i]);
            }


            // pre-queueing some more spawns to avoid mid-scene hitches later
            foreach (ICustomSpawnRegion customSpawnRegion in mManager.SpawnRegionManager.CustomSpawnRegions.Values)
            {
                if (customSpawnRegion == null)
                {
                    continue;
                }
                if (customSpawnRegion.QueuedSpawnsCount < 3)
                {
                    if (customSpawnRegion.SpawnRegion == null)
                    {
                        LogError("Null spawn region on customSpawnRegion wrapper!");
                        continue;
                    }
                    LogDebug($"customSpawnRegion with spawn region hash code {customSpawnRegion.SpawnRegion.GetHashCode()} has only {customSpawnRegion.QueuedSpawnsCount} queued spawns, adding {3 - customSpawnRegion.QueuedSpawnsCount} new ones");
                    if (customSpawnRegion.SpawnRegion.m_SpawnablePrefab == null)
                    {
                        LogDebug("Null spawnable prefab on spawn region on customSpawnRegion wrapper! Usually this means the spawn region isn't active in this run and thus doesnt have its prefab connected.");
                        continue;
                    }
                    if (!customSpawnRegion.SpawnRegion.m_SpawnablePrefab.TryGetComponent<BaseAi>(out BaseAi spawnableBaseAi))
                    {
                        
                        LogError("Could not get base ai script from spawnable prefab on spawn region on customSpawnRegion wrapper!");
                        continue;
                    }
                    for (int i = 0, iMax = 3 - customSpawnRegion.QueuedSpawnsCount; i < iMax; i++)
                    {
                        Type spawnType = mTypePicker.PickType(spawnableBaseAi);
                        if (spawnType == typeof(void))
                        {
                            spawnType = GetFallbackBaseSpawnableType(spawnableBaseAi);
                        }
                        if (spawnType == typeof(void))
                        {
                            LogError("Could not find valid spawn type for base ai while pre-queuing spawns! what the heck bruh");
                            continue;
                        }
                        SpawnModDataProxy newProxy = (new SpawnModDataProxy(Guid.NewGuid(), mManager.CurrentScene, customSpawnRegion.SpawnRegion, spawnType));
                        if (!customSpawnRegion.TryQueueSpawn(newProxy))
                        {
                            LogError("Could pre-queue new spawn proxy!");
                            continue;
                        }
                        mSpawnModDataProxies.Add(newProxy.Guid, newProxy);
                        LogDebug($"Prequeued new spawn guid {newProxy.Guid} on spawn region with hash code {customSpawnRegion.SpawnRegion.GetHashCode()}!");
                    }
                }
            }
        }


        public bool TryGetSpawnModDataProxy(Guid guid, out SpawnModDataProxy proxy)
        {
            return mSpawnModDataProxies.TryGetValue(guid, out proxy);
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
                //LogDebug("Null base ai, can't augment.");
                return false;
            }
            if (mCustomAis.ContainsKey(baseAi.GetHashCode()))
            {
                //LogDebug("BaseAi in dictionary, can't augment.");
                return false;
            }

            Type spawnType = typeof(void);
            for (int i = 0, iMax = mSubManagers.Length; i < iMax; i++)
            {
                //LogDebug($"Allowing submanager {mSubManagers[i]} to intercept spawn...");
                if (mSubManagers[i].ShouldInterceptSpawn(baseAi, region))
                {
                    //LogDebug($"Spawn intercept from submanager {mSubManagers[i]}! new type: {mSubManagers[i].SpawnType}");
                    spawnType =mSubManagers[i].SpawnType;
                    break;
                }
            }
            if (spawnType == typeof(void))
            {
                //LogDebug($"No submanager interceptions, attempting to randomly pick a valid spawn type...");
                spawnType = mTypePicker.PickType(baseAi);
            }
            if (spawnType == typeof(void))
            {
                LogVerbose($"No spawn type available from type picker or manager overrides for base ai {baseAi.gameObject.name}, defaulting to fallback...");
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


        private void InjectCustomAi(BaseAi baseAi, Type spawnType, SpawnRegion spawnRegion, SpawnModDataProxy proxy, bool bypassProxy = false)
        {
            try
            {
                if (!bypassProxy)
                {
                    if (proxy == null)
                    {
                        //LogDebug("Generating new proxy");
                        proxy = new SpawnModDataProxy(Guid.NewGuid(), mManager.CurrentScene, baseAi, spawnType);
                    }
                    if (proxy.ParentGuid == Guid.Empty)
                    {
                        //LogDebug("Trying to connect to parent spawn region proxy");
                        if (mManager == null)
                        {
                            LogError("Null manager!");
                        }
                        if (mManager.SpawnRegionManager == null)
                        {
                            LogError("Null mManager.SpawnRegionManager!");
                        }
                        if (mManager.SpawnRegionManager.CustomSpawnRegions == null)
                        {
                            LogError("Null mManager.SpawnRegionManager.CustomSpawnRegions!");
                        }
                        if (spawnRegion == null)
                        {
                            LogError("Null spawnregion! how the heck did we even get here like this???");
                        }
                        if (!mManager.SpawnRegionManager.CustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out ICustomSpawnRegion customSpawnRegion))
                        {
                            LogError($"Could not fetch custom spawn region wrapper to correlate parentless-spawnmoddataproxy! Aborting..");
                            return;
                        }
                        //LogDebug("Trying to set parent guid per parent proxy");
                        proxy.ParentGuid = customSpawnRegion.ModDataProxy.Guid;
                    }
                }
                //LogDebug("Adding new component");
                mCustomAis.Add(baseAi.GetHashCode(), (ICustomAi)baseAi.gameObject.AddComponent(Il2CppType.From(spawnType)));
                //LogDebug("Adding to proxy dict");
                if (!bypassProxy)
                {
                    mSpawnModDataProxies.Add(proxy.Guid, proxy);
                }
                //LogDebug("trying to re-fetch ai wrapper");
                if (!mCustomAis.TryGetValue(baseAi.GetHashCode(), out ICustomAi customAi))
                {
                    LogError($"Critical error at ExpandedAiFramework.AugmentAi: newly created {spawnType} cannot be found in augment dictionary! Did its hash code change?", FlaggedLoggingLevel.Critical);
                    return;
                }
                //LogDebug("initializing wrapper");;
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
