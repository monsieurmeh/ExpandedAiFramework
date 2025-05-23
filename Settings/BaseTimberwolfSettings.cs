

using ExpandedAiFramework.Enums;

namespace ExpandedAiFramework
{
    public class BaseTimberwolfSettings : TypeSpecificSettings
    {
        [Section("Base Timberwolf Settings")]
        [Name("Enable Base Timberwolves")]
        [Description("Enable base timberwolf behavior with no thematic modifications. Bug fixes and improvements to vanilla logic ARE included. If no other timberwolf mods are installed, this will have no effect.")]
        public bool Enable = true;


        [Name("Spawn Weight")]
        [Slider(0.0f, 100.0f)]
        [Description("Adjust spawn weight for base timberwolves. Higher numbers increase relative spawn chance. If no other timberwolf mods are installed, this will have no effect.")]
        public int SpawnWeight = 100;


        public override bool CanSpawn(BaseAi ai)
        {
            //LogDebug($"[BaseTimberwolfSettings] Enabled: {Enable} | AiSubtype: {ai.m_AiSubType} | BaseWolf is null: {ai.BaseWolf == null} | Timberwolf is null: {ai.Timberwolf == null}");
            return Enable && ai.m_AiSubType == AiSubType.Wolf && ai.BaseWolf != null && ai.Timberwolf != null;
        }


        public override int GetSpawnWeight()
        {
            return SpawnWeight;
        }
    }
}