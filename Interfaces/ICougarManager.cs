using Il2CppTLD.AI;
using UnityEngine;
using Il2CppAK;

namespace ExpandedAiFramework
{
    public interface ICougarManager : ISubManager
    {
        VanillaCougarManager VanillaCougarManager { get; }
        void Update() => VanillaCougarManager.Update();
    }
}
