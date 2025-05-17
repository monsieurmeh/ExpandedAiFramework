using HarmonyLib;
using UnityEngine;

namespace ExpandedAiFramework.CompanionWolfMod
{
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Awake))]
    internal static class GameManager_Awake
    {
        private static void Postfix()
        {
            if (!EAFManager.Instance.SubManagers.TryGetValue(typeof(CompanionWolf), out ISubManager subManager))
            {
                Utility.LogError("Unable to fetch submanager for companion wolf during patching!");
                return;
            }
            CompanionWolfManager companionWolfManager = subManager as CompanionWolfManager;
            if (companionWolfManager == null)
            {
                Utility.LogError("Fetched submanager is not correct type for companion wolf during patching!");
                return;
            }
            GameObject wolfPrefab = GameObject.Find(CompanionWolfManager.WolfPrefabString);
            if (wolfPrefab == null)
            {
                Utility.LogError("Could not find wolf prefab during companion wolf patching!");
                return;
            }
            companionWolfManager.WolfPrefab = wolfPrefab;
        }
    }
}
