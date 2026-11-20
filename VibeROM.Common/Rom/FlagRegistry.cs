namespace VibeROM.Common.Rom;

/// <summary>A validated collection of named flag bits.</summary>
public sealed class FlagRegistry
{
    private readonly IReadOnlyList<FlagDefinition> _definitions;
    private readonly Dictionary<string, FlagDefinition> _byName;

    public FlagRegistry(string category, IEnumerable<FlagDefinition> definitions)
    {
        Category = category;
        _definitions = definitions.OrderBy(definition => definition.Bit).ToArray();

        if (_definitions.Count == 0)
        {
            throw new InvalidOperationException($"Flag registry '{category}' is empty.");
        }

        var bits = new HashSet<int>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in _definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.Name))
            {
                throw new InvalidOperationException($"Flag registry '{category}' contains an empty name.");
            }

            if (definition.Bit <= 0 || (definition.Bit & (definition.Bit - 1)) != 0)
            {
                throw new InvalidOperationException(
                    $"Flag registry '{category}' has a non-power-of-two bit 0x{definition.Bit:X} for '{definition.Name}'.");
            }

            if (!bits.Add(definition.Bit))
            {
                throw new InvalidOperationException(
                    $"Flag registry '{category}' has duplicate bit 0x{definition.Bit:X} for '{definition.Name}'.");
            }

            if (!names.Add(definition.Name))
            {
                throw new InvalidOperationException(
                    $"Flag registry '{category}' has duplicate name '{definition.Name}'.");
            }
        }

        _byName = _definitions.ToDictionary(
            definition => definition.Name,
            StringComparer.OrdinalIgnoreCase);
    }

    public string Category { get; }

    public IReadOnlyList<FlagDefinition> Definitions => _definitions;

    public IReadOnlyList<FlagDefinition> PersistedDefinitions =>
        _definitions.Where(definition => definition.Persisted).ToArray();

    public IReadOnlyList<string> PersistedNames =>
        PersistedDefinitions.Select(definition => definition.Name).ToArray();

    public IReadOnlyList<string> AllNames =>
        _definitions.Select(definition => definition.Name).ToArray();

    public FlagDefinition Get(string name)
    {
        if (TryGet(name, out var definition))
        {
            return definition;
        }

        throw new ArgumentException($"Unknown {Category} flag '{name}'.", nameof(name));
    }

    public bool TryGet(string name, out FlagDefinition definition) =>
        _byName.TryGetValue(name, out definition!);

    public int GetMask(IEnumerable<string> names)
    {
        var mask = 0;
        foreach (var name in names)
        {
            mask |= Get(name).Bit;
        }

        return mask;
    }

    public IReadOnlyList<string> GetNames(int mask, bool persistedOnly = true)
    {
        var result = new List<string>();
        foreach (var definition in _definitions)
        {
            if ((mask & definition.Bit) == 0)
            {
                continue;
            }

            if (persistedOnly && !definition.Persisted)
            {
                continue;
            }

            result.Add(definition.Name);
        }

        return result;
    }

    public bool HasFlag(int mask, string name) => (mask & Get(name).Bit) != 0;
}
