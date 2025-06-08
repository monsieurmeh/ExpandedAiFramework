using Il2CppRewired.Utils;
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

        //One day I might uproot this, but it's not tiny (~500 lines decompiled) and unless we want to start adjusting spawn rate *mechanics* (not just numeric values) we can probably leave it alone. Just need to hook into it for registration
        //public void OverrideStart()
        //{
            //if (!OverrideStartCustom())
            //{
               // return;
            //}
            //mSpawnRegion.Start();
        //}


        //protected virtual bool OverrideStartCustom() => true;


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
    }
}
