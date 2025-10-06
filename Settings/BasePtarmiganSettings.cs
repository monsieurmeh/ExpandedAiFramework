

using ExpandedAiFramework.Enums;

namespace ExpandedAiFramework
{
    public class BasePtarmiganSettings : TypeSpecificSettings
    {
        [Section("Base Ptarmigan Settings")]
        [Name("Enable Base Ptarmigan")]
        [Description("Enable base ptarmigan behavior with no thematic modifications. Bug fixes and improvements to vanilla logic ARE included. If no other ptarmigan mods are included, disabling this will have no effect.")]
        public bool Enable = true;


        [Name("Spawn Weight")]
        [Slider(0.0f, 100.0f)]
        [Description("Adjust spawn weight for base ptarmigan. Higher numbers increase relative spawn chance. If no other ptarmigan mods are included, this will have no effect.")]
        public int SpawnWeight = 100;


        public BasePtarmiganSettings(string path) : base(path) { }


        public override bool CanSpawn(BaseAi ai)
        {
            LogDebug($"[BasePtarmiganSettings] Enabled: {Enable} | AiSubtype: {ai.m_AiSubType} | Rabbit is null: {ai.Rabbit == null} | Ptarmigan is null: {ai.Ptarmigan == null}", LogCategoryFlags.Ai);
            return Enable && ai.m_AiSubType == AiSubType.Rabbit && ai.Rabbit == null && ai.Ptarmigan != null;
        }


        public override int GetSpawnWeight()
        {
            return SpawnWeight;
        }
    }
}