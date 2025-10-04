using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExpandedAiFramework
{
    public class CougarManager : BaseSubManager, ICougarManager
    {
        public CougarManager(EAFManager manager) : base(manager) {}
        public virtual Type SpawnType  => typeof(BaseCougar);
        public virtual bool ShouldInterceptSpawn(CustomSpawnRegion region) => false;
        public virtual void PostProcessNewSpawnModDataProxy(SpawnModDataProxy proxy) { }
    }
}
