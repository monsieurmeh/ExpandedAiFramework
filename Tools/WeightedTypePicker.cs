namespace ExpandedAiFramework
{
    public class WeightedTypePicker<T>
    {
        private class Entry
        {
            public Type Type { get; }
            public Func<float> WeightProvider { get; }
            public Func<T, bool> Condition { get; }

            public Entry(Type type, Func<float> weightProvider, Func<T, bool> condition)
            {
                Type = type;
                WeightProvider = weightProvider;
                Condition = condition;
            }
        }

        private readonly List<Entry> allEntries = new();
        private readonly Random random = new();

        private List<(Type Type, float Weight)> validEntries = new();
        private float totalValidWeight = 0;


        public void AddWeight(Type type, Func<float> weightProvider, Func<T, bool> condition)
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
                //Log($"Checking {entry.Type} in WeightedTypePicker. Condition is {entry.Condition(t)}, weight is {entry.WeightProvider()}");
                if (entry.Condition(t))
                {
                    float weight = entry.WeightProvider();
                    if (weight > 0)
                    {
                        validEntries.Add((entry.Type, weight));
                        totalValidWeight += weight;
                    }
                }
            }

            if (validEntries.Count == 0 || totalValidWeight <= 0)
            {
                LogError("WeightedTypePicker could not pick a valid spawn type!", ComplexLogger.FlaggedLoggingLevel.Critical);
                return typeof(void);
            }

            float roll = (float)(random.NextDouble() * totalValidWeight);
            float cumulative = 0;

            foreach (var (type, weight) in validEntries)
            {
                cumulative += weight;
                if (roll <= cumulative)
                    return type;
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
