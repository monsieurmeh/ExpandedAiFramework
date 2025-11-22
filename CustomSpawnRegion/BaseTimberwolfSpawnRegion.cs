using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandedAiFramework
{ 
    public class BaseTimberwolfSpawnRegion : CustomSpawnRegion
    {
        public BaseTimberwolfSpawnRegion(SpawnRegion spawnRegion, SpawnRegionModDataProxy dataProxy, TimeOfDay timeOfDay) : base(spawnRegion, dataProxy, timeOfDay) {}

        protected override int AdditionalSimultaneousSpawnAllowance() => BaseTimberwolf.BaseTimberwolfSettings.MaxTimberwolfSpawnCountIncrease;
    }
}
