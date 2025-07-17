

namespace ExpandedAiFramework
{
    public class BaseMooseSettings : TypeSpecificSettings
    {
        [Section("Base Moose Settings")]
        [Name("Enable Base Moose")]
        [Description("Enable base moose behavior with no thematic modifications. Bug fixes and improvements to vanilla logic ARE included. If no other moose mods are included, disabling this will have no effect.")]
        public bool Enable = true;


        [Name("Spawn Weight")]
        [Slider(0.0f, 100.0f)]
        [Description("Adjust spawn weight for base moose. Higher numbers increase relative spawn chance. If no other moose mods are included, this will have no effect.")]
        public int SpawnWeight = 100;


        public BaseMooseSettings(string path) : base(path) { }


        public override bool CanSpawn(BaseAi ai)
        {
            //LogDebug($"[BaseMooseSettings] Enabled: {Enable} | AiSubtype: {ai.m_AiSubType} | Moose is null: {ai.Moose == null}");
            return Enable && ai.m_AiSubType == AiSubType.Moose && ai.Moose != null;
        }


        public override int GetSpawnWeight()
        {
            return SpawnWeight;
        }
    }
}