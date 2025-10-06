using HarmonyLib;

namespace ExpandedAiFramework
{
    [HarmonyPatch(typeof(VanillaPackManager), nameof(VanillaPackManager.Update))]
    internal class PackManagerPatches_Update
    {
        private static bool Prefix() => false;
    }

    [HarmonyPatch(typeof(VanillaPackManager), nameof(VanillaPackManager.Start))]
    internal class PackManagerPatches_Start
    {
        private static bool Prefix() => false;
    }

    [HarmonyPatch(typeof(VanillaPackManager), nameof(VanillaPackManager.InPack), new Type[] { typeof(PackAnimal) })]
    internal class PackManagerPatches_InPack
    {
        private static bool Prefix(PackAnimal packAnimal, ref bool __result)
        {
            return true; //not sure yet, but EAF itself is calling this so we may need to override it
        }
    }
}