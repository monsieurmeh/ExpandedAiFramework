using Il2CppTLD.AI;
using UnityEngine;
using Il2CppAK;

namespace ExpandedAiFramework
{
    public interface IPackManager : ISubManager
    {
        VanillaPackManager VanillaPackManager { get; }
        void OverrideStart() {}
    }
}
