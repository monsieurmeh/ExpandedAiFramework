using HarmonyLib;

namespace ExpandedAiFramework.CompanionWolfMod
{

    [HarmonyPatch(typeof(BaseAi), nameof(BaseAi.Serialize))]
    internal class BaseAiPatches_Serialize
    {
        private static bool Prefix(BaseAi __instance)
        {
            if (EAFManager.Instance.CustomAis.TryGetValue(__instance.GetHashCode(), out ICustomAi customAi) && customAi is CompanionWolf companionWolf && companionWolf.PersistentData.Tamed)
            {
                Utility.LogDebug($"Intercepting attempt to serialize custom-spawned companion wolf!");
                return false;
            }
            return true;
        }
    }
}
