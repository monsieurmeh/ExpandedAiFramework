using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandedAiFramework
{ 
    public class BaseWolfSpawnRegion : CustomBaseSpawnRegion
    {
        public BaseWolfSpawnRegion(SpawnRegion spawnRegion, SpawnRegionModDataProxy dataProxy, TimeOfDay timeOfDay) : base(spawnRegion, dataProxy, timeOfDay)
        {

        }

        protected override int GetMaxSimultaneousSpawnsDay()
        {
            int count = base.GetMaxSimultaneousSpawnsDay() + BaseWolf.BaseWolfSettings.MaxWolfSpawnCountIncrease;
            //EAFManager.LogWithStackTrace($"Overriding day spawn count: {count - BaseWolf.BaseWolfSettings.MaxWolfSpawnCountIncrease} -> {count}");
            return count;
        }


        protected override int GetMaxSimultaneousSpawnsNight()
        {
            int count = base.GetMaxSimultaneousSpawnsNight() + BaseWolf.BaseWolfSettings.MaxWolfSpawnCountIncrease;
            //EAFManager.LogWithStackTrace($"Overriding day spawn count: {count - BaseWolf.BaseWolfSettings.MaxWolfSpawnCountIncrease} -> {count}");
            return count;
        }


        protected override int AdditionalSimultaneousSpawnAllowance()
        {
            //EAFManager.LogWithStackTrace($"Overriding max spawn count increase limit by {BaseWolf.BaseWolfSettings.MaxWolfSpawnCountIncrease}!");
            return BaseWolf.BaseWolfSettings.MaxWolfSpawnCountIncrease;
        }

    }
}
