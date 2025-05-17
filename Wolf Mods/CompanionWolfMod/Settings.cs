

namespace ExpandedAiFramework.CompanionWolfMod
{
    internal class CompanionWolfSettings : TypeSpecificSettings
    {
        private CompanionWolfManager mManager;

        public CompanionWolfSettings(CompanionWolfManager manager)
        {
            mManager = manager;
        }

        [Section("Companion Wolf Settings")]
        [Name("Enable Companion Wolf")]
        [Description("Companion wolves will initially run away regardless of player action. After several attempts to chase it down, it will hold ground and wait for food to be dropped. If given enough space to approach and eat, the wolf will follow the player and eventually stick around as a companion wolf.")]
        public bool Enable = false;


        [Name("Spawn Weight")]
        [Slider(0, 100)]
        [Description("Adjust spawn weight for Companion wolves. Higher numbers increase relative spawn chance.")]
        public float SpawnWeight = 1.0f;


        [Name("Spawn Delay")]
        [Slider(0, 100)]
        [Description("Number of days before companion wolves begin to spawn if enabled.")]
        public int SpawnDelay = 25;


        [Name("Taming Affection per Calorie")]
        [Slider(0.1f, 10)]
        [Description("Amount of taming affection gained per calorie fed. Note that feeding an already tamed wolf does not provide affection; you will need to find other ways to provide enrichment!")]
        public float AffectionPerCalorie = 1.0f;


        [Name("Maximum Calorie Store")]
        [Slider(100, 8000)]
        [Description("Maximum number of calories wolf can store.")]
        public float MaximumCalorieIntake = 4000.0f;


        [Name("Taming Affection Required")]
        [Slider(1000, 50000)]
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


        [Name("Untamed Affection Decay Rate")]
        [Slider(10, 1000)]
        [Description("Affection decay rate per hour while wolf is untamed.")]
        public float UntamedAffectionDecayRate = 100.0f;


        [Name("Tamed Affection Decay Rate")]
        [Slider(0, 10)]
        [Description("Affection decay rate per hour while wolf is untamed.")]
        public float TamedAffectionDecayRate = 1f;


        [Name("Linger Duration")]
        [Slider(4, 240)]
        [Description("Number of hours before an untamed wolf with no affection remaining will stop spawning in the same location after initial spawn.")]
        public int LingerDurationHours = 12;


        [Name("Calorie Burned per Day")]
        [Slider(1200, 2000)]
        [Description("Calories burned per day while tamed and outdoors. Companions are not able to come inside, so while you are indoors their affection will decrease but they will hunt for themselves preventing hunger decay.")]
        public float CaloriesBurnedPerDay = 1500.0f;


        [Name("Maximum Condition (HP)")]
        [Slider(100, 1000)]
        [Description("Maximum condition of the wolf. This is tied to the same value affected by in-game damage (HP).")]
        public float MaximumCondition = 100.0f;


        [Name("Starving Condition Decay Rate")]
        [Slider(0, 10)]
        [Description("Condition loss per hour when tamed wolf is starving. Untamed wolves will fend for themselves.")]
        public float StarvingConditionDecayPerHour = 5.0f;


        [Name("Eating Speed")]
        [Slider(500, 5000)]
        [Description("Eating speed in calories per game hour.")]
        public float CaloriesConsumedPerGameHour = 1000f;


        [Name("Maximum Affection from Feeding")]
        [Slider(1000, 50000)]
        [Description("Maximum affection level achievable with feeding alone for tamed companions. Does not apply during taming phase to prevent lock-up against taming affection requirement.")]
        public int MaximumAffectionFromFeeding = 10000;


        public override bool CanSpawn(BaseAi ai)
        {
            Utility.LogDebug($"Enabled: {Enable} | Connected: {mManager.Data.Connected} | Tamed: {mManager.Data.Tamed} |  Type/Sub: {ai.m_AiType}.{ai.m_AiSubType}");
            return Enable
                && !mManager.Data.Tamed
                && !mManager.Data.Connected
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
