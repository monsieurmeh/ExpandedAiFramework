using ComplexLogger;
using UnityEngine;


namespace ExpandedAiFramework
{
    public partial class CustomAiBase
    {
#if DEV_BUILD
        protected AiMode mCachedMode = AiMode.None;
        protected bool mReadout = false;
        public Transform mMarkerTransform;
        public Renderer mMarkerRenderer;


        protected void LogTrace(string message) { Utility.LogTrace(message); }
        protected void LogDebug(string message) { Utility.LogDebug(message); }
        protected void LogVerbose(string message) { Utility.LogVerbose(message); }
        protected void LogWarning(string message) { Utility.LogWarning(message); }
        protected void LogError(string message, FlaggedLoggingLevel additionalFlags = 0U) { Utility.LogError(message, additionalFlags); }
        protected void LogCriticalError(string message) { LogError(message, FlaggedLoggingLevel.Critical); }
        protected void LogException(string message) { LogError(message, FlaggedLoggingLevel.Exception); }
#endif

        private void OnAugmentDebug()
        {
#if DEV_BUILD_STATELABEL

            GameObject marker = Manager.Instance.CreateMarker(mBaseAi.transform.position, Color.clear, $"Debug Ai Marker for {mBaseAi.name}", 100, 0.5f);
            mMarkerTransform = marker.transform;
            mMarkerTransform.SetParent(mBaseAi.transform);
            mMarkerRenderer = marker.GetComponent<Renderer>();
            SetMarkerColor();
#endif
        }


        private void OnUpdateDebug()
        {
#if DEV_BUILD_STATELABEL
            SetMarkerColor();
#endif
        }

#if DEV_BUILD_STATELABEL

        public void SetMarkerColor()
        {
            if (mMarkerRenderer != null && mMarkerRenderer.material != null)
            {
#if DEV_BUILD_STATELABEL
                mMarkerRenderer.material.color = GetMarkerColorByState();
#endif
            }
        }


        public Color GetMarkerColorByState()
        {
            if (mCachedMode != CurrentMode)
            {
                mCachedMode = CurrentMode;
                mMarkerRenderer.gameObject.active = true;
            }
            switch (CurrentMode)
            {
                case AiMode.Wander:
                case AiMode.PatrolPointsOfInterest:
                case AiMode.FollowWaypoints:
                case (AiMode)AiModeEAF.Returning:
                case AiMode.GoToPoint:
                    return Color.grey;
                case AiMode.Attack:
                case AiMode.PassingAttack:
                case AiMode.Struggle:
                    return Color.red;
                case AiMode.Feeding:
                case AiMode.Dead:
                case AiMode.Idle:
                    return Color.blue;
                case AiMode.Flee:
                case AiMode.WanderPaused:
                    return Color.green;
                case AiMode.Investigate:
                case AiMode.InvestigateFood:
                case AiMode.InvestigateSmell:
                case AiMode.Stalking:
                case AiMode.HideAndSeek:
                    return new Color(255, 0, 255);
                case (AiMode)AiModeEAF.Hiding:
                case AiMode.ScratchingAntlers:
                case AiMode.ScriptedSequence:
                case AiMode.InteractWithProp:
                case AiMode.Sleep:
                    return new Color(255, 255, 0);
                case AiMode.Rooted:
                case AiMode.None:
                case AiMode.Disabled:
                    return Color.black;
                default:
                    mMarkerRenderer.gameObject.active = false;
                    return Color.clear;
            }
        }
#endif
    }
}