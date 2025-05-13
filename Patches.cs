using HarmonyLib;
using UnityEngine;


namespace ExpandedAiFramework
{
    public partial class Patches
    {

        #region General

        [HarmonyPatch(typeof(SpawnRegion), "InstantiateSpawnInternal", new Type[] { typeof(GameObject), typeof(WildlifeMode), typeof(Vector3), typeof(Quaternion) })]
        public class SpawnRegionPatches_InstantiateSpawnInternal
        {
            public static void Postfix(BaseAi __result)
            {
                Utility.Manager.TryAugment(__result);
            }
        }


        [HarmonyPatch(typeof(SaveGameSystem), "LoadSceneData", new Type[] { typeof(string), typeof(string) })]
        public class SaveGameSystemPatches_LoadSceneData
        {
            public static void Postfix(string name, string sceneSaveName)
            {
                Utility.Manager?.ClearAugments();
                Utility.Manager?.RefreshAvailableMapData(sceneSaveName);
            }
        }

        #endregion


        #region BaseAi

        [HarmonyPatch(typeof(BaseAi), "Update")]
        public class BaseAiPatches_Update
        {
            public static bool Prefix(BaseAi __instance)
            {
                return __instance.m_AiSubType != AiSubType.Wolf || __instance.Timberwolf;
            }
        }


        [HarmonyPatch(typeof(BaseAi), "SetAiMode", new Type[] { typeof(AiMode) })]
        public class BaseAiPatches_SetAiMode
        {
            public static bool Prefix(BaseAi __instance, AiMode mode)
            {
                return !Utility.Manager.TrySetAiMode(__instance, mode);
            }
        }


        [HarmonyPatch(typeof(BaseAi), "ApplyDamage", new Type[] { typeof(float), typeof(DamageSource), typeof(string) })]
        public class BaseAiPatches_ApplyDamage
        {
            public static bool Prefix(BaseAi __instance, float damage, DamageSource damageSource, string collider)
            {
                return !Utility.Manager.TryApplyDamage(__instance, damage, 0.0f, damageSource);
            }
        }


        [HarmonyPatch(typeof(BaseAi), "ApplyDamage", new Type[] { typeof(float), typeof(float), typeof(DamageSource), typeof(string) })]
        public class BaseAiPatches_ApplyDamageWithBleedout
        {
            public static bool Prefix(BaseAi __instance, float damage, float bleedOutMintues, DamageSource damageSource, string collider)
            {
                return !Utility.Manager.TryApplyDamage(__instance, damage, bleedOutMintues, damageSource);
            }
        }


        [HarmonyPatch(typeof(BaseAi), "DeserializeUsingBaseAiDataProxy", new Type[] { typeof(BaseAiDataProxy) })]
        public class BaseAiPatches_DeserializeUsingBaseAiDataProxy
        {
            public static void Prefix(BaseAi __instance, BaseAiDataProxy proxy)
            {
                if (Utility.Manager.AiAugments.ContainsKey(__instance?.GetHashCode() ?? 0))
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

#if DEV_BUILD

        [HarmonyPatch(typeof(ConsoleManager), "Initialize")]
        public class ConsoleManagerPatches_Initialize
        {
            public const string CommandString_Help =                    "EAF_help";
            public const string CommandString_FollowWanderingWolf =     "EAF_followWanderer";
            public const string CommandString_SaveMapData =             "EAF_save";
            public const string CommandString_LoadMapData =             "EAF_load";
            public const string CommandString_CreateHidingSpot =        "EAF_createHidingSpot";
            public const string CommandString_CreateWanderPath =        "EAF_createWanderPath";
            public const string CommandString_AddToWanderPath =         "EAF_addToWanderPath";
            public const string CommandString_FinishCurrentWanderPath = "EAF_finishCurrentWanderPath";
            public const string CommandString_DeleteWanderPath =        "EAF_deleteWanderPath";
            public const string CommandString_ShowWanderPaths =         "EAF_showWanderPaths";
            public const string CommandString_HideWanderPaths =         "EAF_hideWanderPaths";
            public const string CommandString_ShowHidingSpots =         "EAF_showHidingSpots";
            public const string CommandString_HideHidingSpots =         "EAF_hideHidingSpots";

            public static void Postfix()
            {
                uConsole.RegisterCommand(CommandString_SaveMapData,             new Action(Utility.Manager.SaveMapData));
                uConsole.RegisterCommand(CommandString_LoadMapData,             new Action(Utility.Manager.LoadMapData));
                uConsole.RegisterCommand(CommandString_CreateHidingSpot,        new Action(Utility.Manager.CreateHidingSpot));
                uConsole.RegisterCommand(CommandString_CreateWanderPath,        new Action(Utility.Manager.StartWanderPath));
                uConsole.RegisterCommand(CommandString_AddToWanderPath,         new Action(Utility.Manager.AddWanderPathPos));
                uConsole.RegisterCommand(CommandString_FinishCurrentWanderPath, new Action(Utility.Manager.CompleteWanderingPath));
                uConsole.RegisterCommand(CommandString_DeleteWanderPath,        new Action(Utility.Manager.DeleteWanderingPath));
                uConsole.RegisterCommand(CommandString_ShowWanderPaths,         new Action(Utility.Manager.ShowWanderPaths));
                uConsole.RegisterCommand(CommandString_HideWanderPaths,         new Action(Utility.Manager.HideWanderPaths));
                uConsole.RegisterCommand(CommandString_ShowHidingSpots,         new Action(Utility.Manager.ShowHidingSpots));
                uConsole.RegisterCommand(CommandString_HideHidingSpots,         new Action(Utility.Manager.HideHidingSpots));
            }
        }
#endif

        #endregion
    }
}
