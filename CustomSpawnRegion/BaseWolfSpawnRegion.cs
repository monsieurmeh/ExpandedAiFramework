using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandedAiFramework
{ 
    public class BaseWolfSpawnRegion : CustomSpawnRegion
    {
        public BaseWolfSpawnRegion(SpawnRegion spawnRegion, SpawnRegionModDataProxy dataProxy, TimeOfDay timeOfDay) : base(spawnRegion, dataProxy, timeOfDay) {}

        protected override int AdditionalSimultaneousSpawnAllowance() => BaseWolf.BaseWolfSettings.MaxWolfSpawnCountIncrease;
    }
}
