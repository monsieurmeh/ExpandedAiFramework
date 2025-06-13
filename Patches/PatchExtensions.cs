using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ExpandedAiFramework
{
    public class PatchExtensions
    {
        //Putting these here for now so they are public. Eventually I'd like to support an "extension" system where I relinquish some control to other mods in the same way that Im doign with harmony
        // i.e. someone can "prefix my prefix" via reflection or some plugin Ill develop rather than just have their mod "not work" when used with EAF

        #region SpawnRegionManager

        //public static void SpawnRegionManager_AddSpawnRegion(SpawnRegion sr) => Manager.SpawnRegionManager.Add(sr);
        public static void SpawnRegionManager_AddNoSpawnRegion(NoSpawnRegion nsr) => Manager.SpawnRegionManager.Add(nsr);
        public static void SpawnRegionManager_Deserialize(string text) => Manager.SpawnRegionManager.Deserialize(text);
        public static SpawnRegion SpawnRegionManager_FindSpawnRegionByGuid(string text) => Manager.SpawnRegionManager.FindSpawnRegionByGuid(text);
        public static SpawnRegion SpawnRegionManager_FindSpawnRegionByPosition(SpawnRegionSaveData saveData) => Manager.SpawnRegionManager.FindSpawnRegionByPosition(saveData);
        public static GameObject SpawnRegionManager_GetClosestActiveSpawn(Vector3 pos, string filterSpawnablePrefabName) => Manager.SpawnRegionManager.GetClosestActiveSpawn(pos, filterSpawnablePrefabName);
        public static void SpawnRegionManager_MaybeEnableSpawnRegionsInRange(SpawnRegion otherSpawnRegion, float range, bool enable) => Manager.SpawnRegionManager.MaybeEnableSpawnRegionsInRange(otherSpawnRegion, range, enable);
        public static void SpawnRegionManager_OnAuroraEnabled(bool enabled) => Manager.SpawnRegionManager.OnAuroraEnabled(enabled);
        public static SpawnRegion SpawnRegionManager_PointInsideActiveSpawnRegion(Vector3 pos, string filterSpawnablePrefabName) => Manager.SpawnRegionManager.PointInsideActiveSpawnRegion(pos, filterSpawnablePrefabName);
        public static SpawnRegion SpawnRegionManager_PointInsideSpawnRegion(Vector3 pos, string filterSpawnablePrefabName) => Manager.SpawnRegionManager.PointInsideSpawnRegion(pos, filterSpawnablePrefabName);
        public static bool SpawnRegionManager_PointInsideNoSpawnRegion(Vector3 pos) => Manager.SpawnRegionManager.PointInsideNoSpawnRegion(pos);
        //public static void SpawnRegionManager_Remove(SpawnRegion sr) => Manager.SpawnRegionManager.Remove(sr);
        public static string SpawnRegionManager_Serialize() => Manager.SpawnRegionManager.Serialize();
        public static void SpawnRegionManager_Start() => Manager.SpawnRegionManager.Start();

        #endregion


        #region SpawnRegion


        public static bool SpawnRegion_Awake(SpawnRegion __instance)
        {
            return !Manager.SpawnRegionManager.TryAwake(__instance);
        }



        public static bool SpawnRegion_CanTrap(SpawnRegion __instance, ref bool __result)
        {
            return !Manager.SpawnRegionManager.TryCanTrap(__instance, ref __result);
        }


        public static bool SpawnRegion_GetClosestActiveSpawn(SpawnRegion __instance, Vector3 pos, ref GameObject __result)
        {
            return !Manager.SpawnRegionManager.TryGetClosestActiveSpawn(__instance, pos, ref __result);
        }


        public static bool SpawnRegion_GetDenSleepDurationInHours(SpawnRegion __instance, ref float __result)
        {
            return !Manager.SpawnRegionManager.TryGetDenSleepDurationInHours(__instance, ref __result);
        }


        public static bool SpawnRegion_GetNumActiveSpawns(SpawnRegion __instance, ref int __result)
        {
            return !Manager.SpawnRegionManager.TryGetNumActiveSpawns(__instance, ref __result);
        }


        public static bool SpawnRegion_GetWanderRegion(SpawnRegion __instance, Vector3 pos, ref WanderRegion __result)
        {
            return !Manager.SpawnRegionManager.TryGetWanderRegion(__instance, pos, ref __result);
        }


        public static bool SpawnRegion_GetWaypointCircuit(SpawnRegion __instance, ref Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3> __result)
        {
            return !Manager.SpawnRegionManager.TryGetWaypointCircuit(__instance, ref __result);
        }


        public static bool SpawnRegion_UpdateDeferredDeserializeFromManager(SpawnRegion __instance)
        {
            return !Manager.SpawnRegionManager.TryUpdateDeferredDeserializeFromManager(__instance);
        }

        public static bool SpawnRegion_UpdateFromManager(SpawnRegion __instance)
        {
            return !Manager.SpawnRegionManager.TryUpdateFromManager(__instance);
        }

        public static bool SpawnRegion_SetRandomWaypointCircuit(SpawnRegion __instance)
        {
            return !Manager.SpawnRegionManager.TrySetRandomWaypointCircuit(__instance);
        }

        public static bool SpawnRegion_ShouldSleepInDenAfterWaypointLoop(SpawnRegion __instance, ref bool __result)
        {
            return !Manager.SpawnRegionManager.TryShouldSleepInDenAfterWaypointLoop(__instance, ref __result);
        }

        public static bool SpawnRegion_SetActive(SpawnRegion __instance, bool active)
        {
            return !Manager.SpawnRegionManager.TrySetActive(__instance, active);
        }

        public static bool SpawnRegion_Serialize(SpawnRegion __instance, ref string __result)
        {
            return !Manager.SpawnRegionManager.TrySerialize(__instance, ref __result);
        }

        public static bool SpawnRegion_RemoveFromSpawnRegion(SpawnRegion __instance, BaseAi baseAi)
        {
            return !Manager.SpawnRegionManager.TryRemoveFromSpawnRegion(__instance, baseAi);
        }

        public static bool SpawnRegion_OnAuroraEnabled(SpawnRegion __instance, bool enabled)
        {
            return !Manager.SpawnRegionManager.TryOnAuroraEnabled(__instance, enabled);
        }

        public static bool SpawnRegion_MaybeReRollActive(SpawnRegion __instance)
        {
            return !Manager.SpawnRegionManager.TryMaybeReRollActive(__instance);
        }


        public static bool SpawnRegion_Start(SpawnRegion __instance)
        {
            return !Manager.SpawnRegionManager.TryStart(__instance);
        }

        public static bool SpawnRegion_Deserialize(SpawnRegion __instance, string text)
        {
            return !Manager.SpawnRegionManager.TryDeserialize(__instance, text);
        }


        #endregion
    }
}
