using HarmonyLib;
using UnityEngine;


namespace ExpandedAiFramework
{
    #region SpawnRegion


    internal class SpawnRegionPatches
    {
        [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.InstantiateSpawnInternal), new Type[] { typeof(GameObject), typeof(WildlifeMode), typeof(Vector3), typeof(Quaternion) })]
        internal class SpawnRegionPatches_InstantiateSpawnInternal
        {
            private static void Postfix(BaseAi __result, SpawnRegion __instance)
            {
                LogVerbose($"[SpawnRegionPatches_InstantiateSpawnInternal.Postfix]: SpawnRegion.InstantiateSpawnInternal on {__result.gameObject.name} at {__result?.transform?.position ?? Vector3.zero}");
                if (!Manager.SpawnRegionManager.TryInterceptSpawn(__result, __instance))
                {
                    LogError("[SpawnRegionPatches_InstantiateSpawnInternal.Postfix]: Spawn intercept error!");
                }
            }
        }



        [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.Start))]
        internal class SpawnRegionPatches_Start
        {
            private static /*bool*/ void Prefix(SpawnRegion __instance)
            {
                //return !Manager.TryStart(__instance);
                if (!Manager.TryStart(__instance))
                {
                    //LogTrace($"[SpawnRegionPatches_Start.Prefix]: Could not start spawn region instance with hash code {__instance.GetHashCode()}!");
                }
            }
        }


        #endregion


        #region SpawnRegionManager



        #endregion
    }
}
