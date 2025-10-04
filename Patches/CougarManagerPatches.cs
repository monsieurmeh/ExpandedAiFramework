using HarmonyLib;

namespace ExpandedAiFramework
{
    [HarmonyPatch(typeof(VanillaCougarManager), nameof(VanillaCougarManager.Update))]
    internal class CougarManagerPatches_Update
    {
        private static bool Prefix() => false;
    }
}