using System.Xml.Linq;
using UnityEngine;


namespace ExpandedAiFramework
{
    //todo: need to add originating mod type to construtor and use it in equals
    [Serializable]
    public class SpawnRegionModDataProxy
    {
        public string Scene;
        public Vector3 Position;
        public AiType AiType;
        public AiSubType AiSubType;
        public Type OriginatingModType;
        // I figure if we need more info we can always inherit this class in a mod class and create our own.

        public SpawnRegionModDataProxy() { }


        public SpawnRegionModDataProxy(string scene, BaseAi ai, SpawnRegion spawnRegion)
        {
            Scene = scene;
            Position = spawnRegion.transform.position;
            AiType = ai.m_AiType;
            AiSubType = ai.m_AiSubType;
        }


        public override string ToString()
        {
            return $"SpawnRegionModDataProxy at {Position} of type {AiType}.{AiSubType} in scene {Scene}";
        }


        public override int GetHashCode() => (Scene, Position, AiType, AiSubType).GetHashCode();

        public override bool Equals(object obj) => this.Equals(obj as SpawnRegionModDataProxy);

        public bool Equals(SpawnRegionModDataProxy proxy)
        {
            if (proxy is null)
            {
                return false;
            }

            return (Scene == proxy.Scene)
                && (Vector3.Distance(Position, proxy.Position) <= 0.001f)
                && AiType == proxy.AiType
                && AiSubType == proxy.AiSubType;
        }

        public static bool operator ==(SpawnRegionModDataProxy lhs, SpawnRegionModDataProxy rhs)
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
        public static bool operator !=(SpawnRegionModDataProxy lhs, SpawnRegionModDataProxy rhs) => !(lhs == rhs);
    }
}
