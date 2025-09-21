
// Graciously stolen from UnityExplorerTlD, a well proven solution to a long running problem.
using MelonLoader;
using UnityEngine;
using Il2Cpp;


namespace ExpandedAiFramework.StolenCode 
{
    public static class InputBLocker
    {
        private static PlayerControlMode previousControlMode;
        public static bool isLocked = false;

        private static void SavePosition()
        {
            if(GameManager.GetPlayerManagerComponent() != null)
            {
                previousControlMode = GameManager.GetPlayerManagerComponent().GetControlMode();
            }            
        }

        private static void LoadPosition()
        {
            if (GameManager.GetPlayerManagerComponent() != null)
            {
                GameManager.GetPlayerManagerComponent().SetControlMode(previousControlMode);
            }
        }

        public static void ToggleLock()
        {
            LockPosition(!isLocked);
        }

        public static void LockPosition(bool locked)
        {
            if (GameManager.GetPlayerManagerComponent() != null)
            {
                if (locked)
                {
                    SavePosition();
                    GameManager.GetPlayerManagerComponent().SetControlMode(PlayerControlMode.Locked);
                    isLocked = true;
                }
                else
                {
                    isLocked = false;
                    LoadPosition();
                }
            }            
        }
    }


    [HarmonyLib.HarmonyPatch(typeof(InterfaceManager), "ShouldEnableMousePointer")]
    public class CursorPatch
    {
        public static void Postfix(InterfaceManager __instance, ref bool __result)
        {
            if (InputBLocker.isLocked)
            {
                __result = true;
            }
        }
    }
}