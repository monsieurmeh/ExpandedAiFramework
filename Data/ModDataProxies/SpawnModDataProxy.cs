using Il2Cpp;
using UnityEngine;
using MelonLoader.TinyJSON;

namespace ExpandedAiFramework
{
    public class SpawnModDataProxy : ModDataProxy, ILogInfoProvider
    {
        [Exclude] private Type mVariantSpawnType;
        [Exclude] public bool Disconnected = false;
        [Exclude] public bool AsyncProcessing = false;
        [Exclude] public bool Available = true;
        [Exclude] public bool Spawned = false;
        public Guid ParentGuid = Guid.Empty;
        public Quaternion CurrentRotation;
        public AiSubType AiSubType;
        public WolfType WolfType;
        public WildlifeMode WildlifeMode;
        public float LastDespawnTime;
        public string VariantSpawnTypeString;
        public bool ForceSpawn;

        //Vanilla data
        public AiMode AiMode;
        string AssetReferenceGUID;
        public string BaseAiSerialized;


        public Type VariantSpawnType { get { return mVariantSpawnType; } }

        public SpawnModDataProxy() : base() { }

        public override string DisplayName { get { return $"{VariantSpawnTypeString}-{mGuid}";  } }
        public string InstanceInfo { get { return DisplayName; } }
        public string TypeInfo { get { return "SpawnModDataProxy"; } }




        //Only constructs mod data; it is on managers to serialize in vanilla data later
        public SpawnModDataProxy(Guid guid,
                                 string scene,
                                 Vector3 currentPosition,
                                 Quaternion currentRotation,
                                 AiSubType subTypeSpawned,
                                 WildlifeMode wildlifeMode,
                                 Type variantSpawnType) : base(guid,
                                                               scene,
                                                               currentPosition)
        {
            CurrentRotation = currentRotation;
            AiSubType = subTypeSpawned;
            mVariantSpawnType = variantSpawnType;
            WildlifeMode = wildlifeMode;
            VariantSpawnTypeString = $"{variantSpawnType.FullName}, {variantSpawnType.Assembly.GetName().Name}";
        }


        public bool InitializeType()
        {
            if (mVariantSpawnType != null)
            {
                return true;
            }
            Type type = Type.GetType(VariantSpawnTypeString);
            if (type != null)
            {
                mVariantSpawnType = type;
                return true;
            }
            string[] parts = VariantSpawnTypeString.Split(',');
            if (parts.Length < 2)
            {
                this.ErrorInstanced($"Could not parse type string {VariantSpawnTypeString} during SpawnModDataProxy.InitializeType()!");
                return false;
            }
            string fullName = parts[0].Trim();
            string assemblyName = parts[1].Trim();
            this.LogDebugInstanced($"Checking for {fullName} from assembly {assemblyName}", LogCategoryFlags.SerializedData | LogCategoryFlags.Ai);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                this.LogDebugInstanced($"Checking assembly: {assembly.FullName}", LogCategoryFlags.SerializedData | LogCategoryFlags.Ai);
                if (assembly.GetName().Name != assemblyName)
                {
                    continue;
                }
                type = assembly.GetType(fullName);
                if (type == null)
                {
                    continue;
                }
                mVariantSpawnType = type;
                this.LogDebugInstanced($"Type found and resolved! Name: {mVariantSpawnType.Name}", LogCategoryFlags.Ai | LogCategoryFlags.SerializedData);
                return true;
            }
            this.ErrorInstanced($"Unable to resolve type: {VariantSpawnTypeString} during SpawnModDataProxy.InitializeType()!");
            return false;
        }


        //eventually we'll use this in a cascade from the spawn region wrapper, but the code is a bit too tight right now for it to be fast.
        // I can optimize later, I'd like to get this running first so I have a baseline, however buggy, to test features on.
        public virtual void Save(CustomBaseAi baseAi)
        {
            LastDespawnTime = GetCurrentTimelinePoint();
            if (baseAi.BaseAi.m_AiSubType == AiSubType.Wolf)
            {
                WolfType = baseAi.BaseAi.NormalWolf == null ? WolfType.Timberwolf : WolfType.Normal;
            }
            else if (baseAi.BaseAi.m_AiSubType == AiSubType.Rabbit)
            {
                WolfType = baseAi.BaseAi.Ptarmigan == null ? WolfType.Timberwolf : WolfType.Normal;
            }
            else
            {
                WolfType = WolfType.Normal;
            }
            try
            {
                if (baseAi.transform == null)
                {
                    return;
                }
                AiUtils.GetClosestNavmeshPos(out mCurrentPosition, baseAi.transform.position, baseAi.transform.position, 5, 5);
                CurrentRotation = baseAi.transform.rotation;
            } catch (Exception e)
            {
                this.LogInstanced($"Failed to save position and rotation during SpawnModDataProxy serialization; possibly this creature is despawned currently? ({e})");
            }
        }
    }
}
