

namespace ExpandedAiFramework
{
    internal class BaseCougarSettings : TypeSpecificSettings
    {
        [Section("Base Cougar Settings")]
        [Name("Enable Base Cougars")]
        [Description("Enable base cougar behavior with no thematic modifications. Bug fixes and improvements to vanilla logic ARE included. If no other cougar mods are included, disabling this will have no effect.")]
        public bool Enable = true;


        [Name("Spawn Weight")]
        [Slider(0.0f, 100.0f)]
        [Description("Adjust spawn weight for base cougar. Higher numbers increase relative spawn chance. If no other cougar mods are included, this will have no effect.")]
        public int SpawnWeight = 100;


        public override bool CanSpawn(BaseAi ai)
        {
            return Enable && ai.m_AiSubType == AiSubType.Cougar && ai.Cougar != null;
        }


        public override int GetSpawnWeight()
        {
            return SpawnWeight;
        }
    }
}