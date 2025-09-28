using HarmonyLib;
using Il2Cpp;
using UnityEngine;
using static ExpandedAiFramework.PatchExtensions;

namespace ExpandedAiFramework
{
    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.Awake))]
    internal class SpawnRegionPatches_Awake
    {
        private static bool Prefix(SpawnRegion __instance)
        {
            return SpawnRegion_Awake(__instance);
        }
    }


    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.Start))]
    internal class SpawnRegionPatches_Start
    {
        internal static bool Prefix(SpawnRegion __instance)
        {
            return false;
        }
    }


    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.GetClosestActiveSpawn), new Type[] { typeof(Vector3) })]
    internal class SpawnRegionPatches_GetClosestActiveSpawn
    {
        internal static bool Prefix(SpawnRegion __instance, Vector3 pos, ref GameObject __result)
        {
            return SpawnRegion_GetClosestActiveSpawn(__instance, pos, ref __result);
        }
    }



    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.GetDenSleepDurationInHours))]
    internal class SpawnRegionPatches_GetDenSleepDurationInHours
    {
        internal static bool Prefix(SpawnRegion __instance, ref float __result)
        {
            EAFManager.LogWithStackTrace($"WARNING: External call detected! EAF has cut this method off, expect malfunction from calling mod!");
            return false; 
        }
    }


    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.GetNumActiveSpawns))]
    internal class SpawnRegionPatches_GetNumActiveSpawns
    {
        internal static bool Prefix(SpawnRegion __instance, ref int __result)
        {
            return SpawnRegion_GetNumActiveSpawns(__instance, ref __result);
        }
    }

    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.GetWanderRegion), new Type[] { typeof(Vector3) })]
    internal class SpawnRegionPatches_GetWanderRegion
    {
        internal static bool Prefix(SpawnRegion __instance, Vector3 pos, ref WanderRegion __result)
        {
            return SpawnRegion_GetWanderRegion(__instance, pos, ref __result);
        }
    }


    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.GetWaypointCircuit))]
    internal class SpawnRegionPatches_GetWaypointCircuit
    {
        internal static bool Prefix(SpawnRegion __instance, ref Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3> __result)
        {
            return SpawnRegion_GetWaypointCircuit(__instance, ref __result);
        }
    }


    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.UpdateDeferredDeserializeFromManager))]
    internal class SpawnRegionPatches_UpdateDeferredDeserializeFromManager
    {
        internal static bool Prefix(SpawnRegion __instance)
        {
            EAFManager.LogWithStackTrace($"WARNING: External call detected! EAF has cut this method off, expect malfunction from calling mod!");
            return false; 
        }
    }

    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.UpdateFromManager))]
    internal class SpawnRegionPatches_UpdateFromManager
    {
        internal static bool Prefix(SpawnRegion __instance)
        {
            EAFManager.LogWithStackTrace($"WARNING: External call detected! EAF has cut this method off, expect malfunction from calling mod!");
            return false;
        }
    }

    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.SetRandomWaypointCircuit))]
    internal class SpawnRegionPatches_SetRandomWaypointCircuit
    {
        internal static bool Prefix(SpawnRegion __instance)
        {
            return SpawnRegion_SetRandomWaypointCircuit(__instance);
        }
    }

    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.ShouldSleepInDenAfterWaypointLoop))]
    internal class SpawnRegionPatches_ShouldSleepInDenAfterWaypointLoop
    {
        internal static bool Prefix(SpawnRegion __instance, ref bool __result)
        {
            EAFManager.LogWithStackTrace($"WARNING: External call detected! EAF has cut this method off, expect malfunction from calling mod!");
            return false;
        }
    }

    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.SetActive), new Type[] { typeof(bool) })]
    internal class SpawnRegionPatches_SetActive
    {
        internal static bool Prefix(SpawnRegion __instance, bool active)
        {
            return SpawnRegion_SetActive(__instance, active);
        }
    }

    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.Serialize))]
    internal class SpawnRegionPatches_Serialize
    {
        internal static bool Prefix(SpawnRegion __instance, ref string __result)
        {
            EAFManager.LogWithStackTrace($"WARNING: EAF does not support vanilla serialization. Nobody wants corrupted vanilla save data, and vanilla serialization is very temporary anyways!");
            return false;
        }
    }

    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.RemoveFromSpawnRegion), new Type[] { typeof(BaseAi) })]
    internal class SpawnRegionPatches_RemoveFromSpawnRegion
    {
        internal static bool Prefix(SpawnRegion __instance, BaseAi bai)
        {
            return SpawnRegion_RemoveFromSpawnRegion(__instance, bai);
        }
    }

    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.OnAuroraEnabled), new Type[] { typeof(bool) })]
    internal class SpawnRegionPatches_OnAuroraEnabled
    {
        internal static bool Prefix(SpawnRegion __instance, bool enabled)
        {
            return SpawnRegion_OnAuroraEnabled(__instance, enabled);
        }
    }

    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.MaybeReRollActive))]
    internal class SpawnRegionPatches_MaybeReRollActive
    {
        internal static bool Prefix(SpawnRegion __instance)
        {
            EAFManager.LogWithStackTrace($"WARNING: External call detected! EAF has cut this method off, expect malfunction from calling mod!");
            return false; //return SpawnRegion_MaybeReRollActive(__instance);
        }
    }


    [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.Deserialize), new Type[] { typeof(string) })]
    internal class SpawnRegionPatches_Deserialize
    {
        internal static bool Prefix(SpawnRegion __instance, string text)
        {
            EAFManager.LogWithStackTrace($"WARNING: External call detected! EAF has cut this method off, expect malfunction from calling mod!");
            return false; //return SpawnRegion_Deserialize(__instance, text);
        }
    }
}
