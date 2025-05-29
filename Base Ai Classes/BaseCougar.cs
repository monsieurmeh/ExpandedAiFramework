

using HarmonyLib;
using Il2Cpp;
using Il2CppRewired;
using Il2CppRewired.Utils;
using Il2CppRewired.Utils.Classes.Data;
using Il2CppTLD.AddressableAssets;
using Il2CppTLD.AI;
using UnityEngine;
using static Il2Cpp.UITweener;
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;
using static UnityEngine.GraphicsBuffer;

namespace ExpandedAiFramework
{
    [RegisterTypeInIl2Cpp]
    public class BaseCougar : CustomBaseAi
    {
        public static BaseCougarSettings BaseCougarSettings = new BaseCougarSettings();

        protected AiCougar mCougar;

        public BaseCougar(IntPtr ptr) : base(ptr) { }


        protected override bool OverrideStartCustom()
        {
            bool baseResult = base.OverrideStartCustom();
            if (!mBaseAi.gameObject.TryGetComponent<AiCougar>(out mCougar))
            {
                LogError($"Could not fetch AiCougar from cougar!");
            }
            return baseResult;
        }


        protected override bool EnterAiModeCustom(AiMode mode)
        {
            switch (mode)
            {
                case AiMode.PassingAttack: return EnterPassingAttackCustomBase();
            }
            return base.ProcessCustom();
        }


        private bool EnterPassingAttackCustomBase()
        {
            if (!EnterPassingAttackCustom())
            {
                return false;
            }
            if (CurrentTarget.IsNullOrDestroyed())
            {
                mBaseAi.m_SuppressFleeAudio = true;
                mBaseAi.FleeFrom(GameManager.GetPlayerTransform());
                return false;
            }
            DamageEventTable damageEventTable = Il2Cpp.BaseAi.GetDamageEventsForTag(mCougar.m_PassingAttackDamageEventTableTag);
            if (damageEventTable.IsNullOrDestroyed())
            {
                mCougar.DoFlee();
                return false;
            }
            if (damageEventTable.m_DamageEvents == null)
            {
                damageEventTable.Initialize();
            }
            mCougar.m_HasPassingAttackDamageFired = false;
            if (CurrentTarget.IsPlayer())
            {
                int attackSide = Vector3.Dot((CurrentTarget.transform.position - mBaseAi.m_CachedTransform.position).normalized, GameManager.m_MainCamera.transform.right) > 0 ? 1 : 0;
                mBaseAi.AnimSetInt(mBaseAi.m_AnimParameter_PassingAttackSide, attackSide);
                GameManager.m_SafehouseManager.TryStopCustomizing(); //odd timing for a call like this, but i guess bugs be buggin o_O
                MaybePlayerDropsWeapon();
                if (mCougar.m_SwipeSideTimelineAssets == null || attackSide >= mCougar.m_SwipeSideTimelineAssets.Count)
                {
                    LogError($"attackSide index out of range");
                    return false;
                }
                mCougar.PlayTimelineAnimation(mCougar.m_SwipeSideTimelineAssets[attackSide], new System.Action(() => { /* maybe cleanup here?? All I see in decompile is a type initializer for an action... */ }));
                mBaseAi.AnimSetTrigger(mCougar.m_AnimParameter_Trigger_PassingAttack);
                mCougar.m_TriggeredPassingAttackAnim = true;
                mCougar.m_PassingAttackAnimTimer = mCougar.m_PassingAttackAnimTimeout;
            }
            else if (CurrentTarget.IsNpcSurvivor())
            {
                mBaseAi.AnimSetTrigger(mCougar.m_AnimParameter_Trigger_PassingAttackNpc);
                mCougar.m_TriggeredPassingAttackAnim = true;
                mCougar.m_PassingAttackAnimTimer = mCougar.m_PassingAttackAnimTimeout;
            }
            else
            {
                mCougar.m_TriggeredPassingAttackAnim = false;
            }
            if (GameManager.m_PassTime.m_Timer.m_IsRunning)
            {
                GameManager.m_PassTime.End();
            }
            if (GameManager.m_Rest.m_Sleeping)
            {
                GameManager.m_Rest.EndSleeping(true);
            }
            return false;
        }


        private void MaybePlayerDropsWeapon()
        {
            if (!MaybePlayerDropsWeaponCustom())
            {
                return;
            }
            PlayerManager playerManager = GameManager.m_PlayerManager;
            if (playerManager.IsNullOrDestroyed())
            {
                LogError("Null playermanager in BaseCougar.MaybePlayerDropsWeapon!");
                return;
            }
            if (playerManager.m_ItemInHandsInternal.IsNullOrDestroyed())
            {
                LogTrace($"Item in hands null or destroyed, not dropping...");
                return;
            }
            GearItem itemInHands = playerManager.m_ItemInHandsInternal;
            if (!itemInHands.IsWeapon())
            {
                LogTrace($"Item in hands is not weapon, not dropping...");
                return;
            }
            if (!itemInHands.m_GunItem.IsNullOrDestroyed() && itemInHands.m_GunItem.m_GunType == GunType.Camera)
            {
                LogTrace($"doing some weird force immediate drop for cameras");
                playerManager.UnequipImmediate(false);
                return;
            }
            GameManager.m_vpFPSCamera.MaybeResetCurrentWeapon();
            if (!itemInHands.m_BowItem.IsNullOrDestroyed())
            {
                LogTrace($"Force unequiping bow item");
                itemInHands.m_BowItem.OnDequip();
            }
            itemInHands.Drop(1, true, true, true);
            PlayerAnimation playerAnim = GameManager.m_NewPlayerAnimation;
            playerAnim.HideHands();
            playerManager.UnequipItemInHandsSkipAnimation();
            playerAnim.Reset();            
        }


        protected override bool ProcessCustom()
        {
            switch (CurrentMode)
            {
                case AiMode.PassingAttack: return ProcessPassingAttackCustomBase();
            }
            return base.ProcessCustom();
        }


        private bool ProcessPassingAttackCustomBase()
        {
            if (!ProcessPassingAttackCustom())
            {
                return false;
            }
            if (mCougar.m_TriggeredPassingAttackAnim != false)
            {
                if (mCougar.m_HasPassingAttackDamageFired != false)
                {
                    return false;
                }
                mCougar.m_PassingAttackAnimTimer -= Time.deltaTime;
                if (0.0 < mCougar.m_PassingAttackAnimTimer)
                {
                    return false;
                }
            }
            DoPassingAttackDamage();
            return false;
        }





        private void DoPassingAttackDamage()
        {
            if (!DoPassingAttackDamageCustom())
            {
                return;
            }
            mBaseAi.PlayMeleeAttackAudio();
            if (CurrentTarget != null)
            {
                if (CurrentTarget.IsPlayer())
                { 
                    CurrentTarget.ApplyDamage(UnityEngine.Random.Range(mCougar.m_PlayerPassingAttackDamageMin, mCougar.m_PlayerPassingAttackDamageMax), DamageSource.Cougar, "PassingAttackOnPlayer");
                    GameManager.m_NewPlayerAnimation.SetTrigger(GameManager.m_NewPlayerAnimation.m_AnimParameter_Trigger_WolfPassBite);
                }
                else
                {
                    if (CurrentTarget.IsNpcSurvivor())
                    {
                        CurrentTarget.ApplyDamage(mBaseAi.m_MeleeAttackDamage, DamageSource.Cougar, "PassingAttackAiOnNPC");
                    }
                    else
                    {

                        CurrentTarget.ApplyDamage(mCougar.m_AiPassingAttackDamage, DamageSource.Cougar, "PassingAiAttack");
                    }              
                }
            }
            mCougar.m_HasPassingAttackDamageFired = true;
            mBaseAi.m_SuppressFleeAudio = true;
            mBaseAi.FleeFrom(GameManager.GetPlayerTransform());
            return;
        }

        #region Kitty's Sub-Virtuals

        protected virtual bool ProcessPassingAttackCustom() => true;
        protected virtual bool DoPassingAttackDamageCustom() => true;
        protected virtual bool EnterPassingAttackCustom() => true;
        protected virtual bool MaybePlayerDropsWeaponCustom() => true;

        #endregion
    }
}