global using static ExpandedAiFramework.CommandStrings;

namespace ExpandedAiFramework
{
    public static class CommandStrings
    {
        //commands
        public const string CommandString = "eaf";
        public const string CommandString_Help = "help";
        public const string CommandString_Delete = "delete";
        public const string CommandString_GoTo = "goto";
        public const string CommandString_Show = "show";
        public const string CommandString_Hide = "hide";
        public const string CommandString_List = "list";
        public const string CommandString_Paint = "paint";
        public const string CommandString_Set = "set";
        public const string CommandString_DebugMenu = "debugmenu";
        public const string CommandString_Purge = "purge";
        public const string CommandString_Create = "create";
        public const string CommandString_Save = "save";
        public const string CommandString_Load = "load";
        public const string CommandString_Info = "info";
        public const string CommandString_Spawn = "spawn";

        // types
        public const string CommandString_WanderPath = "wanderpath";
        public const string CommandString_HidingSpot = "hidingspot";
        public const string CommandString_SpawnRegion = "spawnregion";
        public const string CommandString_DataPath = "datapath";
        public const string CommandString_Ai = "ai";

        // modes
        public const string CommandString_Normal = "normal";
        public const string CommandString_Aurora = "aurora";

        public static Dictionary<string, string> CommandDictionary_CommandUsage = new Dictionary<string, string>()
        {
            { $"{CommandString_Help}", $"Usage: {CommandString} {CommandString_Help} - Shows help message for any command type" },
            { $"{CommandString_Delete}", $"Usage: {CommandString} {CommandString_Delete} <type> <name> - Delete an entity of type <type> and name <name>" },
            { $"{CommandString_GoTo}", $"Usage: {CommandString} {CommandString_GoTo} <type> <name> <args...>- Teleport to an entity of type <type> and name <name>. Potential additional arguments depending on type." },
            { $"{CommandString_Show}", $"Usage: {CommandString} {CommandString_Show} <type> <name> - Show an entity of type <type> and name <name>" },
            { $"{CommandString_Hide}", $"Usage: {CommandString} {CommandString_Hide} <type> <name> - Hide an entity of type <type> and name <name>" },
            { $"{CommandString_List}", $"Usage: {CommandString} {CommandString_List} <type> - List entities of type <type>" },
            { $"{CommandString_Paint}", $"Usage: {CommandString} {CommandString_Paint} <type> <name> <args...> - Start paint mode for an entity of type <type> and name <name>. Potential additional arguments depending on type." },
            { $"{CommandString_Set}", $"Usage: {CommandString} {CommandString_Set} <type> <name> <property> <value> - Set a property of an entity of type <type> and name <name>" },
            { $"{CommandString_Purge}", $"Usage: {CommandString} {CommandString_Purge} <type> <args...>- Purge entities of type <type>. Potential additional arguments depending on type." },
        };

        private static string[] SerializedDataTypes = new string[] { CommandString_WanderPath, CommandString_HidingSpot, CommandString_SpawnRegion, CommandString_Ai };

        public static Dictionary<string, string[]> CommandDictionary_SupportedTypes = new Dictionary<string, string[]>()
        {
            { $"{CommandString_Delete}", SerializedDataTypes },
            { $"{CommandString_GoTo}", SerializedDataTypes },
            { $"{CommandString_Show}", SerializedDataTypes },
            { $"{CommandString_Hide}", SerializedDataTypes },
            { $"{CommandString_List}", SerializedDataTypes },
            { $"{CommandString_Paint}", SerializedDataTypes },
            { $"{CommandString_Purge}", new string[] { CommandString_SpawnRegion, CommandString_Ai } },
            { $"{CommandString_Set}", new string[] {$"{CommandString_WanderPath}_{CommandString_DataPath}", $"{CommandString_HidingSpot}_{CommandString_DataPath}" } },
        };
    }
}
