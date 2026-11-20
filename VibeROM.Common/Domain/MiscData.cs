namespace VibeROM.Common.Domain;

/// <summary>Per-level kill counters used by resurrect/XP bookkeeping.</summary>
public sealed class KillData
{
    public int Number { get; set; }

    public int Killed { get; set; }
}

/// <summary>One remembered attacker for mobile AI.</summary>
public sealed record MemoryData(string Name, DateTimeOffset When);

/// <summary>Character-generation selections used by the creation flow.</summary>
public sealed class GenerationData
{
    public HashSet<string> ChosenSkills { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> ChosenGroups { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public int PointsChosen { get; set; }
}

public sealed record NoteData(
    int Id,
    string Sender,
    DateTimeOffset Date,
    IReadOnlyList<string> Recipients,
    string Subject,
    string Text,
    DateTimeOffset? ExpiresAt = null);

public sealed record BanData(string Name, int Flags, int Level);

public sealed class Weather
{
    public int Pressure { get; set; } = 960;

    public int Change { get; set; }

    public SkyState Sky { get; set; } = SkyState.Cloudless;

    public SunState Sunlight { get; set; } = SunState.Light;
}
