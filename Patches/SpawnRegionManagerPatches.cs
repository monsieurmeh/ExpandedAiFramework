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
            return SpawnRegionManager_AddSpawnRegion(sr);
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.Add), new Type[] { typeof(NoSpawnRegion) })]
    internal class SpawnRegionManagerPatches_AddNoSpawnRegion
    {
        internal static bool Prefix(NoSpawnRegion nsr)
        {
            return SpawnRegionManager_AddNoSpawnRegion(nsr);
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.Deserialize), new Type[] { typeof(string) })]
    internal class SpawnRegionManagerPatches_Deserialize
    {
        internal static bool Prefix(string text)
        {
            return SpawnRegionManager_Deserialize(text);
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.FindSpawnRegionByGuid), new Type[] { typeof(string) })]
    internal class SpawnRegionManagerPatches_FindSpawnRegionByGuid
    {
        internal static bool Prefix(string guid, ref SpawnRegion __result)
        {
            SpawnRegion someResult = SpawnRegionManager_FindSpawnRegionByGuid(guid);
            if (someResult != null)
            {
                __result = someResult;
                return false;
            }
            return true;
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.FindSpawnRegionByPosition), new Type[] { typeof(SpawnRegionSaveData) })]
    internal class SpawnRegionManagerPatches_FindSpawnRegionByPostion
    {
        internal static bool Prefix(SpawnRegionSaveData saveData, ref SpawnRegion __result)
        {
            SpawnRegion someResult = SpawnRegionManager_FindSpawnRegionByPosition(saveData);
            if (someResult != null)
            {
                __result = someResult;
                return false;
            }
            return true;
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.GetClosestActiveSpawn), new Type[] { typeof(Vector3), typeof(string) })]
    internal class SpawnRegionManagerPatches_GetClosestActiveSpawn
    {
        internal static bool Prefix(Vector3 pos, string filterSpawnablePrefabName, ref GameObject __result)
        {
            GameObject someResult = SpawnRegionManager_GetClosestActiveSpawn(pos, filterSpawnablePrefabName);
            if (someResult != null)
            {
                __result = someResult;
                return false;
            }
            return true;
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.MaybeEnableSpawnRegionsInRange), new Type[] { typeof(SpawnRegion), typeof(float), typeof(bool) })]
    internal class SpawnRegionManagerPatches_MaybeEnableSpawnRegionsInRange
    {
        internal static bool Prefix(SpawnRegion otherSpawnRegion, float range, bool enable)
        {
            return SpawnRegionManager_MaybeEnableSpawnRegionsInRange(otherSpawnRegion, range, enable);
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.OnAuroraEnabled), new Type[] { typeof(bool) })]
    internal class SpawnRegionManagerPatches_OnAuroraEnabled
    {
        internal static bool Prefix(bool enabled)
        {
            return SpawnRegionManager_OnAuroraEnabled(enabled);
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.PointInsideActiveSpawnRegion), new Type[] { typeof(Vector3), typeof(string) })]
    internal class SpawnRegionManagerPatches_PointInsideActiveSpawnRegion
    {
        internal static bool Prefix(Vector3 pos, string filterSpawnablePrefabName, ref SpawnRegion __result)
        {
            SpawnRegion someResult = SpawnRegionManager_PointInsideActiveSpawnRegion(pos, filterSpawnablePrefabName);
            if (someResult != null)
            {
                __result = someResult;
                return false;
            }
            return true;
        }
    }




    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.PointInsideSpawnRegion), new Type[] { typeof(Vector3), typeof(string) })]
    internal class SpawnRegionManagerPatches_PointInsideSpawnRegion
    {
        internal static bool Prefix(Vector3 pos, string filterSpawnablePrefabName, ref SpawnRegion __result)
        {
            SpawnRegion someResult = SpawnRegionManager_PointInsideSpawnRegion(pos, filterSpawnablePrefabName);
            if (someResult != null)
            {
                __result = someResult;
                return false;
            }
            return true;
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.PointInsideNoSpawnRegion), new Type[] { typeof(Vector3) })]
    internal class SpawnRegionManagerPatches_PointInsideNoSpawnRegion
    {
        internal static bool Prefix(Vector3 pos, ref bool __result)
        {
            NoSpawnRegion someResult = SpawnRegionManager_PointInsideNoSpawnRegion(pos);
            if (someResult != null)
            {
                __result = someResult;
                return false;
            }
            return true;
        }
    }




    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.Remove), new Type[] { typeof(SpawnRegion) })]
    internal class SpawnRegionManagerPatches_Remove
    {
        internal static bool Prefix(SpawnRegion sr)
        {
            return SpawnRegionManager_RemoveSpawnRegion(sr);
        }
    }



    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.Serialize))]
    internal class SpawnRegionManagerPatches_Serialize
    {
        internal static bool Prefix(ref string __result)
        {
            // No log here, this is just to cut off vanilla serialization.
            return false;
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.Start))]
    internal class SpawnRegionManagerPatches_Start
    {
        internal static bool Prefix()
        {
            return SpawnRegionManager_Start();
        }
    }


    [HarmonyPatch(typeof(Il2Cpp.SpawnRegionManager), nameof(Il2Cpp.SpawnRegionManager.Update))]
    internal class SpawnRegionManagerPatches_Update
    {
        internal static bool Prefix()
        {
            // No log here, this is just cut off vanilla update.
            return false;
        }
    }
}
