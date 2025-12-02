using System.Reflection;

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

        [Name("Enable Stalking Timeout")]
        [Description("Allow stalking timeout. Prevents indefinite stalking behavior by switching to from stalking to attack after a set period of time.")]
        public bool EnableStalkingTimeout = false;

        [Name("Stalking Timeout")]
        [Slider(0.0f, 30.0f)]
        [Description("Prevents indefinite stalking behavior by switching to attack state after this length of time.")]
        public float StalkingTimeout = 10.0f;


        [Name("DANGER: Increase Max Spawn Count")]
        [Slider(0, 25)]
        [Description("Spawns more wolves... What else do you want?")]
        public int MaxWolfSpawnCountIncrease = 0;


        public BaseWolfSettings(string path) : base(path) { }

        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
            switch (field.Name)
            {
                case "EnableStalkingTimeout":
                    SetFieldVisible(GetType().GetField("StalkingTimeout", BindingFlags.Instance | BindingFlags.Public), (bool)newValue);
                    break;
            }
            base.OnChange(field, oldValue, newValue);
        }

        public override void ShowSettingsIfEnabled()
        {
            base.ShowSettingsIfEnabled();
            SetFieldVisible(GetType().GetField("StalkingTimeout", BindingFlags.Instance | BindingFlags.Public), EnableStalkingTimeout);
        }


        public override bool CanSpawn(BaseAi ai)
        {
            LogDebug($"[BaseWolfSettings] Enabled: {Enable} | AiSubtype: {ai.m_AiSubType} | BaseWolf is null: {ai.BaseWolf == null} | Timberwolf is null: {ai.Timberwolf == null}", LogCategoryFlags.Ai);
            return Enable && ai.m_AiSubType == AiSubType.Wolf && ai.BaseWolf != null && ai.Timberwolf == null;
        }


        public override int GetSpawnWeight() 
        { 
            return SpawnWeight; 
        }
    }
}