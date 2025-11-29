
using System.Reflection;


namespace ExpandedAiFramework
{
    public abstract class TypeSpecificSettings : BaseNestedSettings, ISpawnTypePickerCandidate
    {
        public TypeSpecificSettings(string path) : base(path) { }
        public abstract bool CanSpawn(BaseAi baseAi);
        public abstract int GetSpawnWeight();
        public virtual bool ForceSpawningEnabled() { return false; }
        protected virtual void OnPick() { }
        void ISpawnTypePickerCandidate.OnPick() => OnPick();
        bool ISpawnTypePickerCandidate.CanSpawn(BaseAi baseAi) => CanSpawn(baseAi);
        int ISpawnTypePickerCandidate.SpawnWeight() => GetSpawnWeight();
    }
}
