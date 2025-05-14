using HarmonyLib;
using UnityEngine;
using static Il2Cpp.SaveGameSlots;


namespace ExpandedAiFramework
{
    internal class Patches
    {
        #region General

        [HarmonyPatch(typeof(SpawnRegion), "InstantiateSpawnInternal", new Type[] { typeof(GameObject), typeof(WildlifeMode), typeof(Vector3), typeof(Quaternion) })]
        internal class SpawnRegionPatches_InstantiateSpawnInternal
        {
            private static void Postfix(BaseAi __result)
            {
                Manager.TryInjectCustomAi(__result);
            }
        }


        [HarmonyPatch(typeof(SaveGameSystem), "LoadSceneData", new Type[] { typeof(string), typeof(string) })]
        internal class SaveGameSystemPatches_LoadSceneData
        {
            private static void Postfix(string name, string sceneSaveName)
            {
                Manager?.ClearCustomAis();
                Manager?.RefreshAvailableMapData(sceneSaveName);
            }
        }

        #endregion


        #region Save/Load/ModData

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadSaveGameSlot), new Type[] { typeof(string), typeof(int) })]
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadSaveGameSlot), new Type[] { typeof(SaveSlotInfo) })]
        private static class GameManagerPatches_LoadSaveGameSlot
        {
            private static void Postfix()
            {
                Manager.OnLoad();
            }
        }

        [HarmonyPatch(typeof(SaveGameSlots), nameof(SaveGameSlots.WriteSlotToDisk), new Type[] { typeof(SlotData), typeof(Timestamp) })]
        private static class ModData_SaveGameSlots_WriteSlotToDisk_Postfix
        {
            private static void Prefix()
            {
                Manager.OnSave();
            }
        }

        #endregion


        #region BaseAi

        [HarmonyPatch(typeof(BaseAi), "Update")]
        internal class BaseAiPatches_Update
        {
            private static bool Prefix(BaseAi __instance)
            {
                return __instance.m_AiSubType != AiSubType.Wolf || __instance.Timberwolf;
            }
        }


        [HarmonyPatch(typeof(BaseAi), "SetAiMode", new Type[] { typeof(AiMode) })]
        internal class BaseAiPatches_SetAiMode
        {
            private static bool Prefix(BaseAi __instance, AiMode mode)
            {
                return !Manager.TrySetAiMode(__instance, mode);
            }
        }


        [HarmonyPatch(typeof(BaseAi), "ApplyDamage", new Type[] { typeof(float), typeof(DamageSource), typeof(string) })]
        internal class BaseAiPatches_ApplyDamage
        {
            private static bool Prefix(BaseAi __instance, float damage, DamageSource damageSource, string collider)
            {
                return !Manager.TryApplyDamage(__instance, damage, 0.0f, damageSource);
            }
        }


        [HarmonyPatch(typeof(BaseAi), "ApplyDamage", new Type[] { typeof(float), typeof(float), typeof(DamageSource), typeof(string) })]
        internal class BaseAiPatches_ApplyDamageWithBleedout
        {
            private static bool Prefix(BaseAi __instance, float damage, float bleedOutMintues, DamageSource damageSource, string collider)
            {
                return !Manager.TryApplyDamage(__instance, damage, bleedOutMintues, damageSource);
            }
        }


        [HarmonyPatch(typeof(BaseAi), "DeserializeUsingBaseAiDataProxy", new Type[] { typeof(BaseAiDataProxy) })]
        internal class BaseAiPatches_DeserializeUsingBaseAiDataProxy
        {
            private static void Prefix(BaseAi __instance, BaseAiDataProxy proxy)
            {
                if (Manager.CustomAis.ContainsKey(__instance?.GetHashCode() ?? 0))
                {
                    if (__instance.m_StartMode != AiMode.None)
                    {
                        proxy.m_StartMode = __instance.m_StartMode;
                    }
                    if (__instance.m_DefaultMode != AiMode.None)
                    {
                        proxy.m_DefaultMode = __instance.m_DefaultMode;
                    }
                    if (__instance.m_CurrentMode != AiMode.None)
                    {
                        proxy.m_CurrentMode = __instance.m_CurrentMode;
                    }
                    if ((__instance.m_Waypoints?.Count ?? 0) > 0)
                    {
                        proxy.m_Waypoints = __instance.m_Waypoints;
                    }
                }
            }
        }

        #endregion


        #region Console/Debug

        [HarmonyPatch(typeof(ConsoleManager), "Initialize")]
        internal class ConsoleManagerPatches_Initialize
        {
            private static void Postfix()
            {
                uConsole.RegisterCommand(EAFManager.CommandString, new Action(EAFManager.Instance.Console_OnCommand));
            }
        }

        #endregion
    }
}
