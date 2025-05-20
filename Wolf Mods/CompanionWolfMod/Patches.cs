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
                Utility.LogVerbose($"Intercepting attempt to serialize custom-spawned companion wolf!");
                return false;
            }
            return true;
        }
    }


    [HarmonyPatch(typeof(ConsoleManager), nameof(ConsoleManager.Initialize))]
    internal class ConsoleManagerPatches_Initialize
    {
        private static void Postfix()
        {
            uConsole.RegisterCommand(CompanionWolfManager.CWolfCommandString, new Action(CompanionWolfManager.Console_OnCommand));
        }
    }
}
