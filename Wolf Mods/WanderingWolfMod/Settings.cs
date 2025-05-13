

namespace ExpandedAiFramework.WanderingWolfMod
{
    internal class WanderingWolfSettings : TypeSpecificSettings
    {
        [Section("Wandering Wolf Settings")]
        [Name("Enable Wandering Wolf")]
        [Description("Wandering wolves follow preset paths across any given region, but otherwise act like normal wolves. Avoidable, if unpredictable.")]
        public bool Enable = true;


        [Name("Spawn Weight")]
        [Slider(0.0f, 100.0f)]
        [Description("Adjust spawn weight for wandering wolves. Higher numbers increase relative spawn chance.")]
        public float SpawnWeight = 10.0f;


        [Name("Spawn Delay")]
        [Slider(0, 100)]
        [Description("Number of days before wandering wolves begin to spawn if enabled.")]
        public int SpawnDelay = 10;


        public override bool CanSpawn(BaseAi ai)
        {
            return Enable
                && ai.m_AiSubType == AiSubType.Wolf
                && ai.Timberwolf == null
                && GameManager.m_TimeOfDay.m_DaysSurvivedLastFrame >= SpawnDelay;
        }


        public override float GetSpawnWeight()
        {
            return SpawnWeight;
        }
    }
}
