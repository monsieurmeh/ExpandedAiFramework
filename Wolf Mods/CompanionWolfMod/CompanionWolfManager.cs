
using UnityEngine;
using System.Text;
using MelonLoader.Utils;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Attributes;
using ComplexLogger;
using UnityEngine.AI;
using ModData;
using MelonLoader.TinyJSON;

namespace ExpandedAiFramework.CompanionWolfMod
{
    public class CompanionWolfManager : ISubManager
    {
        private const float SecondsToDays = 1f / 86400f;
        private const float SecondsToHours = 1f / 3600f;

        [Serializable]
        public class CompanionWolfData
        {
            public float CurrentAffection;
            public float CurrentCalories;
            public float CurrentCondition;

            public bool Indoors;
            public bool Tamed;
            public float TimeUntilUntamedTimeout;
            public float TimeUntilAffectionDecay;
            public float TimeUntilTamingAllowed;

            public CompanionWolfData() { }

        }
        
        protected EAFManager mManager;
        protected CompanionWolf mInstance;
        protected CompanionWolfData mData;
        protected bool mActive = false;
        protected bool mInitialized = false;

        public CompanionWolfData Data { get { return mData; } }
        public bool Tamed { get { return mData?.Tamed ?? false; } }



        public void Initialize(EAFManager manager)
        {
            mManager = manager;
            mInitialized = true;
        }


        public void Shutdown() { }


        public void OnLoad()
        {
            JSON.Populate(JSON.Load(mManager.ModData.Load("CompanionWoldMod")), mData);
        }


        public void OnSave()
        {
            mManager.ModData.Save(JSON.Dump(mData), "CompanionWolfMod");
        }


        public void ActivateInstance(CompanionWolf instance)
        {
            mInstance = instance;
            mActive = instance != null;
        }

        
        public void DeactivateInstance()
        {
            mInstance = null;
            mActive = false;
        }


        public void Update()
        {
            if (!mActive)
            {
                return;
            }
            float deltaTime = Time.deltaTime;
            if (!mData.Indoors && mData.Tamed)
            {
                mData.CurrentCalories -= deltaTime * CompanionWolf.Settings.CaloriesBurnedPerDay * SecondsToDays;
                if (mData.CurrentCalories < 0.0f)
                {
                    mData.CurrentCalories = 0.0f;
                    mData.CurrentCondition -= deltaTime * CompanionWolf.Settings.StarvingConditionDecayPerHour * SecondsToHours;
                }
            }
            if (mData.Indoors || !mData.Tamed)
            {
                mData.CurrentAffection -= deltaTime * (mData.Tamed ? CompanionWolf.Settings.TamedAffectionDecayRate : CompanionWolf.Settings.UntamedAffectionDecayRate) * SecondsToHours;
            }  
            if (!mData.Tamed && mData.TimeUntilUntamedTimeout <= Time.time)
            {
                //Disappear! go away! until next time :-(
                DeactivateInstance();
            }
        }
    }
}


