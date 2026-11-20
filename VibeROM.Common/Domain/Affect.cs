using VibeROM.Common.Rom;

namespace VibeROM.Common.Domain;

/// <summary>Immutable affect stored on a prototype.</summary>
public sealed record AffectData(
    AffectWhere Where,
    ApplyLocation Location,
    int Modifier,
    int Bits = 0,
    int Level = 0,
    int Duration = -1,
    int Type = -1)
{
    public IReadOnlyList<string> Validate(string field)
    {
        var errors = new List<string>();
        if (Level < 0)
        {
            errors.Add($"{field}.level must not be negative.");
        }

        var registry = RegistryFor(Where);
        if (registry is null)
        {
            if (Bits != 0)
            {
                errors.Add($"{field}.flags is not allowed for where '{Where}'.");
            }
        }
        else if ((Bits & ~registry.PersistedDefinitions.Aggregate(0, (mask, definition) => mask | definition.Bit)) != 0)
        {
            errors.Add($"{field}.flags contains unknown {registry.Category} bits.");
        }

        return errors;
    }

    internal static FlagRegistry? RegistryFor(AffectWhere where) =>
        where switch
        {
            AffectWhere.Affect => RomFlags.Affect,
            AffectWhere.Immune or AffectWhere.Resist or AffectWhere.Vulnerable => RomFlags.Immunity,
            AffectWhere.Weapon => RomFlags.WeaponSpecial,
            _ => null
        };
}

/// <summary>Mutable affect on a live character, object or room.</summary>
public sealed class Affect
{
    public AffectWhere Where { get; set; } = AffectWhere.Affect;

    /// <summary>Skill/gsn index, or -1 for object/room affects.</summary>
    public int Type { get; set; } = -1;

    public int Level { get; set; }

    public int Duration { get; set; } = -1;

    public ApplyLocation Location { get; set; } = ApplyLocation.None;

    public int Modifier { get; set; }

    public int Bits { get; set; }

    public static Affect From(AffectData data) =>
        new()
        {
            Where = data.Where,
            Type = data.Type,
            Level = data.Level,
            Duration = data.Duration,
            Location = data.Location,
            Modifier = data.Modifier,
            Bits = data.Bits
        };

    public Affect Clone() => (Affect)MemberwiseClone();

    public AffectData ToData() => new(Where, Location, Modifier, Bits, Level, Duration, Type);

    public IReadOnlyList<string> Validate(string field) => ToData().Validate(field);
}
