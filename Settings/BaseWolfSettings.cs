

namespace ExpandedAiFramework
{
    public class BaseWolfSettings : TypeSpecificSettings
    {
        [Section("Base Wolf Settings")]
        [Name("Enable Base Wolves")]
        [Description("Enable base wolf behavior with no thematic modifications. Bug fixes and improvements to vanilla logic ARE included.")]
        public bool Enable = true;


        [Name("Spawn Weight")]
        [Slider(0.0f, 100.0f)]
        [Description("Adjust spawn weight for base wolves. Higher numbers increase relative spawn chance.")]
        public int SpawnWeight = 100;


        [Name("Stalking Timeout")]
        [Slider(0.0f, 30.0f)]
        [Description("Prevents indefinite stalking behavior by switching to attack state after this length of time.")]
        public float StalkingTimeout = 10.0f;


        public override bool CanSpawn(BaseAi ai)
        {
            return Enable && ai.m_AiSubType == AiSubType.Wolf && ai.BaseWolf != null && ai.Timberwolf != null;
        }


        public override int GetSpawnWeight() 
        { 
            return SpawnWeight; 
        }
    }
}