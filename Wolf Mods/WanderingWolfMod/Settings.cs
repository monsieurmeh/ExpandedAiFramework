

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
        public int SpawnWeight = 10;


        [Name("Spawn Delay")]
        [Slider(0, 100)]
        [Description("Number of days before wandering wolves begin to spawn if enabled.")]
        public int SpawnDelay = 10;


        [Name("Enable Force Spawning")]
        [Description("Ensures that tracking wolves will ALWAYS spawn, even if they are across the map. Limited by global force spawn count.")]
        public bool ForceSpawn = false;


        public WanderingWolfSettings(string path) : base(path) { }

        public override bool ForceSpawningEnabled() => ForceSpawn;

        public override bool CanSpawn(BaseAi ai)
        {
            return Enable
                && ai.m_AiSubType == AiSubType.Wolf
                && ai.Timberwolf == null
                && GameManager.m_TimeOfDay.m_DaysSurvivedLastFrame >= SpawnDelay
                && (!ForceSpawn || (ForceSpawn && EAFManager.Instance.DataManager.CanForceSpawn(ai.m_WildlifeMode, typeof(WanderingWolf)))); //If force spawn IS enabled, then there must also be room for force spawning!
        }


        public override int GetSpawnWeight()
        {
            return SpawnWeight;
        }
    }
}
