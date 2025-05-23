

namespace ExpandedAiFramework
{
    public class BaseBearSettings : TypeSpecificSettings
    {
        [Section("Base Bear Settings")]
        [Name("Enable Base Bears")]
        [Description("Enable base bear behavior with no thematic modifications. Bug fixes and improvements to vanilla logic ARE included. If no other bear mods are included, disabling this will have no effect.")]
        public bool Enable = true;


        [Name("Spawn Weight")]
        [Slider(0.0f, 100.0f)]
        [Description("Adjust spawn weight for base bear. Higher numbers increase relative spawn chance. If no other bear mods are included, this will have no effect.")]
        public int SpawnWeight = 100;



        public override bool CanSpawn(BaseAi ai)
        {
            //LogDebug($"[BaseBearSettings] Enabled: {Enable} | AiSubtype: {ai.m_AiSubType} | Bear is null: {ai.Bear == null}");
            return Enable && ai.m_AiSubType == AiSubType.Bear && ai.Bear != null;
        }


        public override int GetSpawnWeight()
        {
            return SpawnWeight;
        }
    }
}