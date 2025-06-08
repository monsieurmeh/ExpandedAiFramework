

namespace ExpandedAiFramework
{
    public class WanderPathManager : MapDataManager<WanderPath>
    {
        public WanderPathManager(DataManager manager) : base(manager) { }
        protected override bool ValidEntry(MapDataRequest<WanderPath> request, WanderPath path)
        {
            bool customSuccess = request.Args == null;
            customSuccess = request.Args.Length == 0 || customSuccess;
            customSuccess = (WanderPathTypes)request.Args[0] == path.WanderPathType || customSuccess;
            return customSuccess && base.ValidEntry(request, path);
        }
    }
}
