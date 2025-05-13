using UnityEngine;


namespace ExpandedAiFramework
{
    public interface ICustomAi
    {
        BaseAi BaseAi { get; }
        Component Self { get; }
        //todo: We don't need intitialize AND augment. Combine them
        void Initialize(BaseAi ai, TimeOfDay timeOfDay);
        void SetAiMode(AiMode mode);
        void Update();
        void Augment();
        void UnAugment();
        void ApplyDamage(float damage, float bleedOutTime, DamageSource damageSource);
    }
}
