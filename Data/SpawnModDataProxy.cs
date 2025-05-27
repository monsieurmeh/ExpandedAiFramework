using UnityEngine;


namespace ExpandedAiFramework
{ 
    [Serializable]
    public class SpawnModDataProxy
    {
        [NonSerialized] private Type mVariantSpawnType;
        [NonSerialized] public bool Disconnected = false;
        public Guid Guid = Guid.Empty;
        public Guid ParentGuid = Guid.Empty;
        public string Scene;
        public Vector3 OriginalPosition;
        public Vector3 CurrentPosition;
        public Quaternion OriginalRotation;
        public Quaternion CurrentRotation;
        public AiSubType AiSubType;
        public float LastDespawnTime;
        public string VariantSpawnTypeString;

        public Type VariantSpawnType { get { return mVariantSpawnType; } }

        public SpawnModDataProxy() { }

        public bool InitializeType()
        {
            if (mVariantSpawnType != null)
            {
                return true;
            }
            var type = Type.GetType(VariantSpawnTypeString);
            if (type != null)
            { 
                mVariantSpawnType = type;
                return true;
            }
            string[] parts = VariantSpawnTypeString.Split(',');
            if (parts.Length < 2)
            {
                LogError($"Could not parse type string {VariantSpawnTypeString} during SpawnModDataProxy.InitializeType()!");
                return false;
            }
            string fullName = parts[0].Trim();
            string assemblyName = parts[1].Trim();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
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
                return true;
            }
            LogError($"Unable to resolve type: {VariantSpawnTypeString} during SpawnModDataProxy.InitializeType()!");
            return false;   
        }


        public SpawnModDataProxy(Guid guid, string scene, BaseAi ai, Type variantSpawnType)
        {
            Guid = guid;
            Scene = scene;
            OriginalPosition = ai.transform.position;
            CurrentPosition = OriginalPosition;
            OriginalRotation = ai.transform.rotation;
            CurrentRotation = OriginalRotation;
            AiSubType = ai.m_AiSubType;
            mVariantSpawnType = variantSpawnType;
            VariantSpawnTypeString = $"{variantSpawnType.FullName}, {variantSpawnType.Assembly.GetName().Name}";
            LastDespawnTime = GetCurrentTimelinePoint();
        }


        public SpawnModDataProxy(Guid guid, string scene, SpawnRegion spawnRegion, Type variantSpawnType)
        {
            Guid = guid;
            Scene = scene;
            OriginalPosition = Vector3.zero;
            OriginalRotation = Quaternion.identity;
            spawnRegion.TryGetSpawnPositionAndRotation(ref OriginalPosition, ref OriginalRotation);
            CurrentPosition = OriginalPosition;
            CurrentRotation = OriginalRotation;
            AiSubType = spawnRegion.m_AiSubTypeSpawned;
            mVariantSpawnType = variantSpawnType;
            VariantSpawnTypeString = $"{variantSpawnType.FullName}, {variantSpawnType.Assembly.GetName().Name}";
            LastDespawnTime = GetCurrentTimelinePoint();
        }

        //eventually we'll use this in a cascade from the spawn region wrapper, but the code is a bit too tight right now for it to be fast.
        // I can optimize later, I'd like to get this running first so I have a baseline, however buggy, to test features on.
        public virtual void Despawn()
        {
            LastDespawnTime = GetCurrentTimelinePoint();
        }


        public virtual void Respawn(BaseAi baseAi)
        {

        }


        public override string ToString()
        {
            return $"SpawnModDataProxy with guid {Guid} at {OriginalPosition} of variant spawn type {VariantSpawnTypeString} in scene {Scene} belonging to spawn region with wrapper guid {ParentGuid}";
        }


        public override int GetHashCode() => Guid.GetHashCode();

        public override bool Equals(object obj) => this.Equals(obj as SpawnModDataProxy);

        public bool Equals(SpawnModDataProxy proxy)
        {
            if (proxy is null)
            {
                return false;
            }

            return Guid == proxy.Guid;
        }

        public static bool operator ==(SpawnModDataProxy lhs, SpawnModDataProxy rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                {
                    return true;
                }
                return false;
            }
            return lhs.Equals(rhs);
        }
        public static bool operator !=(SpawnModDataProxy lhs, SpawnModDataProxy rhs) => !(lhs == rhs);
    }
}
