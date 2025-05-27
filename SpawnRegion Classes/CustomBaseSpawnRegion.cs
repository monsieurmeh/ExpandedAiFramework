using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace ExpandedAiFramework
{ 
    //right now this is just a spawn region wrapper holding a proxy and itself serving as an index.
    //Eventually might grow. Doing most stuff with managers right now, messy but works at least.
    //[RegisterTypeInIl2Cpp]
    public class CustomBaseSpawnRegion //: MonoBehaviour
    {
        //public CustomBaseSpawnRegion(IntPtr intPtr) : base(intPtr) { }

        protected SpawnRegion mSpawnRegion;
        protected TimeOfDay mTimeOfDay;
        protected EAFManager mManager;
        protected SpawnRegionModDataProxy mModDataProxy;

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
            mModDataProxy.CurrentPosition = mSpawnRegion.transform.position;
        }

        /*
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
            LogVerbose("Queuing spawn!");
            return true;
        }

        public bool TryGetQueuedSpawn(out SpawnModDataProxy proxy)
        {
            proxy = null;
            if (mQueuedSpawnModDataProxies == null)
            {
                LogError("Can't get proxy from null list! Aborting");
                return false;
            }
            if (mQueuedSpawnModDataProxies.Count == 0)
            {
                LogVerbose("No queued proxies, aborting");
                return false;
            }
            if (mQueuedSpawnModDataProxies[0] == null)
            {
                LogError("Found null spawn mod data proxy in list! what the heck? Aborting..");
                return false;
            }
            proxy = mQueuedSpawnModDataProxies[0];
            mQueuedSpawnModDataProxies.RemoveAt(0);
            return true;
        }
        */
    }
}
