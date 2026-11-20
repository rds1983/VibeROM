namespace VibeROM.Common.Text;

/// <summary>Ports of ROM's case-insensitive string helpers and small text transformations.</summary>
public static class RomStrings
{
    /// <summary>Case-insensitive equality, equivalent to a negated ROM <c>str_cmp</c>.</summary>
    public static bool EqualsIgnoreCase(string? left, string? right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    /// <summary>Case-insensitive prefix test, equivalent to a negated ROM <c>str_prefix</c>.</summary>
    public static bool HasPrefixIgnoreCase(string? value, string? prefix) =>
        value is not null && prefix is not null
        && value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>Case-insensitive substring test, equivalent to a negated ROM <c>str_infix</c>.</summary>
    public static bool ContainsIgnoreCase(string? value, string? needle) =>
        value is not null && needle is not null
        && value.Contains(needle, StringComparison.OrdinalIgnoreCase);

    /// <summary>Case-insensitive suffix test, equivalent to a negated ROM <c>str_suffix</c>.</summary>
    public static bool HasSuffixIgnoreCase(string? value, string? suffix) =>
        value is not null && suffix is not null
        && value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);

    /// <summary>Replaces tildes with hyphens, as ROM does before writing user text.</summary>
    public static string SmashTilde(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Replace('~', '-');
    }

    /// <summary>Lowercases the whole string and uppercases the first character.</summary>
    public static string Capitalize(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length == 0)
        {
            return value;
        }

        var characters = value.ToLowerInvariant().ToCharArray();
        characters[0] = char.ToUpperInvariant(characters[0]);
        return new string(characters);
    }
}
