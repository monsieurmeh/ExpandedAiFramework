using System.Xml.Linq;
using UnityEngine;


namespace ExpandedAiFramework
{ 
    [Serializable]
    public class SpawnModDataProxy
    {
        public Guid Guid;
        public Guid ParentGuid = Guid.Empty;
        public string Scene;
        public Vector3 OriginalPosition;
        public Vector3 CurrentPosition;
        public Quaternion OriginalRotation;
        public Quaternion CurrentRotation;
        public AiSubType AiSubType;
        public Il2CppSystem.Type VariantSpawnType;
        public float LastDespawnTime;


        public SpawnModDataProxy() { }

        //Leaving parent guid out of this for now since it wont necessarily be known at construction, only when connected to parent spawn region
        public SpawnModDataProxy(Guid guid, string scene, BaseAi ai, Il2CppSystem.Type variantSpawnType)
        {
            Guid = guid;
            Scene = scene;
            OriginalPosition = ai.transform.position;
            CurrentPosition = OriginalPosition;
            OriginalRotation = ai.transform.rotation;
            CurrentRotation = OriginalRotation;
            AiSubType = ai.m_AiSubType;
            VariantSpawnType = variantSpawnType;
            LastDespawnTime = Utility.GetCurrentTimelinePoint();
        }


        public virtual void Respawn(BaseAi baseAi)
        {

        }


        public override string ToString()
        {
            return $"SpawnModDataProxy with guid {Guid} at {OriginalPosition} of variant spawn type {VariantSpawnType} in scene {Scene} belonging to spawn region with wrapper guid {ParentGuid}";
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
