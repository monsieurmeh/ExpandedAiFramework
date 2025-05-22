using System.Xml.Linq;
using UnityEngine;


namespace ExpandedAiFramework
{ 
    [Serializable]
    public class SpawnModDataProxy
    {
        public Guid Guid;
        public Guid ParentGuid;
        public string Scene; //might be able to get rid of this??
        public Vector3 Position;
        public Quaternion Rotation;
        public Type VariantSpawnType;


        public SpawnModDataProxy() { }


        public SpawnModDataProxy(Guid guid, Guid parentGuid, string scene, BaseAi ai, Type variantSpawnType)
        {
            Guid = guid;
            ParentGuid = parentGuid;
            Scene = scene;
            Position = ai.transform.position;
            Rotation = ai.transform.rotation;
            VariantSpawnType = variantSpawnType;
        }


        public override string ToString()
        {
            return $"SpawnModDataProxy with guid {Guid} at {Position} of variant spawn type {VariantSpawnType} in scene {Scene} belonging to spawn region with wrapper guid {ParentGuid}";
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
