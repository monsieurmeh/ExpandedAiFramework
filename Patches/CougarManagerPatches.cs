using HarmonyLib;

namespace ExpandedAiFramework
{
    [HarmonyPatch(typeof(VanillaCougarManager), nameof(VanillaCougarManager.Update))]
    internal class CougarManagerPatches_Update
    {
        private static bool Prefix() => false;
    }

    [HarmonyPatch(typeof(VanillaCougarManager), nameof(VanillaCougarManager.Start))]
    internal class CougarManagerPatches_Start
    {
        private static bool Prefix()
        {
            EAFManager.LogWithStackTrace($"CougarManager.Start Called by vanilla!");
            return false;
        }
    }
}