using UnityEngine.AI;
using UnityEngine;
using Il2Cpp;


namespace ExpandedAiFramework
{
    public sealed class ConsoleCommandManager : BaseSubManager
    {
        private BasePaintManager mActivePaintManager = null;
        private Dictionary<string, Action<IList<string>>> mCommandMap;    
        private Dictionary<string, Action<IList<string>>> mSetCommandMap;
        
        public ConsoleCommandManager(EAFManager manager) : base(manager) 
        {
            RegisterDefaultPaintManagers();
            RegisterCommandMap();
            RegisterSetCommandMap();
        }

        

        private void RegisterDefaultPaintManagers()
        {
            mManager.RegisterPaintManager(new WanderPathPaintManager(mManager));
            mManager.RegisterPaintManager(new HidingSpotPaintManager(mManager));
            mManager.RegisterPaintManager(new SpawnRegionPaintManager(mManager));
        }


        private void RegisterCommandMap()
        {
            mCommandMap = new Dictionary<string, Action<IList<string>>>()
            {
                { CommandString_Help, ProcessHelp },
                { CommandString_Delete, ProcessDelete },
                { CommandString_GoTo, ProcessGoTo },
                { CommandString_Show, ProcessShow },
                { CommandString_Hide, ProcessHide },
                { CommandString_List, ProcessList },
                { CommandString_Paint, ProcessPaint },
                { CommandString_Set, ProcessSet },
                { CommandString_Purge, ProcessPurge },
            };
        }


        private void RegisterSetCommandMap()
        {
            mSetCommandMap = new Dictionary<string, Action<IList<string>>>()
            {
                // Nothing here yet but, eventually we may have some more set commands available via cli. Easier than building a ui still.
            };
        }

        public override void UpdateFromManager()
        {
            base.UpdateFromManager();
            if (mActivePaintManager != null)
            {
                mActivePaintManager.UpdateFromManager();
            }
        }


        public void Console_OnCommand()
        {
            string command = uConsole.GetString();
            if (command == null || command.Length == 0)
            {
                Log($"Command cannot be empty. Use '{CommandString} {CommandString_Help}' for supported commands.", LogCategoryFlags.ConsoleCommand);
                return;
            }
            if (!mCommandMap.TryGetValue(command, out Action<IList<string>> commandAction))
            {
                Log($"Unknown command: {command}. Use '{CommandString} {CommandString_Help}' for supported commands.", LogCategoryFlags.ConsoleCommand);
                return;
            }
            commandAction(ParseArgs());
        }


        private void ProcessHelp(IList<string> args)
        {
            if (args.Count == 0)
            {
                ListAllHelp();
                return;
            }
            string command = GetNextArg(args);
            if (command == null ||!mCommandMap.TryGetValue(command, out Action<IList<string>> commandAction))
            {
                Log($"Unknown command: {command}. Use '{CommandString} {CommandString_Help}' for supported commands.", LogCategoryFlags.ConsoleCommand);
                return;
            }
        }

        private void ListAllHelp()
        {
            foreach (var command in mCommandMap)
            {
                Log($"{command.Key.PadRight(16)} | {command.Value}", LogCategoryFlags.ConsoleCommand);
            }
        }

        private void ProcessRoutedCommand(string command, string typeName, IList<string> args)
        {
            if (mManager.TryGetPaintManager(typeName, out BasePaintManager paintManager))
            {
                paintManager.ProcessCommand(command, args);
            }
            else
            {
                Error($"Unsupported type {typeName}! Supported types: {ListAvailableTypes(command)}");
            }                
        }

        private void ProcessDelete(IList<string> args)
        {
            string typeName = GetNextArg(args);
            switch (typeName)
            {
                default: ProcessRoutedCommand(CommandString_Delete, typeName, args); break;
            }
        }

        private void ProcessGoTo(IList<string> args)
        {
            string typeName = GetNextArg(args);
            switch (typeName)
            {
                default: ProcessRoutedCommand(CommandString_GoTo, typeName, args); break;
            }
        }

        private void ProcessShow(IList<string> args)
        {
            string typeName = GetNextArg(args);
            switch (typeName)
            {
                case CommandString_SpawnRegion: ShowSpawnRegions(args); break;
                case CommandString_Ai: ShowAi(args); break;
                default: ProcessRoutedCommand(CommandString_Show, typeName, args); break;
            }
        }

        private void ProcessHide(IList<string> args)
        {
            string typeName = GetNextArg(args);
            switch (typeName)
            {
                case CommandString_SpawnRegion: HideSpawnRegions(args); break;
                case CommandString_Ai: HideAi(args); break;
                default: ProcessRoutedCommand(CommandString_Hide, typeName, args); break;
            }
        }

        private void ProcessList(IList<string> args)
        {
            string typeName = GetNextArg(args);
            switch (typeName)
            {
                default: ProcessRoutedCommand(CommandString_List, typeName, args); break;
            }
        }

        private void ProcessPurge(IList<string> args)
        {
            string typeName = GetNextArg(args);
            switch (typeName)
            {
                case null: 
                    // purge it all
                    PurgeAi(new List<string>{"normal"});
                    PurgeAi(new List<string>{"aurora"});
                    PurgeSpawnRegions(new List<string>{});
                    break;
                case CommandString_SpawnRegion: PurgeSpawnRegions(args); break;
                case CommandString_Ai: PurgeAi(args); break;
                default: ProcessRoutedCommand(CommandString_Purge, typeName, args); break;
            }
        }

        private void ProcessPaint(IList<string>args)
        {
            string typeName = GetNextArg(args);
            if (mManager.TryGetPaintManager(typeName, out BasePaintManager paintManager))
            {
                SetActivePaintManager(paintManager);
                paintManager.StartPaint(args);
            }
            else
            {
                Log($"Unknown {CommandString_Paint} type: {typeName}. Supported types: {string.Join(", ", mManager.PaintManagers.Keys)}", LogCategoryFlags.ConsoleCommand);
            }
        }

        private void ProcessSet(IList<string> args)
        {
            string target = GetNextArg(args).ToLower();
            if (mSetCommandMap.TryGetValue(target, out Action<IList<string>> setCommand))
            {
                setCommand(args);
                return;
            }
            ProcessRoutedCommand(CommandString_Set, target, args);
        }

        private void ShowSpawnRegions(IList<string> args)
        {
            string name = GetNextArg(args);
            if (string.IsNullOrEmpty(name))
            {
                foreach (CustomSpawnRegion customSpawnRegion in mManager.SpawnRegionManager.CustomSpawnRegions.Values)
                {
                    if (customSpawnRegion?.VanillaSpawnRegion == null) continue;
                    
                    SpawnRegion spawnRegion = customSpawnRegion.VanillaSpawnRegion;
                    AttachMarker(spawnRegion.transform, Vector3.zero, GetSpawnRegionColor(spawnRegion), "SpawnRegionDebugMarker", 1000, 10);
                }
            }
            else
            {
                foreach (CustomSpawnRegion customSpawnRegion in mManager.SpawnRegionManager.CustomSpawnRegions.Values)
                {
                    if (customSpawnRegion?.VanillaSpawnRegion == null) continue;
                    
                    if ($"{customSpawnRegion.VanillaSpawnRegion.GetHashCode()}" == name || $"{customSpawnRegion.ModDataProxy.Guid}" == name)
                    {
                        SpawnRegion spawnRegion = customSpawnRegion.VanillaSpawnRegion;
                        AttachMarker(spawnRegion.transform, Vector3.zero, GetSpawnRegionColor(spawnRegion), "SpawnRegionDebugMarker", 1000, 10);
                    }
                }
            }
        }

        private void HideSpawnRegions(IList<string> args)
        {
            string name = GetNextArg(args);
            if (string.IsNullOrEmpty(name))
            {
                foreach (CustomSpawnRegion customSpawnRegion in mManager.SpawnRegionManager.CustomSpawnRegions.Values)
                {
                    if (customSpawnRegion?.VanillaSpawnRegion == null) continue;
                    
                    foreach (Transform child in customSpawnRegion.VanillaSpawnRegion.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name.Contains("SpawnRegionDebugMarker"))
                        {
                            GameObject.Destroy(child.gameObject);
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (CustomSpawnRegion customSpawnRegion in mManager.SpawnRegionManager.CustomSpawnRegions.Values)
                {
                    if (customSpawnRegion?.VanillaSpawnRegion == null) continue;
                    
                    if ($"{customSpawnRegion.VanillaSpawnRegion.GetHashCode()}" == name || $"{customSpawnRegion.ModDataProxy.Guid}" == name)
                    {
                        foreach (Transform child in customSpawnRegion.VanillaSpawnRegion.GetComponentsInChildren<Transform>(true))
                        {
                            if (child.name.Contains("SpawnRegionDebugMarker"))
                            {
                                GameObject.Destroy(child.gameObject);
                                break;
                            }
                        }
                        return;
                    }
                }
            }
        }

        private void ShowAi(IList<string> args)
        {
            string name = GetNextArg(args);
            if (string.IsNullOrEmpty(name))
            {
                foreach (CustomBaseAi baseAi in mManager.AiManager.CustomAis.Values)
                {
                    AttachMarker(baseAi.transform, Vector3.zero, baseAi.DebugHighlightColor, "AiDebugMarker", 100);
                }
            }
            else
            {
                foreach (CustomBaseAi baseAi in mManager.AiManager.CustomAis.Values)
                {
                    if ($"{baseAi.BaseAi.GetHashCode()}" == name || $"{baseAi.ModDataProxy.Guid}" == name)
                    {
                        AttachMarker(baseAi.transform, Vector3.zero, baseAi.DebugHighlightColor, "AiDebugMarker", 100);
                    }
                }
            }
        }

        private void HideAi(IList<string> args)
        {
            string name = GetNextArg(args);
            if (string.IsNullOrEmpty(name))
            {
                foreach (CustomBaseAi baseAi in mManager.AiManager.CustomAis.Values)
                {
                    foreach (Transform child in baseAi.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name.Contains("AiDebugMarker"))
                        {
                            GameObject.Destroy(child.gameObject);
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (CustomBaseAi baseAi in mManager.AiManager.CustomAis.Values)
                {
                    if ($"{baseAi.BaseAi.GetHashCode()}" == name || $"{baseAi.ModDataProxy.Guid}" == name)
                    {
                        foreach (Transform child in baseAi.GetComponentsInChildren<Transform>(true))
                        {
                            if (child.name.Contains("AiDebugMarker"))
                            {
                                GameObject.Destroy(child.gameObject);
                                break;
                            }
                        }
                        return;
                    }
                }
            }
        }

        private void PurgeSpawnRegions(IList<string> args)
        {
            mManager.DataManager.ScheduleSpawnRegionModDataProxyRequest(new PurgeDataRequest<SpawnRegionModDataProxy>((spawnRegionModDataProxy, result) => 
            {
                Log("Spawn region mod data proxy purged", LogCategoryFlags.ConsoleCommand);
            }, false));
        }

        private void PurgeAi(IList<string> args)
        {
            string mode = GetNextArg(args);
            WildlifeMode wildlifeMode;
            switch (mode)
            {
                case CommandString_Normal:
                    wildlifeMode = WildlifeMode.Normal;
                    break;
                case CommandString_Aurora:
                    wildlifeMode = WildlifeMode.Aurora;
                    break;
                default:
                    Error($"Invalid mode: {mode}");
                    return;
            }
            mManager.DataManager.ScheduleSpawnModDataProxyRequest(new PurgeDataRequest<SpawnModDataProxy>((spawnModDataProxy, result) => 
            {
                Log("Spawn mod data proxy purged");
            }, false), wildlifeMode);
        }

        private Color GetSpawnRegionColor(SpawnRegion spawnRegion)
        {
            switch (spawnRegion.m_AiSubTypeSpawned)
            {
                case AiSubType.Wolf: return Color.grey;
                case AiSubType.Bear: return Color.red;
                case AiSubType.Cougar: return Color.cyan;
                case AiSubType.Rabbit: return Color.blue;
                case AiSubType.Stag: return Color.yellow;
                case AiSubType.Moose: return Color.green;
                default: return Color.clear;
            }
        }

        private GameObject CreateMarker(Vector3 position, Color color, string name, float height, float diameter = 5f)
        {
            GameObject waypointMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            UnityEngine.Object.Destroy(waypointMarker.GetComponent<Collider>());
            waypointMarker.transform.localScale = new Vector3(diameter, height, diameter);
            waypointMarker.transform.position = position;
            waypointMarker.GetComponent<Renderer>().material.color = color;
            waypointMarker.name = name;
            GameObject waypointTopMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.Destroy(waypointTopMarker.GetComponent<Collider>());
            waypointTopMarker.transform.localScale = new Vector3(diameter * 3f, diameter * 3f, diameter * 3f);
            waypointTopMarker.transform.position = position + new Vector3(0, height, 0);
            waypointTopMarker.GetComponent<Renderer>().material.color = color;
            waypointTopMarker.name = name + "Top";
            waypointTopMarker.transform.SetParent(waypointMarker.transform);
            return waypointMarker;
        }

        private void AttachMarker(Transform transform, Vector3 localPosition, Color color, string name, float height, float diameter = 5f)
        {
            GameObject marker = CreateMarker(localPosition, color, name, height, diameter);
            marker.transform.SetParent(transform, false);
        }

        public void SetActivePaintManager(BasePaintManager paintManager)
        {
            if (mActivePaintManager != null && mActivePaintManager != paintManager)
            {
                mActivePaintManager.ExitPaint();
            }
            mActivePaintManager = paintManager;
        }

        public void ClearActivePaintManager(BasePaintManager paintManager = null)
        {
            if (paintManager == null || mActivePaintManager == paintManager)
            {
                mActivePaintManager = null;
            }
        }
    }
}
