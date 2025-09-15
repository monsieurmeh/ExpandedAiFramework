

namespace ExpandedAiFramework.AmbushWolfMod
{
    internal class AmbushWolfSettings : TypeSpecificSettings
    {
        [Section("Ambush Wolf Settings")]
        [Name("Enable Ambush Wolf")]
        [Description("Ambush wolves find a hiding spot on any given map and wait for the player to approach before immediately charging and attacking, but otherwise act like regular wolves.")]
        public bool Enable = false;


        [Name("Spawn Weight")]
        [Slider(0, 100)]
        [Description("Adjust spawn weight for ambush wolves. Higher numbers increase relative spawn chance.")]
        public int SpawnWeight = 10;


        [Name("Spawn Delay")]
        [Slider(0, 100)]
        [Description("Number of days before ambush wolves begin to spawn if enabled.")]
        public int SpawnDelay = 20;


        public AmbushWolfSettings(string path) : base(path) { }


        public override bool CanSpawn(BaseAi ai)
        {
            return Enable
                && ai.m_AiSubType == AiSubType.Wolf
                && ai.Timberwolf == null
                && GameManager.m_TimeOfDay.m_DaysSurvivedLastFrame >= SpawnDelay;
        }


        public override int GetSpawnWeight()
        {
            return SpawnWeight;
        }
    }
}
