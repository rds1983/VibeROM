using VibeROM.Common.Domain;

namespace VibeROM.Server.Communication;

/// <summary>Logged-in characters, keyed by name, shared by login and commands.</summary>
public sealed class OnlinePlayers
{
    private readonly Dictionary<string, Descriptor> _byName =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();

    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _byName.Count;
            }
        }
    }

    public void Add(Descriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (descriptor.Character is not { } character)
        {
            return;
        }

        lock (_gate)
        {
            _byName[character.Name] = descriptor;
        }
    }

    public void Remove(Descriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (descriptor.Character is not { } character)
        {
            return;
        }

        lock (_gate)
        {
            if (_byName.TryGetValue(character.Name, out var current)
                && ReferenceEquals(current, descriptor))
            {
                _byName.Remove(character.Name);
            }
        }
    }

    public bool TryGet(string name, out Descriptor descriptor)
    {
        lock (_gate)
        {
            return _byName.TryGetValue(name, out descriptor!);
        }
    }

    public IReadOnlyList<Descriptor> Snapshot()
    {
        lock (_gate)
        {
            return _byName.Values.ToArray();
        }
    }

    /// <summary>ROM <c>get_char_world</c> prefix lookup over online player names.</summary>
    public Character? FindByPrefix(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        lock (_gate)
        {
            foreach (var (candidate, descriptor) in _byName)
            {
                if (candidate.StartsWith(name, StringComparison.OrdinalIgnoreCase)
                    && descriptor.Character is { } character)
                {
                    return character;
                }
            }
        }

        return null;
    }
}
