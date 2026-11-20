using VibeROM.Common.Rom;

namespace VibeROM.Common.Domain;

internal static class DomainValidation
{
    public static void ValidateMask(
        ICollection<string> errors,
        string field,
        int mask,
        FlagRegistry registry)
    {
        var allowed = registry.PersistedDefinitions.Aggregate(
            0,
            (current, definition) => current | definition.Bit);

        if ((mask & ~allowed) != 0)
        {
            errors.Add($"{field} contains unknown {registry.Category} bits.");
        }
    }

    public static void ValidateVnum(ICollection<string> errors, string field, int vnum)
    {
        if (vnum < 0)
        {
            errors.Add($"{field} must not be negative.");
        }
    }

    public static void ValidateKeywords(ICollection<string> errors, string field, string keywords)
    {
        if (string.IsNullOrWhiteSpace(keywords))
        {
            errors.Add($"{field} must not be empty.");
        }
    }
}
