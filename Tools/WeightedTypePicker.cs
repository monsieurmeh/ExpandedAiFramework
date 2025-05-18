

namespace ExpandedAiFramework
{
    public class WeightedTypePicker<T>
    {
        private class Entry
        {
            public Type Type { get; }
            public Func<int> WeightProvider { get; }
            public Func<T, bool> Condition { get; }

            public Entry(Type type, Func<int> weightProvider, Func<T, bool> condition)
            {
                Type = type;
                WeightProvider = weightProvider;
                Condition = condition;
            }
        }

        private readonly List<Entry> allEntries = new();
        private readonly Random random = new();

        private List<(Type Type, int Weight)> validEntries = new();
        private float totalValidWeight = 0;


        public void AddWeight(Type type, Func<int> weightProvider, Func<T, bool> condition)
        {
            if (weightProvider == null)
                throw new ArgumentNullException(nameof(weightProvider));

            allEntries.Add(new Entry(type, weightProvider, condition));
        }


        public Type PickType(T t)
        {
            validEntries.Clear();
            totalValidWeight = 0;

            foreach (var entry in allEntries)
            {
                LogDebug($"Checking {entry.Type} in WeightedTypePicker. Condition is {entry.Condition(t)}, weight is {entry.WeightProvider()}");
                if (entry.Condition(t))
                {
                    int weight = entry.WeightProvider();
                    if (weight > 0)
                    {
                        validEntries.Add((entry.Type, weight));
                        totalValidWeight += weight;
                        LogDebug($"Add {entry.Type} to WeightedTypePicker valid pool. Compiled pool weight is now {totalValidWeight} with {validEntries.Count} entries.");
                    }
                }
            }

            if (validEntries.Count == 0 || totalValidWeight <= 0)
            {
                LogError("WeightedTypePicker could not pick a valid spawn type!", ComplexLogger.FlaggedLoggingLevel.Critical);
                return typeof(void);
            }

            int roll = (int)(random.NextDouble() * totalValidWeight);
            float cumulative = 0;

            LogDebug($"Rolled {roll}, checking against pool.");
            foreach (var (type, weight) in validEntries)
            {
                cumulative += weight;
                LogDebug($"Added {type}, cumulative weight is {cumulative}");
                if (roll <= cumulative)
                {

                    LogDebug($"Cumulative weight {cumulative} >=roll {roll}, picking type {type}!");
                    return type;
                }
            }

            return validEntries[0].Type; // fallback
        }

        public void Clear()
        {
            allEntries.Clear();
            validEntries.Clear();
            totalValidWeight = 0;
        }
    }
}
