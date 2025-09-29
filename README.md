****CREDITS****

Warm appreciation to Marcy for icons. They look awesome, and one day I'll have a complete set with moose and bear! :D
Big thanks to the TLD Modding community in general. This game has a lot of moving parts and it would take years longer than it has to make this without their input.

****WARNING****

Please back up your saves RIGHT NOW. Don't touch this mod until you have backed up your saves. It is in alpha and I cannot guarantee safety of any saves it is used on.

Expanded Ai Framework (EAF): Framework mod intended to provide more options for modding ai in The Long Dark. 

This readme works best split into two sections: the first for PLAYERS and the second for MODDERS.

****PLAYERS****:

By itself, this framework provides some small additional in-game features:
1) Fixes known wildlife bugs that I have had time to fix
2) Generic behavior options/fixes such as wolf stalking timeout to prevent infinite exploitable stalking behavior
3) Support for other mods that change wildlife behavior


****MODDERS****:

This mod works by wrapping instances of the game's AI class with its own construct (ICustomAi/CustomAiBase) if the spawn type matches. 

These wrappers use the existing ai script as both a blackboard to store data and a tool to run the ai itself. 

In time as this framework expands, you can expect more and more "uprooting" of existing behaviors like processing methods, on-state-entry/exit methods, targetting, etc.

Where needed, I have exposed the base AI behavior used by EAF with virtual boolean methods that act very similarly to Harmony prefixes in that returning false will halt base behavior execution in its tracks They follow a simple format: 
```
Virtual bool SomeMethodCustom(arguments)
{
    // do your thing here
    return shouldAllowBaseAiBehavior; //Return false to stop base method after your call; return true to allow it to continue
}
```
Eventually I may add a post-fix variant. Convince me it's needed?

See the full list of overrides currently available, see the "Virtuals" section of CustomBaseAi.cs in the top level of the solution. I may move it to a separate file if it makes things easier for others, or just keep a wiki up to date in the future. Expect this to disappear if a wiki appears.
```

public virtual bool OverrideStartCustom() => true;


public virtual bool ShouldAddToBaseAiManager() => true;


protected virtual bool FirstFrameCustom() => true;


protected virtual bool MaybeHoldGroundCustom(out bool shouldHoldGround)
{
    shouldHoldGround = false;
    return true;
}


protected virtual bool MaybeHoldGroundForTorchCustom(float radius, out bool shouldHoldGround)
{
    shouldHoldGround = false;
    return true;
}


protected virtual bool MaybeHoldGroundForTorchOnGroundCustom(float radius, out bool shouldHoldGround)
{
    shouldHoldGround = false;
    return true;
}


protected virtual bool MaybeHoldGroundForFireCustom(float radius, out bool shouldHoldGround)
{
    shouldHoldGround = false;
    return true;
}


protected virtual bool MaybeHoldGroundForRedFlareCustom(float radius, out bool shouldHoldGround)
{
    shouldHoldGround = false;
    return true;
}


protected virtual bool MaybeHoldGroundForRedFlareOnGroundCustom(float radius, out bool shouldHoldGround)
{
    shouldHoldGround = false;
    return true;
}


protected virtual bool MaybeHoldGroundForBlueFlareCustom(float radius, out bool shouldHoldGround)
{
    shouldHoldGround = false;
    return true;
}


protected virtual bool MaybeHoldGroundForBlueFlareOnGroundCustom(float radius, out bool shouldHoldGround)
{
    shouldHoldGround = false;
    return true;
}


protected virtual bool MaybeHoldGroundForSpearCustom(float radius, out bool shouldHoldGround)
{
    shouldHoldGround = false;
    return true;
}


protected virtual bool MaybeHoldGroundAuroraFieldCustom(out bool shouldHoldGround)
{
    shouldHoldGround = false;
    return true;
}


protected virtual bool MaybeHoldGroundDueToSafeHavenCustom(out bool shouldHoldGround)
{
    shouldHoldGround = false;
    return true;
}


protected virtual bool MaybeHoldGroundDueToStruggleCustom(out bool shouldHoldGround)
{
    shouldHoldGround = false;
    return true;
}

protected virtual bool UpdateCustom() => true;


protected virtual bool PreprocesSetAiModeCustom(AiMode mode, out AiMode newMode)
{
    newMode = mode;
    return true;
}


protected virtual bool PreProcessCustom() => true;


protected virtual bool ProcessCustom() => true;


protected virtual bool PostProcessCustom() => true;


protected virtual bool EnterAiModeCustom(AiMode mode) => true;


protected virtual bool ExitAiModeCustom(AiMode mode) => true;


protected virtual bool ScanForNewTargetCustom() => true;


protected virtual bool ChangeModeWhenTargetDetectedCustom() => true;


protected virtual bool UpdateImposterStateCustom() => true;


protected virtual bool TestIsImposterCustom(out bool isImposter)
{
    isImposter = false;
    return true;
}


protected virtual bool GetAiAnimationStateCustom(AiMode mode, out AiAnimationState overrideState)
{
    overrideState = AiAnimationState.Invalid;
    return true;
}


protected virtual bool IsMoveStateCustom(AiMode mode, out bool isMoveState)
{
    isMoveState = false;
    return true;
}

protected bool UpdateWoundsCustom(float deltaTime) => true;


protected bool UpdateBleedingCustom(float deltaTime) => true;


protected virtual bool CanBleedOutCustom(out bool canBleedOut)
{
    canBleedOut = false;
    return true;
}

protected virtual bool TargetCanBeIgnoredCustom(AiTarget target, out bool canBeIgnored)
{
    canBeIgnored = false;
    return true;
}


protected virtual bool CanSeeTargetCustom(out bool canSeeTarget)
{
    canSeeTarget = false;
    return true;
}


protected virtual bool MooseCanBeIgnoredCustom(out bool mooseCanBeIgnored)
{
    mooseCanBeIgnored = false;
    return true;
}

```

Please let me know if anything breaks or you think of improvements, the structure is becoming more concrete over time but there will be room for adjustment and improvement for a while before any kind of official release.
