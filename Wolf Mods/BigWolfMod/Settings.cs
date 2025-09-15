

namespace ExpandedAiFramework.BigWolfMod
{
    internal class BigWolfSettings : TypeSpecificSettings
    {
        [Section("'Big Wolf' Settings")]
        [Name("Enable Big Wolf")]
        [Description("Big wolves are twice as big as normal wolves, and are tougher and faster to match their increased size. Big wolves do not bleed, and will eventually act more like cougars in that they will circle back for additional attacks until killed.")]
        public bool Enable = false;

    
        [Name("Spawn Weight")]
        [Slider(0, 100)]
        [Description("Adjust spawn weight for big wolves. Higher numbers increase relative spawn chance.")]
        public int SpawnWeight = 2;


        [Name("Spawn Delay")]
        [Slider(0, 100)]
        [Description("Number of days before big wolves begin to spawn if enabled.")]
        public int SpawnDelay = 100;


        [Name("Can Big Wolf Bleed Out?")]
        [Description("Enables or disables big wolf bleeding")]
        public bool CanBleedOut = false;

        public BigWolfSettings(string path) : base(path) { }

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
