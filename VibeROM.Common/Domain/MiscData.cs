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

/// <summary>ROM calendar time.</summary>
public sealed record GameTime(int Hour, int Day, int Month, int Year)
{
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (Hour is < 0 or > 23) errors.Add("hour must be between 0 and 23.");
        if (Day < 1) errors.Add("day must be at least 1.");
        if (Month is < 1 or > 12) errors.Add("month must be between 1 and 12.");
        if (Year < 0) errors.Add("year must not be negative.");
        return errors;
    }
}

public sealed class Weather
{
    public int Pressure { get; set; } = 960;

    public int Change { get; set; }

    public SkyState Sky { get; set; } = SkyState.Cloudless;

    public SunState Sunlight { get; set; } = SunState.Light;
}
