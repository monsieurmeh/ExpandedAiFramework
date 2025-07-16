using HarmonyLib;
using UnityEngine;
using static Il2Cpp.SaveGameSlots;

namespace ExpandedAiFramework
{
    internal class Patches
    {
        #region Save/Load/ModData


        [HarmonyPatch(typeof(SaveGameSlots), nameof(SaveGameSlots.CreateSlot), new Type[] { typeof(string), typeof(SaveSlotType), typeof(uint), typeof(Episode) })]
        private static class SaveGameSlotsPatches_CreateSlot
        {
            private static void Postfix()
            {
                LogTrace("OnStartNewGame");
                Manager.OnStartNewGame();
            }
        }



        [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadSaveGameSlot), new Type[] { typeof(string), typeof(int) })]
        private static class GameManagerPatches_LoadSaveGameSlot
        {
            private static void Postfix()
            {
                LogTrace("OnLoadGame");
                Manager.OnLoadGame();
            }
        }


        //[HarmonyPatch(typeof(SaveGameSlots), nameof(SaveGameSlots.WriteSlotToDisk), new Type[] { typeof(string) })]
        [HarmonyPatch(typeof(SaveGameSlots), nameof(SaveGameSlots.WriteSlotToDisk), new Type[] { typeof(SlotData), typeof(Timestamp) })]
        private static class SaveGameSlotsPatches_WriteSlotToDisk
        {
            private static void Prefix()
            {
                LogTrace("OnSaveGame");
                Manager.OnSaveGame();
            }
        }


        [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadSceneWithLoadingScreen), new Type[] { typeof(string) })]
        private static class GameManagerPatches_LoadSceneWithLoadingScreen
        {
            private static void Prefix(string sceneName)
            {
                Manager.OnLoadScene(sceneName);
            }
        }

        [HarmonyPatch(typeof(GameManager), nameof(GameManager.DoExitToMainMenu))]
        [HarmonyPatch(typeof(GameManager), nameof(GameManager.LoadMainMenu))]
        private static class GameManagerPatches_ToMainMenu
        {
            private static void Prefix()
            {
                Manager.OnLoadScene("MainMenu");
            }
        }

        #endregion


        #region ExtendingLoadScreenTest


        [HarmonyPatch(typeof(Panel_Loading), nameof(Panel_Loading.Enable), new Type[] { typeof(bool) })]
        private static class Panel_LoadingPatches_Enable
        {
            private static int RefuseCount = 0;
            private static readonly int RefuseLimit = 5;
            private static bool Prefix(ref bool enable)
            {
                if (!enable && EAFManager.Instance.SpawnRegionManager.PreLoading && RefuseCount <= RefuseLimit)
                {
                    LogVerbose($"Preventing load screen from dropping until preloading is complete! Refusals left: {RefuseLimit - (RefuseCount++)}");
                    return false;
                }
                RefuseCount = 0;
                return true;
            }
        }

        #endregion


        #region BaseAi

        [HarmonyPatch(typeof(BaseAi), nameof(BaseAi.Start))]
        internal class BaseAiPatches_Start
        {
            private static bool Prefix(BaseAi __instance)
            {
                LogVerbose($"Start on {__instance.gameObject.name} with ai subtype {__instance.m_AiSubType} at {__instance.transform.position}!");
                return !Manager.AiManager.TryStart(__instance);
                //return __instance.m_AiSubType != AiSubType.Wolf || __instance.Timberwolf;
            }
        }


        [HarmonyPatch(typeof(BaseAi), nameof(BaseAi.Update))]
        internal class BaseAiPatches_Update
        {
            private static bool Prefix()
            {
                return false;
            }
        }


        [HarmonyPatch(typeof(BaseAi), nameof(BaseAi.SetAiMode), new Type[] { typeof(AiMode) })]
        internal class BaseAiPatches_SetAiMode
        {
            private static bool Prefix(BaseAi __instance, AiMode mode)
            {
                return !Manager.AiManager.TrySetAiMode(__instance, mode);
            }
        }


        [HarmonyPatch(typeof(BaseAi), nameof(BaseAi.ApplyDamage), new Type[] { typeof(float), typeof(DamageSource), typeof(string) })]
        internal class BaseAiPatches_ApplyDamage
        {
            private static bool Prefix(BaseAi __instance, float damage, DamageSource damageSource, string collider)
            {
                return !Manager.AiManager.TryApplyDamage(__instance, damage, 0.0f, damageSource);
            }
        }


        [HarmonyPatch(typeof(BaseAi), nameof(BaseAi.ApplyDamage), new Type[] { typeof(float), typeof(float), typeof(DamageSource), typeof(string) })]
        internal class BaseAiPatches_ApplyDamageWithBleedout
        {
            private static bool Prefix(BaseAi __instance, float damage, float bleedOutMintues, DamageSource damageSource, string collider)
            {
                return !Manager.AiManager.TryApplyDamage(__instance, damage, bleedOutMintues, damageSource);
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


        #region CarcassSite

        [HarmonyPatch(typeof(CarcassSite.Manager), nameof(CarcassSite.Manager.TryInstanciateCarcassSite), new Type[] { typeof(GameObject), typeof(Vector3), typeof(GameObject) })]
        internal class CarcassSitePatches_TryInstanciateCarcassSite
        {
            private static void Postfix(GameObject carcassSitePrefab, Vector3 position, GameObject originCorpse)
            {
                LogVerbose($"[CarcassSitePatches_TryInstanciateCarcassSite.Postfix]: CarcassSite.Manager.TryInstanciateCarcassSite on {carcassSitePrefab.name} at {position}");
                BaseAi baseAi = null;
                bool carcassAiFound = carcassSitePrefab != null && carcassSitePrefab.TryGetComponent(out baseAi);
                carcassAiFound = carcassAiFound || (originCorpse != null && originCorpse.TryGetComponent(out baseAi));
                if (!carcassAiFound)
                {
                    LogVerbose($"[CarcassSitePatches_TryInstanciateCarcassSite.Postfix]: No base ai script found on carcass prefab or origin corpse, aborting...");
                    return;
                }
                if (baseAi == null)
                {
                    LogError("[CarcassSitePatches_TryInstanciateCarcassSite.Postfix]: How was baseAi null if we passed TryGetComponent checks?");
                    return;
                }
                if (!Manager.AiManager.TryInterceptCarcassSpawn(baseAi))
                {
                    LogError("[CarcassSitePatches_TryInstanciateCarcassSite.Postfix]: Carcass intercept error!");
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
