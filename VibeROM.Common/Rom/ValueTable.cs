namespace VibeROM.Common.Rom;

/// <summary>A validated canonical name / numeric value table.</summary>
public sealed class ValueTable
{
    private readonly IReadOnlyList<ValueDefinition> _entries;
    private readonly Dictionary<string, ValueDefinition> _byName;
    private readonly Dictionary<int, ValueDefinition> _byValue;

    public ValueTable(string category, IEnumerable<ValueDefinition> entries)
    {
        Category = category;
        _entries = entries.OrderBy(entry => entry.Value).ToArray();

        if (_entries.Count == 0)
        {
            throw new InvalidOperationException($"Value table '{category}' is empty.");
        }

        _byName = new Dictionary<string, ValueDefinition>(StringComparer.OrdinalIgnoreCase);
        _byValue = new Dictionary<int, ValueDefinition>();

        foreach (var entry in _entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                throw new InvalidOperationException($"Value table '{category}' contains an empty name.");
            }

            if (!_byName.TryAdd(entry.Name, entry))
            {
                throw new InvalidOperationException(
                    $"Value table '{category}' has duplicate name '{entry.Name}'.");
            }

            if (!_byValue.TryAdd(entry.Value, entry))
            {
                throw new InvalidOperationException(
                    $"Value table '{category}' has duplicate value {entry.Value} for '{entry.Name}'.");
            }
        }
    }

    public string Category { get; }

    public IReadOnlyList<ValueDefinition> Entries => _entries;

    public IReadOnlyList<string> Names => _entries.Select(entry => entry.Name).ToArray();

    public ValueDefinition Get(string name)
    {
        if (TryGet(name, out var entry))
        {
            return entry;
        }

        throw new ArgumentException($"Unknown {Category} name '{name}'.", nameof(name));
    }

    public ValueDefinition Get(int value)
    {
        if (TryGet(value, out var entry))
        {
            return entry;
        }

        throw new ArgumentException($"Unknown {Category} value {value}.", nameof(value));
    }

    public bool TryGet(string name, out ValueDefinition entry) =>
        _byName.TryGetValue(name, out entry!);

    public bool TryGet(int value, out ValueDefinition entry) =>
        _byValue.TryGetValue(value, out entry!);

    public static ValueTable FromEnum<TEnum>(string category)
        where TEnum : struct, Enum =>
        new(
            category,
            Enum.GetValues<TEnum>()
                .Select(value => new ValueDefinition(
                    Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture),
                    value.ToString())));
}
