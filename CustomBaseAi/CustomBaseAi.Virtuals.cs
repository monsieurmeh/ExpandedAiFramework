

namespace ExpandedAiFramework
{
    public partial class CustomAiBase : ICustomAi
    {
        //Like with harmony prefix patching, return "false" on any of these to intercept and halt parent logic in favor of your own
        // Many parent methods handle important things because this mod is HIGHLY in-progress, so halt parent logic at your own peril

        #region Setup

        /// <summary>
        /// Intercept or inject logic into parent first frame setup. 
        /// Vanilla logic applies difficulty modifiers and sticks character to ground if not dead.
        /// </summary>
        /// <returns>Return false halt parent first frame logic. Return true to allow parent first frame logic to execute.</returns>
        protected virtual bool FirstFrameCustom() => true;

        #endregion


        #region Update & State Processing

        /// <summary>
        /// Intercept or inject logic into parent update loop. 
        /// Vanilla logic runs the entire process, so you will need to route your own state machine if you halt this method.
        /// </summary>
        /// <returns>Return false halt prevent parent updating. Return true to allow parent updating.</returns>
        protected virtual bool UpdateCustom() => true;


        /// <summary>
        /// Intercept changing of AI modes at the very beginning. Useful if you want to preclude certain behaviors altogether, such as
        /// a wolf that won't attack by turning any "attack", "stalk", "HoldGround", etc state into flee
        /// </summary>
        /// <param name="mode">Incoming AiMode</param>
        /// <param name="newMode">New AiMode to inject</param>
        /// <returns>Return false to skip parent preprocess checks and force set new mode. Return true to allow parent to preprocess newMode, whatever it may have changed to. </returns>
        protected virtual bool PreprocesSetAiModeCustom(AiMode mode, out AiMode newMode)
        {
            newMode = mode;
            return true;
        }


        /// <summary>
        /// Allows intercepting of preprocessing logic.
        /// Vanilla logic increments time in mode, updates wounds/bleeding and tries to trigger some preemptive behaviors like dodging and retargetting.
        /// </summary>
        /// <returns>Return false to halt parent state preprocessing. Return true to allow parent state preprocessing.</returns>
        protected virtual bool PreProcessCustom() => true;


        /// <summary>
        /// Allows intercepting of current mode processing logic. 
        /// Necessary if you are using any non-vanilla state enum values.
        /// Useful if you want to override any base behaviors like attacking or wandering which would otherwise interrupt your intent.
        /// </summary>
        /// <returns>Return false if you are engaging any custom behaviors or overriding any parent behaviors. Return true to allow parent to process current state.</returns>
        protected virtual bool ProcessCustom() => true;


        /// <summary>
        /// Allows intercepting of postprocessing logic.
        /// Vanilla logic primarily handles animation speed updating.
        /// Really not much to see here but provides an easy access point for injecting logic between the end of one state processing frame and the beginning of another.
        /// </summary>
        /// <returns>Return false to halt parent state postprocessing. Return true to allow parent state postprocessing.</returns>
        protected virtual bool PostProcessCustom() => true;


        /// <summary>
        /// Allows handling of on-enter events for custom states, as well as overriding on-enter events for base ai states.
        /// </summary>
        /// <param name="mode">State being entered</param>
        /// <returns>Return false to prevent parent on-enter event from firing. Return true to allow parent on-enter events to fire.</returns>
        protected virtual bool EnterAiModeCustom(AiMode mode) => true;


        /// <summary>
        /// Allows handling of on-exit events for custom states, as well as overriding on-exit events for base ai states.
        /// </summary>
        /// <param name="mode">State being entered</param>
        /// <returns>Return false to prevent parent on-enter event from firing. Return true to allow parent on-enter events to fire.</returns>
        protected virtual bool ExitAiModeCustom(AiMode mode) => true;


        /// <summary>
        /// Allows injection of custom target scanning logic.
        /// Vanilla logic looks for the nearest ai target or player, whichever is closer, and tries to target.
        /// If new target does not match old target, ChangeModeWhenTargetDetected is run. This can be overridden in a separate method.
        /// Only override this method if you want to adjust how new targets are acquired.
        /// </summary>
        /// <returns>Return false to prevent parent retargetting logic from firing. Return true to allow parent re-targetting logic to fire.</returns>
        
        protected virtual bool ScanForNewTargetCustom() => true;


        /// <summary>
        /// Allows injection of custom logic for handling behavior upon acquiring a new target.
        /// 
        /// </summary>
        /// <returns></returns>
        protected virtual bool ChangeModeWhenTargetDetectedCustom() => false;


        #endregion


        #region Imposter Settings

        /// <summary>
        /// Allows intercepting of parent application of imposter status to an AI. 
        /// Classic logic sets m_Imposter to true and disables character controller entirely (as far as I can tell? further testing is needed here).
        /// </summary>
        /// <returns>Return false to halt parent imposter logic. Return true to allow parent to handle imposter state change.</returns>
        protected virtual bool UpdateImposterStateCustom() => true;


        /// <summary>
        /// Allows intercepting of parent imposter status calculation.
        /// Classic logic tests a number of things like distance to camera, whether ai is on screen, etc.
        /// A never-imposter AI can be created by returning true with isImposter set to false. 
        /// </summary>
        /// <returns>Return false to halt parent imposter calculation. Return true to allow parent to handle imposter calculations.</returns>
        protected virtual bool TestIsImposterCustom(out bool isImposter)
        {
            isImposter = false;
            return true;
        }

        #endregion


        #region Animation

        /// <summary>
        /// Allows intercepting of parent AiAnimationState mapping from input AiMode.
        /// Allows for functional routing of custom ai modes to existing (or new?) AiAnimationState values.
        /// </summary>
        /// <param name="mode">Incoming mode. Usually CurrentMode but leaving the parameter open for calculation purposes</param>
        /// <param name="overrideState">Return your own state here. Required for custom states, optionally can override base game states as well.</param>
        /// <returns>Return false to override base mapping with your own overrideState. Return true to allow parent to handle animation state mapping.</returns>
        protected virtual bool GetAiAnimationStateCustom(AiMode mode, out AiAnimationState overrideState)
        {
            overrideState = AiAnimationState.Invalid;
            return true;
        }


        /// <summary>
        /// Allows intercepting of parent move state flag mapping from input AiMode, and for functional routing of custom ai modes to move state flag.
        /// Setting this to false will preclude any AI movement, be careful!
        /// </summary>
        /// <param name="mode">Incoming mode. Usually CurrentMode but leaving the parameter open for calculation purposes</param>
        /// <param name="overrideState">Return your own preference here</param>
        /// <returns>Return false to override base mapping with your own move state. Return true to allow parent to handle move state determination.</returns>
        protected virtual bool IsMoveStateCustom(AiMode mode, out bool isMoveState)
        {
            isMoveState = false;
            return true;
        }

        #endregion


        #region Damage & Wound/Bleeding

        /// <summary>
        /// Allows intercepting of parent logic for wound processing. 
        /// Vanilla logic increments BaseAi.m_ElapsedWoundedMinutes.
        /// </summary>
        /// <param name="deltaTime">frame time</param>
        /// <returns>return false to halt parent wound processing. Return true to allow parent wound processing.</returns>
        protected bool ProcessWoundsCustom(float deltaTime) => true;


        /// <summary>
        /// Allows intercepting of parent logic for bleeding out. 
        /// Vanilla logic increments BaseAi.m_ElapsedWoundedMinutes.
        /// </summary>
        /// <param name="deltaTime">frame time</param>
        /// <returns>return false to halt parent bleeding processing. Return true to allow parent bleeding processing.</returns>
        protected bool ProcessBleedingOutCustom(float deltaTime) => true;


        /// <summary>
        /// Allows intercepting of base game logic for bleed out qualification. 
        /// Vanilla logic is surprisingly obfuscated, but we all pretty much know that Moose (and i think cougar?) can't bleed out in vanilla.
        /// </summary>
        /// <param name="canBleedOut">Return your own value here</param>
        /// <returns>Return false to override parent CanBleedOut logic with your own. Return true to defer to parent calculations.</returns>
        protected virtual bool CanBleedOutCustom(out bool canBleedOut)
        {
            canBleedOut = false;
            return true;
        }

        #endregion
    }
}
