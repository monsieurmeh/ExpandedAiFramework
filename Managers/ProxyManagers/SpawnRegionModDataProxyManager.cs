using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandedAiFramework
{
    public class SpawnRegionModDataProxyManager : ProxyManager<SpawnRegionModDataProxy>
    {
        private List<CustomBaseSpawnRegion> mUnmatchedCustomBaseSpawnRegions = new List<CustomBaseSpawnRegion>();


        public SpawnRegionModDataProxyManager(DataManager manager, string dataLocation) : base(manager, dataLocation) { }


        protected virtual void RefreshProxy(SpawnRegionModDataProxy proxy)
        {
            proxy.Connected = false;
        }
    }
}
