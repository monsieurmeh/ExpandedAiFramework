

namespace ExpandedAiFramework.BigWolfMod
{
    internal class BigWolfSettings : TypeSpecificSettings
    {
        [Section("'Big Wolf' Settings")]
        [Name("Enable Big Wolf")]
        [Description("Big wolves are twice as big as normal wolves, and are tougher and faster to match their increased size. Big wolves do not bleed, and will eventually act more like cougars in that they will circle back for additional attacks until killed.")]
        public bool Enable = false;

    
        [Name("Spawn Weight")]
        [Slider(0.0f, 100.0f)]
        [Description("Adjust spawn weight for big wolves. Higher numbers increase relative spawn chance.")]
        public float SpawnWeight = 2.0f;


        [Name("Spawn Delay")]
        [Slider(0, 100)]
        [Description("Number of days before big wolves begin to spawn if enabled.")]
        public int SpawnDelay = 100;


        [Name("Can Big Wolf Bleed Out?")]
        [Description("Enables or disables big wolf bleeding")]
        public bool CanBleedOut = false;


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
