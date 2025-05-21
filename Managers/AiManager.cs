using ComplexLogger;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace ExpandedAiFramework
{
    //This will act similar to in-game BaseAiManager, but for the ICustomAi wrappers. Things like tracking spawn variant persistency, etc
    //at some point I'll probably make a SpawnRegionManager too, which will do something similar but wrap up the spawn regions instead.

   

    public class AiManager : BaseSubManager
    {
        private Dictionary<int, ICustomAi> mCustomAis = new Dictionary<int, ICustomAi>(); 
        private WeightedTypePicker<BaseAi> mTypePicker = new WeightedTypePicker<BaseAi>();
        private Dictionary<Type, ISpawnTypePickerCandidate> mSpawnSettingsDict = new Dictionary<Type, ISpawnTypePickerCandidate>();
        private float mCheckForMissingScriptsTime = 0.0f;
        private bool mNeedToCheckForMissingScripts = false;

        public Dictionary<int, ICustomAi> CustomAis { get { return mCustomAis; } }
        public WeightedTypePicker<BaseAi> TypePicker { get { return mTypePicker; } }
        public Dictionary<Type, ISpawnTypePickerCandidate> SpawnSettingsDict { get { return mSpawnSettingsDict; } }


        public AiManager(EAFManager manager, ISubManager[] subManagers) : base(manager, subManagers) { }


        public override void Update()
        {
            base.Update();
            if (mNeedToCheckForMissingScripts && Time.time - mCheckForMissingScriptsTime >= 1.0f)
            {
                mNeedToCheckForMissingScripts = false;
                foreach (BaseAi baseAi in GameObject.FindObjectsOfType<BaseAi>())
                {
                    if (baseAi != null && baseAi.gameObject != null && !baseAi.gameObject.TryGetComponent(out CustomAiBase customAi))
                    {
                        TryInjectCustomBaseAi(baseAi);
                    }
                }
            }
        }


        public override void Shutdown()
        {
            ClearCustomAis();
            base.Shutdown();
        }


        public override void OnLoadScene()
        {
            ClearCustomAis();
        }


        public override void OnInitializedScene()
        {
            mCheckForMissingScriptsTime = Time.time;
            mNeedToCheckForMissingScripts = true;
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
                LogVerbose("Null base ai, can't augment.");
                return false;
            }
            if (mCustomAis.ContainsKey(baseAi.GetHashCode()))
            {
                LogVerbose("BaseAi in dictionary, can't augment.");
                return false;
            }

            Il2CppSystem.Type spawnType = Il2CppType.From(typeof(void));
            for (int i = 0, iMax = mSubManagers.Length; i < iMax; i++)
            {
                if (mSubManagers[i].ShouldInterceptSpawn(baseAi, region))
                {
                    spawnType = Il2CppType.From(mSubManagers[i].SpawnType);
                    break;
                }
            }
            if (spawnType == Il2CppType.From(typeof(void)))
            {
                spawnType = Il2CppType.From(mTypePicker.PickType(baseAi));
            }
            if (spawnType == Il2CppType.From(typeof(void)))
            {
                LogVerbose($"No spawn type available from type picker or manager overrides for base ai {baseAi.gameObject.name}, defaulting to fallback...");
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


        private void InjectCustomAi(BaseAi baseAi, Il2CppSystem.Type spawnType, SpawnRegion spawnRegion)
        {
            LogVerbose($"Spawning {spawnType.Name} at {baseAi.gameObject.transform.position}");
            try
            {
                mCustomAis.Add(baseAi.GetHashCode(), (ICustomAi)baseAi.gameObject.AddComponent(spawnType));
                if (!mCustomAis.TryGetValue(baseAi.GetHashCode(), out ICustomAi customAi))
                {
                    LogError($"Critical error at ExpandedAiFramework.AugmentAi: newly created {spawnType} cannot be found in augment dictionary! Did its hash code change?", FlaggedLoggingLevel.Critical);
                    return;
                }
                customAi.Initialize(baseAi, GameManager.m_TimeOfDay, spawnRegion);//, this);
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
    }
}
