using UnityEngine.AI;
using UnityEngine;
using Il2Cpp;


namespace ExpandedAiFramework
{
    public sealed class ConsoleCommandManager : BaseSubManager
    {

        private GameObject mNavmeshObj = null;
        private BasePaintManager mActivePaintManager = null;
        
        public ConsoleCommandManager(EAFManager manager, ISubManager[] subManagers) : base(manager, subManagers) 
        {
            RegisterDefaultPaintManagers();
        }

        public override void Update()
        {
            base.Update();
            if (mActivePaintManager != null)
            {
                mActivePaintManager.Update();
            }
        }

        public void Console_OnCommand()
        {
            string command = uConsole.GetString();
            if (command == null || command.Length == 0)
            {
                LogAlways("Supported commands: help, delete, save, load, goto, show, hide, list, paint, set");
                return;
            }

            string[] args = new string[10];
            int argCount = 0;
            for (int i = 0; i < 10; i++)
            {
                string arg = uConsole.GetString();
                if (string.IsNullOrEmpty(arg)) break;
                args[argCount++] = arg;
            }

            Array.Resize(ref args, argCount);

            switch (command.ToLower())
            {
                case CommandString_Help: ProcessHelp(args); break;
                case CommandString_Delete: ProcessRoutedCommand(CommandString_Delete, args); break;
                case CommandString_Save: ProcessSave(args); break;
                case CommandString_Load: ProcessLoad(args); break;
                case CommandString_GoTo: ProcessRoutedCommand(CommandString_GoTo, args); break;
                case CommandString_Show: ProcessShow(args); break;
                case CommandString_Hide: ProcessHide(args); break;
                case CommandString_List: ProcessRoutedCommand(CommandString_List, args); break;
                case CommandString_Paint: ProcessPaint(args); break;
                case CommandString_Set: ProcessSet(args); break;
                case "unlock": GameManager.GetFeatMasterHunter().Unlock(); break;
                default: LogAlways($"Unknown command: {command}. Type '{CommandString} {CommandString_Help}' for supported commands."); break;
            }
        }

        private void ProcessHelp(string[] args)
        {
            if (args.Length == 0)
            {
                LogAlways("Available commands:");
                LogAlways($"  {CommandString_Delete} <type> <name> - Delete an object");
                LogAlways($"  {CommandString_Save} [type] - Save data");
                LogAlways($"  {CommandString_Load} [type] - Load data");
                LogAlways($"  {CommandString_GoTo} <type> <name> [args...] - Teleport to an object");
                LogAlways($"  {CommandString_Show} <type> [name] - Show objects");
                LogAlways($"  {CommandString_Hide} <type> [name] - Hide objects");
                LogAlways($"  {CommandString_List} <type> - List objects");
                LogAlways($"  {CommandString_Paint} <type> <name> [args...] - Start paint mode");
                LogAlways($"  {CommandString_Set} <target> <property> <value> - Set properties");
                LogAlways($"Supported types: {string.Join(", ", mManager.PaintManagers.Keys)}, {CommandString_NavMesh}, {CommandString_SpawnRegion}, {CommandString_Ai}");
                return;
            }

            string helpTopic = args[0].ToLower();
            switch (helpTopic)
            {
                case CommandString_Paint:
                    LogAlways($"{CommandString_Paint} <type> <name> [filepath] - Starts interactive paint mode for creating objects");
                    break;
                case CommandString_Set:
                    LogAlways($"{CommandString_Set} <target> <property> <value> - Sets properties. Use 'paint_<type>' as target to set paint manager properties");
                    break;
                default:
                    LogAlways($"No help available for: {helpTopic}");
                    break;
            }
        }

        private void ProcessRoutedCommand(string command, string[] args)
        {
            if (args.Length == 0)
            {
                LogAlways($"{command} command requires a type");
                return;
            }

            string typeName = args[0];
            string[] remainingArgs = new string[args.Length - 1];
            Array.Copy(args, 1, remainingArgs, 0, remainingArgs.Length);

            if (mManager.TryGetPaintManager(typeName, out BasePaintManager paintManager))
            {
                paintManager.ProcessCommand(command, remainingArgs);
                }
                else
                {
                LogAlways($"Unknown type: {typeName}. Supported types: {string.Join(", ", mManager.PaintManagers.Keys)}");
                }                
        }

        private void ProcessSave(string[] args)
        {
            if (args.Length == 0)
            {
                mManager.DataManager.SaveMapData();
                return;
            }

            string typeName = args[0];
            if (typeName.ToLower() == CommandString_MapData)
            {
                mManager.DataManager.SaveMapData();
            }
            else if (mManager.TryGetPaintManager(typeName, out BasePaintManager paintManager))
            {
                paintManager.ProcessCommand(CommandString_Save, new string[0]);
                }
                else
                {
                LogAlways($"Unknown save type: {typeName}");
                }
        }

        private void ProcessLoad(string[] args)
        {
            if (args.Length == 0)
            {
                mManager.DataManager.LoadMapData();
                    return;
            }

            string typeName = args[0];
            if (typeName.ToLower() == CommandString_MapData)
            {
                mManager.DataManager.LoadMapData();
            }
            else if (mManager.TryGetPaintManager(typeName, out BasePaintManager paintManager))
            {
                paintManager.ProcessCommand(CommandString_Load, new string[0]);
            }
            else
            {
                LogAlways($"Unknown load type: {typeName}");
            }
        }

        private void ProcessShow(string[] args)
        {
            if (args.Length == 0)
            {
                LogAlways($"{CommandString_Show} command requires a type");
                return;
            }

            string typeName = args[0].ToLower();
            string[] remainingArgs = new string[args.Length - 1];
            Array.Copy(args, 1, remainingArgs, 0, remainingArgs.Length);

            switch (typeName)
            {
                case CommandString_NavMesh: ShowNavMesh(); break;
                case CommandString_SpawnRegion: ShowSpawnRegions(remainingArgs); break;
                case CommandString_Ai: ShowAi(remainingArgs); break;
                default:
                    if (mManager.TryGetPaintManager(typeName, out BasePaintManager paintManager))
                    {
                        paintManager.ProcessCommand(CommandString_Show, remainingArgs);
                    }
                    else
                    {
                        LogAlways($"Unknown {CommandString_Show} type: {typeName}");
                    }
                    break;
            }
        }

        private void ProcessHide(string[] args)
        {
            if (args.Length == 0)
            {
                LogAlways($"{CommandString_Hide} command requires a type");
                return;
            }

            string typeName = args[0].ToLower();
            string[] remainingArgs = new string[args.Length - 1];
            Array.Copy(args, 1, remainingArgs, 0, remainingArgs.Length);

            switch (typeName)
            {
                case CommandString_NavMesh: HideNavMesh(); break;
                case CommandString_SpawnRegion: HideSpawnRegions(remainingArgs); break;
                case CommandString_Ai: HideAi(remainingArgs); break;
                default:
                    if (mManager.TryGetPaintManager(typeName, out BasePaintManager paintManager))
                    {
                        paintManager.ProcessCommand(CommandString_Hide, remainingArgs);
            }
            else
            {
                        LogAlways($"Unknown {CommandString_Hide} type: {typeName}");
                    }
                    break;
            }
        }

        private void ProcessPaint(string[] args)
        {
            if (args.Length == 0)
            {
                LogAlways($"{CommandString_Paint} command requires a type");
                return;
            }

            string typeName = args[0];
            string[] remainingArgs = new string[args.Length - 1];
            Array.Copy(args, 1, remainingArgs, 0, remainingArgs.Length);

            if (mManager.TryGetPaintManager(typeName, out BasePaintManager paintManager))
            {
                if (mActivePaintManager != null && mActivePaintManager != paintManager)
                {
                    mActivePaintManager.ExitPaint();
                }
                mActivePaintManager = paintManager;
                paintManager.StartPaint(remainingArgs);
            }
            else
            {
                LogAlways($"Unknown {CommandString_Paint} type: {typeName}. Supported types: {string.Join(", ", mManager.PaintManagers.Keys)}");
            }
        }

        private void ProcessSet(string[] args)
        {
            if (args.Length < 3)
            {
                LogAlways($"{CommandString_Set} command requires target, property, and value");
                    return;
            }

            string target = args[0].ToLower();
            string property = args[1];
            string value = args[2];

            if (target == "sys")
            {
                LogAlways("No global system properties available to set");
                    return;
            }

            if (target.StartsWith("paint_"))
            {
                string paintType = target.Substring(6);
                if (mManager.TryGetPaintManager(paintType, out BasePaintManager paintManager))
                {
                    paintManager.ProcessCommand(CommandString_Set, new string[] { property, value });
            }
            else
            {
                    LogAlways($"Unknown {CommandString_Paint} type: {paintType}");
                }
            }
            else
            {
                LogAlways($"Unknown {CommandString_Set} target: {target}. Use 'sys' for system or '{CommandString_Paint}_<type>' for paint managers");
            }
        }

        private void ShowNavMesh()
        {
            if (mNavmeshObj == null)
            {
                try
                {
                    var triangulation = NavMesh.CalculateTriangulation();
                    if (triangulation.vertices.Length == 0 || triangulation.indices.Length == 0)
                    {
                        LogWarning("NavMesh triangulation is empty.");
                        return;
                    }

                    Vector3[] vertices = triangulation.vertices;
                    int[] rawIndices = triangulation.indices;
                    int[] areas = triangulation.areas;

                    Color[] areaColors = new Color[vertices.Length];
                    List<int> validIndices = new List<int>(rawIndices.Length);

                    for (int i = 0; i < rawIndices.Length; i += 3)
                    {
                        int idx0 = rawIndices[i];
                        int idx1 = rawIndices[i + 1];
                        int idx2 = rawIndices[i + 2];

                        if (idx0 < vertices.Length && idx1 < vertices.Length && idx2 < vertices.Length)
                        {
                            validIndices.Add(idx0);
                            validIndices.Add(idx1);
                            validIndices.Add(idx2);

                            int triangleIndex = i / 3;
                            int areaType = areas[triangleIndex];
                            Color color = GetNavMeshColor(areaType);

                            areaColors[idx0] = color;
                            areaColors[idx1] = color;
                            areaColors[idx2] = color;
                        }
                    }

                    Mesh mesh = new Mesh();
                    mesh.name = "LargeNavMesh";
                    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                    mesh.vertices = vertices;
                    mesh.triangles = validIndices.ToArray();
                    mesh.colors = areaColors;
                    mesh.RecalculateNormals();

                    mNavmeshObj = new GameObject();
                    MeshFilter meshFilter = mNavmeshObj.AddComponent<MeshFilter>();
                    meshFilter.sharedMesh = mesh;
                    MeshRenderer renderer = mNavmeshObj.AddComponent<MeshRenderer>();
                    Material vertexColorMat = new Material(Shader.Find("Legacy Shaders/Diffuse"));
                    renderer.material = vertexColorMat;
                    mNavmeshObj.name = "eafNavMeshObj";
                }
                catch (Exception e)
                {
                    LogError(e.ToString());
                return;
            }
            }
            mNavmeshObj.SetActive(true);
        }

        private void HideNavMesh()
        {
            if (mNavmeshObj != null)
            {
                mNavmeshObj.SetActive(false);
            }
        }

        private void ShowSpawnRegions(string[] args)
        {
            if (args.Length == 0)
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
                string name = args[0];
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

        private void HideSpawnRegions(string[] args)
        {
            if (args.Length == 0)
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
                string name = args[0];
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

        private void ShowAi(string[] args)
        {
            if (args.Length == 0)
            {
                foreach (CustomBaseAi baseAi in mManager.AiManager.CustomAis.Values)
                {
                    AttachMarker(baseAi.transform, Vector3.zero, baseAi.DebugHighlightColor, "AiDebugMarker", 100);
                }
            }
            else
            {
                string name = args[0];
                foreach (CustomBaseAi baseAi in mManager.AiManager.CustomAis.Values)
                {
                    if ($"{baseAi.BaseAi.GetHashCode()}" == name || $"{baseAi.ModDataProxy.Guid}" == name)
                    {
                        AttachMarker(baseAi.transform, Vector3.zero, baseAi.DebugHighlightColor, "AiDebugMarker", 100);
                    }
                }
            }
        }

        private void HideAi(string[] args)
        {
            if (args.Length == 0)
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
                string name = args[0];
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

        private Color GetNavMeshColor(int layer)
        {
            switch (layer)
            {
                case 0: return Color.black;
                case 1: return Color.white;
                case 2: return Color.red;
                case 3: return Color.blue;
                case 4: return Color.green;
                case 5: return Color.grey;
                case 6: return Color.cyan;
                case 7: return Color.yellow;
                case 8: return Color.magenta;
                default: return new Color(100, 200, 70);
            }
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

        private void RegisterDefaultPaintManagers()
        {
            mManager.RegisterPaintManager(new WanderPathPaintManager(mManager));
            mManager.RegisterPaintManager(new HidingSpotPaintManager(mManager));
            mManager.RegisterPaintManager(new SpawnRegionPaintManager(mManager));
        }
    }
}
