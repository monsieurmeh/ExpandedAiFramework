

namespace ExpandedAiFramework
{
    internal class BaseWolfSettings : TypeSpecificSettings
    {
        [Section("Base Wolf Settings")]
        [Name("Enable Base Wolves")]
        [Description("Enable base wolf behavior with no thematic modifications. Bug fixes and improvements to vanilla logic ARE included.")]
        public bool Enable = true;


        [Name("Spawn Weight")]
        [Slider(0.0f, 100.0f)]
        [Description("Adjust spawn weight for base wolves. Higher numbers increase relative spawn chance.")]
        public float SpawnWeight = 100.0f;


        public override bool CanSpawn(BaseAi ai)
        {
            return Enable && ai.m_AiSubType == AiSubType.Wolf && ai.Timberwolf == null;
        }


        public override float GetSpawnWeight() 
        { 
            return SpawnWeight; 
        }
    }
}