global using static ExpandedAiFramework.Utility;
using ComplexLogger;
using Il2Cpp;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using static Il2CppSystem.Runtime.Remoting.RemotingServices;


namespace ExpandedAiFramework
{
    public static class Utility
    {
        public const float SecondsToDays = 1f / 86400f;
        public const float DaysToSeconds = 86400f;
        public const float SecondsToHours = 1f / 3600f;
        public const float HoursToSeconds = 3600;
        public const string ModName = "Expanded Ai Framework";
        public const string CommandString = "eaf";
        public const string CommandString_Help = "help";
        public const string CommandString_Create = "create";
        public const string CommandString_Delete = "delete";
        public const string CommandString_Save = "save";
        public const string CommandString_Load = "load";
        public const string CommandString_AddTo = "add";
        public const string CommandString_GoTo = "goto";
        public const string CommandString_Finish = "finish";
        public const string CommandString_Show = "show";
        public const string CommandString_Hide = "hide";
        public const string CommandString_List = "list";
        public const string CommandString_Spawn = "spawn";
        public const string CommandString_Info = "info";

        public const string CommandString_NavMesh = "navmesh";
        public const string CommandString_WanderPath = "wanderpath";
        public const string CommandString_HidingSpot = "hidingspot";
        public const string CommandString_MapData = "mapdata";
        public const string CommandString_SpawnRegion = "spawnregion";


        public const string CommandString_OnCommandSupportedTypes =
            $"{CommandString_Help}" +
            $"{CommandString_Create} " +
            $"{CommandString_Delete} " +
            $"{CommandString_Save} " +
            $"{CommandString_Load} " +
            $"{CommandString_AddTo} " +
            $"{CommandString_GoTo} " +
            $"{CommandString_Finish} " +
            $"{CommandString_Show} " +
            $"{CommandString_Hide} " +
            $"{CommandString_List} ";

        public const string CommandString_HelpSupportedCommands =
            $"{CommandString_Create} " +
            $"{CommandString_Delete} " +
            $"{CommandString_Save} " +
            $"{CommandString_Load} " +
            $"{CommandString_AddTo} " +
            $"{CommandString_GoTo} " +
            $"{CommandString_Finish} " +
            $"{CommandString_Show} " +
            $"{CommandString_Hide} " +
            $"{CommandString_List} ";

        public const string CommandString_CreateSupportedTypes = $"{CommandString_WanderPath} {CommandString_HidingSpot}";
        public const string CommandString_DeleteSupportedTypes = $"{CommandString_WanderPath} {CommandString_HidingSpot}";
        public const string CommandString_SaveSupportedTypes = $"{CommandString_MapData}";
        public const string CommandString_AddToSupportedTypes = $"{CommandString_WanderPath}";
        public const string CommandString_FinishSupportedTypes = $"{CommandString_WanderPath}";
        public const string CommandString_GoToSupportedTypes = $"{CommandString_WanderPath} {CommandString_HidingSpot}";
        public const string CommandString_ShowSupportedTypes = $"{CommandString_WanderPath} {CommandString_HidingSpot} {CommandString_NavMesh} {CommandString_SpawnRegion}";
        public const string CommandString_HideSupportedTypes = $"{CommandString_WanderPath} {CommandString_HidingSpot} {CommandString_NavMesh} {CommandString_SpawnRegion}";
        public const string CommandString_ListSupportedTypes = $"{CommandString_WanderPath} {CommandString_HidingSpot}";
        public const string CommandString_LoadSupportedTypes = $"{CommandString_MapData}";

        public static readonly string[] SceneNames = new string[]
        {
            "AFHangar",
            "AirfieldRegion",
            "AirfieldTrailerB",
            "AirfieldWoodCabinA",
            "AshCabinD",
            "AshCabinF",
            "AshCanyonRegion",
            "AshCaveA",
            "AshCaveB",
            "AshMine",
            "AshWoodCabinA",
            "BankA",
            "BarnHouseA",
            "BarnHouseB",
            "BlackrockCaveA",
            "BlackrockInteriorASurvival",
            "BlackrockMineA",
            "BlackrockPowerplantA",
            "BlackrockPrisonSurvivalZone",
            "BlackrockRegion",
            "BlackrockSteamTunnelsASurvival",
            "BlackRockTrailerB",
            "BlackrockTransitionZone",
            "BunkerA",
            "BunkerB",
            "BunkerC",
            "BunkerXL",
            "CampOffice",
            "CanneryMarshTransitionCave",
            "CanneryRegion",
            "CanneryTrailerA",
            "CanyonRoadCave",
            "CanyonRoadTransitionZone",
            "CaveB",
            "CaveC",
            "CaveD",
            "ChurchB",
            "ChurchC",
            "CoastalHouseA",
            "CoastalHouseB",
            "CoastalHouseC",
            "CoastalHouseD",
            "CoastalHouseE",
            "CoastalHouseF",
            "CoastalHouseH",
            "CoastalRegion",
            "CommunityHallA",
            "ConvenienceStoreA",
            "CrashMountainRegion",
            "Dam",
            "DamCaveTransitionZone",
            "DamRiverTransitionZoneB",
            "DamTrailerB",
            "DamTransitionZone",
            "FarmHouseA",
            "FarmHouseABasement",
            "FarmHouseB",
            "FishingCabinA",
            "FishingCabinC",
            "FishingCabinD",
            "GreyMothersHouseA",
            "HighwayMineTransitionZone",
            "HighwayTransitionZone",
            "HouseBasementC",
            "HouseBasementPV",
            "HubCave",
            "HubRegion",
            "HuntingLodgeA",
            "IceCaveA",
            "IceCaveB",
            "LakeCabinA",
            "LakeCabinB",
            "LakeCabinC",
            "LakeCabinD",
            "LakeCabinE",
            "LakeCabinF",
            "LakeRegion",
            "LighthouseA",
            "LongRailTransitionZone",
            "LongTransitionCave",
            "MaintenanceShedA",
            "MaintenanceShedB",
            "MarshRegion",
            "MiltonHouseA",
            "MiltonHouseC",
            "MiltonHouseD",
            "MiltonHouseF1",
            "MiltonHouseF2",
            "MiltonHouseF3",
            "MiltonHouseH1",
            "MiltonHouseH2",
            "MiltonHouseH3",
            "MiltonTrailerB",
            "MineConcentratorBuilding",
            "MineTransitionZone",
            "MiningRegion",
            "MiningRegionMine",
            "MountainCaveA",
            "MountainCaveB",
            "MountainPassBasement",
            "MountainPassBuriedCabin",
            "MountainPassCabinA",
            "MountainPassCaveA",
            "MountainPassCaveB",
            "MountainPassRegion",
            "MountainTownCaveA",
            "MountainTownCaveB",
            "MountainTownRegion",
            "PostOfficeA",
            "PrepperCacheA",
            "PrepperCacheAEmpty",
            "PrepperCacheB",
            "PrepperCacheBEmpty",
            "PrepperCacheBInterloper",
            "PrepperCacheC",
            "PrepperCacheCEmpty",
            "PrepperCacheD",
            "PrepperCacheDEmpty",
            "PrepperCacheE",
            "PrepperCacheEEmpty",
            "PrepperCacheEmpty",
            "PrepperCacheF",
            "PrepperCacheFEmpty",
            "PumpHouse",
            "QuonsetGasStation",
            "RadarBuilding",
            "RadioControlHut",
            "RadioControlHutB",
            "RadioControlHutC",
            "RavineTransitionZone",
            "RiverValleyRegion",
            "RiverValleyTransitionCave",
            "RuralRegion",
            "RuralStoreA",
            "SafeHouseA",
            "TracksRegion",
            "TrailerA",
            "TrailerB",
            "TrailerC",
            "TrailerD",
            "TrailerE",
            "TrailerSShape",
            "WhalingMine",
            "WhalingShipA",
            "WhalingStationRegion",
            "WhalingWarehouseA",
            "WoodCabinA",
            "WoodCabinB",
            "WoodCabinC"
        };

        public static EAFManager Manager { get { return EAFManager.Instance; } }
        public static void Log(string message, FlaggedLoggingLevel logLevel, bool toUConsole) { Manager.Log(message, logLevel, toUConsole); }
        public static void LogTrace(string message) { Manager.LogTrace(message); }
        public static void LogDebug(string message) { Manager.LogDebug(message); }
        public static void LogVerbose(string message) { Manager.LogVerbose(message); }
        public static void LogWarning(string message, bool toUConsole = true) { Manager.LogWarning(message, toUConsole); }
        public static void LogError(string message, FlaggedLoggingLevel additionalLevelFlags = 0U) { Manager.LogError(message, additionalLevelFlags); }
        public static void LogAlways(string message) { Manager.LogAlways(message); }
        public static TEnum ToEnum<TEnum>(this uint uval) where TEnum : Enum { return UnsafeUtility.As<uint, TEnum>(ref uval); }
        public static TEnum ToEnumL<TEnum>(this ulong uval) where TEnum : Enum { return UnsafeUtility.As<ulong, TEnum>(ref uval); }
        public static uint ToUInt<TEnum>(this TEnum val) where TEnum : Enum { return UnsafeUtility.As<TEnum, uint>(ref val); }
        public static ulong ToULong<TEnum>(this TEnum val) where TEnum : Enum { return UnsafeUtility.As<TEnum, ulong>(ref val); }
        public static bool Any<TEnum>(this TEnum val) where TEnum : Enum { return val.ToUInt() != 0; }
        public static bool AnyL<TEnum>(this TEnum val) where TEnum : Enum { return val.ToULong() != 0UL; }
        public static bool OnlyOne<TEnum>(this TEnum val) where TEnum : Enum { uint f = val.ToUInt(); return f != 0 && (f & f - 1) == 0; }
        public static bool OnlyOneL<TEnum>(this TEnum val) where TEnum : Enum { ulong f = val.ToULong(); return f != 0UL && (f & f - 1UL) == 0UL; }
        public static bool OnlyOneOrZero<TEnum>(this TEnum val) where TEnum : Enum { uint f = val.ToUInt(); return (f & f - 1) == 0; }
        public static bool OnlyOneOrZeroL<TEnum>(this TEnum val) where TEnum : Enum { ulong f = val.ToULong(); return (f & f - 1UL) == 0UL; }
        public static bool IsSet<TEnum>(this TEnum val, TEnum toCheck) where TEnum : Enum { return (val.ToUInt() & toCheck.ToUInt()) != 0; }
        public static bool IsSetL<TEnum>(this TEnum val, TEnum toCheck) where TEnum : Enum { return (val.ToULong() & toCheck.ToULong()) != 0UL; }
        public static bool IsUnset<TEnum>(this TEnum val, TEnum toCheck) where TEnum : Enum { return (val.ToUInt() & toCheck.ToUInt()) == 0; }
        public static bool IsUnsetL<TEnum>(this TEnum val, TEnum toCheck) where TEnum : Enum { return (val.ToULong() & toCheck.ToULong()) == 0UL; }
        public static bool AnyOf<TEnum>(this TEnum val, TEnum toCheck) where TEnum : Enum { return (val.ToUInt() & toCheck.ToUInt()) != 0; }
        public static bool AnyOfL<TEnum>(this TEnum val, TEnum toCheck) where TEnum : Enum { return (val.ToULong() & toCheck.ToULong()) != 0UL; }
        public static bool AllOf<TEnum>(this TEnum val, TEnum toCheck) where TEnum : Enum { uint c = toCheck.ToUInt(); return (val.ToUInt() & c) == c; }
        public static bool AllOfL<TEnum>(this TEnum val, TEnum toCheck) where TEnum : Enum { ulong c = toCheck.ToULong(); return (val.ToULong() & c) == c; }
        public static bool OnlyOneOf<TEnum>(this TEnum val, TEnum toCheck) where TEnum : Enum { uint v = val.ToUInt() & toCheck.ToUInt(); return v != 0 && (v & v - 1) == 0; }
        public static bool OnlyOneOfL<TEnum>(this TEnum val, TEnum toCheck) where TEnum : Enum { ulong v = val.ToULong() & toCheck.ToULong(); return v != 0UL && (v & v - 1UL) == 0UL; }
        public static bool NoneOf<TEnum>(this TEnum val, TEnum toCheck) where TEnum : Enum { return (val.ToUInt() & toCheck.ToUInt()) == 0; }
        public static bool NoneOfL<TEnum>(this TEnum val, TEnum toCheck) where TEnum : Enum { return (val.ToULong() & toCheck.ToULong()) == 0UL; }
        public static bool OthersSet<TEnum>(this TEnum val, TEnum toIgnore) where TEnum : Enum { return (val.ToUInt() & ~toIgnore.ToUInt()) != 0; }
        public static bool OthersSetL<TEnum>(this TEnum val, TEnum toIgnore) where TEnum : Enum { return (val.ToULong() & ~toIgnore.ToULong()) != 0UL; }
        public static TEnum UnsetFlags<TEnum>(this TEnum val, TEnum flags) where TEnum : Enum { return (val.ToUInt() & ~flags.ToUInt()).ToEnum<TEnum>(); }
        public static TEnum UnsetFlagsL<TEnum>(this TEnum val, TEnum flags) where TEnum : Enum { return (val.ToULong() & ~flags.ToULong()).ToEnumL<TEnum>(); }
        public static TEnum SetFlags<TEnum>(this TEnum val, TEnum flags, bool shouldSet = true) where TEnum : Enum { return (shouldSet ? val.ToUInt() | flags.ToUInt() : val.ToUInt() & ~flags.ToUInt()).ToEnum<TEnum>(); }
        public static TEnum SetFlagsL<TEnum>(this TEnum val, TEnum flags, bool shouldSet = true) where TEnum : Enum { return (shouldSet ? val.ToULong() | flags.ToULong() : val.ToULong() & ~flags.ToULong()).ToEnumL<TEnum>(); }
        public static float SquaredDistance(Vector3 a, Vector3 b)
        {
            return ((a.x - b.x) * (a.x - b.x)) + ((a.y - b.y) * (a.y - b.y)) + ((a.z - b.z) * (a.z - b.z));
        }
        public static float GetCurrentTimelinePoint()
        {
            return GameManager.m_TimeOfDay.m_WeatherSystem.m_ElapsedHoursAccumulator + GameManager.m_TimeOfDay.m_WeatherSystem.m_ElapsedHours;
        }
        public static bool IsValidGameplayScene(string sceneName, out string parsedSceneName)
        {
            parsedSceneName = string.Empty;
            for (int i = 0, iMax = SceneNames.Length; i < iMax; i++)
            {
                if (sceneName.Contains(SceneNames[i]))
                {
                    parsedSceneName = SceneNames[i];
                    return true;
                }
            }
            return false;
        }


        #region Console Command Helpers


        public static bool IsTypeSupported(string type, string supportedTypeString, bool shouldWarn = true)
        {
            if (!IsTypeProvided(type, supportedTypeString, shouldWarn))
            {
                return false;
            }
            string[] supportedTypes = supportedTypeString.Split(' ');
            for (int i = 0, iMax = supportedTypes.Length; i < iMax; i++)
            {
                if (supportedTypes[i] == type)
                {
                    return true;
                }
            }
            if (shouldWarn)
            {
                LogWarning($"{type} is not supported by this command! Supported types: {supportedTypeString}");
            }
            return false;
        }


        public static bool IsTypeProvided(string type, string supportedTypeString, bool shouldWarn = true)
        {
            if (!IsStringProvided(type))
            {
                if (shouldWarn)
                {
                    LogWarning($"Provide a type to use this command! Supported types: {supportedTypeString}");
                }
                return false;
            }
            return true;
        }


        public static bool IsNameProvided(string name, bool shouldWarn = true)
        {
            if (!IsStringProvided(name))
            {
                if (shouldWarn)
                {
                    LogWarning($"Provide a name!");
                }
                return false;
            }
            return true;
        }


        public static bool IsStringProvided(string str)
        {
            if (str == null || str.Length == 0)
            {
                return false;
            }
            return true;
        }

        #endregion
    }
}