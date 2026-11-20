namespace VibeROM.Common.Domain;

public sealed record AreaData(
    string Name,
    string Credits,
    int LowVnum,
    int HighVnum,
    int Security,
    string Builders,
    string? LegacyFile,
    IReadOnlyList<HelpEntry> Helps,
    IReadOnlyList<MobilePrototype> Mobiles,
    IReadOnlyList<ObjectPrototype> Objects,
    IReadOnlyList<RoomPrototype> Rooms,
    IReadOnlyList<Reset> Resets,
    IReadOnlyList<ShopData> Shops,
    IReadOnlyList<SpecialData> Specials,
    IReadOnlyList<SocialData> Socials)
{
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("name must not be empty.");
        }

        if (LowVnum < 0) errors.Add("vnumRange.low must not be negative.");
        if (HighVnum < 0) errors.Add("vnumRange.high must not be negative.");
        if (HighVnum < LowVnum) errors.Add("vnumRange.high must be greater than or equal to vnumRange.low.");

        if (Security is < 0 or > 9)
        {
            errors.Add("security must be between 0 and 9.");
        }

        ValidateUniqueVnums(errors, "mobiles", Mobiles.Select(mobile => mobile.Vnum));
        ValidateUniqueVnums(errors, "objects", Objects.Select(obj => obj.Vnum));
        ValidateUniqueVnums(errors, "rooms", Rooms.Select(room => room.Vnum));

        for (var index = 0; index < Helps.Count; index++)
        {
            errors.AddRange(Helps[index].Validate($"helps[{index}]"));
        }

        for (var index = 0; index < Mobiles.Count; index++)
        {
            errors.AddRange(Mobiles[index].Validate().Select(error => $"mobiles[{index}].{error}"));
        }

        for (var index = 0; index < Objects.Count; index++)
        {
            errors.AddRange(Objects[index].Validate().Select(error => $"objects[{index}].{error}"));
        }

        for (var index = 0; index < Rooms.Count; index++)
        {
            errors.AddRange(Rooms[index].Validate().Select(error => $"rooms[{index}].{error}"));
        }

        for (var index = 0; index < Resets.Count; index++)
        {
            errors.AddRange(Resets[index].Validate($"resets[{index}]"));
        }

        for (var index = 0; index < Shops.Count; index++)
        {
            errors.AddRange(Shops[index].Validate($"shops[{index}]"));
        }

        for (var index = 0; index < Specials.Count; index++)
        {
            errors.AddRange(Specials[index].Validate($"specials[{index}]"));
        }

        var socialNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < Socials.Count; index++)
        {
            errors.AddRange(Socials[index].Validate($"socials[{index}]"));
            if (!socialNames.Add(Socials[index].Name))
            {
                errors.Add($"socials[{index}].name '{Socials[index].Name}' is duplicated.");
            }
        }

        return errors;
    }

    private static void ValidateUniqueVnums(
        ICollection<string> errors,
        string section,
        IEnumerable<int> vnums)
    {
        var seen = new HashSet<int>();
        foreach (var vnum in vnums)
        {
            if (!seen.Add(vnum))
            {
                errors.Add($"{section} contains duplicate vnum {vnum}.");
            }
        }
    }
}
