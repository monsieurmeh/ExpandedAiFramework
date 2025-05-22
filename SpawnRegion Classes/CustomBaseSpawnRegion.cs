using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ExpandedAiFramework
{ 
    //[RegisterTypeInIl2Cpp]
    public class CustomBaseSpawnRegion : /*MonoBehaviour, */ICustomSpawnRegion
    {
        //public CustomBaseSpawnRegion(IntPtr intPtr) : base(intPtr) { }

        protected SpawnRegion mSpawnRegion;
        protected TimeOfDay mTimeOfDay;
        protected EAFManager mManager;
        protected SpawnRegionModDataProxy mModDataProxy;
        protected List<SpawnModDataProxy> mQueuedSpawnModDataProxies = new List<SpawnModDataProxy>();

        public SpawnRegion SpawnRegion { get { return mSpawnRegion; } }
        //public Component Self { get { return this; } }
        public SpawnRegionModDataProxy ModDataProxy { get { return mModDataProxy; } }


        public CustomBaseSpawnRegion(SpawnRegion spawnRegion, SpawnRegionModDataProxy dataProxy, TimeOfDay timeOfDay)
        {
            Initialize(spawnRegion, dataProxy, timeOfDay);
        }


        public virtual void Initialize(SpawnRegion spawnRegion, SpawnRegionModDataProxy dataProxy, TimeOfDay timeOfDay)
        {
            mSpawnRegion = spawnRegion;
            mModDataProxy = dataProxy;
            mTimeOfDay = timeOfDay;
            mManager = Manager;// manager;
        }


        public void Despawn(float time)
        {
            mModDataProxy.LastDespawnTime = time;
        }


        public bool TryQueueSpawn(SpawnModDataProxy proxy)
        {
            if (proxy == null)
            {
                LogWarning("Null proxy, cannot queue spawn.");
                return false;
            }

            if (mModDataProxy == null)
            {
                LogWarning("Null mod data proxy, cannot queue spawn");
                return false;
            }

            if (proxy.AiSubType != mModDataProxy.AiSubType)
            {
                LogWarning("Ai subtype mismatch, cannot queue spawn");
                return false;
            }

            mQueuedSpawnModDataProxies.Add(proxy);
            LogDebug("Queuing spawn!");
            return true;
        }
    }
}
