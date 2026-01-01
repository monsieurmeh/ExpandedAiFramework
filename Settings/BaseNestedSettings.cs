
using System.Reflection;


namespace ExpandedAiFramework
{
    public abstract class BaseNestedSettings : JsonModSettings
    {
        public BaseNestedSettings(string path) : base(path) => ShowSettingsIfEnabled();

        protected override void OnChange(FieldInfo field, object oldValue, object newValue) => ShowSettingsIfEnabled();
        
        
        public virtual void ShowSettingsIfEnabled()
        {
            FieldInfo enabled = GetType().GetField("Enable", BindingFlags.Instance | BindingFlags.Public);
            if (enabled == null)
            {
                Log($"{this} has no public bool field Enabled!");
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
    }
}
