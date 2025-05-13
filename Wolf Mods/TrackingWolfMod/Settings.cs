

namespace ExpandedAiFramework.TrackingWolfMod
{
    internal class TrackingWolfSettings : TypeSpecificSettings
    {
        [Section("Tracking Wolf Settings")]
        [Name("Enable Tracking Wolf")]
        [Description("Tracking wolves will follow you if they are able to reach you. Fend them off by normal means, or escape out of reach to lose your tail.")]
        public bool Enable = true;


        [Name("Spawn Weight")]
        [Slider(0.0f, 100.0f)]
        [Description("Adjust spawn weight for tracking wolves. Higher numbers increase relative spawn chance.")]
        public float SpawnWeight = 10.0f;


        [Name("Spawn Delay")]
        [Slider(0, 100)]
        [Description("Number of days before Tracking wolves begin to spawn if enabled.")]
        public int SpawnDelay = 25;


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
