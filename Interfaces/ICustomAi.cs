using UnityEngine;


namespace ExpandedAiFramework
{
    // Realistically speaking, nobody but me is ever going to use this, but I am a polymorphism nerd and can't help myself.
    // If for whatever reason someone else wants to hook their own interpretation of a runtime ai into this system (coughdzcough) then they could use this to commit identity fraud
    
    public interface ICustomAi
    {
        BaseAi BaseAi { get; }
        //
        Component Self { get; }
        SpawnModDataProxy GenerateSpawnModDataProxy();
        void Initialize(BaseAi ai, TimeOfDay timeOfDay, SpawnRegion spawnRegion);//, EAFManager manager);
        void Despawn(float despawnTime);
        void SetAiMode(AiMode mode);
        void OverrideStart();
        void Update();
        void ApplyDamage(float damage, float bleedOutTime, DamageSource damageSource);
    }
}
