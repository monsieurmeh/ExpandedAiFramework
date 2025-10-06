using HarmonyLib;
using Il2CppTLD.AI;
using Il2CppTLD.UI;
using UnityEngine;

namespace ExpandedAiFramework
{
    [HarmonyPatch(typeof(CougarTerritoryZoneTrigger), nameof(CougarTerritoryZoneTrigger.ActivateZone), new Type[] { typeof(bool)})]
    internal class CougarTerritoryZoneTriggerPatches_ActivateZone
    {
        private static bool Prefix(bool enable, CougarTerritoryZoneTrigger __instance)
        {                
            SpawnRegion spawnRegion = __instance.m_SpawnRegion;
            if (spawnRegion == null)
            {
                LogError($"Null spawn region!");
                return false;
            }
            GameObject spawnRegionGameObjet = spawnRegion.gameObject;
            if (spawnRegionGameObjet == null)
            {
                LogError($"Null spawn region game object!");
                return false;
            }
            MapDetail mapDetail = __instance.m_MapDetail;
            if (mapDetail == null)
            {
                LogError($"Null map detail!");
                return false;
            }                
            if (!mapDetail.m_Map.TryGetPanel(out Panel_Map panelMap))
            {
                LogError($"Failed to get panel map!");
                return false;
            }
            if (enable) 
            {
                if (EAFManager.Instance.SpawnRegionManager.CustomSpawnRegions.TryGetValue(spawnRegion.GetHashCode(), out _))
                {
                    LogTrace($"Cougar spawn region already registered, skipping.", LogCategoryFlags.CougarManager);
                    return false;
                }
                if (!EAFManager.Instance.SpawnRegionManager.Add(spawnRegion))
                {
                    LogError($"Failed to add spawn region to spawn region manager when trying to enable cougar spawn region via zone trigger!");
                    return false;
                }
                mapDetail.m_IsSurveyed = true;
                panelMap.DoNearbyDetailsCheck(__instance.m_MapRevealRadius, false, true, mapDetail.GetWorldPosition(), false);
            }
            GameObject audioGameObject = __instance.m_AudioGameObject;
            if (audioGameObject != null)
            {
                audioGameObject.SetActive(enable);
            } 
            spawnRegionGameObjet.SetActive(enable);
            __instance.m_BoxCollider.enabled = enable;
            mapDetail.gameObject.SetActive(enable);
            mapDetail.ShowOnMap(enable);
            __instance.MaybePlaceCarcass();
            GameObject visualsGameObject = __instance.m_VisualsGameObject;
            if (visualsGameObject != null)
            {
                visualsGameObject.SetActive(true);
            }
            EAFManager.Instance.SpawnRegionManager.MaybeEnableSpawnRegionsInRange(spawnRegion, __instance.m_DisableSpawnRegionsInRange, enable);
            return false;
        }
    }
}