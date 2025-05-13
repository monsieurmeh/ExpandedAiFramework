

namespace ExpandedAiFramework.CompanionWolfMod
{
    internal class CompanionWolfSettings : TypeSpecificSettings
    {
        [Section("Companion Wolf Settings")]
        [Name("Enable Companion Wolf")]
        [Description("Companion wolves will initially run away regardless of player action. After several attempts to chase it down, it will hold ground and wait for food to be dropped. If given enough space to approach and eat, the wolf will follow the player and eventually stick around as a companion wolf.")]
        public bool Enable = false;


        [Name("Spawn Weight")]
        [Slider(0.0f, 100.0f)]
        [Description("Adjust spawn weight for Companion wolves. Higher numbers increase relative spawn chance.")]
        public float SpawnWeight = 1.0f;


        [Name("Spawn Delay")]
        [Slider(0, 100)]
        [Description("Number of days before companion wolves begin to spawn if enabled.")]
        public int SpawnDelay = 25;


        [Name("Taming Affection per Calorie")]
        [Slider(0.1f, 10.0f)]
        [Description("Amount of taming affection gained per calorie fed.")]
        public float AffectionPerCalorie = 1.0f;


        [Name("Maximum Calorie Store")]
        [Slider(100, 8000)]
        [Description("Maximum number of calories wolf can store.")]
        public int MaximumCalorieIntake = 4000;


        [Name("Taming Affection Required")]
        [Slider(1000f, 50000f)]
        [Description("Amount of taming affection required to complete taming phase.")]
        public float AffectionRequirement = 10000.0f;


        [Name("Minimum Days to Tame")]
        [Slider(0, 100)]
        [Description("Minimum amount of days required to complete taming phase, regardless of calories fed.")]
        public int AffectionDaysRequirement = 10;


        [Name("Affection Decay Delay")]
        [Slider(0, 100)]
        [Description("Number of hours before affection starts to decay during taming phase. Feed the wolf to refresh this timer.")]
        public int AffectionDecayDelayHours = 4;


        [Name("Linger Duration")]
        [Slider(4, 240)]
        [Description("Number of hours before an untamed wolf with no affection remaining will stop spawning in the same location after initial spawn.")]
        public int LingerDurationHours = 12;
        

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
