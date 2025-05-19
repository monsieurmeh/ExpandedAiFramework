

namespace ExpandedAiFramework
{
    internal class BaseDeerSettings : TypeSpecificSettings
    {
        [Section("Base Deer Settings")]
        [Name("Enable Base Deer")]
        [Description("Enable base deer behavior with no thematic modifications. Bug fixes and improvements to vanilla logic ARE included. If no other deer mods are included, disabling this will have no effect.")]
        public bool Enable = true;


        [Name("Spawn Weight")]
        [Slider(0.0f, 100.0f)]
        [Description("Adjust spawn weight for base deer. Higher numbers increase relative spawn chance. If no other deer mods are included, this will have no effect.")]
        public int SpawnWeight = 100;
        

        public override bool CanSpawn(BaseAi ai)
        {
            return Enable && ai.m_AiSubType == AiSubType.Stag && ai.Stag != null;
        }


        public override int GetSpawnWeight()
        {
            return SpawnWeight;
        }
    }
}