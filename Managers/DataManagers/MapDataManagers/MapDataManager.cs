using MelonLoader.Utils;


namespace ExpandedAiFramework
{
    public class MapDataManager<T> : SubDataManager<T>, IMapDataManager where T : MapData, new()
    {
        public MapDataManager(DataManager manager, DispatchManager dispatcher) : base(manager, dispatcher) { }

        public override string TypeInfo { get { return $"MapDataManager<{typeof(T).Name}>"; } }
        protected override string GetDefaultDataPath() => Path.Combine(DataFolderPath, $"{nameof(T)}s.json");
        protected override string LoadJsonFromPath(string dataLocation) => File.ReadAllText(Path.Combine(MelonEnvironment.ModsDirectory, dataLocation), System.Text.Encoding.UTF8);
        protected override void SaveJsonToPath(string json, string dataLocation) => File.WriteAllText(Path.Combine(MelonEnvironment.ModsDirectory, dataLocation), json, System.Text.Encoding.UTF8);
    
    }
}
