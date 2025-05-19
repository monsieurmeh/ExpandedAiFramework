

using ExpandedAiFramework.Enums;

namespace ExpandedAiFramework
{
    internal class BaseRabbitSettings : TypeSpecificSettings
    {
        [Section("Base Rabbit Settings")]
        [Name("Enable Base Rabbit")]
        [Description("Enable base rabbit behavior with no thematic modifications. Bug fixes and improvements to vanilla logic ARE included. If no other rabbit mods are included, disabling this will have no effect.")]
        public bool Enable = true;


        [Name("Spawn Weight")]
        [Slider(0.0f, 100.0f)]
        [Description("Adjust spawn weight for base rabbit. Higher numbers increase relative spawn chance. If no other rabbit mods are included, this will have no effect.")]
        public int SpawnWeight = 100;


        public override bool CanSpawn(BaseAi ai)
        {
            return Enable && ai.m_AiSubType == AiSubType.Rabbit && ai.Rabbit != null;
        }


        public override int GetSpawnWeight()
        {
            return SpawnWeight;
        }
    }
}