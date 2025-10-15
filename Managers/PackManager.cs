global using VanillaPackManager = Il2Cpp.PackManager;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Il2CppAK.Wwise;
using Il2CppTLD.AI;
using UnityEngine;

namespace ExpandedAiFramework
{
    public class PackManager : BaseSubManager, IPackManager
    {
        private const LogCategoryFlags UpdateFlags = LogCategoryFlags.PackManager | LogCategoryFlags.Update;
        private const long TickRate = MillisecondsToTicks * 50;
        private bool mStartCalled = false;
        private long mStartTick = 0;
        private long mDeserializeDelayTick = 0;
        private long mNextTick = 0;
        private PackSettings mPackSettings;
        private VanillaPackManager mVanillaManager;

        public PackManager(EAFManager manager) : base(manager) 
        { 
            PackManagerSettings settings = new PackManagerSettings(Path.Combine(DataFolderPath, $"{nameof(PackManager)}"));
            settings.AddToModSettings(ModName);
            settings.Reload();
        }

        public VanillaPackManager VanillaPackManager
        { 
            get 
            {
                if (mVanillaManager == null)
                {
                    mVanillaManager = GameManager.m_PackManager;
                }
                return mVanillaManager;
            }
        }
        
        public override void OnQuitToMainMenu()
        {
            base.OnQuitToMainMenu();
            mStartCalled = false;
        }

        public override void OnLoadScene(string sceneName)
        {
            base.OnLoadScene(sceneName);
        }

        public override void OnInitializedScene(string sceneName)
        {
            base.OnInitializedScene(sceneName);
        }

        public void OverrideStart()
        {
            if (mStartCalled) 
            {
                LogDebug($"Start already called, aborting", LogCategoryFlags.PackManager);
                return;
            }
            LogDebug($"OverrideStart", LogCategoryFlags.PackManager);
            Il2CppTLD.UI.PanelReference panelReference = Il2CppTLD.UI.PanelReference.Get<Panel_HUD>();
            if (panelReference.TryGetPanel<Panel_HUD>(out Panel_HUD panel))
            {
                panel.QuietlyResetTimberWolfCombatMusic();
            }
            VanillaPackManager.m_HoursPlayedAtStart = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
            VanillaPackManager.m_SystemEnabled = ArePacksAllowed();
            mPackSettings = GetPackSettings();
            mStartTick = DateTime.Now.Ticks;
            mDeserializeDelayTick = DateTime.Now.Ticks + (long)(VanillaPackManager.m_DeserializeDelayTimer * MillisecondsToTicks);
        }


        public override void UpdateFromManager() 
        {
            if (!UpdateCustom()) return; // give priority to a custom implementation if ever exists
            if (mNextTick > DateTime.Now.Ticks) return;
            mNextTick = DateTime.Now.Ticks + TickRate;
            if (!ShouldUpdate()) return;
            MaybeEnableAnimalsOnLoad();
			ResetGroupEventFlags();
			MaybeCleanupDeadPackAnimals();
			MaybeDisbandGroupOnTargetLost();
			MaybeFleeAndDisbandOnMoraleCheck();
			VanillaPackManager.MaybeFleeAndDisbandOnBearOrMooseCheck(mPackSettings);
			VanillaPackManager.MaybeForceMoveMembers(mPackSettings);
			VanillaPackManager.MaybeMoveToNewHoldGroundPosition(mPackSettings);
			VanillaPackManager.MaybeForceAttackInCombatRestrictedArea(mPackSettings);
			VanillaPackManager.MaybeKeepLonersWithinRadius();
			VanillaPackManager.MaybeFormGroupOnPlayerDetectionRange(mPackSettings);
			VanillaPackManager.MaybeUpdateInteriorAudio(mPackSettings);
        }


        private bool ShouldUpdate()
        {
            if (VanillaPackManager == null) 
            {
                LogTrace($"VanillaPackManager is null", UpdateFlags);
                return false;
            }
            if (!VanillaPackManager.m_SystemEnabled) 
            {
                LogTrace($"VanillaPackManager is not enabled", UpdateFlags);
                return false;
            }
            if (GameManager.m_IsPaused) 
            {
                LogTrace($"Game is paused", UpdateFlags);
                return false;
            }
            if (GameManager.s_IsGameplaySuspended) 
            {
                LogTrace($"Game is paused", UpdateFlags);
                return false;
            }
            if (GameManager.ControlsLocked()) 
            {
                LogTrace($"Controls are locked", UpdateFlags);
                return false;
            }
            return true;
        }

        private void MaybeEnableAnimalsOnLoad() 
        {
            if (!VanillaPackManager.m_EnablePacksOnLoad) return;
            foreach (PackGroup pack in VanillaPackManager.m_PackAnimalGroupByLeader.Values)
            {
                foreach (PackAnimal animal in pack.m_Members)
                {
                    EnablePackAnimal(animal);
                }
            }
            VanillaPackManager.m_EnablePacksOnLoad = false;
        }

        private void EnablePackAnimal(PackAnimal animal)
        {
            if (animal.isActiveAndEnabled)
            {
                LogTrace($"Pack animal is already active", UpdateFlags);
                return;
            }
            if (animal.transform.parent == null) 
            {
                LogTrace($"Pack animal has no parent", UpdateFlags);
                return;
            }
            animal.gameObject.SetActive(value: true);
            if (animal.transform.parent.gameObject.activeSelf) return; 
            animal.transform.parent.gameObject.SetActive(value: true);
        }

        private void ResetGroupEventFlags()
        {
            foreach (PackGroup pack in VanillaPackManager.m_PackAnimalGroupByLeader.Values)
            {
                pack.m_GroupEventProcessed = false;
            }
        }

        private void MaybeCleanupDeadPackAnimals()
        {
            foreach (PackAnimal animal in EnumeratePackAnimals())
            {
                if (IsPackAnimalDead(animal)) continue;
                VanillaPackManager.UnregisterPackAnimal(animal, true);
            }
        }


        private IEnumerable<PackAnimal> EnumeratePackAnimals()
        {
            foreach (PackGroup packGroup in EnumeratePackGroups())
            {
                foreach (PackAnimal animal in packGroup.m_Members)
                {
                    yield return animal;
                }
            }
        }

        private IEnumerable<PackGroup> EnumeratePackGroups()
        {
            foreach (PackGroup packGroup in VanillaPackManager.m_PackAnimalGroupByLeader.Values)
            {
                yield return packGroup;
            }
        }

        private IEnumerable<PackAnimal> EnumeratePackLeaders()
        {
            foreach (PackAnimal key in VanillaPackManager.m_PackAnimalGroupByLeader.Keys)
            {
                yield return key;
            }
        }

        private bool TryGetPackGroupByLeader(PackAnimal leader, out PackGroup group) 
        {
            return VanillaPackManager.m_PackAnimalGroupByLeader.TryGetValue(leader, out group);
        }


        private bool IsPackAnimalDead(PackAnimal animal) 
        {
            if (animal.m_BaseAi.GetAiMode() == AiMode.Dead) 
            {
                LogTrace($"Pack animal is dead", UpdateFlags);
                return true;
            }
            if (animal.m_BaseAi.IsBleedingOut() && animal.m_BaseAi.GetBleedingOutMinutesRemaining() < mPackSettings.m_MinBleedOutTimeMinutes) 
            {
                LogTrace($"Pack animal is bleeding out", UpdateFlags);
                return true;
            }
            if (!animal.isActiveAndEnabled && animal.m_BaseAi.HasUpdated()) 
            {
                LogTrace($"Pack animal is not active and enabled and has updated", UpdateFlags);
                return true;
            }
            return false;
        }


        private void MaybeDisbandGroupOnTargetLost()
        {
            if (mDeserializeDelayTick > DateTime.Now.Ticks) 
            {
                LogTrace($"Deserialize delay tick is greater than current time tick, skipping", UpdateFlags);
                return;
            }
            float hoursPlayedNotPaused = GameManager.GetTimeOfDayComponent().GetHoursPlayedNotPaused();
            foreach (PackAnimal leader in EnumeratePackLeaders())
            {
                PackGroup pack = VanillaPackManager.m_PackAnimalGroupByLeader[leader];
                foreach (PackAnimal animal in pack.m_Members)
                {
                    if (animal.m_BaseAi.m_CurrentTarget == null) continue;
                    pack.m_TargetAwarenessTime = hoursPlayedNotPaused;
                    break;
                }
                float dispandTimper = IsTaggedGroupAnimal(leader) 
                    ? mPackSettings.m_DisbandTaggedGroupOnForgetTargetAfterTimeHours 
                    : mPackSettings.m_DisbandGroupOnForgetTargetAfterTimeHours;
                if (hoursPlayedNotPaused - pack.m_TargetAwarenessTime <= dispandTimper) 
                {
                    LogTrace($"Hours played not paused minus target awareness time is less than dispand timer, skipping", UpdateFlags);
                    continue;
                }
                RemoveGroup(leader, setReformTimer: true);
            }
        }


        private void MaybeFleeAndDisbandOnMoraleCheck()
        {
            foreach (PackAnimal leader in EnumeratePackLeaders())
            {
                PackGroup pack = VanillaPackManager.m_PackAnimalGroupByLeader[leader];
                foreach (PackAnimal animal in pack.m_Members)
                {
                    if (!ShouldAnimalFlee(animal, mPackSettings)) continue;
                    LogTrace($"ShouldAnimalFlee returned true for animal with hash {animal.m_BaseAi.GetHashCode()}", UpdateFlags);
                    foreach (PackAnimal member in pack.m_Members)
                    {
                        member.m_BaseAi.FleeFrom(GameManager.GetPlayerTransform().position); // TODO: Handle with EAF
                        member.m_BaseAi.SetFleeReason(AiFleeReason.PackDisband); // TODO: Handle with EAF
                        MaybeSetAuroraMaterialsOnFlee(member.m_BaseAi); // TODO: Handle with EAF
                    }
                    GameManager.GetAchievementManagerComponent().DefeatTimberwolfPack();
                    RemoveGroup(leader, true);
                    break;
                }
            }
        }


        private bool ShouldAnimalFlee(PackAnimal animal, PackSettings settings)
        {
            if (animal == null) 
            {
                LogTrace($"Null PackAnimal", UpdateFlags);
                return false;
            }
            if (!animal.m_GroupLeader) 
            {
                LogTrace($"Not group leader", UpdateFlags);
                return false;
            }
            if (IsMismatchWildlifeMode(animal)) 
            {
                LogTrace($"Mismatch wildlife mode", UpdateFlags);
                return true;
            }
            if (GameManager.GetPlayerManagerComponent().m_ForceAIFlee)
            {
                LogTrace($"Force AIFlee", UpdateFlags);
                return true;
            }
            float fleeMoraleThreshold = settings.m_FleeMoraleThreshold / 100f; // Great place to adjust flee threshold for combat!
            float groupMoraleHeuristic = GroupMoraleHeuristic(animal);
            if (groupMoraleHeuristic < fleeMoraleThreshold) 
            {
                LogTrace($"Group morale heuristic less than flee morale threshold", UpdateFlags);
                return true;
            }
            /* "???bruh???" is right... this part makes zero sense in context. I've never seen a pack of wolves disband for any reason other than "too few members", so this feels... weird...
            if (!IsTaggedGroupAnimal(animal)) 
            {
                LogTrace($"Not tagged group animal", UpdateFlags);
                return false;
            }
            if (VanillaPackManager.m_PackAnimalGroupByLeader.Count == 0) 
            {
                LogTrace($"No pack animal group by leader", UpdateFlags);
                return false;
            }
            PackAnimal groupLeader = animal.m_GroupLeader;
            int packSize = VanillaPackManager.m_PackAnimalGroupByLeader[groupLeader].m_Members.Count;
            bool flag = true;
            foreach (PackAnimal leader in EnumeratePackLeaders())
            {
                if (groupLeader == leader)
                {
                    LogTrace($"Group leader is the same as the leader, setting flag false", UpdateFlags);
                    flag = false;
                    continue;
                }
                float otherLeaderHeuristic = GroupMoraleHeuristic(leader);
                if (otherLeaderHeuristic > groupMoraleHeuristic) 
                {
                    LogTrace($"Other leader heuristic greater than group morale heuristic", UpdateFlags);
                    return true;
                }
                if (!(otherLeaderHeuristic < groupMoraleHeuristic))
                {
                    PackGroup otherPack = VanillaPackManager.m_PackAnimalGroupByLeader[leader];
                    if (otherPack.m_Members.Count > packSize) 
                    {
                        LogTrace($"Other pack members count greater than pack size", UpdateFlags);
                        return true;
                    }
                    if (otherPack.m_Members.Count >= packSize && flag) 
                    {
                        LogTrace($"Other pack members count greater than or equal to pack size and flag is true", UpdateFlags);
                        return true;
                    }
                }
            }
            LogTrace($"???bruh???", UpdateFlags);
            */
            return false;
        }


        private float GroupMoraleHeuristic(PackAnimal animal)
        {
            if (animal == null)
            {
                LogTrace($"Null PackAnimal", UpdateFlags);
                return 1f;
            }
            if (!TryGetPackGroupByLeader(animal.m_GroupLeader, out PackGroup group))
            {
                LogTrace($"Failed to get pack group by leader", UpdateFlags);
                return 1f;
            }
            if (group.m_Members.Count == 0)
            {
                LogTrace($"Pack group members count is 0", UpdateFlags);
                return 1f;
            }
            return Mathf.Clamp01((group.m_Members.Count - 1) / (group.m_FormationCount - 1));            
        }

        #region eventual uproots
        
        private bool IsMismatchWildlifeMode(PackAnimal animal) => VanillaPackManager.IsMismatchWildlifeMode(animal);
        private bool IsTaggedGroupAnimal(PackAnimal animal) => VanillaPackManager.IsTaggedGroupAnimal(animal);
        private void MaybeSetAuroraMaterialsOnFlee(BaseAi baseAi) => VanillaPackManager.MaybeSetAuroraMaterialsOnFlee(baseAi);
        private void RemoveGroup(PackAnimal leader, bool setReformTimer) => VanillaPackManager.RemoveGroup(leader, setReformTimer);
        private bool ArePacksAllowed() => VanillaPackManager.ArePacksAllowed();
        private PackSettings GetPackSettings() => VanillaPackManager.GetPackSettings();

        #endregion



        #region overrides
        protected virtual bool UpdateCustom() => true;

        #endregion
    }
}