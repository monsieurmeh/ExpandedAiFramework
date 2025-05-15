using UnityEngine;


namespace ExpandedAiFramework
{
    public interface ICustomAi
    {
        BaseAi BaseAi { get; }
        Component Self { get; }
        void Initialize(BaseAi ai, TimeOfDay timeOfDay, SpawnRegion spawnRegion);//, EAFManager manager);
        void Despawn(float despawnTime);
        void SetAiMode(AiMode mode);
        void Update();
        void ApplyDamage(float damage, float bleedOutTime, DamageSource damageSource);
    }
}
