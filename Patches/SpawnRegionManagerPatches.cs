using HarmonyLib;
using static ExpandedAiFramework.PatchExtensions;
using Il2Cpp;
using UnityEngine;


namespace ExpandedAiFramework
{
    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.Add), new Type[] { typeof(SpawnRegion) })]
    internal class SpawnRegionManagerPatches_AddSpawnRegion
    {
        internal static bool Prefix(SpawnRegion sr)
        {
            LogTrace($"Intercepting SpawnRegionManager.Add(SpawnRegion sr)!");
            SpawnRegionManager_AddSpawnRegion(sr);
            return false;
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.Add), new Type[] { typeof(NoSpawnRegion) })]
    internal class SpawnRegionManagerPatches_AddNoSpawnRegion
    {
        internal static bool Prefix(NoSpawnRegion nsr)
        {
            LogTrace($"Intercepting SpawnRegionManager.Add(NoSpawnRegion nsr)!");
            SpawnRegionManager_AddNoSpawnRegion(nsr);
            return false;
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.Deserialize), new Type[] { typeof(string) })]
    internal class SpawnRegionManagerPatches_Deserialize
    {
        internal static bool Prefix(string text)
        {
            LogTrace($"Intercepting SpawnRegionManager.Deserialize(string text)!");
            SpawnRegionManager_Deserialize(text);
            return false;
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.FindSpawnRegionByGuid), new Type[] { typeof(string) })]
    internal class SpawnRegionManagerPatches_FindSpawnRegionByGuid
    {
        internal static bool Prefix(string guid, ref SpawnRegion __result)
        {
            LogTrace($"Intercepting SpawnRegionManager.FindSpawnRegionByGuid(string text)!");
            __result = SpawnRegionManager_FindSpawnRegionByGuid(guid);
            return false;
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.FindSpawnRegionByPosition), new Type[] { typeof(SpawnRegionSaveData) })]
    internal class SpawnRegionManagerPatches_FindSpawnRegionByPostion
    {
        internal static bool Prefix(SpawnRegionSaveData saveData, ref SpawnRegion __result)
        {
            LogTrace($"Intercepting SpawnRegionManager.FindSpawnRegionByPosition(SpawnRegionSaveData saveData)!");
            __result = SpawnRegionManager_FindSpawnRegionByPosition(saveData);
            return false;
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.GetClosestActiveSpawn), new Type[] { typeof(Vector3), typeof(string) })]
    internal class SpawnRegionManagerPatches_GetClosestActiveSpawn
    {
        internal static bool Prefix(Vector3 pos, string filterSpawnablePrefabName, ref GameObject __result)
        {
            LogTrace($"Intercepting SpawnRegionManager.GetClosestActiveSpawn(Vector3 pos, string filterSpawnablePrefabName)!");
            __result = SpawnRegionManager_GetClosestActiveSpawn(pos, filterSpawnablePrefabName);
            return false;
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.MaybeEnableSpawnRegionsInRange), new Type[] { typeof(SpawnRegion), typeof(float), typeof(bool) })]
    internal class SpawnRegionManagerPatches_MaybeEnableSpawnRegionsInRange
    {
        internal static bool Prefix(SpawnRegion otherSpawnRegion, float range, bool enable)
        {
            LogTrace($"Intercepting SpawnRegionManager.MaybeEnableSpawnRegionsInRange(SpawnRegion otherSpawnRegion, float range, bool enable)!");
            SpawnRegionManager_MaybeEnableSpawnRegionsInRange(otherSpawnRegion, range, enable);
            return false;
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.OnAuroraEnabled), new Type[] { typeof(bool) })]
    internal class SpawnRegionManagerPatches_OnAuroraEnabled
    {
        internal static bool Prefix(bool enabled)
        {
            LogTrace($"Intercepting SpawnRegionManager.OnAuroraEnabled(bool enabled)!");
            SpawnRegionManager_OnAuroraEnabled(enabled);
            return false;
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.PointInsideActiveSpawnRegion), new Type[] { typeof(Vector3), typeof(string) })]
    internal class SpawnRegionManagerPatches_PointInsideActiveSpawnRegion
    {
        internal static bool Prefix(Vector3 pos, string filterSpawnablePrefabName, ref SpawnRegion __result)
        {
            LogTrace($"Intercepting SpawnRegionManager.PointInsideActiveSpawnRegion(Vector3 pos, string filterSpawnablePrefabName)!");
            __result = SpawnRegionManager_PointInsideActiveSpawnRegion(pos, filterSpawnablePrefabName);
            return false;
        }
    }




    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.PointInsideSpawnRegion), new Type[] { typeof(Vector3), typeof(string) })]
    internal class SpawnRegionManagerPatches_PointInsideSpawnRegion
    {
        internal static bool Prefix(Vector3 pos, string filterSpawnablePrefabName, ref SpawnRegion __result)
        {
            LogTrace($"Intercepting SpawnRegionManager.PointInsideSpawnRegion(Vector3 pos, string filterSpawnablePrefabName)!");
            __result = SpawnRegionManager_PointInsideSpawnRegion(pos, filterSpawnablePrefabName);
            return false;
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.PointInsideNoSpawnRegion), new Type[] { typeof(Vector3) })]
    internal class SpawnRegionManagerPatches_PointInsideNoSpawnRegion
    {
        internal static bool Prefix(Vector3 pos, ref bool __result)
        {
            LogTrace($"Intercepting SpawnRegionManager.PointInsideNoSpawnRegion(Vector3 pos)!");
            __result = SpawnRegionManager_PointInsideNoSpawnRegion(pos);
            return false;
        }
    }




    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.Remove), new Type[] { typeof(SpawnRegion) })]
    internal class SpawnRegionManagerPatches_Remove
    {
        internal static bool Prefix(SpawnRegion sr)
        {
            LogTrace($"Intercepting SpawnRegionManager.Remove(SpawnRegion sr)!");
            SpawnRegionManager_Remove(sr);
            return false;
        }
    }



    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.Serialize))]
    internal class SpawnRegionManagerPatches_Serialize
    {
        internal static bool Prefix(ref string __result)
        {
            LogTrace($"Intercepting SpawnRegionManager.Serialize()!");
            __result = SpawnRegionManager_Serialize();
            return false;
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.Start))]
    internal class SpawnRegionManagerPatches_Start
    {
        internal static bool Prefix()
        {
            LogTrace($"Intercepting SpawnRegionManager.Start()!");
            SpawnRegionManager_Start();
            return false;
        }
    }


    //Updating now happens on EAF schedule ;)
    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.Update))]
    internal class SpawnRegionManagerPatches_Update
    {
        internal static bool Prefix()
        {
            return false;
        }
    }
}
