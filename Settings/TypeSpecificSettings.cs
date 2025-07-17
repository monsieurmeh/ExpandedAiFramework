
using System.Reflection;


namespace ExpandedAiFramework
{
    public abstract class TypeSpecificSettings : JsonModSettings, ISpawnTypePickerCandidate
    {
        //Include a field name named "Enable"!

        public TypeSpecificSettings(string path) : base(path) { }

        public abstract bool CanSpawn(BaseAi baseAi);
        public abstract int GetSpawnWeight();
        public virtual bool ForceSpawningEnabled() { return false; }
        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
            ShowSettingsIfEnabled();
        }
        public void ShowSettingsIfEnabled()
        {
            FieldInfo enabled = GetType().GetField("Enable", BindingFlags.Instance | BindingFlags.Public);
            if (enabled == null)
            {
                LogTrace($"{this} has no public bool field Enabled!");
                return;
            }
            bool isEnabled = (bool)enabled.GetValue(this);
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < fields.Length; ++i)
            {
                if (fields[i] != enabled)
                {
                    SetFieldVisible(fields[i], isEnabled);
                }
            }
        }


        bool ISpawnTypePickerCandidate.CanSpawn(BaseAi baseAi) => CanSpawn(baseAi);
        int ISpawnTypePickerCandidate.SpawnWeight() => GetSpawnWeight();
    }
}
