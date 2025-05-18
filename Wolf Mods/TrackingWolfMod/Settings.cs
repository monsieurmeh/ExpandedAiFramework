

namespace ExpandedAiFramework.TrackingWolfMod
{
    internal class TrackingWolfSettings : TypeSpecificSettings
    {
        [Section("Tracking Wolf Settings")]
        [Name("Enable Tracking Wolf")]
        [Description("Tracking wolves will follow you if they are able to reach you. Fend them off by normal means, or escape out of reach to lose your tail.")]
        public bool Enable = true;


        [Name("Spawn Weight")]
        [Slider(0, 100)]
        [Description("Adjust spawn weight for tracking wolves. Higher numbers increase relative spawn chance.")]
        public int SpawnWeight = 10;


        [Name("Spawn Delay")]
        [Slider(0, 100)]
        [Description("Number of days before Tracking wolves begin to spawn if enabled.")]
        public int SpawnDelay = 25;


        [Name("Post-Struggle Minimum Flee Period")]
        [Slider(0, 60)]
        [Description("Number of seconds before Tracking wolves begin to track the target after a struggle if no damage was done to the wolf.")]
        public float PostStruggleFleePeriodSeconds = 60.0f;


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
