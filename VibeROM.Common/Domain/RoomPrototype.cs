using VibeROM.Common.Rom;

namespace VibeROM.Common.Domain;

public sealed record ExitPrototype(
    Direction Direction,
    int ToRoomVnum,
    int Flags,
    int KeyVnum,
    string Description)
{
    public IReadOnlyList<string> Validate(string field)
    {
        var errors = new List<string>();
        if (!Enum.IsDefined(Direction))
        {
            errors.Add($"{field}.direction is invalid.");
        }

        DomainValidation.ValidateVnum(errors, $"{field}.toRoom", ToRoomVnum);
        DomainValidation.ValidateVnum(errors, $"{field}.keyVnum", KeyVnum);
        DomainValidation.ValidateMask(errors, $"{field}.flags", Flags, RomFlags.Exit);
        return errors;
    }
}

public sealed record RoomPrototype(
    int Vnum,
    string Name,
    string Description,
    int RoomFlags,
    Sector Sector,
    int HealRate,
    int ManaRate,
    int Clan,
    IReadOnlyDictionary<Direction, ExitPrototype> Exits,
    IReadOnlyList<ExtraDescription> ExtraDescriptions)
{
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        DomainValidation.ValidateVnum(errors, "vnum", Vnum);

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("name must not be empty.");
        }

        DomainValidation.ValidateMask(errors, "roomFlags", RoomFlags, RomFlags.Room);

        if (!Enum.IsDefined(Sector))
        {
            errors.Add("sector is invalid.");
        }

        if (HealRate < 0) errors.Add("healRate must not be negative.");
        if (ManaRate < 0) errors.Add("manaRate must not be negative.");
        if (Clan < 0) errors.Add("clan must not be negative.");

        foreach (var (direction, exit) in Exits)
        {
            if (direction != exit.Direction)
            {
                errors.Add($"exit key '{direction}' does not match exit direction '{exit.Direction}'.");
            }

            errors.AddRange(exit.Validate($"exits.{direction}"));
        }

        for (var index = 0; index < ExtraDescriptions.Count; index++)
        {
            errors.AddRange(ExtraDescriptions[index].Validate($"extraDescriptions[{index}]"));
        }

        return errors;
    }
}
