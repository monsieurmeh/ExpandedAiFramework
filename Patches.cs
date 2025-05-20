using HarmonyLib;
using UnityEngine;
using static Il2Cpp.SaveGameSlots;
using System.Collections;


namespace ExpandedAiFramework
{
    internal class Patches
    {
        #region General


        [HarmonyPatch(typeof(SpawnRegion), nameof(SpawnRegion.InstantiateSpawnInternal), new Type[] { typeof(GameObject), typeof(WildlifeMode), typeof(Vector3), typeof(Quaternion) })]
        internal class SpawnRegionPatches_InstantiateSpawnInternal
        {
            private static void Postfix(BaseAi __result, SpawnRegion __instance)
            {
                LogVerbose($"SpawnRegion.InstantiateSpawnInternal on {__result.gameObject.name} at {__result?.transform?.position ?? Vector3.zero}");
                Manager.TryInjectRandomCustomAi(__result, __instance);
            }
        }

        
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadScene), new Type[] { typeof(string), typeof(string) })]
        internal class GameManagerPatches_LoadScene
        {
            private static void Postfix()
            {
                LogVerbose("OnLoadScene");
                Manager.OnLoadScene();
            }
        }
        
        #endregion


        #region Save/Load/ModData
        

        [HarmonyPatch(typeof(SaveGameSlots), nameof(SaveGameSlots.CreateSlot), new Type[] { typeof(string), typeof(SaveSlotType), typeof(uint), typeof(Episode) })]
        private static class SaveGameSlotsPatches_CreateSlow
        {
            private static void Postfix()
            {
                Utility.LogVerbose("OnStartNewGame");
                Manager.OnStartNewGame();
            }
        }
        
        

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadSaveGameSlot), new Type[] { typeof(string), typeof(int) })]
        private static class GameManagerPatches_LoadSaveGameSlot
        {
            private static void Postfix()
            {
                Utility.LogVerbose("OnLoadGame");
                Manager.OnLoadGame();
            }
        }

        //[HarmonyPatch(typeof(SaveGameSlots), nameof(SaveGameSlots.WriteSlotToDisk), new Type[] { typeof(string) })]
        [HarmonyPatch(typeof(SaveGameSlots), nameof(SaveGameSlots.WriteSlotToDisk), new Type[] { typeof(SlotData), typeof(Timestamp) })]
        private static class ModData_SaveGameSlots_WriteSlotToDisk_Postfix
        {
            private static void Prefix()
            {
                Utility.LogVerbose("OnSaveGame");
                Manager.OnSaveGame();
            }
        }

        #endregion


        #region BaseAi

        [HarmonyPatch(typeof(BaseAi), nameof(BaseAi.Start))]
        internal class BaseAiPatches_Start
        {
            private static bool Prefix(BaseAi __instance)
            {
                Utility.LogVerbose($"Start on {__instance.gameObject.name} with ai subtype {__instance.m_AiSubType} at {__instance.transform.position}!");
                return !Manager.TryStart(__instance);
                //return __instance.m_AiSubType != AiSubType.Wolf || __instance.Timberwolf;
            }
        }


        [HarmonyPatch(typeof(BaseAi), nameof(BaseAi.Update))]
        internal class BaseAiPatches_Update
        {
            private static bool Prefix(BaseAi __instance)
            {
                return false;// __instance.m_AiSubType != AiSubType.Wolf || __instance.Timberwolf;
            }
        }


        [HarmonyPatch(typeof(BaseAi), nameof(BaseAi.SetAiMode), new Type[] { typeof(AiMode) })]
        internal class BaseAiPatches_SetAiMode
        {
            private static bool Prefix(BaseAi __instance, AiMode mode)
            {
                return !Manager.TrySetAiMode(__instance, mode);
            }
        }


        [HarmonyPatch(typeof(BaseAi), nameof(BaseAi.ApplyDamage), new Type[] { typeof(float), typeof(DamageSource), typeof(string) })]
        internal class BaseAiPatches_ApplyDamage
        {
            private static bool Prefix(BaseAi __instance, float damage, DamageSource damageSource, string collider)
            {
                return !Manager.TryApplyDamage(__instance, damage, 0.0f, damageSource);
            }
        }


        [HarmonyPatch(typeof(BaseAi), nameof(BaseAi.ApplyDamage), new Type[] { typeof(float), typeof(float), typeof(DamageSource), typeof(string) })]
        internal class BaseAiPatches_ApplyDamageWithBleedout
        {
            private static bool Prefix(BaseAi __instance, float damage, float bleedOutMintues, DamageSource damageSource, string collider)
            {
                return !Manager.TryApplyDamage(__instance, damage, bleedOutMintues, damageSource);
            }
        }


        [HarmonyPatch(typeof(BaseAi), nameof(BaseAi.DeserializeUsingBaseAiDataProxy), new Type[] { typeof(BaseAiDataProxy) })]
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
                    if (__instance.m_CurrentMode != AiMode.None && proxy.m_CurrentMode != AiMode.Dead)
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

        [HarmonyPatch(typeof(ConsoleManager), nameof(ConsoleManager.Initialize))]
        internal class ConsoleManagerPatches_Initialize
        {
            private static void Postfix()
            {
                uConsole.RegisterCommand(CommandString, new Action(EAFManager.Instance.Console_OnCommand));
            }
        }

        #endregion
    }
}
