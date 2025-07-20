using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandedAiFramework
{
    public class SpawnRegionModDataProxyManager : ProxyManagerBase<SpawnRegionModDataProxy>
    {
        public SpawnRegionModDataProxyManager(DataManager manager, DispatchManager dispatcher, string dataLocation) : base(manager, dispatcher, dataLocation) { }

        protected override void RefreshData(SpawnRegionModDataProxy data)
        {
            data.Connected = false;
            base.RefreshData(data);
        }
    }
}
