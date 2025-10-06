using HarmonyLib;

namespace ExpandedAiFramework.CompanionWolfMod
{

    [HarmonyPatch(typeof(BaseAi), nameof(BaseAi.Serialize))]
    internal class BaseAiPatches_Serialize
    {
        private static bool Prefix(BaseAi __instance)
        {
            if (EAFManager.Instance.CustomAis.TryGetValue(__instance.GetHashCode(), out CustomBaseAi customAi) && customAi is CompanionWolf companionWolf && companionWolf.PersistentData.Tamed)
            {
                Utility.LogTrace($"Intercepting attempt to serialize custom-spawned companion wolf!", LogCategoryFlags.Ai);
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
